using System;
using System.Collections.Generic;
using Unity.Profiling;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Sessions;
using AhBearStudios.Core.Profiling.Tagging;

namespace AhBearStudios.Core.Profiling.Profilers
{
    /// <summary>
    /// Specialized profiler for serialization operations that captures serialization-specific metrics.
    /// Implements intelligent tag selection and provides comprehensive serialization profiling capabilities.
    /// </summary>
    public class SerializationProfilerService : IProfilerService
    {
        private readonly IProfilerService _baseProfilerService;
        private readonly ISerializerMetrics _serializerMetrics;
        private readonly IMessageBusService _messageBusService;
        private readonly Dictionary<Guid, SerializerMetricsData> _serializerMetricsCache = new Dictionary<Guid, SerializerMetricsData>();
        private readonly int _maxHistoryItems = 100;
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<string, double> _serializerMetricAlerts = new Dictionary<string, double>();
        private readonly Dictionary<string, double> _operationAlerts = new Dictionary<string, double>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        /// <summary>
        /// Gets whether profiling is enabled
        /// </summary>
        public bool IsEnabled => _baseProfilerService.IsEnabled;

        /// <summary>
        /// Gets the message bus used by this profiler
        /// </summary>
        public IMessageBusService MessageBusService => _messageBusService;

        /// <summary>
        /// Creates a new serialization profiler
        /// </summary>
        /// <param name="baseProfilerService">Base profiler implementation for general profiling</param>
        /// <param name="serializerMetrics">Serializer metrics service</param>
        /// <param name="messageBusService">Message bus for publishing profiling messages</param>
        public SerializationProfilerService(IProfilerService baseProfilerService, ISerializerMetrics serializerMetrics, IMessageBusService messageBusService)
        {
            _baseProfilerService = baseProfilerService ?? throw new ArgumentNullException(nameof(baseProfilerService));
            _serializerMetrics = serializerMetrics ?? throw new ArgumentNullException(nameof(serializerMetrics));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            
            SubscribeToMessages();
        }

        /// <summary>
        /// Begin a profiling sample with a name
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        public IDisposable BeginSample(string name)
        {
            return _baseProfilerService.BeginSample(name);
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _baseProfilerService.BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _baseProfilerService.BeginScope(category, name);
        }

        #region Serialization-Specific Profiling Methods

