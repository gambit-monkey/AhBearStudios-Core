using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Sessions;
using AhBearStudios.Core.Profiling.Tagging;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Utilities;
using Unity.Collections;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Specialized profiler for message bus operations that captures message bus-specific metrics.
    /// Implements intelligent tag selection and provides comprehensive message bus profiling capabilities.
    /// </summary>
    public sealed class MessageBusProfiler : IProfiler, IDisposable
    {
        #region Private Fields
        
        private readonly IProfiler _baseProfiler;
        private readonly IMessageBusMetrics _busMetrics;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<Guid, MessageBusMetricsData> _busMetricsCache = new Dictionary<Guid, MessageBusMetricsData>();
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<Guid, Dictionary<string, double>> _busMetricAlerts = new Dictionary<Guid, Dictionary<string, double>>();
        private readonly Dictionary<string, double> _messageTypeAlerts = new Dictionary<string, double>();
        private readonly Dictionary<string, double> _operationAlerts = new Dictionary<string, double>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        
        private const int MaxHistoryItems = 100;
        private bool _disposed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Creates a new message bus profiler.
        /// </summary>
        /// <param name="baseProfiler">Base profiler implementation for general profiling.</param>
        /// <param name="busMetrics">Message bus metrics service.</param>
        /// <param name="messageBus">Message bus for publishing and subscribing to profiling messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public MessageBusProfiler(IProfiler baseProfiler, IMessageBusMetrics busMetrics, IMessageBus messageBus)
        {
            _baseProfiler = baseProfiler ?? throw new ArgumentNullException(nameof(baseProfiler));
            _busMetrics = busMetrics ?? throw new ArgumentNullException(nameof(busMetrics));
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
            _busMetricsCache.Clear();
            _busMetrics.ResetStats();
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
        
        #region Message Bus-Specific Profiling Methods

        /// <summary>
        /// Begin a specialized message bus profiling session using intelligent tag selection.
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="busName">Name of the message bus</param>
        /// <param name="subscriberCount">Number of subscribers</param>
        /// <param name="queueSize">Current queue size</param>
        /// <param name="messageType">Type of message (optional)</param>
        /// <returns>Message bus profiler session</returns>
        public MessageBusProfilerSession BeginMessageBusScope(
            string operationType,
            Guid busId,
            string busName,
            int subscriberCount = 0,
            int queueSize = 0,
            string messageType = null)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, busId, busName);

            return MessageBusProfilerSession.Create(
                operationType, busId, busName, subscriberCount, queueSize, messageType, _busMetrics, _messageBus);
        }
        
        /// <summary>
        /// Begin a specialized message bus profiling session for message publishing.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being published.</param>
        /// <param name="subscriberCount">Number of subscribers for this message type.</param>
        /// <param name="queueSize">Current queue size.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginPublishScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int subscriberCount, 
            int queueSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Publish", busId, busName);

            return MessageBusProfilerSession.Create(
                "Publish", busId, busName, subscriberCount, queueSize, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for message delivery.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being delivered.</param>
        /// <param name="subscriberCount">Number of subscribers receiving the message.</param>
        /// <param name="queueSize">Current queue size.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginDeliveryScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int subscriberCount, 
            int queueSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Deliver", busId, busName);

            return MessageBusProfilerSession.Create(
                "Deliver", busId, busName, subscriberCount, queueSize, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for subscription operations.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being subscribed to.</param>
        /// <param name="subscriberCount">Total number of subscribers after this operation.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginSubscribeScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int subscriberCount)
        {
            if (!IsEnabled)
                return CreateNullSession("Subscribe", busId, busName);

            return MessageBusProfilerSession.Create(
                "Subscribe", busId, busName, subscriberCount, 0, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for unsubscription operations.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being unsubscribed from.</param>
        /// <param name="subscriberCount">Total number of subscribers after this operation.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginUnsubscribeScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int subscriberCount)
        {
            if (!IsEnabled)
                return CreateNullSession("Unsubscribe", busId, busName);

            return MessageBusProfilerSession.Create(
                "Unsubscribe", busId, busName, subscriberCount, 0, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for message processing.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being processed.</param>
        /// <param name="batchSize">Number of messages being processed in this batch.</param>
        /// <param name="queueSize">Current queue size.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginProcessScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int batchSize, 
            int queueSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Process", busId, busName);

            return MessageBusProfilerSession.Create(
                "Process", busId, busName, batchSize, queueSize, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for queue operations.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="operationType">Type of queue operation (enqueue, dequeue, flush).</param>
        /// <param name="messageCount">Number of messages in the operation.</param>
        /// <param name="queueSize">Current queue size.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginQueueScope(
            Guid busId, 
            string busName, 
            string operationType, 
            int messageCount, 
            int queueSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Queue", busId, busName);

            return MessageBusProfilerSession.Create(
                $"Queue.{operationType}", busId, busName, messageCount, queueSize, "Queue", _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for batch operations.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being batched.</param>
        /// <param name="batchSize">Size of the batch.</param>
        /// <param name="queueSize">Current queue size.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginBatchScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int batchSize, 
            int queueSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("Batch", busId, busName);

            return MessageBusProfilerSession.Create(
                "Batch", busId, busName, batchSize, queueSize, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a specialized message bus profiling session for reliable delivery.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message being delivered reliably.</param>
        /// <param name="subscriberCount">Number of subscribers.</param>
        /// <param name="retryCount">Current retry attempt count.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginReliableDeliveryScope(
            Guid busId, 
            string busName, 
            string messageType, 
            int subscriberCount, 
            int retryCount = 0)
        {
            if (!IsEnabled)
                return CreateNullSession("ReliableDeliver", busId, busName);

            var session = MessageBusProfilerSession.Create(
                "ReliableDeliver", busId, busName, subscriberCount, 0, messageType, _busMetrics, _messageBus);
            
            // Record initial retry count
            session.RecordMetric("RetryCount", retryCount);
            
            return session;
        }

        /// <summary>
        /// Begin a lightweight message bus profiling session with minimal parameters.
        /// </summary>
        /// <param name="operationType">Type of operation.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message (optional).</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginLightweightBusScope(
            string operationType,
            string busName,
            string messageType = null)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, Guid.Empty, busName);

            return MessageBusProfilerSession.CreateMinimal(
                operationType, busName, messageType, _busMetrics, _messageBus);
        }

        /// <summary>
        /// Begin a generic message bus profiling session using predefined tags.
        /// </summary>
        /// <param name="operationType">Type of operation.</param>
        /// <returns>Message bus profiler session.</returns>
        public MessageBusProfilerSession BeginGenericBusScope(string operationType)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, Guid.Empty, "Generic");

            // Use predefined tags for common operations
            ProfilerTag tag;
            switch (operationType.ToLowerInvariant())
            {
                case "publish":
                    tag = MessageBusProfilerTags.MessagePublish;
                    break;
                case "deliver":
                    tag = MessageBusProfilerTags.MessageDeliver;
                    break;
                case "subscribe":
                    tag = MessageBusProfilerTags.MessageSubscribe;
                    break;
                case "unsubscribe":
                    tag = MessageBusProfilerTags.MessageUnsubscribe;
                    break;
                case "process":
                    tag = MessageBusProfilerTags.MessageProcess;
                    break;
                case "queue":
                    tag = MessageBusProfilerTags.MessageQueue;
                    break;
                case "batch":
                    tag = MessageBusProfilerTags.MessageBatch;
                    break;
                case "reliabledeliver":
                    tag = MessageBusProfilerTags.MessageReliableDeliver;
                    break;
                default:
                    tag = MessageBusProfilerTags.ForBusNamed(operationType, "Generic");
                    break;
            }

            return new MessageBusProfilerSession(
                tag, Guid.Empty, "Generic", operationType, 0, 0, "Generic", _busMetrics, _messageBus);
        }

        /// <summary>
        /// Profiles a message bus action with automatic session management.
        /// </summary>
        /// <param name="operationType">Type of message bus operation.</param>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="subscriberCount">Number of subscribers.</param>
        /// <param name="queueSize">Current queue size.</param>
        /// <param name="action">Action to profile.</param>
        public void ProfileBusAction(
            string operationType,
            Guid busId,
            string busName,
            int subscriberCount,
            int queueSize,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }

            using (BeginMessageBusScope(operationType, busId, busName, subscriberCount, queueSize))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a message bus action with message type context.
        /// </summary>
        /// <param name="operationType">Type of message bus operation.</param>
        /// <param name="busName">Name of the message bus.</param>
        /// <param name="messageType">Type of message.</param>
        /// <param name="action">Action to profile.</param>
        public void ProfileBusAction(
            string operationType,
            string busName,
            string messageType,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }

            using (BeginLightweightBusScope(operationType, busName, messageType))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a simple message bus action with minimal context.
        /// </summary>
        /// <param name="operationType">Type of message bus operation.</param>
        /// <param name="action">Action to profile.</param>
        public void ProfileBusAction(string operationType, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }

            using (BeginGenericBusScope(operationType))
            {
                action.Invoke();
            }
        }
        
        #endregion
        
        #region Message Bus-Specific Alert Registration
        
        /// <summary>
        /// Register a message bus metric threshold alert.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <param name="metricName">Name of the message bus metric.</param>
        /// <param name="threshold">Threshold value to trigger alert.</param>
        public void RegisterBusMetricAlert(Guid busId, string metricName, double threshold)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            // Store locally for tracking
            if (!_busMetricAlerts.TryGetValue(busId, out var metricsDict))
            {
                metricsDict = new Dictionary<string, double>();
                _busMetricAlerts[busId] = metricsDict;
            }
            
            metricsDict[metricName] = threshold;
            
            // Forward to the message bus metrics system
            _busMetrics.RegisterAlert(busId, metricName, threshold);
        }

        /// <summary>
        /// Register a message type threshold alert.
        /// </summary>
        /// <param name="messageType">Type of message to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert.</param>
        public void RegisterMessageTypeAlert(string messageType, double thresholdMs)
        {
            if (string.IsNullOrEmpty(messageType) || thresholdMs <= 0)
                return;
                
            _messageTypeAlerts[messageType] = thresholdMs;
            
            // Register with base profiler using the appropriate tag
            var tag = MessageBusProfilerTags.ForMessageType("Deliver", messageType);
            RegisterSessionAlert(tag, thresholdMs);
        }

        /// <summary>
        /// Register an operation type threshold alert.
        /// </summary>
        /// <param name="operationType">Type of operation to monitor.</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert.</param>
        public void RegisterOperationAlert(string operationType, double thresholdMs)
        {
            if (string.IsNullOrEmpty(operationType) || thresholdMs <= 0)
                return;
                
            _operationAlerts[operationType] = thresholdMs;
            
            // Register with base profiler using predefined tags when possible
            ProfilerTag tag;
            switch (operationType.ToLowerInvariant())
            {
                case "publish":
                    tag = MessageBusProfilerTags.MessagePublish;
                    break;
                case "deliver":
                    tag = MessageBusProfilerTags.MessageDeliver;
                    break;
                case "subscribe":
                    tag = MessageBusProfilerTags.MessageSubscribe;
                    break;
                case "unsubscribe":
                    tag = MessageBusProfilerTags.MessageUnsubscribe;
                    break;
                case "process":
                    tag = MessageBusProfilerTags.MessageProcess;
                    break;
                case "queue":
                    tag = MessageBusProfilerTags.MessageQueue;
                    break;
                case "batch":
                    tag = MessageBusProfilerTags.MessageBatch;
                    break;
                case "reliabledeliver":
                    tag = MessageBusProfilerTags.MessageReliableDeliver;
                    break;
                default:
                    tag = MessageBusProfilerTags.ForBusNamed(operationType, "Generic");
                    break;
            }
            
            RegisterSessionAlert(tag, thresholdMs);
        }
        
        #endregion
        
        #region Message Bus Metrics
        
        /// <summary>
        /// Get message bus metrics for a specific bus.
        /// </summary>
        /// <param name="busId">Message bus identifier.</param>
        /// <returns>Message bus metrics data if available.</returns>
        public MessageBusMetricsData? GetMessageBusMetrics(Guid busId)
        {
            if (_busMetricsCache.TryGetValue(busId, out var metrics))
                return metrics;

            var busMetrics = _busMetrics.GetMessageBusMetrics(busId);
            if (busMetrics.HasValue)
            {
                _busMetricsCache[busId] = busMetrics.Value;
                return busMetrics.Value;
            }

            return null;
        }

        /// <summary>
        /// Get all message bus metrics.
        /// </summary>
        /// <returns>Dictionary of message bus metrics by bus identifier.</returns>
        public IReadOnlyDictionary<Guid, MessageBusMetricsData> GetAllMessageBusMetrics()
        {
            // Clear cache to ensure we get fresh data
            _busMetricsCache.Clear();
            
            // Get all bus metrics from the metrics service
            var allBusMetrics = _busMetrics.GetAllBusMetrics();
            
            // Cache the metrics for future use
            foreach (var busMetrics in allBusMetrics)
            {
                if (busMetrics.BusId != default)
                {
                    // Convert FixedString64Bytes to Guid (this would need proper conversion logic)
                    var busId = busMetrics.BusId.ToGuid();
                    _busMetricsCache[busId] = busMetrics;
                }
            }
            
            return new Dictionary<Guid, MessageBusMetricsData>(_busMetricsCache);
        }
        
        #endregion
        
        #region Private Helper Methods

        /// <summary>
        /// Creates a null/disabled session for when profiling is disabled
        /// </summary>
        private MessageBusProfilerSession CreateNullSession(string operationType, Guid busId, string busName)
        {
            return new MessageBusProfilerSession(
                MessageBusProfilerTags.ForBusNamed(operationType, "Disabled"),
                busId,
                busName ?? string.Empty,
                operationType,
                0,
                0,
                string.Empty,
                null,
                null
            );
        }
        
        #endregion
        
        #region Message Subscription
        
        /// <summary>
        /// Subscribes to message bus-related messages from the message bus.
        /// </summary>
        private void SubscribeToMessages()
        {
            try
            {
                // Subscribe to message bus profiler session completed messages
                var sessionCompletedSub = _messageBus.GetSubscriber<MessageBusProfilerSessionCompletedMessage>();
                if (sessionCompletedSub != null)
                {
                    sessionCompletedSub.Subscribe(OnMessageBusSessionCompleted);
                }
                
                // Subscribe to message bus alert messages
                var alertSub = _messageBus.GetSubscriber<MessageBusAlertMessage>();
                if (alertSub != null)
                {
                    alertSub.Subscribe(OnMessageBusAlert);
                }
                
                // Subscribe to message bus performance messages
                var performanceSub = _messageBus.GetSubscriber<MessageBusPerformanceMessage>();
                if (performanceSub != null)
                {
                    performanceSub.Subscribe(OnMessageBusPerformance);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail initialization
                UnityEngine.Debug.LogError($"MessageBusProfiler: Failed to subscribe to some messages: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Message Handlers
        
        /// <summary>
        /// Handles message bus profiler session completed messages.
        /// </summary>
        private void OnMessageBusSessionCompleted(MessageBusProfilerSessionCompletedMessage message)
        {
            if (!IsEnabled)
                return;

            // Update history
            var tag = message.Tag;
            if (!_history.TryGetValue(tag, out var history))
            {
                history = new List<double>(MaxHistoryItems);
                _history[tag] = history;
            }

            if (history.Count >= MaxHistoryItems)
                history.RemoveAt(0);

            history.Add(message.DurationMs);
            
            // Invalidate the cache entry for this bus to ensure fresh data next time
            if (message.BusId != Guid.Empty)
            {
                _busMetricsCache.Remove(message.BusId);
            }

            // Check operation-specific alerts
            if (_operationAlerts.TryGetValue(message.OperationType, out var opThreshold) && 
                message.DurationMs > opThreshold)
            {
                // Log or handle operation threshold exceeded
                UnityEngine.Debug.LogWarning($"MessageBus operation '{message.OperationType}' exceeded threshold: {message.DurationMs}ms > {opThreshold}ms");
            }

            // Check message type-specific alerts
            if (_messageTypeAlerts.TryGetValue(message.MessageType, out var msgThreshold) && 
                message.DurationMs > msgThreshold)
            {
                // Log or handle message type threshold exceeded
                UnityEngine.Debug.LogWarning($"MessageBus message type '{message.MessageType}' exceeded threshold: {message.DurationMs}ms > {msgThreshold}ms");
            }
        }
        
        /// <summary>
        /// Handles message bus alert messages.
        /// </summary>
        private void OnMessageBusAlert(MessageBusAlertMessage message)
        {
            if (!IsEnabled)
                return;
                
            // Invalidate the cache entry for this bus to ensure fresh data next time
            if (message.BusId != Guid.Empty)
            {
                _busMetricsCache.Remove(message.BusId);
            }
        }
        
        /// <summary>
        /// Handles message bus performance messages.
        /// </summary>
        private void OnMessageBusPerformance(MessageBusPerformanceMessage message)
        {
            if (!IsEnabled)
                return;
                
            // Update metrics or handle performance data as needed
            // This could be used to adjust thresholds or trigger additional profiling
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Dispose of resources and unsubscribe from messages.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Dispose of any subscriptions
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();

            // Clear caches
            _busMetricsCache.Clear();
            _history.Clear();
            _busMetricAlerts.Clear();
            _messageTypeAlerts.Clear();
            _operationAlerts.Clear();
        }
        
        #endregion
    }
}