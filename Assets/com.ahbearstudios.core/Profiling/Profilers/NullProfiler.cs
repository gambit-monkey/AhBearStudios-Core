
using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// No-operation implementation of IProfiler that does nothing.
    /// Used when profiling is disabled to avoid null checks and performance overhead.
    /// Properly handles all ProfilerTag operations without any actual profiling.
    /// </summary>
    public class NullProfiler : IProfiler
    {
        private static readonly NoOpDisposable SharedDisposable = new NoOpDisposable();
        private static readonly ProfilerSession SharedNullSession = new ProfilerSession(ProfilerTag.Uncategorized, null);
        private static readonly Dictionary<ProfilerTag, DefaultMetricsData> EmptyMetrics = new Dictionary<ProfilerTag, DefaultMetricsData>();
        private static readonly double[] EmptyHistory = Array.Empty<double>();

        /// <summary>
        /// Always returns false since this is a null profiler
        /// </summary>
        public bool IsEnabled => false;

        /// <summary>
        /// Returns null since this profiler doesn't use a message bus
        /// </summary>
        public IMessageBusService MessageBusService => null;

        /// <summary>
        /// No-op implementation that returns a disposable that does nothing
        /// </summary>
        public IDisposable BeginSample(string name)
        {
            return SharedDisposable;
        }

        /// <summary>
        /// No-op implementation that returns a profiler session that does nothing
        /// </summary>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation that returns a profiler session that does nothing
        /// </summary>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for predefined ProfilerTag scopes
        /// </summary>
        public ProfilerSession BeginPredefinedScope(ProfilerTag predefinedTag)
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for rendering scopes
        /// </summary>
        public ProfilerSession BeginRenderingScope(string renderingOperation = "Main")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for physics scopes
        /// </summary>
        public ProfilerSession BeginPhysicsScope(string physicsOperation = "Update")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for animation scopes
        /// </summary>
        public ProfilerSession BeginAnimationScope(string animationOperation = "Update")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for AI scopes
        /// </summary>
        public ProfilerSession BeginAIScope(string aiOperation = "Update")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for gameplay scopes
        /// </summary>
        public ProfilerSession BeginGameplayScope(string gameplayOperation = "Update")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for UI scopes
        /// </summary>
        public ProfilerSession BeginUIScope(string uiOperation = "Update")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for loading scopes
        /// </summary>
        public ProfilerSession BeginLoadingScope(string loadingOperation = "Main")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for memory scopes
        /// </summary>
        public ProfilerSession BeginMemoryScope(string memoryOperation = "Allocation")
        {
            return SharedNullSession;
        }

        /// <summary>
        /// No-op implementation for network scopes
        /// </summary>
        public ProfilerSession BeginNetworkScope(string networkOperation)
        {
            return SharedNullSession;
        }

        /// <summary>
        /// Returns empty metrics for any tag
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
            return EmptyMetrics;
        }

        /// <summary>
        /// Returns an empty list for any tag
        /// </summary>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            return EmptyHistory;
        }

        /// <summary>
        /// No-op implementation for metric alerts
        /// </summary>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold) { }

        /// <summary>
        /// No-op implementation for session alerts
        /// </summary>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs) { }

        /// <summary>
        /// No-op implementation for common operation alerts
        /// </summary>
        public void RegisterCommonOperationAlerts(double thresholdMs) { }

        /// <summary>
        /// No-op implementation for performance-sensitive alerts
        /// </summary>
        public void RegisterPerformanceSensitiveAlerts(double renderingThresholdMs, double physicsThresholdMs, double networkThresholdMs) { }

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
        /// No-op implementation
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Shared no-op disposable to minimize allocations
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}