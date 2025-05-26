using System;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Burst-compatible interface for pool metrics tracking.
    /// Designed to be used with the Unity Job System and Burst compiler.
    /// </summary>
    public interface INativePoolMetrics : IDisposable
    {
        /// <summary>
        /// Gets metrics data for a specific pool
        /// </summary>
        PoolMetricsData GetMetricsData(FixedString64Bytes poolId);
        
        /// <summary>
        /// Gets global metrics data aggregated across all pools
        /// </summary>
        PoolMetricsData GetGlobalMetricsData();
        
        /// <summary>
        /// Records an acquire operation for a pool
        /// </summary>
        JobHandle RecordAcquire(FixedString64Bytes poolId, int activeCount, float acquireTimeMs, JobHandle dependencies = default);
        
        /// <summary>
        /// Records a release operation for a pool
        /// </summary>
        JobHandle RecordRelease(FixedString64Bytes poolId, int activeCount, float releaseTimeMs, float lifetimeSeconds = 0, JobHandle dependencies = default);
        
        /// <summary>
        /// Updates pool capacity and configuration
        /// </summary>
        void UpdatePoolConfiguration(FixedString64Bytes poolId, int capacity, int minCapacity = 0, int maxCapacity = 0, FixedString64Bytes poolType = default, int itemSizeBytes = 0);
        
        /// <summary>
        /// Gets metrics data for all tracked pools
        /// </summary>
        NativeArray<PoolMetricsData> GetAllPoolMetrics(Allocator allocator);
        
        /// <summary>
        /// Reset statistics for a specific pool
        /// </summary>
        void ResetPoolStats(FixedString64Bytes poolId);
        
        /// <summary>
        /// Reset statistics for all pools
        /// </summary>
        void ResetAllPoolStats();
        
        /// <summary>
        /// Gets the cache hit ratio for a specific pool
        /// </summary>
        float GetPoolHitRatio(FixedString64Bytes poolId);
        
        /// <summary>
        /// Gets the overall efficiency for a specific pool
        /// </summary>
        float GetPoolEfficiency(FixedString64Bytes poolId);
        
        /// <summary>
        /// Checks if the native container is created
        /// </summary>
        bool IsCreated { get; }
        
        /// <summary>
        /// Gets the allocator used by this container
        /// </summary>
        Allocator Allocator { get; }
    }
}