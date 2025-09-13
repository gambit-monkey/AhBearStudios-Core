using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Models
{
    /// <summary>
    /// Statistics for message publishing operations.
    /// Tracks publishing performance, errors, and throughput metrics.
    /// </summary>
    public sealed class MessagePublishingStatistics
    {
        /// <summary>
        /// Gets or sets the total number of messages published successfully.
        /// </summary>
        public long TotalMessagesPublished { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages that failed to publish.
        /// </summary>
        public long TotalMessagesFailedToPublish { get; set; }

        /// <summary>
        /// Gets or sets the total number of batch operations performed.
        /// </summary>
        public long TotalBatchOperations { get; set; }

        /// <summary>
        /// Gets or sets the current number of active publishers.
        /// </summary>
        public int ActivePublishers { get; set; }

        /// <summary>
        /// Gets or sets the average publishing time in milliseconds.
        /// </summary>
        public double AveragePublishingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the peak publishing time in milliseconds.
        /// </summary>
        public double PeakPublishingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the current messages per second rate.
        /// </summary>
        public double MessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the peak messages per second rate.
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
        /// Gets or sets per-message-type statistics.
        /// </summary>
        public Dictionary<Type, MessageTypeStatistics> MessageTypeStatistics { get; set; } = new Dictionary<Type, MessageTypeStatistics>();

        /// <summary>
        /// Gets the total number of messages processed (published + failed).
        /// </summary>
        public long TotalMessages => TotalMessagesPublished + TotalMessagesFailedToPublish;

        /// <summary>
        /// Gets the success rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalMessages > 0 ? (double)TotalMessagesPublished / TotalMessages : 1.0;

        /// <summary>
        /// Returns a string representation of the publishing statistics.
        /// </summary>
        /// <returns>Statistics summary string</returns>
        public override string ToString()
        {
            return $"MessagePublishingStatistics: " +
                   $"Published={TotalMessagesPublished}, " +
                   $"Failed={TotalMessagesFailedToPublish}, " +
                   $"Batches={TotalBatchOperations}, " +
                   $"Publishers={ActivePublishers}, " +
                   $"AvgTime={AveragePublishingTimeMs:F2}ms, " +
                   $"MPS={MessagesPerSecond:F1}, " +
                   $"ErrorRate={ErrorRate:P2}, " +
                   $"Memory={MemoryUsageBytes / 1024}KB";
        }
    }
}