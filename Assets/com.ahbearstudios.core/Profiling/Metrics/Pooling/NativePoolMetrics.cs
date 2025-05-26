using System;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace AhBearStudios.Core.Profiling.Metrics
{
    /// <summary>
    /// Native implementation of pool metrics tracking for use with Burst and Jobs
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public struct NativePoolMetrics : INativePoolMetrics
    {
        // Safety
        [NativeDisableUnsafePtrRestriction]
        private AtomicSafetyHandle m_Safety;
        [NativeSetThreadIndex]
        private int _threadIndex;
        
        // Storage
        private UnsafeParallelHashMap<FixedString64Bytes, PoolMetricsData> _poolMetrics;
        private NativeReference<PoolMetricsData> _globalMetrics;
        
        // State
        private Allocator _allocatorLabel;
        private bool _isCreated;
        
        // Constructors
        /// <summary>
        /// Creates a new native pool metrics tracker
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for the number of pools to track</param>
        /// <param name="allocator">Allocator to use</param>
        public NativePoolMetrics(int initialCapacity, Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Invalid allocator", nameof(allocator));
        
            _allocatorLabel = allocator;
            _threadIndex = 0;
    
            // Create containers
            _poolMetrics = new UnsafeParallelHashMap<FixedString64Bytes, PoolMetricsData>(initialCapacity, allocator);
            _globalMetrics = new NativeReference<PoolMetricsData>(allocator);
    
            // Set up safety
            m_Safety = AtomicSafetyHandle.Create();
            _isCreated = true;
    
            // Initialize global metrics - after all fields are initialized
            float currentTime = GetCurrentTime(); // Use a static method instead
            var globalMetricsData = new PoolMetricsData(default, new FixedString128Bytes("Global"));
            globalMetricsData.PoolId = new FixedString64Bytes("Global");
            globalMetricsData.PoolType = new FixedString64Bytes("Global");
            globalMetricsData.CreationTime = currentTime;
            _globalMetrics.Value = globalMetricsData;
        }

// Add a static version of GetTime for use in the constructor
        private static float GetCurrentTime()
        {
#if UNITY_2019_3_OR_NEWER
            return UnityEngine.Time.time;
#else
    return (float)DateTime.Now.TimeOfDay.TotalSeconds;
#endif
        }
        
        /// <summary>
        /// Creates a new native pool metrics tracker with default initial capacity
        /// </summary>
        /// <param name="allocator">Allocator to use</param>
        public NativePoolMetrics(Allocator allocator) : this(64, allocator) { }
        
        // INativePoolMetrics Implementation
        
        /// <summary>
        /// Gets metrics data for a specific pool
        /// </summary>
        public PoolMetricsData GetMetricsData(FixedString64Bytes poolId)
        {
            CheckReadAccess();
            
            if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                return metricsData;
                
            return default;
        }
        
        /// <summary>
        /// Gets global metrics data aggregated across all pools
        /// </summary>
        public PoolMetricsData GetGlobalMetricsData()
        {
            CheckReadAccess();
            return _globalMetrics.Value;
        }
        
        /// <summary>
        /// Records an acquire operation for a pool
        /// </summary>
        public JobHandle RecordAcquire(FixedString64Bytes poolId, int activeCount, float acquireTimeMs, JobHandle dependencies = default)
        {
            CheckWriteAccess();
            
            var writer = CreateParallelWriter(poolId);
            writer = writer.PrepareAcquire(activeCount, acquireTimeMs, GetTime());
            
            var job = new UpdatePoolMetricsJob { Writer = writer };
            return job.Schedule(dependencies);
        }
        
        /// <summary>
        /// Records a release operation for a pool
        /// </summary>
        public JobHandle RecordRelease(FixedString64Bytes poolId, int activeCount, float releaseTimeMs, float lifetimeSeconds = 0, JobHandle dependencies = default)
        {
            CheckWriteAccess();
            
            var writer = CreateParallelWriter(poolId);
            writer = writer.PrepareRelease(activeCount, releaseTimeMs, lifetimeSeconds, GetTime());
            
            var job = new UpdatePoolMetricsJob { Writer = writer };
            return job.Schedule(dependencies);
        }
        
        /// <summary>
        /// Updates pool capacity and configuration
        /// </summary>
        public void UpdatePoolConfiguration(FixedString64Bytes poolId, int capacity, int minCapacity = 0, int maxCapacity = 0, FixedString64Bytes poolType = default, int itemSizeBytes = 0)
        {
            CheckWriteAccess();
            
            // Get existing or create new metrics data
            PoolMetricsData metricsData;
            bool isNewPool = false;
            
            if (!_poolMetrics.TryGetValue(poolId, out metricsData))
            {
                metricsData = new PoolMetricsData(poolId, new FixedString128Bytes(poolId));
                metricsData.CreationTime = GetTime();
                isNewPool = true;
            }
            
            // Update configuration values
            if (capacity > 0)
            {
                // Only set InitialCapacity for new pools
                if (isNewPool)
                    metricsData.InitialCapacity = capacity;
                
                metricsData = metricsData.WithCapacity(capacity);
            }
            
            if (minCapacity > 0)
                metricsData.MinCapacity = minCapacity;
                
            if (maxCapacity > 0)
                metricsData.MaxCapacity = maxCapacity;
                
            if (!poolType.IsEmpty)
                metricsData.PoolType = poolType;
                
            if (itemSizeBytes > 0)
                metricsData = metricsData.WithItemSize(itemSizeBytes);
                
            // Store updated metrics
            if (isNewPool)
                _poolMetrics.Add(poolId, metricsData);
            else
            {
                _poolMetrics.Remove(poolId);
                _poolMetrics.Add(poolId, metricsData);
            }
            
            // Update global metrics
            UpdateGlobalMetrics();
        }
        
        /// <summary>
        /// Gets metrics data for all tracked pools
        /// </summary>
        public NativeArray<PoolMetricsData> GetAllPoolMetrics(Allocator allocator)
        {
            CheckReadAccess();
            
            var result = new NativeArray<PoolMetricsData>(_poolMetrics.Count(), allocator);
            
            int index = 0;
            var keyValueArrays = _poolMetrics.GetKeyValueArrays(Allocator.Temp);
            
            for (int i = 0; i < keyValueArrays.Length; i++)
            {
                result[index++] = keyValueArrays.Values[i];
            }
            
            keyValueArrays.Dispose();
            return result;
        }
        
        /// <summary>
        /// Reset statistics for a specific pool
        /// </summary>
        public void ResetPoolStats(FixedString64Bytes poolId)
        {
            CheckWriteAccess();
            
            if (_poolMetrics.TryGetValue(poolId, out var metricsData))
            {
                var resetMetrics = metricsData.Reset(GetTime());
                _poolMetrics.Remove(poolId);
                _poolMetrics.Add(poolId, resetMetrics);
                
                // Update global metrics
                UpdateGlobalMetrics();
            }
        }
        
        /// <summary>
        /// Reset statistics for all pools
        /// </summary>
        public void ResetAllPoolStats()
        {
            CheckWriteAccess();
            
            float currentTime = GetTime();
            var keys = _poolMetrics.GetKeyArray(Allocator.Temp);
            
            for (int i = 0; i < keys.Length; i++)
            {
                var poolId = keys[i];
                if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                {
                    var resetMetrics = metricsData.Reset(currentTime);
                    _poolMetrics.Remove(poolId);
                    _poolMetrics.Add(poolId, resetMetrics);
                }
            }
            
            keys.Dispose();
            
            // Reset global metrics
            var globalMetrics = _globalMetrics.Value;
            _globalMetrics.Value = globalMetrics.Reset(currentTime);
        }
        
        /// <summary>
        /// Gets the cache hit ratio for a specific pool
        /// </summary>
        public float GetPoolHitRatio(FixedString64Bytes poolId)
        {
            CheckReadAccess();
            
            if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                return metricsData.CacheHitRatio;
                
            return 0;
        }
        
        /// <summary>
        /// Gets the overall efficiency for a specific pool
        /// </summary>
        public float GetPoolEfficiency(FixedString64Bytes poolId)
        {
            CheckReadAccess();
            
            if (_poolMetrics.TryGetValue(poolId, out var metricsData))
                return metricsData.PoolEfficiency;
                
            return 0;
        }
        
        /// <summary>
        /// The allocator used by this container
        /// </summary>
        public Allocator Allocator => _allocatorLabel;
        
        /// <summary>
        /// Whether the native container is created
        /// </summary>
        public bool IsCreated => _isCreated;
        
        // Helper methods
        
        /// <summary>
        /// Creates a parallel writer for the specified pool
        /// </summary>
        private unsafe PoolMetricsParallelWriter CreateParallelWriter(FixedString64Bytes poolId)
        {
            if (!_poolMetrics.ContainsKey(poolId))
            {
                // Initialize metrics data for this pool if it doesn't exist
                var poolMetricsData = new PoolMetricsData(poolId, new FixedString128Bytes(poolId))
                {
                    CreationTime = GetTime()
                };
                _poolMetrics.Add(poolId, poolMetricsData);
            }
    
            fixed (AtomicSafetyHandle* safetyPtr = &m_Safety)
            {
                return new PoolMetricsParallelWriter(
                    UnsafeUtility.AddressOf(ref _poolMetrics),
                    UnsafeUtility.AddressOf(ref _globalMetrics),
                    safetyPtr,
                    poolId);
            }
        }
        
        /// <summary>
        /// Updates global metrics by aggregating all pool metrics
        /// </summary>
        private void UpdateGlobalMetrics()
        {
            var globalMetrics = _globalMetrics.Value;
            float currentTime = GetTime();
            
            // Reset counters we're going to recalculate
            globalMetrics.ActiveCount = 0;
            globalMetrics.Capacity = 0;
            globalMetrics.TotalCreatedCount = 0;
            globalMetrics.TotalAcquiredCount = 0;
            globalMetrics.TotalReleasedCount = 0;
            globalMetrics.TotalMemoryBytes = 0;
            globalMetrics.OverflowAllocations = 0;
            globalMetrics.TotalResizeOperations = 0;
            
            // Update time metrics
            globalMetrics.LastOperationTime = currentTime;
            globalMetrics.UpTimeSeconds = currentTime - globalMetrics.CreationTime;
            
            // Aggregate metrics from all pools
            var keys = _poolMetrics.GetKeyArray(Allocator.Temp);
            
            for (int i = 0; i < keys.Length; i++)
            {
                var poolId = keys[i];
                if (_poolMetrics.TryGetValue(poolId, out var poolMetrics))
                {
                    // Aggregate capacity and counts
                    globalMetrics.ActiveCount += poolMetrics.ActiveCount;
                    globalMetrics.Capacity += poolMetrics.Capacity;
                    globalMetrics.TotalCreatedCount += poolMetrics.TotalCreatedCount;
                    globalMetrics.TotalAcquiredCount += poolMetrics.TotalAcquiredCount;
                    globalMetrics.TotalReleasedCount += poolMetrics.TotalReleasedCount;
                    globalMetrics.TotalMemoryBytes += poolMetrics.TotalMemoryBytes;
                    globalMetrics.OverflowAllocations += poolMetrics.OverflowAllocations;
                    globalMetrics.TotalResizeOperations += poolMetrics.TotalResizeOperations;
                    
                    // Update peak metrics
                    globalMetrics.PeakActiveCount = Math.Max(globalMetrics.PeakActiveCount, globalMetrics.ActiveCount);
                    globalMetrics.PeakMemoryBytes = Math.Max(globalMetrics.PeakMemoryBytes, globalMetrics.TotalMemoryBytes);
                    
                    // Aggregate cache metrics
                    globalMetrics.CacheHits += poolMetrics.CacheHits;
                    globalMetrics.CacheMisses += poolMetrics.CacheMisses;
                }
            }
            
            keys.Dispose();
            
            // Update calculated metrics
            if (globalMetrics.Capacity > 0)
                globalMetrics.FragmentationRatio = 1.0f - (float)globalMetrics.ActiveCount / globalMetrics.Capacity;
                
            // Store updated global metrics
            _globalMetrics.Value = globalMetrics;
        }
        
        /// <summary>
        /// Checks if the container has read access
        /// </summary>
        private void CheckReadAccess()
        {
            if (!_isCreated)
                throw new ObjectDisposedException("NativePoolMetrics");
                
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }
        
        /// <summary>
        /// Checks if the container has write access
        /// </summary>
        private void CheckWriteAccess()
        {
            if (!_isCreated)
                throw new ObjectDisposedException("NativePoolMetrics");
                
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }
        
        // IDisposable Implementation
        
        /// <summary>
        /// Disposes the native containers and releases unmanaged memory
        /// </summary>
        public void Dispose()
        {
            if (!_isCreated)
                return;
                
            // Check if this is being disposed from a job
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            
            // Dispose containers
            if (_poolMetrics.IsCreated)
                _poolMetrics.Dispose();
                
            if (_globalMetrics.IsCreated)
                _globalMetrics.Dispose();
                
            // Clean up safety handle
            AtomicSafetyHandle.Release(m_Safety);
            
            _isCreated = false;
        }
        
        /// <summary>
        /// Gets the current time in seconds
        /// </summary>
        private float GetTime()
        {
#if UNITY_2019_3_OR_NEWER
            return UnityEngine.Time.time;
#else
            return (float)DateTime.Now.TimeOfDay.TotalSeconds;
#endif
        }
    }
    
    // Extension methods for UpdatePoolMetricsJob
    [BurstCompile]
    internal static class NativePoolMetricsExtensions
    {
        /// <summary>
        /// Executes metrics update for a pool
        /// </summary>
        [BurstCompile]
        internal static unsafe void ExecuteUpdate(ref UpdatePoolMetricsJob job)
        {
            // Get references to the data
            var poolId = job.Writer._poolId;
            var metricsMapPtr = (UnsafeParallelHashMap<FixedString64Bytes, PoolMetricsData>*)job.Writer._metricsBuffer;
            var globalMetricsPtr = (NativeReference<PoolMetricsData>*)job.Writer._globalMetricsBuffer;
            
            if (!metricsMapPtr->TryGetValue(poolId, out var metricsData))
                return;
                
            // Update the metrics
            if (job.Writer._isAcquireOperation)
            {
                metricsData = metricsData.RecordAcquire(
                    job.Writer._activeCount,
                    job.Writer._operationTimeMs,
                    job.Writer._currentTime);
            }
            else
            {
                metricsData = metricsData.RecordRelease(
                    job.Writer._activeCount,
                    job.Writer._lifetimeSeconds,
                    job.Writer._operationTimeMs,
                    job.Writer._currentTime);
            }
            
            // Write back the updated metrics
            metricsMapPtr->Remove(poolId);
            metricsMapPtr->Add(poolId, metricsData);
            
            // Update global metrics (simplified, non-comprehensive update)
            var globalMetrics = globalMetricsPtr->Value;
            
            // Update active counts and time
            globalMetrics.LastOperationTime = job.Writer._currentTime;
            
            if (job.Writer._isAcquireOperation)
                globalMetrics.TotalAcquiredCount++;
            else
                globalMetrics.TotalReleasedCount++;
                
            // We do a simplified global update here - a full update is done in the main thread
            globalMetricsPtr->Value = globalMetrics;
        }
    }
}