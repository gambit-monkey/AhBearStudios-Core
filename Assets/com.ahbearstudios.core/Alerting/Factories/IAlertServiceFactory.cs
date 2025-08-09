using System;
using System.Collections.Generic;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory interface for creating and configuring AlertService instances.
    /// Provides abstraction for dependency injection and testing scenarios.
    /// Designed for Unity game development with support for different deployment configurations.
    /// </summary>
    public interface IAlertServiceFactory
    {
        /// <summary>
        /// Creates a new AlertService instance with default configuration.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing events</param>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <returns>Configured AlertService instance</returns>
        IAlertService CreateAlertService(IMessageBusService messageBusService = null, ILoggingService loggingService = null);

        /// <summary>
        /// Creates a new AlertService instance with specific configuration.
        /// </summary>
        /// <param name="configuration">Alert service configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with configured AlertService instance</returns>
        UniTask<IAlertService> CreateAlertServiceAsync(AlertServiceConfiguration configuration, Guid correlationId = default);

        /// <summary>
        /// Creates a pre-configured AlertService for development environments.
        /// Includes console and logging channels with debug-level filtering.
        /// </summary>
        /// <param name="loggingService">Logging service</param>
        /// <param name="messageBusService">Message bus service</param>
        /// <returns>Development-configured AlertService</returns>
        UniTask<IAlertService> CreateDevelopmentAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService = null);

        /// <summary>
        /// Creates a pre-configured AlertService for production environments.
        /// Includes logging channel with warning-level filtering and suppression.
        /// </summary>
        /// <param name="loggingService">Logging service</param>
        /// <param name="messageBusService">Message bus service</param>
        /// <returns>Production-configured AlertService</returns>
        UniTask<IAlertService> CreateProductionAlertServiceAsync(ILoggingService loggingService, IMessageBusService messageBusService);

        /// <summary>
        /// Creates a minimal AlertService for testing scenarios.
        /// Uses in-memory channels and minimal filtering.
        /// </summary>
        /// <returns>Test-configured AlertService</returns>
        IAlertService CreateTestAlertService();

        /// <summary>
        /// Creates an AlertService with custom channels and filters.
        /// </summary>
        /// <param name="channels">Collection of channels to register</param>
        /// <param name="filters">Collection of filters to register</param>
        /// <param name="suppressionRules">Collection of suppression rules</param>
        /// <param name="messageBusService">Message bus service</param>
        /// <param name="loggingService">Logging service</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with custom-configured AlertService</returns>
        UniTask<IAlertService> CreateCustomAlertServiceAsync(
            IEnumerable<IAlertChannel> channels,
            IEnumerable<IAlertFilter> filters,
            IEnumerable<AlertRule> suppressionRules = null,
            IMessageBusService messageBusService = null,
            ILoggingService loggingService = null,
            Guid correlationId = default);

        /// <summary>
        /// Validates an alert service configuration before creation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateConfiguration(AlertServiceConfiguration configuration);

        /// <summary>
        /// Gets the default configuration for alert services.
        /// </summary>
        /// <returns>Default alert service configuration</returns>
        AlertServiceConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Gets configuration optimized for development environments.
        /// </summary>
        /// <returns>Development alert service configuration</returns>
        AlertServiceConfiguration GetDevelopmentConfiguration();

        /// <summary>
        /// Gets configuration optimized for production environments.
        /// </summary>
        /// <returns>Production alert service configuration</returns>
        AlertServiceConfiguration GetProductionConfiguration();

        /// <summary>
        /// Creates configuration from a dictionary of settings.
        /// </summary>
        /// <param name="settings">Configuration settings</param>
        /// <returns>Alert service configuration</returns>
        AlertServiceConfiguration CreateConfigurationFromSettings(Dictionary<string, object> settings);
    }
}