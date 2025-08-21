using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when message metadata is updated.
/// Used for tracking metadata changes and audit logging.
/// </summary>
public record struct MessageMetadataUpdatedMessage : IMessage
{
    /// <inheritdoc/>
    public Guid Id { get; init; }

    /// <inheritdoc/>
    public long TimestampTicks { get; init; }

    /// <inheritdoc/>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusMetadataUpdatedMessage;

    /// <inheritdoc/>
    public FixedString64Bytes Source { get; init; }

    /// <inheritdoc/>
    public MessagePriority Priority { get; init; }

    /// <inheritdoc/>
    public Guid CorrelationId { get; init; }

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

    /// <summary>
    /// Initializes a new instance of the MessageMetadataUpdatedMessage struct.
    /// </summary>
    public MessageMetadataUpdatedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MetadataId = default;
        UpdateType = default;
        PropertyName = default;
        OldValue = default;
        NewValue = default;
        DeliveryAttempts = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new MessageMetadataUpdatedMessage with default values.
    /// </summary>
    public static MessageMetadataUpdatedMessage Create(
        Guid metadataId,
        string updateType,
        string propertyName,
        string oldValue,
        string newValue,
        int deliveryAttempts = 0,
        FixedString64Bytes source = default,
        Guid? correlationId = null)
    {
        return new MessageMetadataUpdatedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusMetadataUpdatedMessage,
            Source = source,
            Priority = MessagePriority.Normal,
            CorrelationId = correlationId ?? Guid.NewGuid(),
            MetadataId = metadataId,
            UpdateType = new FixedString32Bytes(updateType ?? "Unknown"),
            PropertyName = new FixedString64Bytes(propertyName ?? "Unknown"),
            OldValue = new FixedString128Bytes(oldValue ?? string.Empty),
            NewValue = new FixedString128Bytes(newValue ?? string.Empty),
            DeliveryAttempts = deliveryAttempts
        };
    }
}