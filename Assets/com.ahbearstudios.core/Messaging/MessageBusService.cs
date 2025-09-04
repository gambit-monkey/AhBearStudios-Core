using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Services;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Messaging.Filters;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Common.Utilities;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Production-ready implementation of the message bus service.
    /// Provides high-performance, type-safe messaging with comprehensive monitoring, error handling, and circuit breaker patterns.
    /// Fully integrates with AhBearStudios Core systems for enterprise-grade reliability.
    /// </summary>
    public sealed class MessageBusService : IMessageBusService
    {
        #region Private Fields

        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;
        private readonly IMessageCircuitBreakerService _circuitBreakerService;
        private readonly IMessageBusAdapter _messageBusAdapter;

        // Core collections
        private readonly ConcurrentDictionary<Type, object> _publishers;
        private readonly ConcurrentDictionary<Type, object> _subscribers;
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IDisposable>> _subscriptions;
        private readonly ConcurrentDictionary<Guid, MessageScope> _scopes;

        // Message queues
        private readonly ConcurrentQueue<FailedMessage> _deadLetterQueue;
        private readonly ConcurrentQueue<PendingMessage> _retryQueue;

        // Threading and synchronization
        private readonly SemaphoreSlim _publishSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ReaderWriterLockSlim _healthStatusLock;

        // State management
        private volatile bool _disposed;
        private volatile HealthStatus _currentHealthStatus;

        // Statistics tracking
        private long _totalMessagesPublished;
        private long _totalMessagesProcessed;
        private long _totalMessagesFailed;
        private long _totalMemoryAllocated;
        private long _currentQueueDepth;
        private long _retryQueueSize;
        private readonly ConcurrentDictionary<Type, MessageTypeStatistics> _messageTypeStats;

        // Performance tracking
        private readonly Timer _statisticsTimer;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _retryTimer;
        private DateTime _lastStatsReset;

        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        #endregion


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageBusService class.
        /// </summary>
        /// <param name="config">The message bus configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="circuitBreakerService">The circuit breaker service</param>
        /// <param name="messageBusAdapter">The message pipe adapter for publishing events</param>
        /// <param name="alertService">The alert service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageBusService(
            MessageBusConfig config,
            ILoggingService logger,
            IMessageCircuitBreakerService circuitBreakerService,
            IMessageBusAdapter messageBusAdapter = null,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolingService poolingService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _circuitBreakerService = circuitBreakerService ?? throw new ArgumentNullException(nameof(circuitBreakerService));
            _messageBusAdapter = messageBusAdapter;
            _alertService = alertService;
            _profilerService = profilerService;
            _poolingService = poolingService;

            // Generate correlation ID for tracking
            _correlationId = new FixedString128Bytes($"MessageBus-{DeterministicIdGenerator.GenerateCorrelationId("MessageBusService", "Instance"):N}");

            // Initialize collections
            _publishers = new ConcurrentDictionary<Type, object>();
            _subscribers = new ConcurrentDictionary<Type, object>();
            _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<IDisposable>>();
            _scopes = new ConcurrentDictionary<Guid, MessageScope>();
            _deadLetterQueue = new ConcurrentQueue<FailedMessage>();
            _retryQueue = new ConcurrentQueue<PendingMessage>();
            _messageTypeStats = new ConcurrentDictionary<Type, MessageTypeStatistics>();

            // Initialize synchronization primitives
            _publishSemaphore = new SemaphoreSlim(_config.MaxConcurrentHandlers, _config.MaxConcurrentHandlers);
            _cancellationTokenSource = new CancellationTokenSource();
            _healthStatusLock = new ReaderWriterLockSlim();

            // Initialize timers
            _statisticsTimer = new Timer(UpdateStatistics, null, 
                _config.StatisticsUpdateInterval, _config.StatisticsUpdateInterval);
            _healthCheckTimer = new Timer(PerformHealthCheck, null, 
                _config.HealthCheckInterval, _config.HealthCheckInterval);
            _retryTimer = new Timer(ProcessRetryQueue, null, 
                _config.RetryInterval, _config.RetryInterval);

            // Set initial state
            _currentHealthStatus = HealthStatus.Healthy;
            _lastStatsReset = DateTime.UtcNow;

            // Circuit breaker state changes are handled by the circuit breaker service itself via IMessage publishing

            _logger.LogInfo($"[{_correlationId}] MessageBusService initialized with config: {_config.InstanceName}");
        }

        #endregion

        #region Core Publishing Operations

        /// <inheritdoc />
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            if (!_circuitBreakerService.CanProcess<TMessage>())
            {
                HandleCircuitBreakerOpen<TMessage>(message);
                return;
            }

            using var scope = _profilerService?.BeginScope($"PublishMessage<{typeof(TMessage).Name}>");
            
            try
            {
                _publishSemaphore.Wait(_cancellationTokenSource.Token);
                
                try
                {
                    var startTime = DateTime.UtcNow;
                    
                    // Get or create publisher
                    var publisher = GetOrCreatePublisher<TMessage>();
                    
                    // Publish message
                    publisher.Publish(message);
                    
                    // Update statistics
                    var processingTime = DateTime.UtcNow - startTime;
                    UpdateMessageStatistics<TMessage>(true, processingTime.TotalMilliseconds);
                    Interlocked.Increment(ref _totalMessagesPublished);
                    
                    // Update circuit breaker with success
                    _circuitBreakerService.RecordSuccess<TMessage>();
                    
                    _logger.LogInfo($"[{_correlationId}] Published message {typeof(TMessage).Name} with ID {message.Id}");
                }
                finally
                {
                    _publishSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"[{_correlationId}] Message publishing cancelled for {typeof(TMessage).Name}");
                throw;
            }
            catch (Exception ex)
            {
                HandlePublishingError<TMessage>(message, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

            if (!_circuitBreakerService.CanProcess<TMessage>())
            {
                HandleCircuitBreakerOpen<TMessage>(message);
                return;
            }

            using var scope = _profilerService?.BeginScope($"PublishMessageAsync<{typeof(TMessage).Name}>");
            
            try
            {
                await _publishSemaphore.WaitAsync(combinedCts.Token);
                
                try
                {
                    var startTime = DateTime.UtcNow;
                    
                    // Get or create publisher
                    var publisher = GetOrCreatePublisher<TMessage>();
                    
                    // Publish message
                    await publisher.PublishAsync(message);
                    
                    // Update statistics
                    var processingTime = DateTime.UtcNow - startTime;
                    UpdateMessageStatistics<TMessage>(true, processingTime.TotalMilliseconds);
                    Interlocked.Increment(ref _totalMessagesPublished);
                    
                    // Update circuit breaker with success
                    _circuitBreakerService.RecordSuccess<TMessage>();
                    
                    _logger.LogInfo($"[{_correlationId}] Published async message {typeof(TMessage).Name} with ID {message.Id}");
                }
                finally
                {
                    _publishSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"[{_correlationId}] Async message publishing cancelled for {typeof(TMessage).Name}");
                throw;
            }
            catch (Exception ex)
            {
                HandlePublishingError<TMessage>(message, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public void PublishBatch<TMessage>(TMessage[] messages) where TMessage : IMessage
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (messages.Length == 0)
                return;

            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope($"PublishBatch<{typeof(TMessage).Name}>");
            
            var publisher = GetOrCreatePublisher<TMessage>();
            publisher.PublishBatch(messages);
            
            Interlocked.Add(ref _totalMessagesPublished, messages.Length);
            _logger.LogInfo($"[{_correlationId}] Published batch of {messages.Length} {typeof(TMessage).Name} messages");
        }

        /// <inheritdoc />
        public async UniTask PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (messages.Length == 0)
                return;

            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

            using var scope = _profilerService?.BeginScope($"PublishBatchAsync<{typeof(TMessage).Name}>");
            
            var publisher = GetOrCreatePublisher<TMessage>();
            await publisher.PublishBatchAsync(messages);
            
            Interlocked.Add(ref _totalMessagesPublished, messages.Length);
            _logger.LogInfo($"[{_correlationId}] Published async batch of {messages.Length} {typeof(TMessage).Name} messages");
        }

        #endregion

        #region Core Subscription Operations

        /// <inheritdoc />
        public IDisposable SubscribeToMessage<TMessage>(Action<TMessage> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.Subscribe(handler);
                
                // Track subscription
                var subscriptions = _subscriptions.GetOrAdd(typeof(TMessage), _ => new ConcurrentBag<IDisposable>());
                subscriptions.Add(subscription);
                
                _logger.LogInfo($"[{_correlationId}] Created subscription for {typeof(TMessage).Name}");
                
                return new WrappedSubscription(subscription, _logger, typeof(TMessage), Guid.Parse(_correlationId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to create subscription for {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            try
            {
                var subscriber = GetOrCreateSubscriber<TMessage>();
                var subscription = subscriber.SubscribeAsync(handler);
                
                // Track subscription
                var subscriptions = _subscriptions.GetOrAdd(typeof(TMessage), _ => new ConcurrentBag<IDisposable>());
                subscriptions.Add(subscription);
                
                _logger.LogInfo($"[{_correlationId}] Created async subscription for {typeof(TMessage).Name}");
                
                return new WrappedSubscription(subscription, _logger, typeof(TMessage), Guid.Parse(_correlationId.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to create async subscription for {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        #endregion

        #region Advanced Operations

        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            return GetOrCreatePublisher<TMessage>();
        }

        /// <inheritdoc />
        public IMessageSubscriber<TMessage> GetSubscriber<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            return GetOrCreateSubscriber<TMessage>();
        }

        #endregion

        #region Filtering and Routing

        /// <inheritdoc />
        public IDisposable SubscribeWithFilter<TMessage>(Func<TMessage, bool> filter, Action<TMessage> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            // Use MessagePipe filter instead of manual filtering
            var customFilter = new CustomPredicateFilter<TMessage>(filter, _logger);
            return _messageBusAdapter.Subscribe(handler, customFilter);
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, UniTask> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            // Use MessagePipe async filter instead of manual filtering
            var customFilter = new AsyncCustomPredicateFilter<TMessage>(filter, _logger);
            return _messageBusAdapter.SubscribeAsync(handler, customFilter);
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            // Use optimized MessagePipe priority filter
            var priorityFilter = new MessagePriorityFilter<TMessage>(minPriority, _logger);
            return _messageBusAdapter.Subscribe(handler, priorityFilter);
        }

        #endregion

        #region Scoped Subscriptions

        /// <inheritdoc />
        public IMessageScope CreateScope()
        {
            ThrowIfDisposed();

            var scope = new MessageScope(this, _logger);
            _scopes.TryAdd(scope.Id, scope);
            
            _logger.LogInfo($"[{_correlationId}] Created message scope {scope.Id}");
            
            return scope;
        }

        #endregion

        #region Diagnostics and Management

        /// <inheritdoc />
        public MessageBusStatistics GetStatistics()
        {
            ThrowIfDisposed();

            _healthStatusLock.EnterReadLock();
            try
            {
                return new MessageBusStatistics
                {
                    InstanceName = _config.InstanceName,
                    TotalMessagesPublished = Interlocked.Read(ref _totalMessagesPublished),
                    TotalMessagesProcessed = Interlocked.Read(ref _totalMessagesProcessed),
                    TotalMessagesFailed = Interlocked.Read(ref _totalMessagesFailed),
                    ActiveSubscribers = GetActiveSubscriberCount(),
                    DeadLetterQueueSize = _deadLetterQueue.Count,
                    MessagesInRetry = (int)Interlocked.Read(ref _retryQueueSize),
                    CurrentQueueDepth = (int)Interlocked.Read(ref _currentQueueDepth),
                    MemoryUsage = Interlocked.Read(ref _totalMemoryAllocated),
                    CurrentHealthStatus = _currentHealthStatus,
                    MessageTypeStatistics = _messageTypeStats.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CircuitBreakerStates = new Dictionary<Type, CircuitBreakerState>(), // Circuit breaker states managed by service
                    ActiveScopes = _scopes.Count,
                    LastStatsReset = _lastStatsReset,
                    ErrorRate = CalculateErrorRate(),
                    AverageProcessingTimeMs = CalculateAverageProcessingTime()
                };
            }
            finally
            {
                _healthStatusLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public void ClearMessageHistory()
        {
            ThrowIfDisposed();

            _logger.LogInfo($"[{_correlationId}] Clearing message history and resetting statistics");

            // Reset counters
            Interlocked.Exchange(ref _totalMessagesPublished, 0);
            Interlocked.Exchange(ref _totalMessagesProcessed, 0);
            Interlocked.Exchange(ref _totalMessagesFailed, 0);
            Interlocked.Exchange(ref _totalMemoryAllocated, 0);
            
            // Clear statistics
            _messageTypeStats.Clear();
            _lastStatsReset = DateTime.UtcNow;

            // Clear queues
            while (_deadLetterQueue.TryDequeue(out _)) { }
            while (_retryQueue.TryDequeue(out _)) { }
            
            _logger.LogInfo($"[{_correlationId}] Message history cleared successfully");
        }

        /// <inheritdoc />
        public HealthStatus GetHealthStatus()
        {
            _healthStatusLock.EnterReadLock();
            try
            {
                return _currentHealthStatus;
            }
            finally
            {
                _healthStatusLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public async UniTask<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

            try
            {
                var statistics = GetStatistics();
                var newStatus = DetermineHealthStatus(statistics);
                
                UpdateHealthStatus(newStatus, "Manual health check");
                
                return newStatus;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"[{_correlationId}] Health check cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Health check failed", ex);
                UpdateHealthStatus(HealthStatus.Unhealthy, $"Health check exception: {ex.Message}");
                return HealthStatus.Unhealthy;
            }
        }

        #endregion

        #region Circuit Breaker Operations

        /// <inheritdoc />
        public CircuitBreakerState GetCircuitBreakerState<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            
            return _circuitBreakerService.GetCircuitBreakerState<TMessage>();
        }

        /// <inheritdoc />
        public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            
            _circuitBreakerService.ResetCircuitBreaker<TMessage>();
            
            _logger.LogInfo($"[{_correlationId}] Circuit breaker reset for {typeof(TMessage).Name}");
        }

        #endregion

        #region Private Implementation

        private IMessagePublisher<TMessage> GetOrCreatePublisher<TMessage>() where TMessage : IMessage
        {
            return (IMessagePublisher<TMessage>)_publishers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating publisher for {typeof(TMessage).Name}");
                return new MessagePublisher<TMessage>(_config, _logger, _profilerService, _poolingService);
            });
        }

        private IMessageSubscriber<TMessage> GetOrCreateSubscriber<TMessage>() where TMessage : IMessage
        {
            return (IMessageSubscriber<TMessage>)_subscribers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating subscriber for {typeof(TMessage).Name}");
                
                // Create MessageSubscriberConfig from MessageBusConfig
                var subscriberConfig = new MessageSubscriberConfig(
                    maxConcurrentHandlers: _config.MaxConcurrentHandlers,
                    processingTimeout: _config.HandlerTimeout,
                    enableProfiling: _config.PerformanceMonitoring,
                    enableMessageBusIntegration: true,
                    statisticsInterval: _config.StatisticsUpdateInterval,
                    enableCircuitBreaker: _config.UseCircuitBreaker,
                    enableErrorRetry: _config.RetryFailedMessages,
                    maxRetryAttempts: _config.MaxRetryAttempts,
                    retryDelay: _config.RetryDelay,
                    correlationId: _correlationId);
                
                return new MessageSubscriber<TMessage>(subscriberConfig, _logger, _profilerService, this);
            });
        }


        private void HandlePublishingError<TMessage>(TMessage message, Exception ex) where TMessage : IMessage
        {
            _logger.LogException($"[{_correlationId}] Failed to publish message {typeof(TMessage).Name} with ID {message.Id}", ex);
            
            // Update statistics
            UpdateMessageStatistics<TMessage>(false, 0);
            Interlocked.Increment(ref _totalMessagesFailed);
            
            // Update circuit breaker
            _circuitBreakerService.RecordFailure<TMessage>(ex);
            
            // Add to retry queue if configured
            if (_config.RetryFailedMessages)
            {
                var pendingMessage = new PendingMessage(
                    message: message,
                    messageType: typeof(TMessage),
                    failureCount: 1,
                    lastAttempt: DateTime.UtcNow,
                    nextRetry: DateTime.UtcNow.Add(_config.RetryDelay),
                    originalException: ex,
                    retryContext: $"Publishing failure for {typeof(TMessage).Name}");
                
                _retryQueue.Enqueue(pendingMessage);
                Interlocked.Increment(ref _retryQueueSize);
            }
            
            // Raise alert if enabled
            if (_config.AlertsEnabled && _alertService != null)
            {
                _alertService.RaiseAlert(
                    $"Message publishing failed for {typeof(TMessage).Name}",
                    AlertSeverity.Warning,
                    source: "MessageBusService",
                    tag: "PublishFailure",
                    correlationId: message.CorrelationId);
            }
            
            // Publish message processing failed message
            if (_messageBusAdapter != null)
            {
                var failureMessage = MessageProcessingFailedEventMessage.Create(
                    failedMessage: message,
                    exception: ex,
                    attemptCount: 1,
                    isFinalFailure: !_config.RetryFailedMessages,
                    subscriberName: "MessageBusService",
                    failureContext: $"Failed to publish {typeof(TMessage).Name} in {_config.InstanceName}",
                    source: "MessageBusService",
                    correlationId: message.CorrelationId);
                
                try
                {
                    _messageBusAdapter.Publish(failureMessage);
                }
                catch (Exception publishEx)
                {
                    _logger.LogException("Failed to publish message processing failed message", publishEx);
                }
            }
        }

        private void HandleCircuitBreakerOpen<TMessage>(TMessage message) where TMessage : IMessage
        {
            _logger.LogWarning($"[{_correlationId}] Circuit breaker open for {typeof(TMessage).Name}, dropping message {message.Id}");
            
            // Add to dead letter queue
            var failedMessage = new FailedMessage(
                message: message,
                messageType: typeof(TMessage),
                reason: "Circuit breaker open",
                exception: null,
                attemptCount: 0,
                failedComponent: "MessageBusService",
                failureContext: $"Circuit breaker prevented publishing of {typeof(TMessage).Name}");
            
            _deadLetterQueue.Enqueue(failedMessage);
            
            // Raise alert if enabled
            if (_config.AlertsEnabled && _alertService != null)
            {
                _alertService.RaiseAlert(
                    $"Circuit breaker open for {typeof(TMessage).Name}",
                    AlertSeverity.Critical,
                    source: "MessageBusService",
                    tag: "CircuitBreakerOpen",
                    correlationId: message.CorrelationId);
            }
        }

        private void UpdateMessageStatistics<TMessage>(bool success, double processingTimeMs) where TMessage : IMessage
        {
            _messageTypeStats.AddOrUpdate(typeof(TMessage),
                new MessageTypeStatistics(success ? 1 : 0, success ? 0 : 1, processingTimeMs, processingTimeMs),
                (_, existing) => existing.Update(success, processingTimeMs));
        }

        private int GetActiveSubscriberCount()
        {
            return _subscriptions.Values.Sum(bag => bag.Count);
        }

        private double CalculateErrorRate()
        {
            var totalMessages = _totalMessagesPublished + _totalMessagesFailed;
            return totalMessages > 0 ? (double)_totalMessagesFailed / totalMessages : 0.0;
        }

        private double CalculateAverageProcessingTime()
        {
            var allStats = _messageTypeStats.Values;
            if (!allStats.AsValueEnumerable().Any()) return 0.0;
            
            return allStats.AsValueEnumerable().Average(s => s.AverageProcessingTime);
        }

        private HealthStatus DetermineHealthStatus(MessageBusStatistics statistics)
        {
            // Check critical conditions
            if (statistics.ErrorRate > _config.CriticalErrorRateThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.DeadLetterQueueSize > _config.CriticalQueueSizeThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.AverageProcessingTimeMs > _config.CriticalProcessingTimeThreshold.TotalMilliseconds)
                return HealthStatus.Unhealthy;
            
            // Check degraded conditions
            if (statistics.ErrorRate > _config.WarningErrorRateThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.DeadLetterQueueSize > _config.WarningQueueSizeThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.AverageProcessingTimeMs > _config.WarningProcessingTimeThreshold.TotalMilliseconds)
                return HealthStatus.Degraded;
            
            return HealthStatus.Healthy;
        }

        private void UpdateHealthStatus(HealthStatus newStatus, string reason)
        {
            _healthStatusLock.EnterWriteLock();
            try
            {
                var oldStatus = _currentHealthStatus;
                if (oldStatus != newStatus)
                {
                    _currentHealthStatus = newStatus;
                    
                    _logger.LogInfo($"[{_correlationId}] Health status changed from {oldStatus} to {newStatus}: {reason}");
                    
                    // Publish health status change message
                    if (_messageBusAdapter != null)
                    {
                        var healthMessage = MessageBusHealthChangedMessage.Create(
                            previousStatus: oldStatus,
                            currentStatus: newStatus,
                            reason: reason,
                            context: $"MessageBus instance: {_config.InstanceName}",
                            source: "MessageBusService",
                            correlationId: new Guid(_correlationId.ToString()));
                        
                        try
                        {
                            _messageBusAdapter.Publish(healthMessage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException("Failed to publish health status change message", ex);
                        }
                    }
                    
                    // Raise alert for unhealthy status
                    if (_config.AlertsEnabled && _alertService != null && newStatus == HealthStatus.Unhealthy)
                    {
                        _alertService.RaiseAlert(
                            $"MessageBus health status changed to {newStatus}",
                            AlertSeverity.Critical,
                            source: "MessageBusService",
                            tag: "HealthChange",
                            correlationId: new Guid(_correlationId.ToString()));
                    }
                }
            }
            finally
            {
                _healthStatusLock.ExitWriteLock();
            }
        }

        private void UpdateStatistics(object state)
        {
            if (_disposed) return;

            try
            {
                // Update memory allocation tracking
                var currentMemory = GC.GetTotalMemory(false);
                Interlocked.Exchange(ref _totalMemoryAllocated, currentMemory);

                // Update processed count based on successful operations
                var successfulOperations = _messageTypeStats.Values.Sum(s => s.ProcessedCount);
                Interlocked.Exchange(ref _totalMessagesProcessed, successfulOperations);

                // Update queue depths
                Interlocked.Exchange(ref _currentQueueDepth, _retryQueue.Count + _deadLetterQueue.Count);
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to update statistics", ex);
            }
        }

        private void PerformHealthCheck(object state)
        {
            if (_disposed) return;

            try
            {
                var statistics = GetStatistics();
                var newStatus = DetermineHealthStatus(statistics);
                UpdateHealthStatus(newStatus, "Periodic health check");
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Health check failed", ex);
                UpdateHealthStatus(HealthStatus.Unhealthy, $"Health check exception: {ex.Message}");
            }
        }

        private void ProcessRetryQueue(object state)
        {
            if (_disposed || !_config.RetryFailedMessages) return;

            var now = DateTime.UtcNow;
            var retryCount = 0;
            var maxRetries = Math.Min(_config.MaxRetryBatchSize, _retryQueue.Count);

            for (int i = 0; i < maxRetries; i++)
            {
                if (!_retryQueue.TryDequeue(out var pendingMessage))
                    break;

                Interlocked.Decrement(ref _retryQueueSize);

                if (now >= pendingMessage.NextRetry)
                {
                    try
                    {
                        // Attempt retry
                        var method = typeof(MessageBusService)
                            .GetMethod(nameof(PublishMessage))
                            ?.MakeGenericMethod(pendingMessage.MessageType);
                        
                        method?.Invoke(this, new[] { pendingMessage.Message });
                        
                        retryCount++;
                        _logger.LogInfo($"[{_correlationId}] Successfully retried message {pendingMessage.MessageType.Name}");
                    }
                    catch (Exception ex)
                    {
                        var updatedFailureCount = pendingMessage.FailureCount + 1;

                        // Publish retry failure message
                        if (_messageBusAdapter != null && pendingMessage.Message is IMessage retryMessage)
                        {
                            var retryFailureMessage = MessageProcessingFailedEventMessage.Create(
                                failedMessage: retryMessage,
                                exception: ex,
                                attemptCount: updatedFailureCount,
                                isFinalFailure: updatedFailureCount >= _config.MaxRetryAttempts,
                                subscriberName: "MessageBusService",
                                failureContext: $"Retry attempt {updatedFailureCount} failed for {pendingMessage.MessageType.Name}",
                                source: "MessageBusService",
                                correlationId: retryMessage.CorrelationId);
                            
                            try
                            {
                                _messageBusAdapter.Publish(retryFailureMessage);
                            }
                            catch (Exception publishEx)
                            {
                                _logger.LogException("Failed to publish retry failure message", publishEx);
                            }
                        }

                        if (updatedFailureCount < _config.MaxRetryAttempts)
                        {
                            // Schedule next retry with exponential backoff
                            var delay = TimeSpan.FromMilliseconds(
                                _config.RetryDelay.TotalMilliseconds * Math.Pow(2, updatedFailureCount - 1));
                            
                            var updatedPendingMessage = new PendingMessage(
                                message: pendingMessage.Message,
                                messageType: pendingMessage.MessageType,
                                failureCount: updatedFailureCount,
                                lastAttempt: now,
                                nextRetry: now.Add(delay),
                                originalException: pendingMessage.OriginalException ?? ex,
                                retryContext: pendingMessage.RetryContext);
                            
                            _retryQueue.Enqueue(updatedPendingMessage);
                            Interlocked.Increment(ref _retryQueueSize);
                        }
                        else
                        {
                            // Move to dead letter queue
                            var failedMessage = new FailedMessage(
                                message: pendingMessage.Message,
                                messageType: pendingMessage.MessageType,
                                reason: $"Max retry attempts exceeded: {ex.Message}",
                                exception: ex,
                                attemptCount: updatedFailureCount,
                                failedComponent: "MessageBusService",
                                failureContext: $"Retry processing failed after {updatedFailureCount} attempts");
                            
                            _deadLetterQueue.Enqueue(failedMessage);
                            
                            _logger.LogError($"[{_correlationId}] Message {pendingMessage.MessageType.Name} moved to dead letter queue after {updatedFailureCount} attempts");
                        }
                    }
                }
                else
                {
                    // Not ready for retry, put back in queue
                    _retryQueue.Enqueue(pendingMessage);
                    Interlocked.Increment(ref _retryQueueSize);
                }
            }

            if (retryCount > 0)
            {
                _logger.LogInfo($"[{_correlationId}] Processed {retryCount} retry messages");
            }
        }


        internal void OnScopeDisposed(Guid scopeId)
        {
            _scopes.TryRemove(scopeId, out _);
            _logger.LogInfo($"[{_correlationId}] Scope {scopeId} removed from tracking");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBusService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message bus service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"[{_correlationId}] Disposing MessageBusService");

            try
            {
                _disposed = true;

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Dispose timers
                _statisticsTimer?.Dispose();
                _healthCheckTimer?.Dispose();
                _retryTimer?.Dispose();

                // Dispose all publishers
                foreach (var publisher in _publishers.Values.OfType<IDisposable>())
                {
                    try
                    {
                        publisher?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"[{_correlationId}] Error disposing publisher", ex);
                    }
                }

                // Dispose all subscribers
                foreach (var subscriber in _subscribers.Values.OfType<IDisposable>())
                {
                    try
                    {
                        subscriber?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"[{_correlationId}] Error disposing subscriber", ex);
                    }
                }

                // Dispose all scopes
                foreach (var scope in _scopes.Values)
                {
                    try
                    {
                        scope?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException($"[{_correlationId}] Error disposing scope", ex);
                    }
                }

                // Dispose services
                _circuitBreakerService?.Dispose();

                // Dispose synchronization primitives
                _publishSemaphore?.Dispose();
                _healthStatusLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessageBusService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Error during MessageBusService disposal", ex);
            }
        }

        #endregion
    }
}