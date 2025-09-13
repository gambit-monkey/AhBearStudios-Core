using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Subscribers;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Interface for message subscription operations.
    /// Handles all subscription management, filtering, and subscriber lifecycle.
    /// Focused on subscription responsibilities only, following single responsibility principle.
    /// </summary>
    public interface IMessageSubscriptionService : IDisposable
    {
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
        IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage;

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
        IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, UniTask> handler) where TMessage : IMessage;

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

        #region Subscriber Management

        /// <summary>
        /// Gets a specialized subscriber for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Type-specific message subscriber</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Gets the count of active subscribers for a message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Number of active subscribers</returns>
        int GetSubscriberCount<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Gets the total count of all active subscribers.
        /// </summary>
        /// <returns>Total number of active subscribers</returns>
        int GetTotalSubscriberCount();

        #endregion

        #region Statistics and Diagnostics

        /// <summary>
        /// Gets subscription statistics for monitoring and diagnostics.
        /// </summary>
        /// <returns>Current subscription statistics</returns>
        MessageSubscriptionStatistics GetStatistics();

        /// <summary>
        /// Clears subscription statistics and resets counters.
        /// </summary>
        void ClearStatistics();

        #endregion

        #region Health and Status

        /// <summary>
        /// Gets the current health status of the subscription service.
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

        #region Subscription Lifecycle Events

        /// <summary>
        /// Event fired when a new subscription is created.
        /// </summary>
        event Action<Type, string> SubscriptionCreated;

        /// <summary>
        /// Event fired when a subscription is disposed.
        /// </summary>
        event Action<Type, string> SubscriptionDisposed;

        /// <summary>
        /// Event fired when a subscription processes a message successfully.
        /// </summary>
        event Action<Type, string, TimeSpan> MessageProcessed;

        /// <summary>
        /// Event fired when a subscription fails to process a message.
        /// </summary>
        event Action<Type, string, Exception> MessageProcessingFailed;

        #endregion
    }
}