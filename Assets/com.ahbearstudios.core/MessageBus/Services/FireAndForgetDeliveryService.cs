using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Data;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Services
{
    /// <summary>
    /// Simple fire-and-forget message delivery service with no reliability guarantees.
    /// </summary>
    public sealed class FireAndForgetDeliveryService : IMessageDeliveryService
    {
        private readonly IMessageBusService _messageBusService;
        private readonly ILoggingService _logger;
        private readonly IProfiler _profiler;
        private readonly DeliveryStatistics _statistics;
        
        private DeliveryServiceStatus _status = DeliveryServiceStatus.Stopped;
        private bool _isDisposed = false;
        
        /// <inheritdoc />
        public string Name => "FireAndForget";
        
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
        /// Initializes a new instance of the FireAndForgetDeliveryService class.
        /// </summary>
        /// <param name="messageBusService">The message bus to use for sending messages.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public FireAndForgetDeliveryService(IMessageBusService messageBusService, ILoggingService logger, IProfiler profiler)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _statistics = new DeliveryStatistics();
            
            _logger.Log(LogLevel.Info, "FireAndForgetDeliveryService initialized", "DeliveryService");
        }
        
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ChangeStatus(DeliveryServiceStatus.Running, "Service started");
            _logger.Log(LogLevel.Info, "FireAndForgetDeliveryService started", "DeliveryService");
        }
        
        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            ChangeStatus(DeliveryServiceStatus.Stopped, "Service stopped");
            _logger.Log(LogLevel.Info, "FireAndForgetDeliveryService stopped", "DeliveryService");
        }
        
        /// <inheritdoc />
        public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            using var scope = _profiler.BeginScope(new ProfilerTag(new ProfilerCategory("DeliveryService"), "SendAsync"));
            
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            EnsureServiceRunning();
            
            try
            {
                _messageBusService.Publish(message);
                _statistics.RecordMessageSent();
                _statistics.RecordMessageDelivered(); // Assume success for fire-and-forget
                
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
                    $"Failed to send fire-and-forget message: {ex.Message}",
                    "DeliveryService");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<DeliveryResult> SendWithConfirmationAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
        {
            // Fire-and-forget service doesn't support confirmations
            throw new NotSupportedException("Fire-and-forget delivery service does not support confirmations");
        }
        
        /// <inheritdoc />
        public async Task<ReliableDeliveryResult> SendReliableAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IReliableMessage
        {
            // Fire-and-forget service doesn't support reliable delivery
            throw new NotSupportedException("Fire-and-forget delivery service does not support reliable delivery");
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
            
            var completionTime = DateTime.UtcNow;
            var duration = completionTime - startTime;
            
            return new BatchDeliveryResult(results, completionTime, duration);
        }
        
        /// <inheritdoc />
        public async Task AcknowledgeMessageAsync(Guid messageId, Guid deliveryId, CancellationToken cancellationToken = default)
        {
            // Fire-and-forget service doesn't use acknowledgments
            _logger.Log(LogLevel.Debug, 
                "Acknowledgment ignored by fire-and-forget service",
                "DeliveryService");
        }
        
        /// <inheritdoc />
        public MessageDeliveryStatus? GetMessageStatus(Guid messageId, Guid deliveryId)
        {
            // Fire-and-forget service doesn't track message status
            return null;
        }
        
        /// <inheritdoc />
        public IReadOnlyCollection<IPendingDelivery> GetPendingDeliveries()
        {
            // Fire-and-forget service has no pending deliveries
            return new List<IPendingDelivery>();
        }
        
        /// <inheritdoc />
        public IDeliveryStatistics GetStatistics()
        {
            return _statistics;
        }
        
        /// <inheritdoc />
        public bool CancelDelivery(Guid messageId, Guid deliveryId)
        {
            // Fire-and-forget service can't cancel deliveries
            return false;
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
            
            _status = DeliveryServiceStatus.Stopped;
            _isDisposed = true;
            
            _logger.Log(LogLevel.Info, "FireAndForgetDeliveryService disposed", "DeliveryService");
        }
    }
}