using System;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Defines standard log severity levels.
    /// Higher values indicate more severe log levels.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// Trace level - most detailed information, typically only enabled during development.
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Debug level - detailed information for debugging purposes.
        /// </summary>
        Debug = 10,

        /// <summary>
        /// Info level - informational messages that highlight progress or state.
        /// </summary>
        Info = 20,

        /// <summary>
        /// Warning level - potentially harmful situations or unexpected behavior.
        /// </summary>
        Warning = 30,

        /// <summary>
        /// Error level - error events that might still allow the application to continue.
        /// </summary>
        Error = 40,

        /// <summary>
        /// Critical level - very severe error events that will likely cause the application to abort.
        /// </summary>
        Critical = 50,

        /// <summary>
        /// None level - logging is disabled.
        /// </summary>
        None = byte.MaxValue
    }
    
    /// <summary>
    /// Extension methods for LogLevel enum.
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// Gets the name of a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The name of the log level.</returns>
        public static string GetName(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Info => "INFO",
                LogLevel.Warning => "WARNING",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRITICAL",
                LogLevel.None => "NONE",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Gets a short name (3 characters) for a log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>A 3-character abbreviation of the log level.</returns>
        public static string GetShortName(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Info => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                LogLevel.None => "NON",
                _ => "UNK"
            };
        }
        
        /// <summary>
        /// Checks if a log level is at or above the specified minimum level.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <param name="minimumLevel">The minimum log level threshold.</param>
        /// <returns>True if the level is at or above the minimum, false otherwise.</returns>
        public static bool IsAtLeast(this LogLevel level, LogLevel minimumLevel)
        {
            return (byte)level >= (byte)minimumLevel;
        }
        
        /// <summary>
        /// Converts a string to a log level.
        /// </summary>
        /// <param name="levelName">The name of the log level.</param>
        /// <param name="defaultLevel">The default level to return if parsing fails.</param>
        /// <returns>The parsed log level or the default if parsing fails.</returns>
        public static LogLevel ParseLogLevel(string levelName, LogLevel defaultLevel = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(levelName))
                return defaultLevel;
                
            return levelName.ToUpperInvariant() switch
            {
                "TRACE" => LogLevel.Trace,
                "DEBUG" => LogLevel.Debug,
                "INFO" => LogLevel.Info,
                "WARNING" => LogLevel.Warning,
                "WARN" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                "ERR" => LogLevel.Error,
                "CRITICAL" => LogLevel.Critical,
                "FATAL" => LogLevel.Critical,
                "NONE" => LogLevel.None,
                _ => defaultLevel
            };
        }
    }
}