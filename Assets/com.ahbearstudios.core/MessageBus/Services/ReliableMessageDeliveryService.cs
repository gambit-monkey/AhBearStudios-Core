using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Messages;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Reliable message delivery service that provides guaranteed message delivery with retry logic,
    /// publishing all delivery and status notifications as bus messages instead of .NET events.
    /// </summary>
    public sealed class ReliableMessageDeliveryService : IMessageDeliveryService
    {
        private readonly IMessageBusService _bus;
        private readonly ILoggingService    _logger;
        private readonly IProfilerService          _profilerService;
        private readonly DeliveryServiceConfiguration _config;

        private readonly ConcurrentDictionary<(Guid,Guid), PendingDelivery> _pending;
        private readonly SemaphoreSlim     _lock;
        private readonly DeliveryStatistics _stats;
        private readonly ProfilerTag       _tag;
        private readonly object            _statusLock = new object();

        private Timer                      _timer;
        private CancellationTokenSource    _cts;
        private IDisposable                _ackSub;
        private bool                       _disposed;
        private DeliveryServiceStatus      _status = DeliveryServiceStatus.Stopped;

        public string Name => "ReliableMessageDelivery";
        public bool   IsActive
        {
            get { lock(_statusLock) { return _status == DeliveryServiceStatus.Running; } }
        }
        public DeliveryServiceStatus Status
        {
            get { lock(_statusLock) { return _status; } }
        }

        public ReliableMessageDeliveryService(
            IMessageBusService bus,
            ILoggingService    logger,
            IProfilerService          profilerService,
            DeliveryServiceConfiguration config = null)
        {
            _bus     = bus     ?? throw new ArgumentNullException(nameof(bus));
            _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
            _profilerService= profilerService?? throw new ArgumentNullException(nameof(profilerService));
            _config  = config  ?? new DeliveryServiceConfiguration();

            _pending = new ConcurrentDictionary<(Guid,Guid), PendingDelivery>();
            _lock    = new SemaphoreSlim(1,1);
            _stats   = new DeliveryStatistics();
            _tag     = new ProfilerTag(new ProfilerCategory("DeliveryService"), Name);

            _logger.LogInfo("ReliableMessageDeliveryService initialized");
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            using var scope = _profilerService.BeginScope(_tag);

            lock(_statusLock)
            {
                if (_status != DeliveryServiceStatus.Stopped)
                    return;
                _status = DeliveryServiceStatus.Starting;
            }

            PublishStatusChanged(DeliveryServiceStatus.Starting, "Startup initiated");

            _cts = new CancellationTokenSource();
            _ackSub = _bus.Subscribe<MessageAcknowledged>(OnAckReceived);
            _timer  = new Timer(_ => _ = Task.Run(ProcessPendingDeliveries), 
                                null,
                                _config.ProcessingInterval,
                                _config.ProcessingInterval);

            lock(_statusLock) { _status = DeliveryServiceStatus.Running; }
            PublishStatusChanged(DeliveryServiceStatus.Running, "Service running");
            _logger.LogInfo("ReliableMessageDeliveryService started");
        }

        public async Task StopAsync(CancellationToken token = default)
        {
            using var scope = _profilerService.BeginScope(_tag);

            lock(_statusLock)
            {
                if (_status == DeliveryServiceStatus.Stopped)
                    return;
                _status = DeliveryServiceStatus.Stopping;
            }
            PublishStatusChanged(DeliveryServiceStatus.Stopping, "Shutdown initiated");

            _cts.Cancel();
            await _timer.DisposeAsync();
            _timer = null;
            _ackSub.Dispose();
            _ackSub = null;

            // wait up to 30s for all pending
            var sw = Stopwatch.StartNew();
            while (_pending.Count > 0 && sw.Elapsed < TimeSpan.FromSeconds(30))
                await Task.Delay(100, token);

            lock(_statusLock) { _status = DeliveryServiceStatus.Stopped; }
            PublishStatusChanged(DeliveryServiceStatus.Stopped, "Service stopped");
            _logger.LogInfo("ReliableMessageDeliveryService stopped");
        }

        public async Task SendAsync<T>(T message, CancellationToken token = default)
            where T:IMessage
        {
            using var scope = _profilerService.BeginScope(_tag);
            if (message==null) throw new ArgumentNullException(nameof(message));
            EnsureRunning();

            var sw = Stopwatch.StartNew();
            try
            {
                _bus.Publish(message);
                _stats.RecordMessageSent();
                _stats.RecordMessageDelivered(); // fire-and-forget

                sw.Stop();
                _stats.RecordDeliveryTime(sw.ElapsedMilliseconds);

                // publish as a bus message
                _bus.Publish(new DeliverySucceeded<T>
                {
                    Message = message,
                    DeliveryId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Attempts = 1
                });
                _logger.LogDebug($"Fire-and-forget sent: {typeof(T).Name}");
            }
            catch(Exception ex)
            {
                _stats.RecordMessageFailed();
                _bus.Publish(new DeliveryFailed<T>
                {
                    Message = message,
                    DeliveryId = Guid.NewGuid(),
                    Error = ex.Message,
                    Exception = ex,
                    Attempts = 1,
                    WillRetry = false
                });
                _logger.LogError($"Fire-and-forget failed: {ex.Message}");
                throw;
            }
        }

        public async Task<DeliveryResult> SendWithConfirmationAsync<T>(T message, CancellationToken token = default)
            where T:IMessage
        {
            using var scope = _profilerService.BeginScope(_tag);
            if (message==null) throw new ArgumentNullException(nameof(message));
            EnsureRunning();

            var deliveryId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pd  = new PendingDelivery(message, deliveryId, 1, false, tcs);
            _pending[(message.Id,deliveryId)] = pd;

            try
            {
                pd.UpdateStatus(MessageDeliveryStatus.Sending);
                _bus.Publish(message);
                _stats.RecordMessageSent();
                pd.UpdateStatus(MessageDeliveryStatus.Sent);

                // wait ack or timeout
                var timeout = Task.Delay(_config.DefaultTimeout, token);
                var done    = await Task.WhenAny(tcs.Task, timeout);
                if (done==tcs.Task)
                    return (DeliveryResult)await tcs.Task;

                // timeout
                _pending.TryRemove((message.Id,deliveryId), out _);
                _stats.RecordMessageFailed();
                var msg = token.IsCancellationRequested ? "Cancelled" : "Timed out";
                _bus.Publish(new DeliveryFailed<T>
                {
                    Message = message,
                    DeliveryId = deliveryId,
                    Error = msg,
                    Exception = null,
                    Attempts = 1,
                    WillRetry = false
                });
                return DeliveryResult.Failure(message.Id, deliveryId, msg);
            }
            catch(Exception ex)
            {
                _pending.TryRemove((message.Id,deliveryId), out _);
                _stats.RecordMessageFailed();
                _bus.Publish(new DeliveryFailed<T>
                {
                    Message = message,
                    DeliveryId = deliveryId,
                    Error = ex.Message,
                    Exception = ex,
                    Attempts = 1,
                    WillRetry = false
                });
                _logger.LogError($"SendWithConfirmation failed: {ex.Message}");
                return DeliveryResult.Failure(message.Id, deliveryId, ex.Message, ex);
            }
        }

        public async Task<ReliableDeliveryResult> SendReliableAsync<T>(T message, CancellationToken token = default)
            where T:IReliableMessage
        {
            using var scope = _profilerService.BeginScope(_tag);
            if (message==null) throw new ArgumentNullException(nameof(message));
            EnsureRunning();

            if (message.MaxDeliveryAttempts<=0)
                message.MaxDeliveryAttempts = _config.DefaultMaxDeliveryAttempts;

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pd  = new PendingDelivery(message, message.DeliveryId, message.MaxDeliveryAttempts, true, tcs);
            _pending[(message.Id, message.DeliveryId)] = pd;

            try
            {
                // initial send
                message.DeliveryAttempts = 1;
                message.ScheduleNextAttempt();
                pd.IncrementAttempts();
                pd.UpdateStatus(MessageDeliveryStatus.Sending);

                _bus.Publish(message);
                _stats.RecordMessageSent();
                pd.UpdateStatus(MessageDeliveryStatus.Sent);

                _logger.LogDebug($"Reliable send attempt 1/{message.MaxDeliveryAttempts}");

                // wait for ack or expiry
                var result = (ReliableDeliveryResult)await tcs.Task;
                return result;
            }
            catch(Exception ex)
            {
                _pending.TryRemove((message.Id, message.DeliveryId), out _);
                _stats.RecordMessageFailed();
                _bus.Publish(new DeliveryFailed<T>
                {
                    Message = message,
                    DeliveryId = message.DeliveryId,
                    Error = ex.Message,
                    Exception = ex,
                    Attempts = message.DeliveryAttempts,
                    WillRetry = false
                });
                _logger.LogError($"Reliable send failed: {ex.Message}");
                return ReliableDeliveryResult.Failure(
                    message.Id,
                    message.DeliveryId,
                    message.DeliveryAttempts,
                    MessageDeliveryStatus.Failed,
                    ex.Message, ex);
            }
        }

        public async Task<BatchDeliveryResult> SendBatchAsync(
            IEnumerable<IMessage> messages,
            BatchDeliveryOptions options,
            CancellationToken token = default)
        {
            using var scope = _profilerService.BeginScope(_tag);
            if (messages==null) throw new ArgumentNullException(nameof(messages));
            if (options==null) throw new ArgumentNullException(nameof(options));
            EnsureRunning();

            var list = messages.ToList();
            if (list.Count==0)
                return new BatchDeliveryResult(new List<DeliveryResult>(), DateTime.UtcNow, TimeSpan.Zero);

            _logger.LogInfo($"Batch send {list.Count} messages");
            var sw = Stopwatch.StartNew();
            var results = new List<DeliveryResult>();

            try
            {
                using var sem = new SemaphoreSlim(options.MaxConcurrency);
                using var batchCts = new CancellationTokenSource(options.BatchTimeout);
                using var linked  = CancellationTokenSource.CreateLinkedTokenSource(token, batchCts.Token);

                var tasks = list
                    .Select(msg => ProcessSingleAsync(msg, options, sem, linked.Token))
                    .ToList();

                if (options.StopOnFirstError)
                {
                    foreach(var t in tasks)
                    {
                        var r = await t;
                        results.Add(r);
                        if (!r.IsSuccess) break;
                    }
                }
                else
                {
                    var all = await Task.WhenAll(tasks);
                    results.AddRange(all);
                }

                sw.Stop();
                _logger.LogInfo($"Batch completed in {sw.Elapsed.TotalSeconds:F2}s");
                return new BatchDeliveryResult(results, DateTime.UtcNow, sw.Elapsed);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Batch failed: {ex.Message}");
                // fill failures
                while(results.Count<list.Count)
                    results.Add(DeliveryResult.Failure(list[results.Count].Id, Guid.NewGuid(), ex.Message, ex));
                return new BatchDeliveryResult(results, DateTime.UtcNow, sw.Elapsed);
            }
        }

        public async Task AcknowledgeMessageAsync(Guid messageId, Guid deliveryId, CancellationToken token = default)
        {
            using var scope = _profilerService.BeginScope(_tag);
            _bus.Publish(new MessageAcknowledged
            {
                AcknowledgedMessageId = messageId,
                AcknowledgedDeliveryId = deliveryId,
                AcknowledgmentTime    = DateTime.UtcNow
            });
            _logger.LogDebug($"Ack sent for {messageId}/{deliveryId}");
        }

        public MessageDeliveryStatus? GetMessageStatus(Guid messageId, Guid deliveryId)
        {
            return _pending.TryGetValue((messageId,deliveryId), out var pd)
                ? pd.Status
                : (MessageDeliveryStatus?)null;
        }

        public IReadOnlyCollection<IPendingDelivery> GetPendingDeliveries()
        {
            _stats.UpdatePendingCount(_pending.Count);
            return _pending.Values.ToList();
        }

        public IDeliveryStatistics GetStatistics() => _stats;

        public bool CancelDelivery(Guid messageId, Guid deliveryId)
        {
            if (_pending.TryRemove((messageId,deliveryId), out var pd))
            {
                pd.Cancel();
                _logger.LogDebug($"Cancelled {messageId}/{deliveryId}");
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            try { StopAsync().Wait(TimeSpan.FromSeconds(10)); } catch{}
            _timer?.Dispose();
            _lock.Dispose();
            _cts?.Dispose();
            _ackSub?.Dispose();
            foreach(var pd in _pending.Values) pd.Cancel();
            _pending.Clear();
            _disposed = true;
            _logger.LogInfo("ReliableMessageDeliveryService disposed");
        }

        // ——————————————————————————————————————————————————————————————————
        private void EnsureRunning()
        {
            if (!IsActive)
                throw new InvalidOperationException($"Service not running (status={Status})");
        }

        private async Task<DeliveryResult> ProcessSingleAsync(
            IMessage message,
            BatchDeliveryOptions opts,
            SemaphoreSlim sem,
            CancellationToken token)
        {
            await sem.WaitAsync(token);
            try
            {
                if (opts.RequireConfirmation)
                    return await SendWithConfirmationAsync(message, token);
                else
                {
                    await SendAsync(message, token);
                    return DeliveryResult.Success(message.Id, Guid.NewGuid(), DateTime.UtcNow);
                }
            }
            catch(Exception ex)
            {
                return DeliveryResult.Failure(message.Id, Guid.NewGuid(), ex.Message, ex);
            }
            finally { sem.Release(); }
        }

        private async Task ProcessPendingDeliveries()
        {
            using var scope = _profilerService.BeginScope(_tag);
            if (!await _lock.WaitAsync(100)) return;
            try
            {
                var now = DateTime.UtcNow.Ticks;
                var toProcess = new List<(PendingDelivery pd,(Guid,Guid) key)>();

                foreach(var kvp in _pending)
                {
                    var pd = kvp.Value;
                    if (pd.Status==MessageDeliveryStatus.Cancelled ||
                        (pd.IsReliable && pd.DeliveryAttempts<pd.MaxDeliveryAttempts && pd.Message is IReliableMessage rm
                         && rm.NextAttemptTicks<=now))
                    {
                        toProcess.Add((pd, kvp.Key));
                    }
                }

                foreach(var (pd,key) in toProcess)
                    await HandleSingle(pd,key);
            }
            finally { _lock.Release(); }
        }

        private async Task HandleSingle(PendingDelivery pd, (Guid,Guid) key)
        {
            if (pd.Status==MessageDeliveryStatus.Cancelled)
            {
                _pending.TryRemove(key, out _);
                return;
            }

            if (!(pd.Message is IReliableMessage rm)) return;

            if (pd.DeliveryAttempts>=pd.MaxDeliveryAttempts)
            {
                // expired
                pd.Expire(ReliableDeliveryResult.Failure(
                    rm.Id, rm.DeliveryId, pd.DeliveryAttempts, MessageDeliveryStatus.Expired,
                    $"Max attempts {pd.MaxDeliveryAttempts} reached"));
                _pending.TryRemove(key, out _);
                _stats.RecordMessageFailed();

                _bus.Publish(new DeliveryFailed<IReliableMessage>
                {
                    Message = rm,
                    DeliveryId = rm.DeliveryId,
                    Error = "Expired",
                    Exception = null,
                    Attempts = pd.DeliveryAttempts,
                    WillRetry = false
                });
                _logger.LogWarning($"Expired {rm.Id}/{rm.DeliveryId}");
                return;
            }

            // retry
            pd.IncrementAttempts();
            rm.DeliveryAttempts = pd.DeliveryAttempts;
            var delay = Math.Min(
                Math.Pow(_config.BackoffMultiplier, pd.DeliveryAttempts-1),
                _config.MaxRetryDelay.TotalSeconds);
            var next   = DateTime.UtcNow.AddSeconds(delay);
            rm.NextAttemptTicks = next.Ticks;
            pd.UpdateNextAttemptTime(next);
            pd.UpdateStatus(MessageDeliveryStatus.Sending);

            try
            {
                _bus.Publish(rm);
                _stats.RecordMessageSent();
                pd.UpdateStatus(MessageDeliveryStatus.Sent);
                _logger.LogDebug($"Retrying {rm.Id}/{rm.DeliveryId} attempt {pd.DeliveryAttempts}/{pd.MaxDeliveryAttempts}");
            }
            catch(Exception ex)
            {
                _bus.Publish(new DeliveryFailed<IReliableMessage>
                {
                    Message = rm,
                    DeliveryId = rm.DeliveryId,
                    Error = ex.Message,
                    Exception = ex,
                    Attempts = pd.DeliveryAttempts,
                    WillRetry = pd.DeliveryAttempts < pd.MaxDeliveryAttempts
                });
                _logger.LogError($"Retry failed: {ex.Message}");
            }
        }

        private void OnAckReceived(in MessageAcknowledged ack)
        {
            using var scope = _profilerService.BeginScope(_tag);
            var key = (ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId);
            if (!_pending.TryRemove(key, out var pd))
                return;

            var sw = Stopwatch.StartNew();
            object result;
            if (pd.IsReliable)
            {
                result = ReliableDeliveryResult.Success(
                    ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId, pd.DeliveryAttempts, ack.AcknowledgmentTime);
            }
            else
            {
                result = DeliveryResult.Success(
                    ack.AcknowledgedMessageId, ack.AcknowledgedDeliveryId, ack.AcknowledgmentTime);
            }
            pd.Complete(result);

            sw.Stop();
            _stats.RecordMessageDelivered();
            _stats.RecordMessageAcknowledged();
            _stats.RecordDeliveryTime(sw.ElapsedMilliseconds);

            // publish both ack and delivered notifications
            _bus.Publish(ack);  // struct MessageAcknowledged :contentReference[oaicite:0]{index=0}
            _bus.Publish(new DeliverySucceeded<IMessage>
            {
                Message = pd.Message,
                DeliveryId = pd.DeliveryId,
                Timestamp  = ack.AcknowledgmentTime,
                Attempts   = pd.DeliveryAttempts
            });
            _logger.LogDebug($"Acknowledged {ack.AcknowledgedMessageId}/{ack.AcknowledgedDeliveryId}");
        }

        private void PublishStatusChanged(DeliveryServiceStatus newStatus, string reason)
        {
            var prev = Status;
            _bus.Publish(new DeliveryServiceStatusChanged
            {
                PreviousStatus = prev,
                CurrentStatus  = newStatus,
                Timestamp      = DateTime.UtcNow,
                Reason         = reason
            });
        }
    }
}