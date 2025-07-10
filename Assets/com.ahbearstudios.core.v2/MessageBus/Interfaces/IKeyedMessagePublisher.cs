using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for publishing keyed messages.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    public interface IKeyedMessagePublisher<TKey, TMessage>
    {
        /// <summary>
        /// Publishes a message with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the message.</param>
        /// <param name="message">The message to publish.</param>
        void Publish(TKey key, TMessage message);
        
        /// <summary>
        /// Publishes a message asynchronously with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the message.</param>
        /// <param name="message">The message to publish.</param>
        /// <returns>An IDisposable that completes when all subscribers have processed the message.</returns>
        IDisposable PublishAsync(TKey key, TMessage message);
    }
}