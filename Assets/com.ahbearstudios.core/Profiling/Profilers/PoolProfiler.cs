
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
    /// Specialized profiler for pool operations that captures pool-specific metrics.
    /// Implements intelligent tag selection and provides comprehensive pool profiling capabilities.
    /// </summary>
    public class PoolProfiler : IProfiler
    {
        private readonly IProfiler _baseProfiler;
        private readonly IPoolMetrics _poolMetrics;
        private readonly IMessageBusService _messageBusService;
        private readonly Dictionary<Guid, PoolMetricsData> _poolMetricsCache = new Dictionary<Guid, PoolMetricsData>();
        private readonly int _maxHistoryItems = 100;
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<Guid, Dictionary<string, double>> _poolMetricAlerts = new Dictionary<Guid, Dictionary<string, double>>();
        private readonly Dictionary<string, double> _operationAlerts = new Dictionary<string, double>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        /// <summary>
        /// Gets whether profiling is enabled
        /// </summary>
        public bool IsEnabled => _baseProfiler.IsEnabled;

        /// <summary>
        /// Gets the message bus used by this profiler
        /// </summary>
        public IMessageBusService MessageBusService => _messageBusService;

        /// <summary>
        /// Creates a new pool profiler
        /// </summary>
        /// <param name="baseProfiler">Base profiler implementation for general profiling</param>
        /// <param name="poolMetrics">Pool metrics service</param>
        /// <param name="messageBusService">Message bus for publishing profiling messages</param>
        public PoolProfiler(IProfiler baseProfiler, IPoolMetrics poolMetrics, IMessageBusService messageBusService)
        {
            _baseProfiler = baseProfiler ?? throw new ArgumentNullException(nameof(baseProfiler));
            _poolMetrics = poolMetrics ?? throw new ArgumentNullException(nameof(poolMetrics));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            SubscribeToMessages();
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

        #region Pool-Specific Profiling Methods

        /// <summary>
        /// Begin a specialized pool profiling session using intelligent tag selection
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginPoolScope(
            string operationType,
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, poolId, poolName);

            return PoolProfilerSession.Create(
                operationType, poolId, poolName, activeCount, freeCount, _poolMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a specialized pool profiling session for IPool interface
        /// </summary>
        /// <param name="pool">Pool to profile</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginPoolScope(IPool pool, string operationType)
        {
            if (!IsEnabled || pool == null)
                return CreateNullSession(operationType, Guid.Empty, "Unknown");

            return PoolProfilerSession.Create(
                operationType, pool.Id, pool.PoolName, pool.ActiveCount, pool.InactiveCount, _poolMetrics, _messageBusService);
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
                return CreateNullSession(operationType, Guid.Empty, "Unknown");

            string poolName = pool.NativeId.ToString();
            Guid poolId = pool.NativeId.ToGuid();
            int activeCount = pool.GetActiveCount();
            int inactiveCount = pool.Capacity - activeCount;

            return PoolProfilerSession.Create(
                operationType, poolId, poolName, activeCount, inactiveCount, _poolMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a lightweight pool profiling session with minimal parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginLightweightPoolScope(
            string operationType,
            string poolName,
            int activeCount = 0,
            int freeCount = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, Guid.Empty, poolName);

            return PoolProfilerSession.CreateMinimal(operationType, poolName, _poolMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a generic pool profiling session using predefined tags
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginGenericPoolScope(string operationType)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, Guid.Empty, "Generic");

            return PoolProfilerSession.CreateGeneric(operationType, _poolMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a profiling session for pool acquisition operations
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginAcquireScope(
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount)
        {
            return BeginPoolScope("Acquire", poolId, poolName, activeCount, freeCount);
        }

        /// <summary>
        /// Begin a profiling session for pool release operations
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Current active count</param>
        /// <param name="freeCount">Current free count</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginReleaseScope(
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount)
        {
            return BeginPoolScope("Release", poolId, poolName, activeCount, freeCount);
        }

        /// <summary>
        /// Begin a profiling session for pool creation operations
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="initialCapacity">Initial pool capacity</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginCreateScope(
            Guid poolId,
            string poolName,
            int initialCapacity)
        {
            return BeginPoolScope("Create", poolId, poolName, 0, initialCapacity);
        }

        /// <summary>
        /// Begin a profiling session for pool expansion operations
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="currentActiveCount">Current active count</param>
        /// <param name="currentFreeCount">Current free count</param>
        /// <param name="expandBy">Number of items to expand by</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginExpandScope(
            Guid poolId,
            string poolName,
            int currentActiveCount,
            int currentFreeCount,
            int expandBy)
        {
            var session = BeginPoolScope("Expand", poolId, poolName, currentActiveCount, currentFreeCount);
            
            // Record expansion metrics
            int oldCapacity = currentActiveCount + currentFreeCount;
            int newCapacity = oldCapacity + expandBy;
            session.RecordExpansion(oldCapacity, newCapacity, expandBy);
            
            return session;
        }

        /// <summary>
        /// Begin a profiling session for pool clearing operations
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="itemsToBeCleared">Number of items that will be cleared</param>
        /// <returns>Pool profiler session</returns>
        public PoolProfilerSession BeginClearScope(
            Guid poolId,
            string poolName,
            int itemsToBeCleared)
        {
            return BeginPoolScope("Clear", poolId, poolName, itemsToBeCleared, 0);
        }

        /// <summary>
        /// Profiles a pool action with automatic session management
        /// </summary>
        /// <param name="operationType">Type of pool operation</param>
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
        /// Profiles a pool action with minimal context
        /// </summary>
        /// <param name="operationType">Type of pool operation</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="action">Action to profile</param>
        public void ProfilePoolAction(
            string operationType,
            string poolName,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginLightweightPoolScope(operationType, poolName))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a simple pool action with minimal context
        /// </summary>
        /// <param name="operationType">Type of pool operation</param>
        /// <param name="action">Action to profile</param>
        public void ProfilePoolAction(string operationType, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginGenericPoolScope(operationType))
            {
                action.Invoke();
            }
        }

        #endregion

        #region Standard IProfiler Implementation

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

        #endregion

        #region Pool-Specific Metrics and Alerts

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
        /// Register an operation type threshold alert
        /// </summary>
        /// <param name="operationType">Type of operation to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterOperationAlert(string operationType, double thresholdMs)
        {
            if (string.IsNullOrEmpty(operationType) || thresholdMs <= 0)
                return;
                
            _operationAlerts[operationType] = thresholdMs;
            
            // Register with base profiler using predefined tags
            var tag = PoolProfilerTags.ForOperation(operationType);
            RegisterSessionAlert(tag, thresholdMs);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates a null/disabled session for when profiling is disabled
        /// </summary>
        private PoolProfilerSession CreateNullSession(string operationType, Guid poolId, string poolName)
        {
            return new PoolProfilerSession(
                PoolProfilerTags.ForOperation("Disabled"),
                poolId,
                poolName ?? string.Empty,
                operationType,
                0,
                0,
                null,
                null
            );
        }

        /// <summary>
        /// Subscribes to pool-related messages from the message bus
        /// </summary>
        private void SubscribeToMessages()
        {
            try
            {
                // Subscribe to profiler session messages
                var sessionCompletedSub = _messageBusService.GetSubscriber<PoolProfilerSessionCompletedMessage>();
                if (sessionCompletedSub != null)
                {
                    sessionCompletedSub.Subscribe(OnPoolSessionCompleted);
                }

                var alertSub = _messageBusService.GetSubscriber<PoolMetricAlertMessage>();
                if (alertSub != null)
                {
                    alertSub.Subscribe(OnPoolMetricAlert);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail initialization
                UnityEngine.Debug.LogError($"PoolProfiler: Failed to subscribe to some messages: {ex.Message}");
            }
        }

        #endregion

        #region Message Handlers

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

            // Check operation-specific alerts
            if (_operationAlerts.TryGetValue(message.OperationType, out var threshold) && 
                message.DurationMs > threshold)
            {
                // Log or handle operation threshold exceeded
                UnityEngine.Debug.LogWarning($"Pool operation '{message.OperationType}' exceeded threshold: {message.DurationMs}ms > {threshold}ms");
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
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose of resources and unsubscribe from messages
        /// </summary>
        public void Dispose()
        {
            // Dispose of any subscriptions
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();

            // Clear caches
            _poolMetricsCache.Clear();
            _history.Clear();
            _poolMetricAlerts.Clear();
            _operationAlerts.Clear();
        }

        #endregion
    }
}