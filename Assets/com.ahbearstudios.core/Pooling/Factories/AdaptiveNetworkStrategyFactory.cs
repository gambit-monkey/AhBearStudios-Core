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
    /// Factory for creating AdaptiveNetworkStrategy instances with proper dependency injection and configuration.
    /// Follows the Builder → Config → Factory → Service pattern as per development guidelines.
    /// </summary>
    public class AdaptiveNetworkStrategyFactory : IAdaptiveNetworkStrategyFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingStrategyConfigBuilder _configBuilder;

        /// <summary>
        /// Initializes a new instance of the AdaptiveNetworkStrategyFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for system integration.</param>
        /// <param name="profilerService">The profiler service for performance monitoring.</param>
        /// <param name="alertService">The alert service for critical error notifications.</param>
        /// <param name="messageBusService">The message bus service for event publishing.</param>
        /// <param name="configBuilder">The configuration builder for creating strategy configs.</param>
        public AdaptiveNetworkStrategyFactory(
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

            _loggingService.LogInfo("AdaptiveNetworkStrategyFactory initialized");
        }

        /// <summary>
        /// Creates a new AdaptiveNetworkStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured AdaptiveNetworkStrategy instance.</returns>
        public AdaptiveNetworkStrategy Create(PoolingStrategyConfig configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Creating AdaptiveNetworkStrategy with configuration: {configuration.Name}");

            return new AdaptiveNetworkStrategy(
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService);
        }

        /// <summary>
        /// Creates a new AdaptiveNetworkStrategy instance with default configuration.
        /// </summary>
        /// <returns>A configured AdaptiveNetworkStrategy instance with network-optimized defaults.</returns>
        public AdaptiveNetworkStrategy CreateDefault()
        {
            _loggingService.LogInfo("Creating AdaptiveNetworkStrategy with default network-optimized configuration");

            var defaultConfig = _configBuilder
                .NetworkOptimized()
                .WithName("AdaptiveNetwork-Default")
                .WithCircuitBreaker(true, 10, TimeSpan.FromMinutes(5))
                .WithMetrics(true, 1000)
                .Build();

            return Create(defaultConfig);
        }

        /// <summary>
        /// Creates a new AdaptiveNetworkStrategy instance with custom network parameters.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="spikeDetectionThreshold">Threshold for detecting network spikes (0.1 to 1.0).</param>
        /// <param name="preemptiveAllocationRatio">Ratio of preemptive allocations (0.0 to 0.5).</param>
        /// <param name="burstWindow">Time window for burst detection.</param>
        /// <param name="maxBurstAllocations">Maximum allocations during burst.</param>
        /// <returns>A configured AdaptiveNetworkStrategy instance.</returns>
        public AdaptiveNetworkStrategy CreateWithNetworkParameters(
            PoolingStrategyConfig configuration,
            double spikeDetectionThreshold,
            double preemptiveAllocationRatio,
            TimeSpan burstWindow,
            int maxBurstAllocations)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Creating AdaptiveNetworkStrategy with custom network parameters - " +
                $"Spike Threshold: {spikeDetectionThreshold}, Preemptive Ratio: {preemptiveAllocationRatio}, " +
                $"Burst Window: {burstWindow}, Max Burst: {maxBurstAllocations}");

            // Validate parameters
            if (spikeDetectionThreshold < 0.1 || spikeDetectionThreshold > 1.0)
            {
                var errorMessage = $"Invalid spike detection threshold: {spikeDetectionThreshold}. Must be between 0.1 and 1.0.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(spikeDetectionThreshold), errorMessage);
            }

            if (preemptiveAllocationRatio < 0.0 || preemptiveAllocationRatio > 0.5)
            {
                var errorMessage = $"Invalid preemptive allocation ratio: {preemptiveAllocationRatio}. Must be between 0.0 and 0.5.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(preemptiveAllocationRatio), errorMessage);
            }

            if (maxBurstAllocations < 1)
            {
                var errorMessage = $"Invalid max burst allocations: {maxBurstAllocations}. Must be at least 1.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(maxBurstAllocations), errorMessage);
            }

            return new AdaptiveNetworkStrategy(
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService,
                spikeDetectionThreshold,
                preemptiveAllocationRatio,
                burstWindow,
                maxBurstAllocations);
        }
    }
}