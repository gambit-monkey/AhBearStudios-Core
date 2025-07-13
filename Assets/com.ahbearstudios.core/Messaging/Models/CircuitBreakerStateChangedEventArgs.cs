namespace AhBearStudios.Core.Messaging.Models;

/// <summary>
/// Event arguments for circuit breaker state changes.
/// </summary>
public sealed class CircuitBreakerStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the message type associated with the circuit breaker.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the previous circuit breaker state.
    /// </summary>
    public CircuitBreakerState OldState { get; init; }

    /// <summary>
    /// Gets the new circuit breaker state.
    /// </summary>
    public CircuitBreakerState NewState { get; init; }

    /// <summary>
    /// Gets the reason for the state change.
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// Gets the timestamp of the state change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}