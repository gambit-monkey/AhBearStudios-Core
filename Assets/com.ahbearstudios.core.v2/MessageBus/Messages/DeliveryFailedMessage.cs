using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Published when a message delivery attempt fails.
    /// </summary>
    /// <typeparam name="T">Type of the message that failed delivery.</typeparam>
    public readonly struct DeliveryFailed<T> where T : IMessage
    {
        /// <summary>
        /// The original message that failed to deliver.
        /// </summary>
        public T Message { get; }

        /// <summary>
        /// Unique identifier for this delivery attempt.
        /// </summary>
        public Guid DeliveryId { get; }

        /// <summary>
        /// Human-readable error message describing the failure.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Optional exception captured during delivery, if any.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Number of attempts made before failure.
        /// </summary>
        public int Attempts { get; }

        /// <summary>
        /// Indicates whether the system will retry the delivery.
        /// </summary>
        public bool WillRetry { get; }

        /// <summary>
        /// UTC timestamp when the failure was recorded.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Creates a new <see cref="DeliveryFailed{T}"/> notification.
        /// </summary>
        public DeliveryFailed(
            T message,
            Guid deliveryId,
            string error,
            Exception exception,
            int attempts,
            bool willRetry,
            DateTime timestamp)
        {
            Message    = message;
            DeliveryId = deliveryId;
            Error      = error;
            Exception  = exception;
            Attempts   = attempts;
            WillRetry  = willRetry;
            Timestamp  = timestamp;
        }
    }
}