using System;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for subscribing to messages
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to</typeparam>
    public interface IMessageSubscriber<out TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Subscribes to messages of the specified type
        /// </summary>
        /// <param name="handler">The handler to be called when a message is published</param>
        /// <returns>A token that can be disposed to unsubscribe</returns>
        ISubscriptionToken Subscribe(Action<TMessage> handler);

        /// <summary>
        /// Subscribes to messages of the specified type with an asynchronous handler
        /// </summary>
        /// <param name="handler">The async handler to be called when a message is published</param>
        /// <returns>A token that can be disposed to unsubscribe</returns>
        ISubscriptionToken SubscribeAsync(Func<TMessage, Task> handler);
    }
}