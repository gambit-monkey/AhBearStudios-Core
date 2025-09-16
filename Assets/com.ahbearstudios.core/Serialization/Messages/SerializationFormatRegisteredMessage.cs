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
    /// Message sent when a serialization format is registered with the service.
    /// Implements IMessage for integration with the messaging bus and correlation tracking.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public readonly record struct SerializationFormatRegisteredMessage : IMessage
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
        /// Gets the serialization format that was registered.
        /// </summary>
        public SerializationFormat Format { get; init; }

        /// <summary>
        /// Gets the serializer type name.
        /// </summary>
        public FixedString128Bytes SerializerTypeName { get; init; }

        /// <summary>
        /// Gets whether circuit breaker was created for this format.
        /// </summary>
        public bool CircuitBreakerCreated { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new SerializationFormatRegisteredMessage with proper validation and defaults.
        /// </summary>
        /// <param name="format">Serialization format that was registered</param>
        /// <param name="serializerTypeName">Serializer type name</param>
        /// <param name="circuitBreakerCreated">Whether circuit breaker was created</param>
        /// <param name="source">Source component creating this message</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <param name="priority">Message priority level</param>
        /// <returns>New SerializationFormatRegisteredMessage instance</returns>
        public static SerializationFormatRegisteredMessage Create(
            SerializationFormat format,
            string serializerTypeName,
            bool circuitBreakerCreated = true,
            FixedString64Bytes source = default,
            Guid correlationId = default,
            MessagePriority priority = MessagePriority.Normal)
        {
            // Input validation
            if (string.IsNullOrEmpty(serializerTypeName))
                throw new ArgumentException("Serializer type name cannot be null or empty", nameof(serializerTypeName));

            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "SerializationService" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId(
                messageType: "SerializationFormatRegisteredMessage",
                source: sourceString,
                correlationId: null);
            var finalCorrelationId = correlationId == default
                ? DeterministicIdGenerator.GenerateCorrelationId("SerializationRegistration", format.ToString())
                : correlationId;

            return new SerializationFormatRegisteredMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.SerializationFormatRegisteredMessage,
                Source = source.IsEmpty ? "SerializationService" : source,
                Priority = priority,
                CorrelationId = finalCorrelationId,
                Format = format,
                SerializerTypeName = serializerTypeName?.Length <= 128 ? serializerTypeName : serializerTypeName?[..128] ?? string.Empty,
                CircuitBreakerCreated = circuitBreakerCreated
            };
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message for debugging.
        /// </summary>
        /// <returns>Serialization format registered message string representation</returns>
        public override string ToString()
        {
            return $"SerializationFormatRegistered: {Format} with {SerializerTypeName}";
        }

        #endregion
    }
}