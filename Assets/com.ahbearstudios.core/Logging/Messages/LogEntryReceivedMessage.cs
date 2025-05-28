using System;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when a log message has been received by the batch processor.
    /// Represents a message-based alternative to the legacy event system.
    /// </summary>
    public struct LogEntryReceivedMessage : IMessage
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
        /// Gets the log message that was received.
        /// </summary>
        public LogMessage LogMessage { get; }
        
        /// <summary>
        /// Creates a new LogMessageReceivedMessage instance.
        /// </summary>
        /// <param name="logMessage">The log message that was received.</param>
        public LogEntryReceivedMessage(LogMessage logMessage)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            LogMessage = logMessage;
        }
    }
}