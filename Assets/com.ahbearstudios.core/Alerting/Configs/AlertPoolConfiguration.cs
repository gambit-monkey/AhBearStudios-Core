using System;
using AhBearStudios.Core.Pooling.Configs;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Configs
{
    /// <summary>
    /// Specialized pool configuration for Alert containers.
    /// Extends base PoolConfiguration with alert-specific optimizations.
    /// Designed for high-frequency alert creation scenarios in game development.
    /// </summary>
    public record AlertPoolConfiguration : PoolConfiguration
    {
        /// <summary>
        /// Gets the maximum number of alerts to retain for correlation analysis.
        /// </summary>
        public int MaxCorrelationHistory { get; init; } = 1000;

        /// <summary>
        /// Gets the alert severity threshold for pool expansion decisions.
        /// Higher severity alerts may trigger immediate pool expansion.
        /// </summary>
        public AlertSeverity ExpansionTriggerSeverity { get; init; } = AlertSeverity.Critical;

        /// <summary>
        /// Gets whether to enable alert deduplication within the pool.
        /// </summary>
        public bool EnableDeduplication { get; init; } = true;

        /// <summary>
        /// Gets the time window for alert deduplication in seconds.
        /// </summary>
        public int DeduplicationWindowSeconds { get; init; } = 60;

        /// <summary>
        /// Gets the maximum number of alerts to cache for performance analysis.
        /// </summary>
        public int MaxPerformanceCache { get; init; } = 500;

        /// <summary>
        /// Gets whether to enable automatic pool warming based on alert patterns.
        /// </summary>
        public bool EnablePatternBasedWarming { get; init; } = true;

        /// <summary>
        /// Gets the threshold for triggering emergency pool expansion.
        /// When available alerts drop below this percentage, emergency expansion occurs.
        /// </summary>
        public float EmergencyExpansionThreshold { get; init; } = 0.1f; // 10%

        /// <summary>
        /// Creates a high-performance alert pool configuration optimized for game workloads.
        /// </summary>
        /// <param name="name">Name of the alert pool</param>
        /// <returns>High-performance alert pool configuration</returns>
        public static AlertPoolConfiguration CreateHighPerformance(string name = "AlertPool")
        {
            return new AlertPoolConfiguration
            {
                Name = name,
                InitialCapacity = 50,        // Larger initial capacity for alerts
                MaxCapacity = 1000,          // Higher max capacity for burst scenarios
                ValidationInterval = TimeSpan.FromMinutes(2), // More frequent validation
                MaxIdleTime = TimeSpan.FromMinutes(10),       // Shorter idle time
                EnableValidation = true,
                EnableStatistics = true,
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool,
                MaxCorrelationHistory = 2000,
                ExpansionTriggerSeverity = AlertSeverity.High,
                EnableDeduplication = true,
                DeduplicationWindowSeconds = 30,
                MaxPerformanceCache = 1000,
                EnablePatternBasedWarming = true,
                EmergencyExpansionThreshold = 0.05f // 5% for high-performance
            };
        }

        /// <summary>
        /// Creates a memory-optimized alert pool configuration for resource-constrained environments.
        /// </summary>
        /// <param name="name">Name of the alert pool</param>
        /// <returns>Memory-optimized alert pool configuration</returns>
        public static AlertPoolConfiguration CreateMemoryOptimized(string name = "AlertPool")
        {
            return new AlertPoolConfiguration
            {
                Name = name,
                InitialCapacity = 10,        // Smaller initial capacity
                MaxCapacity = 100,           // Conservative max capacity
                ValidationInterval = TimeSpan.FromMinutes(5), // Less frequent validation
                MaxIdleTime = TimeSpan.FromMinutes(30),       // Longer idle time
                EnableValidation = true,
                EnableStatistics = false,    // Disable stats to save memory
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool,
                MaxCorrelationHistory = 100,
                ExpansionTriggerSeverity = AlertSeverity.Critical,
                EnableDeduplication = true,
                DeduplicationWindowSeconds = 120,
                MaxPerformanceCache = 50,
                EnablePatternBasedWarming = false,
                EmergencyExpansionThreshold = 0.2f // 20% for memory-optimized
            };
        }

        /// <summary>
        /// Creates a development-friendly alert pool configuration with extensive monitoring.
        /// </summary>
        /// <param name="name">Name of the alert pool</param>
        /// <returns>Development alert pool configuration</returns>
        public static AlertPoolConfiguration CreateDevelopment(string name = "AlertPool")
        {
            return new AlertPoolConfiguration
            {
                Name = name,
                InitialCapacity = 20,
                MaxCapacity = 200,
                ValidationInterval = TimeSpan.FromMinutes(1), // Frequent validation for debugging
                MaxIdleTime = TimeSpan.FromMinutes(5),        // Short idle time for testing
                EnableValidation = true,
                EnableStatistics = true,
                DisposalPolicy = PoolDisposalPolicy.ReturnToPool,
                MaxCorrelationHistory = 500,
                ExpansionTriggerSeverity = AlertSeverity.Warning, // Lower threshold for testing
                EnableDeduplication = false,  // Disable for easier debugging
                DeduplicationWindowSeconds = 0,
                MaxPerformanceCache = 200,
                EnablePatternBasedWarming = true,
                EmergencyExpansionThreshold = 0.15f // 15% for development
            };
        }
    }
}