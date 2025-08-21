using System;
using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Metadata container for messages in the messaging system.
/// Provides routing information, delivery options, and tracing capabilities.
/// </summary>
public sealed record MessageMetadata
{
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Gets the correlation identifier for tracing across system boundaries.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the conversation identifier for grouping related messages.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// Gets the message type code for efficient routing.
    /// </summary>
    public ushort TypeCode { get; init; }

    /// <summary>
    /// Gets the message priority level.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the destination system or component for targeted delivery.
    /// </summary>
    public FixedString64Bytes Destination { get; init; }

    /// <summary>
    /// Gets the message category for organizational purposes.
    /// </summary>
    public FixedString32Bytes Category { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was created (UTC ticks).
    /// </summary>
    public long CreatedAtTicks { get; init; }

    /// <summary>
    /// Gets the timestamp when the message expires (UTC ticks). Zero means no expiration.
    /// </summary>
    public long ExpiresAtTicks { get; init; }

    /// <summary>
    /// Gets the maximum time to live for this message.
    /// </summary>
    public TimeSpan TimeToLive { get; init; }

    /// <summary>
    /// Gets the delivery delay for scheduled message processing.
    /// </summary>
    public TimeSpan DeliveryDelay { get; init; }

    /// <summary>
    /// Gets the delivery mode for this message.
    /// </summary>
    public MessageDeliveryMode DeliveryMode { get; init; }

    /// <summary>
    /// Gets the number of delivery attempts made for this message.
    /// </summary>
    public int DeliveryAttempts { get; init; }

    /// <summary>
    /// Gets the maximum number of delivery attempts allowed.
    /// </summary>
    public int MaxDeliveryAttempts { get; init; }

    /// <summary>
    /// Gets whether this message requires acknowledgment.
    /// </summary>
    public bool RequiresAcknowledgment { get; init; }

    /// <summary>
    /// Gets whether this message should be persisted for durability.
    /// </summary>
    public bool IsPersistent { get; init; }

    /// <summary>
    /// Gets whether this message should be compressed during transport.
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    /// Gets the routing strategy for this message.
    /// </summary>
    public MessageRoutingStrategy RoutingStrategy { get; init; }


    /// <summary>
    /// Gets the reply-to address for response messages.
    /// </summary>
    public FixedString128Bytes ReplyTo { get; init; }

    /// <summary>
    /// Gets the dead letter queue destination for failed messages.
    /// </summary>
    public FixedString128Bytes DeadLetterQueue { get; init; }

    /// <summary>
    /// Gets the security token for message authentication.
    /// </summary>
    public FixedString512Bytes SecurityToken { get; init; }

    /// <summary>
    /// Gets the security level required for processing this message.
    /// </summary>
    public MessageSecurityLevel SecurityLevel { get; init; }

    /// <summary>
    /// Gets whether this message contains sensitive data.
    /// </summary>
    public bool ContainsSensitiveData { get; init; }

    /// <summary>
    /// Gets whether this message should be encrypted during transport.
    /// </summary>
    public bool RequiresEncryption { get; init; }

    /// <summary>
    /// Gets custom properties for application-specific metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomProperties { get; init; }

    /// <summary>
    /// Gets custom headers for protocol-specific metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomHeaders { get; init; }
}