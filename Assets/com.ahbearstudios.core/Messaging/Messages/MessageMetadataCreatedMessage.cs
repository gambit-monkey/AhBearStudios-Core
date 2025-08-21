using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when new message metadata is created.
/// Used for tracking and monitoring metadata creation events.
/// </summary>
public record struct MessageMetadataCreatedMessage : IMessage
{
    /// <inheritdoc/>
    public Guid Id { get; init; }

    /// <inheritdoc/>
    public long TimestampTicks { get; init; }

    /// <inheritdoc/>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusMetadataCreatedMessage;

    /// <inheritdoc/>
    public FixedString64Bytes Source { get; init; }

    /// <inheritdoc/>
    public MessagePriority Priority { get; init; }

    /// <inheritdoc/>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the ID of the metadata that was created.
    /// </summary>
    public Guid MetadataId { get; init; }

    /// <summary>
    /// Gets the type code of the message associated with the metadata.
    /// </summary>
    public ushort MessageTypeCode { get; init; }

    /// <summary>
    /// Gets the delivery mode of the created metadata.
    /// </summary>
    public MessageDeliveryMode DeliveryMode { get; init; }

    /// <summary>
    /// Gets whether the metadata is for a persistent message.
    /// </summary>
    public bool IsPersistent { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageMetadataCreatedMessage struct.
    /// </summary>
    public MessageMetadataCreatedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MetadataId = default;
        MessageTypeCode = default;
        DeliveryMode = default;
        IsPersistent = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new MessageMetadataCreatedMessage with default values.
    /// </summary>
    public static MessageMetadataCreatedMessage Create(
        Guid metadataId,
        ushort messageTypeCode,
        MessageDeliveryMode deliveryMode = MessageDeliveryMode.Standard,
        bool isPersistent = false,
        FixedString64Bytes source = default,
        Guid? correlationId = null)
    {
        return new MessageMetadataCreatedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusMetadataCreatedMessage,
            Source = source,
            Priority = MessagePriority.Normal,
            CorrelationId = correlationId ?? Guid.NewGuid(),
            MetadataId = metadataId,
            MessageTypeCode = messageTypeCode,
            DeliveryMode = deliveryMode,
            IsPersistent = isPersistent
        };
    }
}