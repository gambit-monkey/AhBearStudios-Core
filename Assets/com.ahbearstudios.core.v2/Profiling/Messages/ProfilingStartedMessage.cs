using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when profiling is started
    /// </summary>
    public struct ProfilingStartedMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode => 10002; // You would use your message registry to assign appropriate codes

        /// <summary>
        /// Creates a new ProfilingStartedMessage with specified parameters
        /// </summary>
        /// <param name="id">Optional message ID (defaults to a new GUID if not specified)</param>
        /// <param name="timestampTicks">Optional timestamp (defaults to current UTC time if not specified)</param>
        public ProfilingStartedMessage(Guid id, long timestampTicks)
        {
            Id = id;
            TimestampTicks = timestampTicks;
        }
        
        /// <summary>
        /// Creates a new ProfilingStartedMessage with default values
        /// </summary>
        /// <returns>A new message with automatically generated ID and timestamp</returns>
        public static ProfilingStartedMessage CreateDefault()
        {
            return new ProfilingStartedMessage(Guid.NewGuid(), DateTime.UtcNow.Ticks);
        }
    }
}