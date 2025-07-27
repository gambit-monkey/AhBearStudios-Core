using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Profiling.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Unity.Profiling;
using Unity.Collections;
using UnityEngine;
using ILogger = Serilog.ILogger;
using Logger = Serilog.Core.Logger;
using Cysharp.Threading.Tasks;

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
        private static readonly ProfilerTag WriteTag = new("SerilogTarget.Write");
        private static readonly ProfilerTag BatchWriteTag = new("SerilogTarget.WriteBatch");
        private static readonly ProfilerTag HealthCheckTag = new("SerilogTarget.HealthCheck");
        
        private ILogger _serilogLogger;
        private volatile bool _disposed = false;
        private long _messagesWritten = 0;
        private long _messagesDropped = 0;
        private long _errorsEncountered = 0;
        private DateTime _lastWriteTime = DateTime.MinValue;
        private DateTime _lastHealthCheck = DateTime.MinValue;
        private volatile bool _isHealthy = true;
        private Exception _lastError = null;
        
        // Unity game development optimizations
        private int _messagesThisFrame = 0;
        private DateTime _lastFrameReset = DateTime.MinValue;
        
        // Performance monitoring thresholds (Unity game development optimized)
        private const double ERROR_RATE_THRESHOLD = 0.1; // 10% error rate
        private const double FRAME_BUDGET_THRESHOLD_MS = 0.5; // 0.5ms per write operation (60 FPS = 16.67ms frame)
        private const int ALERT_SUPPRESSION_INTERVAL_MINUTES = 5;
        private const int MAX_MESSAGES_PER_FRAME = 10; // Limit messages per frame to prevent frame drops

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

            // Unity game development optimization: Frame budget protection
            if (ShouldLimitMessagesPerFrame())
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
                        // Use UniTask for Unity-optimized async operations
                        WriteAsync(logMessage).Forget();
                    }
                    else
                    {
                        WriteInternal(logMessage);
                    }

                    Interlocked.Increment(ref _messagesWritten);
                    _lastWriteTime = DateTime.UtcNow;
                    
                    // Track messages per frame for Unity optimization
                    Interlocked.Increment(ref _messagesThisFrame);

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
        /// Unity-optimized to respect frame budget constraints.
        /// </summary>
        /// <param name="logMessages">The log messages to write</param>
        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            if (logMessages == null || logMessages.Count == 0 || _disposed)
            {
                return;
            }

            // Unity frame budget protection for batch operations
            var processedMessages = 0;
            var messagesToProcess = new List<LogMessage>();
            
            foreach (var message in logMessages)
            {
                if (processedMessages >= MAX_MESSAGES_PER_FRAME)
                {
                    // Defer remaining messages to prevent frame drops
                    break;
                }
                
                if (ShouldProcessMessage(message))
                {
                    messagesToProcess.Add(message);
                    processedMessages++;
                }
            }

            if (messagesToProcess.Count > 0)
            {
                if (UseAsyncWrite)
                {
                    // Use UniTask for Unity-optimized async operations
                    WriteBatchAsync(messagesToProcess).Forget();
                }
                else
                {
                    WriteBatchInternal(messagesToProcess);
                }
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
                    // Use Log.CloseAndFlush() for proper flushing without recreating logger
                    if (_serilogLogger != null)
                    {
                        // Try to flush using CloseAndFlush if available
                        try
                        {
                            Log.CloseAndFlush();
                        }
                        catch
                        {
                            // Fallback: If CloseAndFlush fails, dispose and recreate
                            if (_serilogLogger is IDisposable disposableLogger)
                            {
                                disposableLogger.Dispose();
                            }
                            InitializeSerilogLogger();
                        }
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
                        var errorRateThreshold = _config?.ErrorRateThreshold ?? ERROR_RATE_THRESHOLD;
                        if (errorRate > errorRateThreshold)
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
                        var testMessage = LogMessage.Create(
                            level: LogLevel.Debug,
                            channel: "HealthCheck",
                            message: $"Serilog health check - {DateTime.UtcNow:HH:mm:ss}",
                            correlationId: Guid.NewGuid().ToString("N")[..8],
                            sourceContext: "SerilogTarget");
                        
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
                    .Enrich.WithProperty("ThreadId", System.Threading.Thread.CurrentThread.ManagedThreadId)
                    .Enrich.WithProperty("MachineName", System.Environment.MachineName);
                
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
            // Console sink - Unity development optimized
            var enableConsole = GetConfigProperty("EnableConsole", false);
            
            // Enable console logging in Unity Editor and Debug builds by default
#if UNITY_EDITOR || DEBUG
            enableConsole = GetConfigProperty("EnableConsole", true);
#endif
            
            if (enableConsole)
            {
                var consoleTemplate = GetConfigProperty("ConsoleTemplate", 
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                config.WriteTo.Console(outputTemplate: consoleTemplate);
            }

            // File sink - Unity game optimized with platform-specific settings
            var enableFileLogging = GetConfigProperty("EnableFileLogging", true) && SupportsFileLogging();
            if (enableFileLogging)
            {
                var filePath = GetUnityLogFilePath();
                var rollOnFileSizeLimit = GetConfigProperty("RollOnFileSizeLimit", true);
                
                // Platform-specific file size limits
                var defaultFileSizeLimit = 10 * 1024 * 1024; // 10MB default
#if UNITY_ANDROID || UNITY_IOS
                defaultFileSizeLimit = 5 * 1024 * 1024; // 5MB for mobile
#elif UNITY_WEBGL
                defaultFileSizeLimit = 1 * 1024 * 1024; // 1MB for WebGL (though file logging is disabled)
#endif
                
                var fileSizeLimitBytes = GetConfigProperty("FileSizeLimitBytes", defaultFileSizeLimit);
                var retainedFileCountLimit = GetConfigProperty("RetainedFileCountLimit", 5); // Keep 5 files for game debugging
                var shared = GetConfigProperty("Shared", false);

                try
                {
                    // Ensure the log directory exists
                    var logDirectory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    config.WriteTo.File(
                        path: filePath,
                        rollOnFileSizeLimit: rollOnFileSizeLimit,
                        fileSizeLimitBytes: fileSizeLimitBytes,
                        retainedFileCountLimit: retainedFileCountLimit,
                        shared: shared,
                        formatter: new JsonFormatter());
                }
                catch (Exception ex)
                {
                    // Fall back to console logging if file logging fails
                    System.Diagnostics.Debug.WriteLine($"Failed to configure file logging: {ex.Message}");
                    config.WriteTo.Console();
                }
            }
            else if (!SupportsFileLogging())
            {
                // Platform doesn't support file logging - ensure console logging is enabled
                System.Diagnostics.Debug.WriteLine("File logging not supported on this platform, using console logging");
                config.WriteTo.Console();
            }

            // Debug sink - Unity Editor and Debug builds
            var enableDebug = GetConfigProperty("EnableDebug", false);
            
            // Enable debug logging in Unity Editor by default
#if UNITY_EDITOR
            enableDebug = GetConfigProperty("EnableDebug", true);
#endif
            
            if (enableDebug)
            {
                config.WriteTo.Console();
            }

            // Custom sinks can be added here based on additional configuration
            ConfigureCustomSinks(config);
        }

        /// <summary>
        /// Configures Unity-specific custom Serilog sinks.
        /// </summary>
        /// <param name="config">The Serilog configuration</param>
        private void ConfigureCustomSinks(LoggerConfiguration config)
        {
            // Unity-specific sinks can be added here
            // Examples: Custom game analytics sink, Unity Cloud Diagnostics integration
            
            // Note: Email and Elasticsearch sinks are not recommended for Unity games
            // due to performance, security, and platform restrictions
            
            // Unity-specific optimizations for different platforms
#if UNITY_ANDROID || UNITY_IOS
            // Mobile-specific optimizations
            // Reduce buffer sizes and increase flush frequency for mobile devices
            var mobileOptimized = GetConfigProperty("MobileOptimized", true);
            if (mobileOptimized)
            {
                // Mobile devices have limited storage and memory
                // These optimizations are applied through configuration
            }
#endif

#if UNITY_STANDALONE
            // Desktop-specific optimizations
            // Can handle larger buffers and more frequent writes
#endif

#if UNITY_WEBGL
            // WebGL-specific limitations
            // File system access is limited in WebGL builds
            System.Diagnostics.Debug.WriteLine("File logging is limited in WebGL builds");
#endif
            
            // Future Unity-specific sinks could include:
            // - Unity Analytics integration
            // - Custom REST API sink for critical errors
            // - Platform-specific logging (Console, Mobile, etc.)
            // - Unity Cloud Diagnostics integration
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
        private async UniTaskVoid WriteAsync(LogMessage logMessage)
        {
            if (_asyncWriteSemaphore == null || _disposed) return;

            try
            {
                // Use UniTask timeout for better Unity integration
                var timeoutToken = new CancellationTokenSource();
                timeoutToken.CancelAfterSlim(TimeSpan.FromSeconds(10));
                
                await _asyncWriteSemaphore.WaitAsync(timeoutToken.Token);
                
                try
                {
                    // Use UniTask.RunOnThreadPool for Unity-optimized threading
                    await UniTask.RunOnThreadPool(() => WriteInternal(logMessage), cancellationToken: timeoutToken.Token);
                }
                finally
                {
                    _asyncWriteSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Handle timeout scenarios
                Interlocked.Increment(ref _errorsEncountered);
                Interlocked.Increment(ref _messagesDropped);
                _lastError = new TimeoutException("Async write operation timed out");
                _isHealthy = false;
                TriggerErrorAlert(_lastError, "Async write operation timed out");
            }
            catch (Exception ex)
            {
                // Handle async write errors
                Interlocked.Increment(ref _errorsEncountered);
                Interlocked.Increment(ref _messagesDropped);
                _lastError = ex;
                _isHealthy = false;
                TriggerErrorAlert(ex, "Async write operation failed");
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
        private async UniTaskVoid WriteBatchAsync(IReadOnlyList<LogMessage> logMessages)
        {
            if (_asyncWriteSemaphore == null || _disposed) return;

            try
            {
                // Use UniTask timeout for better Unity integration (longer for batch operations)
                var timeoutToken = new CancellationTokenSource();
                timeoutToken.CancelAfterSlim(TimeSpan.FromSeconds(30));
                
                await _asyncWriteSemaphore.WaitAsync(timeoutToken.Token);
                
                try
                {
                    // Use UniTask.RunOnThreadPool for Unity-optimized threading
                    await UniTask.RunOnThreadPool(() => WriteBatchInternal(logMessages), cancellationToken: timeoutToken.Token);
                }
                finally
                {
                    _asyncWriteSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Handle timeout scenarios
                Interlocked.Increment(ref _errorsEncountered);
                _lastError = new TimeoutException("Async batch write operation timed out");
                _isHealthy = false;
                TriggerErrorAlert(_lastError, "Async batch write operation timed out");
            }
            catch (Exception ex)
            {
                // Handle async batch write errors
                Interlocked.Increment(ref _errorsEncountered);
                _lastError = ex;
                _isHealthy = false;
                TriggerErrorAlert(ex, "Async batch write operation failed");
            }
        }

        // Note: HandleAsyncWriteTask and HandleAsyncBatchTask methods are no longer needed
        // with UniTask's .Forget() extension which handles exceptions internally

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
            if (_config?.EnablePerformanceMetrics == true)
            {
                // Register performance threshold alerts with the profiler service
                var frameBudgetThreshold = _config.FrameBudgetThresholdMs;
                _profiler.ThresholdExceeded += OnPerformanceThresholdExceeded;
                
                // Record initial metrics
                _profiler.RecordMetric("SerilogTarget.MessagesWritten", _messagesWritten, "count");
                _profiler.RecordMetric("SerilogTarget.MessagesDropped", _messagesDropped, "count");
                _profiler.RecordMetric("SerilogTarget.ErrorsEncountered", _errorsEncountered, "count");
            }
        }

        /// <summary>
        /// Checks for performance threshold violations and triggers alerts if needed.
        /// </summary>
        /// <param name="profilerSession">The profiler session to check</param>
        private void CheckPerformanceThresholds(IDisposable profilerSession)
        {
            if (_config?.EnablePerformanceMetrics != true) return;

            try
            {
                // Record current performance metrics
                _profiler.RecordMetric("SerilogTarget.MessagesWritten", _messagesWritten, "count");
                _profiler.RecordMetric("SerilogTarget.MessagesDropped", _messagesDropped, "count");
                _profiler.RecordMetric("SerilogTarget.ErrorsEncountered", _errorsEncountered, "count");
                
                // Calculate and record error rate
                var totalMessages = _messagesWritten + _messagesDropped;
                if (totalMessages > 0)
                {
                    var errorRate = (double)_errorsEncountered / totalMessages;
                    _profiler.RecordMetric("SerilogTarget.ErrorRate", errorRate, "percentage");
                }
                
                // Record health status
                _profiler.RecordMetric("SerilogTarget.IsHealthy", _isHealthy ? 1.0 : 0.0, "boolean");
            }
            catch (Exception ex)
            {
                // Don't let profiling errors affect logging functionality
                System.Diagnostics.Debug.WriteLine($"SerilogTarget performance monitoring failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles performance threshold exceeded events from the profiler service.
        /// </summary>
        /// <param name="tag">The profiler tag that exceeded the threshold</param>
        /// <param name="value">The measured value</param>
        /// <param name="unit">The unit of measurement</param>
        private void OnPerformanceThresholdExceeded(ProfilerTag tag, double value, string unit)
        {
            try
            {
                var message = $"SerilogTarget performance threshold exceeded: {tag.Name} = {value:F2} {unit}";
                TriggerPerformanceAlert(message);
            }
            catch
            {
                // Ignore alerting failures to prevent infinite loops
            }
        }

        /// <summary>
        /// Unity game development optimization: Limits messages per frame to prevent frame drops.
        /// </summary>
        /// <returns>True if messages should be limited this frame</returns>
        private bool ShouldLimitMessagesPerFrame()
        {
            var now = DateTime.UtcNow;
            
            // Reset frame counter every 16.67ms (60 FPS) or 33.33ms (30 FPS)
            if (now - _lastFrameReset > TimeSpan.FromMilliseconds(16.67))
            {
                _messagesThisFrame = 0;
                _lastFrameReset = now;
                return false;
            }
            
            // Limit messages per frame to maintain performance
            return _messagesThisFrame >= MAX_MESSAGES_PER_FRAME;
        }

        /// <summary>
        /// Gets the Unity-appropriate log file path using persistent data path.
        /// </summary>
        /// <returns>The full path to the log file</returns>
        private string GetUnityLogFilePath()
        {
            // Use custom path if specified, otherwise use Unity persistent data path
            var customPath = GetConfigProperty<string>("FilePath", null);
            if (!string.IsNullOrEmpty(customPath))
            {
                return customPath;
            }

            // Unity persistent data path is platform-appropriate and writable
            var persistentDataPath = Application.persistentDataPath;
            var logDirectory = Path.Combine(persistentDataPath, "Logs");
            
            // Include timestamp in filename for easier debugging
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            var filename = $"game-{timestamp}.log";
            
            return Path.Combine(logDirectory, filename);
        }

        /// <summary>
        /// Unity-specific performance optimization for platform-appropriate logging levels.
        /// </summary>
        /// <returns>The recommended minimum log level for the current platform</returns>
        private LogLevel GetUnityOptimizedLogLevel()
        {
            // Platform-specific log level optimizations
#if UNITY_EDITOR
            // Development environment - verbose logging
            return LogLevel.Debug;
#elif DEVELOPMENT_BUILD
            // Development builds - detailed logging for debugging
            return LogLevel.Debug;
#elif UNITY_ANDROID || UNITY_IOS
            // Mobile platforms - reduce logging to preserve performance
            return LogLevel.Warning;
#elif UNITY_WEBGL
            // WebGL - minimal logging due to browser constraints
            return LogLevel.Error;
#else
            // Production builds - minimal logging
            return LogLevel.Warning;
#endif
        }

        /// <summary>
        /// Unity-specific check for platform logging capabilities.
        /// </summary>
        /// <returns>True if file logging is supported on the current platform</returns>
        private bool SupportsFileLogging()
        {
#if UNITY_WEBGL
            // WebGL doesn't support file system access
            return false;
#elif UNITY_ANDROID || UNITY_IOS
            // Mobile platforms support file logging but with limitations
            return true;
#else
            // Desktop and other platforms support full file logging
            return true;
#endif
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
                // Unregister from profiler events first
                if (_profiler != null && _config?.EnablePerformanceMetrics == true)
                {
                    _profiler.ThresholdExceeded -= OnPerformanceThresholdExceeded;
                }

                // Stop health check timer
                _healthCheckTimer?.Dispose();

                // Flush and dispose Serilog logger with proper resource management
                lock (_loggerLock)
                {
                    if (_serilogLogger != null)
                    {
                        try
                        {
                            // Attempt to flush before disposal
                            Log.CloseAndFlush();
                        }
                        catch
                        {
                            // If flushing fails, still try to dispose
                        }
                        
                        if (_serilogLogger is IDisposable disposableLogger)
                        {
                            disposableLogger.Dispose();
                        }
                        _serilogLogger = null;
                    }
                }

                // Dispose async semaphore
                _asyncWriteSemaphore?.Dispose();
                
                // Dispose Unity ProfilerMarkers (they're structs, no disposal needed)
                // But ensure we're not holding references
                
            }
            catch (Exception ex)
            {
                // Best effort disposal - log to system debug
                System.Diagnostics.Debug.WriteLine($"SerilogTarget disposal error: {ex.Message}");
            }
        }
    }
}