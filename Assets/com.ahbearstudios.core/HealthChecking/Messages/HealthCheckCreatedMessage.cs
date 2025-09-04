using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message sent when health check factory successfully creates a health check.
/// Used for tracking health check creation and system monitoring.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct HealthCheckCreatedMessage : IMessage
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
    /// Gets the name of the health check that was created.
    /// </summary>
    public FixedString128Bytes HealthCheckName { get; init; }

    /// <summary>
    /// Gets the full type name of the health check that was created.
    /// </summary>
    public FixedString128Bytes HealthCheckType { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new HealthCheckCreatedMessage with proper validation and defaults.
    /// </summary>
    /// <param name="healthCheckName">Name of the health check that was created</param>
    /// <param name="healthCheckType">Full type name of the health check that was created</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New HealthCheckCreatedMessage instance</returns>
    public static HealthCheckCreatedMessage Create(
        string healthCheckName,
        string healthCheckType,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.Normal)
    {
        // Input validation
        if (string.IsNullOrEmpty(healthCheckName))
            throw new ArgumentException("Health check name cannot be null or empty", nameof(healthCheckName));
        
        if (string.IsNullOrEmpty(healthCheckType))
            throw new ArgumentException("Health check type cannot be null or empty", nameof(healthCheckType));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckCreatedMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckCreated", healthCheckName)
            : correlationId;

        return new HealthCheckCreatedMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckCreatedMessage,
            Source = source.IsEmpty ? "HealthCheckSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            HealthCheckName = healthCheckName?.Length <= 128 ? healthCheckName : healthCheckName?[..128] ?? string.Empty,
            HealthCheckType = healthCheckType?.Length <= 256 ? healthCheckType : healthCheckType?[..256] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Health check created message string representation</returns>
    public override string ToString()
    {
        var nameText = HealthCheckName.IsEmpty ? "Unknown" : HealthCheckName.ToString();
        var typeText = HealthCheckType.IsEmpty ? "Unknown" : HealthCheckType.ToString();
        return $"HealthCheckCreated: {nameText} ({typeText}) from {Source}";
    }

    #endregion
}