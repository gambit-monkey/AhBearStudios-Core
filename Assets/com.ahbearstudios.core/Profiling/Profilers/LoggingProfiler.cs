using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Sessions;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Tagging;
using Unity.Profiling;
using Unity.Collections;
using LogTag = AhBearStudios.Core.Logging.Tags.Tagging.LogTag;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Profiler specifically designed for logging operations.
    /// Provides detailed metrics about log message processing, target writes, and queue operations.
    /// </summary>
    public sealed class LoggingProfiler : IProfiler, IDisposable
    {
        private readonly IProfiler _baseProfiler;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<string, LoggingMetricsData> _loggingMetrics;
        private readonly Dictionary<LogLevel, double> _logLevelAlerts;
        private readonly Dictionary<string, double> _targetAlerts;
        private bool _isDisposed;

        /// <summary>
        /// Creates a new logging profiler.
        /// </summary>
        /// <param name="baseProfiler">Base profiler to delegate general operations to.</param>
        /// <param name="messageBus">Message bus for publishing profiling events.</param>
        public LoggingProfiler(IProfiler baseProfiler, IMessageBus messageBus)
        {
            _baseProfiler = baseProfiler ?? throw new ArgumentNullException(nameof(baseProfiler));
            _messageBus = messageBus;
            _loggingMetrics = new Dictionary<string, LoggingMetricsData>();
            _logLevelAlerts = new Dictionary<LogLevel, double>();
            _targetAlerts = new Dictionary<string, double>();
            
            SubscribeToMessages();
        }

        /// <summary>
        /// Gets whether profiling is currently enabled.
        /// </summary>
        public bool IsEnabled => _baseProfiler.IsEnabled;

        /// <summary>
        /// Gets the message bus used for profiling events.
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Begins a Unity profiler sample.
        /// </summary>
        /// <param name="name">Name of the sample.</param>
        /// <returns>Disposable sample handle.</returns>
        public IDisposable BeginSample(string name)
        {
            return _baseProfiler.BeginSample(name);
        }

        /// <summary>
        /// Begins a profiler scope with the specified tag.
        /// </summary>
        /// <param name="tag">Profiler tag for the scope.</param>
        /// <returns>Profiler session for the scope.</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _baseProfiler.BeginScope(tag);
        }

        /// <summary>
        /// Begins a profiler scope with the specified category and name.
        /// </summary>
        /// <param name="category">Profiler category.</param>
        /// <param name="name">Name for the scope.</param>
        /// <returns>Profiler session for the scope.</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _baseProfiler.BeginScope(category, name);
        }

        /// <summary>
        /// Gets metrics for the specified profiler tag (delegates to base profiler for general metrics).
        /// </summary>
        /// <param name="tag">Profiler tag to get metrics for.</param>
        /// <returns>Metrics data for the tag.</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _baseProfiler.GetMetrics(tag);
        }

        /// <summary>
        /// Gets all available general metrics (delegates to base profiler).
        /// </summary>
        /// <returns>Dictionary of all metrics by tag.</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _baseProfiler.GetAllMetrics();
        }

        /// <summary>
        /// Gets historical data for the specified tag.
        /// </summary>
        /// <param name="tag">Profiler tag to get history for.</param>
        /// <returns>List of historical values.</returns>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            return _baseProfiler.GetHistory(tag);
        }

        /// <summary>
        /// Registers an alert for when a metric exceeds a threshold.
        /// </summary>
        /// <param name="metricTag">Metric tag to monitor.</param>
        /// <param name="threshold">Threshold value.</param>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold)
        {
            _baseProfiler.RegisterMetricAlert(metricTag, threshold);
        }

        /// <summary>
        /// Registers an alert for when a session duration exceeds a threshold.
        /// </summary>
        /// <param name="sessionTag">Session tag to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds.</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _baseProfiler.RegisterSessionAlert(sessionTag, thresholdMs);
        }

        /// <summary>
        /// Resets all profiling statistics.
        /// </summary>
        public void ResetStats()
        {
            _baseProfiler.ResetStats();
            _loggingMetrics.Clear();
        }

        /// <summary>
        /// Starts profiling.
        /// </summary>
        public void StartProfiling()
        {
            _baseProfiler.StartProfiling();
        }

        /// <summary>
        /// Stops profiling.
        /// </summary>
        public void StopProfiling()
        {
            _baseProfiler.StopProfiling();
        }

        // Logging-specific profiling methods

        /// <summary>
        /// Begins profiling a message processing operation.
        /// </summary>
        /// <param name="logLevel">Log level of the message.</param>
        /// <param name="logTag">Log tag for categorization.</param>
        /// <param name="messageLength">Length of the message in characters.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginMessageProcessingScope(LogLevel logLevel, LogTag logTag, int messageLength = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("MessageProcessing");

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForMessageProcessing(logLevel),
                "MessageProcessing",
                logLevel,
                logTag,
                1,
                messageLength,
                null,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Begins profiling a message processing operation (with string tag for backward compatibility).
        /// </summary>
        /// <param name="logLevel">Log level of the message.</param>
        /// <param name="tag">Log tag as string.</param>
        /// <param name="messageLength">Length of the message in characters.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginMessageProcessingScope(LogLevel logLevel, string tag, int messageLength = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("MessageProcessing");

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForMessageProcessing(logLevel, tag),
                "MessageProcessing",
                logLevel,
                tag,
                1,
                messageLength,
                null,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Begins profiling a target write operation.
        /// </summary>
        /// <param name="targetName">Name of the log target.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="messageCount">Number of messages being written.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginTargetWriteScope(string targetName, LogLevel logLevel, int messageCount = 1)
        {
            if (!IsEnabled)
                return CreateNullSession("TargetWrite");

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForTargetWrite(targetName, logLevel),
                "TargetWrite",
                logLevel,
                LogTag.Default,
                messageCount,
                0,
                targetName,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Begins profiling a queue flush operation.
        /// </summary>
        /// <param name="messageCount">Number of messages being flushed.</param>
        /// <param name="targetCount">Number of targets being flushed to.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginQueueFlushScope(int messageCount, int targetCount = 1)
        {
            if (!IsEnabled)
                return CreateNullSession("QueueFlush");

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForQueueFlush(messageCount),
                "QueueFlush",
                LogLevel.Info,
                LogTag.Performance,
                messageCount,
                0,
                null,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Begins profiling a log level change operation.
        /// </summary>
        /// <param name="oldLevel">Previous log level.</param>
        /// <param name="newLevel">New log level.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginLevelChangeScope(LogLevel oldLevel, LogLevel newLevel)
        {
            if (!IsEnabled)
                return CreateNullSession("LevelChange");

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForLevelChange(oldLevel, newLevel),
                "LevelChange",
                newLevel,
                LogTag.Performance,
                1,
                0,
                null,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Begins profiling a formatter operation.
        /// </summary>
        /// <param name="formatterName">Name of the formatter.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="messageLength">Length of the message being formatted.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginFormatterScope(string formatterName, LogLevel logLevel, int messageLength = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Formatter");

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForFormatter(formatterName, logLevel),
                "Formatter",
                logLevel,
                LogTag.Performance,
                1,
                messageLength,
                null,
                formatterName,
                null,
                _messageBus);
        }

        /// <summary>
        /// Profiles a logging action with timing.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="logTag">Log tag for categorization.</param>
        /// <param name="action">Action to profile.</param>
        public void ProfileLoggingAction(string operationType, LogLevel logLevel, LogTag logTag, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }

            using var session = new LoggingProfilerSession(
                LoggingProfilerTags.ForGenericOperation(operationType, logLevel),
                operationType,
                logLevel,
                logTag,
                1,
                0,
                null,
                null,
                null,
                _messageBus);

            action.Invoke();
        }

        /// <summary>
        /// Profiles a logging action with timing (with string tag for backward compatibility).
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="tag">Log tag as string.</param>
        /// <param name="action">Action to profile.</param>
        public void ProfileLoggingAction(string operationType, LogLevel logLevel, string tag, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }

            using var session = new LoggingProfilerSession(
                LoggingProfilerTags.ForGenericOperation(operationType, logLevel),
                operationType,
                logLevel,
                tag,
                1,
                0,
                null,
                null,
                null,
                _messageBus);

            action.Invoke();
        }

        /// <summary>
        /// Begins a generic logging profiling scope.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="logTag">Log tag for categorization.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginGenericLoggingScope(string operationType, LogLevel logLevel, LogTag logTag)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType);

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForGenericOperation(operationType, logLevel),
                operationType,
                logLevel,
                logTag,
                1,
                0,
                null,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Begins a generic logging profiling scope (with string tag for backward compatibility).
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="tag">Log tag as string.</param>
        /// <returns>Profiler session for the operation.</returns>
        public LoggingProfilerSession BeginGenericLoggingScope(string operationType, LogLevel logLevel, string tag)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType);

            return new LoggingProfilerSession(
                LoggingProfilerTags.ForGenericOperation(operationType, logLevel),
                operationType,
                logLevel,
                tag,
                1,
                0,
                null,
                null,
                null,
                _messageBus);
        }

        /// <summary>
        /// Registers an alert for when operations at a specific log level exceed a threshold.
        /// </summary>
        /// <param name="logLevel">Log level to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds.</param>
        public void RegisterLogLevelAlert(LogLevel logLevel, double thresholdMs)
        {
            _logLevelAlerts[logLevel] = thresholdMs;
        }

        /// <summary>
        /// Registers an alert for when operations on a specific target exceed a threshold.
        /// </summary>
        /// <param name="targetName">Target name to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds.</param>
        public void RegisterTargetAlert(string targetName, double thresholdMs)
        {
            if (!string.IsNullOrEmpty(targetName))
            {
                _targetAlerts[targetName] = thresholdMs;
            }
        }

        /// <summary>
        /// Gets logging metrics for a specific operation type.
        /// </summary>
        /// <param name="operationType">Operation type to get metrics for.</param>
        /// <returns>Logging metrics data, or null if not found.</returns>
        public LoggingMetricsData? GetLoggingMetrics(string operationType)
        {
            return _loggingMetrics.TryGetValue(operationType, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Gets all logging metrics.
        /// </summary>
        /// <returns>Dictionary of all logging metrics by operation type.</returns>
        public IReadOnlyDictionary<string, LoggingMetricsData> GetAllLoggingMetrics()
        {
            return _loggingMetrics;
        }

        /// <summary>
        /// Gets a performance snapshot for all logging operations.
        /// </summary>
        /// <returns>Dictionary containing performance metrics.</returns>
        public Dictionary<string, string> GetLoggingPerformanceSnapshot()
        {
            var snapshot = new Dictionary<string, string>();
            
            foreach (var kvp in _loggingMetrics)
            {
                var operationType = kvp.Key;
                var metrics = kvp.Value;
                
                snapshot[$"{operationType}_TotalMessages"] = metrics.TotalMessagesProcessed.ToString();
                snapshot[$"{operationType}_FailedMessages"] = metrics.TotalMessagesFailed.ToString();
                snapshot[$"{operationType}_SuccessRate"] = $"{metrics.SuccessRate:F2}%";
                snapshot[$"{operationType}_AvgProcessingTime"] = $"{metrics.AverageProcessingTimeMs:F3}ms";
                snapshot[$"{operationType}_PeakProcessingTime"] = $"{metrics.PeakProcessingTimeMs:F3}ms";
                snapshot[$"{operationType}_CurrentQueueSize"] = metrics.CurrentQueueSize.ToString();
                snapshot[$"{operationType}_PeakQueueSize"] = metrics.PeakQueueSize.ToString();
                snapshot[$"{operationType}_MemoryUsage"] = FormatByteSize(metrics.MemoryUsageBytes);
                snapshot[$"{operationType}_PeakMemoryUsage"] = FormatByteSize(metrics.PeakMemoryUsageBytes);
            }
            
            return snapshot;
        }

        /// <summary>
        /// Gets the current time for logging operations.
        /// </summary>
        /// <returns>Current time in seconds.</returns>
        private float GetCurrentTime()
        {
            return UnityEngine.Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Subscribes to relevant message bus events.
        /// </summary>
        private void SubscribeToMessages()
        {
            if (_messageBus == null) return;

            try
            {
                var subscriber = _messageBus.GetSubscriber<LogEntryWrittenMessage>();
                subscriber?.Subscribe(OnLogEntryWritten);

                var levelSubscriber = _messageBus.GetSubscriber<LogLevelChangedMessage>();
                levelSubscriber?.Subscribe(OnLogLevelChanged);

                var sessionSubscriber = _messageBus.GetSubscriber<LoggingProfilerSessionCompletedMessage>();
                sessionSubscriber?.Subscribe(OnLoggingSessionCompleted);
            }
            catch
            {
                // Silently handle subscription errors
            }
        }

        /// <summary>
        /// Handles log entry written events.
        /// </summary>
        /// <param name="message">Log entry written message.</param>
        private void OnLogEntryWritten(LogEntryWrittenMessage message)
        {
            if (message?.Tag != null)
            {
                // Convert string tag to LogTag enum for processing
                var logTag = AhBearStudios.Core.Logging.Tags.Tagging.GetLogTag(message.Tag);
                UpdateLoggingMetrics("LogEntryWritten", logTag, message.Level, 1, message.MessageLength ?? 0);
            }
        }

        /// <summary>
        /// Handles log level changed events.
        /// </summary>
        /// <param name="message">Log level changed message.</param>
        private void OnLogLevelChanged(LogLevelChangedMessage message)
        {
            UpdateLoggingMetrics("LogLevelChanged", LogTag.Performance, message.NewLevel, 1, 0);
        }

        /// <summary>
        /// Handles logging session completed events.
        /// </summary>
        /// <param name="message">Logging session completed message.</param>
        private void OnLoggingSessionCompleted(LoggingProfilerSessionCompletedMessage message)
        {
            var processingTime = TimeSpan.FromMilliseconds(message.DurationMs);
            UpdateLoggingMetrics(message.OperationType, message.LogTag, message.LogLevel, 
                               message.MessageCount, message.MessageLength, processingTime);
            CheckAlerts(message);
        }

        /// <summary>
        /// Creates a null session that doesn't perform any actual profiling.
        /// </summary>
        /// <param name="operationType">Operation type for the null session.</param>
        /// <returns>Null logging profiler session.</returns>
        private LoggingProfilerSession CreateNullSession(string operationType)
        {
            return new LoggingProfilerSession(
                LoggingProfilerTags.ForGenericOperation(operationType),
                operationType,
                LogLevel.Info,
                LogTag.Default,
                1,
                0,
                null,
                null,
                null,
                null);
        }

        /// <summary>
        /// Updates logging metrics for a specific operation.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logTag">Log tag for the operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="messageCount">Number of messages processed.</param>
        /// <param name="messageSize">Size of messages in bytes.</param>
        /// <param name="processingTime">Processing time for the operation.</param>
        private void UpdateLoggingMetrics(string operationType, LogTag logTag, LogLevel logLevel, 
                                        int messageCount, int messageSize = 0, TimeSpan? processingTime = null)
        {
            if (!_loggingMetrics.TryGetValue(operationType, out var metrics))
            {
                var systemId = new FixedString64Bytes($"LogProf_{operationType}");
                var systemName = new FixedString128Bytes($"LoggingProfiler_{operationType}");
                metrics = new LoggingMetricsData(systemId, systemName, GetCurrentTime());
                _loggingMetrics[operationType] = metrics;
            }

            // Update metrics using the LoggingMetricsData methods
            if (processingTime.HasValue)
            {
                // Record message processing with actual timing data
                for (int i = 0; i < messageCount; i++)
                {
                    metrics = metrics.RecordMessageProcessing(logLevel, logTag, processingTime.Value, true, messageSize);
                }
            }
            else
            {
                // Record without timing (for events without duration)
                metrics = metrics.RecordMessageProcessing(logLevel, logTag, TimeSpan.Zero, true, messageSize);
            }

            // Update the stored metrics
            _loggingMetrics[operationType] = metrics.UpdateOperationTime(GetCurrentTime());
        }

        /// <summary>
        /// Checks if any alerts should be triggered for the completed session.
        /// </summary>
        /// <param name="message">Completed session message.</param>
        private void CheckAlerts(LoggingProfilerSessionCompletedMessage message)
        {
            // Check log level alerts
            if (_logLevelAlerts.TryGetValue(message.LogLevel, out var levelThreshold) &&
                message.DurationMs > levelThreshold)
            {
                // Trigger log level alert
                // TODO: Implement alert publishing through message bus
            }

            // Check target alerts
            if (!string.IsNullOrEmpty(message.TargetName) &&
                _targetAlerts.TryGetValue(message.TargetName, out var targetThreshold) &&
                message.DurationMs > targetThreshold)
            {
                // Trigger target alert
                // TODO: Implement alert publishing through message bus
            }
        }

        /// <summary>
        /// Formats byte size for human-readable display.
        /// </summary>
        /// <param name="bytes">Size in bytes.</param>
        /// <returns>Formatted size string.</returns>
        private string FormatByteSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:F2} {sizes[order]}";
        }

        /// <summary>
        /// Disposes the profiler and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _loggingMetrics.Clear();
            _logLevelAlerts.Clear();
            _targetAlerts.Clear();

            _isDisposed = true;
        }

        // Message classes for event handling - these would typically be defined elsewhere
        private class LogEntryWrittenMessage
        {
            public LogLevel Level { get; set; }
            public string Tag { get; set; }
            public int? MessageLength { get; set; }
        }

        private class LogLevelChangedMessage
        {
            public LogLevel NewLevel { get; set; }
        }
    }
}