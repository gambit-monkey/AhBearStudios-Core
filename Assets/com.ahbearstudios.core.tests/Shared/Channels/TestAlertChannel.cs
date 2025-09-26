using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Common.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Shared.Channels
{
    /// <summary>
    /// Production-ready test channel implementing TDD test double patterns following guidelines.
    /// Acts as a SPY pattern test double - records interactions while providing realistic channel behavior.
    /// Integrates with shared test doubles from TestDoubles directory for comprehensive testing scenarios.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests with zero-allocation validation.
    /// Provides performance testing, frame budget compliance, and correlation tracking for Unity game development.
    /// </summary>
    public sealed class TestAlertChannel : BaseAlertChannel
    {
        #region Private Fields - Following TDD Test Double Pattern

        private readonly List<Alert> _sentAlerts = new List<Alert>();
        private readonly List<AlertCall> _alertCalls = new List<AlertCall>();
        private readonly Dictionary<Guid, List<Alert>> _correlationAlerts = new Dictionary<Guid, List<Alert>>();
        private readonly Dictionary<Guid, DateTime> _correlationTimestamps = new Dictionary<Guid, DateTime>();

        // Shared Test Doubles
        private readonly StubLoggingService _loggingService;
        private readonly SpyMessageBusService _spyMessageBus;
        private readonly AllocationTracker _allocationTracker;
        private readonly PerformanceTestHelper _performanceHelper;
        private readonly TestCorrelationHelper _correlationHelper;

        private readonly object _lockObject = new object();
        private bool _isHealthy = true;
        private bool _simulateFailure = false;
        private TimeSpan _simulatedLatency = TimeSpan.Zero;
        private int _consecutiveFailures = 0;
        private readonly DateTime _createdAt = DateTime.UtcNow;

        #endregion

        #region Test Verification Properties

        /// <summary>
        /// Gets all sent alerts for test verification.
        /// </summary>
        public IReadOnlyList<Alert> SentAlerts
        {
            get
            {
                lock (_lockObject)
                {
                    return _sentAlerts.ToList();
                }
            }
        }

        /// <summary>
        /// Gets all alert calls with metadata for test verification.
        /// </summary>
        public IReadOnlyList<AlertCall> AlertCalls
        {
            get
            {
                lock (_lockObject)
                {
                    return _alertCalls.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the number of alerts sent with a specific severity.
        /// </summary>
        public int GetAlertCount(AlertSeverity severity)
        {
            lock (_lockObject)
            {
                return _sentAlerts.Count(a => a.Severity == severity);
            }
        }

        /// <summary>
        /// Gets all alerts for a specific correlation ID.
        /// </summary>
        public IReadOnlyList<Alert> GetAlertsForCorrelation(Guid correlationId)
        {
            lock (_lockObject)
            {
                return _correlationAlerts.TryGetValue(correlationId, out var alerts)
                    ? alerts.ToList()
                    : new List<Alert>();
            }
        }

        /// <summary>
        /// Checks if an alert with specific criteria was sent.
        /// </summary>
        public bool WasAlertSent(Func<Alert, bool> predicate)
        {
            lock (_lockObject)
            {
                return _sentAlerts.Any(predicate);
            }
        }

        /// <summary>
        /// Gets the last sent alert.
        /// </summary>
        public Alert? GetLastAlert()
        {
            lock (_lockObject)
            {
                return _sentAlerts.LastOrDefault();
            }
        }

        /// <summary>
        /// Gets the recorded logging service for verification.
        /// </summary>
        public StubLoggingService LoggingService => _loggingService;

        
        /// <summary>
        /// Gets access to the shared test doubles for external verification (CLAUDETESTS.md compliance).
        /// </summary>
        public StubLoggingService SharedLogging => _loggingService;
        public SpyMessageBusService SharedMessageBus => _spyMessageBus;
        public AllocationTracker SharedAllocationTracker => _allocationTracker;
        public PerformanceTestHelper SharedPerformanceHelper => _performanceHelper;

        #endregion

        public override FixedString64Bytes Name { get; }

        /// <summary>
        /// Initializes TestAlertChannel with shared test doubles following CLAUDETESTS.md guidelines.
        /// Uses dependency injection of shared test doubles for consistent TDD testing patterns.
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <param name="messageBusService">Optional message bus service (uses shared spy if not provided)</param>
        /// <param name="stubLogging">Optional stub logging service</param>
        /// <param name="allocationTracker">Optional allocation tracker</param>
        /// <param name="performanceHelper">Optional performance helper</param>
        public TestAlertChannel(
            string name = "TestChannel",
            IMessageBusService messageBusService = null,
            StubLoggingService stubLogging = null,
            AllocationTracker allocationTracker = null,
            PerformanceTestHelper performanceHelper = null) : base(messageBusService)
        {
            Name = name ?? "TestChannel";
            MinimumSeverity = AlertSeverity.Debug;

            // Initialize shared test doubles
            _loggingService = stubLogging ?? new StubLoggingService();
            _spyMessageBus = messageBusService as SpyMessageBusService ?? new SpyMessageBusService();
            _allocationTracker = allocationTracker ?? new AllocationTracker();
            _performanceHelper = performanceHelper ?? new PerformanceTestHelper();
            _correlationHelper = new TestCorrelationHelper(_loggingService);

            var correlationId = _correlationHelper.CreateCorrelationId($"TestChannel_{name}");
            _loggingService.LogInfo($"TestAlertChannel '{name}' created with shared test doubles",
                new FixedString64Bytes(correlationId.ToString()),
                sourceContext: "TestAlertChannel");
        }

        /// <summary>
        /// Alternative constructor for advanced test scenarios requiring explicit test double instances.
        /// Provides full control over test double configuration for complex testing scenarios.
        /// </summary>
        public TestAlertChannel(
            string name,
            StubLoggingService stubLogging,
            SpyMessageBusService spyMessageBus,
            AllocationTracker allocationTracker = null,
            PerformanceTestHelper performanceHelper = null,
            TestCorrelationHelper correlationHelper = null) : base(spyMessageBus)
        {
            Name = name ?? "TestChannel";
            MinimumSeverity = AlertSeverity.Debug;

            _loggingService = stubLogging ?? throw new ArgumentNullException(nameof(stubLogging));
            _spyMessageBus = spyMessageBus ?? throw new ArgumentNullException(nameof(spyMessageBus));
            _allocationTracker = allocationTracker ?? new AllocationTracker();
            _performanceHelper = performanceHelper ?? new PerformanceTestHelper();
            _correlationHelper = correlationHelper ?? new TestCorrelationHelper(_loggingService);

            var correlationId = _correlationHelper.CreateCorrelationId($"TestChannel_{name}");
            _loggingService.LogInfo($"TestAlertChannel '{name}' created with explicit test doubles",
                new FixedString64Bytes(correlationId.ToString()),
                sourceContext: "TestAlertChannel");
        }

        #region Test Configuration Methods

        /// <summary>
        /// Sets the health status of this test channel for testing purposes.
        /// </summary>
        /// <param name="healthy">True if the channel should report as healthy, false otherwise</param>
        public void SetHealthy(bool healthy)
        {
            _isHealthy = healthy;
            _loggingService.LogInfo($"TestAlertChannel health set to: {healthy}",
                sourceContext: "TestAlertChannel");
        }

        /// <summary>
        /// Configures the channel to simulate failures for testing error handling.
        /// </summary>
        /// <param name="shouldFail">True to simulate failures, false to operate normally</param>
        public void SimulateFailure(bool shouldFail)
        {
            _simulateFailure = shouldFail;
            if (shouldFail)
            {
                _consecutiveFailures++;
                _loggingService.LogWarning($"TestAlertChannel configured to simulate failure (attempt #{_consecutiveFailures})",
                    sourceContext: "TestAlertChannel");
            }
            else
            {
                _consecutiveFailures = 0;
                _loggingService.LogInfo("TestAlertChannel failure simulation disabled",
                    sourceContext: "TestAlertChannel");
            }
        }

        /// <summary>
        /// Configures simulated latency for performance testing.
        /// </summary>
        /// <param name="latency">The latency to simulate</param>
        public void SetSimulatedLatency(TimeSpan latency)
        {
            _simulatedLatency = latency;
            _loggingService.LogInfo($"TestAlertChannel latency set to: {latency.TotalMilliseconds}ms",
                sourceContext: "TestAlertChannel");
        }

        /// <summary>
        /// Validates frame budget compliance for alert operations.
        /// Ensures operations complete within Unity's 60 FPS requirements (16.67ms budget).
        /// </summary>
        /// <param name="operation">The operation to validate</param>
        /// <param name="operationName">Name for reporting</param>
        /// <param name="customBudget">Custom budget (optional, defaults to TestConstants.FrameBudget)</param>
        /// <returns>True if operation completes within frame budget</returns>
        public bool ValidateFrameBudgetCompliance(System.Action operation, string operationName, TimeSpan? customBudget = null)
        {
            var budget = customBudget ?? TestConstants.FrameBudget;
            var correlationId = _correlationHelper.CreateCorrelationId($"FrameBudget_{operationName}");

            try
            {
                // Use shared PerformanceTestHelper for consistent measurement
                var result = _performanceHelper.Measure(operation, operationName);
                var withinBudget = result.Duration <= budget;

                // Log using shared test double with proper correlation
                _loggingService.LogInfo(
                    $"Frame budget validation for '{operationName}': {result.Duration.TotalMilliseconds:F2}ms " +
                    $"(Budget: {budget.TotalMilliseconds:F2}ms) - {(withinBudget ? "PASS" : "FAIL")}",
                    new FixedString64Bytes(correlationId.ToString()),
                    sourceContext: "TestAlertChannel");

                return withinBudget;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Frame budget validation failed for '{operationName}'", ex,
                    new FixedString64Bytes(correlationId.ToString()), "TestAlertChannel");
                return false;
            }
        }

        /// <summary>
        /// Async version of frame budget compliance validation using UniTask for Unity compatibility.
        /// Follows CLAUDETESTS.md async testing patterns.
        /// </summary>
        public async UniTask<bool> ValidateFrameBudgetComplianceAsync(Func<UniTask> operation, string operationName, TimeSpan? customBudget = null)
        {
            var budget = customBudget ?? TestConstants.FrameBudget;
            var correlationId = _correlationHelper.CreateCorrelationId($"FrameBudgetAsync_{operationName}");

            try
            {
                // Use shared PerformanceTestHelper for async measurement
                var result = await _performanceHelper.MeasureAsync(operation, operationName);
                var withinBudget = result.Duration <= budget;

                _loggingService.LogInfo(
                    $"Async frame budget validation for '{operationName}': {result.Duration.TotalMilliseconds:F2}ms " +
                    $"(Budget: {budget.TotalMilliseconds:F2}ms) - {(withinBudget ? "PASS" : "FAIL")}",
                    new FixedString64Bytes(correlationId.ToString()),
                    sourceContext: "TestAlertChannel");

                return withinBudget;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Async frame budget validation failed for '{operationName}'", ex,
                    new FixedString64Bytes(correlationId.ToString()), "TestAlertChannel");
                return false;
            }
        }

        /// <summary>
        /// Validates zero-allocation patterns for Unity Collections usage following CLAUDETESTS.md guidelines.
        /// Uses shared AllocationTracker for consistent allocation measurement.
        /// </summary>
        public bool ValidateZeroAllocations(System.Action operation, string operationName)
        {
            var correlationId = _correlationHelper.CreateCorrelationId($"ZeroAlloc_{operationName}");

            try
            {
                var result = _allocationTracker.MeasureAllocations(operation, operationName);
                var isZeroAllocation = result.TotalBytes == 0 && result.TotalAllocations == 0;

                _loggingService.LogInfo(
                    $"Zero allocation validation for '{operationName}': {result.TotalBytes} bytes, " +
                    $"{result.TotalAllocations} collections - {(isZeroAllocation ? "PASS" : "FAIL")}",
                    new FixedString64Bytes(correlationId.ToString()),
                    sourceContext: "TestAlertChannel");

                return isZeroAllocation;
            }
            catch (Exception ex)
            {
                _loggingService.LogException($"Zero allocation validation failed for '{operationName}'", ex,
                    new FixedString64Bytes(correlationId.ToString()), "TestAlertChannel");
                return false;
            }
        }

        /// <summary>
        /// Clears all recorded alerts and interactions for test isolation following CLAUDETESTS.md guidelines.
        /// Clears both channel-specific data and shared test doubles for complete isolation.
        /// </summary>
        public void ClearRecordedAlerts()
        {
            var correlationId = _correlationHelper.CreateCorrelationId("ClearData");

            lock (_lockObject)
            {
                _sentAlerts.Clear();
                _alertCalls.Clear();
                _correlationAlerts.Clear();
                _correlationTimestamps.Clear();

                // Clear shared test doubles (CLAUDETESTS.md compliance)
                _loggingService?.ClearLogs();
                _spyMessageBus?.ClearRecordedInteractions();
                _allocationTracker?.Clear();
                _performanceHelper?.Clear();
                _correlationHelper?.Clear();

                _consecutiveFailures = 0;
                _simulateFailure = false;
                _isHealthy = true;

                _loggingService.LogInfo("TestAlertChannel cleared all recorded data and reset shared test doubles",
                    new FixedString64Bytes(correlationId.ToString()),
                    sourceContext: "TestAlertChannel");
            }
        }

        #endregion

        protected override bool SendAlertCore(Alert alert, Guid correlationId)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Simulate latency if configured
                if (_simulatedLatency > TimeSpan.Zero)
                {
                    System.Threading.Thread.Sleep(_simulatedLatency);
                }

                // Simulate failure if configured
                if (_simulateFailure)
                {
                    var errorMessage = $"Simulated failure in TestAlertChannel (failure #{_consecutiveFailures})";
                    _loggingService.LogError(errorMessage, correlationId, "TestAlertChannel");
                    throw new InvalidOperationException(errorMessage);
                }

                // Check health status
                if (!_isHealthy)
                {
                    var healthError = "Test channel is not healthy";
                    _loggingService.LogError(healthError, correlationId, "TestAlertChannel");
                    throw new InvalidOperationException(healthError);
                }

                lock (_lockObject)
                {
                    // Record the alert
                    _sentAlerts.Add(alert);

                    // Record call metadata
                    var alertCall = new AlertCall
                    {
                        Alert = alert,
                        CorrelationId = correlationId,
                        IsAsync = false,
                        Timestamp = startTime,
                        Duration = DateTime.UtcNow - startTime,
                        Success = true
                    };
                    _alertCalls.Add(alertCall);

                    // Track correlation
                    if (correlationId != Guid.Empty)
                    {
                        if (!_correlationAlerts.ContainsKey(correlationId))
                        {
                            _correlationAlerts[correlationId] = new List<Alert>();
                            _correlationTimestamps[correlationId] = startTime;
                        }
                        _correlationAlerts[correlationId].Add(alert);
                    }

                    _loggingService.LogInfo($"Alert sent successfully: {alert.Severity} - {alert.Message}",
                        correlationId, "TestAlertChannel");
                }

                return true;
            }
            catch (Exception ex)
            {
                // Record failed call
                lock (_lockObject)
                {
                    var failedCall = new AlertCall
                    {
                        Alert = alert,
                        CorrelationId = correlationId,
                        IsAsync = false,
                        Timestamp = startTime,
                        Duration = DateTime.UtcNow - startTime,
                        Success = false,
                        Exception = ex
                    };
                    _alertCalls.Add(failedCall);
                }

                _loggingService.LogException($"Failed to send alert: {alert?.Severity} - {alert?.Message}", ex, correlationId, "TestAlertChannel");
                throw;
            }
        }

        protected override async UniTask<bool> SendAlertAsyncCore(Alert alert, Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Simulate async latency if configured
                if (_simulatedLatency > TimeSpan.Zero)
                {
                    await UniTask.Delay(_simulatedLatency, cancellationToken: cancellationToken);
                }

                // Simulate failure if configured
                if (_simulateFailure)
                {
                    var errorMessage = $"Simulated async failure in TestAlertChannel (failure #{_consecutiveFailures})";
                    _loggingService.LogError(errorMessage, correlationId, "TestAlertChannel");
                    throw new InvalidOperationException(errorMessage);
                }

                // Check health status
                if (!_isHealthy)
                {
                    var healthError = "Test channel is not healthy";
                    _loggingService.LogError(healthError, correlationId, "TestAlertChannel");
                    throw new InvalidOperationException(healthError);
                }

                lock (_lockObject)
                {
                    // Record the alert
                    _sentAlerts.Add(alert);

                    // Record call metadata
                    var alertCall = new AlertCall
                    {
                        Alert = alert,
                        CorrelationId = correlationId,
                        IsAsync = true,
                        Timestamp = startTime,
                        Duration = DateTime.UtcNow - startTime,
                        Success = true
                    };
                    _alertCalls.Add(alertCall);

                    // Track correlation
                    if (correlationId != Guid.Empty)
                    {
                        if (!_correlationAlerts.ContainsKey(correlationId))
                        {
                            _correlationAlerts[correlationId] = new List<Alert>();
                            _correlationTimestamps[correlationId] = startTime;
                        }
                        _correlationAlerts[correlationId].Add(alert);
                    }

                    _loggingService.LogInfo($"Alert sent asynchronously: {alert.Severity} - {alert.Message}",
                        correlationId, "TestAlertChannel");
                }

                return true;
            }
            catch (Exception ex)
            {
                // Record failed call
                lock (_lockObject)
                {
                    var failedCall = new AlertCall
                    {
                        Alert = alert,
                        CorrelationId = correlationId,
                        IsAsync = true,
                        Timestamp = startTime,
                        Duration = DateTime.UtcNow - startTime,
                        Success = false,
                        Exception = ex
                    };
                    _alertCalls.Add(failedCall);
                }

                _loggingService.LogException($"Failed to send alert asynchronously: {alert?.Severity} - {alert?.Message}", ex, correlationId, "TestAlertChannel");
                throw;
            }
        }

        protected override async UniTask<ChannelHealthResult> TestHealthAsyncCore(Guid correlationId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _loggingService.LogInfo("Starting health check for TestAlertChannel", correlationId, "TestAlertChannel");

                // Simulate health check latency if configured
                if (_simulatedLatency > TimeSpan.Zero)
                {
                    await UniTask.Delay(_simulatedLatency, cancellationToken: cancellationToken);
                }

                await UniTask.CompletedTask;
                var duration = DateTime.UtcNow - startTime;

                // Calculate health metrics
                var statistics = GetChannelStatistics();
                var isOperational = _isHealthy && !_simulateFailure;
                var healthScore = CalculateHealthScore(statistics);

                if (isOperational && healthScore > 0.8)
                {
                    var message = $"Test channel is healthy (Score: {healthScore:F2}, Failures: {_consecutiveFailures})";
                    _loggingService.LogInfo($"Health check passed: {message}", correlationId, "TestAlertChannel");
                    return ChannelHealthResult.Healthy(message, duration);
                }
                else
                {
                    var message = $"Test channel is unhealthy (Score: {healthScore:F2}, Healthy: {_isHealthy}, Failures: {_consecutiveFailures})";
                    _loggingService.LogWarning($"Health check failed: {message}", correlationId, "TestAlertChannel");
                    return ChannelHealthResult.Unhealthy(message, null, duration);
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                var errorMessage = $"Health check failed with exception: {ex.Message}";
                _loggingService.LogException("TestAlertChannel health check exception", ex, correlationId, "TestAlertChannel");
                return ChannelHealthResult.Unhealthy(errorMessage, ex, duration);
            }
        }

        protected override async UniTask<bool> InitializeAsyncCore(ChannelConfig config, Guid correlationId)
        {
            try
            {
                _loggingService.LogInfo($"Initializing TestAlertChannel with config: {config?.Name}", correlationId, "TestAlertChannel");

                // Simulate initialization latency if configured
                if (_simulatedLatency > TimeSpan.Zero)
                {
                    await UniTask.Delay(_simulatedLatency);
                }

                await UniTask.CompletedTask;

                _loggingService.LogInfo("TestAlertChannel initialization completed successfully", correlationId, "TestAlertChannel");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogException("TestAlertChannel initialization failed", ex, correlationId, "TestAlertChannel");
                return false;
            }
        }

        protected override ChannelConfig CreateDefaultConfiguration()
        {
            return new ChannelConfig
            {
                Name = Name,
                ChannelType = AlertChannelType.Log,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Debug,
                MaximumSeverity = AlertSeverity.Emergency,
                MessageFormat = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Severity}] [{Source}] {Message}",
                EnableBatching = false,
                EnableHealthMonitoring = true,
                HealthCheckInterval = TimeSpan.FromMinutes(1),
                SendTimeout = TimeSpan.FromSeconds(1),
                Priority = 10
            };
        }

        public override void ResetStatistics(Guid correlationId = default)
        {
            lock (_lockObject)
            {
                _sentAlerts.Clear();
                _alertCalls.Clear();
                _correlationAlerts.Clear();
                _correlationTimestamps.Clear();
                _consecutiveFailures = 0;

                _loggingService.LogInfo("TestAlertChannel statistics reset", correlationId, "TestAlertChannel");
            }
        }

        #region Enhanced Statistics and Diagnostics

        /// <summary>
        /// Gets comprehensive channel statistics for performance analysis.
        /// Essential for CLAUDETESTS.md performance requirements and monitoring.
        /// </summary>
        /// <returns>Dictionary of channel statistics</returns>
        public Dictionary<string, object> GetChannelStatistics()
        {
            lock (_lockObject)
            {
                var stats = new Dictionary<string, object>
                {
                    ["TotalAlertsSent"] = _sentAlerts.Count,
                    ["TotalAlertCalls"] = _alertCalls.Count,
                    ["SuccessfulCalls"] = _alertCalls.Count(c => c.Success),
                    ["FailedCalls"] = _alertCalls.Count(c => !c.Success),
                    ["AsyncCalls"] = _alertCalls.Count(c => c.IsAsync),
                    ["SyncCalls"] = _alertCalls.Count(c => !c.IsAsync),
                    ["ConsecutiveFailures"] = _consecutiveFailures,
                    ["IsHealthy"] = _isHealthy,
                    ["SimulateFailure"] = _simulateFailure,
                    ["SimulatedLatency"] = _simulatedLatency.TotalMilliseconds,
                    ["ChannelUptime"] = (DateTime.UtcNow - _createdAt).TotalSeconds,
                    ["UniqueCorrelationIds"] = _correlationAlerts.Count,
                    ["AverageCallDuration"] = _alertCalls.Count > 0
                        ? _alertCalls.Average(c => c.Duration.TotalMilliseconds)
                        : 0.0,
                    ["MinCallDuration"] = _alertCalls.Count > 0
                        ? _alertCalls.Min(c => c.Duration.TotalMilliseconds)
                        : 0.0,
                    ["MaxCallDuration"] = _alertCalls.Count > 0
                        ? _alertCalls.Max(c => c.Duration.TotalMilliseconds)
                        : 0.0
                };

                // Severity breakdown
                foreach (AlertSeverity severity in Enum.GetValues(typeof(AlertSeverity)))
                {
                    stats[$"AlertCount_{severity}"] = _sentAlerts.Count(a => a.Severity == severity);
                }

                return stats;
            }
        }

        /// <summary>
        /// Validates channel data integrity for robust testing.
        /// Ensures all internal collections are consistent.
        /// </summary>
        /// <returns>True if all channel data is consistent</returns>
        public bool ValidateDataIntegrity()
        {
            try
            {
                lock (_lockObject)
                {
                    // Check that correlation alerts match timestamps
                    foreach (var kvp in _correlationAlerts)
                    {
                        if (!_correlationTimestamps.ContainsKey(kvp.Key))
                        {
                            _loggingService.LogError($"Integrity violation: Correlation ID {kvp.Key} missing timestamp");
                            return false;
                        }

                        if (kvp.Value.Count == 0)
                        {
                            _loggingService.LogError($"Integrity violation: Correlation ID {kvp.Key} has empty alert list");
                            return false;
                        }
                    }

                    // Check that all alert calls have valid data
                    foreach (var call in _alertCalls)
                    {
                        if (call.Alert == null)
                        {
                            _loggingService.LogError("Integrity violation: AlertCall has null Alert");
                            return false;
                        }

                        if (call.Timestamp == default)
                        {
                            _loggingService.LogError("Integrity violation: AlertCall has default timestamp");
                            return false;
                        }
                    }

                    _loggingService.LogInfo("TestAlertChannel data integrity validation passed");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Integrity validation failed with exception", ex);
                return false;
            }
        }

        /// <summary>
        /// Calculates a health score based on channel performance metrics.
        /// </summary>
        /// <param name="statistics">Channel statistics</param>
        /// <returns>Health score between 0.0 and 1.0</returns>
        private double CalculateHealthScore(Dictionary<string, object> statistics)
        {
            var score = 1.0;

            // Penalize for failures
            var totalCalls = (int)statistics["TotalAlertCalls"];
            var failedCalls = (int)statistics["FailedCalls"];
            if (totalCalls > 0)
            {
                var failureRate = (double)failedCalls / totalCalls;
                score -= failureRate * 0.5;
            }

            // Penalize for consecutive failures
            var consecutiveFailures = (int)statistics["ConsecutiveFailures"];
            if (consecutiveFailures > 0)
            {
                score -= Math.Min(consecutiveFailures * 0.1, 0.3);
            }

            // Penalize for excessive latency
            var avgDuration = (double)statistics["AverageCallDuration"];
            if (avgDuration > TestConstants.WarningThreshold.TotalMilliseconds)
            {
                score -= 0.2;
            }

            return Math.Max(0.0, score);
        }

        #endregion
    }

    /// <summary>
    /// Records details about an alert call for test verification.
    /// Essential for CLAUDETESTS.md compliance and interaction tracking.
    /// </summary>
    public sealed class AlertCall
    {
        public Alert Alert { get; set; }
        public Guid CorrelationId { get; set; }
        public bool IsAsync { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }
}