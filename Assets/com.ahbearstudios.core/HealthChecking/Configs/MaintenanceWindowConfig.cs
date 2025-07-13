using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Maintenance window configuration for scheduled downtime
/// </summary>
public sealed record MaintenanceWindowConfig
{
    /// <summary>
    /// Whether maintenance windows are enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// List of maintenance windows
    /// </summary>
    public List<MaintenanceWindow> MaintenanceWindows { get; init; } = new();

    /// <summary>
    /// Behavior during maintenance windows
    /// </summary>
    public MaintenanceBehavior Behavior { get; init; } = MaintenanceBehavior.Skip;

    /// <summary>
    /// Validates maintenance window configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!Enum.IsDefined(typeof(MaintenanceBehavior), Behavior))
            errors.Add($"Invalid maintenance behavior: {Behavior}");

        foreach (var window in MaintenanceWindows)
        {
            errors.AddRange(window.Validate());
        }

        return errors;
    }
}