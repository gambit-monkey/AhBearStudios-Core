using System;
using Unity.Collections;

namespace AhBearStudios.Unity.HealthChecking.Performance;

/// <summary>
/// Performance metrics for a specific health check
/// </summary>
public class PerformanceMetrics
{
    public FixedString64Bytes Name { get; }
    public int ExecutionCount { get; private set; }
    public float AverageExecutionTime { get; private set; }
    public float MinExecutionTime { get; private set; } = float.MaxValue;
    public float MaxExecutionTime { get; private set; }
    public int SuccessCount { get; private set; }
    public float SuccessRate => ExecutionCount > 0 ? (float)SuccessCount / ExecutionCount : 0f;

    public PerformanceMetrics(FixedString64Bytes name)
    {
        Name = name;
    }

    public void RecordExecution(float executionTime, bool success)
    {
        ExecutionCount++;

        var totalTime = AverageExecutionTime * (ExecutionCount - 1) + executionTime;
        AverageExecutionTime = totalTime / ExecutionCount;

        MinExecutionTime = Math.Min(MinExecutionTime, executionTime);
        MaxExecutionTime = Math.Max(MaxExecutionTime, executionTime);

        if (success) SuccessCount++;
    }
}