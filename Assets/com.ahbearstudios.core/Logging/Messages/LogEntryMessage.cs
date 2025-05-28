using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when a log entry is created, processed, or filtered.
    /// Represents a message-based alternative to LogMessageEventArgs.
    /// </summary>
    public struct LogEntryMessage : IMessage
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
        public ushort TypeCode => 10100; // Assign appropriate code from your message registry
        
        /// <summary>
        /// Gets the log message that triggered this event.
        /// </summary>
        public LogMessage Entry { get; }
        
        /// <summary>
        /// Creates a new LogEntryMessage instance.
        /// </summary>
        /// <param name="logMessage">The log message that triggered this event.</param>
        public LogEntryMessage(LogMessage logMessage)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Entry = logMessage;
        }
    }
}