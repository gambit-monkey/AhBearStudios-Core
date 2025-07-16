using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on their correlation ID.
    /// Provides correlation-based filtering with support for pattern matching and session tracking.
    /// Supports both include and exclude filtering modes for flexible correlation management.
    /// </summary>
    public sealed class CorrelationFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private bool _isEnabled = true;
        private readonly List<string> _correlationIdPatterns = new();
        private readonly List<string> _userIds = new();
        private readonly List<string> _sessionIds = new();
        private bool _includeMode = true;
        private bool _caseSensitive = false;
        private bool _useRegex = false;
        private bool _allowEmptyCorrelationIds = false;

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }

        /// <inheritdoc />
        public int Priority { get; }

        /// <summary>
        /// Gets the correlation ID patterns that this filter matches against.
        /// </summary>
        public IReadOnlyList<string> CorrelationIdPatterns => _correlationIdPatterns.AsReadOnly();

        /// <summary>
        /// Gets the user IDs that this filter matches against.
        /// </summary>
        public IReadOnlyList<string> UserIds => _userIds.AsReadOnly();

        /// <summary>
        /// Gets the session IDs that this filter matches against.
        /// </summary>
        public IReadOnlyList<string> SessionIds => _sessionIds.AsReadOnly();

        /// <summary>
        /// Gets whether the filter is in include mode (true) or exclude mode (false).
        /// </summary>
        public bool IncludeMode => _includeMode;

        /// <summary>
        /// Gets whether the filter uses case-sensitive matching.
        /// </summary>
        public bool CaseSensitive => _caseSensitive;

        /// <summary>
        /// Gets whether the filter uses regex matching.
        /// </summary>
        public bool UseRegex => _useRegex;

        /// <summary>
        /// Gets whether the filter allows empty correlation IDs to pass through.
        /// </summary>
        public bool AllowEmptyCorrelationIds => _allowEmptyCorrelationIds;

        /// <summary>
        /// Initializes a new instance of the CorrelationFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="correlationIdPatterns">The correlation ID patterns to filter</param>
        /// <param name="userIds">The user IDs to filter</param>
        /// <param name="sessionIds">The session IDs to filter</param>
        /// <param name="includeMode">Whether to include (true) or exclude (false) matching entries</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <param name="allowEmptyCorrelationIds">Whether to allow empty correlation IDs</param>
        /// <param name="priority">The filter priority (default: 800)</param>
        public CorrelationFilter(
            string name = "CorrelationFilter",
            IEnumerable<string> correlationIdPatterns = null,
            IEnumerable<string> userIds = null,
            IEnumerable<string> sessionIds = null,
            bool includeMode = true,
            bool caseSensitive = false,
            bool useRegex = false,
            bool allowEmptyCorrelationIds = false,
            int priority = 800)
        {
            Name = name ?? "CorrelationFilter";
            Priority = priority;
            
            if (correlationIdPatterns != null)
                _correlationIdPatterns.AddRange(correlationIdPatterns.Where(p => !string.IsNullOrEmpty(p)));
            
            if (userIds != null)
                _userIds.AddRange(userIds.Where(u => !string.IsNullOrEmpty(u)));
            
            if (sessionIds != null)
                _sessionIds.AddRange(sessionIds.Where(s => !string.IsNullOrEmpty(s)));
            
            _includeMode = includeMode;
            _caseSensitive = caseSensitive;
            _useRegex = useRegex;
            _allowEmptyCorrelationIds = allowEmptyCorrelationIds;
            _statistics = new FilterStatistics();
            
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["CorrelationIdPatterns"] = _correlationIdPatterns,
                ["UserIds"] = _userIds,
                ["SessionIds"] = _sessionIds,
                ["IncludeMode"] = _includeMode,
                ["CaseSensitive"] = _caseSensitive,
                ["UseRegex"] = _useRegex,
                ["AllowEmptyCorrelationIds"] = _allowEmptyCorrelationIds,
                ["Priority"] = priority,
                ["IsEnabled"] = true
            };
        }

        /// <inheritdoc />
        public bool ShouldProcess(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            if (!_isEnabled)
            {
                _statistics.RecordAllowed();
                return true;
            }

            // If no patterns or IDs specified, allow all
            if (_correlationIdPatterns.Count == 0 && _userIds.Count == 0 && _sessionIds.Count == 0)
            {
                _statistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Use the provided correlation ID or extract from entry
                var entryCorrelationId = correlationId.IsEmpty ? entry.CorrelationId : correlationId;
                var correlationIdStr = entryCorrelationId.ToString();
                
                // Handle empty correlation IDs
                if (string.IsNullOrEmpty(correlationIdStr))
                {
                    var shouldProcessEmptyCorrelationIds = _allowEmptyCorrelationIds;
                    
                    stopwatch.Stop();
                    
                    if (shouldProcessEmptyCorrelationIds)
                    {
                        _statistics.RecordAllowed(stopwatch.Elapsed);
                    }
                    else
                    {
                        _statistics.RecordBlocked(stopwatch.Elapsed);
                    }
                    
                    return shouldProcessEmptyCorrelationIds;
                }

                bool matches = false;
                
                // Check correlation ID pattern matching
                if (_correlationIdPatterns.Count > 0)
                {
                    matches = MatchesCorrelationId(correlationIdStr);
                }
                
                // Check user ID matching if correlation didn't match
                if (!matches && _userIds.Count > 0)
                {
                    var userId = GetPropertyValue(entry, "UserId") ?? GetPropertyValue(entry, "User") ?? GetPropertyValue(entry, "UserName");
                    if (!string.IsNullOrEmpty(userId))
                    {
                        matches = MatchesUserId(userId);
                    }
                }
                
                // Check session ID matching if neither correlation nor user matched
                if (!matches && _sessionIds.Count > 0)
                {
                    var sessionId = GetPropertyValue(entry, "SessionId") ?? GetPropertyValue(entry, "Session");
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        matches = MatchesSessionId(sessionId);
                    }
                }
                
                // Apply include/exclude logic
                var shouldProcess = _includeMode ? matches : !matches;
                
                stopwatch.Stop();
                
                if (shouldProcess)
                {
                    _statistics.RecordAllowed(stopwatch.Elapsed);
                }
                else
                {
                    _statistics.RecordBlocked(stopwatch.Elapsed);
                }
                
                return shouldProcess;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _statistics.RecordError(stopwatch.Elapsed);
                
                // On error, allow the entry to pass through to prevent log loss
                return true;
            }
        }

        /// <inheritdoc />
        public ValidationResult Validate(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrEmpty(Name.ToString()))
            {
                errors.Add(new ValidationError("Filter name cannot be empty", nameof(Name)));
            }

            if (_correlationIdPatterns.Count == 0 && _userIds.Count == 0 && _sessionIds.Count == 0)
            {
                warnings.Add(new ValidationWarning("No correlation patterns, user IDs, or session IDs specified - filter will allow all entries", nameof(CorrelationIdPatterns)));
            }

            // Validate regex patterns if enabled
            if (_useRegex)
            {
                foreach (var pattern in _correlationIdPatterns)
                {
                    try
                    {
                        _ = new System.Text.RegularExpressions.Regex(pattern);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(new ValidationError($"Invalid regex pattern in correlation ID: {pattern}", nameof(CorrelationIdPatterns)));
                    }
                }
            }

            // Check for duplicate patterns
            if (_correlationIdPatterns.Distinct().Count() != _correlationIdPatterns.Count)
            {
                warnings.Add(new ValidationWarning("Duplicate correlation ID patterns detected", nameof(CorrelationIdPatterns)));
            }

            if (_userIds.Distinct().Count() != _userIds.Count)
            {
                warnings.Add(new ValidationWarning("Duplicate user IDs detected", nameof(UserIds)));
            }

            if (_sessionIds.Distinct().Count() != _sessionIds.Count)
            {
                warnings.Add(new ValidationWarning("Duplicate session IDs detected", nameof(SessionIds)));
            }

            // Check for potentially problematic configurations
            if (!_allowEmptyCorrelationIds && _includeMode && _correlationIdPatterns.Count > 0)
            {
                warnings.Add(new ValidationWarning("Filter will block entries with empty correlation IDs", nameof(AllowEmptyCorrelationIds)));
            }
            
            // Check if user or session ID filtering is configured but may not find values
            if (_userIds.Count > 0)
            {
                warnings.Add(new ValidationWarning("User ID filtering requires 'UserId', 'User', or 'UserName' properties in log entries", nameof(UserIds)));
            }
            
            if (_sessionIds.Count > 0)
            {
                warnings.Add(new ValidationWarning("Session ID filtering requires 'SessionId' or 'Session' properties in log entries", nameof(SessionIds)));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, Name.ToString(), warnings)
                : ValidationResult.Success(Name.ToString(), warnings);
        }

        /// <inheritdoc />
        public FilterStatistics GetStatistics()
        {
            return _statistics.CreateSnapshot();
        }

        /// <inheritdoc />
        public void Reset(FixedString64Bytes correlationId = default)
        {
            _statistics.Reset();
        }

        /// <inheritdoc />
        public void Configure(IReadOnlyDictionary<FixedString32Bytes, object> settings, FixedString64Bytes correlationId = default)
        {
            if (settings == null) return;

            foreach (var setting in settings)
            {
                _settings[setting.Key] = setting.Value;
                
                // Apply settings to internal state
                switch (setting.Key.ToString())
                {
                    case "CorrelationIdPatterns":
                        if (setting.Value is IEnumerable<string> patterns)
                        {
                            _correlationIdPatterns.Clear();
                            _correlationIdPatterns.AddRange(patterns.Where(p => !string.IsNullOrEmpty(p)));
                        }
                        break;
                        
                    case "UserIds":
                        if (setting.Value is IEnumerable<string> userIds)
                        {
                            _userIds.Clear();
                            _userIds.AddRange(userIds.Where(u => !string.IsNullOrEmpty(u)));
                        }
                        break;
                        
                    case "SessionIds":
                        if (setting.Value is IEnumerable<string> sessionIds)
                        {
                            _sessionIds.Clear();
                            _sessionIds.AddRange(sessionIds.Where(s => !string.IsNullOrEmpty(s)));
                        }
                        break;
                        
                    case "IncludeMode":
                        if (setting.Value is bool includeMode)
                            _includeMode = includeMode;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedInclude))
                            _includeMode = parsedInclude;
                        break;
                        
                    case "CaseSensitive":
                        if (setting.Value is bool caseSensitive)
                            _caseSensitive = caseSensitive;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedCase))
                            _caseSensitive = parsedCase;
                        break;
                        
                    case "UseRegex":
                        if (setting.Value is bool useRegex)
                            _useRegex = useRegex;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedRegex))
                            _useRegex = parsedRegex;
                        break;
                        
                    case "AllowEmptyCorrelationIds":
                        if (setting.Value is bool allowEmpty)
                            _allowEmptyCorrelationIds = allowEmpty;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedEmpty))
                            _allowEmptyCorrelationIds = parsedEmpty;
                        break;
                        
                    case "IsEnabled":
                        if (setting.Value is bool isEnabled)
                            _isEnabled = isEnabled;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedEnabled))
                            _isEnabled = parsedEnabled;
                        break;
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<FixedString32Bytes, object> GetSettings()
        {
            // Update current values in settings
            _settings["CorrelationIdPatterns"] = _correlationIdPatterns;
            _settings["UserIds"] = _userIds;
            _settings["SessionIds"] = _sessionIds;
            _settings["IncludeMode"] = _includeMode;
            _settings["CaseSensitive"] = _caseSensitive;
            _settings["UseRegex"] = _useRegex;
            _settings["AllowEmptyCorrelationIds"] = _allowEmptyCorrelationIds;
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Checks if a correlation ID matches the configured patterns.
        /// </summary>
        /// <param name="correlationId">The correlation ID to check</param>
        /// <returns>True if the correlation ID matches</returns>
        private bool MatchesCorrelationId(string correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                return false;

            foreach (var pattern in _correlationIdPatterns)
            {
                if (IsMatch(correlationId, pattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a user ID matches the configured user IDs.
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if the user ID matches</returns>
        private bool MatchesUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            foreach (var userIdPattern in _userIds)
            {
                if (IsMatch(userId, userIdPattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a session ID matches the configured session IDs.
        /// </summary>
        /// <param name="sessionId">The session ID to check</param>
        /// <returns>True if the session ID matches</returns>
        private bool MatchesSessionId(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return false;

            foreach (var sessionIdPattern in _sessionIds)
            {
                if (IsMatch(sessionId, sessionIdPattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a property value from the log entry's Properties dictionary.
        /// </summary>
        /// <param name="entry">The log entry to extract the property from</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <returns>The property value as a string, or null if not found</returns>
        private string GetPropertyValue(LogEntry entry, string propertyName)
        {
            if (!entry.HasProperties || entry.Properties == null)
                return null;
            
            if (entry.Properties.TryGetValue(propertyName, out var value))
            {
                return value?.ToString();
            }
            
            return null;
        }

        /// <summary>
        /// Determines if a value matches a pattern based on the current configuration.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="pattern">The pattern to match against</param>
        /// <returns>True if the value matches the pattern</returns>
        private bool IsMatch(string value, string pattern)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
                return false;

            var comparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (_useRegex)
            {
                try
                {
                    var options = _caseSensitive ? 
                        System.Text.RegularExpressions.RegexOptions.None : 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    return System.Text.RegularExpressions.Regex.IsMatch(value, pattern, options);
                }
                catch (ArgumentException)
                {
                    // Fall back to simple string matching if regex fails
                    return string.Equals(value, pattern, comparison);
                }
            }

            return string.Equals(value, pattern, comparison);
        }

        /// <summary>
        /// Creates a CorrelationFilter configured for specific correlation IDs.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="correlationIds">The correlation IDs to filter</param>
        /// <param name="includeMode">Whether to include or exclude the specified correlation IDs</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        public static CorrelationFilter ForCorrelationIds(string name, IEnumerable<string> correlationIds, bool includeMode = true, int priority = 800)
        {
            return new CorrelationFilter(name, correlationIds, null, null, includeMode, false, false, false, priority);
        }

        /// <summary>
        /// Creates a CorrelationFilter configured for specific user IDs.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="userIds">The user IDs to filter</param>
        /// <param name="includeMode">Whether to include or exclude the specified user IDs</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        public static CorrelationFilter ForUserIds(string name, IEnumerable<string> userIds, bool includeMode = true, int priority = 800)
        {
            return new CorrelationFilter(name, null, userIds, null, includeMode, false, false, false, priority);
        }

        /// <summary>
        /// Creates a CorrelationFilter configured for specific session IDs.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sessionIds">The session IDs to filter</param>
        /// <param name="includeMode">Whether to include or exclude the specified session IDs</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        public static CorrelationFilter ForSessionIds(string name, IEnumerable<string> sessionIds, bool includeMode = true, int priority = 800)
        {
            return new CorrelationFilter(name, null, null, sessionIds, includeMode, false, false, false, priority);
        }

        /// <summary>
        /// Creates a CorrelationFilter configured for regex pattern matching.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="correlationPatterns">The regex patterns to match</param>
        /// <param name="includeMode">Whether to include or exclude matching correlation IDs</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        public static CorrelationFilter ForRegex(string name, IEnumerable<string> correlationPatterns, bool includeMode = true, bool caseSensitive = false, int priority = 800)
        {
            return new CorrelationFilter(name, correlationPatterns, null, null, includeMode, caseSensitive, true, false, priority);
        }

        /// <summary>
        /// Creates a CorrelationFilter configured to allow empty correlation IDs.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        public static CorrelationFilter AllowEmpty(string name = "AllowEmptyCorrelation", int priority = 800)
        {
            return new CorrelationFilter(name, null, null, null, true, false, false, true, priority);
        }

        /// <summary>
        /// Creates a CorrelationFilter configured to block empty correlation IDs.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured CorrelationFilter instance</returns>
        public static CorrelationFilter BlockEmpty(string name = "BlockEmptyCorrelation", int priority = 800)
        {
            return new CorrelationFilter(name, null, null, null, false, false, false, false, priority);
        }
    }
}