namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Strategies for grouping health checks into batches
/// </summary>
public enum BatchGroupingStrategy
{
    /// <summary>
    /// Group by priority level
    /// </summary>
    Priority,

    /// <summary>
    /// Group by health check category
    /// </summary>
    Category,

    /// <summary>
    /// Group by execution time estimates
    /// </summary>
    ExecutionTime,

    /// <summary>
    /// Group by resource requirements
    /// </summary>
    Resource,

    /// <summary>
    /// First-come, first-served
    /// </summary>
    FIFO
}