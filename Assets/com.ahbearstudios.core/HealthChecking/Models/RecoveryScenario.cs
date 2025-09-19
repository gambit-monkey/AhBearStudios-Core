namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Recovery scenario types for degradation handling.
    /// Defines different approaches for system recovery based on failure severity and context.
    /// </summary>
    public enum RecoveryScenario
    {
        /// <summary>
        /// Graceful recovery with gradual feature restoration.
        /// Slowly brings system back online with careful monitoring.
        /// </summary>
        GracefulRecovery,

        /// <summary>
        /// Emergency recovery with aggressive measures.
        /// Fast recovery focusing on critical functionality only.
        /// </summary>
        EmergencyRecovery,

        /// <summary>
        /// Circuit breaker specific recovery.
        /// Targets circuit breaker state restoration and health validation.
        /// </summary>
        CircuitBreakerRecovery,

        /// <summary>
        /// Service-specific recovery procedures.
        /// Focuses on individual service restoration without system-wide impact.
        /// </summary>
        ServiceRecovery,

        /// <summary>
        /// Full system restart recovery.
        /// Complete system restart as last resort recovery option.
        /// </summary>
        SystemRestart
    }
}