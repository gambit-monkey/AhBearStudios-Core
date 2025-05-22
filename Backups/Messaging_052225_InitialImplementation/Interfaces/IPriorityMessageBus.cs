using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a message bus that supports prioritized messages
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IPriorityMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IPriorityMessage
    {
        /// <summary>
        /// Subscribes to messages with a specific priority
        /// </summary>
        /// <param name="handler">The handler to be called when a message is published</param>
        /// <param name="priority">The priority of messages to subscribe to</param>
        /// <returns>A token that can be used to unsubscribe</returns>
        ISubscriptionToken SubscribeWithPriority(Action<TMessage> handler, MessagePriority priority);
    
        /// <summary>
        /// Subscribes to messages with a minimum priority
        /// </summary>
        /// <param name="handler">The handler to be called when a message is published</param>
        /// <param name="minimumPriority">The minimum priority of messages to subscribe to</param>
        /// <returns>A token that can be used to unsubscribe</returns>
        ISubscriptionToken SubscribeWithMinimumPriority(Action<TMessage> handler, MessagePriority minimumPriority);
    }
}