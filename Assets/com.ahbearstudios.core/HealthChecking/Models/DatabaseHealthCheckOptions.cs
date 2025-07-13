using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models;

/// <summary>
    /// Configuration options for database health checking
    /// </summary>
    public sealed class DatabaseHealthCheckOptions
    {
        /// <summary>
        /// Name of the health check
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what this health check monitors
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Default timeout for all database operations
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Performance threshold that triggers warning status
        /// </summary>
        public TimeSpan PerformanceWarningThreshold { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Performance threshold that triggers critical status
        /// </summary>
        public TimeSpan PerformanceCriticalThreshold { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// SQL query used to test basic connectivity
        /// </summary>
        public string ConnectionTestQuery { get; set; } = "SELECT 1";

        /// <summary>
        /// SQL query used to test query execution performance
        /// </summary>
        public string PerformanceTestQuery { get; set; }

        /// <summary>
        /// Custom health check query for application-specific validation
        /// </summary>
        public string CustomHealthQuery { get; set; }

        /// <summary>
        /// Function to validate the result of the custom health query
        /// </summary>
        public Func<object, bool> CustomQueryResultValidator { get; set; }

        /// <summary>
        /// Whether to test transaction capability
        /// </summary>
        public bool TransactionTestEnabled { get; set; } = true;

        /// <summary>
        /// Whether to use circuit breaker pattern for fault tolerance
        /// </summary>
        public bool UseCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Dependencies that must be healthy before this check runs
        /// </summary>
        public FixedString64Bytes[] Dependencies { get; set; }

        /// <summary>
        /// Creates default database health check options
        /// </summary>
        /// <returns>Default configuration</returns>
        public static DatabaseHealthCheckOptions CreateDefault()
        {
            return new DatabaseHealthCheckOptions();
        }

        /// <summary>
        /// Creates options optimized for high-performance scenarios
        /// </summary>
        /// <returns>High-performance configuration</returns>
        public static DatabaseHealthCheckOptions CreateHighPerformance()
        {
            return new DatabaseHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(15),
                PerformanceWarningThreshold = TimeSpan.FromMilliseconds(500),
                PerformanceCriticalThreshold = TimeSpan.FromSeconds(2),
                TransactionTestEnabled = false, // Skip for performance
                UseCircuitBreaker = true
            };
        }

        /// <summary>
        /// Creates options optimized for comprehensive testing
        /// </summary>
        /// <returns>Comprehensive testing configuration</returns>
        public static DatabaseHealthCheckOptions CreateComprehensive()
        {
            return new DatabaseHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromMinutes(1),
                PerformanceWarningThreshold = TimeSpan.FromSeconds(2),
                PerformanceCriticalThreshold = TimeSpan.FromSeconds(10),
                TransactionTestEnabled = true,
                UseCircuitBreaker = true,
                PerformanceTestQuery = "SELECT COUNT(*) FROM information_schema.tables"
            };
        }
    }