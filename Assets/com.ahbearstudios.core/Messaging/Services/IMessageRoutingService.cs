using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Messaging.Messages;

namespace AhBearStudios.Core.Messaging.Services;

/// <summary>
    /// Interface for message routing service.
    /// </summary>
    public interface IMessageRoutingService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is initialized and operational.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the number of active routing rules.
        /// </summary>
        int ActiveRuleCount { get; }

        /// <summary>
        /// Adds a routing rule to the service.
        /// </summary>
        /// <param name="rule">The routing rule to add</param>
        /// <returns>The unique identifier for the added rule</returns>
        Guid AddRoutingRule(RoutingRule rule);

        /// <summary>
        /// Removes a routing rule from the service.
        /// </summary>
        /// <param name="ruleId">The ID of the rule to remove</param>
        /// <returns>True if the rule was removed, false if not found</returns>
        bool RemoveRoutingRule(Guid ruleId);

        /// <summary>
        /// Routes a message asynchronously through the routing system.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to route</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The routing result</returns>
        Task<RouteResult> RouteMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage;

        /// <summary>
        /// Routes a message synchronously (queues for background processing).
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message to route</param>
        void RouteMessage<TMessage>(TMessage message) where TMessage : IMessage;

        /// <summary>
        /// Gets all routing rules.
        /// </summary>
        /// <returns>Collection of routing rules</returns>
        IEnumerable<RoutingRule> GetRoutingRules();

        /// <summary>
        /// Gets routing rules for a specific message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>Collection of applicable routing rules</returns>
        IEnumerable<RoutingRule> GetRoutingRulesForType<TMessage>() where TMessage : IMessage;

        /// <summary>
        /// Gets routing rules for a specific message type.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <returns>Collection of applicable routing rules</returns>
        IEnumerable<RoutingRule> GetRoutingRulesForType(Type messageType);

        /// <summary>
        /// Registers a route handler.
        /// </summary>
        /// <param name="name">The handler name</param>
        /// <param name="handler">The handler function</param>
        /// <returns>The unique identifier for the handler</returns>
        Guid RegisterRouteHandler(string name, Func<IMessage, CancellationToken, Task<bool>> handler);

        /// <summary>
        /// Unregisters a route handler.
        /// </summary>
        /// <param name="handlerId">The handler ID</param>
        /// <returns>True if unregistered, false if not found</returns>
        bool UnregisterRouteHandler(Guid handlerId);

        /// <summary>
        /// Gets routing statistics.
        /// </summary>
        /// <returns>Current routing statistics</returns>
        MessageRoutingStatistics GetStatistics();

        /// <summary>
        /// Clears all routing rules and handlers.
        /// </summary>
        void ClearRoutes();

        /// <summary>
        /// Event raised when a message is routed.
        /// </summary>
        event EventHandler<MessageRoutedEventArgs> MessageRouted;

        /// <summary>
        /// Event raised when a routing rule is added.
        /// </summary>
        event EventHandler<RoutingRuleEventArgs> RoutingRuleAdded;

        /// <summary>
        /// Event raised when a routing rule is removed.
        /// </summary>
        event EventHandler<RoutingRuleEventArgs> RoutingRuleRemoved;

        /// <summary>
        /// Event raised when a route handler is registered.
        /// </summary>
        event EventHandler<RouteHandlerEventArgs> RouteHandlerRegistered;

        /// <summary>
        /// Event raised when a route handler is unregistered.
        /// </summary>
        event EventHandler<RouteHandlerEventArgs> RouteHandlerUnregistered;

        /// <summary>
        /// Event raised when all routes are cleared.
        /// </summary>
        event EventHandler<RoutesClearedEventArgs> RoutesCleared;
    }