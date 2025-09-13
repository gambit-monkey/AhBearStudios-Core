using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when a log target is registered with the logging system.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct LoggingTargetRegisteredMessage : IMessage
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
        /// Gets the name of the log target that was registered.
        /// </summary>
        public FixedString64Bytes TargetName { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new LoggingTargetRegisteredMessage with proper validation and defaults.
        /// </summary>
        /// <param name="targetName">The name of the registered target</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New LoggingTargetRegisteredMessage instance</returns>
        public static LoggingTargetRegisteredMessage Create(
            string targetName,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            // Input validation
            if (string.IsNullOrEmpty(targetName))
                throw new ArgumentException("Target name cannot be null or empty", nameof(targetName));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "LogTargetService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId(
                messageType: "LoggingTargetRegisteredMessage", 
                source: sourceString, 
                correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId(
                    operation: "TargetRegistration", 
                    context: targetName)
                : correlationId;

            return new LoggingTargetRegisteredMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.LoggingTargetRegisteredMessage,
                Source = source.IsEmpty ? "LogTargetService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                TargetName = targetName?.Length <= 64 ? targetName : targetName?[..64] ?? string.Empty
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>LoggingTargetRegisteredMessage string representation</returns>
        public override string ToString()
        {
            return $"LoggingTargetRegisteredMessage: Target '{TargetName}' registered by {Source}";
        }

        #endregion
    }
}