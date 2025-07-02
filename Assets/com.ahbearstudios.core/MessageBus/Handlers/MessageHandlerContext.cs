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
    /// Implementation of IMessageHandlerContext that provides logging and profiling utilities.
    /// </summary>
    internal sealed class MessageHandlerContext : IMessageHandlerContext
    {
        private readonly ProfilerTag _handlerTag;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MessageHandlerContext class.
        /// </summary>
        /// <param name="messageBusService">The message bus instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="profilerService">The profiler instance.</param>
        /// <param name="handlerType">The type of the handler for profiling purposes.</param>
        public MessageHandlerContext(IMessageBusService messageBusService, ILoggingService logger, IProfilerService profilerService, Type handlerType)
        {
            MessageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ProfilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            
            _handlerTag = new ProfilerTag(new ProfilerCategory("MessageHandler"), handlerType.Name);
            
            Logger.Log(LogLevel.Debug, 
                $"Registered message handler {handlerType.Name}", 
                "MessageHandler");
        }

        /// <inheritdoc />
        public IMessageBusService MessageBusService { get; }
        
        /// <inheritdoc />
        public ILoggingService Logger { get; }
        
        /// <inheritdoc />
        public IProfilerService ProfilerService { get; }

        /// <inheritdoc />
        public void ExecuteWithProfiling(string sampleName, Action action)
        {
            using var scope = ProfilerService.BeginScope(_handlerTag);
            action();
        }

        /// <inheritdoc />
        public IDisposable SubscribeKeyed<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            // Use the extension method for keyed subscription with specific key
            return MessageBusService.SubscribeKeyed<TKey, TMessage>(key, handler);
        }

        /// <inheritdoc />
        public IDisposable SubscribeKeyedAll<TKey, TMessage>(Action<TKey, TMessage> handler)
        {
            // Use the extension method for subscribing to all keyed messages
            return MessageBusService.SubscribeKeyedAll<TKey, TMessage>(handler);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            Logger.Log(LogLevel.Debug, 
                $"Disposed message handler context", 
                "MessageHandler");
            
            _disposed = true;
        }
    }
}