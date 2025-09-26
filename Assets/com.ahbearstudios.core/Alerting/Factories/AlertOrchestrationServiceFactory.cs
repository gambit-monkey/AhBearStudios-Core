using System;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory responsible for creating AlertOrchestrationService instances.
    /// Handles dependency injection and configuration validation for orchestration services.
    /// </summary>
    public sealed class AlertOrchestrationServiceFactory
    {
        #region Dependencies

        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IProfilerService _profilerService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertOrchestrationServiceFactory.
        /// </summary>
        /// <param name="loggingService">Service for logging operations</param>
        /// <param name="messageBusService">Service for message publishing</param>
        /// <param name="profilerService">Service for performance profiling</param>
        public AlertOrchestrationServiceFactory(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _profilerService = profilerService;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new AlertOrchestrationService instance with the provided services and configuration.
        /// </summary>
        /// <param name="stateManagementService">State management service instance</param>
        /// <param name="healthMonitoringService">Health monitoring service instance</param>
        /// <param name="config">Configuration for the orchestration service</param>
        /// <returns>Configured AlertOrchestrationService instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public async UniTask<IAlertOrchestrationService> CreateAlertOrchestrationServiceAsync(
            IAlertStateManagementService stateManagementService,
            IAlertHealthMonitoringService healthMonitoringService,
            AlertConfig config)
        {
            if (stateManagementService == null)
                throw new ArgumentNullException(nameof(stateManagementService));
            if (healthMonitoringService == null)
                throw new ArgumentNullException(nameof(healthMonitoringService));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ValidateConfig(config);

            try
            {
                var service = new AlertOrchestrationService(
                    stateManagementService,
                    healthMonitoringService,
                    _loggingService,
                    _messageBusService,
                    _profilerService);

                _loggingService.LogInfo("AlertOrchestrationService created successfully");
                return service;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    ex,
                    "Failed to create AlertOrchestrationService: {ErrorMessage}",
                    ex.Message);
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the provided configuration for orchestration service creation.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        private void ValidateConfig(AlertConfig config)
        {
            if (config.MaxConcurrentAlerts <= 0)
            {
                throw new InvalidOperationException($"MaxConcurrentAlerts must be greater than 0, got: {config.MaxConcurrentAlerts}");
            }

            if (config.ProcessingTimeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"ProcessingTimeout must be positive, got: {config.ProcessingTimeout}");
            }
        }

        #endregion
    }
}