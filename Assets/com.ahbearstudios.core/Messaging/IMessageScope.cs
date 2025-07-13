using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging;

/// <summary>
/// Interface for scoped subscription management.
/// Automatically disposes all subscriptions when the scope is disposed.
/// </summary>
public interface IMessageScope : IDisposable
{
    /// <summary>
    /// Subscribes to messages within this scope.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="handler">The message handler</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to messages asynchronously within this scope.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="handler">The async message handler</param>
    /// <returns>Disposable subscription handle</returns>
    IDisposable SubscribeAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage;

    /// <summary>
    /// Gets the number of active subscriptions in this scope.
    /// </summary>
    int ActiveSubscriptions { get; }

    /// <summary>
    /// Gets whether this scope is still active.
    /// </summary>
    bool IsActive { get; }
}