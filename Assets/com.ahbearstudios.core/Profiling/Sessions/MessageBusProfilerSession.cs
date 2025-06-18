
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
    /// A specialized profiler session for message bus operations that captures additional messaging metrics.
    /// Implements intelligent tag selection based on available parameters for optimal profiling granularity.
    /// </summary>
    public class MessageBusProfilerSession : IProfilerSession
    {
        private readonly ProfilerMarker _marker;
        private readonly ProfilerTag _tag;
        private readonly IMessageBus _messageBus;
        private readonly Dictionary<string, double> _customMetrics = new Dictionary<string, double>();
        private bool _isDisposed;
        private long _startTimeNs;
        private long _endTimeNs;
        private readonly Guid _sessionId;
        private readonly bool _isNullSession;

        /// <summary>
        /// Message bus identifier
        /// </summary>
        public readonly Guid BusId;

        /// <summary>
        /// Message bus name
        /// </summary>
        public readonly string BusName;

        /// <summary>
        /// Type of operation being performed (Publish, Deliver, Subscribe, etc.)
        /// </summary>
        public readonly string OperationType;

        /// <summary>
        /// Number of subscribers at the time of profiling
        /// </summary>
        public readonly int SubscriberCount;

        /// <summary>
        /// Size of the message queue at the time of profiling
        /// </summary>
        public readonly int QueueSize;

        /// <summary>
        /// Type of message being processed
        /// </summary>
        public readonly string MessageType;

        /// <summary>
        /// The message bus metrics interface for recording metrics
        /// </summary>
        private readonly IMessageBusMetrics _busMetrics;

        /// <summary>
        /// Whether the operation completed successfully
        /// </summary>
        private bool _success = true;

        /// <summary>
        /// Creates a new message bus profiler session with explicit tag
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="busId">Message bus identifier</param>
        /// <param name="busName">Message bus name</param>
        /// <param name="operationType">Type of operation being performed</param>
        /// <param name="subscriberCount">Number of subscribers</param>
        /// <param name="queueSize">Current queue size</param>
        /// <param name="messageType">Type of message being processed</param>
        /// <param name="busMetrics">Message bus metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        public MessageBusProfilerSession(
            ProfilerTag tag,
            Guid busId,
            string busName,
            string operationType,
            int subscriberCount,
            int queueSize,
            string messageType,
            IMessageBusMetrics busMetrics,
            IMessageBus messageBus = null)
        {
            _tag = tag;
            BusId = busId;
            BusName = busName ?? string.Empty;
            OperationType = operationType ?? string.Empty;
            SubscriberCount = subscriberCount;
            QueueSize = queueSize;
            MessageType = messageType ?? string.Empty;
            _busMetrics = busMetrics;
            _messageBus = messageBus;
            _isDisposed = false;
            _sessionId = Guid.NewGuid();
            _isNullSession = busMetrics == null && messageBus == null;

            // Only create marker and start timing if this isn't a null session
            if (!_isNullSession)
            {
                _marker = new ProfilerMarker(_tag.FullName);

                // Begin the profiler marker
                _marker.Begin();
                _startTimeNs = GetHighPrecisionTimestampNs();

                // Notify via message bus that session started
                if (_messageBus != null)
                {
                    var message = new MessageBusProfilerSessionStartedMessage(
                        _tag, _sessionId, BusId, BusName, OperationType, SubscriberCount, QueueSize, MessageType);

                    try
                    {
                        var publisher = _messageBus.GetPublisher<MessageBusProfilerSessionStartedMessage>();
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
        /// <param name="busId">Message bus identifier</param>
        /// <param name="busName">Message bus name</param>
        /// <param name="subscriberCount">Number of subscribers</param>
        /// <param name="queueSize">Current queue size</param>
        /// <param name="messageType">Type of message being processed</param>
        /// <param name="busMetrics">Message bus metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        /// <returns>A new message bus profiler session with appropriate tag</returns>
        public static MessageBusProfilerSession Create(
            string operationType,
            Guid busId,
            string busName,
            int subscriberCount,
            int queueSize,
            string messageType,
            IMessageBusMetrics busMetrics,
            IMessageBus messageBus = null)
        {
            if (string.IsNullOrEmpty(operationType))
                operationType = "Unknown";

            // Choose the most specific tag available using hierarchy
            ProfilerTag tag = SelectOptimalTag(operationType, busId, busName, messageType);

            return new MessageBusProfilerSession(
                tag, busId, busName, operationType, subscriberCount, queueSize, messageType, busMetrics, messageBus);
        }

        /// <summary>
        /// Factory method for creating sessions with minimal parameters
        /// </summary>
        /// <param name="operationType">Operation type</param>
        /// <param name="busName">Message bus name</param>
        /// <param name="messageType">Type of message being processed</param>
        /// <param name="busMetrics">Message bus metrics interface for recording</param>
        /// <param name="messageBus">Message bus for sending messages</param>
        /// <returns>A new message bus profiler session</returns>
        public static MessageBusProfilerSession CreateMinimal(
            string operationType,
            string busName,
            string messageType,
            IMessageBusMetrics busMetrics,
            IMessageBus messageBus = null)
        {
            Guid busId = string.IsNullOrEmpty(busName) ? Guid.Empty : CreateDeterministicGuid(busName);
            
            return Create(operationType, busId, busName, 0, 0, messageType, busMetrics, messageBus);
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
        /// Records the number of messages processed during this session
        /// </summary>
        /// <param name="messageCount">Number of messages processed</param>
        public void RecordMessageCount(int messageCount)
        {
            RecordMetric("MessageCount", messageCount);
        }

        /// <summary>
        /// Records the size of messages processed during this session
        /// </summary>
        /// <param name="totalBytes">Total bytes processed</param>
        public void RecordMessageSize(long totalBytes)
        {
            RecordMetric("MessageSizeBytes", totalBytes);
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
        /// Records an error that occurred during the operation
        /// </summary>
        /// <param name="errorCode">Error code or identifier</param>
        public void RecordError(string errorCode)
        {
            _success = false;
            RecordMetric("Error", 1.0);
            if (!string.IsNullOrEmpty(errorCode))
            {
                RecordMetric($"ErrorCode_{errorCode}", 1.0);
            }
        }

        /// <summary>
        /// Records the current throughput rate
        /// </summary>
        /// <param name="messagesPerSecond">Messages processed per second</param>
        public void RecordThroughput(double messagesPerSecond)
        {
            RecordMetric("ThroughputMPS", messagesPerSecond);
        }

        /// <summary>
        /// Records delivery service information
        /// </summary>
        /// <param name="serviceName">Name of the delivery service</param>
        /// <param name="deliveryAttempts">Number of delivery attempts</param>
        public void RecordDeliveryService(string serviceName, int deliveryAttempts = 1)
        {
            if (!string.IsNullOrEmpty(serviceName))
                RecordMetric("DeliveryService", 1.0);
            RecordMetric("DeliveryAttempts", deliveryAttempts);
        }

        /// <summary>
        /// Records batch operation metrics
        /// </summary>
        /// <param name="batchSize">Size of the batch</param>
        /// <param name="processedCount">Number of items processed</param>
        /// <param name="failedCount">Number of items that failed</param>
        public void RecordBatchOperation(int batchSize, int processedCount, int failedCount = 0)
        {
            RecordMetric("BatchSize", batchSize);
            RecordMetric("ProcessedCount", processedCount);
            if (failedCount > 0)
                RecordMetric("FailedCount", failedCount);
        }

        /// <summary>
        /// Records reliable delivery metrics
        /// </summary>
        /// <param name="retryCount">Number of retries attempted</param>
        /// <param name="acknowledgmentTime">Time to receive acknowledgment</param>
        public void RecordReliableDelivery(int retryCount, double acknowledgmentTime)
        {
            RecordMetric("RetryCount", retryCount);
            RecordMetric("AckTime", acknowledgmentTime);
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

            // Record message bus-specific metrics
            RecordMessageBusMetrics();

            // Notify via message bus that session ended
            if (_messageBus != null)
            {
                var message = new MessageBusProfilerSessionCompletedMessage(
                    _tag, _sessionId, BusId, BusName, OperationType, SubscriberCount, QueueSize,
                    MessageType, ElapsedMilliseconds, _customMetrics);

                try
                {
                    var publisher = _messageBus.GetPublisher<MessageBusProfilerSessionCompletedMessage>();
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
        /// <param name="busId">Message bus identifier</param>
        /// <param name="busName">Message bus name</param>
        /// <param name="messageType">Type of message being processed</param>
        /// <returns>The most specific ProfilerTag available</returns>
        private static ProfilerTag SelectOptimalTag(string operationType, Guid busId, string busName, string messageType)
        {
            // Priority 1: Message type-specific tag (most specific for message operations)
            if (!string.IsNullOrEmpty(messageType) && messageType != "Unknown" && messageType != "Generic")
            {
                return MessageBusProfilerTags.ForMessageType(operationType, messageType);
            }

            // Priority 2: Bus with GUID (more specific than name)
            if (busId != Guid.Empty)
            {
                return MessageBusProfilerTags.ForBus(operationType, busId);
            }

            // Priority 3: Bus by name
            if (!string.IsNullOrEmpty(busName) && busName != "Unknown")
            {
                return MessageBusProfilerTags.ForBusNamed(operationType, busName);
            }

            // Priority 4: Use predefined operation tags when possible
            switch (operationType.ToLowerInvariant())
            {
                case "publish":
                    return MessageBusProfilerTags.MessagePublish;
                case "deliver":
                    return MessageBusProfilerTags.MessageDeliver;
                case "subscribe":
                    return MessageBusProfilerTags.MessageSubscribe;
                case "unsubscribe":
                    return MessageBusProfilerTags.MessageUnsubscribe;
                case "process":
                    return MessageBusProfilerTags.MessageProcess;
                case "queue":
                    return MessageBusProfilerTags.MessageQueue;
                case "batch":
                    return MessageBusProfilerTags.MessageBatch;
                case "reliabledeliver":
                    return MessageBusProfilerTags.MessageReliableDeliver;
                default:
                    // Priority 5: Generic operation tag (least specific)
                    return MessageBusProfilerTags.ForBusNamed(operationType, "Generic");
            }
        }

        /// <summary>
        /// Record metrics specific to message bus operations
        /// </summary>
        private void RecordMessageBusMetrics()
        {
            // Only record metrics if we have a bus ID and metrics system
            if (BusId != Guid.Empty && _busMetrics != null)
            {
                // Convert duration to milliseconds (float) as expected by the interface
                var durationMs = (float)ElapsedMilliseconds;

                // Get message count if available
                int messageCount = 1; // Default to 1 message
                if (_customMetrics.TryGetValue("MessageCount", out double msgCountValue))
                {
                    messageCount = (int)msgCountValue;
                }

                // Record appropriate metrics based on operation type
                switch (OperationType.ToLowerInvariant())
                {
                    case "publish":
                        _busMetrics.RecordPublish(BusId, MessageType, durationMs, SubscriberCount);
                        break;

                    case "deliver":
                    case "reliabledeliver":
                        _busMetrics.RecordDelivery(BusId, MessageType, durationMs, _success);
                        break;

                    case "subscribe":
                        _busMetrics.RecordSubscription(BusId, MessageType, durationMs);
                        break;

                    case "unsubscribe":
                        _busMetrics.RecordUnsubscription(BusId, MessageType, durationMs);
                        break;

                    case "process":
                    case "batch":
                    case "queue":
                        // For complex operations, record as a publish operation with appropriate context
                        _busMetrics.RecordPublish(BusId, $"{OperationType}_{MessageType}", durationMs, SubscriberCount);
                        break;

                    default:
                        // For operations not directly supported by the interface,
                        // record as a publish operation with appropriate context
                        _busMetrics.RecordPublish(BusId, $"{OperationType}_{MessageType}", durationMs, SubscriberCount);
                        break;
                }

                // Update bus configuration if we have meaningful data
                if (SubscriberCount > 0 || QueueSize > 0)
                {
                    _busMetrics.UpdateBusConfiguration(BusId, QueueSize, SubscriberCount, BusName, OperationType);
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

        /// <summary>
        /// Creates a deterministic GUID from a string
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return Guid.Empty;

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return new Guid(hash);
            }
        }
    }
}