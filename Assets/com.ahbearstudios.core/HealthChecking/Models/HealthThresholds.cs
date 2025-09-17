using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Defines health thresholds for determining overall system health status based on individual health check results.
/// Used to configure when a system should be considered unhealthy, degraded, or in warning state.
/// Designed for Unity game development with performance-conscious thresholds.
/// </summary>
public sealed record HealthThresholds
{
    /// <summary>
    /// Gets the percentage threshold for considering the system unhealthy.
    /// When this percentage of health checks fail, the system is considered unhealthy.
    /// </summary>
    public double UnhealthyThreshold { get; init; }

    /// <summary>
    /// Gets the percentage threshold for considering the system in a warning state.
    /// When this percentage of health checks report warnings, the system is in warning state.
    /// </summary>
    public double WarningThreshold { get; init; }

    /// <summary>
    /// Gets the percentage threshold for considering the system degraded.
    /// When this percentage of health checks report degraded status, the system is degraded.
    /// </summary>
    public double DegradedThreshold { get; init; }

    /// <summary>
    /// Gets the minimum number of failed checks required to trigger unhealthy status.
    /// Prevents single check failures from marking the entire system as unhealthy.
    /// </summary>
    public int MinimumFailedChecksForUnhealthy { get; init; }

    /// <summary>
    /// Gets the minimum number of warning checks required to trigger warning status.
    /// Prevents single warnings from marking the entire system with a warning.
    /// </summary>
    public int MinimumWarningChecksForWarning { get; init; }

    /// <summary>
    /// Creates default health thresholds suitable for most game applications
    /// </summary>
    /// <returns>Default HealthThresholds instance</returns>
    public static HealthThresholds CreateDefault()
    {
        return new HealthThresholds
        {
            UnhealthyThreshold = 0.5, // 50% of checks must fail
            WarningThreshold = 0.3,   // 30% of checks with warnings
            DegradedThreshold = 0.4,   // 40% of checks degraded
            MinimumFailedChecksForUnhealthy = 2,
            MinimumWarningChecksForWarning = 2
        };
    }

    /// <summary>
    /// Creates strict health thresholds for critical systems
    /// </summary>
    /// <returns>Strict HealthThresholds instance</returns>
    public static HealthThresholds CreateStrict()
    {
        return new HealthThresholds
        {
            UnhealthyThreshold = 0.2, // 20% of checks fail
            WarningThreshold = 0.1,   // 10% of checks with warnings
            DegradedThreshold = 0.15,  // 15% of checks degraded
            MinimumFailedChecksForUnhealthy = 1,
            MinimumWarningChecksForWarning = 1
        };
    }

    /// <summary>
    /// Creates relaxed health thresholds for development environments
    /// </summary>
    /// <returns>Relaxed HealthThresholds instance</returns>
    public static HealthThresholds CreateRelaxed()
    {
        return new HealthThresholds
        {
            UnhealthyThreshold = 0.75, // 75% of checks must fail
            WarningThreshold = 0.5,    // 50% of checks with warnings
            DegradedThreshold = 0.6,    // 60% of checks degraded
            MinimumFailedChecksForUnhealthy = 3,
            MinimumWarningChecksForWarning = 3
        };
    }

    /// <summary>
    /// Validates the health thresholds configuration
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (UnhealthyThreshold <= 0 || UnhealthyThreshold > 1)
            errors.Add("UnhealthyThreshold must be between 0 and 1");

        if (WarningThreshold <= 0 || WarningThreshold > 1)
            errors.Add("WarningThreshold must be between 0 and 1");

        if (DegradedThreshold <= 0 || DegradedThreshold > 1)
            errors.Add("DegradedThreshold must be between 0 and 1");

        if (MinimumFailedChecksForUnhealthy < 1)
            errors.Add("MinimumFailedChecksForUnhealthy must be at least 1");

        if (MinimumWarningChecksForWarning < 1)
            errors.Add("MinimumWarningChecksForWarning must be at least 1");

        return errors;
    }
}