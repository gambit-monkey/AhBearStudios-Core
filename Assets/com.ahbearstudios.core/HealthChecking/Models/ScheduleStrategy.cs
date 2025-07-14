namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Scheduling strategies
/// </summary>
public enum ScheduleStrategy
{
    Interval,
    Cron,
    Adaptive,
    Dependency,
    Custom
}