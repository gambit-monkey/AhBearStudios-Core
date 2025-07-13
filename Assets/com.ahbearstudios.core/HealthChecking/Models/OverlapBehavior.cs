namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Allows multiple executions to run simultaneously when there is an overlap.
/// </summary>
public enum OverlapBehavior
{
    /// <summary>
    /// Skip the scheduled execution
    /// </summary>
    Skip,

    /// <summary>
    /// Queue the execution to run after current completes
    /// </summary>
    Queue,

    /// <summary>
    /// Cancel the running execution and start new one
    /// </summary>
    Cancel,

    /// <summary>
    /// Allow concurrent executions up to limit
    /// </summary>
    Concurrent,

    /// <summary>
    /// Execute the overlapping tasks in parallel.
    /// </summary>
    Parallel
}