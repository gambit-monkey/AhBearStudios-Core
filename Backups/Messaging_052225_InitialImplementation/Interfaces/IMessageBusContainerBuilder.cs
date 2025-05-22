namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for integrating with a dependency injection container
    /// </summary>
    public interface IMessageBusContainerBuilder
    {
        /// <summary>
        /// Registers the message bus system with the container
        /// </summary>
        /// <param name="containerBuilder">The container builder</param>
        void RegisterWithContainer(object containerBuilder);
    
        /// <summary>
        /// Registers a message type with the container
        /// </summary>
        /// <typeparam name="TMessage">The type of message to register</typeparam>
        /// <param name="containerBuilder">The container builder</param>
        /// <param name="lifetime">The lifetime of the message bus</param>
        void RegisterMessageType<TMessage>(object containerBuilder, string lifetime = "Singleton") where TMessage : IMessage;
    
        /// <summary>
        /// Registers a message handler with the container
        /// </summary>
        /// <typeparam name="TMessage">The type of message to handle</typeparam>
        /// <typeparam name="THandler">The type of handler to register</typeparam>
        /// <param name="containerBuilder">The container builder</param>
        /// <param name="lifetime">The lifetime of the handler</param>
        void RegisterMessageHandler<TMessage, THandler>(object containerBuilder, string lifetime = "Transient") 
            where TMessage : IMessage 
            where THandler : IMessageHandler<TMessage>;
    }
}