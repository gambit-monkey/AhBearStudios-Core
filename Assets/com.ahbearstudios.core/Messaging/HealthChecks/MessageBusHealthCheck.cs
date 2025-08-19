using System;
using System.Collections.Generic;
using System.Threading;
using AhBearStudios.Core.HealthChecking.Checks;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.HealthChecks
{
    /// <summary>
    /// Health check implementation for the message bus system.
    /// Monitors performance, throughput, and system health indicators.
    /// </summary>
    public sealed class MessageBusHealthCheck : IHealthCheck
    {
        private readonly IMessageBusService _messageBusService;
        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;
        private HealthCheckConfiguration _configuration;

        /// <summary>
        /// Gets the name of this health check.
        /// </summary>
        public FixedString64Bytes Name => "MessageBus";

        /// <summary>
        /// Gets the description of this health check.
        /// </summary>
        public string Description => "Monitors message bus performance, throughput, and system health indicators";

        /// <summary>
        /// Gets the category of this health check.
        /// </summary>
        public HealthCheckCategory Category => HealthCheckCategory.System;

        /// <summary>
        /// Gets the timeout for this health check.
        /// </summary>
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets the current configuration for this health check.
        /// </summary>
        public HealthCheckConfiguration Configuration => _configuration ?? CreateDefaultConfiguration();

        /// <summary>
        /// Gets the dependencies for this health check.
        /// </summary>
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();

        /// <summary>
        /// Initializes a new instance of the MessageBusHealthCheck class.
        /// </summary>
        /// <param name="messageBusService">The message bus service to monitor</param>
        /// <param name="config">The message bus configuration</param>
        /// <param name="logger">The logging service</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public MessageBusHealthCheck(
            IMessageBusService messageBusService,
            MessageBusConfig config,
            ILoggingService logger)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs the health check operation for the message bus.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The health check result</returns>
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo("Starting MessageBus health check");

                var statistics = _messageBusService.GetStatistics();
                var currentHealthStatus = _messageBusService.GetHealthStatus();
                var healthData = new Dictionary<string, object>
                {
                    ["InstanceName"] = statistics.InstanceName.ToString(),
                    ["HealthStatus"] = currentHealthStatus.ToString(),
                    ["TotalMessagesPublished"] = statistics.TotalMessagesPublished,
                    ["TotalMessagesProcessed"] = statistics.TotalMessagesProcessed,
                    ["TotalMessagesFailed"] = statistics.TotalMessagesFailed,
                    ["ActiveSubscribers"] = statistics.ActiveSubscribers,
                    ["CurrentQueueDepth"] = statistics.CurrentQueueDepth,
                    ["AverageProcessingTimeMs"] = statistics.AverageProcessingTimeMs,
                    ["MessagesPerSecond"] = statistics.MessagesPerSecond,
                    ["MessagesInRetry"] = statistics.MessagesInRetry,
                    ["DeadLetterQueueSize"] = statistics.DeadLetterQueueSize,
                    ["SuccessRate"] = statistics.SuccessRate,
                    ["FailureRate"] = statistics.FailureRate
                };

                // Check if service is operational based on health status
                if (currentHealthStatus == HealthStatus.Unhealthy || currentHealthStatus == HealthStatus.Critical || currentHealthStatus == HealthStatus.Offline)
                {
                    _logger.LogError($"MessageBus is not operational: {currentHealthStatus}");
                    return HealthCheckResult.Unhealthy(Name.ToString(), $"Message bus service is not operational: {currentHealthStatus}", data: healthData);
                }

                // Check success rate
                if (statistics.SuccessRate < 0.85)
                {
                    var message = $"Low success rate: {statistics.SuccessRate:P2}";
                    _logger.LogWarning(message);
                    return HealthCheckResult.Unhealthy(Name.ToString(), message, data: healthData);
                }

                // Check queue depth
                if (statistics.CurrentQueueDepth > _config.MaxQueueSize * 0.9)
                {
                    var message =
                        $"Queue depth approaching limit: {statistics.CurrentQueueDepth}/{_config.MaxQueueSize}";
                    _logger.LogWarning(message);
                    return HealthCheckResult.Degraded(Name.ToString(), message, data: healthData);
                }

                // Check processing performance
                if (statistics.AverageProcessingTimeMs > _config.HandlerTimeout.TotalMilliseconds * 0.8)
                {
                    var message = $"High processing time: {statistics.AverageProcessingTimeMs:F2}ms";
                    _logger.LogWarning(message);
                    return HealthCheckResult.Degraded(Name.ToString(), message, data: healthData);
                }

                // Check dead letter queue
                if (_config.DeadLetterQueueEnabled &&
                    statistics.DeadLetterQueueSize > _config.DeadLetterQueueMaxSize * 0.8)
                {
                    var message =
                        $"Dead letter queue filling up: {statistics.DeadLetterQueueSize}/{_config.DeadLetterQueueMaxSize}";
                    _logger.LogWarning(message);
                    return HealthCheckResult.Degraded(Name.ToString(), message, data: healthData);
                }

                // Check for degraded performance indicators
                if (statistics.SuccessRate < 0.95 ||
                    statistics.CurrentQueueDepth > _config.MaxQueueSize * 0.5 ||
                    statistics.AverageProcessingTimeMs > 100)
                {
                    var message = "Message bus performance is degraded";
                    _logger.LogInfo(
                        $"{message}: SuccessRate={statistics.SuccessRate:P2}, QueueDepth={statistics.CurrentQueueDepth}, AvgTime={statistics.AverageProcessingTimeMs:F2}ms");
                    return HealthCheckResult.Degraded(Name.ToString(), message, data: healthData);
                }

                // Perform a lightweight functional test
                var functionalTestResult = await PerformFunctionalTestAsync(cancellationToken);
                if (functionalTestResult != null)
                {
                    healthData["FunctionalTestError"] = functionalTestResult;
                    return HealthCheckResult.Degraded(Name.ToString(), $"Functional test failed: {functionalTestResult}", data: healthData);
                }

                _logger.LogInfo("MessageBus health check completed successfully");
                return HealthCheckResult.Healthy(Name.ToString(), "Message bus is operating normally", data: healthData);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("MessageBus health check was cancelled");
                return HealthCheckResult.Unhealthy(Name.ToString(), "Health check operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogException("MessageBus health check failed", ex);
                return HealthCheckResult.Unhealthy(Name.ToString(), $"Health check failed: {ex.Message}", exception: ex);
            }
        }

        /// <summary>
        /// Configures the health check with new configuration settings.
        /// </summary>
        /// <param name="configuration">Configuration to apply to this health check</param>
        public void Configure(HealthCheckConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger.LogInfo("MessageBus health check configuration updated");
        }

        /// <summary>
        /// Gets comprehensive metadata about this health check.
        /// </summary>
        /// <returns>Dictionary containing metadata about the health check</returns>
        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["Name"] = Name.ToString(),
                ["Description"] = Description,
                ["Category"] = Category.ToString(),
                ["Timeout"] = Timeout.ToString(),
                ["ConfigurationVersion"] = Configuration?.Version ?? "Default",
                ["MessageBusInstance"] = _config.InstanceName,
                ["SupportsRetry"] = _config.RetryFailedMessages,
                ["SupportsCircuitBreaker"] = _config.UseCircuitBreaker,
                ["MaxConcurrentHandlers"] = _config.MaxConcurrentHandlers,
                ["MaxQueueSize"] = _config.MaxQueueSize
            };
        }

        /// <summary>
        /// Creates the default configuration for this health check.
        /// </summary>
        /// <returns>Default health check configuration</returns>
        private HealthCheckConfiguration CreateDefaultConfiguration()
        {
            return new HealthCheckConfiguration
            {
                Name = Name,
                DisplayName = "Message Bus Health Check",
                Description = Description,
                Enabled = true,
                Timeout = Timeout,
                Category = Category,
                RetryConfig = new RetryConfig
                {
                    MaxRetries = 1,
                    RetryDelay = TimeSpan.FromSeconds(1)
                }
            };
        }

        /// <summary>
        /// Performs a lightweight functional test of the message bus.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Error message if test fails, null if successful</returns>
        private async UniTask<string> PerformFunctionalTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Create a simple test message
                var testMessage = new HealthCheckTestMessage();
                var messageReceived = false;
                var tcs = new UniTaskCompletionSource<bool>();

                // Set up a test subscription
                using var subscription = _messageBusService.SubscribeToMessage<HealthCheckTestMessage>(msg =>
                {
                    if (msg.Id == testMessage.Id)
                    {
                        messageReceived = true;
                        tcs.TrySetResult(true);
                    }
                });

                // Publish the test message
                await _messageBusService.PublishMessageAsync(testMessage, cancellationToken);

                // Wait for the message to be received (with timeout)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var combinedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    await tcs.Task.AttachExternalCancellation(combinedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    return "Functional test timed out";
                }

                return messageReceived ? null : "Test message was not received";
            }
            catch (Exception ex)
            {
                return $"Functional test exception: {ex.Message}";
            }
        }

    }
}