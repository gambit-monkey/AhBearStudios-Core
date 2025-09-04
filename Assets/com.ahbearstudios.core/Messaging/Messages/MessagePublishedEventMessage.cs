using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a message is successfully published.
/// Replaces MessagePublishedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessagePublishedEventMessage : IMessage
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
    /// Gets the message that was published.
    /// </summary>
    public IMessage PublishedMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was published.
    /// </summary>
    public DateTime PublishedAt { get; init; }

    /// <summary>
    /// Gets the name of the publisher.
    /// </summary>
    public FixedString64Bytes PublisherName { get; init; }

    /// <summary>
    /// Gets the time it took to publish the message.
    /// </summary>
    public TimeSpan PublishDuration { get; init; }

    /// <summary>
    /// Gets the number of subscribers that received the message.
    /// </summary>
    public int SubscriberCount { get; init; }

    /// <summary>
    /// Gets additional context about the publishing.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessagePublishedEventMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="publishedMessage">The message that was published</param>
    /// <param name="publisherName">The name of the publisher</param>
    /// <param name="publishDuration">The time it took to publish</param>
    /// <param name="subscriberCount">The number of subscribers</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePublishedEventMessage instance</returns>
    public static MessagePublishedEventMessage CreateFromFixedStrings(
        IMessage publishedMessage,
        string publisherName,
        TimeSpan publishDuration,
        int subscriberCount,
        string context,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusPublish", null)
            : correlationId;

        return new MessagePublishedEventMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessagePublishedEventMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusPublishedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            PublishedMessage = publishedMessage,
            PublishedAt = DateTime.UtcNow,
            PublisherName = publisherName?.Length <= 64 ? publisherName : publisherName?[..64] ?? string.Empty,
            PublishDuration = publishDuration,
            SubscriberCount = Math.Max(0, subscriberCount),
            Context = context?.Length <= 512 ? context : context?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessagePublishedEventMessage using string parameters.
    /// </summary>
    /// <param name="publishedMessage">The message that was published</param>
    /// <param name="publisherName">The name of the publisher</param>
    /// <param name="publishDuration">The time it took to publish</param>
    /// <param name="subscriberCount">The number of subscribers</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessagePublishedEventMessage instance</returns>
    public static MessagePublishedEventMessage Create(
        IMessage publishedMessage,
        string publisherName = null,
        TimeSpan publishDuration = default,
        int subscriberCount = 0,
        string context = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            publishedMessage,
            publisherName,
            publishDuration,
            subscriberCount,
            context,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}