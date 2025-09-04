using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when routing rules and handlers are cleared.
/// Replaces RoutesClearedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusRoutesClearedMessage : IMessage
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
    /// Gets the timestamp when routes were cleared.
    /// </summary>
    public DateTime ClearedAt { get; init; }

    /// <summary>
    /// Gets the number of routing rules that were removed.
    /// </summary>
    public int RulesRemoved { get; init; }

    /// <summary>
    /// Gets the number of route handlers that were removed.
    /// </summary>
    public int HandlersRemoved { get; init; }

    /// <summary>
    /// Gets the reason for clearing the routes.
    /// </summary>
    public FixedString128Bytes ClearReason { get; init; }

    /// <summary>
    /// Gets whether the clear operation was successful.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Gets additional context about the clear operation.
    /// </summary>
    public FixedString512Bytes ClearContext { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the total number of items removed.
    /// </summary>
    public int TotalItemsRemoved => RulesRemoved + HandlersRemoved;

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageBusRoutesClearedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="rulesRemoved">Number of rules removed</param>
    /// <param name="handlersRemoved">Number of handlers removed</param>
    /// <param name="clearReason">Reason for clearing</param>
    /// <param name="isSuccessful">Whether the operation was successful</param>
    /// <param name="clearContext">Additional context</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRoutesClearedMessage instance</returns>
    public static MessageBusRoutesClearedMessage CreateFromFixedStrings(
        int rulesRemoved = 0,
        int handlersRemoved = 0,
        string clearReason = null,
        bool isSuccessful = true,
        string clearContext = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusRoutesCleared", null)
            : correlationId;

        return new MessageBusRoutesClearedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusRoutesClearedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusRoutesClearedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            ClearedAt = DateTime.UtcNow,
            RulesRemoved = Math.Max(0, rulesRemoved),
            HandlersRemoved = Math.Max(0, handlersRemoved),
            ClearReason = clearReason?.Length <= 128 ? clearReason : clearReason?[..128] ?? "System cleanup",
            IsSuccessful = isSuccessful,
            ClearContext = clearContext?.Length <= 512 ? clearContext : clearContext?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusRoutesClearedMessage using string parameters.
    /// </summary>
    /// <param name="rulesRemoved">Number of rules removed</param>
    /// <param name="handlersRemoved">Number of handlers removed</param>
    /// <param name="clearReason">Reason for clearing</param>
    /// <param name="isSuccessful">Whether the operation was successful</param>
    /// <param name="clearContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRoutesClearedMessage instance</returns>
    public static MessageBusRoutesClearedMessage Create(
        int rulesRemoved = 0,
        int handlersRemoved = 0,
        string clearReason = null,
        bool isSuccessful = true,
        string clearContext = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            rulesRemoved,
            handlersRemoved,
            clearReason,
            isSuccessful,
            clearContext,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}