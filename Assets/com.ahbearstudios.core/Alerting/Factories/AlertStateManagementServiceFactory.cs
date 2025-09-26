using System;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory responsible for creating AlertStateManagementService instances.
    /// Handles dependency injection and configuration validation for state management services.
    /// </summary>
    public sealed class AlertStateManagementServiceFactory
    {
        #region Dependencies

        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IPoolingService _poolingService;
        private readonly ISerializationService _serializationService;
        private readonly IProfilerService _profilerService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertStateManagementServiceFactory.
        /// </summary>
        /// <param name="loggingService">Service for logging operations</param>
        /// <param name="messageBusService">Service for message publishing</param>
        /// <param name="poolingService">Service for object pooling</param>
        /// <param name="serializationService">Service for data serialization</param>
        /// <param name="profilerService">Service for performance profiling</param>
        public AlertStateManagementServiceFactory(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IPoolingService poolingService,
            ISerializationService serializationService,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            _profilerService = profilerService;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new AlertStateManagementService instance with the provided configuration.
        /// </summary>
        /// <param name="config">Configuration for the state management service</param>
        /// <returns>Configured AlertStateManagementService instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public async UniTask<IAlertStateManagementService> CreateAlertStateManagementServiceAsync(AlertConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ValidateConfig(config);

            try
            {
                var service = new AlertStateManagementService(
                    _loggingService,
                    _messageBusService,
                    _poolingService,
                    _serializationService,
                    _profilerService);

                await service.InitializeAsync(config);

                _loggingService.LogInfo("AlertStateManagementService created successfully");
                return service;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    ex,
                    "Failed to create AlertStateManagementService: {ErrorMessage}",
                    ex.Message);
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the provided configuration for state management service creation.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        private void ValidateConfig(AlertConfig config)
        {
            if (config.MaxActiveAlerts <= 0)
            {
                throw new InvalidOperationException($"MaxActiveAlerts must be greater than 0, got: {config.MaxActiveAlerts}");
            }

            if (config.HistoryRetentionHours <= 0)
            {
                throw new InvalidOperationException($"HistoryRetentionHours must be greater than 0, got: {config.HistoryRetentionHours}");
            }

            if (config.MaxHistoryEntries <= 0)
            {
                throw new InvalidOperationException($"MaxHistoryEntries must be greater than 0, got: {config.MaxHistoryEntries}");
            }

            if (config.StatisticsUpdateInterval <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"StatisticsUpdateInterval must be positive, got: {config.StatisticsUpdateInterval}");
            }
        }

        #endregion
    }
}