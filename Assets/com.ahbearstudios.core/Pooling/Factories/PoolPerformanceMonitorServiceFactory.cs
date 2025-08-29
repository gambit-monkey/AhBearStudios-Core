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
    /// Factory interface for creating PoolPerformanceMonitorService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// </summary>
    public interface IPoolPerformanceMonitorServiceFactory
    {
        /// <summary>
        /// Creates a PoolPerformanceMonitorService instance using the provided configuration.
        /// </summary>
        /// <param name="config">Configuration for the performance monitor service</param>
        /// <returns>Configured performance monitor service instance</returns>
        IPoolPerformanceMonitorService CreatePerformanceMonitorService(PoolPerformanceMonitorConfiguration config);

        /// <summary>
        /// Creates a PoolPerformanceMonitorService instance asynchronously.
        /// </summary>
        /// <param name="config">Configuration for the performance monitor service</param>
        /// <returns>Task containing the configured performance monitor service instance</returns>
        UniTask<IPoolPerformanceMonitorService> CreatePerformanceMonitorServiceAsync(PoolPerformanceMonitorConfiguration config);
    }

    /// <summary>
    /// Production-ready factory for creating PoolPerformanceMonitorService instances.
    /// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
    /// </summary>
    public sealed class PoolPerformanceMonitorServiceFactory : IPoolPerformanceMonitorServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;

        /// <summary>
        /// Initializes a new instance of the PoolPerformanceMonitorServiceFactory.
        /// </summary>
        public PoolPerformanceMonitorServiceFactory(
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
        public IPoolPerformanceMonitorService CreatePerformanceMonitorService(PoolPerformanceMonitorConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new PoolPerformanceMonitorService(
                _loggingService,
                _alertService,
                _profilerService);
        }

        /// <inheritdoc />
        public async UniTask<IPoolPerformanceMonitorService> CreatePerformanceMonitorServiceAsync(PoolPerformanceMonitorConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // For now, delegate to synchronous method
            await UniTask.Yield();
            return CreatePerformanceMonitorService(config);
        }
    }
}