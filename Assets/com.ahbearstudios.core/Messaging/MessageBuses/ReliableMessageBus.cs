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
    /// Implementation of IReliableMessageBus that ensures message delivery even across application restarts.
    /// Uses persistence and redelivery mechanisms to guarantee at-least-once message delivery.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages this bus will handle.</typeparam>
    public class ReliableMessageBus<TMessage> : IReliableMessageBus<TMessage> where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _innerBus;
        private readonly IMessageStore<TMessage> _messageStore;
        private readonly IMessageSerializer<TMessage> _serializer;
        private readonly ReliableMessageOptions _options;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly ReliableMessageProcessor<TMessage> _processor;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _stateLock = new object();
        private bool _isStarted;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the ReliableMessageBus class.
        /// </summary>
        /// <param name="innerBus">The underlying message bus to use for immediate delivery.</param>
        /// <param name="messageStore">The message store for persisting messages.</param>
        /// <param name="serializer">The serializer for converting messages to/from storage format.</param>
        /// <param name="options">Options for configuring the reliable message bus behavior.</param>
        /// <param name="logger">Optional logger for operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public ReliableMessageBus(
            IMessageBus<TMessage> innerBus,
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
            
            // Create a cancellation token source for the processor
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Create the reliable message processor
            _processor = new ReliableMessageProcessor<TMessage>(
                _innerBus, 
                _messageStore, 
                _serializer, 
                _options,
                _logger,
                _profiler);
            
            if (_logger != null)
            {
                _logger.Info("ReliableMessageBus initialized");
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken Subscribe<T>(Action<T> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("ReliableMessageBus.Subscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.Subscribe(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeAsync<T>(Func<T, Task> handler) where T : TMessage
        {
            using (_profiler?.BeginSample("ReliableMessageBus.SubscribeAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeAsync(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAll(Action<TMessage> handler)
        {
            using (_profiler?.BeginSample("ReliableMessageBus.SubscribeToAll"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeToAll(handler);
            }
        }

        /// <inheritdoc/>
        public ISubscriptionToken SubscribeToAllAsync(Func<TMessage, Task> handler)
        {
            using (_profiler?.BeginSample("ReliableMessageBus.SubscribeToAllAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                return _innerBus.SubscribeToAllAsync(handler);
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ISubscriptionToken token)
        {
            using (_profiler?.BeginSample("ReliableMessageBus.Unsubscribe"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                // Delegate to the inner bus
                _innerBus.Unsubscribe(token);
            }
        }

        /// <inheritdoc/>
        public void Publish(TMessage message)
        {
            using (_profiler?.BeginSample("ReliableMessageBus.Publish"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Store the message first to ensure it can be redelivered if needed
                StoreMessage(message);
                
                // Then publish to the inner bus for immediate delivery
                try
                {
                    _innerBus.Publish(message);
                    
                    // If we get here, the message was successfully published to the inner bus
                    // Mark it as delivered if configured to do so
                    if (_options.RemoveOnSuccessfulDelivery)
                    {
                        RemoveMessage(message.Id);
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error publishing message: {ex.Message}");
                    }
                    
                    // The processor will handle redelivery
                }
            }
        }

        /// <inheritdoc/>
        public async Task PublishAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("ReliableMessageBus.PublishAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                // Store the message first to ensure it can be redelivered if needed
                await StoreMessageAsync(message, cancellationToken);
                
                // Then publish to the inner bus for immediate delivery
                try
                {
                    await _innerBus.PublishAsync(message, cancellationToken);
                    
                    // If we get here, the message was successfully published to the inner bus
                    // Mark it as delivered if configured to do so
                    if (_options.RemoveOnSuccessfulDelivery)
                    {
                        await RemoveMessageAsync(message.Id, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error publishing message asynchronously: {ex.Message}");
                    }
                    
                    // The processor will handle redelivery
                }
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            using (_profiler?.BeginSample("ReliableMessageBus.Start"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                lock (_stateLock)
                {
                    if (_isStarted)
                    {
                        return;
                    }
                    
                    // Start the processor
                    _processor.Start(_cancellationTokenSource.Token);
                    _isStarted = true;
                    
                    if (_logger != null)
                    {
                        _logger.Info("ReliableMessageBus started");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            using (_profiler?.BeginSample("ReliableMessageBus.Stop"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                lock (_stateLock)
                {
                    if (!_isStarted)
                    {
                        return;
                    }
                    
                    // Stop the processor
                    _cancellationTokenSource.Cancel();
                    _processor.Stop();
                    _isStarted = false;
                    
                    if (_logger != null)
                    {
                        _logger.Info("ReliableMessageBus stopped");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public int GetPendingMessageCount()
        {
            using (_profiler?.BeginSample("ReliableMessageBus.GetPendingMessageCount"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                return _messageStore.GetMessageCount();
            }
        }

        /// <inheritdoc/>
        public void ClearPendingMessages()
        {
            using (_profiler?.BeginSample("ReliableMessageBus.ClearPendingMessages"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                _messageStore.ClearMessages();
                
                if (_logger != null)
                {
                    _logger.Info("Cleared all pending messages");
                }
            }
        }

        /// <inheritdoc/>
        public List<Guid> GetPendingMessageIds()
        {
            using (_profiler?.BeginSample("ReliableMessageBus.GetPendingMessageIds"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                return _messageStore.GetMessageIds();
            }
        }

        /// <inheritdoc/>
        public void RedeliverMessage(Guid messageId)
        {
            using (_profiler?.BeginSample("ReliableMessageBus.RedeliverMessage"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(ReliableMessageBus<TMessage>));
                }

                // Load the message from the store
                var message = _messageStore.GetMessage(messageId);
                
                if (message != null)
                {
                    // Publish it to the inner bus
                    try
                    {
                        _innerBus.Publish(message);
                        
                        // If we get here, the message was successfully published to the inner bus
                        // Mark it as delivered if configured to do so
                        if (_options.RemoveOnSuccessfulDelivery)
                        {
                            RemoveMessage(messageId);
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
                    }
                }
                else if (_logger != null)
                {
                    _logger.Warning($"Could not find message {messageId} for redelivery");
                }
            }
        }

        private void StoreMessage(TMessage message)
        {
            try
            {
                _messageStore.StoreMessage(message);
                
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

        private async Task StoreMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await _messageStore.StoreMessageAsync(message, cancellationToken);
                
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

        private void RemoveMessage(Guid messageId)
        {
            try
            {
                _messageStore.RemoveMessage(messageId);
                
                if (_logger != null)
                {
                    _logger.Debug($"Removed delivered message {messageId}");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Warning($"Error removing delivered message {messageId}: {ex.Message}");
                }
            }
        }

        private async Task RemoveMessageAsync(Guid messageId, CancellationToken cancellationToken)
        {
            try
            {
                await _messageStore.RemoveMessageAsync(messageId, cancellationToken);
                
                if (_logger != null)
                {
                    _logger.Debug($"Removed delivered message {messageId} asynchronously");
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.Warning($"Error removing delivered message {messageId} asynchronously: {ex.Message}");
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (_profiler?.BeginSample("ReliableMessageBus.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the reliable message bus.
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
                _cancellationTokenSource.Dispose();
                
                // Dispose the processor
                _processor.Dispose();
                
                // Dispose the inner bus if it's disposable
                if (_innerBus is IDisposable disposableBus)
                {
                    disposableBus.Dispose();
                }
                
                // Dispose the message store if it's disposable
                if (_messageStore is IDisposable disposableStore)
                {
                    disposableStore.Dispose();
                }
                
                // Dispose the serializer if it's disposable
                if (_serializer is IDisposable disposableSerializer)
                {
                    disposableSerializer.Dispose();
                }
                
                if (_logger != null)
                {
                    _logger.Info("ReliableMessageBus disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~ReliableMessageBus()
        {
            Dispose(false);
        }
    }
}