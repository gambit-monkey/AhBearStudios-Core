using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Processes batches of log messages from a queue and sends them to one or more log targets.
    /// Optimized for use with Unity Collections v2 with robust error handling and 
    /// resource management. Supports custom formatting through ILogFormatter.
    /// </summary>
    [BurstCompile]
    public class LogBatchProcessor : IDisposable
    {
        /// <summary>
        /// The targets that will receive processed log messages.
        /// </summary>
        private readonly List<ILogTarget> _targets;
        
        /// <summary>
        /// Legacy burstLogger for backward compatibility.
        /// </summary>
        private readonly IBurstLogger _burstLogger;

        /// <summary>
        /// Indicates if this processor is using legacy burstLogger mode.
        /// </summary>
        private readonly bool _isLegacyMode;
        
        /// <summary>
        /// Queue of log messages waiting to be processed.
        /// </summary>
        private readonly NativeQueue<LogMessage> _queue;
        
        /// <summary>
        /// Custom formatter to use when formatting log messages.
        /// </summary>
        private readonly ILogFormatter _formatter;
        
        /// <summary>
        /// Temporary buffer used to batch log messages for targets.
        /// </summary>
        private readonly NativeList<LogMessage> _batchBuffer;
        
        /// <summary>
        /// Flag to track if this processor has been disposed.
        /// </summary>
        private bool _isDisposed;
        
        /// <summary>
        /// Whether this processor owns the queue and should dispose of it.
        /// </summary>
        private readonly bool _ownsQueue;
        
        /// <summary>
        /// The maximum number of messages to process in a single flush operation to prevent frame spikes.
        /// </summary>
        private readonly int _maxMessagesPerFlush;
        
        #region Multi-target Constructors

        /// <summary>
        /// Creates a new LogBatchProcessor instance with multiple log targets.
        /// </summary>
        /// <param name="targets">Collection of log targets to receive messages.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if targets or formatter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if targets collection is empty.</exception>
        public LogBatchProcessor(IEnumerable<ILogTarget> targets, ILogFormatter formatter, int initialCapacity = 64, int maxMessagesPerFlush = 200)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            _isLegacyMode = false;
            _burstLogger = null;
            _formatter = formatter;
            
            // Create owned collections
            _targets = new List<ILogTarget>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    _targets.Add(target);
                }
            }
            
            if (_targets.Count == 0)
                throw new ArgumentException("At least one non-null log target must be provided", nameof(targets));
                
            _queue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _batchBuffer = new NativeList<LogMessage>(Math.Min(initialCapacity, 64), Allocator.Persistent);
            
            _ownsQueue = true;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with a single log target.
        /// </summary>
        /// <param name="target">Log target to receive messages.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if target or formatter is null.</exception>
        public LogBatchProcessor(ILogTarget target, ILogFormatter formatter, int initialCapacity = 64, int maxMessagesPerFlush = 200)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            _isLegacyMode = false;
            _burstLogger = null;
            _formatter = formatter;
            
            // Create owned collections
            _targets = new List<ILogTarget> { target };
            
            _queue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _batchBuffer = new NativeList<LogMessage>(Math.Min(initialCapacity, 64), Allocator.Persistent);
            
            _ownsQueue = true;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with multiple log targets and an externally managed queue.
        /// </summary>
        /// <param name="targets">Collection of log targets to receive messages.</param>
        /// <param name="queue">Queue of messages to process.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if targets or formatter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if targets collection is empty.</exception>
        public LogBatchProcessor(IEnumerable<ILogTarget> targets, NativeQueue<LogMessage> queue, ILogFormatter formatter, int maxMessagesPerFlush = 200)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            _isLegacyMode = false;
            _burstLogger = null;
            _formatter = formatter;
            
            // Create owned targets list
            _targets = new List<ILogTarget>();
            foreach (var target in targets)
            {
                if (target != null)
                {
                    _targets.Add(target);
                }
            }
            
            if (_targets.Count == 0)
                throw new ArgumentException("At least one non-null log target must be provided", nameof(targets));
            
            // Use externally managed queue
            _queue = queue;
            _batchBuffer = new NativeList<LogMessage>(Math.Min(64, maxMessagesPerFlush), Allocator.Persistent);
            
            _ownsQueue = false;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        #endregion
        
        #region Legacy BurstLogger Constructors
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with an externally managed queue and custom formatter.
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="queue">Queue of messages to process.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger or formatter is null.</exception>
        public LogBatchProcessor(IBurstLogger burstLogger, NativeQueue<LogMessage> queue, ILogFormatter formatter, int maxMessagesPerFlush = 200)
        {
            _burstLogger = burstLogger ?? throw new ArgumentNullException(nameof(burstLogger));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            
            _isLegacyMode = true;
            _targets = new List<ILogTarget>();
            _batchBuffer = new NativeList<LogMessage>(Math.Min(64, maxMessagesPerFlush), Allocator.Persistent);
            
            _queue = queue;
            _ownsQueue = false;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with its own internal queue and custom formatter.
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="formatter">Custom formatter to format log messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger or formatter is null.</exception>
        public LogBatchProcessor(IBurstLogger burstLogger, ILogFormatter formatter, int initialCapacity = 64, int maxMessagesPerFlush = 200)
        {
            _burstLogger = burstLogger ?? throw new ArgumentNullException(nameof(burstLogger));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            
            _isLegacyMode = true;
            _targets = new List<ILogTarget>();
            _batchBuffer = new NativeList<LogMessage>(Math.Min(initialCapacity, 64), Allocator.Persistent);
            
            _queue = new NativeQueue<LogMessage>(Allocator.Persistent);
            _ownsQueue = true;
            _maxMessagesPerFlush = maxMessagesPerFlush > 0 ? maxMessagesPerFlush : 200;
            _isDisposed = false;
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with multiple targets and default formatter.
        /// </summary>
        /// <param name="targets">Collection of log targets to receive messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentException">Thrown if targets collection is empty.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            IEnumerable<ILogTarget> targets,
            int initialCapacity = 64, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(targets, defaultFormatter, initialCapacity, maxMessagesPerFlush);
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with a single target and default formatter.
        /// </summary>
        /// <param name="target">Log target to receive messages.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            ILogTarget target,
            int initialCapacity = 64, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(target, defaultFormatter, initialCapacity, maxMessagesPerFlush);
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with an externally managed queue and using
        /// the default formatting from LogMessage.FormatMessage().
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="queue">Queue of messages to process.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger is null.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            IBurstLogger burstLogger, 
            NativeQueue<LogMessage> queue, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(burstLogger, queue, defaultFormatter, maxMessagesPerFlush);
        }
        
        /// <summary>
        /// Creates a new LogBatchProcessor instance with its own internal queue and using
        /// the default formatting from LogMessage.FormatMessage().
        /// Uses legacy IBurstLogger interface for backward compatibility.
        /// </summary>
        /// <param name="burstLogger">Legacy logger to send processed messages to.</param>
        /// <param name="initialCapacity">Initial capacity for the internal queue.</param>
        /// <param name="maxMessagesPerFlush">Maximum number of messages to process in a single flush operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if burstLogger is null.</exception>
        /// <returns>A new LogBatchProcessor instance.</returns>
        public static LogBatchProcessor CreateWithDefaultFormatter(
            IBurstLogger burstLogger, 
            int initialCapacity = 64, 
            int maxMessagesPerFlush = 200)
        {
            var defaultFormatter = new DefaultLogFormatter();
            return new LogBatchProcessor(burstLogger, defaultFormatter, initialCapacity, maxMessagesPerFlush);
        }
        
        #endregion
        
        /// <summary>
        /// Adds a new log target to this processor. Only available in multi-target mode.
        /// </summary>
        /// <param name="target">The log target to add.</param>
        /// <exception cref="InvalidOperationException">Thrown if processor is in legacy mode.</exception>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public void AddTarget(ILogTarget target)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            if (_isLegacyMode)
                throw new InvalidOperationException("Cannot add targets in legacy IBurstLogger mode");
                
            if (target == null)
                throw new ArgumentNullException(nameof(target));
                
            _targets.Add(target);
        }
        
        /// <summary>
        /// Removes a log target from this processor. Only available in multi-target mode.
        /// </summary>
        /// <param name="target">The log target to remove.</param>
        /// <returns>True if the target was found and removed, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown if processor is in legacy mode.</exception>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public bool RemoveTarget(ILogTarget target)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            if (_isLegacyMode)
                throw new InvalidOperationException("Cannot remove targets in legacy IBurstLogger mode");
                
            if (target == null)
                throw new ArgumentNullException(nameof(target));
                
            return _targets.Remove(target);
        }
        
        /// <summary>
        /// Enqueues a log message for processing.
        /// </summary>
        /// <param name="message">The log message to enqueue.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public void Enqueue(LogMessage message)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            _queue.Enqueue(message);
        }
        
        /// <summary>
        /// Processes and forwards queued log messages to the configured targets.
        /// Limits the number of processed messages per call to prevent frame spikes.
        /// </summary>
        /// <returns>The number of messages processed.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public int Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            int count = 0;
            int remaining = Math.Min(_queue.Count, _maxMessagesPerFlush);
            
            if (remaining == 0)
                return 0;
                
            try
            {
                // Use different processing paths based on mode
                if (_isLegacyMode)
                {
                    count = FlushLegacy(remaining);
                }
                else
                {
                    count = FlushMultiTarget(remaining);
                }
            }
            catch (Exception ex)
            {
                // If we encounter a critical error, try to log it and abort this flush
                try
                {
                    if (_isLegacyMode && _burstLogger != null)
                    {
                        _burstLogger.Log(
                            (byte)Tagging.LogTag.Critical, 
                            $"Critical error during log flush: {ex.Message}", 
                            Tagging.LogTag.Critical.ToString()
                        );
                    }
                }
                catch
                {
                    // Silently fail if even this fails
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Flush implementation for multi-target mode.
        /// </summary>
        /// <param name="maxToProcess">Maximum number of messages to process.</param>
        /// <returns>Number of messages processed.</returns>
        private int FlushMultiTarget(int maxToProcess)
        {
            int count = 0;
            
            // Clear buffer before use
            _batchBuffer.Clear();
            
            // Dequeue messages into the batch buffer
            while (count < maxToProcess && _queue.TryDequeue(out var log))
            {
                _batchBuffer.Add(log);
                count++;
            }
            
            if (count > 0)
            {
                // Process the batch through all targets
                foreach (var target in _targets)
                {
                    try
                    {
                        target.WriteBatch(_batchBuffer);
                    }
                    catch
                    {
                        // Continue with other targets if one fails
                    }
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Flush implementation for legacy mode using IBurstLogger.
        /// </summary>
        /// <param name="maxToProcess">Maximum number of messages to process.</param>
        /// <returns>Number of messages processed.</returns>
        private int FlushLegacy(int maxToProcess)
        {
            int count = 0;
            
            while (count < maxToProcess && _queue.TryDequeue(out var log))
            {
                try
                {
                    // Use the custom formatter to format the message
                    FixedString512Bytes formattedMessage = _formatter.Format(log);
                    
                    // Convert to string for the burstLogger interface
                    _burstLogger.Log(log.Level, formattedMessage.ToString(), log.GetTagString().ToString());
                    
                    count++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other messages
                    try
                    {
                        _burstLogger.Log(
                            (byte)Tagging.LogTag.Error, 
                            $"Error processing log message: {ex.Message}", 
                            Tagging.LogTag.Error.ToString()
                        );
                    }
                    catch
                    {
                        // Last resort: if even logging the error fails, we have to silently fail
                        // to avoid causing more issues
                    }
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Gets the current number of queued messages waiting to be processed.
        /// </summary>
        /// <returns>The count of queued messages.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the processor has been disposed.</exception>
        public int GetQueuedMessageCount()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LogBatchProcessor));
                
            return _queue.Count;
        }
        
        /// <summary>
        /// Disposes the processor and releases any resources owned by it.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Process any remaining logs before disposing
            try
            {
                // Keep flushing until queue is empty or we hit a reasonable limit
                const int maxFlushAttempts = 10;
                for (int i = 0; i < maxFlushAttempts && _queue.Count > 0; i++)
                {
                    Flush();
                }
            }
            catch
            {
                // Silently continue if flush fails during disposal
            }
            
            // Dispose of collections we own
            if (_ownsQueue && _queue.IsCreated)
            {
                _queue.Dispose();
            }
            
            if (_batchBuffer.IsCreated)
            {
                _batchBuffer.Dispose();
            }
            
            // Clear the targets list
            _targets.Clear();
            
            _isDisposed = true;
        }
        
        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~LogBatchProcessor()
        {
            Dispose();
        }
    }
}