using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a message subscriber is disposed.
/// Provides information about disposed subscriber instances for cleanup tracking and diagnostics.
/// </summary>
public record struct MessageBusSubscriberDisposedMessage : IMessage
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
    /// Gets the subscriber disposal event arguments containing detailed information.
    /// </summary>
    public SubscriptionDisposedEventArgs EventArgs { get; init; }

    /// <summary>
    /// Gets the unique identifier for the disposed subscription.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the reason for subscription disposal.
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Gets the disposal reason code for programmatic handling.
    /// </summary>
    public DisposalReason ReasonCode { get; init; }

    /// <summary>
    /// Gets whether the disposal was due to an error condition.
    /// </summary>
    public bool IsErrorDisposal { get; init; }

    /// <summary>
    /// Initializes a new instance of MessageBusSubscriberDisposedMessage.
    /// </summary>
    /// <param name="eventArgs">The subscription disposal event arguments</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MessageBusSubscriberDisposedMessage(SubscriptionDisposedEventArgs eventArgs, Guid correlationId = default)
    {
        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = MessageTypeCodes.MessageBusSubscriberDisposedMessage;
        Source = "MessageBus";
        Priority = eventArgs.IsErrorDisposal ? MessagePriority.High : MessagePriority.Normal;
        CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
        
        EventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
        SubscriptionId = eventArgs.SubscriptionId;
        Reason = eventArgs.Reason;
        ReasonCode = eventArgs.ReasonCode;
        IsErrorDisposal = eventArgs.IsErrorDisposal;
    }

    /// <summary>
    /// Creates a message for an explicit disposal.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="context">Additional context</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber disposed message</returns>
    public static MessageBusSubscriberDisposedMessage ForExplicit(Guid subscriptionId, TimeSpan lifetime = default, string context = null, Guid correlationId = default) =>
        new(SubscriptionDisposedEventArgs.ForExplicit(subscriptionId, lifetime, context), correlationId);

    /// <summary>
    /// Creates a message for a scope cleanup disposal.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="scopeId">The scope identifier</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber disposed message</returns>
    public static MessageBusSubscriberDisposedMessage ForScopeCleanup(Guid subscriptionId, Guid scopeId, TimeSpan lifetime = default, Guid correlationId = default) =>
        new(SubscriptionDisposedEventArgs.ForScopeCleanup(subscriptionId, scopeId, lifetime), correlationId);

    /// <summary>
    /// Creates a message for a service shutdown disposal.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber disposed message</returns>
    public static MessageBusSubscriberDisposedMessage ForServiceShutdown(Guid subscriptionId, TimeSpan lifetime = default, Guid correlationId = default) =>
        new(SubscriptionDisposedEventArgs.ForServiceShutdown(subscriptionId, lifetime), correlationId);

    /// <summary>
    /// Creates a message for an error disposal.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="exception">The exception that caused disposal</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="context">Additional context</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber disposed message</returns>
    public static MessageBusSubscriberDisposedMessage ForError(Guid subscriptionId, Exception exception, TimeSpan lifetime = default, string context = null, Guid correlationId = default) =>
        new(SubscriptionDisposedEventArgs.ForError(subscriptionId, exception, lifetime, context), correlationId);

    /// <summary>
    /// Creates a message for a timeout disposal.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="timeout">The timeout that was exceeded</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Message bus subscriber disposed message</returns>
    public static MessageBusSubscriberDisposedMessage ForTimeout(Guid subscriptionId, TimeSpan timeout, TimeSpan lifetime = default, Guid correlationId = default) =>
        new(SubscriptionDisposedEventArgs.ForTimeout(subscriptionId, timeout, lifetime), correlationId);
}