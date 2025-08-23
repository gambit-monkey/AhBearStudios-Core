using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Subscribers;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating MessageSubscriber instances.
/// Follows CLAUDE.md factory pattern - simple creation only, no lifecycle management.
/// Does NOT implement IDisposable as factories are stateless creation utilities.
/// </summary>
public interface IMessageSubscriberFactory
{
    /// <summary>
    /// Creates a message subscriber for the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <param name="config">The validated configuration for the subscriber</param>
    /// <returns>A new message subscriber instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber creation fails</exception>
    UniTask<IMessageSubscriber<TMessage>> CreateSubscriberAsync<TMessage>(MessageSubscriberConfig config)
        where TMessage : IMessage;

    /// <summary>
    /// Creates a message subscriber with default configuration.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <returns>A new message subscriber instance with default configuration</returns>
    /// <exception cref="InvalidOperationException">Thrown when subscriber creation fails</exception>
    UniTask<IMessageSubscriber<TMessage>> CreateDefaultSubscriberAsync<TMessage>()
        where TMessage : IMessage;

    /// <summary>
    /// Gets whether the factory can create subscribers for the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type to check</typeparam>
    /// <returns>True if the factory supports the message type</returns>
    bool SupportsMessageType<TMessage>() where TMessage : IMessage;

    /// <summary>
    /// Gets whether the factory can create subscribers for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to check</param>
    /// <returns>True if the factory supports the message type</returns>
    bool SupportsMessageType(Type messageType);

    /// <summary>
    /// Gets the supported message types for this factory.
    /// </summary>
    /// <returns>Collection of supported message types</returns>
    IEnumerable<Type> GetSupportedMessageTypes();

    /// <summary>
    /// Validates a configuration before creating a subscriber.
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    /// <returns>True if the configuration is valid for this factory</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    bool ValidateConfig(MessageSubscriberConfig config);
}