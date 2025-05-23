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
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Reliable message delivery service that provides guaranteed message delivery with retry logic.
    /// </summary>
    public sealed class ReliableMessageDeliveryService : IMessageDeliveryService
    {
        private readonly IMessageBus _messageBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly ConcurrentDictionary<(Guid, Guid), PendingDelivery> _pendingDeliveries;
        private readonly Timer _deliveryTimer;
        private readonly SemaphoreSlim _deliveryLock;
        private readonly DeliveryStatistics _statistics;
        private readonly TimeSpan _timerInterval = TimeSpan.FromSeconds(1);
        
        private DeliveryServiceStatus _status = DeliveryServiceStatus.Stopped;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed = false;
       // ReliableMessageDeliveryService.cs (continued)
        /// <inheritdoc />
        public string Name => "ReliableMessageDelivery";
        
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
        /// Initializes a new instance of the ReliableMessageDeliveryService class.
        /// </summary>
        /// <param name="messageBus">The message bus to use for sending messages.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public ReliableMessageDeliveryService(IMessageBus messageBus, IBurstLogger logger, IProfiler profiler)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            
            _pendingDeliveries = new ConcurrentDictionary<(Guid, Guid), PendingDelivery>();
            _deliveryLock = new SemaphoreSlim(1, 1);
            _statistics = new DeliveryStatistics();
            
            // Subscribe to acknowledgment messages
            _messageBus.Subscribe<MessageAcknowledged>(OnMessageAcknowledgedReceived);
            
            _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService initialized", "DeliveryService");
        }
        
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "Start"));
            
            if (_status != DeliveryServiceStatus.Stopped)
            {
                _logger.Log(LogLevel.Warning, $"Cannot start service - current status: {_status}", "DeliveryService");
                return;
            }
            
            ChangeStatus(DeliveryServiceStatus.Starting, "Service startup initiated");
            
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start the delivery timer
                _deliveryTimer = new Timer(OnDeliveryTimerTick, null, _timerInterval, _timerInterval);
                
                ChangeStatus(DeliveryServiceStatus.Running, "Service started successfully");
                _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService started", "DeliveryService");
            }
            catch (Exception ex)
            {
                ChangeStatus(DeliveryServiceStatus.Error, $"Failed to start service: {ex.Message}");
                _logger.Log(LogLevel.Error, $"Failed to start ReliableMessageDeliveryService: {ex.Message}", "DeliveryService");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "Stop"));
            
            if (_status == DeliveryServiceStatus.Stopped)
            {
                return;
            }
            
            ChangeStatus(DeliveryServiceStatus.Stopping, "Service shutdown initiated");
            
            try
            {
                // Cancel the cancellation token to stop all operations
                _cancellationTokenSource?.Cancel();
                
                // Stop the delivery timer
                _deliveryTimer?.Dispose();
                
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
                        "DeliveryService");
                }
                
                ChangeStatus(DeliveryServiceStatus.Stopped, "Service stopped successfully");
                _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService stopped", "DeliveryService");
            }
            catch (Exception ex)
            {
                ChangeStatus(DeliveryServiceStatus.Error, $"Error during service shutdown: {ex.Message}");
                _logger.Log(LogLevel.Error, $"Error stopping ReliableMessageDeliveryService: {ex.Message}", "DeliveryService");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "SendAsync"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            try
            {
                _messageBus.Publish(message);
                _statistics.RecordMessageSent();
                
                _logger.Log(LogLevel.Debug, 
                    $"Sent fire-and-forget message of type {typeof(TMessage).Name} with ID {message.Id}",
                    "DeliveryService");
            }
            catch (Exception ex)
            {
                _statistics.RecordMessageFailed();
                _logger.Log(LogLevel.Error, 
                    $"Failed to send message: {ex.Message}",
                    "DeliveryService");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<DeliveryResult> SendWithConfirmationAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "SendWithConfirmation"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            var deliveryId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<DeliveryResult>();
            var delivery = new PendingDelivery(message, deliveryId, 1, false, tcs);
            
            var key = (message.Id, deliveryId);
            _pendingDeliveries[key] = delivery;
            
            try
            {
                _messageBus.Publish(message);
                _statistics.RecordMessageSent();
                
                _logger.Log(LogLevel.Debug, 
                    $"Sent message with confirmation of type {typeof(TMessage).Name} with ID {message.Id} and delivery ID {deliveryId}",
                    "DeliveryService");
                
                // Wait for acknowledgment or timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
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
                        : "Message delivery timed out";
                    
                    return DeliveryResult.Failure(message.Id, deliveryId, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _pendingDeliveries.TryRemove(key, out _);
                _statistics.RecordMessageFailed();
                
                _logger.Log(LogLevel.Error, 
                    $"Failed to send message with confirmation: {ex.Message}",
                    "DeliveryService");
                
                return DeliveryResult.Failure(message.Id, deliveryId, ex.Message, ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<ReliableDeliveryResult> SendReliableAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IReliableMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "SendReliable"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            var tcs = new TaskCompletionSource<ReliableDeliveryResult>();
            var delivery = new PendingDelivery(message, message.DeliveryId, message.MaxDeliveryAttempts, true, tcs);
            
            var key = (message.Id, message.DeliveryId);
            _pendingDeliveries[key] = delivery;
            
            try
            {
                // Send the initial message
                message.DeliveryAttempts = 1;
                message.ScheduleNextAttempt();
                delivery.UpdateStatus(MessageDeliveryStatus.Sending);
                
                _messageBus.Publish(message);
                _statistics.RecordMessageSent();
                
                delivery.UpdateStatus(MessageDeliveryStatus.Sent);
                
                _logger.Log(LogLevel.Debug, 
                    $"Sent reliable message of type {typeof(TMessage).Name} with ID {message.Id} and delivery ID {message.DeliveryId} (attempt 1/{message.MaxDeliveryAttempts})",
                    "DeliveryService");
                
                // Return the task - it will be completed when the message is acknowledged or expires
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _pendingDeliveries.TryRemove(key, out _);
                _statistics.RecordMessageFailed();
                
                _logger.Log(LogLevel.Error, 
                    $"Failed to send reliable message: {ex.Message}",
                    "DeliveryService");
                
                return ReliableDeliveryResult.Failure(
                    message.Id, 
                    message.DeliveryId, 
                    1, 
                    MessageDeliveryStatus.Failed, 
                    ex.Message, 
                    ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<BatchDeliveryResult> SendBatchAsync(IEnumerable<IMessage> messages, BatchDeliveryOptions options, CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "SendBatch"));
            
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
                $"Starting batch delivery of {messageList.Count} messages with max concurrency {options.MaxConcurrency}",
                "DeliveryService");
            
            try
            {
                using var semaphore = new SemaphoreSlim(options.MaxConcurrency);
                using var batchCts = new CancellationTokenSource(options.BatchTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, batchCts.Token);
                
                var tasks = messageList.Select(async message =>
                {
                    await semaphore.WaitAsync(combinedCts.Token);
                    try
                    {
                        using var messageCts = new CancellationTokenSource(options.MessageTimeout);
                        using var messageTokens = CancellationTokenSource.CreateLinkedTokenSource(combinedCts.Token, messageCts.Token);
                        
                        if (options.RequireConfirmation)
                        {
                            return await SendWithConfirmationAsync(message, messageTokens.Token);
                        }
                        else
                        {
                            await SendAsync(message, messageTokens.Token);
                            return DeliveryResult.Success(message.Id, Guid.NewGuid(), DateTime.UtcNow);
                        }
                    }
                    catch (Exception ex)
                    {
                        return DeliveryResult.Failure(message.Id, Guid.NewGuid(), ex.Message, ex);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                var completedTasks = await Task.WhenAll(tasks);
                results.AddRange(completedTasks);
                
                var completionTime = DateTime.UtcNow;
                var duration = completionTime - startTime;
                
                var successCount = results.Count(r => r.IsSuccess);
                var failureCount = results.Count - successCount;
                
                _logger.Log(LogLevel.Info, 
                    $"Batch delivery completed: {successCount} successful, {failureCount} failed in {duration.TotalSeconds:F2} seconds",
                    "DeliveryService");
                
                return new BatchDeliveryResult(results, completionTime, duration);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, 
                    $"Batch delivery failed: {ex.Message}",
                    "DeliveryService");
                
                // Fill in failure results for any messages that weren't processed
                while (results.Count < messageList.Count)
                {
                    var message = messageList[results.Count];
                    results.Add(DeliveryResult.Failure(message.Id, Guid.NewGuid(), "Batch operation failed", ex));
                }
                
                return new BatchDeliveryResult(results, DateTime.UtcNow, DateTime.UtcNow - startTime);
            }
        }
        
        /// <inheritdoc />
        public async Task AcknowledgeMessageAsync(Guid messageId, Guid deliveryId, CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "AcknowledgeMessage"));
            
            _messageBus.Publish(new MessageAcknowledged
            {
                AcknowledgedMessageId = messageId,
                AcknowledgedDeliveryId = deliveryId,
                AcknowledgmentTime = DateTime.UtcNow
            });
            
            _logger.Log(LogLevel.Debug, 
                $"Sent acknowledgment for message {messageId} with delivery ID {deliveryId}",
                "DeliveryService");
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
                    "DeliveryService");
                return true;
            }
            
            return false;
        }
        
        private void OnMessageAcknowledgedReceived(MessageAcknowledged ack)
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "ProcessAcknowledgment"));
            
            var key = (ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId);
            if (_pendingDeliveries.TryRemove(key, out var delivery))
            {
                delivery.Complete(DeliveryResult.Success(ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId, ack.AcknowledgmentTime));
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
                    delivery.DeliveryAttempts));
                
                _logger.Log(LogLevel.Debug, 
                    $"Message {ack.AcknowledgedMessageId} with delivery ID {ack.AcknowledgedDeliveryId} acknowledged",
                    "DeliveryService");
            }
        }
        
        private void OnDeliveryTimerTick(object state)
        {
            if (!IsActive) return;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessPendingDeliveries();
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, 
                        $"Error processing pending deliveries: {ex.Message}",
                        "DeliveryService");
                }
            });
        }
        
        private async Task ProcessPendingDeliveries()
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "ProcessPendingDeliveries"));
            
            if (!await _deliveryLock.WaitAsync(100))
            {
                return; // Skip this cycle if we can't get the lock quickly
            }
            
            try
            {
                var now = DateTime.UtcNow;
                var nowTicks = now.Ticks;
                var deliveriesToRetry = new List<PendingDelivery>();
                var deliveriesToExpire = new List<(Guid, Guid)>();
                
                foreach (var kvp in _pendingDeliveries)
                {
                    var key = kvp.Key;
                    var delivery = kvp.Value;
                    
                    if (delivery.Status == MessageDeliveryStatus.Cancelled)
                    {
                        deliveriesToExpire.Add(key);
                        continue;
                    }
                    
                    if (delivery.IsReliable && delivery.Message is IReliableMessage reliableMessage)
                    {
                        if (reliableMessage.DeliveryAttempts >= reliableMessage.MaxDeliveryAttempts)
                        {
                            // Max attempts reached
                            delivery.Expire(ReliableDeliveryResult.Failure(
                                reliableMessage.Id,
                                reliableMessage.DeliveryId,
                                reliableMessage.DeliveryAttempts,
                                MessageDeliveryStatus.Expired,
                                $"Maximum delivery attempts ({reliableMessage.MaxDeliveryAttempts}) reached"));
                            
                            deliveriesToExpire.Add(key);
                            _statistics.RecordMessageFailed();
                            
                            MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                                reliableMessage,
                                reliableMessage.DeliveryId,
                                "Maximum delivery attempts reached",
                                null,
                                reliableMessage.DeliveryAttempts,
                                false));
                            
                            continue;
                        }
                        
                        if (reliableMessage.NextAttemptTicks <= nowTicks)
                        {
                            // Time to retry
                            deliveriesToRetry.Add(delivery);
                        }
                    }
                }
                
                // Remove expired deliveries
                foreach (var key in deliveriesToExpire)
                {
                    _pendingDeliveries.TryRemove(key, out _);
                }
                
                // Process retries
                foreach (var delivery in deliveriesToRetry)
                {
                    if (delivery.Message is IReliableMessage reliableMessage)
                    {
                        try
                        {
                            reliableMessage.DeliveryAttempts++;
                            reliableMessage.ScheduleNextAttempt();
                            delivery.UpdateStatus(MessageDeliveryStatus.Sending);
                            
                            _messageBus.Publish(reliableMessage);
                            _statistics.RecordMessageSent();
                            
                            delivery.UpdateStatus(MessageDeliveryStatus.Sent);
                            
                            _logger.Log(LogLevel.Debug, 
                                $"Retrying reliable message {reliableMessage.Id} with delivery ID {reliableMessage.DeliveryId} (attempt {reliableMessage.DeliveryAttempts}/{reliableMessage.MaxDeliveryAttempts})",
                                "DeliveryService");
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogLevel.Error, 
                                $"Failed to retry message {reliableMessage.Id}: {ex.Message}",
                                "DeliveryService");
                            
                            MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                                reliableMessage,
                                reliableMessage.DeliveryId,
                                ex.Message,
                                ex,
                                reliableMessage.DeliveryAttempts,
                                reliableMessage.DeliveryAttempts < reliableMessage.MaxDeliveryAttempts));
                        }
                    }
                }
            }
            finally
            {
                _deliveryLock.Release();
            }
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
                    $"Error during ReliableMessageDeliveryService disposal: {ex.Message}",
                    "DeliveryService");
            }
            
            _deliveryTimer?.Dispose();
            _deliveryLock?.Dispose();
            _cancellationTokenSource?.Dispose();
            
            // Cancel any remaining pending deliveries
            foreach (var delivery in _pendingDeliveries.Values)
            {
                delivery.Cancel();
            }
            
            _pendingDeliveries.Clear();
            _isDisposed = true;
            
            _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService disposed", "DeliveryService");
        }
    }
} 
        