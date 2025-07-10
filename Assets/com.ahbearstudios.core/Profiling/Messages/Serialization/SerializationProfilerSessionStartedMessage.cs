using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a serialization profiler session is started
    /// </summary>
    public struct SerializationProfilerSessionStartedMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this message was created
        /// </summary>
        public long TimestampTicks { get; }

        /// <summary>
        /// Gets the type code for this message
        /// </summary>
        public ushort TypeCode => 7003; // Arbitrary type code for this message

        /// <summary>
        /// Gets the profiler tag for the session
        /// </summary>
        public readonly ProfilerTag Tag;

        /// <summary>
        /// Gets the session identifier
        /// </summary>
        public readonly Guid SessionId;

        /// <summary>
        /// Gets the serializer identifier
        /// </summary>
        public readonly Guid SerializerId;

        /// <summary>
        /// Gets the serializer name
        /// </summary>
        public readonly string SerializerName;

        /// <summary>
        /// Gets the message identifier
        /// </summary>
        public readonly Guid MessageId;

        /// <summary>
        /// Gets the message type code
        /// </summary>
        public readonly ushort MessageTypeCode;

        /// <summary>
        /// Gets the size of the data in bytes
        /// </summary>
        public readonly int DataSize;

        /// <summary>
        /// Creates a new serialization profiler session started message
        /// </summary>
        public SerializationProfilerSessionStartedMessage(
            ProfilerTag tag,
            Guid sessionId,
            Guid serializerId,
            string serializerName,
            Guid messageId,
            ushort messageTypeCode,
            int dataSize)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = tag;
            SessionId = sessionId;
            SerializerId = serializerId;
            SerializerName = serializerName;
            MessageId = messageId;
            MessageTypeCode = messageTypeCode;
            DataSize = dataSize;
        }
    }
}