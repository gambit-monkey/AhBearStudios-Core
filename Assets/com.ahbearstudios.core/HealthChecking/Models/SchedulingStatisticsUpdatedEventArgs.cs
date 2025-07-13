namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for scheduling statistics updates
/// </summary>
public sealed class SchedulingStatisticsUpdatedEventArgs : EventArgs
{
    public SchedulingStatistics Statistics { get; init; }
}