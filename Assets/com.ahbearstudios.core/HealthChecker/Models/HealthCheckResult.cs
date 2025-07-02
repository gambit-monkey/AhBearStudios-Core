using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the result of executing a health check operation.
/// Provides comprehensive information about the health status, timing, and context
/// of a health check execution in a Burst-compatible, immutable structure.
/// </summary>
/// <param name="Name">The unique identifier name of the health check that was executed.</param>
/// <param name="Status">The health status determined by the check execution.</param>
/// <param name="Message">A descriptive message providing details about the health check result.</param>
/// <param name="TimestampUtc">The UTC timestamp (in seconds since Unix epoch) when the health check was executed.</param>
/// <param name="SourceSystem">The name or identifier of the system that performed the health check.</param>
/// <param name="CorrelationId">An optional correlation identifier for tracking related health check operations.</param>
/// <param name="Category">The category or grouping that this health check belongs to.</param>
/// <param name="ExecutionDurationMs">The time in milliseconds it took to execute this health check.</param>
/// <param name="Severity">The severity level of the health check result for prioritization.</param>
/// <param name="RetryCount">The number of retry attempts made for this health check (0 for first attempt).</param>
/// <param name="Tags">Optional tags for additional categorization and filtering capabilities.</param>
public readonly record struct HealthCheckResult(
    FixedString64Bytes Name,
    HealthStatus Status,
    FixedString128Bytes Message,
    double TimestampUtc,
    FixedString64Bytes SourceSystem,
    FixedString64Bytes CorrelationId,
    FixedString64Bytes Category,
    float ExecutionDurationMs = 0f,
    HealthSeverity Severity = HealthSeverity.Normal,
    int RetryCount = 0,
    FixedString64Bytes Tags = default)
{
    /// <summary>
    /// Gets whether this health check result indicates a healthy state.
    /// </summary>
    public bool IsHealthy => Status == HealthStatus.Healthy;

    /// <summary>
    /// Gets whether this health check result indicates a degraded state.
    /// </summary>
    public bool IsDegraded => Status == HealthStatus.Degraded;

    /// <summary>
    /// Gets whether this health check result indicates an unhealthy state.
    /// </summary>
    public bool IsUnhealthy => Status == HealthStatus.Unhealthy;

    /// <summary>
    /// Gets whether this health check result requires immediate attention based on status and severity.
    /// </summary>
    public bool RequiresAttention => Status != HealthStatus.Healthy || Severity >= HealthSeverity.High;

    /// <summary>
    /// Gets whether this health check execution was slow based on the execution duration.
    /// A check is considered slow if it took longer than 1000ms to execute.
    /// </summary>
    public bool IsSlowExecution => ExecutionDurationMs > 1000f;

    /// <summary>
    /// Gets whether this health check result represents a retry attempt.
    /// </summary>
    public bool IsRetryAttempt => RetryCount > 0;

    /// <summary>
    /// Gets the DateTime representation of the TimestampUtc value.
    /// </summary>
    public DateTime Timestamp => DateTimeOffset.FromUnixTimeSeconds((long)TimestampUtc).DateTime;

    /// <summary>
    /// Gets whether this health check has a valid correlation ID for tracking purposes.
    /// </summary>
    public bool HasCorrelationId => !CorrelationId.IsEmpty && CorrelationId.Length > 0;

    /// <summary>
    /// Gets whether this health check has associated tags for categorization.
    /// </summary>
    public bool HasTags => !Tags.IsEmpty && Tags.Length > 0;

    /// <summary>
    /// Creates a new HealthCheckResult with the specified name and healthy status.
    /// Uses the current UTC time as the timestamp.
    /// </summary>
    /// <param name="name">The name of the health check.</param>
    /// <param name="sourceSystem">The source system identifier.</param>
    /// <param name="category">The category of the health check.</param>
    /// <param name="message">Optional success message.</param>
    /// <returns>A new HealthCheckResult indicating a healthy state.</returns>
    public static HealthCheckResult CreateHealthy(
        FixedString64Bytes name,
        FixedString64Bytes sourceSystem,
        FixedString64Bytes category,
        FixedString128Bytes message = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultMessage = message.IsEmpty ? "Health check passed successfully" : message;
        
        return new HealthCheckResult(
            Name: name,
            Status: HealthStatus.Healthy,
            Message: resultMessage,
            TimestampUtc: timestamp,
            SourceSystem: sourceSystem,
            CorrelationId: GenerateCorrelationId(),
            Category: category,
            Severity: HealthSeverity.Normal);
    }

    /// <summary>
    /// Creates a new HealthCheckResult with the specified name and degraded status.
    /// Uses the current UTC time as the timestamp.
    /// </summary>
    /// <param name="name">The name of the health check.</param>
    /// <param name="sourceSystem">The source system identifier.</param>
    /// <param name="category">The category of the health check.</param>
    /// <param name="message">Descriptive message about the degraded condition.</param>
    /// <param name="severity">The severity level of the degradation.</param>
    /// <returns>A new HealthCheckResult indicating a degraded state.</returns>
    public static HealthCheckResult CreateDegraded(
        FixedString64Bytes name,
        FixedString64Bytes sourceSystem,
        FixedString64Bytes category,
        FixedString128Bytes message,
        HealthSeverity severity = HealthSeverity.Medium)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        return new HealthCheckResult(
            Name: name,
            Status: HealthStatus.Degraded,
            Message: message,
            TimestampUtc: timestamp,
            SourceSystem: sourceSystem,
            CorrelationId: GenerateCorrelationId(),
            Category: category,
            Severity: severity);
    }

    /// <summary>
    /// Creates a new HealthCheckResult with the specified name and unhealthy status.
    /// Uses the current UTC time as the timestamp.
    /// </summary>
    /// <param name="name">The name of the health check.</param>
    /// <param name="sourceSystem">The source system identifier.</param>
    /// <param name="category">The category of the health check.</param>
    /// <param name="message">Descriptive message about the unhealthy condition.</param>
    /// <param name="severity">The severity level of the failure.</param>
    /// <returns>A new HealthCheckResult indicating an unhealthy state.</returns>
    public static HealthCheckResult CreateUnhealthy(
        FixedString64Bytes name,
        FixedString64Bytes sourceSystem,
        FixedString64Bytes category,
        FixedString128Bytes message,
        HealthSeverity severity = HealthSeverity.High)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        return new HealthCheckResult(
            Name: name,
            Status: HealthStatus.Unhealthy,
            Message: message,
            TimestampUtc: timestamp,
            SourceSystem: sourceSystem,
            CorrelationId: GenerateCorrelationId(),
            Category: category,
            Severity: severity);
    }

    /// <summary>
    /// Creates a new HealthCheckResult with updated execution timing information.
    /// Preserves all other properties while updating the execution duration.
    /// </summary>
    /// <param name="executionDurationMs">The execution duration in milliseconds.</param>
    /// <returns>A new HealthCheckResult with updated timing information.</returns>
    public HealthCheckResult WithExecutionDuration(float executionDurationMs)
    {
        return this with { ExecutionDurationMs = executionDurationMs };
    }

    /// <summary>
    /// Creates a new HealthCheckResult with an updated retry count.
    /// Preserves all other properties while incrementing the retry count.
    /// </summary>
    /// <param name="retryCount">The new retry count value.</param>
    /// <returns>A new HealthCheckResult with updated retry information.</returns>
    public HealthCheckResult WithRetryCount(int retryCount)
    {
        return this with { RetryCount = Math.Max(0, retryCount) };
    }

    /// <summary>
    /// Creates a new HealthCheckResult with updated tags.
    /// Preserves all other properties while updating the tags.
    /// </summary>
    /// <param name="tags">The new tags to associate with this result.</param>
    /// <returns>A new HealthCheckResult with updated tags.</returns>
    public HealthCheckResult WithTags(FixedString64Bytes tags)
    {
        return this with { Tags = tags };
    }

    /// <summary>
    /// Creates a new HealthCheckResult with an updated correlation ID.
    /// Preserves all other properties while updating the correlation ID.
    /// </summary>
    /// <param name="correlationId">The new correlation ID.</param>
    /// <returns>A new HealthCheckResult with updated correlation ID.</returns>
    public HealthCheckResult WithCorrelationId(FixedString64Bytes correlationId)
    {
        return this with { CorrelationId = correlationId };
    }

    /// <summary>
    /// Gets a formatted string representation of this health check result suitable for logging.
    /// </summary>
    /// <returns>A formatted string containing key information about the health check result.</returns>
    public override string ToString()
    {
        var statusText = Status switch
        {
            HealthStatus.Healthy => "✓ HEALTHY",
            HealthStatus.Degraded => "⚠ DEGRADED",
            HealthStatus.Unhealthy => "✗ UNHEALTHY",
            _ => "? UNKNOWN"
        };

        var retryText = RetryCount > 0 ? $" (Retry #{RetryCount})" : "";
        var durationText = ExecutionDurationMs > 0 ? $" [{ExecutionDurationMs:F1}ms]" : "";
        var severityText = Severity != HealthSeverity.Normal ? $" [{Severity}]" : "";

        return $"[{Category}] {Name}: {statusText}{retryText}{durationText}{severityText} - {Message}";
    }

    /// <summary>
    /// Generates a unique correlation ID for tracking health check operations.
    /// Uses a shortened GUID format for efficient storage in FixedString64Bytes.
    /// </summary>
    /// <returns>A unique correlation ID as a FixedString64Bytes.</returns>
    private static FixedString64Bytes GenerateCorrelationId()
    {
        // Create a shortened GUID that fits in FixedString64Bytes
        var guid = Guid.NewGuid();
        var shortId = guid.ToString("N")[..12]; // Take first 12 characters
        return new FixedString64Bytes(shortId);
    }

    /// <summary>
    /// Validates that all required fields are properly set and within acceptable ranges.
    /// </summary>
    /// <returns>True if the health check result is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !Name.IsEmpty 
               && !SourceSystem.IsEmpty 
               && !Category.IsEmpty 
               && TimestampUtc > 0 
               && ExecutionDurationMs >= 0 
               && RetryCount >= 0
               && Enum.IsDefined(typeof(HealthStatus), Status)
               && Enum.IsDefined(typeof(HealthSeverity), Severity);
    }

    /// <summary>
    /// Compares this health check result with another for severity-based ordering.
    /// Unhealthy results with high severity are considered "greater" than healthy results.
    /// </summary>
    /// <param name="other">The other health check result to compare with.</param>
    /// <returns>A value indicating the relative severity order of the health check results.</returns>
    public int CompareTo(HealthCheckResult other)
    {
        // First compare by status (Unhealthy > Degraded > Healthy)
        var statusComparison = ((int)Status).CompareTo((int)other.Status);
        if (statusComparison != 0)
            return statusComparison;

        // Then compare by severity
        var severityComparison = ((int)Severity).CompareTo((int)other.Severity);
        if (severityComparison != 0)
            return severityComparison;

        // Finally compare by timestamp (newer first)
        return other.TimestampUtc.CompareTo(TimestampUtc);
    }
}