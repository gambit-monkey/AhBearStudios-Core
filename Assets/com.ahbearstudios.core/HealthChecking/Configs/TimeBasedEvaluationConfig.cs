using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Time-based evaluation configuration for health thresholds
/// </summary>
public sealed record TimeBasedEvaluationConfig
{
    /// <summary>
    /// Whether time-based evaluation is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Different thresholds for different times of day
    /// </summary>
    public Dictionary<TimeSpan, HealthThresholds> TimeBasedThresholds { get; init; } = new();

    /// <summary>
    /// Different thresholds for different days of the week
    /// </summary>
    public Dictionary<DayOfWeek, HealthThresholds> DayBasedThresholds { get; init; } = new();

    /// <summary>
    /// Holiday and weekend threshold adjustments
    /// </summary>
    public HealthThresholds HolidayThresholds { get; init; }

    /// <summary>
    /// Creates time-based evaluation configuration for high availability systems
    /// </summary>
    /// <returns>High availability time-based evaluation configuration</returns>
    public static TimeBasedEvaluationConfig ForHighAvailability()
    {
        return new TimeBasedEvaluationConfig
        {
            Enabled = true,
            DayBasedThresholds = new Dictionary<DayOfWeek, HealthThresholds>
            {
                [DayOfWeek.Saturday] = new HealthThresholds { HealthyThreshold = 0.8 },
                [DayOfWeek.Sunday] = new HealthThresholds { HealthyThreshold = 0.8 }
            },
            HolidayThresholds = new HealthThresholds { HealthyThreshold = 0.75 }
        };
    }

    /// <summary>
    /// Validates time-based evaluation configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        foreach (var threshold in TimeBasedThresholds.Values)
        {
            errors.AddRange(threshold.Validate());
        }

        foreach (var threshold in DayBasedThresholds.Values)
        {
            errors.AddRange(threshold.Validate());
        }

        if (HolidayThresholds != null)
        {
            errors.AddRange(HolidayThresholds.Validate());
        }

        return errors;
    }
}