using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;

namespace AhBearStudios.Core.Profiling.HealthChecks
{
    /// <summary>
    /// Health check implementation for monitoring ProfilerService performance and operational status.
    /// Integrates with the AhBearStudios Core health checking system to provide comprehensive profiler monitoring.
    /// </summary>
    /// <remarks>
    /// ProfilerHealthCheck follows the established health checking patterns in CLAUDE.md:
    /// - Implements IHealthCheck for integration with health monitoring systems
    /// - Provides detailed performance metrics and operational status
    /// - Supports configurable health criteria for different deployment scenarios
    /// - Designed for Unity's performance requirements with minimal overhead
    /// - Includes correlation tracking for distributed health monitoring
    /// 
    /// The health check monitors multiple aspects of profiler service operation:
    /// - Service availability and configuration
    /// - Performance metrics and threshold violations
    /// - Memory usage and resource consumption
    /// - Active scope tracking and limits
    /// - Error rates and service reliability
    /// </remarks>
    public sealed class ProfilerHealthCheck : IHealthCheck
    {
        #region Private Fields

        private readonly IProfilerService _profilerService;
        private readonly FixedString64Bytes _healthCheckName;
        private readonly Guid _healthCheckId;
        private readonly FixedString64Bytes _source;
        private HealthCheckConfiguration _configuration;
        private readonly TimeSpan _timeout;

        #endregion

        #region Properties

        /// <inheritdoc />
        public FixedString64Bytes Name => _healthCheckName;

        /// <inheritdoc />
        public Guid Id => _healthCheckId;

        /// <inheritdoc />
        public FixedString64Bytes Source => _source;

        /// <inheritdoc />
        public HealthCheckCategory Category => HealthCheckCategory.Performance;

        /// <inheritdoc />
        public string Description => "Monitors ProfilerService performance and operational status";

        /// <inheritdoc />
        public HealthCheckConfiguration Configuration => _configuration;

        /// <inheritdoc />
        public TimeSpan Timeout => _timeout;

        /// <inheritdoc />
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilerHealthCheck with the specified profiler service.
        /// </summary>
        /// <param name="profilerService">Profiler service to monitor</param>
        /// <param name="configuration">Health check configuration (optional)</param>
        /// <param name="source">Source system identifier (optional)</param>
        /// <param name="timeout">Health check timeout (optional)</param>
        /// <exception cref="ArgumentNullException">Thrown when profilerService is null</exception>
        public ProfilerHealthCheck(
            IProfilerService profilerService, 
            HealthCheckConfiguration configuration = null,
            FixedString64Bytes source = default,
            TimeSpan timeout = default)
        {
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _configuration = configuration;
            _source = source.IsEmpty ? "ProfilerHealthCheck" : source;
            _timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
            
            _healthCheckName = "ProfilerService";
            _healthCheckId = DeterministicIdGenerator.GenerateHealthCheckId("ProfilerHealthCheck", _source.ToString());
        }

        #endregion

        #region IHealthCheck Implementation

        /// <inheritdoc />
        public void Configure(HealthCheckConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["profiler_service_type"] = _profilerService?.GetType().Name ?? "Unknown",
                ["health_check_version"] = "1.0.0",
                ["category"] = Category.ToString(),
                ["timeout_seconds"] = Timeout.TotalSeconds,
                ["source"] = _source.ToString()
            };
        }

