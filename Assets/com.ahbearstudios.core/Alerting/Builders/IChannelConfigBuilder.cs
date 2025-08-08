using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Builders;

/// <summary>
/// Specialized builder interface for configuring alert channels with fluent syntax.
/// Provides advanced channel configuration capabilities beyond basic channel addition.
/// </summary>
public interface IChannelConfigBuilder
{
    /// <summary>
    /// Adds a channel with advanced configuration options.
    /// </summary>
    /// <typeparam name="TChannel">The channel type to add</typeparam>
    /// <param name="configAction">Action to configure the channel</param>
    /// <returns>The channel builder instance for method chaining</returns>
    IChannelConfigBuilder AddChannel<TChannel>(Action<TChannel> configAction) where TChannel : ChannelConfig, new();

    /// <summary>
    /// Adds a channel with fluent configuration.
    /// </summary>
    /// <param name="channelConfig">The channel configuration</param>
    /// <returns>The channel builder instance for method chaining</returns>
    IChannelConfigBuilder AddChannel(ChannelConfig channelConfig);

    /// <summary>
    /// Removes a channel by name.
    /// </summary>
    /// <param name="channelName">The name of the channel to remove</param>
    /// <returns>The channel builder instance for method chaining</returns>
    IChannelConfigBuilder RemoveChannel(string channelName);

    /// <summary>
    /// Clears all configured channels.
    /// </summary>
    /// <returns>The channel builder instance for method chaining</returns>
    IChannelConfigBuilder ClearChannels();
}