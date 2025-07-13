using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Widget size on dashboard
/// </summary>
public sealed record WidgetSize(int Width = 1, int Height = 1)
{
    /// <summary>
    /// Validates widget size
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Width <= 0)
            errors.Add("Widget Width must be greater than zero");

        if (Height <= 0)
            errors.Add("Widget Height must be greater than zero");

        return errors;
    }
}