using System;
using AhBearStudios.Core.Messaging.Configs;

namespace AhBearStudios.Core.Messaging.Builders
{
    /// <summary>
    /// Builder for creating MessageBusHealthConfig instances.
    /// Provides a fluent API for configuring message bus health monitoring behavior.
    /// </summary>
    public sealed class MessageBusHealthConfigBuilder
    {
        private bool _healthMonitoringEnabled = true;
        private TimeSpan _systemHealthCheckInterval = TimeSpan.FromSeconds(30);
        private bool _healthAlertingEnabled = true;
        private bool _criticalServicesOverride = true;
        private bool _performanceOptimization = false;

        /// <summary>
        /// Enables or disables health monitoring.
        /// </summary>
        /// <param name="enabled">Whether health monitoring is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusHealthConfigBuilder WithHealthMonitoringEnabled(bool enabled)
        {
            _healthMonitoringEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the system health check interval.
        /// </summary>
        /// <param name="interval">System health check interval</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusHealthConfigBuilder WithSystemHealthCheckInterval(TimeSpan interval)
        {
            _systemHealthCheckInterval = interval;
            return this;
        }

        /// <summary>
        /// Enables or disables health alerting.
        /// </summary>
        /// <param name="enabled">Whether health alerting is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusHealthConfigBuilder WithHealthAlertingEnabled(bool enabled)
        {
            _healthAlertingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables critical services override.
        /// </summary>
        /// <param name="enabled">Whether critical services override is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusHealthConfigBuilder WithCriticalServicesOverride(bool enabled)
        {
            _criticalServicesOverride = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables performance optimization.
        /// </summary>
        /// <param name="enabled">Whether performance optimization is enabled</param>
        /// <returns>The builder instance for method chaining</returns>
        public MessageBusHealthConfigBuilder WithPerformanceOptimization(bool enabled)
        {
            _performanceOptimization = enabled;
            return this;
        }

        /// <summary>
        /// Builds the MessageBusHealthConfig instance with the configured values.
        /// </summary>
        /// <returns>A new MessageBusHealthConfig instance</returns>
        public MessageBusHealthConfig Build()
        {
            return new MessageBusHealthConfig
            {
                HealthMonitoringEnabled = _healthMonitoringEnabled,
                SystemHealthCheckInterval = _systemHealthCheckInterval,
                HealthAlertingEnabled = _healthAlertingEnabled,
                CriticalServicesOverride = _criticalServicesOverride,
                HealthAggregationStrategy = _criticalServicesOverride 
                    ? HealthAggregationStrategy.CriticalOnly 
                    : HealthAggregationStrategy.Percentage
            };
        }
    }
}