namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Defines the severity levels for performance anomalies.
/// </summary>
public enum AnomalySeverity : byte
{
    /// <summary>
    /// Low severity - informational, may not require immediate action.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - should be investigated and addressed.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - requires immediate attention and resolution.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - indicates system failure or severe degradation.
    /// </summary>
    Critical = 3
}