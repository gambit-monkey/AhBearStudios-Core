using System;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Builders;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Strategies;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory for creating HighPerformanceStrategy instances with proper dependency injection and configuration.
    /// Follows the Builder → Config → Factory → Service pattern as per development guidelines.
    /// </summary>
    public class HighPerformanceStrategyFactory : IHighPerformanceStrategyFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingStrategyConfigBuilder _configBuilder;

        /// <summary>
        /// Initializes a new instance of the HighPerformanceStrategyFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for system integration.</param>
        /// <param name="profilerService">The profiler service for performance monitoring.</param>
        /// <param name="alertService">The alert service for critical error notifications.</param>
        /// <param name="messageBusService">The message bus service for event publishing.</param>
        /// <param name="configBuilder">The configuration builder for creating strategy configs.</param>
        public HighPerformanceStrategyFactory(
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

            _loggingService.LogInfo("HighPerformanceStrategyFactory initialized");
        }

        /// <summary>
        /// Creates a new HighPerformanceStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured HighPerformanceStrategy instance.</returns>
        public HighPerformanceStrategy Create(PoolingStrategyConfig configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Creating HighPerformanceStrategy with configuration: {configuration.Name}");

            return new HighPerformanceStrategy(
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService);
        }

        /// <summary>
        /// Creates a new HighPerformanceStrategy instance with default configuration.
        /// </summary>
        /// <returns>A configured HighPerformanceStrategy instance with default settings.</returns>
        public HighPerformanceStrategy CreateDefault()
        {
            _loggingService.LogInfo("Creating default HighPerformanceStrategy");

            var defaultConfig = _configBuilder
                .HighPerformance()
                .WithName("HighPerformance-Default")
                .WithPerformanceBudget(PerformanceBudget.For60FPS())
                .WithCircuitBreaker(true, 3, TimeSpan.FromSeconds(15))
                .WithMetrics(false, 50) // Reduce overhead for high performance
                .Build();

            return Create(defaultConfig);
        }

        /// <summary>
        /// Creates a new HighPerformanceStrategy with custom performance parameters.
        /// </summary>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <param name="preAllocationSize">Size to pre-allocate at startup.</param>
        /// <param name="aggressiveExpansionThreshold">Threshold for aggressive expansion.</param>
        /// <param name="conservativeContractionThreshold">Threshold for conservative contraction.</param>
        /// <returns>A configured HighPerformanceStrategy instance.</returns>
        public HighPerformanceStrategy CreateWithParameters(
            PoolingStrategyConfig configuration,
            int preAllocationSize,
            double aggressiveExpansionThreshold,
            double conservativeContractionThreshold)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Validate parameters
            if (preAllocationSize < 1)
            {
                var errorMessage = $"Invalid pre-allocation size: {preAllocationSize}. Must be at least 1.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(preAllocationSize), errorMessage);
            }

            if (aggressiveExpansionThreshold < 0.5 || aggressiveExpansionThreshold > 1.0)
            {
                var errorMessage = $"Invalid aggressive expansion threshold: {aggressiveExpansionThreshold}. Must be between 0.5 and 1.0.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(aggressiveExpansionThreshold), errorMessage);
            }

            if (conservativeContractionThreshold < 0.0 || conservativeContractionThreshold > 0.3)
            {
                var errorMessage = $"Invalid conservative contraction threshold: {conservativeContractionThreshold}. Must be between 0.0 and 0.3.";
                _loggingService.LogError(errorMessage);
                throw new ArgumentOutOfRangeException(nameof(conservativeContractionThreshold), errorMessage);
            }

            _loggingService.LogInfo($"Creating HighPerformanceStrategy with custom parameters - " +
                $"Pre-allocation: {preAllocationSize}, Expansion: {aggressiveExpansionThreshold}, " +
                $"Contraction: {conservativeContractionThreshold}");

            return new HighPerformanceStrategy(
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService,
                preAllocationSize,
                aggressiveExpansionThreshold,
                conservativeContractionThreshold);
        }

        /// <summary>
        /// Creates a new HighPerformanceStrategy optimized for 60 FPS gameplay.
        /// </summary>
        /// <param name="preAllocationSize">Size to pre-allocate.</param>
        /// <returns>60 FPS optimized strategy.</returns>
        public HighPerformanceStrategy CreateFor60FPS(int preAllocationSize)
        {
            if (preAllocationSize < 1)
                throw new ArgumentException("Pre-allocation size must be at least 1", nameof(preAllocationSize));

            _loggingService.LogInfo($"Creating 60FPS-optimized HighPerformanceStrategy with pre-allocation: {preAllocationSize}");

            var fps60Config = _configBuilder
                .HighPerformance()
                .WithName("HighPerf-60FPS")
                .WithPerformanceBudget(PerformanceBudget.For60FPS())
                .WithTags("60fps", "gaming")
                .WithCircuitBreaker(true, 3, TimeSpan.FromSeconds(15))
                .Build();

            return CreateWithParameters(fps60Config, preAllocationSize, 0.9, 0.1);
        }

        /// <summary>
        /// Creates a new HighPerformanceStrategy optimized for competitive gaming (120+ FPS).
        /// </summary>
        /// <param name="preAllocationSize">Size to pre-allocate.</param>
        /// <returns>120+ FPS optimized strategy.</returns>
        public HighPerformanceStrategy CreateForCompetitiveGaming(int preAllocationSize)
        {
            if (preAllocationSize < 1)
                throw new ArgumentException("Pre-allocation size must be at least 1", nameof(preAllocationSize));

            _loggingService.LogInfo($"Creating competitive gaming HighPerformanceStrategy with pre-allocation: {preAllocationSize}");

            // Create an ultra-high performance budget
            var ultraBudget = new PerformanceBudget
            {
                MaxOperationTime = TimeSpan.FromTicks(500), // 0.05ms (50 microseconds)
                MaxValidationTime = TimeSpan.FromMilliseconds(0.5), // 0.5ms
                MaxExpansionTime = TimeSpan.FromMilliseconds(1), // 1ms
                MaxContractionTime = TimeSpan.FromMilliseconds(0.5), // 0.5ms
                TargetFrameRate = 120,
                FrameTimePercentage = 0.02, // 2% of frame time
                EnablePerformanceMonitoring = true,
                LogPerformanceWarnings = true
            };

            var competitiveConfig = _configBuilder
                .HighPerformance()
                .WithName("CompetitiveGaming")
                .WithPerformanceBudget(ultraBudget)
                .WithTags("120fps", "competitive", "esports")
                .WithCircuitBreaker(true, 2, TimeSpan.FromSeconds(10)) // More sensitive for competitive
                .WithMetrics(false, 25) // Minimize overhead
                .Build();

            return CreateWithParameters(competitiveConfig, preAllocationSize, 0.95, 0.05);
        }

        /// <summary>
        /// Creates a new HighPerformanceStrategy optimized for VR applications.
        /// </summary>
        /// <param name="preAllocationSize">Size to pre-allocate.</param>
        /// <returns>VR optimized strategy.</returns>
        public HighPerformanceStrategy CreateForVR(int preAllocationSize)
        {
            if (preAllocationSize < 1)
                throw new ArgumentException("Pre-allocation size must be at least 1", nameof(preAllocationSize));

            _loggingService.LogInfo($"Creating VR-optimized HighPerformanceStrategy with pre-allocation: {preAllocationSize}");

            // VR requires extremely consistent frame times
            var vrBudget = new PerformanceBudget
            {
                MaxOperationTime = TimeSpan.FromTicks(1000), // 0.1ms (100 microseconds)
                MaxValidationTime = TimeSpan.FromMilliseconds(1), // 1ms
                MaxExpansionTime = TimeSpan.FromMilliseconds(2), // 2ms
                MaxContractionTime = TimeSpan.FromMilliseconds(1), // 1ms
                TargetFrameRate = 90, // Common VR target
                FrameTimePercentage = 0.03, // 3% of frame time
                EnablePerformanceMonitoring = true,
                LogPerformanceWarnings = true
            };

            var vrConfig = _configBuilder
                .HighPerformance()
                .WithName("VROptimized")
                .WithPerformanceBudget(vrBudget)
                .WithTags("vr", "90fps", "consistent-timing")
                .WithCircuitBreaker(true, 2, TimeSpan.FromSeconds(10)) // Sensitive for VR
                .WithMetrics(false, 25) // Minimize overhead
                .Build();

            return CreateWithParameters(vrConfig, preAllocationSize, 0.95, 0.05);
        }
    }
}