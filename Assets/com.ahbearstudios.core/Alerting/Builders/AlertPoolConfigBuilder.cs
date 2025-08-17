using System;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;

namespace AhBearStudios.Core.Alerting.Builders
{
    /// <summary>
    /// Builder interface for creating AlertPoolConfiguration instances.
    /// Follows the Builder pattern from CLAUDE.md guidelines.
    /// </summary>
    public interface IAlertPoolConfigBuilder
    {
        /// <summary>
        /// Sets the basic pool capacity settings.
        /// </summary>
        /// <param name="initialCapacity">Initial number of alert containers to create</param>
        /// <param name="maxCapacity">Maximum number of alert containers allowed</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithCapacity(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Sets the pool validation settings.
        /// </summary>
        /// <param name="validationInterval">Time between validation checks</param>
        /// <param name="maxIdleTime">Maximum time objects can remain idle</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithValidation(TimeSpan validationInterval, TimeSpan maxIdleTime);

        /// <summary>
        /// Sets alert-specific correlation settings.
        /// </summary>
        /// <param name="maxHistory">Maximum alerts to retain for correlation</param>
        /// <param name="enableDeduplication">Whether to enable alert deduplication</param>
        /// <param name="deduplicationWindowSeconds">Deduplication time window</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithCorrelation(int maxHistory, bool enableDeduplication, int deduplicationWindowSeconds);

        /// <summary>
        /// Sets performance optimization settings.
        /// </summary>
        /// <param name="maxPerformanceCache">Maximum alerts to cache for performance analysis</param>
        /// <param name="enablePatternWarming">Whether to enable pattern-based pool warming</param>
        /// <param name="emergencyThreshold">Threshold for emergency pool expansion (0.0-1.0)</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithPerformance(int maxPerformanceCache, bool enablePatternWarming, float emergencyThreshold);

        /// <summary>
        /// Sets the alert severity that triggers pool expansion.
        /// </summary>
        /// <param name="severity">Severity level that triggers expansion</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithExpansionTrigger(AlertSeverity severity);

        /// <summary>
        /// Sets monitoring and statistics settings.
        /// </summary>
        /// <param name="enableStatistics">Whether to collect pool statistics</param>
        /// <param name="enableValidation">Whether to enable object validation</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithMonitoring(bool enableStatistics, bool enableValidation);

        /// <summary>
        /// Sets the object disposal policy.
        /// </summary>
        /// <param name="policy">How to handle object disposal</param>
        /// <returns>Builder instance for method chaining</returns>
        IAlertPoolConfigBuilder WithDisposalPolicy(PoolDisposalPolicy policy);

        /// <summary>
        /// Builds the final AlertPoolConfiguration with all specified settings.
        /// </summary>
        /// <returns>Configured AlertPoolConfiguration instance</returns>
        AlertPoolConfiguration Build();
    }

    /// <summary>
    /// Builder implementation for creating AlertPoolConfiguration instances.
    /// Handles complexity and validation, providing fluent API for configuration.
    /// Follows the Builder → Config → Factory → Service pattern from CLAUDE.md.
    /// </summary>
    public class AlertPoolConfigBuilder : IAlertPoolConfigBuilder
    {
        private string _name = "AlertPool";
        private int _initialCapacity = 20;
        private int _maxCapacity = 200;
        private TimeSpan _validationInterval = TimeSpan.FromMinutes(2);
        private TimeSpan _maxIdleTime = TimeSpan.FromMinutes(15);
        private bool _enableValidation = true;
        private bool _enableStatistics = true;
        private PoolDisposalPolicy _disposalPolicy = PoolDisposalPolicy.ReturnToPool;
        
        // Alert-specific settings
        private int _maxCorrelationHistory = 1000;
        private AlertSeverity _expansionTriggerSeverity = AlertSeverity.Critical;
        private bool _enableDeduplication = true;
        private int _deduplicationWindowSeconds = 60;
        private int _maxPerformanceCache = 500;
        private bool _enablePatternBasedWarming = true;
        private float _emergencyExpansionThreshold = 0.1f;

