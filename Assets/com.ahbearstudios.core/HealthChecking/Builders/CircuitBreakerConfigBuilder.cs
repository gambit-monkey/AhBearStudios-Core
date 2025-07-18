﻿using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.HealthChecking.Builders
{
    /// <summary>
    /// Production-ready builder for CircuitBreakerConfig with comprehensive fault tolerance options
    /// </summary>
    public sealed class CircuitBreakerConfigBuilder
    {
        private readonly ILoggingService _logger;
        private readonly List<string> _validationErrors = new();
        
        // Core configuration properties
        private FixedString64Bytes _id = GenerateId();
        private string _name = "Default Circuit Breaker";
        private int _failureThreshold = 5;
        private TimeSpan _timeout = TimeSpan.FromSeconds(60);
        private TimeSpan _samplingDuration = TimeSpan.FromMinutes(2);
        private int _minimumThroughput = 10;
        private double _successThreshold = 50.0;
        private int _halfOpenMaxCalls = 3;
        
        // Sliding window configuration
        private bool _useSlidingWindow = true;
        private SlidingWindowType _slidingWindowType = SlidingWindowType.CountBased;
        private int _slidingWindowSize = 100;
        private TimeSpan _slidingWindowDuration = TimeSpan.FromMinutes(1);
        
        // Recovery configuration
        private bool _enableAutomaticRecovery = true;
        private int _maxRecoveryAttempts = 5;
        private double _timeoutMultiplier = 1.5;
        private TimeSpan _maxTimeout = TimeSpan.FromMinutes(10);
        
        // Exception handling
        private HashSet<Type> _ignoredExceptions = new()
        {
            typeof(ArgumentException),
            typeof(ArgumentNullException),
            typeof(InvalidOperationException)
        };
        private HashSet<Type> _immediateFailureExceptions = new()
        {
            typeof(UnauthorizedAccessException),
            typeof(System.Security.SecurityException)
        };
        private List<Func<Exception, bool>> _failurePredicates = new();
        
        // Monitoring and events
        private bool _enableMetrics = true;
        private bool _enableEvents = true;
        private HashSet<FixedString64Bytes> _tags = new();
        private Dictionary<string, object> _metadata = new();
        
        // Advanced features
        private SlowCallConfig _slowCallConfig = new();
        private BulkheadConfig _bulkheadConfig = new();
        private RateLimitConfig _rateLimitConfig = new();
        private FailoverConfig _failoverConfig = new();
        
        // Build state tracking
        private bool _isBuilt = false;
        private bool _isValidated = false;

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerConfigBuilder class
        /// </summary>
        /// <param name="logger">Logging service for build operations</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public CircuitBreakerConfigBuilder(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("CircuitBreakerConfigBuilder initialized");
        }

        /// <summary>
        /// Sets the name for the circuit breaker
        /// </summary>
        /// <param name="name">Circuit breaker name (required)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or empty</exception>
        public CircuitBreakerConfigBuilder WithName(string name)
        {
            ThrowIfAlreadyBuilt();
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(name));
            
            _name = name.Trim();
            _logger.LogDebug($"Set circuit breaker name to '{_name}'");
            return this;
        }

        /// <summary>
        /// Sets the failure threshold for opening the circuit
        /// </summary>
        /// <param name="threshold">Number of consecutive failures (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is not positive</exception>
        public CircuitBreakerConfigBuilder WithFailureThreshold(int threshold)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Failure threshold must be positive");
            
            if (threshold > 1000)
                _logger.LogWarning($"Very high failure threshold: {threshold}. Consider if this is intended.");
            
            _failureThreshold = threshold;
            _logger.LogDebug($"Set failure threshold to {threshold}");
            return this;
        }

        /// <summary>
        /// Sets the timeout before transitioning from open to half-open
        /// </summary>
        /// <param name="timeout">Timeout duration (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is not positive</exception>
        public CircuitBreakerConfigBuilder WithTimeout(TimeSpan timeout)
        {
            ThrowIfAlreadyBuilt();
            
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive");
            
            if (timeout > TimeSpan.FromHours(1))
                _logger.LogWarning($"Very long timeout: {timeout}. This may delay recovery significantly.");
            
            _timeout = timeout;
            _logger.LogDebug($"Set timeout to {timeout}");
            return this;
        }

        /// <summary>
        /// Sets the sampling duration for failure rate calculation
        /// </summary>
        /// <param name="duration">Sampling duration (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when duration is not positive</exception>
        public CircuitBreakerConfigBuilder WithSamplingDuration(TimeSpan duration)
        {
            ThrowIfAlreadyBuilt();
            
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Sampling duration must be positive");
            
            _samplingDuration = duration;
            _logger.LogDebug($"Set sampling duration to {duration}");
            return this;
        }

        /// <summary>
        /// Sets the minimum throughput required before circuit can open
        /// </summary>
        /// <param name="minimum">Minimum number of requests (must be non-negative)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minimum is negative</exception>
        public CircuitBreakerConfigBuilder WithMinimumThroughput(int minimum)
        {
            ThrowIfAlreadyBuilt();
            
            if (minimum < 0)
                throw new ArgumentOutOfRangeException(nameof(minimum), "Minimum throughput must be non-negative");
            
            _minimumThroughput = minimum;
            _logger.LogDebug($"Set minimum throughput to {minimum}");
            return this;
        }

        /// <summary>
        /// Sets the success threshold for closing circuit from half-open
        /// </summary>
        /// <param name="threshold">Success rate percentage (0-100)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is out of range</exception>
        public CircuitBreakerConfigBuilder WithSuccessThreshold(double threshold)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold < 0.0 || threshold > 100.0)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Success threshold must be between 0 and 100");
            
            _successThreshold = threshold;
            _logger.LogDebug($"Set success threshold to {threshold}%");
            return this;
        }

        /// <summary>
        /// Sets the maximum calls allowed in half-open state
        /// </summary>
        /// <param name="maxCalls">Maximum calls (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxCalls is not positive</exception>
        public CircuitBreakerConfigBuilder WithHalfOpenMaxCalls(int maxCalls)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxCalls <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCalls), "Half-open max calls must be positive");
            
            if (maxCalls > 100)
                _logger.LogWarning($"High half-open max calls: {maxCalls}. This may affect recovery time.");
            
            _halfOpenMaxCalls = maxCalls;
            _logger.LogDebug($"Set half-open max calls to {maxCalls}");
            return this;
        }

        /// <summary>
        /// Configures sliding window settings
        /// </summary>
        /// <param name="enabled">Whether to use sliding window</param>
        /// <param name="type">Type of sliding window</param>
        /// <param name="size">Window size (for count-based) or duration (for time-based)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when type is invalid</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is not positive</exception>
        public CircuitBreakerConfigBuilder WithSlidingWindow(
            bool enabled = true,
            SlidingWindowType type = SlidingWindowType.CountBased,
            int size = 100)
        {
            ThrowIfAlreadyBuilt();
            
            if (!Enum.IsDefined(typeof(SlidingWindowType), type))
                throw new ArgumentException($"Invalid sliding window type: {type}", nameof(type));
            
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Sliding window size must be positive");
            
            _useSlidingWindow = enabled;
            _slidingWindowType = type;
            _slidingWindowSize = size;
            
            _logger.LogDebug($"Sliding window: enabled={enabled}, type={type}, size={size}");
            return this;
        }

        /// <summary>
        /// Sets the duration for time-based sliding windows
        /// </summary>
        /// <param name="duration">Window duration (must be positive)</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when duration is not positive</exception>
        public CircuitBreakerConfigBuilder WithSlidingWindowDuration(TimeSpan duration)
        {
            ThrowIfAlreadyBuilt();
            
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Sliding window duration must be positive");
            
            _slidingWindowDuration = duration;
            _logger.LogDebug($"Set sliding window duration to {duration}");
            return this;
        }

        /// <summary>
        /// Configures automatic recovery settings
        /// </summary>
        /// <param name="enabled">Whether to enable automatic recovery</param>
        /// <param name="maxAttempts">Maximum recovery attempts</param>
        /// <param name="timeoutMultiplier">Multiplier for extending timeout on failures</param>
        /// <param name="maxTimeout">Maximum timeout value</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public CircuitBreakerConfigBuilder WithAutomaticRecovery(
            bool enabled = true,
            int maxAttempts = 5,
            double timeoutMultiplier = 1.5,
            TimeSpan? maxTimeout = null)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max recovery attempts must be non-negative");
            
            if (timeoutMultiplier < 1.0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMultiplier), "Timeout multiplier must be at least 1.0");
            
            var maxTimeoutValue = maxTimeout ?? TimeSpan.FromMinutes(10);
            
            if (maxTimeoutValue < _timeout)
                throw new ArgumentOutOfRangeException(nameof(maxTimeout), "Max timeout must be greater than or equal to base timeout");
            
            _enableAutomaticRecovery = enabled;
            _maxRecoveryAttempts = maxAttempts;
            _timeoutMultiplier = timeoutMultiplier;
            _maxTimeout = maxTimeoutValue;
            
            _logger.LogDebug($"Automatic recovery: enabled={enabled}, maxAttempts={maxAttempts}, multiplier={timeoutMultiplier}");
            return this;
        }

        /// <summary>
        /// Adds exceptions to ignore (won't count as failures)
        /// </summary>
        /// <param name="exceptionTypes">Exception types to ignore</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when exceptionTypes is null</exception>
        /// <exception cref="ArgumentException">Thrown when any type is not an exception type</exception>
        public CircuitBreakerConfigBuilder WithIgnoredExceptions(params Type[] exceptionTypes)
        {
            ThrowIfAlreadyBuilt();
            
            if (exceptionTypes == null)
                throw new ArgumentNullException(nameof(exceptionTypes));
            
            foreach (var type in exceptionTypes)
            {
                if (!typeof(Exception).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type.Name} is not derived from Exception", nameof(exceptionTypes));
                
                _ignoredExceptions.Add(type);
            }
            
            _logger.LogDebug($"Added {exceptionTypes.Length} ignored exception types");
            return this;
        }

        /// <summary>
        /// Adds exceptions that immediately open the circuit
        /// </summary>
        /// <param name="exceptionTypes">Exception types that cause immediate failure</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when exceptionTypes is null</exception>
        /// <exception cref="ArgumentException">Thrown when any type is not an exception type</exception>
        public CircuitBreakerConfigBuilder WithImmediateFailureExceptions(params Type[] exceptionTypes)
        {
            ThrowIfAlreadyBuilt();
            
            if (exceptionTypes == null)
                throw new ArgumentNullException(nameof(exceptionTypes));
            
            foreach (var type in exceptionTypes)
            {
                if (!typeof(Exception).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type.Name} is not derived from Exception", nameof(exceptionTypes));
                
                _immediateFailureExceptions.Add(type);
            }
            
            _logger.LogDebug($"Added {exceptionTypes.Length} immediate failure exception types");
            return this;
        }

        /// <summary>
        /// Adds custom failure predicates for determining failures
        /// </summary>
        /// <param name="predicates">Custom failure predicate functions</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicates is null</exception>
        public CircuitBreakerConfigBuilder WithFailurePredicates(params Func<Exception, bool>[] predicates)
        {
            ThrowIfAlreadyBuilt();
            
            if (predicates == null)
                throw new ArgumentNullException(nameof(predicates));
            
            _failurePredicates.AddRange(predicates);
            _logger.LogDebug($"Added {predicates.Length} custom failure predicates");
            return this;
        }

        /// <summary>
        /// Configures monitoring and events
        /// </summary>
        /// <param name="enableMetrics">Whether to enable metrics collection</param>
        /// <param name="enableEvents">Whether to enable event notifications</param>
        /// <returns>Builder instance for method chaining</returns>
        public CircuitBreakerConfigBuilder WithMonitoring(bool enableMetrics = true, bool enableEvents = true)
        {
            ThrowIfAlreadyBuilt();
            
            _enableMetrics = enableMetrics;
            _enableEvents = enableEvents;
            _logger.LogDebug($"Monitoring: metrics={enableMetrics}, events={enableEvents}");
            return this;
        }

        /// <summary>
        /// Adds tags for categorizing the circuit breaker
        /// </summary>
        /// <param name="tags">Tags to add</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown when tags is null</exception>
        public CircuitBreakerConfigBuilder WithTags(params FixedString64Bytes[] tags)
        {
            ThrowIfAlreadyBuilt();
            
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            
            foreach (var tag in tags)
            {
                _tags.Add(tag);
            }
            
            _logger.LogDebug($"Added {tags.Length} tags");
            return this;
        }

        /// <summary>
        /// Adds metadata to the circuit breaker
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when key is null or empty</exception>
        public CircuitBreakerConfigBuilder WithMetadata(string key, object value)
        {
            ThrowIfAlreadyBuilt();
            
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
            
            _metadata[key] = value;
            _logger.LogDebug($"Added metadata: {key}");
            return this;
        }

        /// <summary>
        /// Configures slow call detection
        /// </summary>
        /// <param name="threshold">Duration threshold for slow calls</param>
        /// <param name="rateThreshold">Percentage of slow calls that triggers action</param>
        /// <param name="minimumCalls">Minimum slow calls before evaluation</param>
        /// <param name="treatAsFailures">Whether to treat slow calls as failures</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public CircuitBreakerConfigBuilder WithSlowCallDetection(
            TimeSpan threshold,
            double rateThreshold = 50.0,
            int minimumCalls = 5,
            bool treatAsFailures = true)
        {
            ThrowIfAlreadyBuilt();
            
            if (threshold <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Slow call threshold must be positive");
            
            if (rateThreshold < 0.0 || rateThreshold > 100.0)
                throw new ArgumentOutOfRangeException(nameof(rateThreshold), "Rate threshold must be between 0 and 100");
            
            if (minimumCalls < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumCalls), "Minimum calls must be non-negative");
            
            _slowCallConfig = new SlowCallConfig
            {
                SlowCallDurationThreshold = threshold,
                SlowCallRateThreshold = rateThreshold,
                MinimumSlowCalls = minimumCalls,
                TreatSlowCallsAsFailures = treatAsFailures
            };
            
            _logger.LogDebug($"Slow call detection: threshold={threshold}, rate={rateThreshold}%, min={minimumCalls}");
            return this;
        }

        /// <summary>
        /// Configures bulkhead isolation
        /// </summary>
        /// <param name="enabled">Whether to enable bulkhead</param>
        /// <param name="maxConcurrentCalls">Maximum concurrent calls</param>
        /// <param name="maxWaitDuration">Maximum wait time for a call slot</param>
        /// <param name="maxQueueSize">Maximum queue size for waiting calls</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public CircuitBreakerConfigBuilder WithBulkhead(
            bool enabled = false,
            int maxConcurrentCalls = 10,
            TimeSpan? maxWaitDuration = null,
            int maxQueueSize = 100)
        {
            ThrowIfAlreadyBuilt();
            
            if (maxConcurrentCalls <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentCalls), "Max concurrent calls must be positive");
            
            var waitDuration = maxWaitDuration ?? TimeSpan.FromSeconds(5);
            
            if (waitDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxWaitDuration), "Max wait duration must be non-negative");
            
            if (maxQueueSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxQueueSize), "Max queue size must be non-negative");
            
            _bulkheadConfig = new BulkheadConfig
            {
                Enabled = enabled,
                MaxConcurrentCalls = maxConcurrentCalls,
                MaxWaitDuration = waitDuration,
                MaxQueueSize = maxQueueSize
            };
            
            _logger.LogDebug($"Bulkhead: enabled={enabled}, maxCalls={maxConcurrentCalls}, wait={waitDuration}");
            return this;
        }

        /// <summary>
        /// Configures rate limiting
        /// </summary>
        /// <param name="enabled">Whether to enable rate limiting</param>
        /// <param name="requestsPerSecond">Maximum requests per second</param>
        /// <param name="burstSize">Burst allowance</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public CircuitBreakerConfigBuilder WithRateLimit(
            bool enabled = false,
            double requestsPerSecond = 100.0,
            int burstSize = 150)
        {
            ThrowIfAlreadyBuilt();
            
            if (requestsPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestsPerSecond), "Requests per second must be positive");
            
            if (burstSize < requestsPerSecond)
                throw new ArgumentOutOfRangeException(nameof(burstSize), "Burst size should be greater than or equal to requests per second");
            
            _rateLimitConfig = new RateLimitConfig
            {
                Enabled = enabled,
                RequestsPerSecond = requestsPerSecond,
                BurstSize = burstSize
            };
            
            _logger.LogDebug($"Rate limit: enabled={enabled}, rps={requestsPerSecond}, burst={burstSize}");
            return this;
        }

        /// <summary>
        /// Configures failover behavior
        /// </summary>
        /// <param name="enabled">Whether to enable failover</param>
        /// <param name="strategy">Failover strategy</param>
        /// <param name="defaultValue">Default value for ReturnDefault strategy</param>
        /// <returns>Builder instance for method chaining</returns>
        /// <exception cref="ArgumentException">Thrown when strategy is invalid</exception>
        public CircuitBreakerConfigBuilder WithFailover(
            bool enabled = false,
            FailoverStrategy strategy = FailoverStrategy.ReturnDefault,
            object defaultValue = null)
        {
            ThrowIfAlreadyBuilt();
            
            if (!Enum.IsDefined(typeof(FailoverStrategy), strategy))
                throw new ArgumentException($"Invalid failover strategy: {strategy}", nameof(strategy));
            
            _failoverConfig = new FailoverConfig
            {
                Enabled = enabled,
                Strategy = strategy,
                DefaultValue = defaultValue
            };
            
            _logger.LogDebug($"Failover: enabled={enabled}, strategy={strategy}");
            return this;
        }

        /// <summary>
        /// Applies a preset configuration for the specified scenario
        /// </summary>
        /// <param name="scenario">Circuit breaker scenario</param>
        /// <returns>Builder instance for method chaining</returns>
        public CircuitBreakerConfigBuilder ForScenario(CircuitBreakerScenario scenario)
        {
            ThrowIfAlreadyBuilt();
            
            switch (scenario)
            {
                case CircuitBreakerScenario.CriticalService:
                    return ApplyCriticalServicePreset();
                
                case CircuitBreakerScenario.Database:
                    return ApplyDatabasePreset();
                
                case CircuitBreakerScenario.NetworkService:
                    return ApplyNetworkServicePreset();
                
                case CircuitBreakerScenario.HighThroughput:
                    return ApplyHighThroughputPreset();
                
                case CircuitBreakerScenario.Development:
                    return ApplyDevelopmentPreset();
                
                default:
                    throw new ArgumentException($"Unknown scenario: {scenario}", nameof(scenario));
            }
        }

        /// <summary>
        /// Validates the current configuration without building
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        public List<string> Validate()
        {
            if (_isValidated)
                return new List<string>(_validationErrors);
            
            _validationErrors.Clear();
            
            // Validate basic configuration
            if (string.IsNullOrWhiteSpace(_name))
                _validationErrors.Add("Name cannot be null or empty");
            
            if (_failureThreshold <= 0)
                _validationErrors.Add("Failure threshold must be positive");
            
            if (_failureThreshold > 1000)
                _validationErrors.Add("Failure threshold should not exceed 1000 for practical purposes");
            
            if (_timeout <= TimeSpan.Zero)
                _validationErrors.Add("Timeout must be positive");
            
            if (_timeout > TimeSpan.FromHours(1))
                _validationErrors.Add("Timeout should not exceed 1 hour for practical recovery");
            
            if (_samplingDuration <= TimeSpan.Zero)
                _validationErrors.Add("Sampling duration must be positive");
            
            if (_minimumThroughput < 0)
                _validationErrors.Add("Minimum throughput must be non-negative");
            
            if (_successThreshold < 0.0 || _successThreshold > 100.0)
                _validationErrors.Add("Success threshold must be between 0 and 100");
            
            if (_halfOpenMaxCalls <= 0)
                _validationErrors.Add("Half-open max calls must be positive");
            
            if (_halfOpenMaxCalls > 100)
                _validationErrors.Add("Half-open max calls should not exceed 100 for efficiency");
            
            if (_slidingWindowSize <= 0)
                _validationErrors.Add("Sliding window size must be positive");
            
            if (_slidingWindowType == SlidingWindowType.TimeBased && _slidingWindowDuration <= TimeSpan.Zero)
                _validationErrors.Add("Sliding window duration must be positive for time-based windows");
            
            if (_maxRecoveryAttempts < 0)
                _validationErrors.Add("Max recovery attempts must be non-negative");
            
            if (_timeoutMultiplier < 1.0)
                _validationErrors.Add("Timeout multiplier must be at least 1.0");
            
            if (_maxTimeout < _timeout)
                _validationErrors.Add("Max timeout must be greater than or equal to base timeout");
            
            // Validate exception configurations
            var conflictingExceptions = _ignoredExceptions.Intersect(_immediateFailureExceptions);
            if (conflictingExceptions.Any())
                _validationErrors.Add($"Exceptions cannot be both ignored and immediate failures: {string.Join(", ", conflictingExceptions.Select(t => t.Name))}");
            
            // Validate nested configurations
            _validationErrors.AddRange(_slowCallConfig.Validate());
            _validationErrors.AddRange(_bulkheadConfig.Validate());
            _validationErrors.AddRange(_rateLimitConfig.Validate());
            _validationErrors.AddRange(_failoverConfig.Validate());
            
            _isValidated = true;
            
            if (_validationErrors.Count > 0)
            {
                _logger.LogWarning($"Configuration validation found {_validationErrors.Count} errors");
            }
            else
            {
                _logger.LogDebug("Configuration validation passed");
            }
            
            return new List<string>(_validationErrors);
        }

        /// <summary>
        /// Builds the CircuitBreakerConfig instance
        /// </summary>
        /// <returns>Configured CircuitBreakerConfig instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or already built</exception>
        public CircuitBreakerConfig Build()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Configuration has already been built. Create a new builder instance.");
            
            var validationErrors = Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Cannot build invalid configuration. Errors: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            var config = new CircuitBreakerConfig
            {
                Id = _id,
                Name = _name,
                FailureThreshold = _failureThreshold,
                Timeout = _timeout,
                SamplingDuration = _samplingDuration,
                MinimumThroughput = _minimumThroughput,
                SuccessThreshold = _successThreshold,
                HalfOpenMaxCalls = _halfOpenMaxCalls,
                UseSlidingWindow = _useSlidingWindow,
                SlidingWindowType = _slidingWindowType,
                SlidingWindowSize = _slidingWindowSize,
                SlidingWindowDuration = _slidingWindowDuration,
                EnableAutomaticRecovery = _enableAutomaticRecovery,
                MaxRecoveryAttempts = _maxRecoveryAttempts,
                TimeoutMultiplier = _timeoutMultiplier,
                MaxTimeout = _maxTimeout,
                IgnoredExceptions = new HashSet<Type>(_ignoredExceptions),
                ImmediateFailureExceptions = new HashSet<Type>(_immediateFailureExceptions),
                FailurePredicates = new List<Func<Exception, bool>>(_failurePredicates),
                EnableMetrics = _enableMetrics,
                EnableEvents = _enableEvents,
                Tags = new HashSet<FixedString64Bytes>(_tags),
                Metadata = new Dictionary<string, object>(_metadata),
                SlowCallConfig = _slowCallConfig,
                BulkheadConfig = _bulkheadConfig,
                RateLimitConfig = _rateLimitConfig,
                FailoverConfig = _failoverConfig
            };
            
            _isBuilt = true;
            _logger.LogInfo($"CircuitBreakerConfig '{_name}' built successfully");
            
            return config;
        }

        /// <summary>
        /// Resets the builder to allow building a new configuration
        /// </summary>
        /// <returns>Builder instance for method chaining</returns>
        public CircuitBreakerConfigBuilder Reset()
        {
            _isBuilt = false;
            _isValidated = false;
            _validationErrors.Clear();
            _id = GenerateId();
            _logger.LogDebug("Builder reset for new configuration");
            return this;
        }

        #region Private Methods

        /// <summary>
        /// Applies critical service preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private CircuitBreakerConfigBuilder ApplyCriticalServicePreset()
        {
            _name = "Critical Service Circuit Breaker";
            _failureThreshold = 3;
            _timeout = TimeSpan.FromSeconds(30);
            _samplingDuration = TimeSpan.FromMinutes(1);
            _minimumThroughput = 5;
            _successThreshold = 80.0;
            _halfOpenMaxCalls = 2;
            _useSlidingWindow = true;
            _slidingWindowType = SlidingWindowType.CountBased;
            _slidingWindowSize = 50;
            _enableAutomaticRecovery = true;
            _maxRecoveryAttempts = 3;
            _timeoutMultiplier = 2.0;
            _maxTimeout = TimeSpan.FromMinutes(5);
            _enableMetrics = true;
            _enableEvents = true;
            _slowCallConfig = new SlowCallConfig
            {
                SlowCallDurationThreshold = TimeSpan.FromSeconds(5),
                SlowCallRateThreshold = 50.0,
                MinimumSlowCalls = 3
            };
            
            _logger.LogInfo("Applied critical service preset");
            return this;
        }

        /// <summary>
        /// Applies database preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private CircuitBreakerConfigBuilder ApplyDatabasePreset()
        {
            _name = "Database Circuit Breaker";
            _failureThreshold = 5;
            _timeout = TimeSpan.FromMinutes(2);
            _samplingDuration = TimeSpan.FromMinutes(5);
            _minimumThroughput = 10;
            _successThreshold = 60.0;
            _halfOpenMaxCalls = 5;
            _useSlidingWindow = true;
            _slidingWindowType = SlidingWindowType.TimeBased;
            _slidingWindowDuration = TimeSpan.FromMinutes(2);
            _enableAutomaticRecovery = true;
            _maxRecoveryAttempts = 5;
            _timeoutMultiplier = 1.5;
            _maxTimeout = TimeSpan.FromMinutes(15);
            _immediateFailureExceptions.Add(typeof(UnauthorizedAccessException));
            _slowCallConfig = new SlowCallConfig
            {
                SlowCallDurationThreshold = TimeSpan.FromSeconds(10),
                SlowCallRateThreshold = 30.0,
                MinimumSlowCalls = 5
            };
            _bulkheadConfig = new BulkheadConfig
            {
                MaxConcurrentCalls = 20,
                MaxWaitDuration = TimeSpan.FromSeconds(30)
            };
            
            _logger.LogInfo("Applied database preset");
            return this;
        }

        /// <summary>
        /// Applies network service preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private CircuitBreakerConfigBuilder ApplyNetworkServicePreset()
        {
            _name = "Network Service Circuit Breaker";
            _failureThreshold = 8;
            _timeout = TimeSpan.FromMinutes(1);
            _samplingDuration = TimeSpan.FromMinutes(3);
            _minimumThroughput = 15;
            _successThreshold = 70.0;
            _halfOpenMaxCalls = 3;
            _useSlidingWindow = true;
            _slidingWindowType = SlidingWindowType.CountBased;
            _slidingWindowSize = 100;
            _enableAutomaticRecovery = true;
            _maxRecoveryAttempts = 10;
            _timeoutMultiplier = 1.2;
            _maxTimeout = TimeSpan.FromMinutes(10);
            _ignoredExceptions.Add(typeof(ArgumentException));
            _ignoredExceptions.Add(typeof(ArgumentNullException));
            _immediateFailureExceptions.Add(typeof(UnauthorizedAccessException));
            _slowCallConfig = new SlowCallConfig
            {
                SlowCallDurationThreshold = TimeSpan.FromSeconds(15),
                SlowCallRateThreshold = 40.0,
                MinimumSlowCalls = 8
            };
            _rateLimitConfig = new RateLimitConfig
            {
                RequestsPerSecond = 100,
                BurstSize = 150
            };
            
            _logger.LogInfo("Applied network service preset");
            return this;
        }

        /// <summary>
        /// Applies high throughput preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private CircuitBreakerConfigBuilder ApplyHighThroughputPreset()
        {
            _name = "High Throughput Circuit Breaker";
            _failureThreshold = 20;
            _timeout = TimeSpan.FromSeconds(30);
            _samplingDuration = TimeSpan.FromMinutes(1);
            _minimumThroughput = 50;
            _successThreshold = 85.0;
            _halfOpenMaxCalls = 10;
            _useSlidingWindow = true;
            _slidingWindowType = SlidingWindowType.CountBased;
            _slidingWindowSize = 1000;
            _enableAutomaticRecovery = true;
            _maxRecoveryAttempts = 3;
            _timeoutMultiplier = 1.1;
            _maxTimeout = TimeSpan.FromMinutes(2);
            _bulkheadConfig = new BulkheadConfig
            {
                MaxConcurrentCalls = 100,
                MaxWaitDuration = TimeSpan.FromSeconds(10)
            };
            _rateLimitConfig = new RateLimitConfig
            {
                RequestsPerSecond = 1000,
                BurstSize = 1500
            };
            
            _logger.LogInfo("Applied high throughput preset");
            return this;
        }

        /// <summary>
        /// Applies development preset configuration
        /// </summary>
        /// <returns>Builder instance</returns>
        private CircuitBreakerConfigBuilder ApplyDevelopmentPreset()
        {
            _name = "Development Circuit Breaker";
            _failureThreshold = 2;
            _timeout = TimeSpan.FromSeconds(10);
            _samplingDuration = TimeSpan.FromSeconds(30);
            _minimumThroughput = 2;
            _successThreshold = 50.0;
            _halfOpenMaxCalls = 1;
            _useSlidingWindow = false;
            _enableAutomaticRecovery = true;
            _maxRecoveryAttempts = 1;
            _timeoutMultiplier = 1.0;
            _maxTimeout = TimeSpan.FromSeconds(30);
            _enableMetrics = true;
            _enableEvents = true;
            
            _logger.LogInfo("Applied development preset");
            return this;
        }

        /// <summary>
        /// Throws exception if configuration has already been built
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when already built</exception>
        private void ThrowIfAlreadyBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Cannot modify configuration after it has been built. Use Reset() or create a new builder.");
        }

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }

        #endregion
    }
}