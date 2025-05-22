using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Factory for creating message instances
    /// </summary>
    public interface IMessageFactory
    {
        /// <summary>
        /// Creates a message of the specified type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to create</typeparam>
        /// <param name="initializer">Optional action to initialize the message</param>
        /// <returns>A new message instance</returns>
        TMessage CreateMessage<TMessage>(Action<TMessage> initializer = null) where TMessage : IMessage, new();

        /// <summary>
        /// Creates a message of the specified type
        /// </summary>
        /// <param name="messageType">The type of message to create</param>
        /// <returns>A new message instance</returns>
        IMessage CreateMessage(Type messageType);
    }
}