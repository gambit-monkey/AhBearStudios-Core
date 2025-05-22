using System;

namespace AhBearStudios.Core.Messaging
{
    /// <summary>
    /// Interface that marks a struct as a message that can be used with the message bus system.
    /// Designed to be compatible with Burst compilation and Unity Collections v2.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance.
        /// </summary>
        Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// Used for serialization and deserialization.
        /// </summary>
        ushort TypeCode { get; }
    }
}