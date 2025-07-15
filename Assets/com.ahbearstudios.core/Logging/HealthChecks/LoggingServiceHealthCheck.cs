using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Profiling.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.HealthChecks
{
    /// <summary>
    /// Health check implementation for the logging service.
    /// Monitors logging system performance, target health, and resource utilization.
    /// Follows AhBearStudios Core Architecture health monitoring requirements.
    /// </summary>
    public sealed class LoggingServiceHealthCheck : IHealthCheck
    {
        private readonly ILoggingService _loggingService;
        private readonly FixedString64Bytes _healthCheckName = "Logging";
        private readonly object _lock = new object();
        private DateTime _lastCheckTime = DateTime.MinValue;
        private HealthCheckResult _cachedResult;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30);
        private HealthCheckConfiguration _configuration;
        
        // Static profiler tags for performance monitoring
        private static readonly ProfilerTag HealthCheckTag = ProfilerTag.CreateMethodTag("LoggingServiceHealthCheck", "CheckHealthAsync");
        private static readonly ProfilerTag TestLogTag = ProfilerTag.CreateMethodTag("LoggingServiceHealthCheck", "PerformTestLogOperation");
        private static readonly ProfilerTag PerformanceCheckTag = ProfilerTag.CreateMethodTag("LoggingServiceHealthCheck", "CheckPerformanceMetrics");

        /// <summary>
        /// Gets the name of this health check.
        /// </summary>
        public FixedString64Bytes Name => _healthCheckName;

        /// <summary>
        /// Gets the description of this health check.
        /// </summary>
        public string Description => "Monitors logging system performance, target health, and resource utilization";

        /// <summary>
        /// Gets the category of this health check.
        /// </summary>
        public HealthCheckCategory Category => HealthCheckCategory.System;

        /// <summary>
        /// Gets the timeout for this health check.
        /// </summary>
        public TimeSpan Timeout => _configuration?.Timeout ?? TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the configuration for this health check.
        /// </summary>
        public HealthCheckConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the dependencies for this health check (logging service has no dependencies).
        /// </summary>
        public IEnumerable<FixedString64Bytes> Dependencies => Enumerable.Empty<FixedString64Bytes>();

        /// <summary>
        /// Initializes a new instance of the LoggingServiceHealthCheck.
        /// </summary>
        /// <param name="loggingService">The logging service to monitor</param>
        /// <exception cref="ArgumentNullException">Thrown when loggingService is null</exception>
        public LoggingServiceHealthCheck(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _configuration = CreateDefaultConfiguration();
            _cachedResult = HealthCheckResult.Unknown(Name.ToString(), "Health check not yet executed");
        }

        /// <summary>
        /// Configures the health check with new settings.
        /// </summary>
        /// <param name="configuration">The configuration to apply</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        public void Configure(HealthCheckConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            configuration.ValidateAndThrow();
            
            lock (_lock)
            {
                _configuration = configuration;
                // Invalidate cache when configuration changes
                _lastCheckTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets comprehensive metadata about this health check.
        /// </summary>
        /// <returns>Dictionary containing metadata about the health check</returns>
        public Dictionary<string, object> GetMetadata()
        {
            var serviceConfig = _loggingService.Configuration;
            
            return new Dictionary<string, object>
            {
                ["Name"] = Name.ToString(),
                ["Description"] = Description,
                ["Category"] = Category.ToString(),
                ["Timeout"] = Timeout.TotalSeconds,
                ["CacheTimeout"] = _cacheTimeout.TotalSeconds,
                ["Dependencies"] = Dependencies.Select(d => d.ToString()).ToArray(),
                ["Version"] = "1.0.0",
                ["ConfigurationVersion"] = _configuration?.Version ?? "Unknown",
                ["LastExecutionTime"] = _lastCheckTime != DateTime.MinValue ? _lastCheckTime : null,
                ["IsCacheValid"] = IsCacheValid(),
                ["LoggingServiceEnabled"] = _loggingService.IsEnabled,
                ["LoggingServiceTargetCount"] = _loggingService.GetRegisteredTargets().Count,
                ["LoggingServiceHighPerformanceMode"] = serviceConfig?.HighPerformanceMode ?? false,
                ["LoggingServiceBatchingEnabled"] = serviceConfig?.BatchingEnabled ?? false,
                ["SupportsRuntimeConfiguration"] = true,
                ["ThreadSafe"] = true,
                ["PerformanceOptimized"] = true,
                ["UnityCompatible"] = true,
                ["BurstCompatible"] = false // Due to managed logging service dependency
            };
        }

        /// <summary>
        /// Creates the default configuration for the logging service health check.
        /// </summary>
        /// <returns>Default health check configuration</returns>
        private HealthCheckConfiguration CreateDefaultConfiguration()
        {
            return HealthCheckConfiguration.ForCriticalSystem(
                _healthCheckName,
                "Logging Service Health Check",
                "Monitors logging system performance, target health, and resource utilization");
        }

        /// <summary>
        /// Performs a comprehensive health check of the logging system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the health check operation</param>
        /// <returns>The health check result with detailed status information</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the logging service has been disposed</exception>
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            // Check cache first to avoid excessive health checking
            lock (_lock)
            {
                if (DateTime.UtcNow - _lastCheckTime < _cacheTimeout)
                {
                    return _cachedResult;
                }
            }

            // Validate that the logging service is still available
            if (_loggingService == null)
            {
                var duration = DateTime.UtcNow - startTime;
                var errorResult = HealthCheckResult.Unhealthy(Name.ToString(), "Logging service is null", duration);
                lock (_lock)
                {
                    _cachedResult = errorResult;
                    _lastCheckTime = DateTime.UtcNow;
                }
                return errorResult;
            }

            // Create timeout token that combines user token with configuration timeout
            using var timeoutConfigCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutConfigCts.CancelAfter(Timeout);

            try
            {
                var healthData = new Dictionary<string, object>();
                var issues = new List<string>();
                var warnings = new List<string>();

                // 1. Check if logging service is enabled and operational
                if (!_loggingService.IsEnabled)
                {
                    issues.Add("Logging service is disabled");
                }

                // 2. Check registered targets
                var targets = _loggingService.GetRegisteredTargets();
                healthData["TargetCount"] = targets.Count;

                if (targets.Count == 0)
                {
                    warnings.Add("No log targets registered");
                }

                // 3. Check individual target health
                var healthyTargets = 0;
                var unhealthyTargets = new List<string>();

                foreach (var target in targets)
                {
                    try
                    {
                        if (target.PerformHealthCheck())
                        {
                            healthyTargets++;
                        }
                        else
                        {
                            unhealthyTargets.Add(target.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        unhealthyTargets.Add($"{target.Name} (Exception: {ex.Message})");
                    }
                }

                healthData["HealthyTargets"] = healthyTargets;
                healthData["UnhealthyTargets"] = unhealthyTargets.Count;

                if (unhealthyTargets.Count > 0)
                {
                    healthData["UnhealthyTargetNames"] = unhealthyTargets;
                    if (unhealthyTargets.Count == targets.Count)
                    {
                        issues.Add("All log targets are unhealthy");
                    }
                    else
                    {
                        warnings.Add($"{unhealthyTargets.Count} log targets are unhealthy");
                    }
                }

                // 4. Check logging service configuration
                var config = _loggingService.Configuration;
                if (config != null)
                {
                    healthData["HighPerformanceMode"] = config.HighPerformanceMode;
                    healthData["BatchingEnabled"] = config.BatchingEnabled;
                    healthData["BurstCompatibility"] = config.BurstCompatibility;
                    healthData["StructuredLogging"] = config.StructuredLogging;
                    healthData["GlobalMinimumLevel"] = config.GlobalMinimumLevel.ToString();
                    
                    // Validate configuration
                    var configErrors = config.Validate();
                    if (configErrors.Count > 0)
                    {
                        issues.Add($"Configuration validation failed: {string.Join(", ", (IEnumerable<string>)configErrors)}");
                        healthData["ConfigurationErrors"] = configErrors;
                    }
                }

                // 5. Perform a test log operation with timeout protection
                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout for test operation
                    await PerformTestLogOperation(healthData, warnings, timeoutCts.Token);
                }

                // 6. Check memory usage and performance metrics
                CheckPerformanceMetrics(healthData, warnings);

                // 7. Determine overall health status
                var duration = DateTime.UtcNow - startTime;
                HealthCheckResult result;
                if (issues.Count > 0)
                {
                    var issueDescription = string.Join("; ", issues);
                    result = HealthCheckResult.Unhealthy(Name.ToString(), $"Logging system issues: {issueDescription}", duration, healthData);
                }
                else if (warnings.Count > 0)
                {
                    var warningDescription = string.Join("; ", warnings);
                    result = HealthCheckResult.Degraded(Name.ToString(), $"Logging system warnings: {warningDescription}", duration, healthData);
                }
                else
                {
                    result = HealthCheckResult.Healthy(Name.ToString(), "Logging system operating normally", duration, healthData);
                }

                // Cache the result
                lock (_lock)
                {
                    _cachedResult = result;
                    _lastCheckTime = DateTime.UtcNow;
                }

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || timeoutConfigCts.Token.IsCancellationRequested)
            {
                var duration = DateTime.UtcNow - startTime;
                var message = timeoutConfigCts.Token.IsCancellationRequested ? 
                    $"Health check timed out after {duration.TotalSeconds:F1} seconds" : 
                    "Health check was cancelled";
                var cancelledResult = HealthCheckResult.Unhealthy(Name.ToString(), message, duration);
                lock (_lock)
                {
                    _cachedResult = cancelledResult;
                    _lastCheckTime = DateTime.UtcNow;
                }
                return cancelledResult;
            }
            catch (Exception ex)
            {
                var errorData = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["ExceptionType"] = ex.GetType().Name,
                    ["StackTrace"] = ex.StackTrace,
                    ["CheckTime"] = DateTime.UtcNow,
                    ["InnerException"] = ex.InnerException?.Message
                };

                var duration = DateTime.UtcNow - startTime;
                var result = HealthCheckResult.Unhealthy(Name.ToString(), $"Logging health check failed: {ex.Message}", duration, errorData, ex);
                
                lock (_lock)
                {
                    _cachedResult = result;
                    _lastCheckTime = DateTime.UtcNow;
                }

                return result;
            }
        }

        /// <summary>
        /// Performs a test log operation to verify logging functionality.
        /// </summary>
        /// <param name="healthData">Dictionary to add health data to</param>
        /// <param name="warnings">List to add warnings to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task PerformTestLogOperation(
            Dictionary<string, object> healthData, 
            List<string> warnings, 
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var testStartTime = DateTime.UtcNow;
                
                // Perform a test log operation
                var testMessage = $"Health check test - {DateTime.UtcNow:HH:mm:ss.fff}";
                var testCorrelationId = Guid.NewGuid().ToString("N")[..8];
                
                _loggingService.LogDebug(testMessage, testCorrelationId, "HealthCheck");
                
                // Force flush to ensure the message is processed
                _loggingService.Flush();
                
                var testDuration = DateTime.UtcNow - testStartTime;
                healthData["TestLogDuration"] = testDuration.TotalMilliseconds;
                
                // Check if test operation took too long (more aggressive threshold for production)
                if (testDuration.TotalMilliseconds > 500) // 500ms threshold for production readiness
                {
                    warnings.Add($"Test log operation slow: {testDuration.TotalMilliseconds:F1}ms");
                }
                
                healthData["TestLogSuccess"] = true;
                healthData["LastTestTime"] = DateTime.UtcNow;
                
                // Simulate async operation to test cancellation handling
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                healthData["TestLogSuccess"] = false;
                healthData["TestLogError"] = "Test operation was cancelled";
                warnings.Add("Test log operation was cancelled");
            }
            catch (Exception ex)
            {
                warnings.Add($"Test log operation failed: {ex.Message}");
                healthData["TestLogSuccess"] = false;
                healthData["TestLogError"] = ex.Message;
                healthData["TestLogExceptionType"] = ex.GetType().Name;
            }
        }

        /// <summary>
        /// Checks performance metrics and resource utilization.
        /// </summary>
        /// <param name="healthData">Dictionary to add health data to</param>
        /// <param name="warnings">List to add warnings to</param>
        private void CheckPerformanceMetrics(Dictionary<string, object> healthData, List<string> warnings)
        {
            try
            {
                // Check current memory usage (avoid forcing GC during health check)
                var memoryUsage = GC.GetTotalMemory(false);
                healthData["MemoryUsageBytes"] = memoryUsage;
                healthData["MemoryPressureMB"] = memoryUsage / (1024.0 * 1024.0); // MB
                
                // Check GC pressure with more production-appropriate thresholds
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                
                healthData["GC_Gen0Collections"] = gen0Collections;
                healthData["GC_Gen1Collections"] = gen1Collections;
                healthData["GC_Gen2Collections"] = gen2Collections;
                
                // More conservative thresholds for production
                if (gen2Collections > 5) // Lower threshold for Gen2 collections
                {
                    warnings.Add($"Gen2 GC pressure detected: {gen2Collections} collections");
                }
                
                if (gen1Collections > 50) // Monitor Gen1 collections
                {
                    warnings.Add($"High Gen1 GC pressure: {gen1Collections} collections");
                }
                
                // Check thread count with more conservative threshold
                try
                {
                    var threadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
                    healthData["ThreadCount"] = threadCount;
                    
                    if (threadCount > 50) // More conservative threshold
                    {
                        warnings.Add($"High thread count detected: {threadCount}");
                    }
                }
                catch (System.PlatformNotSupportedException)
                {
                    // Thread count not available on this platform
                    healthData["ThreadCount"] = "N/A";
                }
                
                // Check logging service configuration
                var serviceConfig = _loggingService.Configuration;
                if (serviceConfig != null)
                {
                    healthData["HighPerformanceModeActive"] = serviceConfig.HighPerformanceMode;
                    healthData["BatchingActive"] = serviceConfig.BatchingEnabled;
                    
                    // Memory pressure warnings for high-performance mode
                    if (serviceConfig.HighPerformanceMode && memoryUsage > 100 * 1024 * 1024) // 100MB threshold
                    {
                        warnings.Add($"High memory usage in high-performance mode: {memoryUsage / (1024.0 * 1024.0):F1}MB");
                    }
                }
                
                // Check for memory pressure based on available memory
                var totalMemory = GC.GetTotalMemory(false);
                if (totalMemory > 500 * 1024 * 1024) // 500MB threshold
                {
                    warnings.Add($"High total memory usage: {totalMemory / (1024.0 * 1024.0):F1}MB");
                }
                
                healthData["PerformanceCheckSuccess"] = true;
            }
            catch (Exception ex)
            {
                warnings.Add($"Performance metrics check failed: {ex.Message}");
                healthData["PerformanceCheckSuccess"] = false;
                healthData["PerformanceCheckError"] = ex.Message;
                healthData["PerformanceCheckExceptionType"] = ex.GetType().Name;
            }
        }

        /// <summary>
        /// Clears the cached health check result to force a fresh check.
        /// </summary>
        public void InvalidateCache()
        {
            lock (_lock)
            {
                _lastCheckTime = DateTime.MinValue;
                _cachedResult = HealthCheckResult.Unknown(Name.ToString(), "Cache invalidated");
            }
        }

        /// <summary>
        /// Gets the last cached health check result without performing a new check.
        /// </summary>
        /// <returns>The last cached health check result</returns>
        public HealthCheckResult GetCachedResult()
        {
            lock (_lock)
            {
                return _cachedResult;
            }
        }

        /// <summary>
        /// Determines if the cached result is still valid.
        /// </summary>
        /// <returns>True if the cached result is still valid, false otherwise</returns>
        public bool IsCacheValid()
        {
            lock (_lock)
            {
                return DateTime.UtcNow - _lastCheckTime < _cacheTimeout;
            }
        }
    }
}