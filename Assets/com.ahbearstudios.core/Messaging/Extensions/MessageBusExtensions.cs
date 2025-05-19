using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging.Extensions
{
    /// <summary>
    /// Extension methods for IMessageBus
    /// </summary>
    public static class MessageBusExtensions
    {
        /// <summary>
        /// Creates and publishes a message of the specified type.
        /// </summary>
        /// <typeparam name="TBus">Type of the message bus.</typeparam>
        /// <typeparam name="TMessage">Base message type for the bus.</typeparam>
        /// <typeparam name="T">Specific message type to create and publish.</typeparam>
        /// <param name="bus">The message bus instance.</param>
        /// <param name="factory">Factory function to create the message.</param>
        public static void Publish<TBus, TMessage, T>(this TBus bus, Func<T> factory)
            where TBus : IMessageBus<TMessage>
            where TMessage : IMessage
            where T : TMessage
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
                
            var message = factory();
            bus.Publish(message);
        }
        
        /// <summary>
        /// Creates and publishes a message of the specified type asynchronously.
        /// </summary>
        /// <typeparam name="TBus">Type of the message bus.</typeparam>
        /// <typeparam name="TMessage">Base message type for the bus.</typeparam>
        /// <typeparam name="T">Specific message type to create and publish.</typeparam>
        /// <param name="bus">The message bus instance.</param>
        /// <param name="factory">Factory function to create the message.</param>
        /// <param name="cancellationToken">Optional token to cancel async operations.</param>
        /// <returns>Task that completes when all async handlers have been invoked.</returns>
        public static Task PublishAsync<TBus, TMessage, T>(this TBus bus, Func<T> factory, CancellationToken cancellationToken = default)
            where TBus : IMessageBus<TMessage>
            where TMessage : IMessage
            where T : TMessage
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
                
            var message = factory();
            return bus.PublishAsync(message, cancellationToken);
        }
        
        /// <summary>
        /// Unsubscribes and disposes the token.
        /// </summary>
        /// <typeparam name="TBus">Type of the message bus.</typeparam>
        /// <typeparam name="TMessage">Base message type for the bus.</typeparam>
        /// <param name="bus">The message bus instance.</param>
        /// <param name="token">Token to unsubscribe.</param>
        public static void UnsubscribeAndDispose<TBus, TMessage>(this TBus bus, ISubscriptionToken token)
            where TBus : IMessageBus<TMessage>
            where TMessage : IMessage
        {
            if (bus == null || token == null)
                return;
                
            bus.Unsubscribe(token);
            token.Dispose();
        }
    }
}