        /// <inheritdoc />
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);
            
            return await UniTask.FromResult(CheckHealth());
        }

        /// <inheritdoc />
        public HealthCheckResult CheckHealth()
        {
            var startTime = DateTime.UtcNow;
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("ProfilerHealthCheck", _healthCheckId.ToString());

            try
            {
                // Perform comprehensive health assessment
                var assessments = PerformHealthAssessments();
                var overallStatus = DetermineOverallHealth(assessments);
                var metrics = CollectHealthMetrics();
                var diagnosticData = CreateDiagnosticData(assessments);

                var result = new HealthCheckResult
                {
                    Name = _healthCheckName.ToString(),
                    Status = overallStatus,
                    Message = GetHealthDescription(overallStatus, assessments),
                    Duration = DateTime.UtcNow - startTime,
                    Timestamp = DateTime.UtcNow,
                    Data = CreateResultData(metrics, diagnosticData),
                    CorrelationId = correlationId.ToString()
                };

                return result;
            }
            catch (Exception ex)
            {
                // Create error result for health check failures
                var errorMetrics = new Dictionary<string, double>
                {
                    ["error_occurred"] = 1.0,
                    ["check_duration_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
                };

                var errorDiagnostics = new Dictionary<string, object>
                {
                    ["error_message"] = ex.Message,
                    ["error_type"] = ex.GetType().Name,
                    ["stack_trace"] = ex.StackTrace
                };

                return new HealthCheckResult
                {
                    Name = _healthCheckName.ToString(),
                    Status = HealthStatus.Unhealthy,
                    Message = $"Health check failed: {ex.Message}",
                    Duration = DateTime.UtcNow - startTime,
                    Timestamp = DateTime.UtcNow,
                    Data = CreateResultData(errorMetrics, errorDiagnostics),
                    CorrelationId = correlationId.ToString()
                };
            }
        }

        #endregion

        #region Private Health Assessment Methods

        /// <summary>
        /// Performs comprehensive health assessments across multiple profiler service aspects.
        /// </summary>
        /// <returns>Dictionary of assessment results keyed by assessment type</returns>
        private Dictionary<string, HealthAssessmentResult> PerformHealthAssessments()
        {
            var assessments = new Dictionary<string, HealthAssessmentResult>();

            // Core service availability assessment
            assessments["service_availability"] = AssessServiceAvailability();

            // Performance metrics assessment
            assessments["performance_metrics"] = AssessPerformanceMetrics();

            // Resource utilization assessment
            assessments["resource_utilization"] = AssessResourceUtilization();

            // Error rate assessment
            assessments["error_rate"] = AssessErrorRate();

            // Configuration validity assessment
            assessments["configuration_validity"] = AssessConfigurationValidity();

            // Scope management assessment
            assessments["scope_management"] = AssessScopeManagement();

            return assessments;
        }

        /// <summary>
        /// Assesses basic service availability and operational status.
        /// </summary>
        /// <returns>Health assessment result for service availability</returns>
        private HealthAssessmentResult AssessServiceAvailability()
        {
            try
            {
                var isHealthy = _profilerService?.PerformHealthCheck() ?? false;
                var isEnabled = _profilerService?.IsEnabled ?? false;
                var isRecording = _profilerService?.IsRecording ?? false;

                var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                var score = isHealthy ? 1.0 : 0.0;

                var details = new Dictionary<string, object>
                {
                    ["is_healthy"] = isHealthy,
                    ["is_enabled"] = isEnabled,
                    ["is_recording"] = isRecording,
                    ["service_available"] = _profilerService != null
                };

                return new HealthAssessmentResult(status, score, "Service availability check", details);
            }
            catch (Exception ex)
            {
                var details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["service_available"] = false
                };

                return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Service availability check failed", details);
            }
        }

        /// <summary>
        /// Assesses profiler performance metrics and threshold violations.
        /// </summary>
        /// <returns>Health assessment result for performance metrics</returns>
        private HealthAssessmentResult AssessPerformanceMetrics()
        {
            try
            {
                var statistics = _profilerService?.GetStatistics();
                if (statistics == null)
                {
                    return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Unable to retrieve statistics", null);
                }

                var hasPerformanceIssues = false;
                var performanceScore = 1.0;
                var details = new Dictionary<string, object>();

                // Check for performance-related statistics
                if (statistics.TryGetValue("PerformanceIssueCount", out var issueCountObj) && issueCountObj is int issueCount)
                {
                    details["performance_issue_count"] = issueCount;
                    if (issueCount > 0)
                    {
                        hasPerformanceIssues = true;
                        performanceScore = Math.Max(0.0, 1.0 - (issueCount / 100.0)); // Reduce score based on issues
                    }
                }

                if (statistics.TryGetValue("AverageExecutionTimeMs", out var avgTimeObj) && avgTimeObj is double avgTime)
                {
                    details["average_execution_time_ms"] = avgTime;
                    if (avgTime > 16.67) // Exceeds 60 FPS frame budget
                    {
                        hasPerformanceIssues = true;
                        performanceScore = Math.Min(performanceScore, 0.5);
                    }
                }

                var status = hasPerformanceIssues ? 
                    (performanceScore > 0.5 ? HealthStatus.Degraded : HealthStatus.Unhealthy) : 
                    HealthStatus.Healthy;

                return new HealthAssessmentResult(status, performanceScore, "Performance metrics assessment", details);
            }
            catch (Exception ex)
            {
                var details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                };

                return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Performance metrics assessment failed", details);
            }
        }

        /// <summary>
        /// Assesses resource utilization including memory usage and scope limits.
        /// </summary>
        /// <returns>Health assessment result for resource utilization</returns>
        private HealthAssessmentResult AssessResourceUtilization()
        {
            try
            {
                var statistics = _profilerService?.GetStatistics();
                if (statistics == null)
                {
                    return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Unable to retrieve resource statistics", null);
                }

                var resourceScore = 1.0;
                var resourceWarnings = new List<string>();
                var details = new Dictionary<string, object>();

                // Check active scope count
                if (statistics.TryGetValue("ActiveScopeCount", out var activeScopesObj) && activeScopesObj is int activeScopes)
                {
                    details["active_scope_count"] = activeScopes;
                    if (activeScopes > 1000) // High scope usage
                    {
                        resourceWarnings.Add("High active scope count");
                        resourceScore = Math.Min(resourceScore, 0.7);
                    }
                }

                // Check memory usage if available
                if (statistics.TryGetValue("EstimatedMemoryUsageBytes", out var memoryObj) && memoryObj is long memoryBytes)
                {
                    details["estimated_memory_usage_bytes"] = memoryBytes;
                    var memoryMB = memoryBytes / (1024.0 * 1024.0);
                    details["estimated_memory_usage_mb"] = memoryMB;
                    
                    if (memoryMB > 100) // Over 100MB
                    {
                        resourceWarnings.Add("High memory usage");
                        resourceScore = Math.Min(resourceScore, 0.6);
                    }
                }

                // Check metric storage count
                if (statistics.TryGetValue("MetricTagCount", out var metricTagsObj) && metricTagsObj is int metricTags)
                {
                    details["metric_tag_count"] = metricTags;
                    if (metricTags > 1000)
                    {
                        resourceWarnings.Add("High metric tag count");
                        resourceScore = Math.Min(resourceScore, 0.8);
                    }
                }

                details["resource_warnings"] = resourceWarnings.ToArray();

                var status = resourceWarnings.Count == 0 ? HealthStatus.Healthy :
                    (resourceScore > 0.6 ? HealthStatus.Degraded : HealthStatus.Unhealthy);

                return new HealthAssessmentResult(status, resourceScore, "Resource utilization assessment", details);
            }
            catch (Exception ex)
            {
                var details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                };

                return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Resource utilization assessment failed", details);
            }
        }

        /// <summary>
        /// Assesses error rates and service reliability.
        /// </summary>
        /// <returns>Health assessment result for error rate</returns>
        private HealthAssessmentResult AssessErrorRate()
        {
            try
            {
                var lastError = _profilerService?.GetLastError();
                var statistics = _profilerService?.GetStatistics();

                var hasErrors = lastError != null;
                var errorScore = hasErrors ? 0.5 : 1.0; // Reduce score if errors present

                var details = new Dictionary<string, object>
                {
                    ["has_recent_errors"] = hasErrors
                };

                if (hasErrors)
                {
                    details["last_error_message"] = lastError.Message;
                    details["last_error_type"] = lastError.GetType().Name;
                }

                // Check error flags in statistics
                if (statistics?.TryGetValue("HasErrors", out var hasErrorsObj) == true && hasErrorsObj is bool statisticsHasErrors)
                {
                    details["statistics_has_errors"] = statisticsHasErrors;
                    if (statisticsHasErrors && !hasErrors)
                    {
                        hasErrors = true;
                        errorScore = 0.7; // Less severe if only in statistics
                    }
                }

                var status = hasErrors ? HealthStatus.Degraded : HealthStatus.Healthy;

                return new HealthAssessmentResult(status, errorScore, "Error rate assessment", details);
            }
            catch (Exception ex)
            {
                var details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                };

                return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Error rate assessment failed", details);
            }
        }

        /// <summary>
        /// Assesses configuration validity and consistency.
        /// </summary>
        /// <returns>Health assessment result for configuration validity</returns>
        private HealthAssessmentResult AssessConfigurationValidity()
        {
            try
            {
                var statistics = _profilerService?.GetStatistics();
                var samplingRate = _profilerService?.SamplingRate ?? 0.0f;

                var configScore = 1.0;
                var configWarnings = new List<string>();
                var details = new Dictionary<string, object>
                {
                    ["sampling_rate"] = samplingRate
                };

                // Check sampling rate reasonableness
                if (samplingRate > 0.5f)
                {
                    configWarnings.Add("High sampling rate may impact performance");
                    configScore = Math.Min(configScore, 0.8);
                }

                // Check if service is enabled but not recording (potential misconfiguration)
                var isEnabled = _profilerService?.IsEnabled ?? false;
                var isRecording = _profilerService?.IsRecording ?? false;

                details["is_enabled"] = isEnabled;
                details["is_recording"] = isRecording;

                if (isEnabled && !isRecording)
                {
                    configWarnings.Add("Service enabled but not recording");
                    configScore = Math.Min(configScore, 0.9);
                }

                details["configuration_warnings"] = configWarnings.ToArray();

                var status = configWarnings.Count == 0 ? HealthStatus.Healthy : HealthStatus.Degraded;

                return new HealthAssessmentResult(status, configScore, "Configuration validity assessment", details);
            }
            catch (Exception ex)
            {
                var details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                };

                return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Configuration validity assessment failed", details);
            }
        }

        /// <summary>
        /// Assesses scope management and tracking functionality.
        /// </summary>
        /// <returns>Health assessment result for scope management</returns>
        private HealthAssessmentResult AssessScopeManagement()
        {
            try
            {
                var activeScopes = _profilerService?.ActiveScopeCount ?? 0;
                var totalScopes = _profilerService?.TotalScopeCount ?? 0L;

                var scopeScore = 1.0;
                var scopeWarnings = new List<string>();
                var details = new Dictionary<string, object>
                {
                    ["active_scope_count"] = activeScopes,
                    ["total_scope_count"] = totalScopes
                };

                // Test scope creation functionality
                try
                {
                    using (var testScope = _profilerService?.BeginScope("HealthCheck.ScopeTest"))
                    {
                        // Scope creation successful
                        details["scope_creation_test"] = "passed";
                    }
                }
                catch (Exception scopeEx)
                {
                    scopeWarnings.Add("Scope creation test failed");
                    details["scope_creation_test"] = "failed";
                    details["scope_creation_error"] = scopeEx.Message;
                    scopeScore = Math.Min(scopeScore, 0.5);
                }

                // Check for scope leaks (active scopes staying active too long)
                if (activeScopes > 100)
                {
                    scopeWarnings.Add("Potential scope leak detected");
                    scopeScore = Math.Min(scopeScore, 0.6);
                }

                details["scope_warnings"] = scopeWarnings.ToArray();

                var status = scopeWarnings.Count == 0 ? HealthStatus.Healthy :
                    (scopeScore > 0.5 ? HealthStatus.Degraded : HealthStatus.Unhealthy);

                return new HealthAssessmentResult(status, scopeScore, "Scope management assessment", details);
            }
            catch (Exception ex)
            {
                var details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                };

                return new HealthAssessmentResult(HealthStatus.Unhealthy, 0.0, "Scope management assessment failed", details);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Determines the overall health status based on individual assessments.
        /// </summary>
        /// <param name="assessments">Individual health assessment results</param>
        /// <returns>Overall health status</returns>
        private HealthStatus DetermineOverallHealth(Dictionary<string, HealthAssessmentResult> assessments)
        {
            if (assessments.Count == 0)
                return HealthStatus.Unhealthy;

            var hasUnhealthy = assessments.Values.AsValueEnumerable().Any(a => a.Status == HealthStatus.Unhealthy);
            var hasDegraded = assessments.Values.AsValueEnumerable().Any(a => a.Status == HealthStatus.Degraded);

            if (hasUnhealthy)
                return HealthStatus.Unhealthy;

            if (hasDegraded)
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }

        /// <summary>
        /// Creates a health description based on the overall status and assessments.
        /// </summary>
        /// <param name="overallStatus">Overall health status</param>
        /// <param name="assessments">Individual assessment results</param>
        /// <returns>Health description string</returns>
        private string GetHealthDescription(HealthStatus overallStatus, Dictionary<string, HealthAssessmentResult> assessments)
        {
            var failedAssessments = assessments
                .AsValueEnumerable()
                .Where(kvp => kvp.Value.Status == HealthStatus.Unhealthy)
                .Select(kvp => kvp.Key)
                .ToArray();

            var degradedAssessments = assessments
                .AsValueEnumerable()
                .Where(kvp => kvp.Value.Status == HealthStatus.Degraded)
                .Select(kvp => kvp.Key)
                .ToArray();

            return overallStatus switch
            {
                HealthStatus.Healthy => "ProfilerService is operating normally with all health checks passing",
                HealthStatus.Degraded => $"ProfilerService is operational but degraded. Issues: {string.Join(", ", degradedAssessments)}",
                HealthStatus.Unhealthy => $"ProfilerService has critical issues. Failed checks: {string.Join(", ", failedAssessments)}",
                _ => "ProfilerService health status is unknown"
            };
        }

        /// <summary>
        /// Collects health metrics for reporting and monitoring.
        /// </summary>
        /// <returns>Dictionary of health metrics</returns>
        private Dictionary<string, double> CollectHealthMetrics()
        {
            var metrics = new Dictionary<string, double>();

            try
            {
                var statistics = _profilerService?.GetStatistics();
                if (statistics != null)
                {
                    // Convert relevant statistics to metrics
                    if (statistics.TryGetValue("ActiveScopeCount", out var activeScopesObj) && activeScopesObj is int activeScopes)
                        metrics["active_scope_count"] = activeScopes;

                    if (statistics.TryGetValue("TotalScopeCount", out var totalScopesObj) && totalScopesObj is long totalScopes)
                        metrics["total_scope_count"] = totalScopes;

                    if (statistics.TryGetValue("SamplingRate", out var samplingRateObj) && samplingRateObj is float samplingRate)
                        metrics["sampling_rate"] = samplingRate;

                    if (statistics.TryGetValue("EstimatedMemoryUsageBytes", out var memoryObj) && memoryObj is long memoryBytes)
                        metrics["estimated_memory_usage_bytes"] = memoryBytes;
                }

                // Add service status metrics
                metrics["is_enabled"] = (_profilerService?.IsEnabled ?? false) ? 1.0 : 0.0;
                metrics["is_recording"] = (_profilerService?.IsRecording ?? false) ? 1.0 : 0.0;
                metrics["has_errors"] = (_profilerService?.GetLastError() != null) ? 1.0 : 0.0;
            }
            catch
            {
                // If metrics collection fails, add error metric
                metrics["metrics_collection_failed"] = 1.0;
            }

            return metrics;
        }

        /// <summary>
        /// Creates diagnostic data for detailed health analysis.
        /// </summary>
        /// <param name="assessments">Health assessment results</param>
        /// <returns>Dictionary of diagnostic data</returns>
        private Dictionary<string, object> CreateDiagnosticData(Dictionary<string, HealthAssessmentResult> assessments)
        {
            var diagnostics = new Dictionary<string, object>();

            // Add assessment details
            foreach (var kvp in assessments)
            {
                diagnostics[$"assessment_{kvp.Key}"] = new Dictionary<string, object>
                {
                    ["status"] = kvp.Value.Status.ToString(),
                    ["score"] = kvp.Value.Score,
                    ["description"] = kvp.Value.Description,
                    ["details"] = kvp.Value.Details
                };
            }

            // Add service information
            try
            {
                var statistics = _profilerService?.GetStatistics();
                if (statistics != null)
                {
                    diagnostics["service_statistics"] = statistics;
                }
            }
            catch (Exception ex)
            {
                diagnostics["statistics_error"] = ex.Message;
            }

            return diagnostics;
        }

        /// <summary>
        /// Creates combined result data from metrics and diagnostics.
        /// </summary>
        /// <param name="metrics">Health metrics</param>
        /// <param name="diagnostics">Diagnostic data</param>
        /// <returns>Combined data dictionary</returns>
        private Dictionary<string, object> CreateResultData(Dictionary<string, double> metrics, Dictionary<string, object> diagnostics)
        {
            var resultData = new Dictionary<string, object>();

            // Add metrics under "metrics" key
            if (metrics?.Count > 0)
            {
                resultData["metrics"] = metrics;
            }

            // Add diagnostics at root level
            if (diagnostics != null)
            {
                foreach (var kvp in diagnostics)
                {
                    resultData[kvp.Key] = kvp.Value;
                }
            }

            return resultData;
        }


        #endregion

    }
}