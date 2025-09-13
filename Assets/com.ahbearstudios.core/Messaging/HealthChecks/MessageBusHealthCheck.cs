using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Messages;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;
using ZLinq;

namespace AhBearStudios.Core.Messaging.HealthChecks
{
    /// <summary>
    /// Comprehensive health check implementation for the message bus system.
    /// Combines internal metrics monitoring with configurable functional testing.
    /// Supports multiple test levels: Quick (metrics only), Standard (basic tests), and Comprehensive (full testing).
    /// </summary>
    public sealed class MessageBusHealthCheck : IHealthCheck, IDisposable
    {
        private readonly IMessageBusService _messageBusService;
        private readonly MessageBusConfig _config;
        private readonly ILoggingService _logger;
        private readonly MessageBusHealthCheckOptions _options;
        private readonly SemaphoreSlim _testMessageSemaphore = new SemaphoreSlim(1, 1);
        private HealthCheckConfiguration _configuration;
        private readonly object _configurationLock = new object();

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
        public HealthCheckConfiguration Configuration
        {
            get
            {
                lock (_configurationLock)
                {
                    return _configuration ?? CreateDefaultConfiguration();
                }
            }
        }

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
        /// <param name="options">Optional health check configuration options</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        public MessageBusHealthCheck(
            IMessageBusService messageBusService,
            MessageBusConfig config,
            ILoggingService logger,
            MessageBusHealthCheckOptions options = null)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? MessageBusHealthCheckOptions.CreateDefault();
            
