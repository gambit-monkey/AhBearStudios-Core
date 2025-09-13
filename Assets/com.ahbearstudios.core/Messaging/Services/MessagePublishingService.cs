using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Messaging.Publishers;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Services
{
    /// <summary>
    /// Service for handling message publishing operations.
    /// Focused solely on publishing responsibilities with comprehensive error handling and monitoring.
    /// </summary>
    public sealed class MessagePublishingService : IMessagePublishingService
    {
        #region Private Fields

        private readonly MessagePublishingConfig _config;
        private readonly ILoggingService _logger;
        private readonly IAlertService _alertService;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;
        
        // Core collections
        private readonly ConcurrentDictionary<Type, object> _publishers;
        private readonly ConcurrentDictionary<Type, MessageTypeStatistics> _messageTypeStats;
        
        // Threading and synchronization
        private readonly SemaphoreSlim _publishSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ReaderWriterLockSlim _healthStatusLock;
        
        // State management
        private volatile bool _disposed;
        private volatile HealthStatus _currentHealthStatus;
        
        // Statistics tracking
        private long _totalMessagesPublished;
        private long _totalMessagesFailedToPublish;
        private long _totalBatchOperations;
        private long _totalMemoryAllocated;
        private DateTime _lastStatsReset;
        private DateTime _lastHealthCheck;
        
        // Performance tracking
        private readonly Timer _statisticsTimer;
        private readonly Timer _healthCheckTimer;
        
        // Correlation tracking
        private readonly FixedString128Bytes _correlationId;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessagePublishingService class.
        /// </summary>
        /// <param name="config">The publishing configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="alertService">The alert service (optional)</param>
        /// <param name="profilerService">The profiler service (optional)</param>
        /// <param name="poolingService">The pooling service (optional)</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessagePublishingService(
            MessagePublishingConfig config,
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

            // Validate configuration
            if (!_config.IsValid())
                throw new ArgumentException("Invalid publishing configuration", nameof(config));

            // Generate correlation ID for tracking
            _correlationId = new FixedString128Bytes($"Publishing-{DeterministicIdGenerator.GenerateCorrelationId("MessagePublishingService", "Instance"):N}");

            // Initialize collections
            var initialCapacity = _config.InitialPublisherCapacity;
            _publishers = new ConcurrentDictionary<Type, object>(Environment.ProcessorCount, initialCapacity);
            _messageTypeStats = new ConcurrentDictionary<Type, MessageTypeStatistics>(Environment.ProcessorCount, _config.MaxTrackedMessageTypes);

            // Initialize synchronization primitives
            _publishSemaphore = new SemaphoreSlim(_config.MaxConcurrentPublishers, _config.MaxConcurrentPublishers);
            _cancellationTokenSource = new CancellationTokenSource();
            _healthStatusLock = new ReaderWriterLockSlim();

            // Initialize timers
            if (_config.PerformanceMonitoringEnabled)
            {
                _statisticsTimer = new Timer(UpdateStatistics, null, 
                    _config.StatisticsUpdateInterval, _config.StatisticsUpdateInterval);
            }

            _healthCheckTimer = new Timer(PerformHealthCheck, null, 
                _config.HealthCheckInterval, _config.HealthCheckInterval);

            // Set initial state
            _currentHealthStatus = HealthStatus.Healthy;
            _lastStatsReset = DateTime.UtcNow;
            _lastHealthCheck = DateTime.UtcNow;

            _logger.LogInfo($"[{_correlationId}] MessagePublishingService initialized");
        }

        #endregion

        #region Core Publishing Operations

        /// <inheritdoc />
        public void PublishMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

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

            if (!_config.AsyncPublishingEnabled)
            {
                PublishMessage(message);
                return;
            }

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

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

            if (!_config.BatchPublishingEnabled)
            {
                foreach (var message in messages)
                {
                    PublishMessage(message);
                }
                return;
            }

            ThrowIfDisposed();

            using var scope = _profilerService?.BeginScope($"PublishBatch<{typeof(TMessage).Name}>");
            
            var publisher = GetOrCreatePublisher<TMessage>();
            publisher.PublishBatch(messages);
            
            Interlocked.Add(ref _totalMessagesPublished, messages.Length);
            Interlocked.Increment(ref _totalBatchOperations);
            
            _logger.LogInfo($"[{_correlationId}] Published batch of {messages.Length} {typeof(TMessage).Name} messages");
        }

        /// <inheritdoc />
        public async UniTask PublishBatchAsync<TMessage>(TMessage[] messages, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            if (messages.Length == 0)
                return;

            if (!_config.AsyncPublishingEnabled)
            {
                PublishBatch(messages);
                return;
            }

            if (!_config.BatchPublishingEnabled)
            {
                foreach (var message in messages)
                {
                    await PublishMessageAsync(message, cancellationToken);
                }
                return;
            }

            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);

            using var scope = _profilerService?.BeginScope($"PublishBatchAsync<{typeof(TMessage).Name}>");
            
            var publisher = GetOrCreatePublisher<TMessage>();
            await publisher.PublishBatchAsync(messages);
            
            Interlocked.Add(ref _totalMessagesPublished, messages.Length);
            Interlocked.Increment(ref _totalBatchOperations);
            
            _logger.LogInfo($"[{_correlationId}] Published async batch of {messages.Length} {typeof(TMessage).Name} messages");
        }

        #endregion

        #region Publisher Management

        /// <inheritdoc />
        public IMessagePublisher<TMessage> GetPublisher<TMessage>() where TMessage : IMessage
        {
            ThrowIfDisposed();
            return GetOrCreatePublisher<TMessage>();
        }

        #endregion

        #region Statistics and Diagnostics

        /// <inheritdoc />
        public MessagePublishingStatistics GetStatistics()
        {
            ThrowIfDisposed();

            _healthStatusLock.EnterReadLock();
            try
            {
                return new MessagePublishingStatistics
                {
                    TotalMessagesPublished = Interlocked.Read(ref _totalMessagesPublished),
                    TotalMessagesFailedToPublish = Interlocked.Read(ref _totalMessagesFailedToPublish),
                    TotalBatchOperations = Interlocked.Read(ref _totalBatchOperations),
                    ActivePublishers = _publishers.Count,
                    AveragePublishingTimeMs = CalculateAveragePublishingTime(),
                    PeakPublishingTimeMs = CalculatePeakPublishingTime(),
                    MessagesPerSecond = CalculateMessagesPerSecond(),
                    PeakMessagesPerSecond = 0, // TODO: Track peak MPS
                    ErrorRate = CalculateErrorRate(),
                    MemoryUsageBytes = Interlocked.Read(ref _totalMemoryAllocated),
                    CapturedAt = DateTime.UtcNow,
                    LastResetAt = _lastStatsReset,
                    MessageTypeStatistics = _messageTypeStats.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
            }
            finally
            {
                _healthStatusLock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public void ClearStatistics()
        {
            ThrowIfDisposed();

            _logger.LogInfo($"[{_correlationId}] Clearing publishing statistics");

            // Reset counters
            Interlocked.Exchange(ref _totalMessagesPublished, 0);
            Interlocked.Exchange(ref _totalMessagesFailedToPublish, 0);
            Interlocked.Exchange(ref _totalBatchOperations, 0);
            Interlocked.Exchange(ref _totalMemoryAllocated, 0);
            
            // Clear per-type statistics
            _messageTypeStats.Clear();
            _lastStatsReset = DateTime.UtcNow;

            _logger.LogInfo($"[{_correlationId}] Publishing statistics cleared");
        }

        #endregion

        #region Health and Status

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

        #region Private Implementation

        private IMessagePublisher<TMessage> GetOrCreatePublisher<TMessage>() where TMessage : IMessage
        {
            return (IMessagePublisher<TMessage>)_publishers.GetOrAdd(typeof(TMessage), _ =>
            {
                _logger.LogInfo($"[{_correlationId}] Creating publisher for {typeof(TMessage).Name}");
                
                // Create publisher directly - pooling not suitable for publishers with dependencies
                return new MessagePublisher<TMessage>(_config, _logger, _profilerService, _poolingService);
            });
        }

        private void HandlePublishingError<TMessage>(TMessage message, Exception ex) where TMessage : IMessage
        {
            _logger.LogException($"[{_correlationId}] Failed to publish message {typeof(TMessage).Name} with ID {message.Id}", ex);
            
            // Update statistics
            UpdateMessageStatistics<TMessage>(false, 0);
            Interlocked.Increment(ref _totalMessagesFailedToPublish);
            
            // Raise alert if enabled
            if (_alertService != null)
            {
                _alertService.RaiseAlert(
                    $"Message publishing failed for {typeof(TMessage).Name}",
                    AlertSeverity.Warning,
                    source: "MessagePublishingService",
                    tag: "PublishFailure",
                    correlationId: message.CorrelationId);
            }
        }

        private void UpdateMessageStatistics<TMessage>(bool success, double processingTimeMs) where TMessage : IMessage
        {
            if (!_config.TrackPerTypeStatistics)
                return;

            if (_messageTypeStats.Count >= _config.MaxTrackedMessageTypes)
                return;

            _messageTypeStats.AddOrUpdate(typeof(TMessage),
                new MessageTypeStatistics(success ? 1 : 0, success ? 0 : 1, processingTimeMs, processingTimeMs),
                (_, existing) => existing.Update(success, processingTimeMs));
        }

        private double CalculateErrorRate()
        {
            var totalMessages = _totalMessagesPublished + _totalMessagesFailedToPublish;
            return totalMessages > 0 ? (double)_totalMessagesFailedToPublish / totalMessages : 0.0;
        }

        private double CalculateAveragePublishingTime()
        {
            var allStats = _messageTypeStats.Values;
            if (!allStats.AsValueEnumerable().Any()) return 0.0;
            
            return allStats.AsValueEnumerable().Average(s => s.AverageProcessingTime);
        }

        private double CalculatePeakPublishingTime()
        {
            var allStats = _messageTypeStats.Values;
            if (!allStats.AsValueEnumerable().Any()) return 0.0;
            
            return allStats.AsValueEnumerable().Max(s => s.PeakProcessingTime);
        }

        private double CalculateMessagesPerSecond()
        {
            var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
            if (timeSinceReset.TotalSeconds <= 0) return 0.0;
            
            return _totalMessagesPublished / timeSinceReset.TotalSeconds;
        }

        private HealthStatus DetermineHealthStatus(MessagePublishingStatistics statistics)
        {
            // Check critical conditions
            if (statistics.ErrorRate > _config.CriticalErrorRateThreshold)
                return HealthStatus.Unhealthy;
                
            if (statistics.AveragePublishingTimeMs > _config.CriticalPublishingTimeThreshold)
                return HealthStatus.Unhealthy;
            
            // Check degraded conditions
            if (statistics.ErrorRate > _config.WarningErrorRateThreshold)
                return HealthStatus.Degraded;
                
            if (statistics.AveragePublishingTimeMs > _config.WarningPublishingTimeThreshold)
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
                    _lastHealthCheck = DateTime.UtcNow;
                    
                    _logger.LogInfo($"[{_correlationId}] Publishing health status changed from {oldStatus} to {newStatus}: {reason}");
                    
                    // Raise alert for unhealthy status
                    if (_alertService != null && newStatus == HealthStatus.Unhealthy)
                    {
                        _alertService.RaiseAlert(
                            $"MessagePublishingService health status changed to {newStatus}",
                            AlertSeverity.Critical,
                            source: "MessagePublishingService",
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

                // Force GC if memory pressure is too high
                if (_config.ForceGCOnHighMemoryPressure && currentMemory > _config.MaxMemoryPressure)
                {
                    _logger.LogWarning($"[{_correlationId}] High memory pressure detected: {currentMemory / 1024 / 1024}MB, forcing GC");
                    GC.Collect(2, GCCollectionMode.Forced);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Failed to update publishing statistics", ex);
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessagePublishingService));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message publishing service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"[{_correlationId}] Disposing MessagePublishingService");

            try
            {
                _disposed = true;

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Dispose timers
                _statisticsTimer?.Dispose();
                _healthCheckTimer?.Dispose();

                // Dispose all publishers
                foreach (var publisher in _publishers.Values.AsValueEnumerable().OfType<IDisposable>())
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

                // Dispose synchronization primitives
                _publishSemaphore?.Dispose();
                _healthStatusLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                _logger.LogInfo($"[{_correlationId}] MessagePublishingService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException($"[{_correlationId}] Error during MessagePublishingService disposal", ex);
            }
        }

        #endregion
    }
}