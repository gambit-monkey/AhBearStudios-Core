using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of IMessageBus that uses MessagePipe as the underlying messaging system.
    /// Provides high-performance publish/subscribe functionality while maintaining compatibility
    /// with the existing messaging architecture.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public sealed class MessagePipeBus<TMessage> : IMessageBus<TMessage>, IDisposable 
        where TMessage : IMessage
    {
        private readonly IPublisher<TMessage> _publisher;
        private readonly ISubscriber<TMessage> _subscriber;
        private readonly IAsyncPublisher<TMessage> _asyncPublisher;
        private readonly IAsyncSubscriber<TMessage> _asyncSubscriber;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Dictionary<IDisposable, MessagePipeSubscriptionToken> _subscriptions;
        private readonly ServiceProvider _serviceProvider;
        private readonly object _subscriptionsLock = new object();
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the MessagePipeBus class.
        /// </summary>
        /// <param name="logger">Optional logger for message bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessagePipeBus(IBurstLogger logger = null, IProfiler profiler = null)
        {
            _logger = logger;
            _profiler = profiler;
            _subscriptions = new Dictionary<IDisposable, MessagePipeSubscriptionToken>();
            
            // Configure and create the service provider with MessagePipe
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            // Get the necessary MessagePipe services
            _publisher = _serviceProvider.GetRequiredService<IPublisher<TMessage>>();
            _subscriber = _serviceProvider.GetRequiredService<ISubscriber<TMessage>>();
            _asyncPublisher = _serviceProvider.GetRequiredService<IAsyncPublisher<TMessage>>();
            _asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<TMessage>>();
            
            if (_logger != null)
            {
                _logger.Info($"MessagePipeBus<{typeof(TMessage).Name}> initialized");
            }
        }

        /// <summary>
        /// Initializes a new instance of the MessagePipeBus class with an existing service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider containing MessagePipe services.</param>
        /// <param name="logger">Optional logger for message bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessagePipeBus(IServiceProvider serviceProvider, IBurstLogger logger = null, IProfiler profiler = null)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            
            _logger = logger;
            _profiler = profiler;
            _subscriptions = new Dictionary<IDisposable, MessagePipeSubscriptionToken>();
            
            // Store the service provider
            _serviceProvider = serviceProvider as ServiceProvider 
                ?? throw new ArgumentException("Service provider must be a ServiceProvider instance", nameof(serviceProvider));
            
            // Get the necessary MessagePipe services
            _publisher = _serviceProvider.GetRequiredService<IPublisher<TMessage>>();
            _subscriber = _serviceProvider.GetRequiredService<ISubscriber<TMessage>>();
            _asyncPublisher = _serviceProvider.GetRequiredService<IAsyncPublisher<TMessage>>();
            _asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<TMessage>>();
            
            if (_logger != null)
            {
                _logger.Info($"MessagePipeBus<{typeof(TMessage).Name}> initialized with existing service provider");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("MessagePipeBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                // Create a wrapper handler that converts from TMessage to T
                Action<TMessage> wrapperHandler = message =>
                {
                    if (message is T typedMessage)
                    {
                        handler(typedMessage);
                    }
                };
                
                // Create a filter for the specific message type
                var filter = new MessageTypeFilter<TMessage, T>();
                
                // Subscribe to MessagePipe with the filter
                var subscription = _subscriber.Subscribe(wrapperHandler, filter);
                
                // Create a subscription token
                var token = new MessagePipeSubscriptionToken(
                    Guid.NewGuid(),
                    typeof(T),
                    subscription);
                
                // Store the subscription mapping
                lock (_subscriptionsLock)
                {
                    _subscriptions[subscription] = token;
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed to {typeof(T).Name} messages with token {token.Id}");
                }
                
                return token;
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("MessagePipeBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                // Create a wrapper handler that converts from TMessage to T
                Func<TMessage, Task> wrapperHandler = async message =>
                {
                    if (message is T typedMessage)
                    {
                        await handler(typedMessage);
                    }
                };
                
                // Create a filter for the specific message type
                var filter = new MessageTypeFilter<TMessage, T>();
                
                // Subscribe to MessagePipe with the filter
                var subscription = _asyncSubscriber.Subscribe(wrapperHandler, filter);
                
                // Create a subscription token
                var token = new MessagePipeSubscriptionToken(
                    Guid.NewGuid(),
                    typeof(T),
                    subscription);
                
                // Store the subscription mapping
                lock (_subscriptionsLock)
                {
                    _subscriptions[subscription] = token;
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed async handler to {typeof(T).Name} messages with token {token.Id}");
                }
                
                return token;
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("MessagePipeBus.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                // Subscribe directly to MessagePipe without a filter
                var subscription = _subscriber.Subscribe(handler);
                
                // Create a subscription token
                var token = new MessagePipeSubscriptionToken(
                    Guid.NewGuid(),
                    typeof(TMessage),
                    subscription);
                
                // Store the subscription mapping
                lock (_subscriptionsLock)
                {
                    _subscriptions[subscription] = token;
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed to all {typeof(TMessage).Name} messages with token {token.Id}");
                }
                
                return token;
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("MessagePipeBus.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                // Subscribe directly to MessagePipe without a filter
                var subscription = _asyncSubscriber.Subscribe(handler);
                
                // Create a subscription token
                var token = new MessagePipeSubscriptionToken(
                    Guid.NewGuid(),
                    typeof(TMessage),
                    subscription);
                
                // Store the subscription mapping
                lock (_subscriptionsLock)
                {
                    _subscriptions[subscription] = token;
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed async handler to all {typeof(TMessage).Name} messages with token {token.Id}");
                }
                
                return token;
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("MessagePipeBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }

                if (!(token is MessagePipeSubscriptionToken messagePipeToken))
                {
                    throw new ArgumentException("Token is not a valid MessagePipe subscription token", nameof(token));
                }
                
                // Get the MessagePipe subscription
                IDisposable subscription = messagePipeToken.Subscription;
                
                if (subscription != null)
                {
                    // Dispose the subscription
                    subscription.Dispose();
                    
                    // Remove from our mapping
                    lock (_subscriptionsLock)
                    {
                        _subscriptions.Remove(subscription);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Unsubscribed token {token.Id}");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Publish(TMessage message)
        {
            using (_profiler?.BeginSample("MessagePipeBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                try
                {
                    // Publish the message using MessagePipe
                    _publisher.Publish(message);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Published message of type {message.GetType().Name} with ID {message.Id}");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error publishing message: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("MessagePipeBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                try
                {
                    // Publish the message using MessagePipe
                    await _asyncPublisher.PublishAsync(message, cancellationToken);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Published message of type {message.GetType().Name} with ID {message.Id} asynchronously");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error publishing message asynchronously: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a MessagePipe filter for filtering messages by type.
        /// </summary>
        /// <typeparam name="TFilter">The specific message type to filter for.</typeparam>
        /// <returns>A MessagePipe filter that only passes messages of the specified type.</returns>
        public ISubscriptionFilter CreateTypeFilter<TFilter>() where TFilter : TMessage
        {
            return new MessageTypeFilter<TMessage, TFilter>();
        }

        /// <summary>
        /// Creates a MessagePipe filter for filtering messages by a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter messages with.</param>
        /// <returns>A MessagePipe filter that passes messages that satisfy the predicate.</returns>
        public ISubscriptionFilter CreatePredicateFilter(Predicate<TMessage> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            
            return new PredicateFilter<TMessage>(predicate);
        }

        /// <summary>
        /// Gets the underlying MessagePipe publisher.
        /// </summary>
        /// <returns>The MessagePipe publisher.</returns>
        public IPublisher<TMessage> GetPublisher()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
            }
            
            return _publisher;
        }

        /// <summary>
        /// Gets the underlying MessagePipe subscriber.
        /// </summary>
        /// <returns>The MessagePipe subscriber.</returns>
        public ISubscriber<TMessage> GetSubscriber()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
            }
            
            return _subscriber;
        }

        /// <summary>
        /// Gets the service provider used by this MessagePipeBus.
        /// </summary>
        /// <returns>The service provider.</returns>
        public IServiceProvider GetServiceProvider()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
            }
            
            return _serviceProvider;
        }

        /// <summary>
        /// Configures the services for MessagePipe.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Configure MessagePipe
            services.AddMessagePipe(options =>
            {
                // Configure for optimal Unity performance
                options.DefaultPublishStrategy = PublishStrategy.Parallel;
                options.EnableCaptureStackTrace = false; // Disable for performance
                
                // Configure error handling
                options.Diagnostics.GlobalHandleMessageHandlerException = (x, ex) =>
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error in message handler: {ex.Message}");
                    }
                };
                
                options.Diagnostics.GlobalHandleAsyncMessageHandlerException = (x, ex) =>
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error in async message handler: {ex.Message}");
                    }
                    
                    return Task.CompletedTask;
                };
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            
            lock (_subscriptionsLock)
            {
                if (_isDisposed)
                {
                    return;
                }
                
                // Dispose all subscriptions
                foreach (var subscription in _subscriptions.Keys)
                {
                    subscription.Dispose();
                }
                
                _subscriptions.Clear();
                
                // Dispose the service provider
                _serviceProvider.Dispose();
                
                _isDisposed = true;
                
                if (_logger != null)
                {
                    _logger.Info($"MessagePipeBus<{typeof(TMessage).Name}> disposed");
                }
            }
        }

        /// <summary>
        /// MessagePipe filter for filtering messages by type.
        /// </summary>
        /// <typeparam name="TBaseMessage">The base message type.</typeparam>
        /// <typeparam name="TFilteredMessage">The specific message type to filter for.</typeparam>
        private class MessageTypeFilter<TBaseMessage, TFilteredMessage> : ISubscriptionFilter
            where TFilteredMessage : TBaseMessage
        {
            /// <summary>
            /// Determines whether the message should be delivered to the subscriber.
            /// </summary>
            /// <param name="message">The message to check.</param>
            /// <returns>True if the message passes the filter; otherwise, false.</returns>
            public bool Apply(TBaseMessage message)
            {
                return message is TFilteredMessage;
            }
        }
    }

    /// <summary>
    /// Implementation of ISubscriptionToken for MessagePipe subscriptions.
    /// </summary>
    public sealed class MessagePipeSubscriptionToken : ISubscriptionToken, IDisposable
    {
        private readonly object _lockObj = new object();
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the underlying MessagePipe subscription.
        /// </summary>
        public IDisposable Subscription { get; }
        
        /// <summary>
        /// Gets a unique identifier for this subscription.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the type of message this subscription is for.
        /// </summary>
        public Type MessageType { get; }
        
        /// <summary>
        /// Gets a value indicating whether this subscription is active.
        /// </summary>
        public bool IsActive
        {
            get { return !_isDisposed && Subscription != null; }
        }

        /// <summary>
        /// Initializes a new instance of the MessagePipeSubscriptionToken class.
        /// </summary>
        /// <param name="id">The unique identifier for this subscription.</param>
        /// <param name="messageType">The type of message this subscription is for.</param>
        /// <param name="subscription">The underlying MessagePipe subscription.</param>
        public MessagePipeSubscriptionToken(Guid id, Type messageType, IDisposable subscription)
        {
            Id = id;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        }

        /// <summary>
        /// Disposes this subscription token.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                
                // Dispose the underlying subscription
                Subscription.Dispose();
                
                _isDisposed = true;
            }
        }
    }
}