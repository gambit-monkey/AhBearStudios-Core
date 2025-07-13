using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Validation configuration for health check results
/// </summary>
public sealed record HealthCheckValidationConfig
{
    /// <summary>
    /// Whether to validate health check results
    /// </summary>
    public bool EnableValidation { get; init; } = true;

    /// <summary>
    /// Minimum acceptable execution time (to detect unusually fast executions)
    /// </summary>
    public TimeSpan MinExecutionTime { get; init; } = TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// Maximum acceptable execution time
    /// </summary>
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Required data fields in health check results
    /// </summary>
    public HashSet<string> RequiredDataFields { get; init; } = new();

    /// <summary>
    /// Custom validation rules
    /// </summary>
    public Dictionary<string, object> CustomValidationRules { get; init; } = new();

    /// <summary>
    /// Validates validation configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MinExecutionTime < TimeSpan.Zero)
            errors.Add("MinExecutionTime must be non-negative");

        if (MaxExecutionTime <= MinExecutionTime)
            errors.Add("MaxExecutionTime must be greater than MinExecutionTime");

        return errors;
    }
}