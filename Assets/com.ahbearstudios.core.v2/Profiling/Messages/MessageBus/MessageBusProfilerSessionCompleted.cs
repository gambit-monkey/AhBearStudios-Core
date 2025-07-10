using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a message bus profiler session completes.
    /// </summary>
    public struct MessageBusProfilerSessionCompletedMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode { get; }
        
        /// <summary>
        /// Gets the profiler tag associated with the completed session.
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Gets the unique identifier for this profiling session.
        /// </summary>
        public Guid SessionId { get; }
        
        /// <summary>
        /// Gets the identifier of the message bus that was profiled.
        /// </summary>
        public Guid BusId { get; }
        
        /// <summary>
        /// Gets the name of the message bus that was profiled.
        /// </summary>
        public string BusName { get; }
        
        /// <summary>
        /// Gets the type of message bus operation that was profiled.
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the number of subscribers at the time of the operation.
        /// </summary>
        public int SubscriberCount { get; }
        
        /// <summary>
        /// Gets the queue size at the time of the operation.
        /// </summary>
        public int QueueSize { get; }
        
        /// <summary>
        /// Gets the message type associated with the operation.
        /// </summary>
        public string MessageType { get; }
        
        /// <summary>
        /// Gets the duration of the operation in milliseconds.
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// Gets the custom metrics collected during the session.
        /// </summary>
        public IReadOnlyDictionary<string, double> CustomMetrics { get; }
        
        /// <summary>
        /// Creates a new message bus profiler session completed message.
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="busId">Identifier of the message bus</param>
        /// <param name="busName">Name of the message bus</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="subscriberCount">Number of subscribers</param>
        /// <param name="queueSize">Queue size</param>
        /// <param name="messageType">Type of message</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <param name="customMetrics">Custom metrics</param>
        public MessageBusProfilerSessionCompletedMessage(
            ProfilerTag tag,
            Guid sessionId,
            Guid busId,
            string busName,
            string operationType,
            int subscriberCount,
            int queueSize,
            string messageType,
            double durationMs,
            IReadOnlyDictionary<string, double> customMetrics = null)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x6003; // Unique type code for this message type
            Tag = tag;
            SessionId = sessionId;
            BusId = busId;
            BusName = busName ?? "Unknown";
            OperationType = operationType ?? "Unknown";
            SubscriberCount = subscriberCount;
            QueueSize = queueSize;
            MessageType = messageType ?? "Unknown";
            DurationMs = durationMs;
            CustomMetrics = customMetrics ?? new Dictionary<string, double>();
        }
    }
}