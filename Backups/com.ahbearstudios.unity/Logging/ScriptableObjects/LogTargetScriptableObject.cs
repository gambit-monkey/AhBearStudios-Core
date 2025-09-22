using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Configs;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// Base ScriptableObject for log target configurations.
    /// Provides Unity-serializable configuration for log targets.
    /// </summary>
    public abstract class LogTargetScriptableObject : LoggingScriptableObjectBase
    {
        [Header("Target Settings")]
        [SerializeField] protected string _targetType = "Unknown";
        [SerializeField] protected bool _useAsyncWrite = true;
        [SerializeField] protected int _bufferSize = 100;
        [SerializeField] protected float _flushIntervalSeconds = 0.1f;
        [SerializeField] protected int _maxConcurrentAsyncOperations = 4;

        [Header("Channel Configuration")]
        [SerializeField] protected List<string> _channels = new List<string>();
        [SerializeField] protected bool _listenToAllChannels = true;

        [Header("Performance Settings")]
        [SerializeField] protected bool _enablePerformanceMetrics = true;
        [SerializeField] protected float _frameBudgetThresholdMs = 0.5f;
        [SerializeField] protected float _errorRateThreshold = 0.1f;
        [SerializeField] protected int _alertSuppressionIntervalMinutes = 5;
        [SerializeField] protected int _healthCheckIntervalSeconds = 30;

        [Header("Unity Integration")]
        [SerializeField] protected bool _enableUnityProfilerIntegration = true;
        [SerializeField] protected bool _includeStackTrace = false;
        [SerializeField] protected bool _includeCorrelationId = true;

        /// <summary>
        /// Gets the target type identifier.
        /// </summary>
        public string TargetType => _targetType;

        /// <summary>
        /// Gets whether async write is enabled.
        /// </summary>
        public bool UseAsyncWrite => _useAsyncWrite;

        /// <summary>
        /// Gets the buffer size for this target.
        /// </summary>
        public int BufferSize => _bufferSize;

        /// <summary>
        /// Gets the flush interval as a TimeSpan.
        /// </summary>
        public TimeSpan FlushInterval => TimeSpan.FromSeconds(_flushIntervalSeconds);

        /// <summary>
        /// Gets the maximum concurrent async operations.
        /// </summary>
        public int MaxConcurrentAsyncOperations => _maxConcurrentAsyncOperations;

        /// <summary>
        /// Gets the list of channels this target listens to.
        /// </summary>
        public IReadOnlyList<string> Channels => _channels.AsReadOnly();

        /// <summary>
        /// Gets whether this target listens to all channels.
        /// </summary>
        public bool ListenToAllChannels => _listenToAllChannels;

        /// <summary>
        /// Gets whether performance metrics are enabled.
        /// </summary>
        public bool EnablePerformanceMetrics => _enablePerformanceMetrics;

        /// <summary>
        /// Gets the frame budget threshold in milliseconds.
        /// </summary>
        public double FrameBudgetThresholdMs => _frameBudgetThresholdMs;

        /// <summary>
        /// Gets the error rate threshold.
        /// </summary>
        public double ErrorRateThreshold => _errorRateThreshold;

        /// <summary>
        /// Gets the alert suppression interval in minutes.
        /// </summary>
        public int AlertSuppressionIntervalMinutes => _alertSuppressionIntervalMinutes;

        /// <summary>
        /// Gets the health check interval in seconds.
        /// </summary>
        public int HealthCheckIntervalSeconds => _healthCheckIntervalSeconds;

        /// <summary>
        /// Gets whether Unity Profiler integration is enabled.
        /// </summary>
        public bool EnableUnityProfilerIntegration => _enableUnityProfilerIntegration;

        /// <summary>
        /// Gets whether stack traces should be included.
        /// </summary>
        public bool IncludeStackTrace => _includeStackTrace;

        /// <summary>
        /// Gets whether correlation IDs should be included.
        /// </summary>
        public bool IncludeCorrelationId => _includeCorrelationId;

        /// <summary>
        /// Creates a LogTargetConfig from this ScriptableObject.
        /// </summary>
        /// <returns>A configured LogTargetConfig</returns>
        public virtual LogTargetConfig ToLogTargetConfig()
        {
            var properties = ToProperties();
            
            return new LogTargetConfig
            {
                Name = Name,
                TargetType = _targetType,
                MinimumLevel = MinimumLevel,
                IsEnabled = IsEnabled,
                BufferSize = _bufferSize,
                FlushInterval = FlushInterval,
                UseAsyncWrite = _useAsyncWrite,
                Properties = properties,
                Channels = _listenToAllChannels ? new List<string>() : _channels,
                IncludeStackTrace = _includeStackTrace,
                IncludeCorrelationId = _includeCorrelationId,
                ErrorRateThreshold = _errorRateThreshold,
                FrameBudgetThresholdMs = _frameBudgetThresholdMs,
                AlertSuppressionIntervalMinutes = _alertSuppressionIntervalMinutes,
                MaxConcurrentAsyncOperations = _maxConcurrentAsyncOperations,
                EnableUnityProfilerIntegration = _enableUnityProfilerIntegration,
                EnablePerformanceMetrics = _enablePerformanceMetrics,
                HealthCheckIntervalSeconds = _healthCheckIntervalSeconds
            };
        }

        /// <summary>
        /// Creates target-specific properties dictionary.
        /// Override in derived classes to add specific properties.
        /// </summary>
        /// <returns>Dictionary of target-specific properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["TargetType"] = _targetType;
            properties["UseAsyncWrite"] = _useAsyncWrite;
            properties["BufferSize"] = _bufferSize;
            properties["FlushIntervalSeconds"] = _flushIntervalSeconds;
            properties["MaxConcurrentAsyncOperations"] = _maxConcurrentAsyncOperations;
            properties["ListenToAllChannels"] = _listenToAllChannels;
            properties["EnablePerformanceMetrics"] = _enablePerformanceMetrics;
            properties["FrameBudgetThresholdMs"] = _frameBudgetThresholdMs;
            properties["ErrorRateThreshold"] = _errorRateThreshold;
            properties["AlertSuppressionIntervalMinutes"] = _alertSuppressionIntervalMinutes;
            properties["HealthCheckIntervalSeconds"] = _healthCheckIntervalSeconds;
            properties["EnableUnityProfilerIntegration"] = _enableUnityProfilerIntegration;
            properties["IncludeStackTrace"] = _includeStackTrace;
            properties["IncludeCorrelationId"] = _includeCorrelationId;
            
            return properties;
        }

        /// <summary>
        /// Validates the target configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(_targetType))
            {
                errors.Add("Target type cannot be empty");
            }

            if (_bufferSize <= 0)
            {
                errors.Add("Buffer size must be greater than zero");
            }

            if (_flushIntervalSeconds <= 0)
            {
                errors.Add("Flush interval must be greater than zero");
            }

            if (_maxConcurrentAsyncOperations <= 0)
            {
                errors.Add("Max concurrent async operations must be greater than zero");
            }

            if (_frameBudgetThresholdMs <= 0)
            {
                errors.Add("Frame budget threshold must be greater than zero");
            }

            if (_errorRateThreshold < 0 || _errorRateThreshold > 1)
            {
                errors.Add("Error rate threshold must be between 0 and 1");
            }

            if (_alertSuppressionIntervalMinutes < 0)
            {
                errors.Add("Alert suppression interval must be non-negative");
            }

            if (_healthCheckIntervalSeconds <= 0)
            {
                errors.Add("Health check interval must be greater than zero");
            }

            return errors;
        }

        /// <summary>
        /// Resets the target configuration to default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _targetType = "Unknown";
            _useAsyncWrite = true;
            _bufferSize = 100;
            _flushIntervalSeconds = 0.1f;
            _maxConcurrentAsyncOperations = 4;
            _channels.Clear();
            _listenToAllChannels = true;
            _enablePerformanceMetrics = true;
            _frameBudgetThresholdMs = 0.5f;
            _errorRateThreshold = 0.1f;
            _alertSuppressionIntervalMinutes = 5;
            _healthCheckIntervalSeconds = 30;
            _enableUnityProfilerIntegration = true;
            _includeStackTrace = false;
            _includeCorrelationId = true;
        }

        /// <summary>
        /// Performs editor-specific validation for targets.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values to valid ranges
            _bufferSize = Mathf.Max(1, _bufferSize);
            _flushIntervalSeconds = Mathf.Max(0.001f, _flushIntervalSeconds);
            _maxConcurrentAsyncOperations = Mathf.Max(1, _maxConcurrentAsyncOperations);
            _frameBudgetThresholdMs = Mathf.Max(0.1f, _frameBudgetThresholdMs);
            _errorRateThreshold = Mathf.Clamp01(_errorRateThreshold);
            _alertSuppressionIntervalMinutes = Mathf.Max(0, _alertSuppressionIntervalMinutes);
            _healthCheckIntervalSeconds = Mathf.Max(1, _healthCheckIntervalSeconds);

            // Validate target type
            if (string.IsNullOrWhiteSpace(_targetType))
            {
                _targetType = "Unknown";
            }
        }

        /// <summary>
        /// Adds a channel to the target's channel list.
        /// </summary>
        /// <param name="channelName">The channel name to add</param>
        [ContextMenu("Add Channel")]
        public void AddChannel(string channelName = "Default")
        {
            if (!string.IsNullOrWhiteSpace(channelName) && !_channels.Contains(channelName))
            {
                _channels.Add(channelName);
                _listenToAllChannels = false;
            }
        }

        /// <summary>
        /// Removes a channel from the target's channel list.
        /// </summary>
        /// <param name="channelName">The channel name to remove</param>
        public void RemoveChannel(string channelName)
        {
            _channels.Remove(channelName);
            
            if (_channels.Count == 0)
            {
                _listenToAllChannels = true;
            }
        }

        /// <summary>
        /// Clears all channels and sets to listen to all channels.
        /// </summary>
        [ContextMenu("Clear Channels")]
        public void ClearChannels()
        {
            _channels.Clear();
            _listenToAllChannels = true;
        }
    }
}