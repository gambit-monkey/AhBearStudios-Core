using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.HealthChecks;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Messaging.Subscribers;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
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

        // Core collections
        private readonly ConcurrentDictionary<Type, object> _publishers;
        private readonly ConcurrentDictionary<Type, object> _subscribers;
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IDisposable>> _subscriptions;
        private readonly ConcurrentDictionary<Guid, MessageScope> _scopes;

        // Circuit breaker management
        private readonly ConcurrentDictionary<Type, CircuitBreaker> _circuitBreakers;
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
        private readonly Timer _circuitBreakerTimer;
        private DateTime _lastStatsReset;

        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;

        /// <inheritdoc />
        public event EventHandler<MessageProcessingFailedEventArgs> MessageProcessingFailed;

        /// <inheritdoc />
        public event EventHandler<CircuitBreakerStateChangedEventArgs> CircuitBreakerStateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessageBusService class.
        /// </summary>
        /// <param name="config">The message bus configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="alertService">The alert service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageBusService(
            MessageBusConfig config,
            ILoggingService logger,
            IAlertService alertService = null,
            IProfilerService profilerService = null,
            IPoolingService poolingService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService;
            _profilerService = profilerService;
            _poolingService = poolingService;

            // Generate correlation ID for tracking
            _correlationId = new FixedString128Bytes($"MessageBus-{Guid.NewGuid():N}");

            // Initialize collections
            _publishers = new ConcurrentDictionary<Type, object>();
            _subscribers = new ConcurrentDictionary<Type, object>();
            _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<IDisposable>>();
            _scopes = new ConcurrentDictionary<Guid, MessageScope>();
            _circuitBreakers = new ConcurrentDictionary<Type, CircuitBreaker>();
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
            _circuitBreakerTimer = new Timer(UpdateCircuitBreakers, null, 
                _config.CircuitBreakerCheckInterval, _config.CircuitBreakerCheckInterval);

            // Set initial state
            _currentHealthStatus = HealthStatus.Healthy;
            _lastStatsReset = DateTime.UtcNow;

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

            var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
            if (circuitBreaker.State == CircuitBreakerState.Open)
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
                    circuitBreaker.RecordSuccess();
                    
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
                HandlePublishingError<TMessage>(message, ex, circuitBreaker);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task PublishMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

            var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
            if (circuitBreaker.State == CircuitBreakerState.Open)
            {
                HandleCircuitBreakerOpen<TMessage>(message);
                return;
            }

            using var scope = _profilerService?.BeginScope($"PublishMessageAsync<{typeof(TMessage).Name}>");
            
            try
            {
                await _publishSemaphore.WaitAsync(combinedCts.Token).ConfigureAwait(false);
                
                try
                {
                    var startTime = DateTime.UtcNow;
                    
                    // Get or create publisher
                    var publisher = GetOrCreatePublisher<TMessage>();
                    
                    // Publish message
                    await publisher.PublishAsync(message).ConfigureAwait(false);
                    
                    // Update statistics
                    var processingTime = DateTime.UtcNow - startTime;
                    UpdateMessageStatistics<TMessage>(true, processingTime.TotalMilliseconds);
                    Interlocked.Increment(ref _totalMessagesPublished);
                    
                    // Update circuit breaker with success
                    circuitBreaker.RecordSuccess();
                    
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
                HandlePublishingError<TMessage>(message, ex, circuitBreaker);
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
        public async Task PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
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
            await publisher.PublishBatchAsync(messages).ConfigureAwait(false);
            
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
                
                return new WrappedSubscription(subscription, this, typeof(TMessage));
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to create subscription for {typeof(TMessage).Name}");
                throw;
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeToMessageAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : IMessage
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
                
                return new WrappedSubscription(subscription, this, typeof(TMessage));
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to create async subscription for {typeof(TMessage).Name}");
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

            return SubscribeToMessage<TMessage>(message =>
            {
                try
                {
                    if (filter(message))
                    {
                        handler(message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, $"[{_correlationId}] Error in filtered message handler for {typeof(TMessage).Name}");
                    throw;
                }
            });
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithFilterAsync<TMessage>(Func<TMessage, bool> filter, Func<TMessage, Task> handler) where TMessage : IMessage
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            return SubscribeToMessageAsync<TMessage>(async message =>
            {
                try
                {
                    if (filter(message))
                    {
                        await handler(message).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, $"[{_correlationId}] Error in async filtered message handler for {typeof(TMessage).Name}");
                    throw;
                }
            });
        }

        /// <inheritdoc />
        public IDisposable SubscribeWithPriority<TMessage>(Action<TMessage> handler, MessagePriority minPriority) where TMessage : IMessage
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ThrowIfDisposed();

            return SubscribeWithFilter<TMessage>(
                message => message.Priority >= minPriority,
                handler);
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
                    MessagesPublished = Interlocked.Read(ref _totalMessagesPublished),
                    MessagesProcessed = Interlocked.Read(ref _totalMessagesProcessed),
                    MessagesFailed = Interlocked.Read(ref _totalMessagesFailed),
                    ActiveSubscriptions = GetActiveSubscriberCount(),
                    DeadLetterQueueSize = _deadLetterQueue.Count,
                    RetryQueueSize = (int)Interlocked.Read(ref _retryQueueSize),
                    CurrentQueueDepth = (int)Interlocked.Read(ref _currentQueueDepth),
                    MemoryUsage = Interlocked.Read(ref _totalMemoryAllocated),
                    CurrentHealthStatus = _currentHealthStatus,
                    MessageTypeStatistics = _messageTypeStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CircuitBreakerStates = _circuitBreakers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.State),
                    ActiveScopes = _scopes.Count,
                    LastStatsReset = _lastStatsReset,
                    ErrorRate = CalculateErrorRate(),
                    AverageProcessingTime = CalculateAverageProcessingTime()
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
        public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
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
                _logger.LogException(ex, $"[{_correlationId}] Health check failed");
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
            
            var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
            return circuitBreaker.State;
        }

        /// <inheritdoc />
        public void ResetCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            
            var circuitBreaker = GetOrCreateCircuitBreaker<TMessage>();
            circuitBreaker.Reset();
            
            _logger.LogInfo($"[{_correlationId}] Circuit breaker reset for {typeof(TMessage).Name}");
            
            CircuitBreakerStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
            {
                MessageType = typeof(TMessage),
                OldState = CircuitBreakerState.Open,
                NewState = CircuitBreakerState.Closed,
                Reason = "Manual reset"
            });
        }

        #endregion

        #region Private Implementation

        private IMessagePublisher<TMessage> GetOrCreatePublisher<TMessage>() where TMessage : IMessage
        {
            return (IMessagePublisher<TMessage>)_publishers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating publisher for {typeof(TMessage).Name}");
                return new MessagePublisher<TMessage>(_config, _logger, _profilerService, this);
            });
        }

        private IMessageSubscriber<TMessage> GetOrCreateSubscriber<TMessage>() where TMessage : IMessage
        {
            return (IMessageSubscriber<TMessage>)_subscribers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating subscriber for {typeof(TMessage).Name}");
                return new MessageSubscriber<TMessage>(_config, _logger, _profilerService, this);
            });
        }

        private CircuitBreaker GetOrCreateCircuitBreaker<TMessage>() where TMessage : IMessage
        {
            return _circuitBreakers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating circuit breaker for {typeof(TMessage).Name}");
                return new CircuitBreaker(_config.CircuitBreakerConfig, _logger);
            });
        }

        private void HandlePublishingError<TMessage>(TMessage message, Exception ex, CircuitBreaker circuitBreaker) where TMessage : IMessage
        {
            _logger.LogException(ex, $"[{_correlationId}] Failed to publish message {typeof(TMessage).Name} with ID {message.Id}");
            
            // Update statistics
            UpdateMessageStatistics<TMessage>(false, 0);
            Interlocked.Increment(ref _totalMessagesFailed);
            
            // Update circuit breaker
            circuitBreaker.RecordFailure();
            
            // Add to retry queue if configured
            if (_config.RetryFailedMessages)
            {
                var pendingMessage = new PendingMessage
                {
                    Message = message,
                    MessageType = typeof(TMessage),
                    FailureCount = 1,
                    LastAttempt = DateTime.UtcNow,
                    NextRetry = DateTime.UtcNow.Add(_config.RetryDelay)
                };
                
                _retryQueue.Enqueue(pendingMessage);
                Interlocked.Increment(ref _retryQueueSize);
            }
            
            // Raise alert if enabled
            if (_config.AlertsEnabled && _alertService != null)
            {
                _alertService.RaiseAlert(
                    $"Message publishing failed for {typeof(TMessage).Name}",
                    AlertSeverity.Warning,
                    new { MessageId = message.Id, Error = ex.Message });
            }
            
            // Raise event
            MessageProcessingFailed?.Invoke(this, new MessageProcessingFailedEventArgs
            {
                MessageType = typeof(TMessage),
                MessageId = message.Id,
                Exception = ex,
                RetryScheduled = _config.RetryFailedMessages
            });
        }

        private void HandleCircuitBreakerOpen<TMessage>(TMessage message) where TMessage : IMessage
        {
            _logger.LogWarning($"[{_correlationId}] Circuit breaker open for {typeof(TMessage).Name}, dropping message {message.Id}");
            
            // Add to dead letter queue
            var failedMessage = new FailedMessage
            {
                Message = message,
                MessageType = typeof(TMessage),
                Reason = "Circuit breaker open",
                Timestamp = DateTime.UtcNow
            };
            
            _deadLetterQueue.Enqueue(failedMessage);
            
            // Raise alert if enabled
            if (_config.AlertsEnabled && _alertService != null)
            {
                _alertService.RaiseAlert(
                    $"Circuit breaker open for {typeof(TMessage).Name}",
                    AlertSeverity.Critical,
                    new { MessageId = message.Id });
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
            if (!allStats.Any()) return 0.0;
            
            return allStats.Average(s => s.AverageProcessingTime);
        }

        private HealthStatus DetermineHealthStatus(MessageBusStatistics statistics)
        {
            // Check critical conditions
            if (statistics.ErrorRate > _config.CriticalErrorRateThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.DeadLetterQueueSize > _config.CriticalQueueSizeThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.AverageProcessingTime > _config.CriticalProcessingTimeThreshold.TotalMilliseconds)
                return HealthStatus.Unhealthy;
            
            // Check degraded conditions
            if (statistics.ErrorRate > _config.WarningErrorRateThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.DeadLetterQueueSize > _config.WarningQueueSizeThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.AverageProcessingTime > _config.WarningProcessingTimeThreshold.TotalMilliseconds)
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
                    
                    HealthStatusChanged?.Invoke(this, new HealthStatusChangedEventArgs
                    {
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    });
                    
                    // Raise alert for unhealthy status
                    if (_config.AlertsEnabled && _alertService != null && newStatus == HealthStatus.Unhealthy)
                    {
                        _alertService.RaiseAlert(
                            $"MessageBus health status changed to {newStatus}",
                            AlertSeverity.Critical,
                            new { Reason = reason });
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
                _logger.LogException(ex, $"[{_correlationId}] Failed to update statistics");
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
                _logger.LogException(ex, $"[{_correlationId}] Health check failed");
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
                        pendingMessage.FailureCount++;
                        pendingMessage.LastAttempt = now;

                        if (pendingMessage.FailureCount < _config.MaxRetryAttempts)
                        {
                            // Schedule next retry with exponential backoff
                            var delay = TimeSpan.FromMilliseconds(
                                _config.RetryDelay.TotalMilliseconds * Math.Pow(2, pendingMessage.FailureCount - 1));
                            pendingMessage.NextRetry = now.Add(delay);
                            
                            _retryQueue.Enqueue(pendingMessage);
                            Interlocked.Increment(ref _retryQueueSize);
                        }
                        else
                        {
                            // Move to dead letter queue
                            var failedMessage = new FailedMessage
                            {
                                Message = pendingMessage.Message,
                                MessageType = pendingMessage.MessageType,
                                Reason = $"Max retry attempts exceeded: {ex.Message}",
                                Timestamp = now
                            };
                            
                            _deadLetterQueue.Enqueue(failedMessage);
                            
                            _logger.LogError($"[{_correlationId}] Message {pendingMessage.MessageType.Name} moved to dead letter queue after {pendingMessage.FailureCount} attempts");
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

        private void UpdateCircuitBreakers(object state)
        {
            if (_disposed) return;

            try
            {
                foreach (var kvp in _circuitBreakers)
                {
                    var oldState = kvp.Value.State;
                    kvp.Value.Update();
                    var newState = kvp.Value.State;

                    if (oldState != newState)
                    {
                        _logger.LogInfo($"[{_correlationId}] Circuit breaker for {kvp.Key.Name} changed from {oldState} to {newState}");
                        
                        CircuitBreakerStateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs
                        {
                            MessageType = kvp.Key,
                            OldState = oldState,
                            NewState = newState,
                            Reason = "Automatic state transition"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Failed to update circuit breakers");
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
                _circuitBreakerTimer?.Dispose();

                // Dispose all publishers
                foreach (var publisher in _publishers.Values.OfType<IDisposable>())
                {
                    try
                    {
                        publisher?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex, $"[{_correlationId}] Error disposing publisher");
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
                        _logger.LogException(ex, $"[{_correlationId}] Error disposing subscriber");
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
                        _logger.LogException(ex, $"[{_correlationId}] Error disposing scope");
                    }
                }

                // Dispose synchronization primitives
                _publishSemaphore?.Dispose();
                _healthStatusLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessageBusService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, $"[{_correlationId}] Error during MessageBusService disposal");
            }
        }

        #endregion
    }
}