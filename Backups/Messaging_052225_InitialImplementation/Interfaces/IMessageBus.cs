using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Defines a message bus that can publish messages and handle subscriptions.
    /// </summary>
    /// <typeparam name="TMessage">The base type of messages this bus will handle.</typeparam>
    public interface IMessageBus<TMessage> : IDisposable where TMessage : IMessage
    {
        /// <summary>
        /// Subscribes to messages of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of message to subscribe to.</typeparam>
        /// <param name="handler">Handler to invoke when message is received.</param>
        /// <returns>Subscription token that can be used to unsubscribe.</returns>
        ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage;
        
        /// <summary>
        /// Subscribes to messages of a specific type with an asynchronous handler.
        /// </summary>
        /// <typeparam name="T">Type of message to subscribe to.</typeparam>
        /// <param name="handler">Async handler to invoke when message is received.</param>
        /// <returns>Subscription token that can be used to unsubscribe.</returns>
        ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage;
        
        /// <summary>
        /// Subscribes to all messages on this bus.
        /// </summary>
        /// <param name="handler">Handler to invoke when any message is received.</param>
        /// <returns>Subscription token that can be used to unsubscribe.</returns>
        ISubscriptionToken SubscribeToAll(Action<TMessage> handler);
        
        /// <summary>
        /// Subscribes to all messages on this bus with an asynchronous handler.
        /// </summary>
        /// <param name="handler">Async handler to invoke when any message is received.</param>
        /// <returns>Subscription token that can be used to unsubscribe.</returns>
        ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler);
        
        /// <summary>
        /// Unsubscribes using the provided token.
        /// </summary>
        /// <param name="token">Token returned from a previous subscription.</param>
        void Unsubscribe(ISubscriptionToken token);
        
        /// <summary>
        /// Publishes a message to all subscribers.
        /// </summary>
        /// <param name="message">Message to publish.</param>
        void Publish(TMessage message);
        
        /// <summary>
        /// Publishes a message to all subscribers, including async subscribers.
        /// </summary>
        /// <param name="message">Message to publish.</param>
        /// <param name="cancellationToken">Optional token to cancel async operations.</param>
        /// <returns>Task that completes when all async handlers have been invoked.</returns>
        Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    }
}