using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Health information for an alert channel.
    /// Tracks channel health status, failures, and diagnostic messages.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public sealed record ChannelHealthInfo
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets whether the channel is healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets the last health check timestamp.
        /// </summary>
        public DateTime LastHealthCheck { get; init; }

        /// <summary>
        /// Gets the last health status message.
        /// </summary>
        public FixedString512Bytes LastHealthMessage { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Creates a healthy channel info instance.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="message">Health message</param>
        /// <returns>Healthy channel info</returns>
        public static ChannelHealthInfo Healthy(FixedString64Bytes channelName, string message = "Channel is operational")
        {
            return new ChannelHealthInfo
            {
                ChannelName = channelName,
                IsHealthy = true,
                LastHealthCheck = DateTime.UtcNow,
                LastHealthMessage = message,
                ConsecutiveFailures = 0
            };
        }

        /// <summary>
        /// Creates an unhealthy channel info instance.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="message">Error message</param>
        /// <param name="consecutiveFailures">Number of consecutive failures</param>
        /// <returns>Unhealthy channel info</returns>
        public static ChannelHealthInfo Unhealthy(FixedString64Bytes channelName, string message, int consecutiveFailures = 1)
        {
            return new ChannelHealthInfo
            {
                ChannelName = channelName,
                IsHealthy = false,
                LastHealthCheck = DateTime.UtcNow,
                LastHealthMessage = message,
                ConsecutiveFailures = consecutiveFailures
            };
        }
    }
}