using System;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Defines a target that can receive and process log messages.
    /// Implementations may include console output, file output, memory buffer, etc.
    /// </summary>
    public interface ILogTarget : IDisposable
    {
        /// <summary>
        /// Gets the name of this log target.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// Messages with lower severity will be ignored.
        /// </summary>
        LogLevel MinimumLevel { get; set; }
        
        /// <summary>
        /// Gets or sets whether this target is currently enabled.
        /// When disabled, no messages will be processed.
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Writes a batch of log messages to this target.
        /// </summary>
        /// <param name="entries">The list of log messages to write.</param>
        void WriteBatch(NativeList<LogMessage> entries);
        
        /// <summary>
        /// Writes a single log message to this target.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        void Write(in LogMessage entry);
        
        /// <summary>
        /// Flushes any buffered log messages to ensure they are persisted.
        /// </summary>
        void Flush();
        
        /// <summary>
        /// Determines if a message with the specified level meets the filtering criteria and would be logged.
        /// Takes into account the current enabled state and minimum level settings.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if messages with this level would be logged; otherwise, false.</returns>
        bool IsLevelEnabled(LogLevel level);
        
        /// <summary>
        /// Adds a tag filter to this target. Only messages with matching tag will be processed.
        /// </summary>
        /// <param name="tagCategory">The tag category to include.</param>
        void AddTagFilter(Tagging.TagCategory tagCategory);
        
        /// <summary>
        /// Removes a tag filter from this target.
        /// </summary>
        /// <param name="tagCategory">The tag category to remove from filtering.</param>
        void RemoveTagFilter(Tagging.TagCategory tagCategory);
        
        /// <summary>
        /// Clears all tag filters from this target.
        /// </summary>
        void ClearTagFilters();
        
        /// <summary>
        /// Adds a specific LogTag filter to this target.
        /// </summary>
        /// <param name="logTag">The specific log tag to include.</param>
        void AddLogTagFilter(Tagging.LogTag logTag);
        
        /// <summary>
        /// Removes a specific LogTag filter from this target.
        /// </summary>
        /// <param name="logTag">The specific log tag to remove from filtering.</param>
        void RemoveLogTagFilter(Tagging.LogTag logTag);
        
        /// <summary>
        /// Determines if a message with the specified tag would be processed by this target.
        /// </summary>
        /// <param name="logTag">The log tag to check.</param>
        /// <returns>True if messages with this tag would be processed; otherwise, false.</returns>
        bool IsLogTagEnabled(Tagging.LogTag logTag);
        
        /// <summary>
        /// Determines if a message would be processed by this target based on all filters.
        /// </summary>
        /// <param name="logMessage">The log message to check.</param>
        /// <returns>True if the message would be processed; otherwise, false.</returns>
        bool ShouldProcessMessage(in LogMessage logMessage);
        
        /// <summary>
        /// Sets comprehensive tag filters for this target.
        /// This provides a configuration-friendly way to set up filtering.
        /// </summary>
        /// <param name="includedTags">Tags that should be included (null or empty means include all).</param>
        /// <param name="excludedTags">Tags that should be excluded.</param>
        /// <param name="processUntaggedMessages">Whether to process messages without tags.</param>
        void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages);
    }
}