namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for logging message activity
    /// </summary>
    public interface IMessageLogger
    {
        /// <summary>
        /// Logs a message being published
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message being published</param>
        void LogPublish<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Logs a message being subscribed to
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="handler">The handler subscribing to the message</param>
        /// <param name="token">The subscription token</param>
        void LogSubscribe<TMessage>(object handler, ISubscriptionToken token) where TMessage : IMessage;
    
        /// <summary>
        /// Logs a message being delivered to a subscriber
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message being delivered</param>
        /// <param name="token">The subscription token</param>
        void LogDeliver<TMessage>(TMessage message, ISubscriptionToken token) where TMessage : IMessage;
    
        /// <summary>
        /// Logs a message being unsubscribed from
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="token">The subscription token</param>
        void LogUnsubscribe<TMessage>(ISubscriptionToken token) where TMessage : IMessage;
    
        /// <summary>
        /// Gets or sets the logging level
        /// </summary>
        MessageLogLevel LogLevel { get; set; }
    }
}