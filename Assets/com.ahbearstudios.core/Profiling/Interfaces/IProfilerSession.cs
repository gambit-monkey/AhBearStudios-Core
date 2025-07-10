using System;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for profiling sessions that track performance metrics
    /// </summary>
    public interface IProfilerSession : IDisposable
    {
        /// <summary>
        /// The profiler tag associated with this session
        /// </summary>
        ProfilerTag Tag { get; }
        
        /// <summary>
        /// Gets the elapsed time in milliseconds for this session
        /// </summary>
        double ElapsedMilliseconds { get; }
        
        /// <summary>
        /// Gets the elapsed time in nanoseconds for high-precision operations
        /// </summary>
        long ElapsedNanoseconds { get; }
        
        /// <summary>
        /// Indicates whether this session has been disposed
        /// </summary>
        bool IsDisposed { get; }
        
        /// <summary>
        /// Records a custom metric with this session
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value to record</param>
        void RecordMetric(string metricName, double value);
    }
}