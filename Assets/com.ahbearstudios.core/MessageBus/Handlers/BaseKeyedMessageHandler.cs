using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Enhanced base class for keyed message handlers that provides logging and profiling support.
    /// Uses composition over inheritance for better flexibility.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TMessage">The type of message to handle.</typeparam>
    public abstract class BaseKeyedMessageHandler<TKey, TMessage> : IDisposable
    {
        private readonly IMessageHandlerContext _context;
        private readonly IDisposable _subscription;
        
        /// <summary>
        /// Initializes a new instance of the BaseKeyedMessageHandler class for a specific key.
        /// </summary>
        /// <param name="messageBusService">The message bus to use.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        /// <param name="key">The key to subscribe to.</param>
        protected BaseKeyedMessageHandler(
            IMessageBusService messageBusService, 
            ILoggingService logger, 
            IProfiler profiler, 
            TKey key)
        {
            _context = new MessageHandlerContext(messageBusService, logger, profiler, GetType());
            _subscription = CreateSpecificKeySubscription(key);
        }
        
        /// <summary>
        /// Initializes a new instance of the BaseKeyedMessageHandler class for all keys.
        /// </summary>
        /// <param name="messageBusService">The message bus to use.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        protected BaseKeyedMessageHandler(
            IMessageBusService messageBusService, 
            ILoggingService logger, 
            IProfiler profiler)
        {
            _context = new MessageHandlerContext(messageBusService, logger, profiler, GetType());
            _subscription = CreateAllKeysSubscription();
        }

        /// <summary>
        /// Gets the message handler context.
        /// </summary>
        protected IMessageHandlerContext Context => _context;

        private IDisposable CreateSpecificKeySubscription(TKey key)
        {
            return _context.SubscribeKeyed<TKey, TMessage>(key, HandleMessageInternal);
        }

        private IDisposable CreateAllKeysSubscription()
        {
            return _context.SubscribeKeyedAll<TKey, TMessage>(HandleMessageWithKeyInternal);
        }
        
        private void HandleMessageInternal(TMessage message)
        {
            _context.ExecuteWithProfiling($"HandleMessage_{typeof(TMessage).Name}", () =>
            {
                _context.Logger.Log(LogLevel.Trace, 
                    $"Handling keyed message of type {typeof(TMessage).Name}", 
                    "MessageHandler");
                
                try
                {
                    HandleMessage(message);
                }
                catch (Exception ex)
                {
                    _context.Logger.Log(LogLevel.Error, 
                        $"Error handling keyed message: {ex.Message}", 
                        "MessageHandler");
                    throw;
                }
            });
        }

        private void HandleMessageWithKeyInternal(TKey key, TMessage message)
        {
            _context.ExecuteWithProfiling($"HandleMessage_{typeof(TMessage).Name}", () =>
            {
                _context.Logger.Log(LogLevel.Trace, 
                    $"Handling keyed message of type {typeof(TMessage).Name} with key '{key}'", 
                    "MessageHandler");
                
                try
                {
                    HandleMessage(key, message);
                }
                catch (Exception ex)
                {
                    _context.Logger.Log(LogLevel.Error, 
                        $"Error handling keyed message with key '{key}': {ex.Message}", 
                        "MessageHandler");
                    throw;
                }
            });
        }
        
        /// <summary>
        /// Handles a message of the specified type. Override this for specific key subscriptions.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        protected virtual void HandleMessage(TMessage message)
        {
            // Default implementation - derived classes should override this for specific key handling
        }

        /// <summary>
        /// Handles a message of the specified type with its key. Override this for all-key subscriptions.
        /// </summary>
        /// <param name="key">The key associated with the message.</param>
        /// <param name="message">The message to handle.</param>
        protected virtual void HandleMessage(TKey key, TMessage message)
        {
            // Default implementation - derived classes should override this for all-key handling
            HandleMessage(message); // Fallback to keyless handling
        }
        
        /// <summary>
        /// Disposes the message handler and unsubscribes from the message bus.
        /// </summary>
        public virtual void Dispose()
        {
            _subscription?.Dispose();
            _context?.Dispose();
        }
    }
}