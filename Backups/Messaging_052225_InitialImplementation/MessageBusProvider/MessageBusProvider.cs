using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Factories;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Service locator for message buses, providing centralized access to message buses throughout the application.
    /// Designed to be DI-friendly and support singleton pattern.
    /// </summary>
    public class MessageBusProvider : IDisposable
    {
        private readonly IMessageBusFactory _factory;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private static MessageBusProvider _instance;
        private static readonly object _instanceLock = new object();
        private bool _isDisposed;

        /// <summary>
        /// Gets the singleton instance of the MessageBusProvider.
        /// </summary>
        public static MessageBusProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MessageBusProvider();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the MessageBusProvider class with default settings.
        /// </summary>
        public MessageBusProvider() 
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MessageBusProvider class.
        /// </summary>
        /// <param name="factory">Optional custom message bus factory.</param>
        /// <param name="logger">Optional logger for provider operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageBusProvider(IMessageBusFactory factory = null, IBurstLogger logger = null, IProfiler profiler = null)
        {
            _factory = factory ?? new MessageBusFactory(logger, profiler);
            _logger = logger;
            _profiler = profiler;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info("MessageBusProvider initialized");
            }
        }

        /// <summary>
        /// Gets a message bus for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of messages the bus will handle.</typeparam>
        /// <returns>A message bus for the specified message type.</returns>
        public IMessageBus<TMessage> GetMessageBus<TMessage>() where TMessage : IMessage
        {
            using (_profiler?.BeginSample("MessageBusProvider.GetMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusProvider));
                }

                var bus = _factory.CreateMessageBus<TMessage>();
                
                if (_logger != null)
                {
                    _logger.Debug($"Provided message bus for type {typeof(TMessage).Name}");
                }
                
                return bus;
            }
        }

        /// <summary>
        /// Gets a keyed message bus for the specified key and message types.
        /// </summary>
        /// <typeparam name="TKey">The type of keys used for message filtering.</typeparam>
        /// <typeparam name="TMessage">The type of messages the bus will handle.</typeparam>
        /// <returns>A keyed message bus for the specified key and message types.</returns>
        public IKeyedMessageBus<TKey, TMessage> GetKeyedMessageBus<TKey, TMessage>() 
            where TMessage : IMessage 
            where TKey : IEquatable<TKey>
        {
            using (_profiler?.BeginSample("MessageBusProvider.GetKeyedMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusProvider));
                }

                var bus = _factory.CreateKeyedMessageBus<TKey, TMessage>();
                
                if (_logger != null)
                {
                    _logger.Debug($"Provided keyed message bus for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}");
                }
                
                return bus;
            }
        }

        /// <summary>
        /// Creates a new default message bus for the specified message type.
        /// This bus is not cached by the factory.
        /// </summary>
        /// <typeparam name="TMessage">The type of messages the bus will handle.</typeparam>
        /// <returns>A new message bus for the specified message type.</returns>
        public IMessageBus<TMessage> CreateNewMessageBus<TMessage>() where TMessage : IMessage
        {
            using (_profiler?.BeginSample("MessageBusProvider.CreateNewMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusProvider));
                }

                var bus = new DefaultMessageBus<TMessage>(_logger, _profiler);
                
                if (_logger != null)
                {
                    _logger.Debug($"Created new message bus for type {typeof(TMessage).Name}");
                }
                
                return bus;
            }
        }

        /// <summary>
        /// Creates a new default keyed message bus for the specified key and message types.
        /// This bus is not cached by the factory.
        /// </summary>
        /// <typeparam name="TKey">The type of keys used for message filtering.</typeparam>
        /// <typeparam name="TMessage">The type of messages the bus will handle.</typeparam>
        /// <returns>A new keyed message bus for the specified key and message types.</returns>
        public IKeyedMessageBus<TKey, TMessage> CreateNewKeyedMessageBus<TKey, TMessage>() 
            where TMessage : IMessage 
            where TKey : IEquatable<TKey>
        {
            using (_profiler?.BeginSample("MessageBusProvider.CreateNewKeyedMessageBus"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusProvider));
                }

                var bus = new DefaultKeyedMessageBus<TKey, TMessage>(_logger, _profiler);
                
                if (_logger != null)
                {
                    _logger.Debug($"Created new keyed message bus for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}");
                }
                
                return bus;
            }
        }

        /// <summary>
        /// Resets the provider, clearing any cached buses in the factory.
        /// </summary>
        public void Reset()
        {
            using (_profiler?.BeginSample("MessageBusProvider.Reset"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusProvider));
                }

                if (_factory is MessageBusFactory factory)
                {
                    factory.ClearCache();
                    
                    if (_logger != null)
                    {
                        _logger.Debug("MessageBusProvider reset");
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the provider and its factory.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageBusProvider.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message bus provider.
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
                _factory.Dispose();
                
                // If this is the singleton instance, null it out
                lock (_instanceLock)
                {
                    if (ReferenceEquals(this, _instance))
                    {
                        _instance = null;
                    }
                }
            }

            _isDisposed = true;
            
            if (_logger != null)
            {
                _logger.Debug("MessageBusProvider disposed");
            }
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessageBusProvider()
        {
            Dispose(false);
        }
    }
}