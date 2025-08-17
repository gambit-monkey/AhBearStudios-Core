using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Defines the contract for alert suppression and deduplication services.
    /// Manages time-based suppression, rate limiting, and duplicate alert consolidation.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public interface IAlertSuppressionService : IDisposable
    {
        /// <summary>
        /// Gets whether the suppression service is enabled and operational.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets the current suppression statistics.
        /// </summary>
        AlertSuppressionStatistics Statistics { get; }

        /// <summary>
        /// Processes an alert through the suppression pipeline.
        /// Returns null if alert should be suppressed, otherwise returns the processed alert.
        /// </summary>
        /// <param name="alert">Alert to process</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Processed alert or null if suppressed</returns>
        Alert ProcessAlert(Alert alert, Guid correlationId = default);

        /// <summary>
        /// Processes multiple alerts through the suppression pipeline.
        /// </summary>
        /// <param name="alerts">Alerts to process</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Collection of non-suppressed alerts</returns>
        IEnumerable<Alert> ProcessAlerts(IEnumerable<Alert> alerts, Guid correlationId = default);

        /// <summary>
        /// Adds a time-based suppression rule for specific alert patterns.
        /// </summary>
        /// <param name="sourcePattern">Source pattern to match (supports wildcards)</param>
        /// <param name="messagePattern">Message pattern to match (supports wildcards)</param>
        /// <param name="suppressionDuration">How long to suppress matching alerts</param>
        /// <param name="severity">Optional severity filter</param>
        void AddSuppressionRule(string sourcePattern, string messagePattern, 
            TimeSpan suppressionDuration, AlertSeverity? severity = null);

        /// <summary>
        /// Adds a rate limiting rule for specific alert sources.
        /// </summary>
        /// <param name="sourcePattern">Source pattern to match</param>
        /// <param name="maxAlertsPerMinute">Maximum alerts allowed per minute</param>
        /// <param name="severity">Optional severity filter</param>
        void AddRateLimitRule(string sourcePattern, int maxAlertsPerMinute, AlertSeverity? severity = null);

        /// <summary>
        /// Removes a suppression rule.
        /// </summary>
        /// <param name="sourcePattern">Source pattern</param>
        /// <param name="messagePattern">Message pattern</param>
        /// <param name="severity">Optional severity filter</param>
        /// <returns>True if rule was removed</returns>
        bool RemoveSuppressionRule(string sourcePattern, string messagePattern, AlertSeverity? severity = null);

        /// <summary>
        /// Removes a rate limit rule.
        /// </summary>
        /// <param name="sourcePattern">Source pattern</param>
        /// <param name="severity">Optional severity filter</param>
        /// <returns>True if rule was removed</returns>
        bool RemoveRateLimitRule(string sourcePattern, AlertSeverity? severity = null);

        /// <summary>
        /// Clears all suppression and rate limit rules.
        /// </summary>
        void ClearAllRules();

        /// <summary>
        /// Gets information about currently suppressed alerts.
        /// </summary>
        /// <returns>Collection of suppression information</returns>
        IEnumerable<SuppressionInfo> GetSuppressedAlerts();

        /// <summary>
        /// Resets suppression statistics.
        /// </summary>
        void ResetStatistics();

        /// <summary>
        /// Enables the suppression service.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables the suppression service (all alerts will pass through).
        /// </summary>
        void Disable();
    }
}