using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Simple test message for health check functional testing.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    internal readonly record struct HealthCheckTestMessage : IMessage
    {
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


        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new HealthCheckTestMessage for testing purposes.
        /// </summary>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New HealthCheckTestMessage instance</returns>
        public static HealthCheckTestMessage Create(
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            var sourceString = source.IsEmpty ? "HealthCheckService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckTestMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckTest", sourceString)
                : correlationId;

            return new HealthCheckTestMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckTestMessage,
                Source = source.IsEmpty ? "HealthCheckService" : source,
                Priority = MessagePriority.Low,
                CorrelationId = finalCorrelationId
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health check test message string representation</returns>
        public override string ToString()
        {
            return $"HealthCheckTest: Test message from {Source}";
        }
    }
}