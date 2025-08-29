using System;

namespace AhBearStudios.Core.Pooling.Configs
{
    public sealed class PoolErrorRecoveryConfiguration
    {
        public bool EnableErrorRecovery { get; init; } = true;
        
        public int DefaultMaxRetries { get; init; } = 3;
        
        public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);
        
        public double RetryBackoffMultiplier { get; init; } = 2.0;
        
        public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
        
        public bool EnableCircuitBreaker { get; init; } = true;
        
        public int CircuitBreakerFailureThreshold { get; init; } = 5;
        
        public TimeSpan CircuitBreakerTimeout { get; init; } = TimeSpan.FromMinutes(1);
        
        public int CircuitBreakerHalfOpenMaxCalls { get; init; } = 3;
        
        public bool LogRecoveryAttempts { get; init; } = true;
        
        public bool EnableEmergencyRecovery { get; init; } = true;
        
        public int EmergencyRecoveryFailureThreshold { get; init; } = 10;
        
        public TimeSpan EmergencyRecoveryCooldown { get; init; } = TimeSpan.FromMinutes(5);
        
        public bool EnableAutomaticPoolRecreation { get; init; } = true;
        
        public int MaxPoolRecreationAttempts { get; init; } = 3;
        
        public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromSeconds(30);
        
        public bool EnableRecoveryMetrics { get; init; } = true;

        public static PoolErrorRecoveryConfiguration Default => new();
        
        public static PoolErrorRecoveryConfiguration Resilient => new()
        {
            EnableErrorRecovery = true,
            DefaultMaxRetries = 5,
            InitialRetryDelay = TimeSpan.FromMilliseconds(50),
            RetryBackoffMultiplier = 1.5,
            MaxRetryDelay = TimeSpan.FromSeconds(10),
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerTimeout = TimeSpan.FromSeconds(30),
            EmergencyRecoveryFailureThreshold = 5,
            EmergencyRecoveryCooldown = TimeSpan.FromMinutes(2),
            MaxPoolRecreationAttempts = 5,
            HealthCheckInterval = TimeSpan.FromSeconds(15)
        };
        
        public static PoolErrorRecoveryConfiguration Conservative => new()
        {
            EnableErrorRecovery = true,
            DefaultMaxRetries = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(200),
            RetryBackoffMultiplier = 3.0,
            MaxRetryDelay = TimeSpan.FromMinutes(1),
            CircuitBreakerFailureThreshold = 10,
            CircuitBreakerTimeout = TimeSpan.FromMinutes(5),
            EmergencyRecoveryFailureThreshold = 20,
            EmergencyRecoveryCooldown = TimeSpan.FromMinutes(10),
            MaxPoolRecreationAttempts = 1,
            HealthCheckInterval = TimeSpan.FromMinutes(1)
        };
        
        public static PoolErrorRecoveryConfiguration Disabled => new()
        {
            EnableErrorRecovery = false,
            EnableCircuitBreaker = false,
            EnableEmergencyRecovery = false,
            EnableAutomaticPoolRecreation = false
        };
    }
}