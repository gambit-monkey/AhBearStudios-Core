using System.Collections.Generic;

namespace AhBearStudios.Core.HealthCheck.Configs;

/// <summary>
/// Circuit breaker integration configuration for degradation
/// </summary>
public sealed record CircuitBreakerIntegrationConfig
{
    /// <summary>
    /// Whether circuit breaker integration is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Percentage of open circuit breakers that triggers degradation
    /// </summary>
    public double OpenCircuitBreakerThreshold { get; init; } = 0.3;

    /// <summary>
    /// Whether to consider circuit breaker states in health calculation
    /// </summary>
    public bool IncludeInHealthCalculation { get; init; } = true;

    /// <summary>
    /// Weight of circuit breaker states in degradation calculation
    /// </summary>
    public double CircuitBreakerWeight { get; init; } = 0.8;

    /// <summary>
    /// Validates circuit breaker integration configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (OpenCircuitBreakerThreshold < 0.0 || OpenCircuitBreakerThreshold > 1.0)
            errors.Add("OpenCircuitBreakerThreshold must be between 0.0 and 1.0");

        if (CircuitBreakerWeight < 0.0 || CircuitBreakerWeight > 2.0)
            errors.Add("CircuitBreakerWeight must be between 0.0 and 2.0");

        return errors;
    }
}