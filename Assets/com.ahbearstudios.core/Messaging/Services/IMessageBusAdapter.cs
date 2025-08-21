using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
/// Service interface for message bus operations.
/// Provides abstraction over message bus backend, enabling future swapping of message bus implementations.
/// Follows CLAUDE.md guidelines for proper service encapsulation and Unity performance optimization.
/// </summary>
public interface IMessageBusAdapter : IDisposable
{
    /// <summary>
    /// Publishes a message synchronously through the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message to publish</param>
    void Publish<TMessage>(TMessage message) where TMessage : IMessage;

    /// <summary>
    /// Publishes a message asynchronously through the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UniTask representing the async operation</returns>
    UniTask PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages synchronously through the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="handler">The message handler</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages asynchronously through the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="handler">The async message handler</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable SubscribeAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages synchronously through the message bus with MessagePipe filters.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="handler">The message handler</param>
    /// <param name="filters">Array of MessagePipe filters to apply</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages asynchronously through the message bus with MessagePipe filters.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="handler">The async message handler</param>
    /// <param name="filters">Array of async MessagePipe filters to apply</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable SubscribeAsync<TMessage>(Func<TMessage, UniTask> handler, params AsyncMessageHandlerFilter<TMessage>[] filters) where TMessage : IMessage;

    /// <summary>
    /// Publishes a message to a specific key/topic through MessagePipe's keyed messaging.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="key">The routing key or topic</param>
    /// <param name="message">The message to publish</param>
    void PublishKeyed<TMessage>(string key, TMessage message) where TMessage : IMessage;

    /// <summary>
    /// Publishes a message to a specific key/topic asynchronously through MessagePipe's keyed messaging.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="key">The routing key or topic</param>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UniTask representing the async operation</returns>
    UniTask PublishKeyedAsync<TMessage>(string key, TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages from a specific key/topic through MessagePipe's keyed messaging.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="key">The routing key or topic to subscribe to</param>
    /// <param name="handler">The message handler</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable SubscribeKeyed<TMessage>(string key, Action<TMessage> handler) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages from a specific key/topic asynchronously through MessagePipe's keyed messaging.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="key">The routing key or topic to subscribe to</param>
    /// <param name="handler">The async message handler</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable SubscribeKeyedAsync<TMessage>(string key, Func<TMessage, UniTask> handler) where TMessage : IMessage;

    /// <summary>
    /// Gets the current operational status of the message bus.
    /// </summary>
    bool IsOperational { get; }

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    int ActiveSubscriptionCount { get; }

    /// <summary>
    /// Gets comprehensive health status of the message bus adapter.
    /// </summary>
    /// <returns>Health status with detailed metrics</returns>
    UniTask<MessageBusAdapterHealthReport> GetHealthReportAsync();

    /// <summary>
    /// Forces a health check and updates internal status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current health status</returns>
    UniTask<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Health report for MessageBusAdapter with comprehensive metrics.
/// </summary>
public sealed record MessageBusAdapterHealthReport
{
    /// <summary>
    /// Gets whether the adapter is operational.
    /// </summary>
    public bool IsOperational { get; init; }

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    /// Gets the total messages published.
    /// </summary>
    public long TotalPublished { get; init; }

    /// <summary>
    /// Gets the total messages failed.
    /// </summary>
    public long TotalFailed { get; init; }

    /// <summary>
    /// Gets the current error rate (0.0 to 1.0).
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Gets the average message processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime { get; init; }

    /// <summary>
    /// Gets the memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; init; }

    /// <summary>
    /// Gets when this report was generated.
    /// </summary>
    public DateTime Timestamp { get; init; }
}