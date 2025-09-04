using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a message subscriber is disposed.
/// Provides information about disposed subscriber instances for cleanup tracking and diagnostics.
/// </summary>
public readonly record struct MessageBusSubscriberDisposedMessage : IMessage
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
    /// Gets the subscriber ID that was disposed.
    /// </summary>
    public Guid SubscriberId { get; init; }

    /// <summary>
    /// Gets the message type that the subscriber handled.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the subscription category.
    /// </summary>
    public FixedString64Bytes Category { get; init; }

    /// <summary>
    /// Gets the timestamp when the subscriber was disposed.
    /// </summary>
    public DateTime DisposedAt { get; init; }

    /// <summary>
    /// Gets the subscriber name or identifier.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the reason for disposal.
    /// </summary>
    public FixedString128Bytes DisposalReason { get; init; }

    /// <summary>
    /// Gets additional context about the subscriber disposal.
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
    /// Creates a new instance of MessageBusSubscriberDisposedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID that was disposed</param>
    /// <param name="messageType">The message type that the subscriber handled</param>
    /// <param name="category">The subscription category</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="disposalReason">The reason for disposal</param>
    /// <param name="context">Additional context about the subscriber disposal</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriberDisposedMessage instance</returns>
    public static MessageBusSubscriberDisposedMessage CreateFromFixedStrings(
        Guid subscriberId,
        Type messageType,
        string category,
        string subscriberName,
        string disposalReason,
        string context,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusSubscriber", null)
            : correlationId;

        return new MessageBusSubscriberDisposedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusSubscriberDisposedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusSubscriberDisposedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            SubscriberId = subscriberId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            Category = category?.Length <= 64 ? category : category?[..64] ?? "Default",
            DisposedAt = DateTime.UtcNow,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            DisposalReason = disposalReason?.Length <= 128 ? disposalReason : disposalReason?[..128] ?? "Normal disposal",
            Context = context?.Length <= 512 ? context : context?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusSubscriberDisposedMessage using string parameters.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID that was disposed</param>
    /// <param name="messageType">The message type that the subscriber handled</param>
    /// <param name="category">The subscription category</param>
    /// <param name="subscriberName">The subscriber name or identifier</param>
    /// <param name="disposalReason">The reason for disposal</param>
    /// <param name="context">Additional context about the subscriber disposal</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusSubscriberDisposedMessage instance</returns>
    public static MessageBusSubscriberDisposedMessage Create(
        Guid subscriberId,
        Type messageType,
        string category = null,
        string subscriberName = null,
        string disposalReason = null,
        string context = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            subscriberId,
            messageType,
            category,
            subscriberName,
            disposalReason,
            context,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}