using System;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Services;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Factory interface for creating PoolAutoScalingService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// </summary>
    public interface IPoolAutoScalingServiceFactory
    {
        /// <summary>
        /// Creates a PoolAutoScalingService instance using the provided configuration.
        /// </summary>
        /// <param name="config">Configuration for the auto-scaling service</param>
        /// <returns>Configured auto-scaling service instance</returns>
        IPoolAutoScalingService CreateAutoScalingService(PoolAutoScalingConfiguration config);

        /// <summary>
        /// Creates a PoolAutoScalingService instance asynchronously.
        /// </summary>
        /// <param name="config">Configuration for the auto-scaling service</param>
        /// <returns>Task containing the configured auto-scaling service instance</returns>
        UniTask<IPoolAutoScalingService> CreateAutoScalingServiceAsync(PoolAutoScalingConfiguration config);
    }

    /// <summary>
    /// Production-ready factory for creating PoolAutoScalingService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// </summary>
    public sealed class PoolAutoScalingServiceFactory : IPoolAutoScalingServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;

        /// <summary>
        /// Initializes a new instance of the PoolAutoScalingServiceFactory.
        /// </summary>
        public PoolAutoScalingServiceFactory(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IAlertService alertService = null,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _alertService = alertService;
            _profilerService = profilerService;
        }

        /// <inheritdoc />
        public IPoolAutoScalingService CreateAutoScalingService(PoolAutoScalingConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new PoolAutoScalingService(
                _loggingService,
                _messageBusService,
                _alertService,
                _profilerService);
        }

        /// <inheritdoc />
        public async UniTask<IPoolAutoScalingService> CreateAutoScalingServiceAsync(PoolAutoScalingConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // For now, delegate to synchronous method
            await UniTask.Yield();
            return CreateAutoScalingService(config);
        }
    }
}