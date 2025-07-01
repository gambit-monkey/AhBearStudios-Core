using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Message published when a message delivery fails.
    /// </summary>
    public readonly record struct MessageDeliveryFailedMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; }

        /// <inheritdoc />
        public long TimestampTicks { get; init; }

        /// <inheritdoc />
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the ID of the message that failed to deliver.
        /// </summary>
        public Guid FailedMessageId { get; init; }

        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        public Guid DeliveryId { get; init; }

        /// <summary>
        /// Gets the type name of the failed message.
        /// </summary>
        public string MessageType { get; init; }

        /// <summary>
        /// Gets the error message describing the failure.
        /// </summary>
        public string ErrorMessage { get; init; }

        /// <summary>
        /// Gets the type name of the exception that caused the failure.
        /// </summary>
        public string ExceptionType { get; init; }

        /// <summary>
        /// Gets when the failure occurred.
        /// </summary>
        public DateTime FailureTime { get; init; }

        /// <summary>
        /// Gets the number of delivery attempts.
        /// </summary>
        public int AttemptCount { get; init; }

        /// <summary>
        /// Gets whether delivery will be retried.
        /// </summary>
        public bool WillRetry { get; init; }

        /// <summary>
        /// Gets the name of the delivery service.
        /// </summary>
        public string ServiceName { get; init; }
    }
}