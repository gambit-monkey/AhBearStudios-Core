using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a profiler session is started
    /// </summary>
    public struct ProfilerSessionStartedMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 10010; // Assign an appropriate type code

        /// <summary>
        /// The profiler tag associated with this session
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Session identifier
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Creates a new ProfilerSessionStartedMessage
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="sessionId">Session identifier</param>
        public ProfilerSessionStartedMessage(ProfilerTag tag, Guid sessionId)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = tag;
            SessionId = sessionId;
        }
    }
}