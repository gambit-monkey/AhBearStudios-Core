using System;
using System.Threading;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Implementation of IDeliveryStatistics that tracks delivery service metrics.
    /// </summary>
    internal class DeliveryStatistics : IDeliveryStatistics
    {
        private long _totalMessagesSent;
        private long _totalMessagesDelivered;
        private long _totalMessagesFailed;
        private long _totalMessagesAcknowledged;
        private long _pendingDeliveries;
        private long _totalDeliveryTimeMs;
        private long _deliveryCount;
        
        /// <inheritdoc />
        public long TotalMessagesSent => _totalMessagesSent;
        
        /// <inheritdoc />
        public long TotalMessagesDelivered => _totalMessagesDelivered;
        
        /// <inheritdoc />
        public long TotalMessagesFailed => _totalMessagesFailed;
        
        /// <inheritdoc />
        public long TotalMessagesAcknowledged => _totalMessagesAcknowledged;
        
        /// <inheritdoc />
        public long PendingDeliveries => _pendingDeliveries;
        
        /// <inheritdoc />
        public double AverageDeliveryTimeMs => 
            _deliveryCount > 0 ? (double)_totalDeliveryTimeMs / _deliveryCount : 0.0;
        
        /// <inheritdoc />
        public double DeliverySuccessRate => 
            _totalMessagesSent > 0 ? (double)_totalMessagesDelivered / _totalMessagesSent * 100.0 : 0.0;
        
        /// <inheritdoc />
        public DateTime LastResetTime { get; private set; } = DateTime.UtcNow;
        
        /// <inheritdoc />
        public virtual void Reset()
        {
            Interlocked.Exchange(ref _totalMessagesSent, 0);
            Interlocked.Exchange(ref _totalMessagesDelivered, 0);
            Interlocked.Exchange(ref _totalMessagesFailed, 0);
            Interlocked.Exchange(ref _totalMessagesAcknowledged, 0);
            Interlocked.Exchange(ref _pendingDeliveries, 0);
            Interlocked.Exchange(ref _totalDeliveryTimeMs, 0);
            Interlocked.Exchange(ref _deliveryCount, 0);
            
            LastResetTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Records that a message was sent.
        /// </summary>
        public void RecordMessageSent()
        {
            Interlocked.Increment(ref _totalMessagesSent);
        }
        
        /// <summary>
        /// Records that a message was delivered successfully.
        /// </summary>
        public void RecordMessageDelivered()
        {
            Interlocked.Increment(ref _totalMessagesDelivered);
        }
        
        /// <summary>
        /// Records that a message delivery failed.
        /// </summary>
        public void RecordMessageFailed()
        {
            Interlocked.Increment(ref _totalMessagesFailed);
        }
        
        /// <summary>
        /// Records that a message was acknowledged.
        /// </summary>
        public void RecordMessageAcknowledged()
        {
            Interlocked.Increment(ref _totalMessagesAcknowledged);
        }
        
        /// <summary>
        /// Records the delivery time for a message.
        /// </summary>
        /// <param name="deliveryTimeMs">The delivery time in milliseconds.</param>
        public void RecordDeliveryTime(long deliveryTimeMs)
        {
            Interlocked.Add(ref _totalDeliveryTimeMs, deliveryTimeMs);
            Interlocked.Increment(ref _deliveryCount);
        }
        
        /// <summary>
        /// Updates the pending deliveries count.
        /// </summary>
        /// <param name="count">The current number of pending deliveries.</param>
        public void UpdatePendingCount(int count)
        {
            Interlocked.Exchange(ref _pendingDeliveries, count);
        }
    }
}