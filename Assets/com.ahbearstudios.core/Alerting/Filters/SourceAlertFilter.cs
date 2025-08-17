using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging;
using ZLinq;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Source-based filtering with pattern matching support.
    /// Supports whitelist/blacklist modes with wildcards and regex patterns.
    /// Uses Unity Collections for zero-allocation operations.
    /// </summary>
    public sealed class SourceAlertFilter : BaseAlertFilter
    {
        private readonly HashSet<FixedString64Bytes> _allowedSources;
        private readonly HashSet<FixedString64Bytes> _blockedSources;
        private readonly List<string> _allowedPatterns;
        private readonly List<string> _blockedPatterns;
        private readonly Dictionary<string, Regex> _regexCache;
        private readonly ProfilerMarker _evaluateMarker;
        private readonly object _sourcesLock = new object();
        
        private bool _useWhitelist = true;
        private bool _caseSensitive = false;
        private bool _useRegex = false;
        private bool _allowEmptySource = false;

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => "SourceFilter";

        /// <summary>
        /// Gets or sets whether to use whitelist mode (true) or blacklist mode (false).
        /// </summary>
        public bool UseWhitelist
        {
            get => _useWhitelist;
            set => _useWhitelist = value;
        }

        /// <summary>
        /// Gets or sets whether source matching is case-sensitive.
        /// </summary>
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => _caseSensitive = value;
        }

        /// <summary>
        /// Gets or sets whether to use regex patterns for matching.
        /// </summary>
        public bool UseRegex
        {
            get => _useRegex;
            set => _useRegex = value;
        }

        /// <summary>
        /// Gets or sets whether to allow alerts with empty source.
        /// </summary>
        public bool AllowEmptySource
        {
            get => _allowEmptySource;
            set => _allowEmptySource = value;
        }

        /// <summary>
        /// Initializes a new instance of the SourceAlertFilter class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing filter events</param>
        /// <param name="name">Filter name</param>
        /// <param name="allowedSources">Initial allowed sources</param>
        /// <param name="useWhitelist">Whether to use whitelist mode</param>
        public SourceAlertFilter(IMessageBusService messageBusService, string name = "SourceFilter", IEnumerable<string> allowedSources = null, bool useWhitelist = true) : base(messageBusService)
        {
            _allowedSources = new HashSet<FixedString64Bytes>();
            _blockedSources = new HashSet<FixedString64Bytes>();
            _allowedPatterns = new List<string>();
            _blockedPatterns = new List<string>();
            _regexCache = new Dictionary<string, Regex>();
            _evaluateMarker = new ProfilerMarker("SourceAlertFilter.Evaluate");
            _useWhitelist = useWhitelist;
            Priority = 20; // High priority for source filtering

            if (allowedSources != null)
            {
                foreach (var source in allowedSources)
                {
                    AddSource(source, true);
                }
            }
        }

        /// <summary>
        /// Adds a source to the allowed or blocked list.
        /// </summary>
        /// <param name="source">Source to add</param>
        /// <param name="allowed">Whether to add to allowed (true) or blocked (false) list</param>
        public void AddSource(string source, bool allowed = true)
        {
            if (string.IsNullOrEmpty(source)) return;

            lock (_sourcesLock)
            {
                // Check if it's a pattern
                if (source.Contains("*") || source.Contains("?") || (_useRegex && ContainsRegexMetacharacters(source)))
                {
                    if (allowed)
                        _allowedPatterns.Add(source);
                    else
                        _blockedPatterns.Add(source);

                    // Cache regex if using regex mode
                    if (_useRegex && !_regexCache.ContainsKey(source))
                    {
                        try
                        {
                            var options = _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                            _regexCache[source] = new Regex(source, options | RegexOptions.Compiled);
                        }
                        catch
                        {
                            // Invalid regex, treat as literal
                        }
                    }
                }
                else
                {
                    // Exact source
                    var fixedSource = new FixedString64Bytes(source);
                    if (allowed)
                    {
                        _allowedSources.Add(fixedSource);
                        _blockedSources.Remove(fixedSource);
                    }
                    else
                    {
                        _blockedSources.Add(fixedSource);
                        _allowedSources.Remove(fixedSource);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a source from both allowed and blocked lists.
        /// </summary>
        /// <param name="source">Source to remove</param>
        /// <returns>True if source was removed</returns>
        public bool RemoveSource(string source)
        {
            if (string.IsNullOrEmpty(source)) return false;

            lock (_sourcesLock)
            {
                bool removed = false;
                
                // Remove from exact sources
                var fixedSource = new FixedString64Bytes(source);
                removed |= _allowedSources.Remove(fixedSource);
                removed |= _blockedSources.Remove(fixedSource);
                
                // Remove from patterns
                removed |= _allowedPatterns.Remove(source);
                removed |= _blockedPatterns.Remove(source);
                
                // Remove from regex cache
                _regexCache.Remove(source);
                
                return removed;
            }
        }

        /// <summary>
        /// Clears all sources from both lists.
        /// </summary>
        public void ClearSources()
        {
            lock (_sourcesLock)
            {
                _allowedSources.Clear();
                _blockedSources.Clear();
                _allowedPatterns.Clear();
                _blockedPatterns.Clear();
                _regexCache.Clear();
            }
        }

        /// <summary>
        /// Core implementation of alert evaluation.
        /// </summary>
        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            using (_evaluateMarker.Auto())
            {
                // Handle empty source
                if (alert.Source.IsEmpty)
                {
                    if (_allowEmptySource)
                        return FilterResult.Allow("Empty source allowed");
                    else
                        return FilterResult.Suppress("Empty source not allowed");
                }

                lock (_sourcesLock)
                {
                    bool isAllowed;
                    
                    if (_useWhitelist)
                    {
                        // Whitelist mode: must be in allowed list
                        isAllowed = IsSourceInList(alert.Source, _allowedSources, _allowedPatterns);
                    }
                    else
                    {
                        // Blacklist mode: must NOT be in blocked list
                        isAllowed = !IsSourceInList(alert.Source, _blockedSources, _blockedPatterns);
                    }

                    if (isAllowed)
                    {
                        return FilterResult.Allow($"Source '{alert.Source}' passed {(_useWhitelist ? "whitelist" : "blacklist")} filter");
                    }
                    else
                    {
                        return FilterResult.Suppress($"Source '{alert.Source}' blocked by {(_useWhitelist ? "whitelist" : "blacklist")} filter");
                    }
                }
            }
        }

        /// <summary>
        /// Core implementation to determine if filter can handle an alert.
        /// </summary>
        protected override bool CanHandleCore(Alert alert)
        {
            // Source filtering applies to all alerts
            return true;
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            if (configuration == null) return true;

            lock (_sourcesLock)
            {
                if (configuration.TryGetValue("AllowedSources", out var allowedObj))
                {
                    if (allowedObj is IEnumerable<string> allowed)
                    {
                        _allowedSources.Clear();
                        _allowedPatterns.Clear();
                        foreach (var source in allowed)
                        {
                            AddSource(source, true);
                        }
                    }
                }

                if (configuration.TryGetValue("BlockedSources", out var blockedObj))
                {
                    if (blockedObj is IEnumerable<string> blocked)
                    {
                        _blockedSources.Clear();
                        _blockedPatterns.Clear();
                        foreach (var source in blocked)
                        {
                            AddSource(source, false);
                        }
                    }
                }

                if (configuration.TryGetValue("UseWhitelist", out var whitelistObj))
                {
                    if (bool.TryParse(whitelistObj.ToString(), out var whitelist))
                    {
                        _useWhitelist = whitelist;
                    }
                }

                if (configuration.TryGetValue("CaseSensitive", out var caseObj))
                {
                    if (bool.TryParse(caseObj.ToString(), out var caseSensitive))
                    {
                        _caseSensitive = caseSensitive;
                        // Rebuild regex cache with new case sensitivity
                        RebuildRegexCache();
                    }
                }

                if (configuration.TryGetValue("UseRegex", out var regexObj))
                {
                    if (bool.TryParse(regexObj.ToString(), out var useRegex))
                    {
                        _useRegex = useRegex;
                        if (_useRegex)
                        {
                            RebuildRegexCache();
                        }
                    }
                }

                if (configuration.TryGetValue("AllowEmptySource", out var emptyObj))
                {
                    if (bool.TryParse(emptyObj.ToString(), out var allowEmpty))
                    {
                        _allowEmptySource = allowEmpty;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Core implementation of configuration validation.
        /// </summary>
        protected override FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (configuration != null)
            {
                // Validate regex patterns if using regex mode
                if (configuration.TryGetValue("UseRegex", out var regexObj) && 
                    bool.TryParse(regexObj.ToString(), out var useRegex) && useRegex)
                {
                    var patterns = new List<string>();
                    
                    if (configuration.TryGetValue("AllowedSources", out var allowedObj) && 
                        allowedObj is IEnumerable<string> allowed)
                    {
                        patterns.AddRange(allowed);
                    }
                    
                    if (configuration.TryGetValue("BlockedSources", out var blockedObj) && 
                        blockedObj is IEnumerable<string> blocked)
                    {
                        patterns.AddRange(blocked);
                    }

                    foreach (var pattern in patterns)
                    {
                        if (ContainsRegexMetacharacters(pattern))
                        {
                            try
                            {
                                _ = new Regex(pattern);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Invalid regex pattern '{pattern}': {ex.Message}");
                            }
                        }
                    }
                }

                // Warn if both whitelist and blacklist are empty
                bool hasAllowed = configuration.ContainsKey("AllowedSources");
                bool hasBlocked = configuration.ContainsKey("BlockedSources");
                if (!hasAllowed && !hasBlocked)
                {
                    warnings.Add("No sources configured - filter will have no effect");
                }
            }

            return errors.AsValueEnumerable().Any()
                ? FilterValidationResult.Invalid(errors, warnings)
                : FilterValidationResult.Valid();
        }

        /// <summary>
        /// Core implementation of filter reset.
        /// </summary>
        protected override void ResetCore(Guid correlationId)
        {
            ClearSources();
        }

        /// <summary>
        /// Core implementation of resource disposal.
        /// </summary>
        protected override void DisposeCore()
        {
            ClearSources();
        }

        /// <summary>
        /// Checks if a source is in the specified list or matches patterns.
        /// </summary>
        private bool IsSourceInList(FixedString64Bytes source, HashSet<FixedString64Bytes> exactSources, List<string> patterns)
        {
            // Check exact match first
            if (exactSources.Contains(source))
                return true;

            // Check patterns
            if (patterns.Count > 0)
            {
                var sourceStr = source.ToString();
                if (!_caseSensitive)
                    sourceStr = sourceStr.ToLowerInvariant();

                foreach (var pattern in patterns)
                {
                    if (MatchesPattern(sourceStr, pattern))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a source matches a pattern.
        /// </summary>
        private bool MatchesPattern(string source, string pattern)
        {
            if (_useRegex && _regexCache.TryGetValue(pattern, out var regex))
            {
                return regex.IsMatch(source);
            }
            else
            {
                // Simple wildcard matching
                return MatchesWildcard(source, pattern);
            }
        }

        /// <summary>
        /// Simple wildcard pattern matching.
        /// </summary>
        private bool MatchesWildcard(string source, string pattern)
        {
            if (!_caseSensitive)
            {
                source = source.ToLowerInvariant();
                pattern = pattern.ToLowerInvariant();
            }

            // Convert wildcard pattern to regex pattern
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            return Regex.IsMatch(source, regexPattern);
        }

        /// <summary>
        /// Checks if a string contains regex metacharacters.
        /// </summary>
        private bool ContainsRegexMetacharacters(string pattern)
        {
            return pattern != null && Regex.IsMatch(pattern, @"[.+^${}()|[\]\\]");
        }

        /// <summary>
        /// Rebuilds the regex cache with current settings.
        /// </summary>
        private void RebuildRegexCache()
        {
            _regexCache.Clear();
            
            if (!_useRegex) return;

            var options = _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var allPatterns = _allowedPatterns.AsValueEnumerable().Concat(_blockedPatterns).Distinct();

            foreach (var pattern in allPatterns)
            {
                if (ContainsRegexMetacharacters(pattern))
                {
                    try
                    {
                        _regexCache[pattern] = new Regex(pattern, options | RegexOptions.Compiled);
                    }
                    catch
                    {
                        // Invalid regex, skip
                    }
                }
            }
        }

        /// <summary>
        /// Gets diagnostic information about current source filters.
        /// </summary>
        public Dictionary<string, object> GetSourceFilterDiagnostics()
        {
            lock (_sourcesLock)
            {
                return new Dictionary<string, object>
                {
                    ["Mode"] = _useWhitelist ? "Whitelist" : "Blacklist",
                    ["AllowedSourcesCount"] = _allowedSources.Count,
                    ["BlockedSourcesCount"] = _blockedSources.Count,
                    ["AllowedPatternsCount"] = _allowedPatterns.Count,
                    ["BlockedPatternsCount"] = _blockedPatterns.Count,
                    ["CaseSensitive"] = _caseSensitive,
                    ["UseRegex"] = _useRegex,
                    ["AllowEmptySource"] = _allowEmptySource,
                    ["RegexCacheSize"] = _regexCache.Count
                };
            }
        }
    }
}