namespace AhBearStudios.Core.Alerting.Models;

/// <summary>
/// Alert severity enumeration for core system integration.
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
    Critical = 4
}