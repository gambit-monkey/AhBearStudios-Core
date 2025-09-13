using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Profiling.Internal;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Null implementation of IProfilerService for use when profiling is disabled or unavailable.
    /// Provides no-op implementations of all profiling operations with minimal performance overhead.
    /// </summary>
    public sealed class NullProfilerService : IProfilerService
    {
        /// <summary>
        /// Shared instance of the null profiler service to avoid unnecessary allocations.
        /// </summary>
        public static readonly NullProfilerService Instance = new NullProfilerService();

        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public bool IsRecording => false;

        /// <inheritdoc />
        public float SamplingRate => 0.0f;

        /// <inheritdoc />
        public int ActiveScopeCount => 0;

        /// <inheritdoc />
        public long TotalScopeCount => 0;

        /// <inheritdoc />
        public event Action<ProfilerTag, double, string> ThresholdExceeded;

        /// <inheritdoc />
        public event Action<ProfilerTag, double> DataRecorded;

        /// <inheritdoc />
        public event Action<Exception> ErrorOccurred;

        /// <inheritdoc />
        public IDisposable BeginScope(ProfilerTag tag)
        {
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public IDisposable BeginScope(string tagName)
        {
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public IDisposable BeginScope(ProfilerTag tag, IReadOnlyDictionary<string, object> metadata)
        {
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public void RecordSample(ProfilerTag tag, float value, string unit = "ms")
        {
            // No-op
        }

        /// <inheritdoc />
        public void RecordMetric(string metricName, double value, string unit = null, IReadOnlyDictionary<string, string> tags = null)
        {
            // No-op
        }

        /// <inheritdoc />
        public void IncrementCounter(string counterName, long increment = 1, IReadOnlyDictionary<string, string> tags = null)
        {
            // No-op
        }

        /// <inheritdoc />
        public void DecrementCounter(string counterName, long decrement = 1, IReadOnlyDictionary<string, string> tags = null)
        {
            // No-op
        }

        /// <inheritdoc />
        public IReadOnlyCollection<MetricSnapshot> GetMetrics(ProfilerTag tag)
        {
            return Array.Empty<MetricSnapshot>();
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyCollection<MetricSnapshot>> GetAllMetrics()
        {
            return new Dictionary<string, IReadOnlyCollection<MetricSnapshot>>();
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["IsEnabled"] = false,
                ["IsRecording"] = false,
                ["SamplingRate"] = 0.0f,
                ["ActiveScopeCount"] = 0,
                ["TotalScopeCount"] = 0L,
                ["Implementation"] = "NullProfilerService"
            };
        }

        /// <inheritdoc />
        public void Enable(float samplingRate = 1.0f)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Disable()
        {
            // No-op
        }

        /// <inheritdoc />
        public void StartRecording()
        {
            // No-op
        }

        /// <inheritdoc />
        public void StopRecording()
        {
            // No-op
        }

        /// <inheritdoc />
        public void ClearData()
        {
            // No-op
        }

        /// <inheritdoc />
        public void Flush()
        {
            // No-op
        }

        /// <inheritdoc />
        public bool PerformHealthCheck()
        {
            return true; // Always healthy since it does nothing
        }

        /// <inheritdoc />
        public Exception GetLastError()
        {
            return null; // No errors since it does nothing
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // No-op
        }

    }
}