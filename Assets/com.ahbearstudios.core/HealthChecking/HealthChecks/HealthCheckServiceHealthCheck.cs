using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.HealthChecks
{
    /// <summary>
    /// Self-monitoring health check for the HealthCheckService itself.
    /// Monitors service statistics, circuit breaker states, degradation levels,
    /// and overall system health monitoring capability.
    /// Follows AhBearStudios Core Architecture health monitoring requirements.
    /// </summary>
    public sealed class HealthCheckServiceHealthCheck : IHealthCheck
    {
        #region Fields

        private readonly IHealthCheckService _healthCheckService;
        private readonly ILoggingService _loggingService;
        private readonly FixedString64Bytes _healthCheckName = "HealthCheckService";
        private readonly FixedString64Bytes _correlationId;
        private readonly object _lockObject = new object();
        
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
        /// Initializes a new instance of the HealthCheckServiceHealthCheck
        /// </summary>
        /// <param name="healthCheckService">The health check service to monitor</param>
        /// <param name="loggingService">The logging service for diagnostic output</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public HealthCheckServiceHealthCheck(
            IHealthCheckService healthCheckService,
            ILoggingService loggingService)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            _correlationId = GenerateCorrelationId();
            _cachedResult = HealthCheckResult.Unknown(_healthCheckName.ToString(), correlationId: _correlationId);
            
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
                "HealthCheckServiceHealthCheck initialized successfully",
                _correlationId);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a comprehensive health check of the health check service itself
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the health check operation</param>
        /// <returns>The health check result with detailed status information</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            // Check cache first to avoid excessive self-monitoring overhead
            lock (_lockObject)
            {
                if (DateTime.UtcNow - _lastCheckTime < _cacheTimeout && _cachedResult != null)
                {
                    _loggingService.LogInfo(
                        "Returning cached health check result",
                        _correlationId);
                    return _cachedResult;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            var healthData = new Dictionary<string, object>();
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _loggingService.LogInfo(
                    "Starting HealthCheckService self-monitoring health check",
                    _correlationId);

                // 1. Validate service is operational
                await ValidateServiceOperationalAsync(healthData, issues, warnings, cancellationToken);

                // 2. Check service statistics and performance
                await AnalyzeServiceStatisticsAsync(healthData, issues, warnings, cancellationToken);

                // 3. Monitor circuit breaker states
                await MonitorCircuitBreakersAsync(healthData, issues, warnings, cancellationToken);

                // 4. Assess degradation levels
                await AssessDegradationLevelsAsync(healthData, issues, warnings, cancellationToken);

                // 5. Validate automatic health monitoring
                await ValidateAutomaticMonitoringAsync(healthData, issues, warnings, cancellationToken);

                stopwatch.Stop();
                var result = DetermineHealthStatus(issues, warnings, healthData, stopwatch.Elapsed);

                // Update cache
                lock (_lockObject)
                {
                    _lastCheckTime = DateTime.UtcNow;
                    _cachedResult = result;
                }

                _loggingService.LogInfo(
                    $"HealthCheckService health check completed with status: {result.Status}",
                    _correlationId);

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
                    correlationId: _correlationId);

                _loggingService.LogWarning(
                    "HealthCheckService health check was cancelled",
                    _correlationId);

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
                    _correlationId);

                _loggingService.LogException(
                    ex,
                    "Critical error during HealthCheckService self-monitoring",
                    _correlationId);

                return result;
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
            
            _loggingService.LogInfo(
                $"HealthCheckServiceHealthCheck configuration updated - Interval: {configuration.Interval}, Enabled: {configuration.Enabled}",
                _correlationId);
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
                ["CorrelationId"] = _correlationId.ToString()
            };
        }

        #endregion

        #region Private Validation Methods

        /// <summary>
        /// Validates that the health check service is operational
        /// </summary>
        private async Task ValidateServiceOperationalAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                // Test basic service responsiveness
                var overallHealth = await _healthCheckService.GetOverallHealthStatusAsync();
                healthData["OverallHealthStatus"] = overallHealth.ToString();

                // Validate automatic checks are enabled if configured
                var isAutomaticEnabled = _healthCheckService.IsAutomaticChecksEnabled;
                healthData["AutomaticChecksEnabled"] = isAutomaticEnabled;

                if (!isAutomaticEnabled)
                {
                    warnings.Add("Automatic health checks are disabled");
                }

                _loggingService.LogInfo(
                    $"Service operational validation completed - Overall Health: {overallHealth}, Automatic: {isAutomaticEnabled}",
                    _correlationId);
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to validate service operational status: {ex.Message}");
                _loggingService.LogException(ex, "Error validating service operational status", _correlationId);
            }
        }

        /// <summary>
        /// Analyzes health check service statistics for performance issues
        /// </summary>
        private async Task AnalyzeServiceStatisticsAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                var statistics = _healthCheckService.GetStatistics();
                
                healthData["TotalHealthChecks"] = statistics.TotalHealthChecks;
                healthData["RegisteredChecks"] = statistics.RegisteredHealthChecks;
                healthData["SuccessfulChecks"] = statistics.SuccessfulHealthChecks;
                healthData["FailedChecks"] = statistics.FailedHealthChecks;
                healthData["AverageExecutionTime"] = statistics.AverageExecutionTime.TotalMilliseconds;
                healthData["LastExecutionTime"] = statistics.LastExecutionTime;

                // Validate minimum expected health checks are registered
                if (statistics.RegisteredHealthChecks < MINIMUM_EXPECTED_CHECKS)
                {
                    warnings.Add($"Low number of registered health checks: {statistics.RegisteredHealthChecks} (minimum expected: {MINIMUM_EXPECTED_CHECKS})");
                }

                // Check failure rate
                var failureRate = statistics.TotalHealthChecks > 0 
                    ? (double)statistics.FailedHealthChecks / statistics.TotalHealthChecks 
                    : 0.0;
                
                healthData["FailureRate"] = failureRate;

                if (failureRate >= UNHEALTHY_FAILURE_RATE)
                {
                    issues.Add($"High failure rate detected: {failureRate:P2} (threshold: {UNHEALTHY_FAILURE_RATE:P2})");
                }
                else if (failureRate >= DEGRADED_FAILURE_RATE)
                {
                    warnings.Add($"Elevated failure rate detected: {failureRate:P2} (threshold: {DEGRADED_FAILURE_RATE:P2})");
                }

                // Check average execution time
                var avgExecutionMs = statistics.AverageExecutionTime.TotalMilliseconds;
                if (avgExecutionMs >= UNHEALTHY_RESPONSE_TIME_MS)
                {
                    issues.Add($"High average execution time: {avgExecutionMs:F0}ms (threshold: {UNHEALTHY_RESPONSE_TIME_MS}ms)");
                }
                else if (avgExecutionMs >= DEGRADED_RESPONSE_TIME_MS)
                {
                    warnings.Add($"Elevated average execution time: {avgExecutionMs:F0}ms (threshold: {DEGRADED_RESPONSE_TIME_MS}ms)");
                }

                _loggingService.LogInfo(
                    $"Service statistics analyzed - Registered: {statistics.RegisteredHealthChecks}, Failure Rate: {failureRate:P2}, Avg Time: {avgExecutionMs:F0}ms",
                    _correlationId);

                await Task.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to analyze service statistics: {ex.Message}");
                _loggingService.LogException(ex, "Error analyzing service statistics", _correlationId);
            }
        }

        /// <summary>
        /// Monitors circuit breaker states for system protection issues
        /// </summary>
        private async Task MonitorCircuitBreakersAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                var circuitBreakerStates = _healthCheckService.GetAllCircuitBreakerStates();
                
                var totalCircuitBreakers = circuitBreakerStates.Count;
                var openCircuitBreakers = circuitBreakerStates.Count(cb => cb.Value == CircuitBreakerState.Open);
                var halfOpenCircuitBreakers = circuitBreakerStates.Count(cb => cb.Value == CircuitBreakerState.HalfOpen);
                var closedCircuitBreakers = circuitBreakerStates.Count(cb => cb.Value == CircuitBreakerState.Closed);

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
                    .Where(cb => cb.Value != CircuitBreakerState.Closed)
                    .Select(cb => $"{cb.Key}:{cb.Value}")
                    .ToList();

                if (problematicBreakers.Any())
                {
                    healthData["ProblematicCircuitBreakers"] = problematicBreakers;
                }

                _loggingService.LogInfo(
                    $"Circuit breaker monitoring completed - Total: {totalCircuitBreakers}, Open: {openCircuitBreakers}, Half-Open: {halfOpenCircuitBreakers}",
                    _correlationId);

                await Task.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to monitor circuit breakers: {ex.Message}");
                _loggingService.LogException(ex, "Error monitoring circuit breakers", _correlationId);
            }
        }

        /// <summary>
        /// Assesses system degradation levels for graceful degradation monitoring
        /// </summary>
        private async Task AssessDegradationLevelsAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                var degradationStatus = _healthCheckService.GetDegradationStatus();
                
                healthData["SystemsInDegradation"] = degradationStatus.Count;
                
                var degradationSummary = new Dictionary<string, int>();
                foreach (DegradationLevel level in Enum.GetValues<DegradationLevel>())
                {
                    var count = degradationStatus.Count(ds => ds.Value == level);
                    degradationSummary[level.ToString()] = count;
                }
                
                healthData["DegradationSummary"] = degradationSummary;

                // Assess degradation severity
                var severeCount = degradationSummary.GetValueOrDefault("Severe", 0);
                var disabledCount = degradationSummary.GetValueOrDefault("Disabled", 0);
                var moderateCount = degradationSummary.GetValueOrDefault("Moderate", 0);
                var minorCount = degradationSummary.GetValueOrDefault("Minor", 0);

                if (disabledCount > 0)
                {
                    issues.Add($"Systems in disabled state: {disabledCount}");
                }

                if (severeCount > 0)
                {
                    issues.Add($"Systems in severe degradation: {severeCount}");
                }

                if (moderateCount > 0)
                {
                    warnings.Add($"Systems in moderate degradation: {moderateCount}");
                }

                if (minorCount > 0)
                {
                    healthData["SystemsInMinorDegradation"] = minorCount;
                }

                // Log specific degraded systems
                var degradedSystems = degradationStatus
                    .Where(ds => ds.Value != DegradationLevel.None)
                    .ToDictionary(ds => ds.Key, ds => ds.Value.ToString());

                if (degradedSystems.Any())
                {
                    healthData["DegradedSystems"] = degradedSystems;
                }

                _loggingService.LogInfo(
                    $"Degradation assessment completed - Severe: {severeCount}, Moderate: {moderateCount}, Minor: {minorCount}, Disabled: {disabledCount}",
                    _correlationId);

                await Task.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to assess degradation levels: {ex.Message}");
                _loggingService.LogException(ex, "Error assessing degradation levels", _correlationId);
            }
        }

        /// <summary>
        /// Validates automatic health monitoring functionality
        /// </summary>
        private async Task ValidateAutomaticMonitoringAsync(
            Dictionary<string, object> healthData,
            List<string> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get last health check results to validate monitoring is active
                var lastResults = _healthCheckService.GetLastResults().ToList();
                
                healthData["LastResultsCount"] = lastResults.Count;
                
                if (lastResults.Any())
                {
                    var oldestResult = lastResults.Min(r => r.Timestamp);
                    var newestResult = lastResults.Max(r => r.Timestamp);
                    var timeSinceLastCheck = DateTime.UtcNow - newestResult;
                    
                    healthData["OldestResultAge"] = (DateTime.UtcNow - oldestResult).TotalMinutes;
                    healthData["NewestResultAge"] = timeSinceLastCheck.TotalMinutes;
                    
                    // Validate recent activity
                    if (timeSinceLastCheck > TimeSpan.FromMinutes(10))
                    {
                        warnings.Add($"No recent health check activity detected: {timeSinceLastCheck.TotalMinutes:F1} minutes ago");
                    }
                    
                    // Check result distribution
                    var healthyCount = lastResults.Count(r => r.Status == HealthStatus.Healthy);
                    var degradedCount = lastResults.Count(r => r.Status == HealthStatus.Degraded);
                    var unhealthyCount = lastResults.Count(r => r.Status == HealthStatus.Unhealthy);
                    
                    healthData["RecentHealthyResults"] = healthyCount;
                    healthData["RecentDegradedResults"] = degradedCount;
                    healthData["RecentUnhealthyResults"] = unhealthyCount;
                }
                else
                {
                    warnings.Add("No recent health check results available");
                }

                _loggingService.LogInfo(
                    $"Automatic monitoring validation completed - Recent results: {lastResults.Count}",
                    _correlationId);

                await Task.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to validate automatic monitoring: {ex.Message}");
                _loggingService.LogException(ex, "Error validating automatic monitoring", _correlationId);
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
            TimeSpan duration)
        {
            var timestamp = DateTime.UtcNow;
            healthData["IssuesCount"] = issues.Count;
            healthData["WarningsCount"] = warnings.Count;
            healthData["CheckDuration"] = duration.TotalMilliseconds;
            healthData["Timestamp"] = timestamp;

            if (issues.Any())
            {
                var message = $"HealthCheckService has {issues.Count} critical issue(s)";
                var description = $"Issues: {string.Join("; ", issues)}";
                
                if (warnings.Any())
                {
                    description += $" | Warnings: {string.Join("; ", warnings)}";
                }

                return HealthCheckResult.Unhealthy(
                    _healthCheckName.ToString(),
                    message,
                    duration,
                    healthData,
                    correlationId: _correlationId);
            }

            if (warnings.Any())
            {
                var message = $"HealthCheckService has {warnings.Count} warning(s) but is operational";
                var description = $"Warnings: {string.Join("; ", warnings)}";

                return HealthCheckResult.Degraded(
                    _healthCheckName.ToString(),
                    message,
                    duration,
                    healthData,
                    _correlationId);
            }

            return HealthCheckResult.Healthy(
                _healthCheckName.ToString(),
                "HealthCheckService is operating normally with no issues detected",
                duration,
                healthData,
                _correlationId);
        }

        /// <summary>
        /// Generates a unique correlation ID for tracing and debugging
        /// </summary>
        private static FixedString64Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..16];
            return new FixedString64Bytes($"HCS-{guid}");
        }

        #endregion
    }
}