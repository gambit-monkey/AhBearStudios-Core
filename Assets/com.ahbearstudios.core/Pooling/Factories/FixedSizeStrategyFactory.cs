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
    /// Factory for creating FixedSizeStrategy instances with proper dependency injection and configuration.
    /// Follows the Builder → Config → Factory → Service pattern as per development guidelines.
    /// </summary>
    public class FixedSizeStrategyFactory : IFixedSizeStrategyFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingStrategyConfigBuilder _configBuilder;

        /// <summary>
        /// Initializes a new instance of the FixedSizeStrategyFactory.
        /// </summary>
        /// <param name="loggingService">The logging service for system integration.</param>
        /// <param name="profilerService">The profiler service for performance monitoring.</param>
        /// <param name="alertService">The alert service for critical error notifications.</param>
        /// <param name="messageBusService">The message bus service for event publishing.</param>
        /// <param name="configBuilder">The configuration builder for creating strategy configs.</param>
        public FixedSizeStrategyFactory(
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

            _loggingService.LogInfo("FixedSizeStrategyFactory initialized");
        }

        /// <summary>
        /// Creates a new FixedSizeStrategy instance with the specified configuration.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <param name="configuration">The pooling strategy configuration to use.</param>
        /// <returns>A configured FixedSizeStrategy instance.</returns>
        public FixedSizeStrategy Create(int fixedSize, PoolingStrategyConfig configuration)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"Creating FixedSizeStrategy with size {fixedSize} and configuration: {configuration.Name}");

            return new FixedSizeStrategy(
                fixedSize,
                configuration,
                _loggingService,
                _profilerService,
                _alertService,
                _messageBusService);
        }

        /// <summary>
        /// Creates a new FixedSizeStrategy instance with default configuration.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A configured FixedSizeStrategy instance with default settings.</returns>
        public FixedSizeStrategy CreateDefault(int fixedSize)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));

            _loggingService.LogInfo($"Creating default FixedSizeStrategy with size {fixedSize}");

            var defaultConfig = _configBuilder
                .MemoryOptimized()
                .WithName($"FixedSize-{fixedSize}")
                .WithCircuitBreaker(true, 3, TimeSpan.FromSeconds(45))
                .WithMetrics(true, 500)
                .Build();

            return Create(fixedSize, defaultConfig);
        }

        /// <summary>
        /// Creates a new FixedSizeStrategy optimized for mobile devices.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A mobile-optimized FixedSizeStrategy instance.</returns>
        public FixedSizeStrategy CreateForMobile(int fixedSize)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));

            _loggingService.LogInfo($"Creating mobile-optimized FixedSizeStrategy with size {fixedSize}");

            var mobileConfig = _configBuilder
                .MemoryOptimized()
                .WithName($"FixedSizeMobile-{fixedSize}")
                .WithPerformanceBudget(PerformanceBudget.For30FPS())
                .WithTags("mobile", "memory-constrained")
                .WithCircuitBreaker(true, 3, TimeSpan.FromMinutes(1))
                .Build();

            return Create(fixedSize, mobileConfig);
        }

        /// <summary>
        /// Creates a new FixedSizeStrategy optimized for high-performance scenarios.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A high-performance FixedSizeStrategy instance.</returns>
        public FixedSizeStrategy CreateForHighPerformance(int fixedSize)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));

            _loggingService.LogInfo($"Creating high-performance FixedSizeStrategy with size {fixedSize}");

            var highPerfConfig = _configBuilder
                .HighPerformance()
                .WithName($"FixedSizeHighPerf-{fixedSize}")
                .WithPerformanceBudget(PerformanceBudget.For60FPS())
                .WithTags("high-performance", "predictable")
                .WithCircuitBreaker(true, 3, TimeSpan.FromSeconds(15))
                .WithMetrics(false, 50) // Reduced overhead for high performance
                .Build();

            return Create(fixedSize, highPerfConfig);
        }

        /// <summary>
        /// Creates a new FixedSizeStrategy optimized for development and testing.
        /// </summary>
        /// <param name="fixedSize">The fixed size to maintain for the pool.</param>
        /// <returns>A development-optimized FixedSizeStrategy instance.</returns>
        public FixedSizeStrategy CreateForDevelopment(int fixedSize)
        {
            if (fixedSize <= 0)
                throw new ArgumentException("Fixed size must be greater than zero", nameof(fixedSize));

            _loggingService.LogInfo($"Creating development-optimized FixedSizeStrategy with size {fixedSize}");

            var devConfig = _configBuilder
                .Development()
                .WithName($"FixedSizeDev-{fixedSize}")
                .WithTags("development", "testing")
                .WithCircuitBreaker(false, 10, TimeSpan.FromMinutes(1)) // Disabled for easier debugging
                .WithMetrics(true, 500)
                .WithDebugLogging(true)
                .Build();

            return Create(fixedSize, devConfig);
        }
    }
}