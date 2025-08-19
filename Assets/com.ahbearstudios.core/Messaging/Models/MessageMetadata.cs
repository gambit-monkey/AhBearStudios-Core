using System;
using System.Collections.Generic;
using ZLinq;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Models;
{
    /// <summary>
    /// Comprehensive metadata container for messages in the messaging system.
    /// Provides routing information, delivery options, and tracing capabilities.
    /// Follows AhBearStudios Core Development Guidelines with immutable design and performance optimization.
    /// </summary>
    public sealed record MessageMetadata
    {
        #region Core Properties

        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid MessageId { get; }

        /// <summary>
        /// Gets the correlation identifier for tracing across system boundaries.
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the conversation identifier for grouping related messages.
        /// </summary>
        public Guid ConversationId { get; }

        /// <summary>
        /// Gets the message type code for efficient routing.
        /// </summary>
        public ushort TypeCode { get; }

        /// <summary>
        /// Gets the message priority level.
        /// </summary>
        public MessagePriority Priority { get; }

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        public FixedString64Bytes Source { get; }

        /// <summary>
        /// Gets the destination system or component for targeted delivery.
        /// </summary>
        public FixedString64Bytes Destination { get; }

        /// <summary>
        /// Gets the message category for organizational purposes.
        /// </summary>
        public FixedString32Bytes Category { get; }

        #endregion

        #region Timing Properties

        /// <summary>
        /// Gets the timestamp when the message was created (UTC ticks).
        /// </summary>
        public long CreatedAtTicks { get; }

        /// <summary>
        /// Gets the timestamp when the message expires (UTC ticks). Zero means no expiration.
        /// </summary>
        public long ExpiresAtTicks { get; }

        /// <summary>
        /// Gets the maximum time to live for this message.
        /// </summary>
        public TimeSpan TimeToLive { get; }

        /// <summary>
        /// Gets the delivery delay for scheduled message processing.
        /// </summary>
        public TimeSpan DeliveryDelay { get; }

        #endregion

        #region Delivery Properties

        /// <summary>
        /// Gets the delivery mode for this message.
        /// </summary>
        public MessageDeliveryMode DeliveryMode { get; }

        /// <summary>
        /// Gets the number of delivery attempts made for this message.
        /// </summary>
        public int DeliveryAttempts { get; }

        /// <summary>
        /// Gets the maximum number of delivery attempts allowed.
        /// </summary>
        public int MaxDeliveryAttempts { get; }

        /// <summary>
        /// Gets whether this message requires acknowledgment.
        /// </summary>
        public bool RequiresAcknowledgment { get; }

        /// <summary>
        /// Gets whether this message should be persisted for durability.
        /// </summary>
        public bool IsPersistent { get; }

        /// <summary>
        /// Gets whether this message should be compressed during transport.
        /// </summary>
        public bool IsCompressed { get; }

        #endregion

        #region Routing Properties

        /// <summary>
        /// Gets the routing strategy for this message.
        /// </summary>
        public MessageRoutingStrategy RoutingStrategy { get; }

        /// <summary>
        /// Gets the routing tags for advanced filtering and routing decisions.
        /// </summary>
        public IReadOnlyList<FixedString32Bytes> RoutingTags { get; }

        /// <summary>
        /// Gets the routing context for rule evaluation.
        /// </summary>
        public IReadOnlyDictionary<string, object> RoutingContext { get; }

        /// <summary>
        /// Gets the reply-to address for response messages.
        /// </summary>
        public FixedString128Bytes ReplyTo { get; }

        /// <summary>
        /// Gets the dead letter queue destination for failed messages.
        /// </summary>
        public FixedString128Bytes DeadLetterQueue { get; }

        #endregion

        #region Security Properties

        /// <summary>
        /// Gets the security token for message authentication.
        /// </summary>
        public FixedString512Bytes SecurityToken { get; }

        /// <summary>
        /// Gets the security level required for processing this message.
        /// </summary>
        public MessageSecurityLevel SecurityLevel { get; }

        /// <summary>
        /// Gets whether this message contains sensitive data.
        /// </summary>
        public bool ContainsSensitiveData { get; }

        /// <summary>
        /// Gets whether this message should be encrypted during transport.
        /// </summary>
        public bool RequiresEncryption { get; }

        #endregion

        #region Custom Properties

        /// <summary>
        /// Gets custom properties for application-specific metadata.
        /// </summary>
        public IReadOnlyDictionary<string, object> CustomProperties { get; }

        /// <summary>
        /// Gets custom headers for protocol-specific metadata.
        /// </summary>
        public IReadOnlyDictionary<string, string> CustomHeaders { get; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the creation timestamp.
        /// </summary>
        public DateTime CreatedAt => new DateTime(CreatedAtTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the DateTime representation of the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt => ExpiresAtTicks > 0 
            ? new DateTime(ExpiresAtTicks, DateTimeKind.Utc) 
            : null;

        /// <summary>
        /// Gets whether this message has expired.
        /// </summary>
        public bool IsExpired => ExpiresAtTicks > 0 && DateTime.UtcNow.Ticks > ExpiresAtTicks;

        /// <summary>
        /// Gets whether this message is ready for delivery (considering delivery delay).
        /// </summary>
        public bool IsReadyForDelivery => DateTime.UtcNow >= CreatedAt.Add(DeliveryDelay);

        /// <summary>
        /// Gets whether this message has routing tags.
        /// </summary>
        public bool HasRoutingTags => RoutingTags.Count > 0;

        /// <summary>
        /// Gets whether this message has custom properties.
        /// </summary>
        public bool HasCustomProperties => CustomProperties.Count > 0;

        /// <summary>
        /// Gets whether this message has custom headers.
        /// </summary>
        public bool HasCustomHeaders => CustomHeaders.Count > 0;

        /// <summary>
        /// Gets the total size estimate in bytes for this metadata.
        /// </summary>
        public int EstimatedSize =>
            sizeof(Guid) * 3 + // MessageId, CorrelationId, ConversationId
            sizeof(ushort) + // TypeCode
            sizeof(MessagePriority) + // Priority
            Source.Length + Destination.Length + Category.Length + // Fixed strings
            sizeof(long) * 2 + // Timestamps
            sizeof(TimeSpan) * 2 + // Time spans
            sizeof(int) * 3 + // Delivery counters
            sizeof(bool) * 5 + // Boolean flags
            ReplyTo.Length + DeadLetterQueue.Length + SecurityToken.Length + // More fixed strings
            (RoutingTags.Count * 32) + // Routing tags estimate
            (CustomProperties.Count * 64) + // Custom properties estimate
            (CustomHeaders.Count * 128); // Custom headers estimate

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageMetadata record.
        /// </summary>
        /// <param name="messageId">The unique message identifier</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="conversationId">The conversation identifier</param>
        /// <param name="typeCode">The message type code</param>
        /// <param name="priority">The message priority</param>
        /// <param name="source">The source system</param>
        /// <param name="destination">The destination system</param>
        /// <param name="category">The message category</param>
        /// <param name="createdAtTicks">The creation timestamp</param>
        /// <param name="expiresAtTicks">The expiration timestamp</param>
        /// <param name="timeToLive">The time to live</param>
        /// <param name="deliveryDelay">The delivery delay</param>
        /// <param name="deliveryMode">The delivery mode</param>
        /// <param name="deliveryAttempts">The delivery attempts count</param>
        /// <param name="maxDeliveryAttempts">The maximum delivery attempts</param>
        /// <param name="requiresAcknowledgment">Whether acknowledgment is required</param>
        /// <param name="isPersistent">Whether the message is persistent</param>
        /// <param name="isCompressed">Whether the message is compressed</param>
        /// <param name="routingStrategy">The routing strategy</param>
        /// <param name="routingTags">The routing tags</param>
        /// <param name="routingContext">The routing context</param>
        /// <param name="replyTo">The reply-to address</param>
        /// <param name="deadLetterQueue">The dead letter queue</param>
        /// <param name="securityToken">The security token</param>
        /// <param name="securityLevel">The security level</param>
        /// <param name="containsSensitiveData">Whether contains sensitive data</param>
        /// <param name="requiresEncryption">Whether encryption is required</param>
        /// <param name="customProperties">Custom properties</param>
        /// <param name="customHeaders">Custom headers</param>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid</exception>
        public MessageMetadata(
            Guid messageId,
            Guid correlationId = default,
            Guid conversationId = default,
            ushort typeCode = 0,
            MessagePriority priority = MessagePriority.Normal,
            FixedString64Bytes source = default,
            FixedString64Bytes destination = default,
            FixedString32Bytes category = default,
            long createdAtTicks = 0,
            long expiresAtTicks = 0,
            TimeSpan timeToLive = default,
            TimeSpan deliveryDelay = default,
            MessageDeliveryMode deliveryMode = MessageDeliveryMode.Standard,
            int deliveryAttempts = 0,
            int maxDeliveryAttempts = 3,
            bool requiresAcknowledgment = false,
            bool isPersistent = false,
            bool isCompressed = false,
            MessageRoutingStrategy routingStrategy = MessageRoutingStrategy.Default,
            IReadOnlyList<FixedString32Bytes> routingTags = null,
            IReadOnlyDictionary<string, object> routingContext = null,
            FixedString128Bytes replyTo = default,
            FixedString128Bytes deadLetterQueue = default,
            FixedString512Bytes securityToken = default,
            MessageSecurityLevel securityLevel = MessageSecurityLevel.None,
            bool containsSensitiveData = false,
            bool requiresEncryption = false,
            IReadOnlyDictionary<string, object> customProperties = null,
            IReadOnlyDictionary<string, string> customHeaders = null)
        {
            // Validate required parameters
            if (messageId == Guid.Empty)
                throw new ArgumentException("Message ID cannot be empty", nameof(messageId));

            if (deliveryAttempts < 0)
                throw new ArgumentException("Delivery attempts cannot be negative", nameof(deliveryAttempts));

            if (maxDeliveryAttempts < 0)
                throw new ArgumentException("Max delivery attempts cannot be negative", nameof(maxDeliveryAttempts));

            if (timeToLive < TimeSpan.Zero)
                throw new ArgumentException("Time to live cannot be negative", nameof(timeToLive));

            if (deliveryDelay < TimeSpan.Zero)
                throw new ArgumentException("Delivery delay cannot be negative", nameof(deliveryDelay));

            // Set core properties
            MessageId = messageId;
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
            ConversationId = conversationId == default ? Guid.NewGuid() : conversationId;
            TypeCode = typeCode;
            Priority = priority;
            Source = source;
            Destination = destination;
            Category = category;

            // Set timing properties
            CreatedAtTicks = createdAtTicks == 0 ? DateTime.UtcNow.Ticks : createdAtTicks;
            ExpiresAtTicks = expiresAtTicks;
            TimeToLive = timeToLive;
            DeliveryDelay = deliveryDelay;

            // Set delivery properties
            DeliveryMode = deliveryMode;
            DeliveryAttempts = deliveryAttempts;
            MaxDeliveryAttempts = maxDeliveryAttempts;
            RequiresAcknowledgment = requiresAcknowledgment;
            IsPersistent = isPersistent;
            IsCompressed = isCompressed;

            // Set routing properties
            RoutingStrategy = routingStrategy;
            RoutingTags = routingTags ?? Array.Empty<FixedString32Bytes>();
            RoutingContext = routingContext ?? new Dictionary<string, object>();
            ReplyTo = replyTo;
            DeadLetterQueue = deadLetterQueue;

            // Set security properties
            SecurityToken = securityToken;
            SecurityLevel = securityLevel;
            ContainsSensitiveData = containsSensitiveData;
            RequiresEncryption = requiresEncryption;

            // Set custom properties
            CustomProperties = customProperties ?? new Dictionary<string, object>();
            CustomHeaders = customHeaders ?? new Dictionary<string, string>();

            // Auto-calculate expiration if TTL is specified but expiration is not
            if (timeToLive > TimeSpan.Zero && expiresAtTicks == 0)
            {
                ExpiresAtTicks = CreatedAtTicks + timeToLive.Ticks;
            }
        }

        #endregion



        #region Utility Methods

        /// <summary>
        /// Gets a custom property value by key.
        /// </summary>
        /// <typeparam name="T">The expected value type</typeparam>
        /// <param name="key">The property key</param>
        /// <returns>The property value, or default if not found</returns>
        public T GetCustomProperty<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || !CustomProperties.TryGetValue(key, out var value))
                return default(T);

            try
            {
                return (T)value;
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Gets a custom header value by key.
        /// </summary>
        /// <param name="key">The header key</param>
        /// <returns>The header value, or null if not found</returns>
        public string GetCustomHeader(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            return CustomHeaders.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Checks if a routing tag exists.
        /// </summary>
        /// <param name="tag">The tag to check</param>
        /// <returns>True if the tag exists, false otherwise</returns>
        public bool HasRoutingTag(FixedString32Bytes tag) =>
            RoutingTags.AsValueEnumerable().Contains(tag);

        /// <summary>
        /// Validates the metadata for consistency and correctness.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                // Basic validation
                if (MessageId == Guid.Empty)
                    return false;

                if (DeliveryAttempts < 0 || MaxDeliveryAttempts < 0)
                    return false;

                if (TimeToLive < TimeSpan.Zero || DeliveryDelay < TimeSpan.Zero)
                    return false;

                // Check expiration consistency
                if (TimeToLive > TimeSpan.Zero && ExpiresAtTicks == 0)
                    return false;

                if (ExpiresAtTicks > 0 && ExpiresAtTicks <= CreatedAtTicks)
                    return false;

                // Check delivery attempts consistency
                if (DeliveryAttempts > MaxDeliveryAttempts)
                    return false;

                // All validations passed
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a summary string of the metadata for logging and debugging.
        /// </summary>
        /// <returns>Formatted summary string</returns>
        public string ToSummary() =>
            $"MessageMetadata[{MessageId:D}]: " +
            $"Priority={Priority}, " +
            $"Source={Source}, " +
            $"Destination={Destination}, " +
            $"CorrelationId={CorrelationId:D}, " +
            $"DeliveryMode={DeliveryMode}, " +
            $"Attempts={DeliveryAttempts}/{MaxDeliveryAttempts}, " +
            $"TTL={TimeToLive.TotalSeconds:F2}s, " +
            $"Tags={RoutingTags.Count}, " +
            $"CustomProps={CustomProperties.Count}";

        #endregion
    }
}