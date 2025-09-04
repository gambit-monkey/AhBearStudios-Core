using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a MessagePipe subscription is created.
/// Replaces MessagePipe subscription creation events with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePipeSubscriptionCreatedMessage : IMessage
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
    /// Gets the subscription identifier.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the message type for the subscription.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the MessagePipe channel name for the subscription.
    /// </summary>
    public FixedString64Bytes ChannelName { get; init; }

    /// <summary>
    /// Gets the subscriber name or identifier.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets whether the subscription is for a persistent channel.
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
    /// Creates a new instance of MessagePipeSubscriptionCreatedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type for the subscription</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="isPersistent">Whether the subscription is persistent</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeSubscriptionCreatedMessage instance</returns>
    public static MessagePipeSubscriptionCreatedMessage CreateFromFixedStrings(
        Guid subscriptionId,
        Type messageType,
        FixedString64Bytes channelName,
        FixedString64Bytes subscriberName,
        bool isPersistent = false,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessagePipe", null)
            : correlationId;

        return new MessagePipeSubscriptionCreatedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessagePipeSubscriptionCreatedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessagePipeSubscriptionCreatedMessage,
            Source = source.IsEmpty ? "MessagePipe" : source,
            Priority = MessagePriority.Low, // Subscription creation is informational
            CorrelationId = finalCorrelationId,
            SubscriptionId = subscriptionId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            ChannelName = channelName.IsEmpty ? "Default" : channelName,
            SubscriberName = subscriberName.IsEmpty ? "Unknown" : subscriberName,
            IsPersistent = isPersistent
        };
    }

    /// <summary>
    /// Creates a new instance of MessagePipeSubscriptionCreatedMessage using string parameters.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type for the subscription</param>
    /// <param name="channelName">The MessagePipe channel name</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="isPersistent">Whether the subscription is persistent</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePipeSubscriptionCreatedMessage instance</returns>
    public static MessagePipeSubscriptionCreatedMessage Create(
        Guid subscriptionId,
        Type messageType,
        string channelName = null,
        string subscriberName = null,
        bool isPersistent = false,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            subscriptionId,
            messageType,
            new FixedString64Bytes(channelName ?? "Default"),
            new FixedString64Bytes(subscriberName ?? "Unknown"),
            isPersistent,
            source?.Length <= 64 ? source : source?[..64] ?? "MessagePipe",
            correlationId);
    }

    #endregion
}