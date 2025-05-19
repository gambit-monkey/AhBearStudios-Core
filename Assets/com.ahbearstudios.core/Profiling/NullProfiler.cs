using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Events;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// NoOp implementation of IProfiler that does nothing (for when profiling is disabled)
    /// </summary>
    public class NullProfiler : IProfiler
    {
        private static readonly ProfilerTag _nullTag = new ProfilerTag(ProfilerCategory.Internal, "NullProfiler");
        private static readonly ProfilerSession _nullSession = null;
        private static readonly ProfileStats _nullStats = new ProfileStats();
        private static readonly IReadOnlyDictionary<ProfilerTag, ProfileStats> _emptyStats = new Dictionary<ProfilerTag, ProfileStats>();
        private static readonly IReadOnlyList<double> _emptyHistory = Array.Empty<double>();
        
        // A null disposable that does nothing
        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();
            public void Dispose() { }
        }

        /// <inheritdoc />
        public bool IsEnabled => false;
        
        /// <inheritdoc />
        public IDisposable BeginSample(string name)
        {
            return NullDisposable.Instance;
        }

        /// <inheritdoc />
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _nullSession;
        }

        /// <inheritdoc />
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _nullSession;
        }

        /// <inheritdoc />
        public ProfileStats GetStats(ProfilerTag tag)
        {
            return _nullStats;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<ProfilerTag, ProfileStats> GetAllStats()
        {
            return _emptyStats;
        }

        /// <inheritdoc />
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            return _emptyHistory;
        }

        /// <inheritdoc />
        public void ResetStats()
        {
            // Do nothing
        }

        /// <inheritdoc />
        public void StartProfiling()
        {
            // Do nothing
        }

        /// <inheritdoc />
        public void StopProfiling()
        {
            // Do nothing
        }

        /// <inheritdoc />
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold, Action<MetricEventArgs> callback)
        {
            // Do nothing
        }

        /// <inheritdoc />
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs, Action<ProfilerSessionEventArgs> callback)
        {
            // Do nothing
        }

        /// <inheritdoc />
        public event EventHandler<ProfilerSessionEventArgs> SessionCompleted
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public event EventHandler ProfilingStarted
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public event EventHandler ProfilingStopped
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public event EventHandler StatsReset
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public event EventHandler<MetricEventArgs> MetricAlertTriggered
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public event EventHandler<ProfilerSessionEventArgs> SessionAlertTriggered
        {
            add { }
            remove { }
        }
    }

    
}