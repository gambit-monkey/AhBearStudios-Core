using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a metric exceeds its threshold value
    /// </summary>
    public struct MetricThresholdExceededMessage : IMessage
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
        public ushort TypeCode { get; }
        
        /// <summary>
        /// The metric tag that exceeded its threshold
        /// </summary>
        public ProfilerTag MetricTag { get; }
        
        /// <summary>
        /// The current value of the metric
        /// </summary>
        public double Value { get; }
        
        /// <summary>
        /// The threshold value that was exceeded
        /// </summary>
        public double ThresholdValue { get; }
        
        /// <summary>
        /// The unit of the metric value
        /// </summary>
        public string Unit { get; }
        
        /// <summary>
        /// Creates a new MetricThresholdExceededMessage
        /// </summary>
        public MetricThresholdExceededMessage(ProfilerTag metricTag, double value, double thresholdValue, string unit = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0; // This will be set by the registry
            
            MetricTag = metricTag;
            Value = value;
            ThresholdValue = thresholdValue;
            Unit = unit ?? string.Empty;
        }
    }
}