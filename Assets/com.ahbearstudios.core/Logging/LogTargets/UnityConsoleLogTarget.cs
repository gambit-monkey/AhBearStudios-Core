using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.LogTargets
{
    /// <summary>
    /// An implementation of ILogTarget that outputs log messages to the Unity console.
    /// Supports colorized output through ILogFormatter implementations.
    /// </summary>
    public class UnityConsoleTarget : ILogTarget
    {
        private readonly HashSet<Tagging.TagCategory> _includedTagFilters;
        private readonly HashSet<Tagging.TagCategory> _excludedTagFilters;
        private bool _processUntaggedMessages = true;
        private bool _isDisposed;
        private bool _isEnabled = true;
        private LogLevel _minimumLevel = LogLevel.Debug;
        private ILogFormatter _formatter;
        
        /// <summary>
        /// Gets the name of this target.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets the minimum level this target will process.
        /// </summary>
        public LogLevel MinimumLevel 
        { 
            get => _minimumLevel;
            set => _minimumLevel = value;
        }
        
        /// <summary>
        /// Gets or sets whether this target is enabled.
        /// </summary>
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }
        
        /// <summary>
        /// Creates a new UnityConsoleTarget.
        /// </summary>
        /// <param name="name">The name of this target.</param>
        /// <param name="minimumLevel">The minimum level of messages to log.</param>
        public UnityConsoleTarget(string name = "UnityConsole", LogLevel minimumLevel = LogLevel.Debug)
        {
            Name = string.IsNullOrEmpty(name) ? "UnityConsole" : name;
            _includedTagFilters = new HashSet<Tagging.TagCategory>();
            _excludedTagFilters = new HashSet<Tagging.TagCategory>();
            _minimumLevel = minimumLevel;
            _processUntaggedMessages = true;
            // Use default formatter by default
            _formatter = new DefaultLogFormatter();
        }
        
        /// <summary>
        /// Writes a batch of log messages to this target.
        /// </summary>
        /// <param name="entries">The list of log messages to write.</param>
        public void WriteBatch(NativeList<LogMessage> entries)
        {
            if (_isDisposed || !_isEnabled || entries.Length == 0)
                return;
                
            foreach (var entry in entries)
            {
                if (ShouldLog(entry.Level, entry.Tag))
                {
                    WriteToUnityConsole(entry);
                }
            }
        }
        
        /// <summary>
        /// Writes a single log message to this target.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        public void Write(in LogMessage entry)
        {
            if (_isDisposed || !_isEnabled)
                return;
                
            if (ShouldLog(entry.Level, entry.Tag))
            {
                WriteToUnityConsole(entry);
            }
        }
        
        /// <summary>
        /// Flushes any buffered log messages.
        /// </summary>
        public void Flush()
        {
            // Unity console doesn't buffer, so nothing to do
        }
        
        /// <summary>
        /// Checks if a specified log level would be processed.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if the level is enabled; otherwise, false.</returns>
        public bool IsLevelEnabled(LogLevel level)
        {
            if (!_isEnabled)
                return false;
                
            return level >= MinimumLevel;
        }
        
        /// <summary>
        /// Sets the formatter to use for log messages.
        /// </summary>
        /// <param name="formatter">The formatter to use.</param>
        public void SetFormatter(ILogFormatter formatter)
        {
            _formatter = formatter ?? new DefaultLogFormatter();
        }
        
        /// <summary>
        /// Adds a tag filter to this target.
        /// </summary>
        /// <param name="tagCategory">The tag category to filter.</param>
        public void AddTagFilter(Tagging.TagCategory tagCategory)
        {
            if (tagCategory != Tagging.TagCategory.None)
            {
                _includedTagFilters.Add(tagCategory);
            }
        }
        
        /// <summary>
        /// Removes a tag filter from this target.
        /// </summary>
        /// <param name="tagCategory">The tag category to remove.</param>
        public void RemoveTagFilter(Tagging.TagCategory tagCategory)
        {
            _includedTagFilters.Remove(tagCategory);
        }
        
        /// <summary>
        /// Clears all tag filters.
        /// </summary>
        public void ClearTagFilters()
        {
            _includedTagFilters.Clear();
            _excludedTagFilters.Clear();
            _processUntaggedMessages = true;
        }
        
        /// <summary>
        /// Sets comprehensive tag filters for this target.
        /// This provides a configuration-friendly way to set up filtering.
        /// </summary>
        /// <param name="includedTags">Tags that should be included (null or empty means include all).</param>
        /// <param name="excludedTags">Tags that should be excluded.</param>
        /// <param name="processUntaggedMessages">Whether to process messages without tags.</param>
        public void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages)
        {
            // Clear existing filters
            _includedTagFilters.Clear();
            _excludedTagFilters.Clear();
            _processUntaggedMessages = processUntaggedMessages;
            
            // Convert and add included tags
            if (includedTags != null && includedTags.Length > 0)
            {
                foreach (var tagString in includedTags)
                {
                    if (!string.IsNullOrEmpty(tagString) && TryParseTagCategory(tagString, out var category))
                    {
                        _includedTagFilters.Add(category);
                    }
                }
            }
            
            // Convert and add excluded tags
            if (excludedTags != null && excludedTags.Length > 0)
            {
                foreach (var tagString in excludedTags)
                {
                    if (!string.IsNullOrEmpty(tagString) && TryParseTagCategory(tagString, out var category))
                    {
                        _excludedTagFilters.Add(category);
                    }
                }
            }
        }
        
        /// <summary>
        /// Attempts to parse a string tag into a TagCategory.
        /// </summary>
        /// <param name="tagString">The tag string to parse.</param>
        /// <param name="tagCategory">The parsed TagCategory.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        private bool TryParseTagCategory(string tagString, out Tagging.TagCategory tagCategory)
        {
            // Try direct enum parsing first
            if (Enum.TryParse<Tagging.TagCategory>(tagString, true, out tagCategory))
            {
                return true;
            }
            
            // You can add custom mappings here if needed
            // For example, mapping common string names to categories
            switch (tagString.ToLowerInvariant())
            {
                case "system":
                case "core":
                    tagCategory = Tagging.TagCategory.System;
                    return true;
                case "gameplay":
                case "game":
                    tagCategory = Tagging.TagCategory.Gameplay;
                    return true;
                case "ui":
                case "interface":
                    tagCategory = Tagging.TagCategory.UI;
                    return true;
                case "network":
                case "networking":
                    tagCategory = Tagging.TagCategory.Gameplay;
                    return true;
                case "audio":
                case "sound":
                    tagCategory = Tagging.TagCategory.Gameplay;
                    return true;
                case "graphics":
                case "rendering":
                    tagCategory = Tagging.TagCategory.Gameplay;
                    return true;
                default:
                    tagCategory = Tagging.TagCategory.None;
                    return false;
            }
        }
        
        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            _includedTagFilters.Clear();
            _excludedTagFilters.Clear();
        }
        
        /// <summary>
        /// Determines if a message should be logged based on level and tag.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="tag">The message tag.</param>
        /// <returns>True if the message should be logged; otherwise, false.</returns>
        private bool ShouldLog(LogLevel level, Tagging.LogTag tag)
        {
            // Check level first
            if (level < MinimumLevel)
                return false;
            
            // Get the tag category for filtering
            var category = Tagging.GetTagCategory(tag);
            
            // Check if tag is explicitly excluded
            if (_excludedTagFilters.Contains(category))
                return false;
            
            // Handle untagged messages
            if (category == Tagging.TagCategory.None)
                return _processUntaggedMessages;
            
            // If no include filters are set, allow all (except excluded)
            if (_includedTagFilters.Count == 0)
                return true;
            
            // Check if this tag's category is in our include filters
            return _includedTagFilters.Contains(category);
        }
        
        /// <summary>
        /// Writes a formatted message to the Unity console.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        private void WriteToUnityConsole(in LogMessage entry)
        {
            try
            {
                // Format the message using the formatter if available
                string messageText;
                if (_formatter != null)
                {
                    var formattedMessage = _formatter.Format(entry);
                    messageText = formattedMessage.ToString();
                }
                else
                {
                    // Fallback formatting if no formatter is available
                    messageText = $"[{entry.GetTagString()}] {entry.Message.ToString()}";
                }
                
                // Log to the appropriate Unity console method based on level
                switch (entry.Level)
                {
                    case LogLevel.Warning:
                        Debug.LogWarning(messageText);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        Debug.LogError(messageText);
                        break;
                    default:
                        Debug.Log(messageText);
                        break;
                }
            }
            catch (Exception ex)
            {
                // If formatting fails, log a raw message
                Debug.LogError($"Error writing to Unity Console: {ex.Message}. Original message: {entry.Message}");
            }
        }
    }
}