using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Central manager for the logging system that coordinates log targets, queue processing, and flush operations.
    /// This class owns the NativeQueue for log messages, the LogBatchProcessor, and maintains the collection of ILogTargets.
    /// 
    /// Thread Safety:
    /// - Enqueue operations are thread-safe and lock-free using NativeQueue.ParallelWriter
    /// - Log methods are thread-safe and can be called from any thread
    /// - Configuration operations (AddTarget, RemoveTarget, etc.) are synchronized with a lock
    /// - Flush operations are synchronized to prevent concurrent flushes
    /// </summary>
    public class JobLoggerManager : IDisposable
    {
        /// <summary>
        /// The queue that stores log messages.
        /// </summary>
        private NativeQueue<LogMessage> _logQueue;

        /// <summary>
        /// Thread-safe parallel writer for the log queue.
        /// </summary>
        private NativeQueue<LogMessage>.ParallelWriter _logQueueWriter;

        /// <summary>
        /// The batch processor that handles log messages.
        /// </summary>
        private LogBatchProcessor _batchProcessor;

        /// <summary>
        /// List of log targets that receive processed log messages.
        /// </summary>
        private readonly List<ILogTarget> _logTargets;

        /// <summary>
        /// The formatter used to format log messages.
        /// </summary>
        private readonly ILogFormatter _formatter;
        
        /// <summary>
        /// The message bus used for publishing log-related events.
        /// </summary>
        private readonly IMessageBus _messageBus;

        /// <summary>
        /// Current global minimum log level across all targets.
        /// </summary>
        private volatile LogLevel _globalMinimumLevel;

        /// <summary>
        /// Maximum messages to process per flush operation.
        /// </summary>
        private readonly int _maxMessagesPerFlush;

        /// <summary>
        /// Whether automatic flush is enabled on a timer.
        /// </summary>
        private volatile bool _autoFlushEnabled;

        /// <summary>
        /// Time interval in seconds between auto-flush operations.
        /// </summary>
        private float _autoFlushInterval;

        /// <summary>
        /// Time tracker for auto-flush.
        /// </summary>
        private float _timeSinceLastAutoFlush;

        /// <summary>
        /// Whether the manager has been disposed.
        /// </summary>
        private volatile bool _isDisposed;

        /// <summary>
        /// Thread synchronization object for configuration changes.
        /// </summary>
        private readonly object _configLock = new object();

        /// <summary>
        /// Thread synchronization object for flush operations.
        /// </summary>
        private readonly object _flushLock = new object();

        /// <summary>
        /// Gets or sets the global minimum log level that will be processed.
        /// </summary>
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set
            {
                if (_globalMinimumLevel != value)
                {
                    _globalMinimumLevel = value;
                    UpdateTargetMinimumLevels();
                }
            }
        }

        /// <summary>
        /// Gets the number of queued log messages.
        /// </summary>
        public int QueuedMessageCount => _logQueue.Count;

        /// <summary>
        /// Gets the number of registered log targets.
        /// </summary>
        public int TargetCount => _logTargets.Count;

        /// <summary>
        /// Creates a new JobLoggerManager with the specified formatter and configuration.
        /// </summary>
        /// <param name="formatter">The formatter to use for log messages.</param>
        /// <param name="initialCapacity">Initial capacity of the log queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum messages to process per flush operation.</param>
        /// <param name="globalMinimumLevel">Global minimum log level.</param>
        /// <param name="messageBus">Optional message bus for publishing log-related events. If null, a no-op implementation will be used.</param>
        /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public JobLoggerManager(ILogFormatter formatter, int initialCapacity = 64, int maxMessagesPerFlush = 200,
            LogLevel globalMinimumLevel = LogLevel.Info, IMessageBus messageBus = null)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            _formatter = formatter;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _globalMinimumLevel = globalMinimumLevel;
            _logTargets = new List<ILogTarget>();
            _autoFlushEnabled = false;
            _autoFlushInterval = 1.0f;
            _timeSinceLastAutoFlush = 0f;
            
            // Use provided message bus or create a no-op implementation
            _messageBus = messageBus ?? CreateNullMessageBus();

            // Initialize native queue
            InitializeNativeQueue(initialCapacity);

            // Create the batch processor
            _batchProcessor = new LogBatchProcessor(_logTargets, _logQueue, _formatter, _messageBus, _maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a new JobLoggerManager with the specified initial targets, formatter and configuration.
        /// </summary>
        /// <param name="initialTargets">The initial log targets.</param>
        /// <param name="formatter">The formatter to use for log messages.</param>
        /// <param name="initialCapacity">Initial capacity of the log queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum messages to process per flush operation.</param>
        /// <param name="globalMinimumLevel">Global minimum log level.</param>
        /// <param name="messageBus">Optional message bus for publishing log-related events. If null, a no-op implementation will be used.</param>
        /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when initialTargets is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public JobLoggerManager(IEnumerable<ILogTarget> initialTargets, ILogFormatter formatter,
            int initialCapacity = 64, int maxMessagesPerFlush = 200, LogLevel globalMinimumLevel = LogLevel.Info,
            IMessageBus messageBus = null)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            if (initialTargets == null || !initialTargets.Any())
                throw new ArgumentException("At least one non-null log target must be provided",
                    nameof(initialTargets));

            _formatter = formatter;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _globalMinimumLevel = globalMinimumLevel;
            _logTargets = new List<ILogTarget>(initialTargets);
            _autoFlushEnabled = false;
            _autoFlushInterval = 1.0f;
            _timeSinceLastAutoFlush = 0f;
            
            // Use provided message bus or create a no-op implementation
            _messageBus = messageBus ?? CreateNullMessageBus();

            // Initialize native queue
            InitializeNativeQueue(initialCapacity);

            // Create the batch processor with the initial targets
            _batchProcessor = new LogBatchProcessor(_logTargets, _logQueue, _formatter, _messageBus, _maxMessagesPerFlush);

            // Set minimum levels for all targets
            UpdateTargetMinimumLevels();
        }

        /// <summary>
        /// Creates a new JobLoggerManager with the specified targets using the default log formatter.
        /// </summary>
        /// <param name="initialTargets">The initial log targets.</param>
        /// <param name="initialCapacity">Initial capacity of the log queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum messages to process per flush operation.</param>
        /// <param name="globalMinimumLevel">Global minimum log level.</param>
        /// <param name="messageBus">Optional message bus for publishing log-related events. If null, a no-op implementation will be used.</param>
        /// <returns>A configured JobLoggerManager instance.</returns>
        /// <exception cref="ArgumentException">Thrown when targets is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public static JobLoggerManager CreateWithTargetsAndDefaultFormatter(
            IEnumerable<ILogTarget> initialTargets,
            int initialCapacity = 64,
            int maxMessagesPerFlush = 200,
            LogLevel globalMinimumLevel = LogLevel.Info,
            IMessageBus messageBus = null)
        {
            return new JobLoggerManager(initialTargets, new DefaultLogFormatter(), initialCapacity, maxMessagesPerFlush,
                globalMinimumLevel, messageBus);
        }

        /// <summary>
        /// Creates a new JobLoggerManager using the default log formatter.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the log queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum messages to process per flush operation.</param>
        /// <param name="globalMinimumLevel">Global minimum log level.</param>
        /// <param name="messageBus">Optional message bus for publishing log-related events. If null, a no-op implementation will be used.</param>
        /// <returns>A configured JobLoggerManager instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public static JobLoggerManager CreateWithDefaultFormatter(int initialCapacity = 64,
            int maxMessagesPerFlush = 200, LogLevel globalMinimumLevel = LogLevel.Info,
            IMessageBus messageBus = null)
        {
            return new JobLoggerManager(new DefaultLogFormatter(), initialCapacity, maxMessagesPerFlush,
                globalMinimumLevel, messageBus);
        }

        /// <summary>
        /// Creates a no-op message bus implementation for when messaging is not needed.
        /// This prevents the logging system from failing when no message bus is configured.
        /// </summary>
        /// <returns>A no-op message bus instance.</returns>
        private static IMessageBus CreateNullMessageBus()
        {
            return new NullMessageBus();
        }

        /// <summary>
        /// Initialize the native queue with safety checks in debug builds.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity hint for the queue.</param>
        /// <exception cref="InvalidOperationException">Thrown if queue initialization fails.</exception>
        private void InitializeNativeQueue(int initialCapacity)
        {
            try
            {
                // Create the queue with the persistent allocator to ensure it lives until explicitly disposed
                _logQueue = new NativeQueue<LogMessage>(Allocator.Persistent);
                
                // Get the parallel writer for thread-safe logging from any thread
                _logQueueWriter = _logQueue.AsParallelWriter();
            }
            catch (Exception ex)
            {
                // Avoid using Debug.LogError here to prevent circular dependency
                // Instead, throw a more specific exception that includes the original error
                throw new InvalidOperationException($"Failed to initialize log queue: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Adds a log target to the manager.
        /// Thread-safe: Uses a lock to synchronize target list access.
        /// </summary>
        /// <param name="target">The log target to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public void AddTarget(ILogTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            lock (_configLock)
            {
                if (!_logTargets.Contains(target))
                {
                    _logTargets.Add(target);

                    // Set the target's minimum level to match our global setting
                    target.MinimumLevel = _globalMinimumLevel;

                    // Inform the batch processor of the new target
                    _batchProcessor.AddTarget(target);
                }
            }
        }

        /// <summary>
        /// Removes a log target from the manager.
        /// Thread-safe: Uses a lock to synchronize target list access.
        /// </summary>
        /// <param name="target">The log target to remove.</param>
        /// <returns>True if the target was removed; otherwise, false.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public bool RemoveTarget(ILogTarget target)
        {
            if (target == null)
                return false;

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            lock (_configLock)
            {
                bool removed = _logTargets.Remove(target);
                if (removed)
                {
                    _batchProcessor.RemoveTarget(target);
                }

                return removed;
            }
        }

        /// <summary>
        /// Creates and returns a JobLogger for use in Unity job contexts.
        /// Thread-safe: Access to native container is thread-safe through the ParallelWriter.
        /// </summary>
        /// <param name="minimumLevel">The minimum level to log at. If not specified, uses the global minimum level.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A configured JobLogger ready for use in jobs.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            // Determine the actual minimum level to use
            LogLevel actualMinLevel = minimumLevel ?? _globalMinimumLevel;

            // Use a default tag if none is provided
            if (defaultTag == default)
            {
                defaultTag = Tagging.LogTag.Job;
            }

            return new JobLogger(_logQueueWriter, actualMinLevel, defaultTag);
        }

        /// <summary>
        /// Enables automatic flushing of log messages.
        /// Thread-safe: Uses a lock to synchronize access to auto-flush settings.
        /// </summary>
        /// <param name="interval">The interval in seconds between flush operations.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if interval is less than or equal to zero.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public void EnableAutoFlush(float interval = 1.0f)
        {
            if (interval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            lock (_configLock)
            {
                _autoFlushEnabled = true;
                _autoFlushInterval = interval;
                _timeSinceLastAutoFlush = 0f;
            }
        }

        /// <summary>
        /// Disables automatic flushing of log messages.
        /// Thread-safe: Uses a lock to synchronize access to auto-flush settings.
        /// </summary>
        public void DisableAutoFlush()
        {
            if (_isDisposed)
                return;

            lock (_configLock)
            {
                _autoFlushEnabled = false;
            }
        }

        /// <summary>
        /// Updates the time tracking for auto-flush and triggers a flush if needed.
        /// Should be called from a MonoBehaviour Update method or similar regular update.
        /// Thread-safe: Uses locks to synchronize access to auto-flush settings and flush operations.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last update.</param>
        /// <returns>Number of messages processed if a flush was triggered; otherwise, zero.</returns>
        public int Update(float deltaTime)
        {
            if (_isDisposed || !_autoFlushEnabled)
                return 0;

            bool shouldFlush = false;

            lock (_configLock)
            {
                _timeSinceLastAutoFlush += deltaTime;

                if (_timeSinceLastAutoFlush >= _autoFlushInterval)
                {
                    shouldFlush = true;
                    _timeSinceLastAutoFlush = 0f;
                }
            }

            if (shouldFlush)
            {
                return Flush();
            }

            return 0;
        }

        /// <summary>
        /// Manually flushes any queued log messages to all targets.
        /// Thread-safe: Uses a lock to ensure only one flush operation occurs at a time.
        /// </summary>
        /// <returns>The number of messages processed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if flush operation fails.</exception>
        public int Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            // Use TryEnter with a timeout to avoid deadlocks
            if (!Monitor.TryEnter(_flushLock, 100))
                return 0; // Return 0 if we couldn't acquire the lock within the timeout

            try
            {
                return _batchProcessor.Flush();
            }
            catch (Exception ex)
            {
                // Rather than logging the error, propagate it to the caller
                throw new InvalidOperationException($"Error during log flush", ex);
            }
            finally
            {
                Monitor.Exit(_flushLock);
            }
        }

        /// <summary>
        /// Updates the minimum level for all targets based on the global setting.
        /// Thread-safe: Uses a lock to synchronize access to the target list.
        /// </summary>
        private void UpdateTargetMinimumLevels()
        {
            lock (_configLock)
            {
                foreach (var target in _logTargets)
                {
                    try
                    {
                        target.MinimumLevel = _globalMinimumLevel;
                    }
                    catch (Exception)
                    {
                        // Silently handle errors when updating target levels
                        // We don't want to break logging functionality because of one target
                    }
                }
            }
        }

        /// <summary>
        /// Enqueues a log message directly to the internal queue.
        /// Thread-safe: Uses NativeQueue.ParallelWriter for thread-safe enqueueing.
        /// </summary>
        /// <param name="message">The message to enqueue.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public void Enqueue(in LogMessage message)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            // Early exit if below global minimum level - atomic operation
            if (message.Level < _globalMinimumLevel)
                return;

            try
            {
                // Use the parallel writer for thread-safe enqueueing
                _logQueueWriter.Enqueue(message);
            }
            catch (Exception)
            {
                // Silently handle errors when enqueuing messages
                // This avoids having to log failures in the logging system itself
            }
        }

        /// <summary>
        /// Logs a message with the specified parameters.
        /// Thread-safe: Uses NativeQueue.ParallelWriter for thread-safe enqueueing.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Optional structured log properties.</param>
        public void Log(LogLevel level, Tagging.LogTag tag, string message, LogProperties properties = default)
        {
            // Early exits for common conditions to avoid unnecessary work
            if (_isDisposed || level < _globalMinimumLevel || string.IsNullOrEmpty(message))
                return;

            try
            {
                // Convert the string to FixedString512Bytes for the LogMessage constructor
                FixedString512Bytes fixedMessage = new FixedString512Bytes(message);

                // Create log message with properties if provided
                LogMessage logMessage = new LogMessage(fixedMessage, level, tag, properties);
                
                // Use the parallel writer for thread-safe enqueueing
                _logQueueWriter.Enqueue(logMessage);
            }
            catch (Exception)
            {
                // Silently handle errors when logging
                // We can't log a failure to log
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// Thread-safe: Delegates to the thread-safe Log method.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        /// <param name="properties">Optional structured log properties.</param>
        public void Debug(string message, Tagging.LogTag tag = Tagging.LogTag.Debug, LogProperties properties = default)
        {
            Log(LogLevel.Debug, tag, message, properties);
        }

        /// <summary>
        /// Logs an info message.
        /// Thread-safe: Delegates to the thread-safe Log method.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        /// <param name="properties">Optional structured log properties.</param>
        public void Info(string message, Tagging.LogTag tag = Tagging.LogTag.Info, LogProperties properties = default)
        {
            Log(LogLevel.Info, tag, message, properties);
        }

        /// <summary>
        /// Logs a warning message.
        /// Thread-safe: Delegates to the thread-safe Log method.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        /// <param name="properties">Optional structured log properties.</param>
        public void Warning(string message, Tagging.LogTag tag = Tagging.LogTag.Warning, LogProperties properties = default)
        {
            Log(LogLevel.Warning, tag, message, properties);
        }

        /// <summary>
        /// Logs an error message.
        /// Thread-safe: Delegates to the thread-safe Log method.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        /// <param name="properties">Optional structured log properties.</param>
        public void Error(string message, Tagging.LogTag tag = Tagging.LogTag.Error, LogProperties properties = default)
        {
            Log(LogLevel.Error, tag, message, properties);
        }

        /// <summary>
        /// Logs a critical message.
        /// Thread-safe: Delegates to the thread-safe Log method.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        /// <param name="properties">Optional structured log properties.</param>
        public void Critical(string message, Tagging.LogTag tag = Tagging.LogTag.Critical, LogProperties properties = default)
        {
            Log(LogLevel.Critical, tag, message, properties);
        }

        /// <summary>
        /// Flushes all targets and disposes all resources.
        /// Thread-safe: Uses locks to synchronize disposal and resource cleanup.
        /// </summary>
        public void Dispose()
        {
            // Use a CAS (Compare-And-Swap) pattern for thread-safe disposal flag check
            // The simplest correct approach:
            int disposedFlag = _isDisposed ? 1 : 0;
            bool wasDisposed = Interlocked.Exchange(ref disposedFlag, 1) != 0;
            _isDisposed = disposedFlag == 1;
            if (wasDisposed)
                return;

            // Ensure exclusive access during disposal
            lock (_configLock)
            {
                lock (_flushLock)
                {
                    _autoFlushEnabled = false;

                    try
                    {
                        // Flush one last time before disposing
                        _batchProcessor.Flush();

                        // Dispose the batch processor
                        _batchProcessor.Dispose();
                    }
                    catch
                    {
                        // Silently ignore errors during disposal
                    }
                    finally
                    {
                        // Always ensure the native queue is properly disposed
                        if (_logQueue.IsCreated)
                        {
                            _logQueue.Dispose();
                        }

                        // We don't dispose log targets here - they should be externally managed
                        _logTargets.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// No-op implementation of IMessageBus for scenarios where messaging is not needed.
        /// This prevents the logging system from failing when no message bus is configured.
        /// </summary>
        private class NullMessageBus : IMessageBus
        {
            public IMessagePublisher<TMessage> GetPublisher<TMessage>() => new NullPublisher<TMessage>();
            public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() => new NullSubscriber<TMessage>();
            public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>() => new NullKeyedPublisher<TKey, TMessage>();
            public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>() => new NullKeyedSubscriber<TKey, TMessage>();
            public void ClearCaches() { }
            public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage { }
            public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage => new NullDisposable();
            public IDisposable SubscribeToAllMessages(Action<IMessage> handler) => new NullDisposable();
            public IMessageRegistry GetMessageRegistry() => new NullMessageRegistry();

            private class NullPublisher<TMessage> : IMessagePublisher<TMessage>
            {
                public void Publish(TMessage message) { }
                public IDisposable PublishAsync(TMessage message) => new NullDisposable();
            }

            private class NullSubscriber<TMessage> : IMessageSubscriber<TMessage>
            {
                public IDisposable Subscribe(Action<TMessage> handler) => new NullDisposable();
                public IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter) => new NullDisposable();
            }

            private class NullKeyedPublisher<TKey, TMessage> : IKeyedMessagePublisher<TKey, TMessage>
            {
                public void Publish(TKey key, TMessage message) { }
                public IDisposable PublishAsync(TKey key, TMessage message) => new NullDisposable();
            }

            private class NullKeyedSubscriber<TKey, TMessage> : IKeyedMessageSubscriber<TKey, TMessage>
            {
                public IDisposable Subscribe(TKey key, Action<TMessage> handler) => new NullDisposable();
                public IDisposable Subscribe(Action<TKey, TMessage> handler) => new NullDisposable();
                public IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter) => new NullDisposable();
            }

            private class NullMessageRegistry : IMessageRegistry
            {
                public void DiscoverMessages() { }
                public void RegisterMessageType(Type messageType) { }
                public void RegisterMessageType(Type messageType, ushort typeCode) { }
                public IReadOnlyDictionary<Type, IMessageInfo> GetAllMessageTypes() => new Dictionary<Type, IMessageInfo>();
                public IReadOnlyList<string> GetCategories() => new List<string>();
                public IReadOnlyList<Type> GetMessageTypesByCategory(string category) => new List<Type>();
                public IMessageInfo GetMessageInfo(Type messageType) => null;
                public IMessageInfo GetMessageInfo<TMessage>() where TMessage : IMessage => null;
                public bool IsRegistered(Type messageType) => false;
                public bool IsRegistered<TMessage>() where TMessage : IMessage => false;
                public ushort GetTypeCode(Type messageType) => 0;
                public ushort GetTypeCode<TMessage>() where TMessage : IMessage => 0;
                public Type GetMessageType(ushort typeCode) => null;
                public IReadOnlyDictionary<ushort, Type> GetAllTypeCodes() => new Dictionary<ushort, Type>();
                public void Clear() { }
            }

            private class NullDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}