using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a pool metric exceeds its alert threshold
    /// </summary>
    public struct PoolMetricAlertMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 10021; // Assign an appropriate type code
        
        /// <summary>
        /// Pool identifier
        /// </summary>
        public readonly Guid PoolId;
        
        /// <summary>
        /// Pool name
        /// </summary>
        public readonly string PoolName;
        
        /// <summary>
        /// Name of the metric that triggered the alert
        /// </summary>
        public readonly string MetricName;
        
        /// <summary>
        /// Current value of the metric
        /// </summary>
        public readonly double CurrentValue;
        
        /// <summary>
        /// Threshold value that was exceeded
        /// </summary>
        public readonly double ThresholdValue;
        
        /// <summary>
        /// Time when the alert was triggered
        /// </summary>
        public readonly DateTime TimeTriggered;
        
        /// <summary>
        /// Creates a new pool metric alert message
        /// </summary>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="currentValue">Current value of the metric</param>
        /// <param name="thresholdValue">Threshold value</param>
        public PoolMetricAlertMessage(
            Guid poolId,
            string poolName,
            string metricName,
            double currentValue,
            double thresholdValue)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            PoolId = poolId;
            PoolName = poolName;
            MetricName = metricName;
            CurrentValue = currentValue;
            ThresholdValue = thresholdValue;
            TimeTriggered = DateTime.Now;
        }
    }
}