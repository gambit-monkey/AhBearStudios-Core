using Unity.Collections;

namespace AhBearStudios.Unity.HealthChecking.Performance;

/// <summary>
/// Pending health check for frame-budget scheduling
/// </summary>
public struct PendingHealthCheck
{
    public FixedString64Bytes Name;
    public float EstimatedExecutionTime;
    public int Priority;
    public float ScheduledAt;
}