using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Performance statistics for individual alert channels.
    /// </summary>
    public readonly partial record struct ChannelPerformanceStats
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets whether the channel is currently healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets the total alerts sent through this channel.
        /// </summary>
        public long TotalAlertsSent { get; init; }

        /// <summary>
        /// Gets the number of successful deliveries.
        /// </summary>
        public long SuccessfulDeliveries { get; init; }

        /// <summary>
        /// Gets the number of failed deliveries.
        /// </summary>
        public long FailedDeliveries { get; init; }

        /// <summary>
        /// Gets the average delivery time in milliseconds.
        /// </summary>
        public double AverageDeliveryTimeMs { get; init; }

        /// <summary>
        /// Gets the last delivery timestamp.
        /// </summary>
        public DateTime? LastDelivery { get; init; }

        /// <summary>
        /// Gets the success rate as percentage (0-100).
        /// </summary>
        public double SuccessRate => TotalAlertsSent > 0 
            ? (double)SuccessfulDeliveries / TotalAlertsSent * 100 
            : 0;
    }
}