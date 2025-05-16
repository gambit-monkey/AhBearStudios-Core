namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for throttling message publishing
    /// </summary>
    /// <typeparam name="TMessage">The type of message to throttle</typeparam>
    public interface IMessageThrottler<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Throttles a message publish request
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="publisher">The publisher to use</param>
        /// <param name="interval">The throttle interval (in seconds)</param>
        void Throttle(TMessage message, IMessagePublisher<TMessage> publisher, float interval);
    
        /// <summary>
        /// Cancels any pending throttled publishes
        /// </summary>
        void CancelPending();
    }
}