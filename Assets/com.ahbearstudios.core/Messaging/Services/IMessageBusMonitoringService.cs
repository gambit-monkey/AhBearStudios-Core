using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.Messaging.Configs;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Interface for message bus monitoring and statistics collection.
    /// Handles performance metrics, statistics aggregation, and monitoring alerts.
    /// Focused on monitoring responsibilities only, following single responsibility principle.
    /// </summary>
    public interface IMessageBusMonitoringService : IDisposable
    {
        #region Statistics Collection

        /// <summary>
        /// Gets comprehensive statistics about message bus performance and health.
        /// </summary>
        /// <returns>Current message bus statistics</returns>
        MessageBusStatistics GetStatistics();

        /// <summary>
        /// Gets publishing-specific statistics.
        /// </summary>
        /// <returns>Current publishing statistics</returns>
        MessagePublishingStatistics GetPublishingStatistics();

        /// <summary>
        /// Gets subscription-specific statistics.
        /// </summary>
        /// <returns>Current subscription statistics</returns>
        MessageSubscriptionStatistics GetSubscriptionStatistics();

        /// <summary>
        /// Clears message history and resets all statistics counters.
        /// </summary>
        void ClearMessageHistory();

        /// <summary>
        /// Clears only publishing statistics.
        /// </summary>
        void ClearPublishingStatistics();

        /// <summary>
        /// Clears only subscription statistics.
        /// </summary>
        void ClearSubscriptionStatistics();

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Records a message publishing operation for metrics.
        /// </summary>
        /// <param name="messageType">The type of message published</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="processingTimeMs">Processing time in milliseconds</param>
        /// <param name="batchSize">Size of batch (1 for single messages)</param>
        void RecordPublishingOperation(Type messageType, bool success, double processingTimeMs, int batchSize = 1);

        /// <summary>
        /// Records a message processing operation for metrics.
        /// </summary>
        /// <param name="messageType">The type of message processed</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="processingTimeMs">Processing time in milliseconds</param>
        /// <param name="subscriberId">Identifier of the subscriber</param>
        void RecordProcessingOperation(Type messageType, bool success, double processingTimeMs, string subscriberId);

        /// <summary>
        /// Records a subscription lifecycle event for metrics.
        /// </summary>
        /// <param name="messageType">The type of message subscribed to</param>
        /// <param name="operation">The operation (Create, Dispose, etc.)</param>
        /// <param name="subscriberId">Identifier of the subscriber</param>
        void RecordSubscriptionOperation(Type messageType, string operation, string subscriberId);

        /// <summary>
        /// Records memory usage metrics.
        /// </summary>
        /// <param name="component">The component reporting memory usage</param>
        /// <param name="memoryUsageBytes">Memory usage in bytes</param>
        void RecordMemoryUsage(string component, long memoryUsageBytes);

        #endregion

        #region Real-time Monitoring

        /// <summary>
        /// Gets the current message processing rate (messages per second).
        /// </summary>
        /// <returns>Current messages per second rate</returns>
        double GetCurrentMessageRate();

        /// <summary>
        /// Gets the current error rate (0.0 to 1.0).
        /// </summary>
        /// <returns>Current error rate</returns>
        double GetCurrentErrorRate();

        /// <summary>
        /// Gets the current average processing time in milliseconds.
        /// </summary>
        /// <returns>Current average processing time</returns>
        double GetCurrentAverageProcessingTime();

        /// <summary>
        /// Gets the current total memory usage in bytes.
        /// </summary>
        /// <returns>Current memory usage</returns>
        long GetCurrentMemoryUsage();

        /// <summary>
        /// Gets the number of active publishers.
        /// </summary>
        /// <returns>Number of active publishers</returns>
        int GetActivePublishersCount();

        /// <summary>
        /// Gets the number of active subscribers.
        /// </summary>
        /// <returns>Number of active subscribers</returns>
        int GetActiveSubscribersCount();

        #endregion

        #region Historical Data

        /// <summary>
        /// Gets historical statistics for a specific time range.
        /// </summary>
        /// <param name="startTime">Start of the time range</param>
        /// <param name="endTime">End of the time range</param>
        /// <returns>Historical statistics</returns>
        MessageBusStatistics GetHistoricalStatistics(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Gets performance trend data for the specified duration.
        /// </summary>
        /// <param name="duration">Duration to analyze</param>
        /// <param name="interval">Interval for data points</param>
        /// <returns>Performance trend data</returns>
        IEnumerable<PerformanceDataPoint> GetPerformanceTrend(TimeSpan duration, TimeSpan interval);

        /// <summary>
        /// Gets the top performing message types by throughput.
        /// </summary>
        /// <param name="count">Number of top performers to return</param>
        /// <returns>Top performing message types</returns>
        IEnumerable<MessageTypePerformance> GetTopPerformingMessageTypes(int count = 10);

        /// <summary>
        /// Gets the worst performing message types by error rate.
        /// </summary>
        /// <param name="count">Number of worst performers to return</param>
        /// <returns>Worst performing message types</returns>
        IEnumerable<MessageTypePerformance> GetWorstPerformingMessageTypes(int count = 10);

        #endregion

        #region Alerts and Thresholds

        /// <summary>
        /// Sets a threshold for monitoring alerts.
        /// </summary>
        /// <param name="metric">The metric to monitor</param>
        /// <param name="threshold">The threshold value</param>
        /// <param name="comparisonType">How to compare against the threshold</param>
        void SetMonitoringThreshold(string metric, double threshold, ThresholdComparisonType comparisonType);

        /// <summary>
        /// Removes a monitoring threshold.
        /// </summary>
        /// <param name="metric">The metric to stop monitoring</param>
        void RemoveMonitoringThreshold(string metric);

        /// <summary>
        /// Gets all currently configured monitoring thresholds.
        /// </summary>
        /// <returns>Dictionary of metric names to thresholds</returns>
        Dictionary<string, MonitoringThreshold> GetMonitoringThresholds();

        /// <summary>
        /// Manually checks all thresholds and triggers alerts if necessary.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of thresholds that triggered alerts</returns>
        UniTask<int> CheckThresholds(CancellationToken cancellationToken = default);

        #endregion

        #region Configuration

        /// <summary>
        /// Updates the monitoring configuration at runtime.
        /// </summary>
        /// <param name="config">The new monitoring configuration</param>
        void UpdateConfiguration(MessageBusMonitoringConfig config);

        /// <summary>
        /// Gets the current monitoring configuration.
        /// </summary>
        /// <returns>Current monitoring configuration</returns>
        MessageBusMonitoringConfig GetConfiguration();

        /// <summary>
        /// Enables or disables monitoring temporarily.
        /// </summary>
        /// <param name="enabled">Whether monitoring should be enabled</param>
        void SetMonitoringEnabled(bool enabled);

        /// <summary>
        /// Gets whether monitoring is currently enabled.
        /// </summary>
        /// <returns>True if monitoring is enabled</returns>
        bool IsMonitoringEnabled();

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a monitoring threshold is exceeded.
        /// </summary>
        event Action<string, double, double> ThresholdExceeded;

        /// <summary>
        /// Event fired when statistics are updated.
        /// </summary>
        event Action<MessageBusStatistics> StatisticsUpdated;

        /// <summary>
        /// Event fired when a performance anomaly is detected.
        /// </summary>
        event Action<PerformanceAnomaly> AnomalyDetected;

        #endregion
    }

}