            _logger.LogInfo($"MessageBusHealthCheck initialized with test level: {_options.TestLevel}");
        }

        /// <summary>
        /// Performs the health check operation for the message bus.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The health check result</returns>
        public async UniTask<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInfo($"Starting MessageBus health check (Level: {_options.TestLevel})");

                cancellationToken.ThrowIfCancellationRequested();
                
                // Always collect basic statistics
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

                // Perform level-appropriate health checks
                HealthCheckResult result;
                
                switch (_options.TestLevel)
                {
                    case HealthCheckTestLevel.Quick:
                        result = await PerformQuickHealthCheck(statistics, currentHealthStatus, healthData, cancellationToken);
                        break;
                        
                    case HealthCheckTestLevel.Standard:
                        result = await PerformStandardHealthCheck(statistics, currentHealthStatus, healthData, cancellationToken);
                        break;
                        
                    case HealthCheckTestLevel.Comprehensive:
                        result = await PerformComprehensiveHealthCheck(statistics, currentHealthStatus, healthData, cancellationToken);
                        break;
                        
                    default:
                        result = await PerformStandardHealthCheck(statistics, currentHealthStatus, healthData, cancellationToken);
                        break;
                }
                
                stopwatch.Stop();
                healthData["CheckDuration"] = stopwatch.Elapsed.TotalMilliseconds;
                healthData["TestLevel"] = _options.TestLevel.ToString();
                
                _logger.LogInfo($"MessageBus health check completed in {stopwatch.ElapsedMilliseconds}ms");
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("MessageBus health check was cancelled");
                return HealthCheckResult.Unhealthy(Name.ToString(), "Health check operation was cancelled", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogException("MessageBus health check failed", ex);
                return HealthCheckResult.Unhealthy(Name.ToString(), $"Health check failed: {ex.Message}", stopwatch.Elapsed, exception: ex);
            }
        }

        /// <summary>
        /// Configures the health check with new configuration settings.
        /// </summary>
        /// <param name="configuration">Configuration to apply to this health check</param>
        public void Configure(HealthCheckConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            lock (_configurationLock)
            {
                _configuration = configuration;
            }
            
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
                ["MessageBusInstance"] = _config.InstanceName,
                ["SupportsRetry"] = _config.RetryFailedMessages,
                ["SupportsCircuitBreaker"] = _config.UseCircuitBreaker,
                ["MaxConcurrentHandlers"] = _config.MaxConcurrentHandlers,
                ["MaxQueueSize"] = _config.MaxQueueSize,
                ["TestConfiguration"] = new Dictionary<string, object>
                {
                    ["TestLevel"] = _options.TestLevel.ToString(),
                    ["EnableConnectivityTest"] = _options.EnableConnectivityTest,
                    ["EnablePublisherTest"] = _options.EnablePublisherTest,
                    ["EnableSubscriberTest"] = _options.EnableSubscriberTest,
                    ["EnableRoundTripTest"] = _options.EnableRoundTripTest,
                    ["TestTimeout"] = _options.TestTimeout.TotalMilliseconds
                },
                ["PerformanceThresholds"] = new Dictionary<string, object>
                {
                    ["SuccessRateWarning"] = _options.SuccessRateWarningThreshold,
                    ["SuccessRateCritical"] = _options.SuccessRateCriticalThreshold,
                    ["QueueDepthWarning"] = _options.QueueDepthWarningThreshold,
                    ["QueueDepthCritical"] = _options.QueueDepthCriticalThreshold,
                    ["ProcessingTimeWarning"] = _options.ProcessingTimeWarningThreshold.TotalMilliseconds,
                    ["ProcessingTimeCritical"] = _options.ProcessingTimeCriticalThreshold.TotalMilliseconds
                }
            };
        }

        #region Private Health Check Methods
        
        /// <summary>
        /// Performs a quick health check using only metrics analysis.
        /// </summary>
        private async UniTask<HealthCheckResult> PerformQuickHealthCheck(
            MessageBusStatistics statistics,
            HealthStatus currentHealthStatus,
            Dictionary<string, object> healthData,
            CancellationToken cancellationToken)
        {
            // Check if service is operational
            if (!IsServiceOperational(currentHealthStatus))
            {
                _logger.LogError($"MessageBus is not operational: {currentHealthStatus}");
                return HealthCheckResult.Unhealthy(Name.ToString(), 
                    $"Message bus service is not operational: {currentHealthStatus}", data: healthData);
            }
            
            // Analyze metrics
            var metricsResult = AnalyzeMetrics(statistics, healthData);
            return metricsResult;
        }
        
        /// <summary>
        /// Performs a standard health check with metrics and basic functional test.
        /// </summary>
        private async UniTask<HealthCheckResult> PerformStandardHealthCheck(
            MessageBusStatistics statistics,
            HealthStatus currentHealthStatus,
            Dictionary<string, object> healthData,
            CancellationToken cancellationToken)
        {
            // First perform quick checks
            var quickResult = await PerformQuickHealthCheck(statistics, currentHealthStatus, healthData, cancellationToken);
            if (quickResult.Status == HealthStatus.Unhealthy)
                return quickResult;
                
            // Perform basic functional test
            if (_options.EnableConnectivityTest)
            {
                var connectivityResult = await TestConnectivity(cancellationToken);
                healthData["ConnectivityTest"] = connectivityResult;
                
                if (!connectivityResult.Success)
                {
                    return HealthCheckResult.Unhealthy(Name.ToString(), 
                        $"Connectivity test failed: {connectivityResult.Details}", data: healthData);
                }
            }
            
            // Perform simple publish test if metrics show issues
            if (statistics.SuccessRate < _options.SuccessRateWarningThreshold && _options.EnablePublisherTest)
            {
                var publishResult = await TestPublisher(cancellationToken);
                healthData["PublisherTest"] = publishResult;
            }
            
            return quickResult;
        }
        
        /// <summary>
        /// Performs comprehensive health check with all available tests.
        /// </summary>
        private async UniTask<HealthCheckResult> PerformComprehensiveHealthCheck(
            MessageBusStatistics statistics,
            HealthStatus currentHealthStatus,
            Dictionary<string, object> healthData,
            CancellationToken cancellationToken)
        {
            var testResults = new List<(string Name, bool Success, TimeSpan Duration, string Details)>();
            
            // Check if service is operational
            if (!IsServiceOperational(currentHealthStatus))
            {
                _logger.LogError($"MessageBus is not operational: {currentHealthStatus}");
                return HealthCheckResult.Unhealthy(Name.ToString(), 
                    $"Message bus service is not operational: {currentHealthStatus}", data: healthData);
            }
            
            // Ensure only one test flow at a time
            await _testMessageSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Test 1: Connectivity
                if (_options.EnableConnectivityTest)
                {
                    var result = await TestConnectivity(cancellationToken);
                    testResults.Add(("Connectivity", result.Success, result.Duration, result.Details));
                    healthData["ConnectivityTest"] = result;
                    
                    if (!result.Success)
                    {
                        return CreateUnhealthyResult("Connectivity test failed", healthData, testResults);
                    }
                }
                
                // Test 2: Publisher
                if (_options.EnablePublisherTest)
                {
                    var result = await TestPublisher(cancellationToken);
                    testResults.Add(("Publisher", result.Success, result.Duration, result.Details));
                    healthData["PublisherTest"] = result;
                }
                
                // Test 3: Subscriber
                if (_options.EnableSubscriberTest)
                {
                    var result = await TestSubscriber(cancellationToken);
                    testResults.Add(("Subscriber", result.Success, result.Duration, result.Details));
                    healthData["SubscriberTest"] = result;
                }
                
                // Test 4: Round-trip
                if (_options.EnableRoundTripTest)
                {
                    var result = await TestRoundTrip(cancellationToken);
                    testResults.Add(("RoundTrip", result.Success, result.Duration, result.Details));
                    healthData["RoundTripTest"] = result;
                }
                
                // Analyze metrics
                var metricsResult = AnalyzeMetrics(statistics, healthData);
                
                // Analyze overall health
                var overallHealth = AnalyzeOverallHealth(testResults, metricsResult.Status);
                var message = CreateStatusMessage(overallHealth, testResults, statistics);
                
                return CreateHealthResult(overallHealth, message, healthData);
            }
            finally
            {
                _testMessageSemaphore.Release();
            }
        }
        
        /// <summary>
        /// Analyzes message bus metrics and returns appropriate health status.
        /// </summary>
        private HealthCheckResult AnalyzeMetrics(MessageBusStatistics statistics, Dictionary<string, object> healthData)
        {
            // Check critical thresholds
            if (statistics.SuccessRate < _options.SuccessRateCriticalThreshold)
            {
                var message = $"Critical: Low success rate: {statistics.SuccessRate:P2}";
                _logger.LogError(message);
                return HealthCheckResult.Unhealthy(Name.ToString(), message, data: healthData);
            }
            
            if (statistics.CurrentQueueDepth > _config.MaxQueueSize * _options.QueueDepthCriticalThreshold)
            {
                var message = $"Critical: Queue depth exceeds threshold: {statistics.CurrentQueueDepth}/{_config.MaxQueueSize}";
                _logger.LogError(message);
                return HealthCheckResult.Unhealthy(Name.ToString(), message, data: healthData);
            }
            
            if (statistics.AverageProcessingTimeMs > _options.ProcessingTimeCriticalThreshold.TotalMilliseconds)
            {
                var message = $"Critical: Processing time exceeds threshold: {statistics.AverageProcessingTimeMs:F2}ms";
                _logger.LogError(message);
                return HealthCheckResult.Unhealthy(Name.ToString(), message, data: healthData);
            }
            
            // Check warning thresholds
            if (statistics.SuccessRate < _options.SuccessRateWarningThreshold ||
                statistics.CurrentQueueDepth > _config.MaxQueueSize * _options.QueueDepthWarningThreshold ||
                statistics.AverageProcessingTimeMs > _options.ProcessingTimeWarningThreshold.TotalMilliseconds)
            {
                var message = "Message bus performance is degraded";
                _logger.LogWarning($"{message}: SuccessRate={statistics.SuccessRate:P2}, " +
                    $"QueueDepth={statistics.CurrentQueueDepth}, AvgTime={statistics.AverageProcessingTimeMs:F2}ms");
                return HealthCheckResult.Degraded(Name.ToString(), message, data: healthData);
            }
            
            // Check dead letter queue if enabled
            if (_config.DeadLetterQueueEnabled && 
                statistics.DeadLetterQueueSize > _config.DeadLetterQueueMaxSize * 0.8)
            {
                var message = $"Dead letter queue filling up: {statistics.DeadLetterQueueSize}/{_config.DeadLetterQueueMaxSize}";
                _logger.LogWarning(message);
                return HealthCheckResult.Degraded(Name.ToString(), message, data: healthData);
            }
            
            return HealthCheckResult.Healthy(Name.ToString(), "Message bus metrics are within normal parameters", data: healthData);
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
                Enabled = true,
                Timeout = Timeout,
                Category = Category,
                Retry = new RetryConfig
                {
                    MaxRetries = 1,
                    RetryDelay = TimeSpan.FromSeconds(1)
                }
            };
        }

        #endregion
        
        #region Test Methods
        
        /// <summary>
        /// Tests basic message bus connectivity.
        /// </summary>
        private async UniTask<TestResult> TestConnectivity(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var timeout = TimeSpan.FromSeconds(Math.Min(_options.TestTimeout.TotalSeconds / 2, 5));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);
                
                // Check if message bus is responsive
                var isHealthy = await _messageBusService.IsHealthyAsync(timeoutCts.Token);
                
                stopwatch.Stop();
                return new TestResult
                {
                    Success = isHealthy,
                    Duration = stopwatch.Elapsed,
                    Details = isHealthy 
                        ? $"Connectivity verified in {stopwatch.ElapsedMilliseconds}ms"
                        : "Message bus is not responsive"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Connectivity test failed: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Tests message publishing functionality.
        /// </summary>
        private async UniTask<TestResult> TestPublisher(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var testMessage = HealthCheckTestMessage.Create("MessageBusHealthCheck");
                await _messageBusService.PublishMessageAsync(testMessage, cancellationToken);
                
                stopwatch.Stop();
                return new TestResult
                {
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    Details = $"Message published in {stopwatch.ElapsedMilliseconds}ms",
                    MessageId = testMessage.Id
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Publisher test failed: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Tests subscription functionality.
        /// </summary>
        private async UniTask<TestResult> TestSubscriber(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var subscriptionActive = false;
                using var subscription = _messageBusService.SubscribeToMessage<HealthCheckTestMessage>(_ => { });
                subscriptionActive = subscription != null;
                
                stopwatch.Stop();
                return new TestResult
                {
                    Success = subscriptionActive,
                    Duration = stopwatch.Elapsed,
                    Details = subscriptionActive
                        ? $"Subscription created in {stopwatch.ElapsedMilliseconds}ms"
                        : "Failed to create subscription"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Subscriber test failed: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Tests message round-trip functionality.
        /// </summary>
        private async UniTask<TestResult> TestRoundTrip(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var testMessage = HealthCheckTestMessage.Create("MessageBusHealthCheck");
                var messageReceived = false;
                var tcs = new TaskCompletionSource<bool>();
                
                using var subscription = _messageBusService.SubscribeToMessage<HealthCheckTestMessage>(msg =>
                {
                    if (msg.Id == testMessage.Id)
                    {
                        messageReceived = true;
                        tcs.TrySetResult(true);
                    }
                });
                
                await _messageBusService.PublishMessageAsync(testMessage, cancellationToken);
                
                using var timeoutCts = new CancellationTokenSource(_options.TestTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                await tcs.Task.AsUniTask().AttachExternalCancellation(combinedCts.Token);
                
                stopwatch.Stop();
                return new TestResult
                {
                    Success = messageReceived,
                    Duration = stopwatch.Elapsed,
                    Details = $"Round-trip completed in {stopwatch.ElapsedMilliseconds}ms",
                    MessageId = testMessage.Id
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new TestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = "Round-trip test timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult
                {
                    Success = false,
                    Duration = stopwatch.Elapsed,
                    Details = $"Round-trip test failed: {ex.Message}"
                };
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsServiceOperational(HealthStatus status)
        {
            return status != HealthStatus.Unhealthy && 
                   status != HealthStatus.Critical && 
                   status != HealthStatus.Offline;
        }
        
        private HealthStatus AnalyzeOverallHealth(
            List<(string Name, bool Success, TimeSpan Duration, string Details)> testResults,
            HealthStatus metricsStatus)
        {
            if (testResults.Count == 0)
                return metricsStatus;
                
            var failedTests = testResults.AsValueEnumerable().Where(t => !t.Success).Count();
            var totalTests = testResults.Count;
            
            if (failedTests > totalTests / 2)
                return HealthStatus.Unhealthy;
            if (failedTests > 0 || metricsStatus == HealthStatus.Degraded)
                return HealthStatus.Degraded;
                
            return HealthStatus.Healthy;
        }
        
        private string CreateStatusMessage(
            HealthStatus status,
            List<(string Name, bool Success, TimeSpan Duration, string Details)> testResults,
            MessageBusStatistics statistics)
        {
            var successfulTests = testResults.AsValueEnumerable().Where(t => t.Success).Count();
            var totalTests = testResults.Count;
            
            var statusDesc = status switch
            {
                HealthStatus.Healthy => "Message bus is healthy",
                HealthStatus.Degraded => "Message bus is degraded",
                HealthStatus.Unhealthy => "Message bus is unhealthy",
                _ => "Message bus status unknown"
            };
            
            if (totalTests > 0)
            {
                return $"{statusDesc} - {successfulTests}/{totalTests} tests passed, " +
                       $"Success rate: {statistics.SuccessRate:P2}, Queue: {statistics.CurrentQueueDepth}";
            }
            
            return $"{statusDesc} - Success rate: {statistics.SuccessRate:P2}, Queue: {statistics.CurrentQueueDepth}";
        }
        
        private HealthCheckResult CreateHealthResult(
            HealthStatus status,
            string message,
            Dictionary<string, object> data)
        {
            return status switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(Name.ToString(), message, data: data),
                HealthStatus.Degraded => HealthCheckResult.Degraded(Name.ToString(), message, data: data),
                HealthStatus.Unhealthy => HealthCheckResult.Unhealthy(Name.ToString(), message, data: data),
                _ => HealthCheckResult.Unhealthy(Name.ToString(), "Unknown status", data: data)
            };
        }
        
        private HealthCheckResult CreateUnhealthyResult(
            string reason,
            Dictionary<string, object> data,
            List<(string Name, bool Success, TimeSpan Duration, string Details)> testResults)
        {
            data["FailedTests"] = testResults.AsValueEnumerable().Where(t => !t.Success).ToList();
            return HealthCheckResult.Unhealthy(Name.ToString(), reason, data: data);
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes resources used by the health check.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _testMessageSemaphore?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException("Error disposing MessageBusHealthCheck", ex);
            }
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration options for message bus health checking.
    /// </summary>
    public sealed class MessageBusHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the level of testing to perform.
        /// </summary>
        public HealthCheckTestLevel TestLevel { get; set; } = HealthCheckTestLevel.Standard;
        
        /// <summary>
        /// Gets or sets whether to test connectivity.
        /// </summary>
        public bool EnableConnectivityTest { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to test publisher functionality.
        /// </summary>
        public bool EnablePublisherTest { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to test subscriber functionality.
        /// </summary>
        public bool EnableSubscriberTest { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether to test round-trip messaging.
        /// </summary>
        public bool EnableRoundTripTest { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the timeout for test operations.
        /// </summary>
        public TimeSpan TestTimeout { get; set; } = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// Gets or sets the warning threshold for success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRateWarningThreshold { get; set; } = 0.95;
        
        /// <summary>
        /// Gets or sets the critical threshold for success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRateCriticalThreshold { get; set; } = 0.85;
        
        /// <summary>
        /// Gets or sets the warning threshold for queue depth (percentage of max).
        /// </summary>
        public double QueueDepthWarningThreshold { get; set; } = 0.5;
        
        /// <summary>
        /// Gets or sets the critical threshold for queue depth (percentage of max).
        /// </summary>
        public double QueueDepthCriticalThreshold { get; set; } = 0.9;
        
        /// <summary>
        /// Gets or sets the warning threshold for processing time.
        /// </summary>
        public TimeSpan ProcessingTimeWarningThreshold { get; set; } = TimeSpan.FromMilliseconds(100);
        
        /// <summary>
        /// Gets or sets the critical threshold for processing time.
        /// </summary>
        public TimeSpan ProcessingTimeCriticalThreshold { get; set; } = TimeSpan.FromSeconds(1);
        
        /// <summary>
        /// Creates default options for message bus health checking.
        /// </summary>
        public static MessageBusHealthCheckOptions CreateDefault()
        {
            return new MessageBusHealthCheckOptions();
        }
        
        /// <summary>
        /// Creates options for quick health checking (metrics only).
        /// </summary>
        public static MessageBusHealthCheckOptions CreateQuick()
        {
            return new MessageBusHealthCheckOptions
            {
                TestLevel = HealthCheckTestLevel.Quick,
                EnableConnectivityTest = false,
                EnablePublisherTest = false,
                EnableSubscriberTest = false,
                EnableRoundTripTest = false,
                TestTimeout = TimeSpan.FromSeconds(2)
            };
        }
        
        /// <summary>
        /// Creates options for comprehensive health checking.
        /// </summary>
        public static MessageBusHealthCheckOptions CreateComprehensive()
        {
            return new MessageBusHealthCheckOptions
            {
                TestLevel = HealthCheckTestLevel.Comprehensive,
                EnableConnectivityTest = true,
                EnablePublisherTest = true,
                EnableSubscriberTest = true,
                EnableRoundTripTest = true,
                TestTimeout = TimeSpan.FromSeconds(15)
            };
        }
    }
    
    /// <summary>
    /// Defines the level of testing for health checks.
    /// </summary>
    public enum HealthCheckTestLevel
    {
        /// <summary>
        /// Quick metrics-only check.
        /// </summary>
        Quick,
        
        /// <summary>
        /// Standard check with basic tests.
        /// </summary>
        Standard,
        
        /// <summary>
        /// Comprehensive check with all tests.
        /// </summary>
        Comprehensive
    }
    
    /// <summary>
    /// Result of an individual test operation.
    /// </summary>
    internal sealed class TestResult
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string Details { get; set; }
        public Guid MessageId { get; set; }
    }
    
    #endregion
    
    /// <summary>
    /// Extension methods for message bus health checking.
    /// </summary>
    public static class MessageBusHealthCheckExtensions
    {
        /// <summary>
        /// Checks if the message bus service is healthy.
        /// </summary>
        public static async UniTask<bool> IsHealthyAsync(
            this IMessageBusService messageBusService,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = messageBusService.GetStatistics();
                var status = messageBusService.GetHealthStatus();
                
                return status == HealthStatus.Healthy && statistics.SuccessRate > 0.9;
            }
            catch
            {
                return false;
            }
        }
    }
}