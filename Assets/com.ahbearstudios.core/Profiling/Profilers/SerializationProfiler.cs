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
    /// Specialized profiler for serialization operations that captures serialization-specific metrics
    /// </summary>
    public class SerializationProfiler : IProfiler
    {
        private readonly IProfiler _baseProfiler;
        private readonly ISerializerMetrics _serializerMetrics;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<Guid, SerializerMetricsData> _serializerMetricsCache = new Dictionary<Guid, SerializerMetricsData>();
        private readonly int _maxHistoryItems = 100;
        private readonly Dictionary<ProfilerTag, List<double>> _history = new Dictionary<ProfilerTag, List<double>>();
        private readonly Dictionary<string, double> _serializerMetricAlerts = new Dictionary<string, double>();

        /// <summary>
        /// Gets whether profiling is enabled
        /// </summary>
        public bool IsEnabled => _baseProfiler.IsEnabled;

        /// <summary>
        /// Gets the message bus used by this profiler
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Creates a new serialization profiler
        /// </summary>
        /// <param name="baseProfiler">Base profiler implementation for general profiling</param>
        /// <param name="serializerMetrics">Serializer metrics service</param>
        /// <param name="messageBus">Message bus for publishing profiling messages</param>
        public SerializationProfiler(IProfiler baseProfiler, ISerializerMetrics serializerMetrics, IMessageBus messageBus)
        {
            _baseProfiler = baseProfiler ?? throw new ArgumentNullException(nameof(baseProfiler));
            _serializerMetrics = serializerMetrics ?? throw new ArgumentNullException(nameof(serializerMetrics));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            
            // Subscribe to profiler session messages if there are any specific ones for serialization
            _messageBus.GetSubscriber<SerializationProfilerSessionCompletedMessage>().Subscribe(OnSerializationSessionCompleted);
            _messageBus.GetSubscriber<SerializerMetricAlertMessage>().Subscribe(OnSerializerMetricAlert);
        }

        /// <summary>
        /// Begin a profiling sample with a name
        /// </summary>
        /// <param name="name">Name of the profiler sample</param>
        /// <returns>Profiler session that should be disposed when sample ends</returns>
        public IDisposable BeginSample(string name)
        {
            return _baseProfiler.BeginSample(name);
        }

        /// <summary>
        /// Begin a profiling scope with the specified tag
        /// </summary>
        /// <param name="tag">Profiler tag for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _baseProfiler.BeginScope(tag);
        }

        /// <summary>
        /// Begin a profiling scope with a category and name
        /// </summary>
        /// <param name="category">Category for this scope</param>
        /// <param name="name">Name for this scope</param>
        /// <returns>Profiler session that should be disposed when scope ends</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _baseProfiler.BeginScope(category, name);
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for a specific serializer
        /// </summary>
        /// <param name="serializerId">Identifier of the serializer</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginSerializerScope(Guid serializerId, string serializerName, string operationType)
        {
            if (!IsEnabled)
                return new SerializationProfilerSession(ProfilerTag.Uncategorized, serializerId, serializerName, 
                    Guid.Empty, 0, 0, null, null);

            var tag = SerializationProfilerTags.ForSerializerName(operationType, serializerName);
            return new SerializationProfilerSession(
                tag,
                serializerId,
                serializerName,
                Guid.Empty, // No specific message ID
                0, // Default message type code
                0, // Default data size
                _serializerMetrics,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for a specific message
        /// </summary>
        /// <param name="message">Message being serialized</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginMessageScope(IMessage message, string serializerName, string operationType, int dataSize = 0)
        {
            if (!IsEnabled || message == null)
                return new SerializationProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 
                    Guid.Empty, 0, 0, null, null);

            var tag = SerializationProfilerTags.ForSerializerAndMessageType(operationType, serializerName, message.TypeCode);
            return new SerializationProfilerSession(
                tag,
                Guid.Empty, // No specific serializer ID
                serializerName,
                message.Id,
                message.TypeCode,
                dataSize,
                _serializerMetrics,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for batch operations
        /// </summary>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="batchSize">Number of messages in the batch</param>
        /// <param name="totalDataSize">Total size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginBatchScope(string serializerName, string operationType, int batchSize, int totalDataSize)
        {
            if (!IsEnabled)
                return new SerializationProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 
                    Guid.Empty, 0, 0, null, null);

            var tag = SerializationProfilerTags.ForBatch(operationType, batchSize);
            return new SerializationProfilerSession(
                tag,
                Guid.Empty, // No specific serializer ID
                serializerName,
                Guid.Empty, // No specific message ID
                0, // No specific message type
                totalDataSize,
                _serializerMetrics,
                _messageBus
            );
        }

        /// <summary>
        /// Begin a specialized serialization profiling session for a specific message type
        /// </summary>
        /// <param name="messageTypeCode">Type code of the message</param>
        /// <param name="serializerName">Name of the serializer</param>
        /// <param name="operationType">Type of operation (serialize/deserialize)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <returns>Serialization profiler session</returns>
        public SerializationProfilerSession BeginMessageTypeScope(ushort messageTypeCode, string serializerName, string operationType, int dataSize = 0)
        {
            if (!IsEnabled)
                return new SerializationProfilerSession(ProfilerTag.Uncategorized, Guid.Empty, string.Empty, 
                    Guid.Empty, 0, 0, null, null);

            var tag = SerializationProfilerTags.ForMessageType(operationType, messageTypeCode);
            return new SerializationProfilerSession(
                tag,
                Guid.Empty, // No specific serializer ID
                serializerName,
                Guid.Empty, // No specific message ID
                messageTypeCode,
                dataSize,
                _serializerMetrics,
                _messageBus
            );
        }

        /// <summary>
        /// Begins a profiling session for a generic serialization operation
        /// </summary>
        /// <param name="operationType">Operation type (serialize, deserialize, etc.)</param>
        /// <returns>A profiler session</returns>
        public ProfilerSession BeginGenericSerializationScope(string operationType)
        {
            if (!IsEnabled)
                return null;
                
            var tag = SerializationProfilerTags.ForOperation(operationType);
            return BeginScope(tag);
        }
        
        /// <summary>
        /// Profiles a serialization action
        /// </summary>
        /// <param name="operationType">Operation type (serialize, deserialize)</param>
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
        /// Get metrics for a specific profiling tag
        /// </summary>
        /// <param name="tag">The tag to get metrics for</param>
        /// <returns>Profile metrics for the tag</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _baseProfiler.GetMetrics(tag);
        }

        /// <summary>
        /// Get all profiling metrics
        /// </summary>
        /// <returns>Dictionary of all profiling metrics by tag</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _baseProfiler.GetAllMetrics();
        }

        /// <summary>
        /// Get serializer metrics data
        /// </summary>
        /// <returns>Current serializer metrics</returns>
        public SerializerMetricsData GetSerializerMetrics()
        {
            return (SerializerMetricsData)_serializerMetrics;
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
            _baseProfiler.RegisterMetricAlert(metricTag, threshold);
        }

        /// <summary>
        /// Register a session threshold alert
        /// </summary>
        /// <param name="sessionTag">Tag for the session to monitor</param>
        /// <param name="thresholdMs">Threshold in milliseconds to trigger alert</param>
        public void RegisterSessionAlert(ProfilerTag sessionTag, double thresholdMs)
        {
            _baseProfiler.RegisterSessionAlert(sessionTag, thresholdMs);
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
            
            // No direct registration with serializer metrics since ISerializerMetrics doesn't have alert functionality
            // This could be added in the future if needed
        }

        /// <summary>
        /// Reset all profiling stats
        /// </summary>
        public void ResetStats()
        {
            _baseProfiler.ResetStats();
            _history.Clear();
            _serializerMetricsCache.Clear();
            _serializerMetrics.Reset();
        }

        /// <summary>
        /// Start profiling
        /// </summary>
        public void StartProfiling()
        {
            _baseProfiler.StartProfiling();
        }

        /// <summary>
        /// Stop profiling
        /// </summary>
        public void StopProfiling()
        {
            _baseProfiler.StopProfiling();
        }

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
    }
}