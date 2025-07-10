using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Represents the severity level of a health check result, providing fine-grained
/// classification for prioritization, alerting, and remediation decisions.
/// Designed to be Burst-compatible and efficient for high-frequency health monitoring.
/// </summary>
public enum HealthSeverity : byte
{
    /// <summary>
    /// Informational severity level for routine health check results.
    /// Typically used for successful health checks or minor informational messages.
    /// No immediate action required.
    /// </summary>
    [Description("Informational - No action required")]
    Info = 0,

    /// <summary>
    /// Normal severity level for standard health check operations.
    /// Used for healthy status results and normal operational conditions.
    /// Default severity level for most health checks.
    /// </summary>
    [Description("Normal - Standard operational status")]
    Normal = 1,

    /// <summary>
    /// Low severity level for minor issues that don't impact functionality.
    /// Used for warnings or degraded conditions that are not critical.
    /// Monitoring recommended but no immediate action required.
    /// </summary>
    [Description("Low - Minor issues, monitoring recommended")]
    Low = 2,

    /// <summary>
    /// Medium severity level for moderate issues that may impact performance.
    /// Used for degraded conditions that could escalate if not addressed.
    /// Investigation and potential corrective action recommended.
    /// </summary>
    [Description("Medium - Moderate issues, investigation recommended")]
    Medium = 3,

    /// <summary>
    /// High severity level for significant issues requiring prompt attention.
    /// Used for conditions that impact functionality or could lead to failures.
    /// Immediate investigation and corrective action required.
    /// </summary>
    [Description("High - Significant issues, immediate attention required")]
    High = 4,

    /// <summary>
    /// Critical severity level for severe issues requiring immediate intervention.
    /// Used for conditions that cause system failures or data loss.
    /// Emergency response and immediate remediation required.
    /// </summary>
    [Description("Critical - Severe issues, emergency response required")]
    Critical = 5,

    /// <summary>
    /// Emergency severity level for catastrophic failures.
    /// Used for conditions that cause complete system outages or security breaches.
    /// Highest priority response and immediate escalation required.
    /// </summary>
    [Description("Emergency - Catastrophic failures, highest priority response")]
    Emergency = 6
}

/// <summary>
/// Provides utility methods and extensions for working with HealthSeverity values.
/// Offers efficient operations for severity classification, comparison, and display.
/// </summary>
public static class HealthSeverityExtensions
{
    /// <summary>
    /// Gets the display name for the health severity level.
    /// Provides human-readable text for UI and logging purposes.
    /// </summary>
    /// <param name="severity">The health severity to get the display name for.</param>
    /// <returns>A human-readable display name for the severity level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown severity value is provided.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDisplayName(this HealthSeverity severity)
    {
        return severity switch
        {
            HealthSeverity.Info => "Informational",
            HealthSeverity.Normal => "Normal",
            HealthSeverity.Low => "Low",
            HealthSeverity.Medium => "Medium",
            HealthSeverity.High => "High",
            HealthSeverity.Critical => "Critical",
            HealthSeverity.Emergency => "Emergency",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity level")
        };
    }

    /// <summary>
    /// Gets a short, abbreviated display name for the health severity level.
    /// Useful for compact displays, dashboards, and status indicators.
    /// </summary>
    /// <param name="severity">The health severity to get the short name for.</param>
    /// <returns>A short, abbreviated name for the severity level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown severity value is provided.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetShortName(this HealthSeverity severity)
    {
        return severity switch
        {
            HealthSeverity.Info => "INFO",
            HealthSeverity.Normal => "NORM",
            HealthSeverity.Low => "LOW",
            HealthSeverity.Medium => "MED",
            HealthSeverity.High => "HIGH",
            HealthSeverity.Critical => "CRIT",
            HealthSeverity.Emergency => "EMRG",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity level")
        };
    }

    /// <summary>
    /// Gets the description attribute value for the health severity level.
    /// Provides detailed descriptions for documentation and help systems.
    /// </summary>
    /// <param name="severity">The health severity to get the description for.</param>
    /// <returns>The detailed description of the severity level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown severity value is provided.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDescription(this HealthSeverity severity)
    {
        return severity switch
        {
            HealthSeverity.Info => "Informational - No action required",
            HealthSeverity.Normal => "Normal - Standard operational status",
            HealthSeverity.Low => "Low - Minor issues, monitoring recommended",
            HealthSeverity.Medium => "Medium - Moderate issues, investigation recommended",
            HealthSeverity.High => "High - Significant issues, immediate attention required",
            HealthSeverity.Critical => "Critical - Severe issues, emergency response required",
            HealthSeverity.Emergency => "Emergency - Catastrophic failures, highest priority response",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity level")
        };
    }

