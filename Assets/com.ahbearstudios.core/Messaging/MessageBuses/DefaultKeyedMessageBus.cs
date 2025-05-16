using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Default implementation of IKeyedMessageBus for message filtering based on keys.
    /// Optimized for efficient key-based message filtering.
    /// </summary>
    /// <typeparam name="TKey">The type of keys used for message filtering.</typeparam>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public class DefaultKeyedMessageBus<TKey, TMessage> : IKeyedMessageBus<TKey, TMessage> 
        where TMessage : IMessage
        where TKey : IEquatable<TKey>
    {
        private readonly Dictionary<TKey, Dictionary<Type, HashSet<Delegate>>> _keyTypeSubscriptions;
        private readonly Dictionary<TKey, HashSet<Delegate>> _keyAllSubscriptions;
        private readonly Dictionary<Type, HashSet<Delegate>> _typeSubscriptions;
        private readonly HashSet<Delegate> _allSubscriptions;
        
        private readonly Dictionary<TKey, Dictionary<Type, HashSet<Delegate>>> _keyTypeAsyncSubscriptions;
        private readonly Dictionary<TKey, HashSet<Delegate>> _keyAllAsyncSubscriptions;
        private readonly Dictionary<Type, HashSet<Delegate>> _typeAsyncSubscriptions;
        private readonly HashSet<Delegate> _allAsyncSubscriptions;
        
        private readonly object _subscriptionLock = new object();
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the DefaultKeyedMessageBus.
        /// </summary>
        /// <param name="logger">Optional logger for message bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public DefaultKeyedMessageBus(IBurstLogger logger = null, IProfiler profiler = null)
        {
            _keyTypeSubscriptions = new Dictionary<TKey, Dictionary<Type, HashSet<Delegate>>>();
            _keyAllSubscriptions = new Dictionary<TKey, HashSet<Delegate>>();
            _typeSubscriptions = new Dictionary<Type, HashSet<Delegate>>();
            _allSubscriptions = new HashSet<Delegate>();
            
            _keyTypeAsyncSubscriptions = new Dictionary<TKey, Dictionary<Type, HashSet<Delegate>>>();
            _keyAllAsyncSubscriptions = new Dictionary<TKey, HashSet<Delegate>>();
            _typeAsyncSubscriptions = new Dictionary<Type, HashSet<Delegate>>();
            _allAsyncSubscriptions = new HashSet<Delegate>();
            
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info("DefaultKeyedMessageBus initialized");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(TKey key, Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                lock (_subscriptionLock)
                {
                    Type messageType = typeof(T);
                    
                    // Ensure we have a dictionary for this key
                    if (!_keyTypeSubscriptions.TryGetValue(key, out var typeDictionary))
                    {
                        typeDictionary = new Dictionary<Type, HashSet<Delegate>>();
                        _keyTypeSubscriptions[key] = typeDictionary;
                    }
                    
                    // Ensure we have a set for this type
                    if (!typeDictionary.TryGetValue(messageType, out var handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        typeDictionary[messageType] = handlers;
                    }
                    
                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed to key {key} for message type {messageType.Name}");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(TKey key, Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                lock (_subscriptionLock)
                {
                    Type messageType = typeof(T);
                    
                    // Ensure we have a dictionary for this key
                    if (!_keyTypeAsyncSubscriptions.TryGetValue(key, out var typeDictionary))
                    {
                        typeDictionary = new Dictionary<Type, HashSet<Delegate>>();
                        _keyTypeAsyncSubscriptions[key] = typeDictionary;
                    }
                    
                    // Ensure we have a set for this type
                    if (!typeDictionary.TryGetValue(messageType, out var handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        typeDictionary[messageType] = handlers;
                    }
                    
                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed async handler to key {key} for message type {messageType.Name}");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllForKey(TKey key, Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeToAllForKey"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                lock (_subscriptionLock)
                {
                    if (!_keyAllSubscriptions.TryGetValue(key, out var handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        _keyAllSubscriptions[key] = handlers;
                    }
                    
                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed to all messages with key {key}");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllForKeyAsync(TKey key, Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeToAllForKeyAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                lock (_subscriptionLock)
                {
                    if (!_keyAllAsyncSubscriptions.TryGetValue(key, out var handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        _keyAllAsyncSubscriptions[key] = handlers;
                    }
                    
                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed async handler to all messages with key {key}");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToType<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeToType"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
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
                        _logger.Debug($"Subscribed to all messages of type {messageType.Name}");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToTypeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeToTypeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    Type messageType = typeof(T);
                    if (!_typeAsyncSubscriptions.TryGetValue(messageType, out HashSet<Delegate> handlers))
                    {
                        handlers = new HashSet<Delegate>();
                        _typeAsyncSubscriptions[messageType] = handlers;
                    }

                    handlers.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Subscribed async handler to all messages of type {messageType.Name}");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                lock (_subscriptionLock)
                {
                    _allSubscriptions.Add(handler);
                    
                    if (_logger != null)
                    {
                        _logger.Debug("Subscribed to all messages");
                    }

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
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

                    return new SubscriptionToken(this);
                }
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

                if (!(token is SubscriptionToken subscriptionToken))
                {
                    throw new ArgumentException("Token is not a valid subscription token", nameof(token));
                }

                // In a real implementation, we would need to track which handler is associated with
                // this token. For now, we're just marking the token as inactive.
                subscriptionToken.Deactivate();
                
                if (_logger != null)
                {
                    _logger.Debug("Unsubscribed token");
                }
            }
        }

        /// <inheritdoc/>
        public void Publish(TKey key, TMessage message)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                Type messageType = message.GetType();
                
                if (_logger != null)
                {
                    _logger.Debug($"Publishing message of type {messageType.Name} with key {key}");
                }

                // Notify key+type subscribers
                NotifyKeyTypeSubscribers(key, message, messageType);
                
                // Notify key-only subscribers
                NotifyKeySubscribers(key, message);
                
                // Notify type-only subscribers
                NotifyTypeSubscribers(message, messageType);

                // Notify all-messages subscribers
                NotifyAllSubscribers(message);
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultKeyedMessageBus<TKey, TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                Type messageType = message.GetType();
                
                if (_logger != null)
                {
                    _logger.Debug($"Publishing async message of type {messageType.Name} with key {key}");
                }

                // First handle synchronous subscribers
                Publish(key, message);

                // Then handle asynchronous subscribers
                await NotifyKeyTypeAsyncSubscribersAsync(key, message, messageType, cancellationToken);
                await NotifyKeyAsyncSubscribersAsync(key, message, cancellationToken);
                await NotifyTypeAsyncSubscribersAsync(message, messageType, cancellationToken);
                await NotifyAllAsyncSubscribersAsync(message, cancellationToken);
            }
        }

        private void NotifyKeyTypeSubscribers(TKey key, TMessage message, Type messageType)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyKeyTypeSubscribers"))
            {
                HashSet<Delegate> keyTypeHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_keyTypeSubscriptions.TryGetValue(key, out var typeDictionary) &&
                        typeDictionary.TryGetValue(messageType, out var handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        keyTypeHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (keyTypeHandlers != null)
                {
                    foreach (var handler in keyTypeHandlers)
                    {
                        try
                        {
                            // We know the handlers are typed properly
                            ((Action<TMessage>)handler)(message);
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error in key+type message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void NotifyKeySubscribers(TKey key, TMessage message)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyKeySubscribers"))
            {
                HashSet<Delegate> keyHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_keyAllSubscriptions.TryGetValue(key, out var handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        keyHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (keyHandlers != null)
                {
                    foreach (var handler in keyHandlers)
                    {
                        try
                        {
                            ((Action<TMessage>)handler)(message);
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error in key-based message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void NotifyTypeSubscribers(TMessage message, Type messageType)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyTypeSubscribers"))
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
                                _logger.Error($"Error in type-based message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void NotifyAllSubscribers(TMessage message)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyAllSubscribers"))
            {
                HashSet<Delegate> allHandlersCopy = null;
                
                lock (_subscriptionLock)
                {
                    // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                    allHandlersCopy = new HashSet<Delegate>(_allSubscriptions);
                }

                foreach (var handler in allHandlersCopy)
                {
                    try
                    {
                        ((Action<TMessage>)handler)(message);
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error in all-messages handler: {ex.Message}");
                        }
                    }
                }
            }
        }

        private async Task NotifyKeyTypeAsyncSubscribersAsync(TKey key, TMessage message, Type messageType, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyKeyTypeAsyncSubscribersAsync"))
            {
                HashSet<Delegate> keyTypeAsyncHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_keyTypeAsyncSubscriptions.TryGetValue(key, out var typeDictionary) &&
                        typeDictionary.TryGetValue(messageType, out var handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        keyTypeAsyncHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (keyTypeAsyncHandlers != null)
                {
                    foreach (var handler in keyTypeAsyncHandlers)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            // We know the handlers are typed properly
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
                                _logger.Error($"Error in key+type async message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private async Task NotifyKeyAsyncSubscribersAsync(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyKeyAsyncSubscribersAsync"))
            {
                HashSet<Delegate> keyAsyncHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_keyAllAsyncSubscriptions.TryGetValue(key, out var handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        keyAsyncHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (keyAsyncHandlers != null)
                {
                    foreach (var handler in keyAsyncHandlers)
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
                                _logger.Error($"Error in key-based async message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private async Task NotifyTypeAsyncSubscribersAsync(TMessage message, Type messageType, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyTypeAsyncSubscribersAsync"))
            {
                HashSet<Delegate> typeAsyncHandlers = null;
                
                lock (_subscriptionLock)
                {
                    if (_typeAsyncSubscriptions.TryGetValue(messageType, out var handlers))
                    {
                        // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                        typeAsyncHandlers = new HashSet<Delegate>(handlers);
                    }
                }

                if (typeAsyncHandlers != null)
                {
                    foreach (var handler in typeAsyncHandlers)
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
                                _logger.Error($"Error in type-based async message handler: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private async Task NotifyAllAsyncSubscribersAsync(TMessage message, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.NotifyAllAsyncSubscribersAsync"))
            {
                HashSet<Delegate> allAsyncHandlersCopy = null;
                
                lock (_subscriptionLock)
                {
                    // Create a copy to avoid issues with handlers that subscribe/unsubscribe during iteration
                    allAsyncHandlersCopy = new HashSet<Delegate>(_allAsyncSubscriptions);
                }

                foreach (var handler in allAsyncHandlersCopy)
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
            using (_profiler?.BeginSample("DefaultKeyedMessageBus.Dispose"))
            {
                if (_isDisposed)
                {
                    return;
                }

                lock (_subscriptionLock)
                {
                    _keyTypeSubscriptions.Clear();
                    _keyAllSubscriptions.Clear();
                    _typeSubscriptions.Clear();
                    _allSubscriptions.Clear();
                    
                    _keyTypeAsyncSubscriptions.Clear();
                    _keyAllAsyncSubscriptions.Clear();
                    _typeAsyncSubscriptions.Clear();
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