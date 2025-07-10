using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// High-performance implementation of the logging service.
    /// Provides centralized logging with multiple targets and advanced features.
    /// Follows the AhBearStudios Core Architecture foundation system pattern.
    /// </summary>
    public sealed class LoggingService : ILoggingService, IDisposable
    {
        private readonly ConcurrentDictionary<string, ILogTarget> _targets;
        private readonly LoggingConfig _config;
        private readonly LogBatchingService _batchingService;
        private readonly LogFormattingService _formattingService;
        private readonly object _configLock = new object();
        private readonly Stopwatch _performanceStopwatch;
        
        private volatile bool _disposed = false;
        private volatile bool _enabled = true;
        private volatile LogLevel _globalMinimumLevel = LogLevel.Debug;

        /// <summary>
        /// Gets the current logging configuration.
        /// </summary>
        public LoggingConfig Configuration => _config;

        /// <summary>
        /// Gets whether the logging service is enabled.
        /// </summary>
        public bool IsEnabled => _enabled && !_disposed;

        /// <summary>
        /// Gets the global minimum log level.
        /// </summary>
        public LogLevel GlobalMinimumLevel => _globalMinimumLevel;

        /// <summary>
        /// Gets the number of registered targets.
        /// </summary>
        public int TargetCount => _targets.Count;

        /// <summary>
        /// Gets whether high-performance mode is enabled.
        /// </summary>
        public bool HighPerformanceMode => _config?.HighPerformanceMode ?? false;

        /// <summary>
        /// Gets whether batch processing is enabled.
        /// </summary>
        public bool BatchingEnabled => _batchingService != null;

        /// <summary>
        /// Initializes a new instance of the LoggingService.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public LoggingService(LoggingConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _targets = new ConcurrentDictionary<string, ILogTarget>();
            _performanceStopwatch = new Stopwatch();
            
            _globalMinimumLevel = _config.MinimumLevel;
            _enabled = _config.IsEnabled;

            // Initialize batching service if enabled
            if (_config.BatchingEnabled)
            {
                _batchingService = new LogBatchingService(
                    Array.Empty<ILogTarget>(), // Will be updated when targets are registered
                    _config.BatchSize,
                    _config.FlushInterval,
                    _config.HighPerformanceMode,
                    _config.BurstCompatibility);
            }

            // Initialize formatting service
            _formattingService = new LogFormattingService(
                _config.MessageFormat,
                _config.TimestampFormat,
                _config.HighPerformanceMode,
                _config.CachingEnabled,
                _config.MaxCacheSize);

            // Register built-in targets if specified in config
            InitializeBuiltInTargets();
        }

        /// <summary>
        /// Initializes a new instance of the LoggingService with default configuration.
        /// </summary>
        public LoggingService() : this(LoggingConfig.Default)
        {
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                LogInternal(LogLevel.Debug, "Default", message);
            }
        }

        /// <summary>
        /// Logs a debug message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                LogInternal(LogLevel.Debug, "Default", message, null, null, properties);
            }
        }

        /// <summary>
        /// Logs a debug message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                LogInternal(LogLevel.Debug, "Default", message, null, correlationId, null, sourceContext);
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message)
        {
            if (ShouldLog(LogLevel.Info))
            {
                LogInternal(LogLevel.Info, "Default", message);
            }
        }

        /// <summary>
        /// Logs an informational message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Info))
            {
                LogInternal(LogLevel.Info, "Default", message, null, null, properties);
            }
        }

        /// <summary>
        /// Logs an informational message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Info))
            {
                LogInternal(LogLevel.Info, "Default", message, null, correlationId, null, sourceContext);
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                LogInternal(LogLevel.Warning, "Default", message);
            }
        }

        /// <summary>
        /// Logs a warning message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                LogInternal(LogLevel.Warning, "Default", message, null, null, properties);
            }
        }

        /// <summary>
        /// Logs a warning message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                LogInternal(LogLevel.Warning, "Default", message, null, correlationId, null, sourceContext);
            }
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message)
        {
            if (ShouldLog(LogLevel.Error))
            {
                LogInternal(LogLevel.Error, "Default", message);
            }
        }

        /// <summary>
        /// Logs an error message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Error))
            {
                LogInternal(LogLevel.Error, "Default", message, null, null, properties);
            }
        }

        /// <summary>
        /// Logs an error message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Error))
            {
                LogInternal(LogLevel.Error, "Default", message, null, correlationId, null, sourceContext);
            }
        }

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                LogInternal(LogLevel.Critical, "Default", message);
            }
        }

        /// <summary>
        /// Logs a critical message with structured properties.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="properties">Additional structured properties</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                LogInternal(LogLevel.Critical, "Default", message, null, null, properties);
            }
        }

        /// <summary>
        /// Logs a critical message with correlation ID and source context.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                LogInternal(LogLevel.Critical, "Default", message, null, correlationId, null, sourceContext);
            }
        }

        /// <summary>
        /// Logs an exception with context information.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Additional context information</param>
        public void LogException(Exception exception, string context)
        {
            if (exception == null) return;
            
            LogInternal(LogLevel.Error, "Exception", context ?? "An exception occurred", exception);
        }

        /// <summary>
        /// Logs an exception with context and correlation ID.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Additional context information</param>
        /// <param name="correlationId">The correlation ID for tracking</param>
        /// <param name="sourceContext">The source context (typically class name)</param>
        public void LogException(Exception exception, string context, string correlationId, string sourceContext = null)
        {
            if (exception == null) return;
            
            LogInternal(LogLevel.Error, "Exception", context ?? "An exception occurred", exception, correlationId, null, sourceContext);
        }

        /// <summary>
        /// Logs a message with the specified level and channel.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The message to log</param>
        /// <param name="exception">The associated exception, if any</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="properties">Additional structured properties</param>
        /// <param name="sourceContext">The source context</param>
        public void Log(LogLevel level, string channel, string message, Exception exception = null, 
                       string correlationId = null, IReadOnlyDictionary<string, object> properties = null, 
                       string sourceContext = null)
        {
            if (ShouldLog(level))
            {
                LogInternal(level, channel ?? "Default", message, exception, correlationId, properties, sourceContext);
            }
        }

        /// <summary>
        /// Registers a log target with the service.
        /// </summary>
        /// <param name="target">The log target to register</param>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        public void RegisterTarget(ILogTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (_disposed) return;

            _targets.TryAdd(target.Name, target);
            
            // Update batching service if enabled
            if (_batchingService != null)
            {
                UpdateBatchingServiceTargets();
            }
        }

        /// <summary>
        /// Unregisters a log target from the service.
        /// </summary>
        /// <param name="target">The log target to unregister</param>
        /// <returns>True if the target was unregistered, false if it was not found</returns>
        public bool UnregisterTarget(ILogTarget target)
        {
            if (target == null) return false;
            
            var removed = _targets.TryRemove(target.Name, out _);
            
            // Update batching service if enabled
            if (removed && _batchingService != null)
            {
                UpdateBatchingServiceTargets();
            }
            
            return removed;
        }

        /// <summary>
        /// Unregisters a log target by name.
        /// </summary>
        /// <param name="targetName">The name of the target to unregister</param>
        /// <returns>True if the target was unregistered, false if it was not found</returns>
        public bool UnregisterTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            var removed = _targets.TryRemove(targetName, out _);
            
            // Update batching service if enabled
            if (removed && _batchingService != null)
            {
                UpdateBatchingServiceTargets();
            }
            
            return removed;
        }

        /// <summary>
        /// Gets all registered log targets.
        /// </summary>
        /// <returns>A read-only list of registered targets</returns>
        public IReadOnlyList<ILogTarget> GetRegisteredTargets()
        {
            return _targets.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets a registered log target by name.
        /// </summary>
        /// <param name="targetName">The name of the target to retrieve</param>
        /// <returns>The log target if found, null otherwise</returns>
        public ILogTarget GetTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return null;
            
            _targets.TryGetValue(targetName, out var target);
            return target;
        }

        /// <summary>
        /// Determines whether a log target is registered.
        /// </summary>
        /// <param name="targetName">The name of the target to check</param>
        /// <returns>True if the target is registered, false otherwise</returns>
        public bool HasTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            return _targets.ContainsKey(targetName);
        }

        /// <summary>
        /// Sets the minimum log level for all targets.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level</param>
        public void SetMinimumLevel(LogLevel minimumLevel)
        {
            lock (_configLock)
            {
                _globalMinimumLevel = minimumLevel;
                
                foreach (var target in _targets.Values)
                {
                    target.MinimumLevel = minimumLevel;
                }
            }
        }

        /// <summary>
        /// Sets the minimum log level for a specific target.
        /// </summary>
        /// <param name="targetName">The name of the target</param>
        /// <param name="minimumLevel">The minimum log level</param>
        /// <returns>True if the target was found and updated, false otherwise</returns>
        public bool SetMinimumLevel(string targetName, LogLevel minimumLevel)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                target.MinimumLevel = minimumLevel;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Enables or disables all log targets.
        /// </summary>
        /// <param name="enabled">True to enable all targets, false to disable</param>
        public void SetEnabled(bool enabled)
        {
            lock (_configLock)
            {
                _enabled = enabled;
                
                foreach (var target in _targets.Values)
                {
                    target.IsEnabled = enabled;
                }
            }
        }

        /// <summary>
        /// Enables or disables a specific log target.
        /// </summary>
        /// <param name="targetName">The name of the target</param>
        /// <param name="enabled">True to enable the target, false to disable</param>
        /// <returns>True if the target was found and updated, false otherwise</returns>
        public bool SetEnabled(string targetName, bool enabled)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                target.IsEnabled = enabled;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Forces all log targets to flush their buffers.
        /// </summary>
        public void Flush()
        {
            if (_batchingService != null)
            {
                _batchingService.ForceFlush();
            }
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.Flush();
                }
                catch (Exception ex)
                {
                    // Avoid circular logging - use system debugging instead
                    System.Diagnostics.Debug.WriteLine($"LoggingService flush error for target '{target.Name}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Forces a specific log target to flush its buffer.
        /// </summary>
        /// <param name="targetName">The name of the target to flush</param>
        /// <returns>True if the target was found and flushed, false otherwise</returns>
        public bool Flush(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                try
                {
                    target.Flush();
                    return true;
                }
                catch (Exception ex)
                {
                    // Avoid circular logging - use system debugging instead
                    System.Diagnostics.Debug.WriteLine($"LoggingService flush error for target '{targetName}': {ex.Message}");
                    return false;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Performs a health check on all registered targets.
        /// </summary>
        /// <returns>True if all targets are healthy, false if any target is unhealthy</returns>
        public bool PerformHealthCheck()
        {
            if (_disposed) return false;
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    if (!target.PerformHealthCheck())
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Avoid circular logging - use system debugging instead
                    System.Diagnostics.Debug.WriteLine($"LoggingService health check error for target '{target.Name}': {ex.Message}");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Gets health status information for all registered targets.
        /// </summary>
        /// <returns>A dictionary containing health status for each target</returns>
        public IReadOnlyDictionary<string, bool> GetHealthStatus()
        {
            var healthStatus = new Dictionary<string, bool>();
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    healthStatus[target.Name] = target.PerformHealthCheck();
                }
                catch (Exception ex)
                {
                    // Avoid circular logging - use system debugging instead
                    System.Diagnostics.Debug.WriteLine($"LoggingService health check error for target '{target.Name}': {ex.Message}");
                    healthStatus[target.Name] = false;
                }
            }
            
            return healthStatus;
        }

        /// <summary>
        /// Internal logging method that handles the actual message processing.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="channel">The channel name</param>
        /// <param name="message">The message to log</param>
        /// <param name="exception">The associated exception, if any</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="properties">Additional structured properties</param>
        /// <param name="sourceContext">The source context</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogInternal(LogLevel level, string channel, string message, Exception exception = null, 
                                string correlationId = null, IReadOnlyDictionary<string, object> properties = null, 
                                string sourceContext = null)
        {
            if (_disposed || !_enabled) return;

            // Create log message
            var logMessage = LogMessage.Create(
                level, 
                channel, 
                message, 
                exception, 
                correlationId, 
                properties, 
                sourceContext ?? GetCallerContext());

            // Route to batching service if enabled, otherwise process directly
            if (_batchingService != null)
            {
                _batchingService.EnqueueMessage(logMessage);
            }
            else
            {
                ProcessMessage(logMessage);
            }
        }

        /// <summary>
        /// Processes a log message through all registered targets.
        /// </summary>
        /// <param name="logMessage">The log message to process</param>
        private void ProcessMessage(in LogMessage logMessage)
        {
            foreach (var target in _targets.Values)
            {
                try
                {
                    if (target.ShouldProcessMessage(logMessage))
                    {
                        target.Write(logMessage);
                    }
                }
                catch (Exception ex)
                {
                    // Avoid circular logging - use system debugging instead
                    System.Diagnostics.Debug.WriteLine($"LoggingService processing error for target '{target.Name}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Determines whether a message should be logged based on the current configuration.
        /// </summary>
        /// <param name="level">The log level to check</param>
        /// <returns>True if the message should be logged, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldLog(LogLevel level)
        {
            return _enabled && !_disposed && level >= _globalMinimumLevel;
        }

        /// <summary>
        /// Gets the caller context for source tracking.
        /// </summary>
        /// <returns>The caller's class name or null if not available</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCallerContext()
        {
            try
            {
                var stackTrace = new StackTrace(3, false);
                var frame = stackTrace.GetFrame(0);
                var method = frame?.GetMethod();
                return method?.DeclaringType?.Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the batching service with current targets.
        /// </summary>
        private void UpdateBatchingServiceTargets()
        {
            if (_batchingService == null) return;
            
            // Note: The batching service constructor takes targets, but we can't easily update them.
            // In a real implementation, we'd need to modify LogBatchingService to support dynamic target updates.
            // For now, we'll recreate the batching service when targets change significantly.
        }

        /// <summary>
        /// Initializes built-in targets based on configuration.
        /// </summary>
        private void InitializeBuiltInTargets()
        {
            // Register null target if no targets are configured and we're in a test environment
            if (_config.Targets == null || _config.Targets.Count == 0)
            {
                var nullTargetConfig = new LogTargetConfig
                {
                    Name = "NullTarget",
                    TargetType = "Null",
                    IsEnabled = true,
                    MinimumLevel = LogLevel.Debug,
                    Channels = new List<string> { "Default" }
                };
                
                RegisterTarget(new NullLogTarget(nullTargetConfig));
            }
        }

        /// <summary>
        /// Disposes the logging service and all registered targets.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            // Flush all targets before disposal
            Flush();
            
            // Dispose batching service
            _batchingService?.Dispose();
            
            // Dispose all targets
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.Dispose();
                }
                catch (Exception ex)
                {
                    // Avoid circular logging - use system debugging instead
                    System.Diagnostics.Debug.WriteLine($"LoggingService disposal error for target '{target.Name}': {ex.Message}");
                }
            }
            
            _targets.Clear();
            _performanceStopwatch?.Stop();
        }
    }
}