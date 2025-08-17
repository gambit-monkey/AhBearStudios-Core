using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Defines the contract for managing alert channel lifecycle and orchestration.
    /// Handles channel registration, health monitoring, configuration, and alert delivery routing.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public interface IAlertChannelService : IDisposable
    {
        /// <summary>
        /// Gets whether the channel service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the count of registered channels.
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        /// Gets the count of healthy channels.
        /// </summary>
        int HealthyChannelCount { get; }

        /// <summary>
        /// Registers an alert channel with the service.
        /// </summary>
        /// <param name="channel">Channel to register</param>
        /// <param name="config">Optional initial configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with registration result</returns>
        UniTask<bool> RegisterChannelAsync(IAlertChannel channel, ChannelConfig config = null, Guid correlationId = default);

        /// <summary>
        /// Unregisters an alert channel from the service.
        /// </summary>
        /// <param name="channelName">Name of channel to unregister</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with unregistration result</returns>
        UniTask<bool> UnregisterChannelAsync(FixedString64Bytes channelName, Guid correlationId = default);

        /// <summary>
        /// Gets a registered channel by name.
        /// </summary>
        /// <param name="channelName">Name of channel to retrieve</param>
        /// <returns>Channel instance or null if not found</returns>
        IAlertChannel GetChannel(FixedString64Bytes channelName);

        /// <summary>
        /// Gets all registered channels.
        /// </summary>
        /// <returns>Collection of registered channels</returns>
        IReadOnlyCollection<IAlertChannel> GetAllChannels();

        /// <summary>
        /// Gets only healthy channels.
        /// </summary>
        /// <returns>Collection of healthy channels</returns>
        IReadOnlyCollection<IAlertChannel> GetHealthyChannels();

        /// <summary>
        /// Delivers an alert to all appropriate channels based on configuration.
        /// </summary>
        /// <param name="alert">Alert to deliver</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with delivery results</returns>
        UniTask<AlertDeliveryResults> DeliverAlertAsync(Alert alert, Guid correlationId = default);

        /// <summary>
        /// Delivers an alert to a specific channel.
        /// </summary>
        /// <param name="channelName">Name of target channel</param>
        /// <param name="alert">Alert to deliver</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with delivery result</returns>
        UniTask<bool> DeliverAlertToChannelAsync(FixedString64Bytes channelName, Alert alert, Guid correlationId = default);

        /// <summary>
        /// Performs health checks on all registered channels.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask representing the health check operation</returns>
        UniTask PerformHealthChecksAsync(Guid correlationId = default);

        /// <summary>
        /// Gets health information for all channels.
        /// </summary>
        /// <returns>Collection of channel health information</returns>
        IReadOnlyCollection<ChannelHealthInfo> GetChannelHealthInfo();

        /// <summary>
        /// Gets health information for a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel</param>
        /// <returns>Channel health information or null if not found</returns>
        ChannelHealthInfo GetChannelHealth(FixedString64Bytes channelName);

        /// <summary>
        /// Updates configuration for a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel to configure</param>
        /// <param name="config">New configuration</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>UniTask with configuration result</returns>
        UniTask<bool> ConfigureChannelAsync(FixedString64Bytes channelName, ChannelConfig config, Guid correlationId = default);

        /// <summary>
        /// Enables a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel to enable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if channel was enabled</returns>
        bool EnableChannel(FixedString64Bytes channelName, Guid correlationId = default);

        /// <summary>
        /// Disables a specific channel.
        /// </summary>
        /// <param name="channelName">Name of channel to disable</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>True if channel was disabled</returns>
        bool DisableChannel(FixedString64Bytes channelName, Guid correlationId = default);

        /// <summary>
        /// Gets comprehensive metrics for all channels.
        /// </summary>
        /// <returns>Channel service metrics</returns>
        ChannelServiceMetrics GetMetrics();

        /// <summary>
        /// Resets metrics for all channels.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking</param>
        void ResetMetrics(Guid correlationId = default);
    }

}