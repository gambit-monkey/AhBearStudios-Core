using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for subscribing to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    public interface IMessageSubscriber<TMessage>
    {
        /// <summary>
        /// Subscribes to messages of the specified type.
        /// </summary>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe(Action<TMessage> handler);
        
        /// <summary>
        /// Subscribes to messages of the specified type with a filter.
        /// </summary>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <param name="filter">A filter to determine if the message should be handled.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe(Action<TMessage> handler, Func<TMessage, bool> filter);
    }
}