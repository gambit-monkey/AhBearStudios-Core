using System.Collections.Generic;
using System.Text;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;

namespace AhBearStudios.Unity.Logging.Targets
{
    /// <summary>
    /// Unity Console log target that integrates with Unity's Debug.Log system.
    /// Provides color-coded output, stack trace support, and Unity Editor integration.
    /// </summary>
    public sealed class UnityConsoleLogTarget : ILogTarget
    {
        private readonly ILogTargetConfig _config;
        private readonly StringBuilder _stringBuilder;
        private readonly object _lock = new object();
        private readonly Dictionary<LogLevel, Color> _colorMapping;
        private readonly bool _useColors;
        private readonly bool _showStackTraces;
        private readonly bool _includeTimestamp;
        private readonly bool _includeThreadId;
        private readonly string _messageFormat;
        
        private volatile bool _disposed = false;
        private long _messagesProcessed = 0;
        private long _errorsEncountered = 0;
        private DateTime _lastWriteTime = DateTime.MinValue;

        /// <summary>
        /// Gets the unique name of this log target.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the minimum log level that this target will process.
        /// </summary>
        public LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Gets or sets whether this target is enabled and should process log messages.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets whether this target is currently healthy and operational.
        /// </summary>
        public bool IsHealthy => !_disposed && Application.isPlaying;

        /// <summary>
        /// Gets the list of channels this target listens to.
        /// </summary>
        public IReadOnlyList<string> Channels { get; }

        /// <summary>
        /// Gets the number of messages processed by this target.
        /// </summary>
        public long MessagesProcessed => _messagesProcessed;

        /// <summary>
        /// Gets the number of errors encountered during processing.
        /// </summary>
        public long ErrorsEncountered => _errorsEncountered;

        /// <summary>
        /// Gets the time when the last message was written.
        /// </summary>
        public DateTime LastWriteTime => _lastWriteTime;

        /// <summary>
        /// Initializes a new instance of the UnityConsoleLogTarget.
        /// </summary>
        /// <param name="config">The target configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when configSo is null</exception>
        public UnityConsoleLogTarget(ILogTargetConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            Name = _config.Name;
            MinimumLevel = _config.MinimumLevel;
            IsEnabled = _config.IsEnabled;
            Channels = _config.Channels;

            _stringBuilder = new StringBuilder(1024);
            
            // Configure from target properties
            _useColors = GetConfigProperty("UseColors", true);
            _showStackTraces = GetConfigProperty("ShowStackTraces", false);
            _includeTimestamp = GetConfigProperty("IncludeTimestamp", true);
            _includeThreadId = GetConfigProperty("IncludeThreadId", false);
            _messageFormat = GetConfigProperty("MessageFormat", "[{Level}] {Message}");

            // Initialize color mapping
            _colorMapping = InitializeColorMapping();
        }

