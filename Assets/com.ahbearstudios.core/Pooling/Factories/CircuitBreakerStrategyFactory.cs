using System;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory for creating CircuitBreakerStrategy instances with proper dependency injection and configuration.
    /// Follows the Builder → Config → Factory → Service pattern as per development guidelines.
    /// </summary>
    public class CircuitBreakerStrategyFactory : ICircuitBreakerStrategyFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingStrategyConfigBuilder _configBuilder;

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerStrategyFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for system integration.</param>
        /// <param name="profilerService">The profiler service for performance monitoring.</param>
        /// <param name="alertService">The alert service for critical error notifications.</param>
        /// <param name="messageBusService">The message bus service for event publishing.</param>
        /// <param name="configBuilder">The configuration builder for creating strategy configs.</param>
        public CircuitBreakerStrategyFactory(
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService,
            IPoolingStrategyConfigBuilder configBuilder)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _configBuilder = configBuilder ?? throw new ArgumentNullException(nameof(configBuilder));

            _loggingService.LogInfo("CircuitBreakerStrategyFactory initialized");
        }

        /// <summary>
        /// Creates a new CircuitBreakerStrategy wrapping the specified inner strategy.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality.</param>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured CircuitBreakerStrategy instance.</returns>
        public CircuitBreakerStrategy Create(IPoolingStrategy innerStrategy, PoolingStrategyConfig configuration)
        {
            if (innerStrategy == null)
                throw new ArgumentNullException(nameof(innerStrategy));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Creating CircuitBreakerStrategy wrapping {innerStrategy.Name}");

            return new CircuitBreakerStrategy(
                innerStrategy,
                configuration);
        }

        /// <summary>
        /// Creates a new CircuitBreakerStrategy with default configuration.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality.</param>
        /// <returns>A configured CircuitBreakerStrategy instance with default settings.</returns>
        public CircuitBreakerStrategy CreateDefault(IPoolingStrategy innerStrategy)
        {
            if (innerStrategy == null)
                throw new ArgumentNullException(nameof(innerStrategy));

            _loggingService.LogInfo($"Creating default CircuitBreakerStrategy wrapping {innerStrategy.Name}");

            var defaultConfig = _configBuilder
                .WithName($"CircuitBreaker-{innerStrategy.Name}")
                .WithCircuitBreaker(true, 5, TimeSpan.FromSeconds(30))
                .WithHealthMonitoring(true, TimeSpan.FromMinutes(2))
                .WithMetrics(true, 500)
                .Build();

            return Create(innerStrategy, defaultConfig);
        }

        /// <summary>
        /// Creates a new CircuitBreakerStrategy with custom circuit breaker parameters.
        /// </summary>
        /// <param name="innerStrategy">The strategy to wrap with circuit breaker functionality.</param>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="failureThreshold">Number of failures before opening the circuit.</param>
        /// <param name="recoveryTime">Time to wait before attempting recovery.</param>
        /// <returns>A configured CircuitBreakerStrategy instance.</returns>
        public CircuitBreakerStrategy CreateWithCustomParameters(
            IPoolingStrategy innerStrategy,
            PoolingStrategyConfig configuration,
            int failureThreshold,
            TimeSpan recoveryTime)
        {
            if (innerStrategy == null)
                throw new ArgumentNullException(nameof(innerStrategy));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Validate parameters
            if (failureThreshold < 1)
            {
                var errorMessage = $"Invalid failure threshold: {failureThreshold}. Must be at least 1.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(failureThreshold), errorMessage);
            }

            if (recoveryTime <= TimeSpan.Zero)
            {
                var errorMessage = $"Invalid recovery time: {recoveryTime}. Must be greater than zero.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(recoveryTime), errorMessage);
            }

            _loggingService.LogInfo($"Creating CircuitBreakerStrategy with custom parameters - " +
                $"Inner Strategy: {innerStrategy.Name}, Failure Threshold: {failureThreshold}, " +
                $"Recovery Time: {recoveryTime}");

            // Update configuration with custom parameters
            var customConfig = new PoolingStrategyConfig
            {
                Name = configuration.Name,
                PerformanceBudget = configuration.PerformanceBudget,
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = failureThreshold,
                CircuitBreakerRecoveryTime = recoveryTime,
                EnableHealthMonitoring = configuration.EnableHealthMonitoring,
                HealthCheckInterval = configuration.HealthCheckInterval,
                EnableDetailedMetrics = configuration.EnableDetailedMetrics,
                MaxMetricsSamples = configuration.MaxMetricsSamples,
                EnableDebugLogging = configuration.EnableDebugLogging,
                Tags = configuration.Tags
            };

            return Create(innerStrategy, customConfig);
        }
    }
}