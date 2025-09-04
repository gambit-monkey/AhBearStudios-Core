using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.HealthChecking.Messages;

/// <summary>
/// Message sent when health check service factory successfully creates a service.
/// Used for tracking service creation and system monitoring.
/// Implements IMessage for integration with the messaging bus and correlation tracking.
/// Designed for Unity game development with zero-allocation patterns.
/// </summary>
public readonly record struct HealthCheckServiceCreatedMessage : IMessage
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
    /// Gets the unique identifier of the created service.
    /// </summary>
    public FixedString128Bytes ServiceId { get; init; }

    /// <summary>
    /// Gets the hash of the configuration used to create the service.
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
    /// Creates a new HealthCheckServiceCreatedMessage with proper validation and defaults.
    /// </summary>
    /// <param name="serviceId">Unique identifier of the created service</param>
    /// <param name="configurationHash">Hash of the configuration used to create the service</param>
    /// <param name="source">Source component creating this message</param>
    /// <param name="correlationId">Optional correlation ID for tracking</param>
    /// <param name="priority">Message priority level</param>
    /// <returns>New HealthCheckServiceCreatedMessage instance</returns>
    public static HealthCheckServiceCreatedMessage Create(
        string serviceId,
        string configurationHash,
        FixedString64Bytes source = default,
        Guid correlationId = default,
        MessagePriority priority = MessagePriority.Normal)
    {
        // Input validation
        if (string.IsNullOrEmpty(serviceId))
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));
        
        if (string.IsNullOrEmpty(configurationHash))
            throw new ArgumentException("Configuration hash cannot be null or empty", nameof(configurationHash));

        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "HealthCheckSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("HealthCheckServiceCreatedMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("HealthCheckServiceCreated", serviceId)
            : correlationId;

        return new HealthCheckServiceCreatedMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.HealthCheckServiceCreatedMessage,
            Source = source.IsEmpty ? "HealthCheckSystem" : source,
            Priority = priority,
            CorrelationId = finalCorrelationId,
            ServiceId = serviceId?.Length <= 128 ? serviceId : serviceId?[..128] ?? string.Empty,
            ConfigurationHash = configurationHash?.Length <= 128 ? configurationHash : configurationHash?[..128] ?? string.Empty
        };
    }

    #endregion

    #region String Representation

    /// <summary>
    /// Returns a string representation of this message for debugging.
    /// </summary>
    /// <returns>Health check service created message string representation</returns>
    public override string ToString()
    {
        var serviceText = ServiceId.IsEmpty ? "Unknown" : ServiceId.ToString();
        return $"HealthCheckServiceCreated: {serviceText} from {Source}";
    }

    #endregion
}