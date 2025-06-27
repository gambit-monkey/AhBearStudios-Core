using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Base class for message handlers that provides logging and profiling support.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to handle.</typeparam>
    public abstract class BaseMessageHandler<TMessage> : IDisposable
    {
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _logger;
        private readonly IProfiler _profiler;
        private readonly IDisposable _subscription;
        private readonly ProfilerTag _handlerTag;
        
        /// <summary>
        /// Initializes a new instance of the BaseMessageHandler class.
        /// </summary>
        /// <param name="messageBusService">The message bus to use.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        protected BaseMessageHandler(IMessageBusService messageBusService, ILoggingService logger, IProfiler profiler)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            var messageType = typeof(TMessage);
            var handlerType = GetType();
            
            _handlerTag = new ProfilerTag(new ProfilerCategory("MessageHandler"), $"{handlerType.Name}_{messageType.Name}");
            
            _subscription = _messageBusService.Subscribe<TMessage>(HandleMessageInternal);
            
            _logger.Log(LogLevel.Debug, $"Registered message handler {handlerType.Name} for message type {messageType.Name}", "MessageHandler");
        }
        
        private void HandleMessageInternal(TMessage message)
        {
            using var scope = _profiler.BeginScope(_handlerTag);
            
            _logger.Log(LogLevel.Trace, $"Handling message of type {typeof(TMessage).Name}", "MessageHandler");
            
            try
            {
                HandleMessage(message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error handling message: {ex.Message}", "MessageHandler");
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
            _logger.Log(LogLevel.Debug, $"Unregistered message handler {GetType().Name} for message type {typeof(TMessage).Name}", "MessageHandler");
        }
    }
}