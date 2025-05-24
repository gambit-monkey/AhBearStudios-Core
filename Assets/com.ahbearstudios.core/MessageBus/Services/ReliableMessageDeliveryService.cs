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
        private readonly DeliveryServiceConfiguration _configuration;

        private readonly ConcurrentDictionary<(Guid, Guid), PendingDelivery> _pendingDeliveries;
        private readonly SemaphoreSlim _deliveryLock;
        private readonly DeliveryStatistics _statistics;
        private readonly ProfilerTag _profileTag;
        private readonly object _statusLock = new object();

        private Timer _deliveryTimer;
        private DeliveryServiceStatus _status = DeliveryServiceStatus.Stopped;
        private CancellationTokenSource _cancellationTokenSource;
        private IDisposable _acknowledgmentSubscription;
        private bool _isDisposed;

        /// <inheritdoc />
        public string Name => "ReliableMessageDelivery";

        /// <inheritdoc />
        public bool IsActive
        {
            get
            {
                lock (_statusLock)
                {
                    return _status == DeliveryServiceStatus.Running;
                }
            }
        }

        /// <inheritdoc />
        public DeliveryServiceStatus Status
        {
            get
            {
                lock (_statusLock)
                {
                    return _status;
                }
            }
        }

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
        /// <param name="configuration">Configuration for the delivery service.</param>
        public ReliableMessageDeliveryService(
            IMessageBus messageBus,
            IBurstLogger logger,
            IProfiler profiler,
            DeliveryServiceConfiguration configuration = null)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _configuration = configuration ?? new DeliveryServiceConfiguration();

            _pendingDeliveries = new ConcurrentDictionary<(Guid, Guid), PendingDelivery>();
            _deliveryLock = new SemaphoreSlim(1, 1);
            _statistics = new DeliveryStatistics();
            _profileTag = new ProfilerTag(new ProfilerCategory("DeliveryService"), Name);

            _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService initialized", "DeliveryService");
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(_profileTag);

            DeliveryServiceStatus currentStatus;
            lock (_statusLock)
            {
                currentStatus = _status;
                if (currentStatus != DeliveryServiceStatus.Stopped)
                {
                    _logger.Log(LogLevel.Warning,
                        $"Cannot start service - current status: {currentStatus}",
                        "DeliveryService");
                    return;
                }

                _status = DeliveryServiceStatus.Starting;
            }

            ChangeStatus(DeliveryServiceStatus.Starting, "Service startup initiated");

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                // Subscribe to acknowledgment messages
                _acknowledgmentSubscription = _messageBus.Subscribe<MessageAcknowledged>(OnMessageAcknowledgedReceived);

                // Start the delivery timer
                _deliveryTimer = new Timer(
                    OnDeliveryTimerTick,
                    null,
                    _configuration.ProcessingInterval,
                    _configuration.ProcessingInterval);

                ChangeStatus(DeliveryServiceStatus.Running, "Service started successfully");
                _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService started", "DeliveryService");
            }
            catch (Exception ex)
            {
                ChangeStatus(DeliveryServiceStatus.Error, $"Failed to start service: {ex.Message}");
                _logger.Log(LogLevel.Error,
                    $"Failed to start ReliableMessageDeliveryService: {ex.Message}",
                    "DeliveryService");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(_profileTag);

            lock (_statusLock)
            {
                if (_status == DeliveryServiceStatus.Stopped)
                {
                    return;
                }
            }

            ChangeStatus(DeliveryServiceStatus.Stopping, "Service shutdown initiated");

            try
            {
                // Cancel the cancellation token to stop all operations
                _cancellationTokenSource?.Cancel();

                // Stop the delivery timer
                await DisposeTimerAsync();

                // Unsubscribe from acknowledgment messages
                _acknowledgmentSubscription?.Dispose();
                _acknowledgmentSubscription = null;

                // Wait for any pending deliveries to complete or timeout
                await WaitForPendingDeliveries(cancellationToken);

                ChangeStatus(DeliveryServiceStatus.Stopped, "Service stopped successfully");
                _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService stopped", "DeliveryService");
            }
            catch (Exception ex)
            {
                ChangeStatus(DeliveryServiceStatus.Error, $"Error during service shutdown: {ex.Message}");
                _logger.Log(LogLevel.Error,
                    $"Error stopping ReliableMessageDeliveryService: {ex.Message}",
                    "DeliveryService");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(_profileTag);

            if (message == null) throw new ArgumentNullException(nameof(message));

            EnsureServiceRunning();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _messageBus.Publish(message);
                _statistics.RecordMessageSent();
                _statistics.RecordMessageDelivered(); // Fire-and-forget is immediately "delivered"

                stopwatch.Stop();
                _statistics.RecordDeliveryTime((long)stopwatch.Elapsed.TotalMilliseconds);

                MessageDelivered?.Invoke(this, new MessageDeliveredEventArgs(
                    message,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    1));

                _logger.Log(LogLevel.Debug,
                    $"Sent fire-and-forget message of type {typeof(TMessage).Name} with ID {message.Id}",
                    "DeliveryService");
            }
            catch (Exception ex)
            {
                _statistics.RecordMessageFailed();

                MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                    message,
                    Guid.NewGuid(),
                    ex.Message,
                    ex,
                    1,
                    false));

                _logger.Log(LogLevel.Error,
                    $"Failed to send message: {ex.Message}",
                    "DeliveryService");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DeliveryResult> SendWithConfirmationAsync<TMessage>(TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(_profileTag);

            if (message == null) throw new ArgumentNullException(nameof(message));

            EnsureServiceRunning();

            var deliveryId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var delivery = new PendingDelivery(message, deliveryId, 1, false, tcs);

            var key = (message.Id, deliveryId);
            _pendingDeliveries[key] = delivery;

            try
            {
                delivery.UpdateStatus(MessageDeliveryStatus.Sending);
                _messageBus.Publish(message);
                _statistics.RecordMessageSent();
                delivery.UpdateStatus(MessageDeliveryStatus.Sent);

                _logger.Log(LogLevel.Debug,
                    $"Sent message with confirmation of type {typeof(TMessage).Name} with ID {message.Id} and delivery ID {deliveryId}",
                    "DeliveryService");

                // Wait for acknowledgment or timeout
                using var timeoutCts = new CancellationTokenSource(_configuration.DefaultTimeout);
                using var combinedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    // Use Task.WhenAny to implement timeout functionality
                    var completionTask = tcs.Task;
                    var timeoutTask = Task.Delay(_configuration.DefaultTimeout, combinedCts.Token);

                    var completedTask = await Task.WhenAny(completionTask, timeoutTask);
                    if (completedTask == completionTask)
                    {
                        return (DeliveryResult)await completionTask;
                    }

                    // If we get here, the timeout task completed first
                    _pendingDeliveries.TryRemove(key, out _);
                    _statistics.RecordMessageFailed();

                    var errorMessage = cancellationToken.IsCancellationRequested
                        ? "Operation was cancelled"
                        : "Message delivery timed out";

                    MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                        message,
                        deliveryId,
                        errorMessage,
                        null,
                        1,
                        false));

                    return DeliveryResult.Failure(message.Id, deliveryId, errorMessage);
                }
                catch (OperationCanceledException)
                {
                    _pendingDeliveries.TryRemove(key, out _);
                    _statistics.RecordMessageFailed();

                    var errorMessage = cancellationToken.IsCancellationRequested
                        ? "Operation was cancelled"
                        : "Message delivery timed out";

                    MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                        message,
                        deliveryId,
                        errorMessage,
                        null,
                        1,
                        false));

                    return DeliveryResult.Failure(message.Id, deliveryId, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _pendingDeliveries.TryRemove(key, out _);
                _statistics.RecordMessageFailed();

                MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                    message,
                    deliveryId,
                    ex.Message,
                    ex,
                    1,
                    false));

                _logger.Log(LogLevel.Error,
                    $"Failed to send message with confirmation: {ex.Message}",
                    "DeliveryService");

                return DeliveryResult.Failure(message.Id, deliveryId, ex.Message, ex);
            }
        }

        /// <inheritdoc />
        public async Task<ReliableDeliveryResult> SendReliableAsync<TMessage>(TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : IReliableMessage
        {
            using var scope = _profiler.BeginScope(_profileTag);

            if (message == null) throw new ArgumentNullException(nameof(message));

            EnsureServiceRunning();

            // Ensure the message has proper configuration
            if (message.MaxDeliveryAttempts <= 0)
            {
                message.MaxDeliveryAttempts = _configuration.DefaultMaxDeliveryAttempts;
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var delivery = new PendingDelivery(message, message.DeliveryId, message.MaxDeliveryAttempts, true, tcs);

            var key = (message.Id, message.DeliveryId);
            _pendingDeliveries[key] = delivery;

            try
            {
                // Send the initial message
                message.DeliveryAttempts = 1;
                message.ScheduleNextAttempt();
                delivery.IncrementAttempts();
                delivery.UpdateStatus(MessageDeliveryStatus.Sending);

                _messageBus.Publish(message);
                _statistics.RecordMessageSent();

                delivery.UpdateStatus(MessageDeliveryStatus.Sent);

                _logger.Log(LogLevel.Debug,
                    $"Sent reliable message of type {typeof(TMessage).Name} with ID {message.Id} and delivery ID {message.DeliveryId} (attempt 1/{message.MaxDeliveryAttempts})",
                    "DeliveryService");

                // Return the task - it will be completed when the message is acknowledged or expires
                var result = await tcs.Task;
                return (ReliableDeliveryResult)result;
            }
            catch (Exception ex)
            {
                _pendingDeliveries.TryRemove(key, out _);
                _statistics.RecordMessageFailed();

                MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                    message,
                    message.DeliveryId,
                    ex.Message,
                    ex,
                    1,
                    false));

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
        public async Task<BatchDeliveryResult> SendBatchAsync(
            IEnumerable<IMessage> messages,
            BatchDeliveryOptions options,
            CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(_profileTag);

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
                using var combinedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, batchCts.Token);

                var tasks = new List<Task<DeliveryResult>>();

                foreach (var message in messageList)
                {
                    if (combinedCts.Token.IsCancellationRequested)
                        break;

                    var task = ProcessSingleMessageAsync(message, options, semaphore, combinedCts.Token);
                    tasks.Add(task);

                    if (options.StopOnFirstError)
                    {
                        var result = await task;
                        results.Add(result);
                        if (!result.IsSuccess)
                            break;
                    }
                }

                if (!options.StopOnFirstError)
                {
                    var completedTasks = await Task.WhenAll(tasks);
                    results.AddRange(completedTasks);
                }

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
        public async Task AcknowledgeMessageAsync(Guid messageId, Guid deliveryId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _profiler.BeginScope(_profileTag);

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
            _acknowledgmentSubscription?.Dispose();

            // Cancel any remaining pending deliveries
            foreach (var delivery in _pendingDeliveries.Values)
            {
                delivery.Cancel();
            }

            _pendingDeliveries.Clear();
            _isDisposed = true;

            _logger.Log(LogLevel.Info, "ReliableMessageDeliveryService disposed", "DeliveryService");
        }

        private async Task<DeliveryResult> ProcessSingleMessageAsync(
            IMessage message,
            BatchDeliveryOptions options,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using var messageCts = new CancellationTokenSource(options.MessageTimeout);
                using var messageTokens =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, messageCts.Token);

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
        }

        private void OnMessageAcknowledgedReceived(MessageAcknowledged ack)
        {
            using var scope = _profiler.BeginScope(_profileTag);

            var key = (ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId);
            if (_pendingDeliveries.TryRemove(key, out var delivery))
            {
                var stopwatch = Stopwatch.StartNew();

                var result = delivery.IsReliable
                    ? (object)ReliableDeliveryResult.Success(
                        ack.AcknowledgedMessageId,
                        ack.AcknowledgedDeliveryId,
                        delivery.DeliveryAttempts,
                        ack.AcknowledgmentTime)
                    : DeliveryResult.Success(
                        ack.AcknowledgedMessageId,
                        ack.AcknowledgedDeliveryId,
                        ack.AcknowledgmentTime);

                delivery.Complete(result);

                stopwatch.Stop();
                var deliveryTime = (long)(ack.AcknowledgmentTime - delivery.FirstAttemptTime).TotalMilliseconds;

                _statistics.RecordMessageDelivered();
                _statistics.RecordMessageAcknowledged();
                _statistics.RecordDeliveryTime(deliveryTime);

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
            }, _cancellationTokenSource?.Token ?? CancellationToken.None);
        }

        private async Task ProcessPendingDeliveries()
        {
            using var scope = _profiler.BeginScope(_profileTag);

            if (!await _deliveryLock.WaitAsync(100))
            {
                return; // Skip this cycle if we can't get the lock quickly
            }

            try
            {
                var now = DateTime.UtcNow;
                var nowTicks = now.Ticks;
                var deliveriesToProcess = new List<(PendingDelivery delivery, (Guid, Guid) key)>();

                // First pass: identify deliveries to process
                foreach (var kvp in _pendingDeliveries)
                {
                    var key = kvp.Key;
                    var delivery = kvp.Value;

                    if (ShouldProcessDelivery(delivery, nowTicks))
                    {
                        deliveriesToProcess.Add((delivery, key));
                    }
                }

                // Second pass: process identified deliveries
                foreach (var (delivery, key) in deliveriesToProcess)
                {
                    await ProcessSingleDelivery(delivery, key, now);
                }
            }
            finally
            {
                _deliveryLock.Release();
            }
        }

        private bool ShouldProcessDelivery(PendingDelivery delivery, long nowTicks)
        {
            if (delivery.Status == MessageDeliveryStatus.Cancelled)
                return true;

            if (!delivery.IsReliable)
                return false;

            if (!(delivery.Message is IReliableMessage reliableMessage))
                return false;

            // Check if max attempts reached
            if (delivery.DeliveryAttempts >= delivery.MaxDeliveryAttempts)
                return true;

            // Check if it's time to retry
            return reliableMessage.NextAttemptTicks <= nowTicks;
        }

        private async Task ProcessSingleDelivery(PendingDelivery delivery, (Guid, Guid) key, DateTime now)
        {
            if (delivery.Status == MessageDeliveryStatus.Cancelled)
            {
                _pendingDeliveries.TryRemove(key, out _);
                return;
            }

            if (!(delivery.Message is IReliableMessage reliableMessage))
                return;

            if (delivery.DeliveryAttempts >= delivery.MaxDeliveryAttempts)
            {
                // Max attempts reached
                ExpireDelivery(delivery, key, reliableMessage);
                return;
            }

            if (reliableMessage.NextAttemptTicks <= now.Ticks)
            {
                // Time to retry
                await RetryDelivery(delivery, reliableMessage);
            }
        }

        private void ExpireDelivery(PendingDelivery delivery, (Guid, Guid) key, IReliableMessage reliableMessage)
        {
            delivery.Expire(ReliableDeliveryResult.Failure(
                reliableMessage.Id,
                reliableMessage.DeliveryId,
                delivery.DeliveryAttempts,
                MessageDeliveryStatus.Expired,
                $"Maximum delivery attempts ({delivery.MaxDeliveryAttempts}) reached"));

            _pendingDeliveries.TryRemove(key, out _);
            _statistics.RecordMessageFailed();

            MessageDeliveryFailed?.Invoke(this, new MessageDeliveryFailedEventArgs(
                reliableMessage,
                reliableMessage.DeliveryId,
                "Maximum delivery attempts reached",
                null,
                delivery.DeliveryAttempts,
                false));

            _logger.Log(LogLevel.Warning,
                $"Message {reliableMessage.Id} with delivery ID {reliableMessage.DeliveryId} expired after {delivery.DeliveryAttempts} attempts",
                "DeliveryService");
        }

        private async Task RetryDelivery(PendingDelivery delivery, IReliableMessage reliableMessage)
        {
            try
            {
                delivery.IncrementAttempts();
                reliableMessage.DeliveryAttempts = delivery.DeliveryAttempts;

                // Calculate next attempt time with exponential backoff
                var delaySeconds = Math.Min(
                    Math.Pow(_configuration.BackoffMultiplier, delivery.DeliveryAttempts - 1),
                    _configuration.MaxRetryDelay.TotalSeconds);

                var nextAttempt = DateTime.UtcNow.AddSeconds(delaySeconds);
                reliableMessage.NextAttemptTicks = nextAttempt.Ticks;
                delivery.UpdateNextAttemptTime(nextAttempt);

                delivery.UpdateStatus(MessageDeliveryStatus.Sending);

                _messageBus.Publish(reliableMessage);
                _statistics.RecordMessageSent();

                delivery.UpdateStatus(MessageDeliveryStatus.Sent);

                _logger.Log(LogLevel.Debug,
                    $"Retrying reliable message {reliableMessage.Id} with delivery ID {reliableMessage.DeliveryId} " +
                    $"(attempt {delivery.DeliveryAttempts}/{delivery.MaxDeliveryAttempts})",
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
                    delivery.DeliveryAttempts,
                    delivery.DeliveryAttempts < delivery.MaxDeliveryAttempts));
            }
        }

        private void EnsureServiceRunning()
        {
            lock (_statusLock)
            {
                if (_status != DeliveryServiceStatus.Running)
                {
                    throw new InvalidOperationException($"Service is not running. Current status: {_status}");
                }
            }
        }

        private void ChangeStatus(DeliveryServiceStatus newStatus, string reason = null)
        {
            DeliveryServiceStatus previousStatus;

            lock (_statusLock)
            {
                previousStatus = _status;
                _status = newStatus;
            }

            StatusChanged?.Invoke(this, new DeliveryServiceStatusChangedEventArgs(
                previousStatus,
                newStatus,
                DateTime.UtcNow,
                reason));
        }

        private async Task DisposeTimerAsync()
        {
            if (_deliveryTimer != null)
            {
                await _deliveryTimer.DisposeAsync();
                _deliveryTimer = null;
            }
        }

        private async Task WaitForPendingDeliveries(CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();

            while (_pendingDeliveries.Count > 0 && stopwatch.Elapsed < timeout)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(100, cancellationToken);
            }

            if (_pendingDeliveries.Count > 0)
            {
                _logger.Log(LogLevel.Warning,
                    $"Service stopped with {_pendingDeliveries.Count} pending deliveries",
                    "DeliveryService");
            }
        }
    }
}