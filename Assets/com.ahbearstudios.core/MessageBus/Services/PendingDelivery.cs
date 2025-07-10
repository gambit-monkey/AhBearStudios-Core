using System;
using System.Threading.Tasks;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Represents a pending message delivery with tracking and completion management.
    /// </summary>
    internal sealed class PendingDelivery : IPendingDelivery
    {
        private readonly TaskCompletionSource<object> _completionSource;
        private readonly object _statusLock = new object();
        
        /// <inheritdoc />
        public IMessage Message { get; }
        
        /// <inheritdoc />
        public Guid DeliveryId { get; }
        
        /// <inheritdoc />
        public MessageDeliveryStatus Status { get; private set; }
        
        /// <inheritdoc />
        public int DeliveryAttempts { get; private set; }
        
        /// <inheritdoc />
        public DateTime FirstAttemptTime { get; }
        
        /// <inheritdoc />
        public DateTime? NextAttemptTime { get; private set; }
        
        /// <inheritdoc />
        public bool IsReliable { get; }
        
        /// <inheritdoc />
        public int MaxDeliveryAttempts { get; }
        
        /// <summary>
        /// Initializes a new instance of the PendingDelivery class.
        /// </summary>
        /// <param name="message">The message being delivered.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="maxDeliveryAttempts">The maximum number of delivery attempts.</param>
        /// <param name="isReliable">Whether this is a reliable delivery.</param>
        /// <param name="completionSource">The completion source for the delivery task.</param>
        public PendingDelivery(
            IMessage message, 
            Guid deliveryId, 
            int maxDeliveryAttempts, 
            bool isReliable, 
            TaskCompletionSource<object> completionSource)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            DeliveryId = deliveryId;
            MaxDeliveryAttempts = maxDeliveryAttempts;
            IsReliable = isReliable;
            _completionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));
            
            Status = MessageDeliveryStatus.Queued;
            DeliveryAttempts = 0;
            FirstAttemptTime = DateTime.UtcNow;
            
            if (message is IReliableMessage reliableMessage)
            {
                NextAttemptTime = new DateTime(reliableMessage.NextAttemptTicks, DateTimeKind.Utc);
            }
        }
        
        /// <summary>
        /// Updates the delivery status in a thread-safe manner.
        /// </summary>
        /// <param name="status">The new status.</param>
        public void UpdateStatus(MessageDeliveryStatus status)
        {
            lock (_statusLock)
            {
                Status = status;
            }
        }
        
        /// <summary>
        /// Increments the delivery attempts counter.
        /// </summary>
        public void IncrementAttempts()
        {
            lock (_statusLock)
            {
                DeliveryAttempts++;
            }
        }
        
        /// <summary>
        /// Updates the next attempt time.
        /// </summary>
        /// <param name="nextAttemptTime">The next attempt time.</param>
        public void UpdateNextAttemptTime(DateTime nextAttemptTime)
        {
            lock (_statusLock)
            {
                NextAttemptTime = nextAttemptTime;
            }
        }
        
        /// <summary>
        /// Completes the delivery with a success result.
        /// </summary>
        /// <param name="result">The delivery result.</param>
        public void Complete(object result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            
            lock (_statusLock)
            {
                if (Status == MessageDeliveryStatus.Delivered || 
                    Status == MessageDeliveryStatus.Failed ||
                    Status == MessageDeliveryStatus.Cancelled ||
                    Status == MessageDeliveryStatus.Expired)
                {
                    // Already completed
                    return;
                }
                
                Status = MessageDeliveryStatus.Delivered;
            }
            
            _completionSource.TrySetResult(result);
        }
        
        /// <summary>
        /// Expires the delivery with a failure result.
        /// </summary>
        /// <param name="result">The failure result.</param>
        public void Expire(ReliableDeliveryResult result)
        {
            lock (_statusLock)
            {
                if (Status == MessageDeliveryStatus.Delivered || 
                    Status == MessageDeliveryStatus.Failed ||
                    Status == MessageDeliveryStatus.Cancelled ||
                    Status == MessageDeliveryStatus.Expired)
                {
                    // Already completed
                    return;
                }
                
                Status = MessageDeliveryStatus.Expired;
            }
            
            _completionSource.TrySetResult(result);
        }
        
        /// <summary>
        /// Cancels the delivery.
        /// </summary>
        public void Cancel()
        {
            lock (_statusLock)
            {
                if (Status == MessageDeliveryStatus.Delivered || 
                    Status == MessageDeliveryStatus.Failed ||
                    Status == MessageDeliveryStatus.Cancelled ||
                    Status == MessageDeliveryStatus.Expired)
                {
                    // Already completed
                    return;
                }
                
                Status = MessageDeliveryStatus.Cancelled;
            }
            
            if (IsReliable)
            {
                var result = ReliableDeliveryResult.Failure(
                    Message.Id,
                    DeliveryId,
                    DeliveryAttempts,
                    MessageDeliveryStatus.Cancelled,
                    "Delivery was cancelled");
                
                _completionSource.TrySetResult(result);
            }
            else
            {
                var result = DeliveryResult.Failure(Message.Id, DeliveryId, "Delivery was cancelled");
                _completionSource.TrySetResult(result);
            }
        }
    }
}