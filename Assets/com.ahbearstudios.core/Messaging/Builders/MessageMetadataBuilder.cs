using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Builder for creating validated MessageMetadata configurations.
/// Implements fluent API for complex metadata construction.
/// </summary>
public sealed class MessageMetadataBuilder : IMessageMetadataBuilder
{
    private MessageMetadataConfig _config;

    /// <summary>
    /// Initializes a new instance of the MessageMetadataBuilder class.
    /// </summary>
    public MessageMetadataBuilder()
    {
        Reset();
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithMessageId(Guid messageId)
    {
        _config.MessageId = messageId;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithCorrelationId(Guid correlationId)
    {
        _config.CorrelationId = correlationId;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithConversationId(Guid conversationId)
    {
        _config.ConversationId = conversationId;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithTypeCode(ushort typeCode)
    {
        _config.TypeCode = typeCode;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithPriority(MessagePriority priority)
    {
        _config.Priority = priority;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithRouting(FixedString64Bytes source, FixedString64Bytes destination)
    {
        _config.Source = source;
        _config.Destination = destination;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithCategory(FixedString32Bytes category)
    {
        _config.Category = category;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithTimeToLive(TimeSpan timeToLive)
    {
        if (timeToLive < TimeSpan.Zero)
            throw new ArgumentException("Time to live cannot be negative", nameof(timeToLive));
        
        _config.TimeToLive = timeToLive;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithDeliveryDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentException("Delivery delay cannot be negative", nameof(delay));
        
        _config.DeliveryDelay = delay;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithDeliveryOptions(
        MessageDeliveryMode mode, 
        int maxAttempts = 3, 
        bool requiresAck = false)
    {
        if (maxAttempts < 0)
            throw new ArgumentException("Max delivery attempts cannot be negative", nameof(maxAttempts));
        
        _config.DeliveryMode = mode;
        _config.MaxDeliveryAttempts = maxAttempts;
        _config.RequiresAcknowledgment = requiresAck;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithPersistenceOptions(bool isPersistent, bool isCompressed = false)
    {
        _config.IsPersistent = isPersistent;
        _config.IsCompressed = isCompressed;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithRoutingStrategy(MessageRoutingStrategy strategy)
    {
        _config.RoutingStrategy = strategy;
        return this;
    }


    /// <inheritdoc/>
    public IMessageMetadataBuilder WithReplyTo(FixedString128Bytes replyTo)
    {
        _config.ReplyTo = replyTo;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithDeadLetterQueue(FixedString128Bytes deadLetterQueue)
    {
        _config.DeadLetterQueue = deadLetterQueue;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder WithSecurityOptions(
        MessageSecurityLevel level,
        FixedString512Bytes token = default,
        bool requiresEncryption = false,
        bool containsSensitiveData = false)
    {
        _config.SecurityLevel = level;
        _config.SecurityToken = token;
        _config.RequiresEncryption = requiresEncryption;
        _config.ContainsSensitiveData = containsSensitiveData;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder AddCustomProperty(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Custom property key cannot be null or empty", nameof(key));
        
        _config.CustomProperties[key] = value;
        return this;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder AddCustomHeader(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Custom header key cannot be null or empty", nameof(key));
        
        _config.CustomHeaders[key] = value;
        return this;
    }

    /// <inheritdoc/>
    public MessageMetadataConfig Build()
    {
        // Validate required fields
        if (_config.MessageId == Guid.Empty)
            _config.MessageId = Guid.NewGuid();

        // Auto-generate correlation and conversation IDs if not set
        if (_config.CorrelationId == Guid.Empty)
            _config.CorrelationId = Guid.NewGuid();

        if (_config.ConversationId == Guid.Empty)
            _config.ConversationId = Guid.NewGuid();

        // Set creation timestamp if not set
        if (_config.CreatedAtTicks == 0)
            _config.CreatedAtTicks = DateTime.UtcNow.Ticks;

        // Auto-calculate expiration if TTL is specified but expiration is not
        if (_config.TimeToLive > TimeSpan.Zero && _config.ExpiresAtTicks == 0)
        {
            _config.ExpiresAtTicks = _config.CreatedAtTicks + _config.TimeToLive.Ticks;
        }

        // Validate delivery attempts
        if (_config.DeliveryAttempts < 0)
            throw new InvalidOperationException("Delivery attempts cannot be negative");

        if (_config.DeliveryAttempts > _config.MaxDeliveryAttempts)
            throw new InvalidOperationException("Delivery attempts cannot exceed max delivery attempts");

        // Validate expiration consistency
        if (_config.ExpiresAtTicks > 0 && _config.ExpiresAtTicks <= _config.CreatedAtTicks)
            throw new InvalidOperationException("Expiration time must be after creation time");

        return _config;
    }

    /// <inheritdoc/>
    public IMessageMetadataBuilder Reset()
    {
        _config = new MessageMetadataConfig
        {
            MessageId = Guid.Empty,
            CorrelationId = Guid.Empty,
            ConversationId = Guid.Empty,
            TypeCode = 0,
            Priority = MessagePriority.Normal,
            Source = default,
            Destination = default,
            Category = default,
            CreatedAtTicks = 0,
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
        return this;
    }
}