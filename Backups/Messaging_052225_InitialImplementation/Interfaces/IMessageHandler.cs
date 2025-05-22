namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for handling messages
    /// </summary>
    /// <typeparam name="TMessage">The type of message to handle</typeparam>
    public interface IMessageHandler<in TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Handles a message
        /// </summary>
        /// <param name="message">The message to handle</param>
        void Handle(TMessage message);
    }
}