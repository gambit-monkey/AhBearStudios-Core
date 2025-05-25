using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Pooling.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Centralized diagnostics system for monitoring pool performance and usage.
    /// Provides detailed tracking and metrics collection for all registered pools.
    /// </summary>
    public sealed class PoolDiagnostics : IPoolDiagnostics, IDisposable
    {
        #region Private Fields

        private readonly IPoolLogger _logger;
        private readonly IPoolMetrics _metrics;
        private readonly IPoolProfiler _profiler;
        private readonly IPoolHealthChecker _healthChecker;

        // Using Unity Collections v2 for thread-safety and Burst compatibility
        private UnsafeParallelHashMap<FixedString64Bytes, PoolTrackingInfo> _poolsById;
        private UnsafeParallelHashMap<int, FixedString64Bytes> _poolGuidByHashCode;
        private UnsafeParallelHashMap<int, int> _objectIds;

        private int _nextObjectId;
        private float _lastResetTime;
        private float _lastRefreshTime;
        private bool _isEnabled = true;
        private bool _isDisposed;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initializes a new instance of the PoolDiagnostics class with optional service dependencies.
        /// </summary>
        /// <param name="serviceLocator">Optional service locator for dependency injection</param>
        public PoolDiagnostics(IPoolingServiceLocator serviceLocator = null)
        {
            // Get dependencies from service locator or use default services
            var services = serviceLocator ?? DefaultPoolingServices.Instance;

            _logger = services.GetService<IPoolLogger>();
            _metrics = services.GetService<IPoolMetrics>();
            _profiler = services.GetService<IPoolProfiler>();
            _healthChecker = services.GetService<IPoolHealthChecker>();

            // Initialize collections
            _poolsById = new UnsafeParallelHashMap<FixedString64Bytes, PoolTrackingInfo>(64, Allocator.Persistent);
            _poolGuidByHashCode = new UnsafeParallelHashMap<int, FixedString64Bytes>(64, Allocator.Persistent);
            _objectIds = new UnsafeParallelHashMap<int, int>(1024, Allocator.Persistent);

            _lastResetTime = Time.realtimeSinceStartup;
            _lastRefreshTime = Time.realtimeSinceStartup;
            _nextObjectId = 1;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the object, cleaning up all unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            // Dispose all native collections
            if (_poolsById.IsCreated) _poolsById.Dispose();
            if (_poolGuidByHashCode.IsCreated) _poolGuidByHashCode.Dispose();
            if (_objectIds.IsCreated) _objectIds.Dispose();

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up if Dispose is not called.
        /// </summary>
        ~PoolDiagnostics()
        {
            Dispose();
        }

        #endregion

        #region IPoolDiagnostics Implementation

        /// <summary>
        /// Registers a pool for tracking by its ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Optional pool name</param>
        /// <param name="estimatedItemSizeBytes">Estimated memory size per item in bytes</param>
        /// <returns>True if registration was successful, false otherwise</returns>
        public bool RegisterPoolById(FixedString64Bytes poolId, string poolName = null, int estimatedItemSizeBytes = 0)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.ContainsKey(poolId))
            {
                // Update existing registration
                if (_poolsById.TryGetValue(poolId, out var existingInfo))
                {
                    existingInfo.PoolName = poolName ?? existingInfo.PoolName;
                    existingInfo.EstimatedItemSizeBytes = estimatedItemSizeBytes > 0
                        ? estimatedItemSizeBytes
                        : existingInfo.EstimatedItemSizeBytes;

                    _poolsById[poolId] = existingInfo;
                }

                return true;
            }

            // Create new registration
            var info = new PoolTrackingInfo
            {
                PoolId = poolId,
                PoolName = poolName ?? $"Pool-{poolId}",
                EstimatedItemSizeBytes = estimatedItemSizeBytes,
                RegistrationTicks = DateTime.UtcNow.Ticks
            };

            _poolsById.Add(poolId, info);
            return true;
        }

        /// <summary>
        /// Registers a pool for tracking.
        /// </summary>
        /// <param name="pool">The pool to track</param>
        /// <param name="poolName">Optional pool name</param>
        /// <param name="estimatedItemSizeBytes">Estimated memory size per item in bytes</param>
        /// <returns>True if registration was successful, false otherwise</returns>
        public bool RegisterPool(IPool pool, string poolName = null, int estimatedItemSizeBytes = 0)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            FixedString64Bytes poolId = pool.Id.ToFixedString64Bytes();
            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (!_poolGuidByHashCode.ContainsKey(hashCode))
            {
                _poolGuidByHashCode.Add(hashCode, poolId);
            }

            if (_poolsById.ContainsKey(poolId))
            {
                // Update existing registration
                if (_poolsById.TryGetValue(poolId, out var existingInfo))
                {
                    existingInfo.PoolName = poolName ?? pool.PoolName ?? existingInfo.PoolName;
                    existingInfo.TypeHash = pool.ItemType.GetHashCode();
                    existingInfo.EstimatedItemSizeBytes = estimatedItemSizeBytes > 0
                        ? estimatedItemSizeBytes
                        : existingInfo.EstimatedItemSizeBytes;

                    _poolsById[poolId] = existingInfo;
                }

                return true;
            }

            // Create new registration
            var info = new PoolTrackingInfo
            {
                PoolId = poolId,
                PoolName = poolName ?? pool.PoolName ?? $"Pool-{poolId}",
                TypeHash = pool.ItemType.GetHashCode(),
                EstimatedItemSizeBytes = estimatedItemSizeBytes,
                RegistrationTicks = DateTime.UtcNow.Ticks
            };

            _poolsById.Add(poolId, info);
            return true;
        }

        /// <summary>
        /// Unregisters a pool from tracking by its ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to unregister</param>
        /// <returns>True if unregistration was successful, false otherwise</returns>
        public bool UnregisterPoolById(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            return _poolsById.Remove(poolId);
        }

        /// <summary>
        /// Unregisters a pool from tracking.
        /// </summary>
        /// <param name="pool">The pool to stop tracking</param>
        /// <returns>True if unregistration was successful, false otherwise</returns>
        public bool UnregisterPool(IPool pool)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);
            bool removed = false;

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                removed = _poolsById.Remove(poolId);
                _poolGuidByHashCode.Remove(hashCode);
            }

            return removed;
        }

        /// <summary>
        /// Resets metrics for a specific pool by its ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to reset metrics for</param>
        /// <returns>True if reset was successful, false otherwise</returns>
        public bool ResetPoolMetricsById(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                info.PeakActiveCount = info.ActiveCount;
                info.TotalAcquireTime = 0;
                info.MaxAcquireTime = 0;
                info.LastAcquireTime = 0;
                info.MaxReleaseTimeMs = 0;
                info.LastReleaseTimeMs = 0;
                info.TotalReleaseTime = 0;

                _poolsById[poolId] = info;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets metrics for a specific pool.
        /// </summary>
        /// <param name="pool">The pool to reset metrics for</param>
        /// <returns>True if reset was successful, false otherwise</returns>
        public bool ResetPoolMetrics(IPool pool)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return ResetPoolMetricsById(poolId);
            }

            return false;
        }

        /// <summary>
        /// Gets a unique ID for tracking an object.
        /// </summary>
        /// <param name="item">The object to track</param>
        /// <returns>Unique ID for the object</returns>
        public int GetObjectId(object item)
        {
            if (item == null || !_isEnabled || _isDisposed)
                return 0;

            int hashCode = RuntimeHelpers.GetHashCode(item);

            if (_objectIds.TryGetValue(hashCode, out int id))
            {
                return id;
            }

            id = System.Threading.Interlocked.Increment(ref _nextObjectId);
            _objectIds.Add(hashCode, id);

            return id;
        }

        /// <summary>
        /// Records that an item was created in a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that created the item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordCreateById(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                info.TotalCreated++;
                _poolsById[poolId] = info;

                // Update metrics service if available
                if (_metrics != null)
                {
                    // For IPoolMetrics (managed)
                    if (_metrics is IPoolMetrics managedMetrics)
                    {
                        Guid guidPoolId = poolId.ToGuid();
                        managedMetrics.RecordCreate(guidPoolId, 1);
                    }
                    // For INativePoolMetrics (unmanaged)
                    else if (_metrics is INativePoolMetrics nativeMetrics)
                    {
                        // The default created count is 1 with no memory overhead
                        nativeMetrics.UpdatePoolConfiguration(
                            poolId, 
                            info.TotalCapacity, 
                            minCapacity: 0, 
                            maxCapacity: 0);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records that an item was created.
        /// </summary>
        /// <param name="pool">The pool that created the item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordCreate(IPool pool)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return RecordCreateById(poolId);
            }

            // Auto-register pool if not found
            if (RegisterPool(pool))
            {
                return RecordCreateById(pool.Id.ToFixedString64Bytes());
            }

            return false;
        }

        /// <summary>
        /// Records the start of an acquire operation for a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool from which the item is being acquired</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordAcquireStartById(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                info.AcquireStartTime = Time.realtimeSinceStartup;
                _poolsById[poolId] = info;

                // Start profiler sample if available
                _profiler?.BeginSample("Acquire", poolId.ToGuid(), info.PoolName.ToString());

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records the start of an acquire operation.
        /// </summary>
        /// <param name="pool">The pool from which the item is being acquired</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordAcquireStart(IPool pool)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return RecordAcquireStartById(poolId);
            }

            // Auto-register pool if not found
            if (RegisterPool(pool))
            {
                return RecordAcquireStartById(pool.Id.ToFixedString64Bytes());
            }

            return false;
        }

        /// <summary>
        /// Records the completion of an acquire operation for a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool from which the item was acquired</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="item">The acquired item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordAcquireCompleteById(FixedString64Bytes poolId, int activeCount, object item = null)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                info.TotalAcquired++;
                info.ActiveCount = activeCount;
                info.PeakActiveCount = math.max(info.PeakActiveCount, activeCount);

                float elapsed = Time.realtimeSinceStartup - info.AcquireStartTime;
                info.TotalAcquireTime += elapsed;
                info.LastAcquireTime = elapsed;
                info.MaxAcquireTime = math.max(info.MaxAcquireTime, elapsed);

                _poolsById[poolId] = info;

                // End profiler sample if available
                _profiler?.EndSample("Acquire", poolId.ToGuid(), info.PoolName.ToString(), activeCount,
                    info.TotalCreated - activeCount);

                // Record in metrics service if available
                if (_metrics != null)
                {
                    float acquireTimeMs = elapsed * 1000f; // Convert to ms

                    // For IPoolMetrics (managed)
                    if (_metrics is IPoolMetrics managedMetrics)
                    {
                        Guid guidPoolId = poolId.ToGuid();
                        managedMetrics.RecordAcquire(guidPoolId, activeCount, acquireTimeMs);
                
                        // Handle item-specific tracking with managed system
                        if (item != null)
                        {
                            int itemId = GetObjectId(item);
                            managedMetrics.RecordOperationResults(guidPoolId, acquireSuccessCount: 1);
                        }
                    }
                    // For INativePoolMetrics (unmanaged)
                    else if (_metrics is INativePoolMetrics nativeMetrics)
                    {
                        // Record the acquire through the native interface
                        // No need to use JobHandle as this is called from main thread
                        nativeMetrics.RecordAcquire(poolId, activeCount, acquireTimeMs);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records the completion of an acquire operation.
        /// </summary>
        /// <param name="pool">The pool from which the item was acquired</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="item">The acquired item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordAcquireComplete(IPool pool, int activeCount, object item = null)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return RecordAcquireCompleteById(poolId, activeCount, item);
            }

            // Auto-register pool if not found
            if (RegisterPool(pool))
            {
                return RecordAcquireCompleteById(pool.Id.ToFixedString64Bytes(), activeCount, item);
            }

            return false;
        }

        /// <summary>
        /// Records that an item was released to a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to which the item was released</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordReleaseById(FixedString64Bytes poolId, object item = null)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                // Start with a timestamp for release timing
                float startTime = Time.realtimeSinceStartup;

                // Start profiler sample if available
                _profiler?.BeginSample("Release", poolId.ToGuid(), info.PoolName.ToString());

                info.TotalReleased++;
                _poolsById[poolId] = info;

                float elapsed = Time.realtimeSinceStartup - startTime;
                info.LastReleaseTimeMs = elapsed;
                info.TotalReleaseTime += elapsed;
                info.MaxReleaseTimeMs = math.max(info.MaxReleaseTimeMs, elapsed);

                _poolsById[poolId] = info;

                // End profiler sample if available
                int freeCount = info.TotalCreated - info.ActiveCount + 1; // +1 because we just released one
                _profiler?.EndSample("Release", poolId.ToGuid(), info.PoolName.ToString(), info.ActiveCount - 1, freeCount);

                // Track the item if provided
                if (item != null)
                {
                    int itemId = GetObjectId(item);
                    _metrics?.RecordRelease(poolId.ToGuid(), info.ActiveCount - 1, itemId, elapsed * 1000f);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records that an item was released.
        /// </summary>
        /// <param name="pool">The pool to which the item was released</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordRelease(IPool pool, object item = null)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return RecordReleaseById(poolId, item);
            }

            // Auto-register pool if not found
            if (RegisterPool(pool))
            {
                return RecordReleaseById(pool.Id.ToFixedString64Bytes(), item);
            }

            return false;
        }

        /// <summary>
        /// Records that an item was released to a pool identified by ID.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool to which the item was released</param>
        /// <param name="activeCount">Current active count after release</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordReleaseById(FixedString64Bytes poolId, int activeCount, object item = null)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                // Start with a timestamp for release timing
                float startTime = Time.realtimeSinceStartup;

                // Start profiler sample if available
                _profiler?.BeginSample("Release", poolId.ToGuid(), info.PoolName.ToString());

                info.TotalReleased++;
                info.ActiveCount = activeCount;
                _poolsById[poolId] = info;

                float elapsed = Time.realtimeSinceStartup - startTime;
                info.LastReleaseTimeMs = elapsed;
                info.TotalReleaseTime += elapsed;
                info.MaxReleaseTimeMs = math.max(info.MaxReleaseTimeMs, elapsed);

                _poolsById[poolId] = info;

                // End profiler sample if available
                int freeCount = info.TotalCreated - activeCount;
                _profiler?.EndSample("Release", poolId.ToGuid(), info.PoolName.ToString(), activeCount, freeCount);

                // Record in metrics service if available
                _metrics?.RecordRelease(poolId.ToGuid(), activeCount, elapsed * 1000f);

                // Track the item if provided
                if (item != null)
                {
                    int itemId = GetObjectId(item);
                    _metrics?.RecordRelease(poolId.ToGuid(), activeCount, itemId, elapsed * 1000f);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records that an item was released.
        /// </summary>
        /// <param name="pool">The pool to which the item was released</param>
        /// <param name="activeCount">Current active count after release</param>
        /// <param name="item">The released item</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordRelease(IPool pool, int activeCount, object item = null)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return RecordReleaseById(poolId, activeCount, item);
            }

            // Auto-register pool if not found
            if (RegisterPool(pool))
            {
                return RecordReleaseById(pool.Id.ToFixedString64Bytes(), activeCount, item);
            }

            return false;
        }

        /// <summary>
        /// Gets metrics for all pools.
        /// </summary>
        /// <returns>List of metrics dictionaries</returns>
        public List<Dictionary<string, object>> GetAllMetrics()
        {
            if (!_isEnabled || _isDisposed)
                return new List<Dictionary<string, object>>();

            var result = new List<Dictionary<string, object>>();

            foreach (var pair in _poolsById)
            {
                var poolId = pair.Key;
                var info = pair.Value;
                result.Add(GetMetricsFromInfo(poolId, info));
            }

            return result;
        }

        /// <summary>
        /// Gets metrics for a specific pool by ID.
        /// </summary>
        /// <param name="poolId">The unique identifier of the pool to get metrics for</param>
        /// <returns>Metrics dictionary or null if not found</returns>
        public Dictionary<string, object> GetPoolMetricsById(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return null;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                return GetMetricsFromInfo(poolId, info);
            }

            return null;
        }

        /// <summary>
        /// Gets metrics for a specific pool.
        /// </summary>
        /// <param name="pool">The pool to get metrics for</param>
        /// <returns>Metrics dictionary or null if not found</returns>
        public Dictionary<string, object> GetPoolMetrics(IPool pool)
        {
            if (pool == null || !_isEnabled || _isDisposed)
                return null;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return GetPoolMetricsById(poolId);
            }

            return null;
        }

        /// <summary>
        /// Records that a pool was shrunk.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that was shrunk</param>
        /// <param name="itemsRemoved">Number of items removed during shrinking</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordPoolShrinkById(FixedString64Bytes poolId, int itemsRemoved)
{
    if (poolId.Length == 0 || !_isEnabled || _isDisposed || itemsRemoved <= 0)
        return false;

    if (_poolsById.TryGetValue(poolId, out var info))
    {
        // Start profiler sample if available
        _profiler?.BeginSample("Shrink", poolId.ToGuid(), info.PoolName.ToString());

        // Record the shrink operation in metrics service if available
        if (_metrics != null)
        {
            // For IPoolMetrics (managed)
            if (_metrics is IPoolMetrics managedMetrics)
            {
                Guid guidPoolId = poolId.ToGuid();
                
                // The old capacity before shrinking
                int oldCapacity = info.Capacity;
                // The new capacity after shrinking
                int newCapacity = Math.Max(0, oldCapacity - itemsRemoved);
                
                // Record as a resize operation with a shrink context
                managedMetrics.RecordResize(guidPoolId, oldCapacity, newCapacity);
                
                // Update memory metrics if we have item size information
                if (info.ItemSizeBytes > 0)
                {
                    long memoryReduction = -(long)itemsRemoved * info.ItemSizeBytes;
                    managedMetrics.RecordCreate(guidPoolId, 0, memoryReduction);
                }
            }
            // For INativePoolMetrics (unmanaged)
            else if (_metrics is INativePoolMetrics nativeMetrics)
            {
                // Update capacity configuration for the native metrics
                int newCapacity = Math.Max(0, info.Capacity - itemsRemoved);
                nativeMetrics.UpdatePoolConfiguration(
                    poolId,
                    newCapacity,
                    info.MinSize,
                    info.MaxSize);
            }
        }

        // End profiler sample if available
        _profiler?.EndSample("Shrink", poolId.ToGuid(), info.PoolName.ToString(), info.ActiveCount,
            info.TotalCreated - info.ActiveCount - itemsRemoved);

        return true;
    }

    return false;
}

        /// <summary>
        /// Records that a pool was shrunk.
        /// </summary>
        /// <param name="pool">The pool that was shrunk</param>
        /// <param name="itemsRemoved">Number of items removed during shrinking</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordPoolShrink(IPool pool, int itemsRemoved)
        {
            if (pool == null || !_isEnabled || _isDisposed || itemsRemoved <= 0)
                return false;

            int hashCode = RuntimeHelpers.GetHashCode(pool);

            if (_poolGuidByHashCode.TryGetValue(hashCode, out FixedString64Bytes poolId))
            {
                return RecordPoolShrinkById(poolId, itemsRemoved);
            }

            // Auto-register pool if not found
            if (RegisterPool(pool))
            {
                return RecordPoolShrinkById(pool.Id.ToFixedString64Bytes(), itemsRemoved);
            }

            return false;
        }

        /// <summary>
        /// Records an item activity (acquire or release) for a pool.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="isAcquire">True if item was acquired, false if released</param>
        /// <param name="totalCount">Total count of items in the pool</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordItemActivity(FixedString64Bytes poolId, bool isAcquire, int totalCount)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                if (isAcquire)
                {
                    info.TotalAcquired++;
                    info.ActiveCount = totalCount;
                    info.PeakActiveCount = math.max(info.PeakActiveCount, totalCount);
                }
                else
                {
                    info.TotalReleased++;
                    info.ActiveCount = totalCount;
                }

                _poolsById[poolId] = info;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Records that a pool was reset.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that was reset</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordPoolReset(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                // Start profiler sample if available
                _profiler?.BeginSample("Reset", poolId.ToGuid(), info.PoolName.ToString());

                // Reset active count but preserve creation metrics
                int prevActiveCount = info.ActiveCount;
                info.ActiveCount = 0;
                info.TotalReleased += prevActiveCount; // All active items are implicitly released

                _poolsById[poolId] = info;

                // End profiler sample if available
                _profiler?.EndSample("Reset", poolId.ToGuid(), info.PoolName.ToString(), 0, info.TotalCreated);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records that a pool was disposed.
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool that was disposed</param>
        /// <returns>True if the operation was recorded successfully, false otherwise</returns>
        public bool RecordPoolDisposed(FixedString64Bytes poolId)
        {
            if (poolId.Length == 0 || !_isEnabled || _isDisposed)
                return false;

            if (_poolsById.TryGetValue(poolId, out var info))
            {
                // Log the disposal if logger is available
                _logger?.LogInfoInstance($"Pool {info.PoolName} ({poolId}) was disposed. " +
                                         $"Stats: Created={info.TotalCreated}, Acquired={info.TotalAcquired}, " +
                                         $"Released={info.TotalReleased}, Peak={info.PeakActiveCount}");

                // Remove the pool from tracking
                _poolsById.Remove(poolId);

                // Remove any hash code references to this pool ID
                foreach (var pair in _poolGuidByHashCode)
                {
                    if (pair.Value.Equals(poolId))
                    {
                        _poolGuidByHashCode.Remove(pair.Key);
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes pool statistics by gathering current metrics from all tracked pools.
        /// </summary>
        public void RefreshPoolStats()
        {
            if (!_isEnabled || _isDisposed)
                return;

            float currentTime = Time.realtimeSinceStartup;
            _lastRefreshTime = currentTime;

            // Call health checker to verify pools health if available
            _healthChecker?.CheckAllPools();
        }

        /// <summary>
        /// Resets metrics for all pools.
        /// </summary>
        public void ResetAllMetrics()
        {
            if (!_isEnabled || _isDisposed)
                return;

            foreach (var poolId in _poolsById.GetKeyArray(Allocator.Temp))
            {
                if (_poolsById.TryGetValue(poolId, out var info))
                {
                    info.PeakActiveCount = info.ActiveCount;
                    info.TotalAcquireTime = 0;
                    info.MaxAcquireTime = 0;
                    info.LastAcquireTime = 0;
                    info.MaxReleaseTimeMs = 0;
                    info.LastReleaseTimeMs = 0;
                    info.TotalReleaseTime = 0;

                    _poolsById[poolId] = info;
                }
            }

            _lastResetTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Checks if metrics need to be automatically reset based on time.
        /// </summary>
        public void CheckAutoReset()
        {
            if (!_isEnabled || _isDisposed)
                return;

            // This method could be called periodically to check if metrics should be reset
            // based on some time interval or other conditions
            float currentTime = Time.realtimeSinceStartup;

            // Example: Reset metrics every 60 seconds
            if (currentTime - _lastResetTime > 60f)
            {
                ResetAllMetrics();
            }
        }

        /// <summary>
        /// Gets pool metrics data as a NativeArray for efficient access by jobs.
        /// </summary>
        /// <param name="allocator">Allocator to use for the array</param>
        /// <returns>NativeArray containing pool metrics data</returns>
        public NativeArray<PoolMetricsData> GetPoolMetricsDataArray(Allocator allocator)
        {
            if (!_isEnabled || _isDisposed)
                return new NativeArray<PoolMetricsData>(0, allocator);

            var result = new NativeArray<PoolMetricsData>(_poolsById.Count(), allocator);
            int index = 0;

            foreach (var pair in _poolsById)
            {
                var info = pair.Value;
                result[index++] = new PoolMetricsData
                {
                    PoolId = info.PoolId, 
                    TotalCreatedCount = info.TotalCreated,
                    TotalAcquiredCount = info.TotalAcquired,
                    TotalReleasedCount = info.TotalReleased,
                    ActiveCount = info.ActiveCount,
                    PeakActiveCount = info.PeakActiveCount,
                    LastAcquireTimeMs = info.LastAcquireTime * 1000f,
                    LastReleaseTimeMs = info.LastReleaseTimeMs, 
                    MaxAcquireTimeMs = info.MaxAcquireTime * 1000f,
                    MaxReleaseTimeMs = info.MaxReleaseTimeMs, 
                    EstimatedItemSizeBytes = info.EstimatedItemSizeBytes
                };
            }

            return result;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Converts tracking info to a metrics dictionary.
        /// </summary>
        /// <param name="poolId">ID of the pool</param>
        /// <param name="info">Tracking information</param>
        /// <returns>Dictionary containing metrics data</returns>
        private Dictionary<string, object> GetMetricsFromInfo(FixedString64Bytes poolId, PoolTrackingInfo info)
        {
            // Calculate derived metrics
            float averageAcquireTime = info.TotalAcquired > 0
                ? info.TotalAcquireTime / info.TotalAcquired * 1000f // Convert to ms
                : 0f;

            float averageReleaseTime = info.TotalReleased > 0
                ? info.TotalReleaseTime / info.TotalReleased * 1000f // Convert to ms
                : 0f;

            int itemsInPool = info.TotalCreated - info.ActiveCount;
            long estimatedTotalMemoryBytes = (long)info.TotalCreated * info.EstimatedItemSizeBytes;

            // Create metrics dictionary
            var metrics = new Dictionary<string, object>
            {
                { "PoolId", poolId },
                { "PoolName", info.PoolName },
                { "TypeHash", info.TypeHash.ToString()},
                { "CreationTime", info.RegistrationTicks },
                { "TotalCreated", info.TotalCreated },
                { "TotalAcquired", info.TotalAcquired },
                { "TotalReleased", info.TotalReleased },
                { "ActiveCount", info.ActiveCount },
                { "PeakActiveCount", info.PeakActiveCount },
                { "ItemsInPool", itemsInPool },
                { "LastAcquireTimeMs", info.LastAcquireTime * 1000f },
                { "MaxAcquireTimeMs", info.MaxAcquireTime * 1000f },
                { "AverageAcquireTimeMs", averageAcquireTime },
                { "LastReleaseTimeMs", info.LastReleaseTimeMs * 1000f },
                { "MaxReleaseTimeMs", info.MaxReleaseTimeMs * 1000f },
                { "AverageReleaseTimeMs", averageReleaseTime },
                { "EstimatedItemSizeBytes", info.EstimatedItemSizeBytes },
                { "EstimatedTotalMemoryBytes", estimatedTotalMemoryBytes }
            };

            // Include health metrics if available
            if (_healthChecker != null)
            {
                var healthMetrics = _healthChecker.GetPoolHealth(poolId.ToGuid());
                if (healthMetrics != null)
                {
                    foreach (var kvp in healthMetrics)
                    {
                        metrics[$"Health_{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            return metrics;
        }

        #endregion
    }
}
        