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
    /// Factory for creating DynamicSizeStrategy instances with proper dependency injection and configuration.
    /// Follows the Builder → Config → Factory → Service pattern as per development guidelines.
    /// </summary>
    public class DynamicSizeStrategyFactory : IDynamicSizeStrategyFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingStrategyConfigBuilder _configBuilder;

        /// <summary>
        /// Initializes a new instance of the DynamicSizeStrategyFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for system integration.</param>
        /// <param name="profilerService">The profiler service for performance monitoring.</param>
        /// <param name="alertService">The alert service for critical error notifications.</param>
        /// <param name="messageBusService">The message bus service for event publishing.</param>
        /// <param name="configBuilder">The configuration builder for creating strategy configs.</param>
        public DynamicSizeStrategyFactory(
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

            _loggingService.LogInfo("DynamicSizeStrategyFactory initialized");
        }

        /// <summary>
        /// Creates a new DynamicSizeStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured DynamicSizeStrategy instance.</returns>
        public DynamicSizeStrategy Create(PoolingStrategyConfig configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Creating DynamicSizeStrategy with configuration: {configuration.Name}");

            return new DynamicSizeStrategy(
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService);
        }

        /// <summary>
        /// Creates a new DynamicSizeStrategy instance with default configuration.
        /// </summary>
        /// <returns>A configured DynamicSizeStrategy instance with default settings.</returns>
        public DynamicSizeStrategy CreateDefault()
        {
            _loggingService.LogInfo("Creating DynamicSizeStrategy with default configuration");

            var defaultConfig = _configBuilder
                .WithName("DynamicSize-Default")
                .WithCircuitBreaker(true, 10, TimeSpan.FromMinutes(5))
                .WithMetrics(true, 1000)
                .WithHealthMonitoring(true, TimeSpan.FromMinutes(2))
                .Build();

            return Create(defaultConfig);
        }

        /// <summary>
        /// Creates a new DynamicSizeStrategy instance with custom threshold parameters.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="expandThreshold">Utilization threshold to trigger expansion (0.0-1.0).</param>
        /// <param name="contractThreshold">Utilization threshold to trigger contraction (0.0-1.0).</param>
        /// <param name="maxUtilization">Maximum allowed utilization before forcing expansion (0.0-1.0).</param>
        /// <param name="validationInterval">Interval between validation checks.</param>
        /// <param name="idleTimeThreshold">Time threshold for considering objects idle.</param>
        /// <returns>A configured DynamicSizeStrategy instance.</returns>
        public DynamicSizeStrategy CreateWithThresholds(
            PoolingStrategyConfig configuration,
            double expandThreshold,
            double contractThreshold,
            double maxUtilization,
            TimeSpan validationInterval,
            TimeSpan idleTimeThreshold)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Validate parameters
            if (expandThreshold < 0.0 || expandThreshold > 1.0)
            {
                var errorMessage = $"Invalid expand threshold: {expandThreshold}. Must be between 0.0 and 1.0.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(expandThreshold), errorMessage);
            }

            if (contractThreshold < 0.0 || contractThreshold > 1.0)
            {
                var errorMessage = $"Invalid contract threshold: {contractThreshold}. Must be between 0.0 and 1.0.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(contractThreshold), errorMessage);
            }

            if (contractThreshold >= expandThreshold)
            {
                var errorMessage = $"Contract threshold ({contractThreshold}) must be less than expand threshold ({expandThreshold}).";
                _loggingService.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            if (maxUtilization < 0.0 || maxUtilization > 1.0)
            {
                var errorMessage = $"Invalid max utilization: {maxUtilization}. Must be between 0.0 and 1.0.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(maxUtilization), errorMessage);
            }

            _loggingService.LogInfo($"Creating DynamicSizeStrategy with custom thresholds - " +
                $"Expand: {expandThreshold}, Contract: {contractThreshold}, Max: {maxUtilization}, " +
                $"Validation: {validationInterval}, Idle: {idleTimeThreshold}");

            return new DynamicSizeStrategy(
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService,
                expandThreshold,
                contractThreshold,
                maxUtilization,
                validationInterval,
                idleTimeThreshold);
        }
    }
}