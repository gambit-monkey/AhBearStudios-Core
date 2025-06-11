
using System;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a message bus profiler session starts.
    /// </summary>
    public struct MessageBusProfilerSessionStartedMessage : IMessage
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
        /// Gets the profiler tag associated with the started session.
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Gets the unique identifier for this profiling session.
        /// </summary>
        public Guid SessionId { get; }
        
        /// <summary>
        /// Gets the identifier of the message bus being profiled.
        /// </summary>
        public Guid BusId { get; }
        
        /// <summary>
        /// Gets the name of the message bus being profiled.
        /// </summary>
        public string BusName { get; }
        
        /// <summary>
        /// Gets the type of message bus operation being profiled.
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the number of subscribers at the time the session started.
        /// </summary>
        public int SubscriberCount { get; }
        
        /// <summary>
        /// Gets the queue size at the time the session started.
        /// </summary>
        public int QueueSize { get; }
        
        /// <summary>
        /// Gets the type of message being processed.
        /// </summary>
        public string MessageType { get; }
        
        /// <summary>
        /// Creates a new message bus profiler session started message.
        /// </summary>
        /// <param name="tag">Profiler tag</param>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="busId">Identifier of the message bus</param>
        /// <param name="busName">Name of the message bus</param>
        /// <param name="operationType">Type of operation being profiled</param>
        /// <param name="subscriberCount">Number of subscribers</param>
        /// <param name="queueSize">Current queue size</param>
        /// <param name="messageType">Type of message being processed</param>
        public MessageBusProfilerSessionStartedMessage(
            ProfilerTag tag,
            Guid sessionId,
            Guid busId,
            string busName,
            string operationType,
            int subscriberCount,
            int queueSize,
            string messageType)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0x6004; // Unique type code for this message type
            Tag = tag;
            SessionId = sessionId;
            BusId = busId;
            BusName = busName ?? "Unknown";
            OperationType = operationType ?? "Unknown";
            SubscriberCount = subscriberCount;
            QueueSize = queueSize;
            MessageType = messageType ?? "Unknown";
        }
    }
}