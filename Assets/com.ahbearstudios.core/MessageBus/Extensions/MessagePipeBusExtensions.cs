using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.MessageBus.Extensions
{
    /// <summary>
    /// Extension methods for the message bus system.
    /// </summary>
    public static class MessageBusExtensions
    {
        /// <summary>
        /// Publishes a message of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="message">The message to publish.</param>
        public static void Publish<TMessage>(this IMessageBus messageBus, TMessage message)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            messageBus.GetPublisher<TMessage>().Publish(message);
        }
        
        /// <summary>
        /// Publishes a keyed message of the specified type.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of message to publish.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="key">The key associated with the message.</param>
        /// <param name="message">The message to publish.</param>
        public static void PublishKeyed<TKey, TMessage>(this IMessageBus messageBus, TKey key, TMessage message)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            messageBus.GetPublisher<TKey, TMessage>().Publish(key, message);
        }
        
        /// <summary>
        /// Subscribes to messages of the specified type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable Subscribe<TMessage>(this IMessageBus messageBus, Action<TMessage> handler)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            return messageBus.GetSubscriber<TMessage>().Subscribe(handler);
        }
        
        /// <summary>
        /// Subscribes to messages of the specified type with a filter.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <param name="filter">A filter to determine if the message should be handled.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable Subscribe<TMessage>(this IMessageBus messageBus, Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            return messageBus.GetSubscriber<TMessage>().Subscribe(handler, filter);
        }
        
        /// <summary>
        /// Subscribes to keyed messages of the specified type with a specific key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="key">The key to subscribe to.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable SubscribeKeyed<TKey, TMessage>(this IMessageBus messageBus, TKey key, Action<TMessage> handler)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            return messageBus.GetSubscriber<TKey, TMessage>().Subscribe(key, handler);
        }
        
        /// <summary>
        /// Subscribes to all keyed messages of the specified type regardless of key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable SubscribeKeyedAll<TKey, TMessage>(this IMessageBus messageBus, Action<TKey, TMessage> handler)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            return messageBus.GetSubscriber<TKey, TMessage>().Subscribe(handler);
        }
        
        /// <summary>
        /// Subscribes to keyed messages of the specified type with a filter.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="key">The key to subscribe to.</param>
        /// <param name="handler">The handler to invoke when a message is received.</param>
        /// <param name="filter">A filter to determine if the message should be handled.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        public static IDisposable SubscribeKeyed<TKey, TMessage>(this IMessageBus messageBus, TKey key, Action<TMessage> handler, Func<TMessage, bool> filter)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));
            return messageBus.GetSubscriber<TKey, TMessage>().Subscribe(key, handler, filter);
        }
    }
}