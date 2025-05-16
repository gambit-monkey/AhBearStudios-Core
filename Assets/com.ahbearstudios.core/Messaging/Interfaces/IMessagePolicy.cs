namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for filtering messages based on a policy
    /// </summary>
    /// <typeparam name="TMessage">The type of message to filter</typeparam>
    public interface IMessagePolicy<in TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Applies the policy to a message
        /// </summary>
        /// <param name="message">The message to apply the policy to</param>
        /// <returns>True if the message should be processed; otherwise, false</returns>
        bool ApplyPolicy(TMessage message);
    }
}