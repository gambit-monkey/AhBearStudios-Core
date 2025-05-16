using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.MessageBuses;

namespace AhBearStudios.Core.Messaging.Factories
{
    /// <summary>
    /// Factory extension for creating hierarchical message buses
    /// </summary>
    public static class HierarchicalMessageBusFactoryExtensions
    {
        public static IHierarchicalMessageBus<TMessage> CreateHierarchicalBus<TMessage>(
            this IMessageBusFactory factory,
            MessagePropagationMode propagationMode = MessagePropagationMode.Bidirectional) 
            where TMessage : IMessage
        {
            var innerBus = factory.CreateBus<TMessage>();
            return new HierarchicalMessageBus<TMessage>(innerBus, propagationMode);
        }
    }
}