using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
/// Event arguments for circuit breaker state changes
/// </summary>
public sealed class CircuitBreakerStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Name of the circuit breaker
    /// </summary>
    public FixedString64Bytes CircuitBreakerName { get; init; }

    /// <summary>
    /// Previous circuit breaker state
    /// </summary>
    public CircuitBreakerState OldState { get; init; }

    /// <summary>
    /// New circuit breaker state
    /// </summary>
    public CircuitBreakerState NewState { get; init; }

    /// <summary>
    /// Reason for the state change
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Timestamp when the state changed
    /// </summary>
    public DateTime Timestamp { get; init; }
}