using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for creating MessageMetadata instances.
/// Created by MessageMetadataBuilder and used by MessageMetadataFactory.
/// </summary>
public sealed class MessageMetadataConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for this message instance.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for tracing across system boundaries.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier for grouping related messages.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the message type code for efficient routing.
    /// </summary>
    public ushort TypeCode { get; set; }

    /// <summary>
    /// Gets or sets the message priority level.
    /// </summary>
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>
    /// Gets or sets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; set; }

    /// <summary>
    /// Gets or sets the destination system or component for targeted delivery.
    /// </summary>
    public FixedString64Bytes Destination { get; set; }

    /// <summary>
    /// Gets or sets the message category for organizational purposes.
    /// </summary>
    public FixedString32Bytes Category { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message was created (UTC ticks).
    /// </summary>
    public long CreatedAtTicks { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message expires (UTC ticks).
    /// </summary>
    public long ExpiresAtTicks { get; set; }

    /// <summary>
    /// Gets or sets the maximum time to live for this message.
    /// </summary>
    public TimeSpan TimeToLive { get; set; }

    /// <summary>
    /// Gets or sets the delivery delay for scheduled message processing.
    /// </summary>
    public TimeSpan DeliveryDelay { get; set; }

    /// <summary>
    /// Gets or sets the delivery mode for this message.
    /// </summary>
    public MessageDeliveryMode DeliveryMode { get; set; } = MessageDeliveryMode.Standard;

    /// <summary>
    /// Gets or sets the number of delivery attempts made for this message.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of delivery attempts allowed.
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether this message requires acknowledgment.
    /// </summary>
    public bool RequiresAcknowledgment { get; set; }

    /// <summary>
    /// Gets or sets whether this message should be persisted for durability.
    /// </summary>
    public bool IsPersistent { get; set; }

    /// <summary>
    /// Gets or sets whether this message should be compressed during transport.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Gets or sets the routing strategy for this message.
    /// </summary>
    public MessageRoutingStrategy RoutingStrategy { get; set; } = MessageRoutingStrategy.Default;


    /// <summary>
    /// Gets or sets the reply-to address for response messages.
    /// </summary>
    public FixedString128Bytes ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the dead letter queue destination for failed messages.
    /// </summary>
    public FixedString128Bytes DeadLetterQueue { get; set; }

    /// <summary>
    /// Gets or sets the security token for message authentication.
    /// </summary>
    public FixedString512Bytes SecurityToken { get; set; }

    /// <summary>
    /// Gets or sets the security level required for processing this message.
    /// </summary>
    public MessageSecurityLevel SecurityLevel { get; set; } = MessageSecurityLevel.None;

    /// <summary>
    /// Gets or sets whether this message contains sensitive data.
    /// </summary>
    public bool ContainsSensitiveData { get; set; }

    /// <summary>
    /// Gets or sets whether this message should be encrypted during transport.
    /// </summary>
    public bool RequiresEncryption { get; set; }

    /// <summary>
    /// Gets or sets custom properties for application-specific metadata.
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();

    /// <summary>
    /// Gets or sets custom headers for protocol-specific metadata.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}