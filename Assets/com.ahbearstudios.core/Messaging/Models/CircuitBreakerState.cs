namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed - normal operation.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open - rejecting calls.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open - testing if service is recovered.
    /// </summary>
    HalfOpen
}