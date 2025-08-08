using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Alert filter implementation that filters alerts based on severity levels.
    /// Supports minimum severity thresholds and per-source severity overrides.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public sealed class SeverityAlertFilter : BaseAlertFilter
    {
        private AlertSeverity _minimumSeverity = AlertSeverity.Information;
        private readonly Dictionary<string, AlertSeverity> _sourceOverrides = new Dictionary<string, AlertSeverity>();
        private bool _allowCriticalAlways = true;
        
        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => "SeverityFilter";

        /// <summary>
        /// Gets or sets the minimum severity level for alerts to pass through.
        /// </summary>
        public AlertSeverity MinimumSeverity
        {
            get => _minimumSeverity;
            set => _minimumSeverity = value;
        }

        /// <summary>
        /// Gets or sets whether critical alerts always pass through regardless of minimum severity.
        /// </summary>
        public bool AllowCriticalAlways
        {
            get => _allowCriticalAlways;
            set => _allowCriticalAlways = value;
        }

        /// <summary>
        /// Initializes a new instance of the SeverityAlertFilter class.
        /// </summary>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="allowCriticalAlways">Whether to always allow critical alerts</param>
        public SeverityAlertFilter(AlertSeverity minimumSeverity = AlertSeverity.Information, bool allowCriticalAlways = true)
        {
            _minimumSeverity = minimumSeverity;
            _allowCriticalAlways = allowCriticalAlways;
            Priority = 10; // High priority for severity filtering
        }

        /// <summary>
        /// Adds a severity override for a specific alert source.
        /// </summary>
        /// <param name="source">Alert source pattern</param>
        /// <param name="minimumSeverity">Minimum severity for this source</param>
        public void AddSourceOverride(string source, AlertSeverity minimumSeverity)
        {
            if (string.IsNullOrEmpty(source))
                return;

            _sourceOverrides[source] = minimumSeverity;
        }

        /// <summary>
        /// Removes a severity override for a specific alert source.
        /// </summary>
        /// <param name="source">Alert source pattern to remove</param>
        /// <returns>True if override was removed</returns>
        public bool RemoveSourceOverride(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            return _sourceOverrides.Remove(source);
        }

        /// <summary>
        /// Gets all configured source overrides.
        /// </summary>
        /// <returns>Dictionary of source overrides</returns>
        public IReadOnlyDictionary<string, AlertSeverity> GetSourceOverrides()
        {
            return new Dictionary<string, AlertSeverity>(_sourceOverrides);
        }

        /// <summary>
        /// Core implementation of alert evaluation.
        /// </summary>
        /// <param name="alert">Alert to evaluate</param>
        /// <param name="context">Filtering context</param>
        /// <returns>Filter result</returns>
        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            // Always allow critical alerts if configured
            if (_allowCriticalAlways && alert.Severity == AlertSeverity.Critical)
            {
                return FilterResult.Allow("Critical alert bypass");
            }

            // Check for source-specific overrides
            var requiredSeverity = GetRequiredSeverityForSource(alert.Source.ToString());
            
            if (alert.Severity >= requiredSeverity)
            {
                return FilterResult.Allow($"Alert meets minimum severity {requiredSeverity}");
            }

            return FilterResult.Suppress($"Alert severity {alert.Severity} below minimum {requiredSeverity}");
        }

        /// <summary>
        /// Core implementation to determine if filter can handle an alert.
        /// </summary>
        /// <param name="alert">Alert to check</param>
        /// <returns>True if filter can handle the alert</returns>
        protected override bool CanHandleCore(Alert alert)
        {
            // Severity filter can handle all alerts
            return true;
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        /// <param name="configuration">Configuration to apply</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>True if configuration was applied</returns>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            if (configuration == null)
                return true;

            // Apply minimum severity
            if (configuration.TryGetValue("MinimumSeverity", out var minSev))
            {
                if (minSev is AlertSeverity severity)
                    _minimumSeverity = severity;
                else if (minSev is string sevStr && Enum.TryParse<AlertSeverity>(sevStr, true, out var parsedSev))
                    _minimumSeverity = parsedSev;
                else if (minSev is int sevInt && Enum.IsDefined(typeof(AlertSeverity), sevInt))
                    _minimumSeverity = (AlertSeverity)sevInt;
            }

            // Apply critical bypass setting
            if (configuration.TryGetValue("AllowCriticalAlways", out var allowCritical) && allowCritical is bool allow)
                _allowCriticalAlways = allow;

            // Apply priority
            if (configuration.TryGetValue("Priority", out var priority) && priority is int prio)
                Priority = prio;

            // Apply source overrides
            if (configuration.TryGetValue("SourceOverrides", out var overrides) && overrides is Dictionary<string, object> overrideDict)
            {
                _sourceOverrides.Clear();
                foreach (var kvp in overrideDict)
                {
                    if (kvp.Value is AlertSeverity sev)
                        _sourceOverrides[kvp.Key] = sev;
                    else if (kvp.Value is string sevString && Enum.TryParse<AlertSeverity>(sevString, true, out var parsedOverride))
                        _sourceOverrides[kvp.Key] = parsedOverride;
                }
            }

            return true;
        }

        /// <summary>
        /// Core implementation of configuration validation.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        protected override FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (configuration.TryGetValue("MinimumSeverity", out var minSev))
            {
                if (minSev is not AlertSeverity && minSev is not string && minSev is not int)
                {
                    errors.Add("MinimumSeverity must be an AlertSeverity enum, string, or integer");
                }
                else if (minSev is string sevStr && !Enum.TryParse<AlertSeverity>(sevStr, true, out _))
                {
                    errors.Add($"Invalid AlertSeverity string: {sevStr}");
                }
                else if (minSev is int sevInt && !Enum.IsDefined(typeof(AlertSeverity), sevInt))
                {
                    errors.Add($"Invalid AlertSeverity integer: {sevInt}");
                }
            }

            if (configuration.TryGetValue("AllowCriticalAlways", out var allowCritical) && allowCritical is not bool)
            {
                errors.Add("AllowCriticalAlways must be a boolean value");
            }

            if (configuration.TryGetValue("Priority", out var priority) && priority is not int)
            {
                errors.Add("Priority must be an integer value");
            }

            if (configuration.TryGetValue("SourceOverrides", out var overrides))
            {
                if (overrides is not Dictionary<string, object>)
                {
                    errors.Add("SourceOverrides must be a dictionary of string keys and AlertSeverity values");
                }
                else
                {
                    var overrideDict = (Dictionary<string, object>)overrides;
                    foreach (var kvp in overrideDict)
                    {
                        if (string.IsNullOrWhiteSpace(kvp.Key))
                        {
                            errors.Add("SourceOverrides cannot have null or empty keys");
                            continue;
                        }

                        if (kvp.Value is not AlertSeverity && kvp.Value is not string)
                        {
                            errors.Add($"SourceOverrides[{kvp.Key}] must be an AlertSeverity enum or string");
                        }
                        else if (kvp.Value is string sevStr && !Enum.TryParse<AlertSeverity>(sevStr, true, out _))
                        {
                            errors.Add($"SourceOverrides[{kvp.Key}] has invalid AlertSeverity string: {sevStr}");
                        }
                    }
                }
            }

            // Warnings
            if (configuration.TryGetValue("MinimumSeverity", out var warnSev) && 
                (warnSev is AlertSeverity ws && ws == AlertSeverity.Debug || 
                 warnSev is string wss && string.Equals(wss, "Debug", StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add("MinimumSeverity set to Debug may result in high alert volume");
            }

            return errors.Count > 0 
                ? FilterValidationResult.Invalid(errors, warnings) 
                : FilterValidationResult.Valid();
        }

        /// <summary>
        /// Gets the required severity level for a given source.
        /// </summary>
        /// <param name="source">Alert source</param>
        /// <returns>Required minimum severity</returns>
        private AlertSeverity GetRequiredSeverityForSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return _minimumSeverity;

            // Check for exact match first
            if (_sourceOverrides.TryGetValue(source, out var exactMatch))
                return exactMatch;

            // Check for pattern matches (simple wildcard support)
            foreach (var kvp in _sourceOverrides)
            {
                if (MatchesPattern(source, kvp.Key))
                    return kvp.Value;
            }

            return _minimumSeverity;
        }

        /// <summary>
        /// Simple pattern matching with wildcard support.
        /// </summary>
        /// <param name="text">Text to match against</param>
        /// <param name="pattern">Pattern with * wildcards</param>
        /// <returns>True if pattern matches</returns>
        private static bool MatchesPattern(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;
            
            if (pattern == "*")
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

        /// <summary>
        /// Creates a severity filter with the specified minimum severity.
        /// </summary>
        /// <param name="minimumSeverity">Minimum severity level</param>
        /// <param name="allowCriticalAlways">Whether to always allow critical alerts</param>
        /// <returns>Configured severity filter</returns>
        public static SeverityAlertFilter Create(AlertSeverity minimumSeverity, bool allowCriticalAlways = true)
        {
            return new SeverityAlertFilter(minimumSeverity, allowCriticalAlways);
        }

        /// <summary>
        /// Creates a severity filter for development environments (allows all severities).
        /// </summary>
        /// <returns>Development-configured severity filter</returns>
        public static SeverityAlertFilter CreateForDevelopment()
        {
            return new SeverityAlertFilter(AlertSeverity.Debug, true)
            {
                Priority = 5 // Lower priority in development
            };
        }

        /// <summary>
        /// Creates a severity filter for production environments (stricter filtering).
        /// </summary>
        /// <returns>Production-configured severity filter</returns>
        public static SeverityAlertFilter CreateForProduction()
        {
            return new SeverityAlertFilter(AlertSeverity.Warning, true)
            {
                Priority = 10 // Higher priority in production
            };
        }

        /// <summary>
        /// Creates a severity filter with source-specific overrides.
        /// </summary>
        /// <param name="minimumSeverity">Global minimum severity</param>
        /// <param name="sourceOverrides">Source-specific severity overrides</param>
        /// <returns>Configured severity filter with overrides</returns>
        public static SeverityAlertFilter CreateWithOverrides(
            AlertSeverity minimumSeverity, 
            Dictionary<string, AlertSeverity> sourceOverrides)
        {
            var filter = new SeverityAlertFilter(minimumSeverity);
            
            if (sourceOverrides != null)
            {
                foreach (var kvp in sourceOverrides)
                {
                    filter.AddSourceOverride(kvp.Key, kvp.Value);
                }
            }
            
            return filter;
        }
    }
}