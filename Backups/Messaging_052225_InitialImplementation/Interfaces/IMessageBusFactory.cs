using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Factory for creating message bus instances
    /// </summary>
    public interface IMessageBusFactory
    {
        /// <summary>
        /// Creates a message bus for the specified message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>A message bus for the specified message type</returns>
        IMessageBus<TMessage> CreateBus<TMessage>() where TMessage : IMessage;
    
        /// <summary>
        /// Creates a message bus for the specified message type
        /// </summary>
        /// <param name="messageType">The type of message</param>
        /// <returns>A message bus for the specified message type</returns>
        object CreateBus(Type messageType);
    
        /// <summary>
        /// Creates a typed message bus that can handle multiple message types
        /// </summary>
        /// <returns>A typed message bus</returns>
        ITypedMessageBus CreateTypedBus();
    }
}