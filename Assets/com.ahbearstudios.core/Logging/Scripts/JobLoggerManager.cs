using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Central manager for the logging system that coordinates log targets, queue processing, and flush operations.
    /// This class owns the NativeQueue for log messages, the LogBatchProcessor, and maintains the collection of ILogTargets.
    /// </summary>
    public class JobLoggerManager : IDisposable
    {
        /// <summary>
        /// The queue that stores log messages.
        /// </summary>
        private NativeQueue<LogMessage> _logQueue;

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
        /// Current global minimum log level across all targets.
        /// </summary>
        private byte _globalMinimumLevel;

        /// <summary>
        /// Maximum messages to process per flush operation.
        /// </summary>
        private readonly int _maxMessagesPerFlush;

        /// <summary>
        /// Whether automatic flush is enabled on a timer.
        /// </summary>
        private bool _autoFlushEnabled;

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
        private bool _isDisposed;

        /// <summary>
        /// Thread synchronization object.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// Gets or sets the global minimum log level that will be processed.
        /// </summary>
        public byte GlobalMinimumLevel
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
        /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public JobLoggerManager(ILogFormatter formatter, int initialCapacity = 64, int maxMessagesPerFlush = 200,
            byte globalMinimumLevel = LogLevel.Info)
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

            // Initialize native queue
            InitializeNativeQueue(initialCapacity);

            // Create the batch processor
            _batchProcessor = new LogBatchProcessor(_logTargets, _logQueue, _formatter, _maxMessagesPerFlush);
        }

        /// <summary>
        /// Creates a new JobLoggerManager with the specified initial targets, formatter and configuration.
        /// </summary>
        /// <param name="initialTargets">The initial log targets.</param>
        /// <param name="formatter">The formatter to use for log messages.</param>
        /// <param name="initialCapacity">Initial capacity of the log queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum messages to process per flush operation.</param>
        /// <param name="globalMinimumLevel">Global minimum log level.</param>
        /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when initialTargets is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public JobLoggerManager(IEnumerable<ILogTarget> initialTargets, ILogFormatter formatter,
            int initialCapacity = 64, int maxMessagesPerFlush = 200, byte globalMinimumLevel = LogLevel.Info)
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

            // Initialize native queue
            InitializeNativeQueue(initialCapacity);

            // Create the batch processor with the initial targets
            _batchProcessor = new LogBatchProcessor(_logTargets, _logQueue, _formatter, _maxMessagesPerFlush);

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
        /// <returns>A configured JobLoggerManager instance.</returns>
        /// <exception cref="ArgumentException">Thrown when targets is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public static JobLoggerManager CreateWithTargetsAndDefaultFormatter(
            IEnumerable<ILogTarget> initialTargets,
            int initialCapacity = 64,
            int maxMessagesPerFlush = 200,
            byte globalMinimumLevel = LogLevel.Info)
        {
            return new JobLoggerManager(initialTargets, new DefaultLogFormatter(), initialCapacity, maxMessagesPerFlush,
                globalMinimumLevel);
        }

        /// <summary>
        /// Creates a new JobLoggerManager using the default log formatter.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the log queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum messages to process per flush operation.</param>
        /// <param name="globalMinimumLevel">Global minimum log level.</param>
        /// <returns>A configured JobLoggerManager instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when native resources cannot be initialized.</exception>
        public static JobLoggerManager CreateWithDefaultFormatter(int initialCapacity = 64,
            int maxMessagesPerFlush = 200, byte globalMinimumLevel = LogLevel.Info)
        {
            return new JobLoggerManager(new DefaultLogFormatter(), initialCapacity, maxMessagesPerFlush,
                globalMinimumLevel);
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

            lock (_syncLock)
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

            lock (_syncLock)
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
        /// </summary>
        /// <param name="minimumLevel">The minimum level to log at. If not specified, uses the global minimum level.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A configured JobLogger ready for use in jobs.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public JobLogger CreateJobLogger(byte? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            // Determine the actual minimum level to use
            byte actualMinLevel = minimumLevel.HasValue ? minimumLevel.Value : _globalMinimumLevel;

            // Use a default tag if none is provided
            if (defaultTag == default)
            {
                defaultTag = Tagging.LogTag.Job;
            }

            return new JobLogger(_logQueue.AsParallelWriter(), actualMinLevel, defaultTag);
        }

        /// <summary>
        /// Enables automatic flushing of log messages.
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

            lock (_syncLock)
            {
                _autoFlushEnabled = true;
                _autoFlushInterval = interval;
                _timeSinceLastAutoFlush = 0f;
            }
        }

        /// <summary>
        /// Disables automatic flushing of log messages.
        /// </summary>
        public void DisableAutoFlush()
        {
            if (_isDisposed)
                return;

            lock (_syncLock)
            {
                _autoFlushEnabled = false;
            }
        }

        /// <summary>
        /// Updates the time tracking for auto-flush and triggers a flush if needed.
        /// Should be called from a MonoBehaviour Update method or similar regular update.
        /// </summary>
        /// <param name="deltaTime">Time in seconds since the last update.</param>
        /// <returns>Number of messages processed if a flush was triggered; otherwise, zero.</returns>
        public int Update(float deltaTime)
        {
            if (_isDisposed || !_autoFlushEnabled)
                return 0;

            bool shouldFlush = false;

            lock (_syncLock)
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
        /// </summary>
        /// <returns>The number of messages processed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if flush operation fails.</exception>
        public int Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            lock (_syncLock)
            {
                try
                {
                    return _batchProcessor.Flush();
                }
                catch (Exception ex)
                {
                    // Rather than logging the error, propagate it to the caller
                    throw new InvalidOperationException($"Error during log flush", ex);
                }
            }
        }

        /// <summary>
        /// Updates the minimum level for all targets based on the global setting.
        /// </summary>
        private void UpdateTargetMinimumLevels()
        {
            lock (_syncLock)
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
        /// This is primarily for internal use or for custom logging implementations.
        /// </summary>
        /// <param name="message">The message to enqueue.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the manager has been disposed.</exception>
        public void Enqueue(in LogMessage message)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JobLoggerManager));

            // Early exit if below global minimum level
            if (message.Level < _globalMinimumLevel)
                return;

            try
            {
                _logQueue.Enqueue(message);
            }
            catch (Exception)
            {
                // Silently handle errors when enqueuing messages
                // This avoids having to log failures in the logging system itself
            }
        }

        /// <summary>
        /// Enqueues a log message with the specified parameters.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message text.</param>
        public void Log(byte level, Tagging.LogTag tag, string message)
        {
            if (_isDisposed || level < _globalMinimumLevel || string.IsNullOrEmpty(message))
                return;

            try
            {
                // Convert the string to FixedString512Bytes for the LogMessage constructor
                FixedString512Bytes fixedMessage = new FixedString512Bytes(message);

                // Use the constructor directly instead of the non-existent Create method
                LogMessage logMessage = new LogMessage(fixedMessage, level, tag,default);
                _logQueue.Enqueue(logMessage);
            }
            catch (Exception)
            {
                // Silently handle errors when logging
                // We can't log a failure to log
            }
        }
        
        /// <summary>
        /// Enqueues a log message with the specified parameters.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured log properties</param>
        public void Log(byte level, Tagging.LogTag tag, string message, LogProperties properties)
        {
            if (_isDisposed || level < _globalMinimumLevel || string.IsNullOrEmpty(message))
                return;

            try
            {
                // Convert the string to FixedString512Bytes for the LogMessage constructor
                FixedString512Bytes fixedMessage = new FixedString512Bytes(message);

                // Use the constructor directly instead of the non-existent Create method
                LogMessage logMessage = new LogMessage(fixedMessage, level, tag, properties);
                _logQueue.Enqueue(logMessage);
            }
            catch (Exception)
            {
                // Silently handle errors when logging
                // We can't log a failure to log
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void Debug(string message, Tagging.LogTag tag = Tagging.LogTag.Debug)
        {
            Log(LogLevel.Debug, tag, message);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void Info(string message, Tagging.LogTag tag = Tagging.LogTag.Info)
        {
            Log(LogLevel.Info, tag, message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void Warning(string message, Tagging.LogTag tag = Tagging.LogTag.Warning)
        {
            Log(LogLevel.Warning, tag, message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void Error(string message, Tagging.LogTag tag = Tagging.LogTag.Error)
        {
            Log(LogLevel.Error, tag, message);
        }

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void Critical(string message, Tagging.LogTag tag = Tagging.LogTag.Critical)
        {
            Log(LogLevel.Critical, tag, message);
        }
        
        /// <summary>
/// Logs a debug message with structured properties.
/// </summary>
/// <param name="message">The message text.</param>
/// <param name="properties">Structured properties providing additional context.</param>
/// <param name="tag">The log tag (optional).</param>
public void Debug(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Debug)
{
    Log(LogLevel.Debug, tag, message, properties);
}

/// <summary>
/// Logs an info message with structured properties.
/// </summary>
/// <param name="message">The message text.</param>
/// <param name="properties">Structured properties providing additional context.</param>
/// <param name="tag">The log tag (optional).</param>
public void Info(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Info)
{
    Log(LogLevel.Info, tag, message, properties);
}

/// <summary>
/// Logs a warning message with structured properties.
/// </summary>
/// <param name="message">The message text.</param>
/// <param name="properties">Structured properties providing additional context.</param>
/// <param name="tag">The log tag (optional).</param>
public void Warning(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Warning)
{
    Log(LogLevel.Warning, tag, message, properties);
}

/// <summary>
/// Logs an error message with structured properties.
/// </summary>
/// <param name="message">The message text.</param>
/// <param name="properties">Structured properties providing additional context.</param>
/// <param name="tag">The log tag (optional).</param>
public void Error(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Error)
{
    Log(LogLevel.Error, tag, message, properties);
}

/// <summary>
/// Logs a critical message with structured properties.
/// </summary>
/// <param name="message">The message text.</param>
/// <param name="properties">Structured properties providing additional context.</param>
/// <param name="tag">The log tag (optional).</param>
public void Critical(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Critical)
{
    Log(LogLevel.Critical, tag, message, properties);
}

        /// <summary>
        /// Enqueues a log message with structured properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured properties providing additional context.</param>
        public void LogStructured(byte level, Tagging.LogTag tag, string message, LogProperties properties)
        {
            if (_isDisposed || level < _globalMinimumLevel || string.IsNullOrEmpty(message))
                return;

            try
            {
                // Convert the string to FixedString512Bytes for the LogMessage constructor
                FixedString512Bytes fixedMessage = new FixedString512Bytes(message);

                // Create log message with properties
                LogMessage logMessage = new LogMessage(fixedMessage, level, tag, properties);
                _logQueue.Enqueue(logMessage);
            }
            catch (Exception)
            {
                // Silently handle errors when logging
                // We can't log a failure to log
            }
        }

        /// <summary>
        /// Logs a debug message with structured properties.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured properties providing additional context.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void DebugStructured(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Debug)
        {
            LogStructured(LogLevel.Debug, tag, message, properties);
        }

        /// <summary>
        /// Logs an info message with structured properties.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured properties providing additional context.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void InfoStructured(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Info)
        {
            LogStructured(LogLevel.Info, tag, message, properties);
        }

        /// <summary>
        /// Logs a warning message with structured properties.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured properties providing additional context.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void WarningStructured(string message, LogProperties properties,
            Tagging.LogTag tag = Tagging.LogTag.Warning)
        {
            LogStructured(LogLevel.Warning, tag, message, properties);
        }

        /// <summary>
        /// Logs an error message with structured properties.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured properties providing additional context.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void ErrorStructured(string message, LogProperties properties, Tagging.LogTag tag = Tagging.LogTag.Error)
        {
            LogStructured(LogLevel.Error, tag, message, properties);
        }

        /// <summary>
        /// Logs a critical message with structured properties.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="properties">Structured properties providing additional context.</param>
        /// <param name="tag">The log tag (optional).</param>
        public void CriticalStructured(string message, LogProperties properties,
            Tagging.LogTag tag = Tagging.LogTag.Critical)
        {
            LogStructured(LogLevel.Critical, tag, message, properties);
        }

        /// <summary>
        /// Flushes all targets and disposes all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            lock (_syncLock)
            {
                _isDisposed = true;
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
}