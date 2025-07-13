using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Widget position on dashboard
/// </summary>
public sealed record WidgetPosition(int X = 0, int Y = 0)
{
    /// <summary>
    /// Validates widget position
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (X < 0)
            errors.Add("Widget X position must be non-negative");

        if (Y < 0)
            errors.Add("Widget Y position must be non-negative");

        return errors;
    }
}