using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a message is routed through the routing system.
/// Replaces MessageRoutedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageRoutedEventMessage : IMessage
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
    /// Gets the message that was routed.
    /// </summary>
    public IMessage RoutedMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when routing completed.
    /// </summary>
    public DateTime RoutedAt { get; init; }

    /// <summary>
    /// Gets the number of successful routes.
    /// </summary>
    public int SuccessfulRoutes { get; init; }

    /// <summary>
    /// Gets the number of failed routes.
    /// </summary>
    public int FailedRoutes { get; init; }

    /// <summary>
    /// Gets the total time taken for routing.
    /// </summary>
    public TimeSpan RoutingDuration { get; init; }

    /// <summary>
    /// Gets the number of routing rules evaluated.
    /// </summary>
    public int RulesEvaluated { get; init; }

    /// <summary>
    /// Gets whether all routes were successful.
    /// </summary>
    public bool IsFullySuccessful { get; init; }

    /// <summary>
    /// Gets additional routing context.
    /// </summary>
    public FixedString512Bytes RoutingContext { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the total number of routes attempted.
    /// </summary>
    public int TotalRoutes => SuccessfulRoutes + FailedRoutes;

    /// <summary>
    /// Gets the success rate for routing (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalRoutes > 0 ? (double)SuccessfulRoutes / TotalRoutes : 1.0;

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageRoutedEventMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="routedMessage">The message that was routed</param>
    /// <param name="successfulRoutes">Number of successful routes</param>
    /// <param name="failedRoutes">Number of failed routes</param>
    /// <param name="routingDuration">Time taken for routing</param>
    /// <param name="rulesEvaluated">Number of rules evaluated</param>
    /// <param name="isFullySuccessful">Whether all routes succeeded</param>
    /// <param name="routingContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageRoutedEventMessage instance</returns>
    public static MessageRoutedEventMessage CreateFromFixedStrings(
        IMessage routedMessage,
        int successfulRoutes,
        int failedRoutes,
        TimeSpan routingDuration,
        int rulesEvaluated,
        bool isFullySuccessful,
        string routingContext,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusRouting", null)
            : correlationId;

        return new MessageRoutedEventMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageRoutedEventMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusRoutedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            RoutedMessage = routedMessage ?? throw new ArgumentNullException(nameof(routedMessage)),
            RoutedAt = DateTime.UtcNow,
            SuccessfulRoutes = Math.Max(0, successfulRoutes),
            FailedRoutes = Math.Max(0, failedRoutes),
            RoutingDuration = routingDuration,
            RulesEvaluated = Math.Max(0, rulesEvaluated),
            IsFullySuccessful = isFullySuccessful,
            RoutingContext = routingContext?.Length <= 512 ? routingContext : routingContext?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageRoutedEventMessage using string parameters.
    /// </summary>
    /// <param name="routedMessage">The message that was routed</param>
    /// <param name="successfulRoutes">Number of successful routes</param>
    /// <param name="failedRoutes">Number of failed routes</param>
    /// <param name="routingDuration">Time taken for routing</param>
    /// <param name="rulesEvaluated">Number of rules evaluated</param>
    /// <param name="isFullySuccessful">Whether all routes succeeded</param>
    /// <param name="routingContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageRoutedEventMessage instance</returns>
    public static MessageRoutedEventMessage Create(
        IMessage routedMessage,
        int successfulRoutes = 0,
        int failedRoutes = 0,
        TimeSpan routingDuration = default,
        int rulesEvaluated = 0,
        bool isFullySuccessful = true,
        string routingContext = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            routedMessage,
            successfulRoutes,
            failedRoutes,
            routingDuration,
            rulesEvaluated,
            isFullySuccessful,
            routingContext,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}