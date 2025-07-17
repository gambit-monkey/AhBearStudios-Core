using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on message patterns.
    /// Provides message pattern filtering with support for regex and wildcard matching.
    /// Supports both include and exclude filtering modes for flexible message filtering.
    /// </summary>
    public sealed class PatternFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private bool _isEnabled = true;
        private readonly List<string> _messagePatterns = new();
        private bool _includeMode = true;
        private bool _caseSensitive = false;
        private bool _useRegex = false;
        private bool _useWildcards = false;

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
        /// Gets the message patterns that this filter matches against.
        /// </summary>
        public IReadOnlyList<string> MessagePatterns => _messagePatterns.AsReadOnly();

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
        /// Gets whether the filter uses wildcard matching (* and ?).
        /// </summary>
        public bool UseWildcards => _useWildcards;

        /// <summary>
        /// Initializes a new instance of the PatternFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="messagePatterns">The message patterns to filter</param>
        /// <param name="includeMode">Whether to include (true) or exclude (false) matching entries</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <param name="useWildcards">Whether to use wildcard matching</param>
        /// <param name="priority">The filter priority (default: 700)</param>
        public PatternFilter(
            string name = "PatternFilter",
            IEnumerable<string> messagePatterns = null,
            bool includeMode = true,
            bool caseSensitive = false,
            bool useRegex = false,
            bool useWildcards = false,
            int priority = 700)
        {
            Name = name ?? "PatternFilter";
            Priority = priority;
            
            if (messagePatterns != null)
                _messagePatterns.AddRange(messagePatterns.Where(p => !string.IsNullOrEmpty(p)));
            
            _includeMode = includeMode;
            _caseSensitive = caseSensitive;
            _useRegex = useRegex;
            _useWildcards = useWildcards;
            _statistics = FilterStatistics.ForPattern(name ?? "PatternFilter");
            
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["MessagePatterns"] = _messagePatterns,
                ["IncludeMode"] = _includeMode,
                ["CaseSensitive"] = _caseSensitive,
                ["UseRegex"] = _useRegex,
                ["UseWildcards"] = _useWildcards,
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

            // If no patterns specified, allow all
            if (_messagePatterns.Count == 0)
            {
                _statistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var message = entry.Message.ToString();
                
                // Handle empty messages
                if (string.IsNullOrEmpty(message))
                {
                    var shouldProcessEmptyMessages = !_includeMode; // Empty messages don't match patterns
                    
                    stopwatch.Stop();
                    
                    if (shouldProcessEmptyMessages)
                    {
                        _statistics.RecordAllowed(stopwatch.Elapsed);
                    }
                    else
                    {
                        _statistics.RecordBlocked(stopwatch.Elapsed);
                    }
                    
                    return shouldProcessEmptyMessages;
                }

                bool matches = false;
                
                // Check pattern matching
                foreach (var pattern in _messagePatterns)
                {
                    if (IsMatch(message, pattern))
                    {
                        matches = true;
                        break;
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

            if (_messagePatterns.Count == 0)
            {
                warnings.Add(new ValidationWarning("No message patterns specified - filter will allow all entries", nameof(MessagePatterns)));
            }

            // Validate regex patterns if enabled
            if (_useRegex)
            {
                foreach (var pattern in _messagePatterns)
                {
                    try
                    {
                        _ = new System.Text.RegularExpressions.Regex(pattern);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(new ValidationError($"Invalid regex pattern: {pattern}", nameof(MessagePatterns)));
                    }
                }
            }

            // Check for duplicate patterns
            if (_messagePatterns.Distinct().Count() != _messagePatterns.Count)
            {
                warnings.Add(new ValidationWarning("Duplicate message patterns detected", nameof(MessagePatterns)));
            }

            // Check for conflicting pattern matching options
            if (_useRegex && _useWildcards)
            {
                warnings.Add(new ValidationWarning("Both regex and wildcard matching enabled - regex will take precedence", nameof(UseRegex)));
            }

            // Check for potentially problematic patterns
            if (_messagePatterns.Any(p => p.Length > 1000))
            {
                warnings.Add(new ValidationWarning("Very long patterns detected - may impact performance", nameof(MessagePatterns)));
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
                    case "MessagePatterns":
                        if (setting.Value is IEnumerable<string> patterns)
                        {
                            _messagePatterns.Clear();
                            _messagePatterns.AddRange(patterns.Where(p => !string.IsNullOrEmpty(p)));
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
                        
                    case "UseWildcards":
                        if (setting.Value is bool useWildcards)
                            _useWildcards = useWildcards;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedWildcards))
                            _useWildcards = parsedWildcards;
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
            _settings["MessagePatterns"] = _messagePatterns;
            _settings["IncludeMode"] = _includeMode;
            _settings["CaseSensitive"] = _caseSensitive;
            _settings["UseRegex"] = _useRegex;
            _settings["UseWildcards"] = _useWildcards;
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Determines if a message matches a pattern based on the current configuration.
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <param name="pattern">The pattern to match against</param>
        /// <returns>True if the message matches the pattern</returns>
        private bool IsMatch(string message, string pattern)
        {
            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(pattern))
                return false;

            var comparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (_useRegex)
            {
                try
                {
                    var options = _caseSensitive ? 
                        System.Text.RegularExpressions.RegexOptions.None : 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    return System.Text.RegularExpressions.Regex.IsMatch(message, pattern, options);
                }
                catch (ArgumentException)
                {
                    // Fall back to simple string matching if regex fails
                    return message.Contains(pattern, comparison);
                }
            }

            if (_useWildcards)
            {
                return IsWildcardMatch(message, pattern, comparison);
            }

            return message.Contains(pattern, comparison);
        }

        /// <summary>
        /// Performs wildcard pattern matching (* and ?).
        /// </summary>
        /// <param name="text">The text to match</param>
        /// <param name="pattern">The wildcard pattern</param>
        /// <param name="comparison">The string comparison type</param>
        /// <returns>True if the text matches the wildcard pattern</returns>
        private bool IsWildcardMatch(string text, string pattern, StringComparison comparison)
        {
            // Convert wildcard pattern to regex
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            try
            {
                var options = comparison == StringComparison.OrdinalIgnoreCase ? 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase : 
                    System.Text.RegularExpressions.RegexOptions.None;
                return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, options);
            }
            catch (ArgumentException)
            {
                // Fall back to simple contains matching
                return text.Contains(pattern.Replace("*", "").Replace("?", ""), comparison);
            }
        }

        /// <summary>
        /// Creates a PatternFilter configured for specific message patterns.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="patterns">The message patterns to filter</param>
        /// <param name="includeMode">Whether to include or exclude matching messages</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured PatternFilter instance</returns>
        public static PatternFilter ForPatterns(string name, IEnumerable<string> patterns, bool includeMode = true, int priority = 700)
        {
            return new PatternFilter(name, patterns, includeMode, false, false, false, priority);
        }

        /// <summary>
        /// Creates a PatternFilter configured for regex pattern matching.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="regexPatterns">The regex patterns to match</param>
        /// <param name="includeMode">Whether to include or exclude matching messages</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured PatternFilter instance</returns>
        public static PatternFilter ForRegex(string name, IEnumerable<string> regexPatterns, bool includeMode = true, bool caseSensitive = false, int priority = 700)
        {
            return new PatternFilter(name, regexPatterns, includeMode, caseSensitive, true, false, priority);
        }

        /// <summary>
        /// Creates a PatternFilter configured for wildcard pattern matching.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="wildcardPatterns">The wildcard patterns to match</param>
        /// <param name="includeMode">Whether to include or exclude matching messages</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured PatternFilter instance</returns>
        public static PatternFilter ForWildcards(string name, IEnumerable<string> wildcardPatterns, bool includeMode = true, bool caseSensitive = false, int priority = 700)
        {
            return new PatternFilter(name, wildcardPatterns, includeMode, caseSensitive, false, true, priority);
        }

        /// <summary>
        /// Creates a PatternFilter configured to exclude error messages.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured PatternFilter instance</returns>
        public static PatternFilter ExcludeErrors(string name = "ExcludeErrors", int priority = 700)
        {
            return new PatternFilter(name, new[] { "error", "exception", "failed", "fault" }, false, false, false, false, priority);
        }

        /// <summary>
        /// Creates a PatternFilter configured to only include specific keywords.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="keywords">The keywords to include</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured PatternFilter instance</returns>
        public static PatternFilter IncludeKeywords(string name, IEnumerable<string> keywords, int priority = 700)
        {
            return new PatternFilter(name, keywords, true, false, false, false, priority);
        }
    }
}