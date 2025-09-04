using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.HealthChecking.Messages
{
    /// <summary>
    /// Message published when health check system degradation levels change.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct HealthCheckDegradationChangeMessage : IMessage
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

        // Health check degradation change-specific properties
        /// <summary>
        /// Gets the name of the system experiencing degradation change.
        /// </summary>
        public FixedString64Bytes SystemName { get; init; }

        /// <summary>
        /// Gets the previous degradation level.
        /// </summary>
        public DegradationLevel OldLevel { get; init; }

        /// <summary>
        /// Gets the new degradation level.
        /// </summary>
        public DegradationLevel NewLevel { get; init; }

        /// <summary>
        /// Gets the reason for the degradation change.
        /// </summary>
        public FixedString512Bytes Reason { get; init; }

        /// <summary>
        /// Gets whether this change was triggered automatically.
        /// </summary>
        public bool IsAutomatic { get; init; }


        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Creates a new HealthCheckDegradationChangeMessage.
        /// </summary>
        /// <param name="systemName">Name of the system</param>
        /// <param name="oldLevel">Previous degradation level</param>
        /// <param name="newLevel">New degradation level</param>
        /// <param name="reason">Reason for the change</param>
        /// <param name="isAutomatic">Whether the change was automatic</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID</param>
        /// <returns>New HealthCheckDegradationChangeMessage instance</returns>
        public static HealthCheckDegradationChangeMessage Create(
            string systemName,
            DegradationLevel oldLevel,
            DegradationLevel newLevel,
            string reason = null,
            bool isAutomatic = true,
            FixedString64Bytes source = default,
            Guid correlationId = default)
        {
            // Determine priority based on degradation severity
            var priority = newLevel switch
            {
                DegradationLevel.Severe => MessagePriority.Critical,
                DegradationLevel.Moderate => MessagePriority.High,
                DegradationLevel.Minor => MessagePriority.Normal,
                DegradationLevel.None => MessagePriority.Low,
                _ => MessagePriority.Low
            };

            var sourceString = source.IsEmpty ? "HealthCheckService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckDegradationChangeMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("DegradationChange", $"{systemName}-{oldLevel}-{newLevel}")
                : correlationId;

            return new HealthCheckDegradationChangeMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.HealthCheckDegradationChangeMessage,
                Source = source.IsEmpty ? "HealthCheckService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                SystemName = systemName?.Length <= 64 ? systemName : systemName?[..64] ?? "Unknown",
                OldLevel = oldLevel,
                NewLevel = newLevel,
                Reason = reason?.Length <= 512 ? reason : reason?[..512] ?? "No reason provided",
                IsAutomatic = isAutomatic
            };
        }

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Health check degradation change message string representation</returns>
        public override string ToString()
        {
            var changeType = IsAutomatic ? "Auto" : "Manual";
            return $"HealthCheckDegradationChange: {SystemName} {OldLevel} -> {NewLevel} ({changeType}) - {Reason}";
        }
    }
}