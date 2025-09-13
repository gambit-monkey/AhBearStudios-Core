using System;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Profiling.Configs
{
    /// <summary>
    /// Configuration object for the profiler service that controls performance monitoring behavior.
    /// Designed for Unity game development with zero-allocation patterns and 60 FPS performance targets.
    /// </summary>
    /// <remarks>
    /// This configuration follows the Builder → Config → Factory → Service pattern established in CLAUDE.md.
    /// It provides immutable settings that control profiler behavior without runtime allocation overhead.
    /// The configuration supports Unity's frame budget constraints (16.67ms) and platform diversity.
    /// </remarks>
    public sealed class ProfilerConfig
    {
        #region Public Properties

        /// <summary>
        /// Gets the unique identifier for this profiler configuration instance.
        /// Used for correlation tracking and debugging across the profiling system.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets a value indicating whether the profiler service is enabled at startup.
        /// When false, the service operates in minimal overhead mode.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether profiling data recording is enabled by default.
        /// Can be controlled at runtime through the service interface.
        /// </summary>
        public bool StartRecording { get; init; }

        /// <summary>
        /// Gets the default sampling rate for profiling operations (0.0 to 1.0).
        /// Used to reduce overhead in production environments while maintaining visibility.
        /// </summary>
        public float DefaultSamplingRate { get; init; }

        /// <summary>
        /// Gets the maximum number of active profiling scopes allowed simultaneously.
        /// Prevents memory bloat in deeply nested profiling scenarios.
        /// </summary>
        public int MaxActiveScopeCount { get; init; }

        /// <summary>
        /// Gets the maximum number of metric snapshots to retain in memory.
        /// Older snapshots are automatically discarded when this limit is exceeded.
        /// </summary>
        public int MaxMetricSnapshots { get; init; }

        /// <summary>
        /// Gets a value indicating whether Unity ProfilerMarker integration is enabled.
        /// When true, all profiling scopes automatically create corresponding Unity markers.
        /// </summary>
        public bool EnableUnityProfilerIntegration { get; init; }

        /// <summary>
        /// Gets a value indicating whether threshold monitoring is enabled.
        /// When true, events are raised when performance thresholds are exceeded.
        /// </summary>
        public bool EnableThresholdMonitoring { get; init; }

        /// <summary>
        /// Gets a value indicating whether custom metric recording is enabled.
        /// When false, RecordMetric operations become no-ops for performance.
        /// </summary>
        public bool EnableCustomMetrics { get; init; }

        /// <summary>
        /// Gets a value indicating whether statistical analysis is enabled.
        /// Provides min/max/average calculations at the cost of additional processing.
        /// </summary>
        public bool EnableStatistics { get; init; }

        /// <summary>
        /// Gets the default threshold for performance monitoring in milliseconds.
        /// Operations exceeding this duration will trigger threshold exceeded events.
        /// </summary>
        public double DefaultThresholdMs { get; init; }

        /// <summary>
        /// Gets the buffer size for pooled profiler scope objects.
        /// Higher values reduce allocations but increase memory usage.
        /// </summary>
        public int ScopePoolSize { get; init; }

        /// <summary>
        /// Gets the correlation ID for this configuration instance.
        /// Used for tracking configuration lifecycle across the profiling system.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the source system or component that created this configuration.
        /// Useful for debugging configuration origins and ownership.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the collection of custom performance thresholds keyed by profiler tag names.
        /// Allows fine-grained control over performance monitoring per operation type.
        /// </summary>
        public IReadOnlyDictionary<string, double> CustomThresholds { get; init; }

        /// <summary>
        /// Gets the collection of profiler tags that should be excluded from monitoring.
        /// Useful for disabling profiling on specific operations without affecting others.
        /// </summary>
        public IReadOnlyCollection<string> ExcludedTags { get; init; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilerConfig class with specified settings.
        /// </summary>
        /// <param name="id">Unique identifier for this configuration instance</param>
        /// <param name="isEnabled">Whether the profiler service is enabled</param>
        /// <param name="startRecording">Whether to start recording immediately</param>
        /// <param name="defaultSamplingRate">Default sampling rate (0.0 to 1.0)</param>
        /// <param name="maxActiveScopeCount">Maximum number of active scopes</param>
        /// <param name="maxMetricSnapshots">Maximum number of retained metric snapshots</param>
        /// <param name="enableUnityProfilerIntegration">Whether to enable Unity ProfilerMarker integration</param>
        /// <param name="enableThresholdMonitoring">Whether to enable threshold monitoring</param>
        /// <param name="enableCustomMetrics">Whether to enable custom metric recording</param>
        /// <param name="enableStatistics">Whether to enable statistical analysis</param>
        /// <param name="defaultThresholdMs">Default performance threshold in milliseconds</param>
        /// <param name="scopePoolSize">Buffer size for scope object pooling</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <param name="source">Source system creating this configuration</param>
        /// <param name="customThresholds">Custom thresholds by tag name</param>
        /// <param name="excludedTags">Tags to exclude from monitoring</param>
        /// <exception cref="ArgumentException">Thrown when sampling rate is out of valid range</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when numeric parameters are negative</exception>
        public ProfilerConfig(
            Guid id = default,
            bool isEnabled = true,
            bool startRecording = true,
            float defaultSamplingRate = 1.0f,
            int maxActiveScopeCount = 1000,
            int maxMetricSnapshots = 10000,
            bool enableUnityProfilerIntegration = true,
            bool enableThresholdMonitoring = true,
            bool enableCustomMetrics = true,
            bool enableStatistics = true,
            double defaultThresholdMs = 16.67, // 60 FPS frame budget
            int scopePoolSize = 100,
            Guid correlationId = default,
            FixedString64Bytes source = default,
            IReadOnlyDictionary<string, double> customThresholds = null,
            IReadOnlyCollection<string> excludedTags = null)
        {
            // Validation
            if (defaultSamplingRate < 0.0f || defaultSamplingRate > 1.0f)
                throw new ArgumentException("Sampling rate must be between 0.0 and 1.0", nameof(defaultSamplingRate));
            
            if (maxActiveScopeCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxActiveScopeCount), "Max active scope count cannot be negative");
            
            if (maxMetricSnapshots < 0)
                throw new ArgumentOutOfRangeException(nameof(maxMetricSnapshots), "Max metric snapshots cannot be negative");
            
            if (defaultThresholdMs < 0.0)
                throw new ArgumentOutOfRangeException(nameof(defaultThresholdMs), "Default threshold cannot be negative");
            
            if (scopePoolSize < 0)
                throw new ArgumentOutOfRangeException(nameof(scopePoolSize), "Scope pool size cannot be negative");

            // Initialize properties
            Id = id == default ? DeterministicIdGenerator.GenerateCoreId($"ProfilerConfig:{(source.IsEmpty ? "ProfilerSystem" : source.ToString())}") : id;
            IsEnabled = isEnabled;
            StartRecording = startRecording;
            DefaultSamplingRate = defaultSamplingRate;
            MaxActiveScopeCount = maxActiveScopeCount;
            MaxMetricSnapshots = maxMetricSnapshots;
            EnableUnityProfilerIntegration = enableUnityProfilerIntegration;
            EnableThresholdMonitoring = enableThresholdMonitoring;
            EnableCustomMetrics = enableCustomMetrics;
            EnableStatistics = enableStatistics;
            DefaultThresholdMs = defaultThresholdMs;
            ScopePoolSize = scopePoolSize;
            CorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("ProfilerConfig", Id.ToString())
                : correlationId;
            Source = source.IsEmpty ? "ProfilerSystem" : source;
            CustomThresholds = customThresholds ?? new Dictionary<string, double>();
            ExcludedTags = excludedTags ?? new HashSet<string>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the performance threshold for a specific profiler tag.
        /// Returns the custom threshold if configured, otherwise returns the default threshold.
        /// </summary>
        /// <param name="tagName">The profiler tag name to get threshold for</param>
        /// <returns>Performance threshold in milliseconds</returns>
        /// <exception cref="ArgumentException">Thrown when tag name is null or empty</exception>
        public double GetThresholdForTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            return CustomThresholds.TryGetValue(tagName, out var customThreshold) 
                ? customThreshold 
                : DefaultThresholdMs;
        }

        /// <summary>
        /// Determines whether a specific profiler tag should be excluded from monitoring.
        /// </summary>
        /// <param name="tagName">The profiler tag name to check</param>
        /// <returns>True if the tag should be excluded, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when tag name is null or empty</exception>
        public bool IsTagExcluded(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            return ExcludedTags.AsValueEnumerable().Contains(tagName);
        }

        /// <summary>
        /// Creates a copy of this configuration with the specified sampling rate.
        /// Useful for runtime sampling rate adjustments without recreating the entire configuration.
        /// </summary>
        /// <param name="newSamplingRate">New sampling rate (0.0 to 1.0)</param>
        /// <returns>New ProfilerConfig instance with updated sampling rate</returns>
        /// <exception cref="ArgumentException">Thrown when sampling rate is out of valid range</exception>
        public ProfilerConfig WithSamplingRate(float newSamplingRate)
        {
            if (newSamplingRate < 0.0f || newSamplingRate > 1.0f)
                throw new ArgumentException("Sampling rate must be between 0.0 and 1.0", nameof(newSamplingRate));

            return new ProfilerConfig(
                id: Id,
                isEnabled: IsEnabled,
                startRecording: StartRecording,
                defaultSamplingRate: newSamplingRate,
                maxActiveScopeCount: MaxActiveScopeCount,
                maxMetricSnapshots: MaxMetricSnapshots,
                enableUnityProfilerIntegration: EnableUnityProfilerIntegration,
                enableThresholdMonitoring: EnableThresholdMonitoring,
                enableCustomMetrics: EnableCustomMetrics,
                enableStatistics: EnableStatistics,
                defaultThresholdMs: DefaultThresholdMs,
                scopePoolSize: ScopePoolSize,
                correlationId: CorrelationId,
                source: Source,
                customThresholds: CustomThresholds,
                excludedTags: ExcludedTags);
        }

        /// <summary>
        /// Validates that this configuration is suitable for production use.
        /// Checks for performance-friendly settings and Unity frame budget compliance.
        /// </summary>
        /// <returns>True if the configuration is production-ready, false otherwise</returns>
        public bool ValidateForProduction()
        {
            // Check for performance-friendly settings
            if (DefaultSamplingRate > 0.1f && IsEnabled) // More than 10% sampling in production may impact performance
                return false;

            if (MaxActiveScopeCount > 2000) // Excessive scope tracking can impact performance
                return false;

            if (MaxMetricSnapshots > 50000) // Excessive metric storage can impact memory
                return false;

            if (DefaultThresholdMs > 16.67) // Should be aligned with 60 FPS frame budget
                return false;

            return true;
        }

        #endregion

        #region Object Overrides

        /// <summary>
        /// Returns a string representation of this profiler configuration for debugging purposes.
        /// </summary>
        /// <returns>String representation of the configuration</returns>
        public override string ToString()
        {
            return $"ProfilerConfig [Id={Id:D}, Enabled={IsEnabled}, SamplingRate={DefaultSamplingRate:F2}, " +
                   $"ThresholdMs={DefaultThresholdMs:F2}, Source={Source}]";
        }

        /// <summary>
        /// Determines whether the specified object is equal to this profiler configuration.
        /// Equality is based on the configuration ID.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            return obj is ProfilerConfig other && Id.Equals(other.Id);
        }

        /// <summary>
        /// Returns the hash code for this profiler configuration.
        /// Based on the configuration ID for consistent hashing.
        /// </summary>
        /// <returns>Hash code for this configuration</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}