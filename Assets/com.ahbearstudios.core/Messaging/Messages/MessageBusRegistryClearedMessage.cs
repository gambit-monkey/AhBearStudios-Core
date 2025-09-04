using System;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when the message type registry is cleared.
/// Replaces RegistryClearedEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusRegistryClearedMessage : IMessage
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
    /// Gets the timestamp when the registry was cleared.
    /// </summary>
    public DateTime ClearedAt { get; init; }

    /// <summary>
    /// Gets the number of message types that were removed.
    /// </summary>
    public int TypesRemoved { get; init; }

    /// <summary>
    /// Gets the reason for clearing the registry.
    /// </summary>
    public FixedString128Bytes ClearReason { get; init; }

    /// <summary>
    /// Gets whether the clear operation was successful.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Gets the number of type codes that were freed.
    /// </summary>
    public int TypeCodesFreed { get; init; }

    /// <summary>
    /// Gets the number of cached entries that were removed.
    /// </summary>
    public int CacheEntriesCleared { get; init; }

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
    /// Gets the total number of registry items cleared.
    /// </summary>
    public int TotalItemsCleared => TypesRemoved + CacheEntriesCleared;

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new instance of MessageBusRegistryClearedMessage using FixedString parameters for optimal performance.
    /// </summary>
    /// <param name="typesRemoved">Number of types removed</param>
    /// <param name="clearReason">Reason for clearing</param>
    /// <param name="isSuccessful">Whether the operation was successful</param>
    /// <param name="typeCodesFreed">Number of type codes freed</param>
    /// <param name="cacheEntriesCleared">Number of cache entries cleared</param>
    /// <param name="clearContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRegistryClearedMessage instance</returns>
    public static MessageBusRegistryClearedMessage CreateFromFixedStrings(
        int typesRemoved,
        string clearReason,
        bool isSuccessful,
        int typeCodesFreed,
        int cacheEntriesCleared,
        string clearContext,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("MessageBusRegistry", clearReason)
            : correlationId;

        return new MessageBusRegistryClearedMessage
        {
            Id = DeterministicIdGenerator.GenerateMessageId("MessageBusRegistryClearedMessage", "MessagingSystem", correlationId: null),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusRegistryClearedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Normal,
            CorrelationId = finalCorrelationId,
            ClearedAt = DateTime.UtcNow,
            TypesRemoved = Math.Max(0, typesRemoved),
            ClearReason = clearReason?.Length <= 128 ? clearReason : clearReason?[..128] ?? "System cleanup",
            IsSuccessful = isSuccessful,
            TypeCodesFreed = Math.Max(0, typeCodesFreed),
            CacheEntriesCleared = Math.Max(0, cacheEntriesCleared),
            ClearContext = clearContext?.Length <= 512 ? clearContext : clearContext?[..512] ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a new instance of MessageBusRegistryClearedMessage using string parameters.
    /// </summary>
    /// <param name="typesRemoved">Number of types removed</param>
    /// <param name="clearReason">Reason for clearing</param>
    /// <param name="isSuccessful">Whether the operation was successful</param>
    /// <param name="typeCodesFreed">Number of type codes freed</param>
    /// <param name="cacheEntriesCleared">Number of cache entries cleared</param>
    /// <param name="clearContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRegistryClearedMessage instance</returns>
    public static MessageBusRegistryClearedMessage Create(
        int typesRemoved = 0,
        string clearReason = null,
        bool isSuccessful = true,
        int typeCodesFreed = 0,
        int cacheEntriesCleared = 0,
        string clearContext = null,
        string source = null,
        Guid correlationId = default)
    {
        return CreateFromFixedStrings(
            typesRemoved,
            clearReason,
            isSuccessful,
            typeCodesFreed,
            cacheEntriesCleared,
            clearContext,
            source?.Length <= 64 ? source : source?[..64] ?? "MessageBusService",
            correlationId);
    }

    #endregion
}