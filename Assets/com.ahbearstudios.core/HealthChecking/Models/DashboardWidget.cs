using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Dashboard widget configuration
/// </summary>
public sealed record DashboardWidget
{
    /// <summary>
    /// Widget type identifier
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Widget position on dashboard
    /// </summary>
    public WidgetPosition Position { get; init; } = new();

    /// <summary>
    /// Widget size
    /// </summary>
    public WidgetSize Size { get; init; } = new();

    /// <summary>
    /// Widget configuration options
    /// </summary>
    public Dictionary<string, object> Config { get; init; } = new();

    /// <summary>
    /// Whether widget is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Validates dashboard widget
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Type))
            errors.Add("Widget Type cannot be null or empty");

        errors.AddRange(Position.Validate());
        errors.AddRange(Size.Validate());

        return errors;
    }
}