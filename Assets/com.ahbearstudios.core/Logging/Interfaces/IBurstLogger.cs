using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Interface for logger implementations that can receive and handle log messages.
    /// This interface is designed to be compatible with various logging backends.
    /// </summary>
    public interface IBurstLogger
    {
        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="level">The severity level of the log (0-255 with higher values indicating more severity).</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        void Log(byte level, string message, string tag);
        
        /// <summary>
        /// Logs a structured message with properties.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        void Log(byte level, string message, string tag, LogProperties properties);
        
        /// <summary>
        /// Checks if logging is enabled for the specified log level.
        /// This allows for performance optimization by avoiding expensive log message construction
        /// when the message would not be logged anyway.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if messages at this level would be logged; otherwise, false.</returns>
        bool IsEnabled(byte level);
    }
}