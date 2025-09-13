using System;
using System.Collections.Generic;

namespace AhBearStudios.Core.Messaging.Configs
{
    /// <summary>
    /// Configuration for message bus health service.
    /// Focused on health monitoring and coordination settings.
    /// </summary>
    public sealed class MessageBusHealthConfig
    {
        #region Core Health Configuration

        /// <summary>
        /// Gets or sets whether health monitoring is enabled.
        /// </summary>
        public bool HealthMonitoringEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the overall system health check interval.
        /// </summary>
        public TimeSpan SystemHealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the default service health check interval.
        /// </summary>
        public TimeSpan DefaultServiceHealthCheckInterval { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Gets or sets whether to enable health status history tracking.
        /// </summary>
        public bool HealthHistoryEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets how long to retain health status history.
        /// </summary>
        public TimeSpan HealthHistoryRetention { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the maximum number of health status changes to keep in memory.
        /// </summary>
        public int MaxHealthHistoryEntries { get; set; } = 10000;

        #endregion

        #region Service Configuration

        /// <summary>
        /// Gets or sets per-service health check intervals.
        /// </summary>
        public Dictionary<string, TimeSpan> ServiceHealthCheckIntervals { get; set; } = new Dictionary<string, TimeSpan>();

        /// <summary>
        /// Gets or sets service health dependencies.
        /// </summary>
        public Dictionary<string, HashSet<string>> ServiceDependencies { get; set; } = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Gets or sets the maximum number of services to monitor.
        /// </summary>
        public int MaxMonitoredServices { get; set; } = 100;

        /// <summary>
        /// Gets or sets the timeout for individual service health checks.
        /// </summary>
        public TimeSpan ServiceHealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);

        #endregion

        #region Health Status Determination

        /// <summary>
        /// Gets or sets the strategy for determining overall health status.
        /// </summary>
        public HealthAggregationStrategy HealthAggregationStrategy { get; set; } = HealthAggregationStrategy.WorstCase;

        /// <summary>
        /// Gets or sets the minimum percentage of healthy services required for overall healthy status.
        /// </summary>
        public double MinimumHealthyServicesPercentage { get; set; } = 0.8; // 80%

        /// <summary>
        /// Gets or sets whether critical services can override overall health status.
        /// </summary>
        public bool CriticalServicesOverride { get; set; } = true;

        /// <summary>
        /// Gets or sets which services are considered critical.
        /// </summary>
        public HashSet<string> CriticalServices { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the grace period before marking a service as unhealthy after failure.
        /// </summary>
        public TimeSpan HealthStatusGracePeriod { get; set; } = TimeSpan.FromSeconds(30);

        #endregion

        #region Alerting Configuration

        /// <summary>
        /// Gets or sets whether to enable alerting for health status changes.
        /// </summary>
        public bool HealthAlertingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum time between duplicate health alerts.
        /// </summary>
        public TimeSpan AlertSuppressionTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether to escalate health alerts based on severity.
        /// </summary>
        public bool AlertEscalationEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the time before escalating an unresolved health alert.
        /// </summary>
        public TimeSpan AlertEscalationTime { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets whether to alert on service recovery.
        /// </summary>
        public bool AlertOnRecovery { get; set; } = true;

        #endregion

        #region Administrative Overrides

        /// <summary>
        /// Gets or sets whether administrative overrides are allowed.
        /// </summary>
        public bool AllowAdministrativeOverrides { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum duration for administrative overrides.
        /// </summary>
        public TimeSpan MaxAdministrativeOverrideDuration { get; set; } = TimeSpan.FromHours(8);

        /// <summary>
        /// Gets or sets whether to log administrative overrides.
        /// </summary>
        public bool LogAdministrativeOverrides { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to alert on administrative overrides.
        /// </summary>
        public bool AlertOnAdministrativeOverrides { get; set; } = true;

        #endregion

        #region Performance Configuration

        /// <summary>
        /// Gets or sets the maximum number of concurrent health checks.
        /// </summary>
        public int MaxConcurrentHealthChecks { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Gets or sets whether to cache health check results.
        /// </summary>
        public bool CacheHealthCheckResults { get; set; } = true;

        /// <summary>
        /// Gets or sets how long to cache health check results.
        /// </summary>
        public TimeSpan HealthCheckCacheDuration { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether to perform health checks in parallel.
        /// </summary>
        public bool ParallelHealthChecks { get; set; } = true;

        #endregion

        #region Memory Management

        /// <summary>
        /// Gets or sets the maximum memory pressure threshold for health data.
        /// </summary>
        public long MaxHealthMemoryPressure { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Gets or sets whether to automatically cleanup old health data.
        /// </summary>
        public bool AutoCleanupOldHealthData { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for cleaning up old health data.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);

        #endregion

        #region Validation

        /// <summary>
        /// Validates the configuration for correctness and completeness.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool IsValid()
        {
            if (SystemHealthCheckInterval <= TimeSpan.Zero) return false;
            if (DefaultServiceHealthCheckInterval <= TimeSpan.Zero) return false;
            if (HealthHistoryRetention <= TimeSpan.Zero) return false;
            if (MaxHealthHistoryEntries <= 0) return false;
            if (MaxMonitoredServices <= 0) return false;
            if (ServiceHealthCheckTimeout <= TimeSpan.Zero) return false;
            
            if (MinimumHealthyServicesPercentage < 0 || MinimumHealthyServicesPercentage > 1) return false;
            if (HealthStatusGracePeriod < TimeSpan.Zero) return false;
            
            if (AlertSuppressionTime < TimeSpan.Zero) return false;
            if (AlertEscalationTime < TimeSpan.Zero) return false;
            
            if (MaxAdministrativeOverrideDuration <= TimeSpan.Zero) return false;
            
            if (MaxConcurrentHealthChecks <= 0) return false;
            if (HealthCheckCacheDuration < TimeSpan.Zero) return false;
            
            if (MaxHealthMemoryPressure <= 0) return false;
            if (CleanupInterval <= TimeSpan.Zero) return false;

            // Validate service dependencies don't have circular references
            if (ServiceDependencies != null)
            {
                if (HasCircularDependencies())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for circular dependencies in service configuration.
        /// </summary>
        /// <returns>True if circular dependencies exist</returns>
        private bool HasCircularDependencies()
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var service in ServiceDependencies.Keys)
            {
                if (HasCircularDependencyHelper(service, visited, recursionStack))
                    return true;
            }

            return false;
        }

        private bool HasCircularDependencyHelper(string service, HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(service))
                return true;

            if (visited.Contains(service))
                return false;

            visited.Add(service);
            recursionStack.Add(service);

            if (ServiceDependencies.TryGetValue(service, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (HasCircularDependencyHelper(dependency, visited, recursionStack))
                        return true;
                }
            }

            recursionStack.Remove(service);
            return false;
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>Deep copy of the configuration</returns>
        public MessageBusHealthConfig Clone()
        {
            var clone = new MessageBusHealthConfig
            {
                HealthMonitoringEnabled = HealthMonitoringEnabled,
                SystemHealthCheckInterval = SystemHealthCheckInterval,
                DefaultServiceHealthCheckInterval = DefaultServiceHealthCheckInterval,
                HealthHistoryEnabled = HealthHistoryEnabled,
                HealthHistoryRetention = HealthHistoryRetention,
                MaxHealthHistoryEntries = MaxHealthHistoryEntries,
                MaxMonitoredServices = MaxMonitoredServices,
                ServiceHealthCheckTimeout = ServiceHealthCheckTimeout,
                HealthAggregationStrategy = HealthAggregationStrategy,
                MinimumHealthyServicesPercentage = MinimumHealthyServicesPercentage,
                CriticalServicesOverride = CriticalServicesOverride,
                HealthStatusGracePeriod = HealthStatusGracePeriod,
                HealthAlertingEnabled = HealthAlertingEnabled,
                AlertSuppressionTime = AlertSuppressionTime,
                AlertEscalationEnabled = AlertEscalationEnabled,
                AlertEscalationTime = AlertEscalationTime,
                AlertOnRecovery = AlertOnRecovery,
                AllowAdministrativeOverrides = AllowAdministrativeOverrides,
                MaxAdministrativeOverrideDuration = MaxAdministrativeOverrideDuration,
                LogAdministrativeOverrides = LogAdministrativeOverrides,
                AlertOnAdministrativeOverrides = AlertOnAdministrativeOverrides,
                MaxConcurrentHealthChecks = MaxConcurrentHealthChecks,
                CacheHealthCheckResults = CacheHealthCheckResults,
                HealthCheckCacheDuration = HealthCheckCacheDuration,
                ParallelHealthChecks = ParallelHealthChecks,
                MaxHealthMemoryPressure = MaxHealthMemoryPressure,
                AutoCleanupOldHealthData = AutoCleanupOldHealthData,
                CleanupInterval = CleanupInterval,
                ServiceHealthCheckIntervals = new Dictionary<string, TimeSpan>(),
                ServiceDependencies = new Dictionary<string, HashSet<string>>(),
                CriticalServices = new HashSet<string>()
            };

            // Deep copy collections
            foreach (var interval in ServiceHealthCheckIntervals)
            {
                clone.ServiceHealthCheckIntervals[interval.Key] = interval.Value;
            }

            foreach (var dependency in ServiceDependencies)
            {
                clone.ServiceDependencies[dependency.Key] = new HashSet<string>(dependency.Value);
            }

            foreach (var service in CriticalServices)
            {
                clone.CriticalServices.Add(service);
            }

            return clone;
        }

        /// <summary>
        /// Returns a string representation of the configuration.
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public override string ToString()
        {
            return $"MessageBusHealthConfig: " +
                   $"Enabled={HealthMonitoringEnabled}, " +
                   $"SystemInterval={SystemHealthCheckInterval.TotalSeconds}s, " +
                   $"ServiceInterval={DefaultServiceHealthCheckInterval.TotalSeconds}s, " +
                   $"Strategy={HealthAggregationStrategy}, " +
                   $"MinHealthy={MinimumHealthyServicesPercentage:P}, " +
                   $"CriticalOverride={CriticalServicesOverride}, " +
                   $"Alerting={HealthAlertingEnabled}";
        }

        #endregion
    }

    /// <summary>
    /// Enum for health aggregation strategies.
    /// </summary>
    public enum HealthAggregationStrategy
    {
        /// <summary>
        /// Overall health is the worst of all services.
        /// </summary>
        WorstCase,

        /// <summary>
        /// Overall health is based on majority of services.
        /// </summary>
        Majority,

        /// <summary>
        /// Overall health is based on minimum healthy percentage.
        /// </summary>
        Percentage,

        /// <summary>
        /// Overall health is based only on critical services.
        /// </summary>
        CriticalOnly,

        /// <summary>
        /// Overall health is weighted based on service importance.
        /// </summary>
        Weighted
    }
}