using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Base class for keyed message handlers that provides logging and profiling support.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to handle.</typeparam>
    public abstract class BaseKeyedMessageHandler<TKey, TMessage> : IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly IDisposable _subscription;
        private readonly ProfilerTag _handlerTag;
        
        /// <summary>
        /// Initializes a new instance of the BaseKeyedMessageHandler class.
        /// </summary>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        /// <param name="key">The key to subscribe to.</param>
        protected BaseKeyedMessageHandler(IMessageBus messageBus, IBurstLogger logger, IProfiler profiler, TKey key)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            var messageType = typeof(TMessage);
            var keyType = typeof(TKey);
            var handlerType = GetType();
            
            _handlerTag = new ProfilerTag(new ProfilerCategory("MessageHandler"), $"{handlerType.Name}_{keyType.Name}_{messageType.Name}");
            
            _subscription = _messageBus.Subscribe<TKey, TMessage>(key, HandleMessageInternal);
            
            _logger.Log(LogLevel.Debug, $"Registered keyed message handler {handlerType.Name} for key type {keyType.Name} and message type {messageType.Name}", "MessageHandler");
        }
        
        private void HandleMessageInternal(TMessage message)
        {
            using var scope = _profiler.BeginScope(_handlerTag);
            
            _logger.Log(LogLevel.Trace, $"Handling keyed message of type {typeof(TMessage).Name}", "MessageHandler");
            
            try
            {
                HandleMessage(message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error handling keyed message: {ex.Message}", "MessageHandler");
                throw;
            }
        }
        
        /// <summary>
        /// Handles a message of the specified type.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        protected abstract void HandleMessage(TMessage message);
        
        /// <summary>
        /// Disposes the message handler and unsubscribes from the message bus.
        /// </summary>
        public virtual void Dispose()
        {
            _subscription.Dispose();
            _logger.Log(LogLevel.Debug, $"Unregistered keyed message handler {GetType().Name} for message type {typeof(TMessage).Name}", "MessageHandler");
        }
    }
}