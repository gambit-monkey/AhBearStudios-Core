using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging;

/// <summary>
/// Interface for message scope management with automatic cleanup.
/// </summary>
public interface IMessageScope : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the number of active subscriptions in this scope.
    /// </summary>
    int ActiveSubscriptions { get; }

    /// <summary>
    /// Gets whether this scope is still active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Subscribes to a message type within this scope.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <param name="handler">The message handler</param>
    /// <returns>Scoped subscription that will be automatically disposed when scope is disposed</returns>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage;

    /// <summary>
    /// Subscribes to a message type with async handler within this scope.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <param name="handler">The async message handler</param>
    /// <returns>Scoped subscription that will be automatically disposed when scope is disposed</returns>
    IDisposable SubscribeAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage;
}