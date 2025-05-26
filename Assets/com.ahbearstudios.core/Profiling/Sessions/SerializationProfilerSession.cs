using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Sessions
{
    /// <summary>
    /// A specialized profiler session for serialization operations that captures additional serialization metrics
    /// </summary>
    public class SerializationProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        private readonly Guid _sessionId;
        
        /// <summary>
        /// Serializer identifier
        /// </summary>
        public readonly Guid SerializerId;
        
        /// <summary>
        /// Serializer name
        /// </summary>
        public readonly string SerializerName;
        
        /// <summary>
        /// Message identifier (if applicable)
        /// </summary>
        public readonly Guid MessageId;
        
        /// <summary>
        /// Message type code (if applicable)
        /// </summary>
        public readonly ushort MessageTypeCode;
        
        /// <summary>
        /// Size of the serialized data in bytes
        /// </summary>
        public readonly int DataSize;
        
        /// <summary>
        /// The serializer metrics interface for recording metrics
        /// </summary>
        private readonly ISerializerMetrics _serializerMetrics;
        
        /// <summary>
        /// The operation type being profiled
        /// </summary>
        private readonly string _operationType;
        
        /// <summary>
        /// Creates a new serialization profiler session
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="serializerId">Serializer identifier</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="messageId">Message identifier (if applicable)</param>
        /// <param name="messageTypeCode">Message type code (if applicable)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        public SerializationProfilerSession(
            ProfilerTag tag, 
            Guid serializerId, 
            string serializerName, 
            Guid messageId, 
            ushort messageTypeCode, 
            int dataSize,
            ISerializerMetrics serializerMetrics,
            IMessageBus messageBus = null)
        {
            _tag = tag;
            SerializerId = serializerId;
            SerializerName = serializerName;
            MessageId = messageId;
            MessageTypeCode = messageTypeCode;
            DataSize = dataSize;
            _serializerMetrics = serializerMetrics;
            _messageBus = messageBus;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _operationType = GetOperationTypeFromTag(tag.Name);
            _marker = new ProfilerMarker(_tag.FullName);
            
            // Begin the profiler marker
            _marker.Begin();
            _startTimeNs = GetHighPrecisionTimestampNs();
            
            // Notify via message bus that session started
            if (_messageBus != null)
            {
                var message = new SerializationProfilerSessionStartedMessage(
                    _tag, _sessionId, SerializerId, SerializerName, MessageId, MessageTypeCode, DataSize);
                _messageBus.PublishMessage(message);
            }
        }

        /// <summary>
        /// Get the tag associated with this session
        /// </summary>
        public ProfilerTag Tag => _tag;
        
        /// <summary>
        /// Gets the elapsed time in milliseconds
        /// </summary>
        public double ElapsedMilliseconds
        {
            get
            {
                long currentTimeNs = _isDisposed ? _endTimeNs : GetHighPrecisionTimestampNs();
                return (currentTimeNs - _startTimeNs) / 1000000.0;
            }
        }
        
        /// <summary>
        /// Gets the elapsed time in nanoseconds
        /// </summary>
        public long ElapsedNanoseconds
        {
            get
            {
                return _isDisposed ? (_endTimeNs - _startTimeNs) : (GetHighPrecisionTimestampNs() - _startTimeNs);
            }
        }
        
        /// <summary>
        /// Indicates if this session has been disposed
        /// </summary>
        public bool IsDisposed => _isDisposed;
        
        /// <summary>
        /// Records a custom metric with this session
        /// </summary>
        public void RecordMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName))
                return;
                
            _customMetrics[metricName] = value;
        }
        
        /// <summary>
        /// Gets a dictionary of all custom metrics recorded with this session
        /// </summary>
        public IReadOnlyDictionary<string, double> GetMetrics()
        {
            return _customMetrics;
        }

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _marker.End();
            _endTimeNs = GetHighPrecisionTimestampNs();
            _isDisposed = true;
            
            // Record serialization-specific metrics
            RecordSerializationMetrics();
            
            // Notify via message bus that session ended
            if (_messageBus != null)
            {
                var message = new SerializationProfilerSessionCompletedMessage(
                    _tag, _sessionId, SerializerId, SerializerName, MessageId, MessageTypeCode, 
                    DataSize, ElapsedMilliseconds, _customMetrics, _operationType);
                _messageBus.PublishMessage(message);
            }
        }
        
        /// <summary>
        /// Record metrics specific to serialization operations
        /// </summary>
        private void RecordSerializationMetrics()
        {
            // Only record metrics if we have a metrics system
            if (_serializerMetrics != null)
            {
                // Calculate duration as TimeSpan for the metrics API
                var duration = TimeSpan.FromMilliseconds(ElapsedMilliseconds);
        
                // Record appropriate metrics based on operation type
                bool success = true; // Default to success
        
                // Check if we have an error flag
                if (_customMetrics.TryGetValue("Error", out double errorValue))
                {
                    // Treat non-zero as error/failure
                    success = Math.Abs(errorValue) < double.Epsilon;
                }
        
                switch (_operationType.ToLowerInvariant())
                {
                    case "serialize":
                        _serializerMetrics.RecordSerialization(duration, DataSize, success);
                        break;
                    case "deserialize":
                        _serializerMetrics.RecordDeserialization(duration, DataSize, success);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Extract operation type from a tag name
        /// </summary>
        private string GetOperationTypeFromTag(string tagName)
        {
            // Extract operation type (after the last dot)
            int lastDot = tagName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < tagName.Length - 1)
            {
                return tagName.Substring(lastDot + 1);
            }
            return tagName;
        }
        
        /// <summary>
        /// Gets high precision timestamp in nanoseconds
        /// </summary>
        private static long GetHighPrecisionTimestampNs()
        {
            long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            long frequency = System.Diagnostics.Stopwatch.Frequency;
            return (long)((double)timestamp / frequency * 1_000_000_000);
        }
    }
}