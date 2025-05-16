namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for notifications when a message's lifetime starts or ends
    /// </summary>
    public interface IMessageLifetimeNotifications
    {
        /// <summary>
        /// Called when a message's lifetime starts
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message</param>
        void OnMessageLifetimeStarted<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Called when a message's lifetime ends
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message</param>
        void OnMessageLifetimeEnded<TMessage>(TMessage message) where TMessage : IMessage;
    }
}