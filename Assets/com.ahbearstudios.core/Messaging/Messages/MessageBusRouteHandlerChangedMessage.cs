using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Operations performed on route handlers.
/// </summary>
public enum RouteHandlerOperation
{
    /// <summary>
    /// Handler was registered.
    /// </summary>
    Registered,

    /// <summary>
    /// Handler was unregistered.
    /// </summary>
    Unregistered
}

/// <summary>
/// Message sent when route handlers are registered or unregistered.
/// Replaces RouteHandlerEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusRouteHandlerChangedMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusRouteHandlerChangedMessage;

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

    /// <summary>
    /// Initializes a new instance of the MessageBusRouteHandlerChangedMessage struct.
    /// </summary>
    public MessageBusRouteHandlerChangedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        HandlerId = default;
        HandlerName = default;
        Operation = default;
        ChangedAt = default;
        MessageType = default;
        RegisteredAt = default;
        ChangeContext = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageBusRouteHandlerChangedMessage.
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
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageBusRouteHandlerChangedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusRouteHandlerChangedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Route handler changes are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            HandlerId = handlerId,
            HandlerName = handlerName?.Length <= 64 ? handlerName : handlerName?[..64] ?? throw new ArgumentNullException(nameof(handlerName)),
            Operation = operation,
            ChangedAt = DateTime.UtcNow,
            MessageType = messageType,
            RegisteredAt = registeredAt == default ? DateTime.UtcNow : registeredAt,
            ChangeContext = changeContext?.Length <= 256 ? changeContext : changeContext?[..256] ?? string.Empty
        };
    }
}