using System;

namespace AhBearStudios.Core.MessageBus.Interfaces
{
    /// <summary>
    /// Interface for publishing messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    public interface IMessagePublisher<TMessage>
    {
        /// <summary>
        /// Publishes a message to all subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        void Publish(TMessage message);
        
        /// <summary>
        /// Publishes a message asynchronously to all subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <returns>An IDisposable that completes when all subscribers have processed the message.</returns>
        IDisposable PublishAsync(TMessage message);
    }
}