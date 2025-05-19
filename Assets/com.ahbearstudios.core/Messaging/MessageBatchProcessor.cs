using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Processes messages in batches for improved throughput in high-volume scenarios.
    /// Reduces processing overhead by handling multiple messages at once.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to process in batches.</typeparam>
    public class MessageBatchProcessor<TMessage> : IDisposable where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _messageBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly List<TMessage> _batchBuffer;
        private readonly int _batchSize;
        private readonly TimeSpan _maxBatchDelay;
        private readonly Timer _batchProcessingTimer;
        private readonly object _batchLock = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;
        private long _totalMessagesProcessed;
        private long _totalBatchesProcessed;
        private bool _isEnabled;
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets a value indicating whether batch processing is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Gets the current number of messages in the batch buffer.
        /// </summary>
        public int BufferCount
        {
            get
            {
                lock (_batchLock)
                {
                    return _batchBuffer.Count;
                }
            }
        }

        /// <summary>
        /// Gets the maximum number of messages in a batch.
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// Gets the maximum time to wait before processing a partially filled batch.
        /// </summary>
        public TimeSpan MaxBatchDelay => _maxBatchDelay;

        /// <summary>
        /// Gets the total number of messages processed.
        /// </summary>
        public long TotalMessagesProcessed => _totalMessagesProcessed;

        /// <summary>
        /// Gets the total number of batches processed.
        /// </summary>
        public long TotalBatchesProcessed => _totalBatchesProcessed;

        /// <summary>
        /// Gets the average batch size (messages per batch).
        /// </summary>
        public double AverageBatchSize
        {
            get
            {
                return _totalBatchesProcessed > 0 ? (double)_totalMessagesProcessed / _totalBatchesProcessed : 0;
            }
        }

        /// <summary>
        /// Event raised when a batch of messages has been processed.
        /// </summary>
        public event EventHandler<BatchProcessedEventArgs> BatchProcessed;

        /// <summary>
        /// Initializes a new instance of the MessageBatchProcessor class.
        /// </summary>
        /// <param name="messageBus">The message bus to publish processed batches to.</param>
        /// <param name="batchSize">The maximum number of messages in a batch.</param>
        /// <param name="maxBatchDelayMs">The maximum time in milliseconds to wait before processing a partially filled batch.</param>
        /// <param name="logger">Optional logger for batch processing operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageBatchProcessor(
            IMessageBus<TMessage> messageBus,
            int batchSize = 100,
            int maxBatchDelayMs = 1000,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero");
            }

            if (maxBatchDelayMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBatchDelayMs), "Max batch delay cannot be negative");
            }

            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger;
            _profiler = profiler;
            _batchSize = batchSize;
            _maxBatchDelay = TimeSpan.FromMilliseconds(maxBatchDelayMs);
            _batchBuffer = new List<TMessage>(batchSize);
            _totalMessagesProcessed = 0;
            _totalBatchesProcessed = 0;
            _isEnabled = true;
            _isDisposed = false;
            
            // Create the batch timer if a delay is specified
            if (maxBatchDelayMs > 0)
            {
                _batchProcessingTimer = new Timer(
                    ProcessBatchTimerCallback,
                    null,
                    _maxBatchDelay,
                    _maxBatchDelay);
            }
            
            // Set up a background task for processing batches
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessingLoop(_cancellationTokenSource.Token));
            
            if (_logger != null)
            {
                _logger.Info($"MessageBatchProcessor initialized with batch size {batchSize}, max delay {maxBatchDelayMs}ms");
            }
        }

        /// <summary>
        /// Adds a message to the current batch, processing the batch if it's full.
        /// </summary>
        /// <param name="message">The message to add to the batch.</param>
        public void AddMessage(TMessage message)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.AddMessage"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBatchProcessor<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (!_isEnabled)
                {
                    // If disabled, just pass the message through directly
                    _messageBus.Publish(message);
                    return;
                }

                bool processBatch = false;
                
                lock (_batchLock)
                {
                    // Add the message to the batch
                    _batchBuffer.Add(message);
                    
                    // Check if we've reached batch size
                    if (_batchBuffer.Count >= _batchSize)
                    {
                        processBatch = true;
                    }
                }
                
                // If the batch is full, process it outside the lock
                if (processBatch)
                {
                    ProcessBatch();
                }
            }
        }

        /// <summary>
        /// Adds a message to the current batch asynchronously, processing the batch if it's full.
        /// </summary>
        /// <param name="message">The message to add to the batch.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddMessageAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.AddMessageAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBatchProcessor<TMessage>));
                }

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (!_isEnabled)
                {
                    // If disabled, just pass the message through directly
                    await _messageBus.PublishAsync(message, cancellationToken);
                    return;
                }

                bool processBatch = false;
                
                lock (_batchLock)
                {
                    // Add the message to the batch
                    _batchBuffer.Add(message);
                    
                    // Check if we've reached batch size
                    if (_batchBuffer.Count >= _batchSize)
                    {
                        processBatch = true;
                    }
                }
                
                // If the batch is full, process it outside the lock
                if (processBatch)
                {
                    await ProcessBatchAsync(cancellationToken);
                }
            }
        }

        /// <summary>
        /// Processes the current batch of messages even if it's not full.
        /// </summary>
        public void ProcessBatch()
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.ProcessBatch"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBatchProcessor<TMessage>));
                }

                if (!_isEnabled)
                {
                    return;
                }

                List<TMessage> batchToProcess = null;
                
                lock (_batchLock)
                {
                    if (_batchBuffer.Count > 0)
                    {
                        // Take the current batch for processing
                        batchToProcess = new List<TMessage>(_batchBuffer);
                        _batchBuffer.Clear();
                    }
                }
                
                // Process the batch outside the lock
                if (batchToProcess != null && batchToProcess.Count > 0)
                {
                    ProcessBatchInternal(batchToProcess);
                }
            }
        }

        /// <summary>
        /// Processes the current batch of messages asynchronously even if it's not full.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessBatchAsync(CancellationToken cancellationToken = default)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.ProcessBatchAsync"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBatchProcessor<TMessage>));
                }

                if (!_isEnabled)
                {
                    return;
                }

                List<TMessage> batchToProcess = null;
                
                lock (_batchLock)
                {
                    if (_batchBuffer.Count > 0)
                    {
                        // Take the current batch for processing
                        batchToProcess = new List<TMessage>(_batchBuffer);
                        _batchBuffer.Clear();
                    }
                }
                
                // Process the batch outside the lock
                if (batchToProcess != null && batchToProcess.Count > 0)
                {
                    await ProcessBatchInternalAsync(batchToProcess, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Internal method to process a batch of messages.
        /// </summary>
        /// <param name="batch">The batch of messages to process.</param>
        private void ProcessBatchInternal(List<TMessage> batch)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.ProcessBatchInternal"))
            {
                if (batch == null || batch.Count == 0)
                {
                    return;
                }

                try
                {
                    int batchCount = batch.Count;
                    
                    // Publish each message in the batch
                    foreach (var message in batch)
                    {
                        _messageBus.Publish(message);
                    }
                    
                    // Update metrics
                    Interlocked.Add(ref _totalMessagesProcessed, batchCount);
                    Interlocked.Increment(ref _totalBatchesProcessed);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Processed batch of {batchCount} messages");
                    }
                    
                    // Raise the batch processed event
                    OnBatchProcessed(batchCount);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error processing batch: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Internal method to process a batch of messages asynchronously.
        /// </summary>
        /// <param name="batch">The batch of messages to process.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessBatchInternalAsync(List<TMessage> batch, CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.ProcessBatchInternalAsync"))
            {
                if (batch == null || batch.Count == 0)
                {
                    return;
                }

                try
                {
                    int batchCount = batch.Count;
                    
                    // Publish each message in the batch
                    foreach (var message in batch)
                    {
                        await _messageBus.PublishAsync(message, cancellationToken);
                    }
                    
                    // Update metrics
                    Interlocked.Add(ref _totalMessagesProcessed, batchCount);
                    Interlocked.Increment(ref _totalBatchesProcessed);
                    
                    if (_logger != null)
                    {
                        _logger.Debug($"Processed batch of {batchCount} messages asynchronously");
                    }
                    
                    // Raise the batch processed event
                    OnBatchProcessed(batchCount);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error processing batch asynchronously: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Callback for the batch processing timer.
        /// </summary>
        /// <param name="state">Timer state, not used.</param>
        private void ProcessBatchTimerCallback(object state)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.ProcessBatchTimerCallback"))
            {
                if (_isDisposed || !_isEnabled)
                {
                    return;
                }

                try
                {
                    // Check if there are any messages to process
                    bool hasMessages;
                    
                    lock (_batchLock)
                    {
                        hasMessages = _batchBuffer.Count > 0;
                    }
                    
                    // Process the batch if there are messages
                    if (hasMessages)
                    {
                        ProcessBatch();
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error in batch timer callback: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Background loop for processing messages.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to stop the loop.</param>
        private async Task ProcessingLoop(CancellationToken cancellationToken)
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.ProcessingLoop"))
            {
                if (_logger != null)
                {
                    _logger.Debug("Message batch processing loop started");
                }
                
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Wait for a short time
                        await Task.Delay(10, cancellationToken);
                        
                        // Check if we need to process a batch
                        bool processBatch = false;
                        
                        lock (_batchLock)
                        {
                            processBatch = _isEnabled && _batchBuffer.Count >= _batchSize;
                        }
                        
                        if (processBatch)
                        {
                            await ProcessBatchAsync(cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error in message processing loop: {ex.Message}");
                    }
                }
                
                if (_logger != null)
                {
                    _logger.Debug("Message batch processing loop stopped");
                }
            }
        }

        /// <summary>
        /// Raises the BatchProcessed event.
        /// </summary>
        /// <param name="batchSize">The size of the batch that was processed.</param>
        private void OnBatchProcessed(int batchSize)
        {
            BatchProcessed?.Invoke(this, new BatchProcessedEventArgs(batchSize));
        }

        /// <summary>
        /// Disposes the batch processor and releases all resources.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageBatchProcessor.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the batch processor.
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
                // Stop the processing loop
                _cancellationTokenSource.Cancel();
                
                try
                {
                    // Wait for the processing task to complete (with timeout)
                    _processingTask.Wait(1000);
                }
                catch
                {
                    // Ignore exceptions during shutdown
                }
                
                // Dispose the cancellation token source
                _cancellationTokenSource.Dispose();
                
                // Dispose the timer if it exists
                _batchProcessingTimer?.Dispose();
                
                // Process any remaining messages
                ProcessBatch();
                
                if (_logger != null)
                {
                    _logger.Info($"MessageBatchProcessor disposed, " +
                                 $"total messages: {_totalMessagesProcessed}, " +
                                 $"total batches: {_totalBatchesProcessed}, " +
                                 $"average batch size: {AverageBatchSize:F2}");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessageBatchProcessor()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Event arguments for when a batch of messages has been processed.
    /// </summary>
    public class BatchProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the number of messages in the batch.
        /// </summary>
        public int BatchSize { get; }
        
        /// <summary>
        /// Gets the time when the batch was processed.
        /// </summary>
        public DateTime ProcessedTime { get; }

        /// <summary>
        /// Initializes a new instance of the BatchProcessedEventArgs class.
        /// </summary>
        /// <param name="batchSize">The number of messages in the batch.</param>
        public BatchProcessedEventArgs(int batchSize)
        {
            BatchSize = batchSize;
            ProcessedTime = DateTime.UtcNow;
        }
    }
}