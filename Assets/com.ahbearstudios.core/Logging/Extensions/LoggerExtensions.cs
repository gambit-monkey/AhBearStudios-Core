using System;
using AhBearStudios.Core.Logging.Data;
using Unity.Collections;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// Provides extension methods for the IBurstLogger interface to support additional functionality.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="level">The severity level of the log (cast to byte).</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        public static void Log(this IBurstLogger burstLogger, int level, string message, string tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            // Clamp level to byte range
            byte byteLevel = level < 0 ? (byte)0 : level > 255 ? (byte)255 : (byte)level;
            burstLogger.Log(byteLevel, message, tag);
        }

        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        public static void Log(this IBurstLogger burstLogger, byte level, string message, Tagging.LogTag tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(level, message, tag.ToString());
        }

        /// <summary>
        /// Logs a message with the specified fixed string content.
        /// Useful for Burst-compatible contexts.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The message content as a FixedString.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        public static void Log(this IBurstLogger burstLogger, byte level, in FixedString512Bytes message,
            in FixedString32Bytes tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(level, message.ToString(), tag.ToString());
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Debug(this IBurstLogger burstLogger, string message, string tag = "Debug")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Debug, message, tag);
        }

        /// <summary>
        /// Logs a debug message with a LogTag enum.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public static void Debug(this IBurstLogger burstLogger, string message, Tagging.LogTag tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Debug, message, tag.ToString());
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Info(this IBurstLogger burstLogger, string message, string tag = "Info")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Info, message, tag);
        }

        /// <summary>
        /// Logs an info message with a LogTag enum.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public static void Info(this IBurstLogger burstLogger, string message, Tagging.LogTag tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Info, message, tag.ToString());
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Warning(this IBurstLogger burstLogger, string message, string tag = "Warning")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Warning, message, tag);
        }

        /// <summary>
        /// Logs a warning message with a LogTag enum.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public static void Warning(this IBurstLogger burstLogger, string message, Tagging.LogTag tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Warning, message, tag.ToString());
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Error(this IBurstLogger burstLogger, string message, string tag = "Error")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Error, message, tag);
        }

        /// <summary>
        /// Logs an error message with a LogTag enum.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public static void Error(this IBurstLogger burstLogger, string message, Tagging.LogTag tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Error, message, tag.ToString());
        }

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Critical(this IBurstLogger burstLogger, string message, string tag = "Critical")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Critical, message, tag);
        }

        /// <summary>
        /// Logs a critical error message with a LogTag enum.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag to categorize the message.</param>
        public static void Critical(this IBurstLogger burstLogger, string message, Tagging.LogTag tag)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Critical, message, tag.ToString());
        }

        /// <summary>
        /// Logs a structured message with properties.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        public static void Log(this IBurstLogger burstLogger, byte level, string message, string tag,
            LogProperties properties)
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(level, message, tag, properties);
        }

        /// <summary>
        /// Logs a structured debug message with properties.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Debug(this IBurstLogger burstLogger, string message, LogProperties properties,
            string tag = "Debug")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Debug, message, tag, properties);
        }

        /// <summary>
        /// Logs a structured info message with properties.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Info(this IBurstLogger burstLogger, string message, LogProperties properties,
            string tag = "Info")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Info, message, tag, properties);
        }

        /// <summary>
        /// Logs a structured warning message with properties.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Warning(this IBurstLogger burstLogger, string message, LogProperties properties,
            string tag = "Warning")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Warning, message, tag, properties);
        }

        /// <summary>
        /// Logs a structured error message with properties.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Error(this IBurstLogger burstLogger, string message, LogProperties properties,
            string tag = "Error")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Error, message, tag, properties);
        }

        /// <summary>
        /// Logs a structured critical message with properties.
        /// </summary>
        /// <param name="burstLogger">The burstLogger instance.</param>
        /// <param name="message">The message content.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        /// <param name="tag">Optional tag identifying the source or category.</param>
        public static void Critical(this IBurstLogger burstLogger, string message, LogProperties properties,
            string tag = "Critical")
        {
            if (burstLogger == null)
                throw new ArgumentNullException(nameof(burstLogger));

            burstLogger.Log(LogLevel.Critical, message, tag, properties);
        }
    }
}