using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// Interface for log targets that define where log messages are written.
    /// Supports multiple output destinations including console, file, network, and custom targets.
    /// </summary>
    public interface ILogTarget : IDisposable
    {
        /// <summary>
        /// Gets the unique name of this log target.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// Messages below this level will be ignored.
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Gets or sets whether this target is enabled and should process log messages.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets whether this target is currently healthy and operational.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Gets the list of channels this target listens to.
        /// Empty list means it listens to all channels.
        /// </summary>
        IReadOnlyList<string> Channels { get; }

        /// <summary>
        /// Writes a log message to this target.
        /// </summary>
        /// <param name="logMessage">The log message to write</param>
        void Write(in LogMessage logMessage);

        /// <summary>
        /// Writes multiple log messages to this target in a batch operation.
        /// </summary>
        /// <param name="logMessages">The log messages to write</param>
        void WriteBatch(IReadOnlyList<LogMessage> logMessages);

        /// <summary>
        /// Determines whether this target should process the given log message.
        /// </summary>
        /// <param name="logMessage">The log message to evaluate</param>
        /// <returns>True if the message should be processed, false otherwise</returns>
        bool ShouldProcessMessage(in LogMessage logMessage);

        /// <summary>
        /// Flushes any buffered log messages to the target destination.
        /// </summary>
        void Flush();

        /// <summary>
        /// Performs a health check on this target.
        /// </summary>
        /// <returns>True if the target is healthy, false otherwise</returns>
        bool PerformHealthCheck();
    }
}