using System.Collections.Generic;
using AhBearStudios.Core.HealthCheck.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthCheck.Models
{
    /// <summary>
    /// Comprehensive configuration for system degradation thresholds and graceful degradation behavior
    /// </summary>
    public sealed record DegradationThresholds
    {
        /// <summary>
        /// Unique identifier for this degradation configuration
        /// </summary>
        public FixedString64Bytes Id { get; init; } = GenerateId();

        /// <summary>
        /// Display name for this degradation configuration
        /// </summary>
        public string Name { get; init; } = "Default Degradation Thresholds";

        /// <summary>
        /// Whether graceful degradation is enabled
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Percentage of unhealthy checks that triggers minor degradation (0.0 to 1.0)
        /// </summary>
        public double MinorThreshold { get; init; } = 0.1;

        /// <summary>
        /// Percentage of unhealthy checks that triggers moderate degradation (0.0 to 1.0)
        /// </summary>
        public double ModerateThreshold { get; init; } = 0.25;

        /// <summary>
        /// Percentage of unhealthy checks that triggers severe degradation (0.0 to 1.0)
        /// </summary>
        public double SevereThreshold { get; init; } = 0.5;

        /// <summary>
        /// Percentage of unhealthy checks that triggers system disable (0.0 to 1.0)
        /// </summary>
        public double DisabledThreshold { get; init; } = 0.75;

        /// <summary>
        /// Time window for evaluating degradation thresholds
        /// </summary>
        public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Minimum number of health checks required before applying degradation
        /// </summary>
        public int MinimumHealthChecks { get; init; } = 3;

        /// <summary>
        /// Whether to use weighted health checks for degradation calculation
        /// </summary>
        public bool UseWeightedCalculation { get; init; } = true;

        /// <summary>
        /// Time to wait before escalating degradation level
        /// </summary>
        public TimeSpan EscalationDelay { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Time to wait before de-escalating degradation level
        /// </summary>
        public TimeSpan DeEscalationDelay { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable automatic recovery from degradation
        /// </summary>
        public bool EnableAutoRecovery { get; init; } = true;

        /// <summary>
        /// Recovery success rate threshold for automatic recovery (0.0 to 1.0)
        /// </summary>
        public double RecoveryThreshold { get; init; } = 0.8;

        /// <summary>
        /// Time window for evaluating recovery
        /// </summary>
        public TimeSpan RecoveryWindow { get; init; } = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Features and services configuration for each degradation level
        /// </summary>
        public DegradationLevelConfig MinorDegradation { get; init; } = new();

        /// <summary>
        /// Features and services configuration for moderate degradation
        /// </summary>
        public DegradationLevelConfig ModerateDegradation { get; init; } = new();

        /// <summary>
        /// Features and services configuration for severe degradation
        /// </summary>
        public DegradationLevelConfig SevereDegradation { get; init; } = new();

        /// <summary>
        /// Features and services configuration for disabled state
        /// </summary>
        public DegradationLevelConfig DisabledState { get; init; } = new();

        /// <summary>
        /// Health check category weights for degradation calculation
        /// </summary>
        public Dictionary<HealthCheckCategory, double> CategoryWeights { get; init; } = new()
        {
            { HealthCheckCategory.System, 1.0 },
            { HealthCheckCategory.Database, 0.9 },
            { HealthCheckCategory.Network, 0.7 },
            { HealthCheckCategory.Performance, 0.5 },
            { HealthCheckCategory.Security, 0.8 },
            { HealthCheckCategory.CircuitBreaker, 0.6 },
            { HealthCheckCategory.Custom, 0.4 }
        };

        /// <summary>
        /// Critical health checks that immediately trigger severe degradation when unhealthy
        /// </summary>
        public HashSet<FixedString64Bytes> CriticalHealthChecks { get; init; } = new();

        /// <summary>
        /// Health checks that should be ignored in degradation calculations
        /// </summary>
        public HashSet<FixedString64Bytes> IgnoredHealthChecks { get; init; } = new();

        /// <summary>
        /// Advanced degradation rules for complex scenarios
        /// </summary>
        public List<DegradationRule> CustomRules { get; init; } = new();

        /// <summary>
        /// Circuit breaker integration settings
        /// </summary>
        public CircuitBreakerIntegrationConfig CircuitBreakerIntegration { get; init; } = new();

        /// <summary>
        /// Load shedding configuration for each degradation level
        /// </summary>
        public LoadSheddingConfig LoadShedding { get; init; } = new();

        /// <summary>
        /// Performance throttling configuration
        /// </summary>
        public PerformanceThrottlingConfig PerformanceThrottling { get; init; } = new();

        /// <summary>
        /// Feature flag integration for controlled degradation
        /// </summary>
        public FeatureFlagIntegrationConfig FeatureFlagIntegration { get; init; } = new();

        /// <summary>
        /// Monitoring and alerting configuration for degradation events
        /// </summary>
        public DegradationMonitoringConfig Monitoring { get; init; } = new();

        /// <summary>
        /// Custom metadata for this degradation configuration
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// Validates the degradation thresholds configuration
        /// </summary>
        /// <returns>List of validation error messages, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name cannot be null or empty");

            if (MinorThreshold < 0.0 || MinorThreshold > 1.0)
                errors.Add("MinorThreshold must be between 0.0 and 1.0");

            if (ModerateThreshold < 0.0 || ModerateThreshold > 1.0)
                errors.Add("ModerateThreshold must be between 0.0 and 1.0");

            if (SevereThreshold < 0.0 || SevereThreshold > 1.0)
                errors.Add("SevereThreshold must be between 0.0 and 1.0");

            if (DisabledThreshold < 0.0 || DisabledThreshold > 1.0)
                errors.Add("DisabledThreshold must be between 0.0 and 1.0");

            // Validate threshold ordering
            if (MinorThreshold >= ModerateThreshold)
                errors.Add("MinorThreshold must be less than ModerateThreshold");

            if (ModerateThreshold >= SevereThreshold)
                errors.Add("ModerateThreshold must be less than SevereThreshold");

            if (SevereThreshold >= DisabledThreshold)
                errors.Add("SevereThreshold must be less than DisabledThreshold");

            if (EvaluationWindow <= TimeSpan.Zero)
                errors.Add("EvaluationWindow must be greater than zero");

            if (MinimumHealthChecks <= 0)
                errors.Add("MinimumHealthChecks must be greater than zero");

            if (EscalationDelay < TimeSpan.Zero)
                errors.Add("EscalationDelay must be non-negative");

            if (DeEscalationDelay < TimeSpan.Zero)
                errors.Add("DeEscalationDelay must be non-negative");

            if (RecoveryThreshold < 0.0 || RecoveryThreshold > 1.0)
                errors.Add("RecoveryThreshold must be between 0.0 and 1.0");

            if (RecoveryWindow <= TimeSpan.Zero)
                errors.Add("RecoveryWindow must be greater than zero");

            // Validate category weights
            foreach (var weight in CategoryWeights.Values)
            {
                if (weight < 0.0 || weight > 2.0)
                    errors.Add($"Category weights must be between 0.0 and 2.0, found: {weight}");
            }

            // Validate nested configurations
            errors.AddRange(MinorDegradation.Validate("Minor"));
            errors.AddRange(ModerateDegradation.Validate("Moderate"));
            errors.AddRange(SevereDegradation.Validate("Severe"));
            errors.AddRange(DisabledState.Validate("Disabled"));

            foreach (var rule in CustomRules)
            {
                errors.AddRange(rule.Validate());
            }

            errors.AddRange(CircuitBreakerIntegration.Validate());
            errors.AddRange(LoadShedding.Validate());
            errors.AddRange(PerformanceThrottling.Validate());
            errors.AddRange(FeatureFlagIntegration.Validate());
            errors.AddRange(Monitoring.Validate());

            return errors;
        }

        /// <summary>
        /// Creates degradation thresholds optimized for critical systems
        /// </summary>
        /// <returns>Critical system degradation configuration</returns>
        public static DegradationThresholds ForCriticalSystem()
        {
            return new DegradationThresholds
            {
                Name = "Critical System Degradation",
                MinorThreshold = 0.05, // 5% unhealthy triggers minor
                ModerateThreshold = 0.15, // 15% unhealthy triggers moderate
                SevereThreshold = 0.3, // 30% unhealthy triggers severe
                DisabledThreshold = 0.5, // 50% unhealthy triggers disabled
                EvaluationWindow = TimeSpan.FromMinutes(2),
                EscalationDelay = TimeSpan.FromSeconds(30),
                DeEscalationDelay = TimeSpan.FromMinutes(10),
                RecoveryThreshold = 0.9,
                MinorDegradation = DegradationLevelConfig.ForCriticalMinor(),
                ModerateDegradation = DegradationLevelConfig.ForCriticalModerate(),
                SevereDegradation = DegradationLevelConfig.ForCriticalSevere(),
                DisabledState = DegradationLevelConfig.ForCriticalDisabled()
            };
        }

        /// <summary>
        /// Creates degradation thresholds optimized for high-availability systems
        /// </summary>
        /// <returns>High-availability degradation configuration</returns>
        public static DegradationThresholds ForHighAvailability()
        {
            return new DegradationThresholds
            {
                Name = "High Availability Degradation",
                MinorThreshold = 0.1, // 10% unhealthy triggers minor
                ModerateThreshold = 0.25, // 25% unhealthy triggers moderate
                SevereThreshold = 0.5, // 50% unhealthy triggers severe
                DisabledThreshold = 0.8, // 80% unhealthy triggers disabled
                EvaluationWindow = TimeSpan.FromMinutes(5),
                EscalationDelay = TimeSpan.FromMinutes(2),
                DeEscalationDelay = TimeSpan.FromMinutes(5),
                RecoveryThreshold = 0.8,
                EnableAutoRecovery = true,
                LoadShedding = LoadSheddingConfig.ForHighAvailability(),
                PerformanceThrottling = PerformanceThrottlingConfig.ForHighAvailability()
            };
        }

        /// <summary>
        /// Creates degradation thresholds optimized for development environments
        /// </summary>
        /// <returns>Development degradation configuration</returns>
        public static DegradationThresholds ForDevelopment()
        {
            return new DegradationThresholds
            {
                Name = "Development Degradation",
                Enabled = false, // Often disabled in development
                MinorThreshold = 0.2,
                ModerateThreshold = 0.4,
                SevereThreshold = 0.7,
                DisabledThreshold = 0.9,
                EvaluationWindow = TimeSpan.FromMinutes(1),
                EscalationDelay = TimeSpan.FromSeconds(10),
                DeEscalationDelay = TimeSpan.FromSeconds(30),
                EnableAutoRecovery = true,
                RecoveryThreshold = 0.6
            };
        }

        /// <summary>
        /// Generates a unique identifier for configurations
        /// </summary>
        /// <returns>Unique configuration ID</returns>
        private static FixedString64Bytes GenerateId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }
    }
}