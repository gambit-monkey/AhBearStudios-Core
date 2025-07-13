using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Represents a maintenance window period
/// </summary>
public sealed record MaintenanceWindow
{
    /// <summary>
    /// Start time of the maintenance window
    /// </summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>
    /// End time of the maintenance window
    /// </summary>
    public TimeSpan EndTime { get; init; }

    /// <summary>
    /// Days of week when this window applies
    /// </summary>
    public HashSet<DayOfWeek> DaysOfWeek { get; init; } = new();

    /// <summary>
    /// Specific dates when this window applies
    /// </summary>
    public List<DateTime> SpecificDates { get; init; } = new();

    /// <summary>
    /// Whether this window recurs
    /// </summary>
    public bool Recurring { get; init; } = true;

    /// <summary>
    /// Validates maintenance window
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (StartTime < TimeSpan.Zero || StartTime >= TimeSpan.FromDays(1))
            errors.Add("StartTime must be between 00:00:00 and 23:59:59");

        if (EndTime < TimeSpan.Zero || EndTime >= TimeSpan.FromDays(1))
            errors.Add("EndTime must be between 00:00:00 and 23:59:59");

        if (Recurring && DaysOfWeek.Count == 0 && SpecificDates.Count == 0)
            errors.Add("Recurring maintenance windows must specify either DaysOfWeek or SpecificDates");

        return errors;
    }
}