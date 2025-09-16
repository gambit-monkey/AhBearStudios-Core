using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.Logging;
using Unity.Collections;

namespace AhBearStudios.Core.Serialization.HealthChecks
{
    /// <summary>
    /// Health check implementation for the SerializationService.
    /// Monitors service health, circuit breaker status, and overall system performance.
    /// </summary>
    public class SerializationServiceHealthCheck : IHealthCheck
    {
        private readonly ISerializationService _serializationService;
        private readonly ILoggingService _logger;
        private readonly SerializationServiceHealthThresholds _thresholds;

        /// <summary>
        /// Gets the name of this health check.
        /// </summary>
        public FixedString64Bytes Name => new("SerializationServiceHealthCheck");

        /// <summary>
        /// Gets the description of this health check.
        /// </summary>
        public string Description => "Monitors SerializationService health including circuit breakers and fault tolerance";

        /// <summary>
        /// Gets the category of this health check.
        /// </summary>
        public HealthCheckCategory Category { get; private set; } = HealthCheckCategory.System;

        /// <summary>
        /// Gets the timeout for this health check.
        /// </summary>
        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the current configuration for this health check.
        /// </summary>
        public HealthCheckConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the dependencies for this health check.
        /// </summary>
        public IEnumerable<FixedString64Bytes> Dependencies { get; private set; } = Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Initializes a new instance of SerializationServiceHealthCheck.
        /// </summary>
        /// <param name="serializationService">SerializationService to monitor</param>
        /// <param name="logger">Logging service</param>
        /// <param name="thresholds">Health check thresholds</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public SerializationServiceHealthCheck(
            ISerializationService serializationService,
            ILoggingService logger,
            SerializationServiceHealthThresholds thresholds = null)
        {
            _serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _thresholds = thresholds ?? SerializationServiceHealthThresholds.Default;
            
            // Initialize default configuration
            Configuration = new HealthCheckConfiguration
            {
                Name = Name,
                DisplayName = "Serialization Service Health Check",
                Category = Category,
                Timeout = Timeout
            };
        }

        /// <summary>
        /// Performs the health check asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var correlationId = GetCorrelationId();
            
            try
            {
                _logger.LogInfo("Starting SerializationService health check", correlationId, nameof(SerializationServiceHealthCheck));

                var healthData = new Dictionary<string, object>();
                var issues = new List<string>();
                var status = HealthStatus.Healthy;

                // Check if service is enabled
                healthData["IsEnabled"] = _serializationService.IsEnabled;
                if (!_serializationService.IsEnabled)
                {
                    issues.Add("SerializationService is disabled");
                    status = HealthStatus.Degraded;
                }

                // Check basic functionality
                var functionalityResult = await CheckBasicFunctionality(correlationId, cancellationToken);
                healthData["BasicFunctionality"] = functionalityResult.IsHealthy;
                if (!functionalityResult.IsHealthy)
                {
                    issues.Add($"Basic functionality failed: {functionalityResult.ErrorMessage}");
                    status = HealthStatus.Unhealthy;
                }

                // Check circuit breaker status
                var circuitBreakerResult = CheckCircuitBreakerStatus();
                healthData.Add("CircuitBreakers", circuitBreakerResult.Data);
                if (circuitBreakerResult.Status != HealthStatus.Healthy)
                {
                    issues.AddRange(circuitBreakerResult.Issues);
                    if (circuitBreakerResult.Status == HealthStatus.Unhealthy)
                        status = HealthStatus.Unhealthy;
                    else if (status == HealthStatus.Healthy)
                        status = HealthStatus.Degraded;
                }

                // Check serializer availability
                var serializerResult = CheckSerializerAvailability();
                healthData.Add("Serializers", serializerResult.Data);
                if (serializerResult.Status != HealthStatus.Healthy)
                {
                    issues.AddRange(serializerResult.Issues);
                    if (serializerResult.Status == HealthStatus.Unhealthy)
                        status = HealthStatus.Unhealthy;
                    else if (status == HealthStatus.Healthy)
                        status = HealthStatus.Degraded;
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

                // Perform service health check
                var serviceHealthy = _serializationService.PerformHealthCheck();
                healthData["ServiceHealthCheck"] = serviceHealthy;
                if (!serviceHealthy)
                {
                    issues.Add("SerializationService health check failed");
                    status = HealthStatus.Unhealthy;
                }

                // Get overall service statistics
                var statistics = _serializationService.GetStatistics();
                var registeredFormats = _serializationService.GetRegisteredFormats();
                
                healthData.Add("Statistics", new
                {
                    TotalOperations = statistics.TotalSerializations + statistics.TotalDeserializations,
                    statistics.FailedOperations,
                    statistics.TotalBytesProcessed,
                    RegisteredFormats = registeredFormats.Count,
                    statistics.RegisteredTypeCount,
                    AvailableFormats = registeredFormats.AsValueEnumerable().ToArray(),
                    FishNetEnabled = registeredFormats.AsValueEnumerable().Contains(Models.SerializationFormat.FishNet)
                });

                var message = status == HealthStatus.Healthy 
                    ? "SerializationService is operating normally"
                    : $"SerializationService has {issues.Count} issue(s)";

                _logger.LogInfo($"SerializationService health check completed with status: {status}", correlationId, nameof(SerializationServiceHealthCheck));

                return new HealthCheckResult
                {
                    Name = Name.ToString(),
                    Status = status,
                    Message = message,
                    Data = healthData,
                    Duration = TimeSpan.FromMilliseconds(150), // Placeholder - should track actual duration
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId
                };
            }
            catch (Exception ex)
            {
                _logger.LogException("SerializationService health check failed", ex, correlationId, nameof(SerializationServiceHealthCheck));
                
                return new HealthCheckResult
                {
                    Name = Name.ToString(),
                    Status = HealthStatus.Unhealthy,
                    Message = $"Health check failed: {ex.Message}",
                    Data = new Dictionary<string, object> { ["Exception"] = ex.GetType().Name },
                    Exception = ex,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = correlationId
                };
            }
        }

