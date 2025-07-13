using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Hysteresis configuration to prevent status flapping
/// </summary>
public sealed record HysteresisConfig
{
    /// <summary>
    /// Whether hysteresis is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Offset applied when transitioning to a worse state
    /// </summary>
    public double WorseStateOffset { get; init; } = 0.05;

    /// <summary>
    /// Offset applied when transitioning to a better state
    /// </summary>
    public double BetterStateOffset { get; init; } = 0.1;

    /// <summary>
    /// Minimum time before allowing state transitions
    /// </summary>
    public TimeSpan MinTransitionTime { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates hysteresis configuration for critical systems
    /// </summary>
    /// <returns>Critical system hysteresis configuration</returns>
    public static HysteresisConfig ForCriticalSystem()
    {
        return new HysteresisConfig
        {
            Enabled = true,
            WorseStateOffset = 0.03,
            BetterStateOffset = 0.15,
            MinTransitionTime = TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Creates hysteresis configuration for high availability systems
    /// </summary>
    /// <returns>High availability hysteresis configuration</returns>
    public static HysteresisConfig ForHighAvailability()
    {
        return new HysteresisConfig
        {
            Enabled = true,
            WorseStateOffset = 0.05,
            BetterStateOffset = 0.1,
            MinTransitionTime = TimeSpan.FromSeconds(45)
        };
    }

    /// <summary>
    /// Validates hysteresis configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (WorseStateOffset < 0.0 || WorseStateOffset > 0.5)
            errors.Add("WorseStateOffset must be between 0.0 and 0.5");

        if (BetterStateOffset < 0.0 || BetterStateOffset > 0.5)
            errors.Add("BetterStateOffset must be between 0.0 and 0.5");

        if (MinTransitionTime < TimeSpan.Zero)
            errors.Add("MinTransitionTime must be non-negative");

        return errors;
    }
}