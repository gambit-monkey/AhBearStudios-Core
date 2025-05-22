using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a UI message bus that ensures UI updates happen on the main thread
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IUIMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Subscribes to messages and ensures the handler is called on the main thread
        /// </summary>
        /// <param name="handler">The handler to be called when a message is published</param>
        /// <returns>A token that can be used to unsubscribe</returns>
        ISubscriptionToken SubscribeOnMainThread(Action<TMessage> handler);
    
        /// <summary>
        /// Gets or sets the throttling interval for UI updates (in seconds)
        /// </summary>
        float ThrottlingInterval { get; set; }
    
        /// <summary>
        /// Gets or sets a value indicating whether to batch UI updates
        /// </summary>
        bool BatchUpdates { get; set; }
    }
}