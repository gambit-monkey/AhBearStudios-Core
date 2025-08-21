using System;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Builders;

/// <summary>
/// Interface for building MessageMetadata configurations with fluent API.
/// Handles complex construction logic, validation, and default values.
/// </summary>
public interface IMessageMetadataBuilder
{
    /// <summary>
    /// Sets the message identifier.
    /// </summary>
    /// <param name="messageId">The unique message identifier</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithMessageId(Guid messageId);

    /// <summary>
    /// Sets the correlation identifier for tracing.
    /// </summary>
    /// <param name="correlationId">The correlation identifier</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithCorrelationId(Guid correlationId);

    /// <summary>
    /// Sets the conversation identifier for grouping messages.
    /// </summary>
    /// <param name="conversationId">The conversation identifier</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithConversationId(Guid conversationId);

    /// <summary>
    /// Sets the message type code for routing.
    /// </summary>
    /// <param name="typeCode">The message type code</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithTypeCode(ushort typeCode);

    /// <summary>
    /// Sets the message priority.
    /// </summary>
    /// <param name="priority">The message priority level</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithPriority(MessagePriority priority);

    /// <summary>
    /// Sets the source and destination for the message.
    /// </summary>
    /// <param name="source">The source system or component</param>
    /// <param name="destination">The destination system or component</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithRouting(FixedString64Bytes source, FixedString64Bytes destination);

    /// <summary>
    /// Sets the message category.
    /// </summary>
    /// <param name="category">The message category</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithCategory(FixedString32Bytes category);

    /// <summary>
    /// Sets the time to live for the message.
    /// </summary>
    /// <param name="timeToLive">The time to live duration</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithTimeToLive(TimeSpan timeToLive);

    /// <summary>
    /// Sets the delivery delay for scheduled messages.
    /// </summary>
    /// <param name="delay">The delivery delay duration</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithDeliveryDelay(TimeSpan delay);

    /// <summary>
    /// Sets the delivery mode and options.
    /// </summary>
    /// <param name="mode">The delivery mode</param>
    /// <param name="maxAttempts">Maximum delivery attempts</param>
    /// <param name="requiresAck">Whether acknowledgment is required</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithDeliveryOptions(
        MessageDeliveryMode mode, 
        int maxAttempts = 3, 
        bool requiresAck = false);

    /// <summary>
    /// Sets persistence options for the message.
    /// </summary>
    /// <param name="isPersistent">Whether the message should be persisted</param>
    /// <param name="isCompressed">Whether the message should be compressed</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithPersistenceOptions(bool isPersistent, bool isCompressed = false);

    /// <summary>
    /// Sets the routing strategy for the message.
    /// </summary>
    /// <param name="strategy">The routing strategy</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithRoutingStrategy(MessageRoutingStrategy strategy);


    /// <summary>
    /// Sets the reply-to address for response messages.
    /// </summary>
    /// <param name="replyTo">The reply-to address</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithReplyTo(FixedString128Bytes replyTo);

    /// <summary>
    /// Sets the dead letter queue for failed messages.
    /// </summary>
    /// <param name="deadLetterQueue">The dead letter queue address</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithDeadLetterQueue(FixedString128Bytes deadLetterQueue);

    /// <summary>
    /// Sets security options for the message.
    /// </summary>
    /// <param name="level">The security level</param>
    /// <param name="token">The security token</param>
    /// <param name="requiresEncryption">Whether encryption is required</param>
    /// <param name="containsSensitiveData">Whether the message contains sensitive data</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder WithSecurityOptions(
        MessageSecurityLevel level,
        FixedString512Bytes token = default,
        bool requiresEncryption = false,
        bool containsSensitiveData = false);

    /// <summary>
    /// Adds a custom property to the metadata.
    /// </summary>
    /// <param name="key">The property key</param>
    /// <param name="value">The property value</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder AddCustomProperty(string key, object value);

    /// <summary>
    /// Adds a custom header to the metadata.
    /// </summary>
    /// <param name="key">The header key</param>
    /// <param name="value">The header value</param>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder AddCustomHeader(string key, string value);

    /// <summary>
    /// Validates and builds the MessageMetadataConfig.
    /// </summary>
    /// <returns>The validated configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    MessageMetadataConfig Build();

    /// <summary>
    /// Resets the builder to its initial state.
    /// </summary>
    /// <returns>The builder for chaining</returns>
    IMessageMetadataBuilder Reset();
}