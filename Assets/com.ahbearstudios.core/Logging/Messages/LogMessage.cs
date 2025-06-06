using System;
using AhBearStudios.Core.Logging.Data;
using Unity.Collections;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Represents a log message in a format that is compatible with Unity Collections v2 and
    /// Burst compilation. Designed for high-performance logging with minimal GC allocations.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LogMessage : IEquatable<LogMessage>, IMessage
    {
        /// <summary>
        /// The category tag for this log message.
        /// </summary>
        public Tagging.LogTag Tag;
        
        /// <summary>
        /// Custom tag string, only used when Tag == LogTag.Custom.
        /// </summary>
        public FixedString32Bytes CustomTag;
        
        /// <summary>
        /// The logging severity level.
        /// </summary>
        public LogLevel Level;
        
        /// <summary>
        /// The message content.
        /// </summary>
        public FixedString512Bytes Message;
        
        /// <summary>
        /// Timestamp in ticks (from DateTime.Ticks).
        /// </summary>
        public long TimestampTicks;
        
        /// <summary>
        /// Property for structured data
        /// </summary>
        public LogProperties Properties;
        
        // IMessage implementation fields
        private readonly Guid _id;
        private readonly ushort _typeCode;
        
        /// <inheritdoc />
        public Guid Id => _id;
        
        /// <inheritdoc />
        long IMessage.TimestampTicks => TimestampTicks;
        
        /// <inheritdoc />
        public ushort TypeCode => _typeCode;
        
        /// <summary>
        /// Creates a new log message with the current timestamp.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="properties">Log properties.</param>
        /// <param name="typeCode">Message type code for the message bus (default: 1001).</param>
        public LogMessage(FixedString512Bytes message, LogLevel level, Tagging.LogTag tag, LogProperties properties, ushort typeCode = 1001)
        {
            Message = message;
            Level = level;
            Tag = tag;
            CustomTag = default;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Properties = properties;
            _id = Guid.NewGuid();
            _typeCode = typeCode;
        }
        
        /// <summary>
        /// Creates a new log message with the current timestamp and custom tag.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="customTag">A custom tag string.</param>
        /// <param name="properties">Log Properties</param>
        /// <param name="typeCode">Message type code for the message bus (default: 1001).</param>
        public LogMessage(FixedString512Bytes message, LogLevel level, FixedString32Bytes customTag, LogProperties properties, ushort typeCode = 1001)
        {
            Message = message;
            Level = level;
            Tag = Tagging.LogTag.Custom;
            CustomTag = customTag;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Properties = properties;
            _id = Guid.NewGuid();
            _typeCode = typeCode;
        }

        /// <summary>
        /// Creates a new log message with auto-detection of the appropriate tag.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="properties">Structured Log Properties</param>
        /// <param name="typeCode">Message type code for the message bus (default: 1001).</param>
        public LogMessage(FixedString512Bytes message, LogLevel level, LogProperties properties, ushort typeCode = 1001)
        {
            Message = message;
            Level = level;
            
            // Convert to FixedString128Bytes for compatibility with Tagging.SuggestTagFromContent
            FixedString128Bytes shorterMessage = new FixedString128Bytes();
            // Copy as much as will fit
            for (int i = 0; i < Math.Min(message.Length, 127); i++)
            {
                shorterMessage.Append(message[i]);
            }
            
            Tag = Tagging.SuggestTagFromContent(in shorterMessage);
            CustomTag = default;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Properties = properties;
            _id = Guid.NewGuid();
            _typeCode = typeCode;
        }
        
        /// <summary>
        /// Creates a new log message with minimal parameters (for backwards compatibility).
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="tag">The log tag (default: Default).</param>
        /// <param name="typeCode">Message type code for the message bus (default: 1001).</param>
        public LogMessage(FixedString512Bytes message, LogLevel level, Tagging.LogTag tag = Tagging.LogTag.Default, ushort typeCode = 1001)
        {
            Message = message;
            Level = level;
            Tag = tag;
            CustomTag = default;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Properties = new LogProperties(0); // Empty properties
            _id = Guid.NewGuid();
            _typeCode = typeCode;
        }
        
        /// <summary>
        /// Initializes the properties collection if it doesn't already exist.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for the properties collection.</param>
        public void InitializeProperties(int initialCapacity = 8)
        {
            if (!Properties.IsCreated)
            {
                Properties = new LogProperties(initialCapacity);
            }
        }
        
        /// <summary>
        /// Gets the appropriate tag string, handling both enum tag and custom tag.
        /// </summary>
        /// <returns>A string representation of the tag.</returns>
        public FixedString32Bytes GetTagString()
        {
            if (Tag == Tagging.LogTag.Custom && !CustomTag.IsEmpty)
            {
                return CustomTag;
            }
            
            return Tagging.GetTagString(Tag);
        }
        
        /// <summary>
        /// Formats the log message with timestamp and tag for display.
        /// </summary>
        /// <returns>A formatted log message string.</returns>
        public FixedString512Bytes FormatMessage()
        {
            var tagString = GetTagString();
            
            // Format timestamp without creating DateTime string allocation
            long ticks = TimestampTicks;
            DateTime dt = new DateTime(ticks);
            FixedString32Bytes timestamp = new FixedString32Bytes();
            timestamp.Append(dt.Hour.ToString("00"));
            timestamp.Append(':');
            timestamp.Append(dt.Minute.ToString("00"));
            timestamp.Append(':');
            timestamp.Append(dt.Second.ToString("00"));
            
            // Combine all parts into result
            FixedString512Bytes result = new FixedString512Bytes();
            result.Append('[');
            result.Append(timestamp);
            result.Append("] [");
            result.Append(tagString);
            result.Append("] ");
            result.Append(Message);
            
            return result;
        }
        
        /// <summary>
        /// Formats the log message with full date/time timestamp for display.
        /// </summary>
        /// <returns>A formatted log message string with full timestamp.</returns>
        public FixedString512Bytes FormatMessageWithFullTimestamp()
        {
            var tagString = GetTagString();
            
            // Format full timestamp
            DateTime dt = new DateTime(TimestampTicks);
            FixedString64Bytes timestamp = new FixedString64Bytes();
            timestamp.Append(dt.Year.ToString("0000"));
            timestamp.Append('-');
            timestamp.Append(dt.Month.ToString("00"));
            timestamp.Append('-');
            timestamp.Append(dt.Day.ToString("00"));
            timestamp.Append(' ');
            timestamp.Append(dt.Hour.ToString("00"));
            timestamp.Append(':');
            timestamp.Append(dt.Minute.ToString("00"));
            timestamp.Append(':');
            timestamp.Append(dt.Second.ToString("00"));
            
            // Combine all parts into result
            FixedString512Bytes result = new FixedString512Bytes();
            result.Append('[');
            result.Append(timestamp);
            result.Append("] [");
            result.Append(tagString);
            result.Append("] ");
            result.Append(Message);
            
            return result;
        }
        
        /// <summary>
        /// Checks if this log entry belongs to a specific tag category.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <returns>True if the log belongs to the specified category.</returns>
        public bool IsInCategory(Tagging.TagCategory category)
        {
            return Tagging.IsInCategory(Tag, category);
        }
        
        /// <summary>
        /// Checks if this log represents an error or critical issue.
        /// </summary>
        /// <returns>True if this is an error or critical log.</returns>
        public bool IsError()
        {
            return Tagging.IsError(Tag);
        }
        
        /// <summary>
        /// Checks if this log represents a warning or worse.
        /// </summary>
        /// <returns>True if this is a warning, error, or critical log.</returns>
        public bool IsWarningOrWorse()
        {
            return Tagging.IsWarningOrWorse(Tag);
        }
        
        /// <summary>
        /// Checks if this log is system-related.
        /// </summary>
        /// <returns>True if this log is system-related.</returns>
        public bool IsSystemRelated()
        {
            return Tagging.IsSystemRelated(Tag);
        }
        
        /// <summary>
        /// Gets the severity level as a string.
        /// </summary>
        /// <returns>String representation of the log level.</returns>
        public FixedString32Bytes GetLevelString()
        {
            return Level.ToString();
        }
        
        /// <summary>
        /// Creates a copy of this log message with a different level.
        /// </summary>
        /// <param name="newLevel">The new log level.</param>
        /// <returns>A new LogMessage with the specified level.</returns>
        public LogMessage WithLevel(LogLevel newLevel)
        {
            return new LogMessage(Message, newLevel, Tag, Properties.Copy(), _typeCode);
        }
        
        /// <summary>
        /// Creates a copy of this log message with a different tag.
        /// </summary>
        /// <param name="newTag">The new log tag.</param>
        /// <returns>A new LogMessage with the specified tag.</returns>
        public LogMessage WithTag(Tagging.LogTag newTag)
        {
            return new LogMessage(Message, Level, newTag, Properties.Copy(), _typeCode);
        }
        
        /// <summary>
        /// Creates a copy of this log message with additional properties.
        /// </summary>
        /// <param name="additionalProperties">Additional properties to add.</param>
        /// <returns>A new LogMessage with the additional properties.</returns>
        public LogMessage WithProperties(in LogProperties additionalProperties)
        {
            var newProperties = Properties.Copy();
            newProperties.AddRange(additionalProperties);
            
            return new LogMessage(Message, Level, Tag, newProperties, _typeCode);
        }
        
        /// <summary>
        /// Disposes any native resources held by this log message.
        /// </summary>
        public void Dispose()
        {
            if (Properties.IsCreated)
            {
                Properties.Dispose();
            }
        }
        
        /// <summary>
        /// Equality comparison.
        /// </summary>
        public bool Equals(LogMessage other)
        {
            return Level == other.Level && 
                   Tag.Equals(other.Tag) && 
                   Message.Equals(other.Message) &&
                   CustomTag.Equals(other.CustomTag) &&
                   Properties.Equals(other.Properties) &&
                   _id.Equals(other._id);
        }
        
        public override bool Equals(object obj)
        {
            return obj is LogMessage other && Equals(other);
        }
        
        /// <summary>
        /// Gets a hash code for the log message.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Level.GetHashCode();
                hash = hash * 23 + Tag.GetHashCode();
                hash = hash * 23 + Message.GetHashCode();
                hash = hash * 23 + CustomTag.GetHashCode();
                hash = hash * 23 + Properties.GetHashCode();
                hash = hash * 23 + _id.GetHashCode();
                return hash;
            }
        }
        
        /// <summary>
        /// Returns a string representation of the log message for debugging.
        /// </summary>
        public override string ToString()
        {
            return FormatMessage().ToString();
        }
        
        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(LogMessage left, LogMessage right)
        {
            return left.Equals(right);
        }
        
        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(LogMessage left, LogMessage right)
        {
            return !(left == right);
        }
    }
}