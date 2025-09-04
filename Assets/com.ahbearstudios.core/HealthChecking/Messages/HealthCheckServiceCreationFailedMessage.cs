using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message sent when health check service factory fails to create a service.
/// Used for alerting and monitoring service factory reliability.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct HealthCheckServiceCreationFailedMessage : IMessage
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
    /// Gets the type of configuration that failed to create the service.
    /// </summary>
    public FixedString128Bytes ConfigType { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new HealthCheckServiceCreationFailedMessage with proper validation and defaults.
    /// </summary>
    /// <param name="errorMessage">Error message describing what went wrong</param>
    /// <param name="configType">Type of configuration that failed to create the service</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New HealthCheckServiceCreationFailedMessage instance</returns>
    public static HealthCheckServiceCreationFailedMessage Create(
        string errorMessage,
        string configType,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.High)
    {
        // Input validation
        if (string.IsNullOrEmpty(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));
        
        if (string.IsNullOrEmpty(configType))
            throw new ArgumentException("Config type cannot be null or empty", nameof(configType));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckServiceCreationFailedMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckServiceCreationFailed", configType)
            : correlationId;

        return new HealthCheckServiceCreationFailedMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckServiceCreationFailedMessage,
            Source = source.IsEmpty ? "HealthCheckSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            ErrorMessage = errorMessage?.Length <= 512 ? errorMessage : errorMessage?[..512] ?? string.Empty,
            ConfigType = configType?.Length <= 128 ? configType : configType?[..128] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Health check service creation failed message string representation</returns>
    public override string ToString()
    {
        var typeText = ConfigType.IsEmpty ? "Unknown" : ConfigType.ToString();
        var errorText = ErrorMessage.IsEmpty ? "No details" : ErrorMessage.ToString();
        return $"HealthCheckServiceCreationFailed [{typeText}]: {errorText}";
    }

    #endregion
}