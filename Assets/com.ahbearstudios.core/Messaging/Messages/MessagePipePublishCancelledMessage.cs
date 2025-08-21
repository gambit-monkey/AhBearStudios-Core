using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when MessagePipe message publishing is cancelled.
/// Replaces MessagePipe publish cancellation events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipePublishCancelledMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessagePipePublishCancelledMessage;

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
    /// Gets the message type that was cancelled.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the cancelled message ID.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Gets the processing time before cancellation.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the reason for cancellation.
    /// </summary>
    public FixedString128Bytes CancellationReason { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name where cancellation occurred.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Gets whether the cancellation was requested by user code.
    /// </summary>
    public bool IsUserRequested { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessagePipePublishCancelledMessage struct.
    /// </summary>
    public MessagePipePublishCancelledMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MessageType = default;
        MessageId = default;
        ProcessingTime = default;
        CancellationReason = default;
        ChannelName = default;
        IsUserRequested = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessagePipePublishCancelledMessage.
    /// </summary>
    /// <param name="messageType">The message type that was cancelled</param>
    /// <param name="messageId">The cancelled message ID</param>
    /// <param name="processingTime">The processing time before cancellation</param>
    /// <param name="cancellationReason">The reason for cancellation</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="isUserRequested">Whether cancellation was user-requested</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipePublishCancelledMessage instance</returns>
    public static MessagePipePublishCancelledMessage Create(
        Type messageType,
        Guid messageId,
        TimeSpan processingTime = default,
        string cancellationReason = null,
        string channelName = null,
        bool isUserRequested = false,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessagePipePublishCancelledMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipePublishCancelledMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Normal, // Cancellation events are normal priority
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            MessageId = messageId,
            ProcessingTime = processingTime,
            CancellationReason = cancellationReason?.Length <= 128 ? cancellationReason : cancellationReason?[..128] ?? "Operation cancelled",
            ChannelName = channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default",
            IsUserRequested = isUserRequested
        };
    }
}