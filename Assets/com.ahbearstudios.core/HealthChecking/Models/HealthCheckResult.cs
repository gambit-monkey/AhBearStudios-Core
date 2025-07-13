using System.Collections.Generic;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Models
{
    /// <summary>
    /// Represents the result of a health check execution with comprehensive diagnostic information
    /// </summary>
    public sealed record HealthCheckResult : IHealthCheckResult
    {
        /// <summary>
        /// Name of the health check that was executed
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Health status determined by the check
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Primary message describing the health check result
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Detailed description of the health check result
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Duration of the health check execution
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Timestamp when the health check was executed
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Exception that occurred during health check execution, if any
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Additional diagnostic data collected during the health check
        /// </summary>
        public Dictionary<string, object> Data { get; init; } = new();

        /// <summary>
        /// Unique correlation ID for tracing this health check execution
        /// </summary>
        public FixedString64Bytes CorrelationId { get; init; }

        /// <summary>
        /// Category of the health check that was executed
        /// </summary>
        public HealthCheckCategory Category { get; init; }

        /// <summary>
        /// Tags associated with this health check for filtering and organization
        /// </summary>
        public HashSet<FixedString64Bytes> Tags { get; init; } = new();

        /// <summary>
        /// Gets whether the health check result indicates a healthy status
        /// </summary>
        public bool IsHealthy => Status == HealthStatus.Healthy;

        /// <summary>
        /// Gets whether the health check result indicates a degraded status
        /// </summary>
        public bool IsDegraded => Status == HealthStatus.Degraded;

        /// <summary>
        /// Gets whether the health check result indicates an unhealthy status
        /// </summary>
        public bool IsUnhealthy => Status == HealthStatus.Unhealthy;

        /// <summary>
        /// Gets whether the health check result indicates an unknown status
        /// </summary>
        public bool IsUnknown => Status == HealthStatus.Unknown;

        /// <summary>
        /// Creates a healthy health check result
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="message">Success message</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="data">Additional diagnostic data</param>
        /// <param name="correlationId">Correlation ID for tracing</param>
        /// <returns>Healthy health check result</returns>
        public static HealthCheckResult Healthy(
            string name,
            string message = "Health check passed",
            TimeSpan? duration = null,
            Dictionary<string, object> data = null,
            FixedString64Bytes correlationId = default)
        {
            return new HealthCheckResult
            {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Status = HealthStatus.Healthy,
                Message = message ?? "Health check passed",
                Duration = duration ?? TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Data = data ?? new Dictionary<string, object>(),
                CorrelationId = correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            };
        }

        /// <summary>
        /// Creates a degraded health check result
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="message">Degradation message</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="data">Additional diagnostic data</param>
        /// <param name="correlationId">Correlation ID for tracing</param>
        /// <returns>Degraded health check result</returns>
        public static HealthCheckResult Degraded(
            string name,
            string message,
            TimeSpan? duration = null,
            Dictionary<string, object> data = null,
            FixedString64Bytes correlationId = default)
        {
            return new HealthCheckResult
            {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Status = HealthStatus.Degraded,
                Message = message ?? "Health check degraded",
                Duration = duration ?? TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Data = data ?? new Dictionary<string, object>(),
                CorrelationId = correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            };
        }

        /// <summary>
        /// Creates an unhealthy health check result
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="message">Failure message</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="data">Additional diagnostic data</param>
        /// <param name="exception">Exception that caused the failure</param>
        /// <param name="correlationId">Correlation ID for tracing</param>
        /// <returns>Unhealthy health check result</returns>
        public static HealthCheckResult Unhealthy(
            string name,
            string message,
            TimeSpan? duration = null,
            Dictionary<string, object> data = null,
            Exception exception = null,
            FixedString64Bytes correlationId = default)
        {
            return new HealthCheckResult
            {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Status = HealthStatus.Unhealthy,
                Message = message ?? "Health check failed",
                Duration = duration ?? TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Data = data ?? new Dictionary<string, object>(),
                Exception = exception,
                CorrelationId = correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            };
        }

        /// <summary>
        /// Creates an unknown health check result
        /// </summary>
        /// <param name="name">Name of the health check</param>
        /// <param name="message">Unknown status message</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="data">Additional diagnostic data</param>
        /// <param name="correlationId">Correlation ID for tracing</param>
        /// <returns>Unknown health check result</returns>
        public static HealthCheckResult Unknown(
            string name,
            string message = "Health check status unknown",
            TimeSpan? duration = null,
            Dictionary<string, object> data = null,
            FixedString64Bytes correlationId = default)
        {
            return new HealthCheckResult
            {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Status = HealthStatus.Unknown,
                Message = message ?? "Health check status unknown",
                Duration = duration ?? TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Data = data ?? new Dictionary<string, object>(),
                CorrelationId = correlationId.IsEmpty ? GenerateCorrelationId() : correlationId
            };
        }

        /// <summary>
        /// Generates a unique correlation ID for tracing
        /// </summary>
        /// <returns>Unique correlation ID</returns>
        private static FixedString64Bytes GenerateCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
        }

        /// <summary>
        /// Creates a copy of this result with updated properties
        /// </summary>
        /// <param name="status">New status</param>
        /// <param name="message">New message</param>
        /// <param name="exception">New exception</param>
        /// <returns>Updated health check result</returns>
        public HealthCheckResult WithUpdate(
            HealthStatus? status = null,
            string message = null,
            Exception exception = null)
        {
            return this with
            {
                Status = status ?? Status,
                Message = message ?? Message,
                Exception = exception ?? Exception,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Adds diagnostic data to this result
        /// </summary>
        /// <param name="key">Data key</param>
        /// <param name="value">Data value</param>
        /// <returns>Updated health check result</returns>
        public HealthCheckResult WithData(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var newData = new Dictionary<string, object>(Data) { [key] = value };
            return this with { Data = newData };
        }

        /// <summary>
        /// Adds multiple diagnostic data entries to this result
        /// </summary>
        /// <param name="data">Data to add</param>
        /// <returns>Updated health check result</returns>
        public HealthCheckResult WithData(Dictionary<string, object> data)
        {
            if (data == null)
                return this;

            var newData = new Dictionary<string, object>(Data);
            foreach (var kvp in data)
            {
                newData[kvp.Key] = kvp.Value;
            }

            return this with { Data = newData };
        }

        /// <summary>
        /// Adds a tag to this result
        /// </summary>
        /// <param name="tag">Tag to add</param>
        /// <returns>Updated health check result</returns>
        public HealthCheckResult WithTag(FixedString64Bytes tag)
        {
            var newTags = new HashSet<FixedString64Bytes>(Tags) { tag };
            return this with { Tags = newTags };
        }

        /// <summary>
        /// Gets a summary string representation of this health check result
        /// </summary>
        /// <returns>Summary string</returns>
        public override string ToString()
        {
            var exceptionInfo = Exception != null ? $" (Exception: {Exception.GetType().Name})" : "";
            return $"{Name}: {Status} - {Message} [{Duration.TotalMilliseconds:F0}ms]{exceptionInfo}";
        }
    }
}

    