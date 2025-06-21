using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Provides high-level logging capabilities: routing, configuration, batching, flushing,
    /// and automatic publication of <see cref="LogEntryMessage"/> over the message bus.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Logs a message at the specified level and publishes a <see cref="LogEntryMessage"/> on the bus.
        /// </summary>
        /// <param name="level">The severity level of the log entry.</param>
        /// <param name="message">The message to log.</param>
        void Log(LogLevel level, string message);

        /// <summary>
        /// Logs a formatted message at the specified level and publishes a <see cref="LogEntryMessage"/> on the bus.
        /// </summary>
        /// <param name="level">The severity level of the log entry.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An array of objects to format.</param>
        void Log(LogLevel level, string format, params object[] args);

        /// <summary>
        /// Logs an exception, with an optional context message, at <see cref="LogLevel.Error"/>,
        /// and publishes a <see cref="LogEntryMessage"/> on the bus.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="contextMessage">An optional message providing context.</param>
        void LogException(Exception exception, string contextMessage = null);

        /// <summary>
        /// Convenience for <see cref="Log(LogLevel, string)"/> at <see cref="LogLevel.Debug"/>.
        /// </summary>
        void Debug(string message);

        /// <summary>
        /// Convenience for <see cref="Log(LogLevel, string)"/> at <see cref="LogLevel.Info"/>.
        /// </summary>
        void Info(string message);

        /// <summary>
        /// Convenience for <see cref="Log(LogLevel, string)"/> at <see cref="LogLevel.Warning"/>.
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// Convenience for <see cref="Log(LogLevel, string)"/> at <see cref="LogLevel.Error"/>.
        /// </summary>
        void Error(string message);

        /// <summary>
        /// Convenience for <see cref="Log(LogLevel, string)"/> at <see cref="LogLevel.Critical"/>.
        /// </summary>
        void Critical(string message);

        /// <summary>
        /// Immediately flushes any buffered entries to all registered targets.
        /// </summary>
        void Flush();

        /// <summary>
        /// Sets the minimum allowed <see cref="LogLevel"/> for a given category.
        /// Entries below this level are dropped.
        /// </summary>
        /// <param name="category">The log category (or <c>null</c> for global default).</param>
        /// <param name="level">The minimum level to accept.</param>
        void SetLogLevel(string category, LogLevel level);

        /// <summary>
        /// Registers a new log target to receive log entries.
        /// </summary>
        /// <param name="target">The target implementation.</param>
        void RegisterTarget(ILogTarget target);

        /// <summary>
        /// Unregisters an existing log target.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns><c>true</c> if the target was previously registered; otherwise <c>false</c>.</returns>
        bool UnregisterTarget(ILogTarget target);

        /// <summary>
        /// Gets a snapshot of all currently registered log targets.
        /// </summary>
        IReadOnlyList<ILogTarget> GetRegisteredTargets();
    }
}