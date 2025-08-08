using System.Collections.Generic;
using UnityEngine;

namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Strongly-typed settings for Unity-specific alert channels.
/// Provides configuration options for Unity console and notification integration.
/// </summary>
public sealed record UnityChannelSettings : IChannelSettings
{
    /// <summary>
    /// Gets whether Unity's built-in Debug.Log methods should be used.
    /// When enabled, uses Debug.Log, Debug.LogWarning, Debug.LogError based on severity.
    /// </summary>
    public bool UseUnityLog { get; init; } = true;

    /// <summary>
    /// Gets whether stack traces should be enabled for Unity log entries.
    /// </summary>
    public bool EnableStackTrace { get; init; } = true;

    /// <summary>
    /// Gets the mapping of alert severities to Unity log types.
    /// </summary>
    public IReadOnlyDictionary<AlertSeverity, UnityLogType> LogTypeMapping { get; init; } = new Dictionary<AlertSeverity, UnityLogType>
    {
        [AlertSeverity.Debug] = UnityLogType.Log,
        [AlertSeverity.Info] = UnityLogType.Log,
        [AlertSeverity.Warning] = UnityLogType.Warning,
        [AlertSeverity.Critical] = UnityLogType.Error,
        [AlertSeverity.Emergency] = UnityLogType.Error
    };

    /// <summary>
    /// Gets whether alerts should be displayed as Unity Editor notifications.
    /// Only effective when running in the Unity Editor.
    /// </summary>
    public bool ShowEditorNotifications { get; init; } = true;

    /// <summary>
    /// Gets whether alerts should be displayed as in-game UI notifications.
    /// Requires UI system integration to be configured.
    /// </summary>
    public bool ShowInGameNotifications { get; init; } = false;

    /// <summary>
    /// Gets the duration (in seconds) for which in-game notifications should be displayed.
    /// </summary>
    public float NotificationDuration { get; init; } = 5.0f;

    /// <summary>
    /// Gets whether colored console output should be used in Unity.
    /// Uses Unity's rich text formatting for colored console output.
    /// </summary>
    public bool UseColoredOutput { get; init; } = true;

    /// <summary>
    /// Gets the color mapping for different alert severities in Unity console.
    /// Colors use Unity's rich text format (e.g., "red", "yellow", "#ff0000").
    /// </summary>
    public IReadOnlyDictionary<AlertSeverity, string> UnityColors { get; init; } = new Dictionary<AlertSeverity, string>
    {
        [AlertSeverity.Debug] = "grey",
        [AlertSeverity.Info] = "white",
        [AlertSeverity.Warning] = "yellow",
        [AlertSeverity.Critical] = "orange",
        [AlertSeverity.Emergency] = "red"
    };

    /// <summary>
    /// Gets whether Unity console output should include GameObject context when available.
    /// </summary>
    public bool IncludeGameObjectContext { get; init; } = true;

    /// <summary>
    /// Gets whether alerts should trigger Unity's built-in assertion system.
    /// Only applies to Critical and Emergency severity alerts.
    /// </summary>
    public bool TriggerAssertions { get; init; } = false;

    /// <summary>
    /// Gets whether alerts should pause the Unity Editor when they occur.
    /// Only effective in Editor mode and for high-severity alerts.
    /// </summary>
    public bool PauseEditorOnCritical { get; init; } = false;

    /// <summary>
    /// Gets the default Unity channel settings.
    /// </summary>
    public static UnityChannelSettings Default => new();

    /// <summary>
    /// Validates the Unity channel settings.
    /// </summary>
    /// <returns>True if the settings are valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return NotificationDuration >= 0;
    }
}

/// <summary>
/// Defines Unity log types for alert integration.
/// Maps to Unity's LogType enum for compatibility.
/// </summary>
public enum UnityLogType : byte
{
    /// <summary>
    /// Regular log message (Debug.Log).
    /// </summary>
    Log = 0,

    /// <summary>
    /// Warning message (Debug.LogWarning).
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error message (Debug.LogError).
    /// </summary>
    Error = 2,

    /// <summary>
    /// Exception message (Debug.LogException).
    /// </summary>
    Exception = 3
}