using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Core interface for the message bus system.
    /// Provides access to publishers and subscribers for different message types.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Gets a publisher for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish.</typeparam>
        /// <returns>A publisher for the specified message type.</returns>
        IMessagePublisher<TMessage> GetPublisher<TMessage>();
        
        /// <summary>
        /// Gets a subscriber for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <returns>A subscriber for the specified message type.</returns>
        IMessageSubscriber<TMessage> GetSubscriber<TMessage>();
        
        /// <summary>
        /// Gets a publisher for the specified message type with a key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of message to publish.</typeparam>
        /// <returns>A keyed publisher for the specified message type.</returns>
        IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>();
        
        /// <summary>
        /// Gets a subscriber for the specified message type with a key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <returns>A keyed subscriber for the specified message type.</returns>
        IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>();
        
        /// <summary>
        /// Clears all cached publishers and subscribers.
        /// </summary>
        void ClearCaches();
        
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage;
        
        /// <summary>
        /// Subscribes to messages of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage;
        
        /// <summary>
        /// Subscribes to all messages.
        /// </summary>
        /// <param name="handler">The handler to invoke when any message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable SubscribeToAllMessages(Action<IMessage> handler);
        
        /// <summary>
        /// Gets the message registry.
        /// </summary>
        /// <returns>The message registry.</returns>
        IMessageRegistry GetMessageRegistry();
    }
}