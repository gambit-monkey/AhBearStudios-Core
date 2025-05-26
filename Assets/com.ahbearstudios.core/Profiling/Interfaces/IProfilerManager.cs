using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Events;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for the central manager of all profiling activities
    /// </summary>
    public interface IProfilerManager
    {
        /// <summary>
        /// Whether profiling is currently enabled
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Whether to log profiling events to the console
        /// </summary>
        bool LogToConsole { get; set; }
        
        /// <summary>
        /// System metrics tracker for ProfilerRecorder metrics
        /// </summary>
        SystemMetricsTracker SystemMetrics { get; }
        
        /// <summary>
        /// ThresholdAlerting system for monitoring metrics
        /// </summary>
        ThresholdAlertSystem AlertSystem { get; }
        
        /// <summary>
        /// Start profiling
        /// </summary>
        void StartProfiling();
        
        /// <summary>
        /// Stop profiling
        /// </summary>
        void StopProfiling();
        
        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        IProfilerSession BeginScope(ProfilerTag tag);
        
        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        IProfilerSession BeginScope(ProfilerCategory category, string name);
        
        /// <summary>
        /// Notifies that a session has started
        /// </summary>
        void OnSessionStarted(IProfilerSession session);
        
        /// <summary>
        /// Notifies that a session has ended
        /// </summary>
        void OnSessionEnded(IProfilerSession session, double durationMs);
        
        /// <summary>
        /// Get stats for a specific profiling tag
        /// </summary>
        ProfileStats GetStats(ProfilerTag tag);
        
        /// <summary>
        /// Get all profiling stats
        /// </summary>
        IReadOnlyDictionary<ProfilerTag, ProfileStats> GetAllStats();
        
        /// <summary>
        /// Get history for a specific profiling tag
        /// </summary>
        IReadOnlyList<double> GetHistory(ProfilerTag tag);
        
        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        void ResetStats();
        
        /// <summary>
        /// Get all active profiling sessions
        /// </summary>
        IReadOnlyDictionary<ProfilerTag, List<IProfilerSession>> GetActiveSessions();
        
        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        void RegisterMetricAlert(ProfilerTag metricTag, double threshold, Action<MetricEventArgs> callback);
        
        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs, Action<ProfilerSessionEventArgs> callback);
        
        /// <summary>
        /// Update the profiler manager (should be called regularly)
        /// </summary>
        void Update(float deltaTime);
    }
}