using System;
using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.Profiling.Messages;
using AhBearStudios.Core.Profiling.Tagging;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Sessions
{
    /// <summary>
    /// A specialized profiler session for serialization operations that captures additional serialization metrics.
    /// Implements intelligent tag selection based on available parameters for optimal profiling granularity.
    /// </summary>
    public class SerializationProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly IMessageBusService _messageBusService;
        private readonly Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        private readonly Guid _sessionId;
        private readonly bool _isNullSession;
        
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
        /// Type of operation being performed
        /// </summary>
        public readonly string OperationType;
        
        /// <summary>
        /// Size of the serialized data in bytes
        /// </summary>
        public readonly int DataSize;
        
        /// <summary>
        /// Batch size for batch operations
        /// </summary>
        public readonly int BatchSize;
        
        /// <summary>
        /// The serializer metrics interface for recording metrics
        /// </summary>
        private readonly ISerializerMetrics _serializerMetrics;
        
        /// <summary>
        /// Whether the operation completed successfully
        /// </summary>
        private bool _success = true;
        
        /// <summary>
        /// Creates a new serialization profiler session with explicit tag
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="serializerId">Serializer identifier</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <param name="messageId">Message identifier (if applicable)</param>
        /// <param name="messageTypeCode">Message type code (if applicable)</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <param name="batchSize">Batch size for batch operations</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        public SerializationProfilerSession(
            ProfilerTag tag, 
            Guid serializerId, 
            string serializerName,
            string operationType,
            Guid messageId, 
            ushort messageTypeCode, 
            int dataSize,
            int batchSize,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            _tag = tag;
            SerializerId = serializerId;
            SerializerName = serializerName ?? string.Empty;
            OperationType = operationType ?? string.Empty;
            MessageId = messageId;
            MessageTypeCode = messageTypeCode;
            DataSize = dataSize;
            BatchSize = batchSize;
            _serializerMetrics = serializerMetrics;
            _messageBusService = messageBusService;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _isNullSession = serializerMetrics == null && messageBusService == null;
            
            // Only create marker and start timing if this isn't a null session
            if (!_isNullSession)
            {
                _marker = new ProfilerMarker(_tag.FullName);
                
                // Begin the profiler marker
                _marker.Begin();
                _startTimeNs = GetHighPrecisionTimestampNs();
                
                // Notify via message bus that session started
                if (_messageBusService != null)
                {
                    var message = new SerializationProfilerSessionStartedMessage(
                        _tag, _sessionId, SerializerId, SerializerName, MessageId, MessageTypeCode, DataSize);

                    try
                    {
                        var publisher = _messageBusService.GetPublisher<SerializationProfilerSessionStartedMessage>();
                        publisher?.Publish(message);
                    }
                    catch
                    {
                        // Silently handle publication errors during session start
                    }
                }
            }
        }

        /// <summary>
        /// Factory method to create a session with appropriate tag based on parameters.
        /// Uses intelligent tag selection hierarchy for optimal profiling granularity.
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerId">Serializer identifier</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="messageId">Message identifier</param>
        /// <param name="messageTypeCode">Message type code</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <param name="batchSize">Batch size for batch operations</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new serialization profiler session with appropriate tag</returns>
        public static SerializationProfilerSession Create(
            string operationType,
            Guid serializerId,
            string serializerName,
            Guid messageId,
            ushort messageTypeCode,
            int dataSize,
            int batchSize,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            if (string.IsNullOrEmpty(operationType))
                operationType = "Unknown";

            // Choose the most specific tag available using hierarchy
            ProfilerTag tag = SelectOptimalTag(operationType, serializerId, serializerName, messageId, messageTypeCode, dataSize, batchSize);

            return new SerializationProfilerSession(
                tag, serializerId, serializerName, operationType, messageId, messageTypeCode, dataSize, batchSize, serializerMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for creating sessions with minimal parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="dataSize">Size of the data in bytes</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new serialization profiler session</returns>
        public static SerializationProfilerSession CreateMinimal(
            string operationType,
            string serializerName,
            int dataSize,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            return Create(operationType, Guid.Empty, serializerName, Guid.Empty, 0, dataSize, 0, serializerMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for creating sessions with just operation type
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new serialization profiler session</returns>
        public static SerializationProfilerSession CreateGeneric(
            string operationType,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            var tag = SerializationProfilerTags.ForOperation(operationType);
            
            return new SerializationProfilerSession(
                tag, Guid.Empty, "Generic", operationType, Guid.Empty, 0, 0, 0, serializerMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for batch operations
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="batchSize">Batch size</param>
        /// <param name="totalDataSize">Total size of all data in the batch</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new serialization profiler session for batch operations</returns>
        public static SerializationProfilerSession CreateBatch(
            string operationType,
            string serializerName,
            int batchSize,
            int totalDataSize,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            var tag = SerializationProfilerTags.ForBatch(operationType, batchSize);
            
            return new SerializationProfilerSession(
                tag, Guid.Empty, serializerName, operationType, Guid.Empty, 0, totalDataSize, batchSize, serializerMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for message type specific operations
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="messageTypeCode">Message type code</param>
        /// <param name="dataSize">Size of the data</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new serialization profiler session for message type operations</returns>
        public static SerializationProfilerSession CreateForMessageType(
            string operationType,
            string serializerName,
            ushort messageTypeCode,
            int dataSize,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            var tag = SerializationProfilerTags.ForMessageType(operationType, messageTypeCode);
            
            return new SerializationProfilerSession(
                tag, Guid.Empty, serializerName, operationType, Guid.Empty, messageTypeCode, dataSize, 0, serializerMetrics, messageBusService);
        }

        /// <summary>
        /// Factory method for size-based operations  
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="messageSize">Size of the message</param>
        /// <param name="serializerMetrics">Serializer metrics interface for recording</param>
        /// <param name="messageBusService">Message bus for sending messages</param>
        /// <returns>A new serialization profiler session with size categorization</returns>
        public static SerializationProfilerSession CreateForSizedOperation(
            string operationType,
            string serializerName,
            int messageSize,
            ISerializerMetrics serializerMetrics,
            IMessageBusService messageBusService = null)
        {
            var tag = SerializationProfilerTags.ForSizedOperation(operationType, serializerName, messageSize);
            
            return new SerializationProfilerSession(
                tag, Guid.Empty, serializerName, operationType, Guid.Empty, 0, messageSize, 0, serializerMetrics, messageBusService);
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
                if (_isNullSession) return 0.0;
                
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
                if (_isNullSession) return 0L;
                
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
            if (string.IsNullOrEmpty(metricName) || _isNullSession)
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
        /// Records serialization/deserialization throughput metrics
        /// </summary>
        /// <param name="bytesPerSecond">Bytes processed per second</param>
        /// <param name="messagesPerSecond">Messages processed per second</param>
        public void RecordThroughput(double bytesPerSecond, double messagesPerSecond)
        {
            RecordMetric("BytesPerSecond", bytesPerSecond);
            RecordMetric("MessagesPerSecond", messagesPerSecond);
        }

        /// <summary>
        /// Records compression metrics
        /// </summary>
        /// <param name="originalSize">Original size before compression</param>
        /// <param name="compressedSize">Size after compression</param>
        /// <param name="compressionRatio">Compression ratio (compressed/original)</param>
        public void RecordCompression(int originalSize, int compressedSize, double compressionRatio)
        {
            RecordMetric("OriginalSize", originalSize);
            RecordMetric("CompressedSize", compressedSize);
            RecordMetric("CompressionRatio", compressionRatio);
            RecordMetric("SpaceSaved", originalSize - compressedSize);
        }

        /// <summary>
        /// Records serialization format metrics
        /// </summary>
        /// <param name="format">Serialization format (JSON, Binary, etc.)</param>
        /// <param name="version">Format version</param>
        public void RecordFormat(string format, string version = null)
        {
            if (!string.IsNullOrEmpty(format))
            {
                RecordMetric($"Format_{format}", 1.0);
                if (!string.IsNullOrEmpty(version))
                    RecordMetric($"Version_{version}", 1.0);
            }
        }

        /// <summary>
        /// Records error information
        /// </summary>
        /// <param name="errorType">Type of error</param>
        /// <param name="errorCode">Error code</param>
        public void RecordError(string errorType, string errorCode = null)
        {
            _success = false;
            RecordMetric("Error", 1.0);
            if (!string.IsNullOrEmpty(errorType))
                RecordMetric($"ErrorType_{errorType}", 1.0);
            if (!string.IsNullOrEmpty(errorCode))
                RecordMetric($"ErrorCode_{errorCode}", 1.0);
        }

        /// <summary>
        /// Records whether the operation was successful
        /// </summary>
        /// <param name="success">True if the operation was successful</param>
        public void RecordSuccess(bool success)
        {
            _success = success;
            RecordMetric("Success", success ? 1.0 : 0.0);
        }

        /// <summary>
        /// Records buffer allocation metrics
        /// </summary>
        /// <param name="allocatedBytes">Number of bytes allocated</param>
        /// <param name="pooledBytes">Number of bytes from pool</param>
        public void RecordBufferAllocation(int allocatedBytes, int pooledBytes = 0)
        {
            RecordMetric("AllocatedBytes", allocatedBytes);
            RecordMetric("PooledBytes", pooledBytes);
            RecordMetric("NewAllocatedBytes", allocatedBytes - pooledBytes);
        }

        /// <summary>
        /// Records batch processing metrics
        /// </summary>
        /// <param name="processedCount">Number of items processed in batch</param>
        /// <param name="failedCount">Number of items that failed processing</param>
        public void RecordBatchProcessing(int processedCount, int failedCount = 0)
        {
            RecordMetric("BatchProcessedCount", processedCount);
            RecordMetric("BatchFailedCount", failedCount);
            RecordMetric("BatchSuccessRate", processedCount > 0 ? (double)(processedCount - failedCount) / processedCount : 0.0);
        }

        /// <summary>
        /// Records network-related serialization metrics
        /// </summary>
        /// <param name="packetSize">Size of network packet</param>
        /// <param name="overhead">Serialization overhead in bytes</param>
        public void RecordNetworkMetrics(int packetSize, int overhead)
        {
            RecordMetric("PacketSize", packetSize);
            RecordMetric("SerializationOverhead", overhead);
            RecordMetric("OverheadPercentage", packetSize > 0 ? (double)overhead / packetSize * 100.0 : 0.0);
        }

        /// <summary>
        /// End the profiler marker and record duration
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed || _isNullSession)
                return;

            _marker.End();
            _endTimeNs = GetHighPrecisionTimestampNs();
            _isDisposed = true;
            
            // Record serialization-specific metrics
            RecordSerializationMetrics();
            
            // Notify via message bus that session ended
            if (_messageBusService != null)
            {
                var message = new SerializationProfilerSessionCompletedMessage(
                    _tag, _sessionId, SerializerId, SerializerName, MessageId, MessageTypeCode, 
                    DataSize, ElapsedMilliseconds, _customMetrics, OperationType);

                try
                {
                    var publisher = _messageBusService.GetPublisher<SerializationProfilerSessionCompletedMessage>();
                    publisher?.Publish(message);
                }
                catch
                {
                    // Silently handle publication errors during session completion
                }
            }
        }

        /// <summary>
        /// Selects the most appropriate ProfilerTag based on available parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="serializerId">Serializer identifier</param>
        /// <param name="serializerName">Serializer name</param>
        /// <param name="messageId">Message identifier</param>
        /// <param name="messageTypeCode">Message type code</param>
        /// <param name="dataSize">Data size</param>
        /// <param name="batchSize">Batch size</param>
        /// <returns>The most specific ProfilerTag available</returns>
        private static ProfilerTag SelectOptimalTag(
            string operationType, 
            Guid serializerId, 
            string serializerName, 
            Guid messageId, 
            ushort messageTypeCode, 
            int dataSize,
            int batchSize)
        {
            // Priority 1: Batch operations (if batch size > 1)
            if (batchSize > 1)
            {
                return SerializationProfilerTags.ForBatch(operationType, batchSize);
            }

            // Priority 2: Size-based profiling for large messages (if data size is significant)
            if (dataSize > 0 && !string.IsNullOrEmpty(serializerName) && serializerName != "Unknown" && serializerName != "Generic")
            {
                return SerializationProfilerTags.ForSizedOperation(operationType, serializerName, dataSize);
            }

            // Priority 3: Serializer with GUID (most specific)
            if (serializerId != Guid.Empty)
            {
                return SerializationProfilerTags.ForSerializer(operationType, serializerId);
            }

            // Priority 4: Specific message (if we have message ID)
            if (messageId != Guid.Empty)
            {
                return SerializationProfilerTags.ForMessage(operationType, messageId);
            }

            // Priority 5: Message type (if we have type code)
            if (messageTypeCode > 0)
            {
                return SerializationProfilerTags.ForMessageType(operationType, messageTypeCode);
            }

            // Priority 6: Serializer by name (if we have a meaningful name)
            if (!string.IsNullOrEmpty(serializerName) && serializerName != "Unknown" && serializerName != "Generic")
            {
                return SerializationProfilerTags.ForSerializerName(operationType, serializerName);
            }

            // Priority 7: Use predefined operation tags (least specific but still meaningful)
            return SerializationProfilerTags.ForOperation(operationType);
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
                switch (OperationType.ToLowerInvariant())
                {
                    case "serialize":
                    case "serializemessage":
                    case "serializepayload":
                    case "serializebatch":
                        _serializerMetrics.RecordSerialization(duration, DataSize, _success);
                        break;
                        
                    case "deserialize":
                    case "deserializemessage":
                    case "deserializepayload":
                    case "deserializebatch":
                        _serializerMetrics.RecordDeserialization(duration, DataSize, _success);
                        break;
                        
                    default:
                        // For unknown operations, try to infer from tag name
                        string tagName = _tag.Name.ToLowerInvariant();
                        if (tagName.Contains("serialize") && !tagName.Contains("deserialize"))
                            _serializerMetrics.RecordSerialization(duration, DataSize, _success);
                        else if (tagName.Contains("deserialize"))
                            _serializerMetrics.RecordDeserialization(duration, DataSize, _success);
                        break;
                }
            }
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