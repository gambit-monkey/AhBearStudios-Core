using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
    /// Base record implementation for all messages in the system.
    /// Provides default implementations of IMessage interface requirements.
    /// </summary>
    public abstract record BaseMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <inheritdoc />
        public long TimestampTicks { get; init; } = DateTime.UtcNow.Ticks;

        /// <inheritdoc />
        public abstract ushort TypeCode { get; }

        /// <inheritdoc />
        public FixedString64Bytes Source { get; init; } = "Unknown";

        /// <inheritdoc />
        public MessagePriority Priority { get; init; } = MessagePriority.Normal;

        /// <inheritdoc />
        public Guid CorrelationId { get; init; } = Guid.Empty;

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Initializes a new instance of the BaseMessage record.
        /// </summary>
        protected BaseMessage() { }

        /// <summary>
        /// Initializes a new instance of the BaseMessage record with the specified source.
        /// </summary>
        /// <param name="source">The source system or component creating this message</param>
        protected BaseMessage(FixedString64Bytes source)
        {
            Source = source;
        }

        /// <summary>
        /// Initializes a new instance of the BaseMessage record with source and priority.
        /// </summary>
        /// <param name="source">The source system or component creating this message</param>
        /// <param name="priority">The message priority</param>
        protected BaseMessage(FixedString64Bytes source, MessagePriority priority)
        {
            Source = source;
            Priority = priority;
        }

        /// <summary>
        /// Initializes a new instance of the BaseMessage record with full metadata.
        /// </summary>
        /// <param name="source">The source system or component creating this message</param>
        /// <param name="priority">The message priority</param>
        /// <param name="correlationId">The correlation ID for message tracing</param>
        protected BaseMessage(FixedString64Bytes source, MessagePriority priority, Guid correlationId)
        {
            Source = source;
            Priority = priority;
            CorrelationId = correlationId;
        }
    }