        private async UniTask<(bool IsHealthy, string ErrorMessage)> CheckBasicFunctionality(FixedString64Bytes correlationId, CancellationToken cancellationToken)
        {
            try
            {
                // Test basic serialization/deserialization with the service
                var testData = new TestSerializationServiceObject
                {
                    Id = Guid.NewGuid(),
                    Name = "ServiceHealthCheck",
                    Value = 123,
                    Timestamp = DateTime.UtcNow
                };

                // Register the type
                _serializationService.RegisterType<TestSerializationServiceObject>(correlationId);

                // Test synchronous operations
                var serialized = _serializationService.Serialize(testData, correlationId);
                var deserialized = _serializationService.Deserialize<TestSerializationServiceObject>(serialized, correlationId);

                if (!testData.Equals(deserialized))
                {
                    return (false, "Service serialization round-trip validation failed");
                }

                // Test asynchronous operations
                var asyncSerialized = await _serializationService.SerializeAsync(testData, correlationId, cancellationToken: cancellationToken);
                var asyncDeserialized = await _serializationService.DeserializeAsync<TestSerializationServiceObject>(asyncSerialized, correlationId, cancellationToken: cancellationToken);

                if (!testData.Equals(asyncDeserialized))
                {
                    return (false, "Service async serialization round-trip validation failed");
                }

                // Test TrySerialize/TryDeserialize
                if (!_serializationService.TrySerialize(testData, out var tryResult, correlationId) || tryResult == null)
                {
                    return (false, "Service TrySerialize validation failed");
                }

                if (!_serializationService.TryDeserialize<TestSerializationServiceObject>(tryResult, out var tryDeserResult, correlationId) || !testData.Equals(tryDeserResult))
                {
                    return (false, "Service TryDeserialize validation failed");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private (HealthStatus Status, Dictionary<string, object> Data, List<string> Issues) CheckCircuitBreakerStatus()
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();
            var status = HealthStatus.Healthy;

            try
            {
                var circuitBreakerStats = _serializationService.GetCircuitBreakerStatistics();
                var circuitBreakerData = new Dictionary<string, object>();
                var openBreakers = 0;
                var halfOpenBreakers = 0;

                foreach (var kvp in circuitBreakerStats)
                {
                    var format = kvp.Key;
                    var stats = kvp.Value;
                    
                    circuitBreakerData[format.ToString()] = new
                    {
                        State = stats.State.ToString(),
                        FailureCount = stats.TotalFailures,
                        SuccessCount = stats.TotalSuccesses,
                        LastStateChange = stats.LastStateChange,
                        FailureRate = stats.TotalExecutions > 0 ? (double)stats.TotalFailures / stats.TotalExecutions : 0.0
                    };

                    switch (stats.State)
                    {
                        case CircuitBreakerState.Open:
                            openBreakers++;
                            issues.Add($"Circuit breaker for {format} is OPEN (failures: {stats.TotalFailures})");
                            break;
                        case CircuitBreakerState.HalfOpen:
                            halfOpenBreakers++;
                            if (status == HealthStatus.Healthy)
                                status = HealthStatus.Degraded;
                            break;
                    }
                }

                data["CircuitBreakerDetails"] = circuitBreakerData;
                data["OpenBreakerCount"] = openBreakers;
                data["HalfOpenBreakerCount"] = halfOpenBreakers;

                // Determine overall status based on circuit breaker states
                if (openBreakers >= _thresholds.MaxOpenCircuitBreakers)
                {
                    status = HealthStatus.Unhealthy;
                }
                else if (openBreakers > 0 || halfOpenBreakers > _thresholds.MaxHalfOpenCircuitBreakers)
                {
                    if (status == HealthStatus.Healthy)
                        status = HealthStatus.Degraded;
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to collect circuit breaker status: {ex.Message}");
                status = HealthStatus.Degraded;
            }

            return (status, data, issues);
        }

        private (HealthStatus Status, Dictionary<string, object> Data, List<string> Issues) CheckSerializerAvailability()
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();
            var status = HealthStatus.Healthy;

            try
            {
                var registeredFormats = _serializationService.GetRegisteredFormats();
                var availableFormats = new List<string>();
                var unavailableFormats = new List<string>();

                foreach (var format in registeredFormats)
                {
                    if (_serializationService.IsSerializerAvailable(format))
                    {
                        availableFormats.Add(format.ToString());
                    }
                    else
                    {
                        unavailableFormats.Add(format.ToString());
                        issues.Add($"Serializer for {format} is not available");
                    }
                }

                data["RegisteredFormats"] = registeredFormats.AsValueEnumerable().Select(f => f.ToString()).ToArray();
                data["AvailableFormats"] = availableFormats.ToArray();
                data["UnavailableFormats"] = unavailableFormats.ToArray();
                data["AvailabilityRatio"] = registeredFormats.Count > 0 ? (double)availableFormats.Count / registeredFormats.Count : 1.0;

                // Check if we have minimum required serializers available
                if (availableFormats.Count == 0)
                {
                    issues.Add("No serializers are available");
                    status = HealthStatus.Unhealthy;
                }
                else if (availableFormats.Count < _thresholds.MinAvailableSerializers)
                {
                    issues.Add($"Only {availableFormats.Count} serializers available, minimum required: {_thresholds.MinAvailableSerializers}");
                    status = HealthStatus.Degraded;
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check serializer availability: {ex.Message}");
                status = HealthStatus.Degraded;
            }

            return (status, data, issues);
        }

        private (HealthStatus Status, Dictionary<string, object> Data, List<string> Issues) CheckPerformanceMetrics()
        {
            var data = new Dictionary<string, object>();
            var issues = new List<string>();
            var status = HealthStatus.Healthy;

            try
            {
                var statistics = _serializationService.GetStatistics();

                // Calculate failure rate
                var totalOperations = statistics.TotalSerializations + statistics.TotalDeserializations;
                var failureRate = totalOperations > 0 ? (double)statistics.FailedOperations / totalOperations : 0.0;

                data["FailureRate"] = failureRate;
                data["TotalOperations"] = totalOperations;
                data["FailedOperations"] = statistics.FailedOperations;
                data["TotalBytesProcessed"] = statistics.TotalBytesProcessed;
                data["RegisteredTypeCount"] = statistics.RegisteredTypeCount;

                if (failureRate > _thresholds.MaxFailureRate)
                {
                    issues.Add($"Service failure rate ({failureRate:P2}) exceeds threshold ({_thresholds.MaxFailureRate:P2})");
                    status = failureRate > _thresholds.CriticalFailureRate ? HealthStatus.Unhealthy : HealthStatus.Degraded;
                }

                // Check if total operations indicate healthy usage
                if (totalOperations == 0)
                {
                    issues.Add("No serialization operations have been performed");
                    if (status == HealthStatus.Healthy)
                        status = HealthStatus.Degraded;
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to collect performance metrics: {ex.Message}");
                status = HealthStatus.Degraded;
            }

            return (status, data, issues);
        }

        /// <summary>
        /// Configures this health check with new settings.
        /// </summary>
        /// <param name="configuration">The configuration to apply</param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null</exception>
        public void Configure(HealthCheckConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Update properties from configuration
            Timeout = configuration.Timeout;
            Category = configuration.Category;
            
            var correlationId = GetCorrelationId();
            _logger.LogInfo($"SerializationServiceHealthCheck reconfigured with timeout: {Timeout}", correlationId, nameof(SerializationServiceHealthCheck));
        }

        /// <summary>
        /// Gets metadata about this health check.
        /// </summary>
        /// <returns>Dictionary containing metadata</returns>
        public Dictionary<string, object> GetMetadata()
        {
            var statistics = _serializationService.GetStatistics();
            var registeredFormats = _serializationService.GetRegisteredFormats();
            
            return new Dictionary<string, object>
            {
                ["Name"] = Name.ToString(),
                ["Description"] = Description,
                ["Category"] = Category.ToString(),
                ["Timeout"] = Timeout.ToString(),
                ["Version"] = "1.0.0",
                ["Implementation"] = GetType().FullName,
                ["ServiceEnabled"] = _serializationService.IsEnabled,
                ["Thresholds"] = new Dictionary<string, object>
                {
                    ["MaxFailureRate"] = _thresholds.MaxFailureRate,
                    ["CriticalFailureRate"] = _thresholds.CriticalFailureRate,
                    ["MinAvailableSerializers"] = _thresholds.MinAvailableSerializers,
                    ["MaxOpenCircuitBreakers"] = _thresholds.MaxOpenCircuitBreakers,
                    ["MaxHalfOpenCircuitBreakers"] = _thresholds.MaxHalfOpenCircuitBreakers
                },
                ["CurrentStatistics"] = new Dictionary<string, object>
                {
                    ["TotalSerializations"] = statistics.TotalSerializations,
                    ["TotalDeserializations"] = statistics.TotalDeserializations,
                    ["FailedOperations"] = statistics.FailedOperations,
                    ["TotalBytesProcessed"] = statistics.TotalBytesProcessed,
                    ["RegisteredTypeCount"] = statistics.RegisteredTypeCount,
                    ["RegisteredFormats"] = registeredFormats.AsValueEnumerable().Select(f => f.ToString()).ToArray()
                },
                ["Dependencies"] = Array.Empty<string>(),
                ["Tags"] = new[] { "serialization", "service", "circuit-breaker", "fault-tolerance", "core" },
                ["SupportsAsync"] = true,
                ["SupportsCancellation"] = true,
                ["RequiresExternalDependencies"] = false
            };
        }

        private FixedString64Bytes GetCorrelationId()
        {
            return new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
        }
    }

    /// <summary>
    /// Configuration for SerializationService health check thresholds.
    /// </summary>
    public record SerializationServiceHealthThresholds
    {
        /// <summary>
        /// Maximum acceptable failure rate (0.0 to 1.0).
        /// </summary>
        public double MaxFailureRate { get; init; } = 0.02; // 2%

        /// <summary>
        /// Critical failure rate that triggers unhealthy status.
        /// </summary>
        public double CriticalFailureRate { get; init; } = 0.10; // 10%

        /// <summary>
        /// Minimum number of serializers that must be available.
        /// </summary>
        public int MinAvailableSerializers { get; init; } = 1;

        /// <summary>
        /// Maximum number of open circuit breakers before triggering unhealthy status.
        /// </summary>
        public int MaxOpenCircuitBreakers { get; init; } = 2;

        /// <summary>
        /// Maximum number of half-open circuit breakers before triggering degraded status.
        /// </summary>
        public int MaxHalfOpenCircuitBreakers { get; init; } = 1;

        /// <summary>
        /// Default thresholds for production environments.
        /// </summary>
        public static SerializationServiceHealthThresholds Default => new();

        /// <summary>
        /// Relaxed thresholds for development environments.
        /// </summary>
        public static SerializationServiceHealthThresholds Development => new()
        {
            MaxFailureRate = 0.10,
            CriticalFailureRate = 0.25,
            MinAvailableSerializers = 1,
            MaxOpenCircuitBreakers = 3,
            MaxHalfOpenCircuitBreakers = 2
        };

        /// <summary>
        /// Strict thresholds for critical production environments.
        /// </summary>
        public static SerializationServiceHealthThresholds Critical => new()
        {
            MaxFailureRate = 0.005, // 0.5%
            CriticalFailureRate = 0.02, // 2%
            MinAvailableSerializers = 2,
            MaxOpenCircuitBreakers = 1,
            MaxHalfOpenCircuitBreakers = 0
        };
    }

    /// <summary>
    /// Test object for SerializationService health checks.
    /// </summary>
    [Serializable]
    internal record TestSerializationServiceObject
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public int Value { get; init; }
        public DateTime Timestamp { get; init; }
    }
}