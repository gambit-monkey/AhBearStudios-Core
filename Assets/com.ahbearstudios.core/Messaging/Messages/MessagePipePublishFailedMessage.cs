using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when MessagePipe fails to publish a message.
/// Replaces MessagePipe publish failure events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipePublishFailedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessagePipePublishFailedMessage;

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
    /// Gets the message type that failed to publish.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the failed message ID.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public FixedString512Bytes Error { get; init; }

    /// <summary>
    /// Gets the processing time before failure.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name where the failure occurred.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Gets whether this failure is retriable.
    /// </summary>
    public bool IsRetriable { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessagePipePublishFailedMessage struct.
    /// </summary>
    public MessagePipePublishFailedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MessageType = default;
        MessageId = default;
        Error = default;
        ProcessingTime = default;
        RetryAttempts = default;
        ChannelName = default;
        IsRetriable = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessagePipePublishFailedMessage.
    /// </summary>
    /// <param name="messageType">The message type that failed to publish</param>
    /// <param name="messageId">The failed message ID</param>
    /// <param name="error">The error message</param>
    /// <param name="processingTime">The processing time before failure</param>
    /// <param name="retryAttempts">Number of retry attempts made</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="isRetriable">Whether this failure is retriable</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipePublishFailedMessage instance</returns>
    public static MessagePipePublishFailedMessage Create(
        Type messageType,
        Guid messageId,
        string error,
        TimeSpan processingTime = default,
        int retryAttempts = 0,
        string channelName = null,
        bool isRetriable = true,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessagePipePublishFailedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipePublishFailedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.High, // Publish failures are high priority
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            MessageId = messageId,
            Error = error?.Length <= 256 ? error : error?[..256] ?? throw new ArgumentNullException(nameof(error)),
            ProcessingTime = processingTime,
            RetryAttempts = Math.Max(0, retryAttempts),
            ChannelName = channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default",
            IsRetriable = isRetriable
        };
    }
}