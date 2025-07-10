using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Interfaces
{
    /// <summary>
    /// Interface for logging system performance metrics.
    /// Provides comprehensive metrics collection and performance analysis capabilities
    /// for the logging infrastructure including message processing, target performance,
    /// and queue management.
    /// </summary>
    public interface ILoggingMetrics
    {
        /// <summary>
        /// Gets the total number of log messages processed.
        /// </summary>
        long TotalMessagesProcessed { get; }
        
        /// <summary>
        /// Gets the total number of log messages that failed to process.
        /// </summary>
        long TotalMessagesFailed { get; }
        
        /// <summary>
        /// Gets the total number of messages currently queued for processing.
        /// </summary>
        int CurrentQueueSize { get; }
        
        /// <summary>
        /// Gets the maximum queue size reached since last reset.
        /// </summary>
        int PeakQueueSize { get; }
        
        /// <summary>
        /// Gets the average message processing time in milliseconds.
        /// </summary>
        double AverageProcessingTimeMs { get; }
        
        /// <summary>
        /// Gets the peak message processing time in milliseconds.
        /// </summary>
        double PeakProcessingTimeMs { get; }
        
        /// <summary>
        /// Gets the number of messages processed in the last batch.
        /// </summary>
        int LastBatchSize { get; }
        
        /// <summary>
        /// Gets the average batch processing time in milliseconds.
        /// </summary>
        double AverageBatchProcessingTimeMs { get; }
        
        /// <summary>
        /// Gets the total number of flush operations performed.
        /// </summary>
        long TotalFlushOperations { get; }
        
        /// <summary>
        /// Gets the number of flush operations that failed.
        /// </summary>
        long FailedFlushOperations { get; }
        
        /// <summary>
        /// Gets the average flush operation time in milliseconds.
        /// </summary>
        double AverageFlushTimeMs { get; }
        
        /// <summary>
        /// Gets the total number of active log targets.
        /// </summary>
        int ActiveTargetCount { get; }
        
        /// <summary>
        /// Gets the total number of failed target operations.
        /// </summary>
        long TotalTargetFailures { get; }
        
        /// <summary>
        /// Gets the memory usage of the logging system in bytes.
        /// </summary>
        long MemoryUsageBytes { get; }
        
        /// <summary>
        /// Gets the peak memory usage in bytes.
        /// </summary>
        long PeakMemoryUsageBytes { get; }

        /// <summary>
        /// Records metrics for a log message processing operation.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="tag">The tag associated with the message.</param>
        /// <param name="processingTime">The time taken to process the message.</param>
        /// <param name="success">Whether the message was processed successfully.</param>
        /// <param name="messageSize">The size of the message in bytes (optional).</param>
        void RecordMessageProcessing(LogLevel level, LogTag tag, TimeSpan processingTime, bool success, int messageSize = 0);

        /// <summary>
        /// Records metrics for a batch processing operation.
        /// </summary>
        /// <param name="batchSize">The number of messages in the batch.</param>
        /// <param name="processingTime">The time taken to process the batch.</param>
        /// <param name="successCount">The number of messages processed successfully.</param>
        /// <param name="failureCount">The number of messages that failed to process.</param>
        void RecordBatchProcessing(int batchSize, TimeSpan processingTime, int successCount, int failureCount);

        /// <summary>
        /// Records metrics for a flush operation.
        /// </summary>
        /// <param name="flushTime">The time taken for the flush operation.</param>
        /// <param name="messagesFlushed">The number of messages flushed.</param>
        /// <param name="success">Whether the flush operation was successful.</param>
        void RecordFlushOperation(TimeSpan flushTime, int messagesFlushed, bool success);

        /// <summary>
        /// Records metrics for log target operations.
        /// </summary>
        /// <param name="targetName">The name of the log target.</param>
        /// <param name="operationType">The type of operation (write, flush, etc.).</param>
        /// <param name="duration">The duration of the operation.</param>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="dataSize">The size of data processed in bytes (optional).</param>
        void RecordTargetOperation(string targetName, string operationType, TimeSpan duration, bool success, int dataSize = 0);

        /// <summary>
        /// Updates the current queue size metrics.
        /// </summary>
        /// <param name="currentSize">The current size of the queue.</param>
        /// <param name="capacity">The total capacity of the queue.</param>
        void UpdateQueueMetrics(int currentSize, int capacity);

        /// <summary>
        /// Records memory usage for the logging system.
        /// </summary>
        /// <param name="memoryUsageBytes">Current memory usage in bytes.</param>
        void RecordMemoryUsage(long memoryUsageBytes);

        /// <summary>
        /// Gets metrics for a specific log level.
        /// </summary>
        /// <param name="level">The log level to get metrics for.</param>
        /// <returns>Dictionary containing metrics for the specified level.</returns>
        Dictionary<string, object> GetLevelMetrics(LogLevel level);

        /// <summary>
        /// Gets metrics for a specific log tag.
        /// </summary>
        /// <param name="tag">The log tag to get metrics for.</param>
        /// <returns>Dictionary containing metrics for the specified tag.</returns>
        Dictionary<string, object> GetTagMetrics(LogTag tag);

        /// <summary>
        /// Gets metrics for a specific log target.
        /// </summary>
        /// <param name="targetName">The name of the log target.</param>
        /// <returns>Dictionary containing metrics for the specified target.</returns>
        Dictionary<string, object> GetTargetMetrics(string targetName);

        /// <summary>
        /// Gets all logging metrics as a comprehensive dictionary.
        /// </summary>
        /// <returns>Dictionary containing all logging metrics.</returns>
        Dictionary<string, object> GetAllMetrics();

        /// <summary>
        /// Gets a performance snapshot suitable for display.
        /// </summary>
        /// <returns>Dictionary of formatted metric values.</returns>
        Dictionary<string, string> GetPerformanceSnapshot();

        /// <summary>
        /// Gets the throughput (messages per second) over the last time period.
        /// </summary>
        /// <param name="timePeriodSeconds">The time period to calculate throughput for (default: 60 seconds).</param>
        /// <returns>Messages processed per second.</returns>
        double GetThroughput(int timePeriodSeconds = 60);

        /// <summary>
        /// Gets the error rate (percentage of failed messages) over the last time period.
        /// </summary>
        /// <param name="timePeriodSeconds">The time period to calculate error rate for (default: 60 seconds).</param>
        /// <returns>Error rate as a percentage (0-100).</returns>
        double GetErrorRate(int timePeriodSeconds = 60);

        /// <summary>
        /// Gets the queue utilization as a percentage of capacity.
        /// </summary>
        /// <returns>Queue utilization percentage (0-100).</returns>
        double GetQueueUtilization();

        /// <summary>
        /// Registers an alert for a specific logging metric.
        /// </summary>
        /// <param name="metricName">Name of the metric to monitor.</param>
        /// <param name="threshold">Threshold value that triggers the alert.</param>
        /// <param name="alertType">Type of alert (above/below threshold).</param>
        void RegisterAlert(string metricName, double threshold, string alertType = "above");

        /// <summary>
        /// Removes a registered alert for a specific metric.
        /// </summary>
        /// <param name="metricName">Name of the metric to stop monitoring.</param>
        void RemoveAlert(string metricName);

        /// <summary>
        /// Gets all currently registered alerts.
        /// </summary>
        /// <returns>Dictionary of metric names and their alert configurations.</returns>
        Dictionary<string, object> GetRegisteredAlerts();

        /// <summary>
        /// Resets all logging metrics to their initial state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Resets metrics for a specific log level.
        /// </summary>
        /// <param name="level">The log level to reset metrics for.</param>
        void ResetLevelMetrics(LogLevel level);

        /// <summary>
        /// Resets metrics for a specific log tag.
        /// </summary>
        /// <param name="tag">The log tag to reset metrics for.</param>
        void ResetTagMetrics(LogTag tag);

        /// <summary>
        /// Resets metrics for a specific log target.
        /// </summary>
        /// <param name="targetName">The name of the log target to reset metrics for.</param>
        void ResetTargetMetrics(string targetName);

        /// <summary>
        /// Whether the metrics tracker is created and initialized.
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// Whether metrics collection is currently enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the current configuration of the metrics system.
        /// </summary>
        /// <returns>Dictionary containing configuration settings.</returns>
        Dictionary<string, object> GetConfiguration();

        /// <summary>
        /// Updates the configuration of the metrics system.
        /// </summary>
        /// <param name="configuration">New configuration settings.</param>
        void UpdateConfiguration(Dictionary<string, object> configuration);
    }
}