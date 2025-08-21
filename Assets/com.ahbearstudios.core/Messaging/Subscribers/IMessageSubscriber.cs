using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Subscribers;

/// <summary>
/// Specialized subscriber interface for specific message types.
/// Provides type-safe subscription operations following CLAUDE.md guidelines.
/// Simplified interface avoiding enterprise anti-patterns with IMessage compliance.
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
    /// Subscribes to messages with an asynchronous handler using UniTask.
    /// </summary>
    /// <param name="handler">The async message handler</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeAsync(Func<TMessage, UniTask> handler);

    /// <summary>
    /// Subscribes to messages with a conditional filter.
    /// Combines priority, source, correlation, and custom filtering into one flexible method.
    /// </summary>
    /// <param name="handler">The message handler</param>
    /// <param name="filter">Optional filter function to determine which messages to receive</param>
    /// <param name="minPriority">Optional minimum priority level to receive</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeWithFilter(
        Action<TMessage> handler, 
        Func<TMessage, bool> filter = null, 
        MessagePriority minPriority = MessagePriority.Debug);

    /// <summary>
    /// Subscribes to messages with an asynchronous conditional filter using UniTask.
    /// Combines priority, source, correlation, and custom filtering into one flexible method.
    /// </summary>
    /// <param name="handler">The async message handler</param>
    /// <param name="filter">Optional async filter function to determine which messages to receive</param>
    /// <param name="minPriority">Optional minimum priority level to receive</param>
    /// <returns>Disposable subscription handle</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when subscriber is disposed</exception>
    IDisposable SubscribeAsyncWithFilter(
        Func<TMessage, UniTask> handler, 
        Func<TMessage, UniTask<bool>> filter = null, 
        MessagePriority minPriority = MessagePriority.Debug);

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
}