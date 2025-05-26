using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.Pooling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Pooling.Pools.Native;
using AhBearStudios.Core.Utilities;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Sessions;
using AhBearStudios.Core.Profiling.Tagging;
using AhBearStudios.Core.Profiling.Messages;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Specialized profiler for pool operations that captures pool-specific metrics
    /// </summary>
    public class PoolProfiler : IProfiler
    {
        private readonly IProfiler _baseProfiler;
        private readonly IPoolMetrics _poolMetrics;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<Guid, PoolMetricsData> _poolMetricsCache = new Dictionary<Guid, PoolMetricsData>();
        private readonly int _maxHistoryItems = 100;
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<Guid, Dictionary<string, double>> _poolMetricAlerts = new Dictionary<Guid, Dictionary<string, double>>();

        /// <summary>
        /// Gets whether profiling is enabled
        /// </summary>
        public bool IsEnabled => _baseProfiler.IsEnabled;

        /// <summary>
        /// Gets the message bus used by this profiler
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Creates a new pool profiler
        /// </summary>
        /// <param name="baseProfiler">Base profiler implementation for general profiling</param>
        /// <param name="poolMetrics">Pool metrics service</param>
        /// <param name="messageBus">Message bus for publishing profiling messages</param>
        public PoolProfiler(IProfiler baseProfiler, IPoolMetrics poolMetrics, IMessageBus messageBus)
        {
            _baseProfiler = baseProfiler ?? throw new ArgumentNullException(nameof(baseProfiler));
            _poolMetrics = poolMetrics ?? throw new ArgumentNullException(nameof(poolMetrics));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            // Subscribe to profiler session messages
            _messageBus.GetSubscriber<PoolProfilerSessionCompletedMessage>().Subscribe(OnPoolSessionCompleted);
            _messageBus.GetSubscriber<PoolMetricAlertMessage>().Subscribe(OnPoolMetricAlert);
        }

        /// <summary>
        /// Begin a profiling sample with a name
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        public IDisposable BeginSample(string name)
        {
            return _baseProfiler.BeginSample(name);
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _baseProfiler.BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _baseProfiler.BeginScope(category, name);
        }

        /// <summary>
        /// Begin a specialized pool profiling session
        /// </summary>
        /// <param name="pool">Pool to profile</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginPoolScope(IPool pool, string operationType)
        {
            if (!IsEnabled || pool == null)
                return new PoolProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 0, 0, null, null);

            var tag = PoolProfilerTags.ForPoolName(operationType, pool.PoolName);
            return new PoolProfilerSession(
                tag,
                pool.Id,
                pool.PoolName,
                pool.ActiveCount,
                pool.InactiveCount,
                _poolMetrics,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized native pool profiling session
        /// </summary>
        /// <param name="pool">Native pool to profile</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginNativePoolScope(INativePool pool, string operationType)
        {
            if (!IsEnabled || pool == null)
                return new PoolProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 0, 0, null, null);

            // Use pool.NativeId.ToString() or another way to get a string representation for the tag
            string poolName = pool.NativeId.ToString();
            var tag = PoolProfilerTags.ForPoolName(operationType, poolName);
    
            int activeCount = pool.GetActiveCount();
            int inactiveCount = pool.Capacity - activeCount; // Calculate inactive count

            return new PoolProfilerSession(
                tag,
                pool.NativeId.ToGuid(),
                poolName,
                activeCount,
                inactiveCount,
                _poolMetrics,
                _messageBus
            );
        }

        /// <summary>
        /// Begins a profiling session for a pool operation
        /// </summary>
        /// <param name="operationType">Operation type (acquire, release, etc.)</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>A pool profiler session</returns>
        public PoolProfilerSession BeginPoolScope(
            string operationType,
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount)
        {
            if (!IsEnabled)
                return new PoolProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 0, 0, null, null);
                
            var tag = PoolProfilerTags.ForPool(operationType, poolId);
            return new PoolProfilerSession(
                tag, 
                poolId, 
                poolName, 
                activeCount, 
                freeCount, 
                _poolMetrics, 
                _messageBus);
        }
        
        /// <summary>
        /// Begins a profiling session for a pool operation using just the pool name
        /// </summary>
        /// <param name="operationType">Operation type (acquire, release, etc.)</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>A pool profiler session</returns>
        public PoolProfilerSession BeginPoolScope(
            string operationType,
            string poolName,
            int activeCount,
            int freeCount)
        {
            if (!IsEnabled)
                return new PoolProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 0, 0, null, null);
                
            var tag = PoolProfilerTags.ForPoolName(operationType, poolName);
            
            // Create a deterministic GUID from the name
            Guid poolId = Guid.Empty;
            if (!string.IsNullOrEmpty(poolName))
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(poolName));
                    poolId = new Guid(hash);
                }
            }
            
            return new PoolProfilerSession(
                tag, 
                poolId, 
                poolName, 
                activeCount, 
                freeCount, 
                _poolMetrics, 
                _messageBus);
        }
        
        /// <summary>
        /// Begins a profiling session for a generic pool operation
        /// </summary>
        /// <param name="operationType">Operation type (acquire, release, etc.)</param>
        /// <returns>A profiler session</returns>
        public ProfilerSession BeginGenericPoolScope(string operationType)
        {
            if (!IsEnabled)
                return null;
                
            var tag = PoolProfilerTags.ForOperation(operationType);
            return BeginScope(tag);
        }
        
        /// <summary>
        /// Profiles a pool action
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <param name="action">Action to profile</param>
        public void ProfilePoolAction(
            string operationType,
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginPoolScope(operationType, poolId, poolName, activeCount, freeCount))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Get metrics for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get metrics for</param>
        /// <returns>Profile metrics for the tag</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _baseProfiler.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _baseProfiler.GetAllMetrics();
        }

        /// <summary>
        /// Get metrics for a specific pool
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <returns>Pool metrics data</returns>
        public PoolMetricsData? GetPoolMetrics(Guid poolId)
        {
            if (_poolMetricsCache.TryGetValue(poolId, out var metrics))
            {
                return metrics;
            }

            var poolMetrics = _poolMetrics.GetPoolMetrics(poolId);
            if (poolMetrics.HasValue)
            {
                _poolMetricsCache[poolId] = poolMetrics.Value;
                return poolMetrics.Value;
            }

            return null;
        }

        /// <summary>
        /// Get metrics for all pools
        /// </summary>
        /// <returns>Dictionary of pool metrics by pool identifier</returns>
        public IReadOnlyDictionary<Guid, PoolMetricsData> GetAllPoolMetrics()
        {
            // Clear cache to ensure we get fresh data
            _poolMetricsCache.Clear();
            
            // Get all pool metrics from the metrics service
            var allPoolMetrics = _poolMetrics.GetAllPoolMetrics();
            
            // Cache the metrics for future use
            foreach (var kvp in allPoolMetrics)
            {
                _poolMetricsCache[kvp.Key] = kvp.Value;
            }
            
            return allPoolMetrics;
        }

        /// <summary>
        /// Get history for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get history for</param>
        /// <returns>List of historical durations</returns>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            if (_history.TryGetValue(tag, out var history))
                return history;

            return Array.Empty<double>();
        }

        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        /// <param name="metricTag">Tag for the metric to monitor</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold)
        {
            _baseProfiler.RegisterMetricAlert(metricTag, threshold);
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _baseProfiler.RegisterSessionAlert(sessionTag, thresholdMs);
        }

        /// <summary>
        /// Register a pool metric threshold alert
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="metricName">Name of the pool metric</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterPoolMetricAlert(Guid poolId, string metricName, double threshold)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            // Store locally for tracking
            if (!_poolMetricAlerts.TryGetValue(poolId, out var metricsDict))
            {
                metricsDict = new Dictionary<string, double>();
                _poolMetricAlerts[poolId] = metricsDict;
            }
            
            metricsDict[metricName] = threshold;
            
            // Forward to the pool metrics system
            _poolMetrics.RegisterAlert(poolId, metricName, threshold);
        }

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _baseProfiler.ResetStats();
            _history.Clear();
            _poolMetricsCache.Clear();
            _poolMetrics.ResetStats();
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            _baseProfiler.StartProfiling();
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            _baseProfiler.StopProfiling();
        }

        /// <summary>
        /// Handler for pool session completed messages
        /// </summary>
        private void OnPoolSessionCompleted(PoolProfilerSessionCompletedMessage message)
        {
            if (!IsEnabled)
                return;

            // Update history
            var tag = message.Tag;
            if (!_history.TryGetValue(tag, out var history))
            {
                history = new List<double>(_maxHistoryItems);
                _history[tag] = history;
            }

            if (history.Count >= _maxHistoryItems)
                history.RemoveAt(0);

            history.Add(message.DurationMs);
            
            // Invalidate the cache entry for this pool to ensure fresh data next time
            if (message.PoolId != Guid.Empty)
            {
                _poolMetricsCache.Remove(message.PoolId);
            }
        }
        
        /// <summary>
        /// Handler for pool metric alert messages
        /// </summary>
        private void OnPoolMetricAlert(PoolMetricAlertMessage message)
        {
            if (!IsEnabled)
                return;
                
            // Invalidate the cache entry for this pool to ensure fresh data next time
            if (message.PoolId != Guid.Empty)
            {
                _poolMetricsCache.Remove(message.PoolId);
            }
            
            // Additional handling could be added here, like logging
        }
    }
}