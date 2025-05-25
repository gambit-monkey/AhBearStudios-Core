using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AhBearStudios.Pooling.Diagnostics
{
    /// <summary>
    /// Job for processing pool health checks in parallel
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct PoolHealthCheckJob : IJob
    {
        /// <summary>
        /// Read-only access to pool metrics
        /// </summary>
        [ReadOnly] public NativeArray<PoolMetricsData> PoolMetrics;
        
        /// <summary>
        /// Writer for detected issues
        /// </summary>
        public NativeList<PoolHealthIssue>.ParallelWriter DetectedIssues;
        
        /// <summary>
        /// Current timestamp in seconds since startup
        /// </summary>
        public float TimeStamp;
        
        /// <summary>
        /// Configuration for health checks
        /// </summary>
        public HealthCheckConfig Config;

        /// <summary>
        /// Executes the health check job
        /// </summary>
        public void Execute()
        {
            for (int i = 0; i < PoolMetrics.Length; i++)
            {
                var metrics = PoolMetrics[i];
                ProcessPoolMetrics(metrics);
            }
        }

        /// <summary>
        /// Processes metrics for a single pool
        /// </summary>
        /// <param name="metrics">Pool metrics data</param>
        private void ProcessPoolMetrics(PoolMetricsData metrics)
        {
            // Skip global or invalid metrics
            if (metrics.PoolId.Equals(new FixedString64Bytes("Global")) || 
                metrics.PoolName.IsEmpty)
            {
                return;
            }

            // Extract key metrics for health checks
            var usageRatio = metrics.UsageRatio;
            var avgAcquireTime = metrics.AverageAcquireTimeMs;
            var hasLeaks = metrics.LeakedItemCount > 0;
            var leakPercent = metrics.TotalCreatedCount > 0 
                ? (float)metrics.LeakedItemCount / metrics.TotalCreatedCount 
                : 0;
            var fragmentationRatio = 1.0f - metrics.PoolEfficiency;

            // Convert pool ID from FixedString to Guid
            Guid poolId;
            try
            {
                poolId = new Guid(metrics.PoolId.ToString());
            }
            catch
            {
                // If can't parse as Guid, use a deterministic fallback based on string hash
                poolId = new Guid(metrics.PoolId.GetHashCode(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            // Check for various health issues
            CheckForIssues(
                poolId,
                metrics.PoolName.ToString(),
                hasLeaks,
                leakPercent,
                usageRatio,
                avgAcquireTime,
                fragmentationRatio,
                metrics.ContentionCount
            );
        }

        /// <summary>
        /// Checks for and reports various health issues
        /// </summary>
        /// <param name="poolId">Pool ID</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="hasLeaks">Whether leaks are present</param>
        /// <param name="leakPercent">Percentage of leaked items</param>
        /// <param name="usageRatio">Current usage ratio</param>
        /// <param name="avgAcquireTime">Average acquisition time in ms</param>
        /// <param name="fragmentationRatio">Fragmentation ratio</param>
        /// <param name="contentionCount">Thread contention count</param>
        private void CheckForIssues(
            Guid poolId,
            string poolName,
            bool hasLeaks,
            float leakPercent,
            float usageRatio,
            float avgAcquireTime,
            float fragmentationRatio,
            int contentionCount)
        {
            // 1. Check for memory leaks
            if (Config.AlertOnLeaks && hasLeaks && leakPercent >= Config.LeakThreshold)
            {
                int severity = leakPercent >= 0.2f ? 3 : (leakPercent >= 0.1f ? 2 : 1);
                
                DetectedIssues.AddNoResize(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "MemoryLeak",
                    $"Detected potential leak ({leakPercent:P2} of items not returned)",
                    severity,
                    true,
                    TimeStamp));
            }

            // 2. Check for high usage
            if (Config.AlertOnHighUsage && usageRatio > Config.HighUsageThreshold)
            {
                int severity = usageRatio >= 0.98f ? 3 : (usageRatio >= 0.9f ? 2 : 1);
                
                DetectedIssues.AddNoResize(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "HighUsage",
                    $"Pool usage at {usageRatio:P2} (threshold: {Config.HighUsageThreshold:P2})",
                    severity,
                    false,
                    TimeStamp));
            }

            // 3. Check for performance issues
            if (Config.AlertOnPerformanceIssues && avgAcquireTime > Config.SlowOperationThreshold)
            {
                int severity = avgAcquireTime >= Config.SlowOperationThreshold * 5 ? 3 : 
                              (avgAcquireTime >= Config.SlowOperationThreshold * 2 ? 2 : 1);
                
                DetectedIssues.AddNoResize(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "SlowAcquire",
                    $"Slow acquire operation: {avgAcquireTime:F2}ms (threshold: {Config.SlowOperationThreshold:F2}ms)",
                    severity,
                    false,
                    TimeStamp));
            }

            // 4. Check for fragmentation
            if (Config.AlertOnFragmentation && fragmentationRatio > 0.3f)
            {
                int severity = fragmentationRatio >= 0.6f ? 3 : (fragmentationRatio >= 0.45f ? 2 : 1);
                
                DetectedIssues.AddNoResize(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "Fragmentation",
                    $"Pool is fragmented: {fragmentationRatio:P2} of capacity inefficient",
                    severity,
                    false,
                    TimeStamp));
            }

            // 5. Check for thread contention
            if (Config.AlertOnThreadContention && contentionCount > 5)
            {
                int severity = contentionCount >= 100 ? 3 : (contentionCount >= 20 ? 2 : 1);
                
                DetectedIssues.AddNoResize(new PoolHealthIssue(
                    poolId,
                    poolName,
                    "ThreadContention",
                    $"Thread contention detected: {contentionCount} collisions",
                    severity,
                    false,
                    TimeStamp));
            }
        }
    }

    /// <summary>
    /// Configuration settings for health check jobs
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct HealthCheckConfig
    {
        /// <summary>
        /// Threshold for high pool usage (0-1)
        /// </summary>
        public float HighUsageThreshold;
        
        /// <summary>
        /// Threshold for slow operations in milliseconds
        /// </summary>
        public float SlowOperationThreshold;
        
        /// <summary>
        /// Threshold for memory leak detection (0-1)
        /// </summary>
        public float LeakThreshold;
        
        /// <summary>
        /// Whether to alert on memory leaks
        /// </summary>
        public bool AlertOnLeaks;
        
        /// <summary>
        /// Whether to alert on high usage
        /// </summary>
        public bool AlertOnHighUsage;
        
        /// <summary>
        /// Whether to alert on performance issues
        /// </summary>
        public bool AlertOnPerformanceIssues;
        
        /// <summary>
        /// Whether to alert on fragmentation
        /// </summary>
        public bool AlertOnFragmentation;
        
        /// <summary>
        /// Whether to alert on thread contention
        /// </summary>
        public bool AlertOnThreadContention;

        /// <summary>
        /// Creates a default configuration with recommended thresholds
        /// </summary>
        /// <returns>Default health check configuration</returns>
        public static HealthCheckConfig CreateDefault()
        {
            return new HealthCheckConfig
            {
                HighUsageThreshold = 0.85f,
                SlowOperationThreshold = 0.5f, // 0.5ms
                LeakThreshold = 0.05f, // 5%
                AlertOnLeaks = true,
                AlertOnHighUsage = true,
                AlertOnPerformanceIssues = true,
                AlertOnFragmentation = true,
                AlertOnThreadContention = true
            };
        }
    }

    /// <summary>
    /// Helper class for scheduling and managing pool health check jobs
    /// </summary>
    public static class PoolHealthCheckJobExtensions
    {
        /// <summary>
        /// Schedules a health check job for pool metrics
        /// </summary>
        /// <param name="poolMetrics">Array of pool metrics to check</param>
        /// <param name="issues">Native list for storing detected issues</param>
        /// <param name="config">Health check configuration</param>
        /// <param name="dependencies">Job dependencies</param>
        /// <returns>Job handle</returns>
        public static JobHandle ScheduleHealthCheck(
            NativeArray<PoolMetricsData> poolMetrics,
            NativeList<PoolHealthIssue> issues,
            HealthCheckConfig config = default,
            JobHandle dependencies = default)
        {
            if (config.Equals(default(HealthCheckConfig)))
            {
                config = HealthCheckConfig.CreateDefault();
            }

            var job = new PoolHealthCheckJob
            {
                PoolMetrics = poolMetrics,
                DetectedIssues = issues.AsParallelWriter(),
                TimeStamp = Time.realtimeSinceStartup,
                Config = config
            };

            return job.Schedule(dependencies);
        }
    }
}