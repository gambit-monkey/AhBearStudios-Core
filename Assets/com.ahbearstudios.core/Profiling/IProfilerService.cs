using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Models;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Core profiling service interface providing comprehensive performance monitoring capabilities.
    /// Integrates with Unity's ProfilerMarker system and provides custom metrics tracking.
    /// Designed for production-ready Unity game development with minimal performance overhead.
    /// </summary>
    public interface IProfilerService : IDisposable
    {
        /// <summary>
        /// Gets whether the profiler service is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets whether the profiler service is currently recording.
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Gets the current sampling rate (0.0 to 1.0).
        /// </summary>
        float SamplingRate { get; }

        /// <summary>
        /// Gets the number of active profiling scopes.
        /// </summary>
        int ActiveScopeCount { get; }

        /// <summary>
        /// Gets the total number of profiling scopes created.
        /// </summary>
        long TotalScopeCount { get; }

        // Core Profiling Operations

        /// <summary>
        /// Begins a profiling scope with automatic Unity ProfilerMarker integration.
        /// </summary>
        /// <param name="tag">The profiling tag</param>
        /// <returns>Disposable profiling scope</returns>
        IDisposable BeginScope(ProfilerTag tag);

        /// <summary>
        /// Begins a profiling scope with string tag for backward compatibility.
        /// </summary>
        /// <param name="tagName">The profiling tag name</param>
        /// <returns>Disposable profiling scope</returns>
        IDisposable BeginScope(string tagName);

        /// <summary>
        /// Begins a profiling scope with custom metadata.
        /// </summary>
        /// <param name="tag">The profiling tag</param>
        /// <param name="metadata">Additional metadata for the scope</param>
        /// <returns>Disposable profiling scope</returns>
        IDisposable BeginScope(ProfilerTag tag, IReadOnlyDictionary<string, object> metadata);

        /// <summary>
        /// Records a one-time sample without creating a scope.
        /// </summary>
        /// <param name="tag">The profiling tag</param>
        /// <param name="value">The value to record</param>
        /// <param name="unit">The unit of measurement</param>
        void RecordSample(ProfilerTag tag, float value, string unit = "ms");

        // Metric Operations

        /// <summary>
        /// Records a custom metric value.
        /// </summary>
        /// <param name="metricName">The metric name</param>
        /// <param name="value">The metric value</param>
        /// <param name="unit">The unit of measurement</param>
        /// <param name="tags">Additional tags for the metric</param>
        void RecordMetric(string metricName, double value, string unit = null, IReadOnlyDictionary<string, string> tags = null);

        /// <summary>
        /// Increments a counter metric.
        /// </summary>
        /// <param name="counterName">The counter name</param>
        /// <param name="increment">The increment value (default: 1)</param>
        /// <param name="tags">Additional tags for the counter</param>
        void IncrementCounter(string counterName, long increment = 1, IReadOnlyDictionary<string, string> tags = null);

        /// <summary>
        /// Decrements a counter metric.
        /// </summary>
        /// <param name="counterName">The counter name</param>
        /// <param name="decrement">The decrement value (default: 1)</param>
        /// <param name="tags">Additional tags for the counter</param>
        void DecrementCounter(string counterName, long decrement = 1, IReadOnlyDictionary<string, string> tags = null);

        // Query Operations

        /// <summary>
        /// Gets metrics for a specific profiling tag.
        /// </summary>
        /// <param name="tag">The profiling tag</param>
        /// <returns>Collection of metric snapshots</returns>
        IReadOnlyCollection<MetricSnapshot> GetMetrics(ProfilerTag tag);

        /// <summary>
        /// Gets all recorded metrics.
        /// </summary>
        /// <returns>Dictionary of all metrics grouped by tag</returns>
        IReadOnlyDictionary<string, IReadOnlyCollection<MetricSnapshot>> GetAllMetrics();

        /// <summary>
        /// Gets performance statistics for the profiler service itself.
        /// </summary>
        /// <returns>Dictionary containing profiler service statistics</returns>
        IReadOnlyDictionary<string, object> GetStatistics();

        // Configuration and Control

        /// <summary>
        /// Enables the profiler service with the specified sampling rate.
        /// </summary>
        /// <param name="samplingRate">The sampling rate (0.0 to 1.0)</param>
        void Enable(float samplingRate = 1.0f);

        /// <summary>
        /// Disables the profiler service.
        /// </summary>
        void Disable();

        /// <summary>
        /// Starts recording profiling data.
        /// </summary>
        void StartRecording();

        /// <summary>
        /// Stops recording profiling data.
        /// </summary>
        void StopRecording();

        /// <summary>
        /// Clears all recorded profiling data.
        /// </summary>
        void ClearData();

        /// <summary>
        /// Flushes any buffered profiling data.
        /// </summary>
        void Flush();

        // Health and Monitoring

        /// <summary>
        /// Performs a health check on the profiler service.
        /// </summary>
        /// <returns>True if the service is healthy, false otherwise</returns>
        bool PerformHealthCheck();

        /// <summary>
        /// Gets the last error that occurred in the profiler service.
        /// </summary>
        /// <returns>The last exception or null if no errors</returns>
        Exception GetLastError();
    }
}