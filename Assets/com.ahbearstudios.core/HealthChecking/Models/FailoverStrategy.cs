namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Failover strategies available when circuit is open
/// </summary>
public enum FailoverStrategy
{
    /// <summary>
    /// Return a default value
    /// </summary>
    ReturnDefault,

    /// <summary>
    /// Throw an exception
    /// </summary>
    ThrowException,

    /// <summary>
    /// Retry with alternative endpoints
    /// </summary>
    Retry,

    /// <summary>
    /// Return cached result if available
    /// </summary>
    ReturnCached,

    /// <summary>
    /// Execute a custom fallback function
    /// </summary>
    CustomFallback
}