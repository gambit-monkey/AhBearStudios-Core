using Unity.Collections;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Interface for formatters that convert log messages to formatted strings.
    /// </summary>
    public interface ILogFormatter
    {
        /// <summary>
        /// Formats a log message into a string representation.
        /// </summary>
        /// <param name="message">The log message to format.</param>
        /// <returns>A formatted string representation of the log message.</returns>
        FixedString512Bytes Format(LogMessage message);
        
        /// <summary>
        /// Determines whether this formatter supports structured logging.
        /// </summary>
        bool SupportsStructuredLogging { get; }
    }
}