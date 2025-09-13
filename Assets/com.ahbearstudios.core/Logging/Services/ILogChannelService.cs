using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service interface for managing log channels in the logging system.
    /// Handles channel registration, routing, filtering, and lifecycle management.
    /// Follows the AhBearStudios Core Architecture patterns for service decomposition.
    /// </summary>
    public interface ILogChannelService : IDisposable
    {
        #region Channel Registration and Management

        /// <summary>
        /// Registers a log channel with the channel service.
        /// </summary>
        /// <param name="channel">The log channel to register</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <exception cref="ArgumentNullException">Thrown when channel is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown when service is disposed</exception>
        void RegisterChannel(ILogChannel channel, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Unregisters a log channel by name and disposes it properly.
        /// </summary>
        /// <param name="channelName">The name of the channel to unregister</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the channel was found and unregistered successfully</returns>
        bool UnregisterChannel(string channelName, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Gets a registered channel by name.
        /// </summary>
        /// <param name="channelName">The name of the channel to retrieve</param>
        /// <returns>The channel instance, or null if not found</returns>
        ILogChannel GetChannel(string channelName);

        /// <summary>
        /// Checks if a channel with the specified name is registered.
        /// </summary>
        /// <param name="channelName">The name of the channel to check</param>
        /// <returns>True if the channel is registered</returns>
        bool HasChannel(string channelName);

        /// <summary>
        /// Gets all registered channels as a read-only collection.
        /// </summary>
        /// <returns>Read-only collection of all registered channels</returns>
        IReadOnlyCollection<ILogChannel> GetChannels();

        #endregion

        #region Channel Routing and Filtering

        /// <summary>
        /// Determines the appropriate channel for a log message based on routing rules.
        /// </summary>
        /// <param name="logMessage">The log message to route</param>
        /// <returns>The channel to use, or null if no specific channel is required</returns>
        ILogChannel RouteMessage(LogMessage logMessage);

        /// <summary>
        /// Gets the default channel for messages that don't match specific routing rules.
        /// </summary>
        /// <returns>The default channel, or null if none is configured</returns>
        ILogChannel GetDefaultChannel();

        /// <summary>
        /// Sets the default channel for messages that don't match specific routing rules.
        /// </summary>
        /// <param name="channelName">The name of the channel to set as default</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the channel was found and set as default</returns>
        bool SetDefaultChannel(string channelName, FixedString64Bytes correlationId = default);

        #endregion

        #region Channel Configuration

        /// <summary>
        /// Sets the enabled state for all registered channels.
        /// </summary>
        /// <param name="enabled">Whether channels should be enabled</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        void SetEnabled(bool enabled, FixedString64Bytes correlationId = default);

        /// <summary>
        /// Sets the enabled state for a specific channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to configure</param>
        /// <param name="enabled">Whether the channel should be enabled</param>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>True if the channel was found and configured successfully</returns>
        bool SetEnabled(string channelName, bool enabled, FixedString64Bytes correlationId = default);

        #endregion

        #region Health Monitoring

        /// <summary>
        /// Performs health checks on all registered channels.
        /// </summary>
        /// <returns>True if all channels are healthy</returns>
        bool PerformHealthCheck();

        /// <summary>
        /// Gets the health status of all registered channels.
        /// </summary>
        /// <returns>Dictionary mapping channel names to their health status</returns>
        IReadOnlyDictionary<string, bool> GetHealthStatus();

        /// <summary>
        /// Validates the current configuration of all channels.
        /// </summary>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Validation result with any errors or warnings</returns>
        ValidationResult ValidateConfiguration(FixedString64Bytes correlationId = default);

        #endregion
    }
}