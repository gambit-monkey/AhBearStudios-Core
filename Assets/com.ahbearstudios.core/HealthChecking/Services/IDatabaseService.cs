using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.HealthChecking.Services
{
    /// <summary>
    /// Interface for database services that provide health check capabilities.
    /// Defines the contract for database connectivity and health monitoring operations.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Tests database connectivity asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if connection is successful, false otherwise</returns>
        UniTask<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a scalar query and returns the result.
        /// </summary>
        /// <typeparam name="T">Expected result type</typeparam>
        /// <param name="query">SQL query to execute</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Query result of type T</returns>
        UniTask<T> ExecuteScalarAsync<T>(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests transaction capability by performing a simple transactional operation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if transactions are supported and working</returns>
        UniTask<bool> TestTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the connection string with sensitive data masked for logging purposes.
        /// </summary>
        /// <returns>Masked connection string safe for logging</returns>
        string GetConnectionString();

        /// <summary>
        /// Gets the database provider name or type.
        /// </summary>
        /// <returns>Database provider identifier</returns>
        string GetProviderName();

        /// <summary>
        /// Gets basic database information for health reporting.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Dictionary containing database information</returns>
        UniTask<Dictionary<string, object>> GetDatabaseInfoAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for database services that can provide detailed metrics for health monitoring.
    /// Extends basic database service with performance and diagnostic capabilities.
    /// </summary>
    public interface IDatabaseMetricsProvider
    {
        /// <summary>
        /// Gets database-specific metrics for comprehensive health monitoring.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Dictionary of metric names and values</returns>
        UniTask<Dictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets connection pool statistics if supported by the provider.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Connection pool metrics or null if not supported</returns>
        UniTask<Dictionary<string, object>> GetConnectionPoolMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets query performance statistics if available.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Performance metrics or null if not available</returns>
        UniTask<Dictionary<string, object>> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for database services that support advanced health check operations.
    /// Provides specialized health monitoring capabilities beyond basic connectivity.
    /// </summary>
    public interface IDatabaseHealthProvider
    {
        /// <summary>
        /// Performs a comprehensive database health check.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Detailed health status information</returns>
        UniTask<DatabaseHealthResult> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests specific database operations for health validation.
        /// </summary>
        /// <param name="operationType">Type of operation to test</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Operation test result</returns>
        UniTask<bool> TestOperationAsync(string operationType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current health status of the database.
        /// </summary>
        /// <returns>Current database health status</returns>
        DatabaseHealthStatus GetCurrentHealthStatus();
    }

    /// <summary>
    /// Represents the result of a database health check operation.
    /// </summary>
    public sealed class DatabaseHealthResult
    {
        /// <summary>
        /// Overall health status of the database.
        /// </summary>
        public DatabaseHealthStatus Status { get; init; }

        /// <summary>
        /// Detailed message about the health status.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Time taken to perform the health check.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Additional data and metrics from the health check.
        /// </summary>
        public Dictionary<string, object> Data { get; init; }

        /// <summary>
        /// Exception that occurred during health check, if any.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Creates a healthy database health result.
        /// </summary>
        /// <param name="message">Status message</param>
        /// <param name="duration">Check duration</param>
        /// <param name="data">Additional data</param>
        /// <returns>Healthy result instance</returns>
        public static DatabaseHealthResult Healthy(string message, TimeSpan duration, Dictionary<string, object> data = null)
        {
            return new DatabaseHealthResult
            {
                Status = DatabaseHealthStatus.Healthy,
                Message = message,
                Duration = duration,
                Data = data ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates a degraded database health result.
        /// </summary>
        /// <param name="message">Status message</param>
        /// <param name="duration">Check duration</param>
        /// <param name="data">Additional data</param>
        /// <returns>Degraded result instance</returns>
        public static DatabaseHealthResult Degraded(string message, TimeSpan duration, Dictionary<string, object> data = null)
        {
            return new DatabaseHealthResult
            {
                Status = DatabaseHealthStatus.Degraded,
                Message = message,
                Duration = duration,
                Data = data ?? new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Creates an unhealthy database health result.
        /// </summary>
        /// <param name="message">Status message</param>
        /// <param name="duration">Check duration</param>
        /// <param name="data">Additional data</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <returns>Unhealthy result instance</returns>
        public static DatabaseHealthResult Unhealthy(string message, TimeSpan duration, Dictionary<string, object> data = null, Exception exception = null)
        {
            return new DatabaseHealthResult
            {
                Status = DatabaseHealthStatus.Unhealthy,
                Message = message,
                Duration = duration,
                Data = data ?? new Dictionary<string, object>(),
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Represents the health status of a database.
    /// </summary>
    public enum DatabaseHealthStatus
    {
        /// <summary>
        /// Database is healthy and functioning normally.
        /// </summary>
        Healthy,

        /// <summary>
        /// Database is functional but showing performance issues or warnings.
        /// </summary>
        Degraded,

        /// <summary>
        /// Database has critical issues or is unavailable.
        /// </summary>
        Unhealthy
    }
}