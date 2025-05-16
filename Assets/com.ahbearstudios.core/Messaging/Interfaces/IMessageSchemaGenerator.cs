using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Data;
using UnityEngine;

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

        /// <summary>
        /// Validates a message against its schema
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="message">The message to validate</param>
        /// <returns>The validation result</returns>
        SchemaValidationResult ValidateMessage<TMessage>(TMessage message) where TMessage : IMessage;

        /// <summary>
        /// Validates a message against its schema
        /// </summary>
        /// <param name="message">The message to validate</param>
        /// <param name="messageType">The type of message</param>
        /// <returns>The validation result</returns>
        SchemaValidationResult ValidateMessage(object message, Type messageType);
    }
}