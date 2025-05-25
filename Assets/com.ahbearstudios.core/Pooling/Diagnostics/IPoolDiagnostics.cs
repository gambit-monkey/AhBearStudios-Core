using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Interface for collecting and managing pool diagnostics.
    /// Provides centralized performance tracking and metrics collection for all pools.
    /// </summary>
    public interface IPoolDiagnostics
    {
        /// <summary>
        /// Registers a pool for tracking by its ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Optional pool name</param>
        /// <param name="estimatedItemSizeBytes">Estimated memory size per item in bytes</param>
        /// <returns>True if registration was successful, false otherwise</returns>
        bool RegisterPoolById(FixedString64Bytes poolId, string poolName = null, int estimatedItemSizeBytes = 0);
        
        /// <summary>
        /// Registers a pool for tracking.
        /// </summary>
        /// <param name="pool">The pool to track</param>
        /// <param name="poolName">Optional pool name</param>
        /// <param name="estimatedItemSizeBytes">Estimated memory size per item in bytes</param>
        /// <returns>True if registration was successful, false otherwise</returns>
        bool RegisterPool(IPool pool, string poolName = null, int estimatedItemSizeBytes = 0);
        
        /// <summary>
        /// Unregisters a pool from tracking by its ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to unregister</param>
        /// <returns>True if unregistration was successful, false otherwise</returns>
        bool UnregisterPoolById(FixedString64Bytes poolId);
        
        /// <summary>
        /// Unregisters a pool from tracking.
        /// </summary>
        /// <param name="pool">The pool to stop tracking</param>
        /// <returns>True if unregistration was successful, false otherwise</returns>
        bool UnregisterPool(IPool pool);
        
        /// <summary>
        /// Resets metrics for a specific pool by its ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to reset metrics for</param>
        /// <returns>True if reset was successful, false otherwise</returns>
        bool ResetPoolMetricsById(FixedString64Bytes poolId);
        
        /// <summary>
        /// Resets metrics for a specific pool.
        /// </summary>
        /// <param name="pool">The pool to reset metrics for</param>
        /// <returns>True if reset was successful, false otherwise</returns>
        bool ResetPoolMetrics(IPool pool);
        
        /// <summary>
        /// Gets a unique ID for tracking an object.
        /// </summary>
        /// <param name="item">The object to track</param>
        /// <returns>Unique ID for the object</returns>
        int GetObjectId(object item);
        
        /// <summary>
        /// Records that an item was created in a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that created the item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordCreateById(FixedString64Bytes poolId);
        
        /// <summary>
        /// Records that an item was created.
        /// </summary>
        /// <param name="pool">The pool that created the item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordCreate(IPool pool);
        
        /// <summary>
        /// Records the start of an acquire operation for a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool from which the item is being acquired</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordAcquireStartById(FixedString64Bytes poolId);
        
        /// <summary>
        /// Records the start of an acquire operation.
        /// </summary>
        /// <param name="pool">The pool from which the item is being acquired</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordAcquireStart(IPool pool);
        
        /// <summary>
        /// Records the completion of an acquire operation for a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool from which the item was acquired</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="item">The acquired item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordAcquireCompleteById(FixedString64Bytes poolId, int activeCount, object item = null);
        
        /// <summary>
        /// Records the completion of an acquire operation.
        /// </summary>
        /// <param name="pool">The pool from which the item was acquired</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="item">The acquired item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordAcquireComplete(IPool pool, int activeCount, object item = null);
        
        /// <summary>
        /// Records that an item was released to a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to which the item was released</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordReleaseById(FixedString64Bytes poolId, object item = null);
        
        /// <summary>
        /// Records that an item was released.
        /// </summary>
        /// <param name="pool">The pool to which the item was released</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordRelease(IPool pool, object item = null);
        
        /// <summary>
        /// Records that an item was released to a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to which the item was released</param>
        /// <param name="activeCount">Current active count after release</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordReleaseById(FixedString64Bytes poolId, int activeCount, object item = null);
        
        /// <summary>
        /// Records that an item was released.
        /// </summary>
        /// <param name="pool">The pool to which the item was released</param>
        /// <param name="activeCount">Current active count after release</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordRelease(IPool pool, int activeCount, object item = null);
        
        /// <summary>
        /// Gets metrics for all pools.
        /// </summary>
        /// <returns>List of metrics dictionaries</returns>
        List<Dictionary<string, object>> GetAllMetrics();
        
        /// <summary>
        /// Gets metrics for a specific pool by ID.
        /// </summary>
        /// <param name="poolId">The unique identifier of the pool to get metrics for</param>
        /// <returns>Metrics dictionary or null if not found</returns>
        Dictionary<string, object> GetPoolMetricsById(FixedString64Bytes poolId);
        
        /// <summary>
        /// Gets metrics for a specific pool.
        /// </summary>
        /// <param name="pool">The pool to get metrics for</param>
        /// <returns>Metrics dictionary or null if not found</returns>
        Dictionary<string, object> GetPoolMetrics(IPool pool);
        
        /// <summary>
        /// Records that a pool was shrunk.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that was shrunk</param>
        /// <param name="itemsRemoved">Number of items removed during shrinking</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordPoolShrinkById(FixedString64Bytes poolId, int itemsRemoved);
        
        /// <summary>
        /// Records that a pool was shrunk.
        /// </summary>
        /// <param name="pool">The pool that was shrunk</param>
        /// <param name="itemsRemoved">Number of items removed during shrinking</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordPoolShrink(IPool pool, int itemsRemoved);
        
        /// <summary>
        /// Records an item activity (acquire or release) for a pool.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="isAcquire">True if item was acquired, false if released</param>
        /// <param name="totalCount">Total count of items in the pool</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordItemActivity(FixedString64Bytes poolId, bool isAcquire, int totalCount);
        
        /// <summary>
        /// Records that a pool was reset.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that was reset</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordPoolReset(FixedString64Bytes poolId);
        
        /// <summary>
        /// Records that a pool was disposed.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that was disposed</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        bool RecordPoolDisposed(FixedString64Bytes poolId);
        
        /// <summary>
        /// Refreshes pool statistics by gathering current metrics from all tracked pools.
        /// This should be called after major changes to the pool system, such as clearing or
        /// initializing multiple pools.
        /// </summary>
        void RefreshPoolStats();
        
        /// <summary>
        /// Resets all metrics.
        /// </summary>
        void ResetAllMetrics();
        
        /// <summary>
        /// Checks whether auto-reset is needed and performs it if necessary.
        /// </summary>
        void CheckAutoReset();
        
        /// <summary>
        /// Gets a NativeArray of pool metrics for all tracked pools.
        /// Useful for jobs and burst-compatible code.
        /// </summary>
        /// <param name="allocator">Allocator to use for the native array</param>
        /// <returns>NativeArray containing pool metrics data</returns>
        NativeArray<PoolMetricsData> GetPoolMetricsDataArray(Allocator allocator);
    }
}