using System;
using AhBearStudios.Core.Common.Utilities;
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
    #region IMessage Implementation

    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the message type code for efficient routing and filtering.
    /// </summary>
    public ushort TypeCode { get; init; }

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

    #endregion

    #region Message-Specific Properties

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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageProcessedEventMessage using FixedString parameters for optimal performance.
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
    public static MessageProcessedEventMessage CreateFromFixedStrings(
        IMessage processedMessage,
        string subscriberName,
        TimeSpan processingDuration,
        bool isSuccessful,
        string processingResult,
        string context,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusProcessing", null)
            : correlationId;

        return new MessageProcessedEventMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageProcessedEventMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusProcessedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            ProcessedMessage = processedMessage ?? throw new ArgumentNullException(nameof(processedMessage)),
            ProcessedAt = DateTime.UtcNow,
            SubscriberName = subscriberName?.Length <= 64 ? subscriberName : subscriberName?[..64] ?? "Unknown",
            ProcessingDuration = processingDuration,
            IsSuccessful = isSuccessful,
            ProcessingResult = processingResult?.Length <= 512 ? processingResult : processingResult?[..512] ?? string.Empty,
            Context = context?.Length <= 512 ? context : context?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageProcessedEventMessage using string parameters.
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
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            processedMessage,
            subscriberName,
            processingDuration,
            isSuccessful,
            processingResult,
            context,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}