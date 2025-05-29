using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Factories;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Processes batches of log messages from a queue and sends them to one or more log targets.
    /// Optimized for use with Unity Collections v2 with robust error handling and 
    /// resource management. Supports custom formatting through ILogFormatter.
    /// 
    /// Thread Safety:
    /// - Enqueue: Thread-safe if using external queue with ParallelWriter
    /// - Flush: Not thread-safe, should be called from a single thread
    /// - Target Management: Not thread-safe, modify targets only from the main thread
    /// </summary>
    [BurstCompile]
    public class LogBatchProcessor : IDisposable
    {
        /// <summary>
        /// The targets that will receive processed log messages.
        /// </summary>
        private readonly List<ILogTarget> _targets;
        
        /// <summary>
        /// Legacy burstLogger for backward compatibility.
        /// </summary>
        private readonly IBurstLogger _burstLogger;

        /// <summary>
        /// Indicates if this processor is using legacy burstLogger mode.
        /// </summary>
        private readonly bool _isLegacyMode;
        
        /// <summary>
        /// Queue of log messages waiting to be processed.
        /// </summary>
        private readonly NativeQueue<LogMessage> _queue;
        
        /// <summary>
        /// Custom formatter to use when formatting log messages.
        /// </summary>
        private readonly ILogFormatter _formatter;
        
        /// <summary>
        /// Temporary buffer used to batch log messages for targets.
        /// </summary>
        private readonly NativeList<LogMessage> _batchBuffer;
        
        /// <summary>
        /// Flag to track if this processor has been disposed.
        /// </summary>
        private bool _isDisposed;
        
        /// <summary>
        /// Whether this processor owns the queue and should dispose of it.
        /// </summary>
        private readonly bool _ownsQueue;
        
        /// <summary>
        /// The maximum number of messages to process in a single flush operation to prevent frame spikes.
        /// </summary>
        private readonly int _maxMessagesPerFlush;
        
        /// <summary>
        /// Size of the last processed batch, used for adaptive buffer sizing.
        /// </summary>
        private int _lastBatchSize;

        /// <summary>
        /// Message bus for publishing log-related messages.
        /// </summary>
        private readonly IMessageBus _messageBus;
        
        #region Multi-target Constructors

        /// <summary>
        /// Creates a new LogBatchProcessor instance with multiple log targets.
        /// </summary>
        /// <param name="targets">Collection of log targets to receive messages.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if targets, formatter, or messageBus is null.</exception>
        /// <exception cref="ArgumentException">Thrown if targets collection is empty.</exception>
        public LogBatchProcessor(IEnumerable<ILogTarget> targets, ILogFormatter formatter, IMessageBus messageBus, int initialCapacity = 64, int maxMessagesPerFlush = 200)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            _isLegacyMode = false;
            _burstLogger = null;
            _formatter = formatter;
            _lastBatchSize = 0;
            _messageBus = messageBus;
            
            // Create owned collections
            _targets = new List<ILogTarget>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    _targets.Add(target);
                }
            }
            
            if (_targets.Count == 0)
                throw new ArgumentException("At least one non-null log target must be provided", nameof(targets));
                
            _queue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _batchBuffer = new NativeList<LogMessage>(Math.Min(initialCapacity, 64), Allocator.Persistent);
            
            _ownsQueue = true;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with a single log target.
        /// </summary>
        /// <param name="target">Log target to receive messages.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, formatter, or messageBus is null.</exception>
        public LogBatchProcessor(ILogTarget target, ILogFormatter formatter, IMessageBus messageBus, int initialCapacity = 64, int maxMessagesPerFlush = 200)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            _isLegacyMode = false;
            _burstLogger = null;
            _formatter = formatter;
            _lastBatchSize = 0;
            _messageBus = messageBus;
            
            // Create owned collections
            _targets = new List<ILogTarget> { target };
            
            _queue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _batchBuffer = new NativeList<LogMessage>(Math.Min(initialCapacity, 64), Allocator.Persistent);
            
            _ownsQueue = true;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with multiple log targets and an externally managed queue.
        /// </summary>
        /// <param name="targets">Collection of log targets to receive messages.</param>
        /// <param name="queue">Queue of messages to process.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if targets, formatter, or messageBus is null.</exception>
        /// <exception cref="ArgumentException">Thrown if targets collection is empty.</exception>
        public LogBatchProcessor(IEnumerable<ILogTarget> targets, NativeQueue<LogMessage> queue, ILogFormatter formatter, IMessageBus messageBus, int maxMessagesPerFlush = 200)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            _isLegacyMode = false;
            _burstLogger = null;
            _formatter = formatter;
            _lastBatchSize = 0;
            _messageBus = messageBus;
            
            // Create owned targets list
            _targets = new List<ILogTarget>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    _targets.Add(target);
                }
            }
            
            if (_targets.Count == 0)
                throw new ArgumentException("At least one non-null log target must be provided", nameof(targets));
            
            // Use externally managed queue
            _queue = queue;
            _batchBuffer = new NativeList<LogMessage>(Math.Min(64, maxMessagesPerFlush), Allocator.Persistent);
            
            _ownsQueue = false;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        #endregion
        
        #region Legacy BurstLogger Constructors
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with an externally managed queue and custom formatter.
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="queue">Queue of messages to process.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger, formatter, or messageBus is null.</exception>
        public LogBatchProcessor(IBurstLogger burstLogger, NativeQueue<LogMessage> queue, ILogFormatter formatter, IMessageBus messageBus, int maxMessagesPerFlush = 200)
        {
            _burstLogger = burstLogger ?? throw new ArgumentNullException(nameof(burstLogger));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            _isLegacyMode = true;
            _targets = new List<ILogTarget>();
            _batchBuffer = new NativeList<LogMessage>(Math.Min(64, maxMessagesPerFlush), Allocator.Persistent);
            _lastBatchSize = 0;
            
            _queue = queue;
            _ownsQueue = false;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with its own internal queue and custom formatter.
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger, formatter, or messageBus is null.</exception>
        public LogBatchProcessor(IBurstLogger burstLogger, ILogFormatter formatter, IMessageBus messageBus, int initialCapacity = 64, int maxMessagesPerFlush = 200)
        {
            _burstLogger = burstLogger ?? throw new ArgumentNullException(nameof(burstLogger));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            _isLegacyMode = true;
            _targets = new List<ILogTarget>();
            _batchBuffer = new NativeList<LogMessage>(Math.Min(initialCapacity, 64), Allocator.Persistent);
            _lastBatchSize = 0;
            
            _queue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _ownsQueue = true;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        #endregion
        
        #region Modern Factory Methods (Using Builders)
        
        /// <summary>
        /// Creates a LogBatchProcessor using configuration builders. This is the recommended approach
        /// for new code as it provides better configuration management and consistency.
        /// </summary>
        /// <param name="builderConfigurator">Action to configure log target builders.</param>
        /// <param name="formatter">Optional formatter. Uses DefaultLogFormatter if null.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <returns>A new LogBatchProcessor instance configured with the specified targets.</returns>
        /// <exception cref="ArgumentNullException">Thrown if builderConfigurator or messageBus is null.</exception>
        public static LogBatchProcessor CreateWithBuilders(
            Action<TargetBuilderCollection> builderConfigurator,
            ILogFormatter formatter = null,
            IMessageBus messageBus = null,
            int initialCapacity = 64,
            int maxMessagesPerFlush = 200)
        {
            if (builderConfigurator == null)
                throw new ArgumentNullException(nameof(builderConfigurator));
            
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            var collection = new TargetBuilderCollection();
            builderConfigurator(collection);
            
            var targets = collection.BuildTargets();
            var actualFormatter = formatter ?? new DefaultLogFormatter();
            
            return new LogBatchProcessor(targets, actualFormatter, messageBus, initialCapacity, maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a LogBatchProcessor optimized for development with console and file logging.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <returns>A LogBatchProcessor configured for development use.</returns>
        public static LogBatchProcessor CreateForDevelopment(
            string logFilePath = "Logs/debug.log",
            IMessageBus messageBus = null,
            int initialCapacity = 64,
            int maxMessagesPerFlush = 200)
        {
            return CreateWithBuilders(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFileDebug(logFilePath));
                    builders.AddUnityConsole(LogConfigBuilderFactory.UnityConsoleDevelopment());
                },
                null, // Use default formatter
                messageBus,
                initialCapacity,
                maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a LogBatchProcessor optimized for production with performance-focused settings.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <returns>A LogBatchProcessor configured for production use.</returns>
        public static LogBatchProcessor CreateForProduction(
            string logFilePath = "Logs/app.log",
            IMessageBus messageBus = null,
            int initialCapacity = 128,
            int maxMessagesPerFlush = 500)
        {
            return CreateWithBuilders(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFileHighPerformance(logFilePath));
                    builders.AddUnityConsole(LogConfigBuilderFactory.UnityConsoleProduction());
                },
                null, // Use default formatter
                messageBus,
                initialCapacity,
                maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a LogBatchProcessor with console output only, useful for lightweight scenarios.
        /// </summary>
        /// <param name="useColorizedOutput">Whether to use colorized console output.</param>
        /// <param name="minimumLevel">Minimum log level for console output.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <returns>A LogBatchProcessor configured for console-only logging.</returns>
        public static LogBatchProcessor CreateConsoleOnly(
            bool useColorizedOutput = true,
            LogLevel minimumLevel = LogLevel.Info,
            IMessageBus messageBus = null,
            int initialCapacity = 64,
            int maxMessagesPerFlush = 200)
        {
            return CreateWithBuilders(
                builders =>
                {
                    builders.AddUnityConsole(LogConfigBuilderFactory.UnityConsole()
                        .WithColorizedOutput(useColorizedOutput)
                        .WithMinimumLevel(minimumLevel));
                },
                null, // Use default formatter
                messageBus,
                initialCapacity,
                maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a LogBatchProcessor with file output only, useful when console output is not desired.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="minimumLevel">Minimum log level for file output.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <returns>A LogBatchProcessor configured for file-only logging.</returns>
        public static LogBatchProcessor CreateFileOnly(
            string logFilePath = "Logs/app.log",
            LogLevel minimumLevel = LogLevel.Debug,
            IMessageBus messageBus = null,
            int initialCapacity = 64,
            int maxMessagesPerFlush = 200)
        {
            return CreateWithBuilders(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFile(logFilePath)
                        .WithMinimumLevel(minimumLevel));
                },
                null, // Use default formatter
                messageBus,
                initialCapacity,
                maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a LogBatchProcessor optimized for maximum performance with minimal overhead.
        /// Uses buffering and reduced formatting for maximum throughput.
        /// </summary>
        /// <param name="logFilePath">Path for the log file.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue (larger for performance).</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation (larger for performance).</param>
        /// <returns>A LogBatchProcessor configured for high-performance logging.</returns>
        public static LogBatchProcessor CreateHighPerformance(
            string logFilePath = "Logs/hp.log",
            IMessageBus messageBus = null,
            int initialCapacity = 256,
            int maxMessagesPerFlush = 1000)
        {
            return CreateWithBuilders(
                builders =>
                {
                    builders.AddSerilogFile(LogConfigBuilderFactory.SerilogFileHighPerformance(logFilePath));
                    // No console output for maximum performance
                },
                null, // Use default formatter
                messageBus,
                initialCapacity,
                maxMessagesPerFlush);
        }

        /// <summary>
        /// Helper class for building multiple log targets using the builder pattern.
        /// </summary>
        public class TargetBuilderCollection
        {
            private readonly List<ILogTarget> _targets = new List<ILogTarget>();

            /// <summary>
            /// Adds a Serilog file target using the provided builder configuration.
            /// </summary>
            /// <param name="builder">Configured Serilog file builder.</param>
            /// <returns>This collection for method chaining.</returns>
            public TargetBuilderCollection AddSerilogFile(SerilogFileConfigBuilder builder)
            {
                if (builder != null)
                {
                    var config = builder.Build();
                    var target = LogTargetFactory.CreateSerilogTarget(config);
                    _targets.Add(target);
                }
                return this;
            }

            /// <summary>
            /// Adds a Unity console target using the provided builder configuration.
            /// </summary>
            /// <param name="builder">Configured Unity console builder.</param>
            /// <returns>This collection for method chaining.</returns>
            public TargetBuilderCollection AddUnityConsole(UnityConsoleConfigBuilder builder)
            {
                if (builder != null)
                {
                    var config = builder.Build();
                    var target = LogTargetFactory.CreateUnityConsoleTarget(config);
                    _targets.Add(target);
                }
                return this;
            }

            /// <summary>
            /// Adds a custom log target directly.
            /// </summary>
            /// <param name="target">The log target to add.</param>
            /// <returns>This collection for method chaining.</returns>
            public TargetBuilderCollection AddCustomTarget(ILogTarget target)
            {
                if (target != null)
                {
                    _targets.Add(target);
                }
                return this;
            }

            /// <summary>
            /// Builds and returns the collection of configured targets.
            /// </summary>
            /// <returns>List of configured log targets.</returns>
            internal List<ILogTarget> BuildTargets()
            {
                return new List<ILogTarget>(_targets);
            }
        }

        #endregion
        
        #region Legacy Factory Methods (For Backward Compatibility)
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with multiple targets and default formatter.
        /// Legacy method - consider using CreateWithBuilders for new code.
        /// </summary>
        /// <param name="targets">Collection of log targets to receive messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentException">Thrown if targets collection is empty.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            IEnumerable<ILogTarget> targets,
            IMessageBus messageBus,
            int initialCapacity = 64, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(targets, defaultFormatter, messageBus, initialCapacity, maxMessagesPerFlush);
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with a single target and default formatter.
        /// Legacy method - consider using CreateWithBuilders for new code.
        /// </summary>
        /// <param name="target">Log target to receive messages.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            ILogTarget target,
            IMessageBus messageBus,
            int initialCapacity = 64, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(target, defaultFormatter, messageBus, initialCapacity, maxMessagesPerFlush);
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with an externally managed queue and using
        /// the default formatting from LogMessage.FormatMessage().
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="queue">Queue of messages to process.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger is null.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            IBurstLogger burstLogger, 
            NativeQueue<LogMessage> queue,
            IMessageBus messageBus,
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(burstLogger, queue, defaultFormatter, messageBus, maxMessagesPerFlush);
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with its own internal queue and using
        /// the default formatting from LogMessage.FormatMessage().
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="messageBus">Message bus for publishing log-related messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger is null.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            IBurstLogger burstLogger,
            IMessageBus messageBus,
            int initialCapacity = 64, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(burstLogger, defaultFormatter, messageBus, initialCapacity, maxMessagesPerFlush);
        }
        
        #endregion
        
        /// <summary>
        /// Adds a new log target to this processor. Only available in multi-target mode.
        /// </summary>
        /// <param name="target">The log target to add.</param>
        /// <exception cref="InvalidOperationException">Thrown if processor is in legacy mode.</exception>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public void AddTarget(ILogTarget target)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            if (_isLegacyMode)
                throw new InvalidOperationException("Cannot add targets in legacy IBurstLogger mode");
                
            if (target == null)
                throw new ArgumentNullException(nameof(target));
                
            _targets.Add(target);
        }
        
        /// <summary>
        /// Removes a log target from this processor. Only available in multi-target mode.
        /// </summary>
        /// <param name="target">The log target to remove.</param>
        /// <returns>True if the target was found and removed, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if processor is in legacy mode.</exception>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public bool RemoveTarget(ILogTarget target)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            if (_isLegacyMode)
                throw new InvalidOperationException("Cannot remove targets in legacy IBurstLogger mode");
                
            if (target == null)
                throw new ArgumentNullException(nameof(target));
                
            return _targets.Remove(target);
        }
        
        /// <summary>
        /// Enqueues a log message for processing.
        /// </summary>
        /// <param name="message">The log message to enqueue.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public void Enqueue(LogMessage message)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            _queue.Enqueue(message);
            
            // Publish a message about message received
            try
            {
                _messageBus.PublishMessage(new LogEntryReceivedMessage(message));
            }
            catch
            {
                // Silently continue if message publishing fails
            }
        }
        
        /// <summary>
        /// Processes and forwards queued log messages to the configured targets.
        /// Limits the number of processed messages per call to prevent frame spikes.
        /// </summary>
        /// <returns>The number of messages processed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public int Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            int count = 0;
            int remaining = Math.Min(_queue.Count, _maxMessagesPerFlush);
            
            if (remaining == 0)
                return 0;
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Use different processing paths based on mode
                if (_isLegacyMode)
                {
                    count = FlushLegacy(remaining);
                }
                else
                {
                    count = FlushMultiTarget(remaining);
                }
            }
            catch (Exception ex)
            {
                // If we encounter a critical error, try to log it and abort this flush
                try
                {
                    if (_isLegacyMode && _burstLogger != null)
                    {
                        _burstLogger.Log(
                            (byte)Tagging.LogTag.Critical, 
                            $"Critical error during log flush: {ex.Message}", 
                            Tagging.LogTag.Critical.ToString()
                        );
                    }
                }
                catch
                {
                    // Silently fail if even this fails
                }
            }
            
            stopwatch.Stop();
            float processingTimeMs = stopwatch.ElapsedMilliseconds;
            
            // Publish a message about batch processing
            try
            {
                _messageBus.PublishMessage(new LogProcessingMessage(count, _queue.Count, processingTimeMs));
            }
            catch
            {
                // Silently continue if message publishing fails
            }
            
            return count;
        }
        
        /// <summary>
        /// Flush implementation for multi-target mode.
        /// </summary>
        /// <param name="maxToProcess">Maximum number of messages to process.</param>
        /// <returns>Number of messages processed.</returns>
        private int FlushMultiTarget(int maxToProcess)
        {
            int count = 0;

            // Clear buffer before use
            _batchBuffer.Clear();

            // Dequeue messages into the batch buffer
            while (count < maxToProcess && _queue.TryDequeue(out var log))
            {
                _batchBuffer.Add(log);
                count++;
            }

            if (count > 0)
            {
                // Process the batch through all targets
                foreach (var target in _targets)
                {
                    try
                    {
                        target.WriteBatch(_batchBuffer);
                    }
                    catch
                    {
                        // Continue with other targets if one fails
                    }
                }

                // Publish message for each processed log message
                foreach (var message in _batchBuffer)
                {
                    try
                    {
                        // Use "MultiTarget" as the target name since this is for multiple targets
                        _messageBus.PublishMessage(new LogEntryWrittenMessage(message, _targets.Count, "MultiTarget"));
                    }
                    catch
                    {
                        // Silently continue if message publishing fails
                    }
                }
            }

            _lastBatchSize = count;
            return count;
        }
        
        /// <summary>
        /// Flush implementation for legacy mode using IBurstLogger.
        /// </summary>
        /// <param name="maxToProcess">Maximum number of messages to process.</param>
        /// <returns>Number of messages processed.</returns>
        private int FlushLegacy(int maxToProcess)
        {
            int count = 0;
    
            while (count < maxToProcess && _queue.TryDequeue(out var log))
            {
                try
                {
                    // Use the custom formatter to format the message
                    FixedString512Bytes formattedMessage = _formatter.Format(log);
            
                    // Convert to string for the burstLogger interface
                    _burstLogger.Log(log.Level, formattedMessage.ToString(), log.GetTagString().ToString());
            
                    // Publish message about processing
                    try
                    {
                        // Use "BurstLogger" as the target name for legacy mode
                        _messageBus.PublishMessage(new LogEntryWrittenMessage(log, 1, "BurstLogger"));
                    }
                    catch
                    {
                        // Silently continue if message publishing fails
                    }
            
                    count++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other messages
                    try
                    {
                        _burstLogger.Log(
                            (byte)Tagging.LogTag.Error, 
                            $"Error processing log message: {ex.Message}", 
                            nameof(Tagging.LogTag.Error)
                        );
                    }
                    catch
                    {
                        // Last resort: if even logging the error fails, we have to silently fail
                        // to avoid causing more issues
                    }
                }
            }
    
            _lastBatchSize = count;
            return count;
        }
        
        /// <summary>
        /// Gets the current number of queued messages waiting to be processed.
        /// </summary>
        /// <returns>The count of queued messages.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public int GetQueuedMessageCount()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            return _queue.Count;
        }
        
        /// <summary>
        /// Disposes the processor and releases any resources owned by it.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Process any remaining logs before disposing
            try
            {
                // Keep flushing until queue is empty or we hit a reasonable limit
                const int maxFlushAttempts = 10;
                for (int i = 0; i < maxFlushAttempts && _queue.Count > 0; i++)
                {
                    Flush();
                }
            }
            catch
            {
                // Silently continue if flush fails during disposal
            }
            
            // Dispose of collections we own
            if (_ownsQueue && _queue.IsCreated)
            {
                _queue.Dispose();
            }
            
            if (_batchBuffer.IsCreated)
            {
                _batchBuffer.Dispose();
            }
            
            // Clear the targets list
            _targets.Clear();
            
            _isDisposed = true;
        }
        
        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~LogBatchProcessor()
        {
            Dispose();
        }
    }
}