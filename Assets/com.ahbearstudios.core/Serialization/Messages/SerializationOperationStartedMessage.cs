using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Serialization.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Serialization.Messages
{
    /// <summary>
    /// Message sent when a serialization operation begins.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct SerializationOperationStartedMessage : IMessage
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
        /// Gets the serialization format being used.
        /// </summary>
        public SerializationFormat Format { get; init; }

        /// <summary>
        /// Gets the type name being serialized.
        /// </summary>
        public FixedString128Bytes TypeName { get; init; }

        /// <summary>
        /// Gets the operation type (Serialize/Deserialize).
        /// </summary>
        public FixedString32Bytes OperationType { get; init; }

        /// <summary>
        /// Gets whether this is an async operation.
        /// </summary>
        public bool IsAsync { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new SerializationOperationStartedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="format">Serialization format being used</param>
        /// <param name="typeName">Type name being serialized</param>
        /// <param name="operationType">Operation type (Serialize/Deserialize)</param>
        /// <param name="isAsync">Whether this is an async operation</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New SerializationOperationStartedMessage instance</returns>
        public static SerializationOperationStartedMessage Create(
            SerializationFormat format,
            string typeName,
            string operationType,
            bool isAsync = false,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            // Input validation
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
            if (string.IsNullOrEmpty(operationType))
                throw new ArgumentException("Operation type cannot be null or empty", nameof(operationType));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "SerializationService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId(
                messageType: "SerializationOperationStartedMessage",
                source: sourceString,
                correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeName)
                : correlationId;

            return new SerializationOperationStartedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.SerializationOperationStartedMessage,
                Source = source.IsEmpty ? "SerializationService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                Format = format,
                TypeName = typeName?.Length <= 128 ? typeName : typeName?[..128] ?? string.Empty,
                OperationType = operationType?.Length <= 32 ? operationType : operationType?[..32] ?? string.Empty,
                IsAsync = isAsync
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Serialization operation started message string representation</returns>
        public override string ToString()
        {
            return $"SerializationOperationStarted: {OperationType} {TypeName} using {Format} (Async: {IsAsync})";
        }

        #endregion
    }
}