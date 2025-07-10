using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a message bus alert is triggered.
    /// </summary>
    public struct MessageBusAlertMessage : IMessage
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
        /// Gets the profiler tag associated with the alert.
        /// </summary>
        public ProfilerTag Tag { get; }

        /// <summary>
        /// Gets the identifier of the message bus that triggered the alert.
        /// </summary>
        public Guid BusId { get; }

        /// <summary>
        /// Gets the name of the message bus that triggered the alert.
        /// </summary>
        public string BusName { get; }

        /// <summary>
        /// Gets the message type associated with the alert.
        /// </summary>
        public string MessageType { get; }

        /// <summary>
        /// Gets the actual value that triggered the alert.
        /// </summary>
        public double ActualValue { get; }

        /// <summary>
        /// Gets the threshold value that was exceeded.
        /// </summary>
        public double ThresholdValue { get; }

        /// <summary>
        /// Gets the type of alert (DeliveryTime, QueueSize, etc.).
        /// </summary>
        public string AlertType { get; }

        /// <summary>
        /// Gets the severity level of the alert.
        /// </summary>
        public string Severity { get; }

        /// <summary>
        /// Gets additional details about the alert.
        /// </summary>
        public string Details { get; }

        /// <summary>
        /// Creates a new message bus alert message.
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="busName">Message bus name</param>
        /// <param name="messageType">Message type</param>
        /// <param name="actualValue">Actual value that triggered the alert</param>
        /// <param name="thresholdValue">Threshold value that was exceeded</param>
        /// <param name="alertType">Type of alert</param>
        /// <param name="severity">Severity level</param>
        /// <param name="details">Additional details</param>
        public MessageBusAlertMessage(
            ProfilerTag tag,
            Guid busId,
            string busName,
            string messageType,
            double actualValue,
            double thresholdValue,
            string alertType,
            string severity = "Warning",
            string details = "")
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x6005; // Unique type code for this message type
            Tag = tag;
            BusId = busId;
            BusName = busName ?? "Unknown";
            MessageType = messageType ?? "Unknown";
            ActualValue = actualValue;
            ThresholdValue = thresholdValue;
            AlertType = alertType ?? "Unknown";
            Severity = severity ?? "Warning";
            Details = details ?? string.Empty;
        }
    }
}