using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject asset for logging system configuration.
    /// Provides Unity-serializable configuration that can be created and managed in the Unity Editor.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Logging/Logging Configuration", 
        fileName = "LoggingConfig", 
        order = 1)]
    public class LoggingConfigAsset : ScriptableObject
    {
        [Header("Global Settings")]
        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Info;
        [SerializeField] private bool _isLoggingEnabled = true;
        [SerializeField] private int _maxQueueSize = 1000;
        [SerializeField] private float _flushIntervalSeconds = 0.1f;

        [Header("Performance Settings")]
        [SerializeField] private bool _highPerformanceMode = true;
        [SerializeField] private bool _burstCompatibility = true;
        [SerializeField] private bool _structuredLogging = true;

        [Header("Batching Configuration")]
        [SerializeField] private bool _batchingEnabled = true;
        [SerializeField] private int _batchSize = 100;

        [Header("Correlation and Formatting")]
        [SerializeField] private bool _autoCorrelationId = true;
        [SerializeField] private string _correlationIdFormat = "{0:N}";
        [SerializeField] private string _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
        [SerializeField] private bool _includeTimestamps = true;
        [SerializeField] private string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

        [Header("Caching Settings")]
        [SerializeField] private bool _cachingEnabled = true;
        [SerializeField] private int _maxCacheSize = 1000;

        [Header("Target Configurations")]
        [SerializeField] private List<LogTargetConfiguration> _targetConfigurations = new List<LogTargetConfiguration>();

        [Header("Channel Configurations")]
        [SerializeField] private List<LogChannelConfiguration> _channelConfigurations = new List<LogChannelConfiguration>
        {
            new LogChannelConfiguration
            {
                Name = "Default",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true,
                Description = "Default logging channel"
            }
        };

        [Header("Editor Settings")]
        [SerializeField] private bool _enableEditorValidation = true;
        [SerializeField] private bool _showAdvancedOptions = false;

        /// <summary>
        /// Gets the LoggingConfig instance created from this ScriptableObject's settings.
        /// </summary>
        public LoggingConfig Config
        {
            get
            {
                var targetConfigs = new List<LogTargetConfig>();
                foreach (var targetConfig in _targetConfigurations)
                {
                    if (targetConfig.IsEnabled)
                    {
                        targetConfigs.Add(targetConfig.ToLogTargetConfig());
                    }
                }

                var channelConfigs = new List<LogChannelConfig>();
                foreach (var channelConfig in _channelConfigurations)
                {
                    if (channelConfig.IsEnabled)
                    {
                        channelConfigs.Add(channelConfig.ToLogChannelConfig());
                    }
                }

                return new LoggingConfig
                {
                    GlobalMinimumLevel = _globalMinimumLevel,
                    IsLoggingEnabled = _isLoggingEnabled,
                    MaxQueueSize = _maxQueueSize,
                    FlushInterval = TimeSpan.FromSeconds(_flushIntervalSeconds),
                    HighPerformanceMode = _highPerformanceMode,
                    BurstCompatibility = _burstCompatibility,
                    StructuredLogging = _structuredLogging,
                    BatchingEnabled = _batchingEnabled,
                    BatchSize = _batchSize,
                    CorrelationIdFormat = _correlationIdFormat,
                    AutoCorrelationId = _autoCorrelationId,
                    MessageFormat = _messageFormat,
                    IncludeTimestamps = _includeTimestamps,
                    TimestampFormat = _timestampFormat,
                    CachingEnabled = _cachingEnabled,
                    MaxCacheSize = _maxCacheSize,
                    TargetConfigs = targetConfigs.AsReadOnly(),
                    ChannelConfigs = channelConfigs.AsReadOnly()
                };
            }
        }

        /// <summary>
        /// Validates the configuration settings and clamps values to valid ranges.
        /// </summary>
        private void OnValidate()
        {
            if (!_enableEditorValidation) return;

            // Clamp numeric values to valid ranges
            _maxQueueSize = Mathf.Max(1, _maxQueueSize);
            _flushIntervalSeconds = Mathf.Max(0.001f, _flushIntervalSeconds);
            _batchSize = Mathf.Max(1, _batchSize);
            _maxCacheSize = Mathf.Max(1, _maxCacheSize);

            // Validate string formats
            if (string.IsNullOrWhiteSpace(_correlationIdFormat))
            {
                _correlationIdFormat = "{0:N}";
            }

            if (string.IsNullOrWhiteSpace(_messageFormat))
            {
                _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
            }

            if (string.IsNullOrWhiteSpace(_timestampFormat))
            {
                _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            }

            // Validate target configurations
            for (int i = 0; i < _targetConfigurations.Count; i++)
            {
                _targetConfigurations[i].Validate();
            }

            // Validate channel configurations
            for (int i = 0; i < _channelConfigurations.Count; i++)
            {
                _channelConfigurations[i].Validate();
            }

            // Ensure at least one channel exists
            if (_channelConfigurations.Count == 0)
            {
                _channelConfigurations.Add(new LogChannelConfiguration
                {
                    Name = "Default",
                    MinimumLevel = LogLevel.Debug,
                    IsEnabled = true,
                    Description = "Default logging channel"
                });
            }
        }

        /// <summary>
        /// Resets the configuration to default values.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            _globalMinimumLevel = LogLevel.Info;
            _isLoggingEnabled = true;
            _maxQueueSize = 1000;
            _flushIntervalSeconds = 0.1f;
            _highPerformanceMode = true;
            _burstCompatibility = true;
            _structuredLogging = true;
            _batchingEnabled = true;
            _batchSize = 100;
            _autoCorrelationId = true;
            _correlationIdFormat = "{0:N}";
            _messageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
            _includeTimestamps = true;
            _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            _cachingEnabled = true;
            _maxCacheSize = 1000;

            _targetConfigurations.Clear();
            _channelConfigurations.Clear();
            _channelConfigurations.Add(new LogChannelConfiguration
            {
                Name = "Default",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true,
                Description = "Default logging channel"
            });

            OnValidate();
        }

        /// <summary>
        /// Validates the current configuration and returns any errors.
        /// </summary>
        /// <returns>A list of validation errors</returns>
        [ContextMenu("Validate Configuration")]
        public List<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            try
            {
                var config = Config;
                var configErrors = config.Validate();
                errors.AddRange(configErrors);
            }
            catch (Exception ex)
            {
                errors.Add($"Configuration creation failed: {ex.Message}");
            }

            if (errors.Count == 0)
            {
                Debug.Log("LoggingConfigAsset: Configuration validation passed successfully.");
            }
            else
            {
                Debug.LogError($"LoggingConfigAsset: Configuration validation failed with {errors.Count} errors:\n{string.Join("\n", errors)}");
            }

            return errors;
        }

        /// <summary>
        /// Adds a default Unity Console target configuration.
        /// </summary>
        [ContextMenu("Add Unity Console Target")]
        public void AddUnityConsoleTarget()
        {
            var unityConsoleConfig = new LogTargetConfiguration
            {
                Name = "UnityConsole",
                TargetType = "UnityConsole",
                MinimumLevel = _globalMinimumLevel,
                IsEnabled = true,
                Description = "Unity Debug.Log console output",
                UseAsyncWrite = false,
                Properties = new List<LogTargetProperty>
                {
                    new LogTargetProperty { Key = "UseColors", Value = "true" },
                    new LogTargetProperty { Key = "ShowStackTraces", Value = "false" },
                    new LogTargetProperty { Key = "IncludeTimestamp", Value = "true" }
                }
            };

            _targetConfigurations.Add(unityConsoleConfig);
            OnValidate();
        }

        /// <summary>
        /// Adds a default file target configuration.
        /// </summary>
        [ContextMenu("Add File Target")]
        public void AddFileTarget()
        {
            var fileConfig = new LogTargetConfiguration
            {
                Name = "File",
                TargetType = "File",
                MinimumLevel = _globalMinimumLevel,
                IsEnabled = true,
                Description = "File-based logging output",
                UseAsyncWrite = true,
                Properties = new List<LogTargetProperty>
                {
                    new LogTargetProperty { Key = "FilePath", Value = "Logs/game.log" },
                    new LogTargetProperty { Key = "MaxFileSize", Value = "10485760" }, // 10MB
                    new LogTargetProperty { Key = "MaxFiles", Value = "5" }
                }
            };

            _targetConfigurations.Add(fileConfig);
            OnValidate();
        }
    }

    /// <summary>
    /// Serializable configuration for a log target.
    /// </summary>
    [Serializable]
    public class LogTargetConfiguration
    {
        [SerializeField] public string Name = string.Empty;
        [SerializeField] public string TargetType = string.Empty;
        [SerializeField] public LogLevel MinimumLevel = LogLevel.Debug;
        [SerializeField] public bool IsEnabled = true;
        [SerializeField] public string Description = string.Empty;
        [SerializeField] public bool UseAsyncWrite = true;
        [SerializeField] public int BufferSize = 100;
        [SerializeField] public float FlushIntervalSeconds = 0.1f;
        [SerializeField] public List<string> Channels = new List<string>();
        [SerializeField] public List<LogTargetProperty> Properties = new List<LogTargetProperty>();

        /// <summary>
        /// Converts this configuration to a LogTargetConfig.
        /// </summary>
        /// <returns>The LogTargetConfig instance</returns>
        public LogTargetConfig ToLogTargetConfig()
        {
            var properties = new Dictionary<string, object>();
            foreach (var prop in Properties)
            {
                properties[prop.Key] = prop.Value;
            }

            return new LogTargetConfig
            {
                Name = Name,
                TargetType = TargetType,
                MinimumLevel = MinimumLevel,
                IsEnabled = IsEnabled,
                UseAsyncWrite = UseAsyncWrite,
                BufferSize = BufferSize,
                FlushInterval = TimeSpan.FromSeconds(FlushIntervalSeconds),
                Channels = Channels,
                Properties = properties
            };
        }

        /// <summary>
        /// Validates this target configuration.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = "Target" + GetHashCode();
            }

            if (string.IsNullOrWhiteSpace(TargetType))
            {
                TargetType = "Unknown";
            }

            BufferSize = Mathf.Max(1, BufferSize);
            FlushIntervalSeconds = Mathf.Max(0.001f, FlushIntervalSeconds);
        }
    }

    /// <summary>
    /// Serializable configuration for a log channel.
    /// </summary>
    [Serializable]
    public class LogChannelConfiguration
    {
        [SerializeField] public string Name = string.Empty;
        [SerializeField] public string Description = string.Empty;
        [SerializeField] public LogLevel MinimumLevel = LogLevel.Debug;
        [SerializeField] public bool IsEnabled = true;
        [SerializeField] public string Color = "#FFFFFF";
        [SerializeField] public string Prefix = string.Empty;
        [SerializeField] public bool IncludeTimestamps = true;
        [SerializeField] public bool IncludeCorrelationId = true;
        [SerializeField] public List<string> TargetNames = new List<string>();
        [SerializeField] public int MaxMessagesPerSecond = 0;
        [SerializeField] public bool UseStructuredLogging = true;

        /// <summary>
        /// Converts this configuration to a LogChannelConfig.
        /// </summary>
        /// <returns>The LogChannelConfig instance</returns>
        public LogChannelConfig ToLogChannelConfig()
        {
            return new LogChannelConfig
            {
                Name = Name,
                Description = Description,
                MinimumLevel = MinimumLevel,
                IsEnabled = IsEnabled,
                Color = Color,
                Prefix = Prefix,
                IncludeTimestamps = IncludeTimestamps,
                IncludeCorrelationId = IncludeCorrelationId,
                TargetNames = TargetNames,
                MaxMessagesPerSecond = MaxMessagesPerSecond,
                UseStructuredLogging = UseStructuredLogging
            };
        }

        /// <summary>
        /// Validates this channel configuration.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = "Channel" + GetHashCode();
            }

            if (string.IsNullOrWhiteSpace(Color))
            {
                Color = "#FFFFFF";
            }

            MaxMessagesPerSecond = Mathf.Max(0, MaxMessagesPerSecond);
        }
    }

    /// <summary>
    /// Serializable key-value property for log target configuration.
    /// </summary>
    [Serializable]
    public class LogTargetProperty
    {
        [SerializeField] public string Key = string.Empty;
        [SerializeField] public string Value = string.Empty;
    }
}