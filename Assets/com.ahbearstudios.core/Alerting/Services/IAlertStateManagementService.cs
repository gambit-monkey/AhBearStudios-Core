using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Defines the contract for managing alert state lifecycle operations.
    /// Handles active alerts, history, acknowledgment, resolution, and source severity management.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public interface IAlertStateManagementService : IDisposable
    {
        #region State Properties

        /// <summary>
        /// Gets whether the state management service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the count of currently active alerts.
        /// </summary>
        int ActiveAlertCount { get; }

        /// <summary>
        /// Gets the count of alerts in history.
        /// </summary>
        int HistoryCount { get; }

        #endregion

        #region Alert Storage and Retrieval

        /// <summary>
        /// Stores an alert in the active alerts collection.
        /// Updates existing alert if already present (increments count).
        /// </summary>
        /// <param name="alert">Alert to store</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void StoreAlert(Alert alert, Guid correlationId = default);

        /// <summary>
        /// Gets all currently active alerts.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Collection of active alerts</returns>
        IEnumerable<Alert> GetActiveAlerts(Guid correlationId = default);

        /// <summary>
        /// Gets alert history for a specified time period.
        /// </summary>
        /// <param name="period">Time period for history retrieval</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Collection of historical alerts</returns>
        IEnumerable<Alert> GetAlertHistory(TimeSpan period, Guid correlationId = default);

        /// <summary>
        /// Gets a specific alert by its ID.
        /// </summary>
        /// <param name="alertId">Alert ID to retrieve</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Alert if found, null otherwise</returns>
        Alert GetAlert(Guid alertId, Guid correlationId = default);

        #endregion

        #region Alert Lifecycle Management

        /// <summary>
        /// Acknowledges an alert by its ID.
        /// Updates the alert state and logs the acknowledgment.
        /// </summary>
        /// <param name="alertId">Alert ID to acknowledge</param>
        /// <param name="acknowledgedBy">Who acknowledged the alert (defaults to "System")</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>True if alert was acknowledged successfully</returns>
        bool AcknowledgeAlert(Guid alertId, string acknowledgedBy = "System", Guid correlationId = default);

        /// <summary>
        /// Resolves an alert by its ID.
        /// Updates the alert state and logs the resolution.
        /// </summary>
        /// <param name="alertId">Alert ID to resolve</param>
        /// <param name="resolvedBy">Who resolved the alert (defaults to "System")</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>True if alert was resolved successfully</returns>
        bool ResolveAlert(Guid alertId, string resolvedBy = "System", Guid correlationId = default);

        /// <summary>
        /// Acknowledges multiple alerts by their IDs.
        /// Batch operation for performance when handling many alerts.
        /// </summary>
        /// <param name="alertIds">Collection of alert IDs to acknowledge</param>
        /// <param name="acknowledgedBy">Who acknowledged the alerts (defaults to "System")</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Number of alerts successfully acknowledged</returns>
        int AcknowledgeAlerts(IEnumerable<Guid> alertIds, string acknowledgedBy = "System", Guid correlationId = default);

        /// <summary>
        /// Resolves multiple alerts by their IDs.
        /// Batch operation for performance when handling many alerts.
        /// </summary>
        /// <param name="alertIds">Collection of alert IDs to resolve</param>
        /// <param name="resolvedBy">Who resolved the alerts (defaults to "System")</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Number of alerts successfully resolved</returns>
        int ResolveAlerts(IEnumerable<Guid> alertIds, string resolvedBy = "System", Guid correlationId = default);

        #endregion

        #region Source Severity Management

        /// <summary>
        /// Sets the minimum severity level for a specific source.
        /// Overrides global minimum severity for the specified source.
        /// </summary>
        /// <param name="source">Source system or component</param>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void SetSourceMinimumSeverity(FixedString64Bytes source, AlertSeverity minimumSeverity, Guid correlationId = default);

        /// <summary>
        /// Gets the minimum severity level for a specific source.
        /// Returns the source-specific override if set, otherwise returns null.
        /// </summary>
        /// <param name="source">Source system or component</param>
        /// <returns>Minimum severity level if set, null if no override</returns>
        AlertSeverity? GetSourceMinimumSeverity(FixedString64Bytes source);

        /// <summary>
        /// Removes the minimum severity override for a specific source.
        /// </summary>
        /// <param name="source">Source system or component</param>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>True if override was removed, false if no override existed</returns>
        bool RemoveSourceSeverityOverride(FixedString64Bytes source, Guid correlationId = default);

        /// <summary>
        /// Gets all source severity overrides currently configured.
        /// </summary>
        /// <returns>Dictionary of source severity overrides</returns>
        IReadOnlyDictionary<FixedString64Bytes, AlertSeverity> GetAllSourceSeverityOverrides();

        #endregion

        #region Statistics and Maintenance

        /// <summary>
        /// Gets statistics about alert state management.
        /// </summary>
        /// <returns>State management statistics</returns>
        AlertStateStatistics GetStatistics();

        /// <summary>
        /// Performs maintenance operations on alert state.
        /// Cleans up old resolved alerts and trims history as needed.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void PerformMaintenance(Guid correlationId = default);

        /// <summary>
        /// Clears all alert history while preserving active alerts.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        void ClearHistory(Guid correlationId = default);

        /// <summary>
        /// Validates the integrity of alert state data.
        /// </summary>
        /// <param name="correlationId">Correlation ID for tracking (auto-generated if not provided)</param>
        /// <returns>Validation result with any issues found</returns>
        ValidationResult ValidateState(Guid correlationId = default);

        #endregion
    }
}