using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
                if (_loggingService.HighPerformanceMode)
                {
                    healthData["HighPerformanceModeActive"] = true;
                    // In high-performance mode, we expect very low allocation
                    if (memoryBefore - memoryAfter > 1024) // More than 1KB difference suggests allocations
                    {
                        warnings.Add("High allocations detected in high-performance mode");
                    }
                }
                
                // Check batch processing status
                if (_loggingService.BatchingEnabled)
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
    /// Represents the result of a health check operation.
    /// </summary>
    public sealed class HealthCheckResult
    {
        /// <summary>
        /// Gets the health status.
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Gets the description of the health check result.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets additional data associated with the health check.
        /// </summary>
        public IReadOnlyDictionary<string, object> Data { get; }

        /// <summary>
        /// Gets the exception associated with the health check, if any.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the HealthCheckResult.
        /// </summary>
        /// <param name="status">The health status</param>
        /// <param name="description">The description</param>
        /// <param name="data">Additional data</param>
        /// <param name="exception">Associated exception</param>
        private HealthCheckResult(
            HealthStatus status, 
            string description, 
            IReadOnlyDictionary<string, object> data = null, 
            Exception exception = null)
        {
            Status = status;
            Description = description ?? string.Empty;
            Data = data ?? new Dictionary<string, object>();
            Exception = exception;
        }

        /// <summary>
        /// Creates a healthy health check result.
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="data">Additional data</param>
        /// <returns>A healthy health check result</returns>
        public static HealthCheckResult Healthy(string description = null, IReadOnlyDictionary<string, object> data = null)
        {
            return new HealthCheckResult(HealthStatus.Healthy, description, data);
        }

        /// <summary>
        /// Creates a degraded health check result.
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="data">Additional data</param>
        /// <returns>A degraded health check result</returns>
        public static HealthCheckResult Degraded(string description = null, IReadOnlyDictionary<string, object> data = null)
        {
            return new HealthCheckResult(HealthStatus.Degraded, description, data);
        }

        /// <summary>
        /// Creates an unhealthy health check result.
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="data">Additional data</param>
        /// <param name="exception">Associated exception</param>
        /// <returns>An unhealthy health check result</returns>
        public static HealthCheckResult Unhealthy(string description = null, IReadOnlyDictionary<string, object> data = null, Exception exception = null)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, description, data, exception);
        }

        /// <summary>
        /// Creates an unknown health check result.
        /// </summary>
        /// <param name="description">The description</param>
        /// <param name="data">Additional data</param>
        /// <returns>An unknown health check result</returns>
        public static HealthCheckResult Unknown = new HealthCheckResult(HealthStatus.Unknown, "Health status unknown");
    }

    /// <summary>
    /// Defines the possible health statuses.
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// The health status is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The component is healthy.
        /// </summary>
        Healthy = 1,

        /// <summary>
        /// The component is degraded but still functional.
        /// </summary>
        Degraded = 2,

        /// <summary>
        /// The component is unhealthy.
        /// </summary>
        Unhealthy = 3
    }

    /// <summary>
    /// Interface for health check implementations.
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// Gets the name of this health check.
        /// </summary>
        FixedString64Bytes Name { get; }

        /// <summary>
        /// Performs the health check operation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The health check result</returns>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for health check service (placeholder for core system integration).
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Registers a health check with the service.
        /// </summary>
        /// <param name="healthCheck">The health check to register</param>
        void RegisterHealthCheck(IHealthCheck healthCheck);

        /// <summary>
        /// Gets the overall health status of all registered checks.
        /// </summary>
        /// <returns>The overall health status</returns>
        Task<HealthCheckResult> GetOverallHealthStatusAsync();
    }

    /// <summary>
    /// Interface for alert service (placeholder for core system integration).
    /// </summary>
    public interface IAlertService
    {
        /// <summary>
        /// Raises an alert with the specified details.
        /// </summary>
        /// <param name="message">The alert message</param>
        /// <param name="severity">The alert severity</param>
        /// <param name="source">The alert source</param>
        /// <param name="tag">The alert tag</param>
        void RaiseAlert(FixedString128Bytes message, AlertSeverity severity, FixedString64Bytes source, FixedString64Bytes tag);
    }

    /// <summary>
    /// Interface for message bus service (placeholder for core system integration).
    /// </summary>
    public interface IMessageBusService
    {
        /// <summary>
        /// Publishes a message to the message bus.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        void PublishMessage<T>(T message) where T : IMessage;
    }

    /// <summary>
    /// Interface for pooling service (placeholder for core system integration).
    /// </summary>
    public interface IPoolingService
    {
        /// <summary>
        /// Gets a service instance from the pool.
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>The service instance</returns>
        T GetService<T>() where T : class;

        /// <summary>
        /// Registers a service with the pool.
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <param name="service">The service instance</param>
        void RegisterService<T>(T service) where T : class;
    }

    /// <summary>
    /// Interface for profiler service (placeholder for core system integration).
    /// </summary>
    public interface IProfilerService
    {
        /// <summary>
        /// Begins a profiler scope with the specified tag.
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <returns>A disposable profiler scope</returns>
        IDisposable BeginScope(ProfilerTag tag);
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

    /// <summary>
    /// Defines alert severity levels.
    /// </summary>
    public enum AlertSeverity
    {
        /// <summary>
        /// Informational alert.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning alert.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Critical alert.
        /// </summary>
        Critical = 2,

        /// <summary>
        /// Emergency alert.
        /// </summary>
        Emergency = 3
    }
}