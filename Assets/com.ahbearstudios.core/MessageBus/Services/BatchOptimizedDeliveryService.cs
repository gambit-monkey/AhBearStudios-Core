using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Events;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Batch-optimized message delivery service that accumulates messages and processes them in batches
    /// for improved throughput and reduced overhead.
    /// </summary>
    public sealed class BatchOptimizedDeliveryService : IMessageDeliveryService
    {
        private readonly IMessageBus _messageBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly BatchOptimizedConfiguration _configuration;
        
        private readonly ConcurrentQueue<BatchedMessage> _messageQueue;
        private readonly ConcurrentDictionary<(Guid, Guid), PendingBatchDelivery> _pendingDeliveries;
        private readonly Timer _batchTimer;
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _batchLock;
        private readonly DeliveryStatistics _statistics;
        
        private DeliveryServiceStatus _status = DeliveryServiceStatus.Stopped;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;
        
        /// <inheritdoc />
        public string Name => "BatchOptimized";
        
        /// <inheritdoc />
        public bool IsActive => _status == DeliveryServiceStatus.Running;
        
        /// <inheritdoc />
        public DeliveryServiceStatus Status => _status;
        
        /// <inheritdoc />
        public event EventHandler<MessageDeliveredEventArgs> MessageDelivered;
        
        /// <inheritdoc />
        public event EventHandler<MessageDeliveryFailedEventArgs> MessageDeliveryFailed;
        
        /// <inheritdoc />
        public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged;
        
        /// <inheritdoc />
        public event EventHandler<DeliveryServiceStatusChangedEventArgs> StatusChanged;
        
        /// <summary>
        /// Initializes a new instance of the BatchOptimizedDeliveryService class.
        /// </summary>
        /// <param name="messageBus">The message bus to use for sending messages.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        /// <param name="configuration">Configuration for batch optimization.</param>
        public BatchOptimizedDeliveryService(
            IMessageBus messageBus, 
            IBurstLogger logger, 
            IProfiler profiler,
            BatchOptimizedConfiguration configuration = null)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _configuration = configuration ?? new BatchOptimizedConfiguration();
            
            _messageQueue = new ConcurrentQueue<BatchedMessage>();
            _pendingDeliveries = new ConcurrentDictionary<(Guid, Guid), PendingBatchDelivery>();
            _batchLock = new SemaphoreSlim(1, 1);
            _statistics = new DeliveryStatistics();
            
            // Subscribe to acknowledgment messages
            _messageBus.Subscribe<MessageAcknowledged>(OnMessageAcknowledgedReceived);
            
            _logger.Log(LogLevel.Info, 
                $"BatchOptimizedDeliveryService initialized with batch size {_configuration.MaxBatchSize} and interval {_configuration.BatchInterval.TotalMilliseconds}ms",
                "BatchDeliveryService");
        }
        
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "Start"));
            
            if (_status != DeliveryServiceStatus.Stopped)
            {
                _logger.Log(LogLevel.Warning, $"Cannot start service - current status: {_status}", "BatchDeliveryService");
                return;
            }
            
            ChangeStatus(DeliveryServiceStatus.Starting, "Service startup initiated");
            
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start the batch processing timer
                _batchTimer = new Timer(OnBatchTimerTick, null, _configuration.BatchInterval, _configuration.BatchInterval);
                
                // Start the flush timer for forced batch processing
                _flushTimer = new Timer(OnFlushTimerTick, null, _configuration.FlushInterval, _configuration.FlushInterval);
                
                ChangeStatus(DeliveryServiceStatus.Running, "Service started successfully");
                _logger.Log(LogLevel.Info, "BatchOptimizedDeliveryService started", "BatchDeliveryService");
            }
            catch (Exception ex)
            {
                ChangeStatus(DeliveryServiceStatus.Error, $"Failed to start service: {ex.Message}");
                _logger.Log(LogLevel.Error, $"Failed to start BatchOptimizedDeliveryService: {ex.Message}", "BatchDeliveryService");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "Stop"));
            
            if (_status == DeliveryServiceStatus.Stopped)
            {
                return;
            }
            
            ChangeStatus(DeliveryServiceStatus.Stopping, "Service shutdown initiated");
            
            try
            {
                // Cancel the cancellation token to stop all operations
                _cancellationTokenSource?.Cancel();
                
                // Stop the timers
                _batchTimer?.Dispose();
                _flushTimer?.Dispose();
                
                // Process any remaining batched messages
                await ProcessPendingBatches(force: true);
                
                // Wait for any pending deliveries to complete or timeout
                var timeout = TimeSpan.FromSeconds(30);
                var stopwatch = Stopwatch.StartNew();
                
                while (_pendingDeliveries.Count > 0 && stopwatch.Elapsed < timeout)
                {
                    await Task.Delay(100, cancellationToken);
                }
                
                if (_pendingDeliveries.Count > 0)
                {
                    _logger.Log(LogLevel.Warning, 
                        $"Service stopped with {_pendingDeliveries.Count} pending deliveries",
                        "BatchDeliveryService");
                }
                
                ChangeStatus(DeliveryServiceStatus.Stopped, "Service stopped successfully");
                _logger.Log(LogLevel.Info, "BatchOptimizedDeliveryService stopped", "BatchDeliveryService");
            }
            catch (Exception ex)
            {
                ChangeStatus(DeliveryServiceStatus.Error, $"Error during service shutdown: {ex.Message}");
                _logger.Log(LogLevel.Error, $"Error stopping BatchOptimizedDeliveryService: {ex.Message}", "BatchDeliveryService");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "SendAsync"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            var batchedMessage = new BatchedMessage(message, DeliveryType.FireAndForget, Guid.NewGuid());
            _messageQueue.Enqueue(batchedMessage);
            _statistics.RecordMessageSent();
            
            _logger.Log(LogLevel.Debug, 
                $"Queued fire-and-forget message of type {typeof(TMessage).Name} with ID {message.Id} for batch processing",
                "BatchDeliveryService");
            
            // Check if we should trigger immediate batch processing
            if (_messageQueue.Count >= _configuration.MaxBatchSize)
            {
                _ = Task.Run(() => ProcessPendingBatches(force: false), cancellationToken);
            }
        }
        
        /// <inheritdoc />
        public async Task<DeliveryResult> SendWithConfirmationAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "SendWithConfirmation"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            var deliveryId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<DeliveryResult>();
            var pendingDelivery = new PendingBatchDelivery(message, deliveryId, DeliveryType.WithConfirmation, tcs);
            
            var key = (message.Id, deliveryId);
            _pendingDeliveries[key] = pendingDelivery;
            
            var batchedMessage = new BatchedMessage(message, DeliveryType.WithConfirmation, deliveryId);
            _messageQueue.Enqueue(batchedMessage);
            _statistics.RecordMessageSent();
            
            _logger.Log(LogLevel.Debug, 
                $"Queued confirmation message of type {typeof(TMessage).Name} with ID {message.Id} and delivery ID {deliveryId} for batch processing",
                "BatchDeliveryService");
            
            // Check if we should trigger immediate batch processing
            if (_messageQueue.Count >= _configuration.MaxBatchSize)
            {
                _ = Task.Run(() => ProcessPendingBatches(force: false), cancellationToken);
            }
            
            // Wait for acknowledgment or timeout
            using var timeoutCts = new CancellationTokenSource(_configuration.ConfirmationTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            try
            {
                return await tcs.Task.WaitAsync(combinedCts.Token);
            }
            catch (OperationCanceledException)
            {
                _pendingDeliveries.TryRemove(key, out _);
                _statistics.RecordMessageFailed();
                
                var errorMessage = cancellationToken.IsCancellationRequested 
                    ? "Operation was cancelled" 
                    : "Message delivery confirmation timed out";
                
                return DeliveryResult.Failure(message.Id, deliveryId, errorMessage);
            }
        }
        
        /// <inheritdoc />
        public async Task<ReliableDeliveryResult> SendReliableAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IReliableMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "SendReliable"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            var tcs = new TaskCompletionSource<ReliableDeliveryResult>();
            var pendingDelivery = new PendingBatchDelivery(message, message.DeliveryId, DeliveryType.Reliable, tcs);
            
            var key = (message.Id, message.DeliveryId);
            _pendingDeliveries[key] = pendingDelivery;
            
            var batchedMessage = new BatchedMessage(message, DeliveryType.Reliable, message.DeliveryId);
            _messageQueue.Enqueue(batchedMessage);
            _statistics.RecordMessageSent();
            
            _logger.Log(LogLevel.Debug, 
                $"Queued reliable message of type {typeof(TMessage).Name} with ID {message.Id} and delivery ID {message.DeliveryId} for batch processing",
                "BatchDeliveryService");
            
            // Check if we should trigger immediate batch processing for reliable messages
            if (_messageQueue.Count >= _configuration.MaxBatchSize || 
                _configuration.ImmediateProcessingForReliable)
            {
                _ = Task.Run(() => ProcessPendingBatches(force: false), cancellationToken);
            }
            
            // Return the task - it will be completed when the message is acknowledged or expires
            return await tcs.Task;
        }
        
        /// <inheritdoc />
        public async Task<BatchDeliveryResult> SendBatchAsync(IEnumerable<IMessage> messages, BatchDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "SendBatch"));
            
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            EnsureServiceRunning();
            
            var messageList = messages.ToList();
            var startTime = DateTime.UtcNow;
            var results = new List<DeliveryResult>();
            
            if (messageList.Count == 0)
            {
                return new BatchDeliveryResult(results, DateTime.UtcNow, TimeSpan.Zero);
            }
            
            _logger.Log(LogLevel.Info, 
                $"Processing external batch of {messageList.Count} messages",
                "BatchDeliveryService");
            
            // Process messages based on batch options
            if (options.RequireConfirmation)
            {
                // Process with confirmation
                var confirmationTasks = messageList.Select(async message =>
                {
                    try
                    {
                        return await SendWithConfirmationAsync(message, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        return DeliveryResult.Failure(message.Id, Guid.NewGuid(), ex.Message, ex);
                    }
                });
                
                var confirmationResults = await Task.WhenAll(confirmationTasks);
                results.AddRange(confirmationResults);
            }
            else
            {
                // Process fire-and-forget
                foreach (var message in messageList)
                {
                    try
                    {
                        await SendAsync(message, cancellationToken);
                        results.Add(DeliveryResult.Success(message.Id, Guid.NewGuid(), DateTime.UtcNow));
                    }
                    catch (Exception ex)
                    {
                        results.Add(DeliveryResult.Failure(message.Id, Guid.NewGuid(), ex.Message, ex));
                        
                        if (options.StopOnFirstError)
                        {
                            break;
                        }
                    }
                }
            }
            
            var completionTime = DateTime.UtcNow;
            var duration = completionTime - startTime;
            
            var successCount = results.Count(r => r.IsSuccess);
            var failureCount = results.Count - successCount;
            
            _logger.Log(LogLevel.Info, 
                $"External batch completed: {successCount} successful, {failureCount} failed in {duration.TotalSeconds:F2} seconds",
                "BatchDeliveryService");
            
            return new BatchDeliveryResult(results, completionTime, duration);
        }
        
        /// <inheritdoc />
        public async Task AcknowledgeMessageAsync(Guid messageId, Guid deliveryId, CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "AcknowledgeMessage"));
            
            _messageBus.Publish(new MessageAcknowledged
            {
                AcknowledgedMessageId = messageId,
                AcknowledgedDeliveryId = deliveryId,
                AcknowledgmentTime = DateTime.UtcNow
            });
            
            _logger.Log(LogLevel.Debug, 
                $"Sent acknowledgment for message {messageId} with delivery ID {deliveryId}",
                "BatchDeliveryService");
        }
        
        /// <inheritdoc />
        public MessageDeliveryStatus? GetMessageStatus(Guid messageId, Guid deliveryId)
        {
            var key = (messageId, deliveryId);
            if (_pendingDeliveries.TryGetValue(key, out var delivery))
            {
                return delivery.Status;
            }
            
            return null;
        }
        
        /// <inheritdoc />
        public IReadOnlyCollection<IPendingDelivery> GetPendingDeliveries()
        {
            return _pendingDeliveries.Values.Cast<IPendingDelivery>().ToList();
        }
        
        /// <inheritdoc />
        public IDeliveryStatistics GetStatistics()
        {
            _statistics.UpdatePendingCount(_pendingDeliveries.Count);
            _statistics.UpdateQueuedCount(_messageQueue.Count);
            return _statistics;
        }
        
        /// <inheritdoc />
        public bool CancelDelivery(Guid messageId, Guid deliveryId)
        {
            var key = (messageId, deliveryId);
            if (_pendingDeliveries.TryRemove(key, out var delivery))
            {
                delivery.Cancel();
                _logger.Log(LogLevel.Debug, 
                    $"Cancelled delivery for message {messageId} with delivery ID {deliveryId}",
                    "BatchDeliveryService");
                return true;
            }
            
            return false;
        }
        
        private async Task ProcessPendingBatches(bool force)
        {
            if (!await _batchLock.WaitAsync(100))
            {
                return; // Skip this cycle if we can't get the lock quickly
            }
            
            try
            {
                using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "ProcessPendingBatches"));
                
                var messagesToProcess = new List<BatchedMessage>();
                var batchSize = force ? int.MaxValue : _configuration.MaxBatchSize;
                
                // Dequeue messages up to batch size
                for (int i = 0; i < batchSize && _messageQueue.TryDequeue(out var message); i++)
                {
                    messagesToProcess.Add(message);
                }
                
                if (messagesToProcess.Count == 0)
                {
                    return;
                }
                
                _logger.Log(LogLevel.Debug, 
                    $"Processing batch of {messagesToProcess.Count} messages (force: {force})",
                    "BatchDeliveryService");
                
                // Group messages by type for optimized batch processing
                var messagesByType = messagesToProcess.GroupBy(m => m.Message.GetType()).ToList();
                
                var stopwatch = Stopwatch.StartNew();
                int successCount = 0;
                int failureCount = 0;
                
                foreach (var typeGroup in messagesByType)
                {
                    try
                    {
                        // Process messages of the same type together
                        await ProcessMessageTypeGroup(typeGroup.ToList());
                        successCount += typeGroup.Count();
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, 
                            $"Error processing batch for message type {typeGroup.Key.Name}: {ex.Message}",
                            "BatchDeliveryService");
                        
                        failureCount += typeGroup.Count();
                        
                        // Mark individual messages as failed
                        foreach (var failedMessage in typeGroup)
                        {
                            HandleMessageFailure(failedMessage, ex);
                        }
                    }
                }
                
                stopwatch.Stop();
                _statistics.RecordBatchProcessed(messagesToProcess.Count, stopwatch.Elapsed);
                
                _logger.Log(LogLevel.Debug, 
                    $"Completed batch processing: {successCount} successful, {failureCount} failed in {stopwatch.ElapsedMilliseconds}ms",
                    "BatchDeliveryService");
            }
            finally
            {
                _batchLock.Release();
            }
        }
        
        private async Task ProcessMessageTypeGroup(List<BatchedMessage> messages)
        {
            // Extract the actual messages for batch publishing
            var actualMessages = messages.Select(bm => bm.Message).ToList();
            
            // Publish all messages of this type as a batch
            foreach (var message in actualMessages)
            {
                _messageBus.PublishMessage(message);
            }
            
            // Handle delivery tracking for messages that require it
            foreach (var batchedMessage in messages)
            {
                if (batchedMessage.DeliveryType == DeliveryType.FireAndForget)
                {
                    // Fire-and-forget messages are considered delivered immediately
                    _statistics.RecordMessageDelivered();
                    
                    MessageDelivered?.Invoke(this, new MessageDeliveredEventArgs(
                        batchedMessage.Message,
                        batchedMessage.DeliveryId,
                        DateTime.UtcNow,
                        1));
                }
                else
                {
                    // For confirmation and reliable messages, they remain in pending deliveries
                    // until acknowledged or they timeout
                    var key = (batchedMessage.Message.Id, batchedMessage.DeliveryId);
                    if (_pendingDeliveries.TryGetValue(key, out var pendingDelivery))
                    {
                        pendingDelivery.UpdateStatus(MessageDeliveryStatus.Sent);
                    }
                }
            }
        }
        
        private void HandleMessageFailure(BatchedMessage batchedMessage, Exception exception)
        {
            _statistics.RecordMessageFailed();
            
            var key = (batchedMessage.Message.Id, batchedMessage.DeliveryId);
            
            if (batchedMessage.DeliveryType == DeliveryType.FireAndForget)
            {
                MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                    batchedMessage.Message,
                    batchedMessage.DeliveryId,
                    exception.Message,
                    exception,
                    1,
                    false));
            }
            else if (_pendingDeliveries.TryGetValue(key, out var pendingDelivery))
            {
                var result = batchedMessage.DeliveryType == DeliveryType.Reliable
                    ? (object)ReliableDeliveryResult.Failure(
                        batchedMessage.Message.Id,
                        batchedMessage.DeliveryId,
                        1,
                        MessageDeliveryStatus.Failed,
                        exception.Message,
                        exception)
                    : DeliveryResult.Failure(
                        batchedMessage.Message.Id,
                        batchedMessage.DeliveryId,
                        exception.Message,
                        exception);
                
                pendingDelivery.Fail(result);
                _pendingDeliveries.TryRemove(key, out _);
                
                MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                    batchedMessage.Message,
                    batchedMessage.DeliveryId,
                    exception.Message,
                    exception,
                    1,
                    false));
            }
        }
        
        private void OnMessageAcknowledgedReceived(MessageAcknowledged ack)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag("BatchDeliveryService", "ProcessAcknowledgment"));
            
            var key = (ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId);
            if (_pendingDeliveries.TryRemove(key, out var delivery))
            {
                var result = delivery.DeliveryType == DeliveryType.Reliable
                    ? (object)ReliableDeliveryResult.Success(
                        ack.AcknowledgedMessageId,
                        ack.AcknowledgedDeliveryId,
                        1,
                        ack.AcknowledgmentTime)
                    : DeliveryResult.Success(
                        ack.AcknowledgedMessageId,
                        ack.AcknowledgedDeliveryId,
                        ack.AcknowledgmentTime);
                
                delivery.Complete(result);
                _statistics.RecordMessageDelivered();
                _statistics.RecordMessageAcknowledged();
                
                MessageAcknowledged?.Invoke(this, new MessageAcknowledgedEventArgs(
                    ack.AcknowledgedMessageId,
                    ack.AcknowledgedDeliveryId,
                    ack.AcknowledgmentTime));
                
                MessageDelivered?.Invoke(this, new MessageDeliveredEventArgs(
                    delivery.Message,
                    delivery.DeliveryId,
                    ack.AcknowledgmentTime,
                    1));
                
                _logger.Log(LogLevel.Debug, 
                    $"Message {ack.AcknowledgedMessageId} with delivery ID {ack.AcknowledgedDeliveryId} acknowledged in batch service",
                    "BatchDeliveryService");
            }
        }
        
        private void OnBatchTimerTick(object state)
        {
            if (!IsActive) return;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessPendingBatches(force: false);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, 
                        $"Error in batch timer processing: {ex.Message}",
                        "BatchDeliveryService");
                }
            });
        }
        
        private void OnFlushTimerTick(object state)
        {
            if (!IsActive) return;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessPendingBatches(force: true);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, 
                        $"Error in flush timer processing: {ex.Message}",
                        "BatchDeliveryService");
                }
            });
        }
        
        private void EnsureServiceRunning()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException($"Service is not running. Current status: {_status}");
            }
        }
        
        private void ChangeStatus(DeliveryServiceStatus newStatus, string reason = null)
        {
            var previousStatus = _status;
            _status = newStatus;
            
            StatusChanged?.Invoke(this, new DeliveryServiceStatusChangedEventArgs(
                previousStatus,
                newStatus,
                DateTime.UtcNow,
                reason));
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed) return;
            
            try
            {
                StopAsync().Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, 
                    $"Error during BatchOptimizedDeliveryService disposal: {ex.Message}",
                    "BatchDeliveryService");
            }
            
            _batchTimer?.Dispose();
            _flushTimer?.Dispose();
            _batchLock?.Dispose();
            _cancellationTokenSource?.Dispose();
            
            // Cancel any remaining pending deliveries
            foreach (var delivery in _pendingDeliveries.Values)
            {
                delivery.Cancel();
            }
            
            _pendingDeliveries.Clear();
            
            // Clear the message queue
            while (_messageQueue.TryDequeue(out _)) { }
            
            _isDisposed = true;
            
            _logger.Log(LogLevel.Info, "BatchOptimizedDeliveryService disposed", "BatchDeliveryService");
        }
    }
}