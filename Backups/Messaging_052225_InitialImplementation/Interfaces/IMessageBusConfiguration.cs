using System;
using AhBearStudios.Core.Messaging.Configuration;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for configuring message buses
    /// </summary>
    public interface IMessageBusConfiguration
    {
        /// <summary>
        /// Configures message buses with the specified options
        /// </summary>
        /// <param name="options">The options to configure with</param>
        void Configure(MessageBusOptions options);
    
        /// <summary>
        /// Gets the options for a specific message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <returns>The options for the message type</returns>
        MessageBusOptions GetOptionsForMessageType<TMessage>() where TMessage : IMessage;
    
        /// <summary>
        /// Gets the options for a specific message type
        /// </summary>
        /// <param name="messageType">The type of message</param>
        /// <returns>The options for the message type</returns>
        MessageBusOptions GetOptionsForMessageType(Type messageType);
    
        /// <summary>
        /// Configures options for a specific message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message</typeparam>
        /// <param name="configureAction">The action to configure the options</param>
        void ConfigureForMessageType<TMessage>(Action<MessageBusOptions> configureAction) where TMessage : IMessage;
    }
}