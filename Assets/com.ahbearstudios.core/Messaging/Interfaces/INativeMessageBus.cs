using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a burst-compatible message bus
    /// </summary>
    /// <typeparam name="T">The type of message to publish or subscribe to</typeparam>
    public interface INativeMessageBus<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        /// Publishes a message to all subscribers
        /// </summary>
        /// <param name="message">The message to publish</param>
        void Publish(T message);
    
        /// <summary>
        /// Queues a message for later processing
        /// </summary>
        /// <param name="message">The message to queue</param>
        void QueueMessage(T message);
    
        /// <summary>
        /// Processes all queued messages
        /// </summary>
        void ProcessQueue();
    
        /// <summary>
        /// Subscribes to messages with a function pointer
        /// </summary>
        /// <param name="handler">The function pointer to call when a message is published</param>
        /// <returns>A handle that can be used to unsubscribe</returns>
        unsafe SubscriptionHandle Subscribe(delegate* managed<T, void> handler);
    
        /// <summary>
        /// Unsubscribes from messages using a subscription handle
        /// </summary>
        /// <param name="handle">The handle of the subscription to remove</param>
        void Unsubscribe(SubscriptionHandle handle);
    
        /// <summary>
        /// Gets the number of subscribers
        /// </summary>
        int SubscriberCount { get; }
    
        /// <summary>
        /// Gets the number of queued messages
        /// </summary>
        int QueueCount { get; }
    }
}