using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a message bus that can handle multiple message types
    /// </summary>
    public interface ITypedMessageBus
    {
        /// <summary>
        /// Publishes a message of the specified type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish</typeparam>
        /// <param name="message">The message to publish</param>
        void Publish<TMessage>(TMessage message) where TMessage : IMessage;

        /// <summary>
        /// Publishes a message of the specified type asynchronously
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : IMessage;

        /// <summary>
        /// Subscribes to messages of the specified type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to</typeparam>
        /// <param name="handler">The handler to be called when a message is published</param>
        /// <returns>A token that can be disposed to unsubscribe</returns>
        ISubscriptionToken Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage;

        /// <summary>
        /// Subscribes to messages of the specified type with an asynchronous handler
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to</typeparam>
        /// <param name="handler">The async handler to be called when a message is published</param>
        /// <returns>A token that can be disposed to unsubscribe</returns>
        ISubscriptionToken SubscribeAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage;

        /// <summary>
        /// Checks if a message type is registered with this bus
        /// </summary>
        /// <typeparam name="TMessage">The type of message to check</typeparam>
        /// <returns>True if the message type is registered; otherwise, false</returns>
        bool IsMessageTypeRegistered<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Checks if a message type is registered with this bus
        /// </summary>
        /// <param name="messageType">The type of message to check</param>
        /// <returns>True if the message type is registered; otherwise, false</returns>
        bool IsMessageTypeRegistered(Type messageType);
    }
}