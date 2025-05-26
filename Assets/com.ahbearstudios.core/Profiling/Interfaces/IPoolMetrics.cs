using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for pool metrics tracking for managed object pools.
    /// Provides metrics collection and performance analysis capabilities
    /// while maintaining consistency with the native metrics system design.
    /// </summary>
    public interface IPoolMetrics
    {
        /// <summary>
        /// Gets metrics data for a specific pool
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <returns>Pool metrics data</returns>
        PoolMetricsData GetMetricsData(Guid poolId);
        
        /// <summary>
        /// Gets global metrics data aggregated across all pools
        /// </summary>
        /// <returns>Aggregated global metrics</returns>
        PoolMetricsData GetGlobalMetricsData();
        
        /// <summary>
        /// Records an acquire operation for a pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="activeCount">Current active item count</param>
        /// <param name="acquireTimeMs">Time taken to acquire in milliseconds</param>
        void RecordAcquire(Guid poolId, int activeCount, float acquireTimeMs);
        
        /// <summary>
        /// Records a release operation for a pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="activeCount">Current active item count</param>
        /// <param name="releaseTimeMs">Time taken to release in milliseconds</param>
        /// <param name="lifetimeSeconds">Lifetime of the item in seconds</param>
        void RecordRelease(Guid poolId, int activeCount, float releaseTimeMs, float lifetimeSeconds = 0);
        
        /// <summary>
        /// Updates pool capacity and configuration
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="capacity">Current capacity</param>
        /// <param name="minCapacity">Minimum capacity (0 for no minimum)</param>
        /// <param name="maxCapacity">Maximum capacity (0 for no maximum)</param>
        /// <param name="poolType">Type of pool</param>
        /// <param name="itemSizeBytes">Estimated size of each item in bytes</param>
        void UpdatePoolConfiguration(Guid poolId, int capacity, int minCapacity = 0, int maxCapacity = 0, string poolType = null, int itemSizeBytes = 0);
        
        /// <summary>
        /// Gets metrics data for all tracked pools
        /// </summary>
        /// <returns>Dictionary mapping pool IDs to metrics data</returns>
        Dictionary<Guid, PoolMetricsData> GetAllPoolMetrics();
        
        /// <summary>
        /// Reset statistics for a specific pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        void ResetPoolStats(Guid poolId);
        
        /// <summary>
        /// Reset statistics for all pools
        /// </summary>
        void ResetAllPoolStats();
        
        /// <summary>
        /// Gets the cache hit ratio for a specific pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <returns>Hit ratio (0-1)</returns>
        float GetPoolHitRatio(Guid poolId);
        
        /// <summary>
        /// Gets the overall efficiency for a specific pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <returns>Efficiency rating (0-1)</returns>
        float GetPoolEfficiency(Guid poolId);
        
        /// <summary>
        /// Records a create operation for the pool that adds new items
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="createdCount">Number of items created</param>
        /// <param name="memoryOverheadBytes">Additional memory overhead in bytes</param>
        void RecordCreate(Guid poolId, int createdCount, long memoryOverheadBytes = 0);
        
        /// <summary>
        /// Records metrics about pool fragmentation
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="fragmentCount">Number of memory fragments</param>
        /// <param name="fragmentationRatio">Ratio of fragmentation (0-1)</param>
        void RecordFragmentation(Guid poolId, int fragmentCount, float fragmentationRatio);
        
        /// <summary>
        /// Records information about operational success/failure
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="acquireSuccessCount">Number of successful acquire operations</param>
        /// <param name="acquireFailureCount">Number of failed acquire operations</param>
        /// <param name="releaseSuccessCount">Number of successful release operations</param>
        /// <param name="releaseFailureCount">Number of failed release operations</param>
        void RecordOperationResults(Guid poolId, int acquireSuccessCount = 0, int acquireFailureCount = 0, 
                                   int releaseSuccessCount = 0, int releaseFailureCount = 0);
                                   
        /// <summary>
        /// Records a resize operation for the pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="oldCapacity">Previous capacity</param>
        /// <param name="newCapacity">New capacity</param>
        /// <param name="resizeTimeMs">Time taken to resize in milliseconds</param>
        void RecordResize(Guid poolId, int oldCapacity, int newCapacity, float resizeTimeMs = 0);
        
        /// <summary>
        /// Gets a performance snapshot of a specific pool suitable for display
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <returns>Dictionary of formatted metric values</returns>
        Dictionary<string, string> GetPerformanceSnapshot(Guid poolId);
    }
}