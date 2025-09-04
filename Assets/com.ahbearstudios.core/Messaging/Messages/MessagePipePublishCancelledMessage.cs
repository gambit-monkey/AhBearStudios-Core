using System;
using AhBearStudios.Core.Common.Utilities;
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessagePipePublishCancelledMessage using FixedString parameters for optimal performance.
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
    public static MessagePipePublishCancelledMessage CreateFromFixedStrings(
        Type messageType,
        Guid messageId,
        TimeSpan processingTime,
        FixedString128Bytes cancellationReason,
        FixedString64Bytes channelName,
        bool isUserRequested = false,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessagePipe", null)
            : correlationId;

        return new MessagePipePublishCancelledMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessagePipePublishCancelledMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipePublishCancelledMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Normal, // Cancellation events are normal priority
            CorrelationId = finalCorrelationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            MessageId = messageId,
            ProcessingTime = processingTime,
            CancellationReason = cancellationReason.IsEmpty ? "Operation cancelled" : cancellationReason,
            ChannelName = channelName.IsEmpty ? "Default" : channelName,
            IsUserRequested = isUserRequested
        };
    }

    /// <summary>
    /// Creates a new instance of MessagePipePublishCancelledMessage using string parameters.
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
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            messageType,
            messageId,
            processingTime,
            new FixedString128Bytes(cancellationReason ?? "Operation cancelled"),
            new FixedString64Bytes(channelName ?? "Default"),
            isUserRequested,
            source?.Length <= 64 ? source : source?[..64] ?? "MessagePipe",
            correlationId);
    }

    #endregion
}