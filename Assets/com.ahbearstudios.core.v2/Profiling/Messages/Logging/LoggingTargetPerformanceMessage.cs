using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a logging target experiences performance issues.
    /// </summary>
    public struct LoggingTargetPerformanceMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
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
        /// Gets the name of the logging target.
        /// </summary>
        public string TargetName { get; }
        
        /// <summary>
        /// Gets the type of performance issue.
        /// </summary>
        public string IssueType { get; }
        
        /// <summary>
        /// Gets the severity of the performance issue.
        /// </summary>
        public LogLevel Severity { get; }
        
        /// <summary>
        /// Gets the measured performance value.
        /// </summary>
        public double MeasuredValue { get; }
        
        /// <summary>
        /// Gets the expected performance threshold.
        /// </summary>
        public double ExpectedThreshold { get; }
        
        /// <summary>
        /// Gets additional details about the performance issue.
        /// </summary>
        public string Details { get; }
        
        /// <summary>
        /// Creates a new logging target performance message.
        /// </summary>
        /// <param name="targetName">Name of the logging target.</param>
        /// <param name="issueType">Type of performance issue.</param>
        /// <param name="severity">Severity of the issue.</param>
        /// <param name="measuredValue">Measured performance value.</param>
        /// <param name="expectedThreshold">Expected performance threshold.</param>
        /// <param name="details">Additional details.</param>
        public LoggingTargetPerformanceMessage(
            string targetName,
            string issueType,
            LogLevel severity,
            double measuredValue,
            double expectedThreshold,
            string details = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x5006; // Unique type code for this message type
            TargetName = targetName ?? "Unknown";
            IssueType = issueType ?? "Performance";
            Severity = severity;
            MeasuredValue = measuredValue;
            ExpectedThreshold = expectedThreshold;
            Details = details ?? string.Empty;
        }
    }
}