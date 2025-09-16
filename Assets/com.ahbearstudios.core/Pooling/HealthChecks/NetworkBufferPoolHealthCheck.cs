using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Services;

namespace AhBearStudios.Core.Pooling.HealthChecks
{
    /// <summary>
    /// Health check implementation for network buffer pools.
    /// Monitors buffer pool health, memory usage, and performance metrics.
    /// Implements IHealthCheck for integration with the core health monitoring system.
    /// </summary>
    public class NetworkBufferPoolHealthCheck : IHealthCheck
    {
        private readonly NetworkSerializationBufferPool _bufferPool;
        private readonly NetworkBufferHealthThresholds _thresholds;
        private HealthCheckConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the NetworkBufferPoolHealthCheck.
        /// </summary>
        /// <param name="bufferPool">The network buffer pool to monitor</param>
        /// <param name="thresholds">Health monitoring thresholds</param>
        /// <param name="configuration">Health check configuration</param>
        public NetworkBufferPoolHealthCheck(
            NetworkSerializationBufferPool bufferPool,
            NetworkBufferHealthThresholds thresholds = null,
            HealthCheckConfiguration configuration = null)
        {
            _bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            _thresholds = thresholds ?? new NetworkBufferHealthThresholds();
            _configuration = configuration ?? CreateDefaultConfiguration();
        }

        #region IHealthCheck Implementation

        /// <summary>
        /// Gets the unique name of this health check.
        /// </summary>
        public FixedString64Bytes Name => new("NetworkBufferPool");

        /// <summary>
        /// Gets the description of this health check.
        /// </summary>
        public string Description => "Monitors network buffer pool health, memory usage, and performance metrics";

        /// <summary>
        /// Gets the category of this health check.
        /// </summary>
        public HealthCheckCategory Category => HealthCheckCategory.Performance;

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
        /// Performs a health check on the network buffer pool asynchronously.
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
                
                // Get pool statistics
                var totalBuffersCreated = EstimateTotalBuffers();
                var activeBuffers = EstimateActiveBuffers();
                var memoryUsageBytes = EstimateMemoryUsage();
                
                healthData["TotalBuffersCreated"] = totalBuffersCreated;
                healthData["ActiveBuffers"] = activeBuffers;
                healthData["MemoryUsageBytes"] = memoryUsageBytes;
                healthData["MemoryUsageMB"] = memoryUsageBytes / (1024.0 * 1024.0);
                
                var duration = DateTime.UtcNow - startTime;
                
                // Check critical thresholds
                if (memoryUsageBytes > _thresholds.CriticalMemoryUsageBytes)
                {
                    return HealthCheckResult.Unhealthy(
                        Name.ToString(),
                        $"Critical memory usage: {memoryUsageBytes / (1024 * 1024)}MB exceeds threshold",
                        duration,
                        healthData);
                }
                
                // Check warning thresholds
                if (memoryUsageBytes > _thresholds.WarningMemoryUsageBytes)
                {
                    return HealthCheckResult.Degraded(
                        Name.ToString(),
                        $"High memory usage: {memoryUsageBytes / (1024 * 1024)}MB",
                        duration,
                        healthData);
                }
                
                // Check for excessive active buffers (potential memory leak)
                var activeBufferRatio = totalBuffersCreated > 0 ? (double)activeBuffers / totalBuffersCreated : 0.0;
                healthData["ActiveBufferRatio"] = activeBufferRatio;
                
                if (activeBufferRatio > 0.8) // 80% of buffers are active
                {
                    return HealthCheckResult.Degraded(
                        Name.ToString(),
                        $"High active buffer ratio: {activeBufferRatio:P}",
                        duration,
                        healthData);
                }

                return HealthCheckResult.Healthy(
                    Name.ToString(),
                    "Network buffer pool is healthy",
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
                ["BufferPoolType"] = _bufferPool?.GetType().Name ?? "Unknown",
                ["ThresholdConfiguration"] = new Dictionary<string, object>
                {
                    ["CriticalMemoryUsageBytes"] = _thresholds.CriticalMemoryUsageBytes,
                    ["WarningMemoryUsageBytes"] = _thresholds.WarningMemoryUsageBytes
                }
            };
        }
        
        /// <summary>
        /// Creates the default configuration for the network buffer pool health check.
        /// </summary>
        /// <returns>Default health check configuration</returns>
        private HealthCheckConfiguration CreateDefaultConfiguration()
        {
            return HealthCheckConfiguration.Create(
                Name.ToString(),
                "Network Buffer Pool Health Check",
                HealthCheckCategory.Performance);
        }

        /// <summary>
        /// Gets total number of buffers created across all pools.
        /// </summary>
        /// <returns>Total buffer count from pool statistics</returns>
        private int EstimateTotalBuffers()
        {
            try
            {
                var stats = _bufferPool.GetStatistics();
                return (int)(stats.SmallBufferPoolStats?.TotalCreated ?? 0) +
                       (int)(stats.MediumBufferPoolStats?.TotalCreated ?? 0) +
                       (int)(stats.LargeBufferPoolStats?.TotalCreated ?? 0) +
                       (int)(stats.CompressionBufferPoolStats?.TotalCreated ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets number of currently active buffers across all pools.
        /// </summary>
        /// <returns>Active buffer count from pool statistics</returns>
        private int EstimateActiveBuffers()
        {
            try
            {
                var stats = _bufferPool.GetStatistics();
                return stats.TotalActiveObjects;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets total memory usage of all buffer pools based on active buffers.
        /// </summary>
        /// <returns>Memory usage in bytes from pool statistics</returns>
        private long EstimateMemoryUsage()
        {
            try
            {
                var stats = _bufferPool.GetStatistics();
                return stats.TotalMemoryUsage;
            }
            catch
            {
                return 0;
            }
        }
    }

}