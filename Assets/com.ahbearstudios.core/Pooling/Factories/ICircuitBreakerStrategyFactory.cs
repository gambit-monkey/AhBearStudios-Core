using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating CircuitBreakerStrategy instances following the Builder → Config → Factory → Service pattern.
    /// </summary>
    public interface ICircuitBreakerStrategyFactory
    {
        /// <summary>
        /// Creates a new CircuitBreakerStrategy wrapping the specified inner strategy.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality.</param>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured CircuitBreakerStrategy instance.</returns>
        CircuitBreakerStrategy Create(IPoolingStrategy innerStrategy, PoolingStrategyConfig configuration);

        /// <summary>
        /// Creates a new CircuitBreakerStrategy with default configuration.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality.</param>
        /// <returns>A configured CircuitBreakerStrategy instance with default settings.</returns>
        CircuitBreakerStrategy CreateDefault(IPoolingStrategy innerStrategy);

        /// <summary>
        /// Creates a new CircuitBreakerStrategy with custom circuit breaker parameters.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality.</param>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="recoveryTime">Time to wait before attempting recovery.</param>
        /// <returns>A configured CircuitBreakerStrategy instance.</returns>
        CircuitBreakerStrategy CreateWithCustomParameters(
            IPoolingStrategy innerStrategy,
            PoolingStrategyConfig configuration,
            int failureThreshold,
            TimeSpan recoveryTime);
    }
}