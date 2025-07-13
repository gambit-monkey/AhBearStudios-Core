using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages
{
    /// <summary>
    /// Base interface for all messages in the messaging system.
    /// Provides core message metadata and identification.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message instance.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this message was created, in UTC ticks.
        /// </summary>
        long TimestampTicks { get; }

        /// <summary>
        /// Gets the message type code for efficient routing and filtering.
        /// </summary>
        ushort TypeCode { get; }

        /// <summary>
        /// Gets the source system or component that created this message.
        /// </summary>
        FixedString64Bytes Source { get; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        MessagePriority Priority { get; }

        /// <summary>
        /// Gets optional correlation ID for message tracing across systems.
        /// </summary>
        Guid CorrelationId { get; }
    }
}