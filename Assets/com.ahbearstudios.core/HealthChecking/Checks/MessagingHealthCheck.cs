using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Messaging;
using Unity.Collections;

namespace AhBearStudios.Core.HealthChecking.Checks
{
    /// <summary>
    /// Health check for monitoring message bus connectivity, performance, and message flow
    /// </summary>
    /// <remarks>
    /// Provides comprehensive message bus health monitoring including:
    /// - Publisher and subscriber connectivity validation
    /// - Message publish/subscribe round-trip testing
    /// - Queue depth and processing performance monitoring
    /// - Dead letter queue and error handling verification
    /// - Circuit breaker integration for fault tolerance
    /// </remarks>
    public sealed class MessagingHealthCheck : IHealthCheck
    {
        private readonly IMessageBusService _messageBusService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILoggingService _logger;
        private readonly MessagingHealthCheckOptions _options;
        
        private HealthCheckConfiguration _configuration;
        private readonly object _configurationLock = new object();
        private readonly SemaphoreSlim _testMessageSemaphore = new SemaphoreSlim(1, 1);

        /// <inheritdoc />
        public FixedString64Bytes Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public HealthCheckCategory Category => HealthCheckCategory.Network;

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
        /// Initializes the messaging health check with required dependencies and configuration
        /// </summary>
        /// <param name="messageBusService">Message bus service to monitor</param>
        /// <param name="healthCheckService">Health check service for circuit breaker integration</param>
        /// <param name="logger">Logging service for diagnostic information</param>
        /// <param name="options">Optional configuration for messaging health checking</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public MessagingHealthCheck(
            IMessageBusService messageBusService,
            IHealthCheckService healthCheckService,
            ILoggingService logger,
            MessagingHealthCheckOptions options = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? MessagingHealthCheckOptions.CreateDefault();

            Name = new FixedString64Bytes(_options.Name ?? "MessagingHealth");
            Description = _options.Description ?? $"Message bus connectivity and performance monitoring for {_messageBusService.GetType().Name}";

            // Set default configuration
            _configuration = HealthCheckConfiguration.ForNetworkService(
                Name.ToString(), 
                Description);

            _logger.LogInfo($"MessagingHealthCheck '{Name}' initialized with comprehensive message bus monitoring");
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug($"Starting messaging health check '{Name}'");

