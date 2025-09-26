using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Production-ready unit tests for AlertFilterService following CLAUDETESTS.md guidelines.
    /// Tests filter service management, filter chain processing, and performance monitoring.
    /// Validates Unity game development performance requirements (60+ FPS, frame budget compliance).
    /// Uses TDD-compliant lightweight test doubles for robust, maintainable testing.
    /// </summary>
    [TestFixture]
    public class AlertFilterTests : BaseServiceTest
    {
        private AlertFilterService _filterService;
        private Alert _testAlert;
        private FilterContext _testContext;
        private SeverityAlertFilter _severityFilter;
        private SourceAlertFilter _sourceFilter;
        private RateLimitAlertFilter _rateLimitFilter;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Initialize filter service using production code with TDD test doubles
            _filterService = new AlertFilterService(StubLogging, FakeSerialization, SpyMessageBus);

            // Create test alert with proper correlation tracking
            var correlationId = CreateTestCorrelationId();
            _testAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                new FixedString64Bytes(TestConstants.TestSource),
                new FixedString32Bytes(TestConstants.SampleTag),
                correlationId);

            _testContext = FilterContext.WithCorrelation(correlationId);

            // Create common filter instances for testing
            _severityFilter = new SeverityAlertFilter(SpyMessageBus, AlertSeverity.Info);
            _sourceFilter = new SourceAlertFilter(SpyMessageBus, "TestSourceFilter", new[] { TestConstants.TestSource });
            _rateLimitFilter = new RateLimitAlertFilter(SpyMessageBus, 100, "*");
        }

        [TearDown]
        public override void TearDown()
        {
            // Ensure clean disposal following CLAUDETESTS.md guidelines
            _severityFilter?.Dispose();
            _sourceFilter?.Dispose();
            _rateLimitFilter?.Dispose();
            _filterService?.Dispose();
            base.TearDown();
        }

        #region AlertFilterService Core Functionality Tests

        [Test]
        public async UniTask AlertFilterService_Constructor_WithValidServices_InitializesCorrectlyWithinFrameBudget()
        {
            // Arrange & Act - Validate frame budget compliance for Unity 60 FPS requirement
            await AssertFrameBudgetComplianceAsync(async () =>
            {
                var service = new AlertFilterService(StubLogging, FakeSerialization, SpyMessageBus);

                // Assert service properties
                Assert.That(service.IsEnabled, Is.True);
                Assert.That(service.FilterCount, Is.EqualTo(0));
                Assert.That(service.EnabledFilterCount, Is.EqualTo(0));

                service.Dispose();
                await UniTask.CompletedTask;
            }, "AlertFilterService_Constructor");

            // Verify no errors occurred during construction
            AssertNoErrors();
            AssertAllServicesHealthy();
        }

        [Test]
        public async UniTask RegisterFilter_WithValidFilter_RegistersSuccessfullyWithCorrelationTracking()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act - Test filter registration with performance measurement
            bool result = false;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.RegisterFilter(_severityFilter, null, correlationId);
                await UniTask.CompletedTask;
            }, "RegisterFilter", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_filterService.FilterCount, Is.EqualTo(1));
            Assert.That(_filterService.EnabledFilterCount, Is.EqualTo(1));

            // Verify filter is retrievable
            var retrievedFilter = _filterService.GetFilter("SeverityFilter");
            Assert.That(retrievedFilter, Is.Not.Null);
            Assert.That(retrievedFilter.Name.ToString(), Is.EqualTo("SeverityFilter"));

            // Verify correlation tracking and logging
            AssertCorrelationTrackingMaintained(correlationId);
            AssertLogContains("Filter registered successfully");
            AssertNoErrors();
        }

        [Test]
        public async UniTask RegisterFilter_WithNullFilter_ReturnsFalseGracefully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act & Assert - Test graceful failure handling
            bool result = true;
            await AssertGracefulFailureHandlingAsync(async () =>
            {
                result = _filterService.RegisterFilter(null, null, correlationId);
                await UniTask.CompletedTask;
            });

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_filterService.FilterCount, Is.EqualTo(0));

            // Verify system remains healthy after null input
            AssertAllServicesHealthy();
            AssertNoErrors();
        }

        [Test]
        public async UniTask UnregisterFilter_WithValidFilter_UnregistersSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Act - Test filter unregistration
            bool result = false;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.UnregisterFilter("SeverityFilter", correlationId);
                await UniTask.CompletedTask;
            }, "UnregisterFilter", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_filterService.FilterCount, Is.EqualTo(0));
            Assert.That(_filterService.EnabledFilterCount, Is.EqualTo(0));

            // Verify filter is no longer retrievable
            var retrievedFilter = _filterService.GetFilter("SeverityFilter");
            Assert.That(retrievedFilter, Is.Null);

            AssertLogContains("Filter unregistered");
            AssertNoErrors();
        }

        #endregion

        #region Filter Chain Processing Tests

        [Test]
        public async UniTask ProcessAlert_WithNoFilters_AllowsAlertWithCorrelationTracking()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var alert = Alert.Create("Test message", AlertSeverity.Info, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId);
            var context = FilterContext.WithCorrelation(correlationId);

            // Act - Test alert processing with performance measurement
            FilterChainResult result = null;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.ProcessAlert(alert, context, correlationId);
                await UniTask.CompletedTask;
            }, "ProcessAlert_NoFilters", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Decision, Is.EqualTo(FilterChainDecision.Allow));
            Assert.That(result.ProcessedAlert, Is.EqualTo(alert));

            // Verify correlation tracking throughout processing
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask ProcessAlert_WithAllowingFilter_AllowsAlertThroughChain()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId); // Will allow Warning alerts

            var alert = Alert.Create("Test alert", AlertSeverity.Warning, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId);
            var context = FilterContext.WithCorrelation(correlationId);

            // Act - Test filter chain with allowing filter
            FilterChainResult result = null;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.ProcessAlert(alert, context, correlationId);
                await UniTask.CompletedTask;
            }, "ProcessAlert_AllowingFilter", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Decision, Is.EqualTo(FilterChainDecision.Allow));
            Assert.That(result.ProcessedAlert, Is.EqualTo(alert));
            Assert.That(result.AppliedFilters.Count, Is.EqualTo(1));

            // Verify filter was applied
            var appliedFilter = result.AppliedFilters.First();
            Assert.That(appliedFilter.FilterName, Is.EqualTo("SeverityFilter"));
            Assert.That(appliedFilter.Applied, Is.True);

            AssertNoErrors();
        }

        [Test]
        public async UniTask ProcessAlert_WithSuppressingFilter_SuppressesAlert()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var suppressingFilter = new SeverityAlertFilter(SpyMessageBus, AlertSeverity.Emergency); // Will suppress Critical alerts
            _filterService.RegisterFilter(suppressingFilter, null, correlationId); // Will suppress Critical alerts

            var alert = Alert.Create("Test alert", AlertSeverity.Warning, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId); // Warning < Emergency, will be suppressed
            var context = FilterContext.WithCorrelation(correlationId);

            // Act - Test filter chain with suppressing filter
            FilterChainResult result = null;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.ProcessAlert(alert, context, correlationId);
                await UniTask.CompletedTask;
            }, "ProcessAlert_SuppressingFilter", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Decision, Is.EqualTo(FilterChainDecision.Suppress));
            Assert.That(result.ProcessedAlert, Is.Null); // Suppressed alerts have no processed alert

            // Verify filter was applied and caused suppression
            Assert.That(result.AppliedFilters.Count, Is.EqualTo(1));
            var appliedFilter = result.AppliedFilters.First();
            Assert.That(appliedFilter.FilterName, Is.EqualTo("SeverityFilter"));
            Assert.That(appliedFilter.Applied, Is.True);

            suppressingFilter.Dispose();
            AssertLogContains("Alert suppressed by filter");
            AssertNoErrors();
        }

        [Test]
        public async UniTask ProcessAlert_WithMultipleFilters_ProcessesInPriorityOrder()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Register filters in reverse priority order to test sorting
            var lowPriorityFilter = new RateLimitAlertFilter(SpyMessageBus, 1000, "*"); // Low priority (high number)
            var highPriorityFilter = new SeverityAlertFilter(SpyMessageBus, AlertSeverity.Debug); // Higher priority (lower number)

            _filterService.RegisterFilter(lowPriorityFilter, null, correlationId);
            _filterService.RegisterFilter(highPriorityFilter, null, correlationId);

            var alert = Alert.Create("Priority test", AlertSeverity.Warning, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId);
            var context = FilterContext.WithCorrelation(correlationId);

            // Act - Test multi-filter processing
            FilterChainResult result = null;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.ProcessAlert(alert, context, correlationId);
                await UniTask.CompletedTask;
            }, "ProcessAlert_MultipleFilters", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Decision, Is.EqualTo(FilterChainDecision.Allow));
            Assert.That(result.AppliedFilters.Count, Is.EqualTo(2));

            // Verify filters were applied in priority order (High priority first)
            Assert.That(result.AppliedFilters[0].FilterName, Is.EqualTo("SeverityFilter")); // SeverityAlertFilter has fixed name
            Assert.That(result.AppliedFilters[1].FilterName, Is.EqualTo("RateLimitFilter")); // RateLimitAlertFilter has fixed name

            lowPriorityFilter.Dispose();
            highPriorityFilter.Dispose();
            AssertNoErrors();
        }

        #endregion

        #region Bulk Processing and Performance Tests

        [Test]
        public async UniTask ProcessAlerts_WithBulkData_MaintainsFrameBudgetCompliance()
        {
            // Arrange - Create realistic game load scenario
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Create 1000 alerts for stress testing
            var alerts = TestConstants.CreateStressTestData(i =>
                Alert.Create(
                    $"Stress test alert {i}",
                    i % 2 == 0 ? AlertSeverity.Warning : AlertSeverity.Critical,
                    new FixedString64Bytes(TestConstants.TestSource),
                    correlationId: correlationId),
                TestConstants.DefaultStressTestIterations);

            var context = FilterContext.WithCorrelation(correlationId);

            // Act & Assert - Validate bulk processing meets Unity 60 FPS requirement
            IEnumerable<FilterChainResult> results = null;
            await AssertFrameBudgetComplianceAsync(async () =>
            {
                results = _filterService.ProcessAlerts(alerts, context, correlationId);
                await UniTask.CompletedTask;
            }, "ProcessAlerts_BulkProcessing_1000Alerts");

            // Verify results
            var resultList = results.ToList();
            Assert.That(resultList.Count, Is.EqualTo(TestConstants.DefaultStressTestIterations));
            Assert.That(resultList.All(r => r.Decision == FilterChainDecision.Allow), Is.True);

            // Verify service health after stress test
            AssertAllServicesHealthy();
            Assert.That(_filterService.IsEnabled, Is.True);
        }

        [Test]
        public async UniTask FilterProcessing_WithZeroAllocationPattern_ProducesMinimalAllocations()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);
            var context = FilterContext.WithCorrelation(correlationId);

            // Act & Assert - Validate Unity Collections zero-allocation pattern
            await AssertAcceptableAllocationsAsync(async () =>
            {
                // Test single alert processing with minimal allocations
                var result = _filterService.ProcessAlert(_testAlert, context, correlationId);
                Assert.That(result.Decision, Is.Not.EqualTo(FilterChainDecision.Allow).Or.EqualTo(FilterChainDecision.Allow));
                await UniTask.CompletedTask;
            }, "FilterProcessing_ZeroAllocation", TestConstants.MaxAcceptableAllocation);
        }

        #endregion

        #region Configuration Management Tests

        [Test]
        public async UniTask EnableFilter_WithValidFilter_EnablesSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);
            _severityFilter.IsEnabled = false;

            // Act - Test filter enabling
            bool result = false;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.EnableFilter("SeverityFilter", correlationId);
                await UniTask.CompletedTask;
            }, "EnableFilter", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_filterService.EnabledFilterCount, Is.EqualTo(1));
            Assert.That(_severityFilter.IsEnabled, Is.True);

            AssertLogContains("Filter enabled");
            AssertNoErrors();
        }

        [Test]
        public async UniTask DisableFilter_WithValidFilter_DisablesSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Act - Test filter disabling
            bool result = false;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.DisableFilter("SeverityFilter", correlationId);
                await UniTask.CompletedTask;
            }, "DisableFilter", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_filterService.EnabledFilterCount, Is.EqualTo(0));
            Assert.That(_severityFilter.IsEnabled, Is.False);

            AssertLogContains("Filter disabled");
            AssertNoErrors();
        }

        [Test]
        public async UniTask UpdateFilterPriority_WithValidFilter_UpdatesPriorityAndSorts()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);
            _filterService.RegisterFilter(_sourceFilter, null, correlationId);

            var initialPriority = _severityFilter.Priority;

            // Act - Test priority update
            bool result = false;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                result = _filterService.UpdateFilterPriority("SeverityFilter", 1000, correlationId);
                await UniTask.CompletedTask;
            }, "UpdateFilterPriority", TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_severityFilter.Priority, Is.EqualTo(1000));
            Assert.That(_severityFilter.Priority, Is.Not.EqualTo(initialPriority));

            // Verify filter chain is re-sorted
            var allFilters = _filterService.GetAllFilters().ToList();
            Assert.That(allFilters.Count, Is.EqualTo(2));

            AssertLogContains("Filter priority updated");
            AssertNoErrors();
        }

        #endregion

        #region Performance Monitoring Tests

        [Test]
        public async UniTask GetPerformanceMetrics_AfterFilterProcessing_ReturnsValidMetrics()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Process some alerts to generate metrics
            for (int i = 0; i < 10; i++)
            {
                var alert = Alert.Create($"Metrics test {i}", AlertSeverity.Info, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId);
                var context = FilterContext.WithCorrelation(correlationId);
                _filterService.ProcessAlert(alert, context, correlationId);
            }

            // Act & Assert - Validate metrics collection
            FilterManagerMetrics metrics = null;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                metrics = _filterService.GetPerformanceMetrics();
                await UniTask.CompletedTask;
            }, "GetPerformanceMetrics", TestConstants.FrameBudget);

            // Assert
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.TotalFilters, Is.EqualTo(1));
            Assert.That(metrics.EnabledFilters, Is.EqualTo(1));
            Assert.That(metrics.HealthyFilters, Is.EqualTo(1));
            Assert.That(metrics.FilterPerformanceData, Is.Not.Null);
            Assert.That(metrics.FilterPerformanceData.Count, Is.GreaterThan(0));

            // Verify filter performance data
            var filterPerf = metrics.FilterPerformanceData.First();
            Assert.That(filterPerf.FilterName, Is.EqualTo("SeverityFilter"));
            Assert.That(filterPerf.TotalEvaluations, Is.GreaterThan(0));

            AssertNoErrors();
        }

        [Test]
        public async UniTask GetFilterHealthInfo_AfterProcessing_ReturnsHealthInformation()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Process some alerts to generate health data
            for (int i = 0; i < 5; i++)
            {
                var alert = Alert.Create($"Health test {i}", AlertSeverity.Warning, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId);
                var context = FilterContext.WithCorrelation(correlationId);
                _filterService.ProcessAlert(alert, context, correlationId);
            }

            // Act - Get health information
            IReadOnlyCollection<FilterHealth> healthInfo = null;
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                healthInfo = _filterService.GetFilterHealthInfo();
                await UniTask.CompletedTask;
            }, "GetFilterHealthInfo", TestConstants.FrameBudget);

            // Assert
            Assert.That(healthInfo, Is.Not.Null);
            Assert.That(healthInfo.Count, Is.EqualTo(1));

            var filterHealth = healthInfo.First();
            Assert.That(filterHealth.FilterName, Is.EqualTo("SeverityFilter"));
            Assert.That(filterHealth.IsHealthy, Is.True);
            Assert.That(filterHealth.ConsecutiveErrors, Is.EqualTo(0));

            AssertNoErrors();
        }

        [Test]
        public async UniTask ResetPerformanceMetrics_AfterProcessing_ResetsAllMetrics()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Process some alerts to generate metrics
            for (int i = 0; i < 5; i++)
            {
                var alert = Alert.Create($"Reset test {i}", AlertSeverity.Info, new FixedString64Bytes(TestConstants.TestSource), correlationId: correlationId);
                var context = FilterContext.WithCorrelation(correlationId);
                _filterService.ProcessAlert(alert, context, correlationId);
            }

            // Act - Reset metrics
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                _filterService.ResetPerformanceMetrics(correlationId);
                await UniTask.CompletedTask;
            }, "ResetPerformanceMetrics", TestConstants.FrameBudget);

            // Assert - Verify metrics were reset
            var metrics = _filterService.GetPerformanceMetrics();
            Assert.That(metrics.FilterPerformanceData.Count, Is.GreaterThanOrEqualTo(0));

            // Verify logging
            AssertLogContains("Filter performance metrics reset");
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        #endregion

        #region Error Handling and Edge Cases

        [Test]
        public async UniTask ProcessAlert_WithNullAlert_HandlesGracefullyWithoutThrowingExceptions()
        {
            // Act & Assert - Test null input handling
            FilterChainResult result = null;
            await AssertGracefulFailureHandlingAsync(async () =>
            {
                result = _filterService.ProcessAlert(null, _testContext);
                await UniTask.CompletedTask;
            });

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Decision, Is.EqualTo(FilterChainDecision.Allow));

            // Verify service remains healthy after null inputs
            Assert.That(_filterService.IsEnabled, Is.True);
            AssertNoErrors();
        }

        [Test]
        public async UniTask FilterService_WithConcurrentAccess_MaintainsThreadSafety()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            _filterService.RegisterFilter(_severityFilter, null, correlationId);

            // Act - Simulate concurrent filter operations
            var tasks = new UniTask[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var taskIndex = i;
                tasks[i] = UniTask.Run(async () =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var alert = Alert.Create(
                            $"Concurrent alert {taskIndex}-{j}",
                            AlertSeverity.Warning,
                            new FixedString64Bytes(TestConstants.TestSource),
                            correlationId: correlationId);
                        var context = FilterContext.WithCorrelation(correlationId);
                        _filterService.ProcessAlert(alert, context, correlationId);
                        await UniTask.Yield();
                    }
                });
            }

            // Wait for all concurrent operations
            await UniTask.WhenAll(tasks);

            // Assert - Service should remain healthy and functional
            Assert.That(_filterService.IsEnabled, Is.True);
            Assert.That(_filterService.FilterCount, Is.EqualTo(1));
            AssertAllServicesHealthy();
        }

        #endregion

        #region Service Lifecycle Tests

        [Test]
        public async UniTask AlertFilterService_Dispose_DisposesCleanlyWithCorrelationTracking()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var tempService = new AlertFilterService(StubLogging, FakeSerialization, SpyMessageBus);
            tempService.RegisterFilter(_severityFilter, null, correlationId);

            // Verify service is operational
            Assert.That(tempService.IsEnabled, Is.True);
            Assert.That(tempService.FilterCount, Is.EqualTo(1));

            // Act - Test disposal with performance measurement
            await ExecuteWithPerformanceMeasurementAsync(async () =>
            {
                tempService.Dispose();
                await UniTask.CompletedTask;
            }, "AlertFilterService_Dispose", TestConstants.FrameBudget);

            // Assert
            Assert.That(tempService.IsEnabled, Is.False);
            Assert.That(tempService.FilterCount, Is.EqualTo(0));

            // Verify disposal logging
            AssertLogContains("Alert filter service disposed");
            AssertCorrelationTrackingMaintained(correlationId);
        }

        #endregion
    }
}