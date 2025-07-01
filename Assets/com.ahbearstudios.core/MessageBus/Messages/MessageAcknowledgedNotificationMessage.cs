using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Message published when a message acknowledgment is received (notification type).
    /// </summary>
    public readonly record struct MessageAcknowledgedNotificationMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; }

        /// <inheritdoc />
        public long TimestampTicks { get; init; }

        /// <inheritdoc />
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the ID of the acknowledged message.
        /// </summary>
        public Guid AcknowledgedMessageId { get; init; }

        /// <summary>
        /// Gets the delivery ID.
        /// </summary>
        public Guid DeliveryId { get; init; }

        /// <summary>
        /// Gets when the acknowledgment was received.
        /// </summary>
        public DateTime AcknowledgmentTime { get; init; }

        /// <summary>
        /// Gets the name of the delivery service.
        /// </summary>
        public string ServiceName { get; init; }
    }
}