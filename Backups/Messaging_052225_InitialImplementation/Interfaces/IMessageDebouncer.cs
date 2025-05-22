namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for debouncing message publishing
    /// </summary>
    /// <typeparam name="TMessage">The type of message to debounce</typeparam>
    public interface IMessageDebouncer<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Debounces a message publish request
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="publisher">The publisher to use</param>
        /// <param name="delay">The debounce delay (in seconds)</param>
        void Debounce(TMessage message, IMessagePublisher<TMessage> publisher, float delay);
    
        /// <summary>
        /// Cancels any pending debounced publishes
        /// </summary>
        void CancelPending();
    }
}