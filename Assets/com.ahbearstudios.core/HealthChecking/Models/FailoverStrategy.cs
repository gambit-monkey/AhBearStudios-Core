namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Enumeration of failover strategies for circuit breaker behavior when the circuit is open.
/// </summary>
public enum FailoverStrategy : byte
{
    /// <summary>
    /// Return a predetermined default value when the circuit is open.
    /// Uses the DefaultValue property from FailoverConfig.
    /// </summary>
    ReturnDefault = 0,

    /// <summary>
    /// Retry the operation using alternative endpoints or services.
    /// Requires AlternativeEndpoints to be configured in FailoverConfig.
    /// </summary>
    Retry = 1,

    /// <summary>
    /// Use cached fallback data when available.
    /// Utilizes the FallbackCache mechanism when enabled.
    /// </summary>
    Fallback = 2,

    /// <summary>
    /// Throw an exception immediately when circuit is open.
    /// No fallback behavior is applied.
    /// </summary>
    ThrowException = 3
}