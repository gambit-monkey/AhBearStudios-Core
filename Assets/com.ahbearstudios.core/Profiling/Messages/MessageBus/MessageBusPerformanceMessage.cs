using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a message bus experiences performance issues.
    /// </summary>
    public struct MessageBusPerformanceMessage : IMessage
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
        /// Gets the identifier of the message bus.
        /// </summary>
        public Guid BusId { get; }
        
        /// <summary>
        /// Gets the name of the message bus.
        /// </summary>
        public string BusName { get; }
        
        /// <summary>
        /// Gets the type of performance issue.
        /// </summary>
        public string IssueType { get; }
        
        /// <summary>
        /// Gets the severity of the performance issue.
        /// </summary>
        public string Severity { get; }
        
        /// <summary>
        /// Gets the measured performance value.
        /// </summary>
        public double MeasuredValue { get; }
        
        /// <summary>
        /// Gets the expected performance threshold.
        /// </summary>
        public double ExpectedThreshold { get; }
        
        /// <summary>
        /// Gets the operation type that caused the performance issue.
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the message type involved in the performance issue.
        /// </summary>
        public string MessageType { get; }
        
        /// <summary>
        /// Gets additional details about the performance issue.
        /// </summary>
        public string Details { get; }
        
        /// <summary>
        /// Creates a new message bus performance message.
        /// </summary>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="busName">Message bus name</param>
        /// <param name="issueType">Type of performance issue</param>
        /// <param name="severity">Severity level</param>
        /// <param name="measuredValue">Measured performance value</param>
        /// <param name="expectedThreshold">Expected performance threshold</param>
        /// <param name="operationType">Operation type</param>
        /// <param name="messageType">Message type</param>
        /// <param name="details">Additional details</param>
        public MessageBusPerformanceMessage(
            Guid busId,
            string busName,
            string issueType,
            string severity,
            double measuredValue,
            double expectedThreshold,
            string operationType = "",
            string messageType = "",
            string details = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x6006; // Unique type code for this message type
            BusId = busId;
            BusName = busName ?? "Unknown";
            IssueType = issueType ?? "Unknown";
            Severity = severity ?? "Warning";
            MeasuredValue = measuredValue;
            ExpectedThreshold = expectedThreshold;
            OperationType = operationType ?? string.Empty;
            MessageType = messageType ?? string.Empty;
            Details = details ?? string.Empty;
        }
    }
}