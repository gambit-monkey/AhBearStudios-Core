using System;
using System.Threading;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Enhanced delivery statistics implementation for batch-optimized services.
    /// </summary>
    internal class BatchingDeliveryStatistics : DeliveryStatistics
    {
        private long _totalBatchesProcessed;
        private long _totalBatchProcessingTimeMs;
        private long _queuedMessages;
        
        /// <summary>
        /// Gets the total number of batches processed.
        /// </summary>
        public long TotalBatchesProcessed => _totalBatchesProcessed;
        
        /// <summary>
        /// Gets the average batch processing time in milliseconds.
        /// </summary>
        public double AverageBatchProcessingTimeMs => 
            _totalBatchesProcessed > 0 ? (double)_totalBatchProcessingTimeMs / _totalBatchesProcessed : 0.0;
        
        /// <summary>
        /// Gets the number of messages currently queued for batch processing.
        /// </summary>
        public long QueuedMessages => _queuedMessages;
        
        /// <summary>
        /// Gets the batch processing efficiency (messages per batch).
        /// </summary>
        public double BatchEfficiency => 
            _totalBatchesProcessed > 0 ? (double)TotalMessagesSent / _totalBatchesProcessed : 0.0;
        
        /// <summary>
        /// Records that a batch was processed.
        /// </summary>
        /// <param name="messageCount">The number of messages in the batch.</param>
        /// <param name="processingTime">The time taken to process the batch.</param>
        public void RecordBatchProcessed(int messageCount, TimeSpan processingTime)
        {
            Interlocked.Increment(ref _totalBatchesProcessed);
            Interlocked.Add(ref _totalBatchProcessingTimeMs, (long)processingTime.TotalMilliseconds);
        }
        
        /// <summary>
        /// Updates the queued messages count.
        /// </summary>
        /// <param name="count">The current number of queued messages.</param>
        public void UpdateQueuedCount(int count)
        {
            Interlocked.Exchange(ref _queuedMessages, count);
        }
        
        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            Interlocked.Exchange(ref _totalBatchesProcessed, 0);
            Interlocked.Exchange(ref _totalBatchProcessingTimeMs, 0);
            Interlocked.Exchange(ref _queuedMessages, 0);
        }
    }
}