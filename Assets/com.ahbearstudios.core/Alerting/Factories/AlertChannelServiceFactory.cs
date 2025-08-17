using System;
using AhBearStudios.Core.Alerting.Builders;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Factory for creating alert channel service instances.
    /// Simple creation only - no lifecycle management per CLAUDE.md guidelines.
    /// Takes validated configurations from builders and creates service instances.
    /// </summary>
    public sealed class AlertChannelServiceFactory : IAlertChannelServiceFactory
    {
        private readonly ILoggingService _loggingService;
        private readonly IMessageBusService _messageBus;

        /// <summary>
        /// Initializes a new instance of the AlertChannelServiceFactory class.
        /// </summary>
        /// <param name="loggingService">Logging service for internal logging</param>
        /// <param name="messageBus">Message bus for event distribution</param>
        public AlertChannelServiceFactory(
            ILoggingService loggingService,
            IMessageBusService messageBus)
        {
            _loggingService = loggingService;
            _messageBus = messageBus;
        }

        /// <summary>
        /// Creates a new alert channel service instance with the specified configuration.
        /// </summary>
        /// <param name="config">Validated configuration from builder</param>
        /// <returns>UniTask with created service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        public async UniTask<IAlertChannelService> CreateAlertChannelServiceAsync(AlertChannelServiceConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.Validate();

            var service = new AlertChannelService(config, _loggingService, _messageBus);
            
            await service.InitializeAsync();
            
            return service;
        }

        /// <summary>
        /// Creates a new alert channel service instance with default configuration.
        /// </summary>
        /// <returns>UniTask with created service instance</returns>
        public async UniTask<IAlertChannelService> CreateDefaultAlertChannelServiceAsync()
        {
            return await CreateAlertChannelServiceAsync(AlertChannelServiceConfig.Default);
        }

        /// <summary>
        /// Creates a new alert channel service instance using a builder.
        /// </summary>
        /// <param name="builderAction">Action to configure the builder</param>
        /// <returns>UniTask with created service instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when builderAction is null</exception>
        public async UniTask<IAlertChannelService> CreateAlertChannelServiceAsync(Action<AlertChannelServiceBuilder> builderAction)
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));

            var builder = new AlertChannelServiceBuilder();
            builderAction(builder);
            var config = builder.Build();
            
            return await CreateAlertChannelServiceAsync(config);
        }
    }

}