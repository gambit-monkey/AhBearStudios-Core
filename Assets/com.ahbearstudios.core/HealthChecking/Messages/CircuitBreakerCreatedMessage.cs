using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message sent when circuit breaker factory successfully creates a circuit breaker.
/// Used for tracking circuit breaker creation and system monitoring.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct CircuitBreakerCreatedMessage : IMessage
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
    /// Gets the name of the operation protected by the circuit breaker.
    /// </summary>
    public FixedString128Bytes OperationName { get; init; }

    /// <summary>
    /// Gets the hash of the configuration used to create the circuit breaker.
    /// </summary>
    public FixedString128Bytes ConfigurationHash { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new CircuitBreakerCreatedMessage with proper validation and defaults.
    /// </summary>
    /// <param name="operationName">Name of the operation protected by the circuit breaker</param>
    /// <param name="configurationHash">Hash of the configuration used to create the circuit breaker</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New CircuitBreakerCreatedMessage instance</returns>
    public static CircuitBreakerCreatedMessage Create(
        string operationName,
        string configurationHash,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.Normal)
    {
        // Input validation
        if (string.IsNullOrEmpty(operationName))
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        
        if (string.IsNullOrEmpty(configurationHash))
            throw new ArgumentException("Configuration hash cannot be null or empty", nameof(configurationHash));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("CircuitBreakerCreatedMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerCreated", operationName)
            : correlationId;

        return new CircuitBreakerCreatedMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.CircuitBreakerCreatedMessage,
            Source = source.IsEmpty ? "HealthCheckSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            OperationName = operationName?.Length <= 128 ? operationName : operationName?[..128] ?? string.Empty,
            ConfigurationHash = configurationHash?.Length <= 128 ? configurationHash : configurationHash?[..128] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Circuit breaker created message string representation</returns>
    public override string ToString()
    {
        var operationText = OperationName.IsEmpty ? "Unknown" : OperationName.ToString();
        return $"CircuitBreakerCreated: {operationText} from {Source}";
    }

    #endregion
}