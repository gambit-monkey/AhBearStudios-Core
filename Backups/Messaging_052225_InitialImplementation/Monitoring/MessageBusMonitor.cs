using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Data;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Provides monitoring and performance metrics collection for message buses.
    /// Tracks message flow, performance, and other diagnostic information.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages to monitor.</typeparam>
    public class MessageBusMonitor<TMessage> : IDisposable where TMessage : IMessage
    {
        private readonly IMessageBus<TMessage> _messageBus;
        private readonly IBurstLogger _logger;
        private readonly IProfiler _profiler;
        private readonly Dictionary<Type, MessageTypeMetrics> _typeMetrics;
        private readonly List<ISubscriptionToken> _subscriptions;
        private readonly object _metricsLock = new object();
        private readonly bool _trackMessageHistory;
        private readonly int _maxHistorySize;
        private readonly List<MessageHistoryEntry> _messageHistory;
        private readonly Timer _periodicReportTimer;
        private long _totalMessagesPublished;
        private long _totalMessagesProcessed;
        private DateTime _startTime;
        private TimeSpan _totalProcessingTime;
        private bool _isEnabled;
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets a value indicating whether performance metrics collection is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Gets the total number of messages published since monitoring began.
        /// </summary>
        public long TotalMessagesPublished => _totalMessagesPublished;

        /// <summary>
        /// Gets the total number of messages processed (received by handlers) since monitoring began.
        /// </summary>
        public long TotalMessagesProcessed => _totalMessagesProcessed;

        /// <summary>
        /// Gets the time elapsed since monitoring began.
        /// </summary>
        public TimeSpan MonitoringTime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Gets the average message processing time in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs
        {
            get
            {
                if (_totalMessagesProcessed == 0)
                {
                    return 0;
                }
                
                return _totalProcessingTime.TotalMilliseconds / _totalMessagesProcessed;
            }
        }

        /// <summary>
        /// Gets the average message rate per second (messages published per second).
        /// </summary>
        public double MessageRatePerSecond
        {
            get
            {
                double totalSeconds = MonitoringTime.TotalSeconds;
                if (totalSeconds <= 0)
                {
                    return 0;
                }
                
                return _totalMessagesPublished / totalSeconds;
            }
        }

        /// <summary>
        /// Initializes a new instance of the MessageBusMonitor class.
        /// </summary>
        /// <param name="messageBus">The message bus to monitor.</param>
        /// <param name="trackMessageHistory">Whether to track message history.</param>
        /// <param name="maxHistorySize">The maximum number of messages to keep in history.</param>
        /// <param name="periodicReportIntervalMs">The interval for periodic report generation in milliseconds, or 0 to disable.</param>
        /// <param name="logger">Optional logger for monitor operations.</param>
        /// <param name="profiler">Optional profiler for performance monitoring.</param>
        public MessageBusMonitor(
            IMessageBus<TMessage> messageBus,
            bool trackMessageHistory = true,
            int maxHistorySize = 1000,
            int periodicReportIntervalMs = 0,
            IBurstLogger logger = null,
            IProfiler profiler = null)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger;
            _profiler = profiler;
            _typeMetrics = new Dictionary<Type, MessageTypeMetrics>();
            _subscriptions = new List<ISubscriptionToken>();
            _trackMessageHistory = trackMessageHistory;
            _maxHistorySize = Math.Max(1, maxHistorySize);
            _messageHistory = _trackMessageHistory ? new List<MessageHistoryEntry>() : null;
            _totalMessagesPublished = 0;
            _totalMessagesProcessed = 0;
            _startTime = DateTime.UtcNow;
            _totalProcessingTime = TimeSpan.Zero;
            _isEnabled = true;
            _isDisposed = false;
            
            // Set up the periodic report timer if requested
            if (periodicReportIntervalMs > 0)
            {
                _periodicReportTimer = new Timer(
                    GeneratePeriodicReport,
                    null,
                    periodicReportIntervalMs,
                    periodicReportIntervalMs);
            }
            
            // Subscribe to message bus events
            AttachToMessageBus();
            
            if (_logger != null)
            {
                _logger.Info("MessageBusMonitor initialized");
            }
        }

        /// <summary>
        /// Gets metrics for a specific message type.
        /// </summary>
        /// <typeparam name="T">The message type to get metrics for.</typeparam>
        /// <returns>The metrics for the specified message type.</returns>
        public MessageTypeMetrics GetMetricsForType<T>() where T : TMessage
        {
            using (_profiler?.BeginSample("MessageBusMonitor.GetMetricsForType"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusMonitor<TMessage>));
                }

                lock (_metricsLock)
                {
                    Type type = typeof(T);
                    if (_typeMetrics.TryGetValue(type, out MessageTypeMetrics metrics))
                    {
                        return metrics.Clone();
                    }
                    
                    return new MessageTypeMetrics
                    {
                        MessageTypeName = type.Name,
                        MessagesPublished = 0,
                        MessagesProcessed = 0,
                        TotalProcessingTime = TimeSpan.Zero,
                        AverageProcessingTimeMs = 0,
                        LastMessageTime = null,
                        PeakMessageRatePerSecond = 0,
                        ErrorCount = 0
                    };
                }
            }
        }

        /// <summary>
        /// Gets metrics for all message types.
        /// </summary>
        /// <returns>A dictionary of message type metrics keyed by type.</returns>
        public Dictionary<Type, MessageTypeMetrics> GetAllMetrics()
        {
            using (_profiler?.BeginSample("MessageBusMonitor.GetAllMetrics"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusMonitor<TMessage>));
                }

                lock (_metricsLock)
                {
                    // Create a deep copy of the metrics
                    var result = new Dictionary<Type, MessageTypeMetrics>();
                    foreach (var kvp in _typeMetrics)
                    {
                        result[kvp.Key] = kvp.Value.Clone();
                    }
                    
                    return result;
                }
            }
        }

        /// <summary>
        /// Gets the recent message history if history tracking is enabled.
        /// </summary>
        /// <param name="count">The maximum number of history entries to return.</param>
        /// <returns>A list of message history entries, or null if history tracking is disabled.</returns>
        public List<MessageHistoryEntry> GetMessageHistory(int count = 100)
        {
            using (_profiler?.BeginSample("MessageBusMonitor.GetMessageHistory"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusMonitor<TMessage>));
                }

                if (!_trackMessageHistory)
                {
                    return null;
                }

                lock (_metricsLock)
                {
                    int actualCount = Math.Min(count, _messageHistory.Count);
                    return _messageHistory.TakeLast(actualCount).ToList();
                }
            }
        }

        /// <summary>
        /// Generates a monitoring report with current metrics.
        /// </summary>
        /// <returns>A monitoring report.</returns>
        public MonitoringReport GenerateReport()
        {
            using (_profiler?.BeginSample("MessageBusMonitor.GenerateReport"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusMonitor<TMessage>));
                }

                lock (_metricsLock)
                {
                    var report = new MonitoringReport
                    {
                        GeneratedAt = DateTime.UtcNow,
                        MonitoringStartTime = _startTime,
                        MonitoringDuration = MonitoringTime,
                        TotalMessagesPublished = _totalMessagesPublished,
                        TotalMessagesProcessed = _totalMessagesProcessed,
                        AverageProcessingTimeMs = AverageProcessingTimeMs,
                        MessageRatePerSecond = MessageRatePerSecond,
                        TypeMetrics = GetAllMetrics(),
                        RecentMessageHistory = _trackMessageHistory ? GetMessageHistory(20) : null
                    };
                    
                    if (_logger != null)
                    {
                        _logger.Debug("Generated monitoring report");
                    }
                    
                    return report;
                }
            }
        }

        /// <summary>
        /// Resets all metrics to zero.
        /// </summary>
        public void ResetMetrics()
        {
            using (_profiler?.BeginSample("MessageBusMonitor.ResetMetrics"))
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MessageBusMonitor<TMessage>));
                }

                lock (_metricsLock)
                {
                    _totalMessagesPublished = 0;
                    _totalMessagesProcessed = 0;
                    _totalProcessingTime = TimeSpan.Zero;
                    _startTime = DateTime.UtcNow;
                    _typeMetrics.Clear();
                    
                    if (_trackMessageHistory)
                    {
                        _messageHistory.Clear();
                    }
                    
                    if (_logger != null)
                    {
                        _logger.Info("Monitoring metrics reset");
                    }
                }
            }
        }

        /// <summary>
        /// Attaches the monitor to the message bus by subscribing to messages.
        /// </summary>
        private void AttachToMessageBus()
        {
            using (_profiler?.BeginSample("MessageBusMonitor.AttachToMessageBus"))
            {
                try
                {
                    // Subscribe to all messages for monitoring
                    var token = _messageBus.SubscribeToAll(OnMessagePublished);
                    _subscriptions.Add(token);
                    
                    if (_logger != null)
                    {
                        _logger.Debug("Attached to message bus for monitoring");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error attaching to message bus: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Handler for messages published on the bus.
        /// </summary>
        /// <param name="message">The published message.</param>
        private void OnMessagePublished(TMessage message)
        {
            using (_profiler?.BeginSample("MessageBusMonitor.OnMessagePublished"))
            {
                if (!_isEnabled || _isDisposed)
                {
                    return;
                }

                DateTime receivedTime = DateTime.UtcNow;
                var messageType = message.GetType();
                
                lock (_metricsLock)
                {
                    // Increment total messages published
                    _totalMessagesPublished++;
                    
                    // Get or create metrics for this message type
                    if (!_typeMetrics.TryGetValue(messageType, out MessageTypeMetrics metrics))
                    {
                        metrics = new MessageTypeMetrics
                        {
                            MessageTypeName = messageType.Name,
                            MessagesPublished = 0,
                            MessagesProcessed = 0,
                            TotalProcessingTime = TimeSpan.Zero,
                            AverageProcessingTimeMs = 0,
                            LastMessageTime = null,
                            PeakMessageRatePerSecond = 0,
                            ErrorCount = 0
                        };
                        
                        _typeMetrics[messageType] = metrics;
                    }
                    
                    // Update metrics for this message type
                    metrics.MessagesPublished++;
                    metrics.LastMessageTime = receivedTime;
                    
                    // Calculate and update peak message rate
                    double duration = MonitoringTime.TotalSeconds;
                    if (duration > 0)
                    {
                        double currentRate = metrics.MessagesPublished / duration;
                        metrics.PeakMessageRatePerSecond = Math.Max(metrics.PeakMessageRatePerSecond, currentRate);
                    }
                    
                    // Add to message history if tracking is enabled
                    if (_trackMessageHistory)
                    {
                        var historyEntry = new MessageHistoryEntry
                        {
                            MessageId = message.Id,
                            MessageType = messageType.Name,
                            ReceivedTime = receivedTime,
                            ProcessingTimeMs = 0 // Will be updated when processing completes
                        };
                        
                        _messageHistory.Add(historyEntry);
                        
                        // Trim history if it exceeds the maximum size
                        if (_messageHistory.Count > _maxHistorySize)
                        {
                            _messageHistory.RemoveAt(0);
                        }
                    }
                }
                
                // Simulate processing the message to record metrics
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                OnMessageProcessed(message, stopwatch.Elapsed, receivedTime, null);
            }
        }

        /// <summary>
        /// Records metrics for a processed message.
        /// </summary>
        /// <param name="message">The processed message.</param>
        /// <param name="processingTime">The time it took to process the message.</param>
        /// <param name="receivedTime">The time the message was received.</param>
        /// <param name="error">Any error that occurred during processing, or null if successful.</param>
        private void OnMessageProcessed(TMessage message, TimeSpan processingTime, DateTime receivedTime, Exception error)
        {
            using (_profiler?.BeginSample("MessageBusMonitor.OnMessageProcessed"))
            {
                if (!_isEnabled || _isDisposed)
                {
                    return;
                }

                var messageType = message.GetType();
                
                lock (_metricsLock)
                {
                    // Update overall metrics
                    _totalMessagesProcessed++;
                    _totalProcessingTime += processingTime;
                    
                    // Update metrics for this message type
                    if (_typeMetrics.TryGetValue(messageType, out MessageTypeMetrics metrics))
                    {
                        metrics.MessagesProcessed++;
                        metrics.TotalProcessingTime += processingTime;
                        
                        if (error != null)
                        {
                            metrics.ErrorCount++;
                        }
                        
                        // Recalculate average processing time
                        metrics.AverageProcessingTimeMs = metrics.TotalProcessingTime.TotalMilliseconds / metrics.MessagesProcessed;
                    }
                    
                    // Update message history if tracking is enabled
                    if (_trackMessageHistory)
                    {
                        for (int i = _messageHistory.Count - 1; i >= 0; i--)
                        {
                            var entry = _messageHistory[i];
                            if (entry.MessageId == message.Id && entry.ReceivedTime == receivedTime)
                            {
                                entry.ProcessingTimeMs = processingTime.TotalMilliseconds;
                                entry.Error = error?.Message;
                                _messageHistory[i] = entry;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a periodic report and logs it.
        /// </summary>
        /// <param name="state">Timer state, not used.</param>
        private void GeneratePeriodicReport(object state)
        {
            using (_profiler?.BeginSample("MessageBusMonitor.GeneratePeriodicReport"))
            {
                if (_isDisposed)
                {
                    return;
                }

                try
                {
                    var report = GenerateReport();
                    
                    if (_logger != null)
                    {
                        _logger.Info($"Periodic Report: {report.TotalMessagesPublished} messages published, " +
                                    $"{report.TotalMessagesProcessed} processed, " +
                                    $"Rate: {report.MessageRatePerSecond:F2} msg/sec, " +
                                    $"Avg Processing: {report.AverageProcessingTimeMs:F2} ms");
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error($"Error generating periodic report: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the monitor and releases all resources.
        /// </summary>
        public void Dispose()
        {
            using (_profiler?.BeginSample("MessageBusMonitor.Dispose"))
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Releases resources used by the message bus monitor.
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
                // Unsubscribe from message bus
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
                
                _subscriptions.Clear();
                
                // Dispose the timer if it exists
                _periodicReportTimer?.Dispose();
                
                if (_logger != null)
                {
                    _logger.Info("MessageBusMonitor disposed");
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resource cleanup.
        /// </summary>
        ~MessageBusMonitor()
        {
            Dispose(false);
        }
    }
}