        /// <summary>
        /// Begin a specialized serialization profiling session using intelligent tag selection
        /// </summary>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="serializerId">Identifier of the serializer</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="messageId">Message identifier</param>
        /// <param name="messageTypeCode">Message type code</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <param name="batchSize">Batch size for batch operations</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginSerializationScope(
            string operationType,
            Guid serializerId,
            string serializerName,
            Guid messageId = default,
            ushort messageTypeCode = 0,
            int dataSize = 0,
            int batchSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            return SerializationProfilerSession.Create(
                operationType, serializerId, serializerName, messageId, messageTypeCode, dataSize, batchSize, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for a specific serializer
        /// </summary>
        /// <param name="serializerId">Identifier of the serializer</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginSerializerScope(
            Guid serializerId, 
            string serializerName, 
            string operationType,
            int dataSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            var tag = SerializationProfilerTags.ForSerializer(operationType, serializerId);
            return new SerializationProfilerSession(
                tag, serializerId, serializerName, operationType, Guid.Empty, 0, dataSize, 0, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a serialization profiling session by serializer name
        /// </summary>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginSerializerNameScope(
            string serializerName, 
            string operationType,
            int dataSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            var tag = SerializationProfilerTags.ForSerializerName(operationType, serializerName);
            return new SerializationProfilerSession(
                tag, Guid.Empty, serializerName, operationType, Guid.Empty, 0, dataSize, 0, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for a specific message
        /// </summary>
        /// <param name="message">Message being serialized</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginMessageScope(
            IMessage message, 
            string serializerName, 
            string operationType, 
            int dataSize = 0)
        {
            if (!IsEnabled || message == null)
                return CreateNullSession(operationType, serializerName);

            var tag = SerializationProfilerTags.ForSerializerAndMessageType(operationType, serializerName, message.TypeCode);
            return new SerializationProfilerSession(
                tag, Guid.Empty, serializerName, operationType, message.Id, message.TypeCode, dataSize, 0, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for a specific message type
        /// </summary>
        /// <param name="messageTypeCode">Type code of the message</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginMessageTypeScope(
            ushort messageTypeCode, 
            string serializerName, 
            string operationType, 
            int dataSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            return SerializationProfilerSession.CreateForMessageType(
                operationType, serializerName, messageTypeCode, dataSize, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for batch operations
        /// </summary>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="batchSize">Number of messages in the batch</param>
        /// <param name="totalDataSize">Total size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginBatchScope(
            string serializerName, 
            string operationType, 
            int batchSize, 
            int totalDataSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            return SerializationProfilerSession.CreateBatch(
                operationType, serializerName, batchSize, totalDataSize, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a size-categorized serialization profiling session
        /// </summary>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="messageSize">Size of the message</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginSizedOperationScope(
            string serializerName, 
            string operationType, 
            int messageSize)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            return SerializationProfilerSession.CreateForSizedOperation(
                operationType, serializerName, messageSize, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a generic serialization profiling session using predefined tags
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginGenericSerializationScope(string operationType)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, "Generic");

            return SerializationProfilerSession.CreateGeneric(operationType, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Begin a profiling session for payload operations
        /// </summary>
        /// <param name="operationType">Operation type (SerializePayload/DeserializePayload)</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="payloadSize">Size of the payload in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginPayloadScope(
            string operationType, 
            string serializerName, 
            int payloadSize = 0)
        {
            if (!IsEnabled)
                return CreateNullSession(operationType, serializerName);

            // Use predefined payload tags for common operations
            ProfilerTag tag;
            switch (operationType.ToLowerInvariant())
            {
                case "serializepayload":
                    tag = SerializationProfilerTags.SerializePayload;
                    break;
                case "deserializepayload":
                    tag = SerializationProfilerTags.DeserializePayload;
                    break;
                default:
                    tag = SerializationProfilerTags.ForSerializerName(operationType, serializerName);
                    break;
            }

            return new SerializationProfilerSession(
                tag, Guid.Empty, serializerName, operationType, Guid.Empty, 0, payloadSize, 0, _serializerMetrics, _messageBusService);
        }

        /// <summary>
        /// Profiles a serialization action with automatic session management
        /// </summary>
        /// <param name="operationType">Type of serialization operation</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="messageTypeCode">Type code of the message</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <param name="action">Action to profile</param>
        public void ProfileSerializationAction(
            string operationType,
            string serializerName,
            ushort messageTypeCode,
            int dataSize,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginMessageTypeScope(messageTypeCode, serializerName, operationType, dataSize))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a batch serialization action
        /// </summary>
        /// <param name="operationType">Type of serialization operation</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="batchSize">Size of the batch</param>
        /// <param name="totalDataSize">Total size of data in the batch</param>
        /// <param name="action">Action to profile</param>
        public void ProfileBatchAction(
            string operationType,
            string serializerName,
            int batchSize,
            int totalDataSize,
            Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginBatchScope(serializerName, operationType, batchSize, totalDataSize))
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Profiles a simple serialization action with minimal context
        /// </summary>
        /// <param name="operationType">Type of serialization operation</param>
        /// <param name="action">Action to profile</param>
        public void ProfileGenericAction(string operationType, Action action)
        {
            if (!IsEnabled || action == null)
            {
                action?.Invoke();
                return;
            }
            
            using (BeginGenericSerializationScope(operationType))
            {
                action.Invoke();
            }
        }

        #endregion

        #region Standard IProfiler Implementation

        /// <summary>
        /// Get metrics for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get metrics for</param>
        /// <returns>Profile metrics for the tag</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _baseProfilerService.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _baseProfilerService.GetAllMetrics();
        }

        /// <summary>
        /// Get history for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get history for</param>
        /// <returns>List of historical durations</returns>
        public IReadOnlyList<double> GetHistory(ProfilerTag tag)
        {
            if (_history.TryGetValue(tag, out var history))
                return history;

            return Array.Empty<double>();
        }

        /// <summary>
        /// Register a system metric threshold alert
        /// </summary>
        /// <param name="metricTag">Tag for the metric to monitor</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterMetricAlert(ProfilerTag metricTag, double threshold)
        {
            _baseProfilerService.RegisterMetricAlert(metricTag, threshold);
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _baseProfilerService.RegisterSessionAlert(sessionTag, thresholdMs);
        }

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _baseProfilerService.ResetStats();
            _history.Clear();
            _serializerMetricsCache.Clear();
            _serializerMetrics.Reset();
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            _baseProfilerService.StartProfiling();
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            _baseProfilerService.StopProfiling();
        }

        #endregion

        #region Serialization-Specific Metrics and Alerts

        /// <summary>
        /// Get serializer metrics data
        /// </summary>
        /// <returns>Current serializer metrics</returns>
        public SerializerMetricsData GetSerializerMetrics()
        {
            return (SerializerMetricsData)_serializerMetrics;
        }

        /// <summary>
        /// Register a serializer metric threshold alert
        /// </summary>
        /// <param name="metricName">Name of the serializer metric (e.g., "AverageSerializationTimeMs")</param>
        /// <param name="threshold">Threshold value to trigger alert</param>
        public void RegisterSerializerMetricAlert(string metricName, double threshold)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            // Store locally for tracking
            _serializerMetricAlerts[metricName] = threshold;
        }

        /// <summary>
        /// Register an operation type threshold alert using predefined tags
        /// </summary>
        /// <param name="operationType">Type of operation to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterOperationAlert(string operationType, double thresholdMs)
        {
            if (string.IsNullOrEmpty(operationType) || thresholdMs <= 0)
                return;
                
            _operationAlerts[operationType] = thresholdMs;
            
            // Register with base profiler using predefined tags
            var tag = SerializationProfilerTags.ForOperation(operationType);
            RegisterSessionAlert(tag, thresholdMs);
        }

        /// <summary>
        /// Register alerts for common serialization operations
        /// </summary>
        /// <param name="serializeThresholdMs">Threshold for serialize operations</param>
        /// <param name="deserializeThresholdMs">Threshold for deserialize operations</param>
        public void RegisterCommonOperationAlerts(double serializeThresholdMs, double deserializeThresholdMs)
        {
            RegisterSessionAlert(SerializationProfilerTags.SerializeMessage, serializeThresholdMs);
            RegisterSessionAlert(SerializationProfilerTags.DeserializeMessage, deserializeThresholdMs);
            RegisterSessionAlert(SerializationProfilerTags.SerializePayload, serializeThresholdMs);
            RegisterSessionAlert(SerializationProfilerTags.DeserializePayload, deserializeThresholdMs);
            RegisterSessionAlert(SerializationProfilerTags.SerializeBatch, serializeThresholdMs * 2); // Batches expected to take longer
            RegisterSessionAlert(SerializationProfilerTags.DeserializeBatch, deserializeThresholdMs * 2);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates a null/disabled session for when profiling is disabled
        /// </summary>
        private SerializationProfilerSession CreateNullSession(string operationType, string serializerName)
        {
            return new SerializationProfilerSession(
                SerializationProfilerTags.ForOperation("Disabled"),
                Guid.Empty,
                serializerName ?? string.Empty,
                operationType,
                Guid.Empty,
                0,
                0,
                0,
                null,
                null
            );
        }

        /// <summary>
        /// Subscribes to serialization-related messages from the message bus
        /// </summary>
        private void SubscribeToMessages()
        {
            try
            {
                // Subscribe to profiler session messages
                var sessionCompletedSub = _messageBusService.GetSubscriber<SerializationProfilerSessionCompletedMessage>();
                if (sessionCompletedSub != null)
                {
                    sessionCompletedSub.Subscribe(OnSerializationSessionCompleted);
                }

                var alertSub = _messageBusService.GetSubscriber<SerializerMetricAlertMessage>();
                if (alertSub != null)
                {
                    alertSub.Subscribe(OnSerializerMetricAlert);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail initialization
                UnityEngine.Debug.LogError($"SerializationProfiler: Failed to subscribe to some messages: {ex.Message}");
            }
        }

        #endregion

        #region Message Handlers

        /// <summary>
        /// Handler for serialization session completed messages
        /// </summary>
        private void OnSerializationSessionCompleted(SerializationProfilerSessionCompletedMessage message)
        {
            if (!IsEnabled)
                return;

            // Update history
            var tag = message.Tag;
            if (!_history.TryGetValue(tag, out var history))
            {
                history = new List<double>(_maxHistoryItems);
                _history[tag] = history;
            }

            if (history.Count >= _maxHistoryItems)
                history.RemoveAt(0);

            history.Add(message.DurationMs);

            // Check operation-specific alerts
            if (_operationAlerts.TryGetValue(message.OperationType, out var threshold) && 
                message.DurationMs > threshold)
            {
                // Log or handle operation threshold exceeded
                UnityEngine.Debug.LogWarning($"Serialization operation '{message.OperationType}' exceeded threshold: {message.DurationMs}ms > {threshold}ms");
            }
        }
        
        /// <summary>
        /// Handler for serializer metric alert messages
        /// </summary>
        private void OnSerializerMetricAlert(SerializerMetricAlertMessage message)
        {
            if (!IsEnabled)
                return;
                
            // Additional handling could be added here, like logging
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose of resources and unsubscribe from messages
        /// </summary>
        public void Dispose()
        {
            // Dispose of any subscriptions
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();

            // Clear caches
            _serializerMetricsCache.Clear();
            _history.Clear();
            _serializerMetricAlerts.Clear();
            _operationAlerts.Clear();
        }

        #endregion
    }
}