using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Default implementation of IMessageBus for general use.
    /// Optimized for memory usage with minimal garbage generation.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public class DefaultMessageBus<TMessage> : IMessageBus<TMessage> where TMessage : IMessage
    {
        private readonly Dictionary<Guid, HashSet<Delegate>> _subscriptions;
        private readonly Dictionary<Type, HashSet<Delegate>> _typeSubscriptions;
        private readonly Dictionary<Type, HashSet<Delegate>> _asyncTypeSubscriptions;
        private readonly HashSet<Delegate> _allAsyncSubscriptions;
        private readonly object _subscriptionLock = new object();
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the DefaultMessageBus.
        /// </summary>
        /// <param name="logger">Optional logger for message bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public DefaultMessageBus(IBurstLogger logger = null, IProfiler profiler = null)
        {
            _subscriptions = new Dictionary<Guid, HashSet<Delegate>>();
            _typeSubscriptions = new Dictionary<Type, HashSet<Delegate>>();
            _asyncTypeSubscriptions = new Dictionary<Type, HashSet<Delegate>>();
            _allAsyncSubscriptions = new HashSet<Delegate>();
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info("DefaultMessageBus initialized");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("DefaultMessageBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    Type messageType = typeof(T);
                    if (!_typeSubscriptions.TryGetValue(messageType, out HashSet<Delegate> handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        _typeSubscriptions[messageType] = handlers;
                    }

                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed to {messageType.Name} messages");
                    }

                    // Create a token with the specific message type
                    return new SubscriptionToken(this, messageType);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("DefaultMessageBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    Type messageType = typeof(T);
                    if (!_asyncTypeSubscriptions.TryGetValue(messageType, out HashSet<Delegate> handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        _asyncTypeSubscriptions[messageType] = handlers;
                    }

                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed async handler to {messageType.Name} messages");
                    }

                    // Create a token with the specific message type
                    return new SubscriptionToken(this, messageType);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    Guid allMessagesId = Guid.Empty; // Special ID for all messages
                    if (!_subscriptions.TryGetValue(allMessagesId, out HashSet<Delegate> handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        _subscriptions[allMessagesId] = handlers;
                    }

                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug("Subscribed to all messages");
                    }

                    // Create a token with null message type (indicating all messages)
                    return new SubscriptionToken(this, null);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    _allAsyncSubscriptions.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug("Subscribed async handler to all messages");
                    }

                    // Create a token with null message type (indicating all messages)
                    return new SubscriptionToken(this, null);
                }
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

                if (!(token is SubscriptionToken subscriptionToken))
                {
                    throw new ArgumentException("Token is not a valid subscription token", nameof(token));
                }

                // Check if this token belongs to this message bus
                if (subscriptionToken.MessageBus != this)
                {
                    throw new ArgumentException("Token was not created by this message bus", nameof(token));
                }

                // Deactivate the token
                subscriptionToken.Deactivate();
                
                if (_logger != null)
                {
                    _logger.Debug("Unsubscribed token");
                }
            }
        }

        /// <inheritdoc/>
        public void Publish(TMessage message)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                Type messageType = message.GetType();
                
                if (_logger != null)
                {
                    _logger.Debug($"Publishing message of type {messageType.Name} with ID {message.Id}");
                }

                // Notify type-specific subscribers
                NotifySubscribers(message, messageType);

                // Notify subscribers to all messages
                NotifyAllSubscribers(message);
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMessageBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                Type messageType = message.GetType();
                
                if (_logger != null)
                {
                    _logger.Debug($"Publishing async message of type {messageType.Name} with ID {message.Id}");
                }

                // First handle synchronous subscribers
                Publish(message);

                // Then handle asynchronous subscribers
                await NotifyAsyncSubscribersAsync(message, messageType, cancellationToken);
                await NotifyAllAsyncSubscribersAsync(message, cancellationToken);
            }
        }

        private void NotifySubscribers(TMessage message, Type messageType)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.NotifySubscribers"))
            {
                HashSet<Delegate> typeHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_typeSubscriptions.TryGetValue(messageType, out HashSet<Delegate> handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        typeHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (typeHandlers != null)
                {
                    foreach (var handler in typeHandlers)
                    {
                        try
                        {
                            // We know these are all typed properly
                            ((Action<TMessage>)handler)(message);
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error in message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void NotifyAllSubscribers(TMessage message)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.NotifyAllSubscribers"))
            {
                HashSet<Delegate> allHandlers = null;
                
                lock (_subscriptionLock)
                {
                    Guid allMessagesId = Guid.Empty;
                    if (_subscriptions.TryGetValue(allMessagesId, out HashSet<Delegate> handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        allHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (allHandlers != null)
                {
                    foreach (var handler in allHandlers)
                    {
                        try
                        {
                            ((Action<TMessage>)handler)(message);
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error in message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private async Task NotifyAsyncSubscribersAsync(TMessage message, Type messageType, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.NotifyAsyncSubscribersAsync"))
            {
                HashSet<Delegate> asyncTypeHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_asyncTypeSubscriptions.TryGetValue(messageType, out HashSet<Delegate> handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        asyncTypeHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (asyncTypeHandlers != null)
                {
                    foreach (var handler in asyncTypeHandlers)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            // We know these are all typed properly
                            await ((Func<TMessage, Task>)handler)(message);
                        }
                        catch (OperationCanceledException)
                        {
                            // Propagate cancellation
                            throw;
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error in async message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private async Task NotifyAllAsyncSubscribersAsync(TMessage message, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("DefaultMessageBus.NotifyAllAsyncSubscribersAsync"))
            {
                HashSet<Delegate> allAsyncHandlers = null;
                
                lock (_subscriptionLock)
                {
                    // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                    allAsyncHandlers = new HashSet<Delegate>(_allAsyncSubscriptions);
                }

                foreach (var handler in allAsyncHandlers)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        await ((Func<TMessage, Task>)handler)(message);
                    }
                    catch (OperationCanceledException)
                    {
                        // Propagate cancellation
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error in all-messages async handler: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("DefaultMessageBus.Dispose"))
            {
                if (_isDisposed)
                {
                    return;
                }

                lock (_subscriptionLock)
                {
                    _subscriptions.Clear();
                    _typeSubscriptions.Clear();
                    _asyncTypeSubscriptions.Clear();
                    _allAsyncSubscriptions.Clear();
                    _isDisposed = true;
                    
                    if (_logger != null)
                    {
                        _logger.Debug("Message bus disposed");
                    }
                }
            }
        }
    }
}