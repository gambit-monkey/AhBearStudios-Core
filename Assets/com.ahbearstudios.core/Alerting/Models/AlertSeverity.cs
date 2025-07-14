namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Represents an emergency-level alert severity.
/// This severity indicates an immediate and critical situation
/// requiring urgent attention and action.
/// </summary>
public enum AlertSeverity : byte
{
    /// <summary>
    /// Low severity alert.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity alert.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity alert.
    /// </summary>
    High = 2,

    /// <summary>
    /// Warning severity alert.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Critical severity alert.
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Debug-level alert severity.
    /// Typically used for development or diagnostic purposes where no
    /// immediate action is required.
    /// </summary>
    Debug = 5,

    /// <summary>
    /// Informational alert severity.
    /// Used to convey general information or updates that do not indicate an issue
    /// and do not require any action.
    /// </summary>
    Info = 6,

    /// <summary>
    /// Emergency-level alert severity.
    /// Indicates a critical and life-threatening situation that necessitates
    /// immediate and decisive action.
    /// </summary>
    Emergency = 7
}