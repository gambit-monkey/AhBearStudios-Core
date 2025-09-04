using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when route handlers are registered or unregistered.
/// Replaces RouteHandlerEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusRouteHandlerChangedMessage : IMessage
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
    /// Gets the unique identifier of the route handler.
    /// </summary>
    public Guid HandlerId { get; init; }

    /// <summary>
    /// Gets the name of the route handler.
    /// </summary>
    public FixedString64Bytes HandlerName { get; init; }

    /// <summary>
    /// Gets the operation performed on the handler.
    /// </summary>
    public RouteHandlerOperation Operation { get; init; }

    /// <summary>
    /// Gets the timestamp when the operation occurred.
    /// </summary>
    public DateTime ChangedAt { get; init; }

    /// <summary>
    /// Gets the message type this handler processes.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the timestamp when the handler was originally registered.
    /// </summary>
    public DateTime RegisteredAt { get; init; }

    /// <summary>
    /// Gets additional context about the change.
    /// </summary>
    public FixedString512Bytes ChangeContext { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageBusRouteHandlerChangedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="handlerId">The unique identifier of the handler</param>
    /// <param name="handlerName">The name of the handler</param>
    /// <param name="operation">The operation performed</param>
    /// <param name="changedAt">When the operation occurred</param>
    /// <param name="messageType">The message type this handler processes</param>
    /// <param name="registeredAt">When the handler was registered</param>
    /// <param name="changeContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRouteHandlerChangedMessage instance</returns>
    public static MessageBusRouteHandlerChangedMessage CreateFromFixedStrings(
        Guid handlerId,
        FixedString64Bytes handlerName,
        RouteHandlerOperation operation,
        DateTime changedAt,
        Type messageType,
        DateTime registeredAt,
        FixedString512Bytes changeContext,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusRoute", null)
            : correlationId;

        return new MessageBusRouteHandlerChangedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusRouteHandlerChangedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusRouteHandlerChangedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Route handler changes are informational
            CorrelationId = finalCorrelationId,
            HandlerId = handlerId,
            HandlerName = handlerName,
            Operation = operation,
            ChangedAt = changedAt,
            MessageType = messageType,
            RegisteredAt = registeredAt == default ? DateTime.UtcNow : registeredAt,
            ChangeContext = changeContext
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusRouteHandlerChangedMessage using string parameters.
    /// </summary>
    /// <param name="handlerId">The unique identifier of the handler</param>
    /// <param name="handlerName">The name of the handler</param>
    /// <param name="operation">The operation performed</param>
    /// <param name="messageType">The message type this handler processes</param>
    /// <param name="registeredAt">When the handler was registered</param>
    /// <param name="changeContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRouteHandlerChangedMessage instance</returns>
    public static MessageBusRouteHandlerChangedMessage Create(
        Guid handlerId,
        string handlerName,
        RouteHandlerOperation operation,
        Type messageType = null,
        DateTime registeredAt = default,
        string changeContext = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            handlerId,
            new FixedString64Bytes(handlerName ?? throw new ArgumentNullException(nameof(handlerName))),
            operation,
            DateTime.UtcNow,
            messageType,
            registeredAt,
            new FixedString512Bytes(changeContext ?? string.Empty),
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}