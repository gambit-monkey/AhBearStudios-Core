using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Results of an alert delivery operation across multiple channels.
    /// Aggregates success/failure statistics and individual channel results.
    /// Designed for Unity game development with performance monitoring.
    /// </summary>
    public sealed record AlertDeliveryResults
    {
        /// <summary>
        /// Gets the total number of channels targeted.
        /// </summary>
        public int TotalChannels { get; init; }

        /// <summary>
        /// Gets the number of successful deliveries.
        /// </summary>
        public int SuccessfulDeliveries { get; init; }

        /// <summary>
        /// Gets the number of failed deliveries.
        /// </summary>
        public int FailedDeliveries { get; init; }

        /// <summary>
        /// Gets the total delivery time across all channels.
        /// </summary>
        public TimeSpan TotalDeliveryTime { get; init; }

        /// <summary>
        /// Gets detailed results for each channel.
        /// </summary>
        public IReadOnlyList<ChannelDeliveryResult> ChannelResults { get; init; } = Array.Empty<ChannelDeliveryResult>();

        /// <summary>
        /// Gets the success rate as a percentage (0-100).
        /// </summary>
        public double SuccessRate => TotalChannels > 0 
            ? (double)SuccessfulDeliveries / TotalChannels * 100 
            : 0;

        /// <summary>
        /// Gets the failure rate as a percentage (0-100).
        /// </summary>
        public double FailureRate => TotalChannels > 0 
            ? (double)FailedDeliveries / TotalChannels * 100 
            : 0;

        /// <summary>
        /// Gets the average delivery time per channel.
        /// </summary>
        public TimeSpan AverageDeliveryTime => TotalChannels > 0 
            ? TimeSpan.FromMilliseconds(TotalDeliveryTime.TotalMilliseconds / TotalChannels)
            : TimeSpan.Zero;

        /// <summary>
        /// Gets whether all deliveries were successful.
        /// </summary>
        public bool AllSuccessful => TotalChannels > 0 && SuccessfulDeliveries == TotalChannels;

        /// <summary>
        /// Gets whether all deliveries failed.
        /// </summary>
        public bool AllFailed => TotalChannels > 0 && FailedDeliveries == TotalChannels;

        /// <summary>
        /// Gets whether there were any failures.
        /// </summary>
        public bool HasFailures => FailedDeliveries > 0;

        /// <summary>
        /// Gets an empty result instance.
        /// </summary>
        public static AlertDeliveryResults Empty => new()
        {
            TotalChannels = 0,
            SuccessfulDeliveries = 0,
            FailedDeliveries = 0,
            TotalDeliveryTime = TimeSpan.Zero,
            ChannelResults = Array.Empty<ChannelDeliveryResult>()
        };

        /// <summary>
        /// Creates a successful result for a single channel.
        /// </summary>
        /// <param name="channelResult">Channel delivery result</param>
        /// <returns>Delivery results instance</returns>
        public static AlertDeliveryResults FromSingleChannel(ChannelDeliveryResult channelResult)
        {
            return new AlertDeliveryResults
            {
                TotalChannels = 1,
                SuccessfulDeliveries = channelResult.IsSuccess ? 1 : 0,
                FailedDeliveries = channelResult.IsSuccess ? 0 : 1,
                TotalDeliveryTime = channelResult.DeliveryTime,
                ChannelResults = new[] { channelResult }
            };
        }

        /// <summary>
        /// Creates results from multiple channel results.
        /// </summary>
        /// <param name="channelResults">Collection of channel results</param>
        /// <returns>Aggregated delivery results</returns>
        public static AlertDeliveryResults FromChannelResults(IReadOnlyList<ChannelDeliveryResult> channelResults)
        {
            if (channelResults == null || channelResults.Count == 0)
                return Empty;

            var totalTime = TimeSpan.Zero;
            var successCount = 0;
            var failureCount = 0;

            foreach (var result in channelResults)
            {
                totalTime += result.DeliveryTime;
                if (result.IsSuccess)
                    successCount++;
                else
                    failureCount++;
            }

            return new AlertDeliveryResults
            {
                TotalChannels = channelResults.Count,
                SuccessfulDeliveries = successCount,
                FailedDeliveries = failureCount,
                TotalDeliveryTime = totalTime,
                ChannelResults = channelResults
            };
        }
    }
}