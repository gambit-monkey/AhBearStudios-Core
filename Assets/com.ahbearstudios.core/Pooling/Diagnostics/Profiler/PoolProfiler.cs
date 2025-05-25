using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Pooling.Diagnostics
{
    /// <summary>
    /// Profiles and reports on pool performance with Unity Profiler integration. Supports custom profiler markers
    /// for high-performance tracking and debugging of pool operations.
    /// Uses pool GUIDs as primary identifiers with names as secondary identifiers.
    /// </summary>
    public class PoolProfiler : IPoolProfiler
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly List<ProfileSample> _samples = new List<ProfileSample>();
        
        private readonly int _maxSamples;
        private readonly bool _enabled;
        private readonly bool _enableUnityProfilerMarkers;
        
        // Custom profiler markers for general pool operations
        private static readonly ProfilerMarker _poolAcquireMarker = new ProfilerMarker("Pool.Acquire");
        private static readonly ProfilerMarker _poolReleaseMarker = new ProfilerMarker("Pool.Release");
        private static readonly ProfilerMarker _poolCreateMarker = new ProfilerMarker("Pool.Create");
        private static readonly ProfilerMarker _poolClearMarker = new ProfilerMarker("Pool.Clear");
        private static readonly ProfilerMarker _poolExpandMarker = new ProfilerMarker("Pool.Expand");
        
        // Dictionary to store pool-specific markers
        private readonly Dictionary<string, ProfilerMarker> _customMarkers = new Dictionary<string, ProfilerMarker>();
        
        /// <summary>
        /// Creates a new profiler
        /// </summary>
        /// <param name="maxSamples">Maximum number of samples to store</param>
        /// <param name="enabled">Whether profiling is enabled</param>
        /// <param name="enableUnityProfilerMarkers">Whether Unity Profiler markers are enabled</param>
        public PoolProfiler(int maxSamples = 1000, bool enabled = true, bool enableUnityProfilerMarkers = true)
        {
            _maxSamples = maxSamples;
            _enabled = enabled;
            _enableUnityProfilerMarkers = enableUnityProfilerMarkers;
        }
        
        /// <summary>
        /// Begins profiling an operation
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        public void BeginSample(string operationType)
        {
            if (!_enabled) return;
            
            _stopwatch.Restart();
            
            if (_enableUnityProfilerMarkers)
            {
                GetGenericProfilerMarker(operationType).Begin();
            }
        }
        
        /// <summary>
        /// Begins profiling an operation for a specific pool
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool (for human readability)</param>
        public void BeginSample(string operationType, Guid poolId, string poolName = null)
        {
            if (!_enabled) return;
            
            _stopwatch.Restart();
            
            if (_enableUnityProfilerMarkers)
            {
                GetProfilerMarker(operationType, poolId, poolName).Begin();
            }
        }
        
        /// <summary>
        /// Begins profiling an operation using only pool name (legacy support)
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolName">Name of the pool</param>
        public void BeginSampleByName(string operationType, string poolName = null)
        {
            if (!_enabled) return;
            
            _stopwatch.Restart();
            
            if (_enableUnityProfilerMarkers)
            {
                GetProfilerMarkerByName(operationType, poolName).Begin();
            }
        }
        
        /// <summary>
        /// Ends profiling an operation and records the sample
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        public void EndSample(string operationType, Guid poolId, string poolName, int activeCount, int freeCount)
        {
            if (!_enabled) return;
            
            _stopwatch.Stop();
            
            var sample = new ProfileSample
            {
                PoolName = poolName,
                PoolId = poolId,
                OperationType = operationType,
                ElapsedTicks = _stopwatch.ElapsedTicks,
                ActiveCount = activeCount,
                FreeCount = freeCount,
                Time = Time.realtimeSinceStartup
            };
            
            // Add sample, maintaining max size
            _samples.Add(sample);
            if (_samples.Count > _maxSamples)
            {
                _samples.RemoveAt(0);
            }
            
            if (_enableUnityProfilerMarkers)
            {
                GetProfilerMarker(operationType, poolId, poolName).End();
            }
        }
        
        /// <summary>
        /// Ends profiling an operation using only pool name (legacy support)
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        public void EndSampleByName(string operationType, string poolName, int activeCount, int freeCount)
        {
            if (!_enabled) return;
            
            _stopwatch.Stop();
            
            var sample = new ProfileSample
            {
                PoolName = poolName,
                PoolId = Guid.Empty, // Empty GUID for name-only samples
                OperationType = operationType,
                ElapsedTicks = _stopwatch.ElapsedTicks,
                ActiveCount = activeCount,
                FreeCount = freeCount,
                Time = Time.realtimeSinceStartup
            };
            
            // Add sample, maintaining max size
            _samples.Add(sample);
            if (_samples.Count > _maxSamples)
            {
                _samples.RemoveAt(0);
            }
            
            if (_enableUnityProfilerMarkers)
            {
                GetProfilerMarkerByName(operationType, poolName).End();
            }
        }
        
        /// <summary>
        /// Gets operations that exceed the specified duration threshold
        /// </summary>
        /// <param name="thresholdMs">Threshold in milliseconds</param>
        /// <returns>List of slow operations</returns>
        public List<ProfileSample> GetSlowOperations(float thresholdMs)
        {
            if (_samples.Count == 0) return new List<ProfileSample>();
    
            var slowOps = new List<ProfileSample>();
    
            foreach (var sample in _samples)
            {
                if (sample.ElapsedMilliseconds >= thresholdMs)
                {
                    slowOps.Add(sample);
                }
            }
    
            return slowOps;
        }
        
        /// <summary>
        /// Wraps an action with profiling. Useful for profiling complete pool operations.
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <param name="action">Action to profile</param>
        public void SampleAction(string operationType, Guid poolId, string poolName, int activeCount, int freeCount, System.Action action)
        {
            if (!_enabled || action == null) 
            {
                action?.Invoke();
                return;
            }
            
            BeginSample(operationType, poolId, poolName);
            try
            {
                action.Invoke();
            }
            finally
            {
                EndSample(operationType, poolId, poolName, activeCount, freeCount);
            }
        }
        
        /// <summary>
        /// Gets the appropriate profiler marker for the operation (GUID-based)
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>ProfilerMarker for the operation</returns>
        private ProfilerMarker GetProfilerMarker(string operationType, Guid poolId, string poolName)
        {
            // Use a generic marker if no specific pool ID is provided
            if (poolId == Guid.Empty)
            {
                return GetGenericProfilerMarker(operationType);
            }
            
            // Create a specific marker for this pool/operation combination
            // Use a shorter prefix of the GUID to keep marker names readable
            string guidPrefix = poolId.ToString().Substring(0, 8);
            string markerName = $"Pool.{guidPrefix}.{operationType}";
            
            if (!_customMarkers.TryGetValue(markerName, out ProfilerMarker marker))
            {
                marker = new ProfilerMarker(markerName);
                _customMarkers[markerName] = marker;
            }
            
            return marker;
        }
        
        /// <summary>
        /// Gets the appropriate profiler marker for the operation (name-based)
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>ProfilerMarker for the operation</returns>
        private ProfilerMarker GetProfilerMarkerByName(string operationType, string poolName)
        {
            // Use a generic marker if no specific pool name is provided
            if (string.IsNullOrEmpty(poolName))
            {
                return GetGenericProfilerMarker(operationType);
            }
            
            // Create a specific marker for this pool/operation combination
            string markerName = $"Pool.{poolName}.{operationType}";
            
            if (!_customMarkers.TryGetValue(markerName, out ProfilerMarker marker))
            {
                marker = new ProfilerMarker(markerName);
                _customMarkers[markerName] = marker;
            }
            
            return marker;
        }
        
        /// <summary>
        /// Gets a generic profiler marker for an operation type
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <returns>Generic ProfilerMarker for the operation type</returns>
        private ProfilerMarker GetGenericProfilerMarker(string operationType)
        {
            switch (operationType)
            {
                case "Acquire": return _poolAcquireMarker;
                case "Release": return _poolReleaseMarker;
                case "Create": return _poolCreateMarker;
                case "Clear": return _poolClearMarker;
                case "Expand": return _poolExpandMarker;
                default: return _poolAcquireMarker; // default
            }
        }
        
        /// <summary>
        /// Creates a standalone profiler marker
        /// </summary>
        /// <param name="name">Marker name</param>
        /// <returns>ProfilerMarker</returns>
        public ProfilerMarker CreateMarker(string name)
        {
            string markerName = $"Pool.Custom.{name}";
            
            if (!_customMarkers.TryGetValue(markerName, out ProfilerMarker marker))
            {
                marker = new ProfilerMarker(markerName);
                _customMarkers[markerName] = marker;
            }
            
            return marker;
        }
        
        /// <summary>
        /// Creates a disposable profiling scope that automatically begins timing when created
        /// and ends when disposed. Designed to be used with the 'using' statement.
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <returns>A disposable struct that ends the profiling sample when disposed</returns>
        public ProfilerSampleScope Sample(string operationType)
        {
            if (!_enabled) return default;
            
            return new ProfilerSampleScope(this, operationType);
        }
        
        /// <summary>
        /// Creates a disposable profiling scope for a specific pool that automatically begins timing
        /// when created and ends when disposed. Designed to be used with the 'using' statement.
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>A disposable struct that ends the profiling sample when disposed</returns>
        public ProfilerSampleScope Sample(string operationType, Guid poolId, string poolName, int activeCount, int freeCount)
        {
            if (!_enabled) return default;
            
            return new ProfilerSampleScope(this, operationType, poolId, poolName, activeCount, freeCount);
        }
        
        /// <summary>
        /// Creates a disposable profiling scope using only pool name (legacy support)
        /// </summary>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>A disposable struct that ends the profiling sample when disposed</returns>
        public ProfilerSampleScope SampleByName(string operationType, string poolName, int activeCount, int freeCount)
        {
            if (!_enabled) return default;
            
            return new ProfilerSampleScope(this, operationType, poolName, activeCount, freeCount);
        }
        
        /// <summary>
        /// Gets all samples
        /// </summary>
        /// <returns>List of samples</returns>
        public List<ProfileSample> GetSamples()
        {
            return new List<ProfileSample>(_samples);
        }
        
        /// <summary>
        /// Gets stats for operations by type
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <returns>Min, max, and average times in milliseconds</returns>
        public (float min, float max, float avg) GetOperationStats(string operationType)
        {
            if (_samples.Count == 0) return (0, 0, 0);
            
            float min = float.MaxValue;
            float max = 0;
            float sum = 0;
            int count = 0;
            
            foreach (var sample in _samples)
            {
                if (sample.OperationType == operationType)
                {
                    float ms = sample.ElapsedMilliseconds;
                    min = Mathf.Min(min, ms);
                    max = Mathf.Max(max, ms);
                    sum += ms;
                    count++;
                }
            }
            
            if (count == 0) return (0, 0, 0);
            return (min, max, sum / count);
        }
        
        /// <summary>
        /// Gets stats for a specific pool by ID
        /// </summary>
        /// <param name="poolId">Unique identifier of the pool</param>
        /// <returns>Min, max, and average times in milliseconds across all operations</returns>
        public (float min, float max, float avg) GetPoolStats(Guid poolId)
        {
            if (_samples.Count == 0) return (0, 0, 0);
            
            float min = float.MaxValue;
            float max = 0;
            float sum = 0;
            int count = 0;
            
            foreach (var sample in _samples)
            {
                if (sample.PoolId == poolId)
                {
                    float ms = sample.ElapsedMilliseconds;
                    min = Mathf.Min(min, ms);
                    max = Mathf.Max(max, ms);
                    sum += ms;
                    count++;
                }
            }
            
            if (count == 0) return (0, 0, 0);
            return (min, max, sum / count);
        }
        
        /// <summary>
        /// Gets stats for a specific pool by name (legacy support)
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <returns>Min, max, and average times in milliseconds across all operations</returns>
        public (float min, float max, float avg) GetPoolStatsByName(string poolName)
        {
            if (_samples.Count == 0) return (0, 0, 0);
            
            float min = float.MaxValue;
            float max = 0;
            float sum = 0;
            int count = 0;
            
            foreach (var sample in _samples)
            {
                if (sample.PoolName == poolName)
                {
                    float ms = sample.ElapsedMilliseconds;
                    min = Mathf.Min(min, ms);
                    max = Mathf.Max(max, ms);
                    sum += ms;
                    count++;
                }
            }
            
            if (count == 0) return (0, 0, 0);
            return (min, max, sum / count);
        }
        
        /// <summary>
        /// Clears all samples
        /// </summary>
        public void ClearSamples()
        {
            _samples.Clear();
        }
    }
}