using System;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Services;

namespace AhBearStudios.Core.MessageBus.Factories
{
    /// <summary>
    /// Factory that walks Config → Service wiring, creating the bus instance.
    /// </summary>
    public sealed class MessageBusServiceFactory : IMessageBusServiceFactory
    {
        private readonly IMessageRegistry _registry;
        private readonly IMessageDeliveryServiceFactory _deliveryFactory;
        private readonly IMessageSerializerFactory _serializerFactory;

        /// <summary>
        /// Constructs the factory with all sub-factories injected.
        /// </summary>
        public MessageBusServiceFactory(
            IMessageRegistry registry,
            IMessageDeliveryServiceFactory deliveryFactory,
            IMessageSerializerFactory serializerFactory)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _deliveryFactory = deliveryFactory ?? throw new ArgumentNullException(nameof(deliveryFactory));
            _serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
        }

        /// <inheritdoc/>
        public IBurstMessageBusService Create(IMessageBusConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate(); // ensure config is sane

            // Build serializer
            var serializer = _serializerFactory.Create(config);

            // Build delivery service (batch or default, based on sub-configs)
            var delivery = _deliveryFactory.Create(config, _registry, serializer);

            // Instantiate the bus
            return new BurstMessageBusService(config, _registry, serializer, delivery);
        }
    }
}