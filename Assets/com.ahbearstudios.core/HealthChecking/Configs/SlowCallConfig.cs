using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Configuration for slow call detection
/// </summary>
public sealed record SlowCallConfig : ISlowCallConfig
{
    /// <summary>
    /// Duration threshold for considering a call slow
    /// </summary>
    public TimeSpan SlowCallDurationThreshold { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Percentage of slow calls that triggers circuit opening (0-100)
    /// </summary>
    public double SlowCallRateThreshold { get; init; } = 50.0;

    /// <summary>
    /// Minimum number of slow calls required before evaluating threshold
    /// </summary>
    public int MinimumSlowCalls { get; init; } = 5;

    /// <summary>
    /// Whether slow calls should be considered as failures
    /// </summary>
    public bool TreatSlowCallsAsFailures { get; init; } = true;

    /// <summary>
    /// Validates slow call configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (SlowCallDurationThreshold <= TimeSpan.Zero)
            errors.Add("SlowCallDurationThreshold must be greater than zero");

        if (SlowCallRateThreshold < 0 || SlowCallRateThreshold > 100)
            errors.Add("SlowCallRateThreshold must be between 0 and 100");

        if (MinimumSlowCalls < 0)
            errors.Add("MinimumSlowCalls must be non-negative");

        return errors;
    }
}