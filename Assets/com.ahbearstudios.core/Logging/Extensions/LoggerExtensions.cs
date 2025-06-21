using System;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Data;

namespace AhBearStudios.Core.Logging.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IBurstLoggingService"/>, honoring
    /// the new LogTag + LogProperties constructors on LogMessage.
    /// </summary>
    public static class BurstLoggingServiceExtensions
    {
        // ——— Simple message (default tag) ———

        public static void Log(this IBurstLoggingService logger, LogLevel level, string message)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            var fsMsg = new FixedString512Bytes(message);
            logger.Log(fsMsg, level);
        }

        public static void Debug(this IBurstLoggingService logger, string message)
            => logger.Log(LogLevel.Debug, message);
        public static void Info(this IBurstLoggingService logger, string message)
            => logger.Log(LogLevel.Info, message);
        public static void Warn(this IBurstLoggingService logger, string message)
            => logger.Log(LogLevel.Warning, message);
        public static void Error(this IBurstLoggingService logger, string message)
            => logger.Log(LogLevel.Error, message);
        public static void Critical(this IBurstLoggingService logger, string message)
            => logger.Log(LogLevel.Critical, message);

        // ——— Message + structured properties (default tag) ———

        public static void Log(this IBurstLoggingService logger, LogLevel level, string message, in LogProperties properties)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            var fsMsg = new FixedString512Bytes(message);
            logger.Log(fsMsg, level, properties);
        }

        // ——— Tagged message ———

        public static void Log(this IBurstLoggingService logger, Tagging.LogTag tag, string message, LogLevel level)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            var fsMsg = new FixedString512Bytes(message);
            logger.Log(tag, fsMsg, level);
        }

        public static void Debug(this IBurstLoggingService logger, Tagging.LogTag tag, string message)
            => logger.Log(tag, message, LogLevel.Debug);
        public static void Info(this IBurstLoggingService logger, Tagging.LogTag tag, string message)
            => logger.Log(tag, message, LogLevel.Info);
        public static void Warn(this IBurstLoggingService logger, Tagging.LogTag tag, string message)
            => logger.Log(tag, message, LogLevel.Warning);
        public static void Error(this IBurstLoggingService logger, Tagging.LogTag tag, string message)
            => logger.Log(tag, message, LogLevel.Error);
        public static void Critical(this IBurstLoggingService logger, Tagging.LogTag tag, string message)
            => logger.Log(tag, message, LogLevel.Critical);

        // ——— Tagged + structured properties ———

        public static void Log(this IBurstLoggingService logger, Tagging.LogTag tag, string message, LogLevel level, in LogProperties properties)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            var fsMsg = new FixedString512Bytes(message);
            logger.Log(tag, fsMsg, level, properties);
        }

        // ——— Dynamic string → LogTag resolution ———

        public static void Log(this IBurstLoggingService logger, string category, string message, LogLevel level)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            var tag = Tagging.GetLogTag(category);
            logger.Log(tag, message, level);
        }

        public static void Debug(this IBurstLoggingService logger, string category, string message)
            => logger.Log(category, message, LogLevel.Debug);
        public static void Info(this IBurstLoggingService logger, string category, string message)
            => logger.Log(category, message, LogLevel.Info);
        public static void Warn(this IBurstLoggingService logger, string category, string message)
            => logger.Log(category, message, LogLevel.Warning);
        public static void Error(this IBurstLoggingService logger, string category, string message)
            => logger.Log(category, message, LogLevel.Error);
        public static void Critical(this IBurstLoggingService logger, string category, string message)
            => logger.Log(category, message, LogLevel.Critical);

        // ——— Raw FixedString overloads ———

        public static void Log(this IBurstLoggingService logger, in FixedString512Bytes message, LogLevel level)
            => logger.Log(message, level);

        public static void Log(this IBurstLoggingService logger, in FixedString512Bytes message, LogLevel level, in LogProperties properties)
            => logger.Log(message, level, properties);

        public static void Log(this IBurstLoggingService logger, Tagging.LogTag tag, in FixedString512Bytes message, LogLevel level)
            => logger.Log(tag, message, level);

        public static void Log(this IBurstLoggingService logger, Tagging.LogTag tag, in FixedString512Bytes message, LogLevel level, in LogProperties properties)
            => logger.Log(tag, message, level, properties);
    }
}
