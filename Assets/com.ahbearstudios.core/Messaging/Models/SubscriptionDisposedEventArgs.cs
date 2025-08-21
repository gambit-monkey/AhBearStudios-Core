using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Event arguments for subscription disposal events.
/// Provides comprehensive information about disposed subscriptions for monitoring and cleanup tracking.
/// Follows AhBearStudios Core Development Guidelines with immutable design and comprehensive error context.
/// </summary>
public sealed class SubscriptionDisposedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the unique identifier for the disposed subscription.
    /// </summary>
    public Guid SubscriptionId { get; }

    /// <summary>
    /// Gets the reason for subscription disposal.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the reason code for programmatic handling.
    /// </summary>
    public DisposalReason ReasonCode { get; }

    /// <summary>
    /// Gets the timestamp when the subscription was disposed (UTC).
    /// </summary>
    public DateTime DisposedAt { get; }

    /// <summary>
    /// Gets the timestamp in ticks for high-performance scenarios.
    /// </summary>
    public long DisposedAtTicks { get; }

    /// <summary>
    /// Gets the lifetime duration of the subscription.
    /// </summary>
    public TimeSpan Lifetime { get; }

    /// <summary>
    /// Gets the optional exception that caused disposal (if applicable).
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets additional context information about the disposal.
    /// </summary>
    public FixedString128Bytes Context { get; }

    /// <summary>
    /// Gets whether the disposal was due to an error condition.
    /// </summary>
    public bool IsErrorDisposal => Exception != null || ReasonCode == DisposalReason.Error;

    /// <summary>
    /// Gets whether the disposal was planned (explicit or scope cleanup).
    /// </summary>
    public bool IsPlannedDisposal => ReasonCode == DisposalReason.Explicit || ReasonCode == DisposalReason.ScopeCleanup;

    /// <summary>
    /// Initializes a new instance of the SubscriptionDisposedEventArgs class.
    /// </summary>
    /// <param name="subscriptionId">The unique subscription identifier</param>
    /// <param name="reason">The reason for disposal</param>
    /// <param name="reasonCode">The disposal reason code</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="exception">Optional exception that caused disposal</param>
    /// <param name="context">Additional context information</param>
    /// <exception cref="ArgumentException">Thrown when subscriptionId is empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when lifetime is negative</exception>
    public SubscriptionDisposedEventArgs(
        Guid subscriptionId,
        string reason,
        DisposalReason reasonCode = DisposalReason.Explicit,
        TimeSpan lifetime = default,
        Exception exception = null,
        string context = null)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID cannot be empty", nameof(subscriptionId));

        if (lifetime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Lifetime cannot be negative");

        SubscriptionId = subscriptionId;
        Reason = reason ?? "Unknown";
        ReasonCode = reasonCode;
        Lifetime = lifetime;
        Exception = exception;
        Context = context ?? string.Empty;
        DisposedAt = DateTime.UtcNow;
        DisposedAtTicks = DisposedAt.Ticks;
    }

    /// <summary>
    /// Initializes a new instance with full timestamp control (for testing scenarios).
    /// </summary>
    /// <param name="subscriptionId">The unique subscription identifier</param>
    /// <param name="reason">The reason for disposal</param>
    /// <param name="disposedAt">The disposal timestamp</param>
    /// <param name="reasonCode">The disposal reason code</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="exception">Optional exception that caused disposal</param>
    /// <param name="context">Additional context information</param>
    /// <exception cref="ArgumentException">Thrown when subscriptionId is empty</exception>
    internal SubscriptionDisposedEventArgs(
        Guid subscriptionId,
        string reason,
        DateTime disposedAt,
        DisposalReason reasonCode = DisposalReason.Explicit,
        TimeSpan lifetime = default,
        Exception exception = null,
        string context = null)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID cannot be empty", nameof(subscriptionId));

        SubscriptionId = subscriptionId;
        Reason = reason ?? "Unknown";
        ReasonCode = reasonCode;
        Lifetime = lifetime;
        Exception = exception;
        Context = context ?? string.Empty;
        DisposedAt = disposedAt;
        DisposedAtTicks = disposedAt.Ticks;
    }

    /// <summary>
    /// Returns a string representation of the subscription disposal event.
    /// </summary>
    /// <returns>Formatted string with disposal details</returns>
    public override string ToString()
    {
        var baseInfo =
            $"SubscriptionDisposed[{SubscriptionId:D}]: {Reason} ({ReasonCode}) at {DisposedAt:yyyy-MM-dd HH:mm:ss.fff} UTC";

        if (Lifetime > TimeSpan.Zero)
            baseInfo += $", Lifetime: {Lifetime.TotalSeconds:F2}s";

        if (Exception != null)
            baseInfo += $", Exception: {Exception.GetType().Name}";

        return baseInfo;
    }

    /// <summary>
    /// Creates an explicit disposal event.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="context">Additional context</param>
    /// <returns>Explicit disposal event args</returns>
    public static SubscriptionDisposedEventArgs ForExplicit(Guid subscriptionId, TimeSpan lifetime = default,
        string context = null) =>
        new(subscriptionId, "Explicit disposal", DisposalReason.Explicit, lifetime, null, context);

    /// <summary>
    /// Creates a scope cleanup disposal event.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="scopeId">The scope identifier</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <returns>Scope cleanup disposal event args</returns>
    public static SubscriptionDisposedEventArgs ForScopeCleanup(Guid subscriptionId, Guid scopeId,
        TimeSpan lifetime = default) =>
        new(subscriptionId, "Scope cleanup", DisposalReason.ScopeCleanup, lifetime, null, $"Scope: {scopeId}");

    /// <summary>
    /// Creates a service shutdown disposal event.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <returns>Service shutdown disposal event args</returns>
    public static SubscriptionDisposedEventArgs ForServiceShutdown(Guid subscriptionId, TimeSpan lifetime = default) =>
        new(subscriptionId, "Service shutdown", DisposalReason.ServiceShutdown, lifetime, null, "MessageBus disposing");

    /// <summary>
    /// Creates an error disposal event.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="exception">The exception that caused disposal</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <param name="context">Additional context</param>
    /// <returns>Error disposal event args</returns>
    public static SubscriptionDisposedEventArgs ForError(Guid subscriptionId, Exception exception,
        TimeSpan lifetime = default, string context = null) =>
        new(subscriptionId, $"Error: {exception?.Message ?? "Unknown error"}", DisposalReason.Error, lifetime,
            exception, context);

    /// <summary>
    /// Creates a timeout disposal event.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="timeout">The timeout that was exceeded</param>
    /// <param name="lifetime">The subscription lifetime</param>
    /// <returns>Timeout disposal event args</returns>
    public static SubscriptionDisposedEventArgs ForTimeout(Guid subscriptionId, TimeSpan timeout,
        TimeSpan lifetime = default) =>
        new(subscriptionId, $"Timeout after {timeout.TotalSeconds:F2}s", DisposalReason.Timeout, lifetime, null,
            $"Timeout: {timeout}");
}