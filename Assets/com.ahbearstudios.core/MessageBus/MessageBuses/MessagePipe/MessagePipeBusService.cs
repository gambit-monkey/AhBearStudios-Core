using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe
{
    /// <summary>
    /// Implementation of the IMessageBusService interface using MessagePipe.
    /// Provides efficient message publishing and subscription with caching support.
    /// </summary>
    public sealed class MessagePipeBusService : IMessageBusService, IDisposable
    {
        private readonly IDependencyProvider _dependencyProvider;
        private readonly IBurstLogger _logger;
        private readonly IProfilerService _profilerService;
        private readonly IMessageRegistry _messageRegistry;

        private readonly Dictionary<Type, object> _publishers = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _subscribers = new Dictionary<Type, object>();
        private readonly Dictionary<(Type, Type), object> _keyedPublishers = new Dictionary<(Type, Type), object>();
        private readonly Dictionary<(Type, Type), object> _keyedSubscribers = new Dictionary<(Type, Type), object>();
        private readonly List<IDisposable> _activeSubscriptions = new List<IDisposable>();
        private readonly object _cacheLock = new object();

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeBusService class.
        /// </summary>
        /// <param name="dependencyProvider">The dependency provider to use for resolving MessagePipe services.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profilerService">The profiler to use for performance monitoring.</param>
        /// <param name="messageRegistry">The message registry to use for message discovery.</param>
        public MessagePipeBusService(
            IDependencyProvider dependencyProvider, 
            IBurstLogger logger, 
            IProfilerService profilerService,
            IMessageRegistry messageRegistry)
        {
            _dependencyProvider = dependencyProvider ?? throw new ArgumentNullException(nameof(dependencyProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));

            _logger.Log(LogLevel.Info, "MessagePipeBusService initialized", "MessageBusService");
        }

        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>()
        {
            ThrowIfDisposed();

            var messageType = typeof(TMessage);

            lock (_cacheLock)
            {
                if (!_publishers.TryGetValue(messageType, out var publisher))
                {
                    using (_profilerService.BeginSample("MessagePipeBusService.GetPublisher"))
                    {
                        var messagePipePublisher = _dependencyProvider.Resolve<IAsyncPublisher<TMessage>>();
                        if (messagePipePublisher == null)
                        {
                            throw new InvalidOperationException(
                                $"Failed to resolve IAsyncPublisher<{messageType.Name}> from dependency provider. " +
                                "Ensure MessagePipe is properly configured in your DI container.");
                        }

                        publisher = new MessagePipePublisher<TMessage>(messagePipePublisher, _logger, _profilerService);
                        _publishers[messageType] = publisher;

                        _logger.Log(LogLevel.Debug, 
                            $"Created publisher for message type {messageType.Name}", 
                            "MessageBusService");

                        // Register the message type if it implements IMessage and is not already registered
                        if (typeof(IMessage).IsAssignableFrom(messageType) && !_messageRegistry.IsRegistered(messageType))
                        {
                            _messageRegistry.RegisterMessageType(messageType);
                        }
                    }
                }

                return (IMessagePublisher<TMessage>)publisher;
            }
        }

        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>()
        {
            ThrowIfDisposed();

            var messageType = typeof(TMessage);

            lock (_cacheLock)
            {
                if (!_subscribers.TryGetValue(messageType, out var subscriber))
                {
                    using (_profilerService.BeginSample("MessagePipeBusService.GetSubscriber"))
                    {
                        var messagePipeSubscriber = _dependencyProvider.Resolve<ISubscriber<TMessage>>();
                        if (messagePipeSubscriber == null)
                        {
                            throw new InvalidOperationException(
                                $"Failed to resolve ISubscriber<{messageType.Name}> from dependency provider. " +
                                "Ensure MessagePipe is properly configured in your DI container.");
                        }

                        subscriber = new MessagePipeSubscriber<TMessage>(messagePipeSubscriber, _logger, _profilerService);
                        _subscribers[messageType] = subscriber;

                        _logger.Log(LogLevel.Debug, 
                            $"Created subscriber for message type {messageType.Name}", 
                            "MessageBusService");

                        // Register the message type if it implements IMessage and is not already registered
                        if (typeof(IMessage).IsAssignableFrom(messageType) && !_messageRegistry.IsRegistered(messageType))
                        {
                            _messageRegistry.RegisterMessageType(messageType);
                        }
                    }
                }

                return (IMessageSubscriber<TMessage>)subscriber;
            }
        }

        /// <inheritdoc />
        public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>()
        {
            ThrowIfDisposed();

            var key = (typeof(TKey), typeof(TMessage));

            lock (_cacheLock)
            {
                if (!_keyedPublishers.TryGetValue(key, out var publisher))
                {
                    using (_profilerService.BeginSample("MessagePipeBusService.GetKeyedPublisher"))
                    {
                        var messagePipePublisher = _dependencyProvider.Resolve<IAsyncPublisher<TKey, TMessage>>();
                        if (messagePipePublisher == null)
                        {
                            throw new InvalidOperationException(
                                $"Failed to resolve IAsyncPublisher<{typeof(TKey).Name}, {typeof(TMessage).Name}> from dependency provider. " +
                                "Ensure MessagePipe is properly configured in your DI container.");
                        }

                        publisher = new MessagePipeKeyedPublisher<TKey, TMessage>(messagePipePublisher, _logger, _profilerService);
                        _keyedPublishers[key] = publisher;

                        _logger.Log(LogLevel.Debug,
                            $"Created keyed publisher for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}",
                            "MessageBusService");

                        // Register the message type if it implements IMessage and is not already registered
                        if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)) &&
                            !_messageRegistry.IsRegistered(typeof(TMessage)))
                        {
                            _messageRegistry.RegisterMessageType(typeof(TMessage));
                        }
                    }
                }

                return (IKeyedMessagePublisher<TKey, TMessage>)publisher;
            }
        }

        /// <inheritdoc />
        public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>()
        {
            ThrowIfDisposed();

            var key = (typeof(TKey), typeof(TMessage));

            lock (_cacheLock)
            {
                if (!_keyedSubscribers.TryGetValue(key, out var subscriber))
                {
                    using (_profilerService.BeginSample("MessagePipeBusService.GetKeyedSubscriber"))
                    {
                        // Resolve both keyed and global subscribers
                        var keyedSubscriber = _dependencyProvider.Resolve<ISubscriber<TKey, TMessage>>();
                        if (keyedSubscriber == null)
                        {
                            throw new InvalidOperationException(
                                $"Failed to resolve ISubscriber<{typeof(TKey).Name}, {typeof(TMessage).Name}> from dependency provider. " +
                                "Ensure MessagePipe is properly configured in your DI container.");
                        }

                        // Also resolve the global subscriber for subscribing to all keys
                        var globalSubscriber = _dependencyProvider.Resolve<ISubscriber<TMessage>>();
                        if (globalSubscriber == null)
                        {
                            throw new InvalidOperationException(
                                $"Failed to resolve ISubscriber<{typeof(TMessage).Name}> from dependency provider. " +
                                "This is required for global keyed subscriptions.");
                        }

                        subscriber = new MessagePipeKeyedSubscriber<TKey, TMessage>(
                            keyedSubscriber, 
                            globalSubscriber, 
                            _logger, 
                            _profilerService);
                            
                        _keyedSubscribers[key] = subscriber;

                        _logger.Log(LogLevel.Debug,
                            $"Created keyed subscriber for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}",
                            "MessageBusService");

                        // Register the message type if it implements IMessage and is not already registered
                        if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)) &&
                            !_messageRegistry.IsRegistered(typeof(TMessage)))
                        {
                            _messageRegistry.RegisterMessageType(typeof(TMessage));
                        }
                    }
                }

                return (IKeyedMessageSubscriber<TKey, TMessage>)subscriber;
            }
        }

        /// <inheritdoc />
        public void ClearCaches()
        {
            ThrowIfDisposed();

            lock (_cacheLock)
            {
                // Dispose all publishers and subscribers that implement IDisposable
                DisposeCollection(_publishers.Values);
                DisposeCollection(_subscribers.Values);
                DisposeCollection(_keyedPublishers.Values);
                DisposeCollection(_keyedSubscribers.Values);

                _publishers.Clear();
                _subscribers.Clear();
                _keyedPublishers.Clear();
                _keyedSubscribers.Clear();

                _logger.Log(LogLevel.Info, "MessageBusService caches cleared", "MessageBusService");
            }
        }

        /// <inheritdoc />
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            ThrowIfDisposed();

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            using (_profilerService.BeginSample("MessagePipeBusService.PublishMessage"))
            {
                try
                {
                    // Get the appropriate publisher and publish the message
                    var publisher = GetPublisher<TMessage>();
                    publisher.Publish(message);

                    _logger.Log(LogLevel.Debug,
                        $"Published message of type {typeof(TMessage).Name} with ID {message.Id}",
                        "MessageBusService");
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error publishing message of type {typeof(TMessage).Name}: {ex.Message}",
                        "MessageBusService");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            ThrowIfDisposed();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            using (_profilerService.BeginSample("MessagePipeBusService.SubscribeToMessage"))
            {
                try
                {
                    // Get the appropriate subscriber and subscribe to messages
                    var subscriber = GetSubscriber<TMessage>();
                    var subscription = subscriber.Subscribe(handler);

                    // Track the subscription for cleanup
                    lock (_cacheLock)
                    {
                        _activeSubscriptions.Add(subscription);
                    }

                    _logger.Log(LogLevel.Debug,
                        $"Subscribed to messages of type {typeof(TMessage).Name}",
                        "MessageBusService");

                    // The subscription already includes proper cleanup through the subscriber's tracker
                    // Just need to ensure we remove it from our active subscriptions list when disposed
                    return new ActiveSubscriptionHandle(subscription, () =>
                    {
                        lock (_cacheLock)
                        {
                            _activeSubscriptions.Remove(subscription);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error subscribing to messages of type {typeof(TMessage).Name}: {ex.Message}",
                        "MessageBusService");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToAllMessages(Action<IMessage> handler)
        {
            ThrowIfDisposed();

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            using (_profilerService.BeginSample("MessagePipeBusService.SubscribeToAllMessages"))
            {
                try
                {
                    // Get all registered message types from the registry
                    var messageTypes = _messageRegistry.GetAllMessageTypes()
                        .Where(kvp => typeof(IMessage).IsAssignableFrom(kvp.Key))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    if (!messageTypes.Any())
                    {
                        _logger.Log(LogLevel.Warning, 
                            "No message types registered in the registry", 
                            "MessageBusService");
                        return null;
                    }

                    // Create a composite disposable to store all subscriptions
                    var subscriptions = new List<IDisposable>(messageTypes.Count);

                    foreach (var messageType in messageTypes)
                    {
                        try
                        {
                            // Create a dynamic delegate to forward the messages to the handler
                            var methodInfo = GetType().GetMethod(nameof(CreateForwardingSubscription),
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (methodInfo != null)
                            {
                                var genericMethod = methodInfo.MakeGenericMethod(messageType);
                                var subscription = (IDisposable)genericMethod.Invoke(this, new object[] { handler });
                                
                                if (subscription != null)
                                {
                                    subscriptions.Add(subscription);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogLevel.Warning, 
                                $"Failed to create subscription for message type {messageType.Name}: {ex.Message}", 
                                "MessageBusService");
                        }
                    }

                    // Track all subscriptions
                    lock (_cacheLock)
                    {
                        _activeSubscriptions.AddRange(subscriptions);
                    }

                    _logger.Log(LogLevel.Info,
                        $"Subscribed to all message types ({subscriptions.Count} successful subscriptions out of {messageTypes.Count} types)",
                        "MessageBusService");

                    return new CompositeSubscriptionHandle(subscriptions, () =>
                    {
                        lock (_cacheLock)
                        {
                            foreach (var sub in subscriptions)
                            {
                                _activeSubscriptions.Remove(sub);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error,
                        $"Error subscribing to all messages: {ex.Message}",
                        "MessageBusService");
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public IMessageRegistry GetMessageRegistry()
        {
            ThrowIfDisposed();
            return _messageRegistry;
        }

        /// <summary>
        /// Helper method to create a forwarding subscription for a specific message type.
        /// </summary>
        private IDisposable CreateForwardingSubscription<TMessage>(Action<IMessage> handler) where TMessage : IMessage
        {
            return SubscribeToMessage<TMessage>(message => handler(message));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_cacheLock)
            {
                // Dispose all active subscriptions
                foreach (var subscription in _activeSubscriptions)
                {
                    try
                    {
                        subscription?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Warning, 
                            $"Error disposing subscription: {ex.Message}", 
                            "MessageBusService");
                    }
                }

                _activeSubscriptions.Clear();

                // Clear caches and dispose cached items
                ClearCaches();

                _disposed = true;
            }

            _logger.Log(LogLevel.Info, "MessagePipeBusService disposed", "MessageBusService");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessagePipeBusService));
        }

        private void DisposeCollection(IEnumerable<object> collection)
        {
            foreach (var item in collection)
            {
                if (item is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Warning,
                            $"Error disposing cached item: {ex.Message}",
                            "MessageBusService");
                    }
                }
            }
        }

        /// <summary>
        /// Lightweight handle for tracking active subscriptions in the message bus.
        /// The actual subscription tracking is handled by the subscriber's tracker.
        /// </summary>
        private sealed class ActiveSubscriptionHandle : IDisposable
        {
            private readonly IDisposable _innerSubscription;
            private readonly Action _onDispose;
            private bool _disposed;

            public ActiveSubscriptionHandle(IDisposable innerSubscription, Action onDispose)
            {
                _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    _innerSubscription.Dispose();
                    _onDispose?.Invoke();
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Composite handle for managing multiple subscriptions as a single unit.
        /// </summary>
        private sealed class CompositeSubscriptionHandle : IDisposable
        {
            private readonly List<IDisposable> _subscriptions;
            private readonly Action _onDispose;
            private bool _disposed;

            public CompositeSubscriptionHandle(List<IDisposable> subscriptions, Action onDispose = null)
            {
                _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                try
                {
                    foreach (var subscription in _subscriptions)
                    {
                        try
                        {
                            subscription?.Dispose();
                        }
                        catch
                        {
                            // Suppress exceptions during disposal
                        }
                    }

                    _subscriptions.Clear();
                    _onDispose?.Invoke();
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}