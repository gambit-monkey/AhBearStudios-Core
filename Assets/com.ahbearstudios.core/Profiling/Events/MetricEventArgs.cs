using System;

namespace AhBearStudios.Core.Profiling.Events
{
    /// <summary>
    /// Event data for system metric events
    /// </summary>
    public class MetricEventArgs : EventArgs
    {
        /// <summary>
        /// The metric that triggered the event
        /// </summary>
        public SystemMetric Metric { get; }
        
        /// <summary>
        /// The value that triggered the event
        /// </summary>
        public double Value { get; }
        
        /// <summary>
        /// Create new metric event args
        /// </summary>
        public MetricEventArgs(SystemMetric metric, double value)
        {
            Metric = metric;
            Value = value;
        }
    }
}