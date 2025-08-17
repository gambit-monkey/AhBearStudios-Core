using System;
using AhBearStudios.Core.Alerting.Builders;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Services;

namespace AhBearStudios.Core.Alerting.Factories
{
    /// <summary>
    /// Interface for alert channel service factory.
    /// Defines the contract for creating alert channel service instances.
    /// Simple creation only - no lifecycle management per CLAUDE.md guidelines.
    /// </summary>
    public interface IAlertChannelServiceFactory
    {
        /// <summary>
        /// Creates a new alert channel service instance with the specified configuration.
        /// </summary>
        /// <param name="config">Validated configuration from builder</param>
        /// <returns>UniTask with created service instance</returns>
        UniTask<IAlertChannelService> CreateAlertChannelServiceAsync(AlertChannelServiceConfig config);

        /// <summary>
        /// Creates a new alert channel service instance with default configuration.
        /// </summary>
        /// <returns>UniTask with created service instance</returns>
        UniTask<IAlertChannelService> CreateDefaultAlertChannelServiceAsync();

        /// <summary>
        /// Creates a new alert channel service instance using a builder.
        /// </summary>
        /// <param name="builderAction">Action to configure the builder</param>
        /// <returns>UniTask with created service instance</returns>
        UniTask<IAlertChannelService> CreateAlertChannelServiceAsync(Action<AlertChannelServiceBuilder> builderAction);
    }
}