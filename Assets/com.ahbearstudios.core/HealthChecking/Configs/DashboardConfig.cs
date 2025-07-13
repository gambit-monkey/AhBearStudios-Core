using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
/// Dashboard configuration for real-time monitoring
/// </summary>
public sealed record DashboardConfig
{
    /// <summary>
    /// Whether dashboard is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Dashboard refresh interval
    /// </summary>
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Widgets to display on the dashboard
    /// </summary>
    public List<DashboardWidget> Widgets { get; init; } = new();

    /// <summary>
    /// Dashboard themes available
    /// </summary>
    public List<string> AvailableThemes { get; init; } = new() { "Light", "Dark", "Auto" };

    /// <summary>
    /// Default theme
    /// </summary>
    public string DefaultTheme { get; init; } = "Auto";

    /// <summary>
    /// Whether to enable real-time updates
    /// </summary>
    public bool EnableRealTimeUpdates { get; init; } = true;

    /// <summary>
    /// Creates dashboard configuration for production environments
    /// </summary>
    /// <returns>Production dashboard configuration</returns>
    public static DashboardConfig ForProduction()
    {
        return new DashboardConfig
        {
            Enabled = true,
            RefreshInterval = TimeSpan.FromSeconds(15),
            EnableRealTimeUpdates = true,
            Widgets = new List<DashboardWidget>
            {
                new() { Type = "HealthOverview", Position = new(0, 0), Size = new(4, 2) },
                new() { Type = "StatusTrends", Position = new(4, 0), Size = new(4, 2) },
                new() { Type = "AlertSummary", Position = new(0, 2), Size = new(3, 2) },
                new() { Type = "PerformanceMetrics", Position = new(3, 2), Size = new(5, 2) },
                new() { Type = "CircuitBreakerStatus", Position = new(0, 4), Size = new(4, 1) },
                new() { Type = "DegradationStatus", Position = new(4, 4), Size = new(4, 1) }
            }
        };
    }

    /// <summary>
    /// Validates dashboard configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (RefreshInterval <= TimeSpan.Zero)
            errors.Add("RefreshInterval must be greater than zero");

        if (string.IsNullOrWhiteSpace(DefaultTheme))
            errors.Add("DefaultTheme cannot be null or empty");

        foreach (var widget in Widgets)
        {
            errors.AddRange(widget.Validate());
        }

        return errors;
    }
}