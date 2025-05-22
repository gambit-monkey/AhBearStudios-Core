using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for subscribing to keyed messages.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    public interface IKeyedMessageSubscriber<TKey, TMessage>
    {
        /// <summary>
        /// Subscribes to messages with the specified key.
        /// </summary>
        /// <param name="key">The key to subscribe to.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe(TKey key, Action<TMessage> handler);
        
        /// <summary>
        /// Subscribes to all messages regardless of key.
        /// </summary>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe(Action<TKey, TMessage> handler);
        
        /// <summary>
        /// Subscribes to messages with the specified key and filter.
        /// </summary>
        /// <param name="key">The key to subscribe to.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <param name="filter">A filter to determine if the message should be handled.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe(TKey key, Action<TMessage> handler, Func<TMessage, bool> filter);
    }
}