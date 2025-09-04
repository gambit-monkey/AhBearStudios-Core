using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message sent when a health check begins execution.
    /// Used for tracking health check execution and performance monitoring.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckStartedMessage : IMessage
    {
        #region IMessage Implementation

        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created, in UTC ticks.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets optional correlation ID for message tracing across systems.
        /// </summary>
        public Guid CorrelationId { get; init; }

        #endregion

        #region Message-Specific Properties

        /// <summary>
        /// Gets the name of the health check that started.
        /// </summary>
        public FixedString64Bytes HealthCheckName { get; init; }

        /// <summary>
        /// Gets the type/category of the health check that started.
        /// </summary>
        public HealthCheckCategory HealthCheckCategory { get; init; }

        /// <summary>
        /// Gets the implementation type name of the health check.
        /// </summary>
        public FixedString128Bytes HealthCheckType { get; init; }

        /// <summary>
        /// Gets the expected timeout duration for this health check.
        /// </summary>
        public long TimeoutTicks { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the TimeSpan representation of the timeout.
        /// </summary>
        public TimeSpan Timeout => new TimeSpan(TimeoutTicks);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new HealthCheckStartedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="healthCheckName">Name of the health check</param>
        /// <param name="healthCheckCategory">Category of the health check</param>
        /// <param name="healthCheckType">Implementation type of the health check</param>
        /// <param name="timeout">Expected timeout duration</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New HealthCheckStartedMessage instance</returns>
        public static HealthCheckStartedMessage Create(
            FixedString64Bytes healthCheckName,
            HealthCheckCategory healthCheckCategory,
            string healthCheckType,
            TimeSpan timeout,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Low)
        {
            var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckStartedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckStarted", healthCheckName.ToString())
                : correlationId;

            return new HealthCheckStartedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckStartedMessage,
                Source = source.IsEmpty ? "HealthCheckSystem" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                HealthCheckName = healthCheckName,
                HealthCheckCategory = healthCheckCategory,
                HealthCheckType = healthCheckType?.Length <= 128 ? healthCheckType : healthCheckType?[..128] ?? "Unknown",
                TimeoutTicks = timeout.Ticks
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health check started message string representation</returns>
        public override string ToString()
        {
            return $"HealthCheckStarted: {HealthCheckName} ({HealthCheckCategory}) - Timeout: {Timeout.TotalSeconds:F1}s";
        }

        #endregion
    }
}