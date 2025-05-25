using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Thread-safe writer for updating pool metrics from parallel jobs
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    public struct PoolMetricsParallelWriter
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* _metricsBuffer;
        
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* _globalMetricsBuffer;
        
        [NativeDisableUnsafePtrRestriction]
        internal unsafe AtomicSafetyHandle* _safety;
        
        internal FixedString64Bytes _poolId;
        internal bool _isAcquireOperation;
        internal int _activeCount;
        internal float _operationTimeMs;
        internal float _lifetimeSeconds;
        internal float _currentTime;
        
        /// <summary>
        /// Creates a parallel writer for the specified pool
        /// </summary>
        internal unsafe PoolMetricsParallelWriter(void* metricsBuffer, void* globalMetricsBuffer, AtomicSafetyHandle* safety, FixedString64Bytes poolId)
        {
            _metricsBuffer = metricsBuffer;
            _globalMetricsBuffer = globalMetricsBuffer;
            _safety = safety;
            _poolId = poolId;
            _isAcquireOperation = false;
            _activeCount = 0;
            _operationTimeMs = 0;
            _lifetimeSeconds = 0;
            _currentTime = 0;
        }
        
        /// <summary>
        /// Sets up an acquire operation with the specified parameters
        /// </summary>
        internal PoolMetricsParallelWriter PrepareAcquire(int activeCount, float acquireTimeMs, float currentTime)
        {
            PoolMetricsParallelWriter writer = this;
            writer._isAcquireOperation = true;
            writer._activeCount = activeCount;
            writer._operationTimeMs = acquireTimeMs;
            writer._currentTime = currentTime;
            return writer;
        }
        
        /// <summary>
        /// Sets up a release operation with the specified parameters
        /// </summary>
        internal PoolMetricsParallelWriter PrepareRelease(int activeCount, float releaseTimeMs, float lifetimeSeconds, float currentTime)
        {
            PoolMetricsParallelWriter writer = this;
            writer._isAcquireOperation = false;
            writer._activeCount = activeCount;
            writer._operationTimeMs = releaseTimeMs;
            writer._lifetimeSeconds = lifetimeSeconds;
            writer._currentTime = currentTime;
            return writer;
        }
    }
}