using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a message type is registered in the registry.
/// Replaces MessageTypeRegisteredEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageTypeRegisteredEventMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusTypeRegisteredMessage;

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
    /// Gets the message type that was registered.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the type code assigned to the registered message type.
    /// </summary>
    public ushort AssignedTypeCode { get; init; }

    /// <summary>
    /// Gets the timestamp when the registration occurred.
    /// </summary>
    public DateTime RegisteredAt { get; init; }

    /// <summary>
    /// Gets the category the message type was assigned to.
    /// </summary>
    public FixedString64Bytes Category { get; init; }

    /// <summary>
    /// Gets the description of the registered message type.
    /// </summary>
    public FixedString512Bytes Description { get; init; }

    /// <summary>
    /// Gets the default priority for the message type.
    /// </summary>
    public MessagePriority DefaultPriority { get; init; }

    /// <summary>
    /// Gets whether the message type is serializable.
    /// </summary>
    public bool IsSerializable { get; init; }

    /// <summary>
    /// Gets additional registration context.
    /// </summary>
    public FixedString512Bytes RegistrationContext { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageTypeRegisteredEventMessage struct.
    /// </summary>
    public MessageTypeRegisteredEventMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        MessageType = default;
        AssignedTypeCode = default;
        RegisteredAt = default;
        Category = default;
        Description = default;
        DefaultPriority = default;
        IsSerializable = default;
        RegistrationContext = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageTypeRegisteredEventMessage.
    /// </summary>
    /// <param name="messageType">The message type that was registered</param>
    /// <param name="assignedTypeCode">The type code assigned</param>
    /// <param name="category">The category assigned</param>
    /// <param name="description">The description</param>
    /// <param name="defaultPriority">The default priority</param>
    /// <param name="isSerializable">Whether it's serializable</param>
    /// <param name="registrationContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageTypeRegisteredEventMessage instance</returns>
    public static MessageTypeRegisteredEventMessage Create(
        Type messageType,
        ushort assignedTypeCode,
        string category = null,
        string description = null,
        MessagePriority defaultPriority = MessagePriority.Normal,
        bool isSerializable = true,
        string registrationContext = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageTypeRegisteredEventMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusTypeRegisteredMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Type registration events are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            AssignedTypeCode = assignedTypeCode,
            RegisteredAt = DateTime.UtcNow,
            Category = category?.Length <= 64 ? category : category?[..64] ?? "Default",
            Description = description?.Length <= 256 ? description : description?[..256] ?? string.Empty,
            DefaultPriority = defaultPriority,
            IsSerializable = isSerializable,
            RegistrationContext = registrationContext?.Length <= 256 ? registrationContext : registrationContext?[..256] ?? string.Empty
        };
    }
}