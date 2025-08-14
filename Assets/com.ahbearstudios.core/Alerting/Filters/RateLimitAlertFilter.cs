using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using AhBearStudios.Core.Alerting.Models;
using ZLinq;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Rate limiting filter using token bucket algorithm for smooth rate limiting.
    /// Supports per-source rate limits with pattern matching and sliding windows.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    public sealed class RateLimitAlertFilter : BaseAlertFilter
    {
        private readonly Dictionary<FixedString64Bytes, RateLimitBucket> _sourceBuckets;
        private readonly object _bucketsLock = new object();
        private readonly ProfilerMarker _evaluateMarker;
        private readonly ProfilerMarker _cleanupMarker;
        
        private int _maxAlertsPerMinute = 60;
        private string _sourcePattern = "*";
        private int _burstSize = 10;
        private TimeSpan _windowSize = TimeSpan.FromMinutes(1);
        private DateTime _lastCleanup = DateTime.UtcNow;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => "RateLimitFilter";

        /// <summary>
        /// Gets or sets the maximum number of alerts allowed per minute.
        /// </summary>
        public int MaxAlertsPerMinute
        {
            get => _maxAlertsPerMinute;
            set => _maxAlertsPerMinute = Math.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the source pattern to match (supports wildcards).
        /// </summary>
        public string SourcePattern
        {
            get => _sourcePattern;
            set => _sourcePattern = value ?? "*";
        }

        /// <summary>
        /// Gets or sets the burst size allowed above the normal rate.
        /// </summary>
        public int BurstSize
        {
            get => _burstSize;
            set => _burstSize = Math.Max(1, value);
        }

        /// <summary>
        /// Initializes a new instance of the RateLimitAlertFilter class.
        /// </summary>
        /// <param name="maxAlertsPerMinute">Maximum alerts allowed per minute</param>
        /// <param name="sourcePattern">Source pattern to match</param>
        public RateLimitAlertFilter(int maxAlertsPerMinute = 60, string sourcePattern = "*")
        {
            _maxAlertsPerMinute = Math.Max(1, maxAlertsPerMinute);
            _sourcePattern = sourcePattern ?? "*";
            _sourceBuckets = new Dictionary<FixedString64Bytes, RateLimitBucket>();
            _evaluateMarker = new ProfilerMarker("RateLimitAlertFilter.Evaluate");
            _cleanupMarker = new ProfilerMarker("RateLimitAlertFilter.Cleanup");
            Priority = 30; // Medium priority for rate limiting
        }

        /// <summary>
        /// Core implementation of alert evaluation.
        /// </summary>
        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            using (_evaluateMarker.Auto())
            {
                // Check if source matches pattern
                if (!MatchesSourcePattern(alert.Source))
                {
                    return FilterResult.Allow("Source does not match rate limit pattern");
                }

                // Perform periodic cleanup
                PerformCleanupIfNeeded();

                lock (_bucketsLock)
                {
                    // Get or create bucket for this source
                    if (!_sourceBuckets.TryGetValue(alert.Source, out var bucket))
                    {
                        bucket = new RateLimitBucket
                        {
                            Source = alert.Source,
                            TokensPerMinute = _maxAlertsPerMinute,
                            BurstSize = _burstSize,
                            LastRefill = DateTime.UtcNow,
                            AvailableTokens = _burstSize
                        };
                        _sourceBuckets[alert.Source] = bucket;
                    }

                    // Refill tokens based on elapsed time
                    RefillTokens(bucket);

                    // Check if we have tokens available
                    if (bucket.AvailableTokens >= 1.0)
                    {
                        // Consume a token
                        bucket.AvailableTokens -= 1.0;
                        bucket.LastAlertTime = DateTime.UtcNow;
                        bucket.AlertCount++;
                        
                        return FilterResult.Allow($"Rate limit passed: {bucket.AvailableTokens:F1} tokens remaining");
                    }
                    else
                    {
                        // Rate limit exceeded
                        bucket.SuppressedCount++;
                        var nextTokenTime = bucket.LastRefill.AddSeconds(60.0 / _maxAlertsPerMinute);
                        var waitTime = nextTokenTime - DateTime.UtcNow;
                        
                        return FilterResult.Suppress(
                            $"Rate limit exceeded for source '{alert.Source}'. " +
                            $"Next token available in {waitTime.TotalSeconds:F1}s");
                    }
                }
            }
        }

        /// <summary>
        /// Core implementation to determine if filter can handle an alert.
        /// </summary>
        protected override bool CanHandleCore(Alert alert)
        {
            // Rate limiting applies to all alerts
            return true;
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            if (configuration == null) return true;

            if (configuration.TryGetValue("MaxAlertsPerMinute", out var maxAlerts))
            {
                if (int.TryParse(maxAlerts.ToString(), out var max))
                {
                    MaxAlertsPerMinute = max;
                }
            }

            if (configuration.TryGetValue("SourcePattern", out var pattern))
            {
                SourcePattern = pattern?.ToString() ?? "*";
            }

            if (configuration.TryGetValue("BurstSize", out var burst))
            {
                if (int.TryParse(burst.ToString(), out var size))
                {
                    BurstSize = size;
                }
            }

            if (configuration.TryGetValue("WindowSize", out var window))
            {
                if (int.TryParse(window.ToString(), out var seconds))
                {
                    _windowSize = TimeSpan.FromSeconds(seconds);
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

            if (configuration != null)
            {
                if (configuration.TryGetValue("MaxAlertsPerMinute", out var maxAlerts))
                {
                    if (!int.TryParse(maxAlerts.ToString(), out var max) || max <= 0)
                    {
                        errors.Add("MaxAlertsPerMinute must be a positive integer");
                    }
                }

                if (configuration.TryGetValue("BurstSize", out var burst))
                {
                    if (!int.TryParse(burst.ToString(), out var size) || size <= 0)
                    {
                        errors.Add("BurstSize must be a positive integer");
                    }
                }

                if (configuration.TryGetValue("WindowSize", out var window))
                {
                    if (!int.TryParse(window.ToString(), out var seconds) || seconds <= 0)
                    {
                        errors.Add("WindowSize must be a positive number of seconds");
                    }
                }
            }

            return errors.AsValueEnumerable().Any()
                ? FilterValidationResult.Invalid(errors)
                : FilterValidationResult.Valid();
        }

        /// <summary>
        /// Core implementation of filter reset.
        /// </summary>
        protected override void ResetCore(Guid correlationId)
        {
            lock (_bucketsLock)
            {
                _sourceBuckets.Clear();
            }
            _lastCleanup = DateTime.UtcNow;
        }

        /// <summary>
        /// Core implementation of resource disposal.
        /// </summary>
        protected override void DisposeCore()
        {
            lock (_bucketsLock)
            {
                _sourceBuckets.Clear();
            }
        }

        /// <summary>
        /// Checks if a source matches the configured pattern.
        /// </summary>
        private bool MatchesSourcePattern(FixedString64Bytes source)
        {
            if (_sourcePattern == "*") return true;
            
            var sourceStr = source.ToString();
            
            // Simple wildcard matching
            if (_sourcePattern.StartsWith("*") && _sourcePattern.EndsWith("*"))
            {
                var middle = _sourcePattern.Substring(1, _sourcePattern.Length - 2);
                return sourceStr.Contains(middle);
            }
            else if (_sourcePattern.StartsWith("*"))
            {
                var suffix = _sourcePattern.Substring(1);
                return sourceStr.EndsWith(suffix);
            }
            else if (_sourcePattern.EndsWith("*"))
            {
                var prefix = _sourcePattern.Substring(0, _sourcePattern.Length - 1);
                return sourceStr.StartsWith(prefix);
            }
            else
            {
                return sourceStr == _sourcePattern;
            }
        }

        /// <summary>
        /// Refills tokens in a bucket based on elapsed time.
        /// </summary>
        private void RefillTokens(RateLimitBucket bucket)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - bucket.LastRefill;
            
            if (elapsed.TotalSeconds > 0)
            {
                // Calculate tokens to add based on rate
                var tokensToAdd = elapsed.TotalMinutes * bucket.TokensPerMinute;
                bucket.AvailableTokens = Math.Min(bucket.BurstSize, bucket.AvailableTokens + tokensToAdd);
                bucket.LastRefill = now;
            }
        }

        /// <summary>
        /// Performs cleanup of old buckets if needed.
        /// </summary>
        private void PerformCleanupIfNeeded()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanup < _cleanupInterval) return;

            using (_cleanupMarker.Auto())
            {
                lock (_bucketsLock)
                {
                    var cutoff = now - TimeSpan.FromMinutes(10);
                    var toRemove = _sourceBuckets
                        .AsValueEnumerable()
                        .Where(kvp => kvp.Value.LastAlertTime < cutoff)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in toRemove)
                    {
                        _sourceBuckets.Remove(key);
                    }
                }
                _lastCleanup = now;
            }
        }

        /// <summary>
        /// Gets diagnostic information about current rate limits.
        /// </summary>
        public Dictionary<string, object> GetRateLimitDiagnostics()
        {
            lock (_bucketsLock)
            {
                return new Dictionary<string, object>
                {
                    ["TotalBuckets"] = _sourceBuckets.Count,
                    ["MaxAlertsPerMinute"] = _maxAlertsPerMinute,
                    ["SourcePattern"] = _sourcePattern,
                    ["BurstSize"] = _burstSize,
                    ["ActiveSources"] = _sourceBuckets.Keys.AsValueEnumerable().Select(k => k.ToString()).ToList()
                };
            }
        }
    }
}