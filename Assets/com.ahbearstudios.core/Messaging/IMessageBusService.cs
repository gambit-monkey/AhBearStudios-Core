using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Subscribers;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Primary interface for the message bus service.
    /// Provides high-level messaging operations with type safety and performance optimization.
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
        /// <param name="filter">The async filter predicate</param>
        /// <param name="handler">The async message handler</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter or handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, Task<bool>> filter, Func<TMessage, Task> handler) where TMessage : IMessage;

        /// <summary>
        /// Subscribes to messages with a minimum priority level.
        /// </summary>
        /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
        /// <param name="handler">The message handler</param>
        /// <param name="minPriority">Minimum priority level</param>
        /// <returns>Disposable subscription handle</returns>
        /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage;

        #endregion

        #region Scoped Subscriptions

        /// <summary>
        /// Creates a message scope for automatic subscription cleanup.
        /// </summary>
        /// <returns>Message scope for subscription management</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        IMessageScope CreateScope();

        #endregion

        #region Diagnostics and Monitoring

        /// <summary>
        /// Gets comprehensive statistics for the message bus.
        /// </summary>
        /// <returns>Current message bus statistics</returns>
        MessageBusStatistics GetStatistics();

        /// <summary>
        /// Clears the message history and resets statistics.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when service is disposed</exception>
        void ClearMessageHistory();

        /// <summary>
        /// Gets the current health status of the message bus.
        /// </summary>
        /// <returns>Current health status</returns>
        HealthStatus GetHealthStatus();

        #endregion

        #region Service State

        /// <summary>
        /// Gets whether the message bus service is currently operational.
        /// </summary>
        bool IsOperational { get; }

        /// <summary>
        /// Gets the configuration used by this message bus instance.
        /// </summary>
        MessageBusConfig Configuration { get; }

        /// <summary>
        /// Gets the unique identifier for this message bus instance.
        /// </summary>
        Guid InstanceId { get; }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when a message is published through the bus.
        /// </summary>
        event EventHandler<MessageBusEventArgs> MessagePublished;

        /// <summary>
        /// Event raised when message publishing fails.
        /// </summary>
        event EventHandler<MessageBusErrorEventArgs> MessagePublishFailed;

        /// <summary>
        /// Event raised when a subscription is created.
        /// </summary>
        event EventHandler<SubscriptionEventArgs> SubscriptionCreated;

        /// <summary>
        /// Event raised when a subscription is disposed.
        /// </summary>
        event EventHandler<SubscriptionEventArgs> SubscriptionDisposed;

        /// <summary>
        /// Event raised when the health status changes.
        /// </summary>
        event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        #endregion
    }
}