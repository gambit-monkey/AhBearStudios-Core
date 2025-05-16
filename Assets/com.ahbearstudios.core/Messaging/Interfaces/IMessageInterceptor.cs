namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for intercepting messages before they're published
    /// </summary>
    /// <typeparam name="TMessage">The type of message to intercept</typeparam>
    public interface IMessageInterceptor<TMessage> where TMessage : IMessage
    {
        /// <summary>
        /// Intercepts a message before it's published
        /// </summary>
        /// <param name="message">The message to intercept</param>
        /// <param name="context">The interception context</param>
        /// <returns>True if the message should be published; otherwise, false</returns>
        bool Intercept(TMessage message, MessageInterceptionContext context);
    }
}