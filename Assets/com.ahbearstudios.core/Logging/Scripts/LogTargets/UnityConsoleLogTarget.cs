using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Formatters;

namespace AhBearStudios.Core.Logging.LogTargets
{
    /// <summary>
    /// An implementation of ILogTarget that outputs log messages to the Unity console.
    /// Supports colorized output through ILogFormatter implementations.
    /// </summary>
    public class UnityConsoleTarget : ILogTarget
    {
        private readonly HashSet<Tagging.TagCategory> _tagFilters;
        private bool _isDisposed;
        private bool _isEnabled = true;
        private byte _minimumLevel = LogLevel.Debug;
        private ILogFormatter _formatter;
        
        /// <summary>
        /// Gets the name of this target.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets the minimum level this target will process.
        /// </summary>
        public byte MinimumLevel 
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
        public UnityConsoleTarget(string name = "UnityConsole", byte minimumLevel = LogLevel.Debug)
        {
            Name = string.IsNullOrEmpty(name) ? "UnityConsole" : name;
            _tagFilters = new HashSet<Tagging.TagCategory>();
            _minimumLevel = minimumLevel;
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
        public bool IsLevelEnabled(byte level)
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
                _tagFilters.Add(tagCategory);
            }
        }
        
        /// <summary>
        /// Removes a tag filter from this target.
        /// </summary>
        /// <param name="tagCategory">The tag category to remove.</param>
        public void RemoveTagFilter(Tagging.TagCategory tagCategory)
        {
            _tagFilters.Remove(tagCategory);
        }
        
        /// <summary>
        /// Clears all tag filters.
        /// </summary>
        public void ClearTagFilters()
        {
            _tagFilters.Clear();
        }
        
        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _isDisposed = true;
            _tagFilters.Clear();
        }
        
        /// <summary>
        /// Determines if a message should be logged based on level and tag.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="tag">The message tag.</param>
        /// <returns>True if the message should be logged; otherwise, false.</returns>
        private bool ShouldLog(byte level, Tagging.LogTag tag)
        {
            // Check level first
            if (level < MinimumLevel)
                return false;
                
            // If no filters, log everything
            if (_tagFilters.Count == 0)
                return true;
                
            // Check if this tag's category is in our filters
            var category = Tagging.GetTagCategory(tag);
            return _tagFilters.Contains(category);
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