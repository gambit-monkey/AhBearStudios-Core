namespace AhBearStudios.Core.Messaging.Configs;

/// <summary>
/// Configuration for circuit breaker behavior.
/// </summary>
public sealed class CircuitBreakerConfig
{
    /// <summary>
    /// Gets or sets the number of failures required to open the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout before transitioning from Open to HalfOpen.
    /// </summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the number of successes required in HalfOpen to close the circuit.
    /// </summary>
    public int HalfOpenSuccessThreshold { get; set; } = 2;

    /// <summary>
    /// Validates the circuit breaker configuration.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return FailureThreshold > 0 &&
               OpenTimeout > TimeSpan.Zero &&
               HalfOpenSuccessThreshold > 0;
    }
}