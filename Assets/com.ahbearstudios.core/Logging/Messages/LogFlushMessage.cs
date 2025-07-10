using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when the log queue is flushed.
    /// Represents a message-based alternative to LogFlushEventArgs.
    /// </summary>
    public struct LogFlushMessage : IMessage
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
        public ushort TypeCode => 10102; // Assign appropriate code from your message registry
        
        /// <summary>
        /// Gets the number of messages processed during the flush.
        /// </summary>
        public int ProcessedCount { get; }
        
        /// <summary>
        /// Gets the duration of the flush operation in milliseconds.
        /// </summary>
        public float DurationMs { get; }
        
        /// <summary>
        /// Creates a new LogFlushMessage instance.
        /// </summary>
        /// <param name="processedCount">The number of messages processed during the flush.</param>
        /// <param name="durationMs">The duration of the flush operation in milliseconds.</param>
        public LogFlushMessage(int processedCount, float durationMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ProcessedCount = processedCount;
            DurationMs = durationMs;
        }
    }
}