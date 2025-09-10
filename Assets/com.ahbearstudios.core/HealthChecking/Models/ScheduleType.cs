namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Defines the type of scheduling strategy for health check execution.
/// Used to determine how health checks are scheduled and executed over time.
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Fixed interval-based scheduling - executes at regular time intervals
    /// </summary>
    Interval = 0,

    /// <summary>
    /// Daily scheduling - executes at specific times each day
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Weekly scheduling - executes on specific days of the week at specific times
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Monthly scheduling - executes on specific dates of each month
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Cron expression-based scheduling - uses cron syntax for complex scheduling
    /// </summary>
    Cron = 4,

    /// <summary>
    /// One-time execution - executes once at a specified time
    /// </summary>
    Once = 5,

    /// <summary>
    /// Manual trigger only - no automatic scheduling
    /// </summary>
    Manual = 6
}