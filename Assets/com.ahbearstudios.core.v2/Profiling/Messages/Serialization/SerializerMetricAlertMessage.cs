using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a serializer metric exceeds a threshold
    /// </summary>
    public struct SerializerMetricAlertMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this message was created
        /// </summary>
        public long TimestampTicks { get; }

        /// <summary>
        /// Gets the type code for this message
        /// </summary>
        public ushort TypeCode => 7002; // Arbitrary type code for this message

        /// <summary>
        /// Gets the metric name
        /// </summary>
        public string MetricName { get; }

        /// <summary>
        /// Gets the current value of the metric
        /// </summary>
        public double CurrentValue { get; }

        /// <summary>
        /// Gets the threshold value that was exceeded
        /// </summary>
        public double ThresholdValue { get; }

        /// <summary>
        /// Creates a new serializer metric alert message
        /// </summary>
        public SerializerMetricAlertMessage(string metricName, double currentValue, double thresholdValue)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            MetricName = metricName;
            CurrentValue = currentValue;
            ThresholdValue = thresholdValue;
        }
    }
}