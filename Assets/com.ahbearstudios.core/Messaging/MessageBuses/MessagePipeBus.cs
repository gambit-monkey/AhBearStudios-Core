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
    /// Message bus implementation that wraps the Cysharp MessagePipe library to leverage its
    /// efficient pub/sub capabilities, filtering, and async support within our messaging architecture.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public class MessagePipeBus<TMessage> : IMessageBus<TMessage>, IDisposable where TMessage : IMessage
    {
        private readonly IPublisher<TMessage> _publisher;
        private readonly ISubscriber<TMessage> _subscriber;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly List<IDisposable> _subscriptions;
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
            _subscriptions = new List<IDisposable>();
            _isDisposed = false;
            
            // Initialize MessagePipe
            var services = new ServiceCollection();
            
            // Configure MessagePipe with options
            services.AddMessagePipe(options =>
            {
                // Set reasonable defaults for Unity games
                options.DefaultPublishStrategy = PublishStrategy.Parallel;
                options.EnableCaptureStackTrace = false; // Disable for performance
                options.InstanceLifetime = InstanceLifetime.Singleton;
            });
            
            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            
            // Get publisher and subscriber instances
            _publisher = _serviceProvider.GetRequiredService<IPublisher<TMessage>>();
            _subscriber = _serviceProvider.GetRequiredService<ISubscriber<TMessage>>();
            
            if (_logger != null)
            {
                _logger.Info("MessagePipeBus initialized with Cysharp MessagePipe");
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
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
            _profiler = profiler;
            _subscriptions = new List<IDisposable>();
            _isDisposed = false;
            
            // Get publisher and subscriber instances
            _publisher = _serviceProvider.GetRequiredService<IPublisher<TMessage>>();
            _subscriber = _serviceProvider.GetRequiredService<ISubscriber<TMessage>>();
            
            if (_logger != null)
            {
                _logger.Info("MessagePipeBus initialized with existing service provider");
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

                // Create a wrapper delegate that converts from TMessage to T
                Action<TMessage> wrapperHandler = message =>
                {
                    if (message is T typedMessage)
                    {
                        handler(typedMessage);
                    }
                };
                
                // Subscribe to MessagePipe
                var subscription = _subscriber.Subscribe(wrapperHandler);
                
                // Create a subscription token that will dispose the MessagePipe subscription
                var token = new MessagePipeSubscriptionToken(subscription, typeof(T));
                
                // Store the subscription for cleanup
                lock (_subscriptionsLock)
                {
                    _subscriptions.Add(subscription);
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed to {typeof(T).Name} messages");
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

                // Create a wrapper delegate that converts from TMessage to T
                Func<TMessage, Task> wrapperHandler = async message =>
                {
                    if (message is T typedMessage)
                    {
                        await handler(typedMessage);
                    }
                };
                
                // Get the async subscriber
                var asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<TMessage>>();
                
                // Subscribe to MessagePipe
                var subscription = asyncSubscriber.Subscribe(wrapperHandler);
                
                // Create a subscription token that will dispose the MessagePipe subscription
                var token = new MessagePipeSubscriptionToken(subscription, typeof(T));
                
                // Store the subscription for cleanup
                lock (_subscriptionsLock)
                {
                    _subscriptions.Add(subscription);
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed async handler to {typeof(T).Name} messages");
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

                // Subscribe to MessagePipe
                var subscription = _subscriber.Subscribe(handler);
                
                // Create a subscription token that will dispose the MessagePipe subscription
                var token = new MessagePipeSubscriptionToken(subscription, typeof(TMessage));
                
                // Store the subscription for cleanup
                lock (_subscriptionsLock)
                {
                    _subscriptions.Add(subscription);
                }
                
                if (_logger != null)
                {
                    _logger.Debug("Subscribed to all messages");
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

                // Get the async subscriber
                var asyncSubscriber = _serviceProvider.GetRequiredService<IAsyncSubscriber<TMessage>>();
                
                // Subscribe to MessagePipe
                var subscription = asyncSubscriber.Subscribe(handler);
                
                // Create a subscription token that will dispose the MessagePipe subscription
                var token = new MessagePipeSubscriptionToken(subscription, typeof(TMessage));
                
                // Store the subscription for cleanup
                lock (_subscriptionsLock)
                {
                    _subscriptions.Add(subscription);
                }
                
                if (_logger != null)
                {
                    _logger.Debug("Subscribed async handler to all messages");
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
                
                // Dispose the underlying subscription
                messagePipeToken.Dispose();
                
                // Remove from our subscriptions list
                lock (_subscriptionsLock)
                {
                    _subscriptions.Remove(messagePipeToken.Subscription);
                }
                
                if (_logger != null)
                {
                    _logger.Debug($"Unsubscribed from {messagePipeToken.MessageType.Name} messages");
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
                    // Get the async publisher
                    var asyncPublisher = _serviceProvider.GetRequiredService<IAsyncPublisher<TMessage>>();
                    
                    // Publish the message using MessagePipe
                    await asyncPublisher.PublishAsync(message, cancellationToken);
                    
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
        /// Creates a filter for messages.
        /// </summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>A MessagePipe filter.</returns>
        public MessagePipe.ISubscriptionFilter CreateFilter(Predicate<TMessage> predicate)
        {
            using (_profiler?.BeginSample("MessagePipeBus.CreateFilter"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                if (predicate == null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }
                
                // Create a MessagePipe filter using the predicate
                return new MessagePipe.PredicateFilter<TMessage>(predicate);
            }
        }

        /// <summary>
        /// Gets the underlying MessagePipe service provider.
        /// </summary>
        /// <returns>The service provider.</returns>
        public IServiceProvider GetServiceProvider()
        {
            using (_profiler?.BeginSample("MessagePipeBus.GetServiceProvider"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessagePipeBus<TMessage>));
                }

                return _serviceProvider;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessagePipeBus.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message pipe bus.
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
                // Dispose all subscriptions
                lock (_subscriptionsLock)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Dispose();
                    }
                    
                    _subscriptions.Clear();
                }
                
                // Dispose the service provider if it's disposable
                if (_serviceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                
                if (_logger != null)
                {
                    _logger.Info("MessagePipeBus disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessagePipeBus()
        {
            Dispose(false);
        }

        /// <summary>
        /// Implementation of ISubscriptionToken for MessagePipe subscriptions.
        /// </summary>
        private class MessagePipeSubscriptionToken : ISubscriptionToken, IDisposable
        {
            /// <summary>
            /// The underlying MessagePipe subscription.
            /// </summary>
            public IDisposable Subscription { get; }
            
            /// <summary>
            /// The type of message this subscription is for.
            /// </summary>
            public Type MessageType { get; }
            
            /// <summary>
            /// Gets a unique identifier for this subscription.
            /// </summary>
            public Guid Id { get; }
            
            /// <summary>
            /// Gets a value indicating whether this subscription is active.
            /// </summary>
            public bool IsActive { get; private set; }
            
            /// <summary>
            /// Initializes a new instance of the MessagePipeSubscriptionToken class.
            /// </summary>
            /// <param name="subscription">The underlying MessagePipe subscription.</param>
            /// <param name="messageType">The type of message this subscription is for.</param>
            public MessagePipeSubscriptionToken(IDisposable subscription, Type messageType)
            {
                Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
                MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
                Id = Guid.NewGuid();
                IsActive = true;
            }
            
            /// <summary>
            /// Disposes this subscription.
            /// </summary>
            public void Dispose()
            {
                if (IsActive)
                {
                    Subscription.Dispose();
                    IsActive = false;
                }
            }
        }
    }
}