using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when profiling is stopped
    /// </summary>
    public struct ProfilingStoppedMessage : IMessage
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
        public ushort TypeCode => 10003; // You would use your message registry to assign appropriate codes

        /// <summary>
        /// Total profiling duration in milliseconds
        /// </summary>
        public double TotalDurationMs { get; }

        /// <summary>
        /// Creates a new ProfilingStoppedMessage
        /// </summary>
        /// <param name="totalDurationMs">Total profiling duration in milliseconds</param>
        public ProfilingStoppedMessage(double totalDurationMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TotalDurationMs = totalDurationMs;
        }
    }
}