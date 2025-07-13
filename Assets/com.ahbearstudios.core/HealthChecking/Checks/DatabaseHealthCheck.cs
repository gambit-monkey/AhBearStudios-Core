using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Checks
{
    /// <summary>
    /// Health check for monitoring database connectivity, performance, and operational status
    /// </summary>
    /// <remarks>
    /// Provides comprehensive database health monitoring including:
    /// - Connection establishment and validation
    /// - Query execution performance measurement
    /// - Transaction capability verification
    /// - Database-specific health metrics collection
    /// - Circuit breaker integration for fault tolerance
    /// </remarks>
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IDatabaseService _databaseService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILoggingService _logger;
        private readonly DatabaseHealthCheckOptions _options;
        
        private HealthCheckConfiguration _configuration;
        private readonly object _configurationLock = new object();

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public HealthCheckCategory Category => HealthCheckCategory.Database;

        /// <inheritdoc />
        public TimeSpan Timeout => _configuration?.Timeout ?? _options.DefaultTimeout;

        /// <inheritdoc />
        public HealthCheckConfiguration Configuration 
        { 
            get 
            { 
                lock (_configurationLock) 
                { 
                    return _configuration; 
                } 
            } 
        }

        /// <inheritdoc />
        public IEnumerable<FixedString64Bytes> Dependencies => _options.Dependencies ?? Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Initializes the database health check with required dependencies and configuration
        /// </summary>
        /// <param name="databaseService">Database service to monitor</param>
        /// <param name="healthCheckService">Health check service for circuit breaker integration</param>
        /// <param name="logger">Logging service for diagnostic information</param>
        /// <param name="options">Optional configuration for database health checking</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public DatabaseHealthCheck(
            IDatabaseService databaseService,
            IHealthCheckService healthCheckService,
            ILoggingService logger,
            DatabaseHealthCheckOptions options = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? DatabaseHealthCheckOptions.CreateDefault();

            Name = new FixedString64Bytes(_options.Name ?? "DatabaseHealth");
            Description = _options.Description ?? $"Database connectivity and performance monitoring for {_databaseService.GetType().Name}";

            // Set default configuration
            _configuration = HealthCheckConfiguration.ForDatabase(
                Name.ToString(), 
                Description);

            _logger.LogInfo($"DatabaseHealthCheck '{Name}' initialized with comprehensive monitoring");
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug($"Starting database health check '{Name}'");

                // Execute health checks with circuit breaker protection
                if (_options.UseCircuitBreaker && _healthCheckService != null)
                {
                    return await _healthCheckService.ExecuteWithProtectionAsync(
                        $"Database.{Name}",
                        () => ExecuteHealthCheckInternal(data, cancellationToken),
                        () => CreateCircuitBreakerFallbackResult(stopwatch.Elapsed, data),
                        cancellationToken);
                }
                else
                {
                    return await ExecuteHealthCheckInternal(data, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                data["CancellationRequested"] = true;
                _logger.LogDebug($"Database health check '{Name}' was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, $"Database health check '{Name}' failed with unexpected error");
                
                data["Exception"] = ex.GetType().Name;
                data["ErrorMessage"] = ex.Message;
                data["StackTrace"] = ex.StackTrace;

                return HealthCheckResult.Unhealthy(
                    $"Database health check failed: {ex.Message}",
                    stopwatch.Elapsed,
                    data,
                    ex);
            }
        }

        /// <inheritdoc />
        public void Configure(HealthCheckConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            lock (_configurationLock)
            {
                _configuration = configuration;
            }

            _logger.LogInfo($"DatabaseHealthCheck '{Name}' configuration updated");
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Type"] = nameof(DatabaseHealthCheck),
                ["Category"] = Category.ToString(),
                ["Description"] = Description,
                ["DatabaseServiceType"] = _databaseService?.GetType().Name,
                ["SupportedOperations"] = new[] 
                { 
                    "ConnectionTest", 
                    "QueryExecution", 
                    "TransactionTest", 
                    "PerformanceMeasurement" 
                },
                ["CircuitBreakerEnabled"] = _options.UseCircuitBreaker,
                ["PerformanceThresholds"] = new Dictionary<string, object>
                {
                    ["WarningThreshold"] = _options.PerformanceWarningThreshold.TotalMilliseconds,
                    ["CriticalThreshold"] = _options.PerformanceCriticalThreshold.TotalMilliseconds,
                    ["TimeoutThreshold"] = _options.DefaultTimeout.TotalMilliseconds
                },
                ["TestQueries"] = new Dictionary<string, object>
                {
                    ["ConnectionTest"] = _options.ConnectionTestQuery,
                    ["PerformanceTest"] = _options.PerformanceTestQuery,
                    ["TransactionTest"] = _options.TransactionTestEnabled
                },
                ["Dependencies"] = Dependencies,
                ["Version"] = "1.0.0",
                ["SupportsTransactions"] = _options.TransactionTestEnabled,
                ["SupportsCustomQueries"] = !string.IsNullOrEmpty(_options.CustomHealthQuery)
            };
        }

        #region Private Implementation

        private async Task<HealthCheckResult> ExecuteHealthCheckInternal(
            Dictionary<string, object> data, 
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var healthChecks = new List<(string Name, bool Success, TimeSpan Duration, string Details)>();

            try
            {
                // Test 1: Basic connection test
                var connectionResult = await TestDatabaseConnection(cancellationToken);
                healthChecks.Add(("Connection", connectionResult.Success, connectionResult.Duration, connectionResult.Details));
                data["ConnectionTest"] = connectionResult;

                if (!connectionResult.Success)
                {
                    stopwatch.Stop();
                    return CreateUnhealthyResult("Database connection failed", stopwatch.Elapsed, data, healthChecks);
                }

                // Test 2: Query execution performance
                var queryResult = await TestQueryExecution(cancellationToken);
                healthChecks.Add(("QueryExecution", queryResult.Success, queryResult.Duration, queryResult.Details));
                data["QueryExecutionTest"] = queryResult;

                // Test 3: Transaction capability (if enabled)
                if (_options.TransactionTestEnabled)
                {
                    var transactionResult = await TestTransactionCapability(cancellationToken);
                    healthChecks.Add(("Transaction", transactionResult.Success, transactionResult.Duration, transactionResult.Details));
                    data["TransactionTest"] = transactionResult;
                }

                // Test 4: Custom health query (if configured)
                if (!string.IsNullOrEmpty(_options.CustomHealthQuery))
                {
                    var customResult = await ExecuteCustomHealthQuery(cancellationToken);
                    healthChecks.Add(("CustomQuery", customResult.Success, customResult.Duration, customResult.Details));
                    data["CustomQueryTest"] = customResult;
                }

                // Test 5: Database-specific health metrics
                var metricsResult = await CollectDatabaseMetrics(cancellationToken);
                data["DatabaseMetrics"] = metricsResult;

                stopwatch.Stop();

                // Analyze overall health based on all tests
                var overallHealth = AnalyzeOverallHealth(healthChecks, data);
                var statusMessage = CreateStatusMessage(overallHealth, healthChecks, stopwatch.Elapsed);

                _logger.LogDebug($"Database health check '{Name}' completed: {overallHealth} in {stopwatch.Elapsed}");

                return CreateHealthResult(overallHealth, statusMessage, stopwatch.Elapsed, data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, $"Database health check '{Name}' execution failed");
                throw;
            }
        }

        private async Task<DatabaseTestResult> TestDatabaseConnection(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var connectionTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 2, 15));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(connectionTimeout);

                var isConnected = await _databaseService.TestConnectionAsync(timeoutCts.Token);
                
                stopwatch.Stop();

                if (isConnected)
                {
                    return new DatabaseTestResult
                    {
                        Success = true,
                        Duration = stopwatch.Elapsed,
                        Details = $"Connection established successfully in {stopwatch.ElapsedMilliseconds}ms"
                    };
                }
                else
                {
                    return new DatabaseTestResult
                    {
                        Success = false,
                        Duration = stopwatch.Elapsed,
                        Details = "Connection test returned false - database may be unavailable"
                    };
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Connection test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Connection test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<DatabaseTestResult> TestQueryExecution(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var queryTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 3, 10));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(queryTimeout);

                // Execute performance test query or default query
                var query = !string.IsNullOrEmpty(_options.PerformanceTestQuery) 
                    ? _options.PerformanceTestQuery 
                    : _options.ConnectionTestQuery;

                var result = await _databaseService.ExecuteScalarAsync<object>(query, timeoutCts.Token);
                
                stopwatch.Stop();

                // Analyze query performance
                var performanceStatus = AnalyzeQueryPerformance(stopwatch.Elapsed);

                return new DatabaseTestResult
                {
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Details = $"Query executed successfully in {stopwatch.ElapsedMilliseconds}ms - {performanceStatus}",
                    Result = result
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Query execution timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Query execution failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<DatabaseTestResult> TestTransactionCapability(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var transactionTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 4, 5));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(transactionTimeout);

                // Test transaction capability with a simple transaction
                var transactionSuccessful = await _databaseService.TestTransactionAsync(timeoutCts.Token);
                
                stopwatch.Stop();

                return new DatabaseTestResult
                {
                    Success = transactionSuccessful,
                    Duration = stopwatch.Elapsed,
                    Details = transactionSuccessful 
                        ? $"Transaction test passed in {stopwatch.ElapsedMilliseconds}ms"
                        : "Transaction test failed - database may not support transactions"
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Transaction test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Transaction test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<DatabaseTestResult> ExecuteCustomHealthQuery(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var customQueryTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 3, 10));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(customQueryTimeout);

                var result = await _databaseService.ExecuteScalarAsync<object>(_options.CustomHealthQuery, timeoutCts.Token);
                
                stopwatch.Stop();

                // Validate custom query result if validator is provided
                var isValidResult = _options.CustomQueryResultValidator?.Invoke(result) ?? true;

                return new DatabaseTestResult
                {
                    Success = isValidResult,
                    Duration = stopwatch.Elapsed,
                    Details = isValidResult 
                        ? $"Custom health query executed successfully in {stopwatch.ElapsedMilliseconds}ms"
                        : "Custom health query returned invalid result",
                    Result = result
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Custom health query timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new DatabaseTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Custom health query failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<Dictionary<string, object>> CollectDatabaseMetrics(CancellationToken cancellationToken)
        {
            var metrics = new Dictionary<string, object>();
            
            try
            {
                // Collect basic database information
                metrics["DatabaseType"] = _databaseService.GetType().Name;
                metrics["ConnectionString"] = _databaseService.GetConnectionString()?.MaskSensitiveData();
                metrics["SupportsTransactions"] = _options.TransactionTestEnabled;
                
                // Collect database-specific metrics if available
                if (_databaseService is IDatabaseMetricsProvider metricsProvider)
                {
                    var dbMetrics = await metricsProvider.GetMetricsAsync(cancellationToken);
                    foreach (var metric in dbMetrics)
                    {
                        metrics[$"DB_{metric.Key}"] = metric.Value;
                    }
                }

                // Add performance context
                metrics["PerformanceWarningThreshold"] = _options.PerformanceWarningThreshold.TotalMilliseconds;
                metrics["PerformanceCriticalThreshold"] = _options.PerformanceCriticalThreshold.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect database metrics: {ex.Message}");
                metrics["MetricsCollectionError"] = ex.Message;
            }

            return metrics;
        }

        private string AnalyzeQueryPerformance(TimeSpan duration)
        {
            if (duration >= _options.PerformanceCriticalThreshold)
                return "Critical Performance";
            if (duration >= _options.PerformanceWarningThreshold)
                return "Degraded Performance";
            return "Good Performance";
        }

        private HealthStatus AnalyzeOverallHealth(
            List<(string Name, bool Success, TimeSpan Duration, string Details)> healthChecks,
            Dictionary<string, object> data)
        {
            var failedChecks = 0;
            var degradedChecks = 0;
            var totalChecks = healthChecks.Count;

            foreach (var check in healthChecks)
            {
                if (!check.Success)
                {
                    failedChecks++;
                }
                else if (check.Duration >= _options.PerformanceWarningThreshold)
                {
                    degradedChecks++;
                }
            }

            // Determine health status based on failures and performance
            if (failedChecks > 0)
            {
                return failedChecks >= totalChecks * 0.5 ? HealthStatus.Unhealthy : HealthStatus.Degraded;
            }

            if (degradedChecks >= totalChecks * 0.5)
            {
                return HealthStatus.Degraded;
            }

            return HealthStatus.Healthy;
        }

        private string CreateStatusMessage(
            HealthStatus status,
            List<(string Name, bool Success, TimeSpan Duration, string Details)> healthChecks,
            TimeSpan totalDuration)
        {
            var successfulChecks = healthChecks.FindAll(c => c.Success).Count;
            var totalChecks = healthChecks.Count;
            var avgDuration = totalChecks > 0 
                ? TimeSpan.FromTicks(healthChecks.ConvertAll(c => c.Duration.Ticks).Sum() / totalChecks)
                : TimeSpan.Zero;

            var statusDescription = status switch
            {
                HealthStatus.Healthy => "Database is healthy and performing well",
                HealthStatus.Degraded => "Database is operational but showing performance issues",
                HealthStatus.Unhealthy => "Database has critical issues affecting functionality",
                _ => "Database status is unknown"
            };

            return $"{statusDescription} - {successfulChecks}/{totalChecks} checks passed, " +
                   $"avg response: {avgDuration.TotalMilliseconds:F0}ms, total: {totalDuration.TotalMilliseconds:F0}ms";
        }

        private HealthCheckResult CreateHealthResult(
            HealthStatus status,
            string message,
            TimeSpan duration,
            Dictionary<string, object> data)
        {
            return status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(message, duration, data),
                HealthStatus.Degraded => HealthCheckResult.Degraded(message, duration, data),
                HealthStatus.Unhealthy => HealthCheckResult.Unhealthy(message, duration, data),
                _ => HealthCheckResult.Unhealthy("Unknown database health status", duration, data)
            };
        }

        private HealthCheckResult CreateUnhealthyResult(
            string reason,
            TimeSpan duration,
            Dictionary<string, object> data,
            List<(string Name, bool Success, TimeSpan Duration, string Details)> healthChecks)
        {
            data["FailedChecks"] = healthChecks.FindAll(c => !c.Success);
            return HealthCheckResult.Unhealthy(reason, duration, data);
        }

        private HealthCheckResult CreateCircuitBreakerFallbackResult(TimeSpan duration, Dictionary<string, object> data)
        {
            data["CircuitBreakerTriggered"] = true;
            return HealthCheckResult.Unhealthy(
                "Database health check failed - circuit breaker is open",
                duration,
                data);
        }

        #endregion
    }

    #region Supporting Types

    

    //TODO Need to move these to the database service or a common utilities namespace

    /// <summary>
    /// Interface for database services that provide health check capabilities
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Tests database connectivity
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a scalar query and returns the result
        /// </summary>
        /// <typeparam name="T">Expected result type</typeparam>
        /// <param name="query">SQL query to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query result</returns>
        Task<T> ExecuteScalarAsync<T>(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests transaction capability
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if transactions are supported and working</returns>
        Task<bool> TestTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the connection string (with sensitive data masked)
        /// </summary>
        /// <returns>Masked connection string</returns>
        string GetConnectionString();
    }

    /// <summary>
    /// Interface for database services that can provide detailed metrics
    /// </summary>
    public interface IDatabaseMetricsProvider
    {
        /// <summary>
        /// Gets database-specific metrics for health monitoring
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of metric names and values</returns>
        Task<Dictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default);
    }

    #endregion
}

/// <summary>
/// Extension methods for database health checking
/// </summary>
public static class DatabaseHealthCheckExtensions
{
    /// <summary>
    /// Masks sensitive data in connection strings for logging
    /// </summary>
    /// <param name="connectionString">Original connection string</param>
    /// <returns>Connection string with sensitive data masked</returns>
    public static string MaskSensitiveData(this string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Simple masking - in production, use more sophisticated masking
        return connectionString
            .Replace("password=", "password=***", StringComparison.OrdinalIgnoreCase)
            .Replace("pwd=", "pwd=***", StringComparison.OrdinalIgnoreCase)
            .Replace("user id=", "user id=***", StringComparison.OrdinalIgnoreCase)
            .Replace("uid=", "uid=***", StringComparison.OrdinalIgnoreCase);
    }
}