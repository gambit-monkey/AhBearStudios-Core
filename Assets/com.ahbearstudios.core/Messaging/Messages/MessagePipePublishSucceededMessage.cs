using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when MessagePipe successfully publishes a message.
/// Replaces MessagePipe publish success events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipePublishSucceededMessage : IMessage
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
    /// Gets the unique type code for this message type.
    /// </summary>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessagePipePublishSucceededMessage;

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
    /// Gets the message type that was published.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the published message ID.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Gets the processing time for the publish operation.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the serialized message size in bytes.
    /// </summary>
    public int SerializedSize { get; init; }

    /// <summary>
    /// Gets the number of subscribers that received the message.
    /// </summary>
    public int SubscriberCount { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessagePipePublishSucceededMessage struct.
    /// </summary>
    public MessagePipePublishSucceededMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MessageType = default;
        MessageId = default;
        ProcessingTime = default;
        SerializedSize = default;
        SubscriberCount = default;
        ChannelName = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessagePipePublishSucceededMessage.
    /// </summary>
    /// <param name="messageType">The message type that was published</param>
    /// <param name="messageId">The published message ID</param>
    /// <param name="processingTime">The processing time for publish</param>
    /// <param name="serializedSize">The serialized message size</param>
    /// <param name="subscriberCount">Number of subscribers that received the message</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipePublishSucceededMessage instance</returns>
    public static MessagePipePublishSucceededMessage Create(
        Type messageType,
        Guid messageId,
        TimeSpan processingTime = default,
        int serializedSize = 0,
        int subscriberCount = 0,
        string channelName = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessagePipePublishSucceededMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipePublishSucceededMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Low, // Publish success events are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            MessageId = messageId,
            ProcessingTime = processingTime,
            SerializedSize = Math.Max(0, serializedSize),
            SubscriberCount = Math.Max(0, subscriberCount),
            ChannelName = channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default"
        };
    }
}