using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AhBearStudios.Core.HealthCheck.Models;
using Unity.Collections;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Messages;

/// <summary>
/// Message published when a health check creation fails in a factory.
/// Provides comprehensive error information including exception details, timing,
/// retry information, and correlation data for troubleshooting, monitoring, and audit purposes.
/// Designed to be Burst-compatible and efficient for high-frequency error reporting.
/// </summary>
public readonly record struct HealthCheckCreationFailedMessage(
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
    /// Gets the name of the health check that failed to be created.
    /// </summary>
    FixedString64Bytes HealthCheckName,

    /// <summary>
    /// Gets the type name of the health check that failed to be created.
    /// </summary>
    FixedString64Bytes HealthCheckType,

    /// <summary>
    /// Gets the configuration identifier used during the failed creation attempt.
    /// </summary>
    FixedString128Bytes ConfigurationId,

    /// <summary>
    /// Gets the primary error message describing the creation failure.
    /// </summary>
    FixedString512Bytes ErrorMessage,

    /// <summary>
    /// Gets the type name of the exception that caused the failure.
    /// </summary>
    FixedString128Bytes ExceptionType,

    /// <summary>
    /// Gets the identifier of the factory that attempted to create the health check.
    /// </summary>
    FixedString64Bytes FactoryId,

    /// <summary>
    /// Gets the correlation identifier for tracking related operations.
    /// </summary>
    FixedString64Bytes CorrelationId,

    /// <summary>
    /// Gets the time taken before the creation failed in ticks.
    /// Use FailureDuration property for TimeSpan representation.
    /// </summary>
    long FailureDurationTicks = 0,

    /// <summary>
    /// Gets the severity level of this failure for routing and alerting.
    /// </summary>
    MessageSeverity Severity = MessageSeverity.Error,

    /// <summary>
    /// Gets the category of failure for classification and routing.
    /// </summary>
    FailureCategory Category = FailureCategory.Unknown,

    /// <summary>
    /// Gets the number of retry attempts made before final failure.
    /// </summary>
    int RetryAttempts = 0,

    /// <summary>
    /// Gets whether this failure is recoverable and could succeed on retry.
    /// </summary>
    bool IsRecoverable = false,

    /// <summary>
    /// Gets additional failure context and diagnostic information.
    /// </summary>
    FixedString128Bytes FailureContext = default,

    /// <summary>
    /// Gets optional tags for categorization and filtering.
    /// </summary>
    FixedString64Bytes Tags = default,

    /// <summary>
    /// Gets the environment where this failure occurred.
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
    /// Gets the TimeSpan representation of the failure duration.
    /// </summary>
    public TimeSpan FailureDuration => new TimeSpan(FailureDurationTicks);

    /// <summary>
    /// Gets the failure duration in milliseconds for performance monitoring.
    /// </summary>
    public double FailureDurationMs => FailureDuration.TotalMilliseconds;

    /// <summary>
    /// Gets whether this failure took a long time to occur (> 5000ms).
    /// </summary>
    public bool IsSlowFailure => FailureDurationMs > 5000.0;

    /// <summary>
    /// Gets whether this failure occurred immediately (< 100ms).
    /// </summary>
    public bool IsImmediateFailure => FailureDurationMs < 100.0;

    /// <summary>
    /// Gets the age of this message relative to the current time.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - Timestamp;

    /// <summary>
    /// Gets whether this message has a valid correlation ID for tracking purposes.
    /// </summary>
    public bool HasCorrelationId => !CorrelationId.IsEmpty && CorrelationId.Length > 0;

    /// <summary>
    /// Gets whether this message has additional failure context.
    /// </summary>
    public bool HasFailureContext => !FailureContext.IsEmpty && FailureContext.Length > 0;

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
    /// Gets whether retry attempts were made before this failure.
    /// </summary>
    public bool HasRetryAttempts => RetryAttempts > 0;

    /// <summary>
    /// Gets whether this message has a non-default configuration ID.
    /// </summary>
    public bool HasCustomConfiguration => !ConfigurationId.IsEmpty && 
                                         ConfigurationId.Length > 0 && 
                                         !ConfigurationId.ToString().Equals("default", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this failure should trigger immediate alerts based on severity and category.
    /// </summary>
    public bool ShouldTriggerAlert => Severity >= MessageSeverity.Error || 
                                    Category == FailureCategory.Security ||
                                    Category == FailureCategory.CriticalDependency;

    /// <summary>
    /// Gets whether this failure indicates a systemic issue rather than an isolated problem.
    /// </summary>
    public bool IndicatesSystemicIssue => Category == FailureCategory.Configuration ||
                                         Category == FailureCategory.CriticalDependency ||
                                         Category == FailureCategory.Security ||
                                         (!IsRecoverable && RetryAttempts > 0);

    #endregion

    #region Update Methods (Immutable)

    /// <summary>
    /// Creates a new message with updated tags.
    /// </summary>
    /// <param name="newTags">The new tags to assign.</param>
    /// <returns>A new message with updated tags.</returns>
    public HealthCheckCreationFailedMessage WithTags(FixedString64Bytes newTags)
    {
        return this with { Tags = newTags };
    }

    /// <summary>
    /// Creates a new message with additional tags appended.
    /// </summary>
    /// <param name="additionalTags">The tags to append.</param>
    /// <returns>A new message with appended tags.</returns>
    public HealthCheckCreationFailedMessage AddTags(FixedString64Bytes additionalTags)
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
    public HealthCheckCreationFailedMessage WithSeverity(MessageSeverity newSeverity)
    {
        return this with { Severity = newSeverity };
    }

    /// <summary>
    /// Creates a new message with updated failure category.
    /// </summary>
    /// <param name="newCategory">The new failure category.</param>
    /// <returns>A new message with updated category.</returns>
    public HealthCheckCreationFailedMessage WithCategory(FailureCategory newCategory)
    {
        return this with { Category = newCategory };
    }

    /// <summary>
    /// Creates a new message with updated environment information.
    /// </summary>
    /// <param name="environment">The environment identifier.</param>
    /// <param name="instanceId">The instance identifier.</param>
    /// <returns>A new message with updated environment information.</returns>
    public HealthCheckCreationFailedMessage WithEnvironmentInfo(
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
    /// Creates a new message with updated failure context.
    /// </summary>
    /// <param name="context">The new failure context.</param>
    /// <returns>A new message with updated context.</returns>
    public HealthCheckCreationFailedMessage WithContext(FixedString128Bytes context)
    {
        return this with { FailureContext = context };
    }

    /// <summary>
    /// Creates a new message with updated correlation ID.
    /// </summary>
    /// <param name="correlationId">The new correlation ID.</param>
    /// <returns>A new message with updated correlation ID.</returns>
    /// <exception cref="ArgumentException">Thrown when correlationId is null or empty.</exception>
    public HealthCheckCreationFailedMessage WithCorrelationId(FixedString64Bytes correlationId)
    {
        if (correlationId.IsEmpty)
            throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

        return this with { CorrelationId = correlationId };
    }

    /// <summary>
    /// Creates a new message marking it as recoverable or non-recoverable.
    /// </summary>
    /// <param name="isRecoverable">Whether the failure is recoverable.</param>
    /// <returns>A new message with updated recoverability status.</returns>
    public HealthCheckCreationFailedMessage WithRecoverability(bool isRecoverable)
    {
        return this with { IsRecoverable = isRecoverable };
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
               && !ErrorMessage.IsEmpty
               && FailureDurationTicks >= 0
               && RetryAttempts >= 0
               && Enum.IsDefined(typeof(MessageSeverity), Severity)
               && Enum.IsDefined(typeof(FailureCategory), Category);
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
        var durationText = FailureDurationTicks > 0 ? $" after {FailureDurationMs:F1}ms" : "";
        var severityText = Severity != MessageSeverity.Error ? $" [{Severity}]" : "";
        var categoryText = Category != FailureCategory.Unknown ? $" ({Category})" : "";
        var retryText = HasRetryAttempts ? $" (Retries: {RetryAttempts})" : "";
        var recoverableText = IsRecoverable ? " [Recoverable]" : " [Non-Recoverable]";
        var correlationText = HasCorrelationId ? $" (Corr: {CorrelationId})" : "";
        var environmentText = HasEnvironment ? $" @{Environment}" : "";
        var contextText = HasFailureContext ? $" - {FailureContext}" : "";

        return $"[{Timestamp:HH:mm:ss.fff}] HealthCheck Creation FAILED: {HealthCheckName} ({HealthCheckType})" +
               $"{durationText}{severityText}{categoryText}{retryText}{recoverableText}{environmentText}{correlationText}" +
               $" Error: {ErrorMessage}{contextText}";
    }

    /// <summary>
    /// Gets a compact string representation suitable for metrics and monitoring.
    /// </summary>
    /// <returns>A compact string with essential information.</returns>
    public string ToCompactString()
    {
        var categoryText = Category != FailureCategory.Unknown ? $"[{Category}]" : "";
        var retryText = HasRetryAttempts ? $"R{RetryAttempts}" : "";
        var recoverableText = IsRecoverable ? "REC" : "NON-REC";
        return $"{HealthCheckName}:{HealthCheckType} FAILED {categoryText} {recoverableText} {retryText} - {ErrorMessage}";
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
            [nameof(ErrorMessage)] = ErrorMessage.ToString(),
            [nameof(ExceptionType)] = ExceptionType.ToString(),
            [nameof(FactoryId)] = FactoryId.ToString(),
            [nameof(CorrelationId)] = CorrelationId.ToString(),
            [nameof(FailureDuration)] = FailureDuration,
            [nameof(FailureDurationMs)] = FailureDurationMs,
            [nameof(FailureDurationTicks)] = FailureDurationTicks,
            [nameof(Severity)] = Severity.ToString(),
            [nameof(Category)] = Category.ToString(),
            [nameof(RetryAttempts)] = RetryAttempts,
            [nameof(IsRecoverable)] = IsRecoverable,
            [nameof(FailureContext)] = FailureContext.ToString(),
            [nameof(Tags)] = Tags.ToString(),
            [nameof(Environment)] = Environment.ToString(),
            [nameof(InstanceId)] = InstanceId.ToString(),
            [nameof(IsSlowFailure)] = IsSlowFailure,
            [nameof(IsImmediateFailure)] = IsImmediateFailure,
            [nameof(ShouldTriggerAlert)] = ShouldTriggerAlert,
            [nameof(IndicatesSystemicIssue)] = IndicatesSystemicIssue,
            [nameof(Age)] = Age
        };
    }

    /// <summary>
    /// Compares this message with another for chronological ordering and severity.
    /// </summary>
    /// <param name="other">The other message to compare with.</param>
    /// <returns>A value indicating the relative order for processing priority.</returns>
    public int CompareTo(HealthCheckCreationFailedMessage other)
    {
        // Primary sort by severity (higher severity first)
        var severityComparison = ((int)other.Severity).CompareTo((int)Severity);
        if (severityComparison != 0)
            return severityComparison;

        // Secondary sort by timestamp (newest first)
        var timestampComparison = other.TimestampTicks.CompareTo(TimestampTicks);
        if (timestampComparison != 0)
            return timestampComparison;

        // Tertiary sort by category (critical categories first)
        var categoryComparison = GetCategoryPriority(other.Category).CompareTo(GetCategoryPriority(Category));
        if (categoryComparison != 0)
            return categoryComparison;

        // Final sort by health check name
        return string.Compare(HealthCheckName.ToString(), other.HealthCheckName.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the priority value for a failure category (lower values = higher priority).
    /// </summary>
    /// <param name="category">The failure category.</param>
    /// <returns>Priority value for sorting.</returns>
    private static int GetCategoryPriority(FailureCategory category)
    {
        return category switch
        {
            FailureCategory.Security => 0,
            FailureCategory.CriticalDependency => 1,
            FailureCategory.Configuration => 2,
            FailureCategory.ResourceExhaustion => 3,
            FailureCategory.NetworkConnectivity => 4,
            FailureCategory.InvalidInput => 5,
            FailureCategory.InternalError => 6,
            FailureCategory.Timeout => 7,
            FailureCategory.Unknown => 8,
            _ => 9
        };
    }

    #endregion
}