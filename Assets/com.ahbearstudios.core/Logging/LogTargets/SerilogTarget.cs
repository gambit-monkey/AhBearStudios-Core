using System;
using System.Collections.Generic;
using System.IO;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
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
    public class SerilogTarget : ILogTarget
    {
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly HashSet<Tagging.TagCategory> _tagFilters;
        private bool _isDisposed;
        private bool _isEnabled = true;
        
        /// <summary>
        /// Gets the name of this log target.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// Messages with lower severity will be ignored.
        /// </summary>
        public byte MinimumLevel 
        { 
            get => ConvertLevelSwitchToLogLevel(_levelSwitch.MinimumLevel);
            set => _levelSwitch.MinimumLevel = ConvertLogLevelToLogEventLevel(value);
        }
        
        /// <summary>
        /// Gets or sets whether this target is currently enabled.
        /// When disabled, no messages will be processed.
        /// </summary>
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }
        
        /// <summary>
        /// Creates a new SerilogTarget that logs to a file with default settings.
        /// </summary>
        /// <param name="name">The name of this target.</param>
        /// <param name="logFilePath">The path where log files will be created.</param>
        /// <param name="minimumLevel">The minimum level of messages to log.</param>
        public SerilogTarget(string name, string logFilePath, byte minimumLevel = LogLevel.Info)
        {
            Name = string.IsNullOrEmpty(name) ? "SerilogFile" : name;
            _tagFilters = new HashSet<Tagging.TagCategory>();
            _levelSwitch = new LoggingLevelSwitch(ConvertLogLevelToLogEventLevel(minimumLevel));
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Configure Serilog for file logging with rolling files
            _logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch)
                .Enrich.WithProperty("Application", "AhBearStudios")
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true,
                    buffered: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(2))
                .CreateLogger();
        }
        
        /// <summary>
        /// Creates a new SerilogTarget with a pre-configured logger.
        /// </summary>
        /// <param name="name">The name of this target.</param>
        /// <param name="logger">The pre-configured Serilog logger to use.</param>
        /// <param name="levelSwitch">The level switch to control logging levels.</param>
        public SerilogTarget(string name, ILogger logger, LoggingLevelSwitch levelSwitch)
        {
            Name = string.IsNullOrEmpty(name) ? "SerilogCustom" : name;
            _tagFilters = new HashSet<Tagging.TagCategory>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _levelSwitch = levelSwitch ?? new LoggingLevelSwitch(LogEventLevel.Information);
        }
        
        /// <summary>
        /// Creates a new SerilogTarget that logs to both file and console with JSON formatting.
        /// </summary>
        /// <param name="name">The name of this target.</param>
        /// <param name="logFilePath">The path where log files will be created.</param>
        /// <param name="minimumLevel">The minimum level of messages to log.</param>
        /// <returns>A new SerilogTarget configured for both file and console output with JSON formatting.</returns>
        public static SerilogTarget CreateFileAndConsoleTarget(string name, string logFilePath, byte minimumLevel = LogLevel.Info)
        {
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var levelSwitch = new LoggingLevelSwitch(ConvertLogLevelToLogEventLevel(minimumLevel));
            
            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.WithProperty("Application", "AhBearStudios")
                .WriteTo.File(
                    new JsonFormatter(), 
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true)
                .WriteTo.Console()
                .CreateLogger();
                
            return new SerilogTarget(name, logger, levelSwitch);
        }
        
        /// <summary>
        /// Creates a new SerilogTarget that logs to both file and console with JSON formatting.
        /// </summary>
        /// <param name="name">The name of this target.</param>
        /// <param name="minimumLevel">The minimum level of messages to log.</param>
        /// <returns>A new SerilogTarget configured for console output.</returns>
        public static SerilogTarget CreateConsoleTarget(string name, byte minimumLevel = LogLevel.Info)
        {
            var levelSwitch = new LoggingLevelSwitch(ConvertLogLevelToLogEventLevel(minimumLevel));
            
            var logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.WithProperty("Application", "AhBearStudios")
                .WriteTo.Console()
                .CreateLogger();
                
            return new SerilogTarget(name, logger, levelSwitch);
        }
        
        /// <summary>
        /// Creates a new SerilogTarget that logs to a file with optional JSON formatting.
        /// </summary>
        /// <param name="name">The name of this target.</param>
        /// <param name="logFilePath">The path where log files will be created.</param>
        /// <param name="minimumLevel">The minimum level of messages to log.</param>
        /// <param name="useJsonFormat">Whether to use JSON formatting for the log files.</param>
        /// <returns>A new SerilogTarget configured for file output.</returns>
        public static SerilogTarget CreateFileTarget(string name, string logFilePath, byte minimumLevel = LogLevel.Info,
            bool useJsonFormat = false)
        {
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var levelSwitch = new LoggingLevelSwitch(ConvertLogLevelToLogEventLevel(minimumLevel));

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.WithProperty("Application", "AhBearStudios");

            // Add file sink with or without JSON formatting
            if (useJsonFormat)
            {
                loggerConfig.WriteTo.File(
                    new JsonFormatter(),
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true);
            }
            else
            {
                loggerConfig.WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true);
            }

            var logger = loggerConfig.CreateLogger();

            return new SerilogTarget(name, logger, levelSwitch);
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
                    WriteToSerilog(entry);
                }
            }
        }

        /// <summary>
        /// Writes a structured log message to Serilog with property context.
        /// </summary>
        protected void WriteToSerilog(LogMessage message)
        {
            // Skip if below minimum level or message doesn't pass tag filter
            if (message.Level < MinimumLevel || !ShouldLog(message.Level, message.Tag))
                return;
    
            var level = ConvertLogLevel(message.Level);
            var messageTemplate = message.Message.ToString();

            try
            {
                // Start with a logger that has the tag context
                var contextLogger = _logger.ForContext("Tag", message.Tag.ToString())
                    .ForContext("Level", message.Level);
        
                // Add structured properties if available
                if (message.Properties.IsCreated)
                {
                    // Add each property individually to the logger context
                    foreach (var property in message.Properties)
                    {
                        contextLogger = contextLogger.ForContext(
                            property.Key.ToString(), 
                            property.Value.ToString());
                    }
                }
        
                // Write the log message with the fully enriched context
                contextLogger.Write(level, messageTemplate);
            }
            catch (Exception ex)
            {
                // Log error but continue - we don't want logging to cause application failures
                Console.WriteLine($"Error writing to Serilog: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts the internal byte log level to Serilog's LogEventLevel enum.
        /// </summary>
        /// <param name="messageLevel">The byte log level from the logging system.</param>
        /// <returns>The corresponding Serilog LogEventLevel.</returns>
        private LogEventLevel ConvertLogLevel(byte messageLevel)
        {
            // Map our byte-based log levels to Serilog's LogEventLevel
            if (messageLevel >= LogLevel.Critical)
                return LogEventLevel.Fatal;
            if (messageLevel >= LogLevel.Error)
                return LogEventLevel.Error;
            if (messageLevel >= LogLevel.Warning)
                return LogEventLevel.Warning;
            if (messageLevel >= LogLevel.Info)
                return LogEventLevel.Information;
            if (messageLevel >= LogLevel.Debug)
                return LogEventLevel.Debug;
    
            // Default to Verbose for any remaining lower levels
            return LogEventLevel.Verbose;
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
                WriteToSerilog(entry);
            }
        }
        
        /// <summary>
        /// Flushes any buffered log messages to ensure they are persisted.
        /// </summary>
        public void Flush()
        {
            // Serilog's File sink doesn't have an explicit flush method,
            // but we can use this opportunity to ensure any critical
            // logs get written out immediately by forcing a garbage collection.
            // This is only a best-effort attempt.
            Log.CloseAndFlush();
        }
        
        /// <summary>
        /// Checks if a message with the specified level would be logged by this target.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if messages with this level would be logged; otherwise, false.</returns>
        public bool IsLevelEnabled(byte level)
        {
            if (!_isEnabled)
                return false;
                
            return level >= MinimumLevel;
        }
        
        /// <summary>
        /// Adds a tag filter to this target. Only messages with matching tags will be processed.
        /// </summary>
        /// <param name="tagCategory">The tag category to include.</param>
        public void AddTagFilter(Tagging.TagCategory tagCategory)
        {
            if (tagCategory == Tagging.TagCategory.None)
                return;
                
            _tagFilters.Add(tagCategory);
        }
        
        /// <summary>
        /// Removes a tag filter from this target.
        /// </summary>
        /// <param name="tagCategory">The tag category to remove from filtering.</param>
        public void RemoveTagFilter(Tagging.TagCategory tagCategory)
        {
            _tagFilters.Remove(tagCategory);
        }
        
        /// <summary>
        /// Clears all tag filters from this target.
        /// </summary>
        public void ClearTagFilters()
        {
            _tagFilters.Clear();
        }
        
        /// <summary>
        /// Disposes the resources used by this target.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Flush any remaining logs
            try
            {
                Flush();
            }
            catch
            {
                // Silently ignore errors during disposal
            }
            
            // Log.CloseAndFlush() handles disposing of Serilog
            
            _isDisposed = true;
        }
        
        /// <summary>
        /// Checks if a message with the given level and tag should be logged.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="tag">The message tag.</param>
        /// <returns>True if the message should be logged; otherwise, false.</returns>
        private bool ShouldLog(byte level, Tagging.LogTag tag)
        {
            if (level < MinimumLevel)
                return false;
                
            // If no tag filters are set, log everything
            if (_tagFilters.Count == 0)
                return true;
                
            // Check if the tag matches any of our filters
            Tagging.TagCategory category = Tagging.GetTagCategory(tag);
            foreach (var filter in _tagFilters)
            {
                if ((category & filter) != 0)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Writes a log message to Serilog with the appropriate level and properties.
        /// </summary>
        /// <param name="entry">The log message to write.</param>
        private void WriteToSerilog(in LogMessage entry)
        {
            // Get the Serilog LogEventLevel equivalent to our log level
            LogEventLevel level = ConvertLogLevelToLogEventLevel(entry.Level);
    
            // Get tag string
            string tagString = entry.GetTagString().ToString();
    
            // Get timestamp
            DateTime timestamp = new DateTime(entry.TimestampTicks);
    
            // Create a logger with context properties
            var contextLogger = _logger
                .ForContext("Tag", tagString)
                .ForContext("Timestamp", timestamp);
    
            // Write to Serilog with the appropriate level
            string message = entry.Message.ToString();
    
            switch (level)
            {
                case LogEventLevel.Verbose:
                    contextLogger.Verbose("{Message}", message);
                    break;
            
                case LogEventLevel.Debug:
                    contextLogger.Debug("{Message}", message);
                    break;
            
                case LogEventLevel.Information:
                    contextLogger.Information("{Message}", message);
                    break;
            
                case LogEventLevel.Warning:
                    contextLogger.Warning("{Message}", message);
                    break;
            
                case LogEventLevel.Error:
                    contextLogger.Error("{Message}", message);
                    break;
            
                case LogEventLevel.Fatal:
                    contextLogger.Fatal("{Message}", message);
                    break;
            }
        }
        
        /// <summary>
        /// Converts from LogLevel to Serilog's LogEventLevel.
        /// </summary>
        /// <param name="level">The LogLevel value to convert.</param>
        /// <returns>The equivalent LogEventLevel.</returns>
        private static LogEventLevel ConvertLogLevelToLogEventLevel(byte level)
        {
            if (level == LogLevel.Debug)
                return LogEventLevel.Debug;
            else if (level == LogLevel.Info)
                return LogEventLevel.Information;
            else if (level == LogLevel.Warning)
                return LogEventLevel.Warning;
            else if (level == LogLevel.Error)
                return LogEventLevel.Error;
            else if (level == LogLevel.Critical)
                return LogEventLevel.Fatal;
            else
                return LogEventLevel.Information; // Default to Information
        }
        
        /// <summary>
        /// Converts from Serilog's LogEventLevel to LogLevel.
        /// </summary>
        /// <param name="level">The LogEventLevel value to convert.</param>
        /// <returns>The equivalent LogLevel.</returns>
        private static byte ConvertLevelSwitchToLogLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    return LogLevel.Debug;
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Information:
                    return LogLevel.Info;
                case LogEventLevel.Warning:
                    return LogLevel.Warning;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Fatal:
                    return LogLevel.Critical;
                default:
                    return LogLevel.Info;
            }
        }
    }
}