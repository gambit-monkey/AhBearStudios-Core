using System;

namespace AhBearStudios.Core.Messaging.Data
{
    /// <summary>
    /// Represents a message in the batch processing queue.
    /// </summary>
    internal sealed class BatchedMessage
    {
        /// <summary>
        /// Gets the message to be delivered.
        /// </summary>
        public IMessage Message { get; }

        /// <summary>
        /// Gets the delivery type for this message.
        /// </summary>
        public DeliveryType DeliveryType { get; }

        /// <summary>
        /// Gets the delivery ID for tracking this message.
        /// </summary>
        public Guid DeliveryId { get; }

        /// <summary>
        /// Gets the time when this message was queued for batch processing.
        /// </summary>
        public DateTime QueuedTime { get; }

        /// <summary>
        /// Gets the priority of this message (lower values are higher priority).
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Initializes a new instance of the BatchedMessage class.
        /// </summary>
        /// <param name="message">The message to be delivered.</param>
        /// <param name="deliveryType">The delivery type for this message.</param>
        /// <param name="deliveryId">The delivery ID for tracking this message.</param>
        /// <param name="priority">The priority of this message.</param>
        public BatchedMessage(IMessage message, DeliveryType deliveryType, Guid deliveryId, int priority = 0)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            DeliveryType = deliveryType;
            DeliveryId = deliveryId;
            Priority = priority;
            QueuedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Enumeration of delivery types for batched messages.
    /// </summary>
    internal enum DeliveryType
    {
        /// <summary>
        /// Fire-and-forget delivery with no confirmation required.
        /// </summary>
        FireAndForget,

        /// <summary>
        /// Delivery with confirmation required.
        /// </summary>
        WithConfirmation,

        /// <summary>
        /// Reliable delivery with retry logic.
        /// </summary>
        Reliable
    }
}