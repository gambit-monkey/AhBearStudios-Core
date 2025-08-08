using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for console alert channels.
/// Provides configuration options specific to console-based alert delivery.
/// </summary>
public sealed record ConsoleChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets whether colored output is enabled for console alerts.
    /// When enabled, different severity levels are displayed with different colors.
    /// </summary>
    public bool EnableColors { get; init; } = true;

    /// <summary>
    /// Gets the mapping of alert severities to color codes.
    /// Colors are specified in hexadecimal format (e.g., #ff0000 for red).
    /// </summary>
    public IReadOnlyDictionary<AlertSeverity, string> ColorMap { get; init; } = new Dictionary<AlertSeverity, string>
    {
        [AlertSeverity.Debug] = "#808080",    // Gray
        [AlertSeverity.Info] = "#00ff00",     // Green
        [AlertSeverity.Warning] = "#ffff00",  // Yellow
        [AlertSeverity.Critical] = "#ff0000", // Red
        [AlertSeverity.Emergency] = "#800000" // Dark Red
    };

    /// <summary>
    /// Gets whether console output should include timestamps.
    /// </summary>
    public bool IncludeTimestamps { get; init; } = true;

    /// <summary>
    /// Gets whether console output should include source information.
    /// </summary>
    public bool IncludeSource { get; init; } = true;

    /// <summary>
    /// Gets whether console output should include severity level indicators.
    /// </summary>
    public bool IncludeSeverity { get; init; } = true;

    /// <summary>
    /// Gets the maximum line width for console output before wrapping.
    /// Set to 0 for no wrapping.
    /// </summary>
    public int MaxLineWidth { get; init; } = 120;

    /// <summary>
    /// Gets whether stack traces should be included for error-level alerts.
    /// </summary>
    public bool IncludeStackTrace { get; init; } = false;

    /// <summary>
    /// Gets the default console channel settings.
    /// </summary>
    public static ConsoleChannelSettings Default => new();

    /// <summary>
    /// Validates the console channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return MaxLineWidth >= 0;
    }
}