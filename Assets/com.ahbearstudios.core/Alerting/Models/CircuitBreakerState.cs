namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Represents the state of a circuit breaker in the alert system.
    /// Used to control system behavior during failures and recovery.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit breaker is closed - normal operations are allowed.
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Circuit breaker is open - operations are blocked to prevent cascading failures.
        /// </summary>
        Open = 1,

        /// <summary>
        /// Circuit breaker is in half-open state - testing if service has recovered.
        /// Limited operations are allowed to test system health.
        /// </summary>
        HalfOpen = 2
    }
}