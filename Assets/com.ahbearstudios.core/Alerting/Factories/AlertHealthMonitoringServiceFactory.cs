using System;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.HealthCheck;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Profiling;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory responsible for creating AlertHealthMonitoringService instances.
    /// Handles dependency injection and configuration validation for health monitoring services.
    /// </summary>
    public sealed class AlertHealthMonitoringServiceFactory
    {
        #region Dependencies

        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBusService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IProfilerService _profilerService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AlertHealthMonitoringServiceFactory.
        /// </summary>
        /// <param name="loggingService">Service for logging operations</param>
        /// <param name="messageBusService">Service for message publishing</param>
        /// <param name="healthCheckService">Service for health check operations</param>
        /// <param name="profilerService">Service for performance profiling</param>
        public AlertHealthMonitoringServiceFactory(
            ILoggingService loggingService,
            IMessageBusService messageBusService,
            IHealthCheckService healthCheckService,
            IProfilerService profilerService = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _profilerService = profilerService;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new AlertHealthMonitoringService instance with the provided configuration.
        /// </summary>
        /// <param name="config">Configuration for the health monitoring service</param>
        /// <returns>Configured AlertHealthMonitoringService instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public async UniTask<IAlertHealthMonitoringService> CreateAlertHealthMonitoringServiceAsync(AlertConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ValidateConfig(config);

            try
            {
                var service = new AlertHealthMonitoringService(
                    _loggingService,
                    _messageBusService,
                    _healthCheckService,
                    _profilerService);

                await service.InitializeAsync(config);

                _loggingService.LogInfo("AlertHealthMonitoringService created successfully");
                return service;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    ex,
                    "Failed to create AlertHealthMonitoringService: {ErrorMessage}",
                    ex.Message);
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the provided configuration for health monitoring service creation.
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        private void ValidateConfig(AlertConfig config)
        {
            if (config.HealthCheckInterval <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"HealthCheckInterval must be positive, got: {config.HealthCheckInterval}");
            }

            if (config.HealthCheckTimeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"HealthCheckTimeout must be positive, got: {config.HealthCheckTimeout}");
            }

            if (config.CircuitBreakerFailureThreshold <= 0)
            {
                throw new InvalidOperationException($"CircuitBreakerFailureThreshold must be greater than 0, got: {config.CircuitBreakerFailureThreshold}");
            }

            if (config.CircuitBreakerRecoveryTimeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"CircuitBreakerRecoveryTimeout must be positive, got: {config.CircuitBreakerRecoveryTimeout}");
            }

            if (config.EmergencyModeThreshold <= 0)
            {
                throw new InvalidOperationException($"EmergencyModeThreshold must be greater than 0, got: {config.EmergencyModeThreshold}");
            }

            if (config.StatisticsUpdateInterval <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"StatisticsUpdateInterval must be positive, got: {config.StatisticsUpdateInterval}");
            }
        }

        #endregion
    }
}