using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Configuration for health status thresholds that control overall system health determination.
    /// Used to aggregate individual health check results into overall system health status.
    /// </summary>
    public sealed record HealthThresholds
    {
        /// <summary>
        /// Gets the threshold for determining overall healthy status (0.0 to 1.0).
        /// Represents the minimum percentage of healthy checks required for overall healthy status.
        /// </summary>
        public double HealthyThreshold { get; init; } = 0.95; // 95% healthy required

        /// <summary>
        /// Gets the threshold for determining overall warning status (0.0 to 1.0).
        /// Represents the minimum percentage of non-failing checks for warning status.
        /// </summary>
        public double WarningThreshold { get; init; } = 0.80; // 80% non-failing for warning

        /// <summary>
        /// Gets the threshold for determining overall degraded status (0.0 to 1.0).
        /// Represents the minimum percentage of non-critical checks for degraded status.
        /// </summary>
        public double DegradedThreshold { get; init; } = 0.60; // 60% non-critical for degraded

        /// <summary>
        /// Gets the threshold for critical health check failures.
        /// If this percentage of critical checks fail, status is immediately unhealthy.
        /// </summary>
        public double CriticalFailureThreshold { get; init; } = 0.20; // 20% critical failures = unhealthy

        /// <summary>
        /// Gets the maximum allowed response time before considering a check as slow.
        /// Slow checks contribute to degraded status even if they pass.
        /// </summary>
        public TimeSpan SlowResponseThreshold { get; init; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets the percentage of slow responses that trigger degraded status.
        /// Even if checks pass, too many slow responses indicate degradation.
        /// </summary>
        public double SlowResponseThreshold_Percentage { get; init; } = 0.30; // 30% slow = degraded

        /// <summary>
        /// Gets the time window for evaluating health thresholds.
        /// Only recent health check results within this window are considered.
        /// </summary>
        public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the minimum number of health check results required for threshold evaluation.
        /// Prevents status changes with insufficient data.
        /// </summary>
        public int MinimumSampleSize { get; init; } = 3;

        /// <summary>
        /// Gets whether to use weighted scoring for different health check categories.
        /// Critical system checks have higher weight than optional feature checks.
        /// </summary>
        public bool UseWeightedScoring { get; init; } = true;

        /// <summary>
        /// Gets the weight assigned to critical system health checks.
        /// Only used when UseWeightedScoring is true.
        /// </summary>
        public double CriticalSystemWeight { get; init; } = 3.0;

        /// <summary>
        /// Gets the weight assigned to important feature health checks.
        /// Only used when UseWeightedScoring is true.
        /// </summary>
        public double ImportantFeatureWeight { get; init; } = 2.0;

        /// <summary>
        /// Gets the weight assigned to optional feature health checks.
        /// Only used when UseWeightedScoring is true.
        /// </summary>
        public double OptionalFeatureWeight { get; init; } = 1.0;

        /// <summary>
        /// Gets whether to apply hysteresis to prevent rapid status changes.
        /// Status must remain consistent for HysteresisDelay before change is applied.
        /// </summary>
        public bool ApplyHysteresis { get; init; } = true;

        /// <summary>
        /// Gets the hysteresis delay for status changes.
        /// Only used when ApplyHysteresis is true.
        /// </summary>
        public TimeSpan HysteresisDelay { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Validates the health thresholds configuration.
        /// </summary>
        /// <returns>List of validation errors, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Threshold range validation
            if (HealthyThreshold < 0.0 || HealthyThreshold > 1.0)
                errors.Add("HealthyThreshold must be between 0.0 and 1.0");

            if (WarningThreshold < 0.0 || WarningThreshold > 1.0)
                errors.Add("WarningThreshold must be between 0.0 and 1.0");

            if (DegradedThreshold < 0.0 || DegradedThreshold > 1.0)
                errors.Add("DegradedThreshold must be between 0.0 and 1.0");

            if (CriticalFailureThreshold < 0.0 || CriticalFailureThreshold > 1.0)
                errors.Add("CriticalFailureThreshold must be between 0.0 and 1.0");

            if (SlowResponseThreshold_Percentage < 0.0 || SlowResponseThreshold_Percentage > 1.0)
                errors.Add("SlowResponseThreshold_Percentage must be between 0.0 and 1.0");

            // Threshold ordering validation (higher status requires higher threshold)
            if (DegradedThreshold >= WarningThreshold)
                errors.Add("DegradedThreshold must be less than WarningThreshold");

            if (WarningThreshold >= HealthyThreshold)
                errors.Add("WarningThreshold must be less than HealthyThreshold");

            // Time validation
            if (SlowResponseThreshold <= TimeSpan.Zero)
                errors.Add("SlowResponseThreshold must be greater than zero");

            if (EvaluationWindow <= TimeSpan.Zero)
                errors.Add("EvaluationWindow must be greater than zero");

            if (ApplyHysteresis && HysteresisDelay < TimeSpan.Zero)
                errors.Add("HysteresisDelay must be non-negative when ApplyHysteresis is enabled");

            // Sample size validation
            if (MinimumSampleSize < 1)
                errors.Add("MinimumSampleSize must be at least 1");

            // Weight validation
            if (UseWeightedScoring)
            {
                if (CriticalSystemWeight < 1.0)
                    errors.Add("CriticalSystemWeight must be at least 1.0");

                if (ImportantFeatureWeight < 1.0)
                    errors.Add("ImportantFeatureWeight must be at least 1.0");

                if (OptionalFeatureWeight < 1.0)
                    errors.Add("OptionalFeatureWeight must be at least 1.0");
            }

            return errors;
        }

        /// <summary>
        /// Creates health thresholds optimized for production environments.
        /// </summary>
        /// <returns>Production-optimized health thresholds</returns>
        public static HealthThresholds ForProduction()
        {
            return new HealthThresholds
            {
                HealthyThreshold = 0.95,           // 95% healthy required
                WarningThreshold = 0.85,           // 85% for warning
                DegradedThreshold = 0.70,          // 70% for degraded
                CriticalFailureThreshold = 0.10,   // 10% critical failures = unhealthy
                SlowResponseThreshold = TimeSpan.FromSeconds(3),
                SlowResponseThreshold_Percentage = 0.20, // 20% slow = degraded
                EvaluationWindow = TimeSpan.FromMinutes(3),
                MinimumSampleSize = 5,
                UseWeightedScoring = true,
                CriticalSystemWeight = 4.0,
                ImportantFeatureWeight = 2.0,
                OptionalFeatureWeight = 1.0,
                ApplyHysteresis = true,
                HysteresisDelay = TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>
        /// Creates health thresholds optimized for development environments.
        /// </summary>
        /// <returns>Development-optimized health thresholds</returns>
        public static HealthThresholds ForDevelopment()
        {
            return new HealthThresholds
            {
                HealthyThreshold = 0.80,           // 80% healthy required
                WarningThreshold = 0.60,           // 60% for warning
                DegradedThreshold = 0.40,          // 40% for degraded
                CriticalFailureThreshold = 0.30,   // 30% critical failures = unhealthy
                SlowResponseThreshold = TimeSpan.FromSeconds(10),
                SlowResponseThreshold_Percentage = 0.50, // 50% slow = degraded
                EvaluationWindow = TimeSpan.FromMinutes(10),
                MinimumSampleSize = 2,
                UseWeightedScoring = false,
                CriticalSystemWeight = 1.0,
                ImportantFeatureWeight = 1.0,
                OptionalFeatureWeight = 1.0,
                ApplyHysteresis = false,
                HysteresisDelay = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Creates health thresholds optimized for testing environments.
        /// </summary>
        /// <returns>Testing-optimized health thresholds</returns>
        public static HealthThresholds ForTesting()
        {
            return new HealthThresholds
            {
                HealthyThreshold = 1.0,            // 100% healthy required
                WarningThreshold = 0.90,           // 90% for warning
                DegradedThreshold = 0.70,          // 70% for degraded
                CriticalFailureThreshold = 0.50,   // 50% critical failures = unhealthy
                SlowResponseThreshold = TimeSpan.FromSeconds(30),
                SlowResponseThreshold_Percentage = 0.80, // 80% slow = degraded
                EvaluationWindow = TimeSpan.FromMinutes(1),
                MinimumSampleSize = 1,
                UseWeightedScoring = false,
                ApplyHysteresis = false,
                HysteresisDelay = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Returns a string representation of the health thresholds.
        /// </summary>
        /// <returns>Health thresholds string</returns>
        public override string ToString()
        {
            return $"HealthThresholds: Healthy={HealthyThreshold:P0}, Warning={WarningThreshold:P0}, " +
                   $"Degraded={DegradedThreshold:P0}, Critical={CriticalFailureThreshold:P0}";
        }
    }
}