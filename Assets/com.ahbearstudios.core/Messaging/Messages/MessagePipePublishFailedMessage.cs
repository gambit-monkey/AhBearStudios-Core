using System;
using AhBearStudios.Core.Common.Utilities;
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessagePipePublishFailedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="messageType">The message type that failed to publish</param>
    /// <param name="messageId">The failed message ID</param>
    /// <param name="error">The error message</param>
    /// <param name="processingTime">The processing time before failure</param>
    /// <param name="retryAttempts">Number of retry attempts made</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="isRetriable">Whether this failure is retriable</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipePublishFailedMessage instance</returns>
    public static MessagePipePublishFailedMessage CreateFromFixedStrings(
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
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessagePipePublishFailed", null)
            : correlationId;

        return new MessagePipePublishFailedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessagePipePublishFailedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipePublishFailedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.High,
            CorrelationId = finalCorrelationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            MessageId = messageId,
            Error = error?.Length <= 512 ? error : error?[..512] ?? throw new ArgumentNullException(nameof(error)),
            ProcessingTime = processingTime,
            RetryAttempts = Math.Max(0, retryAttempts),
            ChannelName = channelName?.Length <= 64 ? channelName : channelName?[..64] ?? "Default",
            IsRetriable = isRetriable
        };
    }

    /// <summary>
    /// Creates a new instance of MessagePipePublishFailedMessage using string parameters.
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
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            messageType,
            messageId,
            error,
            processingTime,
            retryAttempts,
            channelName,
            isRetriable,
            source?.Length <= 64 ? source : source?[..64] ?? "MessagePipe",
            correlationId);
    }

    #endregion
}