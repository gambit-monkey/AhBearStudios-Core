using System;
using AhBearStudios.Core.Common.Utilities;
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessagePipePublishSucceededMessage using FixedString parameters for optimal performance.
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
    public static MessagePipePublishSucceededMessage CreateFromFixedStrings(
        Type messageType,
        Guid messageId,
        TimeSpan processingTime = default,
        int serializedSize = 0,
        int subscriberCount = 0,
        FixedString64Bytes channelName = default,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessagePipe", null)
            : correlationId;

        return new MessagePipePublishSucceededMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessagePipePublishSucceededMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipePublishSucceededMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Low, // Publish success events are informational
            CorrelationId = finalCorrelationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            MessageId = messageId,
            ProcessingTime = processingTime,
            SerializedSize = Math.Max(0, serializedSize),
            SubscriberCount = Math.Max(0, subscriberCount),
            ChannelName = channelName.IsEmpty ? "Default" : channelName
        };
    }

    /// <summary>
    /// Creates a new instance of MessagePipePublishSucceededMessage using string parameters.
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
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            messageType,
            messageId,
            processingTime,
            serializedSize,
            subscriberCount,
            channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default",
            source?.Length <= 64 ? source : source?[..64] ?? "MessagePipe",
            correlationId);
    }

    #endregion
}