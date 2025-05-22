namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a message bus dispatcher that can batch and throttle messages
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Dispatches a message immediately
        /// </summary>
        /// <typeparam name="TMessage">The type of message to dispatch</typeparam>
        /// <param name="message">The message to dispatch</param>
        void DispatchImmediate<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Queues a message for later dispatch
        /// </summary>
        /// <typeparam name="TMessage">The type of message to dispatch</typeparam>
        /// <param name="message">The message to dispatch</param>
        void QueueMessage<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Dispatches all queued messages
        /// </summary>
        void DispatchQueuedMessages();
    
        /// <summary>
        /// Gets or sets the throttling interval for dispatches (in seconds)
        /// </summary>
        float ThrottlingInterval { get; set; }
    
        /// <summary>
        /// Gets the number of queued messages
        /// </summary>
        int QueuedMessageCount { get; }
    }
}