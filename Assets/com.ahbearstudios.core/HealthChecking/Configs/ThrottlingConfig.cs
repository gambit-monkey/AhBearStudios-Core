using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Throttling configuration to control execution rate
/// </summary>
public sealed record ThrottlingConfig
{
    /// <summary>
    /// Whether throttling is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Maximum executions per minute
    /// </summary>
    public int MaxExecutionsPerMinute { get; init; } = 60;

    /// <summary>
    /// Burst allowance for traffic spikes
    /// </summary>
    public int BurstAllowance { get; init; } = 10;

    /// <summary>
    /// Time window for rate calculation
    /// </summary>
    public TimeSpan RateWindow { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Validates throttling configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (MaxExecutionsPerMinute <= 0)
            errors.Add("MaxExecutionsPerMinute must be greater than zero");

        if (BurstAllowance < 0)
            errors.Add("BurstAllowance must be non-negative");

        if (RateWindow <= TimeSpan.Zero)
            errors.Add("RateWindow must be greater than zero");

        return errors;
    }
}