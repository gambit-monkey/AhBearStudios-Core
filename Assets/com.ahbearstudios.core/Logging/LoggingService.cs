using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Filters;

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
        private readonly ConcurrentDictionary<string, ILogChannel> _channels;
        private readonly LogFormattingService _formattingService;
        private readonly LogBatchingService _batchingService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IMessageBusService _messageBusService;
        
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
        /// <param name="messageBusService">Message bus service for loose coupling through events</param>
        public LoggingService(
            LoggingConfig config,
            IEnumerable<ILogTarget> targets = null,
            LogFormattingService formattingService = null,
            LogBatchingService batchingService = null,
            IHealthCheckService healthCheckService = null,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IMessageBusService messageBusService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _targets = new ConcurrentDictionary<string, ILogTarget>();
            _channels = new ConcurrentDictionary<string, ILogChannel>();
            _formattingService = formattingService;
            _batchingService = batchingService;
            _healthCheckService = healthCheckService;
            _alertService = alertService;
            _profilerService = profilerService;
            _messageBusService = messageBusService;
            
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
        public void LogDebug(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Debug, "Default", message, null, correlationIdStr, properties, sourceContext);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            if (ShouldLog(LogLevel.Info))
            {
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Info, "Default", message, null, correlationIdStr, properties, sourceContext);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Warning, "Default", message, null, correlationIdStr, properties, sourceContext);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            if (ShouldLog(LogLevel.Error))
            {
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Error, "Default", message, null, correlationIdStr, properties, sourceContext);
                TriggerErrorAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            if (ShouldLog(LogLevel.Critical))
            {
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Critical, "Default", message, null, correlationIdStr, properties, sourceContext);
                TriggerCriticalAlert(message, null);
            }
        }
        
        /// <inheritdoc />
        public void LogException(string message, Exception exception, FixedString64Bytes correlationId = default, 
            string sourceContext = null, IReadOnlyDictionary<string, object> properties = null)
        {
            if (exception == null) return;
            
            if (ShouldLog(LogLevel.Error))
            {
                var contextMessage = !string.IsNullOrEmpty(message) ? $"{message}: {exception.Message}" : exception.Message;
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Error, "Default", contextMessage, exception, correlationIdStr, properties, sourceContext);
                TriggerErrorAlert(contextMessage, exception);
                
                Interlocked.Increment(ref _totalErrorsEncountered);
            }
        }
        
        /// <inheritdoc />
        public void Log(LogLevel level, string message, FixedString64Bytes correlationId = default, 
            string sourceContext = null, Exception exception = null, 
            IReadOnlyDictionary<string, object> properties = null, string channel = null)
        {
            if (ShouldLog(level))
            {
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(level, channel ?? "Default", message, exception, correlationIdStr, properties, sourceContext);
                
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
        public void RegisterTarget(ILogTarget target, FixedString64Bytes correlationId = default)
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
                    LogInfo($"Registered log target: {target.Name}", correlationId, "LoggingService");
                    
                    // Publish configuration change message for loose coupling
                    if (_messageBusService != null)
                    {
                        var configMessage = LogConfigurationChangedMessage.Create(
                            LogConfigurationChangeType.TargetAdded,
                            "LoggingService",
                            "RegisteredTargets",
                            previousValue: $"{_targets.Count - 1} targets",
                            newValue: $"{_targets.Count} targets (added: {target.Name})",
                            changedBy: "LoggingService",
                            changeReason: "Target registration");
                        
                        _messageBusService.PublishMessage(configMessage);
                    }
                }
                else
                {
                    LogWarning($"Target with name '{target.Name}' is already registered", correlationId, "LoggingService");
                }
            }
        }
        
        /// <inheritdoc />
        public bool UnregisterTarget(string targetName, FixedString64Bytes correlationId = default)
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
                        LogInfo($"Unregistered log target: {targetName}", correlationId, "LoggingService");
                        
                        // Publish configuration change message for loose coupling
                        if (_messageBusService != null)
                        {
                            var configMessage = LogConfigurationChangedMessage.Create(
                                LogConfigurationChangeType.TargetRemoved,
                                "LoggingService",
                                "RegisteredTargets",
                                previousValue: $"{_targets.Count + 1} targets",
                                newValue: $"{_targets.Count} targets (removed: {targetName})",
                                changedBy: "LoggingService",
                                changeReason: "Target unregistration");
                            
                            _messageBusService.PublishMessage(configMessage);
                        }
                        
                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error disposing target '{targetName}': {ex.Message}", correlationId, "LoggingService");
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
            
            return new ReadOnlyDictionary<string, bool>(healthStatus);
        }
        
        /// <inheritdoc />
        public LoggingStatistics GetStatistics()
        {
            var totalTargets = _targets.Count;
            var healthyTargets = GetHealthStatus().Values.Count(healthy => healthy);
            
            return LoggingStatistics.Create(
                messagesProcessed: _totalMessagesProcessed,
                errorCount: _totalErrorsEncountered,
                activeTargets: totalTargets,
                healthyTargets: healthyTargets,
                uptimeSeconds: _performanceStopwatch.Elapsed.TotalSeconds,
                lastHealthCheck: _lastHealthCheck
            );
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
                    _batchingService.EnqueueMessage(logMessage);
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
            if (string.IsNullOrEmpty(correlationId) && _config.AutoCorrelationId)
            {
                correlationId = Guid.NewGuid().ToString("N");
            }
            
            return LogMessage.Create(
                level: level,
                channel: channel,
                message: message,
                exception: exception,
                correlationId: correlationId,
                properties: properties,
                sourceContext: sourceContext,
                threadId: Thread.CurrentThread.ManagedThreadId);
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
        /// Publishes a critical logging message through the message bus for loose coupling.
        /// Also triggers an alert for backward compatibility.
        /// </summary>
        private void TriggerCriticalAlert(string message, Exception exception)
        {
            var now = DateTime.UtcNow;
            if (now - _lastCriticalAlert < _alertCooldown) return; // Prevent alert spam
            
            _lastCriticalAlert = now;
            
            try
            {
                // Publish health status message for critical issues
                if (_messageBusService != null)
                {
                    var healthMessage = LoggingSystemHealthMessage.Create(
                        isHealthy: false,
                        healthyTargets: _targets.Values.Count(t => t.IsHealthy),
                        totalTargets: _targets.Count,
                        activeChannels: _channels.Count,
                        details: $"Critical logging error: {message}");
                    
                    _messageBusService.PublishMessage(healthMessage);
                }
                
                // Also trigger alert for backward compatibility
                if (_alertService != null)
                {
                    var alertMessage = exception != null ? $"CRITICAL: {message} - {exception.Message}" : $"CRITICAL: {message}";
                    _alertService.RaiseAlert(
                        new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                        AlertSeverity.Critical,
                        new FixedString64Bytes("LoggingService"),
                        new FixedString64Bytes("Critical"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish critical message or alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publishes a target error message through the message bus for loose coupling.
        /// Also triggers an alert for backward compatibility.
        /// </summary>
        private void TriggerTargetErrorAlert(string targetName, Exception exception)
        {
            try
            {
                // Publish message through message bus for loose coupling
                if (_messageBusService != null)
                {
                    var errorMessage = LogTargetErrorMessage.Create(
                        targetName,
                        exception?.Message ?? "Unknown error",
                        severity: LogTargetErrorSeverity.Error);
                    
                    _messageBusService.PublishMessage(errorMessage);
                }
                
                // Also trigger alert for backward compatibility
                if (_alertService != null)
                {
                    var alertMessage = $"Log target '{targetName}' error: {exception?.Message ?? "Unknown error"}";
                    _alertService.RaiseAlert(
                        new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                        AlertSeverity.Medium,
                        new FixedString64Bytes("LoggingService"),
                        new FixedString64Bytes("TargetError"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish target error message or alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publishes an internal error message through the message bus for loose coupling.
        /// Also triggers an alert for backward compatibility.
        /// </summary>
        private void TriggerInternalErrorAlert(Exception exception)
        {
            try
            {
                // Publish internal error as target error message
                if (_messageBusService != null)
                {
                    var errorMessage = LogTargetErrorMessage.Create(
                        "LoggingService",
                        $"Internal logging service error: {exception?.Message ?? "Unknown error"}",
                        severity: LogTargetErrorSeverity.Critical);
                    
                    _messageBusService.PublishMessage(errorMessage);
                }
                
                // Also trigger alert for backward compatibility
                if (_alertService != null)
                {
                    var alertMessage = $"Logging service internal error: {exception?.Message ?? "Unknown error"}";
                    _alertService.RaiseAlert(
                        new FixedString128Bytes(alertMessage.Substring(0, Math.Min(alertMessage.Length, 127))),
                        AlertSeverity.Critical,
                        new FixedString64Bytes("LoggingService"),
                        new FixedString64Bytes("Internal"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish internal error message or alert: {ex.Message}");
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
                var healthCheck = LoggingServiceHealthCheck.Create(this);
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
        


        // Channel management methods implementation
        /// <inheritdoc />
        public void RegisterChannel(ILogChannel channel, FixedString64Bytes correlationId = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            if (_disposed)
                throw new ObjectDisposedException(nameof(LoggingService));

            if (_channels.TryAdd(channel.Name, channel))
            {
                LogInfo($"Registered log channel: {channel.Name}", correlationId, "LoggingService");
            }
            else
            {
                LogWarning($"Channel with name '{channel.Name}' is already registered", correlationId, "LoggingService");
            }
        }

        /// <inheritdoc />
        public bool UnregisterChannel(string channelName, FixedString64Bytes correlationId = default)
        {
            if (string.IsNullOrEmpty(channelName) || _disposed)
                return false;

            if (_channels.TryRemove(channelName, out var channel))
            {
                LogInfo($"Unregistered log channel: {channelName}", correlationId, "LoggingService");
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<ILogChannel> GetChannels()
        {
            return _channels.Values.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public ILogChannel GetChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
                return null;

            _channels.TryGetValue(channelName, out var channel);
            return channel;
        }

        /// <inheritdoc />
        public bool HasChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
                return false;

            return _channels.ContainsKey(channelName);
        }

        /// <inheritdoc />
        public ILogScope BeginScope(string scopeName, FixedString64Bytes correlationId = default, 
            string sourceContext = null)
        {
            var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
            return LogScope.Create(this, scopeName, correlationIdStr, sourceContext);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<ILogTarget> GetTargets()
        {
            return _targets.Values.ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public void SetMinimumLevel(LogLevel level, FixedString64Bytes correlationId = default)
        {
            foreach (var target in _targets.Values)
            {
                try
                {
                    target.MinimumLevel = level;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set minimum level for target '{target.Name}': {ex.Message}", correlationId, "LoggingService");
                }
            }
            
            LogInfo($"Set global minimum level to {level}", correlationId, "LoggingService");
        }

        /// <inheritdoc />
        public void AddFilter(ILogFilter filter, FixedString64Bytes correlationId = default)
        {
            // Implementation for adding log filters
            // This would be implemented based on the ILogFilter interface design
            LogInfo($"Added log filter: {filter?.GetType().Name ?? "Unknown"}", correlationId, "LoggingService");
        }

        /// <inheritdoc />
        public bool RemoveFilter(string filterName, FixedString64Bytes correlationId = default)
        {
            // Implementation for removing log filters
            // This would be implemented based on the ILogFilter interface design
            LogInfo($"Removed log filter: {filterName}", correlationId, "LoggingService");
            return true; // Placeholder
        }

        /// <inheritdoc />
        public async Task FlushAsync(FixedString64Bytes correlationId = default)
        {
            using var scope = _profilerService?.BeginScope("LoggingService.FlushAsync");
            
            var flushTasks = new List<Task>();
            
            if (_batchingService != null)
            {
                flushTasks.Add(Task.Run(() => _batchingService.ForceFlush()));
            }
            
            foreach (var target in _targets.Values)
            {
                flushTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        target.Flush();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"FlushAsync error for target '{target.Name}': {ex.Message}");
                        TriggerTargetErrorAlert(target.Name, ex);
                    }
                }));
            }
            
            await Task.WhenAll(flushTasks).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            // Validate logging configuration
            if (_config != null)
            {
                var configErrors = _config.Validate();
                errors.AddRange(configErrors.Select(e => new ValidationError(e, "Configuration")));
            }
            else
            {
                errors.Add(new ValidationError("Logging configuration is null", "Configuration"));
            }
            
            // Validate targets
            if (_targets.Count == 0)
            {
                warnings.Add(new ValidationWarning("No log targets registered", "Targets"));
            }
            
            foreach (var target in _targets.Values)
            {
                try
                {
                    if (!target.PerformHealthCheck())
                    {
                        warnings.Add(new ValidationWarning($"Target '{target.Name}' failed health check", $"Target.{target.Name}"));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError($"Target '{target.Name}' validation error: {ex.Message}", $"Target.{target.Name}"));
                }
            }
            
            // Use appropriate factory method based on whether errors exist
            if (errors.Count == 0)
            {
                return ValidationResult.Success(
                    component: "LoggingService",
                    warnings: warnings,
                    context: new Dictionary<string, object>
                    {
                        ["TargetCount"] = _targets.Count,
                        ["ChannelCount"] = _channels.Count,
                        ["IsEnabled"] = _isEnabled,
                        ["CorrelationId"] = correlationId.ToString()
                    });
            }
            else
            {
                return ValidationResult.Failure(
                    errors: errors,
                    component: "LoggingService",
                    warnings: warnings,
                    context: new Dictionary<string, object>
                    {
                        ["TargetCount"] = _targets.Count,
                        ["ChannelCount"] = _channels.Count,
                        ["IsEnabled"] = _isEnabled,
                        ["CorrelationId"] = correlationId.ToString()
                    });
            }
        }

        /// <inheritdoc />
        public void PerformMaintenance(FixedString64Bytes correlationId = default)
        {
            try
            {
                // Clear any internal caches
                if (_formattingService != null)
                {
                    // Clear formatter caches if available
                }
                
                // Force garbage collection for memory cleanup
                if (_config?.HighPerformanceMode == false)
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }
                
                // Perform target maintenance
                foreach (var target in _targets.Values)
                {
                    try
                    {
                        target.PerformHealthCheck();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Maintenance error for target '{target.Name}': {ex.Message}", correlationId, "LoggingService");
                    }
                }
                
                LogInfo("Logging service maintenance completed", correlationId, "LoggingService");
            }
            catch (Exception ex)
            {
                LogError($"Maintenance operation failed: {ex.Message}", correlationId, "LoggingService");
            }
        }

        // Generic structured logging methods with Burst compatibility
        /// <inheritdoc />
        public void LogDebug<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (ShouldLog(LogLevel.Debug))
            {
                var properties = ConvertUnmanagedToProperties(data);
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Debug, "Default", message, null, correlationIdStr, properties);
            }
        }

        /// <inheritdoc />
        public void LogInfo<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (ShouldLog(LogLevel.Info))
            {
                var properties = ConvertUnmanagedToProperties(data);
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Info, "Default", message, null, correlationIdStr, properties);
            }
        }

        /// <inheritdoc />
        public void LogWarning<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (ShouldLog(LogLevel.Warning))
            {
                var properties = ConvertUnmanagedToProperties(data);
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Warning, "Default", message, null, correlationIdStr, properties);
            }
        }

        /// <inheritdoc />
        public void LogError<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (ShouldLog(LogLevel.Error))
            {
                var properties = ConvertUnmanagedToProperties(data);
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Error, "Default", message, null, correlationIdStr, properties);
                TriggerErrorAlert(message, null);
            }
        }

        /// <inheritdoc />
        public void LogCritical<T>(string message, T data, FixedString64Bytes correlationId = default) where T : unmanaged
        {
            if (ShouldLog(LogLevel.Critical))
            {
                var properties = ConvertUnmanagedToProperties(data);
                var correlationIdStr = correlationId.IsEmpty ? null : correlationId.ToString();
                LogInternal(LogLevel.Critical, "Default", message, null, correlationIdStr, properties);
                TriggerCriticalAlert(message, null);
            }
        }

        /// <summary>
        /// Converts unmanaged data to properties dictionary for structured logging.
        /// </summary>
        /// <typeparam name="T">The unmanaged type</typeparam>
        /// <param name="data">The data to convert</param>
        /// <returns>Properties dictionary</returns>
        private IReadOnlyDictionary<string, object> ConvertUnmanagedToProperties<T>(T data) where T : unmanaged
        {
            var properties = new Dictionary<string, object>();
            
            // Use reflection to get fields and properties of the unmanaged type
            var type = typeof(T);
            var fields = type.GetFields();
            
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(data);
                    properties[field.Name] = value;
                }
                catch (Exception ex)
                {
                    properties[field.Name] = $"Error reading field: {ex.Message}";
                }
            }
            
            return properties;
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