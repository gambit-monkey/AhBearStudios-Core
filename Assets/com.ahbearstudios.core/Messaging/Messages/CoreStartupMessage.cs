using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Message sent when a core system completes startup initialization.
/// Contains essential startup information following IMessage pattern.
/// </summary>
public readonly record struct CoreStartupMessage : IMessage
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
    public ushort TypeCode { get; init; } = MessageTypeCodes.CoreStartupMessage;

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
    /// Gets the name of the system that started up.
    /// </summary>
    public FixedString64Bytes SystemName { get; init; }

    /// <summary>
    /// Gets the version of the system.
    /// </summary>
    public FixedString32Bytes Version { get; init; }

    /// <summary>
    /// Gets the environment where the system is running.
    /// </summary>
    public FixedString32Bytes Environment { get; init; }

    /// <summary>
    /// Gets the unique instance identifier for this system instance.
    /// </summary>
    public Guid InstanceId { get; init; }

    /// <summary>
    /// Gets the total startup duration in milliseconds.
    /// </summary>
    public double StartupDurationMs { get; init; }

    /// <summary>
    /// Gets the number of components initialized during startup.
    /// </summary>
    public int ComponentsInitialized { get; init; }

    /// <summary>
    /// Gets the number of services registered during startup.
    /// </summary>
    public int ServicesRegistered { get; init; }

    /// <summary>
    /// Gets the current memory usage after startup in bytes.
    /// </summary>
    public long CurrentMemoryUsageBytes { get; init; }

    /// <summary>
    /// Gets whether the startup was successful.
    /// </summary>
    public bool IsStartupSuccessful { get; init; }

    /// <summary>
    /// Initializes a new instance of the CoreStartupMessage struct.
    /// </summary>
    public CoreStartupMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        SystemName = default;
        Version = default;
        Environment = default;
        InstanceId = default;
        StartupDurationMs = default;
        ComponentsInitialized = default;
        ServicesRegistered = default;
        CurrentMemoryUsageBytes = default;
        IsStartupSuccessful = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the startup duration as a TimeSpan.
    /// </summary>
    public TimeSpan StartupDuration => TimeSpan.FromMilliseconds(StartupDurationMs);

    /// <summary>
    /// Creates a new instance of the CoreStartupMessage.
    /// </summary>
    /// <param name="systemName">The name of the system that started</param>
    /// <param name="version">The system version</param>
    /// <param name="environment">The environment name</param>
    /// <param name="instanceId">The unique instance identifier</param>
    /// <param name="startupDurationMs">The startup duration in milliseconds</param>
    /// <param name="componentsInitialized">Number of components initialized</param>
    /// <param name="servicesRegistered">Number of services registered</param>
    /// <param name="currentMemoryUsageBytes">Current memory usage</param>
    /// <param name="isStartupSuccessful">Whether startup was successful</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New CoreStartupMessage instance</returns>
    public static CoreStartupMessage Create(
        string systemName,
        string version = null,
        string environment = null,
        Guid instanceId = default,
        double startupDurationMs = 0,
        int componentsInitialized = 0,
        int servicesRegistered = 0,
        long currentMemoryUsageBytes = 0,
        bool isStartupSuccessful = true,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new CoreStartupMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.CoreStartupMessage,
            Source = source.IsEmpty ? systemName?.Length <= 64 ? systemName : systemName?[..64] ?? "System" : source,
            Priority = MessagePriority.High, // System startup is high priority
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            SystemName = systemName?.Length <= 64 ? systemName : systemName?[..64] ?? throw new ArgumentNullException(nameof(systemName)),
            Version = version?.Length <= 32 ? version : version?[..32] ?? "1.0.0",
            Environment = environment?.Length <= 32 ? environment : environment?[..32] ?? "Development",
            InstanceId = instanceId == default ? Guid.NewGuid() : instanceId,
            StartupDurationMs = Math.Max(0, startupDurationMs),
            ComponentsInitialized = Math.Max(0, componentsInitialized),
            ServicesRegistered = Math.Max(0, servicesRegistered),
            CurrentMemoryUsageBytes = Math.Max(0, currentMemoryUsageBytes),
            IsStartupSuccessful = isStartupSuccessful
        };
    }
}