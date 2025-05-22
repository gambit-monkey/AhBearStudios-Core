using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for handling errors during message processing
    /// </summary>
    public interface IMessageErrorHandler
    {
        /// <summary>
        /// Handles an error that occurred during message publishing
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message being published</param>
        /// <param name="exception">The exception that occurred</param>
        /// <returns>True if the error was handled; otherwise, false</returns>
        bool HandlePublishError<TMessage>(TMessage message, Exception exception) where TMessage : IMessage;
    
        /// <summary>
        /// Handles an error that occurred during message delivery
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message being delivered</param>
        /// <param name="handler">The handler that threw the exception</param>
        /// <param name="exception">The exception that occurred</param>
        /// <returns>True if the error was handled; otherwise, false</returns>
        bool HandleDeliveryError<TMessage>(TMessage message, object handler, Exception exception) where TMessage : IMessage;
    }
}