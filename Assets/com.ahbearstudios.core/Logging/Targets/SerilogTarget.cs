using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Unity.Profiling;
using ILogger = Serilog.ILogger;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// Unity game-optimized Serilog log target with performance-first design.
    /// Provides structured logging with frame budget awareness and game-specific optimizations.
    /// Follows AhBearStudios Unity game development guidelines with lightweight error handling.
    /// </summary>
    public sealed class SerilogTarget : ILogTarget
    {
        private readonly ILogTargetConfig _config;
        private readonly IProfilerService _profiler;
        private readonly IAlertService _alertService;
        private readonly object _loggerLock = new object();
        private readonly Timer _healthCheckTimer;
        private readonly SemaphoreSlim _asyncWriteSemaphore;
        
        // Unity Profiler markers for frame budget monitoring
        private readonly ProfilerMarker _writeMarker = new ProfilerMarker("SerilogTarget.Write");
        private readonly ProfilerMarker _batchWriteMarker = new ProfilerMarker("SerilogTarget.WriteBatch");
        private readonly ProfilerMarker _healthCheckMarker = new ProfilerMarker("SerilogTarget.HealthCheck");
        
        // Core system profiling tags
        private static readonly ProfilerTag WriteTag = new ProfilerTag("SerilogTarget.Write");
        private static readonly ProfilerTag BatchWriteTag = new ProfilerTag("SerilogTarget.WriteBatch");
        private static readonly ProfilerTag HealthCheckTag = new ProfilerTag("SerilogTarget.HealthCheck");
        
        private ILogger _serilogLogger;
        private volatile bool _disposed = false;
        private long _messagesWritten = 0;
        private long _messagesDropped = 0;
        private long _errorsEncountered = 0;
        private DateTime _lastWriteTime = DateTime.MinValue;
        private DateTime _lastHealthCheck = DateTime.MinValue;
        private volatile bool _isHealthy = true;
        private Exception _lastError = null;
        
        // Performance monitoring thresholds
        private const double ERROR_RATE_THRESHOLD = 0.1; // 10% error rate
        private const double FRAME_BUDGET_THRESHOLD_MS = 0.5; // 0.5ms per write operation
        private const int ALERT_SUPPRESSION_INTERVAL_MINUTES = 5;

        /// <summary>
        /// Gets the name of this log target.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// </summary>
        public LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Gets or sets whether this target is enabled and should process log messages.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets whether this target is currently healthy and operational.
        /// </summary>
        public bool IsHealthy => !_disposed && _isHealthy;

        /// <summary>
        /// Gets the list of channels this target listens to.
        /// </summary>
        public IReadOnlyList<string> Channels { get; }

        /// <summary>
        /// Gets the number of messages successfully written.
        /// </summary>
        public long MessagesWritten => _messagesWritten;

        /// <summary>
        /// Gets the number of messages dropped due to errors or filtering.
        /// </summary>
        public long MessagesDropped => _messagesDropped;

        /// <summary>
        /// Gets the number of errors encountered.
        /// </summary>
        public long ErrorsEncountered => _errorsEncountered;

        /// <summary>
        /// Gets the underlying Serilog logger instance.
        /// </summary>
        public ILogger UnderlyingLogger => _serilogLogger;

        /// <summary>
        /// Gets whether async writing is enabled.
        /// </summary>
        public bool UseAsyncWrite { get; }

        /// <summary>
        /// Gets the maximum concurrent async operations.
        /// </summary>
        public int MaxConcurrentAsyncOperations { get; }

        /// <summary>
        /// Initializes a new instance of the SerilogTarget with full core system integration.
        /// </summary>
        /// <param name="config">The configuration for this target</param>
        /// <param name="profiler">The profiler service for performance monitoring</param>
        /// <param name="alertService">The alert service for critical notifications</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public SerilogTarget(ILogTargetConfig config, IProfilerService profiler, IAlertService alertService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            
            Name = _config.Name;
            MinimumLevel = _config.MinimumLevel;
            IsEnabled = _config.IsEnabled;
            Channels = _config.Channels;
            UseAsyncWrite = _config.UseAsyncWrite;
            MaxConcurrentAsyncOperations = _config.MaxConcurrentAsyncOperations;

            // Initialize async semaphore for write operations
            if (UseAsyncWrite)
            {
                _asyncWriteSemaphore = new SemaphoreSlim(MaxConcurrentAsyncOperations, MaxConcurrentAsyncOperations);
            }

            // Register performance metric alerts with profiler service
            RegisterPerformanceAlerts();

            // Initialize Serilog logger
            InitializeSerilogLogger();

            // Start health check timer - using configured interval
            var healthCheckInterval = TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds);
            _healthCheckTimer = new Timer(PerformPeriodicHealthCheck, null, healthCheckInterval, healthCheckInterval);

            _lastHealthCheck = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance with a pre-configured Serilog logger.
        /// </summary>
        /// <param name="config">The target configuration</param>
        /// <param name="profiler">The profiler service for performance monitoring</param>
        /// <param name="alertService">The alert service for critical notifications</param>
        /// <param name="serilogLogger">The pre-configured Serilog logger</param>
        public SerilogTarget(ILogTargetConfig config, IProfilerService profiler, IAlertService alertService, ILogger serilogLogger) 
            : this(config, profiler, alertService)
        {
            _serilogLogger = serilogLogger ?? throw new ArgumentNullException(nameof(serilogLogger));
        }

        /// <summary>
        /// Writes a log message to Serilog with full performance monitoring and alerting.
        /// </summary>
        /// <param name="logMessage">The log message to write</param>
        public void Write(in LogMessage logMessage)
        {
            if (!ShouldProcessMessage(logMessage) || _disposed)
            {
                Interlocked.Increment(ref _messagesDropped);
                return;
            }

            // Unity Profiler integration for frame budget monitoring
            using (_writeMarker.Auto())
            {
                // Core profiling system integration
                using var profilerSession = _profiler.BeginScope(WriteTag);
                
                try
                {
                    if (UseAsyncWrite)
                    {
                        _ = WriteAsync(logMessage);
                    }
                    else
                    {
                        WriteInternal(logMessage);
                    }

                    Interlocked.Increment(ref _messagesWritten);
                    _lastWriteTime = DateTime.UtcNow;

                    // Check for performance threshold violations
                    CheckPerformanceThresholds(profilerSession);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _errorsEncountered);
                    Interlocked.Increment(ref _messagesDropped);
                    _lastError = ex;
                    _isHealthy = false;

                    // Alert on critical logging failures
                    TriggerErrorAlert(ex, "SerilogTarget write failed");

                    // Fallback logging to prevent infinite loops
                    System.Diagnostics.Debug.WriteLine($"SerilogTarget write failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Writes multiple log messages to Serilog in a batch operation.
        /// </summary>
        /// <param name="logMessages">The log messages to write</param>
        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            if (logMessages == null || logMessages.Count == 0 || _disposed)
            {
                return;
            }

            if (UseAsyncWrite)
            {
                _ = WriteBatchAsync(logMessages);
            }
            else
            {
                WriteBatchInternal(logMessages);
            }
        }

        /// <summary>
        /// Determines whether this target should process the given log message.
        /// </summary>
        /// <param name="logMessage">The log message to evaluate</param>
        /// <returns>True if the message should be processed, false otherwise</returns>
        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            if (!IsEnabled || _disposed)
                return false;

            if (logMessage.Level < MinimumLevel)
                return false;

            // Check channel filtering
            if (Channels.Count > 0)
            {
                var messageChannel = logMessage.Channel.ToString();
                var found = false;
                foreach (var channel in Channels)
                {
                    if (string.Equals(channel, messageChannel, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Flushes any buffered log messages in Serilog.
        /// </summary>
        public void Flush()
        {
            try
            {
                lock (_loggerLock)
                {
                    // Serilog doesn't have a direct flush method, but we can dispose and recreate
                    // or use CloseAndFlush if available
                    if (_serilogLogger is Logger logger)
                    {
                        logger.Dispose(); // This flushes automatically
                        InitializeSerilogLogger(); // Recreate
                    }
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorsEncountered);
                _lastError = ex;
                System.Diagnostics.Debug.WriteLine($"SerilogTarget flush failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs a health check on this target with performance monitoring.
        /// </summary>
        /// <returns>True if the target is healthy, false otherwise</returns>
        public bool PerformHealthCheck()
        {
            using (_healthCheckMarker.Auto())
            {
                using var profilerSession = _profiler.BeginScope(HealthCheckTag);
                
                try
                {
                    if (_disposed)
                        return false;

                    // Check if logger is still functional
                    lock (_loggerLock)
                    {
                        if (_serilogLogger == null)
                        {
                            _isHealthy = false;
                            TriggerErrorAlert(new InvalidOperationException("Serilog logger is null"), "SerilogTarget health check failed");
                            return false;
                        }
                    }

                    // Check error rate using configured threshold
                    var totalMessages = _messagesWritten + _messagesDropped;
                    if (totalMessages > 100) // Only check after processing some messages
                    {
                        var errorRate = (double)_errorsEncountered / totalMessages;
                        if (errorRate > _config.ErrorRateThreshold)
                        {
                            _isHealthy = false;
                            TriggerErrorAlert(new InvalidOperationException($"Error rate too high: {errorRate:P1}"), "SerilogTarget error rate exceeded threshold");
                            return false;
                        }
                    }

                    // Check if we've written recently (if we should have)
                    var timeSinceLastWrite = DateTime.UtcNow - _lastWriteTime;
                    if (_messagesWritten > 0 && timeSinceLastWrite > TimeSpan.FromMinutes(30))
                    {
                        // This might indicate a problem, but not necessarily unhealthy
                        // Could just mean no recent log activity
                    }

                    // Perform a test write
                    try
                    {
                        var testMessage = LogMessage.Create(LogLevel.Debug, "HealthCheck", 
                            $"Serilog health check - {DateTime.UtcNow:HH:mm:ss}");
                        
                        WriteInternal(testMessage);
                        _isHealthy = true;
                    }
                    catch (Exception ex)
                    {
                        _lastError = ex;
                        _isHealthy = false;
                        TriggerErrorAlert(ex, "SerilogTarget health check test write failed");
                        return false;
                    }

                    _lastHealthCheck = DateTime.UtcNow;
                    
                    // Check health check performance
                    CheckPerformanceThresholds(profilerSession);
                    
                    return _isHealthy;
                }
                catch (Exception ex)
                {
                    _lastError = ex;
                    _isHealthy = false;
                    TriggerErrorAlert(ex, "SerilogTarget health check failed");
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets detailed statistics about this target's performance.
        /// </summary>
        /// <returns>A dictionary containing performance statistics</returns>
        public Dictionary<string, object> GetStatistics()
        {
            var totalMessages = _messagesWritten + _messagesDropped;
            var errorRate = totalMessages > 0 ? (double)_errorsEncountered / totalMessages : 0.0;

            return new Dictionary<string, object>
            {
                ["MessagesWritten"] = _messagesWritten,
                ["MessagesDropped"] = _messagesDropped,
                ["ErrorsEncountered"] = _errorsEncountered,
                ["ErrorRate"] = errorRate,
                ["LastWriteTime"] = _lastWriteTime,
                ["LastHealthCheck"] = _lastHealthCheck,
                ["IsHealthy"] = _isHealthy,
                ["UseAsyncWrite"] = UseAsyncWrite,
                ["MaxConcurrentAsyncOperations"] = MaxConcurrentAsyncOperations,
                ["MinimumLevel"] = MinimumLevel.ToString(),
                ["ChannelCount"] = Channels.Count,
                ["LastError"] = _lastError?.Message
            };
        }

        /// <summary>
        /// Updates the Serilog configuration at runtime.
        /// </summary>
        /// <param name="configurator">Action to configure the logger</param>
        public void ReconfigureLogger(Action<LoggerConfiguration> configurator)
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));

            try
            {
                lock (_loggerLock)
                {
                    var config = new LoggerConfiguration();
                    configurator(config);
                    
                    var oldLogger = _serilogLogger;
                    _serilogLogger = config.CreateLogger();
                    
                    // Clean up old logger
                    (oldLogger as IDisposable)?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorsEncountered);
                _lastError = ex;
                throw new InvalidOperationException("Failed to reconfigure Serilog logger", ex);
            }
        }

        /// <summary>
        /// Initializes the Serilog logger based on configuration.
        /// </summary>
        private void InitializeSerilogLogger()
        {
            try
            {
                var config = new LoggerConfiguration()
                    .MinimumLevel.Is(ConvertToSerilogLevel(MinimumLevel))
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .Enrich.WithMachineName();

                // Configure sinks based on configuration properties
                ConfigureSinks(config);

                // Configure output template
                var outputTemplate = GetConfigProperty("OutputTemplate", 
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

                _serilogLogger = config.CreateLogger();
            }
            catch (Exception ex)
            {
                // Fallback to minimal console logger
                _serilogLogger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();

                _lastError = ex;
                _isHealthy = false;
                System.Diagnostics.Debug.WriteLine($"SerilogTarget initialization failed, using fallback: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures Serilog sinks based on target configuration.
        /// </summary>
        /// <param name="config">The Serilog configuration</param>
        private void ConfigureSinks(LoggerConfiguration config)
        {
            // Console sink
            if (GetConfigProperty("EnableConsole", false))
            {
                var consoleTemplate = GetConfigProperty("ConsoleTemplate", 
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                config.WriteTo.Console(outputTemplate: consoleTemplate);
            }

            // File sink
            var filePath = GetConfigProperty<string>("FilePath", null);
            if (!string.IsNullOrEmpty(filePath))
            {
                var rollOnFileSizeLimit = GetConfigProperty("RollOnFileSizeLimit", true);
                var fileSizeLimitBytes = GetConfigProperty("FileSizeLimitBytes", 100 * 1024 * 1024); // 100MB
                var retainedFileCountLimit = GetConfigProperty("RetainedFileCountLimit", 31);
                var shared = GetConfigProperty("Shared", false);

                config.WriteTo.File(
                    path: filePath,
                    rollOnFileSizeLimit: rollOnFileSizeLimit,
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    retainedFileCountLimit: retainedFileCountLimit,
                    shared: shared,
                    formatter: new JsonFormatter());
            }

            // Seq sink (if configured)
            var seqServerUrl = GetConfigProperty<string>("SeqServerUrl", null);
            if (!string.IsNullOrEmpty(seqServerUrl))
            {
                var seqApiKey = GetConfigProperty<string>("SeqApiKey", null);
                if (!string.IsNullOrEmpty(seqApiKey))
                {
                    config.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
                }
                else
                {
                    config.WriteTo.Seq(seqServerUrl);
                }
            }

            // Debug sink (for development)
            if (GetConfigProperty("EnableDebug", false))
            {
                config.WriteTo.Debug();
            }

            // Custom sinks can be added here based on additional configuration
            ConfigureCustomSinks(config);
        }

        /// <summary>
        /// Configures custom Serilog sinks based on advanced configuration.
        /// </summary>
        /// <param name="config">The Serilog configuration</param>
        private void ConfigureCustomSinks(LoggerConfiguration config)
        {
            // Example: Email sink for critical errors
            var emailEnabled = GetConfigProperty("EnableEmail", false);
            if (emailEnabled)
            {
                var smtpServer = GetConfigProperty<string>("EmailSmtpServer", null);
                var emailTo = GetConfigProperty<string>("EmailTo", null);
                var emailFrom = GetConfigProperty<string>("EmailFrom", null);

                if (!string.IsNullOrEmpty(smtpServer) && !string.IsNullOrEmpty(emailTo) && !string.IsNullOrEmpty(emailFrom))
                {
                    config.WriteTo.Email(
                        fromEmail: emailFrom,
                        toEmail: emailTo,
                        mailServer: smtpServer,
                        restrictedToMinimumLevel: LogEventLevel.Error);
                }
            }

            // Example: Elasticsearch sink
            var elasticsearchEnabled = GetConfigProperty("EnableElasticsearch", false);
            if (elasticsearchEnabled)
            {
                var elasticsearchUri = GetConfigProperty<string>("ElasticsearchUri", null);
                var indexName = GetConfigProperty("ElasticsearchIndex", "logs-{0:yyyy.MM.dd}");

                if (!string.IsNullOrEmpty(elasticsearchUri))
                {
                    try
                    {
                        config.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticsearchUri))
                        {
                            IndexFormat = indexName,
                            AutoRegisterTemplate = true,
                            OverwriteTemplate = true,
                        });
                    }
                    catch (Exception ex)
                    {
                        // Elasticsearch sink failed to configure
                        _lastError = ex;
                        System.Diagnostics.Debug.WriteLine($"Failed to configure Elasticsearch sink: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Converts AhBearStudios LogLevel to Serilog LogEventLevel.
        /// </summary>
        /// <param name="logLevel">The AhBearStudios log level</param>
        /// <returns>The corresponding Serilog log event level</returns>
        private static LogEventLevel ConvertToSerilogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Info => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        /// <summary>
        /// Internal method to write a log message to Serilog.
        /// </summary>
        /// <param name="logMessage">The log message to write</param>
        private void WriteInternal(in LogMessage logMessage)
        {
            lock (_loggerLock)
            {
                if (_serilogLogger == null || _disposed) return;

                var serilogLevel = ConvertToSerilogLevel(logMessage.Level);
                var messageTemplate = logMessage.Message.ToString();
                var correlationId = logMessage.CorrelationId.ToString();
                var sourceContext = logMessage.SourceContext.ToString();

                // Create enriched logger with context
                var contextLogger = _serilogLogger
                    .ForContext("CorrelationId", correlationId)
                    .ForContext("SourceContext", sourceContext)
                    .ForContext("Channel", logMessage.Channel.ToString())
                    .ForContext("ThreadId", logMessage.ThreadId);

                // Add structured properties if present
                if (logMessage.HasProperties && logMessage.Properties != null)
                {
                    foreach (var kvp in logMessage.Properties)
                    {
                        contextLogger = contextLogger.ForContext(kvp.Key, kvp.Value);
                    }
                }

                // Write the log message
                if (logMessage.HasException && logMessage.Exception != null)
                {
                    contextLogger.Write(serilogLevel, logMessage.Exception, messageTemplate);
                }
                else
                {
                    contextLogger.Write(serilogLevel, messageTemplate);
                }
            }
        }

        /// <summary>
        /// Asynchronously writes a log message to Serilog.
        /// </summary>
        /// <param name="logMessage">The log message to write</param>
        private async Task WriteAsync(LogMessage logMessage)
        {
            if (_asyncWriteSemaphore == null || _disposed) return;

            await _asyncWriteSemaphore.WaitAsync();
            try
            {
                await Task.Run(() => WriteInternal(logMessage));
            }
            finally
            {
                _asyncWriteSemaphore.Release();
            }
        }

        /// <summary>
        /// Internal method to write multiple log messages to Serilog.
        /// </summary>
        /// <param name="logMessages">The log messages to write</param>
        private void WriteBatchInternal(IReadOnlyList<LogMessage> logMessages)
        {
            foreach (var message in logMessages)
            {
                if (ShouldProcessMessage(message))
                {
                    try
                    {
                        WriteInternal(message);
                        Interlocked.Increment(ref _messagesWritten);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _errorsEncountered);
                        Interlocked.Increment(ref _messagesDropped);
                        _lastError = ex;
                    }
                }
                else
                {
                    Interlocked.Increment(ref _messagesDropped);
                }
            }

            _lastWriteTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Asynchronously writes multiple log messages to Serilog.
        /// </summary>
        /// <param name="logMessages">The log messages to write</param>
        private async Task WriteBatchAsync(IReadOnlyList<LogMessage> logMessages)
        {
            if (_asyncWriteSemaphore == null || _disposed) return;

            await _asyncWriteSemaphore.WaitAsync();
            try
            {
                await Task.Run(() => WriteBatchInternal(logMessages));
            }
            finally
            {
                _asyncWriteSemaphore.Release();
            }
        }

        /// <summary>
        /// Periodic health check timer callback.
        /// </summary>
        /// <param name="state">Timer state</param>
        private void PerformPeriodicHealthCheck(object state)
        {
            try
            {
                PerformHealthCheck();
            }
            catch (Exception ex)
            {
                _lastError = ex;
                _isHealthy = false;
            }
        }

        /// <summary>
        /// Gets a configuration property with a default value.
        /// </summary>
        /// <typeparam name="T">The type of the property</typeparam>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">The default value if not found</param>
        /// <returns>The property value or default</returns>
        private T GetConfigProperty<T>(string key, T defaultValue)
        {
            if (_config.Properties != null && _config.Properties.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Registers performance metric alerts with the profiler service.
        /// </summary>
        private void RegisterPerformanceAlerts()
        {
            if (_config.EnablePerformanceMetrics)
            {
                _profiler.RegisterMetricAlert(WriteTag, _config.FrameBudgetThresholdMs);
                _profiler.RegisterMetricAlert(BatchWriteTag, _config.FrameBudgetThresholdMs * 10); // Batch operations get 10x threshold
                _profiler.RegisterMetricAlert(HealthCheckTag, _config.FrameBudgetThresholdMs * 2); // Health checks get 2x threshold
            }
        }

        /// <summary>
        /// Checks for performance threshold violations and triggers alerts if needed.
        /// </summary>
        /// <param name="profilerSession">The profiler session to check</param>
        private void CheckPerformanceThresholds(IProfilerSession profilerSession)
        {
            if (!_config.EnablePerformanceMetrics) return;

            var metrics = profilerSession.GetMetrics();
            if (metrics.TryGetValue("ElapsedMilliseconds", out var elapsedMs))
            {
                var elapsed = Convert.ToDouble(elapsedMs);
                if (elapsed > _config.FrameBudgetThresholdMs)
                {
                    TriggerPerformanceAlert($"SerilogTarget write exceeded frame budget: {elapsed:F2}ms > {_config.FrameBudgetThresholdMs:F2}ms");
                }
            }
        }

        /// <summary>
        /// Triggers an error alert through the alerting system.
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="message">Additional context message</param>
        private void TriggerErrorAlert(Exception exception, string message)
        {
            try
            {
                _alertService.RaiseAlert(
                    message: $"{message}: {exception.Message}",
                    severity: AlertSeverity.Critical,
                    source: "SerilogTarget",
                    tag: "LoggingFailure"
                );
            }
            catch
            {
                // Ignore alerting failures to prevent infinite loops
            }
        }

        /// <summary>
        /// Triggers a performance alert through the alerting system.
        /// </summary>
        /// <param name="message">The performance alert message</param>
        private void TriggerPerformanceAlert(string message)
        {
            try
            {
                _alertService.RaiseAlert(
                    message: message,
                    severity: AlertSeverity.Warning,
                    source: "SerilogTarget",
                    tag: "PerformanceIssue"
                );
            }
            catch
            {
                // Ignore alerting failures to prevent infinite loops
            }
        }

        /// <summary>
        /// Disposes the Serilog target and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                // Stop health check timer
                _healthCheckTimer?.Dispose();

                // Flush and dispose Serilog logger
                lock (_loggerLock)
                {
                    if (_serilogLogger is IDisposable disposableLogger)
                    {
                        disposableLogger.Dispose();
                    }
                    _serilogLogger = null;
                }

                // Dispose async semaphore
                _asyncWriteSemaphore?.Dispose();
            }
            catch (Exception ex)
            {
                // Best effort disposal - log to system debug
                System.Diagnostics.Debug.WriteLine($"SerilogTarget disposal error: {ex.Message}");
            }
        }
    }
}