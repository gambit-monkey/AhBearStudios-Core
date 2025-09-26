using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;

namespace AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs
{
    /// <summary>
    /// Null object implementation of IProfilerService for TDD testing.
    /// Performs no actual profiling operations with minimal overhead.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// Ideal for performance-sensitive tests where profiling overhead should be avoided.
    /// </summary>
    public sealed class NullProfilerService : IProfilerService
    {
        /// <summary>
        /// Singleton instance for efficiency in tests.
        /// </summary>
        public static readonly NullProfilerService Instance = new();

        private NullProfilerService() { }

        #region IProfilerService Implementation - Null Object Pattern

        // Properties - all return safe defaults for null object
        public bool IsEnabled => false; // Always disabled for null object
        public bool IsRecording => false; // Never recording for null object
        public float SamplingRate => 0.0f; // No sampling for null object
        public int ActiveScopeCount => 0; // No active scopes
        public long TotalScopeCount => 0; // No scopes created

        // Core profiling operations - all no-op
        public IDisposable BeginScope(ProfilerTag tag)
        {
            return NullProfileScope.Instance;
        }

        public IDisposable BeginScope(string tagName)
        {
            return NullProfileScope.Instance;
        }

        public IDisposable BeginScope(ProfilerTag tag, IReadOnlyDictionary<string, object> metadata)
        {
            return NullProfileScope.Instance;
        }

        public void RecordSample(ProfilerTag tag, float value, string unit = "ms")
        {
            // No-op: null object doesn't record samples
        }

        // Metric operations - all no-op
        public void RecordMetric(string metricName, double value, string unit = null, IReadOnlyDictionary<string, string> tags = null)
        {
            // No-op: null object doesn't record metrics
        }

        public void IncrementCounter(string counterName, long increment = 1, IReadOnlyDictionary<string, string> tags = null)
        {
            // No-op: null object doesn't increment counters
        }

        public void DecrementCounter(string counterName, long decrement = 1, IReadOnlyDictionary<string, string> tags = null)
        {
            // No-op: null object doesn't decrement counters
        }

        // Query operations - return empty results
        public IReadOnlyCollection<MetricSnapshot> GetMetrics(ProfilerTag tag)
        {
            return Array.Empty<MetricSnapshot>(); // No metrics in null object
        }

        public IReadOnlyDictionary<string, IReadOnlyCollection<MetricSnapshot>> GetAllMetrics()
        {
            return new Dictionary<string, IReadOnlyCollection<MetricSnapshot>>(); // No metrics in null object
        }

        public IReadOnlyDictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["enabled"] = false,
                ["recording"] = false,
                ["samplingRate"] = 0.0f,
                ["activeScopeCount"] = 0,
                ["totalScopeCount"] = 0L,
                ["totalMetrics"] = 0,
                ["memoryUsage"] = 0L
            };
        }

        // Configuration and control operations - all no-op
        public void Enable(float samplingRate = 1.0f)
        {
            // No-op: null object doesn't change state
        }

        public void Disable()
        {
            // No-op: null object is already disabled
        }

        public void StartRecording()
        {
            // No-op: null object doesn't record
        }

        public void StopRecording()
        {
            // No-op: null object doesn't record
        }

        public void ClearData()
        {
            // No-op: null object has no data to clear
        }

        public void Flush()
        {
            // No-op: null object has nothing to flush
        }

        // Health and monitoring - always healthy for null object
        public bool PerformHealthCheck()
        {
            return true; // Null object always passes health check
        }

        public Exception GetLastError()
        {
            return null; // Null object never has errors
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // No-op: null object has nothing to dispose
        }

        #endregion
    }

    /// <summary>
    /// Null object implementation of IDisposable for profiler scopes.
    /// Provides zero-overhead scope management for tests.
    /// </summary>
    internal sealed class NullProfileScope : IDisposable
    {
        /// <summary>
        /// Singleton instance for efficiency.
        /// </summary>
        public static readonly NullProfileScope Instance = new();

        private NullProfileScope() { }

        public void Dispose()
        {
            // No-op: null scope has nothing to dispose
        }
    }
}