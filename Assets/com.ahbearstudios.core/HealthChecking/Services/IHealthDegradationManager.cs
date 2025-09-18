using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Interface for managing system degradation levels and graceful degradation patterns.
    /// Monitors health check results and automatically adjusts system capabilities.
    /// Degradation events are published via IMessageBusService following CLAUDE.md patterns:
    /// - HealthCheckDegradationChangeMessage for degradation level changes
    /// - HealthCheckFeatureToggledMessage for feature enable/disable events
    /// </summary>
    public interface IHealthDegradationManager : IDisposable
    {

        /// <summary>
        /// Gets the current system degradation level.
        /// </summary>
        DegradationLevel CurrentLevel { get; }

        /// <summary>
        /// Gets the timestamp when the current degradation level was set.
        /// </summary>
        DateTime LevelSetTime { get; }

        /// <summary>
        /// Gets the duration the system has been at the current degradation level.
        /// </summary>
        TimeSpan TimeInCurrentLevel { get; }

        /// <summary>
        /// Gets the reason for the current degradation level.
        /// </summary>
        string CurrentLevelReason { get; }

        /// <summary>
        /// Gets whether automatic degradation is enabled.
        /// </summary>
        bool IsAutomaticDegradationEnabled { get; }

        /// <summary>
        /// Evaluates health check results and updates degradation level if necessary.
        /// </summary>
        /// <param name="healthReport">Latest health report to evaluate</param>
        /// <param name="reason">Optional reason for the evaluation</param>
        /// <returns>True if degradation level changed, false otherwise</returns>
        bool EvaluateAndUpdateDegradationLevel(HealthReport healthReport, string reason = null);

        /// <summary>
        /// Manually sets the system degradation level.
        /// </summary>
        /// <param name="level">Degradation level to set</param>
        /// <param name="reason">Reason for the manual change</param>
        /// <returns>True if level was changed, false if already at that level</returns>
        bool SetDegradationLevel(DegradationLevel level, string reason);

        /// <summary>
        /// Registers a feature that should be managed based on degradation level.
        /// </summary>
        /// <param name="featureName">Name of the feature</param>
        /// <param name="minimumLevel">Minimum degradation level at which feature should be disabled</param>
        /// <param name="isEssential">Whether this is an essential feature that should only be disabled at severe levels</param>
        void RegisterManagedFeature(string featureName, DegradationLevel minimumLevel, bool isEssential = false);

        /// <summary>
        /// Unregisters a managed feature.
        /// </summary>
        /// <param name="featureName">Name of the feature to unregister</param>
        /// <returns>True if feature was found and removed</returns>
        bool UnregisterManagedFeature(string featureName);

        /// <summary>
        /// Checks if a specific feature is currently enabled based on degradation level.
        /// </summary>
        /// <param name="featureName">Name of the feature to check</param>
        /// <returns>True if feature is enabled, false if disabled or not found</returns>
        bool IsFeatureEnabled(string featureName);

        /// <summary>
        /// Gets all currently enabled features.
        /// </summary>
        /// <returns>Collection of enabled feature names</returns>
        IEnumerable<string> GetEnabledFeatures();

        /// <summary>
        /// Gets all currently disabled features.
        /// </summary>
        /// <returns>Collection of disabled feature names</returns>
        IEnumerable<string> GetDisabledFeatures();

        /// <summary>
        /// Forces a re-evaluation of all managed features based on current degradation level.
        /// </summary>
        /// <param name="reason">Reason for the re-evaluation</param>
        void RefreshFeatureStates(string reason = null);

        /// <summary>
        /// Gets degradation history for analysis.
        /// </summary>
        /// <param name="period">Time period to retrieve history for</param>
        /// <returns>Collection of degradation level changes</returns>
        IEnumerable<DegradationLevelChange> GetDegradationHistory(TimeSpan period);

        /// <summary>
        /// Gets statistics about degradation patterns.
        /// </summary>
        /// <returns>Degradation statistics</returns>
        DegradationStatistics GetStatistics();

        /// <summary>
        /// Enables or disables automatic degradation management.
        /// </summary>
        /// <param name="enabled">Whether to enable automatic degradation</param>
        /// <param name="reason">Reason for the change</param>
        void SetAutomaticDegradation(bool enabled, string reason = null);
    }

    /// <summary>
    /// Event arguments for degradation level changes.
    /// </summary>
    public sealed record DegradationLevelChangedEventArgs
    {
        /// <summary>
        /// Gets the previous degradation level.
        /// </summary>
        public DegradationLevel OldLevel { get; init; }

        /// <summary>
        /// Gets the new degradation level.
        /// </summary>
        public DegradationLevel NewLevel { get; init; }

        /// <summary>
        /// Gets the reason for the level change.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets the timestamp of the change.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets whether this was an automatic change.
        /// </summary>
        public bool IsAutomatic { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// Gets the health metrics that triggered the change.
        /// </summary>
        public DegradationTriggerMetrics TriggerMetrics { get; init; }
    }

    /// <summary>
    /// Event arguments for feature toggle events.
    /// </summary>
    public sealed record FeatureToggleEventArgs
    {
        /// <summary>
        /// Gets the name of the feature.
        /// </summary>
        public string FeatureName { get; init; }

        /// <summary>
        /// Gets whether the feature was enabled or disabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the degradation level that triggered this change.
        /// </summary>
        public DegradationLevel TriggeringLevel { get; init; }

        /// <summary>
        /// Gets the reason for the feature toggle.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets the timestamp of the toggle.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a historical degradation level change.
    /// </summary>
    public sealed record DegradationLevelChange
    {
        /// <summary>
        /// Gets the timestamp of the change.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the previous level.
        /// </summary>
        public DegradationLevel FromLevel { get; init; }

        /// <summary>
        /// Gets the new level.
        /// </summary>
        public DegradationLevel ToLevel { get; init; }

        /// <summary>
        /// Gets the reason for the change.
        /// </summary>
        public string Reason { get; init; }

        /// <summary>
        /// Gets whether this was an automatic change.
        /// </summary>
        public bool IsAutomatic { get; init; }

        /// <summary>
        /// Gets the duration at the previous level.
        /// </summary>
        public TimeSpan DurationAtPreviousLevel { get; init; }

        /// <summary>
        /// Gets the trigger metrics.
        /// </summary>
        public DegradationTriggerMetrics TriggerMetrics { get; init; }
    }

    /// <summary>
    /// Metrics that triggered a degradation level change.
    /// </summary>
    public sealed record DegradationTriggerMetrics
    {
        /// <summary>
        /// Gets the total number of health checks evaluated.
        /// </summary>
        public int TotalHealthChecks { get; init; }

        /// <summary>
        /// Gets the number of failed health checks.
        /// </summary>
        public int FailedHealthChecks { get; init; }

        /// <summary>
        /// Gets the failure rate (0.0 to 1.0).
        /// </summary>
        public double FailureRate { get; init; }

        /// <summary>
        /// Gets the number of critical health check failures.
        /// </summary>
        public int CriticalFailures { get; init; }

        /// <summary>
        /// Gets the threshold that was exceeded.
        /// </summary>
        public double ExceededThreshold { get; init; }

        /// <summary>
        /// Gets the evaluation window used.
        /// </summary>
        public TimeSpan EvaluationWindow { get; init; }
    }

    /// <summary>
    /// Statistics about degradation patterns and behavior.
    /// </summary>
    public sealed record DegradationStatistics
    {
        /// <summary>
        /// Gets the current degradation level.
        /// </summary>
        public DegradationLevel CurrentLevel { get; init; }

        /// <summary>
        /// Gets the time spent at the current level.
        /// </summary>
        public TimeSpan TimeAtCurrentLevel { get; init; }

        /// <summary>
        /// Gets the total number of level changes.
        /// </summary>
        public int TotalLevelChanges { get; init; }

        /// <summary>
        /// Gets the total number of automatic changes.
        /// </summary>
        public int AutomaticChanges { get; init; }

        /// <summary>
        /// Gets the total number of manual changes.
        /// </summary>
        public int ManualChanges { get; init; }

        /// <summary>
        /// Gets the total number of managed features.
        /// </summary>
        public int ManagedFeatures { get; init; }

        /// <summary>
        /// Gets the number of currently enabled features.
        /// </summary>
        public int EnabledFeatures { get; init; }

        /// <summary>
        /// Gets the number of currently disabled features.
        /// </summary>
        public int DisabledFeatures { get; init; }

        /// <summary>
        /// Gets the time distribution across degradation levels.
        /// </summary>
        public IReadOnlyDictionary<DegradationLevel, TimeSpan> TimeDistribution { get; init; }

        /// <summary>
        /// Gets whether automatic degradation is enabled.
        /// </summary>
        public bool AutomaticDegradationEnabled { get; init; }

        /// <summary>
        /// Gets the last evaluation timestamp.
        /// </summary>
        public DateTime LastEvaluation { get; init; }

        /// <summary>
        /// Gets the reason for the current level.
        /// </summary>
        public string CurrentLevelReason { get; init; }
    }
}