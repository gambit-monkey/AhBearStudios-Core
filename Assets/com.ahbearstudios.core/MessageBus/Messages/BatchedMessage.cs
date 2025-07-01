using System;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
  /// <summary>
    /// Internal record struct for batched message processing.
    /// Provides immutable batch processing semantics.
    /// </summary>
    internal readonly record struct BatchedMessage
    {
        /// <summary>
        /// Gets the message to be delivered.
        /// </summary>
        public IMessage Message { get; init; }

        /// <summary>
        /// Gets the delivery type for this message.
        /// </summary>
        public DeliveryType DeliveryType { get; init; }

        /// <summary>
        /// Gets the delivery ID for tracking this message.
        /// </summary>
        public Guid DeliveryId { get; init; }

        /// <summary>
        /// Gets the time when this message was queued for batch processing.
        /// </summary>
        public DateTime QueuedTime { get; init; }

        /// <summary>
        /// Gets the priority of this message (lower values are higher priority).
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        /// Initializes a new instance of the BatchedMessage record struct.
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
}