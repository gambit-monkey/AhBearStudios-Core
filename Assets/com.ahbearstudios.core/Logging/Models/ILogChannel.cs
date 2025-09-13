using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Interface for log channels that provide domain-specific logging categorization.
    /// Channels allow for fine-grained control over logging behavior by domain or feature area.
    /// </summary>
    public interface ILogChannel
    {
        /// <summary>
        /// Gets the unique name of this log channel.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the minimum log level that this channel will process.
        /// Messages below this level will be filtered out.
        /// </summary>
        LogLevel MinimumLevel { get; }

        /// <summary>
        /// Gets or sets whether this channel is enabled and should process log messages.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets whether this channel is currently healthy and operational.
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Gets the list of tags associated with this channel for categorization.
        /// </summary>
        IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// Gets the list of target names that this channel should write to.
        /// Empty list means it writes to all targets.
        /// </summary>
        IReadOnlyList<string> TargetNames { get; }

        /// <summary>
        /// Determines whether this channel should process the given log message.
        /// </summary>
        /// <param name="logMessage">The log message to evaluate</param>
        /// <returns>True if the channel should process the message, false otherwise</returns>
        bool ShouldProcessMessage(in LogMessage logMessage);

        /// <summary>
        /// Gets additional properties associated with this channel.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }
    }
}