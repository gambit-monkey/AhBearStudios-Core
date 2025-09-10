using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Comprehensive configuration for circuit breaker behavior with advanced fault tolerance settings.
    /// Enhanced with bulkhead isolation and failover capabilities.
    /// Designed for Unity game development with performance-first approach.
    /// </summary>
    public sealed record CircuitBreakerConfig : ICircuitBreakerConfig
    {
        /// <summary>
        /// Unique identifier for this circuit breaker configuration
        /// </summary>
        public FixedString64Bytes Id { get; init; }

        /// <summary>
        /// Display name for this circuit breaker configuration
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Number of consecutive failures required to open the circuit
        /// </summary>
        public int FailureThreshold { get; init; }

        /// <summary>
        /// Time to wait in open state before transitioning to half-open
        /// </summary>
        public TimeSpan Timeout { get; init; }

        /// <summary>
        /// Duration to collect failure statistics for threshold calculation
        /// </summary>
        public TimeSpan SamplingDuration { get; init; }

        /// <summary>
        /// Minimum number of requests in sampling period before circuit can open
        /// </summary>
        public int MinimumThroughput { get; init; }

        /// <summary>
        /// Success threshold percentage (0-100) required to close circuit from half-open
        /// </summary>
        public double SuccessThreshold { get; init; }

        /// <summary>
        /// Number of test requests allowed in half-open state
        /// </summary>
        public int HalfOpenMaxCalls { get; init; }

        /// <summary>
        /// Whether to use sliding window for failure rate calculation
        /// </summary>
        public bool UseSlidingWindow { get; init; }

        /// <summary>
        /// Type of sliding window to use
        /// </summary>
        public SlidingWindowType SlidingWindowType { get; init; }

        /// <summary>
        /// Size of the sliding window (requests for count-based, duration for time-based)
        /// </summary>
        public int SlidingWindowSize { get; init; }

        /// <summary>
        /// Time-based sliding window duration (used when SlidingWindowType is TimeBased)
        /// </summary>
        public TimeSpan SlidingWindowDuration { get; init; }

        /// <summary>
        /// Whether to automatically attempt recovery when circuit is open
        /// </summary>
        public bool EnableAutomaticRecovery { get; init; }

        /// <summary>
        /// Maximum number of automatic recovery attempts
        /// </summary>
        public int MaxRecoveryAttempts { get; init; }

        /// <summary>
        /// Multiplier for extending timeout on repeated failures
        /// </summary>
        public double TimeoutMultiplier { get; init; }

        /// <summary>
        /// Maximum timeout value to prevent indefinite waiting
        /// </summary>
        public TimeSpan MaxTimeout { get; init; }

        /// <summary>
        /// Types of exceptions that should be ignored by the circuit breaker
        /// </summary>
        public HashSet<Type> IgnoredExceptions { get; init; } = new()
        {
            typeof(ArgumentException),
            typeof(ArgumentNullException),
            typeof(InvalidOperationException)
        };

        /// <summary>
        /// Types of exceptions that should immediately open the circuit
        /// </summary>
        public HashSet<Type> ImmediateFailureExceptions { get; init; } = new()
        {
            typeof(UnauthorizedAccessException),
            typeof(System.Security.SecurityException)
        };

        /// <summary>
        /// Custom failure predicates for determining if an exception counts as a failure
        /// </summary>
        public List<Func<Exception, bool>> FailurePredicates { get; init; } = new();

        /// <summary>
        /// Whether to enable detailed metrics collection
        /// </summary>
        public bool EnableMetrics { get; init; } = true;

        /// <summary>
        /// Whether to enable event notifications for state changes
        /// </summary>
        public bool EnableEvents { get; init; } = true;

        /// <summary>
        /// Custom tags for categorizing this circuit breaker
        /// </summary>
        public HashSet<FixedString64Bytes> Tags { get; init; }

        /// <summary>
        /// Custom metadata for this circuit breaker configuration
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; }

        #region Slow Call Detection (consolidated from ISlowCallConfig)
        
        /// <summary>
        /// Duration threshold for considering a call slow
        /// </summary>
        public TimeSpan SlowCallDurationThreshold { get; init; } = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// Rate threshold percentage for slow calls (0-100)
        /// </summary>
        public double SlowCallRateThreshold { get; init; } = 50.0;
        
        /// <summary>
        /// Minimum number of slow calls before triggering circuit breaker
        /// </summary>
        public int MinimumSlowCalls { get; init; } = 5;
        
        #endregion

        #region Bulkhead Isolation (consolidated from IBulkheadConfig)
        
        /// <summary>
        /// Maximum number of concurrent calls allowed
        /// </summary>
        public int MaxConcurrentCalls { get; init; } = 10;
        
        /// <summary>
        /// Maximum wait duration before rejecting requests
        /// </summary>
        public TimeSpan MaxWaitDuration { get; init; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Whether to enable bulkhead isolation
        /// </summary>
        public bool EnableBulkhead { get; init; } = false;
        
        #endregion

        #region Rate Limiting (consolidated from IRateLimitConfig)
        
        /// <summary>
        /// Maximum requests per second when circuit is closed
        /// </summary>
        public int RequestsPerSecond { get; init; } = 100;
        
        /// <summary>
        /// Burst size allowed for temporary spikes
        /// </summary>
        public int BurstSize { get; init; } = 10;
        
        /// <summary>
        /// Whether to enable rate limiting
        /// </summary>
        public bool EnableRateLimit { get; init; } = false;
        
        #endregion

        #region Failover (consolidated from IFailoverConfig)
        
        /// <summary>
        /// Whether to enable failover when circuit is open
        /// </summary>
        public bool EnableFailover { get; init; } = false;
        
        /// <summary>
        /// List of failover endpoints or alternatives
        /// </summary>
        public List<string> FailoverEndpoints { get; init; }
        
        /// <summary>
        /// Timeout for failover attempts
        /// </summary>
        public TimeSpan FailoverTimeout { get; init; } = TimeSpan.FromSeconds(10);
        
        #endregion

        /// <summary>
        /// Validates the circuit breaker configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name cannot be null or empty");

            if (FailureThreshold <= 0)
                errors.Add("FailureThreshold must be greater than zero");

            if (FailureThreshold > 1000)
                errors.Add("FailureThreshold should not exceed 1000 for practical purposes");

            if (Timeout <= TimeSpan.Zero)
                errors.Add("Timeout must be greater than zero");

            if (Timeout > TimeSpan.FromHours(1))
                errors.Add("Timeout should not exceed 1 hour for practical recovery");

            if (SamplingDuration <= TimeSpan.Zero)
                errors.Add("SamplingDuration must be greater than zero");

            if (MinimumThroughput < 0)
                errors.Add("MinimumThroughput must be non-negative");

            if (SuccessThreshold < 0 || SuccessThreshold > 100)
                errors.Add("SuccessThreshold must be between 0 and 100");

            if (HalfOpenMaxCalls <= 0)
                errors.Add("HalfOpenMaxCalls must be greater than zero");

            if (HalfOpenMaxCalls > 100)
                errors.Add("HalfOpenMaxCalls should not exceed 100 for efficiency");

            if (SlidingWindowSize <= 0)
                errors.Add("SlidingWindowSize must be greater than zero");

            if (SlidingWindowType == SlidingWindowType.TimeBased && SlidingWindowDuration <= TimeSpan.Zero)
                errors.Add("SlidingWindowDuration must be greater than zero for time-based windows");

            if (MaxRecoveryAttempts < 0)
                errors.Add("MaxRecoveryAttempts must be non-negative");

            if (TimeoutMultiplier < 1.0)
                errors.Add("TimeoutMultiplier must be at least 1.0");

            if (MaxTimeout < Timeout)
                errors.Add("MaxTimeout must be greater than or equal to Timeout");

            // Validate consolidated configurations
            if (SlowCallDurationThreshold <= TimeSpan.Zero)
                errors.Add("SlowCallDurationThreshold must be greater than zero");
                
            if (SlowCallRateThreshold < 0.0 || SlowCallRateThreshold > 100.0)
                errors.Add("SlowCallRateThreshold must be between 0.0 and 100.0");
                
            if (MinimumSlowCalls < 0)
                errors.Add("MinimumSlowCalls must be non-negative");
                
            if (MaxConcurrentCalls <= 0)
                errors.Add("MaxConcurrentCalls must be greater than zero");
                
            if (MaxWaitDuration < TimeSpan.Zero)
                errors.Add("MaxWaitDuration must be non-negative");
                
            if (RequestsPerSecond <= 0)
                errors.Add("RequestsPerSecond must be greater than zero");
                
            if (BurstSize < 0)
                errors.Add("BurstSize must be non-negative");
                
            if (FailoverTimeout <= TimeSpan.Zero)
                errors.Add("FailoverTimeout must be greater than zero");

            // Validate exception types using ZLinq
            if (IgnoredExceptions?.AsValueEnumerable().Any(type => !typeof(System.Exception).IsAssignableFrom(type)) == true)
                errors.Add("All IgnoredExceptions must be derived from Exception");

            if (ImmediateFailureExceptions?.AsValueEnumerable().Any(type => !typeof(System.Exception).IsAssignableFrom(type)) == true)
                errors.Add("All ImmediateFailureExceptions must be derived from Exception");

            // Check for conflicting exception configurations using ZLinq
            if (IgnoredExceptions != null && ImmediateFailureExceptions != null)
            {
                var conflictingExceptions = IgnoredExceptions.AsValueEnumerable()
                    .Where(ignored => ImmediateFailureExceptions.Contains(ignored))
                    .ToList();
                if (conflictingExceptions.Count > 0)
                    errors.Add($"Exceptions cannot be both ignored and immediate failures: {string.Join(", ", conflictingExceptions.AsValueEnumerable().Select(t => t.Name))}");
            }

            return errors;
        }

        /// <summary>
        /// Creates a configuration optimized for critical services
        /// </summary>
        /// <returns>Critical service circuit breaker configuration</returns>
        public static CircuitBreakerConfig ForCriticalService()
        {
            return new CircuitBreakerConfig
            {
                Name = "Critical Service Circuit Breaker",
                FailureThreshold = 3,
                Timeout = TimeSpan.FromSeconds(30),
                SamplingDuration = TimeSpan.FromMinutes(1),
                MinimumThroughput = 5,
                SuccessThreshold = 80.0,
                HalfOpenMaxCalls = 2,
                UseSlidingWindow = true,
                SlidingWindowType = SlidingWindowType.CountBased,
                SlidingWindowSize = 50,
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 3,
                TimeoutMultiplier = 2.0,
                MaxTimeout = TimeSpan.FromMinutes(5),
                EnableMetrics = true,
                EnableEvents = true,
                SlowCallConfig = new SlowCallConfig
                {
                    SlowCallDurationThreshold = TimeSpan.FromSeconds(5),
                    SlowCallRateThreshold = 50.0,
                    MinimumSlowCalls = 3
                }
            };
        }

        /// <summary>
        /// Creates a configuration optimized for database connections
        /// </summary>
        /// <returns>Database circuit breaker configuration</returns>
        public static CircuitBreakerConfig ForDatabase()
        {
            return new CircuitBreakerConfig
            {
                Name = "Database Circuit Breaker",
                FailureThreshold = 5,
                Timeout = TimeSpan.FromMinutes(2),
                SamplingDuration = TimeSpan.FromMinutes(5),
                MinimumThroughput = 10,
                SuccessThreshold = 60.0,
                HalfOpenMaxCalls = 5,
                UseSlidingWindow = true,
                SlidingWindowType = SlidingWindowType.TimeBased,
                SlidingWindowDuration = TimeSpan.FromMinutes(2),
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 5,
                TimeoutMultiplier = 1.5,
                MaxTimeout = TimeSpan.FromMinutes(15),
                ImmediateFailureExceptions = new HashSet<Type>
                {
                    typeof(UnauthorizedAccessException),
                    //TODO Add support for DBs typeof(System.Data.SqlClient.SqlException)
                },
                SlowCallConfig = new SlowCallConfig
                {
                    SlowCallDurationThreshold = TimeSpan.FromSeconds(10),
                    SlowCallRateThreshold = 30.0,
                    MinimumSlowCalls = 5
                },
                BulkheadConfig = new BulkheadConfig
                {
                    MaxConcurrentCalls = 20,
                    MaxWaitDuration = TimeSpan.FromSeconds(30)
                }
            };
        }

        /// <summary>
        /// Creates a configuration optimized for network services
        /// </summary>
        /// <returns>Network service circuit breaker configuration</returns>
        public static CircuitBreakerConfig ForNetworkService()
        {
            return new CircuitBreakerConfig
            {
                Name = "Network Service Circuit Breaker",
                FailureThreshold = 8,
                Timeout = TimeSpan.FromMinutes(1),
                SamplingDuration = TimeSpan.FromMinutes(3),
                MinimumThroughput = 15,
                SuccessThreshold = 70.0,
                HalfOpenMaxCalls = 3,
                UseSlidingWindow = true,
                SlidingWindowType = SlidingWindowType.CountBased,
                SlidingWindowSize = 100,
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 10,
                TimeoutMultiplier = 1.2,
                MaxTimeout = TimeSpan.FromMinutes(10),
                IgnoredExceptions = new HashSet<Type>
                {
                    typeof(ArgumentException),
                    typeof(ArgumentNullException)
                },
                ImmediateFailureExceptions = new HashSet<Type>
                {
                    typeof(UnauthorizedAccessException)
                },
                SlowCallDurationThreshold = TimeSpan.FromSeconds(15),
                SlowCallRateThreshold = 40.0,
                MinimumSlowCalls = 8,
                RequestsPerSecond = 100,
                BurstSize = 150,
                EnableRateLimit = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for high-throughput scenarios
        /// </summary>
        /// <returns>High-throughput circuit breaker configuration</returns>
        public static CircuitBreakerConfig ForHighThroughput()
        {
            return new CircuitBreakerConfig
            {
                Name = "High Throughput Circuit Breaker",
                FailureThreshold = 20,
                Timeout = TimeSpan.FromSeconds(30),
                SamplingDuration = TimeSpan.FromMinutes(1),
                MinimumThroughput = 50,
                SuccessThreshold = 85.0,
                HalfOpenMaxCalls = 10,
                UseSlidingWindow = true,
                SlidingWindowType = SlidingWindowType.CountBased,
                SlidingWindowSize = 1000,
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 3,
                TimeoutMultiplier = 1.1,
                MaxTimeout = TimeSpan.FromMinutes(2),
                MaxConcurrentCalls = 100,
                MaxWaitDuration = TimeSpan.FromSeconds(10),
                EnableBulkhead = true,
                RequestsPerSecond = 1000,
                BurstSize = 1500,
                EnableRateLimit = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for development/testing
        /// </summary>
        /// <returns>Development circuit breaker configuration</returns>
        public static CircuitBreakerConfig ForDevelopment()
        {
            return new CircuitBreakerConfig
            {
                Name = "Development Circuit Breaker",
                FailureThreshold = 2,
                Timeout = TimeSpan.FromSeconds(10),
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 2,
                SuccessThreshold = 50.0,
                HalfOpenMaxCalls = 1,
                UseSlidingWindow = false,
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 1,
                TimeoutMultiplier = 1.0,
                MaxTimeout = TimeSpan.FromSeconds(30),
                EnableMetrics = true,
                EnableEvents = true
            };
        }

        /// <summary>
        /// Creates a circuit breaker configuration with all required fields
        /// </summary>
        /// <param name="name">Name of the circuit breaker</param>
        /// <returns>Circuit breaker configuration</returns>
        public static CircuitBreakerConfig Create(string name)
        {
            return new CircuitBreakerConfig
            {
                Id = new FixedString64Bytes(DeterministicIdGenerator.GenerateHealthCheckId("CircuitBreakerConfig", name).ToString("N")[..16]),
                Name = name ?? "Circuit Breaker",
                FailureThreshold = 5,
                Timeout = TimeSpan.FromSeconds(60),
                SamplingDuration = TimeSpan.FromMinutes(2),
                MinimumThroughput = 10,
                SuccessThreshold = 50.0,
                HalfOpenMaxCalls = 3,
                UseSlidingWindow = true,
                SlidingWindowType = SlidingWindowType.CountBased,
                SlidingWindowSize = 100,
                SlidingWindowDuration = TimeSpan.FromMinutes(1),
                EnableAutomaticRecovery = true,
                MaxRecoveryAttempts = 5,
                TimeoutMultiplier = 1.5,
                MaxTimeout = TimeSpan.FromMinutes(10),
                IgnoredExceptions = new HashSet<System.Type>
                {
                    typeof(System.ArgumentException),
                    typeof(System.ArgumentNullException),
                    typeof(System.InvalidOperationException)
                },
                ImmediateFailureExceptions = new HashSet<System.Type>
                {
                    typeof(System.UnauthorizedAccessException),
                    typeof(System.Security.SecurityException)
                },
                FailurePredicates = new List<System.Func<System.Exception, bool>>(),
                EnableMetrics = true,
                EnableEvents = true,
                Tags = new HashSet<FixedString64Bytes>(),
                Metadata = new Dictionary<string, object>(),
                SlowCallDurationThreshold = TimeSpan.FromSeconds(5),
                SlowCallRateThreshold = 50.0,
                MinimumSlowCalls = 5,
                MaxConcurrentCalls = 10,
                MaxWaitDuration = TimeSpan.FromSeconds(30),
                EnableBulkhead = false,
                RequestsPerSecond = 100,
                BurstSize = 10,
                EnableRateLimit = false,
                EnableFailover = false,
                FailoverEndpoints = new List<string>(),
                FailoverTimeout = TimeSpan.FromSeconds(10)
            };
        }
    }
}