namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for a reliable message bus that guarantees message delivery
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish or subscribe to</typeparam>
    public interface IReliableMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Gets the number of pending messages that have not been confirmed
        /// </summary>
        int PendingMessageCount { get; }
    
        /// <summary>
        /// Publishes a message with guaranteed delivery
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns>A unique identifier for tracking the message</returns>
        string PublishReliable(TMessage message);
    
        /// <summary>
        /// Confirms that a message has been successfully processed
        /// </summary>
        /// <param name="messageId">The ID of the message to confirm</param>
        /// <returns>True if the message was confirmed; otherwise, false</returns>
        bool ConfirmMessage(string messageId);
    
        /// <summary>
        /// Redelivers all pending messages that have not been confirmed
        /// </summary>
        /// <returns>The number of messages redelivered</returns>
        int RedeliverPendingMessages();
    }
}