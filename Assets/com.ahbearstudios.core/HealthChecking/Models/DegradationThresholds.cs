using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Configuration for degradation thresholds that control automatic system degradation.
    /// Used to determine when to transition between degradation levels based on system health.
    /// </summary>
    public sealed record DegradationThresholds
    {
        /// <summary>
        /// Gets the threshold for triggering minor degradation (0.0 to 1.0).
        /// Represents the percentage of failed health checks before entering minor degradation.
        /// </summary>
        public double MinorThreshold { get; init; } = 0.10; // 10% failures

        /// <summary>
        /// Gets the threshold for triggering moderate degradation (0.0 to 1.0).
        /// Represents the percentage of failed health checks before entering moderate degradation.
        /// </summary>
        public double ModerateThreshold { get; init; } = 0.25; // 25% failures

        /// <summary>
        /// Gets the threshold for triggering severe degradation (0.0 to 1.0).
        /// Represents the percentage of failed health checks before entering severe degradation.
        /// </summary>
        public double SevereThreshold { get; init; } = 0.50; // 50% failures

        /// <summary>
        /// Gets the threshold for completely disabling the system (0.0 to 1.0).
        /// Represents the percentage of failed health checks before entering disabled state.
        /// </summary>
        public double DisabledThreshold { get; init; } = 0.75; // 75% failures

        /// <summary>
        /// Gets the recovery threshold for returning to a better degradation level (0.0 to 1.0).
        /// Represents the percentage of successful health checks needed for recovery.
        /// </summary>
        public double RecoveryThreshold { get; init; } = 0.80; // 80% success for recovery

        /// <summary>
        /// Gets the minimum number of health checks required before applying degradation rules.
        /// Prevents degradation from being triggered with too few samples.
        /// </summary>
        public int MinimumHealthChecks { get; init; } = 5;

        /// <summary>
        /// Gets the time window for evaluating degradation thresholds.
        /// Only health checks within this window are considered for degradation decisions.
        /// </summary>
        public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the hysteresis delay to prevent rapid state changes.
        /// System must remain in a degraded state for this duration before recovery is considered.
        /// </summary>
        public TimeSpan HysteresisDelay { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets whether automatic degradation is enabled.
        /// When false, degradation levels must be set manually.
        /// </summary>
        public bool AutomaticDegradation { get; init; } = true;

        /// <summary>
        /// Gets whether to consider critical health checks with higher weight.
        /// Critical health check failures count more towards degradation thresholds.
        /// </summary>
        public bool WeightCriticalChecks { get; init; } = true;

        /// <summary>
        /// Gets the weight multiplier for critical health check failures.
        /// Only used when WeightCriticalChecks is true.
        /// </summary>
        public double CriticalCheckWeight { get; init; } = 2.0;

        /// <summary>
        /// Validates the degradation thresholds configuration.
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Threshold validation
            if (MinorThreshold < 0.0 || MinorThreshold > 1.0)
                errors.Add("MinorThreshold must be between 0.0 and 1.0");

            if (ModerateThreshold < 0.0 || ModerateThreshold > 1.0)
                errors.Add("ModerateThreshold must be between 0.0 and 1.0");

            if (SevereThreshold < 0.0 || SevereThreshold > 1.0)
                errors.Add("SevereThreshold must be between 0.0 and 1.0");

            if (DisabledThreshold < 0.0 || DisabledThreshold > 1.0)
                errors.Add("DisabledThreshold must be between 0.0 and 1.0");

            if (RecoveryThreshold < 0.0 || RecoveryThreshold > 1.0)
                errors.Add("RecoveryThreshold must be between 0.0 and 1.0");

            // Threshold ordering validation
            if (MinorThreshold >= ModerateThreshold)
                errors.Add("MinorThreshold must be less than ModerateThreshold");

            if (ModerateThreshold >= SevereThreshold)
                errors.Add("ModerateThreshold must be less than SevereThreshold");

            if (SevereThreshold >= DisabledThreshold)
                errors.Add("SevereThreshold must be less than DisabledThreshold");

            // Other validations
            if (MinimumHealthChecks < 1)
                errors.Add("MinimumHealthChecks must be at least 1");

            if (EvaluationWindow <= TimeSpan.Zero)
                errors.Add("EvaluationWindow must be greater than zero");

            if (HysteresisDelay < TimeSpan.Zero)
                errors.Add("HysteresisDelay must be non-negative");

            if (WeightCriticalChecks && CriticalCheckWeight < 1.0)
                errors.Add("CriticalCheckWeight must be at least 1.0 when WeightCriticalChecks is enabled");

            return errors;
        }

        /// <summary>
        /// Creates degradation thresholds optimized for high-availability systems.
        /// </summary>
        /// <returns>High-availability degradation thresholds</returns>
        public static DegradationThresholds ForHighAvailability()
        {
            return new DegradationThresholds
            {
                MinorThreshold = 0.05,     // 5% failures
                ModerateThreshold = 0.15,  // 15% failures
                SevereThreshold = 0.30,    // 30% failures
                DisabledThreshold = 0.60,  // 60% failures
                RecoveryThreshold = 0.90,  // 90% success for recovery
                MinimumHealthChecks = 10,
                EvaluationWindow = TimeSpan.FromMinutes(3),
                HysteresisDelay = TimeSpan.FromMinutes(1),
                AutomaticDegradation = true,
                WeightCriticalChecks = true,
                CriticalCheckWeight = 3.0
            };
        }

        /// <summary>
        /// Creates degradation thresholds optimized for development environments.
        /// </summary>
        /// <returns>Development degradation thresholds</returns>
        public static DegradationThresholds ForDevelopment()
        {
            return new DegradationThresholds
            {
                MinorThreshold = 0.20,     // 20% failures
                ModerateThreshold = 0.40,  // 40% failures
                SevereThreshold = 0.60,    // 60% failures
                DisabledThreshold = 0.80,  // 80% failures
                RecoveryThreshold = 0.70,  // 70% success for recovery
                MinimumHealthChecks = 3,
                EvaluationWindow = TimeSpan.FromMinutes(10),
                HysteresisDelay = TimeSpan.FromSeconds(30),
                AutomaticDegradation = false, // Manual control in development
                WeightCriticalChecks = false,
                CriticalCheckWeight = 1.0
            };
        }

        /// <summary>
        /// Returns a string representation of the degradation thresholds.
        /// </summary>
        /// <returns>Degradation thresholds string</returns>
        public override string ToString()
        {
            return $"DegradationThresholds: Minor={MinorThreshold:P0}, Moderate={ModerateThreshold:P0}, " +
                   $"Severe={SevereThreshold:P0}, Disabled={DisabledThreshold:P0}";
        }
    }
}