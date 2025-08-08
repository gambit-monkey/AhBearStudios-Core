using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Alerting.Services
{
    /// <summary>
    /// Service responsible for alert suppression and deduplication logic.
    /// Manages time-based suppression, rate limiting, and duplicate alert consolidation.
    /// Designed for Unity game development with zero-allocation patterns and high performance.
    /// </summary>
    public sealed class AlertSuppressionService : IDisposable
    {
        private readonly object _syncLock = new object();
        private readonly Dictionary<string, SuppressionEntry> _suppressedAlerts = new Dictionary<string, SuppressionEntry>();
        private readonly Dictionary<string, RateLimitEntry> _rateLimits = new Dictionary<string, RateLimitEntry>();
        private readonly Dictionary<string, DuplicateEntry> _duplicateTracker = new Dictionary<string, DuplicateEntry>();
        private readonly ILoggingService _loggingService;
        
        private volatile bool _isEnabled = true;
        private volatile bool _isDisposed;
        private DateTime _lastCleanup = DateTime.UtcNow;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);
        private readonly Timer _maintenanceTimer;

        // Default configuration
        private TimeSpan _defaultSuppressionDuration = TimeSpan.FromMinutes(5);
        private int _defaultRateLimit = 10; // alerts per minute
        private TimeSpan _duplicateWindow = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets whether the suppression service is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled && !_isDisposed;

        /// <summary>
        /// Gets the current suppression statistics.
        /// </summary>
        public AlertSuppressionStatistics Statistics { get; private set; } = AlertSuppressionStatistics.Empty;

        /// <summary>
        /// Initializes a new instance of the AlertSuppressionService class.
        /// </summary>
        /// <param name="loggingService">Optional logging service for internal logging</param>
        public AlertSuppressionService(ILoggingService loggingService = null)
        {
            _loggingService = loggingService;
            
            // Set up maintenance timer to run every minute
            _maintenanceTimer = new Timer(PerformMaintenance, null, _cleanupInterval, _cleanupInterval);
            
            LogInfo("Alert suppression service initialized");
        }

        /// <summary>
        /// Processes an alert through the suppression pipeline.
        /// Returns null if alert should be suppressed, otherwise returns the processed alert.
        /// </summary>
        /// <param name="alert">Alert to process</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Processed alert or null if suppressed</returns>
        public Alert ProcessAlert(Alert alert, Guid correlationId = default)
        {
            if (!IsEnabled || alert == null)
                return alert;

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check for duplicate suppression
                var duplicateResult = CheckDuplicateSuppressionForAlert(alert);
                if (duplicateResult.IsSuppressed)
                {
                    UpdateStatistics(true, SuppressionReason.Duplicate, startTime);
                    LogDebug($"Alert suppressed (duplicate): {alert.Id}", correlationId);
                    return duplicateResult.ConsolidatedAlert;
                }

                // Check for time-based suppression
                if (IsAlertSuppressed(alert))
                {
                    UpdateStatistics(true, SuppressionReason.TimeBased, startTime);
                    LogDebug($"Alert suppressed (time-based): {alert.Id}", correlationId);
                    return null;
                }

                // Check rate limiting
                if (IsRateLimited(alert))
                {
                    UpdateStatistics(true, SuppressionReason.RateLimit, startTime);
                    LogDebug($"Alert suppressed (rate limit): {alert.Id}", correlationId);
                    return null;
                }

                // Alert passes all suppression checks
                UpdateStatistics(false, SuppressionReason.None, startTime);
                return alert;
            }
            catch (Exception ex)
            {
                LogError($"Error in alert suppression processing: {ex.Message}", correlationId);
                // Return original alert on error to avoid blocking
                return alert;
            }
        }

        /// <summary>
        /// Processes multiple alerts through the suppression pipeline.
        /// </summary>
        /// <param name="alerts">Alerts to process</param>
        /// <param name="correlationId">Correlation ID for tracking</param>
        /// <returns>Collection of non-suppressed alerts</returns>
        public IEnumerable<Alert> ProcessAlerts(IEnumerable<Alert> alerts, Guid correlationId = default)
        {
            if (alerts == null)
                return Array.Empty<Alert>();

            var results = new List<Alert>();
            foreach (var alert in alerts)
            {
                var processed = ProcessAlert(alert, correlationId);
                if (processed != null)
                    results.Add(processed);
            }
            
            return results;
        }

        /// <summary>
        /// Adds a time-based suppression rule for specific alert patterns.
        /// </summary>
        /// <param name="sourcePattern">Source pattern to match (supports wildcards)</param>
        /// <param name="messagePattern">Message pattern to match (supports wildcards)</param>
        /// <param name="suppressionDuration">How long to suppress matching alerts</param>
        /// <param name="severity">Optional severity filter</param>
        public void AddSuppressionRule(string sourcePattern, string messagePattern, 
            TimeSpan suppressionDuration, AlertSeverity? severity = null)
        {
            if (string.IsNullOrEmpty(sourcePattern) && string.IsNullOrEmpty(messagePattern))
                return;

            var key = CreateSuppressionKey(sourcePattern, messagePattern, severity);
            
            lock (_syncLock)
            {
                _suppressedAlerts[key] = new SuppressionEntry
                {
                    SourcePattern = sourcePattern ?? "*",
                    MessagePattern = messagePattern ?? "*",
                    Severity = severity,
                    SuppressionDuration = suppressionDuration,
                    LastTriggered = DateTime.MinValue,
                    TriggerCount = 0
                };
            }

            LogInfo($"Suppression rule added: {sourcePattern} / {messagePattern} for {suppressionDuration}");
        }

        /// <summary>
        /// Adds a rate limiting rule for specific alert sources.
        /// </summary>
        /// <param name="sourcePattern">Source pattern to match</param>
        /// <param name="maxAlertsPerMinute">Maximum alerts allowed per minute</param>
        /// <param name="severity">Optional severity filter</param>
        public void AddRateLimitRule(string sourcePattern, int maxAlertsPerMinute, AlertSeverity? severity = null)
        {
            if (string.IsNullOrEmpty(sourcePattern) || maxAlertsPerMinute <= 0)
                return;

            var key = CreateRateLimitKey(sourcePattern, severity);
            
            lock (_syncLock)
            {
                _rateLimits[key] = new RateLimitEntry
                {
                    SourcePattern = sourcePattern,
                    Severity = severity,
                    MaxAlertsPerMinute = maxAlertsPerMinute,
                    WindowStart = DateTime.UtcNow,
                    AlertCount = 0
                };
            }

            LogInfo($"Rate limit rule added: {sourcePattern} limited to {maxAlertsPerMinute}/min");
        }

        /// <summary>
        /// Removes a suppression rule.
        /// </summary>
        /// <param name="sourcePattern">Source pattern</param>
        /// <param name="messagePattern">Message pattern</param>
        /// <param name="severity">Optional severity filter</param>
        /// <returns>True if rule was removed</returns>
        public bool RemoveSuppressionRule(string sourcePattern, string messagePattern, AlertSeverity? severity = null)
        {
            var key = CreateSuppressionKey(sourcePattern, messagePattern, severity);
            
            lock (_syncLock)
            {
                return _suppressedAlerts.Remove(key);
            }
        }

        /// <summary>
        /// Removes a rate limit rule.
        /// </summary>
        /// <param name="sourcePattern">Source pattern</param>
        /// <param name="severity">Optional severity filter</param>
        /// <returns>True if rule was removed</returns>
        public bool RemoveRateLimitRule(string sourcePattern, AlertSeverity? severity = null)
        {
            var key = CreateRateLimitKey(sourcePattern, severity);
            
            lock (_syncLock)
            {
                return _rateLimits.Remove(key);
            }
        }

        /// <summary>
        /// Clears all suppression and rate limit rules.
        /// </summary>
        public void ClearAllRules()
        {
            lock (_syncLock)
            {
                _suppressedAlerts.Clear();
                _rateLimits.Clear();
                _duplicateTracker.Clear();
            }
            
            LogInfo("All suppression rules cleared");
        }

        /// <summary>
        /// Gets information about currently suppressed alerts.
        /// </summary>
        /// <returns>Collection of suppression information</returns>
        public IEnumerable<SuppressionInfo> GetSuppressedAlerts()
        {
            lock (_syncLock)
            {
                return _suppressedAlerts.AsValueEnumerable()
                    .Select(kvp => new SuppressionInfo
                    {
                        Key = kvp.Key,
                        SourcePattern = kvp.Value.SourcePattern,
                        MessagePattern = kvp.Value.MessagePattern,
                        Severity = kvp.Value.Severity,
                        LastTriggered = kvp.Value.LastTriggered,
                        TriggerCount = kvp.Value.TriggerCount,
                        TimeRemaining = kvp.Value.LastTriggered == DateTime.MinValue 
                            ? TimeSpan.Zero 
                            : kvp.Value.SuppressionDuration - (DateTime.UtcNow - kvp.Value.LastTriggered)
                    })
                    .Where(info => info.TimeRemaining > TimeSpan.Zero || info.LastTriggered == DateTime.MinValue)
                    .ToList();
            }
        }

        /// <summary>
        /// Resets suppression statistics.
        /// </summary>
        public void ResetStatistics()
        {
            Statistics = AlertSuppressionStatistics.Empty;
            LogInfo("Suppression statistics reset");
        }

        /// <summary>
        /// Enables the suppression service.
        /// </summary>
        public void Enable()
        {
            _isEnabled = true;
            LogInfo("Alert suppression service enabled");
        }

        /// <summary>
        /// Disables the suppression service (all alerts will pass through).
        /// </summary>
        public void Disable()
        {
            _isEnabled = false;
            LogInfo("Alert suppression service disabled");
        }

        #region Private Methods

        private DuplicateSuppressionResult CheckDuplicateSuppressionForAlert(Alert alert)
        {
            var duplicateKey = CreateDuplicateKey(alert);
            var now = DateTime.UtcNow;
            
            lock (_syncLock)
            {
                if (_duplicateTracker.TryGetValue(duplicateKey, out var entry))
                {
                    // Check if within duplicate window
                    if (now - entry.FirstSeen <= _duplicateWindow)
                    {
                        // Update duplicate entry
                        entry.LastSeen = now;
                        entry.Count++;
                        
                        // Return consolidated alert
                        var consolidatedAlert = alert.IncrementCount();
                        for (int i = 1; i < entry.Count; i++)
                        {
                            consolidatedAlert = consolidatedAlert.IncrementCount();
                        }
                        
                        return new DuplicateSuppressionResult
                        {
                            IsSuppressed = true,
                            ConsolidatedAlert = consolidatedAlert
                        };
                    }
                    else
                    {
                        // Outside window, reset entry
                        entry.FirstSeen = now;
                        entry.LastSeen = now;
                        entry.Count = 1;
                    }
                }
                else
                {
                    // First occurrence
                    _duplicateTracker[duplicateKey] = new DuplicateEntry
                    {
                        FirstSeen = now,
                        LastSeen = now,
                        Count = 1,
                        AlertKey = duplicateKey
                    };
                }
            }

            return new DuplicateSuppressionResult
            {
                IsSuppressed = false,
                ConsolidatedAlert = alert
            };
        }

        private bool IsAlertSuppressed(Alert alert)
        {
            var now = DateTime.UtcNow;
            
            lock (_syncLock)
            {
                foreach (var kvp in _suppressedAlerts)
                {
                    var entry = kvp.Value;
                    
                    // Check if patterns match
                    if (!MatchesPattern(alert.Source.ToString(), entry.SourcePattern) ||
                        !MatchesPattern(alert.Message.ToString(), entry.MessagePattern))
                        continue;
                    
                    // Check severity filter
                    if (entry.Severity.HasValue && alert.Severity != entry.Severity.Value)
                        continue;
                    
                    // Check if currently suppressed
                    if (entry.LastTriggered != DateTime.MinValue && 
                        now - entry.LastTriggered < entry.SuppressionDuration)
                    {
                        entry.TriggerCount++;
                        return true;
                    }
                    
                    // Start new suppression period
                    entry.LastTriggered = now;
                    entry.TriggerCount = 1;
                    return true;
                }
            }
            
            return false;
        }

        private bool IsRateLimited(Alert alert)
        {
            var now = DateTime.UtcNow;
            
            lock (_syncLock)
            {
                foreach (var kvp in _rateLimits)
                {
                    var entry = kvp.Value;
                    
                    // Check if source pattern matches
                    if (!MatchesPattern(alert.Source.ToString(), entry.SourcePattern))
                        continue;
                    
                    // Check severity filter
                    if (entry.Severity.HasValue && alert.Severity != entry.Severity.Value)
                        continue;
                    
                    // Check if we need to reset the window
                    if (now - entry.WindowStart >= TimeSpan.FromMinutes(1))
                    {
                        entry.WindowStart = now;
                        entry.AlertCount = 0;
                    }
                    
                    // Check if rate limit exceeded
                    if (entry.AlertCount >= entry.MaxAlertsPerMinute)
                    {
                        return true;
                    }
                    
                    // Increment counter
                    entry.AlertCount++;
                }
            }
            
            return false;
        }

        private static bool MatchesPattern(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
                return true;

            if (!pattern.Contains("*"))
                return string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase);

            var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return true;

            var currentIndex = 0;
            foreach (var part in parts)
            {
                var index = text.IndexOf(part, currentIndex, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return false;
                currentIndex = index + part.Length;
            }

            return true;
        }

        private static string CreateSuppressionKey(string sourcePattern, string messagePattern, AlertSeverity? severity)
        {
            return $"{sourcePattern ?? "*"}|{messagePattern ?? "*"}|{severity?.ToString() ?? "*"}";
        }

        private static string CreateRateLimitKey(string sourcePattern, AlertSeverity? severity)
        {
            return $"{sourcePattern}|{severity?.ToString() ?? "*"}";
        }

        private static string CreateDuplicateKey(Alert alert)
        {
            return $"{alert.Source}|{alert.Severity}|{alert.Message.GetHashCode():X8}";
        }

        private void UpdateStatistics(bool wasSuppressed, SuppressionReason reason, DateTime startTime)
        {
            var duration = DateTime.UtcNow - startTime;
            var stats = Statistics;
            
            Statistics = new AlertSuppressionStatistics
            {
                TotalAlertsProcessed = stats.TotalAlertsProcessed + 1,
                TotalAlertsSuppressed = wasSuppressed ? stats.TotalAlertsSuppressed + 1 : stats.TotalAlertsSuppressed,
                DuplicatesSuppressed = reason == SuppressionReason.Duplicate ? stats.DuplicatesSuppressed + 1 : stats.DuplicatesSuppressed,
                RateLimitSuppressed = reason == SuppressionReason.RateLimit ? stats.RateLimitSuppressed + 1 : stats.RateLimitSuppressed,
                TimeBasedSuppressed = reason == SuppressionReason.TimeBased ? stats.TimeBasedSuppressed + 1 : stats.TimeBasedSuppressed,
                AverageProcessingTimeMs = (stats.AverageProcessingTimeMs * stats.TotalAlertsProcessed + duration.TotalMilliseconds) / (stats.TotalAlertsProcessed + 1),
                LastUpdated = DateTime.UtcNow
            };
        }

        private void PerformMaintenance(object state)
        {
            if (_isDisposed || DateTime.UtcNow - _lastCleanup < _cleanupInterval)
                return;

            _lastCleanup = DateTime.UtcNow;
            
            try
            {
                var now = DateTime.UtcNow;
                var removedCount = 0;
                
                lock (_syncLock)
                {
                    // Clean up expired duplicate entries
                    var expiredDuplicates = _duplicateTracker.AsValueEnumerable()
                        .Where(kvp => now - kvp.Value.LastSeen > _duplicateWindow)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    
                    foreach (var key in expiredDuplicates)
                    {
                        _duplicateTracker.Remove(key);
                        removedCount++;
                    }
                }
                
                if (removedCount > 0)
                {
                    LogDebug($"Suppression maintenance: {removedCount} expired entries cleaned up");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during suppression maintenance: {ex.Message}");
            }
        }

        private void LogDebug(string message, Guid correlationId = default)
        {
            _loggingService?.LogDebug($"[AlertSuppressionService] {message}", correlationId.ToString(), "AlertSuppressionService");
        }

        private void LogInfo(string message, Guid correlationId = default)
        {
            _loggingService?.LogInfo($"[AlertSuppressionService] {message}", correlationId.ToString(), "AlertSuppressionService");
        }

        private void LogError(string message, Guid correlationId = default)
        {
            _loggingService?.LogError($"[AlertSuppressionService] {message}", correlationId.ToString(), "AlertSuppressionService");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes of the suppression service resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isEnabled = false;
            _isDisposed = true;

            _maintenanceTimer?.Dispose();

            lock (_syncLock)
            {
                _suppressedAlerts.Clear();
                _rateLimits.Clear();
                _duplicateTracker.Clear();
            }

            LogInfo("Alert suppression service disposed");
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Internal entry for tracking suppressed alerts.
    /// </summary>
    internal sealed class SuppressionEntry
    {
        public string SourcePattern { get; set; }
        public string MessagePattern { get; set; }
        public AlertSeverity? Severity { get; set; }
        public TimeSpan SuppressionDuration { get; set; }
        public DateTime LastTriggered { get; set; }
        public int TriggerCount { get; set; }
    }

    /// <summary>
    /// Internal entry for tracking rate limits.
    /// </summary>
    internal sealed class RateLimitEntry
    {
        public string SourcePattern { get; set; }
        public AlertSeverity? Severity { get; set; }
        public int MaxAlertsPerMinute { get; set; }
        public DateTime WindowStart { get; set; }
        public int AlertCount { get; set; }
    }

    /// <summary>
    /// Internal entry for tracking duplicate alerts.
    /// </summary>
    internal sealed class DuplicateEntry
    {
        public string AlertKey { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Result of duplicate suppression check.
    /// </summary>
    internal sealed class DuplicateSuppressionResult
    {
        public bool IsSuppressed { get; set; }
        public Alert ConsolidatedAlert { get; set; }
    }

    /// <summary>
    /// Information about a suppressed alert pattern.
    /// </summary>
    public sealed class SuppressionInfo
    {
        public string Key { get; set; }
        public string SourcePattern { get; set; }
        public string MessagePattern { get; set; }
        public AlertSeverity? Severity { get; set; }
        public DateTime LastTriggered { get; set; }
        public int TriggerCount { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }

    /// <summary>
    /// Statistics for alert suppression operations.
    /// </summary>
    public readonly record struct AlertSuppressionStatistics
    {
        public long TotalAlertsProcessed { get; init; }
        public long TotalAlertsSuppressed { get; init; }
        public long DuplicatesSuppressed { get; init; }
        public long RateLimitSuppressed { get; init; }
        public long TimeBasedSuppressed { get; init; }
        public double AverageProcessingTimeMs { get; init; }
        public DateTime LastUpdated { get; init; }

        public double SuppressionRate => TotalAlertsProcessed > 0 
            ? (double)TotalAlertsSuppressed / TotalAlertsProcessed * 100 
            : 0;

        public static AlertSuppressionStatistics Empty => new AlertSuppressionStatistics
        {
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Reasons for alert suppression.
    /// </summary>
    internal enum SuppressionReason
    {
        None = 0,
        Duplicate = 1,
        RateLimit = 2,
        TimeBased = 3
    }

    #endregion
}