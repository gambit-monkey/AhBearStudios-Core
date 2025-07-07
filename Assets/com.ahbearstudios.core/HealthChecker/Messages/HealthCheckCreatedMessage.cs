using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Messages;

/// <summary>
/// Message published when a health check is successfully created by a factory.
/// Provides comprehensive information about the creation event including timing,
/// configuration, and correlation data for monitoring, logging, and audit purposes.
/// Designed to be Burst-compatible and efficient for high-frequency factory operations.
/// </summary>
public readonly record struct HealthCheckCreatedMessage(
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    Guid Id,

    /// <summary>
    /// Gets the timestamp when this message was created (UTC ticks).
    /// </summary>
    long TimestampTicks,

    /// <summary>
    /// Gets the type code that uniquely identifies this message type.
    /// Used for serialization and deserialization.
    /// </summary>
    ushort TypeCode,

    /// <summary>
    /// Gets the name of the health check that was created.
    /// </summary>
    FixedString64Bytes HealthCheckName,

    /// <summary>
    /// Gets the type name of the health check that was created.
    /// </summary>
    FixedString64Bytes HealthCheckType,

    /// <summary>
    /// Gets the configuration identifier used during creation.
    /// </summary>
    FixedString128Bytes ConfigurationId,

    /// <summary>
    /// Gets the time taken to create the health check in ticks.
    /// Use CreationDuration property for TimeSpan representation.
    /// </summary>
    long CreationDurationTicks,

    /// <summary>
    /// Gets the identifier of the factory that created the health check.
    /// </summary>
    FixedString64Bytes FactoryId,

    /// <summary>
    /// Gets the correlation identifier for tracking related operations.
    /// </summary>
    FixedString64Bytes CorrelationId,

    /// <summary>
    /// Gets additional creation context and metadata.
    /// </summary>
    FixedString128Bytes CreationContext = default,

    /// <summary>
    /// Gets the severity level associated with this creation event.
    /// Used for filtering and routing in monitoring systems.
    /// </summary>
    MessageSeverity Severity = MessageSeverity.Info,

    /// <summary>
    /// Gets optional tags for categorization and filtering.
    /// </summary>
    FixedString64Bytes Tags = default,

    /// <summary>
    /// Gets the environment where this creation occurred.
    /// </summary>
    FixedString32Bytes Environment = default,

    /// <summary>
    /// Gets the application instance identifier.
    /// </summary>
    FixedString32Bytes InstanceId = default) : IMessage
{
    #region Computed Properties

    /// <summary>
    /// Gets the DateTime representation of the TimestampTicks value (UTC).
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Gets the TimeSpan representation of the creation duration.
    /// </summary>
    public TimeSpan CreationDuration => new TimeSpan(CreationDurationTicks);

    /// <summary>
    /// Gets the creation duration in milliseconds for performance monitoring.
    /// </summary>
    public double CreationDurationMs => CreationDuration.TotalMilliseconds;

    /// <summary>
    /// Gets whether this health check creation was slow based on common thresholds.
    /// A creation is considered slow if it took longer than 1000ms.
    /// </summary>
    public bool IsSlowCreation => CreationDurationMs > 1000.0;

    /// <summary>
    /// Gets whether this health check creation was very fast.
    /// A creation is considered very fast if it took less than 10ms.
    /// </summary>
    public bool IsFastCreation => CreationDurationMs < 10.0;

    /// <summary>
    /// Gets the age of this message relative to the current time.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - Timestamp;

    /// <summary>
    /// Gets whether this message has a valid correlation ID for tracking purposes.
    /// </summary>
    public bool HasCorrelationId => !CorrelationId.IsEmpty && CorrelationId.Length > 0;

    /// <summary>
    /// Gets whether this message has additional creation context.
    /// </summary>
    public bool HasCreationContext => !CreationContext.IsEmpty && CreationContext.Length > 0;

    /// <summary>
    /// Gets whether this message has associated tags.
    /// </summary>
    public bool HasTags => !Tags.IsEmpty && Tags.Length > 0;

    /// <summary>
    /// Gets whether this message has environment information.
    /// </summary>
    public bool HasEnvironment => !Environment.IsEmpty && Environment.Length > 0;

    /// <summary>
    /// Gets whether this message has instance ID information.
    /// </summary>
    public bool HasInstanceId => !InstanceId.IsEmpty && InstanceId.Length > 0;

    /// <summary>
    /// Gets whether this message has a non-default configuration ID.
    /// </summary>
    public bool HasCustomConfiguration => !ConfigurationId.IsEmpty && 
                                         ConfigurationId.Length > 0 && 
                                         !ConfigurationId.ToString().Equals("default", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Update Methods (Immutable)

    /// <summary>
    /// Creates a new message with updated tags.
    /// </summary>
    /// <param name="newTags">The new tags to assign.</param>
    /// <returns>A new message with updated tags.</returns>
    public HealthCheckCreatedMessage WithTags(FixedString64Bytes newTags)
    {
        return this with { Tags = newTags };
    }

    /// <summary>
    /// Creates a new message with additional tags appended.
    /// </summary>
    /// <param name="additionalTags">The tags to append.</param>
    /// <returns>A new message with appended tags.</returns>
    public HealthCheckCreatedMessage AddTags(FixedString64Bytes additionalTags)
    {
        if (additionalTags.IsEmpty)
            return this;

        if (Tags.IsEmpty)
            return this with { Tags = additionalTags };

        var combinedTags = $"{Tags},{additionalTags}";
        return this with { Tags = combinedTags };
    }

    /// <summary>
    /// Creates a new message with updated severity.
    /// </summary>
    /// <param name="newSeverity">The new severity level.</param>
    /// <returns>A new message with updated severity.</returns>
    public HealthCheckCreatedMessage WithSeverity(MessageSeverity newSeverity)
    {
        return this with { Severity = newSeverity };
    }

    /// <summary>
    /// Creates a new message with updated environment information.
    /// </summary>
    /// <param name="environment">The environment identifier.</param>
    /// <param name="instanceId">The instance identifier.</param>
    /// <returns>A new message with updated environment information.</returns>
    public HealthCheckCreatedMessage WithEnvironmentInfo(
        FixedString32Bytes environment,
        FixedString32Bytes instanceId = default)
    {
        return this with 
        { 
            Environment = environment,
            InstanceId = instanceId.IsEmpty ? InstanceId : instanceId
        };
    }

    /// <summary>
    /// Creates a new message with updated creation context.
    /// </summary>
    /// <param name="context">The new creation context.</param>
    /// <returns>A new message with updated context.</returns>
    public HealthCheckCreatedMessage WithContext(FixedString128Bytes context)
    {
        return this with { CreationContext = context };
    }

    /// <summary>
    /// Creates a new message with updated correlation ID.
    /// </summary>
    /// <param name="correlationId">The new correlation ID.</param>
    /// <returns>A new message with updated correlation ID.</returns>
    /// <exception cref="ArgumentException">Thrown when correlationId is null or empty.</exception>
    public HealthCheckCreatedMessage WithCorrelationId(FixedString64Bytes correlationId)
    {
        if (correlationId.IsEmpty)
            throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

        return this with { CorrelationId = correlationId };
    }

    #endregion

    #region Validation and Utility Methods

    /// <summary>
    /// Validates that all required fields are properly set and within acceptable ranges.
    /// </summary>
    /// <returns>True if the message is valid; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid()
    {
        return Id != Guid.Empty
               && TimestampTicks > 0
               && !HealthCheckName.IsEmpty
               && !HealthCheckType.IsEmpty
               && !FactoryId.IsEmpty
               && CreationDurationTicks >= 0
               && Enum.IsDefined(typeof(MessageSeverity), Severity);
    }

    /// <summary>
    /// Gets individual tags from the Tags field.
    /// </summary>
    /// <returns>An array of individual tag strings.</returns>
    public string[] GetTagsArray()
    {
        if (Tags.IsEmpty)
            return Array.Empty<string>();

        return Tags.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Checks if the message contains a specific tag.
    /// </summary>
    /// <param name="tag">The tag to check for.</param>
    /// <returns>True if the tag is present; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tag is null or empty.</exception>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            throw new ArgumentNullException(nameof(tag));

        if (Tags.IsEmpty)
            return false;

        var tags = GetTagsArray();
        return Array.Exists(tags, t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a formatted string representation suitable for logging and debugging.
    /// </summary>
    /// <returns>A formatted string containing key message information.</returns>
    public override string ToString()
    {
        var durationText = $"{CreationDurationMs:F1}ms";
        var severityText = Severity != MessageSeverity.Info ? $" [{Severity}]" : "";
        var correlationText = HasCorrelationId ? $" (Corr: {CorrelationId})" : "";
        var environmentText = HasEnvironment ? $" @{Environment}" : "";
        var contextText = HasCreationContext ? $" - {CreationContext}" : "";

        return $"[{Timestamp:HH:mm:ss.fff}] HealthCheck Created: {HealthCheckName} ({HealthCheckType}) " +
               $"in {durationText}{severityText}{environmentText}{correlationText}{contextText}";
    }

    /// <summary>
    /// Gets a compact string representation suitable for metrics and monitoring.
    /// </summary>
    /// <returns>A compact string with essential information.</returns>
    public string ToCompactString()
    {
        var statusText = IsSlowCreation ? "SLOW" : IsFastCreation ? "FAST" : "OK";
        return $"{HealthCheckName}:{HealthCheckType} [{statusText}] {CreationDurationMs:F1}ms";
    }

    /// <summary>
    /// Gets a dictionary representation of the message for serialization or analysis.
    /// </summary>
    /// <returns>A dictionary containing all message properties.</returns>
    public IReadOnlyDictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [nameof(Id)] = Id,
            [nameof(Timestamp)] = Timestamp,
            [nameof(TimestampTicks)] = TimestampTicks,
            [nameof(TypeCode)] = TypeCode,
            [nameof(HealthCheckName)] = HealthCheckName.ToString(),
            [nameof(HealthCheckType)] = HealthCheckType.ToString(),
            [nameof(ConfigurationId)] = ConfigurationId.ToString(),
            [nameof(CreationDuration)] = CreationDuration,
            [nameof(CreationDurationMs)] = CreationDurationMs,
            [nameof(CreationDurationTicks)] = CreationDurationTicks,
            [nameof(FactoryId)] = FactoryId.ToString(),
            [nameof(CorrelationId)] = CorrelationId.ToString(),
            [nameof(CreationContext)] = CreationContext.ToString(),
            [nameof(Severity)] = Severity.ToString(),
            [nameof(Tags)] = Tags.ToString(),
            [nameof(Environment)] = Environment.ToString(),
            [nameof(InstanceId)] = InstanceId.ToString(),
            [nameof(IsSlowCreation)] = IsSlowCreation,
            [nameof(IsFastCreation)] = IsFastCreation,
            [nameof(Age)] = Age
        };
    }

    /// <summary>
    /// Compares this message with another for chronological ordering.
    /// </summary>
    /// <param name="other">The other message to compare with.</param>
    /// <returns>A value indicating the relative chronological order.</returns>
    public int CompareTo(HealthCheckCreatedMessage other)
    {
        // Primary sort by timestamp (newest first)
        var timestampComparison = other.TimestampTicks.CompareTo(TimestampTicks);
        if (timestampComparison != 0)
            return timestampComparison;

        // Secondary sort by creation duration (fastest first)
        var durationComparison = CreationDurationTicks.CompareTo(other.CreationDurationTicks);
        if (durationComparison != 0)
            return durationComparison;

        // Tertiary sort by health check name
        return string.Compare(HealthCheckName.ToString(), other.HealthCheckName.ToString(), StringComparison.Ordinal);
    }

    #endregion
}