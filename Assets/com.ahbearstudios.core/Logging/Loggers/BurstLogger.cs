using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using Unity.Collections;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Standard implementation of IBurstLogger that provides flexible logging with multiple targets.
    /// Thread-safe and optimized for high-performance scenarios.
    /// </summary>
    public sealed class BurstLogger : IBurstLogger, IDisposable
    {
        private readonly List<ILogTarget> _targets;
        private readonly object _syncLock = new object();
        private readonly string _loggerName;
        private LogLevel _minimumLevel;
        private bool _isEnabled;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the minimum log level that will be processed by this logger.
        /// </summary>
        public LogLevel MinimumLevel
        {
            get
            {
                lock (_syncLock)
                {
                    return _minimumLevel;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _minimumLevel = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether this logger is enabled.
        /// When disabled, no log messages will be processed.
        /// </summary>
        public bool IsEnabledGlobal
        {
            get
            {
                lock (_syncLock)
                {
                    return _isEnabled;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _isEnabled = value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the BurstLogger class.
        /// </summary>
        /// <param name="loggerName">The name of this logger instance.</param>
        /// <param name="minimumLevel">The minimum log level to process.</param>
        public BurstLogger(string loggerName = "BurstLogger", LogLevel minimumLevel = LogLevel.Debug)
        {
            _loggerName = loggerName ?? "BurstLogger";
            _minimumLevel = minimumLevel;
            _isEnabled = true;
            _targets = new List<ILogTarget>();
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, string tag)
        {
            if (_disposed)
                throw new ObjectDisposedException(_loggerName);

            if (!IsEnabled(level))
                return;

            if (string.IsNullOrEmpty(message))
                return;

            // Create log message
            var logMessage = new LogMessage
            {
                Level = level,
                Message = new FixedString512Bytes(message),
                Tag = Tagging.GetLogTag(tag ?? "Default"),
                TimestampTicks = DateTime.UtcNow.Ticks
            };

            // Send to all targets
            lock (_syncLock)
            {
                foreach (var target in _targets)
                {
                    try
                    {
                        if (target.IsEnabled && target.IsLevelEnabled(level))
                        {
                            target.Write(in logMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Swallow exceptions from targets to prevent logging from breaking the application
                        Console.WriteLine($"Error writing to log target {target.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, string tag, LogProperties properties)
        {
            if (_disposed)
                throw new ObjectDisposedException(_loggerName);

            if (!IsEnabled(level))
                return;

            if (string.IsNullOrEmpty(message))
                return;

            bool createdTempProperties = false;
            // Initialize with default to ensure it's always initialized
            LogProperties propertiesInstance = default;
    
            try
            {
                // Check if properties value is provided
                if (properties.IsCreated)
                {
                    propertiesInstance = properties;
                }
                else
                {
                    // Create a temporary properties object
                    propertiesInstance = new LogProperties((int)Allocator.Temp);
                    createdTempProperties = true;
                }

                // Create log message with properties
                var logMessage = new LogMessage
                {
                    Level = level,
                    Message = new FixedString512Bytes(message),
                    Tag = Tagging.GetLogTag(tag ?? "Default"),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    Properties = propertiesInstance
                };

                // Send to all targets
                lock (_syncLock)
                {
                    foreach (var target in _targets)
                    {
                        try
                        {
                            if (target.IsEnabled && target.IsLevelEnabled(level))
                            {
                                target.Write(in logMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Swallow exceptions from targets to prevent logging from breaking the application
                            Console.WriteLine($"Error writing to log target {target.Name}: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                // Dispose of temporary properties if we created them
                if (createdTempProperties && propertiesInstance.IsCreated)
                {
                    propertiesInstance.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel level)
        {
            if (_disposed)
                return false;

            lock (_syncLock)
            {
                if (!_isEnabled)
                    return false;

                if (level < _minimumLevel)
                    return false;

                // Check if any target would log this level
                foreach (var target in _targets)
                {
                    if (target.IsEnabled && target.IsLevelEnabled(level))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Adds a log target to this logger.
        /// </summary>
        /// <param name="target">The log target to add.</param>
        public void AddTarget(ILogTarget target)
        {
            if (_disposed)
                throw new ObjectDisposedException(_loggerName);

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            lock (_syncLock)
            {
                if (!_targets.Contains(target))
                {
                    _targets.Add(target);
                }
            }
        }

        /// <summary>
        /// Removes a log target from this logger.
        /// </summary>
        /// <param name="target">The log target to remove.</param>
        /// <returns>True if the target was removed; otherwise, false.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            if (_disposed)
                throw new ObjectDisposedException(_loggerName);

            if (target == null)
                return false;

            lock (_syncLock)
            {
                return _targets.Remove(target);
            }
        }

        /// <summary>
        /// Removes all log targets from this logger.
        /// </summary>
        public void ClearTargets()
        {
            if (_disposed)
                throw new ObjectDisposedException(_loggerName);

            lock (_syncLock)
            {
                _targets.Clear();
            }
        }

        /// <summary>
        /// Gets the current number of log targets.
        /// </summary>
        public int TargetCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _targets.Count;
                }
            }
        }

        /// <summary>
        /// Flushes all log targets to ensure messages are persisted.
        /// </summary>
        public void Flush()
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                foreach (var target in _targets)
                {
                    try
                    {
                        target.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error flushing log target {target.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_syncLock)
            {
                if (_disposed)
                    return;

                // Flush all targets before disposal
                Flush();

                // Clear targets (but don't dispose them as they might be shared)
                _targets.Clear();

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for IBurstLogger to provide convenience logging methods.
    /// </summary>
    public static class BurstLoggerExtensions
    {
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void LogDebug(this IBurstLogger logger, string message, string tag = "Default")
        {
            logger.Log(LogLevel.Debug, message, tag);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        public static void LogInfo(this IBurstLogger logger, string message, string tag = "Default")
        {
            logger.Log(LogLevel.Info, message, tag);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarning(this IBurstLogger logger, string message, string tag = "Default")
        {
            logger.Log(LogLevel.Warning, message, tag);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(this IBurstLogger logger, string message, string tag = "Default")
        {
            logger.Log(LogLevel.Error, message, tag);
        }

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        public static void LogCritical(this IBurstLogger logger, string message, string tag = "Default")
        {
            logger.Log(LogLevel.Critical, message, tag);
        }
    }
}