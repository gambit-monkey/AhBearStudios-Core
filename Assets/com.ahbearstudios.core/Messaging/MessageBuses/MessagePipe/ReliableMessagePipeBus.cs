using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configuration;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Implementation of IReliableMessageBus that uses MessagePipe for delivery
    /// while adding message persistence and guaranteed delivery.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public sealed class ReliableMessagePipeBus<TMessage> : IReliableMessageBus<TMessage>, IDisposable 
        where TMessage : IMessage
    {
        private const int DefaultRedeliveryIntervalMs = 10000; // 10 seconds
        private const int DefaultMaxDeliveryAttempts = 5;
        
        private readonly MessagePipeBus<TMessage> _innerBus;
        private readonly IMessageStore<TMessage> _messageStore;
        private readonly IMessageSerializer<TMessage> _serializer;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Timer _redeliveryTimer;
        private readonly ReliableMessageOptions _options;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _stateLock = new object();
        private bool _isStarted;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the ReliableMessagePipeBus class.
        /// </summary>
        /// <param name="innerBus">The MessagePipeBus to use for delivering messages.</param>
        /// <param name="messageStore">The store for persisting messages.</param>
        /// <param name="serializer">The serializer for messages.</param>
        /// <param name="options">Options for configuring reliability behavior.</param>
        /// <param name="logger">Optional logger for operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public ReliableMessagePipeBus(
            MessagePipeBus<TMessage> innerBus,
            IMessageStore<TMessage> messageStore,
            IMessageSerializer<TMessage> serializer,
            ReliableMessageOptions options = null,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            _innerBus = innerBus ?? throw new ArgumentNullException(nameof(innerBus));
            _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _options = options ?? new ReliableMessageOptions();
            _logger = logger;
            _profiler = profiler;
            
            _cancellationTokenSource = new CancellationTokenSource();
            _redeliveryTimer = new Timer(
                ProcessPendingMessages, 
                null, 
                Timeout.Infinite, 
                Timeout.Infinite);
            
            if (_logger != null)
            {
                _logger.Info($"ReliableMessagePipeBus<{typeof(TMessage).Name}> initialized");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.Subscribe(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeAsync(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeToAll(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeToAllAsync(handler);
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                // Delegate to the inner bus
                _innerBus.Unsubscribe(token);
            }
        }

        /// <inheritdoc/>
        public void Publish(TMessage message)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Store the message first to ensure it can be redelivered if needed
                StoreMessage(message);
                
                try
                {
                    // Then publish to the inner bus for immediate delivery
                    _innerBus.Publish(message);
                    
                    // If we get here, message was delivered successfully
                    if (_options.RemoveOnSuccessfulDelivery)
                    {
                        RemoveMessage(message.Id);
                    }
                    else
                    {
                        // Mark as delivered
                        MarkMessageAsDelivered(message.Id);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Published message {message.Id} reliably");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't rethrow - the message is persisted
                    // and will be retried by the redelivery process
                    if (_logger != null)
                    {
                        _logger.Error($"Error publishing message {message.Id}: {ex.Message}. Will retry later.");
                    }
                    
                    // If configured to throw, rethrow the exception
                    if (_options.RethrowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Store the message first to ensure it can be redelivered if needed
                await StoreMessageAsync(message, cancellationToken);
                
                try
                {
                    // Then publish to the inner bus for immediate delivery
                    await _innerBus.PublishAsync(message, cancellationToken);
                    
                    // If we get here, message was delivered successfully
                    if (_options.RemoveOnSuccessfulDelivery)
                    {
                        await RemoveMessageAsync(message.Id, cancellationToken);
                    }
                    else
                    {
                        // Mark as delivered
                        await MarkMessageAsDeliveredAsync(message.Id, cancellationToken);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Published message {message.Id} reliably asynchronously");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't rethrow - the message is persisted
                    // and will be retried by the redelivery process
                    if (_logger != null)
                    {
                        _logger.Error($"Error publishing message {message.Id} asynchronously: {ex.Message}. Will retry later.");
                    }
                    
                    // If user cancelled, propagate the cancellation
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    
                    // If configured to throw, rethrow the exception
                    if (_options.RethrowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.Start"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                lock (_stateLock)
                {
                    if (_isStarted)
                    {
                        return;
                    }
                    
                    // Start the redelivery timer
                    int redeliveryInterval = _options.RetryDelayBaseMs > 0 
                        ? _options.RetryDelayBaseMs 
                        : DefaultRedeliveryIntervalMs;
                    
                    _redeliveryTimer.Change(redeliveryInterval, redeliveryInterval);
                    _isStarted = true;
                    
                    if (_logger != null)
                    {
                        _logger.Info($"ReliableMessagePipeBus<{typeof(TMessage).Name}> started with redelivery interval {redeliveryInterval}ms");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.Stop"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                lock (_stateLock)
                {
                    if (!_isStarted)
                    {
                        return;
                    }
                    
                    // Stop the redelivery timer
                    _redeliveryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _isStarted = false;
                    
                    if (_logger != null)
                    {
                        _logger.Info($"ReliableMessagePipeBus<{typeof(TMessage).Name}> stopped");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public int GetPendingMessageCount()
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.GetPendingMessageCount"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                try
                {
                    return _messageStore.GetMessageCount();
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error getting pending message count: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void ClearPendingMessages()
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.ClearPendingMessages"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                try
                {
                    _messageStore.ClearMessages();
                    
                    if (_logger != null)
                    {
                        _logger.Info("Cleared all pending messages");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error clearing pending messages: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public List<Guid> GetPendingMessageIds()
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.GetPendingMessageIds"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                try
                {
                    return _messageStore.GetMessageIds();
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error getting pending message IDs: {ex.Message}");
                    }
                    
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void RedeliverMessage(Guid messageId)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.RedeliverMessage"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessagePipeBus<TMessage>));
                }

                try
                {
                    // Get the message from the store
                    var message = _messageStore.GetMessage(messageId);
                    
                    if (message == null)
                    {
                        if (_logger != null)
                        {
                            _logger.Warning($"Message {messageId} not found for redelivery");
                        }
                        
                        return;
                    }
                    
                    // Reset the delivery attempts
                    ResetMessageDeliveryAttempts(messageId);
                    
                    // Publish to the inner bus
                    _innerBus.Publish(message);
                    
                    // If successful, mark as delivered or remove
                    if (_options.RemoveOnSuccessfulDelivery)
                    {
                        RemoveMessage(messageId);
                    }
                    else
                    {
                        MarkMessageAsDelivered(messageId);
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Redelivered message {messageId}");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error redelivering message {messageId}: {ex.Message}");
                    }
                    
                    // Increment delivery attempts
                    IncrementMessageDeliveryAttempts(messageId);
                    
                    // If configured to throw, rethrow the exception
                    if (_options.RethrowExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Processes pending messages for redelivery.
        /// </summary>
        /// <param name="state">State object (not used).</param>
        private void ProcessPendingMessages(object state)
        {
            using (_profiler?.BeginSample("ReliableMessagePipeBus.ProcessPendingMessages"))
            {
                if (_isDisposed || !_isStarted)
                {
                    return;
                }
                
                try
                {
                    // Get all messages that need redelivery
                    var pendingMessages = GetPendingMessagesForRedelivery();
                    
                    if (pendingMessages.Count == 0)
                    {
                        return;
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Processing {pendingMessages.Count} pending messages for redelivery");
                    }
                    
                    // Process each message
                    foreach (var message in pendingMessages)
                    {
                        // Skip delivered messages if they should be removed
                        if (IsMessageDelivered(message.Id) && _options.RemoveOnSuccessfulDelivery)
                        {
                            RemoveMessage(message.Id);
                            continue;
                        }
                        
                        // Skip if we've exceeded the maximum delivery attempts
                        int maxAttempts = _options.MaxRetryAttempts > 0 
                            ? _options.MaxRetryAttempts 
                            : DefaultMaxDeliveryAttempts;
                        
                        if (GetMessageDeliveryAttempts(message.Id) >= maxAttempts)
                        {
                            if (_logger != null)
                            {
                                _logger.Warning($"Message {message.Id} exceeded maximum delivery attempts ({maxAttempts})");
                            }
                            
                            // Handle maximum attempts exceeded
                            if (_options.RemoveFailedMessages)
                            {
                                RemoveMessage(message.Id);
                            }
                            
                            continue;
                        }
                        
                        try
                        {
                            // Attempt to redeliver the message
                            _innerBus.Publish(message);
                            
                            // If successful, mark as delivered or remove
                            if (_options.RemoveOnSuccessfulDelivery)
                            {
                                RemoveMessage(message.Id);
                            }
                            else
                            {
                                MarkMessageAsDelivered(message.Id);
                            }
                            
                            if (_logger != null)
                            {
                                _logger.Debug($"Redelivered message {message.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null)
                            {
                                _logger.Error($"Error redelivering message {message.Id}: {ex.Message}");
                            }
                            
                            // Increment delivery attempts
                            IncrementMessageDeliveryAttempts(message.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error processing pending messages: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets all pending messages that need redelivery.
        /// </summary>
        /// <returns>A list of messages that need redelivery.</returns>
        private List<TMessage> GetPendingMessagesForRedelivery()
        {
            try
            {
                return _messageStore.GetAllMessages();
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Error($"Error getting pending messages: {ex.Message}");
                }
                
                return new List<TMessage>();
            }
        }

        /// <summary>
        /// Stores a message for reliable delivery.
        /// </summary>
        /// <param name="message">The message to store.</param>
        private void StoreMessage(TMessage message)
        {
            try
            {
                // Store the message
                _messageStore.StoreMessage(message);
                
                // Initialize metadata
                InitializeMessageMetadata(message.Id);
                
                if (_logger != null)
                {
                    _logger.Debug($"Stored message {message.Id} for reliable delivery");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Error($"Error storing message: {ex.Message}");
                }
                
                throw;
            }
        }

        /// <summary>
        /// Stores a message for reliable delivery asynchronously.
        /// </summary>
        /// <param name="message">The message to store.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task StoreMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // Store the message
                await _messageStore.StoreMessageAsync(message, cancellationToken);
                
                // Initialize metadata
                await InitializeMessageMetadataAsync(message.Id, cancellationToken);
                
                if (_logger != null)
                {
                    _logger.Debug($"Stored message {message.Id} for reliable delivery asynchronously");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Error($"Error storing message asynchronously: {ex.Message}");
                }
                
                throw;
            }
        }

        /// <summary>
        /// Removes a message from the store.
        /// </summary>
        /// <param name="messageId">The ID of the message to remove.</param>
        private void RemoveMessage(Guid messageId)
        {
            try
            {
                _messageStore.RemoveMessage(messageId);
                
                if (_logger != null)
                {
                    _logger.Debug($"Removed message {messageId}");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Warning($"Error removing message {messageId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Removes a message from the store asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task RemoveMessageAsync(Guid messageId, CancellationToken cancellationToken)
        {
            try
            {
                await _messageStore.RemoveMessageAsync(messageId, cancellationToken);
                
                if (_logger != null)
                {
                    _logger.Debug($"Removed message {messageId} asynchronously");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Warning($"Error removing message {messageId} asynchronously: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Initializes metadata for a new message.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        private void InitializeMessageMetadata(Guid messageId)
        {
            // Metadata is stored in a separate store or can be part of the message
            // itself. This implementation assumes a separate metadata store or
            // implementation-specific details.
            
            // Example implementation:
            // _metadataStore.SetDeliveryAttempts(messageId, 0);
            // _metadataStore.SetDelivered(messageId, false);
        }

        /// <summary>
        /// Initializes metadata for a new message asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task InitializeMessageMetadataAsync(Guid messageId, CancellationToken cancellationToken)
        {
            // Async version of InitializeMessageMetadata
            await Task.CompletedTask; // Placeholder
        }

        /// <summary>
        /// Gets the number of delivery attempts for a message.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>The number of delivery attempts.</returns>
        private int GetMessageDeliveryAttempts(Guid messageId)
        {
            // Example implementation:
            // return _metadataStore.GetDeliveryAttempts(messageId);
            return 0; // Placeholder
        }

        /// <summary>
        /// Increments the number of delivery attempts for a message.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        private void IncrementMessageDeliveryAttempts(Guid messageId)
        {
            // Example implementation:
            // _metadataStore.SetDeliveryAttempts(messageId, GetMessageDeliveryAttempts(messageId) + 1);
        }

        /// <summary>
        /// Resets the number of delivery attempts for a message.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        private void ResetMessageDeliveryAttempts(Guid messageId)
        {
            // Example implementation:
            // _metadataStore.SetDeliveryAttempts(messageId, 0);
        }

        /// <summary>
        /// Marks a message as delivered.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        private void MarkMessageAsDelivered(Guid messageId)
        {
            // Example implementation:
            // _metadataStore.SetDelivered(messageId, true);
        }

        /// <summary>
        /// Marks a message as delivered asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task MarkMessageAsDeliveredAsync(Guid messageId, CancellationToken cancellationToken)
        {
            // Async version of MarkMessageAsDelivered
            await Task.CompletedTask; // Placeholder
        }

        /// <summary>
        /// Checks if a message has been delivered.
        /// </summary>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>True if the message has been delivered; otherwise, false.</returns>
        private bool IsMessageDelivered(Guid messageId)
        {
            // Example implementation:
            // return _metadataStore.GetDelivered(messageId);
            return false; // Placeholder
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            
            // Stop the redelivery process
            Stop();
            
            // Cancel any pending operations
            _cancellationTokenSource.Cancel();
            
            // Dispose resources
            _redeliveryTimer.Dispose();
            _cancellationTokenSource.Dispose();
            
            // Dispose the inner bus if we own it
            if (_innerBus is IDisposable disposableBus)
            {
                disposableBus.Dispose();
            }
            
            // Mark as disposed
            _isDisposed = true;
            
            if (_logger != null)
            {
                _logger.Info($"ReliableMessagePipeBus<{typeof(TMessage).Name}> disposed");
            }
        }
    }
}