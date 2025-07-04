using System;
using System.Collections.Generic;
using System.IO;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.MessageBus.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.LogTargets
{
    /// <summary>
    /// An implementation of ILogTarget that forwards log messages to Serilog.
    /// Supports configuration options, filtering, and high-performance logging.
    /// </summary>
    public sealed class SerilogTarget : ILogTarget
    {
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly HashSet<Tagging.TagCategory> _includedTagFilters;
        private readonly HashSet<Tagging.TagCategory> _excludedTagFilters;
        private readonly HashSet<Tagging.LogTag> _includedLogTagFilters;
        private readonly HashSet<Tagging.LogTag> _excludedLogTagFilters;
        private readonly SerilogFileTargetConfig _targetConfig;
        private readonly IMessageBusService _messageBusService;
        private bool _processUntaggedMessages = true;
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the name of this log target.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// Messages with lower severity will be ignored.
        /// </summary>
        public LogLevel MinimumLevel 
        { 
            get => ConvertLevelSwitchToLogLevel(_levelSwitch.MinimumLevel);
            set => _levelSwitch.MinimumLevel = ConvertLogLevelToLogEventLevel(value);
        }
        
        /// <summary>
        /// Gets or sets whether this target is currently enabled.
        /// When disabled, no messages will be processed.
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// Creates a new SerilogTarget from configuration.
        /// </summary>
        /// <param name="targetConfig">The Serilog configuration to use.</param>
        /// <param name="messageBusService">Optional message bus for publishing log events.</param>
        /// <exception cref="ArgumentNullException">Thrown when targetConfig is null.</exception>
        public SerilogTarget(SerilogFileTargetConfig targetConfig, IMessageBusService messageBusService = null)
        {
            _targetConfig = targetConfig ?? throw new ArgumentNullException(nameof(targetConfig));
            _messageBusService = messageBusService;
            
            Name = string.IsNullOrEmpty(targetConfig.TargetName) ? "SerilogFile" : targetConfig.TargetName;
            IsEnabled = targetConfig.Enabled;
            
            _includedTagFilters = new HashSet<Tagging.TagCategory>();
            _excludedTagFilters = new HashSet<Tagging.TagCategory>();
            _includedLogTagFilters = new HashSet<Tagging.LogTag>();
            _excludedLogTagFilters = new HashSet<Tagging.LogTag>();
            _levelSwitch = new LoggingLevelSwitch(ConvertLogLevelToLogEventLevel(targetConfig.MinimumLevel));
            
            // Configure tag filters
            SetTagFilters(targetConfig.IncludedTags, targetConfig.ExcludedTags, targetConfig.ProcessUntaggedMessages);
            
            // Create the Serilog logger based on configuration
            _logger = CreateLoggerFromConfig(targetConfig);
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
                    WriteToSerilog(entry);
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
                WriteToSerilog(entry);
                PublishLogEntryMessage(entry);
            }
        }
        
        /// <summary>
        /// Flushes any buffered log messages to ensure they are persisted.
        /// </summary>
        public void Flush()
        {
            if (_isDisposed)
                return;
                
            try
            {
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                // Log error but continue - we don't want logging to cause application failures
                Console.WriteLine($"Error flushing Serilog: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a message with the specified level would be logged by this target.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if messages with this level would be logged; otherwise, false.</returns>
        public bool IsLevelEnabled(LogLevel level)
        {
            return IsEnabled && level >= MinimumLevel;
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

        void ILogTarget.SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages)
        {
            SetTagFilters(includedTags, excludedTags, processUntaggedMessages);
        }

        /// <summary>
        /// Disposes the resources used by this target.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            try
            {
                Flush();
            }
            catch
            {
                // Silently ignore errors during disposal
            }
            
            _includedTagFilters?.Clear();
            _excludedTagFilters?.Clear();
            _includedLogTagFilters?.Clear();
            _excludedLogTagFilters?.Clear();
            
            _isDisposed = true;
        }
        
        /// <summary>
        /// Creates a Serilog logger based on the provided configuration.
        /// </summary>
        /// <param name="targetConfig">The configuration to use.</param>
        /// <returns>A configured ILogger instance.</returns>
        private ILogger CreateLoggerFromConfig(SerilogFileTargetConfig targetConfig)
        {
            if (string.IsNullOrEmpty(targetConfig.LogFilePath))
                throw new ArgumentException("LogFilePath cannot be null or empty", nameof(targetConfig));
                
            // Ensure directory exists
            string directory = Path.GetDirectoryName(targetConfig.LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch)
                .Enrich.WithProperty("Application", "AhBearStudios")
                .Enrich.WithProperty("Target", Name);
            
            // Configure file output
            if (targetConfig.UseJsonFormat)
            {
                loggerConfig.WriteTo.File(
                    new JsonFormatter(),
                    targetConfig.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: targetConfig.RetainedDays > 0 ? targetConfig.RetainedDays : 7,
                    shared: true,
                    buffered: targetConfig.AutoFlush,
                    flushToDiskInterval: targetConfig.AutoFlush ? TimeSpan.FromSeconds(targetConfig.FlushIntervalSeconds) : null);
            }
            else
            {
                loggerConfig.WriteTo.File(
                    targetConfig.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: targetConfig.RetainedDays > 0 ? targetConfig.RetainedDays : 7,
                    shared: true,
                    buffered: targetConfig.AutoFlush,
                    flushToDiskInterval: targetConfig.AutoFlush ? TimeSpan.FromSeconds(targetConfig.FlushIntervalSeconds) : null);
            }
            
            // Add console output if configured
            if (targetConfig.LogToConsole)
            {
                loggerConfig.WriteTo.Console();
            }
            
            return loggerConfig.CreateLogger();
        }
        
        /// <summary>
        /// Sets comprehensive tag filters for this target.
        /// </summary>
        /// <param name="includedTags">Tags that should be included (null or empty means include all).</param>
        /// <param name="excludedTags">Tags that should be excluded.</param>
        /// <param name="processUntaggedMessages">Whether to process messages without tags.</param>
        private void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages)
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
        /// Checks if a message with the given level and tag should be logged.
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
        /// Writes a log message to Serilog with structured properties.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        private void WriteToSerilog(in LogMessage entry)
        {
            try
            {
                var level = ConvertLogLevelToLogEventLevel(entry.Level);
                var tagString = entry.GetTagString().ToString();
                var message = entry.Message.ToString();
                var timestamp = new DateTime(entry.TimestampTicks);
                
                // Create enriched logger context
                var contextLogger = _logger
                    .ForContext("Tag", tagString)
                    .ForContext("Timestamp", timestamp)
                    .ForContext("Level", entry.Level.ToString());
                
                // Add properties if available
                if (entry.Properties.IsCreated)
                {
                    foreach (var property in entry.Properties)
                    {
                        contextLogger = contextLogger.ForContext(
                            property.Key.ToString(), 
                            property.Value.ToString());
                    }
                }
                
                // Write with appropriate level
                contextLogger.Write(level, "{Message}", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to Serilog: {ex.Message}");
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
        /// Converts from LogLevel to Serilog's LogEventLevel.
        /// </summary>
        /// <param name="level">The LogLevel value to convert.</param>
        /// <returns>The equivalent LogEventLevel.</returns>
        private static LogEventLevel ConvertLogLevelToLogEventLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Info => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }
        
        /// <summary>
        /// Converts from Serilog's LogEventLevel to LogLevel.
        /// </summary>
        /// <param name="level">The LogEventLevel value to convert.</param>
        /// <returns>The equivalent LogLevel.</returns>
        private static LogLevel ConvertLevelSwitchToLogLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => LogLevel.Debug,
                LogEventLevel.Debug => LogLevel.Debug,
                LogEventLevel.Information => LogLevel.Info,
                LogEventLevel.Warning => LogLevel.Warning,
                LogEventLevel.Error => LogLevel.Error,
                LogEventLevel.Fatal => LogLevel.Critical,
                _ => LogLevel.Info
            };
        }
    }
}