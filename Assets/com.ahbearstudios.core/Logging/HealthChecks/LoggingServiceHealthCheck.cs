using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
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
        private HealthCheckResult _cachedResult = HealthCheckResult.Unknown;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the name of this health check.
        /// </summary>
        public FixedString64Bytes Name => _healthCheckName;

        /// <summary>
        /// Initializes a new instance of the LoggingServiceHealthCheck.
        /// </summary>
        /// <param name="loggingService">The logging service to monitor</param>
        /// <exception cref="ArgumentNullException">Thrown when loggingService is null</exception>
        public LoggingServiceHealthCheck(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Performs a comprehensive health check of the logging system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the health check operation</param>
        /// <returns>The health check result with detailed status information</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            // Check cache first to avoid excessive health checking
            lock (_lock)
            {
                if (DateTime.UtcNow - _lastCheckTime < _cacheTimeout)
                {
                    return _cachedResult;
                }
            }

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
                        issues.Add($"Configuration validation failed: {string.Join(", ", configErrors)}");
                        healthData["ConfigurationErrors"] = configErrors;
                    }
                }

                // 5. Perform a test log operation
                await PerformTestLogOperation(healthData, warnings, cancellationToken);

                // 6. Check memory usage and performance metrics
                CheckPerformanceMetrics(healthData, warnings);

                // 7. Determine overall health status
                HealthCheckResult result;
                if (issues.Count > 0)
                {
                    var issueDescription = string.Join("; ", issues);
                    result = HealthCheckResult.Unhealthy($"Logging system issues: {issueDescription}", healthData);
                }
                else if (warnings.Count > 0)
                {
                    var warningDescription = string.Join("; ", warnings);
                    result = HealthCheckResult.Degraded($"Logging system warnings: {warningDescription}", healthData);
                }
                else
                {
                    result = HealthCheckResult.Healthy("Logging system operating normally", healthData);
                }

                // Cache the result
                lock (_lock)
                {
                    _cachedResult = result;
                    _lastCheckTime = DateTime.UtcNow;
                }

                return result;
            }
            catch (Exception ex)
            {
                var errorData = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["ExceptionType"] = ex.GetType().Name,
                    ["CheckTime"] = DateTime.UtcNow
                };

                var result = HealthCheckResult.Unhealthy($"Logging health check failed: {ex.Message}", errorData);
                
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
                var testStartTime = DateTime.UtcNow;
                
                // Perform a test log operation
                var testMessage = $"Health check test - {DateTime.UtcNow:HH:mm:ss.fff}";
                var testCorrelationId = Guid.NewGuid().ToString("N")[..8];
                
                _loggingService.LogDebug(testMessage, testCorrelationId, "HealthCheck");
                
                // Force flush to ensure the message is processed
                _loggingService.Flush();
                
                var testDuration = DateTime.UtcNow - testStartTime;
                healthData["TestLogDuration"] = testDuration.TotalMilliseconds;
                
                // Check if test operation took too long
                if (testDuration.TotalMilliseconds > 1000) // 1 second threshold
                {
                    warnings.Add($"Test log operation slow: {testDuration.TotalMilliseconds:F1}ms");
                }
                
                healthData["TestLogSuccess"] = true;
                healthData["LastTestTime"] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                warnings.Add($"Test log operation failed: {ex.Message}");
                healthData["TestLogSuccess"] = false;
                healthData["TestLogError"] = ex.Message;
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
                // Check current memory usage
                var memoryBefore = GC.GetTotalMemory(false);
                GC.Collect(0, GCCollectionMode.Optimized);
                var memoryAfter = GC.GetTotalMemory(false);
                
                healthData["MemoryUsageBytes"] = memoryAfter;
                healthData["MemoryPressure"] = GC.GetTotalMemory(false) / (1024 * 1024); // MB
                
                // Check GC pressure
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                
                healthData["GC_Gen0Collections"] = gen0Collections;
                healthData["GC_Gen1Collections"] = gen1Collections;
                healthData["GC_Gen2Collections"] = gen2Collections;
                
                // Check if there's excessive GC pressure
                if (gen2Collections > 10) // Threshold for concern
                {
                    warnings.Add($"High Gen2 GC pressure detected: {gen2Collections} collections");
                }
                
                // Check thread count
                var threadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
                healthData["ThreadCount"] = threadCount;
                
                if (threadCount > 100) // Threshold for concern
                {
                    warnings.Add($"High thread count detected: {threadCount}");
                }
                
                // Check if high-performance mode is delivering expected performance
                var serviceConfig = _loggingService.Configuration;
                if (serviceConfig?.HighPerformanceMode == true)
                {
                    healthData["HighPerformanceModeActive"] = true;
                    // In high-performance mode, we expect very low allocation
                    if (memoryBefore - memoryAfter > 1024) // More than 1KB difference suggests allocations
                    {
                        warnings.Add("High allocations detected in high-performance mode");
                    }
                }
                
                // Check batch processing status
                if (serviceConfig?.BatchingEnabled == true)
                {
                    healthData["BatchingActive"] = true;
                    // Additional batching-specific metrics could be added here
                }
                
                healthData["PerformanceCheckSuccess"] = true;
            }
            catch (Exception ex)
            {
                warnings.Add($"Performance metrics check failed: {ex.Message}");
                healthData["PerformanceCheckSuccess"] = false;
                healthData["PerformanceCheckError"] = ex.Message;
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
                _cachedResult = HealthCheckResult.Unknown;
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
    
    /// <summary>
    /// Represents a profiler tag for performance monitoring.
    /// </summary>
    public readonly struct ProfilerTag
    {
        private readonly FixedString64Bytes _name;

        /// <summary>
        /// Initializes a new instance of the ProfilerTag.
        /// </summary>
        /// <param name="name">The tag name</param>
        public ProfilerTag(string name)
        {
            _name = new FixedString64Bytes(name ?? "Unknown");
        }

        /// <summary>
        /// Gets the tag name.
        /// </summary>
        public FixedString64Bytes Name => _name;

        /// <summary>
        /// Returns a string representation of the profiler tag.
        /// </summary>
        /// <returns>The tag name as a string</returns>
        public override string ToString() => _name.ToString();
    }
}