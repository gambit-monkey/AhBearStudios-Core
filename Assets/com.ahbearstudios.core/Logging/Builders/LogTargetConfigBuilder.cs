using System.Collections.Generic;
using System.IO;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.Serialization;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Concrete implementation of ILogTargetConfigBuilder for creating robust, production-ready log target configurations.
    /// Provides comprehensive validation, Unity-specific features, and core system integration.
    /// </summary>
    public sealed class LogTargetConfigBuilder : ILogTargetConfigBuilder
    {
        private readonly ISerializer _serializer;
        private readonly Dictionary<string, object> _properties = new();
        private readonly List<string> _channels = new();
        private readonly List<Func<ILogTargetConfig, IReadOnlyList<string>>> _validators = new();
        private readonly Dictionary<string, (Func<object, bool> validator, string errorMessage)> _propertyValidators = new();
        private readonly Dictionary<string, Func<object>> _customMetrics = new();

        // Core configuration properties
        private string _name = string.Empty;
        private string _targetType = string.Empty;
        private LogLevel _minimumLevel = LogLevel.Debug;
        private bool _isEnabled = true;
        private int _bufferSize = 100;
        private TimeSpan _flushInterval = TimeSpan.FromMilliseconds(100);
        private bool _useAsyncWrite = true;
        private string _messageFormat = string.Empty;
        private bool _includeStackTrace = true;
        private bool _includeCorrelationId = true;

        // Unity-specific game development properties
        private double _errorRateThreshold = 0.1;
        private double _frameBudgetThresholdMs = 0.5;
        private int _alertSuppressionIntervalMinutes = 5;
        private int _maxConcurrentOperations = 10;
        private bool _enableUnityProfilerIntegration = true;
        private bool _enablePerformanceMetrics = true;
        private int _healthCheckIntervalSeconds = 30;
        private double _frameRateImpactLimitMs = 1.0;
        private long _memoryBudgetBytes = 10 * 1024 * 1024; // 10MB default
        private int _gcPressureLimit = 1000;

        // Core system integration
        private IProfilerService _profiler;
        private IAlertService _alertService;
        private IHealthCheckService _healthService;

        /// <summary>
        /// Initializes a new instance of LogTargetConfigBuilder.
        /// </summary>
        /// <param name="serializer">Serializer for configuration serialization support</param>
        /// <exception cref="ArgumentNullException">Thrown when serializer is null</exception>
        public LogTargetConfigBuilder(ISerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        #region Core Configuration Methods

        public ILogTargetConfigBuilder WithName(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        public ILogTargetConfigBuilder WithTargetType(string targetType)
        {
            _targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            return this;
        }

        public ILogTargetConfigBuilder WithMinimumLevel(LogLevel level)
        {
            _minimumLevel = level;
            return this;
        }

        public ILogTargetConfigBuilder WithEnabled(bool isEnabled)
        {
            _isEnabled = isEnabled;
            return this;
        }

        public ILogTargetConfigBuilder WithBufferSize(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException("Buffer size must be greater than zero.", nameof(bufferSize));
            _bufferSize = bufferSize;
            return this;
        }

        public ILogTargetConfigBuilder WithFlushInterval(TimeSpan flushInterval)
        {
            if (flushInterval <= TimeSpan.Zero)
                throw new ArgumentException("Flush interval must be greater than zero.", nameof(flushInterval));
            _flushInterval = flushInterval;
            return this;
        }

        public ILogTargetConfigBuilder WithAsyncWrite(bool useAsync)
        {
            _useAsyncWrite = useAsync;
            return this;
        }

        public ILogTargetConfigBuilder WithMessageFormat(string messageFormat)
        {
            _messageFormat = messageFormat ?? string.Empty;
            return this;
        }

        public ILogTargetConfigBuilder WithChannel(string channel)
        {
            if (!string.IsNullOrWhiteSpace(channel))
            {
                _channels.Add(channel);
            }
            return this;
        }

        public ILogTargetConfigBuilder WithChannels(params string[] channels)
        {
            if (channels != null)
            {
                foreach (var channel in channels)
                {
                    if (!string.IsNullOrWhiteSpace(channel))
                    {
                        _channels.Add(channel);
                    }
                }
            }
            return this;
        }

        public ILogTargetConfigBuilder ClearChannels()
        {
            _channels.Clear();
            return this;
        }

        public ILogTargetConfigBuilder WithStackTrace(bool includeStackTrace)
        {
            _includeStackTrace = includeStackTrace;
            return this;
        }

        public ILogTargetConfigBuilder WithCorrelationId(bool includeCorrelationId)
        {
            _includeCorrelationId = includeCorrelationId;
            return this;
        }

        #endregion

        #region Unity-Specific Game Development Properties

        public ILogTargetConfigBuilder WithErrorRateThreshold(double threshold)
        {
            if (threshold < 0.0 || threshold > 1.0)
                throw new ArgumentException("Error rate threshold must be between 0.0 and 1.0.", nameof(threshold));
            _errorRateThreshold = threshold;
            return this;
        }

        public ILogTargetConfigBuilder WithFrameBudgetThreshold(double thresholdMs)
        {
            if (thresholdMs < 0.0)
                throw new ArgumentException("Frame budget threshold must be non-negative.", nameof(thresholdMs));
            _frameBudgetThresholdMs = thresholdMs;
            return this;
        }

        public ILogTargetConfigBuilder WithAlertSuppressionInterval(int intervalMinutes)
        {
            if (intervalMinutes < 0)
                throw new ArgumentException("Alert suppression interval must be non-negative.", nameof(intervalMinutes));
            _alertSuppressionIntervalMinutes = intervalMinutes;
            return this;
        }

        public ILogTargetConfigBuilder WithMaxConcurrentOperations(int maxOperations)
        {
            if (maxOperations <= 0)
                throw new ArgumentException("Max concurrent operations must be greater than zero.", nameof(maxOperations));
            _maxConcurrentOperations = maxOperations;
            return this;
        }

        public ILogTargetConfigBuilder WithUnityProfilerIntegration(bool enabled)
        {
            _enableUnityProfilerIntegration = enabled;
            return this;
        }

        public ILogTargetConfigBuilder WithPerformanceMetrics(bool enabled)
        {
            _enablePerformanceMetrics = enabled;
            return this;
        }

        public ILogTargetConfigBuilder WithHealthCheckInterval(int intervalSeconds)
        {
            if (intervalSeconds <= 0)
                throw new ArgumentException("Health check interval must be greater than zero.", nameof(intervalSeconds));
            _healthCheckIntervalSeconds = intervalSeconds;
            return this;
        }

        public ILogTargetConfigBuilder WithFrameRateImpactLimit(double maxFrameImpactMs)
        {
            if (maxFrameImpactMs < 0.0)
                throw new ArgumentException("Frame rate impact limit must be non-negative.", nameof(maxFrameImpactMs));
            _frameRateImpactLimitMs = maxFrameImpactMs;
            return this;
        }

        public ILogTargetConfigBuilder WithMemoryBudget(long maxMemoryBytes)
        {
            if (maxMemoryBytes <= 0)
                throw new ArgumentException("Memory budget must be greater than zero.", nameof(maxMemoryBytes));
            _memoryBudgetBytes = maxMemoryBytes;
            return this;
        }

        public ILogTargetConfigBuilder WithGCPressureLimit(int maxAllocationsPerSecond)
        {
            if (maxAllocationsPerSecond < 0)
                throw new ArgumentException("GC pressure limit must be non-negative.", nameof(maxAllocationsPerSecond));
            _gcPressureLimit = maxAllocationsPerSecond;
            return this;
        }

        public ILogTargetConfigBuilder WithPlatformSpecificSettings(RuntimePlatform platform, Action<ILogTargetConfigBuilder> platformConfig)
        {
            if (platformConfig == null)
                throw new ArgumentNullException(nameof(platformConfig));

            if (Application.platform == platform)
            {
                platformConfig(this);
            }
            return this;
        }

        public ILogTargetConfigBuilder WithDevelopmentMode(bool isDevelopment)
        {
            if (isDevelopment)
            {
                // Development-friendly settings
                _minimumLevel = LogLevel.Debug;
                _enableUnityProfilerIntegration = true;
                _enablePerformanceMetrics = true;
                _healthCheckIntervalSeconds = 15; // More frequent health checks
                _frameBudgetThresholdMs = 1.0; // More lenient frame budget
            }
            else
            {
                // Production-optimized settings
                _minimumLevel = LogLevel.Info;
                _enableUnityProfilerIntegration = false;
                _enablePerformanceMetrics = false;
                _healthCheckIntervalSeconds = 60; // Less frequent health checks
                _frameBudgetThresholdMs = 0.3; // Stricter frame budget
            }
            return this;
        }

        public ILogTargetConfigBuilder WithBuildType(bool isDebugBuild)
        {
            if (isDebugBuild)
            {
                // Debug build settings
                _includeStackTrace = true;
                _includeCorrelationId = true;
                _enableUnityProfilerIntegration = true;
            }
            else
            {
                // Release build settings
                _includeStackTrace = false;
                _includeCorrelationId = false;
                _enableUnityProfilerIntegration = false;
            }
            return this;
        }

        #endregion

        #region Custom Property Management

        public ILogTargetConfigBuilder WithProperty<T>(string key, T value, Func<T, bool> validator = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty.", nameof(key));

            if (validator != null && !validator(value))
                throw new ArgumentException($"Property '{key}' failed validation.", nameof(value));

            _properties[key] = value;
            return this;
        }

        public ILogTargetConfigBuilder WithPropertyFromEnvironment(string key, string environmentVariable, object defaultValue)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty.", nameof(key));

            if (string.IsNullOrWhiteSpace(environmentVariable))
                throw new ArgumentException("Environment variable name cannot be null or empty.", nameof(environmentVariable));

            var envValue = Environment.GetEnvironmentVariable(environmentVariable);
            _properties[key] = envValue ?? defaultValue;
            return this;
        }

        public ILogTargetConfigBuilder WithSecureProperty(string key, string encryptedValue)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty.", nameof(key));

            // TODO: Implement decryption logic when security system is available
            _properties[key] = encryptedValue;
            return this;
        }

        public ILogTargetConfigBuilder WithProperties(IDictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        public ILogTargetConfigBuilder WithPropertiesFromConfig(ILogTargetConfig sourceConfig)
        {
            if (sourceConfig?.Properties != null)
            {
                foreach (var kvp in sourceConfig.Properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        public ILogTargetConfigBuilder RemoveProperty(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _properties.Remove(key);
            }
            return this;
        }

        public ILogTargetConfigBuilder ClearProperties()
        {
            _properties.Clear();
            return this;
        }

        #endregion

        #region Core Systems Integration

        public ILogTargetConfigBuilder WithProfilerIntegration(IProfilerService profiler)
        {
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            return this;
        }

        public ILogTargetConfigBuilder WithAlertingIntegration(IAlertService alertService)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            return this;
        }

        public ILogTargetConfigBuilder WithHealthCheckIntegration(IHealthCheckService healthService)
        {
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
            return this;
        }

        public ILogTargetConfigBuilder WithCustomMetric(string metricName, Func<object> metricProvider)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty.", nameof(metricName));

            _customMetrics[metricName] = metricProvider ?? throw new ArgumentNullException(nameof(metricProvider));
            return this;
        }

        #endregion

        #region Conditional Configuration

        public ILogTargetConfigBuilder When(Func<bool> condition, Action<ILogTargetConfigBuilder> configuration)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (condition())
            {
                configuration(this);
            }
            return this;
        }

        public ILogTargetConfigBuilder ForPlatform(RuntimePlatform platform, Action<ILogTargetConfigBuilder> configuration)
        {
            return When(() => Application.platform == platform, configuration);
        }

        public ILogTargetConfigBuilder ForBuildType(bool isDebugBuild, Action<ILogTargetConfigBuilder> configuration)
        {
            return When(() => Debug.isDebugBuild == isDebugBuild, configuration);
        }

        #endregion

        #region Configuration Inheritance & Composition

        public ILogTargetConfigBuilder BasedOn(ILogTargetConfig baseConfig)
        {
            if (baseConfig == null)
                throw new ArgumentNullException(nameof(baseConfig));

            _name = baseConfig.Name;
            _targetType = baseConfig.TargetType;
            _minimumLevel = baseConfig.MinimumLevel;
            _isEnabled = baseConfig.IsEnabled;
            _bufferSize = baseConfig.BufferSize;
            _flushInterval = baseConfig.FlushInterval;
            _useAsyncWrite = baseConfig.UseAsyncWrite;
            _messageFormat = baseConfig.MessageFormat;
            _includeStackTrace = baseConfig.IncludeStackTrace;
            _includeCorrelationId = baseConfig.IncludeCorrelationId;
            _errorRateThreshold = baseConfig.ErrorRateThreshold;
            _frameBudgetThresholdMs = baseConfig.FrameBudgetThresholdMs;
            _alertSuppressionIntervalMinutes = baseConfig.AlertSuppressionIntervalMinutes;
            _maxConcurrentOperations = baseConfig.MaxConcurrentAsyncOperations;
            _enableUnityProfilerIntegration = baseConfig.EnableUnityProfilerIntegration;
            _enablePerformanceMetrics = baseConfig.EnablePerformanceMetrics;
            _healthCheckIntervalSeconds = baseConfig.HealthCheckIntervalSeconds;

            // Copy channels
            _channels.Clear();
            _channels.AddRange(baseConfig.Channels);

            // Copy properties
            WithPropertiesFromConfig(baseConfig);

            return this;
        }

        public ILogTargetConfigBuilder WithDefaults(LogTargetDefaults defaults)
        {
            switch (defaults)
            {
                case LogTargetDefaults.GameOptimized:
                    ApplyGameOptimizedDefaults();
                    break;
                case LogTargetDefaults.Development:
                    ApplyDevelopmentDefaults();
                    break;
                case LogTargetDefaults.Production:
                    ApplyProductionDefaults();
                    break;
                case LogTargetDefaults.Mobile:
                    ApplyMobileDefaults();
                    break;
                case LogTargetDefaults.Console:
                    ApplyConsoleDefaults();
                    break;
                case LogTargetDefaults.Testing:
                    ApplyTestingDefaults();
                    break;
            }
            return this;
        }

        public ILogTargetConfigBuilder OverrideWith(ILogTargetConfig overrideConfig)
        {
            if (overrideConfig == null)
                throw new ArgumentNullException(nameof(overrideConfig));

            // Apply overrides only for non-default values
            if (!string.IsNullOrEmpty(overrideConfig.Name))
                _name = overrideConfig.Name;

            if (!string.IsNullOrEmpty(overrideConfig.TargetType))
                _targetType = overrideConfig.TargetType;

            // Apply property overrides
            WithPropertiesFromConfig(overrideConfig);

            return this;
        }

        #endregion

        #region Serialization (AhBearStudios.Core.Serialization Integration)

        public ILogTargetConfigBuilder FromSerialized(byte[] serializedData)
        {
            if (serializedData == null)
                throw new ArgumentNullException(nameof(serializedData));

            try
            {
                var config = _serializer.Deserialize<LogTargetConfig>(serializedData);
                return BasedOn(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize configuration data.", ex);
            }
        }

        public ILogTargetConfigBuilder FromStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                var config = _serializer.DeserializeFromStream<LogTargetConfig>(stream);
                return BasedOn(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize configuration from stream.", ex);
            }
        }

        public ILogTargetConfigBuilder FromScriptableObject(ScriptableObject configAsset)
        {
            if (configAsset == null)
                throw new ArgumentNullException(nameof(configAsset));

            // TODO: Implement ScriptableObject configuration extraction
            // This would require Unity-specific implementation
            throw new NotImplementedException("ScriptableObject configuration loading will be implemented in Unity layer.");
        }

        public ILogTargetConfigBuilder FromEnvironmentVariables(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));

            // Load common environment variables
            WithPropertyFromEnvironment("Name", $"{prefix}_NAME", _name);
            WithPropertyFromEnvironment("TargetType", $"{prefix}_TARGET_TYPE", _targetType);
            WithPropertyFromEnvironment("MinimumLevel", $"{prefix}_MIN_LEVEL", _minimumLevel.ToString());
            WithPropertyFromEnvironment("IsEnabled", $"{prefix}_ENABLED", _isEnabled.ToString());

            return this;
        }

        #endregion

        #region Validation & Error Handling

        public ILogTargetConfigBuilder WithValidation(Func<ILogTargetConfig, IReadOnlyList<string>> validator)
        {
            _validators.Add(validator ?? throw new ArgumentNullException(nameof(validator)));
            return this;
        }

        public ILogTargetConfigBuilder WithPropertyValidation<T>(string key, Func<T, bool> validator, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty.", nameof(key));

            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));

            _propertyValidators[key] = (obj => obj is T value && validator(value), errorMessage);
            return this;
        }

        public ILogTargetConfig Build()
        {
            if (!TryBuild(out var config, out var errors))
            {
                throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", errors)}");
            }
            return config;
        }

        public bool TryBuild(out ILogTargetConfig config, out IReadOnlyList<string> errors)
        {
            var errorList = new List<string>();
            config = null;

            // Create configuration
            var builtConfig = new LogTargetConfig
            {
                Name = _name,
                TargetType = _targetType,
                MinimumLevel = _minimumLevel,
                IsEnabled = _isEnabled,
                BufferSize = _bufferSize,
                FlushInterval = _flushInterval,
                UseAsyncWrite = _useAsyncWrite,
                MessageFormat = _messageFormat,
                IncludeStackTrace = _includeStackTrace,
                IncludeCorrelationId = _includeCorrelationId,
                ErrorRateThreshold = _errorRateThreshold,
                FrameBudgetThresholdMs = _frameBudgetThresholdMs,
                AlertSuppressionIntervalMinutes = _alertSuppressionIntervalMinutes,
                MaxConcurrentAsyncOperations = _maxConcurrentOperations,
                EnableUnityProfilerIntegration = _enableUnityProfilerIntegration,
                EnablePerformanceMetrics = _enablePerformanceMetrics,
                HealthCheckIntervalSeconds = _healthCheckIntervalSeconds,
                Channels = new List<string>(_channels),
                Properties = new Dictionary<string, object>(_properties)
            };

            // Validate built configuration
            var validationErrors = builtConfig.Validate();
            errorList.AddRange(validationErrors);

            // Run custom validators
            foreach (var validator in _validators)
            {
                try
                {
                    var customErrors = validator(builtConfig);
                    errorList.AddRange(customErrors);
                }
                catch (Exception ex)
                {
                    errorList.Add($"Validator threw exception: {ex.Message}");
                }
            }

            // Validate properties
            foreach (var kvp in _propertyValidators)
            {
                if (_properties.TryGetValue(kvp.Key, out var value))
                {
                    if (!kvp.Value.validator(value))
                    {
                        errorList.Add(kvp.Value.errorMessage);
                    }
                }
            }

            errors = errorList.AsReadOnly();
            
            if (errorList.Count == 0)
            {
                config = builtConfig;
                return true;
            }

            return false;
        }

        #endregion

        #region Private Helper Methods

        private void ApplyGameOptimizedDefaults()
        {
            _minimumLevel = LogLevel.Info;
            _frameBudgetThresholdMs = 0.3;
            _errorRateThreshold = 0.05;
            _enableUnityProfilerIntegration = true;
            _enablePerformanceMetrics = true;
            _healthCheckIntervalSeconds = 30;
            _memoryBudgetBytes = 5 * 1024 * 1024; // 5MB
            _gcPressureLimit = 500;
        }

        private void ApplyDevelopmentDefaults()
        {
            _minimumLevel = LogLevel.Debug;
            _frameBudgetThresholdMs = 1.0;
            _errorRateThreshold = 0.2;
            _enableUnityProfilerIntegration = true;
            _enablePerformanceMetrics = true;
            _healthCheckIntervalSeconds = 15;
            _includeStackTrace = true;
            _includeCorrelationId = true;
        }

        private void ApplyProductionDefaults()
        {
            _minimumLevel = LogLevel.Warning;
            _frameBudgetThresholdMs = 0.2;
            _errorRateThreshold = 0.02;
            _enableUnityProfilerIntegration = false;
            _enablePerformanceMetrics = false;
            _healthCheckIntervalSeconds = 60;
            _includeStackTrace = false;
            _includeCorrelationId = false;
        }

        private void ApplyMobileDefaults()
        {
            _minimumLevel = LogLevel.Warning;
            _frameBudgetThresholdMs = 0.1;
            _errorRateThreshold = 0.01;
            _enableUnityProfilerIntegration = false;
            _enablePerformanceMetrics = false;
            _healthCheckIntervalSeconds = 120;
            _memoryBudgetBytes = 2 * 1024 * 1024; // 2MB
            _gcPressureLimit = 100;
            _useAsyncWrite = true;
        }

        private void ApplyConsoleDefaults()
        {
            _minimumLevel = LogLevel.Info;
            _frameBudgetThresholdMs = 0.5;
            _errorRateThreshold = 0.05;
            _enableUnityProfilerIntegration = true;
            _enablePerformanceMetrics = true;
            _healthCheckIntervalSeconds = 30;
            _memoryBudgetBytes = 20 * 1024 * 1024; // 20MB
        }

        private void ApplyTestingDefaults()
        {
            _minimumLevel = LogLevel.Debug;
            _frameBudgetThresholdMs = 10.0; // Very lenient for testing
            _errorRateThreshold = 0.5;
            _enableUnityProfilerIntegration = false;
            _enablePerformanceMetrics = false;
            _healthCheckIntervalSeconds = 5;
            _useAsyncWrite = false; // Synchronous for deterministic tests
        }

        #endregion
    }
}