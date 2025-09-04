using System;
using AhBearStudios.Core.Common.Utilities;
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
    /// Gets the unique type code for this message type.
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

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the startup duration as a TimeSpan.
    /// </summary>
    public TimeSpan StartupDuration => TimeSpan.FromMilliseconds(StartupDurationMs);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new CoreStartupMessage with proper validation and defaults.
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
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="source">Source component</param>
    /// <returns>New CoreStartupMessage instance</returns>
    public static CoreStartupMessage CreateFromFixedStrings(
        FixedString64Bytes systemName,
        FixedString32Bytes version,
        FixedString32Bytes environment,
        Guid instanceId,
        double startupDurationMs,
        int componentsInitialized,
        int servicesRegistered,
        long currentMemoryUsageBytes,
        bool isStartupSuccessful,
        Guid correlationId = default,
        FixedString64Bytes source = default)
    {
        // ID generation with explicit parameters to avoid ambiguity
        var sourceString = source.IsEmpty ? "CoreSystem" : source.ToString();
        var messageId = DeterministicIdGenerator.GenerateMessageId("CoreStartupMessage", sourceString, correlationId: null);
        var finalCorrelationId = correlationId == default 
            ? DeterministicIdGenerator.GenerateCorrelationId("CoreStartup", systemName.ToString())
            : correlationId;
        
        return new CoreStartupMessage
        {
            Id = messageId,
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.CoreStartupMessage,
            Source = source.IsEmpty ? "CoreSystem" : source,
            Priority = MessagePriority.High, // System startup is high priority
            CorrelationId = finalCorrelationId,
            
            SystemName = systemName,
            Version = version,
            Environment = environment,
            InstanceId = instanceId == default ? DeterministicIdGenerator.GenerateCoreId($"Instance-{systemName}") : instanceId,
            StartupDurationMs = Math.Max(0, startupDurationMs),
            ComponentsInitialized = Math.Max(0, componentsInitialized),
            ServicesRegistered = Math.Max(0, servicesRegistered),
            CurrentMemoryUsageBytes = Math.Max(0, currentMemoryUsageBytes),
            IsStartupSuccessful = isStartupSuccessful
        };
    }

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
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <param name="source">Source component</param>
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
        Guid correlationId = default,
        string source = null)
    {
        return CreateFromFixedStrings(
            new FixedString64Bytes(systemName?.Length <= 64 ? systemName : systemName?[..64] ?? throw new ArgumentNullException(nameof(systemName))),
            new FixedString32Bytes(version?.Length <= 32 ? version : version?[..32] ?? "1.0.0"),
            new FixedString32Bytes(environment?.Length <= 32 ? environment : environment?[..32] ?? "Development"),
            instanceId,
            startupDurationMs,
            componentsInitialized,
            servicesRegistered,
            currentMemoryUsageBytes,
            isStartupSuccessful,
            correlationId,
            new FixedString64Bytes(source ?? "CoreSystem"));
    }

    #endregion
}