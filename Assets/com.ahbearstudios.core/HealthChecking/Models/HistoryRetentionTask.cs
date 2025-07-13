using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Task for history retention cleanup
/// </summary>
internal sealed record HistoryRetentionTask
{
    public FixedString64Bytes CheckName { get; init; }
    public DateTime ScheduledTime { get; init; }
}