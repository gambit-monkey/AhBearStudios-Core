using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Subscribers;

/// <summary>
/// Specialized subscriber interface for specific message types.
/// Provides type-safe subscription operations with advanced filtering and priority handling.
/// </summary>
/// <typeparam name="TMessage">The message type this subscriber handles</typeparam>
public interface IMessageSubscriber<out TMessage> : IDisposable where TMessage : IMessage
{
    /// <summary>
    /// Subscribes to messages with a synchronous handler.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable Subscribe(Action<TMessage> handler);

    /// <summary>
    /// Subscribes to messages with an asynchronous handler.
    /// </summary>
    /// <param name="handler">The async message handler</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeAsync(Func<TMessage, Task> handler);

    /// <summary>
    /// Subscribes to messages with a minimum priority level.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <param name="minPriority">Minimum priority level to receive</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeWithPriority(Action<TMessage> handler, MessagePriority minPriority);

    /// <summary>
    /// Subscribes to messages with a conditional filter.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <param name="condition">The condition that must be true to receive the message</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or condition is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeConditional(Action<TMessage> handler, Func<TMessage, bool> condition);

    /// <summary>
    /// Subscribes to messages with an async conditional filter.
    /// </summary>
    /// <param name="handler">The async message handler</param>
    /// <param name="condition">The async condition that must be true to receive the message</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or condition is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeConditionalAsync(Func<TMessage, Task> handler, Func<TMessage, Task<bool>> condition);

    /// <summary>
    /// Subscribes to messages from a specific source.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <param name="source">The source system to filter by</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeFromSource(Action<TMessage> handler, string source);

    /// <summary>
    /// Subscribes to messages with a correlation ID filter.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <param name="correlationId">The correlation ID to filter by</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeWithCorrelation(Action<TMessage> handler, Guid correlationId);

    /// <summary>
    /// Subscribes to messages with error handling.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <param name="errorHandler">The error handler for exceptions in the message handler</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or errorHandler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeWithErrorHandling(Action<TMessage> handler, Action<Exception, TMessage> errorHandler);

    /// <summary>
    /// Unsubscribes all active subscriptions for this subscriber.
    /// </summary>
    void UnsubscribeAll();

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    int ActiveSubscriptions { get; }

    /// <summary>
    /// Gets whether this subscriber is currently operational.
    /// </summary>
    bool IsOperational { get; }

    /// <summary>
    /// Gets the message type this subscriber handles.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Gets statistics for this specific subscriber.
    /// </summary>
    /// <returns>Subscriber-specific statistics</returns>
    SubscriberStatistics GetStatistics();

    /// <summary>
    /// Event raised when a message is successfully processed.
    /// </summary>
    event EventHandler<MessageProcessedEventArgs> MessageProcessed;

    /// <summary>
    /// Event raised when message processing fails.
    /// </summary>
    event EventHandler<MessageProcessingFailedEventArgs> MessageProcessingFailed;

    /// <summary>
    /// Event raised when a subscription is created.
    /// </summary>
    event EventHandler<SubscriptionCreatedEventArgs> SubscriptionCreated;

    /// <summary>
    /// Event raised when a subscription is disposed.
    /// </summary>
    event EventHandler<SubscriptionDisposedEventArgs> SubscriptionDisposed;
}