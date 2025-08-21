using System;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Comprehensive information about a registered message type.
/// Provides metadata for routing, serialization, and system management.
/// Immutable record for thread-safe operations following CLAUDE.md guidelines.
/// </summary>
public sealed record MessageTypeInformation
{
    /// <summary>
    /// Gets the message type.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the unique type code assigned to this message type.
    /// </summary>
    public ushort TypeCode { get; init; }

    /// <summary>
    /// Gets the simple name of the message type.
    /// </summary>
    public FixedString64Bytes Name { get; init; }

    /// <summary>
    /// Gets the full name of the message type.
    /// </summary>
    public string FullName { get; init; }

    /// <summary>
    /// Gets the category this message type belongs to.
    /// </summary>
    public string Category { get; init; }

    /// <summary>
    /// Gets the description of this message type.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Gets the default priority for messages of this type.
    /// </summary>
    public MessagePriority DefaultPriority { get; init; }

    /// <summary>
    /// Gets whether this message type is serializable.
    /// </summary>
    public bool IsSerializable { get; init; }

    /// <summary>
    /// Gets when this message type was registered.
    /// </summary>
    public DateTime RegisteredAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageTypeInformation record.
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="typeCode">The type code</param>
    /// <param name="name">The type name</param>
    /// <param name="fullName">The full type name</param>
    /// <param name="category">The category</param>
    /// <param name="description">The description</param>
    /// <param name="defaultPriority">The default priority</param>
    /// <param name="isSerializable">Whether the type is serializable</param>
    /// <param name="registeredAt">The registration timestamp</param>
    public MessageTypeInformation(
        Type messageType,
        ushort typeCode,
        string name,
        string fullName,
        string category = null,
        string description = null,
        MessagePriority defaultPriority = MessagePriority.Normal,
        bool isSerializable = true,
        DateTime registeredAt = default)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        TypeCode = typeCode;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Category = category ?? string.Empty;
        Description = description ?? string.Empty;
        DefaultPriority = defaultPriority;
        IsSerializable = isSerializable;
        RegisteredAt = registeredAt == default ? DateTime.UtcNow : registeredAt;
    }
}