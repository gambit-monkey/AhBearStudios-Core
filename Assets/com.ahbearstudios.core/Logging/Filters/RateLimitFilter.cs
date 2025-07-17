using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Filters
{
    /// <summary>
    /// Filter that evaluates log entries based on rate limiting.
    /// Provides rate limiting to prevent log flooding and manage system resources.
    /// Supports different rate limiting strategies including token bucket and sliding window.
    /// </summary>
    public sealed class RateLimitFilter : ILogFilter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly FilterStatistics _statistics;
        private readonly object _lock = new object();
        private bool _isEnabled = true;
        private int _rateLimit = 0;
        private int _rateLimitWindow = 60;
        private RateLimitStrategy _strategy = RateLimitStrategy.TokenBucket;
        private long _lastResetTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private volatile int _currentCount = 0;
        private volatile int _droppedCount = 0;

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
        /// Gets the rate limit (messages per window).
        /// </summary>
        public int RateLimit => _rateLimit;

        /// <summary>
        /// Gets the rate limit window in seconds.
        /// </summary>
        public int RateLimitWindow => _rateLimitWindow;

        /// <summary>
        /// Gets the rate limiting strategy.
        /// </summary>
        public RateLimitStrategy Strategy => _strategy;

        /// <summary>
        /// Gets the current count of messages in the current window.
        /// </summary>
        public int CurrentCount => _currentCount;

        /// <summary>
        /// Gets the number of messages dropped due to rate limiting.
        /// </summary>
        public int DroppedCount => _droppedCount;

        /// <summary>
        /// Gets the remaining capacity in the current window.
        /// </summary>
        public int RemainingCapacity => Math.Max(0, _rateLimit - _currentCount);

        /// <summary>
        /// Initializes a new instance of the RateLimitFilter class.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="rateLimit">The rate limit (messages per window)</param>
        /// <param name="rateLimitWindow">The rate limit window in seconds</param>
        /// <param name="strategy">The rate limiting strategy</param>
        /// <param name="priority">The filter priority (default: 50)</param>
        public RateLimitFilter(
            string name = "RateLimitFilter",
            int rateLimit = 0,
            int rateLimitWindow = 60,
            RateLimitStrategy strategy = RateLimitStrategy.TokenBucket,
            int priority = 50)
        {
            Name = name ?? "RateLimitFilter";
            Priority = priority;
            
            _rateLimit = Math.Max(0, rateLimit);
            _rateLimitWindow = Math.Max(1, rateLimitWindow);
            _strategy = strategy;
            var eventsPerSecond = _rateLimitWindow > 0 ? (double)_rateLimit / _rateLimitWindow : 0; 
            _statistics = FilterStatistics.ForCustom("RateLimit", $"Rate: {eventsPerSecond:F1}/sec"); 
            
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["RateLimit"] = _rateLimit,
                ["RateLimitWindow"] = _rateLimitWindow,
                ["Strategy"] = _strategy.ToString(),
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

            // If no rate limit set, allow all
            if (_rateLimit <= 0)
            {
                _statistics.RecordAllowed();
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                bool shouldProcess = false;
                
                switch (_strategy)
                {
                    case RateLimitStrategy.TokenBucket:
                        shouldProcess = TokenBucketCheck();
                        break;
                        
                    case RateLimitStrategy.SlidingWindow:
                        shouldProcess = SlidingWindowCheck();
                        break;
                        
                    case RateLimitStrategy.FixedWindow:
                        shouldProcess = FixedWindowCheck();
                        break;
                        
                    case RateLimitStrategy.LeakyBucket:
                        shouldProcess = LeakyBucketCheck();
                        break;
                        
                    default:
                        shouldProcess = TokenBucketCheck();
                        break;
                }
                
                if (!shouldProcess)
                {
                    Interlocked.Increment(ref _droppedCount);
                }
                
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

            if (_rateLimit < 0)
            {
                errors.Add(new ValidationError("Rate limit cannot be negative", nameof(RateLimit)));
            }

            if (_rateLimitWindow < 1)
            {
                errors.Add(new ValidationError("Rate limit window must be at least 1 second", nameof(RateLimitWindow)));
            }

            if (_rateLimit > 0 && _rateLimit < 10)
            {
                warnings.Add(new ValidationWarning("Low rate limit may cause significant message drops", nameof(RateLimit)));
            }

            if (_rateLimitWindow > 300)
            {
                warnings.Add(new ValidationWarning("Very long rate limit window may cause memory issues", nameof(RateLimitWindow)));
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
            lock (_lock)
            {
                _statistics.Reset();
                _currentCount = 0;
                _droppedCount = 0;
                _lastResetTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
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
                    case "RateLimit":
                        if (setting.Value is int rateLimit)
                            _rateLimit = Math.Max(0, rateLimit);
                        else if (int.TryParse(setting.Value?.ToString(), out var parsedLimit))
                            _rateLimit = Math.Max(0, parsedLimit);
                        break;
                        
                    case "RateLimitWindow":
                        if (setting.Value is int rateLimitWindow)
                            _rateLimitWindow = Math.Max(1, rateLimitWindow);
                        else if (int.TryParse(setting.Value?.ToString(), out var parsedWindow))
                            _rateLimitWindow = Math.Max(1, parsedWindow);
                        break;
                        
                    case "Strategy":
                        if (setting.Value is RateLimitStrategy strategy)
                            _strategy = strategy;
                        else if (Enum.TryParse<RateLimitStrategy>(setting.Value?.ToString(), out var parsedStrategy))
                            _strategy = parsedStrategy;
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
            _settings["RateLimit"] = _rateLimit;
            _settings["RateLimitWindow"] = _rateLimitWindow;
            _settings["Strategy"] = _strategy.ToString();
            _settings["IsEnabled"] = _isEnabled;
            
            return _settings;
        }

        /// <summary>
        /// Performs token bucket rate limiting check.
        /// </summary>
        /// <returns>True if the entry should be processed</returns>
        private bool TokenBucketCheck()
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var elapsed = now - _lastResetTime;
                
                // Refill tokens based on elapsed time
                if (elapsed >= _rateLimitWindow)
                {
                    _currentCount = 0;
                    _lastResetTime = now;
                }
                
                if (_currentCount < _rateLimit)
                {
                    _currentCount++;
                    return true;
                }
                
                return false;
            }
        }

        /// <summary>
        /// Performs sliding window rate limiting check.
        /// </summary>
        /// <returns>True if the entry should be processed</returns>
        private bool SlidingWindowCheck()
        {
            // For simplicity, this implementation uses a fixed window approach
            // A full sliding window would require tracking individual timestamps
            return FixedWindowCheck();
        }

        /// <summary>
        /// Performs fixed window rate limiting check.
        /// </summary>
        /// <returns>True if the entry should be processed</returns>
        private bool FixedWindowCheck()
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var windowStart = (now / _rateLimitWindow) * _rateLimitWindow;
                
                // Reset if we're in a new window
                if (windowStart > _lastResetTime)
                {
                    _currentCount = 0;
                    _lastResetTime = windowStart;
                }
                
                if (_currentCount < _rateLimit)
                {
                    _currentCount++;
                    return true;
                }
                
                return false;
            }
        }

        /// <summary>
        /// Performs leaky bucket rate limiting check.
        /// </summary>
        /// <returns>True if the entry should be processed</returns>
        private bool LeakyBucketCheck()
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var elapsed = now - _lastResetTime;
                
                // Calculate leak rate (messages per second)
                var leakRate = (double)_rateLimit / _rateLimitWindow;
                var leaked = (int)(elapsed * leakRate);
                
                // Leak messages from the bucket
                if (leaked > 0)
                {
                    _currentCount = Math.Max(0, _currentCount - leaked);
                    _lastResetTime = now;
                }
                
                if (_currentCount < _rateLimit)
                {
                    _currentCount++;
                    return true;
                }
                
                return false;
            }
        }

        /// <summary>
        /// Creates a RateLimitFilter configured with token bucket strategy.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="rateLimit">The rate limit (messages per window)</param>
        /// <param name="window">The rate limit window in seconds</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured RateLimitFilter instance</returns>
        public static RateLimitFilter TokenBucket(string name, int rateLimit, int window = 60, int priority = 50)
        {
            return new RateLimitFilter(name, rateLimit, window, RateLimitStrategy.TokenBucket, priority);
        }

        /// <summary>
        /// Creates a RateLimitFilter configured with fixed window strategy.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="rateLimit">The rate limit (messages per window)</param>
        /// <param name="window">The rate limit window in seconds</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured RateLimitFilter instance</returns>
        public static RateLimitFilter FixedWindow(string name, int rateLimit, int window = 60, int priority = 50)
        {
            return new RateLimitFilter(name, rateLimit, window, RateLimitStrategy.FixedWindow, priority);
        }

        /// <summary>
        /// Creates a RateLimitFilter configured with leaky bucket strategy.
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="rateLimit">The rate limit (messages per window)</param>
        /// <param name="window">The rate limit window in seconds</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured RateLimitFilter instance</returns>
        public static RateLimitFilter LeakyBucket(string name, int rateLimit, int window = 60, int priority = 50)
        {
            return new RateLimitFilter(name, rateLimit, window, RateLimitStrategy.LeakyBucket, priority);
        }

        /// <summary>
        /// Creates a RateLimitFilter configured for high-frequency limiting (100 messages per second).
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured RateLimitFilter instance</returns>
        public static RateLimitFilter HighFrequency(string name = "HighFrequencyLimit", int priority = 50)
        {
            return new RateLimitFilter(name, 100, 1, RateLimitStrategy.TokenBucket, priority);
        }

        /// <summary>
        /// Creates a RateLimitFilter configured for burst limiting (1000 messages per minute).
        /// </summary>
        /// <param name="name">The filter name</param>
        /// <param name="priority">The filter priority</param>
        /// <returns>A configured RateLimitFilter instance</returns>
        public static RateLimitFilter BurstLimit(string name = "BurstLimit", int priority = 50)
        {
            return new RateLimitFilter(name, 1000, 60, RateLimitStrategy.TokenBucket, priority);
        }
    }

    /// <summary>
    /// Defines the rate limiting strategy for the RateLimitFilter.
    /// </summary>
    public enum RateLimitStrategy
    {
        /// <summary>
        /// Token bucket algorithm - allows bursts up to the bucket capacity.
        /// </summary>
        TokenBucket,

        /// <summary>
        /// Sliding window algorithm - maintains a rolling window of requests.
        /// </summary>
        SlidingWindow,

        /// <summary>
        /// Fixed window algorithm - resets the count at fixed intervals.
        /// </summary>
        FixedWindow,

        /// <summary>
        /// Leaky bucket algorithm - processes requests at a constant rate.
        /// </summary>
        LeakyBucket
    }
}