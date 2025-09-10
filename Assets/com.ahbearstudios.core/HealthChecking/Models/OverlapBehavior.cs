namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Defines the behavior when a scheduled health check execution overlaps with a previous execution.
/// Used to control how concurrent or overlapping executions are handled.
/// </summary>
public enum OverlapBehavior
{
    /// <summary>
    /// Skip the current execution if previous is still running
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Queue the current execution to run after the previous completes
    /// </summary>
    Queue = 1,

    /// <summary>
    /// Allow concurrent execution - run both simultaneously
    /// </summary>
    Concurrent = 2,

    /// <summary>
    /// Cancel the previous execution and start the new one
    /// </summary>
    Cancel = 3,

    /// <summary>
    /// Replace the queued execution with the new one (if using queue behavior)
    /// </summary>
    Replace = 4,

    /// <summary>
    /// Extend the timeout of the previous execution and skip the current one
    /// </summary>
    ExtendTimeout = 5,

    /// <summary>
    /// Fail the current execution attempt and log the overlap condition
    /// </summary>
    Fail = 6
}