using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.HealthChecking.Factories;

/// <summary>
/// Factory interface for creating circuit breaker instances with proper configuration and monitoring
/// </summary>
public interface ICircuitBreakerFactory
{
    /// <summary>
    /// Creates or retrieves a circuit breaker for the specified operation
    /// </summary>
    /// <param name="operationName">Unique name for the operation being protected</param>
    /// <param name="config">Optional configuration for the circuit breaker. Uses default if null.</param>
    /// <returns>Circuit breaker instance for the operation</returns>
    /// <exception cref="ArgumentException">Thrown when operation name is null or empty</exception>
    ICircuitBreaker CreateCircuitBreaker(string operationName, CircuitBreakerConfig config = null);

    /// <summary>
    /// Creates a circuit breaker with integrated health check monitoring
    /// </summary>
    /// <param name="operationName">Unique name for the operation being protected</param>
    /// <param name="healthCheckName">Name for the health check integration</param>
    /// <param name="config">Optional configuration for the circuit breaker. Uses default if null.</param>
    /// <returns>Circuit breaker instance with health check integration</returns>
    /// <exception cref="ArgumentException">Thrown when operation name is null or empty</exception>
    ICircuitBreaker CreateCircuitBreakerWithHealthCheck(
        string operationName, 
        string healthCheckName, 
        CircuitBreakerConfig config = null);

    /// <summary>
    /// Gets an existing circuit breaker by operation name
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Circuit breaker if found, null otherwise</returns>
    ICircuitBreaker GetCircuitBreaker(string operationName);

    /// <summary>
    /// Removes and disposes a circuit breaker for the specified operation
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>True if circuit breaker was found and removed, false otherwise</returns>
    bool RemoveCircuitBreaker(string operationName);

    /// <summary>
    /// Gets all registered circuit breaker names
    /// </summary>
    /// <returns>Collection of circuit breaker operation names</returns>
    IReadOnlyCollection<string> GetRegisteredOperations();

    /// <summary>
    /// Validates that all required dependencies are available
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required dependencies are missing</exception>
    void ValidateDependencies();
}