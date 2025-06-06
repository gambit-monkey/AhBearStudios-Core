using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a coroutine metric alert is triggered
    /// </summary>
    public struct CoroutineMetricAlertMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; }
        
        /// <inheritdoc />
        public long TimestampTicks { get; }
        
        /// <inheritdoc />
        public ushort TypeCode { get; }
        
        /// <summary>
        /// Runner identifier
        /// </summary>
        public readonly Guid RunnerId;
        
        /// <summary>
        /// Runner name
        /// </summary>
        public readonly string RunnerName;
        
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
        public readonly double Threshold;
        
        /// <summary>
        /// Alert severity level
        /// </summary>
        public readonly AlertSeverity Severity;
        
        /// <summary>
        /// Creates a new coroutine metric alert message
        /// </summary>
        public CoroutineMetricAlertMessage(
            Guid runnerId,
            string runnerName,
            string metricName,
            double currentValue,
            double threshold,
            AlertSeverity severity = AlertSeverity.Warning)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0; // Will be assigned by message registry
            RunnerId = runnerId;
            RunnerName = runnerName;
            MetricName = metricName;
            CurrentValue = currentValue;
            Threshold = threshold;
            Severity = severity;
        }
    }
}