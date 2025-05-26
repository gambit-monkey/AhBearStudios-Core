using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when profiling stats are reset
    /// </summary>
    public struct StatsResetMessage : IMessage
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
        public ushort TypeCode => 10004; // You would use your message registry to assign appropriate codes

        /// <summary>
        /// Creates a new StatsResetMessage with specified parameters
        /// </summary>
        /// <param name="id">Message identifier</param>
        /// <param name="timestampTicks">Message creation timestamp in UTC ticks</param>
        public StatsResetMessage(Guid id, long timestampTicks)
        {
            Id = id;
            TimestampTicks = timestampTicks;
        }
        
        /// <summary>
        /// Creates a new StatsResetMessage with default values
        /// </summary>
        /// <returns>A new message with automatically generated ID and timestamp</returns>
        public static StatsResetMessage CreateDefault()
        {
            return new StatsResetMessage(Guid.NewGuid(), DateTime.UtcNow.Ticks);
        }
    }
}