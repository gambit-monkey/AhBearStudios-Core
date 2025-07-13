using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.HealthChecks
{
    /// <summary>
    /// Health check implementation for the serialization system.
    /// Monitors serializer performance, memory usage, and operational health.
    /// </summary>
    public class SerializationHealthCheck : IHealthCheck
    {
        private readonly ISerializer _serializer;
        private readonly ILoggingService _logger;
        private readonly SerializationHealthThresholds _thresholds;

        /// <summary>
        /// Gets the name of this health check.
        /// </summary>
        public string Name => "Serialization System";

        /// <summary>
        /// Gets the description of this health check.
        /// </summary>
        public string Description => "Monitors serialization system performance and health";

        /// <summary>
        /// Gets the tags associated with this health check.
        /// </summary>
        public IReadOnlyList<string> Tags => new[] { "serialization", "performance", "core" };

        /// <summary>
        /// Initializes a new instance of SerializationHealthCheck.
        /// </summary>
        /// <param name="serializer">Serializer to monitor</param>
        /// <param name="logger">Logging service</param>
        /// <param name="thresholds">Health check thresholds</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public SerializationHealthCheck(
            ISerializer serializer,
            ILoggingService logger,
            SerializationHealthThresholds thresholds = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _thresholds = thresholds ?? SerializationHealthThresholds.Default;
        }

        /// <summary>
        /// Performs the health check asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var correlationId = GetCorrelationId();
            
            try
            {
                _logger.LogInfo("Starting serialization health check", correlationId);

                var healthData = new Dictionary<string, object>();
                var issues = new List<string>();
                var status = HealthStatus.Healthy;

                // Check basic functionality
                var functionalityResult = await CheckBasicFunctionality(cancellationToken);
                healthData["BasicFunctionality"] = functionalityResult.IsHealthy;
                if (!functionalityResult.IsHealthy)
                {
                    issues.Add($"Basic functionality failed: {functionalityResult.ErrorMessage}");
                    status = HealthStatus.Unhealthy;
                }

                // Check performance metrics
                var performanceResult = CheckPerformanceMetrics();
                healthData.Add("Performance", performanceResult.Data);
                if (performanceResult.Status != HealthStatus.Healthy)
                {
                    issues.AddRange(performanceResult.Issues);
                    if (performanceResult.Status == HealthStatus.Unhealthy)
                        status = HealthStatus.Unhealthy;
                    else if (status == HealthStatus.Healthy)
                        status = HealthStatus.Degraded;
                }

                // Check memory usage
                var memoryResult = CheckMemoryUsage();
                healthData.Add("Memory", memoryResult.Data);
                if (memoryResult.Status != HealthStatus.Healthy)
                {
                    issues.AddRange(memoryResult.Issues);
                    if (memoryResult.Status == HealthStatus.Unhealthy)
                        status = HealthStatus.Unhealthy;
                    else if (status == HealthStatus.Healthy)
                        status = HealthStatus.Degraded;
                }

                // Add overall statistics
                var statistics = _serializer.GetStatistics();
                healthData.Add("Statistics", new
                {
                    TotalOperations = statistics.TotalSerializations + statistics.TotalDeserializations,
                    statistics.FailedOperations,
                    statistics.TotalBytesProcessed,
                    statistics.AverageSerializationTimeMs,
                    statistics.AverageDeserializationTimeMs,
                    statistics.RegisteredTypeCount
                });

                var message = status == HealthStatus.Healthy 
                    ? "Serialization system is operating normally"
                    : $"Serialization system has {issues.Count} issue(s)";

                _logger.LogInfo($"Serialization health check completed with status: {status}", correlationId);

                return new HealthCheckResult(status, message, healthData)
                {
                    Issues = issues,
                    CheckedAt = DateTime.UtcNow,
                    Duration = TimeSpan.FromMilliseconds(100) // Placeholder - should track actual duration
                };
            }
            catch (Exception ex)
            {
                _logger.LogException("Serialization health check failed", ex, correlationId);
                
                return new HealthCheckResult(
                    HealthStatus.Unhealthy,
                    $"Health check failed: {ex.Message}",
                    new Dictionary<string, object> { ["Exception"] = ex.GetType().Name })
                {
                    Issues = new[] { ex.Message },
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<(bool IsHealthy, string ErrorMessage)> CheckBasicFunctionality(CancellationToken cancellationToken)
        {
            try
            {
                // Test basic serialization/deserialization with a simple object
                var testData = new TestSerializationObject
                {
                    Id = Guid.NewGuid(),
                    Name = "HealthCheck",
                    Value = 42,
                    Timestamp = DateTime.UtcNow
                };

                // Ensure type is registered
                _serializer.RegisterType<TestSerializationObject>();

                // Test synchronous operations
                var serialized = _serializer.Serialize(testData);
                var deserialized = _serializer.Deserialize<TestSerializationObject>(serialized);

                if (!testData.Equals(deserialized))
                {
                    return (false, "Serialization round-trip validation failed");
                }

                // Test asynchronous operations
                var asyncSerialized = await _serializer.SerializeAsync(testData, cancellationToken);
                var asyncDeserialized = await _serializer.DeserializeAsync<TestSerializationObject>(asyncSerialized, cancellationToken);

                if (!testData.Equals(asyncDeserialized))
                {
                    return (false, "Async serialization round-trip validation failed");
                }

                // Test TryDeserialize
                if (!_serializer.TryDeserialize<TestSerializationObject>(serialized, out var tryResult) || !testData.Equals(tryResult))
                {
                    return (false, "TryDeserialize validation failed");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private (HealthStatus Status, Dictionary<string, object> Data, List<string> Issues) CheckPerformanceMetrics()
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();
            var status = HealthStatus.Healthy;

            try
            {
                var statistics = _serializer.GetStatistics();

                // Calculate failure rate
                var totalOperations = statistics.TotalSerializations + statistics.TotalDeserializations;
                var failureRate = totalOperations > 0 ? (double)statistics.FailedOperations / totalOperations : 0.0;

                data["FailureRate"] = failureRate;
                data["TotalOperations"] = totalOperations;
                data["FailedOperations"] = statistics.FailedOperations;

                if (failureRate > _thresholds.MaxFailureRate)
                {
                    issues.Add($"Failure rate ({failureRate:P2}) exceeds threshold ({_thresholds.MaxFailureRate:P2})");
                    status = failureRate > _thresholds.CriticalFailureRate ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                // Check average performance
                data["AverageSerializationTimeMs"] = statistics.AverageSerializationTimeMs;
                data["AverageDeserializationTimeMs"] = statistics.AverageDeserializationTimeMs;

                if (statistics.AverageSerializationTimeMs > _thresholds.MaxAverageSerializationTimeMs)
                {
                    issues.Add($"Average serialization time ({statistics.AverageSerializationTimeMs:F2}ms) exceeds threshold ({_thresholds.MaxAverageSerializationTimeMs}ms)");
                    status = statistics.AverageSerializationTimeMs > _thresholds.CriticalSerializationTimeMs ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                if (statistics.AverageDeserializationTimeMs > _thresholds.MaxAverageDeserializationTimeMs)
                {
                    issues.Add($"Average deserialization time ({statistics.AverageDeserializationTimeMs:F2}ms) exceeds threshold ({_thresholds.MaxAverageDeserializationTimeMs}ms)");
                    if (status == HealthStatus.Healthy)
                        status = statistics.AverageDeserializationTimeMs > _thresholds.CriticalDeserializationTimeMs ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                // Check throughput
                data["TotalBytesProcessed"] = statistics.TotalBytesProcessed;
                data["RegisteredTypeCount"] = statistics.RegisteredTypeCount;
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to collect performance metrics: {ex.Message}");
                status = HealthStatus.Degraded;
            }

            return (status, data, issues);
        }

        private (HealthStatus Status, Dictionary<string, object> Data, List<string> Issues) CheckMemoryUsage()
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();
            var status = HealthStatus.Healthy;

            try
            {
                var statistics = _serializer.GetStatistics();
                var currentMemory = GC.GetTotalMemory(false);

                data["CurrentMemoryUsage"] = currentMemory;
                data["PeakMemoryUsage"] = statistics.PeakMemoryUsage;

                if (statistics.PeakMemoryUsage > _thresholds.MaxMemoryUsageBytes)
                {
                    issues.Add($"Peak memory usage ({statistics.PeakMemoryUsage:N0} bytes) exceeds threshold ({_thresholds.MaxMemoryUsageBytes:N0} bytes)");
                    status = statistics.PeakMemoryUsage > _thresholds.CriticalMemoryUsageBytes ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                if (currentMemory > _thresholds.MaxMemoryUsageBytes)
                {
                    issues.Add($"Current memory usage ({currentMemory:N0} bytes) exceeds threshold ({_thresholds.MaxMemoryUsageBytes:N0} bytes)");
                    status = currentMemory > _thresholds.CriticalMemoryUsageBytes ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                // Check buffer pool statistics if available
                if (statistics.BufferPoolStats != null)
                {
                    data["BufferPoolStats"] = new
                    {
                        statistics.BufferPoolStats.BuffersInPool,
                        statistics.BufferPoolStats.BuffersRented,
                        statistics.BufferPoolStats.HitRatio,
                        statistics.BufferPoolStats.TotalPoolMemory
                    };

                    if (statistics.BufferPoolStats.HitRatio < _thresholds.MinBufferPoolHitRatio)
                    {
                        issues.Add($"Buffer pool hit ratio ({statistics.BufferPoolStats.HitRatio:P2}) below threshold ({_thresholds.MinBufferPoolHitRatio:P2})");
                        if (status == HealthStatus.Healthy)
                            status = HealthStatus.Degraded;
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to collect memory usage metrics: {ex.Message}");
                status = HealthStatus.Degraded;
            }

            return (status, data, issues);
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }

    /// <summary>
    /// Configuration for serialization health check thresholds.
    /// </summary>
    public record SerializationHealthThresholds
    {
        /// <summary>
        /// Maximum acceptable failure rate (0.0 to 1.0).
        /// </summary>
        public double MaxFailureRate { get; init; } = 0.01; // 1%

        /// <summary>
        /// Critical failure rate that triggers unhealthy status.
        /// </summary>
        public double CriticalFailureRate { get; init; } = 0.05; // 5%

        /// <summary>
        /// Maximum acceptable average serialization time in milliseconds.
        /// </summary>
        public double MaxAverageSerializationTimeMs { get; init; } = 10.0;

        /// <summary>
        /// Critical serialization time that triggers unhealthy status.
        /// </summary>
        public double CriticalSerializationTimeMs { get; init; } = 50.0;

        /// <summary>
        /// Maximum acceptable average deserialization time in milliseconds.
        /// </summary>
        public double MaxAverageDeserializationTimeMs { get; init; } = 15.0;

        /// <summary>
        /// Critical deserialization time that triggers unhealthy status.
        /// </summary>
        public double CriticalDeserializationTimeMs { get; init; } = 75.0;

        /// <summary>
        /// Maximum acceptable memory usage in bytes.
        /// </summary>
        public long MaxMemoryUsageBytes { get; init; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Critical memory usage that triggers unhealthy status.
        /// </summary>
        public long CriticalMemoryUsageBytes { get; init; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Minimum acceptable buffer pool hit ratio.
        /// </summary>
        public double MinBufferPoolHitRatio { get; init; } = 0.8; // 80%

        /// <summary>
        /// Default thresholds for production environments.
        /// </summary>
        public static SerializationHealthThresholds Default => new();

        /// <summary>
        /// Relaxed thresholds for development environments.
        /// </summary>
        public static SerializationHealthThresholds Development => new()
        {
            MaxFailureRate = 0.05,
            CriticalFailureRate = 0.15,
            MaxAverageSerializationTimeMs = 50.0,
            CriticalSerializationTimeMs = 200.0,
            MaxAverageDeserializationTimeMs = 75.0,
            CriticalDeserializationTimeMs = 300.0,
            MaxMemoryUsageBytes = 200 * 1024 * 1024, // 200MB
            CriticalMemoryUsageBytes = 500 * 1024 * 1024, // 500MB
            MinBufferPoolHitRatio = 0.6 // 60%
        };
    }

    /// <summary>
    /// Test object for serialization health checks.
    /// </summary>
    [Serializable]
    internal record TestSerializationObject
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public int Value { get; init; }
        public DateTime Timestamp { get; init; }
    }
}