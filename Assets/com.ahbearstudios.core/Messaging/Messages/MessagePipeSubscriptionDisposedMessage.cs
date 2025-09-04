using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a MessagePipe subscription is disposed.
/// Replaces MessagePipe subscription disposal events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipeSubscriptionDisposedMessage : IMessage
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
    /// Gets the subscription identifier that was disposed.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type for the disposed subscription.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name for the disposed subscription.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Gets the subscriber name or identifier.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the subscription duration in milliseconds.
    /// </summary>
    public double SubscriptionDurationMs { get; init; }

    /// <summary>
    /// Gets the total number of messages received by this subscription.
    /// </summary>
    public int MessagesReceived { get; init; }

    /// <summary>
    /// Gets the reason for disposal.
    /// </summary>
    public FixedString128Bytes DisposalReason { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the subscription duration as a TimeSpan.
    /// </summary>
    public TimeSpan SubscriptionDuration => TimeSpan.FromMilliseconds(SubscriptionDurationMs);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessagePipeSubscriptionDisposedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier that was disposed</param>
    /// <param name="messageType">The message type for the disposed subscription</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="subscriptionDurationMs">The subscription duration in milliseconds</param>
    /// <param name="messagesReceived">Total messages received by this subscription</param>
    /// <param name="disposalReason">The reason for disposal</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeSubscriptionDisposedMessage instance</returns>
    public static MessagePipeSubscriptionDisposedMessage CreateFromFixedStrings(
        Guid subscriptionId,
        Type messageType,
        FixedString64Bytes channelName,
        FixedString64Bytes subscriberName,
        double subscriptionDurationMs,
        int messagesReceived,
        FixedString128Bytes disposalReason,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessagePipe", null)
            : correlationId;

        return new MessagePipeSubscriptionDisposedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessagePipeSubscriptionDisposedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipeSubscriptionDisposedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Low, // Subscription disposal is informational
            CorrelationId = finalCorrelationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            ChannelName = channelName.IsEmpty ? "Default" : channelName,
            SubscriberName = subscriberName.IsEmpty ? "Unknown" : subscriberName,
            SubscriptionDurationMs = Math.Max(0, subscriptionDurationMs),
            MessagesReceived = Math.Max(0, messagesReceived),
            DisposalReason = disposalReason.IsEmpty ? "Normal disposal" : disposalReason
        };
    }

    /// <summary>
    /// Creates a new instance of MessagePipeSubscriptionDisposedMessage using string parameters.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier that was disposed</param>
    /// <param name="messageType">The message type for the disposed subscription</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="subscriptionDurationMs">The subscription duration in milliseconds</param>
    /// <param name="messagesReceived">Total messages received by this subscription</param>
    /// <param name="disposalReason">The reason for disposal</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeSubscriptionDisposedMessage instance</returns>
    public static MessagePipeSubscriptionDisposedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string channelName = null,
        string subscriberName = null,
        double subscriptionDurationMs = 0,
        int messagesReceived = 0,
        string disposalReason = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            subscriptionId,
            messageType,
            new FixedString64Bytes(channelName ?? "Default"),
            new FixedString64Bytes(subscriberName ?? "Unknown"),
            subscriptionDurationMs,
            messagesReceived,
            new FixedString128Bytes(disposalReason ?? "Normal disposal"),
            source?.Length <= 64 ? source : source?[..64] ?? "MessagePipe",
            correlationId);
    }

    #endregion
}