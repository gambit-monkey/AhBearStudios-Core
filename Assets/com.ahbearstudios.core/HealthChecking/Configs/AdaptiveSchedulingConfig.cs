using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Adaptive scheduling configuration for dynamic intervals
/// </summary>
public sealed record AdaptiveSchedulingConfig
{
    /// <summary>
    /// Whether adaptive scheduling is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Minimum allowed interval
    /// </summary>
    public TimeSpan MinInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum allowed interval
    /// </summary>
    public TimeSpan MaxInterval { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Adjustment factor for interval changes (0.1 = 10% adjustment)
    /// </summary>
    public double AdjustmentFactor { get; init; } = 0.2;

    /// <summary>
    /// Whether to adjust based on system health
    /// </summary>
    public bool HealthBasedAdjustment { get; init; } = true;

    /// <summary>
    /// Whether to adjust based on execution time
    /// </summary>
    public bool ExecutionTimeBasedAdjustment { get; init; } = true;

    /// <summary>
    /// Whether to adjust based on system load
    /// </summary>
    public bool LoadBasedAdjustment { get; init; } = false;

    /// <summary>
    /// Target execution time for adjustments
    /// </summary>
    public TimeSpan TargetExecutionTime { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Learning rate for adaptive algorithms
    /// </summary>
    public double LearningRate { get; init; } = 0.1;

    /// <summary>
    /// Validates adaptive scheduling configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MinInterval <= TimeSpan.Zero)
            errors.Add("MinInterval must be greater than zero");

        if (MaxInterval <= MinInterval)
            errors.Add("MaxInterval must be greater than MinInterval");

        if (AdjustmentFactor <= 0 || AdjustmentFactor > 1)
            errors.Add("AdjustmentFactor must be between 0 and 1");

        if (TargetExecutionTime <= TimeSpan.Zero)
            errors.Add("TargetExecutionTime must be greater than zero");

        if (LearningRate <= 0 || LearningRate > 1)
            errors.Add("LearningRate must be between 0 and 1");

        return errors;
    }
}