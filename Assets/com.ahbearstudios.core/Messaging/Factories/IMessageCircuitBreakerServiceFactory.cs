using AhBearStudios.Core.HealthChecking.Factories;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Services;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating message circuit breaker services.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageCircuitBreakerServiceFactory
{
    /// <summary>
    /// Creates a message circuit breaker service instance.
    /// </summary>
    /// <param name="config">Configuration for the service</param>
    /// <param name="logger">Logging service dependency</param>
    /// <param name="circuitBreakerFactory">Circuit breaker factory from HealthChecking system</param>
    /// <param name="messageBus">Optional message bus service for state change publishing</param>
    /// <returns>Configured message circuit breaker service</returns>
    UniTask<IMessageCircuitBreakerService> CreateServiceAsync(
        MessageCircuitBreakerConfig config,
        ILoggingService logger,
        ICircuitBreakerFactory circuitBreakerFactory,
        IMessageBusService messageBus = null);

    /// <summary>
    /// Creates a message circuit breaker service with default configuration.
    /// </summary>
    /// <param name="logger">Logging service dependency</param>
    /// <param name="circuitBreakerFactory">Circuit breaker factory from HealthChecking system</param>
    /// <param name="messageBus">Optional message bus service for state change publishing</param>
    /// <returns>Message circuit breaker service with default configuration</returns>
    UniTask<IMessageCircuitBreakerService> CreateDefaultServiceAsync(
        ILoggingService logger,
        ICircuitBreakerFactory circuitBreakerFactory,
        IMessageBusService messageBus = null);
}