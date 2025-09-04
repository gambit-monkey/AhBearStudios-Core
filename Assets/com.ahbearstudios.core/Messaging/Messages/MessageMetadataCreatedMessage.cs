using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when new message metadata is created.
/// Used for tracking and monitoring metadata creation events.
/// </summary>
public readonly record struct MessageMetadataCreatedMessage : IMessage
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageMetadataCreatedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="metadataId">The ID of the metadata that was created</param>
    /// <param name="messageTypeCode">The type code of the message associated with the metadata</param>
    /// <param name="deliveryMode">The delivery mode of the created metadata</param>
    /// <param name="isPersistent">Whether the metadata is for a persistent message</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageMetadataCreatedMessage instance</returns>
    public static MessageMetadataCreatedMessage CreateFromFixedStrings(
        Guid metadataId,
        ushort messageTypeCode,
        MessageDeliveryMode deliveryMode = MessageDeliveryMode.Standard,
        bool isPersistent = false,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageMetadata", null)
            : correlationId;

        return new MessageMetadataCreatedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageMetadataCreatedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusMetadataCreatedMessage,
            Source = source.IsEmpty ? "MessageMetadata" : source,
            Priority = MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            MetadataId = metadataId,
            MessageTypeCode = messageTypeCode,
            DeliveryMode = deliveryMode,
            IsPersistent = isPersistent
        };
    }

    /// <summary>
    /// Creates a new instance of MessageMetadataCreatedMessage using string parameters.
    /// </summary>
    /// <param name="metadataId">The ID of the metadata that was created</param>
    /// <param name="messageTypeCode">The type code of the message associated with the metadata</param>
    /// <param name="deliveryMode">The delivery mode of the created metadata</param>
    /// <param name="isPersistent">Whether the metadata is for a persistent message</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageMetadataCreatedMessage instance</returns>
    public static MessageMetadataCreatedMessage Create(
        Guid metadataId,
        ushort messageTypeCode,
        MessageDeliveryMode deliveryMode = MessageDeliveryMode.Standard,
        bool isPersistent = false,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            metadataId,
            messageTypeCode,
            deliveryMode,
            isPersistent,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageMetadata",
            correlationId);
    }

    #endregion
}