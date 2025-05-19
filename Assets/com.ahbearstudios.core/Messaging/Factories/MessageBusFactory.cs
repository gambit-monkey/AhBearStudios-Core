using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging.Factories
{
    /// <summary>
    /// Factory for creating and caching different types of message buses.
    /// Implements IMessageBusFactory to provide consistent message bus creation.
    /// </summary>
    public class MessageBusFactory : IMessageBusFactory
    {
        private readonly Dictionary<Type, object> _messageBusCache;
        private readonly Dictionary<(Type, Type), object> _keyedMessageBusCache;
        private readonly object _cacheLock = new object();
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the MessageBusFactory class.
        /// </summary>
        /// <param name="logger">Optional logger for factory operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageBusFactory(IBurstLogger logger = null, IProfiler profiler = null)
        {
            _messageBusCache = new Dictionary<Type, object>();
            _keyedMessageBusCache = new Dictionary<(Type, Type), object>();
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;

            if (_logger != null)
            {
                _logger.Info("MessageBusFactory initialized");
            }
        }

        /// <inheritdoc/>
        public IMessageBus<TMessage> CreateMessageBus<TMessage>() where TMessage : IMessage
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                Type messageType = typeof(TMessage);

                lock (_cacheLock)
                {
                    // Check if we already have a bus for this message type
                    if (_messageBusCache.TryGetValue(messageType, out object existingBus))
                    {
                        if (_logger != null)
                        {
                            _logger.Debug($"Retrieved cached message bus for type {messageType.Name}");
                        }

                        return (IMessageBus<TMessage>)existingBus;
                    }

                    // Create a new bus
                    var newBus = new DefaultMessageBus<TMessage>(_logger, _profiler);
                    _messageBusCache[messageType] = newBus;

                    if (_logger != null)
                    {
                        _logger.Debug($"Created new message bus for type {messageType.Name}");
                    }

                    return newBus;
                }
            }
        }

        /// <inheritdoc/>
        public IKeyedMessageBus<TKey, TMessage> CreateKeyedMessageBus<TKey, TMessage>()
            where TMessage : IMessage
            where TKey : IEquatable<TKey>
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateKeyedMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                Type keyType = typeof(TKey);
                Type messageType = typeof(TMessage);
                var cacheKey = (keyType, messageType);

                lock (_cacheLock)
                {
                    // Check if we already have a bus for this key+message type combination
                    if (_keyedMessageBusCache.TryGetValue(cacheKey, out object existingBus))
                    {
                        if (_logger != null)
                        {
                            _logger.Debug(
                                $"Retrieved cached keyed message bus for key type {keyType.Name} and message type {messageType.Name}");
                        }

                        return (IKeyedMessageBus<TKey, TMessage>)existingBus;
                    }

                    // Create a new bus
                    var newBus = new DefaultKeyedMessageBus<TKey, TMessage>(_logger, _profiler);
                    _keyedMessageBusCache[cacheKey] = newBus;

                    if (_logger != null)
                    {
                        _logger.Debug(
                            $"Created new keyed message bus for key type {keyType.Name} and message type {messageType.Name}");
                    }

                    return newBus;
                }
            }
        }

        /// <summary>
        /// Creates a customized message bus with specific configuration.
        /// </summary>
        /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
        /// <param name="configureBus">Action to configure the new message bus.</param>
        /// <returns>A new, configured message bus.</returns>
        public IMessageBus<TMessage> CreateCustomMessageBus<TMessage>(Action<DefaultMessageBus<TMessage>> configureBus)
            where TMessage : IMessage
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateCustomMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                if (configureBus == null)
                {
                    throw new ArgumentNullException(nameof(configureBus));
                }

                var newBus = new DefaultMessageBus<TMessage>(_logger, _profiler);
                configureBus(newBus);

                if (_logger != null)
                {
                    _logger.Debug($"Created new custom message bus for type {typeof(TMessage).Name}");
                }

                return newBus;
            }
        }

        /// <summary>
        /// Creates a customized keyed message bus with specific configuration.
        /// </summary>
        /// <typeparam name="TKey">The type of keys used for message filtering.</typeparam>
        /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
        /// <param name="configureBus">Action to configure the new keyed message bus.</param>
        /// <returns>A new, configured keyed message bus.</returns>
        public IKeyedMessageBus<TKey, TMessage> CreateCustomKeyedMessageBus<TKey, TMessage>(
            Action<DefaultKeyedMessageBus<TKey, TMessage>> configureBus)
            where TMessage : IMessage
            where TKey : IEquatable<TKey>
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateCustomKeyedMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                if (configureBus == null)
                {
                    throw new ArgumentNullException(nameof(configureBus));
                }

                var newBus = new DefaultKeyedMessageBus<TKey, TMessage>(_logger, _profiler);
                configureBus(newBus);

                if (_logger != null)
                {
                    _logger.Debug(
                        $"Created new custom keyed message bus for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}");
                }

                return newBus;
            }
        }

        /// <summary>
        /// Clears the factory's message bus cache.
        /// </summary>
        public void ClearCache()
        {
            using (_profiler?.BeginSample("MessageBusFactory.ClearCache"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                lock (_cacheLock)
                {
                    // Dispose cached buses
                    foreach (var bus in _messageBusCache.Values)
                    {
                        try
                        {
                            ((IDisposable)bus).Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error disposing message bus: {ex.Message}");
                            }
                        }
                    }

                    foreach (var bus in _keyedMessageBusCache.Values)
                    {
                        try
                        {
                            ((IDisposable)bus).Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error disposing keyed message bus: {ex.Message}");
                            }
                        }
                    }

                    _messageBusCache.Clear();
                    _keyedMessageBusCache.Clear();

                    if (_logger != null)
                    {
                        _logger.Debug("Message bus cache cleared");
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the factory and all cached message buses.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageBusFactory.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message bus factory.
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
                ClearCache();
            }

            _isDisposed = true;

            if (_logger != null)
            {
                _logger.Debug("MessageBusFactory disposed");
            }
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessageBusFactory()
        {
            Dispose(false);
        }

        public IMessageBus<TMessage> CreateBus<TMessage>() where TMessage : IMessage
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                // This can reuse the existing CreateMessageBus implementation
                return CreateMessageBus<TMessage>();
            }
        }

        public object CreateBus(Type messageType)
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                if (messageType == null)
                {
                    throw new ArgumentNullException(nameof(messageType));
                }

                if (!typeof(IMessage).IsAssignableFrom(messageType))
                {
                    throw new ArgumentException("Type must implement IMessage", nameof(messageType));
                }

                lock (_cacheLock)
                {
                    // Check if we already have a bus for this message type
                    if (_messageBusCache.TryGetValue(messageType, out object existingBus))
                    {
                        if (_logger != null)
                        {
                            _logger.Debug($"Retrieved cached message bus for type {messageType.Name}");
                        }

                        return existingBus;
                    }

                    // Create a new bus using reflection
                    Type busType = typeof(DefaultMessageBus<>).MakeGenericType(messageType);
                    var newBus = Activator.CreateInstance(busType, _logger, _profiler);
                    _messageBusCache[messageType] = newBus;

                    if (_logger != null)
                    {
                        _logger.Debug($"Created new message bus for type {messageType.Name}");
                    }

                    return newBus;
                }
            }
        }

        /// <summary>
        /// Creates a typed message bus that can handle multiple message types.
        /// </summary>
        /// <returns>A new typed message bus.</returns>
        public ITypedMessageBus CreateTypedBus()
        {
            using (_profiler?.BeginSample("MessageBusFactory.CreateTypedMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusFactory));
                }

                var typedBus = new TypedMessageBus(this, _logger, _profiler);
        
                if (_logger != null)
                {
                    _logger.Debug("Created new typed message bus");
                }
        
                return typedBus;
            }
        }
    }
}