using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for routing messages to specific handlers
    /// </summary>
    public interface IMessageRouter
    {
        /// <summary>
        /// Routes a message to the appropriate handlers
        /// </summary>
        /// <typeparam name="TMessage">The type of message to route</typeparam>
        /// <param name="message">The message to route</param>
        void Route<TMessage>(TMessage message) where TMessage : IMessage;
    
        /// <summary>
        /// Routes a message to the appropriate handlers asynchronously
        /// </summary>
        /// <typeparam name="TMessage">The type of message to route</typeparam>
        /// <param name="message">The message to route</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        Task RouteAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;
    
        /// <summary>
        /// Registers a route for a message type
        /// </summary>
        /// <typeparam name="TMessage">The type of message to route</typeparam>
        /// <param name="handler">The handler to route messages to</param>
        /// <returns>A token that can be disposed to unregister the route</returns>
        ISubscriptionToken RegisterRoute<TMessage>(Action<TMessage> handler) where TMessage : IMessage;
    
        /// <summary>
        /// Registers a route for a message type with a filter
        /// </summary>
        /// <typeparam name="TMessage">The type of message to route</typeparam>
        /// <param name="handler">The handler to route messages to</param>
        /// <param name="filter">The filter to apply to messages</param>
        /// <returns>A token that can be disposed to unregister the route</returns>
        ISubscriptionToken RegisterRoute<TMessage>(Action<TMessage> handler, IMessageFilter<TMessage> filter) where TMessage : IMessage;
    }
}