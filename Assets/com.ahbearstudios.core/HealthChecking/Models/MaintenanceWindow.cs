using System;
using System.Collections.Generic;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Represents a maintenance window during which health checks may behave differently.
/// Used to define scheduled downtime periods and their impact on health monitoring.
/// </summary>
public sealed record MaintenanceWindow : IValidatable
{
    /// <summary>
    /// Gets or sets the start time of the maintenance window (time of day)
    /// </summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>
    /// Gets or sets the end time of the maintenance window (time of day)
    /// </summary>
    public TimeSpan EndTime { get; init; }

    /// <summary>
    /// Gets or sets the days of the week when this maintenance window applies
    /// </summary>
    public HashSet<DayOfWeek> DaysOfWeek { get; init; } = new();

    /// <summary>
    /// Gets or sets the specific dates when this maintenance window applies (overrides DaysOfWeek)
    /// </summary>
    public HashSet<DateTime> SpecificDates { get; init; } = new();

    /// <summary>
    /// Gets or sets the description of this maintenance window
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this maintenance window is currently active
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the time zone for the maintenance window times
    /// </summary>
    public string TimeZone { get; init; } = "UTC";

    /// <summary>
    /// Validates the maintenance window configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (StartTime < TimeSpan.Zero || StartTime >= TimeSpan.FromDays(1))
            errors.Add("StartTime must be between 00:00:00 and 23:59:59");

        if (EndTime < TimeSpan.Zero || EndTime >= TimeSpan.FromDays(1))
            errors.Add("EndTime must be between 00:00:00 and 23:59:59");

        // Note: End time can be before start time for overnight maintenance windows
        // This is valid and represents a window that crosses midnight

        if (DaysOfWeek.Count == 0 && SpecificDates.Count == 0)
            errors.Add("Either DaysOfWeek or SpecificDates must be specified");

        if (string.IsNullOrWhiteSpace(TimeZone))
            errors.Add("TimeZone cannot be null or empty");

        return errors;
    }

    /// <summary>
    /// Determines if the maintenance window is active at the specified date and time
    /// </summary>
    /// <param name="dateTime">The date and time to check</param>
    /// <returns>True if the maintenance window is active</returns>
    public bool IsActiveAt(DateTime dateTime)
    {
        if (!IsEnabled)
            return false;

        var timeOfDay = dateTime.TimeOfDay;
        var dayOfWeek = dateTime.DayOfWeek;
        var dateOnly = dateTime.Date;

        // Check specific dates first (they override days of week)
        if (SpecificDates.Count > 0)
        {
            if (SpecificDates.Contains(dateOnly))
            {
                return IsTimeInWindow(timeOfDay);
            }
            return false;
        }

        // Check days of week
        if (DaysOfWeek.Contains(dayOfWeek))
        {
            return IsTimeInWindow(timeOfDay);
        }

        return false;
    }

    /// <summary>
    /// Checks if a time of day falls within the maintenance window time range
    /// </summary>
    /// <param name="timeOfDay">The time of day to check</param>
    /// <returns>True if the time is within the maintenance window</returns>
    private bool IsTimeInWindow(TimeSpan timeOfDay)
    {
        // Handle overnight windows (end time before start time)
        if (EndTime <= StartTime)
        {
            return timeOfDay >= StartTime || timeOfDay <= EndTime;
        }
        
        // Normal window (same day)
        return timeOfDay >= StartTime && timeOfDay <= EndTime;
    }

    /// <summary>
    /// Returns a string representation of the maintenance window
    /// </summary>
    /// <returns>Formatted string describing the maintenance window</returns>
    public override string ToString()
    {
        var description = !string.IsNullOrWhiteSpace(Description) ? $" ({Description})" : "";
        var timeRange = $"{StartTime:hh\\:mm}-{EndTime:hh\\:mm}";
        
        if (SpecificDates.Count > 0)
        {
            return $"MaintenanceWindow{description}: {timeRange} on {SpecificDates.Count} specific dates";
        }
        
        if (DaysOfWeek.Count == 7)
        {
            return $"MaintenanceWindow{description}: {timeRange} daily";
        }
        
        return $"MaintenanceWindow{description}: {timeRange} on {string.Join(", ", DaysOfWeek)}";
    }
}