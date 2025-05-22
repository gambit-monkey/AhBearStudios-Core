using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.MessageBuses;

namespace AhBearStudios.Core.Messaging.Factories
{
    /// <summary>
    /// Factory extension for creating schema-validating message buses
    /// </summary>
    public static class SchemaValidatingMessageBusFactory
    {
        public static IMessageBus<TMessage> CreateSchemaValidatingBus<TMessage>(
            this IMessageBusFactory factory,
            IMessageSchemaGenerator schemaGenerator,
            IBurstLogger logger = null) 
            where TMessage : IMessage
        {
            var innerBus = factory.CreateBus<TMessage>();
            return new SchemaValidatingMessageBus<TMessage>(innerBus, schemaGenerator, logger);
        }
    }
}