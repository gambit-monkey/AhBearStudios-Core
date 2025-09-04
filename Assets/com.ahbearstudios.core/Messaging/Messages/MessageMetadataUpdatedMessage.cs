using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message metadata is updated.
/// Used for tracking metadata changes and audit logging.
/// </summary>
public readonly record struct MessageMetadataUpdatedMessage : IMessage
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
    /// Gets the ID of the metadata that was updated.
    /// </summary>
    public Guid MetadataId { get; init; }

    /// <summary>
    /// Gets the type of update that was performed.
    /// </summary>
    public FixedString32Bytes UpdateType { get; init; }

    /// <summary>
    /// Gets the name of the property that was updated.
    /// </summary>
    public FixedString64Bytes PropertyName { get; init; }

    /// <summary>
    /// Gets the previous value as a string representation.
    /// </summary>
    public FixedString128Bytes OldValue { get; init; }

    /// <summary>
    /// Gets the new value as a string representation.
    /// </summary>
    public FixedString128Bytes NewValue { get; init; }

    /// <summary>
    /// Gets the number of delivery attempts at the time of update.
    /// </summary>
    public int DeliveryAttempts { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageMetadataUpdatedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="metadataId">The ID of the metadata that was updated</param>
    /// <param name="updateType">The type of update that was performed</param>
    /// <param name="propertyName">The name of the property that was updated</param>
    /// <param name="oldValue">The previous value as a string representation</param>
    /// <param name="newValue">The new value as a string representation</param>
    /// <param name="deliveryAttempts">The number of delivery attempts at the time of update</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageMetadataUpdatedMessage instance</returns>
    public static MessageMetadataUpdatedMessage CreateFromFixedStrings(
        Guid metadataId,
        FixedString32Bytes updateType,
        FixedString64Bytes propertyName,
        FixedString128Bytes oldValue,
        FixedString128Bytes newValue,
        int deliveryAttempts = 0,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageMetadata", null)
            : correlationId;

        return new MessageMetadataUpdatedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageMetadataUpdatedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusMetadataUpdatedMessage,
            Source = source.IsEmpty ? "MessageMetadata" : source,
            Priority = MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            MetadataId = metadataId,
            UpdateType = updateType,
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = newValue,
            DeliveryAttempts = deliveryAttempts
        };
    }

    /// <summary>
    /// Creates a new instance of MessageMetadataUpdatedMessage using string parameters.
    /// </summary>
    /// <param name="metadataId">The ID of the metadata that was updated</param>
    /// <param name="updateType">The type of update that was performed</param>
    /// <param name="propertyName">The name of the property that was updated</param>
    /// <param name="oldValue">The previous value as a string representation</param>
    /// <param name="newValue">The new value as a string representation</param>
    /// <param name="deliveryAttempts">The number of delivery attempts at the time of update</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageMetadataUpdatedMessage instance</returns>
    public static MessageMetadataUpdatedMessage Create(
        Guid metadataId,
        string updateType,
        string propertyName,
        string oldValue,
        string newValue,
        int deliveryAttempts = 0,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            metadataId,
            new FixedString32Bytes(updateType ?? "Unknown"),
            new FixedString64Bytes(propertyName ?? "Unknown"),
            new FixedString128Bytes(oldValue ?? string.Empty),
            new FixedString128Bytes(newValue ?? string.Empty),
            deliveryAttempts,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageMetadata",
            correlationId);
    }

    #endregion
}