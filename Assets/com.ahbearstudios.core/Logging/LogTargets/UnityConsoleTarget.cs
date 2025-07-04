using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.MessageBus.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Logging.LogTargets
{
    /// <summary>
    /// An implementation of ILogTarget that outputs log messages to the Unity console.
    /// Supports colorized output, tag filtering, and Unity-specific formatting.
    /// </summary>
    public sealed class UnityConsoleTarget : ILogTarget
    {
        private readonly HashSet<Tagging.TagCategory> _includedTagFilters;
        private readonly HashSet<Tagging.TagCategory> _excludedTagFilters;
        private readonly HashSet<Tagging.LogTag> _includedLogTagFilters;
        private readonly HashSet<Tagging.LogTag> _excludedLogTagFilters;
        private readonly UnityConsoleTargetConfig _config;
        private readonly IMessageBusService _messageBusService;
        private readonly ILogFormatter _formatter;
        private bool _processUntaggedMessages = true;
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the name of this target.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets the minimum level this target will process.
        /// </summary>
        public LogLevel MinimumLevel { get; set; }
        
        /// <summary>
        /// Gets or sets whether this target is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// Creates a new UnityConsoleTarget from configuration.
        /// </summary>
        /// <param name="config">The Unity console configuration to use.</param>
        /// <param name="formatter">Optional custom formatter. If null, uses default formatter.</param>
        /// <param name="messageBusService">Optional message bus for publishing log events.</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public UnityConsoleTarget(UnityConsoleTargetConfig config, ILogFormatter formatter = null, IMessageBusService messageBusService = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _messageBusService = messageBusService;
            _formatter = formatter ?? new DefaultLogFormatter();
            
            Name = string.IsNullOrEmpty(config.TargetName) ? "UnityConsole" : config.TargetName;
            MinimumLevel = config.MinimumLevel;
            IsEnabled = config.Enabled;
            
            _includedTagFilters = new HashSet<Tagging.TagCategory>();
            _excludedTagFilters = new HashSet<Tagging.TagCategory>();
            _includedLogTagFilters = new HashSet<Tagging.LogTag>();
            _excludedLogTagFilters = new HashSet<Tagging.LogTag>();
            
            // Configure tag filters
            SetTagFilters(config.IncludedTags, config.ExcludedTags, config.ProcessUntaggedMessages);
            
            // Register Unity log handler if configured
            if (config.RegisterUnityLogHandler)
            {
                RegisterUnityLogHandler(config.DuplicateToOriginalHandler);
            }
        }
        
        /// <summary>
        /// Writes a batch of log messages to this target.
        /// </summary>
        /// <param name="entries">The list of log messages to write.</param>
        public void WriteBatch(NativeList<LogMessage> entries)
        {
            if (_isDisposed || !IsEnabled || entries.Length == 0)
                return;
                
            foreach (var entry in entries)
            {
                if (ShouldProcessMessage(entry))
                {
                    WriteToUnityConsole(entry);
                    PublishLogEntryMessage(entry);
                }
            }
        }
        
        /// <summary>
        /// Writes a single log message to this target.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        public void Write(in LogMessage entry)
        {
            if (_isDisposed || !IsEnabled)
                return;
                
            if (ShouldProcessMessage(entry))
            {
                WriteToUnityConsole(entry);
                PublishLogEntryMessage(entry);
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
            return IsEnabled && level >= MinimumLevel;
        }
        
        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            _includedTagFilters?.Clear();
            _excludedTagFilters?.Clear();
            _includedLogTagFilters?.Clear();
            _excludedLogTagFilters?.Clear();
            _isDisposed = true;
        }

        /// <summary>
        /// Determines if a message would be processed by this target based on all filters.
        /// </summary>
        /// <param name="logMessage">The log message to check.</param>
        /// <returns>True if the message would be processed; otherwise, false.</returns>
        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            if (_isDisposed || !IsEnabled)
                return false;
            
            // Check log level first (most basic filter)
            if (!IsLevelEnabled(logMessage.Level))
                return false;
            
            // Check tag-based filtering
            return IsLogTagEnabled(logMessage.Tag);
        }

        /// <summary>
        /// Sets comprehensive tag filters for this target.
        /// </summary>
        /// <param name="includedTags">Tags that should be included (null or empty means include all).</param>
        /// <param name="excludedTags">Tags that should be excluded.</param>
        /// <param name="processUntaggedMessages">Whether to process messages without tags.</param>
        public void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages)
        {
            _includedTagFilters.Clear();
            _excludedTagFilters.Clear();
            _includedLogTagFilters.Clear();
            _excludedLogTagFilters.Clear();
            _processUntaggedMessages = processUntaggedMessages;
            
            // Convert and add included tags
            if (includedTags != null && includedTags.Length > 0)
            {
                foreach (var tagString in includedTags)
                {
                    if (string.IsNullOrEmpty(tagString))
                        continue;
                    
                    // Try to parse as specific LogTag first
                    if (TryParseLogTag(tagString, out var logTag))
                    {
                        _includedLogTagFilters.Add(logTag);
                    }
                    // Fall back to TagCategory
                    else if (TryParseTagCategory(tagString, out var category))
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
                    if (string.IsNullOrEmpty(tagString))
                        continue;
                    
                    // Try to parse as specific LogTag first
                    if (TryParseLogTag(tagString, out var logTag))
                    {
                        _excludedLogTagFilters.Add(logTag);
                    }
                    // Fall back to TagCategory
                    else if (TryParseTagCategory(tagString, out var category))
                    {
                        _excludedTagFilters.Add(category);
                    }
                }
            }
        }
        
        /// <summary>
        /// Attempts to parse a string tag into a LogTag.
        /// </summary>
        /// <param name="tagString">The tag string to parse.</param>
        /// <param name="logTag">The parsed LogTag.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        private static bool TryParseLogTag(string tagString, out Tagging.LogTag logTag)
        {
            if (Enum.TryParse<Tagging.LogTag>(tagString, true, out logTag))
            {
                return true;
            }
            
            // Try common aliases using the Tagging class method
            logTag = Tagging.GetLogTag(tagString);
            return logTag != Tagging.LogTag.Default;
        }
        
        /// <summary>
        /// Attempts to parse a string tag into a TagCategory.
        /// </summary>
        /// <param name="tagString">The tag string to parse.</param>
        /// <param name="tagCategory">The parsed TagCategory.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        private static bool TryParseTagCategory(string tagString, out Tagging.TagCategory tagCategory)
        {
            if (Enum.TryParse<Tagging.TagCategory>(tagString, true, out tagCategory))
            {
                return true;
            }
            
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
                case "debug":
                case "debugging":
                    tagCategory = Tagging.TagCategory.Debug;
                    return true;
                case "error":
                case "errors":
                    tagCategory = Tagging.TagCategory.Error;
                    return true;
                case "development":
                case "dev":
                    tagCategory = Tagging.TagCategory.Development;
                    return true;
                case "features":
                case "feature":
                    tagCategory = Tagging.TagCategory.Features;
                    return true;
                case "custom":
                    tagCategory = Tagging.TagCategory.Custom;
                    return true;
                default:
                    tagCategory = Tagging.TagCategory.None;
                    return false;
            }
        }
        
        /// <summary>
        /// Determines if a message should be logged based on level and tag.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="tag">The message tag.</param>
        /// <returns>True if the message should be logged; otherwise, false.</returns>
        private bool ShouldLog(LogLevel level, Tagging.LogTag tag)
        {
            if (level < MinimumLevel)
                return false;
            
            return IsLogTagEnabled(tag);
        }
        
        
        /// <summary>
        /// Adds a tag filter to this target. Only messages with matching tag categories will be processed.
        /// </summary>
        /// <param name="tagCategory">The tag category to include in filtering.</param>
        public void AddTagFilter(Tagging.TagCategory tagCategory)
        {
            if (_isDisposed)
                return;
        
            if (tagCategory != Tagging.TagCategory.None)
            {
                _includedTagFilters.Add(tagCategory);
            }
        }

        /// <summary>
        /// Removes a tag filter from this target.
        /// </summary>
        /// <param name="tagCategory">The tag category to remove from filtering.</param>
        public void RemoveTagFilter(Tagging.TagCategory tagCategory)
        {
            if (_isDisposed)
                return;
        
            _includedTagFilters.Remove(tagCategory);
            _excludedTagFilters.Remove(tagCategory);
        }

        /// <summary>
        /// Clears all tag filters from this target.
        /// After clearing, all tag categories will be processed (subject to other filtering rules).
        /// </summary>
        public void ClearTagFilters()
        {
            if (_isDisposed)
                return;
        
            _includedTagFilters.Clear();
            _excludedTagFilters.Clear();
            _includedLogTagFilters.Clear();
            _excludedLogTagFilters.Clear();
            _processUntaggedMessages = true;
        }

        /// <summary>
        /// Adds a specific LogTag filter to this target.
        /// </summary>
        /// <param name="logTag">The specific log tag to include.</param>
        public void AddLogTagFilter(Tagging.LogTag logTag)
        {
            if (_isDisposed)
                return;
            
            if (logTag != Tagging.LogTag.None && logTag != Tagging.LogTag.Undefined)
            {
                _includedLogTagFilters.Add(logTag);
                // Remove from excluded if it was there
                _excludedLogTagFilters.Remove(logTag);
            }
        }

        /// <summary>
        /// Removes a specific LogTag filter from this target.
        /// </summary>
        /// <param name="logTag">The specific log tag to remove from filtering.</param>
        public void RemoveLogTagFilter(Tagging.LogTag logTag)
        {
            if (_isDisposed)
                return;
            
            _includedLogTagFilters.Remove(logTag);
            _excludedLogTagFilters.Remove(logTag);
        }

        /// <summary>
        /// Determines if a message with the specified tag would be processed by this target.
        /// </summary>
        /// <param name="logTag">The log tag to check.</param>
        /// <returns>True if messages with this tag would be processed; otherwise, false.</returns>
        public bool IsLogTagEnabled(Tagging.LogTag logTag)
        {
            if (_isDisposed || !IsEnabled)
                return false;
            
            // Check if tag is explicitly excluded
            if (_excludedLogTagFilters.Contains(logTag))
                return false;
            
            // Handle special cases
            if (logTag == Tagging.LogTag.None || logTag == Tagging.LogTag.Undefined)
                return _processUntaggedMessages;
            
            // If no specific log tag filters are set, fall back to category filtering
            if (_includedLogTagFilters.Count == 0)
            {
                var category = Tagging.GetTagCategory(logTag);
                
                // Check category exclusions
                if (_excludedTagFilters.Contains(category))
                    return false;
                
                // If no category filters are set either, allow all
                if (_includedTagFilters.Count == 0)
                    return true;
                
                // Check if category is included
                return _includedTagFilters.Contains(category);
            }
            
            // Check if this specific log tag is included
            return _includedLogTagFilters.Contains(logTag);
        }

        /// <summary>
        /// Writes a formatted message to the Unity console.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        private void WriteToUnityConsole(in LogMessage entry)
        {
            try
            {
                // Format the message using the configured formatter
                string messageText = FormatMessage(entry);
                
                // Apply length limit if configured
                if (_config.LimitMessageLength && messageText.Length > _config.MaxMessageLength)
                {
                    messageText = messageText.Substring(0, _config.MaxMessageLength - 3) + "...";
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
        
        /// <summary>
        /// Formats a log message according to the target configuration.
        /// </summary>
        /// <param name="entry">The log message to format.</param>
        /// <returns>The formatted message string.</returns>
        private string FormatMessage(in LogMessage entry)
        {
            if (_formatter != null)
            {
                var formatted = _formatter.Format(entry);
                return formatted.ToString();
            }
            
            // Fallback formatting if no formatter is available
            var message = entry.Message.ToString();
            var tagString = entry.GetTagString().ToString();
            
            if (_config.IncludeTimestamps)
            {
                var timestamp = new DateTime(entry.TimestampTicks);
                var timestampStr = timestamp.ToString(_config.TimestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff");
                return $"{timestampStr} [{tagString}] {message}";
            }
            
            return $"[{tagString}] {message}";
        }
        
        /// <summary>
        /// Registers this target as Unity's log handler if configured.
        /// </summary>
        /// <param name="duplicateToOriginal">Whether to also send logs to the original handler.</param>
        private void RegisterUnityLogHandler(bool duplicateToOriginal)
        {
            if (duplicateToOriginal)
            {
                var originalHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = new DuplicatingLogHandler(originalHandler, this);
            }
            else
            {
                Debug.unityLogger.logHandler = new UnityLogHandler(this);
            }
        }
        
        /// <summary>
        /// Publishes a log entry message to the message bus if available.
        /// </summary>
        /// <param name="entry">The log entry to publish.</param>
        private void PublishLogEntryMessage(in LogMessage entry)
        {
            if (_messageBusService == null)
                return;
                
            try
            {
                var logEntryMessage = new LogEntryMessage(entry);
                _messageBusService.PublishMessage(logEntryMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing log entry message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Custom Unity log handler that forwards to our target.
        /// </summary>
        private sealed class UnityLogHandler : ILogHandler
        {
            private readonly UnityConsoleTarget _target;
            
            public UnityLogHandler(UnityConsoleTarget target)
            {
                _target = target ?? throw new ArgumentNullException(nameof(target));
            }
            
            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                var message = string.Format(format, args);
                var level = ConvertUnityLogTypeToLogLevel(logType);
                var logMessage = new LogMessage(message, level, Tagging.LogTag.Unity, default);
                _target.Write(logMessage);
            }
            
            public void LogException(Exception exception, UnityEngine.Object context)
            {
                var message = $"Exception: {exception}";
                var logMessage = new LogMessage(message, LogLevel.Error, Tagging.LogTag.Unity, default);
                _target.Write(logMessage);
            }
            
            private static LogLevel ConvertUnityLogTypeToLogLevel(LogType logType)
            {
                return logType switch
                {
                    LogType.Error => LogLevel.Error,
                    LogType.Assert => LogLevel.Error,
                    LogType.Warning => LogLevel.Warning,
                    LogType.Log => LogLevel.Info,
                    LogType.Exception => LogLevel.Error,
                    _ => LogLevel.Info
                };
            }
        }
        
        /// <summary>
        /// Log handler that duplicates logs to both original handler and our target.
        /// </summary>
        private sealed class DuplicatingLogHandler : ILogHandler
        {
            private readonly ILogHandler _originalHandler;
            private readonly UnityConsoleTarget _target;
            
            public DuplicatingLogHandler(ILogHandler originalHandler, UnityConsoleTarget target)
            {
                _originalHandler = originalHandler ?? throw new ArgumentNullException(nameof(originalHandler));
                _target = target ?? throw new ArgumentNullException(nameof(target));
            }
            
            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                // Send to original handler first
                _originalHandler.LogFormat(logType, context, format, args);
                
                // Then send to our target
                var message = string.Format(format, args);
                var level = ConvertUnityLogTypeToLogLevel(logType);
                var logMessage = new LogMessage(message, level, Tagging.LogTag.Unity, default);
                _target.Write(logMessage);
            }
            
            public void LogException(Exception exception, UnityEngine.Object context)
            {
                // Send to original handler first
                _originalHandler.LogException(exception, context);
                
                // Then send to our target
                var message = $"Exception: {exception}";
                var logMessage = new LogMessage(message, LogLevel.Error, Tagging.LogTag.Unity, default);
                _target.Write(logMessage);
            }
            
            private static LogLevel ConvertUnityLogTypeToLogLevel(LogType logType)
            {
                return logType switch
                {
                    LogType.Error => LogLevel.Error,
                    LogType.Assert => LogLevel.Error,
                    LogType.Warning => LogLevel.Warning,
                    LogType.Log => LogLevel.Info,
                    LogType.Exception => LogLevel.Error,
                    _ => LogLevel.Info
                };
            }
        }
    }
}