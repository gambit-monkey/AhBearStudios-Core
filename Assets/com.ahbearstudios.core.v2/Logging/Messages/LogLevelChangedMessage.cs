using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when the global log level is changed.
    /// Represents a message-based alternative to LogLevelChangedEventArgs.
    /// </summary>
    public struct LogLevelChangedMessage : IMessage
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
        public ushort TypeCode => 10103; // Assign appropriate code from your message registry
        
        /// <summary>
        /// Gets the previous log level.
        /// </summary>
        public LogLevel OldLevel { get; }
        
        /// <summary>
        /// Gets the new log level.
        /// </summary>
        public LogLevel NewLevel { get; }
        
        /// <summary>
        /// Creates a new LogLevelChangedMessage instance.
        /// </summary>
        /// <param name="oldLevel">The previous log level.</param>
        /// <param name="newLevel">The new log level.</param>
        public LogLevelChangedMessage(LogLevel oldLevel, LogLevel newLevel)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
        
        /// <summary>
        /// Creates a new LogLevelChangedMessage instance from byte values.
        /// </summary>
        /// <param name="oldLevel">The previous log level as a byte.</param>
        /// <param name="newLevel">The new log level as a byte.</param>
        public LogLevelChangedMessage(byte oldLevel, byte newLevel)
            : this((LogLevel)oldLevel, (LogLevel)newLevel)
        {
        }
    }
}