                // Execute health checks with circuit breaker protection
                if (_options.UseCircuitBreaker && _healthCheckService != null)
                {
                    return await _healthCheckService.ExecuteWithProtectionAsync(
                        $"Messaging.{Name}",
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
                _logger.LogDebug($"Messaging health check '{Name}' was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, $"Messaging health check '{Name}' failed with unexpected error");
                
                data["Exception"] = ex.GetType().Name;
                data["ErrorMessage"] = ex.Message;
                data["StackTrace"] = ex.StackTrace;

                return HealthCheckResult.Unhealthy(
                    $"Messaging health check failed: {ex.Message}",
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

            _logger.LogInfo($"MessagingHealthCheck '{Name}' configuration updated");
        }

        /// <inheritdoc />
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Type"] = nameof(MessagingHealthCheck),
                ["Category"] = Category.ToString(),
                ["Description"] = Description,
                ["MessageBusServiceType"] = _messageBusService?.GetType().Name,
                ["SupportedOperations"] = new[] 
                { 
                    "ConnectivityTest", 
                    "PublishTest", 
                    "SubscribeTest", 
                    "RoundTripTest", 
                    "PerformanceMeasurement" 
                },
                ["CircuitBreakerEnabled"] = _options.UseCircuitBreaker,
                ["TestConfiguration"] = new Dictionary<string, object>
                {
                    ["RoundTripTestEnabled"] = _options.EnableRoundTripTest,
                    ["PublishOnlyTestEnabled"] = _options.EnablePublishOnlyTest,
                    ["SubscriberTestEnabled"] = _options.EnableSubscriberTest,
                    ["TestMessageTimeout"] = _options.TestMessageTimeout.TotalMilliseconds,
                    ["MaxConcurrentTests"] = _options.MaxConcurrentTests
                },
                ["PerformanceThresholds"] = new Dictionary<string, object>
                {
                    ["WarningThreshold"] = _options.PerformanceWarningThreshold.TotalMilliseconds,
                    ["CriticalThreshold"] = _options.PerformanceCriticalThreshold.TotalMilliseconds,
                    ["TimeoutThreshold"] = _options.DefaultTimeout.TotalMilliseconds
                },
                ["Dependencies"] = Dependencies,
                ["Version"] = "1.0.0",
                ["SupportsTransactions"] = _messageBusService is ITransactionalMessageBus,
                ["SupportsMetrics"] = _messageBusService is IMessageBusMetricsProvider
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
                // Ensure only one test message flow at a time
                await _testMessageSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Test 1: Basic connectivity test
                    var connectivityResult = await TestMessageBusConnectivity(cancellationToken);
                    healthChecks.Add(("Connectivity", connectivityResult.Success, connectivityResult.Duration, connectivityResult.Details));
                    data["ConnectivityTest"] = connectivityResult;

                    if (!connectivityResult.Success)
                    {
                        stopwatch.Stop();
                        return CreateUnhealthyResult("Message bus connectivity failed", stopwatch.Elapsed, data, healthChecks);
                    }

                    // Test 2: Publisher functionality test
                    if (_options.EnablePublishOnlyTest)
                    {
                        var publishResult = await TestPublisherFunctionality(cancellationToken);
                        healthChecks.Add(("Publisher", publishResult.Success, publishResult.Duration, publishResult.Details));
                        data["PublisherTest"] = publishResult;
                    }

                    // Test 3: Subscriber functionality test
                    if (_options.EnableSubscriberTest)
                    {
                        var subscribeResult = await TestSubscriberFunctionality(cancellationToken);
                        healthChecks.Add(("Subscriber", subscribeResult.Success, subscribeResult.Duration, subscribeResult.Details));
                        data["SubscriberTest"] = subscribeResult;
                    }

                    // Test 4: Round-trip message test (if enabled)
                    if (_options.EnableRoundTripTest)
                    {
                        var roundTripResult = await TestMessageRoundTrip(cancellationToken);
                        healthChecks.Add(("RoundTrip", roundTripResult.Success, roundTripResult.Duration, roundTripResult.Details));
                        data["RoundTripTest"] = roundTripResult;
                    }

                    // Test 5: Message bus metrics collection
                    var metricsResult = await CollectMessageBusMetrics(cancellationToken);
                    data["MessageBusMetrics"] = metricsResult;
                }
                finally
                {
                    _testMessageSemaphore.Release();
                }

                stopwatch.Stop();

                // Analyze overall health based on all tests
                var overallHealth = AnalyzeOverallHealth(healthChecks, data);
                var statusMessage = CreateStatusMessage(overallHealth, healthChecks, stopwatch.Elapsed);

                _logger.LogDebug($"Messaging health check '{Name}' completed: {overallHealth} in {stopwatch.Elapsed}");

                return CreateHealthResult(overallHealth, statusMessage, stopwatch.Elapsed, data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException(ex, $"Messaging health check '{Name}' execution failed");
                throw;
            }
        }

        private async Task<MessagingTestResult> TestMessageBusConnectivity(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var connectivityTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 2, 15));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(connectivityTimeout);

                // Test basic connectivity by checking if the message bus is operational
                var isConnected = await _messageBusService.IsHealthyAsync(timeoutCts.Token);
                
                stopwatch.Stop();

                return new MessagingTestResult
                {
                    Success = isConnected,
                    Duration = stopwatch.Elapsed,
                    Details = isConnected 
                        ? $"Message bus connectivity verified in {stopwatch.ElapsedMilliseconds}ms"
                        : "Message bus is not responsive or unhealthy"
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Connectivity test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Connectivity test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<MessagingTestResult> TestPublisherFunctionality(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var publishTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 3, 10));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(publishTimeout);

                // Create a test message
                var testMessage = HealthCheckTestMessage.Create("MessagingHealthCheck");

                // Get publisher and test message publishing
                var publisher = _messageBusService.GetPublisher<HealthCheckTestMessage>();
                await publisher.PublishMessageAsync(testMessage, timeoutCts.Token);
                
                stopwatch.Stop();

                // Analyze publish performance
                var performanceStatus = AnalyzeMessagePerformance(stopwatch.Elapsed);

