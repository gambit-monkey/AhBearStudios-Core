using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;
using UnityEngine;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// Unity ScriptableObject asset for logging system configuration.
    /// Provides Unity Editor integration for logging configuration while maintaining
    /// the Builder → ConfigSo → Factory → Service pattern.
    /// Follows AhBearStudios Core Development Guidelines for Unity Game Development First approach.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LoggingConfigSO",
        menuName = "AhBearStudios/Core/Logging/Logging Configuration",
        order = 1)]
    public class LoggingConfigAsset : ScriptableObject
    {
        [Header("Core Settings")]
        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Info;
        [SerializeField] private bool _enableHighPerformanceMode = true;
        [SerializeField] private bool _enableBurstCompatibility = true;
        [SerializeField] private bool _enableStructuredLogging = true;
        [SerializeField] private bool _enableCorrelationIds = true;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableBatching = true;
        [SerializeField] private int _batchSize = 100;
        [SerializeField] private float _flushIntervalSeconds = 0.1f;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private int _maxCacheSize = 1000;

        [Header("Unity Integration")]
        [SerializeField] private bool _enableUnityConsoleTarget = true;
        [SerializeField] private bool _enableFileLogging = true;
        [SerializeField] private string _logFilePath = "Logs/game.log";
        [SerializeField] private bool _enableMemoryTarget = false;
        [SerializeField] private int _memoryTargetCapacity = 1000;

        [Header("Serilog Integration")]
        [SerializeField] private bool _enableSerilogTarget = true;
        [SerializeField] private bool _serilogEnableConsole = true;
        [SerializeField] private bool _serilogEnableFileLogging = true;
        [SerializeField] private string _serilogFilePath = "Logs/serilog.log";
        [SerializeField] private bool _serilogEnableDebug = false;
        [SerializeField] private int _serilogFileSizeLimitMB = 10;
        [SerializeField] private int _serilogRetainedFileCount = 5;
        [SerializeField] private bool _serilogUseJsonFormatter = true;

        [Header("System Integration")]
        [SerializeField] private bool _enableHealthCheckIntegration = true;
        [SerializeField] private bool _enableAlertIntegration = true;
        [SerializeField] private bool _enableProfilerIntegration = true;
        [SerializeField] private float _healthCheckIntervalMinutes = 1.0f;

        [Header("Debug Settings")]
        [SerializeField] private bool _verboseInitialization = false;
        [SerializeField] private bool _validateConfiguration = true;
        [SerializeField] private bool _enableBootstrapLogging = true;

        #region Public Properties

        /// <summary>
        /// Gets the global minimum log level.
        /// </summary>
        public LogLevel GlobalMinimumLevel => _globalMinimumLevel;

        /// <summary>
        /// Gets whether high performance mode is enabled.
        /// </summary>
        public bool EnableHighPerformanceMode => _enableHighPerformanceMode;

        /// <summary>
        /// Gets whether Burst compatibility is enabled.
        /// </summary>
        public bool EnableBurstCompatibility => _enableBurstCompatibility;

        /// <summary>
        /// Gets whether structured logging is enabled.
        /// </summary>
        public bool EnableStructuredLogging => _enableStructuredLogging;

        /// <summary>
        /// Gets whether correlation IDs are enabled.
        /// </summary>
        public bool EnableCorrelationIds => _enableCorrelationIds;

        /// <summary>
        /// Gets whether batching is enabled.
        /// </summary>
        public bool EnableBatching => _enableBatching;

        /// <summary>
        /// Gets the batch size.
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// Gets the flush interval in seconds.
        /// </summary>
        public float FlushIntervalSeconds => _flushIntervalSeconds;

        /// <summary>
        /// Gets whether caching is enabled.
        /// </summary>
        public bool EnableCaching => _enableCaching;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Gets whether Unity console target is enabled.
        /// </summary>
        public bool EnableUnityConsoleTarget => _enableUnityConsoleTarget;

        /// <summary>
        /// Gets whether file logging is enabled.
        /// </summary>
        public bool EnableFileLogging => _enableFileLogging;

        /// <summary>
        /// Gets the log file path.
        /// </summary>
        public string LogFilePath => _logFilePath;

        /// <summary>
        /// Gets whether memory target is enabled.
        /// </summary>
        public bool EnableMemoryTarget => _enableMemoryTarget;

        /// <summary>
        /// Gets the memory target capacity.
        /// </summary>
        public int MemoryTargetCapacity => _memoryTargetCapacity;

        /// <summary>
        /// Gets whether Serilog target is enabled.
        /// </summary>
        public bool EnableSerilogTarget => _enableSerilogTarget;

        /// <summary>
        /// Gets whether Serilog console output is enabled.
        /// </summary>
        public bool SerilogEnableConsole => _serilogEnableConsole;

        /// <summary>
        /// Gets whether Serilog file logging is enabled.
        /// </summary>
        public bool SerilogEnableFileLogging => _serilogEnableFileLogging;

        /// <summary>
        /// Gets the Serilog file path.
        /// </summary>
        public string SerilogFilePath => _serilogFilePath;

        /// <summary>
        /// Gets whether Serilog debug output is enabled.
        /// </summary>
        public bool SerilogEnableDebug => _serilogEnableDebug;

        /// <summary>
        /// Gets the Serilog file size limit in MB.
        /// </summary>
        public int SerilogFileSizeLimitMB => _serilogFileSizeLimitMB;

        /// <summary>
        /// Gets the Serilog retained file count.
        /// </summary>
        public int SerilogRetainedFileCount => _serilogRetainedFileCount;

        /// <summary>
        /// Gets whether Serilog should use JSON formatter.
        /// </summary>
        public bool SerilogUseJsonFormatter => _serilogUseJsonFormatter;

        /// <summary>
        /// Gets whether health check integration is enabled.
        /// </summary>
        public bool EnableHealthCheckIntegration => _enableHealthCheckIntegration;

        /// <summary>
        /// Gets whether alert integration is enabled.
        /// </summary>
        public bool EnableAlertIntegration => _enableAlertIntegration;

        /// <summary>
        /// Gets whether profiler integration is enabled.
        /// </summary>
        public bool EnableProfilerIntegration => _enableProfilerIntegration;

        /// <summary>
        /// Gets the health check interval in minutes.
        /// </summary>
        public float HealthCheckIntervalMinutes => _healthCheckIntervalMinutes;

        /// <summary>
        /// Gets whether verbose initialization is enabled.
        /// </summary>
        public bool VerboseInitialization => _verboseInitialization;

        /// <summary>
        /// Gets whether configuration validation is enabled.
        /// </summary>
        public bool ValidateConfiguration => _validateConfiguration;

        /// <summary>
        /// Gets whether bootstrap logging is enabled.
        /// </summary>
        public bool EnableBootstrapLogging => _enableBootstrapLogging;

        #endregion

        #region Configuration Conversion

        /// <summary>
        /// Converts this ScriptableObject to a Core LoggingConfigSO.
        /// Follows the Builder → ConfigSo → Factory → Service pattern.
        /// </summary>
        /// <returns>A configured LoggingConfigSO instance</returns>
        public AhBearStudios.Core.Logging.Configs.LoggingConfig ToConfig()
        {
            return new AhBearStudios.Core.Logging.Configs.LoggingConfig
            {
                GlobalMinimumLevel = _globalMinimumLevel,
                HighPerformanceMode = _enableHighPerformanceMode,
                BurstCompatibility = _enableBurstCompatibility,
                BatchingEnabled = _enableBatching,
                BatchSize = _batchSize,
                FlushInterval = TimeSpan.FromSeconds(_flushIntervalSeconds),
                StructuredLogging = _enableStructuredLogging,
                AutoCorrelationId = _enableCorrelationIds,
                CachingEnabled = _enableCaching,
                MaxCacheSize = _maxCacheSize
            };
        }

        /// <summary>
        /// Creates target configurations based on this asset's settings.
        /// </summary>
        /// <returns>A list of target configurations</returns>
        public List<AhBearStudios.Core.Logging.Configs.LogTargetConfig> CreateTargetConfigs()
        {
            var configs = new List<AhBearStudios.Core.Logging.Configs.LogTargetConfig>();

            // Unity Console Target
            if (_enableUnityConsoleTarget)
            {
                configs.Add(new AhBearStudios.Core.Logging.Configs.LogTargetConfig
                {
                    Name = "UnityConsole",
                    TargetType = "UnityConsole",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    Properties = new Dictionary<string, object>()
                });
            }

            // File Target
            if (_enableFileLogging)
            {
                configs.Add(new AhBearStudios.Core.Logging.Configs.LogTargetConfig
                {
                    Name = "File",
                    TargetType = "File",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["FilePath"] = _logFilePath
                    }
                });
            }

            // Memory Target
            if (_enableMemoryTarget)
            {
                configs.Add(new AhBearStudios.Core.Logging.Configs.LogTargetConfig
                {
                    Name = "Memory",
                    TargetType = "Memory",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["MaxEntries"] = _memoryTargetCapacity
                    }
                });
            }

            // Serilog Target
            if (_enableSerilogTarget)
            {
                configs.Add(new AhBearStudios.Core.Logging.Configs.LogTargetConfig
                {
                    Name = "Serilog",
                    TargetType = "Serilog",
                    MinimumLevel = _globalMinimumLevel,
                    IsEnabled = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["EnableConsole"] = _serilogEnableConsole,
                        ["EnableFileLogging"] = _serilogEnableFileLogging,
                        ["FilePath"] = _serilogFilePath,
                        ["EnableDebug"] = _serilogEnableDebug,
                        ["FileSizeLimitBytes"] = _serilogFileSizeLimitMB * 1024 * 1024,
                        ["RetainedFileCountLimit"] = _serilogRetainedFileCount,
                        ["UseJsonFormatter"] = _serilogUseJsonFormatter,
                        ["EnablePerformanceMetrics"] = _enableProfilerIntegration,
                        ["FrameBudgetThresholdMs"] = 0.5f,
                        ["HealthCheckIntervalSeconds"] = _healthCheckIntervalMinutes * 60f
                    }
                });
            }

            return configs;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration and returns any errors.
        /// </summary>
        /// <returns>A list of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Validate file paths
            if (_enableFileLogging && string.IsNullOrWhiteSpace(_logFilePath))
            {
                errors.Add("Log file path is required when file logging is enabled");
            }

            if (_enableSerilogTarget && _serilogEnableFileLogging && string.IsNullOrWhiteSpace(_serilogFilePath))
            {
                errors.Add("Serilog file path is required when Serilog file logging is enabled");
            }

            // Validate numeric values
            if (_enableBatching && _batchSize <= 0)
            {
                errors.Add("Batch size must be greater than zero when batching is enabled");
            }

            if (_enableMemoryTarget && _memoryTargetCapacity <= 0)
            {
                errors.Add("Memory target capacity must be greater than zero when memory target is enabled");
            }

            if (_flushIntervalSeconds <= 0)
            {
                errors.Add("Flush interval must be greater than zero");
            }

            if (_enableCaching && _maxCacheSize <= 0)
            {
                errors.Add("Max cache size must be greater than zero when caching is enabled");
            }

            if (_healthCheckIntervalMinutes <= 0)
            {
                errors.Add("Health check interval must be greater than zero");
            }

            // Validate Serilog settings
            if (_enableSerilogTarget)
            {
                if (_serilogFileSizeLimitMB <= 0)
                {
                    errors.Add("Serilog file size limit must be greater than zero");
                }

                if (_serilogRetainedFileCount <= 0)
                {
                    errors.Add("Serilog retained file count must be greater than zero");
                }
            }

            return errors;
        }

        #endregion

        #region Unity Editor Integration

        /// <summary>
        /// Unity validation callback for Inspector value changes.
        /// </summary>
        private void OnValidate()
        {
            // Clamp values to valid ranges
            _batchSize = Mathf.Max(1, _batchSize);
            _flushIntervalSeconds = Mathf.Max(0.01f, _flushIntervalSeconds);
            _memoryTargetCapacity = Mathf.Max(10, _memoryTargetCapacity);
            _maxCacheSize = Mathf.Max(10, _maxCacheSize);
            _healthCheckIntervalMinutes = Mathf.Max(0.1f, _healthCheckIntervalMinutes);
            _serilogFileSizeLimitMB = Mathf.Max(1, _serilogFileSizeLimitMB);
            _serilogRetainedFileCount = Mathf.Max(1, _serilogRetainedFileCount);

            // Validate file paths
            if (_enableFileLogging && !string.IsNullOrWhiteSpace(_logFilePath))
            {
                try
                {
                    var directory = System.IO.Path.GetDirectoryName(_logFilePath);
                    if (string.IsNullOrEmpty(directory))
                    {
                        _logFilePath = "Logs/game.log";
                    }
                }
                catch
                {
                    _logFilePath = "Logs/game.log";
                }
            }

            if (_enableSerilogTarget && _serilogEnableFileLogging && !string.IsNullOrWhiteSpace(_serilogFilePath))
            {
                try
                {
                    var directory = System.IO.Path.GetDirectoryName(_serilogFilePath);
                    if (string.IsNullOrEmpty(directory))
                    {
                        _serilogFilePath = "Logs/serilog.log";
                    }
                }
                catch
                {
                    _serilogFilePath = "Logs/serilog.log";
                }
            }
        }

        /// <summary>
        /// Resets all configuration values to their defaults.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            _globalMinimumLevel = LogLevel.Info;
            _enableHighPerformanceMode = true;
            _enableBurstCompatibility = true;
            _enableStructuredLogging = true;
            _enableCorrelationIds = true;
            _enableBatching = true;
            _batchSize = 100;
            _flushIntervalSeconds = 0.1f;
            _enableCaching = true;
            _maxCacheSize = 1000;
            _enableUnityConsoleTarget = true;
            _enableFileLogging = true;
            _logFilePath = "Logs/game.log";
            _enableMemoryTarget = false;
            _memoryTargetCapacity = 1000;
            _enableSerilogTarget = true;
            _serilogEnableConsole = true;
            _serilogEnableFileLogging = true;
            _serilogFilePath = "Logs/serilog.log";
            _serilogEnableDebug = false;
            _serilogFileSizeLimitMB = 10;
            _serilogRetainedFileCount = 5;
            _serilogUseJsonFormatter = true;
            _enableHealthCheckIntegration = true;
            _enableAlertIntegration = true;
            _enableProfilerIntegration = true;
            _healthCheckIntervalMinutes = 1.0f;
            _verboseInitialization = false;
            _validateConfiguration = true;
            _enableBootstrapLogging = true;
        }

        #endregion
    }
}