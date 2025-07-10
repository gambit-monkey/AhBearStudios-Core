using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for coroutine metrics tracking for coroutine runners.
    /// Provides metrics collection and performance analysis capabilities
    /// for coroutine execution systems.
    /// </summary>
    public interface ICoroutineMetrics
    {
        /// <summary>
        /// Gets metrics data for a specific coroutine runner
        /// </summary>
        /// <param name="runnerId">Unique identifier of the runner</param>
        /// <returns>Coroutine metrics data</returns>
        CoroutineMetricsData GetMetricsData(Guid runnerId);
        
        /// <summary>
        /// Gets metrics data for a specific runner with nullable return for error handling
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <returns>Coroutine metrics data if found, null otherwise</returns>
        CoroutineMetricsData? GetRunnerMetrics(Guid runnerId);
        
        /// <summary>
        /// Gets global metrics data aggregated across all runners
        /// </summary>
        /// <returns>Aggregated global metrics</returns>
        CoroutineMetricsData GetGlobalMetricsData();
        
        /// <summary>
        /// Records a coroutine start operation for a runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="startupTimeMs">Time taken to start the coroutine in milliseconds</param>
        /// <param name="hasTag">Whether the coroutine has a tag</param>
        void RecordStart(Guid runnerId, float startupTimeMs, bool hasTag = false);
        
        /// <summary>
        /// Records a coroutine completion for a runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="executionTimeMs">Total execution time in milliseconds</param>
        /// <param name="cleanupTimeMs">Time taken for cleanup in milliseconds</param>
        /// <param name="hasTag">Whether the coroutine had a tag</param>
        void RecordCompletion(Guid runnerId, float executionTimeMs, float cleanupTimeMs = 0f, bool hasTag = false);
        
        /// <summary>
        /// Records a coroutine cancellation for a runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="hasTag">Whether the coroutine had a tag</param>
        void RecordCancellation(Guid runnerId, bool hasTag = false);
        
        /// <summary>
        /// Records a coroutine failure for a runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="hasTag">Whether the coroutine had a tag</param>
        /// <param name="isTimeout">Whether the failure was due to timeout</param>
        void RecordFailure(Guid runnerId, bool hasTag = false, bool isTimeout = false);
        
        /// <summary>
        /// Updates runner configuration and metadata
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="runnerName">Name of the runner</param>
        /// <param name="runnerType">Type of runner</param>
        /// <param name="estimatedOverheadBytes">Estimated memory overhead per coroutine</param>
        void UpdateRunnerConfiguration(Guid runnerId, string runnerName, string runnerType = null, int estimatedOverheadBytes = 0);
        
        /// <summary>
        /// Gets metrics data for all tracked runners
        /// </summary>
        /// <returns>Dictionary mapping runner IDs to metrics data</returns>
        Dictionary<Guid, CoroutineMetricsData> GetAllRunnerMetrics();
        
        /// <summary>
        /// Reset statistics for a specific runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        void ResetRunnerStats(Guid runnerId);
        
        /// <summary>
        /// Reset statistics for all runners
        /// </summary>
        void ResetAllRunnerStats();
        
        /// <summary>
        /// Reset all statistics (alias for ResetAllRunnerStats)
        /// </summary>
        void ResetStats();
        
        /// <summary>
        /// Gets the success rate for a specific runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <returns>Success rate (0-1)</returns>
        float GetRunnerSuccessRate(Guid runnerId);
        
        /// <summary>
        /// Gets the overall efficiency for a specific runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <returns>Efficiency rating (0-1)</returns>
        float GetRunnerEfficiency(Guid runnerId);
        
        /// <summary>
        /// Records tag usage for a runner
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="tagName">Name of the tag</param>
        /// <param name="increment">Whether to increment (true) or decrement (false) the tag count</param>
        void RecordTagUsage(Guid runnerId, string tagName, bool increment = true);
        
        /// <summary>
        /// Records memory allocation for coroutines
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="allocatedBytes">Number of bytes allocated</param>
        /// <param name="isGCAllocation">Whether this is a garbage collection allocation</param>
        void RecordMemoryAllocation(Guid runnerId, long allocatedBytes, bool isGCAllocation = false);
        
        /// <summary>
        /// Gets a performance snapshot of a specific runner suitable for display
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <returns>Dictionary of formatted metric values</returns>
        Dictionary<string, string> GetPerformanceSnapshot(Guid runnerId);
        
        /// <summary>
        /// Register an alert for a specific runner metric
        /// </summary>
        /// <param name="runnerId">Runner identifier</param>
        /// <param name="metricName">Name of the metric to monitor</param>
        /// <param name="threshold">Threshold value that triggers the alert</param>
        void RegisterAlert(Guid runnerId, string metricName, double threshold);
        
        /// <summary>
        /// Whether the metrics tracker is created and initialized
        /// </summary>
        bool IsCreated { get; }
    }
}