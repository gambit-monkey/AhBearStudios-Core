using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using Unity.Collections;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Null implementation of IMessageBusService for use when messaging is disabled or unavailable.
    /// Provides no-op implementations of all messaging operations with minimal performance overhead.
    /// Integrates with core services for consistency and debugging support.
    /// Created via MessageBusFactory to follow established architectural patterns.
    /// </summary>
    public sealed class NullMessageBusService : IMessageBusService
    {
        #region Private Fields

        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;

        // Performance monitoring
        private readonly ProfilerMarker _publishMarker = new ProfilerMarker("NullMessageBus.Publish");
        private readonly ProfilerMarker _subscribeMarker = new ProfilerMarker("NullMessageBus.Subscribe");
        private readonly ProfilerMarker _healthCheckMarker = new ProfilerMarker("NullMessageBus.HealthCheck");

        // Cached statistics to avoid allocations
        private static readonly MessageBusStatistics CachedStatistics = new MessageBusStatistics
        {
            InstanceName = "NullMessageBus",
            TotalMessagesPublished = 0,
            TotalMessagesProcessed = 0,
            TotalMessagesFailed = 0,
            ActiveSubscribers = 0,
            DeadLetterQueueSize = 0,
            MessagesInRetry = 0,
            CurrentQueueDepth = 0,
            MemoryUsage = 0,
            CurrentHealthStatus = HealthStatus.Healthy,
            MessageTypeStatistics = new Dictionary<Type, MessageTypeStatistics>(),
            CircuitBreakerStates = new Dictionary<Type, CircuitBreakerState>(),
            ActiveScopes = 0,
            LastStatsReset = DateTime.UtcNow,
            ErrorRate = 0.0,
            AverageProcessingTimeMs = 0.0
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the NullMessageBusService.
        /// </summary>
        /// <param name="logger">Optional logging service for debugging</param>
        /// <param name="alertService">Optional alert service for health monitoring</param>
        /// <param name="profilerService">Optional profiler service for performance monitoring</param>
        /// <param name="poolingService">Optional pooling service for memory management</param>
        public NullMessageBusService(
            ILoggingService logger = null,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolingService poolingService = null)
        {
            _logger = logger;
            _alertService = alertService;
            _profilerService = profilerService;
            _poolingService = poolingService;

            // Log initialization for debugging purposes
            _logger?.LogDebug("NullMessageBusService initialized");
        }

        #endregion

        #region Core Publishing Operations

        /// <inheritdoc />
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            using (_publishMarker.Auto())
            {
                // Log for debugging purposes
                _logger?.LogDebug($"NullMessageBus: PublishMessage<{typeof(TMessage).Name}> - No-op");
                
                // No actual publishing operation
            }
        }

        /// <inheritdoc />
        public UniTask PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using (_publishMarker.Auto())
            {
                // Log for debugging purposes
                _logger?.LogDebug($"NullMessageBus: PublishMessageAsync<{typeof(TMessage).Name}> - No-op");
                
                return UniTask.CompletedTask;
            }
        }

        /// <inheritdoc />
        public void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage
        {
            using (_publishMarker.Auto())
            {
                // Log batch operation for debugging
                _logger?.LogDebug($"NullMessageBus: PublishBatch<{typeof(TMessage).Name}> [{messages?.Length ?? 0} messages] - No-op");
                
                // No actual publishing operation
            }
        }

        /// <inheritdoc />
        public UniTask PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using (_publishMarker.Auto())
            {
                // Log batch operation for debugging
                _logger?.LogDebug($"NullMessageBus: PublishBatchAsync<{typeof(TMessage).Name}> [{messages?.Length ?? 0} messages] - No-op");
                
                return UniTask.CompletedTask;
            }
        }

        #endregion

        #region Core Subscription Operations

        /// <inheritdoc />
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            using (_subscribeMarker.Auto())
            {
                _logger?.LogDebug($"NullMessageBus: SubscribeToMessage<{typeof(TMessage).Name}> - No-op");
                
                return NullDisposable.Instance;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            using (_subscribeMarker.Auto())
            {
                _logger?.LogDebug($"NullMessageBus: SubscribeToMessageAsync<{typeof(TMessage).Name}> - No-op");
                
                return NullDisposable.Instance;
            }
        }

        #endregion

        #region Advanced Operations

        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            return NullMessagePublisher<TMessage>.Instance;
        }

        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            return NullMessageSubscriber<TMessage>.Instance;
        }

        #endregion

        #region Filtering and Routing

        /// <inheritdoc />
        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            using (_subscribeMarker.Auto())
            {
                _logger?.LogDebug($"NullMessageBus: SubscribeWithFilter<{typeof(TMessage).Name}> - No-op");
                
                return NullDisposable.Instance;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            using (_subscribeMarker.Auto())
            {
                _logger?.LogDebug($"NullMessageBus: SubscribeWithFilterAsync<{typeof(TMessage).Name}> - No-op");
                
                return NullDisposable.Instance;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            using (_subscribeMarker.Auto())
            {
                _logger?.LogDebug($"NullMessageBus: SubscribeWithPriority<{typeof(TMessage).Name}> [MinPriority: {minPriority}] - No-op");
                
                return NullDisposable.Instance;
            }
        }

        #endregion

        #region Scoped Subscriptions

        /// <inheritdoc />
        public IMessageScope CreateScope()
        {
            return new NullMessageScope();
        }

        #endregion

        #region Diagnostics and Management

        /// <inheritdoc />
        public MessageBusStatistics GetStatistics()
        {
            // Return cached statistics to avoid allocations
            return CachedStatistics;
        }

        /// <inheritdoc />
        public void ClearMessageHistory()
        {
            // No-op
        }

        /// <inheritdoc />
        public HealthStatus GetHealthStatus()
        {
            using (_healthCheckMarker.Auto())
            {
                _logger?.LogDebug("NullMessageBus: GetHealthStatus - Always Healthy");
                
                return HealthStatus.Healthy;
            }
        }

        /// <inheritdoc />
        public UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            using (_healthCheckMarker.Auto())
            {
                _logger?.LogDebug("NullMessageBus: CheckHealthAsync - Always Healthy");
                
                return UniTask.FromResult(HealthStatus.Healthy);
            }
        }

        #endregion

        #region Circuit Breaker Operations

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage
        {
            return CircuitBreakerState.Closed;
        }

        /// <inheritdoc />
        public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            // No-op
        }

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            _logger?.LogDebug("NullMessageBusService disposed");
            
            // No actual resources to dispose in null implementation
        }

        #endregion
    }
}