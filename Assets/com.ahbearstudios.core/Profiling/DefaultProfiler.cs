using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Events;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Default implementation of IProfiler that uses RuntimeProfilerManager
    /// </summary>
    public class DefaultProfiler : IProfiler
    {
        private readonly RuntimeProfilerManager _manager;

        /// <summary>
        /// Create a new DefaultProfiler using the RuntimeProfilerManager singleton
        /// </summary>
        public DefaultProfiler()
        {
            _manager = RuntimeProfilerManager.Instance;
        }

        /// <summary>
        /// Create a new DefaultProfiler with a specific RuntimeProfilerManager
        /// </summary>
        /// <param name="manager">RuntimeProfilerManager instance to use</param>
        public DefaultProfiler(RuntimeProfilerManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <inheritdoc />
        public bool IsEnabled => _manager.IsEnabled;

        /// <inheritdoc />
        public IDisposable BeginSample(string name)
        {
            if (!IsEnabled)
                return null;

            // Create a profiler tag with a default category
            ProfilerTag tag = new ProfilerTag(ProfilerCategory.Scripts, name);
            return BeginScope(tag);
        }
        
        /// <inheritdoc />
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _manager.BeginScope(tag);
        }

        /// <inheritdoc />
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _manager.BeginScope(category, name);
        }

        /// <inheritdoc />
        public ProfileStats GetStats(ProfilerTag tag)
        {
            return _manager.GetStats(tag);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<ProfilerTag, ProfileStats> GetAllStats()
        {
            return _manager.GetAllStats();
        }

        /// <inheritdoc />
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            return _manager.GetHistory(tag);
        }

        /// <inheritdoc />
        public void ResetStats()
        {
            _manager.ResetStats();
        }

        /// <inheritdoc />
        public void StartProfiling()
        {
            _manager.StartProfiling();
        }

        /// <inheritdoc />
        public void StopProfiling()
        {
            _manager.StopProfiling();
        }

        /// <inheritdoc />
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold, Action<MetricEventArgs> callback)
        {
            _manager.RegisterMetricAlert(metricTag, threshold, callback);
        }

        /// <inheritdoc />
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs, Action<ProfilerSessionEventArgs> callback)
        {
            _manager.RegisterSessionAlert(sessionTag, thresholdMs, callback);
        }

        /// <inheritdoc />
        public event EventHandler<ProfilerSessionEventArgs> SessionCompleted
        {
            add => _manager.SessionCompleted += value;
            remove => _manager.SessionCompleted -= value;
        }

        /// <inheritdoc />
        public event EventHandler ProfilingStarted
        {
            add => _manager.ProfilingStarted += value;
            remove => _manager.ProfilingStarted -= value;
        }

        /// <inheritdoc />
        public event EventHandler ProfilingStopped
        {
            add => _manager.ProfilingStopped += value;
            remove => _manager.ProfilingStopped -= value;
        }

        /// <inheritdoc />
        public event EventHandler StatsReset
        {
            add => _manager.StatsReset += value;
            remove => _manager.StatsReset -= value;
        }

        /// <inheritdoc />
        public event EventHandler<MetricEventArgs> MetricAlertTriggered
        {
            add => _manager.MetricAlertTriggered += value;
            remove => _manager.MetricAlertTriggered -= value;
        }

        /// <inheritdoc />
        public event EventHandler<ProfilerSessionEventArgs> SessionAlertTriggered
        {
            add => _manager.SessionAlertTriggered += value;
            remove => _manager.SessionAlertTriggered -= value;
        }
    }

    
}