    /// <summary>
    /// Gets an emoji or symbol representation of the health severity level.
    /// Useful for visual indicators in dashboards and notifications.
    /// </summary>
    /// <param name="severity">The health severity to get the symbol for.</param>
    /// <returns>An emoji or symbol representing the severity level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown severity value is provided.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetSymbol(this HealthSeverity severity)
    {
        return severity switch
        {
            HealthSeverity.Info => "ℹ️",
            HealthSeverity.Normal => "✅",
            HealthSeverity.Low => "🟡",
            HealthSeverity.Medium => "🟠",
            HealthSeverity.High => "🔴",
            HealthSeverity.Critical => "🚨",
            HealthSeverity.Emergency => "🆘",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity level")
        };
    }

    /// <summary>
    /// Determines if the severity level requires immediate attention.
    /// Used for automated alerting and escalation decisions.
    /// </summary>
    /// <param name="severity">The health severity to evaluate.</param>
    /// <returns>True if the severity requires immediate attention; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RequiresImmediateAttention(this HealthSeverity severity)
    {
        return severity >= HealthSeverity.High;
    }

    /// <summary>
    /// Determines if the severity level requires monitoring.
    /// Used for determining when to increase monitoring frequency or alerting.
    /// </summary>
    /// <param name="severity">The health severity to evaluate.</param>
    /// <returns>True if the severity requires monitoring; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RequiresMonitoring(this HealthSeverity severity)
    {
        return severity >= HealthSeverity.Low;
    }

    /// <summary>
    /// Determines if the severity level indicates a critical system condition.
    /// Used for emergency response triggers and system protection mechanisms.
    /// </summary>
    /// <param name="severity">The health severity to evaluate.</param>
    /// <returns>True if the severity indicates a critical condition; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCriticalCondition(this HealthSeverity severity)
    {
        return severity >= HealthSeverity.Critical;
    }

    /// <summary>
    /// Determines if the severity level is considered normal operation.
    /// Used for filtering out routine status updates from alerting systems.
    /// </summary>
    /// <param name="severity">The health severity to evaluate.</param>
    /// <returns>True if the severity indicates normal operation; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNormalOperation(this HealthSeverity severity)
    {
        return severity <= HealthSeverity.Normal;
    }

    /// <summary>
    /// Gets the numeric priority value for the severity level.
    /// Higher numbers indicate higher priority. Useful for sorting and prioritization.
    /// </summary>
    /// <param name="severity">The health severity to get the priority for.</param>
    /// <returns>A numeric priority value (0-6, where 6 is highest priority).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPriority(this HealthSeverity severity)
    {
        return (int)severity;
    }

    /// <summary>
    /// Gets the color code (hex) associated with the severity level.
    /// Useful for UI components, dashboards, and visual representations.
    /// </summary>
    /// <param name="severity">The health severity to get the color for.</param>
    /// <returns>A hex color code representing the severity level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown severity value is provided.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetColorCode(this HealthSeverity severity)
    {
        return severity switch
        {
            HealthSeverity.Info => "#17A2B8",      // Bootstrap info blue
            HealthSeverity.Normal => "#28A745",    // Bootstrap success green
            HealthSeverity.Low => "#FFC107",       // Bootstrap warning yellow
            HealthSeverity.Medium => "#FD7E14",    // Bootstrap orange
            HealthSeverity.High => "#DC3545",     // Bootstrap danger red
            HealthSeverity.Critical => "#721C24", // Dark red
            HealthSeverity.Emergency => "#000000", // Black for maximum contrast
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity level")
        };
    }

    /// <summary>
    /// Determines the escalation timeout in seconds for the severity level.
    /// Used by automated systems to determine when to escalate alerts.
    /// </summary>
    /// <param name="severity">The health severity to get the escalation timeout for.</param>
    /// <returns>The escalation timeout in seconds.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEscalationTimeoutSeconds(this HealthSeverity severity)
    {
        return severity switch
        {
            HealthSeverity.Info => 3600,        // 1 hour
            HealthSeverity.Normal => 1800,      // 30 minutes
            HealthSeverity.Low => 900,          // 15 minutes
            HealthSeverity.Medium => 300,       // 5 minutes
            HealthSeverity.High => 60,          // 1 minute
            HealthSeverity.Critical => 30,      // 30 seconds
            HealthSeverity.Emergency => 10,     // 10 seconds
            _ => 300 // Default to 5 minutes for unknown severity
        };
    }

