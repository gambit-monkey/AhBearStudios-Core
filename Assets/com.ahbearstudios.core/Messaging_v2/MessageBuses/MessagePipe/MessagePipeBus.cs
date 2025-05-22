using System;
using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Implementation;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Messaging.MessageBuses.MessagePipe;
using AhBearStudios.Core.Messaging.Registration;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of the IMessageBus interface using MessagePipe.
    /// </summary>
    public sealed class MessagePipeBus : IMessageBus, IDisposable
    {
        private readonly IDependencyProvider _dependencyProvider;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly CachedMessageRegistry _messageRegistry;
        
        private readonly Dictionary<Type, object> _publishers = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _subscribers = new Dictionary<Type, object>();
        private readonly Dictionary<(Type, Type), object> _keyedPublishers = new Dictionary<(Type, Type), object>();
        private readonly Dictionary<(Type, Type), object> _keyedSubscribers = new Dictionary<(Type, Type), object>();
        
        /// <summary>
        /// Initializes a new instance of the MessageBus class.
        /// </summary>
        /// <param name="dependencyProvider">The dependency provider to use for resolving MessagePipe services.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        /// <param name="messageRegistry">The message registry to use for message discovery.</param>
        public MessagePipeBus(IDependencyProvider dependencyProvider, IBurstLogger logger, IProfiler profiler, IMessageRegistry messageRegistry)
        {
            _dependencyProvider = dependencyProvider ?? throw new ArgumentNullException(nameof(dependencyProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _messageRegistry = messageRegistry ?? throw new ArgumentNullException(nameof(messageRegistry));
            
            _logger.Log(LogLevel.Info, "MessageBus initialized", "MessageBus");
        }
        
        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>()
        {
            var messageType = typeof(TMessage);
            
            if (!_publishers.TryGetValue(messageType, out var publisher))
            {
                var messagePipePublisher = _dependencyProvider.Resolve<IPublisher<TMessage>>();
                publisher = new MessagePipePublisher<TMessage>(messagePipePublisher, _logger, _profiler);
                _publishers[messageType] = publisher;
                
                _logger.Log(LogLevel.Debug, $"Created publisher for message type {messageType.Name}", "MessageBus");
                
                // Register the message type if it implements IMessage and is not already registered
                if (typeof(IMessage).IsAssignableFrom(messageType) && !_messageRegistry.IsRegistered(messageType))
                {
                    _messageRegistry.RegisterMessageType(messageType);
                }
            }
            
            return (IMessagePublisher<TMessage>)publisher;
        }
        
        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>()
        {
            var messageType = typeof(TMessage);
            
            if (!_subscribers.TryGetValue(messageType, out var subscriber))
            {
                var messagePipeSubscriber = _dependencyProvider.Resolve<ISubscriber<TMessage>>();
                subscriber = new MessagePipeSubscriber<TMessage>(messagePipeSubscriber, _logger, _profiler);
                _subscribers[messageType] = subscriber;
                
                _logger.Log(LogLevel.Debug, $"Created subscriber for message type {messageType.Name}", "MessageBus");
                
                // Register the message type if it implements IMessage and is not already registered
                if (typeof(IMessage).IsAssignableFrom(messageType) && !_messageRegistry.IsRegistered(messageType))
                {
                    _messageRegistry.RegisterMessageType(messageType);
                }
            }
            
            return (IMessageSubscriber<TMessage>)subscriber;
        }
        
        /// <inheritdoc />
        public IKeyedMessagePublisher<TKey, TMessage> GetPublisher<TKey, TMessage>()
        {
            var key = (typeof(TKey), typeof(TMessage));
            
            if (!_keyedPublishers.TryGetValue(key, out var publisher))
            {
                var messagePipePublisher = _dependencyProvider.Resolve<IPublisher<TKey, TMessage>>();
                publisher = new MessagePipeKeyedPublisher<TKey, TMessage>(messagePipePublisher, _logger, _profiler);
                _keyedPublishers[key] = publisher;
                
                _logger.Log(LogLevel.Debug, $"Created keyed publisher for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}", "MessageBus");
                
                // Register the message type if it implements IMessage and is not already registered
                if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)) && !_messageRegistry.IsRegistered(typeof(TMessage)))
                {
                    _messageRegistry.RegisterMessageType(typeof(TMessage));
                }
            }
            
            return (IKeyedMessagePublisher<TKey, TMessage>)publisher;
        }
        
        /// <inheritdoc />
        public IKeyedMessageSubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>()
        {
            var key = (typeof(TKey), typeof(TMessage));
            
            if (!_keyedSubscribers.TryGetValue(key, out var subscriber))
            {
                var messagePipeSubscriber = _dependencyProvider.Resolve<ISubscriber<TKey, TMessage>>();
                subscriber = new MessagePipeKeyedSubscriber<TKey, TMessage>(messagePipeSubscriber, _logger, _profiler);
                _keyedSubscribers[key] = subscriber;
                
                _logger.Log(LogLevel.Debug, $"Created keyed subscriber for key type {typeof(TKey).Name} and message type {typeof(TMessage).Name}", "MessageBus");
                
                // Register the message type if it implements IMessage and is not already registered
                if (typeof(IMessage).IsAssignableFrom(typeof(TMessage)) && !_messageRegistry.IsRegistered(typeof(TMessage)))
                {
                    _messageRegistry.RegisterMessageType(typeof(TMessage));
                }
            }
            
            return (IKeyedMessageSubscriber<TKey, TMessage>)subscriber;
        }
        
        /// <inheritdoc />
        public void ClearCaches()
        {
            _publishers.Clear();
            _subscribers.Clear();
            _keyedPublishers.Clear();
            _keyedSubscribers.Clear();
            
            _logger.Log(LogLevel.Info, "MessageBus caches cleared", "MessageBus");
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            _publishers.Clear();
            _subscribers.Clear();
            _keyedPublishers.Clear();
            _keyedSubscribers.Clear();
            
            _logger.Log(LogLevel.Info, "MessageBus disposed", "MessageBus");
        }
    }
}