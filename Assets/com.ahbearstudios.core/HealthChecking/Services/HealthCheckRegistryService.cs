using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Production implementation of health check registry service.
    /// Manages thread-safe registration, storage, and retrieval of health checks.
    /// Uses ZLinq for zero-allocation operations and follows CLAUDE.md patterns.
    /// </summary>
    public sealed class HealthCheckRegistryService : IHealthCheckRegistryService
    {
        private readonly ILoggingService _logger;
        private readonly HealthCheckServiceConfig _config;
        private readonly Guid _serviceId;

        // Thread-safe collections for health check storage
        private readonly ConcurrentDictionary<FixedString64Bytes, IHealthCheck> _healthChecks;
        private readonly ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration> _configurations;
        private readonly ConcurrentDictionary<FixedString64Bytes, DateTime> _registrationTimes;
        private readonly ConcurrentDictionary<FixedString64Bytes, bool> _enabledStates;

        // Health check history storage (bounded per check)
        private readonly ConcurrentDictionary<FixedString64Bytes, ConcurrentQueue<HealthCheckResult>> _healthCheckHistory;
        private readonly DateTime _serviceStartTime;

        /// <summary>
        /// Initializes a new instance of the HealthCheckRegistryService.
        /// </summary>
        /// <param name="logger">Logging service for registry operations</param>
        /// <param name="config">Health check service configuration</param>
        public HealthCheckRegistryService(
            ILoggingService logger,
            HealthCheckServiceConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _serviceId = DeterministicIdGenerator.GenerateCoreId("HealthCheckRegistryService");

            _healthChecks = new ConcurrentDictionary<FixedString64Bytes, IHealthCheck>();
            _configurations = new ConcurrentDictionary<FixedString64Bytes, HealthCheckConfiguration>();
            _registrationTimes = new ConcurrentDictionary<FixedString64Bytes, DateTime>();
            _enabledStates = new ConcurrentDictionary<FixedString64Bytes, bool>();
            _healthCheckHistory = new ConcurrentDictionary<FixedString64Bytes, ConcurrentQueue<HealthCheckResult>>();
            _serviceStartTime = DateTime.UtcNow;

            _logger.LogDebug($"HealthCheckRegistryService initialized with ID: {_serviceId}", sourceContext: nameof(HealthCheckRegistryService));
        }

        /// <inheritdoc />
        public void RegisterHealthCheck(IHealthCheck healthCheck, HealthCheckConfiguration configuration = null)
        {
            if (healthCheck == null)
                throw new ArgumentNullException(nameof(healthCheck));

            var effectiveConfig = configuration ?? HealthCheckConfiguration.Create(healthCheck.Name.ToString());

            if (!_healthChecks.TryAdd(healthCheck.Name, healthCheck))
            {
                throw new InvalidOperationException($"Health check '{healthCheck.Name}' is already registered");
            }

            _configurations.TryAdd(healthCheck.Name, effectiveConfig);
            _registrationTimes.TryAdd(healthCheck.Name, DateTime.UtcNow);
            _enabledStates.TryAdd(healthCheck.Name, effectiveConfig.Enabled);

            _logger.LogDebug($"Registered health check: {healthCheck.Name} (Category: {healthCheck.Category}, Enabled: {effectiveConfig.Enabled})",
                sourceContext: nameof(HealthCheckRegistryService));
        }

        /// <inheritdoc />
        public void RegisterHealthChecks(Dictionary<IHealthCheck, HealthCheckConfiguration> healthChecks)
        {
            if (healthChecks == null)
                throw new ArgumentNullException(nameof(healthChecks));

            var registeredCount = 0;
            var failures = new List<string>();

            foreach (var (healthCheck, configuration) in healthChecks)
            {
                try
                {
                    RegisterHealthCheck(healthCheck, configuration);
                    registeredCount++;
                }
                catch (InvalidOperationException ex)
                {
                    failures.Add($"{healthCheck.Name}: {ex.Message}");
                    _logger.LogWarning($"Failed to register health check {healthCheck.Name}: {ex.Message}",
                        sourceContext: nameof(HealthCheckRegistryService));
                }
            }

            if (failures.Count > 0)
            {
                _logger.LogWarning($"Bulk registration completed with {registeredCount} successes and {failures.Count} failures",
                    sourceContext: nameof(HealthCheckRegistryService));
            }
            else
            {
                _logger.LogInfo($"Bulk registered {registeredCount} health checks successfully", sourceContext: nameof(HealthCheckRegistryService));
            }
        }

        /// <inheritdoc />
        public bool UnregisterHealthCheck(FixedString64Bytes name)
        {
            if (name.IsEmpty)
                return false;

            var removed = _healthChecks.TryRemove(name, out var healthCheck);

            if (removed)
            {
                _configurations.TryRemove(name, out _);
                _registrationTimes.TryRemove(name, out _);
                _enabledStates.TryRemove(name, out _);

                _logger.LogDebug($"Unregistered health check: {name}", sourceContext: nameof(HealthCheckRegistryService));
            }

            return removed;
        }

        /// <inheritdoc />
        public int UnregisterAllHealthChecks()
        {
            var count = _healthChecks.Count;

            _healthChecks.Clear();
            _configurations.Clear();
            _registrationTimes.Clear();
            _enabledStates.Clear();

            _logger.LogInfo($"Unregistered all {count} health checks", sourceContext: nameof(HealthCheckRegistryService));
            return count;
        }

        /// <inheritdoc />
        public IHealthCheck GetHealthCheck(FixedString64Bytes name)
        {
            return _healthChecks.TryGetValue(name, out var healthCheck) ? healthCheck : null;
        }

        /// <inheritdoc />
        public HealthCheckConfiguration GetHealthCheckConfiguration(FixedString64Bytes name)
        {
            return _configurations.TryGetValue(name, out var configuration) ? configuration : null;
        }

        /// <inheritdoc />
        public Dictionary<IHealthCheck, HealthCheckConfiguration> GetAllHealthChecks()
        {
            var result = new Dictionary<IHealthCheck, HealthCheckConfiguration>();

            // Use ZLinq for zero-allocation operations
            var healthCheckPairs = _healthChecks.AsValueEnumerable()
                .Where(kvp => _configurations.ContainsKey(kvp.Key))
                .ToArray();

            foreach (var kvp in healthCheckPairs)
            {
                if (_configurations.TryGetValue(kvp.Key, out var config))
                {
                    result[kvp.Value] = config;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public List<FixedString64Bytes> GetRegisteredHealthCheckNames()
        {
            return _healthChecks.Keys.AsValueEnumerable().ToList();
        }

        /// <inheritdoc />
        public List<IHealthCheck> GetHealthChecksByCategory(HealthCheckCategory category)
        {
            return _healthChecks.Values
                .AsValueEnumerable()
                .Where(hc => hc.Category == category)
                .ToList();
        }

        /// <inheritdoc />
        public bool IsHealthCheckRegistered(FixedString64Bytes name)
        {
            return !name.IsEmpty && _healthChecks.ContainsKey(name);
        }

        /// <inheritdoc />
        public bool IsHealthCheckEnabled(FixedString64Bytes name)
        {
            if (name.IsEmpty)
                return false;

            return _enabledStates.TryGetValue(name, out var enabled) && enabled;
        }

        /// <inheritdoc />
        public bool SetHealthCheckEnabled(FixedString64Bytes name, bool enabled)
        {
            if (name.IsEmpty || !_healthChecks.ContainsKey(name))
                return false;

            _enabledStates.TryUpdate(name, enabled, !enabled);

            // Update the configuration as well
            if (_configurations.TryGetValue(name, out var config))
            {
                var updatedConfig = config with { Enabled = enabled };
                _configurations.TryUpdate(name, updatedConfig, config);
            }

            _logger.LogDebug($"Health check '{name}' enabled state changed to {enabled}", sourceContext: nameof(HealthCheckRegistryService));
            return true;
        }

        /// <inheritdoc />
        public bool UpdateHealthCheckConfiguration(FixedString64Bytes name, HealthCheckConfiguration configuration)
        {
            if (name.IsEmpty || configuration == null)
                return false;

            if (!_healthChecks.ContainsKey(name))
                return false;

            _configurations.TryUpdate(name, configuration, _configurations[name]);
            _enabledStates.TryUpdate(name, configuration.Enabled, _enabledStates[name]);

            _logger.LogDebug($"Updated configuration for health check '{name}'", sourceContext: nameof(HealthCheckRegistryService));
            return true;
        }

        /// <inheritdoc />
        public int GetHealthCheckCount()
        {
            return _healthChecks.Count;
        }

        /// <inheritdoc />
        public int GetEnabledHealthCheckCount()
        {
            return _enabledStates
                .AsValueEnumerable()
                .Count(kvp => kvp.Value);
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetHealthCheckMetadata(FixedString64Bytes name)
        {
            var metadata = new Dictionary<string, object>();

            if (!_healthChecks.TryGetValue(name, out var healthCheck))
                return metadata;

            metadata["Name"] = healthCheck.Name.ToString();
            metadata["Category"] = healthCheck.Category.ToString();

            if (_configurations.TryGetValue(name, out var config))
            {
                metadata["Enabled"] = config.Enabled;
                metadata["Timeout"] = config.Timeout.TotalMilliseconds;
                metadata["CriticalToSystem"] = config.IsCritical;
                metadata["EnableAlerting"] = config.EnableAlerting;
            }

            if (_registrationTimes.TryGetValue(name, out var registrationTime))
            {
                metadata["RegisteredAt"] = registrationTime;
                metadata["RegisteredDuration"] = (DateTime.UtcNow - registrationTime).TotalMinutes;
            }

            if (_enabledStates.TryGetValue(name, out var enabled))
            {
                metadata["CurrentlyEnabled"] = enabled;
            }

            return metadata;
        }

        /// <inheritdoc />
        public void RecordHealthCheckHistory(HealthCheckResult result)
        {
            if (result == null) return;

            var name = new FixedString64Bytes(result.Name);
            var queue = _healthCheckHistory.GetOrAdd(name, _ => new ConcurrentQueue<HealthCheckResult>());

            queue.Enqueue(result);

            // Maintain bounded history per the configuration
            var maxHistory = _config.MaxHistorySize;
            while (queue.Count > maxHistory)
            {
                queue.TryDequeue(out _);
            }

            _logger.LogDebug($"Recorded health check history for {result.Name}: {result.Status}",
                sourceContext: nameof(HealthCheckRegistryService));
        }

        /// <inheritdoc />
        public List<HealthCheckResult> GetHealthCheckHistory(FixedString64Bytes name, int maxResults = 100)
        {
            if (!_healthCheckHistory.TryGetValue(name, out var queue))
                return new List<HealthCheckResult>();

            // Convert queue to list and take the most recent results
            var results = queue.ToArray()
                .AsValueEnumerable()
                .TakeLast(maxResults)
                .ToList();

            return results;
        }

        /// <inheritdoc />
        public int ClearHealthCheckHistory(FixedString64Bytes name)
        {
            if (!_healthCheckHistory.TryGetValue(name, out var queue))
                return 0;

            var count = queue.Count;

            // Clear the queue by replacing it with a new empty one
            _healthCheckHistory.TryUpdate(name, new ConcurrentQueue<HealthCheckResult>(), queue);

            _logger.LogInfo($"Cleared {count} history entries for health check: {name}",
                sourceContext: nameof(HealthCheckRegistryService));

            return count;
        }

        /// <inheritdoc />
        public HealthStatistics GetHealthStatistics()
        {
            var serviceUptime = DateTime.UtcNow - _serviceStartTime;
            var registeredCount = _healthChecks.Count;
            var enabledCount = GetEnabledHealthCheckCount();

            // Calculate totals from all history
            long totalChecks = 0;
            long successfulChecks = 0;
            long failedChecks = 0;
            var allExecutionTimes = new List<TimeSpan>();
            var circuitBreakerStats = new Dictionary<FixedString64Bytes, CircuitBreakerStatistics>();

            foreach (var kvp in _healthCheckHistory)
            {
                var results = kvp.Value.ToArray();
                totalChecks += results.Length;

                foreach (var result in results)
                {
                    if (result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Warning)
                        successfulChecks++;
                    else
                        failedChecks++;

                    allExecutionTimes.Add(result.Duration);
                }

                // Create basic circuit breaker statistics for each health check
                if (results.Length > 0)
                {
                    var lastResult = results[results.Length - 1];
                    circuitBreakerStats[kvp.Key] = new CircuitBreakerStatistics
                    {
                        Name = kvp.Key,
                        State = CircuitBreakerState.Closed, // Would be determined by actual circuit breaker
                        TotalExecutions = results.Length,
                        TotalFailures = results.Count(r => r.Status == HealthStatus.Unhealthy || r.Status == HealthStatus.Degraded),
                        TotalSuccesses = results.Count(r => r.Status == HealthStatus.Healthy || r.Status == HealthStatus.Warning),
                        LastStateChange = lastResult.Timestamp
                    };
                }
            }

            // Calculate average execution time
            var averageExecutionTime = allExecutionTimes.Count > 0
                ? TimeSpan.FromTicks(allExecutionTimes.Sum(t => t.Ticks) / allExecutionTimes.Count)
                : TimeSpan.Zero;

            return HealthStatistics.Create(
                serviceUptime: serviceUptime,
                totalHealthChecks: totalChecks,
                successfulHealthChecks: successfulChecks,
                failedHealthChecks: failedChecks,
                registeredHealthCheckCount: registeredCount,
                currentDegradationLevel: DegradationLevel.None, // Would be determined by health service
                lastOverallStatus: HealthStatus.Unknown, // Would be determined by health service
                circuitBreakerStatistics: circuitBreakerStats,
                averageExecutionTime: averageExecutionTime,
                openCircuitBreakers: 0, // Would be determined by circuit breaker service
                activeCircuitBreakers: circuitBreakerStats.Count);
        }
    }
}