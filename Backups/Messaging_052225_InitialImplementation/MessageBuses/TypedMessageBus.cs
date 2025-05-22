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
    /// Implementation of ITypedMessageBus that handles multiple message types.
    /// Provides a central point for type-specific messaging operations.
    /// </summary>
    public class TypedMessageBus : ITypedMessageBus, IDisposable
    {
        private readonly Dictionary<Type, object> _typedBuses;
        private readonly IMessageBusFactory _messageBusFactory;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly object _busesLock = new object();
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the TypedMessageBus class.
        /// </summary>
        /// <param name="messageBusFactory">The factory to create message buses for specific types.</param>
        /// <param name="logger">Optional logger for typed bus operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public TypedMessageBus(IMessageBusFactory messageBusFactory, IBurstLogger logger = null, IProfiler profiler = null)
        {
            _messageBusFactory = messageBusFactory ?? throw new ArgumentNullException(nameof(messageBusFactory));
            _logger = logger;
            _profiler = profiler;
            _typedBuses = new Dictionary<Type, object>();
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info("TypedMessageBus initialized");
            }
        }

        /// <inheritdoc/>
        public void Publish<TMessage>(TMessage message) where TMessage : IMessage
        {
            using (_profiler?.BeginSample("TypedMessageBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(TypedMessageBus));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Get or create a bus for this message type
                var bus = GetOrCreateBusForType<TMessage>();
                
                // Publish the message to the typed bus
                bus.Publish(message);
                
                if (_logger != null)
                {
                    _logger.Debug($"Published message of type {typeof(TMessage).Name} with ID {message.Id}");
                }
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using (_profiler?.BeginSample("TypedMessageBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(TypedMessageBus));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Get or create a bus for this message type
                var bus = GetOrCreateBusForType<TMessage>();
                
                // Publish the message to the typed bus asynchronously
                await bus.PublishAsync(message, cancellationToken);
                
                if (_logger != null)
                {
                    _logger.Debug($"Published message of type {typeof(TMessage).Name} with ID {message.Id} asynchronously");
                }
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            using (_profiler?.BeginSample("TypedMessageBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(TypedMessageBus));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                // Get or create a bus for this message type
                var bus = GetOrCreateBusForType<TMessage>();
                
                // Subscribe to the typed bus
                var token = bus.Subscribe(handler);
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed to messages of type {typeof(TMessage).Name}");
                }
                
                return token;
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage
        {
            using (_profiler?.BeginSample("TypedMessageBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(TypedMessageBus));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                // Get or create a bus for this message type
                var bus = GetOrCreateBusForType<TMessage>();
                
                // Subscribe to the typed bus asynchronously
                var token = bus.SubscribeAsync(handler);
                
                if (_logger != null)
                {
                    _logger.Debug($"Subscribed to messages of type {typeof(TMessage).Name} asynchronously");
                }
                
                return token;
            }
        }

        /// <inheritdoc/>
        public bool IsMessageTypeRegistered<TMessage>() where TMessage : IMessage
        {
            using (_profiler?.BeginSample("TypedMessageBus.IsMessageTypeRegistered"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(TypedMessageBus));
                }

                lock (_busesLock)
                {
                    return _typedBuses.ContainsKey(typeof(TMessage));
                }
            }
        }

        /// <inheritdoc/>
        public bool IsMessageTypeRegistered(Type messageType)
        {
            using (_profiler?.BeginSample("TypedMessageBus.IsMessageTypeRegistered"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(TypedMessageBus));
                }

                if (messageType == null)
                {
                    throw new ArgumentNullException(nameof(messageType));
                }

                if (!typeof(IMessage).IsAssignableFrom(messageType))
                {
                    throw new ArgumentException("The specified type does not implement IMessage", nameof(messageType));
                }

                lock (_busesLock)
                {
                    return _typedBuses.ContainsKey(messageType);
                }
            }
        }

        /// <summary>
        /// Gets or creates a message bus for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message the bus will handle.</typeparam>
        /// <returns>A message bus for the specified message type.</returns>
        private IMessageBus<TMessage> GetOrCreateBusForType<TMessage>() where TMessage : IMessage
        {
            Type messageType = typeof(TMessage);
            
            lock (_busesLock)
            {
                if (_typedBuses.TryGetValue(messageType, out object existingBus))
                {
                    return (IMessageBus<TMessage>)existingBus;
                }
                
                var newBus = _messageBusFactory.CreateBus<TMessage>();
                _typedBuses[messageType] = newBus;
                
                if (_logger != null)
                {
                    _logger.Debug($"Created new message bus for type {messageType.Name}");
                }
                
                return newBus;
            }
        }

        /// <summary>
        /// Disposes the typed message bus and all created message buses.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("TypedMessageBus.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the typed message bus.
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
                lock (_busesLock)
                {
                    // Dispose all created buses
                    foreach (var bus in _typedBuses.Values)
                    {
                        if (bus is IDisposable disposableBus)
                        {
                            try
                            {
                                disposableBus.Dispose();
                            }
                            catch (Exception ex)
                            {
                                if (_logger != null)
                                {
                                    _logger.Error($"Error disposing message bus: {ex.Message}");
                                }
                            }
                        }
                    }
                    
                    _typedBuses.Clear();
                }
                
                if (_logger != null)
                {
                    _logger.Info("TypedMessageBus disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~TypedMessageBus()
        {
            Dispose(false);
        }
    }
}