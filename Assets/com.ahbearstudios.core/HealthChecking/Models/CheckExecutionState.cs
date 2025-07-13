using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Services;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Execution state tracking for health checks
/// </summary>
internal sealed class CheckExecutionState
{
    public FixedString64Bytes HealthCheckName { get; set; }
    public int ExecutionCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public TimeSpan LastExecutionDuration { get; set; }
    public HealthStatus LastExecutionStatus { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public bool IsCurrentlyExecuting { get; set; }
    public int ConsecutiveFailures { get; set; }
    public List<ExecutionHistoryEntry> ExecutionHistory { get; set; } = new();
}