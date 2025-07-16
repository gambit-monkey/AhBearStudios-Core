using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Service for buffering log entries with configurable flushing strategies.
    /// Provides high-performance buffering with automatic and manual flush capabilities.
    /// Supports different buffering strategies including size-based, time-based, and hybrid approaches.
    /// </summary>
    public sealed class LogBufferService : IDisposable
    {
        private readonly NativeList<LogEntry> _buffer;
        private readonly Timer _flushTimer;
        private readonly object _bufferLock = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the maximum buffer size before automatic flushing.
        /// </summary>
        public int MaxBufferSize { get; }

        /// <summary>
        /// Gets the flush interval for time-based flushing.
        /// </summary>
        public TimeSpan FlushInterval { get; }

        /// <summary>
        /// Gets the buffering strategy being used.
        /// </summary>
        public BufferingStrategy Strategy { get; }

        /// <summary>
        /// Gets the current number of buffered entries.
        /// </summary>
        public int BufferedCount
        {
            get
            {
                lock (_bufferLock)
                {
                    return _buffer.IsCreated ? _buffer.Length : 0;
                }
            }
        }

        /// <summary>
        /// Gets whether the buffer is currently full.
        /// </summary>
        public bool IsFull
        {
            get
            {
                lock (_bufferLock)
                {
                    return _buffer.IsCreated && _buffer.Length >= MaxBufferSize;
                }
            }
        }

        /// <summary>
        /// Gets buffering performance metrics.
        /// </summary>
        public BufferingMetrics Metrics { get; private set; }

        /// <summary>
        /// Event raised when the buffer is flushed.
        /// </summary>
        public event EventHandler<BufferFlushedEventArgs> BufferFlushed;

        /// <summary>
        /// Event raised when the buffer reaches capacity.
        /// </summary>
        public event EventHandler<BufferCapacityEventArgs> BufferCapacityReached;

        /// <summary>
        /// Initializes a new instance of the LogBufferService.
        /// </summary>
        /// <param name="maxBufferSize">The maximum buffer size before automatic flushing</param>
        /// <param name="flushInterval">The flush interval for time-based flushing</param>
        /// <param name="strategy">The buffering strategy to use</param>
        public LogBufferService(
            int maxBufferSize = 1000,
            TimeSpan flushInterval = default,
            BufferingStrategy strategy = BufferingStrategy.Hybrid)
        {
            if (maxBufferSize <= 0)
                throw new ArgumentException("Max buffer size must be greater than zero", nameof(maxBufferSize));

            MaxBufferSize = maxBufferSize;
            FlushInterval = flushInterval == default ? TimeSpan.FromSeconds(5) : flushInterval;
            Strategy = strategy;

            _buffer = new NativeList<LogEntry>(maxBufferSize, Allocator.Persistent);
            _cancellationTokenSource = new CancellationTokenSource();
            Metrics = new BufferingMetrics();

            // Start flush timer based on strategy
            if (strategy == BufferingStrategy.TimeBased || strategy == BufferingStrategy.Hybrid)
            {
                _flushTimer = new Timer(FlushTimerCallback, null, FlushInterval, FlushInterval);
            }
        }

        /// <summary>
        /// Buffers a log entry for later flushing.
        /// </summary>
        /// <param name="logEntry">The log entry to buffer</param>
        /// <returns>True if the entry was buffered successfully, false if the buffer is full</returns>
        [BurstCompile]
        public bool BufferEntry(LogEntry logEntry)
        {
            if (_disposed) return false;

            lock (_bufferLock)
            {
                if (_buffer.Length >= MaxBufferSize)
                {
                    OnBufferCapacityReached(new BufferCapacityEventArgs(_buffer.Length, MaxBufferSize));
                    
                    // Handle buffer overflow based on strategy
                    if (Strategy == BufferingStrategy.SizeBased || Strategy == BufferingStrategy.Hybrid)
                    {
                        // Auto-flush when buffer is full
                        var flushedEntries = FlushBufferInternal();
                        if (flushedEntries.Count > 0)
                        {
                            OnBufferFlushed(new BufferFlushedEventArgs(flushedEntries.Count, FlushReason.BufferFull));
                        }
                    }
                    else
                    {
                        // Drop entry if buffer is full and not using size-based flushing
                        Metrics.IncrementDroppedEntries();
                        return false;
                    }
                }

                _buffer.Add(logEntry);
                Metrics.IncrementBufferedEntries();
                return true;
            }
        }

        /// <summary>
        /// Buffers multiple log entries for later flushing.
        /// </summary>
        /// <param name="logEntries">The log entries to buffer</param>
        /// <returns>The number of entries successfully buffered</returns>
        public int BufferEntries(IReadOnlyList<LogEntry> logEntries)
        {
            if (_disposed || logEntries == null) return 0;

            int bufferedCount = 0;
            foreach (var entry in logEntries)
            {
                if (BufferEntry(entry))
                {
                    bufferedCount++;
                }
            }

            return bufferedCount;
        }

        /// <summary>
        /// Buffers entries from a native array for Burst compatibility using Unity.Collections v2.
        /// </summary>
        /// <param name="logEntries">The native array of log entries</param>
        /// <returns>The number of entries successfully buffered</returns>
        [BurstCompile]
        public int BufferEntries(NativeArray<LogEntry> logEntries)
        {
            if (_disposed || !logEntries.IsCreated) return 0;

            int bufferedCount = 0;
            for (int i = 0; i < logEntries.Length; i++)
            {
                if (BufferEntry(logEntries[i]))
                {
                    bufferedCount++;
                }
            }

            return bufferedCount;
        }

        /// <summary>
        /// Manually flushes the buffer and returns the flushed entries.
        /// </summary>
        /// <returns>The list of flushed log entries</returns>
        public IReadOnlyList<LogEntry> FlushBuffer()
        {
            if (_disposed) return Array.Empty<LogEntry>();

            var flushedEntries = FlushBufferInternal();
            if (flushedEntries.Count > 0)
            {
                OnBufferFlushed(new BufferFlushedEventArgs(flushedEntries.Count, FlushReason.Manual));
            }

            return flushedEntries;
        }

        /// <summary>
        /// Asynchronously flushes the buffer.
        /// </summary>
        /// <returns>A task representing the asynchronous flush operation</returns>
        public async Task<IReadOnlyList<LogEntry>> FlushBufferAsync()
        {
            if (_disposed) return Array.Empty<LogEntry>();

            return await Task.Run(() => FlushBuffer());
        }

        /// <summary>
        /// Clears the buffer without flushing.
        /// </summary>
        public void ClearBuffer()
        {
            if (_disposed) return;

            lock (_bufferLock)
            {
                if (_buffer.IsCreated)
                {
                    var clearedCount = _buffer.Length;
                    _buffer.Clear();
                    Metrics.IncrementClearedEntries(clearedCount);
                }
            }
        }

        /// <summary>
        /// Gets the current buffering performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        public BufferingMetrics GetMetrics()
        {
            return Metrics.CreateSnapshot();
        }

        /// <summary>
        /// Resets the buffering performance metrics.
        /// </summary>
        public void ResetMetrics()
        {
            Metrics = new BufferingMetrics();
        }

        /// <summary>
        /// Internal method to flush the buffer.
        /// </summary>
        /// <returns>The list of flushed log entries</returns>
        private List<LogEntry> FlushBufferInternal()
        {
            var flushedEntries = new List<LogEntry>();

            lock (_bufferLock)
            {
                if (_buffer.IsCreated && _buffer.Length > 0)
                {
                    // Copy entries to managed list
                    for (int i = 0; i < _buffer.Length; i++)
                    {
                        flushedEntries.Add(_buffer[i]);
                    }

                    // Clear the buffer
                    _buffer.Clear();
                    Metrics.IncrementFlushedEntries(flushedEntries.Count);
                }
            }

            return flushedEntries;
        }

        /// <summary>
        /// Timer callback for periodic flushing.
        /// </summary>
        /// <param name="state">Timer state</param>
        private void FlushTimerCallback(object state)
        {
            if (_disposed) return;

            var flushedEntries = FlushBufferInternal();
            if (flushedEntries.Count > 0)
            {
                OnBufferFlushed(new BufferFlushedEventArgs(flushedEntries.Count, FlushReason.Timer));
            }
        }

        /// <summary>
        /// Raises the BufferFlushed event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void OnBufferFlushed(BufferFlushedEventArgs args)
        {
            BufferFlushed?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the BufferCapacityReached event.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void OnBufferCapacityReached(BufferCapacityEventArgs args)
        {
            BufferCapacityReached?.Invoke(this, args);
        }

        /// <summary>
        /// Disposes the buffer service and flushes remaining entries.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Stop the timer
            _flushTimer?.Dispose();

            // Flush remaining entries
            try
            {
                var remainingEntries = FlushBufferInternal();
                if (remainingEntries.Count > 0)
                {
                    OnBufferFlushed(new BufferFlushedEventArgs(remainingEntries.Count, FlushReason.Disposal));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogBufferService disposal flush error: {ex.Message}");
            }

            // Dispose native collections
            if (_buffer.IsCreated)
            {
                _buffer.Dispose();
            }

            // Dispose managed resources
            _cancellationTokenSource?.Dispose();
        }
    }

    /// <summary>
    /// Defines the buffering strategy for the LogBufferService.
    /// </summary>
    public enum BufferingStrategy
    {
        /// <summary>
        /// Flush when buffer reaches maximum size.
        /// </summary>
        SizeBased,

        /// <summary>
        /// Flush at regular time intervals.
        /// </summary>
        TimeBased,

        /// <summary>
        /// Flush when buffer reaches maximum size OR at regular time intervals.
        /// </summary>
        Hybrid,

        /// <summary>
        /// Manual flushing only.
        /// </summary>
        Manual
    }

    /// <summary>
    /// Defines the reason for a buffer flush operation.
    /// </summary>
    public enum FlushReason
    {
        /// <summary>
        /// Buffer was manually flushed.
        /// </summary>
        Manual,

        /// <summary>
        /// Buffer was flushed due to reaching capacity.
        /// </summary>
        BufferFull,

        /// <summary>
        /// Buffer was flushed due to timer expiration.
        /// </summary>
        Timer,

        /// <summary>
        /// Buffer was flushed during disposal.
        /// </summary>
        Disposal
    }

    /// <summary>
    /// Performance metrics for log buffering operations.
    /// </summary>
    public sealed class BufferingMetrics
    {
        private volatile int _bufferedEntries = 0;
        private volatile int _flushedEntries = 0;
        private volatile int _droppedEntries = 0;
        private volatile int _clearedEntries = 0;
        private volatile int _flushOperations = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// Gets the total number of buffered entries.
        /// </summary>
        public int BufferedEntries => _bufferedEntries;

        /// <summary>
        /// Gets the total number of flushed entries.
        /// </summary>
        public int FlushedEntries => _flushedEntries;

        /// <summary>
        /// Gets the total number of dropped entries.
        /// </summary>
        public int DroppedEntries => _droppedEntries;

        /// <summary>
        /// Gets the total number of cleared entries.
        /// </summary>
        public int ClearedEntries => _clearedEntries;

        /// <summary>
        /// Gets the total number of flush operations.
        /// </summary>
        public int FlushOperations => _flushOperations;

        /// <summary>
        /// Gets the average entries per flush operation.
        /// </summary>
        public double AverageEntriesPerFlush => _flushOperations > 0 ? (double)_flushedEntries / _flushOperations : 0.0;

        /// <summary>
        /// Gets the buffer efficiency ratio (flushed / buffered).
        /// </summary>
        public double BufferEfficiency => _bufferedEntries > 0 ? (double)_flushedEntries / _bufferedEntries : 0.0;

        /// <summary>
        /// Gets the drop rate (dropped / buffered).
        /// </summary>
        public double DropRate => _bufferedEntries > 0 ? (double)_droppedEntries / _bufferedEntries : 0.0;

        /// <summary>
        /// Gets the total uptime of the buffering service.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        internal void IncrementBufferedEntries() => Interlocked.Increment(ref _bufferedEntries);
        internal void IncrementDroppedEntries() => Interlocked.Increment(ref _droppedEntries);
        internal void IncrementFlushedEntries(int count)
        {
            Interlocked.Add(ref _flushedEntries, count);
            Interlocked.Increment(ref _flushOperations);
        }
        internal void IncrementClearedEntries(int count) => Interlocked.Add(ref _clearedEntries, count);

        /// <summary>
        /// Creates a snapshot of the current metrics.
        /// </summary>
        /// <returns>A new BufferingMetrics instance with current values</returns>
        public BufferingMetrics CreateSnapshot()
        {
            return new BufferingMetrics
            {
                _bufferedEntries = _bufferedEntries,
                _flushedEntries = _flushedEntries,
                _droppedEntries = _droppedEntries,
                _clearedEntries = _clearedEntries,
                _flushOperations = _flushOperations
            };
        }
    }

    /// <summary>
    /// Event arguments for buffer flushed events.
    /// </summary>
    public sealed class BufferFlushedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the number of entries that were flushed.
        /// </summary>
        public int EntryCount { get; }

        /// <summary>
        /// Gets the reason for the flush operation.
        /// </summary>
        public FlushReason Reason { get; }

        /// <summary>
        /// Initializes a new instance of the BufferFlushedEventArgs.
        /// </summary>
        /// <param name="entryCount">The number of entries that were flushed</param>
        /// <param name="reason">The reason for the flush operation</param>
        public BufferFlushedEventArgs(int entryCount, FlushReason reason)
        {
            EntryCount = entryCount;
            Reason = reason;
        }
    }

    /// <summary>
    /// Event arguments for buffer capacity events.
    /// </summary>
    public sealed class BufferCapacityEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current buffer size.
        /// </summary>
        public int CurrentSize { get; }

        /// <summary>
        /// Gets the maximum buffer capacity.
        /// </summary>
        public int MaxCapacity { get; }

        /// <summary>
        /// Initializes a new instance of the BufferCapacityEventArgs.
        /// </summary>
        /// <param name="currentSize">The current buffer size</param>
        /// <param name="maxCapacity">The maximum buffer capacity</param>
        public BufferCapacityEventArgs(int currentSize, int maxCapacity)
        {
            CurrentSize = currentSize;
            MaxCapacity = maxCapacity;
        }
    }
}