        /// <summary>
        /// Initializes a new AlertPoolConfigBuilder with the specified pool name.
        /// </summary>
        /// <param name="poolName">Name of the alert pool</param>
        public AlertPoolConfigBuilder(string poolName = "AlertPool")
        {
            _name = poolName ?? throw new ArgumentNullException(nameof(poolName));
        }

        /// <summary>
        /// Sets the basic pool capacity settings.
        /// </summary>
        /// <param name="initialCapacity">Initial number of alert containers to create</param>
        /// <param name="maxCapacity">Maximum number of alert containers allowed</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithCapacity(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException("Initial capacity must be greater than zero", nameof(initialCapacity));
            if (maxCapacity < initialCapacity)
                throw new ArgumentException("Max capacity must be greater than or equal to initial capacity", nameof(maxCapacity));

            _initialCapacity = initialCapacity;
            _maxCapacity = maxCapacity;
            return this;
        }

        /// <summary>
        /// Sets the pool validation settings.
        /// </summary>
        /// <param name="validationInterval">Time between validation checks</param>
        /// <param name="maxIdleTime">Maximum time objects can remain idle</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithValidation(TimeSpan validationInterval, TimeSpan maxIdleTime)
        {
            if (validationInterval <= TimeSpan.Zero)
                throw new ArgumentException("Validation interval must be positive", nameof(validationInterval));
            if (maxIdleTime <= TimeSpan.Zero)
                throw new ArgumentException("Max idle time must be positive", nameof(maxIdleTime));

            _validationInterval = validationInterval;
            _maxIdleTime = maxIdleTime;
            return this;
        }

        /// <summary>
        /// Sets alert-specific correlation settings.
        /// </summary>
        /// <param name="maxHistory">Maximum alerts to retain for correlation</param>
        /// <param name="enableDeduplication">Whether to enable alert deduplication</param>
        /// <param name="deduplicationWindowSeconds">Deduplication time window</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithCorrelation(int maxHistory, bool enableDeduplication, int deduplicationWindowSeconds)
        {
            if (maxHistory < 0)
                throw new ArgumentException("Max correlation history cannot be negative", nameof(maxHistory));
            if (deduplicationWindowSeconds < 0)
                throw new ArgumentException("Deduplication window cannot be negative", nameof(deduplicationWindowSeconds));

            _maxCorrelationHistory = maxHistory;
            _enableDeduplication = enableDeduplication;
            _deduplicationWindowSeconds = deduplicationWindowSeconds;
            return this;
        }

        /// <summary>
        /// Sets performance optimization settings.
        /// </summary>
        /// <param name="maxPerformanceCache">Maximum alerts to cache for performance analysis</param>
        /// <param name="enablePatternWarming">Whether to enable pattern-based pool warming</param>
        /// <param name="emergencyThreshold">Threshold for emergency pool expansion (0.0-1.0)</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithPerformance(int maxPerformanceCache, bool enablePatternWarming, float emergencyThreshold)
        {
            if (maxPerformanceCache < 0)
                throw new ArgumentException("Max performance cache cannot be negative", nameof(maxPerformanceCache));
            if (emergencyThreshold < 0.0f || emergencyThreshold > 1.0f)
                throw new ArgumentException("Emergency threshold must be between 0.0 and 1.0", nameof(emergencyThreshold));

            _maxPerformanceCache = maxPerformanceCache;
            _enablePatternBasedWarming = enablePatternWarming;
            _emergencyExpansionThreshold = emergencyThreshold;
            return this;
        }

        /// <summary>
        /// Sets the alert severity that triggers pool expansion.
        /// </summary>
        /// <param name="severity">Severity level that triggers expansion</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithExpansionTrigger(AlertSeverity severity)
        {
            _expansionTriggerSeverity = severity;
            return this;
        }

