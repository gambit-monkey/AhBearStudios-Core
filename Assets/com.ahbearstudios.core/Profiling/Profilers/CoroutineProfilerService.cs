using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.Coroutine.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Sessions;
using AhBearStudios.Core.Profiling.Tagging;
using AhBearStudios.Core.Profiling.Messages;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Specialized profiler for coroutine operations that captures coroutine-specific metrics.
    /// Implements intelligent tag selection based on available parameters for optimal profiling granularity.
    /// </summary>
    public class CoroutineProfilerService : IProfilerService
    {
        private readonly IProfilerService _baseProfilerService;
        private readonly ICoroutineMetrics _coroutineMetrics;
        private readonly IMessageBusService _messageBusService;
        private readonly Dictionary<Guid, CoroutineMetricsData> _runnerMetricsCache = new Dictionary<Guid, CoroutineMetricsData>();
        private readonly int _maxHistoryItems = 100;
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<Guid, Dictionary<string, double>> _runnerMetricAlerts = new Dictionary<Guid, Dictionary<string, double>>();

        /// <summary>
        /// Gets whether profiling is enabled
        /// </summary>
        public bool IsEnabled => _baseProfilerService.IsEnabled;

        /// <summary>
        /// Gets the message bus used by this profiler
        /// </summary>
        public IMessageBusService MessageBusService => _messageBusService;

        /// <summary>
        /// Creates a new coroutine profiler
        /// </summary>
        /// <param name="baseProfilerService">Base profiler implementation for general profiling</param>
        /// <param name="coroutineMetrics">Coroutine metrics service</param>
        /// <param name="messageBusService">Message bus for publishing profiling messages</param>
        public CoroutineProfilerService(IProfilerService baseProfilerService, ICoroutineMetrics coroutineMetrics, IMessageBusService messageBusService)
        {
            _baseProfilerService = baseProfilerService ?? throw new ArgumentNullException(nameof(baseProfilerService));
            _coroutineMetrics = coroutineMetrics ?? throw new ArgumentNullException(nameof(coroutineMetrics));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            // Subscribe to profiler session messages
            _messageBusService.GetSubscriber<CoroutineProfilerSessionCompletedMessage>().Subscribe(OnCoroutineSessionCompleted);
            _messageBusService.GetSubscriber<CoroutineMetricAlertMessage>().Subscribe(OnCoroutineMetricAlert);
        }

        /// <summary>
        /// Begin a profiling sample with a name
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        public IDisposable BeginSample(string name)
        {
            return _baseProfilerService.BeginSample(name);
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _baseProfilerService.BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _baseProfilerService.BeginScope(category, name);
        }

        /// <summary>
        /// Begin a specialized coroutine profiling session using intelligent tag selection
        /// </summary>
        /// <param name="runner">Coroutine runner to profile</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <returns>Coroutine profiler session</returns>
        public CoroutineProfilerSession BeginCoroutineScope(ICoroutineRunner runner, string operationType, int coroutineId = 0, string coroutineTag = null)
        {
            if (!IsEnabled || runner == null)
                return CreateNullSession();

            // Create a deterministic GUID for the runner if needed
            Guid runnerId = GetRunnerIdFromRunner(runner);
            string runnerName = GetRunnerNameFromRunner(runner);
            
            return CoroutineProfilerSession.Create(
                operationType,
                runnerId,
                runnerName,
                coroutineId,
                coroutineTag,
                _coroutineMetrics,
                _messageBusService
            );
        }

        /// <summary>
        /// Begins a profiling session for a coroutine operation with full parameters
        /// </summary>
        /// <param name="operationType">Operation type (start, complete, etc.)</param>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <returns>A coroutine profiler session</returns>
        public CoroutineProfilerSession BeginCoroutineScope(
            string operationType,
            Guid runnerId,
            string runnerName,
            int coroutineId,
            string coroutineTag = null)
        {
            if (!IsEnabled)
                return CreateNullSession();
                
            return CoroutineProfilerSession.Create(
                operationType,
                runnerId,
                runnerName,
                coroutineId,
                coroutineTag,
                _coroutineMetrics,
                _messageBusService
            );
        }
        
        /// <summary>
        /// Begins a profiling session for a coroutine operation using just the runner name
        /// </summary>
        /// <param name="operationType">Operation type (start, complete, etc.)</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <returns>A coroutine profiler session</returns>
        public CoroutineProfilerSession BeginCoroutineScope(
            string operationType,
            string runnerName,
            int coroutineId = 0,
            string coroutineTag = null)
        {
            if (!IsEnabled)
                return CreateNullSession();
                
            // Create a deterministic GUID from the name
            Guid runnerId = CreateDeterministicGuid(runnerName);
            
            return CoroutineProfilerSession.Create(
                operationType,
                runnerId,
                runnerName,
                coroutineId,
                coroutineTag,
                _coroutineMetrics,
                _messageBusService
            );
        }
        
        /// <summary>
        /// Begins a profiling session for a generic coroutine operation
        /// </summary>
        /// <param name="operationType">Operation type (start, complete, etc.)</param>
        /// <returns>A profiler session</returns>
        public ProfilerSession BeginGenericCoroutineScope(string operationType)
        {
            if (!IsEnabled)
                return null;
                
            var tag = CoroutineProfilerTags.ForOperation(operationType);
            return BeginScope(tag);
        }

        /// <summary>
        /// Begins a coroutine scope with minimal parameters using intelligent defaults
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <returns>A lightweight coroutine profiler session</returns>
        public CoroutineProfilerSession BeginLightweightCoroutineScope(string operationType)
        {
            if (!IsEnabled)
                return CreateNullSession();

            return CoroutineProfilerSession.Create(
                operationType,
                Guid.Empty,
                "Unknown",
                0,
                null,
                _coroutineMetrics,
                _messageBusService
            );
        }
        
        /// <summary>
        /// Profiles a coroutine action with full context
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <param name="coroutineTag">Coroutine tag</param>
        /// <param name="action">Action to profile</param>
        public void ProfileCoroutineAction(
            string operationType,
            Guid runnerId,
            string runnerName,
            int coroutineId,
            string coroutineTag,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginCoroutineScope(operationType, runnerId, runnerName, coroutineId, coroutineTag))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a coroutine action with runner context only
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="runnerName">Runner name</param>
        /// <param name="action">Action to profile</param>
        public void ProfileCoroutineAction(string operationType, string runnerName, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginCoroutineScope(operationType, runnerName))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a simple coroutine action with minimal context
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="action">Action to profile</param>
        public void ProfileCoroutineAction(string operationType, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginLightweightCoroutineScope(operationType))
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
            return _baseProfilerService.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _baseProfilerService.GetAllMetrics();
        }

        /// <summary>
        /// Get metrics for a specific runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <returns>Coroutine metrics data</returns>
        public CoroutineMetricsData? GetRunnerMetrics(Guid runnerId)
        {
            if (_runnerMetricsCache.TryGetValue(runnerId, out var metrics))
            {
                return metrics;
            }

            var runnerMetrics = _coroutineMetrics.GetRunnerMetrics(runnerId);
            if (runnerMetrics.HasValue)
            {
                _runnerMetricsCache[runnerId] = runnerMetrics.Value;
                return runnerMetrics.Value;
            }

            return null;
        }

        /// <summary>
        /// Get metrics for all runners
        /// </summary>
        /// <returns>Dictionary of runner metrics by runner identifier</returns>
        public IReadOnlyDictionary<Guid, CoroutineMetricsData> GetAllRunnerMetrics()
        {
            // Clear cache to ensure we get fresh data
            _runnerMetricsCache.Clear();
            
            // Get all runner metrics from the metrics service
            var allRunnerMetrics = _coroutineMetrics.GetAllRunnerMetrics();
            
            // Cache the metrics for future use
            foreach (var kvp in allRunnerMetrics)
            {
                _runnerMetricsCache[kvp.Key] = kvp.Value;
            }
            
            return allRunnerMetrics;
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
            _baseProfilerService.RegisterMetricAlert(metricTag, threshold);
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _baseProfilerService.RegisterSessionAlert(sessionTag, thresholdMs);
        }

        /// <summary>
        /// Register a runner metric threshold alert
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="metricName">Name of the runner metric</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterRunnerMetricAlert(Guid runnerId, string metricName, double threshold)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            // Store locally for tracking
            if (!_runnerMetricAlerts.TryGetValue(runnerId, out var metricsDict))
            {
                metricsDict = new Dictionary<string, double>();
                _runnerMetricAlerts[runnerId] = metricsDict;
            }
            
            metricsDict[metricName] = threshold;
            
            // Forward to the coroutine metrics system
            _coroutineMetrics.RegisterAlert(runnerId, metricName, threshold);
        }

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _baseProfilerService.ResetStats();
            _history.Clear();
            _runnerMetricsCache.Clear();
            _coroutineMetrics.ResetStats();
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            _baseProfilerService.StartProfiling();
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            _baseProfilerService.StopProfiling();
        }

        /// <summary>
        /// Creates a null/disabled session for when profiling is disabled
        /// </summary>
        private CoroutineProfilerSession CreateNullSession()
        {
            return new CoroutineProfilerSession(
                ProfilerTag.Uncategorized, 
                Guid.Empty, 
                string.Empty, 
                0, 
                string.Empty, 
                null, 
                null
            );
        }

        /// <summary>
        /// Handler for coroutine session completed messages
        /// </summary>
        private void OnCoroutineSessionCompleted(CoroutineProfilerSessionCompletedMessage message)
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
            
            // Invalidate the cache entry for this runner to ensure fresh data next time
            if (message.RunnerId != Guid.Empty)
            {
                _runnerMetricsCache.Remove(message.RunnerId);
            }
        }
        
        /// <summary>
        /// Handler for coroutine metric alert messages
        /// </summary>
        private void OnCoroutineMetricAlert(CoroutineMetricAlertMessage message)
        {
            if (!IsEnabled)
                return;
                
            // Invalidate the cache entry for this runner to ensure fresh data next time
            if (message.RunnerId != Guid.Empty)
            {
                _runnerMetricsCache.Remove(message.RunnerId);
            }
            
            // Additional handling could be added here, like logging
        }
        
        /// <summary>
        /// Gets a runner ID from a coroutine runner instance
        /// </summary>
        private Guid GetRunnerIdFromRunner(ICoroutineRunner runner)
        {
            // Try to get ID from runner if it has an ID property
            // if (runner is IIdentifiable identifiable)
            // {
            //     return identifiable.Id;
            // }
            
            // Otherwise create a deterministic GUID from the runner's type and hash code
            string identifier = $"{runner.GetType().Name}_{runner.GetHashCode()}";
            return CreateDeterministicGuid(identifier);
        }
        
        /// <summary>
        /// Gets a runner name from a coroutine runner instance
        /// </summary>
        private string GetRunnerNameFromRunner(ICoroutineRunner runner)
        {
            // Try to get name from runner if it has a name property
            // if (runner is INamed named)
            // {
            //     return named.Name;
            // }
            
            // Otherwise use the type name
            return runner.GetType().Name;
        }
        
        /// <summary>
        /// Creates a deterministic GUID from a string
        /// </summary>
        private Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return Guid.Empty;
                
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return new Guid(hash);
            }
        }
    }
}