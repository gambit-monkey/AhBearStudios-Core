using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a subscription successfully processes a message.
/// Provides processing metrics and performance information for monitoring.
/// </summary>
public record struct MessageBusSubscriptionProcessedMessage : IMessage
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
    /// Gets the subscription identifier that processed the message.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the identifier of the original message that was processed.
    /// </summary>
    public Guid ProcessedMessageId { get; init; }

    /// <summary>
    /// Gets the type code of the original message that was processed.
    /// </summary>
    public ushort ProcessedMessageTypeCode { get; init; }

    /// <summary>
    /// Gets the processing time in milliseconds.
    /// </summary>
    public double ProcessingTimeMs { get; init; }

    /// <summary>
    /// Gets the name of the message type that was processed.
    /// </summary>
    public FixedString64Bytes ProcessedMessageTypeName { get; init; }

    /// <summary>
    /// Gets whether the processing was completed synchronously.
    /// </summary>
    public bool IsAsyncProcessing { get; init; }

    /// <summary>
    /// Gets the subscription category that processed the message.
    /// </summary>
    public SubscriptionCategory Category { get; init; }

    /// <summary>
    /// Initializes a new instance of MessageBusSubscriptionProcessedMessage.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="processedMessage">The original message that was processed</param>
    /// <param name="processingTime">The processing time</param>
    /// <param name="isAsyncProcessing">Whether processing was asynchronous</param>
    /// <param name="category">The subscription category</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MessageBusSubscriptionProcessedMessage(
        Guid subscriptionId,
        IMessage processedMessage,
        TimeSpan processingTime,
        bool isAsyncProcessing = false,
        SubscriptionCategory category = SubscriptionCategory.Standard,
        Guid correlationId = default)
    {
        if (processedMessage == null)
            throw new ArgumentNullException(nameof(processedMessage));

        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = MessageTypeCodes.MessageBusSubscriptionProcessedMessage;
        Source = "MessageBus";
        Priority = MessagePriority.Low; // Processing success is informational
        CorrelationId = correlationId == default ? processedMessage.CorrelationId : correlationId;
        
        SubscriptionId = subscriptionId;
        ProcessedMessageId = processedMessage.Id;
        ProcessedMessageTypeCode = processedMessage.TypeCode;
        ProcessingTimeMs = processingTime.TotalMilliseconds;
        ProcessedMessageTypeName = processedMessage.GetType().Name;
        IsAsyncProcessing = isAsyncProcessing;
        Category = category;
    }

    /// <summary>
    /// Creates a message for synchronous subscription processing.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="processedMessage">The processed message</param>
    /// <param name="processingTime">The processing time</param>
    /// <param name="category">The subscription category</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Subscription processed message</returns>
    public static MessageBusSubscriptionProcessedMessage ForSync(
        Guid subscriptionId,
        IMessage processedMessage,
        TimeSpan processingTime,
        SubscriptionCategory category = SubscriptionCategory.Standard,
        Guid correlationId = default) =>
        new(subscriptionId, processedMessage, processingTime, false, category, correlationId);

    /// <summary>
    /// Creates a message for asynchronous subscription processing.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="processedMessage">The processed message</param>
    /// <param name="processingTime">The processing time</param>
    /// <param name="category">The subscription category</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Subscription processed message</returns>
    public static MessageBusSubscriptionProcessedMessage ForAsync(
        Guid subscriptionId,
        IMessage processedMessage,
        TimeSpan processingTime,
        SubscriptionCategory category = SubscriptionCategory.Async,
        Guid correlationId = default) =>
        new(subscriptionId, processedMessage, processingTime, true, category, correlationId);
}