using System;
using System.Threading.Tasks;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;
using AhBearStudios.Core.MessageBus.Services;

namespace AhBearStudios.Core.MessageBus.Data
{
    /// <summary>
    /// Represents a pending delivery in the batch-optimized service.
    /// </summary>
    internal sealed class PendingBatchDelivery : IPendingDelivery
    {
        private readonly TaskCompletionSource<object> _completionSource;
        
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
        /// Gets the delivery type for this pending delivery.
        /// </summary>
        public DeliveryType DeliveryType { get; }
        
        /// <summary>
        /// Initializes a new instance of the PendingBatchDelivery class.
        /// </summary>
        /// <param name="message">The message being delivered.</param>
        /// <param name="deliveryId">The delivery ID.</param>
        /// <param name="deliveryType">The delivery type.</param>
        /// <param name="completionSource">The completion source for the delivery task.</param>
        public PendingBatchDelivery(IMessage message, Guid deliveryId, DeliveryType deliveryType, TaskCompletionSource<object> completionSource)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            DeliveryId = deliveryId;
            DeliveryType = deliveryType;
            _completionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));
            
            Status = MessageDeliveryStatus.Queued;
            DeliveryAttempts = 0;
            FirstAttemptTime = DateTime.UtcNow;
            IsReliable = deliveryType == DeliveryType.Reliable;
            
            if (message is IReliableMessage reliableMessage)
            {
                MaxDeliveryAttempts = reliableMessage.MaxDeliveryAttempts;
                NextAttemptTime = new DateTime(reliableMessage.NextAttemptTicks, DateTimeKind.Utc);
            }
            else
            {
                MaxDeliveryAttempts = 1;
                NextAttemptTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Updates the delivery status.
        /// </summary>
        /// <param name="status">The new status.</param>
        public void UpdateStatus(MessageDeliveryStatus status)
        {
            Status = status;
        }
        
        /// <summary>
        /// Increments the delivery attempts counter.
        /// </summary>
        public void IncrementAttempts()
        {
            DeliveryAttempts++;
        }
        
        /// <summary>
        /// Completes the delivery with a success result.
        /// </summary>
        /// <param name="result">The delivery result.</param>
        public void Complete(object result)
        {
            Status = MessageDeliveryStatus.Delivered;
            _completionSource.TrySetResult(result);
        }
        
        /// <summary>
        /// Fails the delivery with an error result.
        /// </summary>
        /// <param name="result">The failure result.</param>
        public void Fail(object result)
        {
            Status = MessageDeliveryStatus.Failed;
            _completionSource.TrySetResult(result);
        }
        
        /// <summary>
        /// Cancels the delivery.
        /// </summary>
        public void Cancel()
        {
            Status = MessageDeliveryStatus.Cancelled;
            
            var result = DeliveryType == DeliveryType.Reliable
                ? (object)ReliableDeliveryResult.Failure(
                    Message.Id,
                    DeliveryId,
                    DeliveryAttempts,
                    MessageDeliveryStatus.Cancelled,
                    "Delivery was cancelled")
                : DeliveryResult.Failure(Message.Id, DeliveryId, "Delivery was cancelled");
            
            _completionSource.TrySetResult(result);
        }
    }
}