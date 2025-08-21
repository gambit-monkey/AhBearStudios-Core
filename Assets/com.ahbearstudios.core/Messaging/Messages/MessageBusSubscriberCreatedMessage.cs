using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a message subscriber is created.
/// Provides information about new subscriber instances for monitoring and diagnostics.
/// </summary>
public record struct MessageBusSubscriberCreatedMessage : IMessage
{
    /// <summary>
    /// Gets the unique identifier for this message.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was created (in ticks).
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the message type code for routing and identification.
    /// </summary>
    public ushort TypeCode { get; init; }

    /// <summary>
    /// Gets the source system that generated this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the message priority level.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets the correlation identifier for message tracking.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the subscriber creation event arguments containing detailed information.
    /// </summary>
    public SubscriptionCreatedEventArgs EventArgs { get; init; }

    /// <summary>
    /// Gets the message type that the subscriber handles.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the subscription category.
    /// </summary>
    public SubscriptionCategory Category { get; init; }

    /// <summary>
    /// Initializes a new instance of MessageBusSubscriberCreatedMessage.
    /// </summary>
    /// <param name="eventArgs">The subscription creation event arguments</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MessageBusSubscriberCreatedMessage(SubscriptionCreatedEventArgs eventArgs, Guid correlationId = default)
    {
        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = MessageTypeCodes.MessageBusSubscriberCreatedMessage;
        Source = "MessageBus";
        Priority = MessagePriority.Normal;
        CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
        
        EventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
        MessageType = eventArgs.MessageType;
        Category = eventArgs.Category;
    }

    /// <summary>
    /// Creates a message for a standard subscription creation.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber created message</returns>
    public static MessageBusSubscriberCreatedMessage ForStandard(Guid subscriptionId, Type messageType, Guid correlationId = default) =>
        new(SubscriptionCreatedEventArgs.ForStandard(subscriptionId, messageType), correlationId);

    /// <summary>
    /// Creates a message for an async subscription creation.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber created message</returns>
    public static MessageBusSubscriberCreatedMessage ForAsync(Guid subscriptionId, Type messageType, Guid correlationId = default) =>
        new(SubscriptionCreatedEventArgs.ForAsync(subscriptionId, messageType), correlationId);

    /// <summary>
    /// Creates a message for a filtered subscription creation.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type</param>
    /// <param name="filterDescription">Description of the filter</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber created message</returns>
    public static MessageBusSubscriberCreatedMessage ForFiltered(Guid subscriptionId, Type messageType, string filterDescription, Guid correlationId = default) =>
        new(SubscriptionCreatedEventArgs.ForFiltered(subscriptionId, messageType, filterDescription), correlationId);

    /// <summary>
    /// Creates a message for a priority subscription creation.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="messageType">The message type</param>
    /// <param name="minPriority">The minimum priority level</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber created message</returns>
    public static MessageBusSubscriberCreatedMessage ForPriority(Guid subscriptionId, Type messageType, MessagePriority minPriority, Guid correlationId = default) =>
        new(SubscriptionCreatedEventArgs.ForPriority(subscriptionId, messageType, minPriority), correlationId);
}