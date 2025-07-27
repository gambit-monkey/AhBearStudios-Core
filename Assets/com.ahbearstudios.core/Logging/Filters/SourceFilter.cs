using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using ZLinq;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on their source information.
    /// Provides source-based filtering with support for source patterns, contexts, and hierarchical matching.
    /// Supports both include and exclude filtering modes for flexible source management.
    /// </summary>
    public sealed class SourceFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private bool _isEnabled = true;
        private readonly List<string> _sources = new();
        private readonly List<string> _sourceContexts = new();
        private bool _includeMode = true;
        private bool _caseSensitive = false;
        private bool _useRegex = false;
        private bool _hierarchicalMatch = true;

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
        /// Gets the sources that this filter matches against.
        /// </summary>
        public IReadOnlyList<string> Sources => _sources.AsReadOnly();

        /// <summary>
        /// Gets the source contexts that this filter matches against.
        /// </summary>
        public IReadOnlyList<string> SourceContexts => _sourceContexts.AsReadOnly();

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
        /// Gets whether the filter supports hierarchical matching (e.g., "MyApp.Services" matches "MyApp.Services.UserService").
        /// </summary>
        public bool HierarchicalMatch => _hierarchicalMatch;

        /// <summary>
        /// Initializes a new instance of the SourceFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sources">The sources to filter</param>
        /// <param name="sourceContexts">The source contexts to filter</param>
        /// <param name="includeMode">Whether to include (true) or exclude (false) matching entries</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="useRegex">Whether to use regex matching</param>
        /// <param name="hierarchicalMatch">Whether to support hierarchical matching</param>
        /// <param name="priority">The filter priority (default: 900)</param>
        public SourceFilter(
            string name = "SourceFilter",
            IEnumerable<string> sources = null,
            IEnumerable<string> sourceContexts = null,
            bool includeMode = true,
            bool caseSensitive = false,
            bool useRegex = false,
            bool hierarchicalMatch = true,
            int priority = 900)
        {
            Name = name ?? "SourceFilter";
            Priority = priority;
            
            if (sources != null)
                _sources.AddRange(sources.AsValueEnumerable().Where(s => !string.IsNullOrEmpty(s)).ToList());
            
            if (sourceContexts != null)
                _sourceContexts.AddRange(sourceContexts.AsValueEnumerable().Where(sc => !string.IsNullOrEmpty(sc)).ToList());
            
            _includeMode = includeMode;
            _caseSensitive = caseSensitive;
            _useRegex = useRegex;
            _hierarchicalMatch = hierarchicalMatch;
            _statistics = FilterStatistics.ForSource(_sources.AsValueEnumerable().FirstOrDefault());
            
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["Sources"] = _sources,
                ["SourceContexts"] = _sourceContexts,
                ["IncludeMode"] = _includeMode,
                ["CaseSensitive"] = _caseSensitive,
                ["UseRegex"] = _useRegex,
                ["HierarchicalMatch"] = _hierarchicalMatch,
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

            // If no sources or contexts specified, allow all
            if (_sources.Count == 0 && _sourceContexts.Count == 0)
            {
                _statistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                bool matches = false;
                
                // Check source matching
                if (_sources.Count > 0 && !string.IsNullOrEmpty(entry.Source.ToString()))
                {
                    matches = MatchesSource(entry.Source.ToString());
                }
                
                // Check source context matching if source didn't match
                if (!matches && _sourceContexts.Count > 0 && !string.IsNullOrEmpty(entry.SourceContext.ToString()))
                {
                    matches = MatchesSourceContext(entry.SourceContext.ToString());
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

            if (_sources.Count == 0 && _sourceContexts.Count == 0)
            {
                warnings.Add(new ValidationWarning("No sources or source contexts specified - filter will allow all entries", nameof(Sources)));
            }

            // Validate regex patterns if enabled
            if (_useRegex)
            {
                foreach (var source in _sources)
                {
                    try
                    {
                        _ = new System.Text.RegularExpressions.Regex(source);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(new ValidationError($"Invalid regex pattern in source: {source}", nameof(Sources)));
                    }
                }
                
                foreach (var context in _sourceContexts)
                {
                    try
                    {
                        _ = new System.Text.RegularExpressions.Regex(context);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add(new ValidationError($"Invalid regex pattern in source context: {context}", nameof(SourceContexts)));
                    }
                }
            }

            // Check for duplicate sources
            if (_sources.AsValueEnumerable().Distinct().Count() != _sources.Count)
            {
                warnings.Add(new ValidationWarning("Duplicate sources detected", nameof(Sources)));
            }

            if (_sourceContexts.AsValueEnumerable().Distinct().Count() != _sourceContexts.Count)
            {
                warnings.Add(new ValidationWarning("Duplicate source contexts detected", nameof(SourceContexts)));
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
                    case "Sources":
                        if (setting.Value is IEnumerable<string> sources)
                        {
                            _sources.Clear();
                            _sources.AddRange(sources.AsValueEnumerable().Where(s => !string.IsNullOrEmpty(s)).ToList());
                        }
                        break;
                        
                    case "SourceContexts":
                        if (setting.Value is IEnumerable<string> contexts)
                        {
                            _sourceContexts.Clear();
                            _sourceContexts.AddRange(contexts.AsValueEnumerable().Where(sc => !string.IsNullOrEmpty(sc)).ToList());
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
                        
                    case "HierarchicalMatch":
                        if (setting.Value is bool hierarchicalMatch)
                            _hierarchicalMatch = hierarchicalMatch;
                        else if (bool.TryParse(setting.Value?.ToString(), out var parsedHierarchy))
                            _hierarchicalMatch = parsedHierarchy;
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
            _settings["Sources"] = _sources;
            _settings["SourceContexts"] = _sourceContexts;
            _settings["IncludeMode"] = _includeMode;
            _settings["CaseSensitive"] = _caseSensitive;
            _settings["UseRegex"] = _useRegex;
            _settings["HierarchicalMatch"] = _hierarchicalMatch;
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Checks if a source matches the configured source patterns.
        /// </summary>
        /// <param name="source">The source to check</param>
        /// <returns>True if the source matches</returns>
        private bool MatchesSource(string source)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            foreach (var sourcePattern in _sources)
            {
                if (IsMatch(source, sourcePattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a source context matches the configured source context patterns.
        /// </summary>
        /// <param name="sourceContext">The source context to check</param>
        /// <returns>True if the source context matches</returns>
        private bool MatchesSourceContext(string sourceContext)
        {
            if (string.IsNullOrEmpty(sourceContext))
                return false;

            foreach (var contextPattern in _sourceContexts)
            {
                if (IsMatch(sourceContext, contextPattern))
                    return true;
            }

            return false;
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

            if (_hierarchicalMatch)
            {
                // Support hierarchical matching: "MyApp.Services" matches "MyApp.Services.UserService"
                return value.StartsWith(pattern, comparison) && 
                       (value.Length == pattern.Length || value[pattern.Length] == '.');
            }

            return string.Equals(value, pattern, comparison);
        }

        /// <summary>
        /// Creates a SourceFilter configured for specific sources.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sources">The sources to filter</param>
        /// <param name="includeMode">Whether to include or exclude the specified sources</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SourceFilter instance</returns>
        public static SourceFilter ForSources(string name, IEnumerable<string> sources, bool includeMode = true, int priority = 900)
        {
            return new SourceFilter(name, sources, null, includeMode, false, false, true, priority);
        }

        /// <summary>
        /// Creates a SourceFilter configured for specific source contexts.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sourceContexts">The source contexts to filter</param>
        /// <param name="includeMode">Whether to include or exclude the specified contexts</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SourceFilter instance</returns>
        public static SourceFilter ForSourceContexts(string name, IEnumerable<string> sourceContexts, bool includeMode = true, int priority = 900)
        {
            return new SourceFilter(name, null, sourceContexts, includeMode, false, false, true, priority);
        }

        /// <summary>
        /// Creates a SourceFilter configured for hierarchical source matching.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sourcePrefix">The source prefix to match hierarchically</param>
        /// <param name="includeMode">Whether to include or exclude matching sources</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SourceFilter instance</returns>
        public static SourceFilter ForHierarchy(string name, string sourcePrefix, bool includeMode = true, int priority = 900)
        {
            return new SourceFilter(name, new[] { sourcePrefix }, null, includeMode, false, false, true, priority);
        }

        /// <summary>
        /// Creates a SourceFilter configured for regex pattern matching.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="sourcePatterns">The regex patterns to match</param>
        /// <param name="includeMode">Whether to include or exclude matching sources</param>
        /// <param name="caseSensitive">Whether to use case-sensitive matching</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured SourceFilter instance</returns>
        public static SourceFilter ForRegex(string name, IEnumerable<string> sourcePatterns, bool includeMode = true, bool caseSensitive = false, int priority = 900)
        {
            return new SourceFilter(name, sourcePatterns, null, includeMode, caseSensitive, true, false, priority);
        }
    }
}