using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message sent when health check factory encounters creation errors.
/// Used for alerting and monitoring health check factory reliability.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct HealthCheckFactoryErrorMessage : IMessage
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
    /// Gets the error message describing what went wrong.
    /// </summary>
    public FixedString512Bytes ErrorMessage { get; init; }

    /// <summary>
    /// Gets the type of health check that failed to be created.
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
    /// Creates a new HealthCheckFactoryErrorMessage with proper validation and defaults.
    /// </summary>
    /// <param name="errorMessage">Error message describing what went wrong</param>
    /// <param name="healthCheckType">Type of health check that failed to be created</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New HealthCheckFactoryErrorMessage instance</returns>
    public static HealthCheckFactoryErrorMessage Create(
        string errorMessage,
        string healthCheckType,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.High)
    {
        // Input validation
        if (string.IsNullOrEmpty(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));
        
        if (string.IsNullOrEmpty(healthCheckType))
            throw new ArgumentException("Health check type cannot be null or empty", nameof(healthCheckType));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckFactoryErrorMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckFactoryError", healthCheckType)
            : correlationId;

        return new HealthCheckFactoryErrorMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckFactoryErrorMessage,
            Source = source.IsEmpty ? "HealthCheckSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            ErrorMessage = errorMessage?.Length <= 512 ? errorMessage : errorMessage?[..512] ?? string.Empty,
            HealthCheckType = healthCheckType?.Length <= 128 ? healthCheckType : healthCheckType?[..128] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Health check factory error message string representation</returns>
    public override string ToString()
    {
        var typeText = HealthCheckType.IsEmpty ? "Unknown" : HealthCheckType.ToString();
        var errorText = ErrorMessage.IsEmpty ? "No details" : ErrorMessage.ToString();
        return $"HealthCheckFactoryError [{typeText}]: {errorText}";
    }

    #endregion
}