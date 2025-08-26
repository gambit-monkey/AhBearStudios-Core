using System;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Strategies;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Simple fallback strategy selector that always returns DefaultPoolingStrategy.
    /// Used when the full strategy selector cannot be created due to missing dependencies.
    /// </summary>
    internal class SimpleStrategySelector : IPoolingStrategySelector
    {
        private readonly ILoggingService _loggingService;
        private readonly IProfilerService _profilerService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBusService;

        /// <summary>
        /// Initializes a new instance of the SimpleStrategySelector.
        /// </summary>
        /// <param name="loggingService">Logging service for system integration</param>
        /// <param name="profilerService">Profiler service for performance monitoring</param>
        /// <param name="alertService">Alert service for critical notifications</param>
        /// <param name="messageBusService">Message bus service for event publishing</param>
        public SimpleStrategySelector(
            ILoggingService loggingService,
            IProfilerService profilerService,
            IAlertService alertService,
            IMessageBusService messageBusService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

            _loggingService.LogWarning("Using SimpleStrategySelector fallback - advanced strategies not available");
        }

        /// <inheritdoc />
        public IPoolingStrategy SelectStrategy(PoolConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _loggingService.LogInfo($"SimpleStrategySelector: Using DefaultPoolingStrategy for pool '{configuration.Name}'");
            return GetDefaultStrategy();
        }

        /// <inheritdoc />
        public bool CanCreateStrategy(PoolConfiguration configuration)
        {
            return configuration != null;
        }

        /// <inheritdoc />
        public IPoolingStrategy GetDefaultStrategy()
        {
            return new DefaultPoolingStrategy(_loggingService, _profilerService, _alertService, _messageBusService);
        }
    }
}