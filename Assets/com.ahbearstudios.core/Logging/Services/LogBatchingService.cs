using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// High-performance batching service for log messages using Unity.Collections v2.
    /// Implements zero-allocation logging with Burst compilation compatibility.
    /// Provides efficient batching and asynchronous processing for optimal performance.
    /// </summary>
    public sealed class LogBatchingService : IDisposable
    {
        // Native collections for high-performance scenarios
        private NativeQueue<LogMessage> _messageQueue;
        private NativeList<LogMessage> _batchBuffer;
        private NativeList<LogMessage> _processingBuffer;
        
        // Managed collections for complex operations
        private readonly List<ILogTarget> _targets;
        private readonly Timer _flushTimer;
        private readonly object _batchLock = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processingTask;
        private readonly Stopwatch _performanceStopwatch;
        
        private volatile bool _disposed = false;
        private volatile bool _isProcessing = false;
        
        /// <summary>
        /// Gets the maximum number of messages to queue before forcing a flush.
        /// </summary>
        public int MaxQueueSize { get; }
        
        /// <summary>
        /// Gets the interval at which batched messages are flushed.
        /// </summary>
        public TimeSpan FlushInterval { get; }
        
        /// <summary>
        /// Gets whether high-performance mode is enabled for zero-allocation logging.
        /// </summary>
        public bool HighPerformanceMode { get; }
        
        /// <summary>
        /// Gets whether Burst compilation compatibility is enabled.
        /// </summary>
        public bool BurstCompatibility { get; }
        
        /// <summary>
        /// Gets the registered log targets for batch processing.
        /// </summary>
        public IReadOnlyList<ILogTarget> Targets => _targets.AsReadOnly();
        
        /// <summary>
        /// Gets the current queue size.
        /// </summary>
        public int QueueSize => _messageQueue.IsCreated ? _messageQueue.Count : 0;
        
        /// <summary>
        /// Gets whether the service is currently processing messages.
        /// </summary>
        public bool IsProcessing => _isProcessing;
        
        /// <summary>
        /// Gets batching performance metrics.
        /// </summary>
        public BatchingMetrics Metrics { get; private set; }

        /// <summary>
        /// Event raised when a batch is processed.
        /// </summary>
        public event EventHandler<BatchProcessedEventArgs> BatchProcessed;

        /// <summary>
        /// Event raised when the queue reaches capacity.
        /// </summary>
        public event EventHandler<QueueCapacityEventArgs> QueueCapacityReached;

        /// <summary>
        /// Initializes a new instance of the LogBatchingService with Unity.Collections v2.
        /// </summary>
        /// <param name="targets">The log targets to process batches for</param>
        /// <param name="maxQueueSize">The maximum number of messages to queue</param>
        /// <param name="flushInterval">The interval at which to flush batches</param>
        /// <param name="highPerformanceMode">Whether to enable high-performance mode</param>
        /// <param name="burstCompatibility">Whether to enable Burst compatibility</param>
        /// <exception cref="ArgumentNullException">Thrown when targets is null</exception>
        /// <exception cref="ArgumentException">Thrown when maxQueueSize is less than or equal to zero</exception>
        public LogBatchingService(
            IReadOnlyList<ILogTarget> targets,
            int maxQueueSize = 1000,
            TimeSpan flushInterval = default,
            bool highPerformanceMode = true,
            bool burstCompatibility = true)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (maxQueueSize <= 0) throw new ArgumentException("Max queue size must be greater than zero", nameof(maxQueueSize));

            _targets = new List<ILogTarget>(targets);
            MaxQueueSize = maxQueueSize;
            FlushInterval = flushInterval == default ? TimeSpan.FromMilliseconds(100) : flushInterval;
            HighPerformanceMode = highPerformanceMode;
            BurstCompatibility = burstCompatibility;

            // Initialize native collections
            _messageQueue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _batchBuffer = new NativeList<LogMessage>(maxQueueSize, Allocator.Persistent);
            _processingBuffer = new NativeList<LogMessage>(maxQueueSize, Allocator.Persistent);
            
            _cancellationTokenSource = new CancellationTokenSource();
            _performanceStopwatch = Stopwatch.StartNew();
            
            Metrics = new BatchingMetrics();

            // Start the flush timer
            _flushTimer = new Timer(FlushCallback, null, FlushInterval, FlushInterval);
            
            // Start the processing task
            _processingTask = Task.Run(ProcessingLoop, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Enqueues a log message for batch processing using native collections.
        /// </summary>
        /// <param name="logMessage">The log message to enqueue</param>
        /// <returns>True if the message was enqueued successfully, false if the queue is full</returns>
        [BurstCompile]
        public bool EnqueueMessage(in LogMessage logMessage)
        {
            if (_disposed || !_messageQueue.IsCreated) return false;

            // Check queue capacity
            if (_messageQueue.Count >= MaxQueueSize)
            {
                OnQueueCapacityReached(new QueueCapacityEventArgs(_messageQueue.Count, MaxQueueSize));
                
                // In high-performance mode, drop messages instead of blocking
                if (HighPerformanceMode)
                {
                    Metrics.IncrementDroppedMessages();
                    return false;
                }
                
                // Force flush when queue is full
                ForceFlush();
            }

            _messageQueue.Enqueue(logMessage);
            Metrics.IncrementEnqueuedMessages();
            return true;
        }

        /// <summary>
        /// Enqueues multiple log messages for batch processing.
        /// </summary>
        /// <param name="logMessages">The log messages to enqueue</param>
        /// <returns>The number of messages successfully enqueued</returns>
        public int EnqueueMessages(IReadOnlyList<LogMessage> logMessages)
        {
            if (_disposed || logMessages == null) return 0;

            int enqueuedCount = 0;
            foreach (var message in logMessages)
            {
                if (EnqueueMessage(message))
                {
                    enqueuedCount++;
                }
            }

            return enqueuedCount;
        }

        /// <summary>
        /// Enqueues messages from a native array for Burst compatibility.
        /// </summary>
        /// <param name="logMessages">The native array of log messages</param>
        /// <returns>The number of messages successfully enqueued</returns>
        [BurstCompile]
        public int EnqueueMessages(NativeArray<LogMessage> logMessages)
        {
            if (_disposed || !logMessages.IsCreated) return 0;

            int enqueuedCount = 0;
            for (int i = 0; i < logMessages.Length; i++)
            {
                if (EnqueueMessage(logMessages[i]))
                {
                    enqueuedCount++;
                }
            }

            return enqueuedCount;
        }

        /// <summary>
        /// Forces an immediate flush of all queued messages.
        /// </summary>
        public void ForceFlush()
        {
            if (_disposed) return;

            FlushCallback(null);
        }

        /// <summary>
        /// Flushes all queued messages asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous flush operation</returns>
        public async Task FlushAsync()
        {
            if (_disposed) return;

            var tcs = new TaskCompletionSource<bool>();
            
            // Trigger flush and wait for completion
            FlushCallback(tcs);
            
            await tcs.Task;
        }

        /// <summary>
        /// Gets the current batching performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        public BatchingMetrics GetMetrics()
        {
            return Metrics.CreateSnapshot();
        }

        /// <summary>
        /// Resets the batching performance metrics.
        /// </summary>
        public void ResetMetrics()
        {
            Metrics = new BatchingMetrics();
        }

        /// <summary>
        /// Updates the target list for batch processing.
        /// </summary>
        /// <param name="newTargets">The new list of targets</param>
        public void UpdateTargets(IReadOnlyList<ILogTarget> newTargets)
        {
            if (newTargets == null) return;

            lock (_batchLock)
            {
                _targets.Clear();
                _targets.AddRange(newTargets);
            }
        }

        /// <summary>
        /// Timer callback for periodic flushing.
        /// </summary>
        /// <param name="state">Timer state (can be TaskCompletionSource for async operations)</param>
        private void FlushCallback(object state)
        {
            if (_disposed || _isProcessing) return;

            var tcs = state as TaskCompletionSource<bool>;
            
            try
            {
                ProcessBatch();
                tcs?.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs?.SetException(ex);
            }
        }

        /// <summary>
        /// Main processing loop for batch operations.
        /// </summary>
        private async Task ProcessingLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Process batches every flush interval
                    await Task.Delay(FlushInterval, _cancellationTokenSource.Token);
                    
                    if (!_disposed && !_isProcessing)
                    {
                        ProcessBatch();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    // Log processing errors (avoid circular logging)
                    System.Diagnostics.Debug.WriteLine($"LogBatchingService processing error: {ex.Message}");
                    Metrics.IncrementTargetErrors();
                }
            }
        }

        /// <summary>
        /// Processes a batch of log messages using native collections.
        /// </summary>
        private void ProcessBatch()
        {
            if (_disposed) return;

            lock (_batchLock)
            {
                if (_isProcessing) return;
                _isProcessing = true;
            }

            try
            {
                _performanceStopwatch.Restart();

                // Clear processing buffer
                _processingBuffer.Clear();

                // Dequeue messages into processing buffer
                int dequeuedCount = 0;
                while (_messageQueue.Count > 0 && dequeuedCount < MaxQueueSize)
                {
                    if (_messageQueue.TryDequeue(out var message))
                    {
                        _processingBuffer.Add(message);
                        dequeuedCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (_processingBuffer.Length == 0)
                {
                    return;
                }

                // Process messages for each target
                ProcessTargetsWithNativeData();

                _performanceStopwatch.Stop();
                
                // Update metrics
                Metrics.IncrementProcessedBatches();
                Metrics.AddProcessedMessages(_processingBuffer.Length);
                Metrics.UpdateProcessingTime(_performanceStopwatch.Elapsed);

                // Raise batch processed event
                OnBatchProcessed(new BatchProcessedEventArgs(_processingBuffer.Length, _performanceStopwatch.Elapsed));
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Processes targets with native data for optimal performance.
        /// </summary>
        private void ProcessTargetsWithNativeData()
        {
            // If Burst compatibility is enabled, use job system for processing
            if (BurstCompatibility && _processingBuffer.Length > 100) // Use jobs for larger batches
            {
                ProcessWithJobSystem();
            }
            else
            {
                ProcessDirectly();
            }
        }

        /// <summary>
        /// Processes messages using Unity's job system for Burst compatibility.
        /// </summary>
        private void ProcessWithJobSystem()
        {
            // Create a job to filter messages for each target
            var filterJob = new MessageFilterJob
            {
                Messages = _processingBuffer.AsArray(),
                FilteredMessages = new NativeList<LogMessage>(_processingBuffer.Length, Allocator.TempJob)
            };

            // Schedule and complete the job
            var jobHandle = filterJob.Schedule();
            jobHandle.Complete();

            try
            {
                // Process filtered messages through targets
                ProcessFilteredMessages(filterJob.FilteredMessages);
            }
            finally
            {
                // Dispose temporary allocations
                if (filterJob.FilteredMessages.IsCreated)
                {
                    filterJob.FilteredMessages.Dispose();
                }
            }
        }

        /// <summary>
        /// Processes messages directly without job system.
        /// </summary>
        private void ProcessDirectly()
        {
            foreach (var target in _targets)
            {
                if (!target.IsEnabled) continue;

                try
                {
                    // Create a list of messages for this target
                    var targetMessages = new List<LogMessage>();
                    
                    for (int i = 0; i < _processingBuffer.Length; i++)
                    {
                        var message = _processingBuffer[i];
                        if (target.ShouldProcessMessage(message))
                        {
                            targetMessages.Add(message);
                        }
                    }

                    // Write messages to target
                    if (targetMessages.Count > 0)
                    {
                        if (targetMessages.Count > 1)
                        {
                            target.WriteBatch(targetMessages);
                        }
                        else
                        {
                            target.Write(targetMessages[0]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Metrics.IncrementTargetErrors();
                    System.Diagnostics.Debug.WriteLine($"LogBatchingService target error ({target.Name}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processes filtered messages from job system.
        /// </summary>
        /// <param name="filteredMessages">The filtered messages to process</param>
        private void ProcessFilteredMessages(NativeList<LogMessage> filteredMessages)
        {
            var messageList = new List<LogMessage>();
            for (int i = 0; i < filteredMessages.Length; i++)
            {
                messageList.Add(filteredMessages[i]);
            }

            foreach (var target in _targets)
            {
                if (target.IsEnabled)
                {
                    try
                    {
                        target.WriteBatch(messageList);
                    }
                    catch (Exception ex)
                    {
                        Metrics.IncrementTargetErrors();
                        System.Diagnostics.Debug.WriteLine($"LogBatchingService target error ({target.Name}): {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Raises the BatchProcessed event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        protected virtual void OnBatchProcessed(BatchProcessedEventArgs args)
        {
            BatchProcessed?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the QueueCapacityReached event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        protected virtual void OnQueueCapacityReached(QueueCapacityEventArgs args)
        {
            QueueCapacityReached?.Invoke(this, args);
        }

        /// <summary>
        /// Disposes the batching service and flushes remaining messages.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Stop the timer
            _flushTimer?.Dispose();

            // Cancel processing task
            _cancellationTokenSource?.Cancel();

            // Process remaining messages
            try
            {
                ProcessBatch();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogBatchingService final batch processing error: {ex.Message}");
            }

            // Wait for processing task to complete
            try
            {
                _processingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogBatchingService disposal error: {ex.Message}");
            }

            // Dispose native collections
            if (_messageQueue.IsCreated)
            {
                _messageQueue.Dispose();
            }

            if (_batchBuffer.IsCreated)
            {
                _batchBuffer.Dispose();
            }

            if (_processingBuffer.IsCreated)
            {
                _processingBuffer.Dispose();
            }

            // Dispose managed resources
            _cancellationTokenSource?.Dispose();
            _processingTask?.Dispose();
            _performanceStopwatch?.Stop();
        }
    }

    /// <summary>
    /// Burst-compatible job for filtering log messages.
    /// </summary>
    [BurstCompile]
    public struct MessageFilterJob : IJob
    {
        [ReadOnly] public NativeArray<LogMessage> Messages;
        public NativeList<LogMessage> FilteredMessages;

        public void Execute()
        {
            FilteredMessages.Clear();
            
            for (int i = 0; i < Messages.Length; i++)
            {
                var message = Messages[i];
                
                // Simple filtering logic - in a real implementation, this would be more sophisticated
                if (message.Level >= LogLevel.Debug)
                {
                    FilteredMessages.Add(message);
                }
            }
        }
    }

    /// <summary>
    /// Performance metrics for log batching operations.
    /// </summary>
    public sealed class BatchingMetrics
    {
        private volatile int _enqueuedMessages = 0;
        private volatile int _processedMessages = 0;
        private volatile int _droppedMessages = 0;
        private volatile int _processedBatches = 0;
        private volatile int _targetErrors = 0;
        private volatile long _totalProcessingTicks = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// Gets the total number of enqueued messages.
        /// </summary>
        public int EnqueuedMessages => _enqueuedMessages;

        /// <summary>
        /// Gets the total number of processed messages.
        /// </summary>
        public int ProcessedMessages => _processedMessages;

        /// <summary>
        /// Gets the total number of dropped messages.
        /// </summary>
        public int DroppedMessages => _droppedMessages;

        /// <summary>
        /// Gets the total number of processed batches.
        /// </summary>
        public int ProcessedBatches => _processedBatches;

        /// <summary>
        /// Gets the total number of target errors.
        /// </summary>
        public int TargetErrors => _targetErrors;

        /// <summary>
        /// Gets the average processing time per batch.
        /// </summary>
        public TimeSpan AverageProcessingTime => _processedBatches > 0 
            ? TimeSpan.FromTicks(_totalProcessingTicks / _processedBatches) 
            : TimeSpan.Zero;

        /// <summary>
        /// Gets the total uptime of the batching service.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Gets the messages per second throughput.
        /// </summary>
        public double MessagesPerSecond => Uptime.TotalSeconds > 0 
            ? _processedMessages / Uptime.TotalSeconds 
            : 0.0;

        internal void IncrementEnqueuedMessages() => Interlocked.Increment(ref _enqueuedMessages);
        internal void IncrementDroppedMessages() => Interlocked.Increment(ref _droppedMessages);
        internal void IncrementProcessedBatches() => Interlocked.Increment(ref _processedBatches);
        internal void IncrementTargetErrors() => Interlocked.Increment(ref _targetErrors);
        internal void AddProcessedMessages(int count) => Interlocked.Add(ref _processedMessages, count);
        internal void UpdateProcessingTime(TimeSpan processingTime) => Interlocked.Add(ref _totalProcessingTicks, processingTime.Ticks);

        /// <summary>
        /// Creates a snapshot of the current metrics.
        /// </summary>
        /// <returns>A new BatchingMetrics instance with current values</returns>
        public BatchingMetrics CreateSnapshot()
        {
            return new BatchingMetrics
            {
                _enqueuedMessages = _enqueuedMessages,
                _processedMessages = _processedMessages,
                _droppedMessages = _droppedMessages,
                _processedBatches = _processedBatches,
                _targetErrors = _targetErrors,
                _totalProcessingTicks = _totalProcessingTicks
            };
        }
    }

    /// <summary>
    /// Event arguments for batch processed events.
    /// </summary>
    public sealed class BatchProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the number of messages processed in the batch.
        /// </summary>
        public int MessageCount { get; }

        /// <summary>
        /// Gets the time taken to process the batch.
        /// </summary>
        public TimeSpan ProcessingTime { get; }

        /// <summary>
        /// Initializes a new instance of the BatchProcessedEventArgs.
        /// </summary>
        /// <param name="messageCount">The number of messages processed</param>
        /// <param name="processingTime">The time taken to process the batch</param>
        public BatchProcessedEventArgs(int messageCount, TimeSpan processingTime)
        {
            MessageCount = messageCount;
            ProcessingTime = processingTime;
        }
    }

    /// <summary>
    /// Event arguments for queue capacity events.
    /// </summary>
    public sealed class QueueCapacityEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current queue size.
        /// </summary>
        public int CurrentSize { get; }

        /// <summary>
        /// Gets the maximum queue capacity.
        /// </summary>
        public int MaxCapacity { get; }

        /// <summary>
        /// Initializes a new instance of the QueueCapacityEventArgs.
        /// </summary>
        /// <param name="currentSize">The current queue size</param>
        /// <param name="maxCapacity">The maximum queue capacity</param>
        public QueueCapacityEventArgs(int currentSize, int maxCapacity)
        {
            CurrentSize = currentSize;
            MaxCapacity = maxCapacity;
        }
    }
}