using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a metric alert is triggered
    /// </summary>
    public struct MetricAlertMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode => 10005; // You would use your message registry to assign appropriate codes

        /// <summary>
        /// The profiler tag associated with this metric
        /// </summary>
        public ProfilerTag MetricTag { get; }
        
        /// <summary>
        /// The current value of the metric
        /// </summary>
        public double CurrentValue { get; }
        
        /// <summary>
        /// The threshold that was exceeded
        /// </summary>
        public double Threshold { get; }
        
        /// <summary>
        /// The percentage by which the threshold was exceeded
        /// </summary>
        public double ExceedancePercentage { get; }

        /// <summary>
        /// Creates a new MetricAlertMessage
        /// </summary>
        /// <param name="metricTag">The metric tag</param>
        /// <param name="currentValue">Current metric value</param>
        /// <param name="threshold">Threshold value</param>
        public MetricAlertMessage(ProfilerTag metricTag, double currentValue, double threshold)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            MetricTag = metricTag;
            CurrentValue = currentValue;
            Threshold = threshold;
            ExceedancePercentage = threshold > 0 ? ((currentValue / threshold) - 1.0) * 100.0 : 0;
        }
    }
}