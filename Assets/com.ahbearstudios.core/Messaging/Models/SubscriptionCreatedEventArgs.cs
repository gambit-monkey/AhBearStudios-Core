using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Models;

 /// <summary>
    /// Event arguments for subscription creation events.
    /// Provides comprehensive information about newly created subscriptions for monitoring and diagnostics.
    /// Follows AhBearStudios Core Development Guidelines with immutable design and comprehensive validation.
    /// </summary>
    public sealed class SubscriptionCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier for the created subscription.
        /// </summary>
        public Guid SubscriptionId { get; }

        /// <summary>
        /// Gets the message type for the subscription.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the name of the message type for performance optimization.
        /// </summary>
        public FixedString64Bytes MessageTypeName { get; }

        /// <summary>
        /// Gets the timestamp when the subscription was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp in ticks for high-performance scenarios.
        /// </summary>
        public long CreatedAtTicks { get; }

        /// <summary>
        /// Gets the subscription type identifier for categorization.
        /// </summary>
        public SubscriptionCategory Category { get; }

        /// <summary>
        /// Gets optional subscription metadata for extended information.
        /// </summary>
        public SubscriptionMetadata Metadata { get; }

        /// <summary>
        /// Gets whether this subscription has filtering enabled.
        /// </summary>
        public bool HasFiltering => Category == SubscriptionCategory.Filtered || Category == SubscriptionCategory.Priority;

        /// <summary>
        /// Gets whether this subscription is asynchronous.
        /// </summary>
        public bool IsAsync => Category == SubscriptionCategory.Async || Category == SubscriptionCategory.AsyncFiltered;

        /// <summary>
        /// Initializes a new instance of the SubscriptionCreatedEventArgs class.
        /// </summary>
        /// <param name="subscriptionId">The unique subscription identifier</param>
        /// <param name="messageType">The message type for the subscription</param>
        /// <param name="category">The subscription category</param>
        /// <param name="metadata">Optional subscription metadata</param>
        /// <exception cref="ArgumentNullException">Thrown when messageType is null</exception>
        /// <exception cref="ArgumentException">Thrown when subscriptionId is empty</exception>
        public SubscriptionCreatedEventArgs(
            Guid subscriptionId, 
            Type messageType, 
            SubscriptionCategory category = SubscriptionCategory.Standard,
            SubscriptionMetadata metadata = null)
        {
            if (subscriptionId == Guid.Empty)
                throw new ArgumentException("Subscription ID cannot be empty", nameof(subscriptionId));

            SubscriptionId = subscriptionId;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            MessageTypeName = messageType.Name;
            Category = category;
            Metadata = metadata ?? SubscriptionMetadata.Empty;
            CreatedAt = DateTime.UtcNow;
            CreatedAtTicks = CreatedAt.Ticks;
        }

        /// <summary>
        /// Initializes a new instance with full timestamp control (for testing scenarios).
        /// </summary>
        /// <param name="subscriptionId">The unique subscription identifier</param>
        /// <param name="messageType">The message type for the subscription</param>
        /// <param name="createdAt">The creation timestamp</param>
        /// <param name="category">The subscription category</param>
        /// <param name="metadata">Optional subscription metadata</param>
        /// <exception cref="ArgumentNullException">Thrown when messageType is null</exception>
        /// <exception cref="ArgumentException">Thrown when subscriptionId is empty</exception>
        internal SubscriptionCreatedEventArgs(
            Guid subscriptionId,
            Type messageType,
            DateTime createdAt,
            SubscriptionCategory category = SubscriptionCategory.Standard,
            SubscriptionMetadata metadata = null)
        {
            if (subscriptionId == Guid.Empty)
                throw new ArgumentException("Subscription ID cannot be empty", nameof(subscriptionId));

            SubscriptionId = subscriptionId;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            MessageTypeName = messageType.Name;
            Category = category;
            Metadata = metadata ?? SubscriptionMetadata.Empty;
            CreatedAt = createdAt;
            CreatedAtTicks = createdAt.Ticks;
        }

        /// <summary>
        /// Returns a string representation of the subscription creation event.
        /// </summary>
        /// <returns>Formatted string with subscription details</returns>
        public override string ToString() =>
            $"SubscriptionCreated[{SubscriptionId:D}]: {MessageTypeName} ({Category}) at {CreatedAt:yyyy-MM-dd HH:mm:ss.fff} UTC";

        /// <summary>
        /// Creates a standard subscription creation event.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier</param>
        /// <param name="messageType">The message type</param>
        /// <returns>Standard subscription creation event args</returns>
        public static SubscriptionCreatedEventArgs ForStandard(Guid subscriptionId, Type messageType) =>
            new(subscriptionId, messageType, SubscriptionCategory.Standard);

        /// <summary>
        /// Creates an async subscription creation event.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier</param>
        /// <param name="messageType">The message type</param>
        /// <returns>Async subscription creation event args</returns>
        public static SubscriptionCreatedEventArgs ForAsync(Guid subscriptionId, Type messageType) =>
            new(subscriptionId, messageType, SubscriptionCategory.Async);

        /// <summary>
        /// Creates a filtered subscription creation event.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier</param>
        /// <param name="messageType">The message type</param>
        /// <param name="filterDescription">Description of the filter</param>
        /// <returns>Filtered subscription creation event args</returns>
        public static SubscriptionCreatedEventArgs ForFiltered(Guid subscriptionId, Type messageType, string filterDescription) =>
            new(subscriptionId, messageType, SubscriptionCategory.Filtered, 
                new SubscriptionMetadata(filterDescription, null, null));

        /// <summary>
        /// Creates a priority subscription creation event.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier</param>
        /// <param name="messageType">The message type</param>
        /// <param name="minPriority">The minimum priority level</param>
        /// <returns>Priority subscription creation event args</returns>
        public static SubscriptionCreatedEventArgs ForPriority(Guid subscriptionId, Type messageType, MessagePriority minPriority) =>
            new(subscriptionId, messageType, SubscriptionCategory.Priority, 
                new SubscriptionMetadata($"MinPriority: {minPriority}", minPriority, null));
    }

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