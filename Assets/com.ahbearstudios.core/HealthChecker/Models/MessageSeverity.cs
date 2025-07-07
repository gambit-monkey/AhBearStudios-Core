namespace AhBearStudios.Core.HealthCheck.Models;

/// <summary>
/// Defines severity levels for health check messages.
/// Used for filtering, routing, and prioritization in monitoring systems.
/// </summary>
public enum MessageSeverity : byte
{
    /// <summary>
    /// Informational message for routine operations.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Debug message for detailed troubleshooting.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Warning message indicating potential issues.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error message indicating operation failures.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical message indicating system-level issues.
    /// </summary>
    Critical = 4
}