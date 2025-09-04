using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message sent when circuit breaker factory configures health check integration.
/// Used for tracking integration configuration and system monitoring.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct HealthCheckCircuitBreakerIntegrationConfiguredMessage : IMessage
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
    /// Gets the name of the circuit breaker.
    /// </summary>
    public FixedString64Bytes CircuitBreakerName { get; init; }

    /// <summary>
    /// Gets the name of the associated health check (optional).
    /// </summary>
    public FixedString64Bytes HealthCheckName { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the message timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new HealthCheckCircuitBreakerIntegrationConfiguredMessage with proper validation and defaults.
    /// </summary>
    /// <param name="circuitBreakerName">Name of the circuit breaker</param>
    /// <param name="healthCheckName">Name of the associated health check (optional)</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New HealthCheckCircuitBreakerIntegrationConfiguredMessage instance</returns>
    public static HealthCheckCircuitBreakerIntegrationConfiguredMessage Create(
        string circuitBreakerName,
        string healthCheckName = null,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.Low)
    {
        // Input validation
        if (string.IsNullOrEmpty(circuitBreakerName))
            throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(circuitBreakerName));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckCircuitBreakerIntegrationConfiguredMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("CircuitBreakerIntegrationConfigured", circuitBreakerName)
            : correlationId;

        return new HealthCheckCircuitBreakerIntegrationConfiguredMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckCircuitBreakerIntegrationConfiguredMessage,
            Source = source.IsEmpty ? "HealthCheckSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            CircuitBreakerName = circuitBreakerName?.Length <= 64 ? circuitBreakerName : circuitBreakerName?[..64] ?? string.Empty,
            HealthCheckName = healthCheckName?.Length <= 64 ? healthCheckName : healthCheckName?[..64] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Circuit breaker integration configured message string representation</returns>
    public override string ToString()
    {
        var circuitBreakerText = CircuitBreakerName.IsEmpty ? "Unknown" : CircuitBreakerName.ToString();
        var healthCheckText = HealthCheckName.IsEmpty ? "None" : HealthCheckName.ToString();
        return $"CircuitBreakerIntegrationConfigured: {circuitBreakerText} -> {healthCheckText}";
    }

    #endregion
}