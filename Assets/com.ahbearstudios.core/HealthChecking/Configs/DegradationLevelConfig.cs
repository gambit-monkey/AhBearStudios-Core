using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Configs;

/// <summary>
    /// Configuration for a specific degradation level
    /// </summary>
    public sealed record DegradationLevelConfig
    {
        /// <summary>
        /// Features to disable at this degradation level
        /// </summary>
        public HashSet<FixedString64Bytes> DisabledFeatures { get; init; } = new();

        /// <summary>
        /// Services to disable at this degradation level
        /// </summary>
        public HashSet<FixedString64Bytes> DisabledServices { get; init; } = new();

        /// <summary>
        /// Services to run in degraded mode
        /// </summary>
        public HashSet<FixedString64Bytes> DegradedServices { get; init; } = new();

        /// <summary>
        /// Performance limits to apply at this level
        /// </summary>
        public Dictionary<string, double> PerformanceLimits { get; init; } = new();

        /// <summary>
        /// Resource usage limits at this level
        /// </summary>
        public Dictionary<string, double> ResourceLimits { get; init; } = new();

        /// <summary>
        /// Custom actions to execute when entering this level
        /// </summary>
        public List<string> OnEnterActions { get; init; } = new();

        /// <summary>
        /// Custom actions to execute when exiting this level
        /// </summary>
        public List<string> OnExitActions { get; init; } = new();

        /// <summary>
        /// Whether to enable reduced functionality mode
        /// </summary>
        public bool EnableReducedFunctionality { get; init; } = true;

        /// <summary>
        /// Percentage of normal capacity to operate at (0.0 to 1.0)
        /// </summary>
        public double CapacityReduction { get; init; } = 1.0;

        /// <summary>
        /// User-facing message for this degradation level
        /// </summary>
        public string UserMessage { get; init; } = string.Empty;

        /// <summary>
        /// Administrative message for this degradation level
        /// </summary>
        public string AdminMessage { get; init; } = string.Empty;

        /// <summary>
        /// Custom metadata for this degradation level
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// Validates degradation level configuration
        /// </summary>
        /// <param name="levelName">Name of the degradation level for error messages</param>
        /// <returns>List of validation errors</returns>
        public List<string> Validate(string levelName)
        {
            var errors = new List<string>();

            if (CapacityReduction < 0.0 || CapacityReduction > 1.0)
                errors.Add($"{levelName} CapacityReduction must be between 0.0 and 1.0");

            foreach (var limit in PerformanceLimits.Values)
            {
                if (limit < 0.0)
                    errors.Add($"{levelName} PerformanceLimits values must be non-negative");
            }

            foreach (var limit in ResourceLimits.Values)
            {
                if (limit < 0.0)
                    errors.Add($"{levelName} ResourceLimits values must be non-negative");
            }

            return errors;
        }

        /// <summary>
        /// Creates configuration for critical system minor degradation
        /// </summary>
        /// <returns>Critical minor degradation configuration</returns>
        public static DegradationLevelConfig ForCriticalMinor()
        {
            return new DegradationLevelConfig
            {
                DisabledFeatures = new HashSet<FixedString64Bytes>
                {
                    "NonEssentialReporting",
                    "DetailedLogging",
                    "PerformanceMetrics"
                },
                CapacityReduction = 0.9,
                UserMessage = "Some non-essential features may be temporarily unavailable.",
                AdminMessage = "System operating in minor degraded mode - non-essential features disabled."
            };
        }

        /// <summary>
        /// Creates configuration for critical system moderate degradation
        /// </summary>
        /// <returns>Critical moderate degradation configuration</returns>
        public static DegradationLevelConfig ForCriticalModerate()
        {
            return new DegradationLevelConfig
            {
                DisabledFeatures = new HashSet<FixedString64Bytes>
                {
                    "NonEssentialReporting",
                    "DetailedLogging",
                    "PerformanceMetrics",
                    "BackgroundProcessing",
                    "OptionalIntegrations"
                },
                DegradedServices = new HashSet<FixedString64Bytes>
                {
                    "CacheService",
                    "SearchService"
                },
                CapacityReduction = 0.7,
                UserMessage = "System is operating with reduced functionality.",
                AdminMessage = "System operating in moderate degraded mode - several features disabled."
            };
        }

        /// <summary>
        /// Creates configuration for critical system severe degradation
        /// </summary>
        /// <returns>Critical severe degradation configuration</returns>
        public static DegradationLevelConfig ForCriticalSevere()
        {
            return new DegradationLevelConfig
            {
                DisabledFeatures = new HashSet<FixedString64Bytes>
                {
                    "NonEssentialReporting",
                    "DetailedLogging",
                    "PerformanceMetrics",
                    "BackgroundProcessing",
                    "OptionalIntegrations",
                    "UserPersonalization",
                    "AdvancedSearch"
                },
                DisabledServices = new HashSet<FixedString64Bytes>
                {
                    "RecommendationService",
                    "AnalyticsService"
                },
                DegradedServices = new HashSet<FixedString64Bytes>
                {
                    "CacheService",
                    "SearchService",
                    "NotificationService"
                },
                CapacityReduction = 0.5,
                UserMessage = "System is operating in emergency mode with essential functions only.",
                AdminMessage = "System operating in severe degraded mode - only essential functions available."
            };
        }

        /// <summary>
        /// Creates configuration for critical system disabled state
        /// </summary>
        /// <returns>Critical disabled state configuration</returns>
        public static DegradationLevelConfig ForCriticalDisabled()
        {
            return new DegradationLevelConfig
            {
                EnableReducedFunctionality = false,
                CapacityReduction = 0.1,
                UserMessage = "System is temporarily unavailable. Please try again later.",
                AdminMessage = "System is disabled due to critical health issues."
            };
        }
    }