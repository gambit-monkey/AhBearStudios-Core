using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ZLinq;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Services;

namespace AhBearStudios.Core.Pooling.HealthChecks
{
    /// <summary>
    /// Health check implementation for the pooling service.
    /// Monitors overall pooling system health and performance.
    /// Implements IHealthCheck for integration with the core health monitoring system.
    /// </summary>
    public class PoolingServiceHealthCheck : IHealthCheck
    {
        private readonly IPoolingService _poolingService;
        private HealthCheckConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the PoolingServiceHealthCheck.
        /// </summary>
        /// <param name="poolingService">The pooling service to monitor</param>
        /// <param name="configuration">Health check configuration</param>
        public PoolingServiceHealthCheck(IPoolingService poolingService, HealthCheckConfiguration configuration = null)
        {
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            _configuration = configuration ?? CreateDefaultConfiguration();
        }
        
        #region IHealthCheck Implementation
        
        /// <summary>
        /// Gets the unique name of this health check.
        /// </summary>
        public FixedString64Bytes Name => new("PoolingService");
        
        /// <summary>
        /// Gets the description of this health check.
        /// </summary>
        public string Description => "Monitors overall pooling system health and performance";
        
        /// <summary>
        /// Gets the category of this health check.
        /// </summary>
        public HealthCheckCategory Category => HealthCheckCategory.System;
        
        /// <summary>
        /// Gets the timeout for this health check.
        /// </summary>
        public TimeSpan Timeout => _configuration?.Timeout ?? TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Gets the current configuration for this health check.
        /// </summary>
        public HealthCheckConfiguration Configuration => _configuration;
        
        /// <summary>
        /// Gets the dependencies for this health check.
        /// </summary>
        public IEnumerable<FixedString64Bytes> Dependencies { get; } = Array.Empty<FixedString64Bytes>();
        
        #endregion

        /// <summary>
        /// Performs a health check on the pooling service asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Health check result</returns>
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var healthData = new Dictionary<string, object>();
                
                // Check if service is responsive
                var statistics = _poolingService.GetAllPoolStatistics();
                healthData["PoolCount"] = statistics.Count;
                
                // Validate all pools
                var allPoolsValid = _poolingService.ValidateAllPools();
                healthData["AllPoolsValid"] = allPoolsValid;
                
                var duration = DateTime.UtcNow - startTime;
                
                if (!allPoolsValid)
                {
                    return HealthCheckResult.Degraded(
                        Name.ToString(),
                        "Some pools failed validation",
                        duration,
                        healthData);
                }

                // Check memory usage and pool performance
                var totalMemoryUsage = CalculateTotalMemoryUsage(statistics);
                healthData["TotalMemoryUsageBytes"] = totalMemoryUsage;
                healthData["TotalMemoryUsageMB"] = totalMemoryUsage / (1024.0 * 1024.0);
                
                var healthThresholds = new NetworkBufferHealthThresholds();
                
                if (totalMemoryUsage > healthThresholds.CriticalMemoryUsageBytes)
                {
                    return HealthCheckResult.Unhealthy(
                        Name.ToString(),
                        $"Memory usage critical: {totalMemoryUsage / (1024 * 1024)}MB",
                        duration,
                        healthData);
                }
                
                if (totalMemoryUsage > healthThresholds.WarningMemoryUsageBytes)
                {
                    return HealthCheckResult.Degraded(
                        Name.ToString(),
                        $"Memory usage high: {totalMemoryUsage / (1024 * 1024)}MB",
                        duration,
                        healthData);
                }

                return HealthCheckResult.Healthy(
                    Name.ToString(),
                    "Pooling service is healthy",
                    duration,
                    healthData);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                var duration = DateTime.UtcNow - startTime;
                return HealthCheckResult.Unhealthy(
                    Name.ToString(),
                    "Health check was cancelled",
                    duration);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var errorData = new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["ExceptionType"] = ex.GetType().Name,
                    ["StackTrace"] = ex.StackTrace
                };
                
                return HealthCheckResult.Unhealthy(
                    Name.ToString(),
                    $"Health check failed: {ex.Message}",
                    duration,
                    errorData,
                    ex);
            }
        }

        /// <summary>
        /// Configures the health check with new settings.
        /// </summary>
        /// <param name="configuration">The configuration to apply</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        public void Configure(HealthCheckConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        /// <summary>
        /// Gets comprehensive metadata about this health check.
        /// </summary>
        /// <returns>Dictionary containing metadata about the health check</returns>
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Name"] = Name.ToString(),
                ["Description"] = Description,
                ["Category"] = Category.ToString(),
                ["Timeout"] = Timeout.TotalSeconds,
                ["Version"] = "1.0.0",
                ["Dependencies"] = Dependencies.AsValueEnumerable().Select(d => d.ToString()).ToArray(),
                ["SupportsAsync"] = true,
                ["ThreadSafe"] = true,
                ["PerformanceOptimized"] = true,
                ["UnityCompatible"] = true,
                ["PoolingServiceType"] = _poolingService?.GetType().Name ?? "Unknown"
            };
        }
        
        /// <summary>
        /// Creates the default configuration for the pooling service health check.
        /// </summary>
        /// <returns>Default health check configuration</returns>
        private HealthCheckConfiguration CreateDefaultConfiguration()
        {
            return HealthCheckConfiguration.ForCriticalSystem(
                Name,
                "Pooling Service Health Check",
                "Monitors overall pooling system health and performance");
        }
        
        /// <summary>
        /// Calculates total memory usage across all pools.
        /// </summary>
        /// <param name="statistics">Pool statistics</param>
        /// <returns>Total memory usage in bytes</returns>
        private static long CalculateTotalMemoryUsage(Dictionary<string, Models.PoolStatistics> statistics)
        {
            // This would need to be implemented based on actual pool statistics
            // For now, returning estimated usage
            return statistics.Count * 1024 * 1024; // Rough estimate of 1MB per pool
        }
    }

}