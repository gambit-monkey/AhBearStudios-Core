using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Configuration for bulkhead isolation pattern
/// </summary>
public sealed record BulkheadConfig
{
    /// <summary>
    /// Whether bulkhead isolation is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Maximum number of concurrent calls allowed
    /// </summary>
    public int MaxConcurrentCalls { get; init; } = 10;

    /// <summary>
    /// Maximum time to wait for a call slot to become available
    /// </summary>
    public TimeSpan MaxWaitDuration { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether to use fair queuing for waiting calls
    /// </summary>
    public bool UseFairQueuing { get; init; } = true;

    /// <summary>
    /// Maximum queue size for waiting calls
    /// </summary>
    public int MaxQueueSize { get; init; } = 100;

    /// <summary>
    /// Validates bulkhead configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxConcurrentCalls <= 0)
            errors.Add("MaxConcurrentCalls must be greater than zero");

        if (MaxWaitDuration < TimeSpan.Zero)
            errors.Add("MaxWaitDuration must be non-negative");

        if (MaxQueueSize < 0)
            errors.Add("MaxQueueSize must be non-negative");

        return errors;
    }
}