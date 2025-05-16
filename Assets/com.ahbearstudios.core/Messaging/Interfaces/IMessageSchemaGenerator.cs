using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for generating message schemas
    /// </summary>
    public interface IMessageSchemaGenerator
    {
        /// <summary>
        /// Generates a schema for a message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>The generated schema</returns>
        MessageSchema GenerateSchema<TMessage>() where TMessage : IMessage;
    
        /// <summary>
        /// Generates a schema for a message type
        /// </summary>
        /// <param name="messageType">The type of message</param>
        /// <returns>The generated schema</returns>
        MessageSchema GenerateSchema(Type messageType);
    }
}