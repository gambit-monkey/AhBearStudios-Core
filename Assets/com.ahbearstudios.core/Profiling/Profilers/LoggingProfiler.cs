using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging;
using Unity.Profiling;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Tagging;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Specialized profiler for logging operations that captures logging-specific metrics.
    /// This profiler tracks log message processing, target writes, queue flushes, and other logging operations.
    /// </summary>
    public sealed class LoggingProfiler : IProfiler, IDisposable
    {
        #region Private Fields
        
        private readonly IProfiler _baseProfiler;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<string, LoggingMetricsData> _loggingMetricsCache = new Dictionary<string, LoggingMetricsData>();
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<LogLevel, double> _logLevelAlerts = new Dictionary<LogLevel, double>();
        private readonly Dictionary<string, double> _targetAlerts = new Dictionary<string, double>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        
        private const int MaxHistoryItems = 100;
        private bool _disposed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Creates a new logging profiler.
        /// </summary>
        /// <param name="baseProfiler">Base profiler implementation for general profiling.</param>
        /// <param name="messageBus">Message bus for publishing and subscribing to profiling messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public LoggingProfiler(IProfiler baseProfiler, IMessageBus messageBus)
        {
            _baseProfiler = baseProfiler ?? throw new ArgumentNullException(nameof(baseProfiler));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            SubscribeToMessages();
        }
        
        #endregion
        
        #region IProfiler Implementation
        
        /// <summary>
        /// Gets whether profiling is enabled.
        /// </summary>
        public bool IsEnabled => _baseProfiler.IsEnabled;

        /// <summary>
        /// Gets the message bus used by this profiler.
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Begin a profiling sample with a name.
        /// </summary>
        /// <param name="name">Name of the profiler sample.</param>
        /// <returns>Profiler session that should be disposed when sample ends.</returns>
        public IDisposable BeginSample(string name)
        {
            return _baseProfiler.BeginSample(name);
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag.
        /// </summary>
        /// <param name="tag">Profiler tag for this scope.</param>
        /// <returns>Profiler session that should be disposed when scope ends.</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _baseProfiler.BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name.
        /// </summary>
        /// <param name="category">Category for this scope.</param>
        /// <param name="name">Name for this scope.</param>
        /// <returns>Profiler session that should be disposed when scope ends.</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _baseProfiler.BeginScope(category, name);
        }

        /// <summary>
        /// Get metrics for a specific profiling tag.
        /// </summary>
        /// <param name="tag">The tag to get metrics for.</param>
        /// <returns>Profile metrics for the tag.</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _baseProfiler.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics.
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag.</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _baseProfiler.GetAllMetrics();
        }

        /// <summary>
        /// Get history for a specific profiling tag.
        /// </summary>
        /// <param name="tag">The tag to get history for.</param>
        /// <returns>List of historical durations.</returns>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            if (_history.TryGetValue(tag, out var history))
                return history;

            return Array.Empty<double>();
        }

        /// <summary>
        /// Register a system metric threshold alert.
        /// </summary>
        /// <param name="metricTag">Tag for the metric to monitor.</param>
        /// <param name="threshold">Threshold value to trigger alert.</param>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold)
        {
            _baseProfiler.RegisterMetricAlert(metricTag, threshold);
        }

        /// <summary>
        /// Register a session threshold alert.
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert.</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _baseProfiler.RegisterSessionAlert(sessionTag, thresholdMs);
        }

        /// <summary>
        /// Reset all profiling stats.
        /// </summary>
        public void ResetStats()
        {
            _baseProfiler.ResetStats();
            _history.Clear();
            _loggingMetricsCache.Clear();
        }

        /// <summary>
        /// Start profiling.
        /// </summary>
        public void StartProfiling()
        {
            _baseProfiler.StartProfiling();
        }

        /// <summary>
        /// Stop profiling.
        /// </summary>
        public void StopProfiling()
        {
            _baseProfiler.StopProfiling();
        }
        
        #endregion
        
        #region Logging-Specific Profiling Methods
        
        /// <summary>
        /// Begin a specialized logging profiling session for message processing.
        /// </summary>
        /// <param name="logLevel">Log level of the message being processed.</param>
        /// <param name="tag">Log tag of the message.</param>
        /// <param name="messageLength">Length of the log message.</param>
        /// <returns>Logging profiler session.</returns>
        public LoggingProfilerSession BeginMessageProcessingScope(LogLevel logLevel, string tag, int messageLength = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("MessageProcessing");

            var profilerTag = LoggingProfilerTags.ForMessageProcessing(logLevel, tag);
            return new LoggingProfilerSession(
                profilerTag,
                "MessageProcessing",
                logLevel,
                tag,
                messageLength,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized logging profiling session for target writes.
        /// </summary>
        /// <param name="targetName">Name of the log target.</param>
        /// <param name="logLevel">Log level being written.</param>LoggingProfilerTags
        /// <param name="messageCount">Number of messages being written.</param>
        /// <returns>Logging profiler session.</returns>
        public LoggingProfilerSession BeginTargetWriteScope(string targetName, LogLevel logLevel, int messageCount = 1)
        {
            if (!IsEnabled)
                return CreateNullSession("TargetWrite");

            var tag = LoggingProfilerTags.ForTargetWrite(targetName, logLevel);
            return new LoggingProfilerSession(
                tag,
                "TargetWrite",
                logLevel,
                targetName,
                messageCount,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized logging profiling session for queue flush operations.
        /// </summary>
        /// <param name="messageCount">Number of messages being flushed.</param>
        /// <param name="targetCount">Number of targets being flushed to.</param>
        /// <returns>Logging profiler session.</returns>
        public LoggingProfilerSession BeginQueueFlushScope(int messageCount, int targetCount = 1)
        {
            if (!IsEnabled)
                return CreateNullSession("QueueFlush");

            var tag = LoggingProfilerTags.ForQueueFlush(messageCount);
            return new LoggingProfilerSession(
                tag,
                "QueueFlush",
                LogLevel.Info, // Default level for flush operations
                "Flush",
                messageCount,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized logging profiling session for log level changes.
        /// </summary>
        /// <param name="oldLevel">Previous log level.</param>
        /// <param name="newLevel">New log level.</param>
        /// <returns>Logging profiler session.</returns>
        public LoggingProfilerSession BeginLevelChangeScope(LogLevel oldLevel, LogLevel newLevel)
        {
            if (!IsEnabled)
                return CreateNullSession("LevelChange");

            var tag = LoggingProfilerTags.ForLevelChange(oldLevel, newLevel);
            return new LoggingProfilerSession(
                tag,
                "LevelChange",
                newLevel,
                $"{oldLevel}To{newLevel}",
                0,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized logging profiling session for formatter operations.
        /// </summary>
        /// <param name="formatterName">Name of the log formatter.</param>
        /// <param name="logLevel">Log level being formatted.</param>
        /// <param name="messageLength">Length of the message being formatted.</param>
        /// <returns>Logging profiler session.</returns>
        public LoggingProfilerSession BeginFormatterScope(string formatterName, LogLevel logLevel, int messageLength = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Formatter");

            var tag = LoggingProfilerTags.ForFormatter(formatterName, logLevel);
            return new LoggingProfilerSession(
                tag,
                "Formatter",
                logLevel,
                formatterName,
                messageLength,
                _messageBus
            );
        }

        /// <summary>
        /// Profiles a logging action with automatic session management.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <param name="logLevel">Log level for the operation.</param>
        /// <param name="tag">Tag for the operation.</param>
        /// <param name="action">Action to profile.</param>
        public void ProfileLoggingAction(string operationType, LogLevel logLevel, string tag, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }

            using (BeginGenericLoggingScope(operationType, logLevel, tag))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Begin a generic logging profiling session.
        /// </summary>
        /// <param name="operationType">Type of operation.</param>
        /// <param name="logLevel">Log level.</param>
        /// <param name="tag">Operation tag.</param>
        /// <returns>Logging profiler session.</returns>
        public LoggingProfilerSession BeginGenericLoggingScope(string operationType, LogLevel logLevel, string tag)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType);

            var profilerTag = LoggingProfilerTags.ForGenericOperation(operationType, logLevel);
            return new LoggingProfilerSession(
                profilerTag,
                operationType,
                logLevel,
                tag,
                0,
                _messageBus
            );
        }
        
        #endregion
        
        #region Logging-Specific Alert Registration
        
        /// <summary>
        /// Register a log level threshold alert.
        /// </summary>
        /// <param name="logLevel">Log level to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert.</param>
        public void RegisterLogLevelAlert(LogLevel logLevel, double thresholdMs)
        {
            if (thresholdMs <= 0)
                return;
                
            _logLevelAlerts[logLevel] = thresholdMs;
        }

        /// <summary>
        /// Register a log target threshold alert.
        /// </summary>
        /// <param name="targetName">Name of the log target to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert.</param>
        public void RegisterTargetAlert(string targetName, double thresholdMs)
        {
            if (string.IsNullOrEmpty(targetName) || thresholdMs <= 0)
                return;
                
            _targetAlerts[targetName] = thresholdMs;
        }
        
        #endregion
        
        #region Logging Metrics
        
        /// <summary>
        /// Get logging metrics for a specific operation type.
        /// </summary>
        /// <param name="operationType">Type of logging operation.</param>
        /// <returns>Logging metrics data if available.</returns>
        public LoggingMetricsData? GetLoggingMetrics(string operationType)
        {
            if (string.IsNullOrEmpty(operationType))
                return null;
                
            if (_loggingMetricsCache.TryGetValue(operationType, out var metrics))
                return metrics;

            return null;
        }

        /// <summary>
        /// Get all logging metrics.
        /// </summary>
        /// <returns>Dictionary of logging metrics by operation type.</returns>
        public IReadOnlyDictionary<string, LoggingMetricsData> GetAllLoggingMetrics()
        {
            return new Dictionary<string, LoggingMetricsData>(_loggingMetricsCache);
        }
        
        #endregion
        
        #region Message Subscription
        
        /// <summary>
        /// Subscribes to logging-related messages from the message bus.
        /// </summary>
        private void SubscribeToMessages()
        {
            try
            {
                // Subscribe to log entry written messages
                var entryWrittenSub = _messageBus.SubscribeToMessage<LogEntryWrittenMessage>(OnLogEntryWritten);
                if (entryWrittenSub != null)
                    _subscriptions.Add(entryWrittenSub);
                
                // Subscribe to log level changed messages
                var levelChangedSub = _messageBus.SubscribeToMessage<LogLevelChangedMessage>(OnLogLevelChanged);
                if (levelChangedSub != null)
                    _subscriptions.Add(levelChangedSub);
                
                // Subscribe to logging profiler session completed messages
                var sessionCompletedSub = _messageBus.SubscribeToMessage<LoggingProfilerSessionCompletedMessage>(OnLoggingSessionCompleted);
                if (sessionCompletedSub != null)
                    _subscriptions.Add(sessionCompletedSub);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail initialization
                UnityEngine.Debug.LogError($"LoggingProfiler: Failed to subscribe to some messages: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Message Handlers
        
        /// <summary>
        /// Handles log entry written messages.
        /// </summary>
        private void OnLogEntryWritten(LogEntryWrittenMessage message)
        {
            if (!IsEnabled)
                return;

            // Update metrics cache for the target
            var targetName = message.TargetName ?? "Unknown";
            var logLevel = (LogLevel)message.LogMessage.Level;
    
            UpdateLoggingMetrics("TargetWrite", targetName, logLevel, 1);
    
            // Check target alerts
            if (_targetAlerts.TryGetValue(targetName, out var threshold))
            {
                // We don't have timing info from this message, so we can't check thresholds here
                // This would typically be handled by the LoggingProfilerSession
            }
        }

        /// <summary>
        /// Handles log level changed messages.
        /// </summary>
        private void OnLogLevelChanged(LogLevelChangedMessage message)
        {
            if (!IsEnabled)
                return;

            UpdateLoggingMetrics("LevelChange", $"{message.OldLevel}To{message.NewLevel}", message.NewLevel, 1);
        }

        /// <summary>
        /// Handles logging profiler session completed messages.
        /// </summary>
        private void OnLoggingSessionCompleted(LoggingProfilerSessionCompletedMessage message)
        {
            if (!IsEnabled)
                return;

            // Update history
            if (!_history.TryGetValue(message.Tag, out var history))
            {
                history = new List<double>(MaxHistoryItems);
                _history[message.Tag] = history;
            }

            if (history.Count >= MaxHistoryItems)
                history.RemoveAt(0);

            history.Add(message.DurationMs);

            // Update metrics cache
            UpdateLoggingMetrics(message.OperationType, message.LogTag, message.LogLevel, message.MessageCount);

            // Check alerts
            CheckAlerts(message);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Creates a null session for when profiling is disabled.
        /// </summary>
        private LoggingProfilerSession CreateNullSession(string operationType)
        {
            return new LoggingProfilerSession(
                ProfilerTag.Uncategorized,
                operationType,
                LogLevel.Info,
                "Null",
                0,
                null
            );
        }

        /// <summary>
        /// Updates logging metrics in the cache.
        /// </summary>
        private void UpdateLoggingMetrics(string operationType, string tag, LogLevel logLevel, int messageCount)
        {
            if (string.IsNullOrEmpty(operationType))
                return;

            if (!_loggingMetricsCache.TryGetValue(operationType, out var metrics))
            {
                metrics = new LoggingMetricsData();
                _loggingMetricsCache[operationType] = metrics;
            }

            // Update metrics (this would need to be implemented based on LoggingMetricsData structure)
            // For now, this is a placeholder
        }

        /// <summary>
        /// Checks alerts for a completed logging session.
        /// </summary>
        private void CheckAlerts(LoggingProfilerSessionCompletedMessage message)
        {
            // Check log level alerts
            if (_logLevelAlerts.TryGetValue(message.LogLevel, out var levelThreshold) && 
                message.DurationMs > levelThreshold)
            {
                var alertMessage = new LoggingAlertMessage(
                    message.Tag,
                    message.LogLevel,
                    message.LogTag,
                    message.DurationMs,
                    levelThreshold,
                    "LogLevel"
                );
                _messageBus.PublishMessage(alertMessage);
            }

            // Check target alerts
            if (_targetAlerts.TryGetValue(message.LogTag, out var targetThreshold) && 
                message.DurationMs > targetThreshold)
            {
                var alertMessage = new LoggingAlertMessage(
                    message.Tag,
                    message.LogLevel,
                    message.LogTag,
                    message.DurationMs,
                    targetThreshold,
                    "Target"
                );
                _messageBus.PublishMessage(alertMessage);
            }
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes of resources and unsubscribes from messages.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Unsubscribe from all message bus subscriptions
                foreach (var subscription in _subscriptions)
                {
                    try
                    {
                        subscription?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"LoggingProfiler: Error disposing subscription: {ex.Message}");
                    }
                }

                _subscriptions.Clear();
                _history.Clear();
                _loggingMetricsCache.Clear();
                _logLevelAlerts.Clear();
                _targetAlerts.Clear();
            }
            finally
            {
                _disposed = true;
            }
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Specialized profiler session for logging operations.
    /// </summary>
    public sealed class LoggingProfilerSession : IDisposable
    {
        private readonly ProfilerTag _tag;
        private readonly string _operationType;
        private readonly LogLevel _logLevel;
        private readonly string _logTag;
        private readonly int _messageCount;
        private readonly IMessageBus _messageBus;
        private readonly System.Diagnostics.Stopwatch _stopwatch;
        private bool _disposed;

        /// <summary>
        /// Creates a new logging profiler session.
        /// </summary>
        public LoggingProfilerSession(
            ProfilerTag tag,
            string operationType,
            LogLevel logLevel,
            string logTag,
            int messageCount,
            IMessageBus messageBus)
        {
            _tag = tag;
            _operationType = operationType;
            _logLevel = logLevel;
            _logTag = logTag;
            _messageCount = messageCount;
            _messageBus = messageBus;
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets the elapsed time in milliseconds.
        /// </summary>
        public double ElapsedMilliseconds => _stopwatch.Elapsed.TotalMilliseconds;

        /// <summary>
        /// Disposes the session and publishes completion message.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _stopwatch.Stop();

            if (_messageBus != null)
            {
                var completionMessage = new LoggingProfilerSessionCompletedMessage(
                    _tag,
                    _operationType,
                    _logLevel,
                    _logTag,
                    _messageCount,
                    _stopwatch.Elapsed.TotalMilliseconds
                );
                
                try
                {
                    _messageBus.PublishMessage(completionMessage);
                }
                catch
                {
                    // Silently handle publication errors
                }
            }

            _disposed = true;
        }
    }
    
    /// <summary>
    /// Logging metrics data structure.
    /// </summary>
    public struct LoggingMetricsData
    {
        public long TotalOperations;
        public double AverageTimeMs;
        public double MinTimeMs;
        public double MaxTimeMs;
        public long TotalMessageCount;
        public Dictionary<LogLevel, long> MessagesByLevel;
        
        // Constructor and methods would be implemented here
    }
    
    #endregion
}