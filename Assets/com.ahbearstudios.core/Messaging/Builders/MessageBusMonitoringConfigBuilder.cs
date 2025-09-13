using System;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders
{
    /// <summary>
    /// Builder for creating MessageBusMonitoringConfig instances.
    /// Provides a fluent API for configuring message bus monitoring behavior.
    /// </summary>
    public sealed class MessageBusMonitoringConfigBuilder
    {
        private bool _monitoringEnabled = true;
        private bool _performanceTrendAnalysisEnabled = true;
        private TimeSpan _statisticsUpdateInterval = TimeSpan.FromSeconds(10);
        private TimeSpan _historicalDataRetention = TimeSpan.FromDays(7);
        private bool _anomalyDetectionEnabled = true;
        private bool _trackPerTypeStatistics = true;

        /// <summary>
        /// Enables or disables monitoring.
        /// </summary>
        /// <param name="enabled">Whether monitoring is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusMonitoringConfigBuilder WithMonitoringEnabled(bool enabled)
        {
            _monitoringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables performance trend analysis.
        /// </summary>
        /// <param name="enabled">Whether performance trend analysis is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusMonitoringConfigBuilder WithPerformanceTrendAnalysis(bool enabled)
        {
            _performanceTrendAnalysisEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the statistics update interval.
        /// </summary>
        /// <param name="interval">Statistics update interval</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusMonitoringConfigBuilder WithStatisticsUpdateInterval(TimeSpan interval)
        {
            _statisticsUpdateInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the historical data retention period.
        /// </summary>
        /// <param name="retention">Historical data retention period</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusMonitoringConfigBuilder WithHistoricalDataRetention(TimeSpan retention)
        {
            _historicalDataRetention = retention;
            return this;
        }

        /// <summary>
        /// Enables or disables anomaly detection.
        /// </summary>
        /// <param name="enabled">Whether anomaly detection is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusMonitoringConfigBuilder WithAnomalyDetection(bool enabled)
        {
            _anomalyDetectionEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables per-message-type statistics tracking.
        /// </summary>
        /// <param name="enabled">Whether per-type statistics tracking is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusMonitoringConfigBuilder WithPerTypeStatistics(bool enabled)
        {
            _trackPerTypeStatistics = enabled;
            return this;
        }

        /// <summary>
        /// Builds the MessageBusMonitoringConfig instance with the configured values.
        /// </summary>
        /// <returns>A new MessageBusMonitoringConfig instance</returns>
        public MessageBusMonitoringConfig Build()
        {
            return new MessageBusMonitoringConfig
            {
                MonitoringEnabled = _monitoringEnabled,
                PerformanceTrendAnalysisEnabled = _performanceTrendAnalysisEnabled,
                StatisticsUpdateInterval = _statisticsUpdateInterval,
                HistoricalDataRetention = _historicalDataRetention,
                AnomalyDetectionEnabled = _anomalyDetectionEnabled,
                TrackPerTypeStatistics = _trackPerTypeStatistics
            };
        }
    }
}