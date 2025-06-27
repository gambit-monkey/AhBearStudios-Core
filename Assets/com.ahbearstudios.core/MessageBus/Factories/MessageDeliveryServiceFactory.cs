using System;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Services;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Factories
{
    /// <summary>
    /// Default implementation of IMessageDeliveryServiceFactory.
    /// </summary>
    public sealed class MessageDeliveryServiceFactory : IMessageDeliveryServiceFactory
    {
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _logger;
        private readonly IProfiler _profiler;
        private readonly DeliveryServiceConfiguration _configuration;
        
        /// <summary>
        /// Initializes a new instance of the MessageDeliveryServiceFactory class.
        /// </summary>
        /// <param name="messageBusService">The message bus to use for message delivery.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        /// <param name="configuration">Configuration for delivery services.</param>
        public MessageDeliveryServiceFactory(
            IMessageBusService messageBusService, 
            ILoggingService logger, 
            IProfiler profiler,
            DeliveryServiceConfiguration configuration = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _configuration = configuration ?? new DeliveryServiceConfiguration();
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateReliableDeliveryService()
        {
            return new ReliableMessageDeliveryService(_messageBusService, _logger, _profiler);
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateFireAndForgetService()
        {
            return new FireAndForgetDeliveryService(_messageBusService, _logger, _profiler);
        }
        
        /// <inheritdoc />
        public IMessageDeliveryService CreateBatchOptimizedService()
        {
            var batchConfig = new BatchOptimizedConfiguration
            {
                MaxBatchSize = _configuration.MaxConcurrentDeliveries,
                BatchInterval = _configuration.ProcessingInterval,
                FlushInterval = TimeSpan.FromMilliseconds(_configuration.ProcessingInterval.TotalMilliseconds * 4),
                ConfirmationTimeout = _configuration.DefaultTimeout,
                ImmediateProcessingForReliable = true,
                EnableAdaptiveBatching = true,
                TargetThroughput = 1000
            };
            
            return new BatchOptimizedDeliveryService(_messageBusService, _logger, _profiler, batchConfig);
        }
    }
}