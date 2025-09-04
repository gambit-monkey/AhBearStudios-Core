using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using ZLinq;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Core.HealthChecking.HealthChecks
{
    /// <summary>
    /// Self-monitoring health check for the HealthCheckService itself.
    /// Monitors service statistics, circuit breaker states, degradation levels,
    /// and overall system health monitoring capability.
    /// Follows CLAUDE.md Builder → Config → Factory → Service pattern and integrates with all core systems.
    /// </summary>
    public sealed class HealthCheckServiceHealthCheck : IHealthCheck
    {
        #region Fields

        private readonly IHealthCheckService _healthCheckService;
        private readonly ILoggingService _loggingService;
        private readonly IAlertService _alertService;
        private readonly IMessageBusService _messageBus;
        private readonly IPoolingService _poolingService;
        private readonly IProfilerService _profilerService;
        private readonly ISerializationService _serializationService;
        private readonly FixedString64Bytes _healthCheckName = "HealthCheckService";
        private readonly Guid _instanceId;
        private readonly object _lockObject = new object();
        private readonly ProfilerMarker _healthCheckMarker = new ProfilerMarker("HealthCheckServiceHealthCheck.CheckHealth");
        private readonly ProfilerMarker _validationMarker = new ProfilerMarker("HealthCheckServiceHealthCheck.Validation");
        
        // Caching for performance optimization
        private DateTime _lastCheckTime = DateTime.MinValue;
        private HealthCheckResult _cachedResult;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(15);
        
        // Performance and health thresholds
        private const int MINIMUM_EXPECTED_CHECKS = 3;
        private const int DEGRADED_OPEN_CIRCUIT_BREAKERS = 2;
        private const int UNHEALTHY_OPEN_CIRCUIT_BREAKERS = 5;
        private const double DEGRADED_FAILURE_RATE = 0.25;
        private const double UNHEALTHY_FAILURE_RATE = 0.50;
        private const int DEGRADED_RESPONSE_TIME_MS = 1000;
        private const int UNHEALTHY_RESPONSE_TIME_MS = 3000;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of this health check
        /// </summary>
        public FixedString64Bytes Name => _healthCheckName;

        /// <summary>
        /// Gets the description of what this health check monitors
        /// </summary>
        public string Description => 
            "Monitors the health check service itself, including circuit breaker states, degradation levels, service statistics, and overall monitoring capability";

        /// <summary>
        /// Gets the category of this health check
        /// </summary>
        public HealthCheckCategory Category => HealthCheckCategory.System;

        /// <summary>
        /// Gets the timeout for this health check
        /// </summary>
        public TimeSpan Timeout => TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets the dependencies for this health check (none - self-monitoring)
        /// </summary>
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Gets the configuration for this health check
        /// </summary>
        public HealthCheckConfiguration Configuration { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HealthCheckServiceHealthCheck with all core system dependencies
        /// </summary>
        /// <param name="healthCheckService">The health check service to monitor</param>
        /// <param name="loggingService">The logging service for diagnostic output</param>
        /// <param name="alertService">The alert service for critical notifications</param>
        /// <param name="messageBus">The message bus for health check events</param>
        /// <param name="poolingService">The pooling service for memory management</param>
        /// <param name="profilerService">The profiler service for performance monitoring</param>
        /// <param name="serializationService">The serialization service for data serialization</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public HealthCheckServiceHealthCheck(
            IHealthCheckService healthCheckService,
            ILoggingService loggingService,
            IAlertService alertService,
            IMessageBusService messageBus,
            IPoolingService poolingService,
            IProfilerService profilerService,
            ISerializationService serializationService)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _poolingService = poolingService ?? throw new ArgumentNullException(nameof(poolingService));
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            
            _instanceId = DeterministicIdGenerator.GenerateHealthCheckId("HealthCheckServiceHealthCheck", Environment.MachineName);
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheckServiceInit", _instanceId.ToString());
            _cachedResult = HealthCheckResult.Unknown(_healthCheckName.ToString(), correlationId: correlationId.ToString());
            
            // Initialize with default configuration
            Configuration = new HealthCheckConfiguration
            {
                Name = _healthCheckName,
                Interval = TimeSpan.FromMinutes(1),
                Timeout = Timeout,
                Enabled = true,
                Category = Category
            };

            _loggingService.LogInfo(
                "HealthCheckServiceHealthCheck initialized successfully with all core system dependencies",
                correlationId: correlationId);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a comprehensive health check of the health check service itself
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the health check operation</param>
        /// <returns>The health check result with detailed status information</returns>
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            using (_healthCheckMarker.Auto())
            {
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("HealthCheck", _instanceId.ToString());
                
                // Check cache first to avoid excessive self-monitoring overhead
                lock (_lockObject)
                {
                    if (DateTime.UtcNow - _lastCheckTime < _cacheTimeout && _cachedResult != null)
                    {
                        _loggingService.LogInfo(
                            "Returning cached health check result",
                            correlationId: correlationId);
                        return _cachedResult;
                    }
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var healthData = new Dictionary<string, object>();
                var issues = new List<string>();
                var warnings = new List<string>();

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await _messageBus.PublishMessageAsync(new HealthCheckStartedMessage
                    {
                        Id = DeterministicIdGenerator.GenerateMessageId("HealthCheckStarted", source: _healthCheckName.ToString(), correlationId: null),
                        TimestampTicks = DateTime.UtcNow.Ticks,
                        TypeCode = MessageTypeCodes.HealthCheckStartedMessage,
                        Source = "HealthCheckSystem",
                        Priority = MessagePriority.Low,
                        CorrelationId = correlationId,
                        HealthCheckName = _healthCheckName.ToString(),
                        HealthCheckType = GetType().Name
                    });

                    _loggingService.LogInfo(
                        "Starting HealthCheckService self-monitoring health check",
                        correlationId: correlationId);

                    // 1. Validate service is operational
                    await ValidateServiceOperationalAsync(healthData, issues, warnings, cancellationToken, correlationId);

                    // 2. Check service statistics and performance
                    await AnalyzeServiceStatisticsAsync(healthData, issues, warnings, cancellationToken, correlationId);

                    // 3. Monitor circuit breaker states
                    await MonitorCircuitBreakersAsync(healthData, issues, warnings, cancellationToken, correlationId);

                    // 4. Assess degradation levels
                    await AssessDegradationLevelsAsync(healthData, issues, warnings, cancellationToken, correlationId);

                    // 5. Validate automatic health monitoring
                    await ValidateAutomaticMonitoringAsync(healthData, issues, warnings, cancellationToken, correlationId);

                    stopwatch.Stop();
                    var result = DetermineHealthStatus(issues, warnings, healthData, stopwatch.Elapsed, correlationId);

                    // Update cache
                    lock (_lockObject)
                    {
                        _lastCheckTime = DateTime.UtcNow;
                        _cachedResult = result;
                    }

                    // Publish completion message
                    var completionMessage = HealthCheckCompletedWithResultsMessage.Create(
                        healthCheckName: _healthCheckName.ToString(),
                        healthCheckType: GetType().Name,
                        status: result.Status,
                        message: result.Message ?? "Health check completed",
                        durationMs: stopwatch.Elapsed.TotalMilliseconds,
                        hasIssues: issues.Count > 0,
                        hasWarnings: warnings.Count > 0,
                        source: "HealthCheckSystem",
                        correlationId: correlationId,
                        priority: result.Status == HealthStatus.Healthy ? MessagePriority.Low : MessagePriority.Normal);

                    await _messageBus.PublishMessageAsync(completionMessage);

                    _loggingService.LogInfo(
                        $"HealthCheckService health check completed with status: {result.Status}",
                        correlationId: correlationId);

                    return result;
                }
                catch (OperationCanceledException)
                {
                    stopwatch.Stop();
                    var result = HealthCheckResult.Unhealthy(
                        _healthCheckName.ToString(),
                        "Health check was cancelled",
                        stopwatch.Elapsed,
                        healthData,
                        correlationId: correlationId.ToString());

                    _loggingService.LogWarning(
                        "HealthCheckService health check was cancelled",
                        correlationId: correlationId);

                    return result;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var result = HealthCheckResult.Unhealthy(
                        _healthCheckName.ToString(),
                        $"Failed to perform health check: {ex.Message}",
                        stopwatch.Elapsed,
                        healthData,
                        ex,
                        correlationId.ToString());

                    _loggingService.LogException(
                        "Critical error during HealthCheckService self-monitoring",
                        ex,
                        correlationId: correlationId.ToString());

                    return result;
                }
            }
        }

        /// <summary>
        /// Configures this health check with the specified configuration
        /// </summary>
        /// <param name="configuration">The configuration to apply</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        public void Configure(HealthCheckConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("Configure", _instanceId.ToString());
            _loggingService.LogInfo(
                $"HealthCheckServiceHealthCheck configuration updated - Interval: {configuration.Interval}, Enabled: {configuration.Enabled}",
                correlationId: correlationId);
        }

        /// <summary>
        /// Gets metadata about this health check
        /// </summary>
        /// <returns>Dictionary containing metadata about this health check</returns>
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["ServiceType"] = _healthCheckService.GetType().Name,
                ["SelfMonitoring"] = true,
                ["CircuitBreakerSupport"] = true,
                ["GracefulDegradationSupport"] = true,
                ["Category"] = Category.ToString(),
                ["CacheTimeout"] = _cacheTimeout.ToString(),
                ["MinimumExpectedChecks"] = MINIMUM_EXPECTED_CHECKS,
                ["DegradedFailureRate"] = DEGRADED_FAILURE_RATE,
                ["UnhealthyFailureRate"] = UNHEALTHY_FAILURE_RATE,
                ["InstanceId"] = _instanceId.ToString(),
                ["CoreSystemsIntegrated"] = new[] { 
                    nameof(ILoggingService), 
                    nameof(IAlertService), 
                    nameof(IMessageBusService), 
                    nameof(IPoolingService), 
                    nameof(IProfilerService), 
                    nameof(ISerializationService) 
                },
                ["CLAUDECompliant"] = true,
                ["Version"] = "2.0.0"
            };
        }

        #endregion

        #region Private Validation Methods

        /// <summary>
        /// Validates that the health check service is operational
        /// </summary>
        private async UniTask ValidateServiceOperationalAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken,
            Guid correlationId)
        {
            try
            {
                // Test basic service responsiveness
                var overallHealth = await _healthCheckService.GetOverallHealthStatusAsync();
                healthData["OverallHealthStatus"] = overallHealth.ToString();

                // Validate automatic checks are enabled if configured
                var isAutomaticEnabled = _healthCheckService.IsAutomaticChecksRunning();
                healthData["AutomaticChecksEnabled"] = isAutomaticEnabled;

                if (!isAutomaticEnabled)
                {
                    warnings.Add("Automatic health checks are disabled");
                }

                _loggingService.LogInfo(
                    $"Service operational validation completed - Overall Health: {overallHealth}, Automatic: {isAutomaticEnabled}",
                    correlationId: correlationId);
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to validate service operational status: {ex.Message}");
                _loggingService.LogException(
                    "Error validating service operational status",
                    ex,
                    correlationId: correlationId.ToString());
            }
        }

        /// <summary>
        /// Analyzes health check service statistics for performance issues
        /// </summary>
        private async UniTask AnalyzeServiceStatisticsAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken,
            Guid correlationId)
        {
            try
            {
                // Get basic health check information since detailed statistics are not available
                var registeredChecks = _healthCheckService.GetRegisteredHealthCheckNames();
                var currentStatus = await _healthCheckService.GetOverallHealthStatusAsync();
                
                healthData["RegisteredChecks"] = registeredChecks.Count;
                healthData["CurrentOverallStatus"] = currentStatus.ToString();

                // Validate minimum expected health checks are registered
                if (registeredChecks.Count < MINIMUM_EXPECTED_CHECKS)
                {
                    warnings.Add($"Low number of registered health checks: {registeredChecks.Count} (minimum expected: {MINIMUM_EXPECTED_CHECKS})");
                }

                // Basic status assessment
                switch (currentStatus)
                {
                    case HealthStatus.Unhealthy:
                        issues.Add("Overall health status is unhealthy");
                        break;
                    case HealthStatus.Degraded:
                        warnings.Add("Overall health status is degraded");
                        break;
                    case HealthStatus.Unknown:
                        warnings.Add("Overall health status is unknown");
                        break;
                }

                _loggingService.LogInfo(
                    $"Service statistics analyzed - Registered: {registeredChecks.Count}, Status: {currentStatus}",
                    correlationId: correlationId);

                await UniTask.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to analyze service statistics: {ex.Message}");
                _loggingService.LogException(
                    "Error analyzing service statistics",
                    ex,
                    correlationId: correlationId.ToString());
            }
        }

        /// <summary>
        /// Monitors circuit breaker states for system protection issues
        /// </summary>
        private async UniTask MonitorCircuitBreakersAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken,
            Guid correlationId)
        {
            try
            {
                var circuitBreakerStates = _healthCheckService.GetAllCircuitBreakerStates();
                
                var totalCircuitBreakers = circuitBreakerStates.Count;
                var openCircuitBreakers = circuitBreakerStates.AsValueEnumerable().Count(cb => cb.Value == CircuitBreakerState.Open);
                var halfOpenCircuitBreakers = circuitBreakerStates.AsValueEnumerable().Count(cb => cb.Value == CircuitBreakerState.HalfOpen);
                var closedCircuitBreakers = circuitBreakerStates.AsValueEnumerable().Count(cb => cb.Value == CircuitBreakerState.Closed);

                healthData["TotalCircuitBreakers"] = totalCircuitBreakers;
                healthData["OpenCircuitBreakers"] = openCircuitBreakers;
                healthData["HalfOpenCircuitBreakers"] = halfOpenCircuitBreakers;
                healthData["ClosedCircuitBreakers"] = closedCircuitBreakers;

                // Assess circuit breaker health
                if (openCircuitBreakers >= UNHEALTHY_OPEN_CIRCUIT_BREAKERS)
                {
                    issues.Add($"Critical number of open circuit breakers: {openCircuitBreakers} (threshold: {UNHEALTHY_OPEN_CIRCUIT_BREAKERS})");
                }
                else if (openCircuitBreakers >= DEGRADED_OPEN_CIRCUIT_BREAKERS)
                {
                    warnings.Add($"Elevated number of open circuit breakers: {openCircuitBreakers} (threshold: {DEGRADED_OPEN_CIRCUIT_BREAKERS})");
                }

                // Monitor half-open states (should be transitioning)
                if (halfOpenCircuitBreakers > 0)
                {
                    healthData["CircuitBreakersInRecovery"] = halfOpenCircuitBreakers;
                    warnings.Add($"Circuit breakers in recovery state: {halfOpenCircuitBreakers}");
                }

                // Log details of problematic circuit breakers
                var problematicBreakers = circuitBreakerStates
                    .AsValueEnumerable()
                    .Where(cb => cb.Value != CircuitBreakerState.Closed)
                    .Select(cb => $"{cb.Key}:{cb.Value}")
                    .ToList();

                if (problematicBreakers.AsValueEnumerable().Any())
                {
                    healthData["ProblematicCircuitBreakers"] = problematicBreakers;
                }

                _loggingService.LogInfo(
                    $"Circuit breaker monitoring completed - Total: {totalCircuitBreakers}, Open: {openCircuitBreakers}, Half-Open: {halfOpenCircuitBreakers}",
                    correlationId: correlationId);

                await UniTask.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to monitor circuit breakers: {ex.Message}");
                _loggingService.LogException(
                    "Error monitoring circuit breakers",
                    ex,
                    correlationId: correlationId.ToString());
            }
        }

        /// <summary>
        /// Assesses system degradation levels for graceful degradation monitoring
        /// </summary>
        private async UniTask AssessDegradationLevelsAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken,
            Guid correlationId)
        {
            try
            {
                var currentDegradationLevel = _healthCheckService.GetCurrentDegradationLevel();
                
                healthData["CurrentDegradationLevel"] = currentDegradationLevel.ToString();

                // Assess degradation severity
                switch (currentDegradationLevel)
                {
                    case DegradationLevel.Disabled:
                        issues.Add("System is in disabled degradation state");
                        break;
                    case DegradationLevel.Severe:
                        issues.Add("System is in severe degradation state");
                        break;
                    case DegradationLevel.Moderate:
                        warnings.Add("System is in moderate degradation state");
                        break;
                    case DegradationLevel.Minor:
                        healthData["SystemsInMinorDegradation"] = true;
                        break;
                    case DegradationLevel.None:
                        // No degradation - healthy state
                        break;
                }

                _loggingService.LogInfo(
                    $"Degradation assessment completed - Current Level: {currentDegradationLevel}",
                    correlationId: correlationId);

                await UniTask.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to assess degradation levels: {ex.Message}");
                _loggingService.LogException(
                    "Error assessing degradation levels",
                    ex,
                    correlationId: correlationId.ToString());
            }
        }

        /// <summary>
        /// Validates automatic health monitoring functionality
        /// </summary>
        private async UniTask ValidateAutomaticMonitoringAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken,
            Guid correlationId)
        {
            try
            {
                // Check if automatic monitoring is running
                var isAutomaticRunning = _healthCheckService.IsAutomaticChecksRunning();
                healthData["IsAutomaticMonitoringRunning"] = isAutomaticRunning;
                
                if (!isAutomaticRunning)
                {
                    warnings.Add("Automatic health monitoring is not currently running");
                }

                // Get basic status information
                var registeredChecks = _healthCheckService.GetRegisteredHealthCheckNames();
                healthData["MonitoredHealthChecksCount"] = registeredChecks.Count;

                // Validate we have health checks to monitor
                if (registeredChecks.Count == 0)
                {
                    warnings.Add("No health checks are registered for automatic monitoring");
                }
                else if (registeredChecks.Count < MINIMUM_EXPECTED_CHECKS)
                {
                    warnings.Add($"Few health checks registered: {registeredChecks.Count} (minimum expected: {MINIMUM_EXPECTED_CHECKS})");
                }

                _loggingService.LogInfo(
                    $"Automatic monitoring validation completed - Running: {isAutomaticRunning}, Registered: {registeredChecks.Count}",
                    correlationId: correlationId);

                await UniTask.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to validate automatic monitoring: {ex.Message}");
                _loggingService.LogException(
                    "Error validating automatic monitoring",
                    ex,
                    correlationId: correlationId.ToString());
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Determines the overall health status based on collected issues and warnings
        /// </summary>
        private HealthCheckResult DetermineHealthStatus(
            List<string> issues,
            List<string> warnings,
            Dictionary<string, object> healthData,
            TimeSpan duration,
            Guid correlationId)
        {
            var timestamp = DateTime.UtcNow;
            healthData["IssuesCount"] = issues.Count;
            healthData["WarningsCount"] = warnings.Count;
            healthData["CheckDuration"] = duration.TotalMilliseconds;
            healthData["Timestamp"] = timestamp;

            if (issues.AsValueEnumerable().Any())
            {
                var message = $"HealthCheckService has {issues.Count} critical issue(s)";
                var description = $"Issues: {string.Join("; ", issues)}";
                
                if (warnings.AsValueEnumerable().Any())
                {
                    description += $" | Warnings: {string.Join("; ", warnings)}";
                }

                return HealthCheckResult.Unhealthy(
                    _healthCheckName.ToString(),
                    message,
                    duration,
                    healthData,
                    correlationId: correlationId.ToString());
            }

            if (warnings.AsValueEnumerable().Any())
            {
                var message = $"HealthCheckService has {warnings.Count} warning(s) but is operational";
                var description = $"Warnings: {string.Join("; ", warnings)}";

                return HealthCheckResult.Degraded(
                    _healthCheckName.ToString(),
                    message,
                    duration,
                    healthData,
                    correlationId.ToString());
            }

            return HealthCheckResult.Healthy(
                _healthCheckName.ToString(),
                "HealthCheckService is operating normally with no issues detected",
                duration,
                healthData,
                correlationId.ToString());
        }


        #endregion
    }
}