    /// <summary>
    /// Determines if one severity level is greater than another.
    /// Useful for comparison operations and severity escalation logic.
    /// </summary>
    /// <param name="severity">The first severity to compare.</param>
    /// <param name="other">The second severity to compare.</param>
    /// <returns>True if the first severity is greater than the second; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGreaterThan(this HealthSeverity severity, HealthSeverity other)
    {
        return severity > other;
    }

    /// <summary>
    /// Determines if one severity level is less than another.
    /// Useful for comparison operations and severity filtering logic.
    /// </summary>
    /// <param name="severity">The first severity to compare.</param>
    /// <param name="other">The second severity to compare.</param>
    /// <returns>True if the first severity is less than the second; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLessThan(this HealthSeverity severity, HealthSeverity other)
    {
        return severity < other;
    }

    /// <summary>
    /// Converts a string representation to a HealthSeverity enum value.
    /// Supports both display names and short names for flexible parsing.
    /// </summary>
    /// <param name="severityText">The string representation of the severity.</param>
    /// <param name="ignoreCase">Whether to ignore case when parsing.</param>
    /// <returns>The corresponding HealthSeverity enum value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when severityText is null.</exception>
    /// <exception cref="ArgumentException">Thrown when severityText is not a valid severity.</exception>
    public static HealthSeverity Parse(string severityText, bool ignoreCase = true)
    {
        if (severityText == null)
            throw new ArgumentNullException(nameof(severityText));

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Try exact enum name match first
        if (Enum.TryParse<HealthSeverity>(severityText, ignoreCase, out var result))
            return result;

        // Try display name matching
        return severityText switch
        {
            var s when string.Equals(s, "Informational", comparison) => HealthSeverity.Info,
            var s when string.Equals(s, "INFO", comparison) => HealthSeverity.Info,
            var s when string.Equals(s, "NORM", comparison) => HealthSeverity.Normal,
            var s when string.Equals(s, "LOW", comparison) => HealthSeverity.Low,
            var s when string.Equals(s, "MED", comparison) => HealthSeverity.Medium,
            var s when string.Equals(s, "HIGH", comparison) => HealthSeverity.High,
            var s when string.Equals(s, "CRIT", comparison) => HealthSeverity.Critical,
            var s when string.Equals(s, "EMRG", comparison) => HealthSeverity.Emergency,
            _ => throw new ArgumentException($"'{severityText}' is not a valid HealthSeverity value", nameof(severityText))
        };
    }

    /// <summary>
    /// Tries to convert a string representation to a HealthSeverity enum value.
    /// Returns false if the conversion fails instead of throwing an exception.
    /// </summary>
    /// <param name="severityText">The string representation of the severity.</param>
    /// <param name="severity">The resulting HealthSeverity value if parsing succeeds.</param>
    /// <param name="ignoreCase">Whether to ignore case when parsing.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string severityText, out HealthSeverity severity, bool ignoreCase = true)
    {
        try
        {
            severity = Parse(severityText, ignoreCase);
            return true;
        }
        catch
        {
            severity = HealthSeverity.Normal;
            return false;
        }
    }

    /// <summary>
    /// Gets all defined HealthSeverity values in order of increasing severity.
    /// Useful for UI dropdowns, configuration systems, and iteration.
    /// </summary>
    /// <returns>An array of all HealthSeverity values in ascending order.</returns>
    public static HealthSeverity[] GetAllSeverities()
    {
        return new[]
        {
            HealthSeverity.Info,
            HealthSeverity.Normal,
            HealthSeverity.Low,
            HealthSeverity.Medium,
            HealthSeverity.High,
            HealthSeverity.Critical,
            HealthSeverity.Emergency
        };
    }

    /// <summary>
    /// Validates that a HealthSeverity value is within the defined range.
    /// Useful for input validation and defensive programming.
    /// </summary>
    /// <param name="severity">The severity value to validate.</param>
    /// <returns>True if the severity is valid; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this HealthSeverity severity)
    {
        return Enum.IsDefined(typeof(HealthSeverity), severity);
    }
}