using System;
using System.Diagnostics;

namespace AhBearStudios.Core.Pooling.Diagnostics
{
    /// <summary>
    /// Sample data for profiling with pool identification using both GUID and name
    /// </summary>
    public struct ProfileSample
    {
        /// <summary>
        /// Human-readable name of the pool
        /// </summary>
        public string PoolName;
        
        /// <summary>
        /// Unique identifier of the pool
        /// </summary>
        public Guid PoolId;
        
        /// <summary>
        /// Type of operation being profiled
        /// </summary>
        public string OperationType;
        
        /// <summary>
        /// Elapsed time in ticks
        /// </summary>
        public long ElapsedTicks;
        
        /// <summary>
        /// Number of active items at sampling time
        /// </summary>
        public int ActiveCount;
        
        /// <summary>
        /// Number of free items at sampling time
        /// </summary>
        public int FreeCount;
        
        /// <summary>
        /// Time when sample was taken
        /// </summary>
        public float Time;
        
        /// <summary>
        /// Converts elapsed ticks to milliseconds for human-readable timing
        /// </summary>
        public float ElapsedMilliseconds => ElapsedTicks / (float)Stopwatch.Frequency * 1000f;
    }
    
    /// <summary>
    /// Disposable struct for profiling operations with using statement.
    /// Implemented as a struct for better performance and compatibility with Unity Jobs/Burst.
    /// </summary>
    public readonly struct ProfilerSampleScope : IDisposable
    {
        // Using readonly fields for thread safety and better performance
        private readonly IPoolProfiler _profiler;
        private readonly string _operationType;
        private readonly string _poolName;
        private readonly Guid _poolId;
        private readonly int _activeCount;
        private readonly int _freeCount;
        private readonly bool _hasPoolInfo;
        private readonly bool _enabled;
        
        /// <summary>
        /// Creates a new profiler sample scope without pool-specific information
        /// </summary>
        /// <param name="profiler">Profiler to use</param>
        /// <param name="operationType">Type of operation being profiled</param>
        public ProfilerSampleScope(IPoolProfiler profiler, string operationType)
        {
            _profiler = profiler;
            _operationType = operationType;
            _poolName = null;
            _poolId = Guid.Empty;
            _activeCount = 0;
            _freeCount = 0;
            _hasPoolInfo = false;
            _enabled = profiler != null;
            
            if (_enabled)
            {
                _profiler.BeginSample(_operationType);
            }
        }

        /// <summary>
        /// Creates a new profiler sample scope with pool-specific information
        /// </summary>
        /// <param name="profiler">Profiler to use</param>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        public ProfilerSampleScope(IPoolProfiler profiler, string operationType, Guid poolId, string poolName, int activeCount, int freeCount)
        {
            _profiler = profiler;
            _operationType = operationType;
            _poolName = poolName;
            _poolId = poolId;
            _activeCount = activeCount;
            _freeCount = freeCount;
            _hasPoolInfo = true;
            _enabled = profiler != null;
            
            if (_enabled)
            {
                _profiler.BeginSample(_operationType, _poolId, _poolName);
            }
        }
        
        /// <summary>
        /// Creates a new profiler sample scope with pool name only (for compatibility)
        /// </summary>
        /// <param name="profiler">Profiler to use</param>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        public ProfilerSampleScope(IPoolProfiler profiler, string operationType, string poolName, int activeCount, int freeCount)
        {
            _profiler = profiler;
            _operationType = operationType;
            _poolName = poolName;
            _poolId = Guid.Empty;
            _activeCount = activeCount;
            _freeCount = freeCount;
            _hasPoolInfo = true;
            _enabled = profiler != null;
            
            if (_enabled)
            {
                _profiler.BeginSampleByName(_operationType, _poolName);
            }
        }

        /// <summary>
        /// Ends the profiler sample when the scope is disposed
        /// </summary>
        public void Dispose()
        {
            if (!_enabled) return;
            
            if (_hasPoolInfo)
            {
                if (_poolId != Guid.Empty)
                {
                    _profiler.EndSample(_operationType, _poolId, _poolName, _activeCount, _freeCount);
                }
                else
                {
                    _profiler.EndSampleByName(_operationType, _poolName, _activeCount, _freeCount);
                }
            }
            else
            {
                _profiler.EndSample(_operationType, Guid.Empty, "Unknown", 0, 0);
            }
        }
    }
}