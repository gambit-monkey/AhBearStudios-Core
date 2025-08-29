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
    /// Factory interface for creating PoolErrorRecoveryService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// </summary>
    public interface IPoolErrorRecoveryServiceFactory
    {
        /// <summary>
        /// Creates a PoolErrorRecoveryService instance using the provided configuration.
        /// </summary>
        /// <param name="config">Configuration for the error recovery service</param>
        /// <returns>Configured error recovery service instance</returns>
        IPoolErrorRecoveryService CreateErrorRecoveryService(PoolErrorRecoveryConfiguration config);

        /// <summary>
        /// Creates a PoolErrorRecoveryService instance asynchronously.
        /// </summary>
        /// <param name="config">Configuration for the error recovery service</param>
        /// <returns>Task containing the configured error recovery service instance</returns>
        UniTask<IPoolErrorRecoveryService> CreateErrorRecoveryServiceAsync(PoolErrorRecoveryConfiguration config);
    }

    /// <summary>
    /// Production-ready factory for creating PoolErrorRecoveryService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// </summary>
    public sealed class PoolErrorRecoveryServiceFactory : IPoolErrorRecoveryServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;

        /// <summary>
        /// Initializes a new instance of the PoolErrorRecoveryServiceFactory.
        /// </summary>
        public PoolErrorRecoveryServiceFactory(
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
        public IPoolErrorRecoveryService CreateErrorRecoveryService(PoolErrorRecoveryConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new PoolErrorRecoveryService(
                _loggingService,
                _messageBusService,
                _alertService,
                _profilerService);
        }

        /// <inheritdoc />
        public async UniTask<IPoolErrorRecoveryService> CreateErrorRecoveryServiceAsync(PoolErrorRecoveryConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // For now, delegate to synchronous method
            await UniTask.Yield();
            return CreateErrorRecoveryService(config);
        }
    }
}