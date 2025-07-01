using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Message published when a message is successfully delivered.
    /// </summary>
    public readonly record struct MessageDeliveredMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; }

        /// <inheritdoc />
        public long TimestampTicks { get; init; }

        /// <inheritdoc />
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the ID of the delivered message.
        /// </summary>
        public Guid DeliveredMessageId { get; init; }

        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        public Guid DeliveryId { get; init; }

        /// <summary>
        /// Gets the type name of the delivered message.
        /// </summary>
        public string MessageType { get; init; }

        /// <summary>
        /// Gets when the message was delivered.
        /// </summary>
        public DateTime DeliveryTime { get; init; }

        /// <summary>
        /// Gets the number of delivery attempts.
        /// </summary>
        public int AttemptCount { get; init; }

        /// <summary>
        /// Gets the name of the delivery service.
        /// </summary>
        public string ServiceName { get; init; }
    }
}