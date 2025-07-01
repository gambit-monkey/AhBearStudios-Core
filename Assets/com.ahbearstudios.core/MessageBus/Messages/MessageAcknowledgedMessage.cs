using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Message used for acknowledging delivery of other messages.
    /// </summary>
    public readonly record struct MessageAcknowledgedMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; }

        /// <inheritdoc />
        public long TimestampTicks { get; init; }

        /// <inheritdoc />
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the ID of the message being acknowledged.
        /// </summary>
        public Guid AcknowledgedMessageId { get; init; }

        /// <summary>
        /// Gets the delivery ID of the message being acknowledged.
        /// </summary>
        public Guid AcknowledgedDeliveryId { get; init; }

        /// <summary>
        /// Gets when the acknowledgment was sent.
        /// </summary>
        public DateTime AcknowledgmentTime { get; init; }

        /// <summary>
        /// Gets the name of the service sending the acknowledgment.
        /// </summary>
        public string ServiceName { get; init; }
    }
}