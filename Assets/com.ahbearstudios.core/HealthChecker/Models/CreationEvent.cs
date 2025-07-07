namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents a creation event for tracking and analysis.
/// Immutable record that captures the details of a health check creation attempt.
/// </summary>
public readonly record struct CreationEvent(
    DateTime Timestamp,
    string HealthCheckType,
    bool Success,
    double CreationTimeMs,
    string ConfigurationId = null,
    string FailureReason = null)
{
    /// <summary>
    /// Gets whether this event represents a failed creation.
    /// </summary>
    public bool IsFailed => !Success;

    /// <summary>
    /// Gets whether this event has a configuration ID associated with it.
    /// </summary>
    public bool HasConfigurationId => !string.IsNullOrEmpty(ConfigurationId);

    /// <summary>
    /// Gets whether this event has failure information.
    /// </summary>
    public bool HasFailureReason => !string.IsNullOrEmpty(FailureReason);

    /// <summary>
    /// Gets the age of this event relative to the current time.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - Timestamp;

    /// <summary>
    /// Gets a formatted string representation of the creation event.
    /// </summary>
    /// <returns>A human-readable summary of the creation event.</returns>
    public override string ToString()
    {
        var status = Success ? "SUCCESS" : "FAILED";
        var duration = Success ? $" ({CreationTimeMs:F1}ms)" : "";
        var failure = !Success && HasFailureReason ? $" - {FailureReason}" : "";
        var config = HasConfigurationId ? $" [Config: {ConfigurationId}]" : "";
        
        return $"[{Timestamp:HH:mm:ss.fff}] {HealthCheckType}: {status}{duration}{failure}{config}";
    }
}