using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Holiday and exception date configuration
/// </summary>
public sealed record HolidayConfig
{
    /// <summary>
    /// Whether holiday handling is enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// List of holiday dates
    /// </summary>
    public List<DateTime> Holidays { get; init; } = new();

    /// <summary>
    /// Behavior on holidays
    /// </summary>
    public HolidayBehavior Behavior { get; init; } = HolidayBehavior.Normal;

    /// <summary>
    /// Whether to automatically detect common holidays
    /// </summary>
    public bool AutoDetectHolidays { get; init; } = false;

    /// <summary>
    /// Country/region for holiday detection
    /// </summary>
    public string Region { get; init; } = "US";

    /// <summary>
    /// Validates holiday configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(typeof(HolidayBehavior), Behavior))
            errors.Add($"Invalid holiday behavior: {Behavior}");

        if (AutoDetectHolidays && string.IsNullOrWhiteSpace(Region))
            errors.Add("Region must be specified when AutoDetectHolidays is enabled");

        return errors;
    }
}