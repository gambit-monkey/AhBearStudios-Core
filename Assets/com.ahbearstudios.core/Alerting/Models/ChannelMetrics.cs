using System;
using Unity.Collections;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Performance metrics for an alert channel.
    /// Tracks delivery statistics, success rates, and timing information.
    /// Designed for Unity game development with performance monitoring.
    /// </summary>
    public sealed record ChannelMetrics
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public FixedString64Bytes ChannelName { get; init; }

        /// <summary>
        /// Gets the registration timestamp.
        /// </summary>
        public DateTime RegistrationTime { get; init; }

        /// <summary>
        /// Gets the total number of delivery attempts.
        /// </summary>
        public long TotalDeliveryAttempts { get; init; }

        /// <summary>
        /// Gets the number of successful deliveries.
        /// </summary>
        public long SuccessfulDeliveries { get; init; }

        /// <summary>
        /// Gets the number of failed deliveries.
        /// </summary>
        public long FailedDeliveries { get; init; }

        /// <summary>
        /// Gets the total delivery time.
        /// </summary>
        public TimeSpan TotalDeliveryTime { get; init; }

        /// <summary>
        /// Gets the last delivery attempt timestamp.
        /// </summary>
        public DateTime? LastDeliveryAttempt { get; init; }

        /// <summary>
        /// Gets the success rate as a percentage (0-100).
        /// </summary>
        public double SuccessRate => TotalDeliveryAttempts > 0 
            ? (double)SuccessfulDeliveries / TotalDeliveryAttempts * 100 
            : 0;

        /// <summary>
        /// Gets the failure rate as a percentage (0-100).
        /// </summary>
        public double FailureRate => TotalDeliveryAttempts > 0 
            ? (double)FailedDeliveries / TotalDeliveryAttempts * 100 
            : 0;

        /// <summary>
        /// Gets the average delivery time in milliseconds.
        /// </summary>
        public double AverageDeliveryTimeMs => TotalDeliveryAttempts > 0 
            ? TotalDeliveryTime.TotalMilliseconds / TotalDeliveryAttempts 
            : 0;

        /// <summary>
        /// Creates an empty metrics instance for a new channel.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <returns>Empty metrics instance</returns>
        public static ChannelMetrics CreateEmpty(FixedString64Bytes channelName)
        {
            return new ChannelMetrics
            {
                ChannelName = channelName,
                RegistrationTime = DateTime.UtcNow,
                TotalDeliveryAttempts = 0,
                SuccessfulDeliveries = 0,
                FailedDeliveries = 0,
                TotalDeliveryTime = TimeSpan.Zero,
                LastDeliveryAttempt = null
            };
        }

        /// <summary>
        /// Creates a copy with incremented delivery counters.
        /// </summary>
        /// <param name="success">Whether the delivery was successful</param>
        /// <param name="deliveryTime">Time taken for delivery</param>
        /// <returns>Updated metrics instance</returns>
        public ChannelMetrics WithDelivery(bool success, TimeSpan deliveryTime)
        {
            return this with
            {
                TotalDeliveryAttempts = TotalDeliveryAttempts + 1,
                SuccessfulDeliveries = success ? SuccessfulDeliveries + 1 : SuccessfulDeliveries,
                FailedDeliveries = success ? FailedDeliveries : FailedDeliveries + 1,
                TotalDeliveryTime = TotalDeliveryTime + deliveryTime,
                LastDeliveryAttempt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a reset copy with counters zeroed but preserving registration time.
        /// </summary>
        /// <returns>Reset metrics instance</returns>
        public ChannelMetrics Reset()
        {
            return this with
            {
                TotalDeliveryAttempts = 0,
                SuccessfulDeliveries = 0,
                FailedDeliveries = 0,
                TotalDeliveryTime = TimeSpan.Zero,
                LastDeliveryAttempt = null
            };
        }
    }
}