using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using UnityEngine.Profiling;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Sessions
{
    /// <summary>
    /// Profiler session specifically designed for logging operations.
    /// Tracks timing, metrics, and other performance data related to logging activities.
    /// </summary>
    public class LoggingProfilerSession : IProfilerSession
    {
        private readonly ProfilerTag _tag;
        private readonly string _operationType;
        private readonly LogLevel _logLevel;
        private readonly LogTag _logTag;
        private readonly string _logTagString;
        private readonly int _messageCount;
        private readonly int _messageLength;
        private readonly string _targetName;
        private readonly string _formatterName;
        private readonly ILoggingMetrics _loggingMetrics;
        private readonly IMessageBus _messageBus;
        private readonly Guid _sessionId;
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, double> _customMetrics;
        private readonly long _startTimestampNs;
        private bool _isDisposed;
        private bool _success = true;
        private int _batchSuccessCount = 0;
        private int _batchFailureCount = 0;

        /// <summary>
        /// Initializes a new logging profiler session.
        /// </summary>
        /// <param name="tag">Profiler tag for this session.</param>
        /// <param name="operationType">Type of logging operation being profiled.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="logTag">Log tag for categorization.</param>
        /// <param name="messageCount">Number of messages being processed.</param>
        /// <param name="messageLength">Length of the message(s) in characters.</param>
        /// <param name="targetName">Name of the target being written to.</param>
        /// <param name="formatterName">Name of the formatter being used.</param>
        /// <param name="loggingMetrics">Optional logging metrics tracker.</param>
        /// <param name="messageBus">Optional message bus for publishing events.</param>
        public LoggingProfilerSession(
            ProfilerTag tag,
            string operationType,
            LogLevel logLevel,
            LogTag logTag,
            int messageCount = 1,
            int messageLength = 0,
            string targetName = null,
            string formatterName = null,
            ILoggingMetrics loggingMetrics = null,
            IMessageBus messageBus = null)
        {
            _tag = tag;
            _operationType = operationType ?? "Unknown";
            _logLevel = logLevel;
            _logTag = logTag;
            _logTagString = logTag.ToString();
            _messageCount = messageCount;
            _messageLength = messageLength;
            _targetName = targetName;
            _formatterName = formatterName;
            _loggingMetrics = loggingMetrics;
            _messageBus = messageBus;
            _sessionId = Guid.NewGuid();
            _customMetrics = new Dictionary<string, double>();
            _startTimestampNs = GetHighPrecisionTimestampNs();

            _stopwatch = Stopwatch.StartNew();
            
            // Begin Unity profiler sample
            Profiler.BeginSample(_tag.ToString());

            // Record initial queue metrics if available
            if (_loggingMetrics?.IsEnabled == true)
            {
                RecordInitialMetrics();
            }
        }

        /// <summary>
        /// Initializes a new logging profiler session (with string tag for backward compatibility).
        /// </summary>
        /// <param name="tag">Profiler tag for this session.</param>
        /// <param name="operationType">Type of logging operation being profiled.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="logTagString">Log tag as string for categorization.</param>
        /// <param name="messageCount">Number of messages being processed.</param>
        /// <param name="messageLength">Length of the message(s) in characters.</param>
        /// <param name="targetName">Name of the target being written to.</param>
        /// <param name="formatterName">Name of the formatter being used.</param>
        /// <param name="loggingMetrics">Optional logging metrics tracker.</param>
        /// <param name="messageBus">Optional message bus for publishing events.</param>
        public LoggingProfilerSession(
            ProfilerTag tag,
            string operationType,
            LogLevel logLevel,
            string logTagString,
            int messageCount = 1,
            int messageLength = 0,
            string targetName = null,
            string formatterName = null,
            ILoggingMetrics loggingMetrics = null,
            IMessageBus messageBus = null)
        {
            _tag = tag;
            _operationType = operationType ?? "Unknown";
            _logLevel = logLevel;
            _logTag = AhBearStudios.Core.Logging.Tags.Tagging.GetLogTag(logTagString);
            _logTagString = logTagString ?? string.Empty;
            _messageCount = messageCount;
            _messageLength = messageLength;
            _targetName = targetName;
            _formatterName = formatterName;
            _loggingMetrics = loggingMetrics;
            _messageBus = messageBus;
            _sessionId = Guid.NewGuid();
            _customMetrics = new Dictionary<string, double>();
            _startTimestampNs = GetHighPrecisionTimestampNs();

            _stopwatch = Stopwatch.StartNew();
            
            // Begin Unity profiler sample
            Profiler.BeginSample(_tag.ToString());

            // Record initial queue metrics if available
            if (_loggingMetrics?.IsEnabled == true)
            {
                RecordInitialMetrics();
            }
        }

        /// <summary>
        /// Gets the profiler tag for this session.
        /// </summary>
        public ProfilerTag Tag => _tag;

        /// <summary>
        /// Gets the elapsed time in milliseconds since the session started.
        /// </summary>
        public double ElapsedMilliseconds => _stopwatch.Elapsed.TotalMilliseconds;

        /// <summary>
        /// Gets the elapsed time in nanoseconds since the session started.
        /// </summary>
        public long ElapsedNanoseconds => GetHighPrecisionTimestampNs() - _startTimestampNs;

        /// <summary>
        /// Gets whether this session has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Records a custom metric for this session.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="value">Value to record.</param>
        public void RecordMetric(string metricName, double value)
        {
            if (_isDisposed || string.IsNullOrEmpty(metricName)) return;

            _customMetrics[metricName] = value;
        }

        /// <summary>
        /// Gets all custom metrics recorded for this session.
        /// </summary>
        /// <returns>Read-only dictionary of custom metrics.</returns>
        public IReadOnlyDictionary<string, double> GetMetrics()
        {
            return _customMetrics;
        }

        /// <summary>
        /// Records the number of log entries processed.
        /// </summary>
        /// <param name="entryCount">Number of entries processed.</param>
        public void RecordLogEntryCount(int entryCount)
        {
            RecordMetric("LogEntryCount", entryCount);
        }

        /// <summary>
        /// Records the total size of log data processed.
        /// </summary>
        /// <param name="totalBytes">Total data size in bytes.</param>
        public void RecordLogDataSize(long totalBytes)
        {
            RecordMetric("LogDataSizeBytes", totalBytes);
            
            // Record memory usage in the logging metrics
            if (_loggingMetrics?.IsEnabled == true)
            {
                _loggingMetrics.RecordMemoryUsage(totalBytes);
            }
        }

        /// <summary>
        /// Records whether the operation was successful.
        /// </summary>
        /// <param name="success">True if successful, false otherwise.</param>
        public void RecordSuccess(bool success)
        {
            _success = success;
            RecordMetric("Success", success ? 1.0 : 0.0);
            
            // Update batch counters
            if (success)
            {
                _batchSuccessCount++;
            }
            else
            {
                _batchFailureCount++;
            }
        }

        /// <summary>
        /// Records an error that occurred during the operation.
        /// </summary>
        /// <param name="errorCode">Error code or message.</param>
        public void RecordError(string errorCode)
        {
            RecordSuccess(false);
            if (!string.IsNullOrEmpty(errorCode))
            {
                RecordMetric("ErrorCode", errorCode.GetHashCode());
            }
        }

        /// <summary>
        /// Records the throughput (messages per second) for the operation.
        /// </summary>
        /// <param name="messagesPerSecond">Messages processed per second.</param>
        public void RecordThroughput(double messagesPerSecond)
        {
            RecordMetric("ThroughputMPS", messagesPerSecond);
        }

        /// <summary>
        /// Records the number of targets involved in the operation.
        /// </summary>
        /// <param name="targetCount">Number of targets.</param>
        public void RecordTargetCount(int targetCount)
        {
            RecordMetric("TargetCount", targetCount);
        }

        /// <summary>
        /// Records the current queue size.
        /// </summary>
        /// <param name="queueSize">Size of the queue.</param>
        public void RecordQueueSize(int queueSize)
        {
            RecordMetric("QueueSize", queueSize);
            
            // Update queue metrics in the logging metrics tracker
            if (_loggingMetrics?.IsEnabled == true)
            {
                // Assume a reasonable default capacity if not known
                var capacity = Math.Max(queueSize * 2, 1000);
                _loggingMetrics.UpdateQueueMetrics(queueSize, capacity);
            }
        }

        /// <summary>
        /// Records the time spent formatting messages.
        /// </summary>
        /// <param name="formattingTimeMs">Formatting time in milliseconds.</param>
        public void RecordFormattingTime(double formattingTimeMs)
        {
            RecordMetric("FormattingTimeMs", formattingTimeMs);
        }

        /// <summary>
        /// Records a batch processing operation.
        /// </summary>
        /// <param name="batchSize">Size of the batch processed.</param>
        /// <param name="successCount">Number of successful operations.</param>
        /// <param name="failureCount">Number of failed operations.</param>
        public void RecordBatchOperation(int batchSize, int successCount, int failureCount)
        {
            RecordMetric("BatchSize", batchSize);
            RecordMetric("BatchSuccessCount", successCount);
            RecordMetric("BatchFailureCount", failureCount);
            
            _batchSuccessCount = successCount;
            _batchFailureCount = failureCount;
        }

        /// <summary>
        /// Records a flush operation for this session.
        /// </summary>
        /// <param name="messagesFlushed">Number of messages flushed.</param>
        /// <param name="success">Whether the flush was successful.</param>
        public void RecordFlushOperation(int messagesFlushed, bool success)
        {
            RecordMetric("MessagesFlushed", messagesFlushed);
            RecordMetric("FlushSuccess", success ? 1.0 : 0.0);
        }

        /// <summary>
        /// Disposes the session and publishes completion metrics.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _stopwatch.Stop();
            
            // End Unity profiler sample
            Profiler.EndSample();

            // Record logging metrics if available
            RecordLoggingMetrics();

            // Publish session completed message
            if (_messageBus != null)
            {
                try
                {
                    var completedMessage = new LoggingProfilerSessionCompletedMessage(
                        _tag,
                        _sessionId,
                        _operationType,
                        _logLevel,
                        _logTag,
                        _messageCount,
                        _messageLength,
                        _targetName,
                        _formatterName,
                        ElapsedMilliseconds,
                        _customMetrics);

                    var publisher = _messageBus.GetPublisher<LoggingProfilerSessionCompletedMessage>();
                    publisher?.Publish(completedMessage);
                }
                catch
                {
                    // Silently handle publishing errors
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Records initial metrics when the session starts.
        /// </summary>
        private void RecordInitialMetrics()
        {
            if (_loggingMetrics?.IsEnabled != true) return;

            try
            {
                // Record initial queue state if we have custom metrics
                if (_customMetrics.TryGetValue("QueueSize", out var queueSize))
                {
                    var capacity = Math.Max((int)queueSize * 2, 1000);
                    _loggingMetrics.UpdateQueueMetrics((int)queueSize, capacity);
                }
            }
            catch
            {
                // Silently handle metrics recording errors
            }
        }

        /// <summary>
        /// Records metrics to the logging metrics tracker if available.
        /// </summary>
        private void RecordLoggingMetrics()
        {
            if (_loggingMetrics?.IsEnabled != true) return;

            try
            {
                var processingTime = _stopwatch.Elapsed;

                // Determine the appropriate recording method based on operation type
                switch (_operationType.ToLowerInvariant())
                {
                    case "messageprocessing":
                    case "logentrywritten":
                        // Record individual message processing
                        _loggingMetrics.RecordMessageProcessing(_logLevel, _logTag, processingTime, _success, _messageLength);
                        break;

                    case "batchprocessing":
                    case "queueflush":
                        // Record batch processing operation
                        var batchSize = _customMetrics.TryGetValue("BatchSize", out var size) ? (int)size : _messageCount;
                        _loggingMetrics.RecordBatchProcessing(batchSize, processingTime, _batchSuccessCount, _batchFailureCount);
                        break;

                    case "flush":
                        // Record flush operation
                        var messagesFlushed = _customMetrics.TryGetValue("MessagesFlushed", out var flushed) ? (int)flushed : _messageCount;
                        var flushSuccess = _customMetrics.TryGetValue("FlushSuccess", out var flushSuccessValue) ? flushSuccessValue > 0 : _success;
                        _loggingMetrics.RecordFlushOperation(processingTime, messagesFlushed, flushSuccess);
                        break;

                    case "targetwrite":
                    case "formatter":
                        // Record target operation
                        var targetName = _targetName ?? _formatterName ?? "Unknown";
                        var dataSize = _customMetrics.TryGetValue("LogDataSizeBytes", out var dataSizeValue) ? (int)dataSizeValue : _messageLength;
                        _loggingMetrics.RecordTargetOperation(targetName, _operationType, processingTime, _success, dataSize);
                        break;

                    default:
                        // Default to message processing for unknown operation types
                        _loggingMetrics.RecordMessageProcessing(_logLevel, _logTag, processingTime, _success, _messageLength);
                        break;
                }

                // Record memory usage if available
                if (_customMetrics.TryGetValue("LogDataSizeBytes", out var memoryUsage))
                {
                    _loggingMetrics.RecordMemoryUsage((long)memoryUsage);
                }

                // Update queue metrics if available
                if (_customMetrics.TryGetValue("QueueSize", out var currentQueueSize))
                {
                    var capacity = Math.Max((int)currentQueueSize * 2, 1000);
                    _loggingMetrics.UpdateQueueMetrics((int)currentQueueSize, capacity);
                }
            }
            catch
            {
                // Silently handle metrics recording errors
            }
        }

        /// <summary>
        /// Gets a high-precision timestamp in nanoseconds.
        /// </summary>
        /// <returns>Timestamp in nanoseconds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetHighPrecisionTimestampNs()
        {
            return Stopwatch.GetTimestamp() * 1_000_000_000L / Stopwatch.Frequency;
        }
    }
}