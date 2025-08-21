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

