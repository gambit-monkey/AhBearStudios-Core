using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Interface for dead letter queue management.
    /// Handles permanently failed messages and provides analysis and recovery capabilities.
    /// Focused on dead letter queue responsibilities only, following single responsibility principle.
    /// </summary>
    public interface IDeadLetterQueueService : IDisposable
    {
        #region Dead Letter Operations

        /// <summary>
        /// Adds a failed message to the dead letter queue.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message that failed</param>
        /// <param name="reason">The reason for failure</param>
        /// <param name="exception">The exception that caused the failure</param>
        /// <param name="attemptCount">Number of attempts made</param>
        /// <param name="context">Additional context about the failure</param>
        void AddToDeadLetterQueue<TMessage>(
            TMessage message, 
            string reason, 
            Exception exception = null, 
            int attemptCount = 0,
            string context = null) where TMessage : IMessage;

        /// <summary>
        /// Adds a failed message to the dead letter queue using FailedMessage structure.
        /// </summary>
        /// <param name="failedMessage">The failed message information</param>
        void AddToDeadLetterQueue(FailedMessage failedMessage);

        /// <summary>
        /// Adds multiple failed messages to the dead letter queue.
        /// </summary>
        /// <param name="failedMessages">Collection of failed messages</param>
        void AddToDeadLetterQueue(IEnumerable<FailedMessage> failedMessages);

        /// <summary>
        /// Attempts to recover a message from the dead letter queue.
        /// </summary>
        /// <param name="messageId">ID of the message to recover</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the message was successfully recovered and reprocessed</returns>
        UniTask<bool> RecoverMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to recover multiple messages from the dead letter queue.
        /// </summary>
        /// <param name="messageIds">IDs of messages to recover</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of messages successfully recovered</returns>
        UniTask<int> RecoverMessagesAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);

        #endregion

        #region Queue Management

        /// <summary>
        /// Gets the current size of the dead letter queue.
        /// </summary>
        /// <returns>Number of messages in the dead letter queue</returns>
        int GetDeadLetterQueueSize();

        /// <summary>
        /// Gets messages currently in the dead letter queue.
        /// </summary>
        /// <param name="maxCount">Maximum number of messages to return</param>
        /// <param name="offset">Offset for pagination</param>
        /// <returns>Collection of failed messages</returns>
        IEnumerable<FailedMessage> GetDeadLetterMessages(int maxCount = 100, int offset = 0);

        /// <summary>
        /// Gets messages in the dead letter queue for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="maxCount">Maximum number of messages to return</param>
        /// <param name="offset">Offset for pagination</param>
        /// <returns>Collection of failed messages of the specified type</returns>
        IEnumerable<FailedMessage> GetDeadLetterMessages<TMessage>(int maxCount = 100, int offset = 0) where TMessage : IMessage;

        /// <summary>
        /// Gets messages that failed with a specific reason.
        /// </summary>
        /// <param name="reason">The failure reason to filter by</param>
        /// <param name="maxCount">Maximum number of messages to return</param>
        /// <returns>Collection of failed messages with the specified reason</returns>
        IEnumerable<FailedMessage> GetDeadLetterMessagesByReason(string reason, int maxCount = 100);

        /// <summary>
        /// Gets messages that failed within a specific time range.
        /// </summary>
        /// <param name="startTime">Start of the time range</param>
        /// <param name="endTime">End of the time range</param>
        /// <param name="maxCount">Maximum number of messages to return</param>
        /// <returns>Collection of failed messages within the time range</returns>
        IEnumerable<FailedMessage> GetDeadLetterMessagesByTimeRange(DateTime startTime, DateTime endTime, int maxCount = 100);

        /// <summary>
        /// Removes a specific message from the dead letter queue.
        /// </summary>
        /// <param name="messageId">ID of the message to remove</param>
        /// <returns>True if the message was found and removed</returns>
        bool RemoveFromDeadLetterQueue(Guid messageId);

        /// <summary>
        /// Removes multiple messages from the dead letter queue.
        /// </summary>
        /// <param name="messageIds">IDs of messages to remove</param>
        /// <returns>Number of messages successfully removed</returns>
        int RemoveFromDeadLetterQueue(IEnumerable<Guid> messageIds);

        /// <summary>
        /// Clears all messages from the dead letter queue.
        /// </summary>
        /// <returns>Number of messages that were cleared</returns>
        int ClearDeadLetterQueue();

        /// <summary>
        /// Clears messages of a specific type from the dead letter queue.
        /// </summary>
        /// <typeparam name="TMessage">The message type to clear</typeparam>
        /// <returns>Number of messages that were cleared</returns>
        int ClearDeadLetterQueue<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Clears messages older than the specified age from the dead letter queue.
        /// </summary>
        /// <param name="age">Maximum age of messages to keep</param>
        /// <returns>Number of messages that were cleared</returns>
        int ClearDeadLetterQueue(TimeSpan age);

        #endregion

        #region Analysis and Reporting

        /// <summary>
        /// Gets failure analysis for the dead letter queue.
        /// </summary>
        /// <returns>Analysis of failure patterns and statistics</returns>
        DeadLetterQueueAnalysis GetFailureAnalysis();

        /// <summary>
        /// Gets failure analysis for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Analysis of failure patterns for the message type</returns>
        DeadLetterQueueAnalysis GetFailureAnalysis<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Gets the most common failure reasons.
        /// </summary>
        /// <param name="count">Number of top reasons to return</param>
        /// <returns>Collection of failure reasons with counts</returns>
        IEnumerable<FailureReasonCount> GetTopFailureReasons(int count = 10);

        /// <summary>
        /// Gets the message types with the highest failure rates.
        /// </summary>
        /// <param name="count">Number of top types to return</param>
        /// <returns>Collection of message types with failure counts</returns>
        IEnumerable<MessageTypeFailureCount> GetTopFailingMessageTypes(int count = 10);

        /// <summary>
        /// Gets failure trends over time.
        /// </summary>
        /// <param name="duration">Duration to analyze</param>
        /// <param name="interval">Interval for data points</param>
        /// <returns>Collection of failure trend data points</returns>
        IEnumerable<FailureTrendDataPoint> GetFailureTrends(TimeSpan duration, TimeSpan interval);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Attempts to recover all messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type to recover</typeparam>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of messages successfully recovered</returns>
        UniTask<int> RecoverAllMessagesOfTypeAsync<TMessage>(CancellationToken cancellationToken = default) where TMessage : IMessage;

        /// <summary>
        /// Attempts to recover all messages that failed with a specific reason.
        /// </summary>
        /// <param name="reason">The failure reason</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Number of messages successfully recovered</returns>
        UniTask<int> RecoverMessagesByReasonAsync(string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports dead letter queue messages for external analysis.
        /// </summary>
        /// <param name="format">Export format (JSON, CSV, XML)</param>
        /// <param name="filePath">Path where to save the export</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if export was successful</returns>
        UniTask<bool> ExportDeadLetterQueueAsync(string format, string filePath, CancellationToken cancellationToken = default);

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the maximum size of the dead letter queue.
        /// </summary>
        /// <param name="maxSize">Maximum number of messages to keep</param>
        void SetMaxQueueSize(int maxSize);

        /// <summary>
        /// Gets the maximum size of the dead letter queue.
        /// </summary>
        /// <returns>Maximum queue size</returns>
        int GetMaxQueueSize();

        /// <summary>
        /// Sets the retention policy for dead letter messages.
        /// </summary>
        /// <param name="retentionPeriod">How long to keep messages</param>
        void SetRetentionPolicy(TimeSpan retentionPeriod);

        /// <summary>
        /// Gets the current retention policy.
        /// </summary>
        /// <returns>Current retention period</returns>
        TimeSpan GetRetentionPolicy();

        #endregion

        #region Health and Status

        /// <summary>
        /// Gets the current health status of the dead letter queue service.
        /// </summary>
        /// <returns>Current health status</returns>
        HealthStatus GetHealthStatus();

        /// <summary>
        /// Forces a health check evaluation and returns the result.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Health check result</returns>
        UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dead letter queue statistics.
        /// </summary>
        /// <returns>Current dead letter queue statistics</returns>
        DeadLetterQueueStatistics GetStatistics();

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a message is added to the dead letter queue.
        /// </summary>
        event Action<FailedMessage> MessageAddedToDeadLetterQueue;

        /// <summary>
        /// Event fired when a message is recovered from the dead letter queue.
        /// </summary>
        event Action<Guid, Type, bool> MessageRecovered;

        /// <summary>
        /// Event fired when the dead letter queue reaches capacity.
        /// </summary>
        event Action<int, int> DeadLetterQueueCapacityReached;

        /// <summary>
        /// Event fired when old messages are purged from the queue.
        /// </summary>
        event Action<int, TimeSpan> OldMessagesPurged;

        #endregion
    }

    /// <summary>
    /// Analysis results for the dead letter queue.
    /// </summary>
    public sealed class DeadLetterQueueAnalysis
    {
        /// <summary>
        /// Gets or sets the total number of failed messages.
        /// </summary>
        public long TotalFailedMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of unique failure reasons.
        /// </summary>
        public int UniqueFailureReasons { get; set; }

        /// <summary>
        /// Gets or sets the number of unique message types.
        /// </summary>
        public int UniqueMessageTypes { get; set; }

        /// <summary>
        /// Gets or sets the most common failure reason.
        /// </summary>
        public string MostCommonFailureReason { get; set; }

        /// <summary>
        /// Gets or sets the message type with the most failures.
        /// </summary>
        public Type MostFailingMessageType { get; set; }

        /// <summary>
        /// Gets or sets the average attempt count before failure.
        /// </summary>
        public double AverageAttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the oldest message timestamp.
        /// </summary>
        public DateTime OldestMessageTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the newest message timestamp.
        /// </summary>
        public DateTime NewestMessageTimestamp { get; set; }

        /// <summary>
        /// Gets or sets when this analysis was performed.
        /// </summary>
        public DateTime AnalyzedAt { get; set; }
    }

    /// <summary>
    /// Represents a failure reason with its count.
    /// </summary>
    public sealed class FailureReasonCount
    {
        /// <summary>
        /// Gets or sets the failure reason.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the count of messages with this reason.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the percentage of total failures.
        /// </summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Represents a message type with its failure count.
    /// </summary>
    public sealed class MessageTypeFailureCount
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public Type MessageType { get; set; }

        /// <summary>
        /// Gets or sets the count of failed messages of this type.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the percentage of total failures.
        /// </summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Represents a failure trend data point.
    /// </summary>
    public sealed class FailureTrendDataPoint
    {
        /// <summary>
        /// Gets or sets the timestamp of this data point.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the number of failures at this point.
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the failure rate per hour.
        /// </summary>
        public double FailureRatePerHour { get; set; }
    }

    /// <summary>
    /// Statistics for the dead letter queue service.
    /// </summary>
    public sealed class DeadLetterQueueStatistics
    {
        /// <summary>
        /// Gets or sets the current number of messages in the queue.
        /// </summary>
        public int CurrentQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum queue size.
        /// </summary>
        public int MaxQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages ever added to the queue.
        /// </summary>
        public long TotalMessagesAdded { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages recovered from the queue.
        /// </summary>
        public long TotalMessagesRecovered { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages removed from the queue.
        /// </summary>
        public long TotalMessagesRemoved { get; set; }

        /// <summary>
        /// Gets or sets the recovery success rate (0.0 to 1.0).
        /// </summary>
        public double RecoverySuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the current retention policy.
        /// </summary>
        public TimeSpan RetentionPeriod { get; set; }

        /// <summary>
        /// Gets or sets when these statistics were captured.
        /// </summary>
        public DateTime CapturedAt { get; set; }
    }
}