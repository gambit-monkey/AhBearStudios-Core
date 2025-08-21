using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a message is successfully processed.
/// Replaces MessageProcessedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageProcessedEventMessage : IMessage
{
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the unique type code for this message type.
    /// </summary>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusProcessedMessage;

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the priority level for message processing.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets optional correlation ID for message tracing across systems.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the message that was processed.
    /// </summary>
    public IMessage ProcessedMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when processing completed.
    /// </summary>
    public DateTime ProcessedAt { get; init; }

    /// <summary>
    /// Gets the name of the subscriber that processed the message.
    /// </summary>
    public FixedString64Bytes SubscriberName { get; init; }

    /// <summary>
    /// Gets the time it took to process the message.
    /// </summary>
    public TimeSpan ProcessingDuration { get; init; }

    /// <summary>
    /// Gets whether processing was successful.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Gets the result of the processing operation.
    /// </summary>
    public FixedString512Bytes ProcessingResult { get; init; }

    /// <summary>
    /// Gets additional context about the processing.
    /// </summary>
    public FixedString512Bytes Context { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageProcessedEventMessage struct.
    /// </summary>
    public MessageProcessedEventMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        ProcessedMessage = default;
        ProcessedAt = default;
        SubscriberName = default;
        ProcessingDuration = default;
        IsSuccessful = default;
        ProcessingResult = default;
        Context = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageProcessedEventMessage.
    /// </summary>
    /// <param name="processedMessage">The message that was processed</param>
    /// <param name="subscriberName">The name of the subscriber</param>
    /// <param name="processingDuration">The time it took to process</param>
    /// <param name="isSuccessful">Whether processing was successful</param>
    /// <param name="processingResult">The result of processing</param>
    /// <param name="context">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageProcessedEventMessage instance</returns>
    public static MessageProcessedEventMessage Create(
        IMessage processedMessage,
        string subscriberName = null,
        TimeSpan processingDuration = default,
        bool isSuccessful = true,
        string processingResult = null,
        string context = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageProcessedEventMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusProcessedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Processing events are informational
            CorrelationId = correlationId == default ? processedMessage?.CorrelationId ?? Guid.NewGuid() : correlationId,
            ProcessedMessage = processedMessage ?? throw new ArgumentNullException(nameof(processedMessage)),
            ProcessedAt = DateTime.UtcNow,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            ProcessingDuration = processingDuration,
            IsSuccessful = isSuccessful,
            ProcessingResult = processingResult?.Length <= 256 ? processingResult : processingResult?[..256] ?? string.Empty,
            Context = context?.Length <= 256 ? context : context?[..256] ?? string.Empty
        };
    }
}