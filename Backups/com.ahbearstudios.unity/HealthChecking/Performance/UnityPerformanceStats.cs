namespace AhBearStudios.Unity.HealthChecking.Performance;

/// <summary>
/// Unity-specific performance statistics
/// </summary>
public struct UnityPerformanceStats
{
    public float AverageFrameTime;
    public float CurrentFrameTime;
    public float TargetFrameTime;
    public float AvailableHealthCheckBudget;
    public float TotalHealthCheckBudget;
    public float AverageHealthCheckExecutionTime;
    public int PendingHealthCheckCount;
    public int CurrentFrameHealthCheckCount;
    public long MemoryUsage;
    public int DrawCalls;
    public int Batches;
}