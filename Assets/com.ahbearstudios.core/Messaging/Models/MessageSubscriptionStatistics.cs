using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Statistics for message subscription operations.
    /// Tracks subscription performance, errors, and processing metrics.
    /// </summary>
    public sealed class MessageSubscriptionStatistics
    {
        /// <summary>
        /// Gets or sets the total number of subscriptions created.
        /// </summary>
        public long TotalSubscriptionsCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of subscriptions disposed.
        /// </summary>
        public long TotalSubscriptionsDisposed { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages processed by subscriptions.
        /// </summary>
        public long TotalMessagesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages that failed processing.
        /// </summary>
        public long TotalMessagesFailedToProcess { get; set; }

        /// <summary>
        /// Gets or sets the current number of active subscriptions.
        /// </summary>
        public int ActiveSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the current number of active message scopes.
        /// </summary>
        public int ActiveScopes { get; set; }

        /// <summary>
        /// Gets or sets the average message processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the peak message processing time in milliseconds.
        /// </summary>
        public double PeakProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the current messages processed per second rate.
        /// </summary>
        public double MessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the peak messages processed per second rate.
        /// </summary>
        public double PeakMessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the current error rate (0.0 to 1.0).
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Gets or sets the memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when these statistics were captured.
        /// </summary>
        public DateTime CapturedAt { get; set; }

        /// <summary>
        /// Gets or sets the last reset timestamp.
        /// </summary>
        public DateTime LastResetAt { get; set; }

        /// <summary>
        /// Gets or sets per-message-type subscription statistics.
        /// </summary>
        public Dictionary<Type, SubscriberStatistics> MessageTypeStatistics { get; set; } = new Dictionary<Type, SubscriberStatistics>();

        /// <summary>
        /// Gets or sets statistics for scoped subscriptions.
        /// </summary>
        public Dictionary<Guid, ScopeStatistics> ScopeStatistics { get; set; } = new Dictionary<Guid, ScopeStatistics>();

        /// <summary>
        /// Gets the total number of messages processed (successful + failed).
        /// </summary>
        public long TotalMessages => TotalMessagesProcessed + TotalMessagesFailedToProcess;

        /// <summary>
        /// Gets the success rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalMessages > 0 ? (double)TotalMessagesProcessed / TotalMessages : 1.0;

        /// <summary>
        /// Gets the current number of active subscriptions.
        /// </summary>
        public int CurrentActiveSubscriptions => ActiveSubscriptions;

        /// <summary>
        /// Gets the subscription churn rate (created - disposed).
        /// </summary>
        public long SubscriptionChurnRate => TotalSubscriptionsCreated - TotalSubscriptionsDisposed;

        /// <summary>
        /// Returns a string representation of the subscription statistics.
        /// </summary>
        /// <returns>Statistics summary string</returns>
        public override string ToString()
        {
            return $"MessageSubscriptionStatistics: " +
                   $"Active={ActiveSubscriptions}, " +
                   $"Created={TotalSubscriptionsCreated}, " +
                   $"Disposed={TotalSubscriptionsDisposed}, " +
                   $"Processed={TotalMessagesProcessed}, " +
                   $"Failed={TotalMessagesFailedToProcess}, " +
                   $"Scopes={ActiveScopes}, " +
                   $"AvgTime={AverageProcessingTimeMs:F2}ms, " +
                   $"MPS={MessagesPerSecond:F1}, " +
                   $"ErrorRate={ErrorRate:P2}, " +
                   $"Memory={MemoryUsageBytes / 1024}KB";
        }
    }

    /// <summary>
    /// Statistics for a specific message scope.
    /// </summary>
    public sealed class ScopeStatistics
    {
        /// <summary>
        /// Gets or sets the scope identifier.
        /// </summary>
        public Guid ScopeId { get; set; }

        /// <summary>
        /// Gets or sets when the scope was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of subscriptions in this scope.
        /// </summary>
        public int SubscriptionCount { get; set; }

        /// <summary>
        /// Gets or sets the total messages processed by this scope.
        /// </summary>
        public long MessagesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total processing failures in this scope.
        /// </summary>
        public long ProcessingFailures { get; set; }

        /// <summary>
        /// Gets or sets whether this scope is still active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Returns a string representation of the scope statistics.
        /// </summary>
        /// <returns>Scope statistics summary string</returns>
        public override string ToString()
        {
            return $"ScopeStatistics[{ScopeId:N}]: " +
                   $"Subs={SubscriptionCount}, " +
                   $"Processed={MessagesProcessed}, " +
                   $"Failed={ProcessingFailures}, " +
                   $"Active={IsActive}";
        }
    }
}