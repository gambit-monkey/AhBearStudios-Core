namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Defines standard log levels as byte constants.
    /// Higher values indicate more severe log levels.
    /// </summary>
    public static class LogLevel
    {
        /// <summary>
        /// Trace level - most detailed information, typically only enabled during development.
        /// </summary>
        public const byte Trace = 0;

        /// <summary>
        /// Debug level - detailed information for debugging purposes.
        /// </summary>
        public const byte Debug = 10;

        /// <summary>
        /// Info level - informational messages that highlight progress or state.
        /// </summary>
        public const byte Info = 20;

        /// <summary>
        /// Warning level - potentially harmful situations or unexpected behavior.
        /// </summary>
        public const byte Warning = 30;

        /// <summary>
        /// Error level - error events that might still allow the application to continue.
        /// </summary>
        public const byte Error = 40;

        /// <summary>
        /// Critical level - very severe error events that will likely cause the application to abort.
        /// </summary>
        public const byte Critical = 50;

        /// <summary>
        /// None level - logging is disabled.
        /// </summary>
        public const byte None = byte.MaxValue;

        /// <summary>
        /// Gets the name of a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The name of the log level.</returns>
        public static string GetName(byte level)
        {
            if (level >= Critical)
                return "CRITICAL";
            if (level >= Error)
                return "ERROR";
            if (level >= Warning)
                return "WARNING";
            if (level >= Info)
                return "INFO";
            if (level >= Debug)
                return "DEBUG";
            if (level >= Trace)
                return "TRACE";
            
            return "UNKNOWN";
        }

        /// <summary>
        /// Gets a short name (3 characters) for a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>A 3-character abbreviation of the log level.</returns>
        public static string GetShortName(byte level)
        {
            if (level >= Critical)
                return "CRT";
            if (level >= Error)
                return "ERR";
            if (level >= Warning)
                return "WRN";
            if (level >= Info)
                return "INF";
            if (level >= Debug)
                return "DBG";
            if (level >= Trace)
                return "TRC";
            
            return "UNK";
        }
    }
}