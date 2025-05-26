using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a profiling session is completed
    /// </summary>
    public struct ProfilerSessionCompletedMessage : IMessage
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
        public ushort TypeCode => 10001; // You would use your message registry to assign appropriate codes

        /// <summary>
        /// The profiler tag associated with this session
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// The duration of the session in milliseconds
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// Custom metrics recorded during the session
        /// </summary>
        public IReadOnlyDictionary<string, double> Metrics { get; }
        
        /// <summary>
        /// The session identifier
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Creates a new ProfilerSessionCompletedMessage with specified parameters
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <param name="metrics">Optional custom metrics</param>
        /// <param name="sessionId">Optional session identifier (defaults to a new GUID if not specified)</param>
        public ProfilerSessionCompletedMessage(ProfilerTag tag, double durationMs, 
            IReadOnlyDictionary<string, double> metrics = null, Guid sessionId = default)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = tag;
            DurationMs = durationMs;
            Metrics = metrics ?? new Dictionary<string, double>();
            SessionId = sessionId == default ? Guid.NewGuid() : sessionId;
        }
        
        /// <summary>
        /// Creates a new ProfilerSessionCompletedMessage with specified parameters
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <param name="metrics">Custom metrics as a Dictionary</param>
        /// <param name="sessionId">Session identifier</param>
        public ProfilerSessionCompletedMessage(ProfilerTag tag, double durationMs, 
            Dictionary<string, double> metrics, Guid sessionId)
            : this(tag, durationMs, (IReadOnlyDictionary<string, double>)metrics, sessionId)
        {
        }
    }
}