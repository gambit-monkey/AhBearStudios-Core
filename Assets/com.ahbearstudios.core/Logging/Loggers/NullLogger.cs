using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// A no-op implementation of IBurstLogger that discards all log messages.
    /// Useful for testing or when logging needs to be completely disabled with minimal overhead.
    /// </summary>
    public sealed class NullLogger : IBurstLogger
    {
        private static readonly NullLogger _instance = new NullLogger();

        /// <summary>
        /// Gets the singleton instance of the NullLogger.
        /// </summary>
        public static NullLogger Instance => _instance;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private NullLogger()
        {
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, string tag)
        {
            // No-op: discard the message
        }

        /// <inheritdoc />
        public void Log(LogLevel level, string message, string tag, LogProperties properties)
        {
            // No-op: discard the message
            // Note: Caller is responsible for disposing properties if needed
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel level)
        {
            // Always return false since we don't log anything
            return false;
        }
    }
}