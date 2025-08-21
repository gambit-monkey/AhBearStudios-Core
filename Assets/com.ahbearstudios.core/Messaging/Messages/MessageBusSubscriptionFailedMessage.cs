using Unity.Collections;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message published when a subscription fails to process a message.
/// Provides error information and failure diagnostics for monitoring and alerting.
/// </summary>
public record struct MessageBusSubscriptionFailedMessage : IMessage
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
    /// Gets the subscription identifier that failed to process the message.
    /// </summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>
    /// Gets the identifier of the original message that failed processing.
    /// </summary>
    public Guid FailedMessageId { get; init; }

    /// <summary>
    /// Gets the type code of the original message that failed processing.
    /// </summary>
    public ushort FailedMessageTypeCode { get; init; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exception type name that caused the failure.
    /// </summary>
    public FixedString64Bytes ExceptionTypeName { get; init; }

    /// <summary>
    /// Gets the processing time before failure occurred (in milliseconds).
    /// </summary>
    public double ProcessingTimeMs { get; init; }

    /// <summary>
    /// Gets the name of the message type that failed processing.
    /// </summary>
    public FixedString64Bytes FailedMessageTypeName { get; init; }

    /// <summary>
    /// Gets whether the failed processing was asynchronous.
    /// </summary>
    public bool IsAsyncProcessing { get; init; }

    /// <summary>
    /// Gets the subscription category that failed processing.
    /// </summary>
    public SubscriptionCategory Category { get; init; }

    /// <summary>
    /// Gets the stack trace of the exception (if available).
    /// </summary>
    public string StackTrace { get; init; }

    /// <summary>
    /// Initializes a new instance of MessageBusSubscriptionFailedMessage.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="failedMessage">The original message that failed processing</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="processingTime">The processing time before failure</param>
    /// <param name="isAsyncProcessing">Whether processing was asynchronous</param>
    /// <param name="category">The subscription category</param>
    /// <param name="correlationId">Optional correlation ID for message tracking</param>
    public MessageBusSubscriptionFailedMessage(
        Guid subscriptionId,
        IMessage failedMessage,
        Exception exception,
        TimeSpan processingTime = default,
        bool isAsyncProcessing = false,
        SubscriptionCategory category = SubscriptionCategory.Standard,
        Guid correlationId = default)
    {
        if (failedMessage == null)
            throw new ArgumentNullException(nameof(failedMessage));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        Id = Guid.NewGuid();
        TimestampTicks = DateTime.UtcNow.Ticks;
        TypeCode = MessageTypeCodes.MessageBusSubscriptionFailedMessage;
        Source = "MessageBus";
        Priority = MessagePriority.High; // Failures are high priority for alerting
        CorrelationId = correlationId == default ? failedMessage.CorrelationId : correlationId;
        
        SubscriptionId = subscriptionId;
        FailedMessageId = failedMessage.Id;
        FailedMessageTypeCode = failedMessage.TypeCode;
        ErrorMessage = exception.Message ?? "Unknown error";
        ExceptionTypeName = exception.GetType().Name;
        ProcessingTimeMs = processingTime.TotalMilliseconds;
        FailedMessageTypeName = failedMessage.GetType().Name;
        IsAsyncProcessing = isAsyncProcessing;
        Category = category;
        StackTrace = exception.StackTrace ?? string.Empty;
    }

    /// <summary>
    /// Creates a message for synchronous subscription processing failure.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="failedMessage">The failed message</param>
    /// <param name="exception">The exception that caused failure</param>
    /// <param name="processingTime">The processing time before failure</param>
    /// <param name="category">The subscription category</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Subscription failed message</returns>
    public static MessageBusSubscriptionFailedMessage ForSync(
        Guid subscriptionId,
        IMessage failedMessage,
        Exception exception,
        TimeSpan processingTime = default,
        SubscriptionCategory category = SubscriptionCategory.Standard,
        Guid correlationId = default) =>
        new(subscriptionId, failedMessage, exception, processingTime, false, category, correlationId);

    /// <summary>
    /// Creates a message for asynchronous subscription processing failure.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier</param>
    /// <param name="failedMessage">The failed message</param>
    /// <param name="exception">The exception that caused failure</param>
    /// <param name="processingTime">The processing time before failure</param>
    /// <param name="category">The subscription category</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Subscription failed message</returns>
    public static MessageBusSubscriptionFailedMessage ForAsync(
        Guid subscriptionId,
        IMessage failedMessage,
        Exception exception,
        TimeSpan processingTime = default,
        SubscriptionCategory category = SubscriptionCategory.Async,
        Guid correlationId = default) =>
        new(subscriptionId, failedMessage, exception, processingTime, true, category, correlationId);
}