using System;
using AhBearStudios.Core.Common.Utilities;
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageTypeRegisteredEventMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="messageType">The message type that was registered</param>
    /// <param name="assignedTypeCode">The type code assigned</param>
    /// <param name="category">The category assigned</param>
    /// <param name="description">The description</param>
    /// <param name="defaultPriority">The default priority</param>
    /// <param name="isSerializable">Whether it's serializable</param>
    /// <param name="registrationContext">Additional context</param>
    /// <param name="source">Source system or component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageTypeRegisteredEventMessage instance</returns>
    public static MessageTypeRegisteredEventMessage CreateFromFixedStrings(
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
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusTypeRegistered", null)
            : correlationId;

        return new MessageTypeRegisteredEventMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageTypeRegisteredEventMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusTypeRegisteredMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low,
            CorrelationId = finalCorrelationId,
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType)),
            AssignedTypeCode = assignedTypeCode,
            RegisteredAt = DateTime.UtcNow,
            Category = category?.Length <= 64 ? category : category?[..64] ?? "Default",
            Description = description?.Length <= 512 ? description : description?[..512] ?? string.Empty,
            DefaultPriority = defaultPriority,
            IsSerializable = isSerializable,
            RegistrationContext = registrationContext?.Length <= 512 ? registrationContext : registrationContext?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageTypeRegisteredEventMessage using string parameters.
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
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            messageType,
            assignedTypeCode,
            category,
            description,
            defaultPriority,
            isSerializable,
            registrationContext,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}