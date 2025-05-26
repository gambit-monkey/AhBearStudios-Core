using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Sessions
{
    /// <summary>
    /// A specialized profiler session for pool operations that captures additional pool metrics
    /// </summary>
    public class PoolProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly RuntimeProfilerManager _manager;
        private readonly Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        
        /// <summary>
        /// Pool identifier
        /// </summary>
        public readonly Guid PoolId;
        
        /// <summary>
        /// Pool name
        /// </summary>
        public readonly string PoolName;
        
        /// <summary>
        /// Number of active items at the time of profiling
        /// </summary>
        public readonly int ActiveCount;
        
        /// <summary>
        /// Number of free items at the time of profiling
        /// </summary>
        public readonly int FreeCount;
        
        /// <summary>
        /// The pool metrics interface for recording metrics
        /// </summary>
        private readonly IPoolMetrics _poolMetrics;
        
        /// <summary>
        /// The operation type being profiled
        /// </summary>
        private readonly string _operationType;
        
        /// <summary>
        /// Creates a new pool profiler session
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Active item count</param>
        /// <param name="freeCount">Free item count</param>
        /// <param name="poolMetrics">Pool metrics interface for recording</param>
        /// <param name="manager">Runtime profiler manager</param>
        public PoolProfilerSession(
            ProfilerTag tag, 
            Guid poolId, 
            string poolName, 
            int activeCount, 
            int freeCount,
            IPoolMetrics poolMetrics,
            RuntimeProfilerManager manager = null)
        {
            _tag = tag;
            PoolId = poolId;
            PoolName = poolName;
            ActiveCount = activeCount;
            FreeCount = freeCount;
            _poolMetrics = poolMetrics;
            _manager = manager;
            _isDisposed = false;
            _operationType = GetOperationTypeFromTag(tag.Name);
            _marker = new ProfilerMarker(_tag.FullName);
            
            // Begin the profiler marker
            _marker.Begin();
            _startTimeNs = GetHighPrecisionTimestampNs();
            
            // Notify manager that session started (if provided)
            _manager?.OnSessionStarted(this);
        }

        /// <summary>
        /// Get the tag associated with this session
        /// </summary>
        public ProfilerTag Tag => _tag;
        
        /// <summary>
        /// Gets the elapsed time in milliseconds
        /// </summary>
        public double ElapsedMilliseconds
        {
            get
            {
                long currentTimeNs = _isDisposed ? _endTimeNs : GetHighPrecisionTimestampNs();
                return (currentTimeNs - _startTimeNs) / 1000000.0;
            }
        }
        
        /// <summary>
        /// Gets the elapsed time in nanoseconds
        /// </summary>
        public long ElapsedNanoseconds
        {
            get
            {
                return _isDisposed ? (_endTimeNs - _startTimeNs) : (GetHighPrecisionTimestampNs() - _startTimeNs);
            }
        }
        
        /// <summary>
        /// Indicates if this session has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Records a custom metric with this session
        /// </summary>
        public void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _customMetrics[metricName] = value;
        }
        
        /// <summary>
        /// Gets a dictionary of all custom metrics recorded with this session
        /// </summary>
        public IReadOnlyDictionary<string, double> CustomMetrics => _customMetrics;

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _marker.End();
            _endTimeNs = GetHighPrecisionTimestampNs();
            _isDisposed = true;
            
            // Record pool-specific metrics
            RecordPoolMetrics();
            
            // Notify manager that session ended (if provided)
            _manager?.OnSessionEnded(this, ElapsedMilliseconds);
        }
        
        /// <summary>
        /// Record metrics specific to pool operations
        /// </summary>
        private void RecordPoolMetrics()
        {
            // Only record metrics if we have a pool ID and metrics system
            if (PoolId != Guid.Empty && _poolMetrics != null)
            {
                // Record appropriate metrics based on operation type
                switch (_operationType.ToLowerInvariant())
                {
                    case "acquire":
                        _poolMetrics.RecordAcquire(PoolId, ActiveCount, (float)ElapsedMilliseconds);
                        break;
                    case "release":
                        _poolMetrics.RecordRelease(PoolId, ActiveCount, (float)ElapsedMilliseconds);
                        break;
                    case "create":
                        _poolMetrics.RecordCreate(PoolId, FreeCount);
                        break;
                    case "expand":
                        // We don't have old capacity here, so just record the resize
                        _poolMetrics.RecordResize(PoolId, ActiveCount + FreeCount, ActiveCount + FreeCount, (float)ElapsedMilliseconds);
                        break;
                    case "shrink":
                        // Similar to expand
                        _poolMetrics.RecordResize(PoolId, ActiveCount + FreeCount, ActiveCount + FreeCount, (float)ElapsedMilliseconds);
                        break;
                }
                
                // Update pool configuration
                _poolMetrics.UpdatePoolConfiguration(PoolId, ActiveCount + FreeCount);
            }
        }
        
        /// <summary>
        /// Extract operation type from a tag name
        /// </summary>
        private string GetOperationTypeFromTag(string tagName)
        {
            // Extract operation type (after the last dot)
            int lastDot = tagName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < tagName.Length - 1)
            {
                return tagName.Substring(lastDot + 1);
            }
            return tagName;
        }
        
        /// <summary>
        /// Gets high precision timestamp in nanoseconds
        /// </summary>
        private static long GetHighPrecisionTimestampNs()
        {
            long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            long frequency = System.Diagnostics.Stopwatch.Frequency;
            return (long)((double)timestamp / frequency * 1_000_000_000);
        }
    }
}