using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Messages
{
    /// <summary>
    /// Message published when a delivery service is created.
    /// Represents immutable data about the service creation event.
    /// </summary>
    public readonly record struct DeliveryServiceCreatedMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; }

        /// <inheritdoc />
        public long TimestampTicks { get; init; }

        /// <inheritdoc />
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the unique identifier of the created service.
        /// </summary>
        public Guid ServiceId { get; init; }

        /// <summary>
        /// Gets the type name of the created service.
        /// </summary>
        public string ServiceTypeName { get; init; }

        /// <summary>
        /// Gets the name of the created service.
        /// </summary>
        public string ServiceName { get; init; }

        /// <summary>
        /// Gets whether the service is currently active.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets when the service was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Gets the configuration used to create the service.
        /// </summary>
        public DeliveryServiceConfiguration Configuration { get; init; }
    }
}