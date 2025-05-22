using System;
using System.Collections.Generic;
using AhBearStudios.Core.Messaging.Routers;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for routing messages to different message buses based on filtering rules.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to route.</typeparam>
    public interface IMessageRouter<TMessage> : IDisposable where TMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the name of this router.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the number of defined routes.
        /// </summary>
        int RouteCount { get; }

        /// <summary>
        /// Adds a route from a filter to a destination.
        /// </summary>
        /// <param name="filter">The filter that determines whether a message should be routed.</param>
        /// <param name="destination">The destination message bus.</param>
        /// <param name="routeName">Optional name for the route.</param>
        void AddRoute(IMessageFilter<TMessage> filter, IMessageBus<TMessage> destination, string routeName = null);

        /// <summary>
        /// Removes a route by name.
        /// </summary>
        /// <param name="routeName">The name of the route to remove.</param>
        /// <returns>True if the route was found and removed, false otherwise.</returns>
        bool RemoveRoute(string routeName);

        /// <summary>
        /// Removes all routes to a specific destination.
        /// </summary>
        /// <param name="destination">The destination to remove routes for.</param>
        /// <returns>True if any routes were removed, false otherwise.</returns>
        bool RemoveRoutesByDestination(IMessageBus<TMessage> destination);

        /// <summary>
        /// Clears all routes.
        /// </summary>
        void ClearRoutes();

        /// <summary>
        /// Gets information about all defined routes.
        /// </summary>
        /// <returns>A list of route information.</returns>
        List<RouteInfo> GetRouteInfo();

        /// <summary>
        /// Connects a source message bus to this router.
        /// </summary>
        /// <param name="source">The source message bus.</param>
        void ConnectSource(IMessageBus<TMessage> source);

        /// <summary>
        /// Disconnects a source message bus from this router.
        /// </summary>
        /// <param name="source">The source message bus to disconnect.</param>
        void DisconnectSource(IMessageBus<TMessage> source);

        /// <summary>
        /// Disconnects all source message buses from this router.
        /// </summary>
        void DisconnectAllSources();
    }
}