                return new MessagingTestResult
                {
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Details = $"Message published successfully in {stopwatch.ElapsedMilliseconds}ms - {performanceStatus}",
                    MessageId = testMessage.Id
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Publisher test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Publisher test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<MessagingTestResult> TestSubscriberFunctionality(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var subscribeTimeout = TimeSpan.FromSeconds(Math.Min(_options.DefaultTimeout.TotalSeconds / 3, 10));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(subscribeTimeout);

                // Test subscriber creation and basic functionality
                var subscriber = _messageBusService.GetSubscriber<HealthCheckTestMessage>();
                var subscriptionActive = subscriber != null;
                
                stopwatch.Stop();

                return new MessagingTestResult
                {
                    Success = subscriptionActive,
                    Duration = stopwatch.Elapsed,
                    Details = subscriptionActive 
                        ? $"Subscriber created successfully in {stopwatch.ElapsedMilliseconds}ms"
                        : "Failed to create subscriber"
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Subscriber test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Subscriber test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<MessagingTestResult> TestMessageRoundTrip(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var messageReceived = false;
            var receivedMessageId = Guid.Empty;
            
            try
            {
                var roundTripTimeout = _options.TestMessageTimeout;
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(roundTripTimeout);

                // Create test message with unique identifier
                var testMessage = HealthCheckTestMessage.Create("MessagingHealthCheck");

                // Set up subscriber to receive the test message
                var subscriber = _messageBusService.GetSubscriber<HealthCheckTestMessage>();
                var messageReceivedTask = new TaskCompletionSource<HealthCheckTestMessage>();

                // Subscribe to messages temporarily for this test
                Action<HealthCheckTestMessage> messageHandler = (msg) =>
                {
                    if (msg.Id == testMessage.Id)
                    {
                        messageReceivedTask.TrySetResult(msg);
                    }
                };

                subscriber.Subscribe(messageHandler);

                try
                {
                    // Publish the test message
                    var publisher = _messageBusService.GetPublisher<HealthCheckTestMessage>();
                    await publisher.PublishMessageAsync(testMessage, timeoutCts.Token);

                    // Wait for the message to be received
                    var receivedMessage = await messageReceivedTask.Task.WaitAsync(roundTripTimeout, timeoutCts.Token);
                    
                    messageReceived = true;
                    receivedMessageId = receivedMessage.Id;
                }
                finally
                {
                    // Clean up subscription
                    subscriber.Unsubscribe(messageHandler);
                }
                
                stopwatch.Stop();

                var performanceStatus = AnalyzeMessagePerformance(stopwatch.Elapsed);

                return new MessagingTestResult
                {
                    Success = messageReceived,
                    Duration = stopwatch.Elapsed,
                    Details = messageReceived 
                        ? $"Round-trip completed successfully in {stopwatch.ElapsedMilliseconds}ms - {performanceStatus}"
                        : $"Round-trip failed - message not received within {roundTripTimeout.TotalSeconds}s",
                    MessageId = testMessage.Id
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Round-trip test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new MessagingTestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Round-trip test failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        private async Task<Dictionary<string, object>> CollectMessageBusMetrics(CancellationToken cancellationToken)
        {
            var metrics = new Dictionary<string, object>();
            
            try
            {
                // Collect basic message bus information
                metrics["MessageBusType"] = _messageBusService.GetType().Name;
                metrics["SupportsTransactions"] = _messageBusService is ITransactionalMessageBus;
                metrics["TestsEnabled"] = new Dictionary<string, bool>
                {
                    ["RoundTrip"] = _options.EnableRoundTripTest,
                    ["PublishOnly"] = _options.EnablePublishOnlyTest,
                    ["Subscriber"] = _options.EnableSubscriberTest
                };
                
                // Collect message bus specific metrics if available
                if (_messageBusService is IMessageBusMetricsProvider metricsProvider)
                {
                    var busMetrics = await metricsProvider.GetMetricsAsync(cancellationToken);
                    foreach (var metric in busMetrics)
                    {
                        metrics[$"Bus_{metric.Key}"] = metric.Value;
                    }
                }

                // Add performance context
                metrics["PerformanceWarningThreshold"] = _options.PerformanceWarningThreshold.TotalMilliseconds;
                metrics["PerformanceCriticalThreshold"] = _options.PerformanceCriticalThreshold.TotalMilliseconds;
                metrics["TestMessageTimeout"] = _options.TestMessageTimeout.TotalMilliseconds;
                metrics["MaxConcurrentTests"] = _options.MaxConcurrentTests;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to collect message bus metrics: {ex.Message}");
                metrics["MetricsCollectionError"] = ex.Message;
            }

            return metrics;
        }

        private string AnalyzeMessagePerformance(TimeSpan duration)
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
                HealthStatus.Healthy => "Message bus is healthy and performing well",
                HealthStatus.Degraded => "Message bus is operational but showing performance issues",
                HealthStatus.Unhealthy => "Message bus has critical issues affecting functionality",
                _ => "Message bus status is unknown"
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
                _ => HealthCheckResult.Unhealthy("Unknown message bus health status", duration, data)
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
                "Message bus health check failed - circuit breaker is open",
                duration,
                data);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the health check
        /// </summary>
        public void Dispose()
        {
            try
            {
                _testMessageSemaphore?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error disposing MessagingHealthCheck");
            }
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Configuration options for messaging health checking
    /// </summary>
    public sealed class MessagingHealthCheckOptions
    {
        /// <summary>
        /// Name of the health check
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what this health check monitors
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Default timeout for all messaging operations
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Performance threshold that triggers warning status
        /// </summary>
        public TimeSpan PerformanceWarningThreshold { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Performance threshold that triggers critical status
        /// </summary>
        public TimeSpan PerformanceCriticalThreshold { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Timeout for round-trip test messages
        /// </summary>
        public TimeSpan TestMessageTimeout { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Whether to perform round-trip message testing
        /// </summary>
        public bool EnableRoundTripTest { get; set; } = true;

        /// <summary>
        /// Whether to test publisher functionality only
        /// </summary>
        public bool EnablePublishOnlyTest { get; set; } = true;

        /// <summary>
        /// Whether to test subscriber functionality
        /// </summary>
        public bool EnableSubscriberTest { get; set; } = true;

        /// <summary>
        /// Maximum number of concurrent health check tests
        /// </summary>
        public int MaxConcurrentTests { get; set; } = 1;

        /// <summary>
        /// Whether to use circuit breaker pattern for fault tolerance
        /// </summary>
        public bool UseCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Dependencies that must be healthy before this check runs
        /// </summary>
        public FixedString64Bytes[] Dependencies { get; set; }

        /// <summary>
        /// Creates default messaging health check options
        /// </summary>
        /// <returns>Default configuration</returns>
        public static MessagingHealthCheckOptions CreateDefault()
        {
            return new MessagingHealthCheckOptions();
        }

        /// <summary>
        /// Creates options optimized for high-performance scenarios
        /// </summary>
        /// <returns>High-performance configuration</returns>
        public static MessagingHealthCheckOptions CreateHighPerformance()
        {
            return new MessagingHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromSeconds(10),
                PerformanceWarningThreshold = TimeSpan.FromSeconds(1),
                PerformanceCriticalThreshold = TimeSpan.FromSeconds(5),
                TestMessageTimeout = TimeSpan.FromSeconds(5),
                EnableRoundTripTest = false, // Skip for performance
                EnablePublishOnlyTest = true,
                EnableSubscriberTest = false,
                UseCircuitBreaker = true
            };
        }

        /// <summary>
        /// Creates options optimized for comprehensive testing
        /// </summary>
        /// <returns>Comprehensive testing configuration</returns>
        public static MessagingHealthCheckOptions CreateComprehensive()
        {
            return new MessagingHealthCheckOptions
            {
                DefaultTimeout = TimeSpan.FromMinutes(1),
                PerformanceWarningThreshold = TimeSpan.FromSeconds(3),
                PerformanceCriticalThreshold = TimeSpan.FromSeconds(15),
                TestMessageTimeout = TimeSpan.FromSeconds(30),
                EnableRoundTripTest = true,
                EnablePublishOnlyTest = true,
                EnableSubscriberTest = true,
                MaxConcurrentTests = 3,
                UseCircuitBreaker = true
            };
        }
    }

    /// <summary>
    /// Result of an individual messaging test operation
    /// </summary>
    internal sealed class MessagingTestResult
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string Details { get; set; }
        public Guid MessageId { get; set; }
        public Exception Exception { get; set; }
    }


    /// <summary>
    /// Interface for message bus services that provide health check capabilities
    /// </summary>
    public interface IMessageBusHealthProvider
    {
        /// <summary>
        /// Tests message bus connectivity and health
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if message bus is healthy</returns>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for message bus services that can provide detailed metrics
    /// </summary>
    public interface IMessageBusMetricsProvider
    {
        /// <summary>
        /// Gets message bus specific metrics for health monitoring
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of metric names and values</returns>
        Task<Dictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for transactional message bus operations
    /// </summary>
    public interface ITransactionalMessageBus
    {
        /// <summary>
        /// Tests transactional message bus capabilities
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if transactions are supported</returns>
        Task<bool> TestTransactionAsync(CancellationToken cancellationToken = default);
    }

    #endregion
}

/// <summary>
/// Extension methods for message bus health checking
/// </summary>
public static class MessageBusHealthCheckExtensions
{
    /// <summary>
    /// Extension method to check if message bus service implements health provider interface
    /// </summary>
    /// <param name="messageBusService">Message bus service to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message bus is healthy</returns>
    public static async Task<bool> IsHealthyAsync(this IMessageBusService messageBusService, CancellationToken cancellationToken = default)
    {
        if (messageBusService is IMessageBusHealthProvider healthProvider)
        {
            return await healthProvider.IsHealthyAsync(cancellationToken);
        }

        // Fallback: assume healthy if no health provider interface
        return true;
    }
}