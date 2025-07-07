using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Contains information about a health check creation for batch operations.
/// Used with HealthCheckMessageFactory.CreateBatchHealthCheckCreated method.
/// </summary>
public readonly record struct HealthCheckCreationInfo(
    /// <summary>
    /// Gets the name of the health check that was created.
    /// </summary>
    FixedString64Bytes HealthCheckName,

    /// <summary>
    /// Gets the type of the health check that was created.
    /// </summary>
    FixedString64Bytes HealthCheckType,

    /// <summary>
    /// Gets the time taken to create the health check.
    /// </summary>
    TimeSpan CreationDuration,

    /// <summary>
    /// Gets the configuration identifier used during creation.
    /// </summary>
    FixedString128Bytes ConfigurationId = default)
{
    /// <summary>
    /// Gets the creation duration in milliseconds.
    /// </summary>
    public double CreationDurationMs => CreationDuration.TotalMilliseconds;

    /// <summary>
    /// Gets whether this creation was slow (> 1000ms).
    /// </summary>
    public bool IsSlowCreation => CreationDurationMs > 1000.0;

    /// <summary>
    /// Gets whether this creation was fast (< 10ms).
    /// </summary>
    public bool IsFastCreation => CreationDurationMs < 10.0;

    /// <summary>
    /// Validates that all required fields are properly set.
    /// </summary>
    /// <returns>True if the info is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !HealthCheckName.IsEmpty
               && !HealthCheckType.IsEmpty
               && CreationDuration >= TimeSpan.Zero;
    }

    /// <summary>
    /// Gets a formatted string representation of the creation info.
    /// </summary>
    /// <returns>A formatted string containing key information.</returns>
    public override string ToString()
    {
        var configText = !ConfigurationId.IsEmpty ? $" (Config: {ConfigurationId})" : "";
        return $"{HealthCheckName}:{HealthCheckType} - {CreationDurationMs:F1}ms{configText}";
    }
}