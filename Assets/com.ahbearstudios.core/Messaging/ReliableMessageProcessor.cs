using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configuration;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Background processor for handling pending reliable messages.
    /// Ensures messages are redelivered until acknowledged.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to process.</typeparam>
    public class ReliableMessageProcessor<TMessage> : IDisposable where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _messageBus;
        private readonly IMessageStore<TMessage> _messageStore;
        private readonly IMessageSerializer<TMessage> _serializer;
        private readonly ReliableMessageOptions _options;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Dictionary<Guid, int> _retryAttempts;
        private readonly object _stateLock = new object();
        private Task _processingTask;
        private CancellationTokenSource _internalCancellationSource;
        private bool _isProcessing;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the ReliableMessageProcessor class.
        /// </summary>
        /// <param name="messageBus">The message bus for delivering messages.</param>
        /// <param name="messageStore">The message store for retrieving pending messages.</param>
        /// <param name="serializer">The serializer for deserializing messages.</param>
        /// <param name="options">Options for configuring processor behavior.</param>
        /// <param name="logger">Optional logger for processor operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public ReliableMessageProcessor(
            IMessageBus<TMessage> messageBus,
            IMessageStore<TMessage> messageStore,
            IMessageSerializer<TMessage> serializer,
            ReliableMessageOptions options,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _profiler = profiler;
            _retryAttempts = new Dictionary<Guid, int>();
            _isProcessing = false;
            _isDisposed = false;
            
            if (_logger != null)
            {
                _logger.Info("ReliableMessageProcessor initialized");
            }
        }

        /// <summary>
        /// Starts the message processor.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to stop processing.</param>
        public void Start(CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("ReliableMessageProcessor.Start"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageProcessor<TMessage>));
                }

                lock (_stateLock)
                {
                    if (_isProcessing)
                    {
                        return;
                    }

                    // Create a linked cancellation token source
                    _internalCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    
                    // Start the processing task
                    _processingTask = Task.Run(() => ProcessMessagesAsync(_internalCancellationSource.Token), _internalCancellationSource.Token);
                    _isProcessing = true;
                    
                    if (_logger != null)
                    {
                        _logger.Info("ReliableMessageProcessor started");
                    }
                }
            }
        }

        /// <summary>
        /// Stops the message processor.
        /// </summary>
        public void Stop()
        {
            using (_profiler?.BeginSample("ReliableMessageProcessor.Stop"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageProcessor<TMessage>));
                }

                lock (_stateLock)
                {
                    if (!_isProcessing)
                    {
                        return;
                    }

                    // Cancel the processing task
                    if (_internalCancellationSource != null && !_internalCancellationSource.IsCancellationRequested)
                    {
                        _internalCancellationSource.Cancel();
                    }
                    
                    // Wait for the task to complete
                    try
                    {
                        _processingTask?.Wait(5000); // Wait up to 5 seconds
                    }
                    catch (AggregateException)
                    {
                        // Task was canceled, which is expected
                    }
                    
                    _isProcessing = false;
                    
                    if (_logger != null)
                    {
                        _logger.Info("ReliableMessageProcessor stopped");
                    }
                }
            }
        }

        /// <summary>
        /// Main processing loop that checks for pending messages and redelivers them.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to stop processing.</param>
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            if (_logger != null)
            {
                _logger.Debug("ReliableMessageProcessor starting processing loop");
            }
            
            while (!cancellationToken.IsCancellationRequested)
            {
                using (_profiler?.BeginSample("ReliableMessageProcessor.ProcessMessagesAsync"))
                {
                    try
                    {
                        // Get all pending message IDs
                        List<Guid> pendingMessageIds = _messageStore.GetMessageIds();
                        
                        if (pendingMessageIds.Count > 0)
                        {
                            if (_logger != null)
                            {
                                _logger.Debug($"Processing {pendingMessageIds.Count} pending messages");
                            }
                            
                            // Process each pending message
                            foreach (Guid messageId in pendingMessageIds)
                            {
                                // Check cancellation between messages
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                
                                await ProcessMessageAsync(messageId, cancellationToken);
                            }
                            
                            // Clean up old messages
                            if (_options.MaxMessageAgeMinutes > 0)
                            {
                                await CleanupOldMessagesAsync(cancellationToken);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal cancellation
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error($"Error in message processing loop: {ex.Message}");
                        }
                    }
                    
                    // Wait for the next processing interval
                    try
                    {
                        await Task.Delay(_options.ProcessingIntervalMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal cancellation
                        break;
                    }
                }
            }
            
            if (_logger != null)
            {
                _logger.Debug("ReliableMessageProcessor processing loop terminated");
            }
        }

        /// <summary>
        /// Processes a single pending message.
        /// </summary>
        /// <param name="messageId">The ID of the message to process.</param>
        /// <param name="cancellationToken">A cancellation token to stop processing.</param>
        private async Task ProcessMessageAsync(Guid messageId, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("ReliableMessageProcessor.ProcessMessageAsync"))
            {
                try
                {
                    // Load the message
                    TMessage message = await _messageStore.GetMessageAsync(messageId, cancellationToken);
                    if (message == null)
                    {
                        // Message no longer exists in the store
                        _retryAttempts.Remove(messageId);
                        return;
                    }
                    
                    // Check if we've exceeded the maximum retry attempts
                    if (_retryAttempts.TryGetValue(messageId, out int attempts) && attempts >= _options.MaxRetryAttempts)
                    {
                        if (_logger != null)
                        {
                            _logger.Warning($"Message {messageId} exceeded maximum retry attempts, abandoning");
                        }
                        
                        // Remove it from the store if it's been retried too many times
                        await _messageStore.RemoveMessageAsync(messageId, cancellationToken);
                        _retryAttempts.Remove(messageId);
                        return;
                    }
                    
                    // Calculate the next retry delay
                    int retryDelay = _options.RetryDelayBaseMs * (int)Math.Pow(2, attempts); // Exponential backoff
                    DateTime messageTime = new DateTime(message.Timestamp);
                    TimeSpan age = DateTime.UtcNow - messageTime;
                    
                    // Only redeliver if the message has been waiting long enough
                    if (age.TotalMilliseconds >= retryDelay)
                    {
                        // Try to redeliver the message
                        try
                        {
                            await _messageBus.PublishAsync(message, cancellationToken);
                            
                            // If we get here, the message was successfully published
                            if (_options.RemoveOnSuccessfulDelivery)
                            {
                                await _messageStore.RemoveMessageAsync(messageId, cancellationToken);
                                _retryAttempts.Remove(messageId);
                                
                                if (_logger != null)
                                {
                                    _logger.Debug($"Successfully redelivered message {messageId}");
                                }
                            }
                            else
                            {
                                // Increment retry attempts
                                _retryAttempts[messageId] = attempts + 1;
                                
                                if (_logger != null)
                                {
                                    _logger.Debug($"Redelivered message {messageId}, attempt {attempts + 1}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Increment retry attempts
                            _retryAttempts[messageId] = attempts + 1;
                            
                            if (_logger != null)
                            {
                                _logger.Error($"Error redelivering message {messageId}, attempt {attempts + 1}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error processing message {messageId}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up messages that are older than the maximum age.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to stop processing.</param>
        private async Task CleanupOldMessagesAsync(CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("ReliableMessageProcessor.CleanupOldMessagesAsync"))
            {
                try
                {
                    // Define the cutoff time
                    DateTime cutoffTime = DateTime.UtcNow.AddMinutes(-_options.MaxMessageAgeMinutes);
                    long cutoffTicks = cutoffTime.Ticks;
                    
                    // Get all messages
                    List<TMessage> messages = await _messageStore.GetAllMessagesAsync(cancellationToken);
                    int removedCount = 0;
                    
                    foreach (TMessage message in messages)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        if (message.Timestamp < cutoffTicks)
                        {
                            await _messageStore.RemoveMessageAsync(message.Id, cancellationToken);
                            _retryAttempts.Remove(message.Id);
                            removedCount++;
                        }
                    }
                    
                    if (removedCount > 0 && _logger != null)
                    {
                        _logger.Info($"Removed {removedCount} expired messages");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation
                    throw;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error cleaning up old messages: {ex.Message}");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("ReliableMessageProcessor.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the reliable message processor.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // Stop the processor if it's running
                Stop();
                
                // Dispose the cancellation token source
                _internalCancellationSource?.Dispose();
                _internalCancellationSource = null;
                
                if (_logger != null)
                {
                    _logger.Info("ReliableMessageProcessor disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~ReliableMessageProcessor()
        {
            Dispose(false);
        }
    }
}