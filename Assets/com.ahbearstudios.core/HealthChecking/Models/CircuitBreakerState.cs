namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Defines the possible states of a circuit breaker.
    /// Used to control and monitor circuit breaker behavior in health checks.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit breaker is closed - operations are allowed to proceed normally.
        /// This is the normal operating state.
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Circuit breaker is open - operations are blocked due to failures.
        /// Requests will fail immediately without attempting the operation.
        /// </summary>
        Open = 1,

        /// <summary>
        /// Circuit breaker is half-open - limited operations are allowed.
        /// Testing if the system has recovered from previous failures.
        /// </summary>
        HalfOpen = 2
    }
}