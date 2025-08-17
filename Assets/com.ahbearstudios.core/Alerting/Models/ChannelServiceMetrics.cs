using System;
using System.Collections.Generic;
using ZLinq;

namespace AhBearStudios.Core.Alerting.Models
{
    /// <summary>
    /// Comprehensive metrics for the alert channel service.
    /// Aggregates statistics across all registered channels and service operations.
    /// Designed for Unity game development with performance monitoring.
    /// </summary>
    public sealed record ChannelServiceMetrics
    {
        /// <summary>
        /// Gets the total number of registered channels.
        /// </summary>
        public int TotalChannels { get; init; }

        /// <summary>
        /// Gets the number of healthy channels.
        /// </summary>
        public int HealthyChannels { get; init; }

        /// <summary>
        /// Gets the number of enabled channels.
        /// </summary>
        public int EnabledChannels { get; init; }

        /// <summary>
        /// Gets the number of disabled channels.
        /// </summary>
        public int DisabledChannels => TotalChannels - EnabledChannels;

        /// <summary>
        /// Gets the number of unhealthy channels.
        /// </summary>
        public int UnhealthyChannels => TotalChannels - HealthyChannels;

        /// <summary>
        /// Gets detailed metrics for each channel.
        /// </summary>
        public IReadOnlyList<ChannelMetrics> ChannelMetrics { get; init; } = Array.Empty<ChannelMetrics>();

        /// <summary>
        /// Gets the last update timestamp.
        /// </summary>
        public DateTime LastUpdated { get; init; }

        /// <summary>
        /// Gets the service startup timestamp.
        /// </summary>
        public DateTime ServiceStartTime { get; init; }

        /// <summary>
        /// Gets the service uptime.
        /// </summary>
        public TimeSpan ServiceUptime => DateTime.UtcNow - ServiceStartTime;

        /// <summary>
        /// Gets the health rate as a percentage (0-100).
        /// </summary>
        public double HealthRate => TotalChannels > 0 
            ? (double)HealthyChannels / TotalChannels * 100 
            : 0;

        /// <summary>
        /// Gets the enabled rate as a percentage (0-100).
        /// </summary>
        public double EnabledRate => TotalChannels > 0 
            ? (double)EnabledChannels / TotalChannels * 100 
            : 0;

        /// <summary>
        /// Gets the total delivery attempts across all channels.
        /// </summary>
        public long TotalDeliveryAttempts => ChannelMetrics
            .AsValueEnumerable()
            .Sum(m => (long)m.TotalDeliveryAttempts);

        /// <summary>
        /// Gets the total successful deliveries across all channels.
        /// </summary>
        public long TotalSuccessfulDeliveries => ChannelMetrics
            .AsValueEnumerable()
            .Sum(m => (long)m.SuccessfulDeliveries);

        /// <summary>
        /// Gets the total failed deliveries across all channels.
        /// </summary>
        public long TotalFailedDeliveries => ChannelMetrics
            .AsValueEnumerable()
            .Sum(m => (long)m.FailedDeliveries);

        /// <summary>
        /// Gets the overall success rate across all channels.
        /// </summary>
        public double OverallSuccessRate => TotalDeliveryAttempts > 0 
            ? (double)TotalSuccessfulDeliveries / TotalDeliveryAttempts * 100 
            : 0;

        /// <summary>
        /// Gets the average delivery time across all channels.
        /// </summary>
        public double AverageDeliveryTimeMs
        {
            get
            {
                var totalTime = ChannelMetrics
                    .AsValueEnumerable()
                    .Sum(m => m.TotalDeliveryTime.TotalMilliseconds);
                
                return TotalDeliveryAttempts > 0 
                    ? totalTime / TotalDeliveryAttempts 
                    : 0;
            }
        }

        /// <summary>
        /// Creates an empty metrics instance.
        /// </summary>
        /// <param name="serviceStartTime">Service startup time</param>
        /// <returns>Empty metrics instance</returns>
        public static ChannelServiceMetrics CreateEmpty(DateTime serviceStartTime)
        {
            return new ChannelServiceMetrics
            {
                TotalChannels = 0,
                HealthyChannels = 0,
                EnabledChannels = 0,
                ChannelMetrics = Array.Empty<ChannelMetrics>(),
                LastUpdated = DateTime.UtcNow,
                ServiceStartTime = serviceStartTime
            };
        }

        /// <summary>
        /// Creates metrics from current channel state.
        /// </summary>
        /// <param name="allChannelMetrics">All channel metrics</param>
        /// <param name="healthyChannelCount">Number of healthy channels</param>
        /// <param name="enabledChannelCount">Number of enabled channels</param>
        /// <param name="serviceStartTime">Service startup time</param>
        /// <returns>Current metrics instance</returns>
        public static ChannelServiceMetrics FromChannelState(
            IReadOnlyList<ChannelMetrics> allChannelMetrics,
            int healthyChannelCount,
            int enabledChannelCount,
            DateTime serviceStartTime)
        {
            return new ChannelServiceMetrics
            {
                TotalChannels = allChannelMetrics.Count,
                HealthyChannels = healthyChannelCount,
                EnabledChannels = enabledChannelCount,
                ChannelMetrics = allChannelMetrics,
                LastUpdated = DateTime.UtcNow,
                ServiceStartTime = serviceStartTime
            };
        }

        /// <summary>
        /// Gets a summary string for debugging.
        /// </summary>
        /// <returns>Summary string</returns>
        public override string ToString()
        {
            return $"ChannelService: {TotalChannels} channels, " +
                   $"{HealthyChannels} healthy ({HealthRate:F1}%), " +
                   $"{EnabledChannels} enabled ({EnabledRate:F1}%), " +
                   $"{TotalDeliveryAttempts} deliveries, " +
                   $"{OverallSuccessRate:F1}% success rate";
        }
    }
}