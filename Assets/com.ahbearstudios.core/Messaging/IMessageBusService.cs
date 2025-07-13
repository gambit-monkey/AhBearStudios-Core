using System;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Subscribers;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Primary interface for the message bus service.
    /// Provides high-level messaging operations with type safety, performance optimization, and comprehensive monitoring.
    /// Integrates with all required AhBearStudios Core systems for production readiness.
    /// </summary>
    public interface IMessageBusService : IDisposable
    {
        #region Core Publishing Operations

        /// <summary>
        /// Publishes a message synchronously to all subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage;

        /// <summary>
        /// Publishes a message asynchronously to all subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        Task PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;

        /// <summary>
        /// Publishes multiple messages as a batch operation.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="messages">The messages to publish</param>
        /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage;

        /// <summary>
        /// Publishes multiple messages as a batch operation asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="messages">The messages to publish</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when messages is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        Task PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage;

        #endregion

        #region Core Subscription Operations

        /// <summary>
        /// Subscribes to messages with a synchronous handler.
        /// </summary>
        /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
        /// <param name="handler">The message handler</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage;

        /// <summary>
        /// Subscribes to messages with an asynchronous handler.
        /// </summary>
        /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
        /// <param name="handler">The async message handler</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage;

        #endregion

        #region Advanced Operations

        /// <summary>
        /// Gets a specialized publisher for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Type-specific message publisher</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Gets a specialized subscriber for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Type-specific message subscriber</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage;

        #endregion

        #region Filtering and Routing

        /// <summary>
        /// Subscribes to messages with a conditional filter.
        /// </summary>
        /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
        /// <param name="filter">The filter predicate</param>
        /// <param name="handler">The message handler</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter or handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage;

        /// <summary>
        /// Subscribes to messages with an async conditional filter.
        /// </summary>
        /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
        /// <param name="filter">The filter predicate</param>
        /// <param name="handler">The async message handler</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter or handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, Task> handler) where TMessage : IMessage;

        /// <summary>
        /// Subscribes to messages with priority filtering.
        /// </summary>
        /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
        /// <param name="handler">The message handler</param>
        /// <param name="minPriority">Minimum message priority to process</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage;

        #endregion

        #region Scoped Subscriptions

        /// <summary>
        /// Creates a message scope for automatic subscription cleanup.
        /// </summary>
        /// <returns>Message scope for scoped subscription management</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IMessageScope CreateScope();

        #endregion

        #region Diagnostics and Management

        /// <summary>
        /// Gets comprehensive statistics about message bus performance and health.
        /// </summary>
        /// <returns>Current message bus statistics</returns>
        MessageBusStatistics GetStatistics();

        /// <summary>
        /// Clears message history and resets statistics counters.
        /// </summary>
        void ClearMessageHistory();

        /// <summary>
        /// Gets the current health status of the message bus.
        /// </summary>
        /// <returns>Current health status</returns>
        HealthStatus GetHealthStatus();

        /// <summary>
        /// Forces a health check evaluation and returns the result.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Health check result</returns>
        Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Circuit Breaker Operations

        /// <summary>
        /// Gets the current circuit breaker state for message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Circuit breaker state</returns>
        CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Manually resets the circuit breaker for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        void ResetCircuitBreaker<TMessage>() where TMessage : IMessage;

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the health status changes.
        /// </summary>
        event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        /// <summary>
        /// Event raised when a message processing fails.
        /// </summary>
        event EventHandler<MessageProcessingFailedEventArgs> MessageProcessingFailed;

        /// <summary>
        /// Event raised when circuit breaker state changes.
        /// </summary>
        event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;

        #endregion
    }
}