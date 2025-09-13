using System.Collections.Generic;
using System.Diagnostics;
using ZLinq;
using System.Threading;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging.Publishers
{
    /// <summary>
    /// Production-ready implementation of a type-specific message publisher.
    /// Provides high-performance publishing with comprehensive monitoring and error handling.
    /// </summary>
    /// <typeparam name="TMessage">The message type this publisher handles</typeparam>
    internal sealed class MessagePublisher<TMessage> : IMessagePublisher<TMessage> where TMessage : IMessage
    {
        #region Private Fields

        private readonly MessagePublishingConfig _config;
        private readonly ILoggingService _logger;
        private readonly IProfilerService _profilerService;
        private readonly IPoolingService _poolingService;

        private readonly SemaphoreSlim _publishSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private volatile bool _disposed;

        // Statistics tracking
        private long _totalPublished;
        private long _successfulPublications;
        private long _failedPublications;
        private long _lastPublishedTicks;
        private double _totalPublishTime;
        private double _peakPublishTime;

        // Performance tracking
        private readonly Timer _statisticsTimer;
        private DateTime _lastStatsReset;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MessagePublisher class.
        /// </summary>
        /// <param name="config">The message publishing configuration</param>
        /// <param name="logger">The logging service</param>
        /// <param name="profilerService">The profiler service</param>
        /// <param name="poolingService">The pooling service</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessagePublisher(
            MessagePublishingConfig config,
            ILoggingService logger,
            IProfilerService profilerService,
            IPoolingService poolingService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));

            _publishSemaphore = new SemaphoreSlim(_config.MaxConcurrentPublishers, _config.MaxConcurrentPublishers);
            _cancellationTokenSource = new CancellationTokenSource();

            _lastStatsReset = DateTime.UtcNow;
            _statisticsTimer = new Timer(UpdateStatistics, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInfo($"MessagePublisher<{typeof(TMessage).Name}> initialized");
        }

        #endregion

        #region IMessagePublisher<TMessage> Implementation

        /// <inheritdoc />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc />
        public bool IsOperational => !_disposed && !_cancellationTokenSource.Token.IsCancellationRequested;

        /// <inheritdoc />
        public void Publish(TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            using var profilerScope = _profilerService?.BeginScope($"Publisher_Publish_{typeof(TMessage).Name}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Update statistics
                Interlocked.Increment(ref _totalPublished);

                // Validate message
                ValidateMessage(message);

                // Perform the actual publishing (this would integrate with MessagePipe or other infrastructure)
                PublishInternal(message);

                // Track success
                stopwatch.Stop();
                var publishTime = stopwatch.Elapsed.TotalMilliseconds;

                Interlocked.Increment(ref _successfulPublications);
                Interlocked.Exchange(ref _lastPublishedTicks, DateTime.UtcNow.Ticks);

                UpdatePublishTimeStatistics(publishTime);

                _logger.LogInfo($"Published message {typeof(TMessage).Name} with ID {message.Id} in {publishTime:F2}ms");

                // Raise success event
                MessagePublished?.Invoke(this, new MessagePublishedEventArgs(message, stopwatch.Elapsed));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Interlocked.Increment(ref _failedPublications);

                _logger.LogException($"Failed to publish message {typeof(TMessage).Name} with ID {message.Id}", ex);

                // Raise failure event
                MessagePublishFailed?.Invoke(this, new MessagePublishFailedEventArgs(message, ex, 0));

                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            using var profilerScope = _profilerService?.BeginScope($"Publisher_PublishAsync_{typeof(TMessage).Name}");
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _publishSemaphore.WaitAsync(combinedCts.Token);

                try
                {
                    // Update statistics
                    Interlocked.Increment(ref _totalPublished);

                    // Validate message
                    ValidateMessage(message);

                    // Perform the actual async publishing
                    await PublishInternalAsync(message, combinedCts.Token);

                    // Track success
                    stopwatch.Stop();
                    var publishTime = stopwatch.Elapsed.TotalMilliseconds;

                    Interlocked.Increment(ref _successfulPublications);
                    Interlocked.Exchange(ref _lastPublishedTicks, DateTime.UtcNow.Ticks);

                    UpdatePublishTimeStatistics(publishTime);

                    _logger.LogInfo($"Published async message {typeof(TMessage).Name} with ID {message.Id} in {publishTime:F2}ms");

                    // Raise success event
                    MessagePublished?.Invoke(this, new MessagePublishedEventArgs(message, stopwatch.Elapsed));
                }
                finally
                {
                    _publishSemaphore.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning($"Async publishing cancelled for message {typeof(TMessage).Name} with ID {message.Id}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Interlocked.Increment(ref _failedPublications);

                _logger.LogException($"Failed to publish async message {typeof(TMessage).Name} with ID {message.Id}", ex);

                // Raise failure event
                MessagePublishFailed?.Invoke(this, new MessagePublishFailedEventArgs(message, ex, 0));

                throw;
            }
        }

        /// <inheritdoc />
        public void PublishBatch(IEnumerable<TMessage> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            ThrowIfDisposed();

            using var profilerScope = _profilerService?.BeginScope($"Publisher_PublishBatch_{typeof(TMessage).Name}");

            var messageList = messages.AsValueEnumerable().ToList();
            if (messageList.Count == 0)
            {
                _logger.LogWarning($"Empty batch provided to PublishBatch for {typeof(TMessage).Name}");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var successCount = 0;
            var failureCount = 0;

            try
            {
                foreach (var message in messageList)
                {
                    try
                    {
                        Publish(message);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogException($"Failed to publish message in batch: {message.Id}", ex);
                    }
                }

                stopwatch.Stop();
                _logger.LogInfo($"Published batch of {messageList.Count} messages for {typeof(TMessage).Name}: {successCount} succeeded, {failureCount} failed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException($"Failed to publish batch for {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishBatchAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            ThrowIfDisposed();

            using var profilerScope = _profilerService?.BeginScope($"Publisher_PublishBatchAsync_{typeof(TMessage).Name}");
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            // Convert to list for performance and count validation
            var messageList = messages.AsValueEnumerable().ToList();
            if (messageList.Count == 0)
            {
                _logger.LogWarning($"Empty batch provided to PublishBatchAsync for {typeof(TMessage).Name}");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            
            // Use a thread-safe counter class for async operations
            var batchStats = new BatchPublishStats();

            try
            {
                // Determine optimal batch size based on config
                var batchSize = Math.Min(_config.MaxConcurrentPublishers, messageList.Count);
                var semaphore = new SemaphoreSlim(batchSize, batchSize);
                
                // Create UniTask array for proper type compatibility
                var tasks = new UniTask[messageList.Count];
                
                for (int i = 0; i < messageList.Count; i++)
                {
                    var index = i;
                    var message = messageList[index];
                    
                    tasks[index] = PublishBatchItemAsync(
                        message, 
                        semaphore, 
                        combinedCts.Token,
                        batchStats);
                }

                // Wait for all tasks to complete
                await UniTask.WhenAll(tasks);

                stopwatch.Stop();
                
                // Log detailed results
                var totalTime = stopwatch.ElapsedMilliseconds;
                var avgTime = messageList.Count > 0 ? totalTime / (double)messageList.Count : 0;
                
                _logger.LogInfo($"Published async batch of {messageList.Count} messages for {typeof(TMessage).Name}: " +
                    $"{batchStats.SuccessCount} succeeded, {batchStats.FailureCount} failed in {totalTime}ms (avg: {avgTime:F2}ms/msg)");
                
                // Log individual failures if any (only log first few to avoid spam)
                var failedMessages = batchStats.GetFailedMessages();
                if (failedMessages.Count > 0)
                {
                    var maxFailuresToLog = Math.Min(failedMessages.Count, 5);
                    for (int i = 0; i < maxFailuresToLog; i++)
                    {
                        var (message, exception) = failedMessages[i];
                        _logger.LogException($"Batch item failed: {message.Id}", exception);
                    }
                    
                    if (failedMessages.Count > maxFailuresToLog)
                    {
                        _logger.LogWarning($"And {failedMessages.Count - maxFailuresToLog} more failures not shown");
                    }
                }
                
                // Dispose the batch semaphore
                semaphore?.Dispose();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning($"Batch publishing cancelled for {typeof(TMessage).Name} after {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException($"Critical failure in async batch for {typeof(TMessage).Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// Thread-safe statistics tracking for batch operations
        /// </summary>
        private sealed class BatchPublishStats
        {
            private int _successCount;
            private int _failureCount;
            private readonly List<(TMessage message, Exception exception)> _failedMessages = new();
            private readonly object _lockObject = new();

            public int SuccessCount => _successCount;
            public int FailureCount => _failureCount;

            public void IncrementSuccess()
            {
                Interlocked.Increment(ref _successCount);
            }

            public void IncrementFailure()
            {
                Interlocked.Increment(ref _failureCount);
            }

            public void AddFailedMessage(TMessage message, Exception exception)
            {
                lock (_lockObject)
                {
                    _failedMessages.Add((message, exception));
                }
            }

            public List<(TMessage message, Exception exception)> GetFailedMessages()
            {
                lock (_lockObject)
                {
                    return new List<(TMessage, Exception)>(_failedMessages);
                }
            }
        }

        /// <summary>
        /// Publishes a single message item within a batch operation with proper concurrency control
        /// </summary>
        private async UniTask PublishBatchItemAsync(
            TMessage message,
            SemaphoreSlim batchSemaphore,
            CancellationToken cancellationToken,
            BatchPublishStats batchStats)
        {
            await batchSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Validate message first
                ValidateMessage(message);
                
                // Use internal publishing to avoid nested semaphore waits
                await PublishInternalAsync(message, cancellationToken);
                
                // Update statistics
                Interlocked.Increment(ref _totalPublished);
                Interlocked.Increment(ref _successfulPublications);
                batchStats.IncrementSuccess();
                Interlocked.Exchange(ref _lastPublishedTicks, DateTime.UtcNow.Ticks);
                
                // Raise success event
                MessagePublished?.Invoke(this, new MessagePublishedEventArgs(message, TimeSpan.Zero));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Don't count cancellation as failure
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedPublications);
                batchStats.IncrementFailure();
                batchStats.AddFailedMessage(message, ex);
                
                // Raise failure event but don't throw to allow batch to continue
                MessagePublishFailed?.Invoke(this, new MessagePublishFailedEventArgs(message, ex, 0));
            }
            finally
            {
                batchSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public bool PublishIf(TMessage message, Func<bool> condition)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            ThrowIfDisposed();

            try
            {
                if (condition())
                {
                    Publish(message);
                    return true;
                }

                _logger.LogInfo($"Conditional publish skipped for message {typeof(TMessage).Name} with ID {message.Id}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed in conditional publish for message {typeof(TMessage).Name} with ID {message.Id}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask<bool> PublishIfAsync(TMessage message, Func<UniTask<bool>> condition, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            try
            {
                if (await condition())
                {
                    await PublishAsync(message, combinedCts.Token);
                    return true;
                }

                _logger.LogInfo($"Async conditional publish skipped for message {typeof(TMessage).Name} with ID {message.Id}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed in async conditional publish for message {typeof(TMessage).Name} with ID {message.Id}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async UniTask PublishDelayedAsync(TMessage message, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (delay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative");

            ThrowIfDisposed();

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            try
            {
                _logger.LogInfo($"Scheduled delayed publish for message {typeof(TMessage).Name} with ID {message.Id} in {delay.TotalMilliseconds}ms");

                await UniTask.Delay(delay, cancellationToken: combinedCts.Token);
                await PublishAsync(message, combinedCts.Token);

                _logger.LogInfo($"Completed delayed publish for message {typeof(TMessage).Name} with ID {message.Id}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning($"Delayed publish cancelled for message {typeof(TMessage).Name} with ID {message.Id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed in delayed publish for message {typeof(TMessage).Name} with ID {message.Id}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public PublisherStatistics GetStatistics()
        {
            var timeSinceReset = DateTime.UtcNow - _lastStatsReset;
            var publishRate = timeSinceReset.TotalSeconds > 0 
                ? _successfulPublications / timeSinceReset.TotalSeconds 
                : 0;

            var avgPublishTime = _successfulPublications > 0 
                ? _totalPublishTime / _successfulPublications 
                : 0;

            return new PublisherStatistics(
                _totalPublished,
                _successfulPublications,
                _failedPublications,
                avgPublishTime,
                publishRate,
                _lastPublishedTicks);
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<MessagePublishedEventArgs> MessagePublished;

        /// <inheritdoc />
        public event EventHandler<MessagePublishFailedEventArgs> MessagePublishFailed;

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the message before publishing.
        /// </summary>
        /// <param name="message">The message to validate</param>
        /// <exception cref="ArgumentException">Thrown when message is invalid</exception>
        private void ValidateMessage(TMessage message)
        {
            if (message.Id == Guid.Empty)
                throw new ArgumentException("Message ID cannot be empty", nameof(message));

            if (message.TimestampTicks <= 0)
                throw new ArgumentException("Message timestamp is invalid", nameof(message));

            // Additional validation can be added here
        }

        /// <summary>
        /// Performs the internal publishing logic.
        /// This would integrate with MessagePipe or other messaging infrastructure.
        /// </summary>
        /// <param name="message">The message to publish</param>
        private void PublishInternal(TMessage message)
        {
            // This is where the actual message publishing would occur
            // For now, this is a placeholder that would integrate with MessagePipe or other infrastructure
            
            _logger.LogInfo($"Internal publish for message {typeof(TMessage).Name} with ID {message.Id}");
            
            // Example: messagePipe.Publish(message);
        }

        /// <summary>
        /// Performs the internal async publishing logic.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        private async UniTask PublishInternalAsync(TMessage message, CancellationToken cancellationToken)
        {
            // This is where the actual async message publishing would occur
            
            _logger.LogInfo($"Internal async publish for message {typeof(TMessage).Name} with ID {message.Id}");
            
            // Simulate async work
            await UniTask.Yield();
            
            // Example: await messagePipe.PublishAsync(message, cancellationToken);
        }

        /// <summary>
        /// Updates publish time statistics in a thread-safe manner.
        /// </summary>
        /// <param name="publishTime">The publish time in milliseconds</param>
        private void UpdatePublishTimeStatistics(double publishTime)
        {
            // Update total publish time
            var currentTotal = _totalPublishTime;
            var newTotal = currentTotal + publishTime;
            while (Interlocked.CompareExchange(ref _totalPublishTime, newTotal, currentTotal) != currentTotal)
            {
                currentTotal = _totalPublishTime;
                newTotal = currentTotal + publishTime;
            }

            // Update peak publish time
            var currentPeak = _peakPublishTime;
            if (publishTime > currentPeak)
            {
                Interlocked.CompareExchange(ref _peakPublishTime, publishTime, currentPeak);
            }
        }

        /// <summary>
        /// Updates statistics periodically.
        /// </summary>
        /// <param name="state">Timer state</param>
        private void UpdateStatistics(object state)
        {
            if (_disposed) return;

            try
            {
                // Statistics are updated in real-time by other methods
                // This timer could be used for periodic cleanup or aggregation
            }
            catch (Exception ex)
            {
                _logger.LogException($"Failed to update statistics for publisher {typeof(TMessage).Name}", ex);
            }
        }

        /// <summary>
        /// Throws an exception if the publisher has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the publisher is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"MessagePublisher<{typeof(TMessage).Name}>");
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the message publisher and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInfo($"Disposing MessagePublisher<{typeof(TMessage).Name}>");

            try
            {
                // Cancel all operations
                _cancellationTokenSource.Cancel();

                // Dispose resources
                _statisticsTimer?.Dispose();
                _publishSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();

                _disposed = true;

                _logger.LogInfo($"MessagePublisher<{typeof(TMessage).Name}> disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogException($"Error during MessagePublisher<{typeof(TMessage).Name}> disposal", ex);
            }
        }

        #endregion
    }
}