        /// <summary>
        /// Writes a log message to the Unity Console.
        /// </summary>
        /// <param name="logMessage">The log message to write</param>
        public void Write(in LogMessage logMessage)
        {
            if (!ShouldProcessMessage(logMessage)) return;

            try
            {
                lock (_lock)
                {
                    if (_disposed) return;

                    var formattedMessage = FormatMessage(logMessage);
                    WriteToUnityConsole(logMessage.Level, formattedMessage, logMessage.Exception);
                    
                    _messagesProcessed++;
                    _lastWriteTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _errorsEncountered++;
                // Fallback to basic Unity logging to avoid infinite recursion
                Debug.LogError($"UnityConsoleLogTarget error: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes multiple log messages to the Unity Console in a batch operation.
        /// </summary>
        /// <param name="logMessages">The log messages to write</param>
        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            if (logMessages == null || logMessages.Count == 0) return;

            foreach (var message in logMessages)
            {
                Write(message);
            }
        }

        /// <summary>
        /// Determines whether this target should process the given log message.
        /// </summary>
        /// <param name="logMessage">The log message to evaluate</param>
        /// <returns>True if the message should be processed, false otherwise</returns>
        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            if (!IsEnabled || _disposed) return false;
            
            // Check minimum level
            if (logMessage.Level < MinimumLevel) return false;
            
            // Check channel filtering
            if (Channels.Count > 0)
            {
                var messageChannel = logMessage.Channel.ToString();
                var channelFound = false;
                
                foreach (var channel in Channels)
                {
                    if (string.Equals(channel, messageChannel, StringComparison.OrdinalIgnoreCase))
                    {
                        channelFound = true;
                        break;
                    }
                }
                
                if (!channelFound) return false;
            }
            
            return true;
        }

        /// <summary>
        /// Flushes any buffered log messages to the Unity Console.
        /// Note: Unity Console doesn't require explicit flushing.
        /// </summary>
        public void Flush()
        {
            // Unity Debug.Log automatically flushes, so no action needed
            // This method is here for interface compliance
        }

        /// <summary>
        /// Performs a health check on this target.
        /// </summary>
        /// <returns>True if the target is healthy, false otherwise</returns>
        public bool PerformHealthCheck()
        {
            try
            {
                // Check if Unity is available and running
                if (!Application.isPlaying)
                {
                    return false;
                }

                // Test a simple log operation
                var testMessage = $"Health check test - {DateTime.UtcNow:HH:mm:ss.fff}";
                Debug.Log($"[HEALTH_CHECK] {testMessage}");
                
                return !_disposed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets diagnostic information about this target.
        /// </summary>
        /// <returns>A dictionary containing diagnostic data</returns>
        public Dictionary<string, object> GetDiagnostics()
        {
            return new Dictionary<string, object>
            {
                ["Name"] = Name,
                ["IsEnabled"] = IsEnabled,
                ["IsHealthy"] = IsHealthy,
                ["MinimumLevel"] = MinimumLevel.ToString(),
                ["MessagesProcessed"] = _messagesProcessed,
                ["ErrorsEncountered"] = _errorsEncountered,
                ["LastWriteTime"] = _lastWriteTime,
                ["UseColors"] = _useColors,
                ["ShowStackTraces"] = _showStackTraces,
                ["ChannelCount"] = Channels.Count,
                ["UnityVersion"] = Application.unityVersion,
                ["Platform"] = Application.platform.ToString(),
                ["IsEditor"] = Application.isEditor
            };
        }

        /// <summary>
        /// Formats the log message according to the target's configuration.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <returns>The formatted message string</returns>
        private string FormatMessage(in LogMessage logMessage)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();

                var format = _messageFormat;
                
                // Replace common placeholders
                format = format.Replace("{Level}", logMessage.Level.ToString());
                format = format.Replace("{Message}", logMessage.Message.ToString());
                format = format.Replace("{Channel}", logMessage.Channel.ToString());
                
                if (_includeTimestamp)
                {
                    format = format.Replace("{Timestamp}", logMessage.Timestamp.ToString("HH:mm:ss.fff"));
                }
                
                if (_includeThreadId)
                {
                    format = format.Replace("{ThreadId}", logMessage.ThreadId.ToString());
                }
                
                if (!logMessage.CorrelationId.IsEmpty)
                {
                    format = format.Replace("{CorrelationId}", logMessage.CorrelationId.ToString());
                }
                
                if (!logMessage.SourceContext.IsEmpty)
                {
                    format = format.Replace("{SourceContext}", logMessage.SourceContext.ToString());
                }

                // Apply color formatting if enabled
                if (_useColors && _colorMapping.TryGetValue(logMessage.Level, out var color))
                {
                    var colorHex = ColorUtility.ToHtmlStringRGB(color);
                    format = $"<color=#{colorHex}>{format}</color>";
                }

                _stringBuilder.Append(format);
                
                // Add exception details if present
                if (logMessage.HasException && logMessage.Exception != null)
                {
                    _stringBuilder.AppendLine();
                    _stringBuilder.Append("Exception: ");
                    _stringBuilder.Append(logMessage.Exception.GetType().Name);
                    _stringBuilder.Append(": ");
                    _stringBuilder.Append(logMessage.Exception.Message);
                    
                    if (_showStackTraces && !string.IsNullOrEmpty(logMessage.Exception.StackTrace))
                    {
                        _stringBuilder.AppendLine();
                        _stringBuilder.Append("Stack Trace:");
                        _stringBuilder.AppendLine();
                        _stringBuilder.Append(logMessage.Exception.StackTrace);
                    }
                }

                return _stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Writes the formatted message to the appropriate Unity Console method.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The formatted message</param>
        /// <param name="exception">The associated exception, if any</param>
        private void WriteToUnityConsole(LogLevel level, string message, Exception exception)
        {
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    if (exception != null)
                    {
                        Debug.LogException(exception);
                        Debug.Log(message);
                    }
                    else
                    {
                        Debug.Log(message);
                    }
                    break;
                    
                case LogLevel.Warning:
                    if (exception != null)
                    {
                        Debug.LogException(exception);
                        Debug.LogWarning(message);
                    }
                    else
                    {
                        Debug.LogWarning(message);
                    }
                    break;
                    
                case LogLevel.Error:
                case LogLevel.Critical:
                    if (exception != null)
                    {
                        Debug.LogException(exception);
                        Debug.LogError(message);
                    }
                    else
                    {
                        Debug.LogError(message);
                    }
                    break;
                    
                default:
                    Debug.Log(message);
                    break;
            }
        }

        /// <summary>
        /// Initializes the color mapping for different log levels.
        /// </summary>
        /// <returns>A dictionary mapping log levels to colors</returns>
        private Dictionary<LogLevel, Color> InitializeColorMapping()
        {
            return new Dictionary<LogLevel, Color>
            {
                [LogLevel.Debug] = new Color(0.7f, 0.7f, 0.7f, 1f),    // Light gray
                [LogLevel.Info] = Color.white,                          // White
                [LogLevel.Warning] = Color.yellow,                      // Yellow
                [LogLevel.Error] = new Color(1f, 0.5f, 0.5f, 1f),     // Light red
                [LogLevel.Critical] = Color.red                         // Red
            };
        }

        /// <summary>
        /// Gets a configuration property with a default value.
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">The default value</param>
        /// <returns>The property value or default if not found</returns>
        private T GetConfigProperty<T>(string key, T defaultValue)
        {
            if (_config.Properties != null && _config.Properties.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }

        /// <summary>
        /// Disposes the Unity Console log target.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        
                        // Log final statistics
                        if (Application.isPlaying)
                        {
                            Debug.Log($"[UnityConsoleLogTarget] Disposed. " +
                                     $"Messages processed: {_messagesProcessed}, " +
                                     $"Errors: {_errorsEncountered}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a string representation of this log target.
        /// </summary>
        /// <returns>A descriptive string</returns>
        public override string ToString()
        {
            return $"UnityConsoleLogTarget(Name={Name}, Enabled={IsEnabled}, Level={MinimumLevel})";
        }
    }
}