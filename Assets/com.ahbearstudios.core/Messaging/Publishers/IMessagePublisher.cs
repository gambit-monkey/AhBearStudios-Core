using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Publishers;

/// <summary>
    /// Specialized publisher interface for specific message types.
    /// Provides type-safe publishing operations with advanced features.
    /// </summary>
    /// <typeparam name="TMessage">The message type this publisher handles</typeparam>
    public interface IMessagePublisher<in TMessage> : IDisposable where TMessage : IMessage
    {
        /// <summary>
        /// Publishes a message synchronously.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        void Publish(TMessage message);

        /// <summary>
        /// Publishes a message asynchronously.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple messages in a batch operation.
        /// </summary>
        /// <param name="messages">The messages to publish</param>
        /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        void PublishBatch(IEnumerable<TMessage> messages);

        /// <summary>
        /// Publishes multiple messages in a batch operation asynchronously.
        /// </summary>
        /// <param name="messages">The messages to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        Task PublishBatchAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message conditionally based on a predicate.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="condition">The condition that must be true to publish</param>
        /// <returns>True if the message was published, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when message or condition is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        bool PublishIf(TMessage message, Func<bool> condition);

        /// <summary>
        /// Publishes a message conditionally based on a predicate asynchronously.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="condition">The async condition that must be true to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing true if the message was published, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when message or condition is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        Task<bool> PublishIfAsync(TMessage message, Func<Task<bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message with a specified delay.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="delay">The delay before publishing</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when delay is negative</exception>
        /// <exception cref="InvalidOperationException">Thrown when publisher is disposed</exception>
        Task PublishDelayedAsync(TMessage message, TimeSpan delay, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics for this specific publisher.
        /// </summary>
        /// <returns>Publisher-specific statistics</returns>
        PublisherStatistics GetStatistics();

        /// <summary>
        /// Gets whether this publisher is currently operational.
        /// </summary>
        bool IsOperational { get; }

        /// <summary>
        /// Gets the message type this publisher handles.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        /// Event raised when a message is successfully published.
        /// </summary>
        event EventHandler<MessagePublishedEventArgs> MessagePublished;

        /// <summary>
        /// Event raised when message publishing fails.
        /// </summary>
        event EventHandler<MessagePublishFailedEventArgs> MessagePublishFailed;
    }