using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory for creating MessageMetadata instances from configurations.
/// Simple creation only - no lifecycle management per CLAUDE.md guidelines.
/// </summary>
public sealed class MessageMetadataFactory : IMessageMetadataFactory
{
    /// <inheritdoc/>
    public MessageMetadata Create(MessageMetadataConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        return new MessageMetadata
        {
            MessageId = config.MessageId,
            CorrelationId = config.CorrelationId,
            ConversationId = config.ConversationId,
            TypeCode = config.TypeCode,
            Priority = config.Priority,
            Source = config.Source,
            Destination = config.Destination,
            Category = config.Category,
            CreatedAtTicks = config.CreatedAtTicks,
            ExpiresAtTicks = config.ExpiresAtTicks,
            TimeToLive = config.TimeToLive,
            DeliveryDelay = config.DeliveryDelay,
            DeliveryMode = config.DeliveryMode,
            DeliveryAttempts = config.DeliveryAttempts,
            MaxDeliveryAttempts = config.MaxDeliveryAttempts,
            RequiresAcknowledgment = config.RequiresAcknowledgment,
            IsPersistent = config.IsPersistent,
            IsCompressed = config.IsCompressed,
            RoutingStrategy = config.RoutingStrategy,
            ReplyTo = config.ReplyTo,
            DeadLetterQueue = config.DeadLetterQueue,
            SecurityToken = config.SecurityToken,
            SecurityLevel = config.SecurityLevel,
            ContainsSensitiveData = config.ContainsSensitiveData,
            RequiresEncryption = config.RequiresEncryption,
            CustomProperties = new Dictionary<string, object>(config.CustomProperties),
            CustomHeaders = new Dictionary<string, string>(config.CustomHeaders)
        };
    }

    /// <inheritdoc/>
    public MessageMetadata CreateDefault()
    {
        return new MessageMetadata
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            TypeCode = 0,
            Priority = MessagePriority.Normal,
            Source = default,
            Destination = default,
            Category = default,
            CreatedAtTicks = DateTime.UtcNow.Ticks,
            ExpiresAtTicks = 0,
            TimeToLive = TimeSpan.Zero,
            DeliveryDelay = TimeSpan.Zero,
            DeliveryMode = MessageDeliveryMode.Standard,
            DeliveryAttempts = 0,
            MaxDeliveryAttempts = 3,
            RequiresAcknowledgment = false,
            IsPersistent = false,
            IsCompressed = false,
            RoutingStrategy = MessageRoutingStrategy.Default,
            ReplyTo = default,
            DeadLetterQueue = default,
            SecurityToken = default,
            SecurityLevel = MessageSecurityLevel.None,
            ContainsSensitiveData = false,
            RequiresEncryption = false,
            CustomProperties = new Dictionary<string, object>(),
            CustomHeaders = new Dictionary<string, string>()
        };
    }
}