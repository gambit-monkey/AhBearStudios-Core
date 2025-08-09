using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Specialized builder implementation for configuring alert channels with fluent syntax.
    /// Provides advanced channel configuration capabilities and validation.
    /// </summary>
    internal sealed class ChannelConfigBuilder : IChannelConfigBuilder
    {
        private readonly List<ChannelConfig> _channels;

        /// <summary>
        /// Initializes a new instance of the ChannelConfigBuilder.
        /// </summary>
        /// <param name="channels">The channel list to modify</param>
        public ChannelConfigBuilder(List<ChannelConfig> channels)
        {
            _channels = channels ?? throw new ArgumentNullException(nameof(channels));
        }

        /// <summary>
        /// Adds a channel configuration with fluent syntax.
        /// </summary>
        public IChannelConfigBuilder AddChannel<TChannel>(Action<TChannel> configAction) where TChannel : ChannelConfig, new()
        {
            if (configAction == null)
                throw new ArgumentNullException(nameof(configAction));

            var channel = new TChannel();
            configAction(channel);
            channel.Validate();

            // Remove existing channel with the same name
            _channels.RemoveAll(c => c.Name.Equals(channel.Name));
            _channels.Add(channel);
            return this;
        }

        /// <summary>
        /// Adds a channel configuration with fluent syntax.
        /// </summary>
        public IChannelConfigBuilder AddChannel(ChannelConfig channelConfig)
        {
            if (channelConfig == null)
                throw new ArgumentNullException(nameof(channelConfig));

            channelConfig.Validate();

            // Remove existing channel with the same name
            _channels.RemoveAll(c => c.Name.Equals(channelConfig.Name));
            _channels.Add(channelConfig);
            return this;
        }

        /// <summary>
        /// Adds a channel configuration with fluent syntax.
        /// </summary>
        public IChannelConfigBuilder RemoveChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name cannot be null or whitespace.", nameof(channelName));

            _channels.RemoveAll(c => c.Name.ToString().Equals(channelName, StringComparison.OrdinalIgnoreCase));
            return this;
        }

        /// <summary>
        /// Adds a channel configuration with fluent syntax.
        /// </summary>
        public IChannelConfigBuilder ClearChannels()
        {
            _channels.Clear();
            return this;
        }
    }
}