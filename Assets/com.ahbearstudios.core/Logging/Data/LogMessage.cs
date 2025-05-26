using System;
using Unity.Collections;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// Represents a log message in a format that is compatible with Unity Collections v2 and
    /// Burst compilation. Designed for high-performance logging with minimal GC allocations.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct LogMessage : IEquatable<LogMessage>
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
        public byte Level;
        
        /// <summary>
        /// The message content.
        /// </summary>
        public FixedString512Bytes Message;
        
        /// <summary>
        /// Timestamp in ticks (from DateTime.Ticks).
        /// </summary>
        public long TimestampTicks;
        
        // New property for structured data
        public LogProperties Properties;
        
        /// <summary>
        /// Creates a new log message with the current timestamp.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="tag">The log tag.</param>
        public LogMessage(FixedString512Bytes message, byte level, Tagging.LogTag tag, LogProperties properties)
        {
            Message = message;
            Level = level;
            Tag = tag;
            CustomTag = default;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Properties = properties;
        }
        
        /// <summary>
        /// Creates a new log message with the current timestamp and custom tag.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="customTag">A custom tag string.</param>
        /// <param name="properties">Log Properties</param>
        public LogMessage(FixedString512Bytes message, byte level, FixedString32Bytes customTag, LogProperties properties)
        {
            Message = message;
            Level = level;
            Tag = Tagging.LogTag.Custom;
            CustomTag = customTag;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Properties = properties;
        }

        /// <summary>
        /// Creates a new log message with auto-detection of the appropriate tag.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="level">The severity level.</param>
        /// <param name="properties">Structured Log Properties</param>
        public LogMessage(FixedString512Bytes message, byte level, LogProperties properties)
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
        
        // /// <summary>
        // /// Equality comparison.
        // /// </summary>
        // public bool Equals(LogMessage other)
        // {
        //     return Tag == other.Tag &&
        //            CustomTag.Equals(other.CustomTag) &&
        //            Level == other.Level &&
        //            Message.Equals(other.Message) &&
        //            TimestampTicks == other.TimestampTicks;
        // }
        
        // /// <summary>
        // /// Gets a hash code for the log message.
        // /// </summary>
        // public override int GetHashCode()
        // {
        //     return HashCode.Combine(
        //         (int)Tag,
        //         CustomTag.GetHashCode(),
        //         Level,
        //         Message.GetHashCode(),
        //         TimestampTicks.GetHashCode());
        // }
        
        /// <summary>
        /// Equality comparison.
        /// </summary>
        public bool Equals(LogMessage other)
        {
            return Level == other.Level && 
                   Tag.Equals(other.Tag) && 
                   Message.Equals(other.Message) &&
                   Properties.Equals(other.Properties);
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
                hash = hash * 23 + Properties.GetHashCode();
                return hash;
            }
        }
    }
}