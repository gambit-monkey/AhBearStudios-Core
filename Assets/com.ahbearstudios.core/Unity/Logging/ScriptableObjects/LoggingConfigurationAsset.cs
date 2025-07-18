using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Configs;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// Master ScriptableObject configuration for the logging system.
    /// Provides Unity-serializable configuration that can be created and managed in the Unity Editor.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Logging Configuration", 
        fileName = "LoggingConfiguration", 
        order = 0)]
    public class LoggingConfigurationAsset : LoggingScriptableObjectBase
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
        [SerializeField] private List<LogTargetScriptableObject> _targetConfigurations = new List<LogTargetScriptableObject>();

        [Header("Formatter Configurations")]
        [SerializeField] private List<LogFormatterScriptableObject> _formatterConfigurations = new List<LogFormatterScriptableObject>();

        [Header("Filter Configurations")]
        [SerializeField] private List<LogFilterScriptableObject> _filterConfigurations = new List<LogFilterScriptableObject>();

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

        [Header("Scenario Configurations")]
        [SerializeField] private LoggingScenario _currentScenario = LoggingScenario.Development;
        [SerializeField] private bool _useScenarioOverrides = true;

        [Header("Platform-Specific Settings")]
        [SerializeField] private bool _enablePlatformOptimizations = true;
        [SerializeField] private List<PlatformOverride> _platformOverrides = new List<PlatformOverride>();

        [Header("Editor Settings")]
        [SerializeField] private bool _enableEditorValidation = true;
        [SerializeField] private bool _showAdvancedOptions = false;
        [SerializeField] private bool _enablePreviewMode = false;

        /// <summary>
        /// Gets the global minimum log level.
        /// </summary>
        public LogLevel GlobalMinimumLevel => _globalMinimumLevel;

        /// <summary>
        /// Gets whether logging is enabled.
        /// </summary>
        public bool IsLoggingEnabled => _isLoggingEnabled;

        /// <summary>
        /// Gets the maximum queue size.
        /// </summary>
        public int MaxQueueSize => _maxQueueSize;

        /// <summary>
        /// Gets the flush interval as a TimeSpan.
        /// </summary>
        public TimeSpan FlushInterval => TimeSpan.FromSeconds(_flushIntervalSeconds);

        /// <summary>
        /// Gets whether high performance mode is enabled.
        /// </summary>
        public bool HighPerformanceMode => _highPerformanceMode;

        /// <summary>
        /// Gets whether burst compatibility is enabled.
        /// </summary>
        public bool BurstCompatibility => _burstCompatibility;

        /// <summary>
        /// Gets whether structured logging is enabled.
        /// </summary>
        public bool StructuredLogging => _structuredLogging;

        /// <summary>
        /// Gets whether batching is enabled.
        /// </summary>
        public bool BatchingEnabled => _batchingEnabled;

        /// <summary>
        /// Gets the batch size.
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// Gets whether auto correlation ID is enabled.
        /// </summary>
        public bool AutoCorrelationId => _autoCorrelationId;

        /// <summary>
        /// Gets the correlation ID format.
        /// </summary>
        public string CorrelationIdFormat => _correlationIdFormat;

        /// <summary>
        /// Gets the message format.
        /// </summary>
        public string MessageFormat => _messageFormat;

        /// <summary>
        /// Gets whether timestamps are included.
        /// </summary>
        public bool IncludeTimestamps => _includeTimestamps;

        /// <summary>
        /// Gets the timestamp format.
        /// </summary>
        public string TimestampFormat => _timestampFormat;

        /// <summary>
        /// Gets whether caching is enabled.
        /// </summary>
        public bool CachingEnabled => _cachingEnabled;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Gets the target configurations.
        /// </summary>
        public IReadOnlyList<LogTargetScriptableObject> TargetConfigurations => _targetConfigurations.AsReadOnly();

        /// <summary>
        /// Gets the formatter configurations.
        /// </summary>
        public IReadOnlyList<LogFormatterScriptableObject> FormatterConfigurations => _formatterConfigurations.AsReadOnly();

        /// <summary>
        /// Gets the filter configurations.
        /// </summary>
        public IReadOnlyList<LogFilterScriptableObject> FilterConfigurations => _filterConfigurations.AsReadOnly();

        /// <summary>
        /// Gets the channel configurations.
        /// </summary>
        public IReadOnlyList<LogChannelConfiguration> ChannelConfigurations => _channelConfigurations.AsReadOnly();

        /// <summary>
        /// Gets the current scenario.
        /// </summary>
        public LoggingScenario CurrentScenario => _currentScenario;

        /// <summary>
        /// Gets whether scenario overrides are used.
        /// </summary>
        public bool UseScenarioOverrides => _useScenarioOverrides;

        /// <summary>
        /// Gets whether platform optimizations are enabled.
        /// </summary>
        public bool EnablePlatformOptimizations => _enablePlatformOptimizations;

        /// <summary>
        /// Gets the platform overrides.
        /// </summary>
        public IReadOnlyList<PlatformOverride> PlatformOverrides => _platformOverrides.AsReadOnly();

        /// <summary>
        /// Gets whether preview mode is enabled.
        /// </summary>
        public bool EnablePreviewMode => _enablePreviewMode;

        /// <summary>
        /// Gets the LoggingConfigSO instance created from this ScriptableObject's settings.
        /// </summary>
        public LoggingConfigSO ConfigSo
        {
            get
            {
                var config = CreateBaseConfig();
                
                // Apply scenario overrides
                if (_useScenarioOverrides)
                {
                    ApplyScenarioOverrides(config);
                }
                
                // Apply platform overrides
                if (_enablePlatformOptimizations)
                {
                    ApplyPlatformOverrides(config);
                }
                
                return config;
            }
        }

        /// <summary>
        /// Creates the base configuration without overrides.
        /// </summary>
        /// <returns>Base LoggingConfigSO</returns>
        private LoggingConfigSO CreateBaseConfig()
        {
            var targetConfigs = new List<LogTargetConfig>();
            foreach (var targetConfig in _targetConfigurations)
            {
                if (targetConfig != null && targetConfig.IsEnabled)
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

            return new LoggingConfigSO
            {
                GlobalMinimumLevel = _globalMinimumLevel,
                IsLoggingEnabled = _isLoggingEnabled,
                MaxQueueSize = _maxQueueSize,
                FlushInterval = FlushInterval,
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

        /// <summary>
        /// Applies scenario-specific overrides to the configuration.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyScenarioOverrides(LoggingConfigSO configSo)
        {
            switch (_currentScenario)
            {
                case LoggingScenario.Development:
                    ApplyDevelopmentOverrides(configSo);
                    break;
                case LoggingScenario.Testing:
                    ApplyTestingOverrides(configSo);
                    break;
                case LoggingScenario.Staging:
                    ApplyStagingOverrides(configSo);
                    break;
                case LoggingScenario.Production:
                    ApplyProductionOverrides(configSo);
                    break;
                case LoggingScenario.Debugging:
                    ApplyDebuggingOverrides(configSo);
                    break;
            }
        }

        /// <summary>
        /// Applies platform-specific overrides to the configuration.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyPlatformOverrides(LoggingConfigSO configSo)
        {
            var currentPlatform = Application.platform;
            
            foreach (var platformOverride in _platformOverrides)
            {
                if (platformOverride.Platform == currentPlatform && platformOverride.IsEnabled)
                {
                    platformOverride.ApplyOverrides(configSo);
                }
            }
        }

        /// <summary>
        /// Applies development scenario overrides.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyDevelopmentOverrides(LoggingConfigSO configSo)
        {
            // Development typically wants verbose logging
            // This would modify the configSo object if it were mutable
            // For now, we'll note that this is where development-specific logic would go
        }

        /// <summary>
        /// Applies testing scenario overrides.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyTestingOverrides(LoggingConfigSO configSo)
        {
            // Testing typically wants controllable logging
        }

        /// <summary>
        /// Applies staging scenario overrides.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyStagingOverrides(LoggingConfigSO configSo)
        {
            // Staging typically wants production-like logging with some debug info
        }

        /// <summary>
        /// Applies production scenario overrides.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyProductionOverrides(LoggingConfigSO configSo)
        {
            // Production typically wants minimal, efficient logging
        }

        /// <summary>
        /// Applies debugging scenario overrides.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        private void ApplyDebuggingOverrides(LoggingConfigSO configSo)
        {
            // Debugging typically wants maximum verbosity
        }

        /// <summary>
        /// Validates the configuration settings and clamps values to valid ranges.
        /// </summary>
        protected override void OnValidate()
        {
            if (!_enableEditorValidation) return;

            base.OnValidate();
            
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
            _targetConfigurations.RemoveAll(target => target == null);
            foreach (var target in _targetConfigurations)
            {
                if (target.EnableEditorValidation)
                {
                    target.ValidateConfiguration();
                }
            }

            // Validate formatter configurations
            _formatterConfigurations.RemoveAll(formatter => formatter == null);
            foreach (var formatter in _formatterConfigurations)
            {
                if (formatter.EnableEditorValidation)
                {
                    formatter.ValidateConfiguration();
                }
            }

            // Validate filter configurations
            _filterConfigurations.RemoveAll(filter => filter == null);
            foreach (var filter in _filterConfigurations)
            {
                if (filter.EnableEditorValidation)
                {
                    filter.ValidateConfiguration();
                }
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
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "Logging Configuration";
            _description = "Master logging system configuration";
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
            _formatterConfigurations.Clear();
            _filterConfigurations.Clear();
            _channelConfigurations.Clear();
            _channelConfigurations.Add(new LogChannelConfiguration
            {
                Name = "Default",
                MinimumLevel = LogLevel.Debug,
                IsEnabled = true,
                Description = "Default logging channel"
            });

            _currentScenario = LoggingScenario.Development;
            _useScenarioOverrides = true;
            _enablePlatformOptimizations = true;
            _platformOverrides.Clear();
            _enableEditorValidation = true;
            _showAdvancedOptions = false;
            _enablePreviewMode = false;
        }

        /// <summary>
        /// Validates the current configuration and returns any errors.
        /// </summary>
        /// <returns>A list of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            try
            {
                var config = ConfigSo;
                var configErrors = config.Validate();
                errors.AddRange(configErrors);
            }
            catch (Exception ex)
            {
                errors.Add($"Configuration creation failed: {ex.Message}");
            }

            // Validate individual components
            foreach (var target in _targetConfigurations)
            {
                if (target != null)
                {
                    var targetErrors = target.ValidateConfiguration();
                    errors.AddRange(targetErrors.Select(e => $"Target '{target.Name}': {e}"));
                }
            }

            foreach (var formatter in _formatterConfigurations)
            {
                if (formatter != null)
                {
                    var formatterErrors = formatter.ValidateConfiguration();
                    errors.AddRange(formatterErrors.Select(e => $"Formatter '{formatter.Name}': {e}"));
                }
            }

            foreach (var filter in _filterConfigurations)
            {
                if (filter != null)
                {
                    var filterErrors = filter.ValidateConfiguration();
                    errors.AddRange(filterErrors.Select(e => $"Filter '{filter.Name}': {e}"));
                }
            }

            LogValidationResults(errors);
            return errors;
        }

        /// <summary>
        /// Adds a target configuration to the logging system.
        /// </summary>
        /// <param name="target">The target configuration to add</param>
        public void AddTargetConfiguration(LogTargetScriptableObject target)
        {
            if (target != null && !_targetConfigurations.Contains(target))
            {
                _targetConfigurations.Add(target);
            }
        }

        /// <summary>
        /// Removes a target configuration from the logging system.
        /// </summary>
        /// <param name="target">The target configuration to remove</param>
        public void RemoveTargetConfiguration(LogTargetScriptableObject target)
        {
            _targetConfigurations.Remove(target);
        }

        /// <summary>
        /// Adds a formatter configuration to the logging system.
        /// </summary>
        /// <param name="formatter">The formatter configuration to add</param>
        public void AddFormatterConfiguration(LogFormatterScriptableObject formatter)
        {
            if (formatter != null && !_formatterConfigurations.Contains(formatter))
            {
                _formatterConfigurations.Add(formatter);
            }
        }

        /// <summary>
        /// Removes a formatter configuration from the logging system.
        /// </summary>
        /// <param name="formatter">The formatter configuration to remove</param>
        public void RemoveFormatterConfiguration(LogFormatterScriptableObject formatter)
        {
            _formatterConfigurations.Remove(formatter);
        }

        /// <summary>
        /// Adds a filter configuration to the logging system.
        /// </summary>
        /// <param name="filter">The filter configuration to add</param>
        public void AddFilterConfiguration(LogFilterScriptableObject filter)
        {
            if (filter != null && !_filterConfigurations.Contains(filter))
            {
                _filterConfigurations.Add(filter);
            }
        }

        /// <summary>
        /// Removes a filter configuration from the logging system.
        /// </summary>
        /// <param name="filter">The filter configuration to remove</param>
        public void RemoveFilterConfiguration(LogFilterScriptableObject filter)
        {
            _filterConfigurations.Remove(filter);
        }

        /// <summary>
        /// Sets the current scenario and applies overrides.
        /// </summary>
        /// <param name="scenario">The scenario to set</param>
        public void SetScenario(LoggingScenario scenario)
        {
            _currentScenario = scenario;
            _useScenarioOverrides = true;
        }

        /// <summary>
        /// Gets a summary of the current configuration.
        /// </summary>
        /// <returns>Configuration summary</returns>
        public string GetConfigurationSummary()
        {
            var summary = $"Logging Configuration: {Name}\n";
            summary += $"Enabled: {_isLoggingEnabled}\n";
            summary += $"Global Level: {_globalMinimumLevel}\n";
            summary += $"Scenario: {_currentScenario}\n";
            summary += $"Targets: {_targetConfigurations.Count(t => t != null && t.IsEnabled)}\n";
            summary += $"Formatters: {_formatterConfigurations.Count(f => f != null && f.IsEnabled)}\n";
            summary += $"Filters: {_filterConfigurations.Count(f => f != null && f.IsEnabled)}\n";
            summary += $"Channels: {_channelConfigurations.Count(c => c.IsEnabled)}\n";
            summary += $"High Performance: {_highPerformanceMode}\n";
            summary += $"Burst Compatible: {_burstCompatibility}\n";
            return summary;
        }
    }

    /// <summary>
    /// Represents different logging scenarios.
    /// </summary>
    public enum LoggingScenario
    {
        Development,
        Testing,
        Staging,
        Production,
        Debugging
    }

    /// <summary>
    /// Represents a platform-specific configuration override.
    /// </summary>
    [System.Serializable]
    public class PlatformOverride
    {
        [SerializeField] public RuntimePlatform Platform;
        [SerializeField] public bool IsEnabled = true;
        [SerializeField] public LogLevel MinimumLevelOverride = LogLevel.Info;
        [SerializeField] public bool OverrideMinimumLevel = false;
        [SerializeField] public bool DisableBatchingOnPlatform = false;
        [SerializeField] public bool DisableHighPerformanceMode = false;
        [SerializeField] public int MaxQueueSizeOverride = 1000;
        [SerializeField] public bool OverrideMaxQueueSize = false;

        /// <summary>
        /// Applies the platform overrides to the configuration.
        /// </summary>
        /// <param name="configSo">The configuration to modify</param>
        public void ApplyOverrides(LoggingConfigSO configSo)
        {
            // Since LoggingConfigSO is immutable, we'd need to create a new one
            // For now, this serves as a placeholder for the override logic
            // In a full implementation, this would return a modified configuration
        }
    }

    /// <summary>
    /// Serializable configuration for a log channel.
    /// </summary>
    [System.Serializable]
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
}