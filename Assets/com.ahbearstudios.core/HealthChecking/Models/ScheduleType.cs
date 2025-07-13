namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Types of scheduling strategies available
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Fixed interval scheduling
    /// </summary>
    Interval,

    /// <summary>
    /// Cron expression-based scheduling
    /// </summary>
    Cron,

    /// <summary>
    /// Daily at specific times
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly on specific days and times
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly on specific dates
    /// </summary>
    Monthly,

    /// <summary>
    /// One-time execution
    /// </summary>
    Once,

    /// <summary>
    /// Adaptive interval based on system health
    /// </summary>
    Adaptive
}