using System;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Default implementation of IMessageDeliveryServiceFactory.
    /// </summary>
    public sealed class MessageDeliveryServiceFactory : IMessageDeliveryServiceFactory
    {
        private readonly IMessageBus _messageBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        
        /// <summary>
        /// Initializes a new instance of the MessageDeliveryServiceFactory class.
        /// </summary>
        /// <param name="messageBus">The message bus to use for message delivery.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public MessageDeliveryServiceFactory(IMessageBus messageBus, IBurstLogger logger, IProfiler profiler)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateReliableDeliveryService()
        {
            return new ReliableMessageDeliveryService(_messageBus, _logger, _profiler);
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateFireAndForgetService()
        {
            return new FireAndForgetDeliveryService(_messageBus, _logger, _profiler);
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateBatchOptimizedService()
        {
            return new BatchOptimizedDeliveryService(_messageBus, _logger, _profiler);
        }
    }
}