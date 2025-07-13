namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Behavior when previous execution overlaps with scheduled time
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
    Concurrent
}