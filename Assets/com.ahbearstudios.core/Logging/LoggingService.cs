using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Logging.HealthChecks;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Enhanced high-performance logging service implementation with full system integration.
    /// Provides centralized logging with health monitoring, alerting, and performance tracking.
    /// Follows AhBearStudios Core Architecture patterns with complete dependency integration.
    /// </summary>
    public sealed class LoggingService : ILoggingService, IDisposable
    {
        private readonly LoggingConfig _config;
        private readonly ConcurrentDictionary<string, ILogTarget> _targets;
        private readonly LogFormattingService _formattingService;
        private readonly LogBatchingService _batchingService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        
        private readonly object _lock = new object();
        private readonly Stopwatch _performanceStopwatch;
        private volatile bool _disposed;
        private volatile bool _isEnabled;
        
        // Native collections for high-performance scenarios
        private NativeArray<LogMessage> _nativeMessageBuffer;
        private NativeHashMap<FixedString64Bytes, int> _channelLookup;
        
        // Performance tracking
        private long _totalMessagesProcessed;
        private long _totalErrorsEncountered;
        private DateTime _lastHealthCheck;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        
        // Alert tracking
        private DateTime _lastCriticalAlert = DateTime.MinValue;
        private readonly TimeSpan _alertCooldown = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Initializes a new instance of the LoggingService with full system integration.
        /// </summary>
        /// <param name="config">The logging configuration</param>
        /// <param name="targets">Initial log targets to register</param>
        /// <param name="formattingService">Service for formatting log messages</param>
        /// <param name="batchingService">Service for batching log operations</param>
        /// <param name="healthCheckService">Health check service for monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        public LoggingService(
            LoggingConfig config,
            IEnumerable<ILogTarget> targets = null,
            LogFormattingService formattingService = null,
            LogBatchingService batchingService = null,
            IHealthCheckService healthCheckService = null,
            IAlertService alertService = null,
            IProfilerService profilerService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _targets = new ConcurrentDictionary<string, ILogTarget>();
            _formattingService = formattingService;
            _batchingService = batchingService;
            _healthCheckService = healthCheckService;
            _alertService = alertService;
            _profilerService = profilerService;
            
            _isEnabled = config.IsLoggingEnabled;
            _performanceStopwatch = Stopwatch.StartNew();
            _lastHealthCheck = DateTime.UtcNow;
            
            InitializeNativeCollections();
            RegisterInitialTargets(targets);
            RegisterHealthCheck();
            
            LogInfo("LoggingService initialized successfully", "Logging.Bootstrap", "LoggingService");
        }
        
        /// <summary>
        /// Gets the current configuration of the logging service.
        /// </summary>
        public LoggingConfig Configuration => _config;
        
        /// <summary>
        /// Gets whether the logging service is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_disposed;
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                LogInternal(LogLevel.Debug, "Default", message);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                LogInternal(LogLevel.Debug, "Default", message, null, null, properties);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                LogInternal(LogLevel.Debug, "Default", message, null, correlationId, null, sourceContext);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message)
        {
            if (ShouldLog(LogLevel.Info))
            {
                LogInternal(LogLevel.Info, "Default", message);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Info))
            {
                LogInternal(LogLevel.Info, "Default", message, null, null, properties);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Info))
            {
                LogInternal(LogLevel.Info, "Default", message, null, correlationId, null, sourceContext);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                LogInternal(LogLevel.Warning, "Default", message);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                LogInternal(LogLevel.Warning, "Default", message, null, null, properties);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                LogInternal(LogLevel.Warning, "Default", message, null, correlationId, null, sourceContext);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message)
        {
            if (ShouldLog(LogLevel.Error))
            {
                LogInternal(LogLevel.Error, "Default", message);
                TriggerErrorAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Error))
            {
                LogInternal(LogLevel.Error, "Default", message, null, null, properties);
                TriggerErrorAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Error))
            {
                LogInternal(LogLevel.Error, "Default", message, null, correlationId, null, sourceContext);
                TriggerErrorAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                LogInternal(LogLevel.Critical, "Default", message);
                TriggerCriticalAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message, IReadOnlyDictionary<string, object> properties)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                LogInternal(LogLevel.Critical, "Default", message, null, null, properties);
                TriggerCriticalAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message, string correlationId, string sourceContext = null)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                LogInternal(LogLevel.Critical, "Default", message, null, correlationId, null, sourceContext);
                TriggerCriticalAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        public void LogException(Exception exception, string context = null)
        {
            if (exception == null) return;
            
            if (ShouldLog(LogLevel.Error))
            {
                var message = context != null ? $"{context}: {exception.Message}" : exception.Message;
                LogInternal(LogLevel.Error, "Default", message, exception);
                TriggerErrorAlert(message, exception);
                
                Interlocked.Increment(ref _totalErrorsEncountered);
            }
        }
        
        /// <inheritdoc />
        public void LogException(Exception exception, string context, string correlationId, string sourceContext = null)
        {
            if (exception == null) return;
            
            if (ShouldLog(LogLevel.Error))
            {
                var message = context != null ? $"{context}: {exception.Message}" : exception.Message;
                LogInternal(LogLevel.Error, "Default", message, exception, correlationId, null, sourceContext);
                TriggerErrorAlert(message, exception);
                
                Interlocked.Increment(ref _totalErrorsEncountered);
            }
        }
        
        /// <inheritdoc />
        public void Log(LogLevel level, string channel, string message, Exception exception = null, 
                       string correlationId = null, IReadOnlyDictionary<string, object> properties = null, 
                       string sourceContext = null)
        {
            if (ShouldLog(level))
            {
                LogInternal(level, channel ?? "Default", message, exception, correlationId, properties, sourceContext);
                
                // Trigger alerts for errors and critical messages
                if (level >= LogLevel.Error)
                {
                    if (level == LogLevel.Critical)
                        TriggerCriticalAlert(message, exception);
                    else
                        TriggerErrorAlert(message, exception);
                }
            }
        }
        
        /// <inheritdoc />
        public void RegisterTarget(ILogTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            
            if (_disposed)
                throw new ObjectDisposedException(nameof(LoggingService));
            
            lock (_lock)
            {
                if (_targets.TryAdd(target.Name, target))
                {
                    UpdateBatchingServiceTargets();
                    LogInfo($"Registered log target: {target.Name}", "Logging.Registration", "LoggingService");
                }
                else
                {
                    LogWarning($"Target with name '{target.Name}' is already registered", "Logging.Registration", "LoggingService");
                }
            }
        }
        
        /// <inheritdoc />
        public bool UnregisterTarget(ILogTarget target)
        {
            if (target == null) return false;
            return UnregisterTarget(target.Name);
        }
        
        /// <inheritdoc />
        public bool UnregisterTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            if (_disposed) return false;
            
            lock (_lock)
            {
                if (_targets.TryRemove(targetName, out var target))
                {
                    try
                    {
                        target.Dispose();
                        UpdateBatchingServiceTargets();
                        LogInfo($"Unregistered log target: {targetName}", "Logging.Registration", "LoggingService");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error disposing target '{targetName}': {ex.Message}", "Logging.Registration", "LoggingService");
                        return false;
                    }
                }
                
                return false;
            }
        }
        
        /// <inheritdoc />
        public ILogTarget GetTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return null;
            
            _targets.TryGetValue(targetName, out var target);
            return target;
        }
        
        /// <inheritdoc />
        public bool HasTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            return _targets.ContainsKey(targetName);
        }
        
        /// <inheritdoc />
        public void SetMinimumLevel(LogLevel minimumLevel)
        {
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.MinimumLevel = minimumLevel;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set minimum level for target '{target.Name}': {ex.Message}", "Logging.Configuration", "LoggingService");
                }
            }
            
            LogInfo($"Set global minimum level to {minimumLevel}", "Logging.Configuration", "LoggingService");
        }
        
        /// <inheritdoc />
        public bool SetMinimumLevel(string targetName, LogLevel minimumLevel)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                try
                {
                    target.MinimumLevel = minimumLevel;
                    LogInfo($"Set minimum level for target '{targetName}' to {minimumLevel}", "Logging.Configuration", "LoggingService");
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set minimum level for target '{targetName}': {ex.Message}", "Logging.Configuration", "LoggingService");
                    return false;
                }
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.IsEnabled = enabled;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set enabled state for target '{target.Name}': {ex.Message}", "Logging.Configuration", "LoggingService");
                }
            }
            
            LogInfo($"Set global enabled state to {enabled}", "Logging.Configuration", "LoggingService");
        }
        
        /// <inheritdoc />
        public bool SetEnabled(string targetName, bool enabled)
        {
            if (string.IsNullOrEmpty(targetName)) return false;
            
            if (_targets.TryGetValue(targetName, out var target))
            {
                try
                {
                    target.IsEnabled = enabled;
                    LogInfo($"Set enabled state for target '{targetName}' to {enabled}", "Logging.Configuration", "LoggingService");
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set enabled state for target '{targetName}': {ex.Message}", "Logging.Configuration", "LoggingService");
                    return false;
                }
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public IReadOnlyList<ILogTarget> GetRegisteredTargets()
        {
            return _targets.Values.ToList().AsReadOnly();
        }
        
        /// <inheritdoc />
        public void Flush()
        {
            using var scope = _profilerService?.BeginScope("LoggingService.Flush");
            
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
                    TriggerTargetErrorAlert(target.Name, ex);
                }
            }
        }
        
        /// <inheritdoc />
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
                    System.Diagnostics.Debug.WriteLine($"LoggingService flush error for target '{targetName}': {ex.Message}");
                    TriggerTargetErrorAlert(targetName, ex);
                    return false;
                }
            }
            
            return false;
        }
        
        /// <inheritdoc />
        public bool PerformHealthCheck()
        {
            if (_disposed) return false;
            
            _lastHealthCheck = DateTime.UtcNow;
            
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
                    System.Diagnostics.Debug.WriteLine($"LoggingService health check error for target '{target.Name}': {ex.Message}");
                    TriggerTargetErrorAlert(target.Name, ex);
                    return false;
                }
            }
            
            return true;
        }
        
        /// <inheritdoc />
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
                    healthStatus[target.Name] = false;
                    System.Diagnostics.Debug.WriteLine($"Health check failed for target '{target.Name}': {ex.Message}");
                }
            }
            
            return healthStatus.AsReadOnly();
        }
        
        /// <summary>
        /// Gets performance statistics for the logging service.
        /// </summary>
        /// <returns>Performance statistics</returns>
        public LoggingStatistics GetStatistics()
        {
            var totalTargets = _targets.Count;
            var healthyTargets = GetHealthStatus().Values.Count(healthy => healthy);
            var errorRate = _totalMessagesProcessed > 0 ? (double)_totalErrorsEncountered / _totalMessagesProcessed : 0.0;
            
            return new LoggingStatistics
            {
                MessagesProcessed = _totalMessagesProcessed,
                ErrorCount = _totalErrorsEncountered,
                ErrorRate = errorRate,
                ActiveTargets = totalTargets,
                HealthyTargets = healthyTargets,
                UptimeSeconds = _performanceStopwatch.Elapsed.TotalSeconds,
                LastHealthCheck = _lastHealthCheck
            };
        }
        
        /// <summary>
        /// Internal logging method with full feature support.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogInternal(
            LogLevel level,
            string channel,
            string message,
            Exception exception = null,
            string correlationId = null,
            IReadOnlyDictionary<string, object> properties = null,
            string sourceContext = null)
        {
            if (!IsEnabled || message == null) return;
            
            try
            {
                using var scope = _profilerService?.BeginScope("LoggingService.LogInternal");
                
                var logMessage = CreateLogMessage(level, channel, message, exception, correlationId, properties, sourceContext);
                
                if (_config.HighPerformanceMode && _batchingService != null)
                {
                    _batchingService.QueueMessage(logMessage);
                }
                else
                {
                    WriteToTargets(logMessage);
                }
                
                Interlocked.Increment(ref _totalMessagesProcessed);
                
                // Periodic health check
                if (DateTime.UtcNow - _lastHealthCheck > _healthCheckInterval)
                {
                    Task.Run(() => PerformAsyncHealthCheck());
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalErrorsEncountered);
                System.Diagnostics.Debug.WriteLine($"LoggingService internal error: {ex.Message}");
                TriggerInternalErrorAlert(ex);
            }
        }
        
        /// <summary>
        /// Creates a structured log message from the provided parameters.
        /// </summary>
        private LogMessage CreateLogMessage(
            LogLevel level,
            string channel,
            string message,
            Exception exception,
            string correlationId,
            IReadOnlyDictionary<string, object> properties,
            string sourceContext)
        {
            var timestamp = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(correlationId) && _config.AutoCorrelationId)
            {
                correlationId = Guid.NewGuid().ToString("N");
            }
            
            return new LogMessage
            {
                Level = level,
                Channel = channel ?? "Default",
                Message = message,
                Exception = exception,
                Timestamp = timestamp,
                CorrelationId = correlationId,
                SourceContext = sourceContext,
                Properties = properties,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };
        }
        
        /// <summary>
        /// Writes a log message to all appropriate targets.
        /// </summary>
        private void WriteToTargets(LogMessage logMessage)
        {
            foreach (var target in _targets.Values)
            {
                try
                {
                    if (target.IsEnabled && target.ShouldProcessMessage(logMessage))
                    {
                        target.Write(logMessage);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalErrorsEncountered);
                    System.Diagnostics.Debug.WriteLine($"Error writing to target '{target.Name}': {ex.Message}");
                    TriggerTargetErrorAlert(target.Name, ex);
                }
            }
        }
        
        /// <summary>
        /// Determines whether a message at the specified level should be logged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldLog(LogLevel level)
        {
            return IsEnabled && level >= _config.GlobalMinimumLevel;
        }
        
        /// <summary>
        /// Triggers an error-level alert for critical logging failures.
        /// </summary>
        private void TriggerErrorAlert(string message, Exception exception)
        {
            if (_alertService == null) return;
            
            try
            {
                var alertMessage = exception != null ? $"{message} - {exception.Message}" : message;
                _alertService.RaiseAlert(
                    new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                    AlertSeverity.High,
                    new FixedString64Bytes("LoggingService"),
                    new FixedString64Bytes("Error"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to raise error alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Triggers a critical-level alert for severe logging failures.
        /// </summary>
        private void TriggerCriticalAlert(string message, Exception exception)
        {
            if (_alertService == null) return;
            
            var now = DateTime.UtcNow;
            if (now - _lastCriticalAlert < _alertCooldown) return; // Prevent alert spam
            
            _lastCriticalAlert = now;
            
            try
            {
                var alertMessage = exception != null ? $"CRITICAL: {message} - {exception.Message}" : $"CRITICAL: {message}";
                _alertService.RaiseAlert(
                    new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                    AlertSeverity.Critical,
                    new FixedString64Bytes("LoggingService"),
                    new FixedString64Bytes("Critical"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to raise critical alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Triggers an alert for target-specific errors.
        /// </summary>
        private void TriggerTargetErrorAlert(string targetName, Exception exception)
        {
            if (_alertService == null) return;
            
            try
            {
                var alertMessage = $"Log target '{targetName}' error: {exception.Message}";
                _alertService.RaiseAlert(
                    new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                    AlertSeverity.Medium,
                    new FixedString64Bytes("LoggingService"),
                    new FixedString64Bytes("TargetError"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to raise target error alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Triggers an alert for internal logging service errors.
        /// </summary>
        private void TriggerInternalErrorAlert(Exception exception)
        {
            if (_alertService == null) return;
            
            try
            {
                var alertMessage = $"Logging service internal error: {exception.Message}";
                _alertService.RaiseAlert(
                    new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                    AlertSeverity.Critical,
                    new FixedString64Bytes("LoggingService"),
                    new FixedString64Bytes("Internal"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to raise internal error alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initializes native collections for high-performance scenarios.
        /// </summary>
        private void InitializeNativeCollections()
        {
            if (_config.HighPerformanceMode)
            {
                _nativeMessageBuffer = new NativeArray<LogMessage>(_config.MaxQueueSize, Allocator.Persistent);
                _channelLookup = new NativeHashMap<FixedString64Bytes, int>(16, Allocator.Persistent);
            }
        }
        
        /// <summary>
        /// Registers initial targets with the logging service.
        /// </summary>
        private void RegisterInitialTargets(IEnumerable<ILogTarget> targets)
        {
            if (targets == null) return;
            
            foreach (var target in targets)
            {
                try
                {
                    RegisterTarget(target);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to register initial target '{target?.Name}': {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Updates the batching service with current targets.
        /// </summary>
        private void UpdateBatchingServiceTargets()
        {
            // Note: In a complete implementation, LogBatchingService would support dynamic target updates
        }
        
        /// <summary>
        /// Registers this service with the health check system.
        /// </summary>
        private void RegisterHealthCheck()
        {
            if (_healthCheckService == null) return;
            
            try
            {
                var healthCheck = new LoggingServiceHealthCheck(this);
                _healthCheckService.RegisterHealthCheck(healthCheck);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register logging health check: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Performs an asynchronous health check without blocking the main thread.
        /// </summary>
        private async Task PerformAsyncHealthCheck()
        {
            try
            {
                var isHealthy = PerformHealthCheck();
                if (!isHealthy)
                {
                    TriggerErrorAlert("Logging service health check failed", null);
                }
            }
            catch (Exception ex)
            {
                TriggerInternalErrorAlert(ex);
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
            
            // Dispose native collections
            if (_nativeMessageBuffer.IsCreated)
            {
                _nativeMessageBuffer.Dispose();
            }
            
            if (_channelLookup.IsCreated)
            {
                _channelLookup.Dispose();
            }
            
            // Dispose all targets
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LoggingService disposal error for target '{target.Name}': {ex.Message}");
                }
            }
            
            _targets.Clear();
            _performanceStopwatch?.Stop();
        }
    }
}