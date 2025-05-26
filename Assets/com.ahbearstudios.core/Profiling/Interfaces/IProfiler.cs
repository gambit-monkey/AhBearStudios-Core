using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for profiling operations using the message bus system
    /// </summary>
    public interface IProfiler
    {
        /// <summary>
        /// Whether profiling is currently enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the message bus used by the profiler
        /// </summary>
        IMessageBus MessageBus { get; }

        /// <summary>
        /// Begin a profiling sample with a name
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        IDisposable BeginSample(string name);
        
        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        ProfilerSession BeginScope(ProfilerTag tag);

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        ProfilerSession BeginScope(ProfilerCategory category, string name);

        /// <summary>
        /// Get stats for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get stats for</param>
        /// <returns>Profile stats for the tag</returns>
        ProfileStats GetStats(ProfilerTag tag);

        /// <summary>
        /// Get all profiling stats
        /// </summary>
        /// <returns>Dictionary of all profiling stats by tag</returns>
        IReadOnlyDictionary<ProfilerTag, ProfileStats> GetAllStats();

        /// <summary>
        /// Get history for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get history for</param>
        /// <returns>List of historical durations</returns>
        IReadOnlyList<double> GetHistory(ProfilerTag tag);

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        void ResetStats();

        /// <summary>
        /// Start profiling
        /// </summary>
        void StartProfiling();

        /// <summary>
        /// Stop profiling
        /// </summary>
        void StopProfiling();

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
    }
}