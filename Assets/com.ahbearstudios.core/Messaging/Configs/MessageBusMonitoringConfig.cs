using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Configs
{
    /// <summary>
    /// Configuration for message bus monitoring service.
    /// Focused on monitoring-specific settings and performance tracking.
    /// </summary>
    public sealed class MessageBusMonitoringConfig
    {
        #region Core Monitoring Configuration

        /// <summary>
        /// Gets or sets whether monitoring is enabled.
        /// </summary>
        public bool MonitoringEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for updating statistics.
        /// </summary>
        public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the interval for checking monitoring thresholds.
        /// </summary>
        public TimeSpan ThresholdCheckInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the interval for performing anomaly detection.
        /// </summary>
        public TimeSpan AnomalyDetectionInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets whether to enable performance trend analysis.
        /// </summary>
        public bool PerformanceTrendAnalysisEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable anomaly detection.
        /// </summary>
        public bool AnomalyDetectionEnabled { get; set; } = true;

        #endregion

        #region Statistics Configuration

        /// <summary>
        /// Gets or sets whether to track per-message-type statistics.
        /// </summary>
        public bool TrackPerTypeStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of message types to track statistics for.
        /// </summary>
        public int MaxTrackedMessageTypes { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to track historical statistics.
        /// </summary>
        public bool TrackHistoricalStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets how long to retain historical statistics.
        /// </summary>
        public TimeSpan HistoricalDataRetention { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets the interval for archiving historical data.
        /// </summary>
        public TimeSpan HistoricalDataInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the maximum number of historical data points to keep in memory.
        /// </summary>
        public int MaxHistoricalDataPoints { get; set; } = 10000;

        #endregion

        #region Performance Thresholds

        /// <summary>
        /// Gets or sets the warning threshold for error rate (0.0 to 1.0).
        /// </summary>
        public double WarningErrorRateThreshold { get; set; } = 0.05; // 5%

        /// <summary>
        /// Gets or sets the critical threshold for error rate (0.0 to 1.0).
        /// </summary>
        public double CriticalErrorRateThreshold { get; set; } = 0.10; // 10%

        /// <summary>
        /// Gets or sets the warning threshold for average processing time in milliseconds.
        /// </summary>
        public double WarningProcessingTimeThreshold { get; set; } = 1000; // 1 second

        /// <summary>
        /// Gets or sets the critical threshold for average processing time in milliseconds.
        /// </summary>
        public double CriticalProcessingTimeThreshold { get; set; } = 5000; // 5 seconds

        /// <summary>
        /// Gets or sets the warning threshold for messages per second.
        /// </summary>
        public double WarningThroughputThreshold { get; set; } = 100;

        /// <summary>
        /// Gets or sets the critical threshold for messages per second.
        /// </summary>
        public double CriticalThroughputThreshold { get; set; } = 10;

        /// <summary>
        /// Gets or sets the warning threshold for memory usage in bytes.
        /// </summary>
        public long WarningMemoryUsageThreshold { get; set; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Gets or sets the critical threshold for memory usage in bytes.
        /// </summary>
        public long CriticalMemoryUsageThreshold { get; set; } = 100 * 1024 * 1024; // 100MB

        #endregion

        #region Anomaly Detection Configuration

        /// <summary>
        /// Gets or sets the sensitivity for anomaly detection (0.0 to 1.0).
        /// Higher values detect more anomalies.
        /// </summary>
        public double AnomalyDetectionSensitivity { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the minimum baseline period for anomaly detection.
        /// </summary>
        public TimeSpan AnomalyDetectionBaselinePeriod { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the minimum deviation percentage to consider as an anomaly.
        /// </summary>
        public double MinimumAnomalyDeviationPercentage { get; set; } = 20.0; // 20%

        /// <summary>
        /// Gets or sets the maximum number of anomalies to track.
        /// </summary>
        public int MaxTrackedAnomalies { get; set; } = 1000;

        #endregion

        #region Custom Thresholds

        /// <summary>
        /// Gets or sets custom monitoring thresholds.
        /// </summary>
        public Dictionary<string, MonitoringThreshold> CustomThresholds { get; set; } = new Dictionary<string, MonitoringThreshold>();

        #endregion

        #region Alerting Configuration

        /// <summary>
        /// Gets or sets whether to enable alerting for threshold violations.
        /// </summary>
        public bool AlertingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum time between duplicate alerts.
        /// </summary>
        public TimeSpan AlertSuppressionTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether to escalate alerts based on severity.
        /// </summary>
        public bool AlertEscalationEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the time before escalating an unresolved alert.
        /// </summary>
        public TimeSpan AlertEscalationTime { get; set; } = TimeSpan.FromMinutes(15);

        #endregion

        #region Memory Management

        /// <summary>
        /// Gets or sets the maximum memory pressure threshold for monitoring data.
        /// </summary>
        public long MaxMonitoringMemoryPressure { get; set; } = 25 * 1024 * 1024; // 25MB

        /// <summary>
        /// Gets or sets whether to automatically cleanup old data when memory pressure is high.
        /// </summary>
        public bool AutoCleanupOnHighMemoryPressure { get; set; } = true;

        /// <summary>
        /// Gets or sets the percentage of data to cleanup when memory pressure is high.
        /// </summary>
        public double MemoryPressureCleanupPercentage { get; set; } = 0.25; // 25%

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration for correctness and completeness.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            if (StatisticsUpdateInterval <= TimeSpan.Zero) return false;
            if (ThresholdCheckInterval <= TimeSpan.Zero) return false;
            if (AnomalyDetectionInterval <= TimeSpan.Zero) return false;
            if (MaxTrackedMessageTypes <= 0) return false;
            if (HistoricalDataRetention <= TimeSpan.Zero) return false;
            if (HistoricalDataInterval <= TimeSpan.Zero) return false;
            if (MaxHistoricalDataPoints <= 0) return false;
            
            if (WarningErrorRateThreshold < 0 || WarningErrorRateThreshold > 1) return false;
            if (CriticalErrorRateThreshold < 0 || CriticalErrorRateThreshold > 1) return false;
            if (CriticalErrorRateThreshold <= WarningErrorRateThreshold) return false;
            
            if (WarningProcessingTimeThreshold <= 0) return false;
            if (CriticalProcessingTimeThreshold <= WarningProcessingTimeThreshold) return false;
            
            if (WarningThroughputThreshold <= 0) return false;
            if (CriticalThroughputThreshold >= WarningThroughputThreshold) return false;
            
            if (WarningMemoryUsageThreshold <= 0) return false;
            if (CriticalMemoryUsageThreshold <= WarningMemoryUsageThreshold) return false;
            
            if (AnomalyDetectionSensitivity < 0 || AnomalyDetectionSensitivity > 1) return false;
            if (AnomalyDetectionBaselinePeriod <= TimeSpan.Zero) return false;
            if (MinimumAnomalyDeviationPercentage <= 0) return false;
            if (MaxTrackedAnomalies <= 0) return false;
            
            if (AlertSuppressionTime < TimeSpan.Zero) return false;
            if (AlertEscalationTime < TimeSpan.Zero) return false;
            
            if (MaxMonitoringMemoryPressure <= 0) return false;
            if (MemoryPressureCleanupPercentage <= 0 || MemoryPressureCleanupPercentage > 1) return false;

            return true;
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>Deep copy of the configuration</returns>
        public MessageBusMonitoringConfig Clone()
        {
            var clone = new MessageBusMonitoringConfig
            {
                MonitoringEnabled = MonitoringEnabled,
                StatisticsUpdateInterval = StatisticsUpdateInterval,
                ThresholdCheckInterval = ThresholdCheckInterval,
                AnomalyDetectionInterval = AnomalyDetectionInterval,
                PerformanceTrendAnalysisEnabled = PerformanceTrendAnalysisEnabled,
                AnomalyDetectionEnabled = AnomalyDetectionEnabled,
                TrackPerTypeStatistics = TrackPerTypeStatistics,
                MaxTrackedMessageTypes = MaxTrackedMessageTypes,
                TrackHistoricalStatistics = TrackHistoricalStatistics,
                HistoricalDataRetention = HistoricalDataRetention,
                HistoricalDataInterval = HistoricalDataInterval,
                MaxHistoricalDataPoints = MaxHistoricalDataPoints,
                WarningErrorRateThreshold = WarningErrorRateThreshold,
                CriticalErrorRateThreshold = CriticalErrorRateThreshold,
                WarningProcessingTimeThreshold = WarningProcessingTimeThreshold,
                CriticalProcessingTimeThreshold = CriticalProcessingTimeThreshold,
                WarningThroughputThreshold = WarningThroughputThreshold,
                CriticalThroughputThreshold = CriticalThroughputThreshold,
                WarningMemoryUsageThreshold = WarningMemoryUsageThreshold,
                CriticalMemoryUsageThreshold = CriticalMemoryUsageThreshold,
                AnomalyDetectionSensitivity = AnomalyDetectionSensitivity,
                AnomalyDetectionBaselinePeriod = AnomalyDetectionBaselinePeriod,
                MinimumAnomalyDeviationPercentage = MinimumAnomalyDeviationPercentage,
                MaxTrackedAnomalies = MaxTrackedAnomalies,
                AlertingEnabled = AlertingEnabled,
                AlertSuppressionTime = AlertSuppressionTime,
                AlertEscalationEnabled = AlertEscalationEnabled,
                AlertEscalationTime = AlertEscalationTime,
                MaxMonitoringMemoryPressure = MaxMonitoringMemoryPressure,
                AutoCleanupOnHighMemoryPressure = AutoCleanupOnHighMemoryPressure,
                MemoryPressureCleanupPercentage = MemoryPressureCleanupPercentage,
                CustomThresholds = new Dictionary<string, MonitoringThreshold>()
            };

            // Deep copy custom thresholds
            foreach (var threshold in CustomThresholds)
            {
                clone.CustomThresholds[threshold.Key] = new MonitoringThreshold
                {
                    Metric = threshold.Value.Metric,
                    Threshold = threshold.Value.Threshold,
                    ComparisonType = threshold.Value.ComparisonType,
                    Enabled = threshold.Value.Enabled,
                    LastTriggered = threshold.Value.LastTriggered
                };
            }

            return clone;
        }

        /// <summary>
        /// Returns a string representation of the configuration.
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public override string ToString()
        {
            return $"MessageBusMonitoringConfig: " +
                   $"Enabled={MonitoringEnabled}, " +
                   $"StatsInterval={StatisticsUpdateInterval.TotalSeconds}s, " +
                   $"ThresholdCheck={ThresholdCheckInterval.TotalSeconds}s, " +
                   $"AnomalyDetection={AnomalyDetectionEnabled}, " +
                   $"MaxTypes={MaxTrackedMessageTypes}, " +
                   $"HistoricalData={TrackHistoricalStatistics}, " +
                   $"Alerting={AlertingEnabled}";
        }

        #endregion
    }

}