        /// <summary>
        /// Sets monitoring and statistics settings.
        /// </summary>
        /// <param name="enableStatistics">Whether to collect pool statistics</param>
        /// <param name="enableValidation">Whether to enable object validation</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithMonitoring(bool enableStatistics, bool enableValidation)
        {
            _enableStatistics = enableStatistics;
            _enableValidation = enableValidation;
            return this;
        }

        /// <summary>
        /// Sets the object disposal policy.
        /// </summary>
        /// <param name="policy">How to handle object disposal</param>
        /// <returns>Builder instance for method chaining</returns>
        public IAlertPoolConfigBuilder WithDisposalPolicy(PoolDisposalPolicy policy)
        {
            _disposalPolicy = policy;
            return this;
        }

        /// <summary>
        /// Builds the final AlertPoolConfiguration with all specified settings.
        /// Validates all settings and creates an immutable configuration.
        /// </summary>
        /// <returns>Configured AlertPoolConfiguration instance</returns>
        public AlertPoolConfiguration Build()
        {
            // Final validation
            if (string.IsNullOrWhiteSpace(_name))
                throw new InvalidOperationException("Pool name cannot be null or whitespace");

            if (_enableDeduplication && _deduplicationWindowSeconds <= 0)
                throw new InvalidOperationException("Deduplication window must be positive when deduplication is enabled");

            return new AlertPoolConfiguration
            {
                Name = _name,
                InitialCapacity = _initialCapacity,
                MaxCapacity = _maxCapacity,
                ValidationInterval = _validationInterval,
                MaxIdleTime = _maxIdleTime,
                EnableValidation = _enableValidation,
                EnableStatistics = _enableStatistics,
                DisposalPolicy = _disposalPolicy,
                MaxCorrelationHistory = _maxCorrelationHistory,
                ExpansionTriggerSeverity = _expansionTriggerSeverity,
                EnableDeduplication = _enableDeduplication,
                DeduplicationWindowSeconds = _deduplicationWindowSeconds,
                MaxPerformanceCache = _maxPerformanceCache,
                EnablePatternBasedWarming = _enablePatternBasedWarming,
                EmergencyExpansionThreshold = _emergencyExpansionThreshold
            };
        }

        /// <summary>
        /// Creates a builder configured for high-performance scenarios.
        /// </summary>
        /// <param name="poolName">Name of the alert pool</param>
        /// <returns>Pre-configured builder for high-performance use</returns>
        public static IAlertPoolConfigBuilder CreateHighPerformance(string poolName = "AlertPool")
        {
            return new AlertPoolConfigBuilder(poolName)
                .WithCapacity(50, 1000)
                .WithValidation(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10))
                .WithCorrelation(2000, true, 30)
                .WithPerformance(1000, true, 0.05f)
                .WithExpansionTrigger(AlertSeverity.High)
                .WithMonitoring(true, true);
        }

        /// <summary>
        /// Creates a builder configured for memory-optimized scenarios.
        /// </summary>
        /// <param name="poolName">Name of the alert pool</param>
        /// <returns>Pre-configured builder for memory-optimized use</returns>
        public static IAlertPoolConfigBuilder CreateMemoryOptimized(string poolName = "AlertPool")
        {
            return new AlertPoolConfigBuilder(poolName)
                .WithCapacity(10, 100)
                .WithValidation(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
                .WithCorrelation(100, true, 120)
                .WithPerformance(50, false, 0.2f)
                .WithExpansionTrigger(AlertSeverity.Critical)
                .WithMonitoring(false, true);
        }

        /// <summary>
        /// Creates a builder configured for development scenarios.
        /// </summary>
        /// <param name="poolName">Name of the alert pool</param>
        /// <returns>Pre-configured builder for development use</returns>
        public static IAlertPoolConfigBuilder CreateDevelopment(string poolName = "AlertPool")
        {
            return new AlertPoolConfigBuilder(poolName)
                .WithCapacity(20, 200)
                .WithValidation(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5))
                .WithCorrelation(500, false, 0)
                .WithPerformance(200, true, 0.15f)
                .WithExpansionTrigger(AlertSeverity.Warning)
                .WithMonitoring(true, true);
        }
    }
}