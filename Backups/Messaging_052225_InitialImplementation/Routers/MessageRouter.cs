using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging.Routers
{
    /// <summary>
    /// Implementation of IMessageRouter that routes messages to different message buses based on filtering rules.
    /// Provides message routing based on content or metadata.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to route.</typeparam>
    public class MessageRouter<TMessage> : IMessageRouter<TMessage>, IMessageBus<TMessage> where TMessage : IMessage
    {
        private readonly List<RouteDefinition> _routes;
        private readonly object _routesLock = new object();
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly bool _routeToAllMatches;
        private readonly List<ISubscriptionToken> _sourceSubscriptions;
        private string _name;
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets the name of this router.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// Gets the number of defined routes.
        /// </summary>
        public int RouteCount
        {
            get
            {
                lock (_routesLock)
                {
                    return _routes.Count;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the MessageRouter class.
        /// </summary>
        /// <param name="routeToAllMatches">Whether to route to all matching destinations or just the first match.</param>
        /// <param name="name">Optional name for this router instance.</param>
        /// <param name="logger">Optional logger for router operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageRouter(bool routeToAllMatches = true, string name = null, IBurstLogger logger = null, IProfiler profiler = null)
        {
            _routes = new List<RouteDefinition>();
            _routeToAllMatches = routeToAllMatches;
            _name = name ?? $"Router_{Guid.NewGuid():N}";
            _logger = logger;
            _profiler = profiler;
            _sourceSubscriptions = new List<ISubscriptionToken>();
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info($"MessageRouter '{_name}' initialized (RouteToAllMatches: {_routeToAllMatches})");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("MessageRouter.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                throw new NotSupportedException("MessageRouter does not support direct subscriptions. Subscribe to the destination message buses instead.");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("MessageRouter.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                throw new NotSupportedException("MessageRouter does not support direct subscriptions. Subscribe to the destination message buses instead.");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("MessageRouter.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                throw new NotSupportedException("MessageRouter does not support direct subscriptions. Subscribe to the destination message buses instead.");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("MessageRouter.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                throw new NotSupportedException("MessageRouter does not support direct subscriptions. Subscribe to the destination message buses instead.");
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("MessageRouter.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                throw new NotSupportedException("MessageRouter does not support direct subscriptions or unsubscriptions.");
            }
        }

        /// <inheritdoc/>
        public void Publish(TMessage message)
        {
            using (_profiler?.BeginSample("MessageRouter.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                RouteMessage(message);
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("MessageRouter.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                await RouteMessageAsync(message, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public void AddRoute(IMessageFilter<TMessage> filter, IMessageBus<TMessage> destination, string routeName = null)
        {
            using (_profiler?.BeginSample("MessageRouter.AddRoute"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (filter == null)
                {
                    throw new ArgumentNullException(nameof(filter));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                // Generate a route name if none was provided
                if (string.IsNullOrEmpty(routeName))
                {
                    routeName = $"Route_{Guid.NewGuid():N}";
                }

                lock (_routesLock)
                {
                    // Add the route
                    _routes.Add(new RouteDefinition
                    {
                        Filter = filter,
                        Destination = destination,
                        Name = routeName
                    });
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Added route '{routeName}' with filter '{filter.Description}' to destination");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool RemoveRoute(string routeName)
        {
            using (_profiler?.BeginSample("MessageRouter.RemoveRoute"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (string.IsNullOrEmpty(routeName))
                {
                    throw new ArgumentException("Route name cannot be null or empty.", nameof(routeName));
                }

                lock (_routesLock)
                {
                    int index = _routes.FindIndex(r => r.Name == routeName);
                    if (index >= 0)
                    {
                        var route = _routes[index];
                        _routes.RemoveAt(index);
                        
                        if (_logger != null)
                        {
                            _logger.Info($"Removed route '{routeName}'");
                        }
                        
                        return true;
                    }
                    
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public bool RemoveRoutesByDestination(IMessageBus<TMessage> destination)
        {
            using (_profiler?.BeginSample("MessageRouter.RemoveRoutesByDestination"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                lock (_routesLock)
                {
                    int count = _routes.RemoveAll(r => r.Destination == destination);
                    
                    if (count > 0 && _logger != null)
                    {
                        _logger.Info($"Removed {count} routes to specified destination");
                    }
                    
                    return count > 0;
                }
            }
        }

        /// <inheritdoc/>
        public void ClearRoutes()
        {
            using (_profiler?.BeginSample("MessageRouter.ClearRoutes"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                lock (_routesLock)
                {
                    int count = _routes.Count;
                    _routes.Clear();
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Cleared {count} routes");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public List<RouteInfo> GetRouteInfo()
        {
            using (_profiler?.BeginSample("MessageRouter.GetRouteInfo"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                lock (_routesLock)
                {
                    return _routes.Select(r => new RouteInfo
                    {
                        Name = r.Name,
                        FilterDescription = r.Filter.Description,
                        DestinationType = r.Destination.GetType().Name
                    }).ToList();
                }
            }
        }

        /// <inheritdoc/>
        public void ConnectSource(IMessageBus<TMessage> source)
        {
            using (_profiler?.BeginSample("MessageRouter.ConnectSource"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                lock (_routesLock)
                {
                    // Subscribe to the source bus to route its messages
                    var token = source.SubscribeToAll(message => RouteMessage(message));
                    _sourceSubscriptions.Add(token);
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Connected source to router '{_name}'");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void DisconnectSource(IMessageBus<TMessage> source)
        {
            using (_profiler?.BeginSample("MessageRouter.DisconnectSource"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                lock (_routesLock)
                {
                    // Find and remove all subscriptions to this source
                    for (int i = _sourceSubscriptions.Count - 1; i >= 0; i--)
                    {
                        var subscription = _sourceSubscriptions[i];
                        source.Unsubscribe(subscription);
                        _sourceSubscriptions.RemoveAt(i);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Disconnected source from router '{_name}'");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void DisconnectAllSources()
        {
            using (_profiler?.BeginSample("MessageRouter.DisconnectAllSources"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageRouter<TMessage>));
                }

                lock (_routesLock)
                {
                    // Dispose all source subscriptions
                    foreach (var subscription in _sourceSubscriptions)
                    {
                        subscription.Dispose();
                    }
                    
                    _sourceSubscriptions.Clear();
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Disconnected all sources from router '{_name}'");
                    }
                }
            }
        }

        /// <summary>
        /// Routes a message to the appropriate destination(s) based on the defined routes.
        /// </summary>
        /// <param name="message">The message to route.</param>
        private void RouteMessage(TMessage message)
        {
            using (_profiler?.BeginSample("MessageRouter.RouteMessage"))
            {
                List<RouteDefinition> matchingRoutes = new List<RouteDefinition>();
                
                lock (_routesLock)
                {
                    // Find all matching routes
                    foreach (var route in _routes)
                    {
                        if (route.Filter.PassesFilter(message))
                        {
                            matchingRoutes.Add(route);
                            
                            // If we're not routing to all matches, stop after the first match
                            if (!_routeToAllMatches)
                            {
                                break;
                            }
                        }
                    }
                }
                
                if (matchingRoutes.Count == 0)
                {
                    if (_logger != null)
                    {
                        _logger.Debug($"No matching routes found for message {message.Id}");
                    }
                    
                    return;
                }
                
                // Publish to all matching destinations
                foreach (var route in matchingRoutes)
                {
                    try
                    {
                        route.Destination.Publish(message);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Routed message {message.Id} via route '{route.Name}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error publishing message {message.Id} to destination via route '{route.Name}': {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Routes a message to the appropriate destination(s) based on the defined routes asynchronously.
        /// </summary>
        /// <param name="message">The message to route.</param>
        /// <param name="cancellationToken">A cancellation token to stop routing.</param>
        private async Task RouteMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("MessageRouter.RouteMessageAsync"))
            {
                List<RouteDefinition> matchingRoutes = new List<RouteDefinition>();
                
                lock (_routesLock)
                {
                    // Find all matching routes
                    foreach (var route in _routes)
                    {
                        if (route.Filter.PassesFilter(message))
                        {
                            matchingRoutes.Add(route);
                            
                            // If we're not routing to all matches, stop after the first match
                            if (!_routeToAllMatches)
                            {
                                break;
                            }
                        }
                    }
                }
                
                if (matchingRoutes.Count == 0)
                {
                    if (_logger != null)
                    {
                        _logger.Debug($"No matching routes found for message {message.Id} asynchronously");
                    }
                    
                    return;
                }
                
                // Publish to all matching destinations in parallel
                var publishTasks = matchingRoutes.Select(async route =>
                {
                    try
                    {
                        await route.Destination.PublishAsync(message, cancellationToken);
                        
                        if (_logger != null)
                        {
                            _logger.Debug($"Routed message {message.Id} via route '{route.Name}' asynchronously");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error publishing message {message.Id} to destination via route '{route.Name}' asynchronously: {ex.Message}");
                        }
                    }
                });
                
                await Task.WhenAll(publishTasks);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageRouter.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message router.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_routesLock)
                {
                    // Disconnect all sources
                    DisconnectAllSources();
                    
                    // Dispose all filters
                    foreach (var route in _routes)
                    {
                        route.Filter.Dispose();
                    }
                    
                    _routes.Clear();
                    
                    if (_logger != null)
                    {
                        _logger.Info($"MessageRouter '{_name}' disposed");
                    }
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessageRouter()
        {
            Dispose(false);
        }

        /// <summary>
        /// Defines a message route with a filter and destination.
        /// </summary>
        private class RouteDefinition
        {
            /// <summary>
            /// Gets or sets the filter that determines whether a message should be routed.
            /// </summary>
            public IMessageFilter<TMessage> Filter { get; set; }
            
            /// <summary>
            /// Gets or sets the destination message bus.
            /// </summary>
            public IMessageBus<TMessage> Destination { get; set; }
            
            /// <summary>
            /// Gets or sets the name of the route.
            /// </summary>
            public string Name { get; set; }
        }
    }

    /// <summary>
    /// Information about a message route.
    /// </summary>
    public class RouteInfo
    {
        /// <summary>
        /// Gets or sets the name of the route.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the description of the filter.
        /// </summary>
        public string FilterDescription { get; set; }
        
        /// <summary>
        /// Gets or sets the type name of the destination.
        /// </summary>
        public string DestinationType { get; set; }
    }
}