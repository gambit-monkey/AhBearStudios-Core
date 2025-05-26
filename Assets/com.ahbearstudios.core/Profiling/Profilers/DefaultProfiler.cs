using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Metrics;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Implementation of the profiler interface using message bus for communication
    /// </summary>
    public class DefaultProfiler : IProfiler
    {
        private readonly IMessageBus _messageBus;
        private readonly ProfilerStatsCollection _statsCollection = new ProfilerStatsCollection();
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<ProfilerTag, double> _metricAlerts = new Dictionary<ProfilerTag, double>();
        private readonly Dictionary<ProfilerTag, double> _sessionAlerts = new Dictionary<ProfilerTag, double>();
        private bool _isEnabled = false;
        private readonly int _maxHistoryItems = 100;

        /// <summary>
        /// Whether profiling is currently enabled
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Gets the message bus used by the profiler
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Creates a new Profiler instance
        /// </summary>
        /// <param name="messageBus">Message bus for publishing profiling messages</param>
        public DefaultProfiler(IMessageBus messageBus)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            // Subscribe to profiler session completed messages
            _messageBus.GetSubscriber<ProfilerSessionCompletedMessage>().Subscribe(OnSessionCompleted);
        }

        /// <summary>
        /// Begin a profiling sample with a name
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        public IDisposable BeginSample(string name)
        {
            if (!IsEnabled)
                return new NoOpDisposable();
        
            return BeginScope(new ProfilerTag(ProfilerCategory.Scripts, name));
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            if (!IsEnabled)
                return new ProfilerSession(tag, null);

            return new ProfilerSession(tag, _messageBus);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return BeginScope(new ProfilerTag(category, name));
        }
        
        /// <summary>
        /// Get metrics for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get metrics for</param>
        /// <returns>Profile metrics for the tag</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _statsCollection.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _statsCollection.GetAllGeneralMetrics();
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
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _statsCollection.Reset();
            _history.Clear();
            
            // Publish stats reset message
            if (IsEnabled)
            {
                _messageBus.PublishMessage(new StatsResetMessage());
            }
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            if (_isEnabled)
                return;

            _isEnabled = true;
            
            // Publish profiling started message
            _messageBus.PublishMessage(new ProfilingStartedMessage());
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            if (!_isEnabled)
                return;

            _isEnabled = false;
            
            // Calculate total duration from all metrics
            double totalDuration = 0;
            foreach (var metrics in _statsCollection.GetAllGeneralMetrics().Values)
            {
                totalDuration += metrics.TotalValue;
            }
            
            // Publish profiling stopped message
            _messageBus.PublishMessage(new ProfilingStoppedMessage(totalDuration));
        }

        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        /// <param name="metricTag">Tag for the metric to monitor</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold)
        {
            _metricAlerts[metricTag] = threshold;
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _sessionAlerts[sessionTag] = thresholdMs;
        }
        
        /// <summary>
        /// Handler for session completed messages
        /// </summary>
        private void OnSessionCompleted(ProfilerSessionCompletedMessage message)
        {
            if (!IsEnabled)
                return;

            double durationMs = message.DurationMs;

            // Update metrics
            _statsCollection.AddSample(message.Tag, durationMs);

            // Update history
            if (!_history.TryGetValue(message.Tag, out var history))
            {
                history = new List<double>(_maxHistoryItems);
                _history[message.Tag] = history;
            }

            if (history.Count >= _maxHistoryItems)
                history.RemoveAt(0);

            history.Add(durationMs);

            // Check for session alerts
            if (_sessionAlerts.TryGetValue(message.Tag, out var threshold) && durationMs > threshold)
            {
                _messageBus.PublishMessage(new SessionAlertMessage(message.Tag, message.SessionId, durationMs, threshold));
            }
            
            // Check for custom metric alerts
            foreach (var metric in message.Metrics)
            {
                var metricTag = new ProfilerTag(message.Tag.Category, $"{message.Tag.Name}/{metric.Key}");
                if (_metricAlerts.TryGetValue(metricTag, out var metricThreshold) && metric.Value > metricThreshold)
                {
                    _messageBus.PublishMessage(new MetricAlertMessage(metricTag, metric.Value, metricThreshold));
                }
            }
        }

        /// <summary>
        /// No-op disposable for when profiling is disabled
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}