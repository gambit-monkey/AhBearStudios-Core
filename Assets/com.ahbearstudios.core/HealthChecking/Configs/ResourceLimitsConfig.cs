using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Resource limits configuration for health check execution
/// </summary>
public sealed record ResourceLimitsConfig
{
    /// <summary>
    /// Maximum memory usage allowed (in bytes, 0 = no limit)
    /// </summary>
    public long MaxMemoryUsage { get; init; } = 0;

    /// <summary>
    /// Maximum CPU usage allowed (percentage, 0 = no limit)
    /// </summary>
    public double MaxCpuUsage { get; init; } = 0;

    /// <summary>
    /// Maximum number of concurrent executions for this health check
    /// </summary>
    public int MaxConcurrentExecutions { get; init; } = 1;

    /// <summary>
    /// Validates resource limits configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxMemoryUsage < 0)
            errors.Add("MaxMemoryUsage must be non-negative");

        if (MaxCpuUsage < 0 || MaxCpuUsage > 100)
            errors.Add("MaxCpuUsage must be between 0 and 100");

        if (MaxConcurrentExecutions < 1)
            errors.Add("MaxConcurrentExecutions must be at least 1");

        return errors;
    }
}