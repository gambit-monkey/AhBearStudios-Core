using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Published when a message has been successfully delivered through the bus.
    /// </summary>
    /// <typeparam name="T">Type of the delivered message.</typeparam>
    public readonly struct DeliverySucceeded<T> where T : IMessage
    {
        /// <summary>
        /// The original message that was delivered.
        /// </summary>
        public T Message { get; }

        /// <summary>
        /// Unique identifier for this delivery attempt.
        /// </summary>
        public Guid DeliveryId { get; }

        /// <summary>
        /// UTC timestamp when the delivery completed.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Number of attempts made before successful delivery.
        /// </summary>
        public int Attempts { get; }

        /// <summary>
        /// Creates a new <see cref="DeliverySucceeded{T}"/> notification.
        /// </summary>
        public DeliverySucceeded(
            T message,
            Guid deliveryId,
            DateTime timestamp,
            int attempts)
        {
            Message    = message;
            DeliveryId = deliveryId;
            Timestamp  = timestamp;
            Attempts   = attempts;
        }
    }
}