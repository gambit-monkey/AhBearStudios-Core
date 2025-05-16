namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for filtering messages
    /// </summary>
    /// <typeparam name="TMessage">The type of message to filter</typeparam>
    public interface IMessageFilter<in TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Determines whether a message should be processed
        /// </summary>
        /// <param name="message">The message to filter</param>
        /// <returns>True if the message should be processed; otherwise, false</returns>
        bool ShouldProcess(TMessage message);
    }
}