using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Services;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Message published when delivery service status changes.
    /// </summary>
    public readonly record struct DeliveryServiceStatusChangedMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; }

        /// <inheritdoc />
        public long TimestampTicks { get; init; }

        /// <inheritdoc />
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the name of the delivery service.
        /// </summary>
        public string ServiceName { get; init; }

        /// <summary>
        /// Gets the previous status of the service.
        /// </summary>
        public DeliveryServiceStatus PreviousStatus { get; init; }

        /// <summary>
        /// Gets the new status of the service.
        /// </summary>
        public DeliveryServiceStatus NewStatus { get; init; }

        /// <summary>
        /// Gets when the status change occurred.
        /// </summary>
        public DateTime ChangeTime { get; init; }

        /// <summary>
        /// Gets the reason for the status change.
        /// </summary>
        public string Reason { get; init; }
    }
}