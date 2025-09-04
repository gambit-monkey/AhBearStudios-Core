using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Factory interface for creating circuit breaker instances with proper configuration and monitoring.
/// Follows Builder → Config → Factory → Service pattern - factories only create, never manage lifecycle.
/// </summary>
public interface ICircuitBreakerFactory
{
    /// <summary>
    /// Creates a circuit breaker for the specified operation asynchronously
    /// </summary>
    /// <param name="operationName">Unique name for the operation being protected</param>
    /// <param name="config">Optional configuration for the circuit breaker. Uses default if null.</param>
    /// <returns>Circuit breaker instance for the operation</returns>
    /// <exception cref="ArgumentException">Thrown when operation name is null or empty</exception>
    UniTask<ICircuitBreaker> CreateCircuitBreakerAsync(string operationName, CircuitBreakerConfig config = null);

    /// <summary>
    /// Creates a circuit breaker with integrated health check monitoring asynchronously
    /// </summary>
    /// <param name="operationName">Unique name for the operation being protected</param>
    /// <param name="healthCheckName">Name for the health check integration</param>
    /// <param name="config">Optional configuration for the circuit breaker. Uses default if null.</param>
    /// <returns>Circuit breaker instance with health check integration</returns>
    /// <exception cref="ArgumentException">Thrown when operation name is null or empty</exception>
    UniTask<ICircuitBreaker> CreateCircuitBreakerWithHealthCheckAsync(
        string operationName, 
        string healthCheckName, 
        CircuitBreakerConfig config = null);

    /// <summary>
    /// Gets all available circuit breaker types that can be created
    /// </summary>
    /// <returns>Collection of available circuit breaker types</returns>
    IEnumerable<Type> GetAvailableCircuitBreakerTypes();

    /// <summary>
    /// Validates that a circuit breaker configuration is valid
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>True if configuration is valid</returns>
    bool ValidateConfiguration(CircuitBreakerConfig config);

    /// <summary>
    /// Validates that all required dependencies are available
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required dependencies are missing</exception>
    void ValidateDependencies();
}