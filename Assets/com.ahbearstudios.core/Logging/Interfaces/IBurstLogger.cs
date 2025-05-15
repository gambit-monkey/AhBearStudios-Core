using System;
using AhBearStudios.Core.Logging.Data;
using Unity.Collections;
using AhBearStudios.Core.Logging.Tags;

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
    }
}