using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// No-operation implementation of IProfiler that does nothing
    /// Used when profiling is disabled to avoid null checks
    /// </summary>
    public class NullProfiler : IProfiler
    {
        /// <summary>
        /// Always returns false since this is a null profiler
        /// </summary>
        public bool IsEnabled => false;

        /// <summary>
        /// Returns null since this profiler doesn't use a message bus
        /// </summary>
        public IMessageBus MessageBus => null;

        /// <summary>
        /// No-op implementation that returns a disposable that does nothing
        /// </summary>
        public IDisposable BeginSample(string name)
        {
            return new NoOpDisposable();
        }

        /// <summary>
        /// No-op implementation that returns a profiler session that does nothing
        /// </summary>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return new ProfilerSession(tag, null);
        }

        /// <summary>
        /// No-op implementation that returns a profiler session that does nothing
        /// </summary>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return new ProfilerSession(new ProfilerTag(category, name), null);
        }

        /// <summary>
        /// Returns empty metrics
        /// </summary>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return new DefaultMetricsData();
        }

        /// <summary>
        /// Returns an empty dictionary
        /// </summary>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return new Dictionary<ProfilerTag, DefaultMetricsData>();
        }

        /// <summary>
        /// Returns an empty list
        /// </summary>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            return Array.Empty<double>();
        }

        /// <summary>
        /// No-op implementation
        /// </summary>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold) { }

        /// <summary>
        /// No-op implementation
        /// </summary>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs) { }

        /// <summary>
        /// No-op implementation
        /// </summary>
        public void ResetStats() { }

        /// <summary>
        /// No-op implementation
        /// </summary>
        public void StartProfiling() { }

        /// <summary>
        /// No-op implementation
        /// </summary>
        public void StopProfiling() { }

        /// <summary>
        /// No-op disposable for the null profiler
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}