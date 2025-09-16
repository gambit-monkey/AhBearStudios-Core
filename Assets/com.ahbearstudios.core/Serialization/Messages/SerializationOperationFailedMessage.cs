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
    /// Message sent when a serialization operation fails.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct SerializationOperationFailedMessage : IMessage
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
        /// Gets the serialization format that failed.
        /// </summary>
        public SerializationFormat Format { get; init; }

        /// <summary>
        /// Gets the type name that failed to serialize.
        /// </summary>
        public FixedString128Bytes TypeName { get; init; }

        /// <summary>
        /// Gets the operation type (Serialize/Deserialize).
        /// </summary>
        public FixedString32Bytes OperationType { get; init; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public FixedString512Bytes ErrorMessage { get; init; }

        /// <summary>
        /// Gets the exception type name.
        /// </summary>
        public FixedString128Bytes ExceptionType { get; init; }

        /// <summary>
        /// Gets whether this was an async operation.
        /// </summary>
        public bool IsAsync { get; init; }

        /// <summary>
        /// Gets whether fallback was attempted.
        /// </summary>
        public bool FallbackAttempted { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new SerializationOperationFailedMessage with proper validation and defaults.
        /// </summary>
        /// <param name="format">Serialization format that failed</param>
        /// <param name="typeName">Type name that failed to serialize</param>
        /// <param name="operationType">Operation type (Serialize/Deserialize)</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="isAsync">Whether this was an async operation</param>
        /// <param name="fallbackAttempted">Whether fallback was attempted</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New SerializationOperationFailedMessage instance</returns>
        public static SerializationOperationFailedMessage Create(
            SerializationFormat format,
            string typeName,
            string operationType,
            Exception exception,
            bool isAsync = false,
            bool fallbackAttempted = false,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.High)
        {
            // Input validation
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
            if (string.IsNullOrEmpty(operationType))
                throw new ArgumentException("Operation type cannot be null or empty", nameof(operationType));
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "SerializationService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId(
                messageType: "SerializationOperationFailedMessage",
                source: sourceString,
                correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationOperation", typeName)
                : correlationId;

            // Truncate error message to fit FixedString512Bytes
            var errorMessage = exception.Message ?? string.Empty;
            if (errorMessage.Length > 512)
                errorMessage = errorMessage.Substring(0, 512);

            return new SerializationOperationFailedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.SerializationOperationFailedMessage,
                Source = source.IsEmpty ? "SerializationService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                Format = format,
                TypeName = typeName?.Length <= 128 ? typeName : typeName?[..128] ?? string.Empty,
                OperationType = operationType?.Length <= 32 ? operationType : operationType?[..32] ?? string.Empty,
                ErrorMessage = errorMessage,
                ExceptionType = exception.GetType().Name.Length <= 128 ? exception.GetType().Name : exception.GetType().Name[..128],
                IsAsync = isAsync,
                FallbackAttempted = fallbackAttempted
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Serialization operation failed message string representation</returns>
        public override string ToString()
        {
            return $"SerializationOperationFailed: {OperationType} {TypeName} using {Format} " +
                   $"failed with {ExceptionType}";
        }

        #endregion
    }
}