using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Profiling.Configs;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Profiling.Builders
{
    /// <summary>
    /// Fluent API builder for creating ProfilerConfig instances with comprehensive validation and defaults.
    /// Follows the Builder → Config → Factory → Service pattern established in CLAUDE.md for consistent architecture.
    /// </summary>
    /// <remarks>
    /// The ProfilerConfigBuilder handles configuration complexity through a fluent API that:
    /// - Provides sensible defaults for Unity game development (60 FPS targeting)
    /// - Validates configuration consistency and performance implications
    /// - Supports both development and production optimization scenarios
    /// - Ensures zero-allocation patterns where possible
    /// - Integrates with Unity's frame budget constraints (16.67ms)
    /// 
    /// This builder is designed for ease of use while preventing configuration errors
    /// that could impact runtime performance or profiling accuracy.
    /// </remarks>
    public sealed class ProfilerConfigBuilder
    {
        #region Private Fields

        private Guid _id;
        private bool _isEnabled = true;
        private bool _startRecording = true;
        private float _defaultSamplingRate = 1.0f;
        private int _maxActiveScopeCount = 1000;
        private int _maxMetricSnapshots = 10000;
        private bool _enableUnityProfilerIntegration = true;
        private bool _enableThresholdMonitoring = true;
        private bool _enableCustomMetrics = true;
        private bool _enableStatistics = true;
        private double _defaultThresholdMs = 16.67; // 60 FPS frame budget
        private int _scopePoolSize = 100;
        private Guid _correlationId;
        private FixedString64Bytes _source = "ProfilerBuilder";
        private readonly Dictionary<string, double> _customThresholds = new();
        private readonly HashSet<string> _excludedTags = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilerConfigBuilder with Unity-optimized defaults.
        /// </summary>
        public ProfilerConfigBuilder()
        {
            // Initialize with deterministic ID for consistent builder behavior
            _id = DeterministicIdGenerator.GenerateCoreId("ProfilerConfigBuilder:DefaultInstance");
            _correlationId = DeterministicIdGenerator.GenerateCorrelationId("ProfilerBuilder", _id.ToString());
        }

        /// <summary>
        /// Initializes a new instance of the ProfilerConfigBuilder with a specified source system.
        /// </summary>
        /// <param name="source">Source system or component creating this configuration</param>
        /// <exception cref="ArgumentException">Thrown when source exceeds FixedString64Bytes capacity</exception>
        public ProfilerConfigBuilder(string source)
        {
            if (!string.IsNullOrEmpty(source) && Encoding.UTF8.GetByteCount(source) > 61)
                throw new ArgumentException("Source name exceeds 64 byte limit", nameof(source));

            _source = string.IsNullOrEmpty(source) ? "ProfilerBuilder" : source;
            _id = DeterministicIdGenerator.GenerateCoreId($"ProfilerConfigBuilder:{_source.ToString()}");
            _correlationId = DeterministicIdGenerator.GenerateCorrelationId("ProfilerBuilder", _id.ToString());
        }

        #endregion

        #region Basic Configuration Methods

        /// <summary>
        /// Sets whether the profiler service should be enabled at startup.
        /// </summary>
        /// <param name="enabled">True to enable the profiler, false to disable</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets whether profiling data recording should start immediately upon service creation.
        /// </summary>
        /// <param name="startRecording">True to start recording immediately, false to start paused</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetStartRecording(bool startRecording)
        {
            _startRecording = startRecording;
            return this;
        }

        /// <summary>
        /// Sets the default sampling rate for profiling operations.
        /// </summary>
        /// <param name="samplingRate">Sampling rate between 0.0 and 1.0 (0% to 100%)</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when sampling rate is outside valid range</exception>
        public ProfilerConfigBuilder SetSamplingRate(float samplingRate)
        {
            if (samplingRate < 0.0f || samplingRate > 1.0f)
                throw new ArgumentException("Sampling rate must be between 0.0 and 1.0", nameof(samplingRate));

            _defaultSamplingRate = samplingRate;
            return this;
        }

        /// <summary>
        /// Sets the source system identifier for this configuration.
        /// </summary>
        /// <param name="source">Source system or component name</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when source exceeds FixedString64Bytes capacity</exception>
        public ProfilerConfigBuilder SetSource(string source)
        {
            if (!string.IsNullOrEmpty(source) && Encoding.UTF8.GetByteCount(source) > 61)
                throw new ArgumentException("Source name exceeds 64 byte limit", nameof(source));

            _source = string.IsNullOrEmpty(source) ? "ProfilerBuilder" : source;
            return this;
        }

        /// <summary>
        /// Sets a custom correlation ID for tracking this configuration across systems.
        /// </summary>
        /// <param name="correlationId">Custom correlation ID</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetCorrelationId(Guid correlationId)
        {
            _correlationId = correlationId;
            return this;
        }

        #endregion

        #region Performance Configuration Methods

        /// <summary>
        /// Sets the maximum number of active profiling scopes allowed simultaneously.
        /// </summary>
        /// <param name="maxCount">Maximum active scope count</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative</exception>
        public ProfilerConfigBuilder SetMaxActiveScopeCount(int maxCount)
        {
            if (maxCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Max active scope count cannot be negative");

            _maxActiveScopeCount = maxCount;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of metric snapshots to retain in memory.
        /// </summary>
        /// <param name="maxSnapshots">Maximum metric snapshot count</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative</exception>
        public ProfilerConfigBuilder SetMaxMetricSnapshots(int maxSnapshots)
        {
            if (maxSnapshots < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSnapshots), "Max metric snapshots cannot be negative");

            _maxMetricSnapshots = maxSnapshots;
            return this;
        }

        /// <summary>
        /// Sets the buffer size for pooled profiler scope objects.
        /// </summary>
        /// <param name="poolSize">Scope pool buffer size</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pool size is negative</exception>
        public ProfilerConfigBuilder SetScopePoolSize(int poolSize)
        {
            if (poolSize < 0)
                throw new ArgumentOutOfRangeException(nameof(poolSize), "Scope pool size cannot be negative");

            _scopePoolSize = poolSize;
            return this;
        }

        #endregion

        #region Feature Toggle Methods

        /// <summary>
        /// Enables or disables Unity ProfilerMarker integration.
        /// </summary>
        /// <param name="enabled">True to enable Unity integration, false to disable</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetUnityProfilerIntegration(bool enabled)
        {
            _enableUnityProfilerIntegration = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables performance threshold monitoring and events.
        /// </summary>
        /// <param name="enabled">True to enable threshold monitoring, false to disable</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetThresholdMonitoring(bool enabled)
        {
            _enableThresholdMonitoring = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables custom metric recording capabilities.
        /// </summary>
        /// <param name="enabled">True to enable custom metrics, false to disable</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetCustomMetrics(bool enabled)
        {
            _enableCustomMetrics = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables statistical analysis of performance data.
        /// </summary>
        /// <param name="enabled">True to enable statistics, false to disable</param>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder SetStatistics(bool enabled)
        {
            _enableStatistics = enabled;
            return this;
        }

        #endregion

        #region Threshold Configuration Methods

        /// <summary>
        /// Sets the default performance threshold for all profiling operations.
        /// </summary>
        /// <param name="thresholdMs">Default threshold in milliseconds</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative</exception>
        public ProfilerConfigBuilder SetDefaultThreshold(double thresholdMs)
        {
            if (thresholdMs < 0.0)
                throw new ArgumentOutOfRangeException(nameof(thresholdMs), "Threshold cannot be negative");

            _defaultThresholdMs = thresholdMs;
            return this;
        }

        /// <summary>
        /// Adds a custom performance threshold for a specific profiler tag.
        /// </summary>
        /// <param name="tagName">Profiler tag name</param>
        /// <param name="thresholdMs">Custom threshold in milliseconds</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when tag name is null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is negative</exception>
        public ProfilerConfigBuilder AddCustomThreshold(string tagName, double thresholdMs)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            if (thresholdMs < 0.0)
                throw new ArgumentOutOfRangeException(nameof(thresholdMs), "Threshold cannot be negative");

            _customThresholds[tagName] = thresholdMs;
            return this;
        }

        /// <summary>
        /// Adds multiple custom performance thresholds from a dictionary.
        /// </summary>
        /// <param name="thresholds">Dictionary of tag names to threshold values</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when thresholds dictionary is null</exception>
        /// <exception cref="ArgumentException">Thrown when any threshold is invalid</exception>
        public ProfilerConfigBuilder AddCustomThresholds(IReadOnlyDictionary<string, double> thresholds)
        {
            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));

            foreach (var kvp in thresholds)
            {
                AddCustomThreshold(kvp.Key, kvp.Value);
            }

            return this;
        }

        /// <summary>
        /// Removes a custom threshold for a specific profiler tag.
        /// </summary>
        /// <param name="tagName">Profiler tag name to remove</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when tag name is null or empty</exception>
        public ProfilerConfigBuilder RemoveCustomThreshold(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            _customThresholds.Remove(tagName);
            return this;
        }

        /// <summary>
        /// Clears all custom thresholds, reverting to default threshold for all tags.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder ClearCustomThresholds()
        {
            _customThresholds.Clear();
            return this;
        }

        #endregion

        #region Tag Exclusion Methods

        /// <summary>
        /// Adds a profiler tag to the exclusion list, preventing its monitoring.
        /// </summary>
        /// <param name="tagName">Profiler tag name to exclude</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when tag name is null or empty</exception>
        public ProfilerConfigBuilder AddExcludedTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            _excludedTags.Add(tagName);
            return this;
        }

        /// <summary>
        /// Adds multiple profiler tags to the exclusion list.
        /// </summary>
        /// <param name="tagNames">Collection of tag names to exclude</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when tag names collection is null</exception>
        public ProfilerConfigBuilder AddExcludedTags(IEnumerable<string> tagNames)
        {
            if (tagNames == null)
                throw new ArgumentNullException(nameof(tagNames));

            foreach (var tagName in tagNames)
            {
                if (!string.IsNullOrEmpty(tagName))
                    _excludedTags.Add(tagName);
            }

            return this;
        }

        /// <summary>
        /// Removes a profiler tag from the exclusion list, enabling its monitoring.
        /// </summary>
        /// <param name="tagName">Profiler tag name to include</param>
        /// <returns>This builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when tag name is null or empty</exception>
        public ProfilerConfigBuilder RemoveExcludedTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            _excludedTags.Remove(tagName);
            return this;
        }

        /// <summary>
        /// Clears the tag exclusion list, enabling monitoring for all tags.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder ClearExcludedTags()
        {
            _excludedTags.Clear();
            return this;
        }

        #endregion

        #region Preset Configuration Methods

        /// <summary>
        /// Configures the builder with settings optimized for Unity development.
        /// Enables all features with strict performance monitoring for 60+ FPS.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder UseUnityDevelopmentPreset()
        {
            _isEnabled = true;
            _startRecording = true;
            _defaultSamplingRate = 1.0f;
            _maxActiveScopeCount = 2000;
            _maxMetricSnapshots = 20000;
            _enableUnityProfilerIntegration = true;
            _enableThresholdMonitoring = true;
            _enableCustomMetrics = true;
            _enableStatistics = true;
            _defaultThresholdMs = 16.67; // 60 FPS
            _scopePoolSize = 200;

            return this;
        }

        /// <summary>
        /// Configures the builder with settings optimized for production environments.
        /// Uses conservative settings to minimize performance impact while maintaining visibility.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder UseProductionPreset()
        {
            _isEnabled = true;
            _startRecording = false; // Start paused in production
            _defaultSamplingRate = 0.1f; // 10% sampling for production
            _maxActiveScopeCount = 500;
            _maxMetricSnapshots = 5000;
            _enableUnityProfilerIntegration = false; // Disable Unity integration in production
            _enableThresholdMonitoring = true;
            _enableCustomMetrics = false; // Disable custom metrics for performance
            _enableStatistics = false; // Disable statistics for performance
            _defaultThresholdMs = 33.33; // More relaxed 30 FPS threshold
            _scopePoolSize = 50;

            return this;
        }

        /// <summary>
        /// Configures the builder with settings optimized for performance testing scenarios.
        /// Uses comprehensive monitoring with strict thresholds for detailed analysis.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder UsePerformanceTestingPreset()
        {
            _isEnabled = true;
            _startRecording = true;
            _defaultSamplingRate = 1.0f;
            _maxActiveScopeCount = 5000;
            _maxMetricSnapshots = 50000;
            _enableUnityProfilerIntegration = true;
            _enableThresholdMonitoring = true;
            _enableCustomMetrics = true;
            _enableStatistics = true;
            _defaultThresholdMs = 8.33; // Strict 120 FPS threshold
            _scopePoolSize = 500;

            return this;
        }

        /// <summary>
        /// Configures the builder for minimal profiling overhead.
        /// Disables most features while maintaining basic Unity integration.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ProfilerConfigBuilder UseMinimalOverheadPreset()
        {
            _isEnabled = true;
            _startRecording = false;
            _defaultSamplingRate = 0.01f; // 1% sampling
            _maxActiveScopeCount = 100;
            _maxMetricSnapshots = 1000;
            _enableUnityProfilerIntegration = true; // Keep Unity integration
            _enableThresholdMonitoring = false; // Disable threshold monitoring
            _enableCustomMetrics = false;
            _enableStatistics = false;
            _defaultThresholdMs = 100.0; // Very relaxed threshold
            _scopePoolSize = 20;

            return this;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates the current configuration for consistency and performance implications.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool ValidateConfiguration()
        {
            // Basic range validations
            if (_defaultSamplingRate < 0.0f || _defaultSamplingRate > 1.0f) return false;
            if (_maxActiveScopeCount < 0) return false;
            if (_maxMetricSnapshots < 0) return false;
            if (_defaultThresholdMs < 0.0) return false;
            if (_scopePoolSize < 0) return false;

            // Validate custom thresholds
            foreach (var threshold in _customThresholds.Values)
            {
                if (threshold < 0.0) return false;
            }

            // Performance consistency checks
            if (_enableThresholdMonitoring && !_isEnabled) return false; // Can't monitor if disabled
            if (_enableStatistics && !_enableCustomMetrics) return false; // Statistics need metrics

            // Production readiness checks
            if (_defaultSamplingRate > 0.5f && _maxActiveScopeCount > 1000)
            {
                // High sampling with many scopes might impact performance
                // This is a warning condition, not a hard failure
            }

            return true;
        }

        /// <summary>
        /// Gets validation warnings for the current configuration.
        /// Useful for identifying potential performance or functionality issues.
        /// </summary>
        /// <returns>Collection of validation warning messages</returns>
        public IReadOnlyCollection<string> GetValidationWarnings()
        {
            var warnings = new List<string>();

            // Performance warnings
            if (_defaultSamplingRate > 0.5f && _maxActiveScopeCount > 1000)
                warnings.Add("High sampling rate with many active scopes may impact performance");

            if (_enableStatistics && _maxMetricSnapshots > 20000)
                warnings.Add("Statistics with many metric snapshots may consume significant memory");

            if (_enableUnityProfilerIntegration && _defaultSamplingRate < 0.1f)
                warnings.Add("Low sampling rate may reduce Unity Profiler visibility");

            // Functionality warnings
            if (!_isEnabled && _startRecording)
                warnings.Add("Cannot start recording when profiler is disabled");

            if (_enableThresholdMonitoring && _defaultThresholdMs > 100.0)
                warnings.Add("Very high threshold may miss performance issues");

            if (_scopePoolSize > 0 && _maxActiveScopeCount > _scopePoolSize * 10)
                warnings.Add("Scope pool may be too small for the maximum active scope count");

            return warnings.AsReadOnly();
        }

        #endregion

        #region Build Methods

        /// <summary>
        /// Builds a validated ProfilerConfig instance with the current settings.
        /// </summary>
        /// <returns>New ProfilerConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration validation fails</exception>
        public ProfilerConfig Build()
        {
            if (!ValidateConfiguration())
                throw new InvalidOperationException("Configuration validation failed. Use ValidateConfiguration() to check for issues.");

            return new ProfilerConfig(
                id: _id,
                isEnabled: _isEnabled,
                startRecording: _startRecording,
                defaultSamplingRate: _defaultSamplingRate,
                maxActiveScopeCount: _maxActiveScopeCount,
                maxMetricSnapshots: _maxMetricSnapshots,
                enableUnityProfilerIntegration: _enableUnityProfilerIntegration,
                enableThresholdMonitoring: _enableThresholdMonitoring,
                enableCustomMetrics: _enableCustomMetrics,
                enableStatistics: _enableStatistics,
                defaultThresholdMs: _defaultThresholdMs,
                scopePoolSize: _scopePoolSize,
                correlationId: _correlationId,
                source: _source,
                customThresholds: new Dictionary<string, double>(_customThresholds),
                excludedTags: new HashSet<string>(_excludedTags));
        }

        /// <summary>
        /// Builds a ProfilerConfig instance with validation warnings logged but not blocking.
        /// Use this method when you want to proceed despite configuration warnings.
        /// </summary>
        /// <param name="warnings">Output parameter containing any validation warnings</param>
        /// <returns>New ProfilerConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration has critical validation errors</exception>
        public ProfilerConfig BuildWithWarnings(out IReadOnlyCollection<string> warnings)
        {
            warnings = GetValidationWarnings();

            if (!ValidateConfiguration())
                throw new InvalidOperationException("Configuration has critical validation errors that prevent building.");

            return Build();
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns a string representation of this builder's current configuration.
        /// </summary>
        /// <returns>String representation of builder state</returns>
        public override string ToString()
        {
            return $"ProfilerConfigBuilder [Enabled={_isEnabled}, SamplingRate={_defaultSamplingRate:F2}, " +
                   $"ThresholdMs={_defaultThresholdMs:F2}, Source={_source}]";
        }

        #endregion
    }
}