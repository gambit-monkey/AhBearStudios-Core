using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;

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
        /// Gets the message bus used by the profiler manager
        /// </summary>
        IMessageBus MessageBus { get; }
        
        /// <summary>
        /// System metrics tracker for ProfilerRecorder metrics
        /// </summary>
        SystemMetricsTracker SystemMetrics { get; }
        
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
        /// <param name="metricTag">Tag for the metric to monitor</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        void RegisterMetricAlert(ProfilerTag metricTag, double threshold);
        
        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs);
        
        /// <summary>
        /// Update the profiler manager (should be called regularly)
        /// </summary>
        void Update(float deltaTime);
    }
}