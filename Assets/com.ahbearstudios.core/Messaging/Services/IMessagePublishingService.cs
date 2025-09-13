using System;
using System.Threading;
using AhBearStudios.Core.HealthChecking.Models;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Interface for message publishing operations.
    /// Handles synchronous and asynchronous message publishing with batching support.
    /// Focused on publishing responsibilities only, following single responsibility principle.
    /// </summary>
    public interface IMessagePublishingService : IDisposable
    {
        #region Core Publishing Operations

        /// <summary>
        /// Publishes a message synchronously to all subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or circuit breaker is open</exception>
        void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage;

        /// <summary>
        /// Publishes a message asynchronously to all subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or circuit breaker is open</exception>
        UniTask PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;

        /// <summary>
        /// Publishes multiple messages as a batch operation.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="messages">The messages to publish</param>
        /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or circuit breaker is open</exception>
        void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage;

        /// <summary>
        /// Publishes multiple messages as a batch operation asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="messages">The messages to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed or circuit breaker is open</exception>
        UniTask PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage;

        #endregion

        #region Publisher Management

        /// <summary>
        /// Gets a specialized publisher for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Type-specific message publisher</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage;

        #endregion

        #region Statistics and Diagnostics

        /// <summary>
        /// Gets publishing statistics for monitoring and diagnostics.
        /// </summary>
        /// <returns>Current publishing statistics</returns>
        MessagePublishingStatistics GetStatistics();

        /// <summary>
        /// Clears publishing statistics and resets counters.
        /// </summary>
        void ClearStatistics();

        #endregion

        #region Health and Status

        /// <summary>
        /// Gets the current health status of the publishing service.
        /// </summary>
        /// <returns>Current health status</returns>
        HealthStatus GetHealthStatus();

        /// <summary>
        /// Forces a health check evaluation and returns the result.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Health check result</returns>
        UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}