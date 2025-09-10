namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Defines the type of jitter distribution to apply to scheduled execution times.
/// Used to distribute load and prevent thundering herd problems in health check execution.
/// </summary>
public enum JitterType
{
    /// <summary>
    /// No jitter - executes at exact scheduled time
    /// </summary>
    None = 0,

    /// <summary>
    /// Uniform distribution jitter - random delay within the jitter range
    /// </summary>
    Uniform = 1,

    /// <summary>
    /// Exponential distribution jitter - favors smaller delays with occasional larger ones
    /// </summary>
    Exponential = 2,

    /// <summary>
    /// Normal (Gaussian) distribution jitter - bell curve distribution around the scheduled time
    /// </summary>
    Normal = 3,

    /// <summary>
    /// Linear increasing jitter - delay increases linearly with each execution
    /// </summary>
    Linear = 4,

    /// <summary>
    /// Decorrelated jitter - each delay is based on the previous delay to reduce correlation
    /// </summary>
    Decorrelated = 5
}