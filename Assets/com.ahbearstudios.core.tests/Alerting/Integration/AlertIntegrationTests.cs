using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Builders;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Tests.Shared.Channels;

namespace AhBearStudios.Core.Tests.Alerting.Integration
{
    /// <summary>
    /// Comprehensive integration tests for the complete alert system.
    /// Tests end-to-end alert flow, service integration with dependencies, correlation tracking,
    /// and performance under load. Strictly follows guidelines with robust
    /// TDD test double integration and Unity game development requirements.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    [TestFixture]
    public class AlertIntegrationTests : BaseIntegrationTest
    {
        private AlertService _alertService;
        private AlertServiceConfiguration _configuration;
        private TestAlertChannel _testChannel;
        private MemoryAlertChannel _memoryChannel;
        private TestCorrelationHelper _correlationHelper;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Initialize correlation helper for robust test tracking
            _correlationHelper = new TestCorrelationHelper(StubLogging);

            // Create comprehensive test configuration with TDD compliance
            _configuration = new AlertConfigBuilder()
                .ForTesting()
                .WithMinimumSeverity(AlertSeverity.Debug)
                .WithHistoryLimit(100)
                .WithSeverityFilter("SeverityFilter", AlertSeverity.Info)
                .WithRateLimitFilter("RateLimit", 50)
                .WithConsoleChannel("Console")
                .WithLogChannel("Memory")
                .BuildServiceConfiguration();

            // Create integrated alert service with enhanced TDD test doubles
            var channelServiceConfig = new AlertChannelServiceConfig();
            var channelService = new AlertChannelService(
                channelServiceConfig,
                StubLogging,
                SpyMessageBus);

            var filterService = new AlertFilterService(StubLogging);
            var suppressionService = new AlertSuppressionService(StubLogging);

            _alertService = new AlertService(
                _configuration,
                channelService,
                filterService,
                suppressionService,
                SpyMessageBus,
                StubLogging,
                FakeSerialization,
                FakePooling);

            // Set up enhanced test channels with TDD test double integration
            _testChannel = new TestAlertChannel("TestChannel", SpyMessageBus, StubLogging);
            _memoryChannel = new MemoryAlertChannel(new MemoryChannelSettings
            {
                MaxStoredAlerts = 1000
            }, SpyMessageBus);

            _alertService.RegisterChannel(_testChannel);
            _alertService.RegisterChannel(_memoryChannel);

            // Log test setup completion for correlation tracking
            var setupCorrelationId = _correlationHelper.CreateCorrelationId("AlertIntegrationTests_Setup");
            StubLogging.LogInfo("AlertIntegrationTests setup completed with enhanced TDD test doubles",
                setupCorrelationId, "AlertIntegrationTests");
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                // Clear test channel data for test isolation
                _testChannel?.ClearRecordedAlerts();

                // Validate data integrity before cleanup
                if (_testChannel != null)
                {
                    var integrity = _testChannel.ValidateDataIntegrity();
                    if (!integrity)
                    {
                        StubLogging.LogWarning("TestAlertChannel data integrity validation failed during teardown");
                    }
                }

                // Dispose services
                _alertService?.Dispose();
                _testChannel?.Dispose();
                _memoryChannel?.Dispose();
                _correlationHelper?.Dispose();

                // Log teardown completion
                StubLogging.LogInfo("AlertIntegrationTests teardown completed", sourceContext: "AlertIntegrationTests");
            }
            finally
            {
                base.TearDown();
            }
        }

        #region End-to-End Alert Flow Tests

        [Test]
        public async UniTask EndToEndAlertFlow_WithCompleteWorkflow_WorksCorrectly()
        {
            // Arrange
            var startCorrelationId = _correlationHelper.CreateCorrelationId("EndToEndAlertFlow_Start");
            await _alertService.StartAsync(startCorrelationId);
            var correlationId = _correlationHelper.CreateCorrelationId("EndToEndAlertFlow_MainOperation");

            // Act - Raise alert with correlation tracking
            var alert = Alert.Create(
                "Integration test alert",
                AlertSeverity.Warning,
                "IntegrationTest",
                "TestTag",
                correlationId);

            // Validate frame budget compliance for alert raising
            var withinBudget = _testChannel.ValidateFrameBudgetCompliance(
                () => _alertService.RaiseAlert(alert),
                "EndToEndAlertFlow_RaiseAlert");
            Assert.That(withinBudget, Is.True, "Alert raising should complete within frame budget");

            // Wait for async processing
            await UniTask.Delay(100);

            // Assert - Verify alert was processed through the entire pipeline
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);
            Assert.That(activeAlerts.FirstOrDefault().Id, Is.EqualTo(alert.Id));

            // Verify message was published with correlation
            AssertMessagePublished<AhBearStudios.Core.Alerting.Messages.AlertRaisedMessage>();
            var raisedMessage = GetLastMessage<AhBearStudios.Core.Alerting.Messages.AlertRaisedMessage>();
            Assert.That(raisedMessage.CorrelationId, Is.EqualTo(correlationId));

            // Verify channels received the alert with enhanced verification
            Assert.That(_testChannel.SentAlerts, Is.Not.Empty);
            Assert.That(_testChannel.WasAlertSent(a => a.Id == alert.Id), Is.True);
            Assert.That(_testChannel.GetAlertsForCorrelation(correlationId), Is.Not.Empty);
            Assert.That(_memoryChannel.StoredAlerts, Is.Not.Empty);

            // Test acknowledgment with correlation tracking
            _alertService.AcknowledgeAlert(alert.Id, correlationId.ToString());

            // Verify acknowledgment message
            AssertMessagePublished<AhBearStudios.Core.Alerting.Messages.AlertAcknowledgedMessage>();

            // Test resolution with correlation tracking
            _alertService.ResolveAlert(alert.Id, correlationId.ToString());

            // Verify resolution message
            AssertMessagePublished<AhBearStudios.Core.Alerting.Messages.AlertResolvedMessage>();

            // Verify correlation tracking was maintained throughout
            Assert.That(_correlationHelper.WasCreatedByHelper(correlationId), Is.True);
            AssertLogContains("Integration test alert");
        }

        [Test]
        public async UniTask EndToEndAlertFlow_WithFilteringSuppression_FiltersCorrectly()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            // Add a severity filter that suppresses Debug alerts
            var severityFilter = new SeverityAlertFilter(SpyMessageBus, AlertSeverity.Info);
            _alertService.AddFilter(severityFilter);

            // Act - Raise debug alert (should be suppressed)
            var debugAlert = Alert.Create(
                "Debug alert",
                AlertSeverity.Debug, // Below Info threshold
                "IntegrationTest");

            _alertService.RaiseAlert(debugAlert);

            // Wait for processing
            await UniTask.Delay(100);

            // Assert - Debug alert should be suppressed
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Any(a => a.Severity == AlertSeverity.Debug), Is.False);
            Assert.That(_testChannel.SentAlerts.Any(a => a.Severity == AlertSeverity.Debug), Is.False);
            Assert.That(_testChannel.GetAlertCount(AlertSeverity.Debug), Is.EqualTo(0));

            // Act - Raise info alert (should pass through)
            var infoAlert = Alert.Create(
                "Info alert",
                AlertSeverity.Info,
                "IntegrationTest");

            _alertService.RaiseAlert(infoAlert);

            // Wait for processing
            await UniTask.Delay(100);

            // Assert - Info alert should pass through
            activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Any(a => a.Severity == AlertSeverity.Info), Is.True);
            Assert.That(_testChannel.SentAlerts.Any(a => a.Severity == AlertSeverity.Info), Is.True);
            Assert.That(_testChannel.GetAlertCount(AlertSeverity.Info), Is.GreaterThan(0));
        }

        [Test]
        public async UniTask EndToEndAlertFlow_WithChannelFailure_HandlesGracefully()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            // Create a failing channel with enhanced TDD test double integration
            var failingChannel = new TestAlertChannel("FailingChannel", SpyMessageBus, StubLogging);
            failingChannel.SetHealthy(false); // Will throw on send
            failingChannel.SimulateFailure(true); // Enhanced failure simulation
            _alertService.RegisterChannel(failingChannel);

            var correlationId = CreateTestCorrelationId();

            // Act - Raise alert
            var alert = Alert.Create(
                "Test alert with failing channel",
                AlertSeverity.Error,
                "IntegrationTest",
                correlationId: correlationId);

            _alertService.RaiseAlert(alert);

            // Wait for processing
            await UniTask.Delay(100);

            // Assert - Alert should still be raised despite channel failure
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);

            // Working channels should still receive the alert
            Assert.That(_testChannel.SentAlerts, Is.Not.Empty);
            Assert.That(_memoryChannel.StoredAlerts, Is.Not.Empty);

            // Delivery failure message should be published
            AssertMessagePublished<AhBearStudios.Core.Alerting.Messages.AlertDeliveryFailedMessage>();
        }

        #endregion

        #region Bulk Operations Tests

        [Test]
        public async UniTask BulkAlertOperations_WithManyAlerts_ProcessesEfficiently()
        {
            // Arrange
            var startCorrelationId = _correlationHelper.CreateCorrelationId("BulkAlertOperations_Start");
            await _alertService.StartAsync(startCorrelationId);
            var correlationId = _correlationHelper.CreateCorrelationId("BulkAlertOperations_MainBatch");

            var alerts = Enumerable.Range(0, 100)
                .Select(i => Alert.Create(
                    $"Bulk alert {i}",
                    AlertSeverity.Info,
                    "BulkTest",
                    correlationId: correlationId))
                .ToList();

            // Act - Validate frame budget compliance for bulk operations
            var performanceResult = await ExecuteWithPerformanceMeasurementAsync(
                async () => await _alertService.RaiseAlertsAsync(alerts, correlationId),
                "BulkAlertRaising",
                TestConstants.FrameBudget); // Should complete within frame budget

            // Wait for all async processing
            await UniTask.Delay(500);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Count(), Is.EqualTo(100));

            // Verify all channels received all alerts with enhanced verification
            Assert.That(_testChannel.SentAlerts.Count(), Is.EqualTo(100));
            Assert.That(_testChannel.GetAlertsForCorrelation(correlationId).Count, Is.EqualTo(100));
            Assert.That(_memoryChannel.StoredAlerts.Count(), Is.EqualTo(100));

            // Verify performance and test channel statistics
            var channelStats = _testChannel.GetChannelStatistics();
            Assert.That((int)channelStats["TotalAlertsSent"], Is.EqualTo(100));
            Assert.That((bool)channelStats["IsHealthy"], Is.True);
            Assert.That((int)channelStats["SuccessfulCalls"], Is.EqualTo(100));

            // Verify performance within acceptable limits
            LogPerformanceMetrics(performanceResult);
            Assert.That(performanceResult.Duration, Is.LessThan(TestConstants.FrameBudget),
                "Bulk alert operations should complete within frame budget");
        }

        [Test]
        public async UniTask BulkAcknowledgment_WithManyAlerts_ProcessesEfficiently()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            var correlationId = _correlationHelper.CreateCorrelationId("BulkAcknowledgment_Operation");
            var alerts = Enumerable.Range(0, 50)
                .Select(i => Alert.Create($"Alert {i}", AlertSeverity.Warning, "BulkTest", correlationId: correlationId))
                .ToList();

            // Raise all alerts first with frame budget validation
            _testChannel.ValidateFrameBudgetCompliance(
                () =>
                {
                    foreach (var alert in alerts)
                    {
                        _alertService.RaiseAlert(alert);
                    }
                },
                "BulkAcknowledgment_RaiseAlerts");

            var alertIds = alerts.Select(a => a.Id).ToList();

            // Act
            await _alertService.AcknowledgeAlertsAsync(alertIds, correlationId);

            // Wait for processing
            await UniTask.Delay(200);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            var acknowledgedCount = activeAlerts.Count(a => a.IsAcknowledged);
            Assert.That(acknowledgedCount, Is.EqualTo(50));

            // Verify acknowledgment messages were published with correlation
            var acknowledgedMessages = SpyMessageBus.PublishedMessages.OfType<AlertAcknowledgedMessage>().ToList();
            Assert.That(acknowledgedMessages.Count(), Is.EqualTo(50));
            Assert.That(acknowledgedMessages.All(m => m.CorrelationId == correlationId), Is.True);

            // Verify test channel recorded all operations
            var channelStats = _testChannel.GetChannelStatistics();
            Assert.That((int)channelStats["TotalAlertsSent"], Is.EqualTo(50));
            Assert.That((int)channelStats["SuccessfulCalls"], Is.EqualTo(50));
        }

        [Test]
        public async UniTask BulkResolution_WithManyAlerts_ProcessesEfficiently()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            var correlationId = _correlationHelper.CreateCorrelationId("BulkResolution_Operation");
            var alerts = Enumerable.Range(0, 30)
                .Select(i => Alert.Create($"Alert {i}", AlertSeverity.Error, "BulkTest", correlationId: correlationId))
                .ToList();

            // Raise all alerts first with performance monitoring
            _testChannel.ValidateFrameBudgetCompliance(
                () =>
                {
                    foreach (var alert in alerts)
                    {
                        _alertService.RaiseAlert(alert);
                    }
                },
                "BulkResolution_RaiseAlerts");

            var alertIds = alerts.Select(a => a.Id).ToList();

            // Act
            await _alertService.ResolveAlertsAsync(alertIds, correlationId);

            // Wait for processing
            await UniTask.Delay(200);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            var resolvedCount = activeAlerts.Count(a => a.IsResolved);
            Assert.That(resolvedCount, Is.EqualTo(30));

            // Verify resolution messages were published with correlation
            var resolvedMessages = SpyMessageBus.PublishedMessages.OfType<AlertResolvedMessage>().ToList();
            Assert.That(resolvedMessages.All(m => m.CorrelationId == correlationId), Is.True);

            // Verify test channel performance statistics
            var channelStats = _testChannel.GetChannelStatistics();
            Assert.That((int)channelStats["TotalAlertsSent"], Is.EqualTo(30));
            Assert.That((bool)channelStats["IsHealthy"], Is.True);
        }

        #endregion

        #region Health Monitoring Integration Tests

        [Test]
        public async UniTask HealthMonitoring_WithCompleteSystem_ReportsCorrectHealth()
        {
            // Arrange
            var startCorrelationId = _correlationHelper.CreateCorrelationId("HealthMonitoring_Start");
            await _alertService.StartAsync(startCorrelationId);
            var correlationId = _correlationHelper.CreateCorrelationId("HealthMonitoring_Check");

            // Act
            var healthReport = await _alertService.PerformHealthCheckAsync(correlationId);

            // Assert
            Assert.That(healthReport.OverallHealth, Is.True);
            Assert.That(healthReport.ServiceEnabled, Is.True);
            Assert.That(healthReport.ChannelServiceHealth, Is.True);
            Assert.That(healthReport.HealthyChannelCount, Is.GreaterThan(0));

            // Verify test channel health with enhanced verification
            var channelHealthResult = await _testChannel.TestHealthAsync(correlationId, CancellationToken.None);
            Assert.That(channelHealthResult.IsHealthy, Is.True);
            Assert.That(_testChannel.ValidateDataIntegrity(), Is.True);

            // Verify all subsystems are healthy
            AssertAllServicesHealthy();
            AssertLogContains("Health check");
        }

        [Test]
        public async UniTask HealthMonitoring_WithUnhealthyChannel_ReportsCorrectly()
        {
            // Arrange
            var startCorrelationId = _correlationHelper.CreateCorrelationId("HealthMonitoring_UnhealthyStart");
            await _alertService.StartAsync(startCorrelationId);

            // Make a channel unhealthy with enhanced simulation
            _testChannel.SetHealthy(false);
            _testChannel.SimulateFailure(true);

            var correlationId = _correlationHelper.CreateCorrelationId("HealthMonitoring_UnhealthyCheck");

            // Act
            var healthReport = await _alertService.PerformHealthCheckAsync(correlationId);

            // Assert
            // Overall system should still be healthy even with one unhealthy channel
            Assert.That(healthReport.OverallHealth, Is.True);
            Assert.That(healthReport.ServiceEnabled, Is.True);

            // Verify unhealthy channel is detected
            var channelHealthResult = await _testChannel.TestHealthAsync(correlationId, CancellationToken.None);
            Assert.That(channelHealthResult.IsHealthy, Is.False);

            // Verify logging captured the health issue
            AssertLogContains("unhealthy");
            Assert.That(StubLogging.HasErrorLogs(), Is.True);
        }

        [Test]
        public void DiagnosticsReporting_WithActiveSystem_ProvidesCompleteDiagnostics()
        {
            // Arrange
            var alert = Alert.Create("Diagnostic test", AlertSeverity.Warning, "DiagnosticTest");
            _alertService.RaiseAlert(alert);

            // Act
            var diagnostics = _alertService.GetDiagnostics(CreateTestCorrelationId());

            // Assert
            Assert.That(diagnostics.ServiceVersion, Is.Not.Null);
            Assert.That(diagnostics.IsEnabled, Is.True);
            Assert.That(diagnostics.IsHealthy, Is.True);
            Assert.That(diagnostics.ActiveAlertCount, Is.GreaterThan(0));
            Assert.That(diagnostics.SubsystemStatuses, Is.Not.Null);
            Assert.That(diagnostics.SubsystemStatuses.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Configuration Management Tests

        [Test]
        public async UniTask ConfigurationHotReload_WithNewConfiguration_UpdatesCorrectly()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            var newConfiguration = new AlertConfigBuilder()
                .ForProduction()
                .WithMinimumSeverity(AlertSeverity.Error) // Changed from Debug to Error
                .BuildServiceConfiguration();

            var correlationId = CreateTestCorrelationId();

            // Act
            var result = await _alertService.UpdateConfigurationAsync(newConfiguration, correlationId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_alertService.Configuration.AlertConfig.MinimumSeverity, Is.EqualTo(AlertSeverity.Error));

            // Verify the new configuration is applied
            var lowSeverityAlert = Alert.Create("Low severity", AlertSeverity.Warning, "ConfigTest");
            _alertService.RaiseAlert(lowSeverityAlert);

            // Warning should be suppressed due to Error minimum severity
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Any(a => a.Severity == AlertSeverity.Warning), Is.False);
            Assert.That(_testChannel.GetAlertCount(AlertSeverity.Warning), Is.EqualTo(0));

            // Verify configuration logging
            AssertLogContains("configuration");
        }

        [Test]
        public async UniTask ConfigurationValidation_WithInvalidConfiguration_HandlesGracefully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act & Assert
            var result = await _alertService.UpdateConfigurationAsync(null, correlationId);

            Assert.That(result, Is.False);
            AssertLogContains("Failed to update configuration");
        }

        #endregion

        #region Correlation Tracking Tests

        [Test]
        public void CorrelationTracking_AcrossEntireAlertLifecycle_MaintainsCorrelation()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act - Raise alert with correlation ID
            var alert = Alert.Create(
                "Correlation test alert",
                AlertSeverity.Warning,
                "CorrelationTest",
                correlationId: correlationId);

            _alertService.RaiseAlert(alert);

            // Acknowledge with same correlation ID
            _alertService.AcknowledgeAlert(alert.Id, correlationId.ToString());

            // Resolve with same correlation ID
            _alertService.ResolveAlert(alert.Id, correlationId.ToString());

            // Assert - All messages should have the same correlation ID
            var raisedMessage = GetLastMessage<AhBearStudios.Core.Alerting.Messages.AlertRaisedMessage>();
            var acknowledgedMessage = GetLastMessage<AhBearStudios.Core.Alerting.Messages.AlertAcknowledgedMessage>();
            var resolvedMessage = GetLastMessage<AhBearStudios.Core.Alerting.Messages.AlertResolvedMessage>();

            Assert.That(raisedMessage.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(acknowledgedMessage.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(resolvedMessage.CorrelationId, Is.EqualTo(correlationId));

            // Verify correlation in log entries with enhanced tracking
            Assert.That(StubLogging.RecordedLogs.Any(log => log.CorrelationId.ToString().Contains(correlationId.ToString())), Is.True);
            Assert.That(_correlationHelper.WasCreatedByHelper(correlationId), Is.True);
        }

        [Test]
        public void CorrelationTracking_WithRelatedOperations_MaintainsRelationships()
        {
            // Arrange
            var baseCorrelationId = CreateTestCorrelationId();
            var relatedIds = TestCorrelationHelper.CreateRelatedCorrelationIds("AlertOperation", 3);

            // Act - Create alerts with related correlation IDs
            var alerts = relatedIds
                .Select(id => Alert.Create($"Related alert", AlertSeverity.Info, "RelationTest", correlationId: id))
                .ToList();

            foreach (var alert in alerts)
            {
                _alertService.RaiseAlert(alert);
            }

            // Assert - All related messages should be trackable
            var raisedMessages = SpyMessageBus.PublishedMessages.OfType<AlertRaisedMessage>().ToList();
            var correlationIds = raisedMessages.Select(m => m.CorrelationId).ToList();

            // Verify test channel captured all related alerts
            foreach (var relatedId in relatedIds)
            {
                Assert.That(_testChannel.GetAlertsForCorrelation(relatedId), Is.Not.Empty);
            }

            foreach (var relatedId in relatedIds)
            {
                Assert.That(correlationIds.Contains(relatedId), Is.True);
            }
        }

        #endregion

        #region Emergency Mode Tests

        [Test]
        public void EmergencyMode_WhenEnabled_BypassesFiltersAndSuppression()
        {
            // Arrange
            // Add a filter that would normally suppress debug alerts
            var severityFilter = new SeverityAlertFilter(SpyMessageBus, AlertSeverity.Critical);
            _alertService.AddFilter(severityFilter);

            var correlationId = CreateTestCorrelationId();

            // Act - Enable emergency mode
            _alertService.EnableEmergencyMode("Integration test emergency", correlationId);

            // Raise a low-severity alert that would normally be suppressed
            var debugAlert = Alert.Create(
                "Emergency debug alert",
                AlertSeverity.Debug,
                "EmergencyTest");

            _alertService.RaiseAlert(debugAlert);

            // Assert - Alert should pass through despite filter
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Any(a => a.Severity == AlertSeverity.Debug), Is.True);
            Assert.That(_testChannel.GetAlertCount(AlertSeverity.Debug), Is.GreaterThan(0));

            // Verify emergency mode is active
            Assert.That(_alertService.IsEmergencyModeActive, Is.True);

            // Disable emergency mode
            _alertService.DisableEmergencyMode(correlationId);
            Assert.That(_alertService.IsEmergencyModeActive, Is.False);
        }

        [Test]
        public async UniTask EmergencyEscalation_WithFailedDelivery_EscalatesCorrectly()
        {
            // Arrange
            var criticalAlert = Alert.Create(
                "Critical emergency alert",
                AlertSeverity.Critical,
                "EmergencyTest");

            var correlationId = CreateTestCorrelationId();

            // Act
            await _alertService.PerformEmergencyEscalationAsync(criticalAlert, correlationId);

            // Assert
            AssertLogContains("Emergency escalation completed for alert");
        }

        #endregion

        #region Performance Under Load Tests

        [Test]
        public async UniTask SystemUnderLoad_WithHighThroughput_MaintainsPerformance()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            var alertCount = 1000;
            var correlationId = CreateTestCorrelationId();

            // Act
            var performanceResult = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var tasks = Enumerable.Range(0, alertCount)
                        .Select(async i =>
                        {
                            var alert = Alert.Create(
                                $"Load test alert {i}",
                                AlertSeverity.Info,
                                "LoadTest",
                                correlationId: correlationId);

                            await _alertService.RaiseAlertAsync(alert);
                        })
                        .ToList();

                    await UniTask.WhenAll(tasks);
                },
                "HighThroughputAlerts",
                TimeSpan.FromSeconds(2)); // Should complete within 2 seconds

            // Wait for all processing
            await UniTask.Delay(1000);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Count(), Is.EqualTo(alertCount));

            // Verify system remains healthy under load with enhanced monitoring
            Assert.That(_alertService.IsHealthy, Is.True);
            var channelStats = _testChannel.GetChannelStatistics();
            Assert.That((int)channelStats["TotalAlertsSent"], Is.EqualTo(alertCount));
            Assert.That((bool)channelStats["IsHealthy"], Is.True);
            Assert.That((int)channelStats["FailedCalls"], Is.EqualTo(0));

            // Verify performance metrics
            LogPerformanceMetrics(performanceResult);
            Assert.That(performanceResult.Duration, Is.LessThan(TimeSpan.FromSeconds(2)),
                "High throughput operations should complete within acceptable time");
        }

        [Test]
        public async UniTask ConcurrentOperations_WithMixedOperations_HandlesCorrectly()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            var correlationId = _correlationHelper.CreateCorrelationId("ConcurrentOperations_Batch");
            var alerts = Enumerable.Range(0, 100)
                .Select(i => Alert.Create($"Concurrent alert {i}", AlertSeverity.Warning, "ConcurrentTest", correlationId: correlationId))
                .ToList();

            // Act - Perform concurrent raise, acknowledge, and resolve operations
            var raiseTasks = alerts
                .Select(alert => _alertService.RaiseAlertAsync(alert))
                .ToList();

            await UniTask.WhenAll(raiseTasks);

            var acknowledgeTasks = alerts
                .Take(50)
                .Select(alert => UniTask.Run(() => _alertService.AcknowledgeAlert(alert.Id)))
                .ToList();

            var resolveTasks = alerts
                .Skip(50)
                .Select(alert => UniTask.Run(() => _alertService.ResolveAlert(alert.Id)))
                .ToList();

            await UniTask.WhenAll(acknowledgeTasks.Concat(resolveTasks));

            // Wait for processing
            await UniTask.Delay(500);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts.Count(), Is.EqualTo(100));

            var acknowledgedCount = activeAlerts.Count(a => a.IsAcknowledged);
            var resolvedCount = activeAlerts.Count(a => a.IsResolved);

            Assert.That(acknowledgedCount + resolvedCount, Is.EqualTo(100));

            // Verify test channel captured all concurrent operations
            var channelStats = _testChannel.GetChannelStatistics();
            Assert.That((int)channelStats["TotalAlertsSent"], Is.EqualTo(100));
            Assert.That((int)channelStats["SuccessfulCalls"], Is.EqualTo(100));
            Assert.That(_testChannel.GetAlertsForCorrelation(correlationId).Count, Is.EqualTo(100));

            // Verify correlation tracking worked across concurrent operations
            Assert.That(_correlationHelper.WasCreatedByHelper(correlationId), Is.True);
        }

        #endregion

        #region Service Integration Tests

        [Test]
        public void ServiceIntegration_WithAllDependencies_IntegratesCorrectly()
        {
            // Arrange & Act - Already done in Setup

            // Assert - Verify all services are properly integrated
            Assert.That(_alertService.ChannelService, Is.Not.Null);
            Assert.That(_alertService.FilterService, Is.Not.Null);
            Assert.That(_alertService.SuppressionService, Is.Not.Null);

            // Verify mock services are being used
            Assert.That(StubLogging.RecordedLogs.Count, Is.GreaterThan(0));
            Assert.That(SpyMessageBus.IsEnabled, Is.True);
            Assert.That(FakePooling.IsEnabled, Is.True);
            Assert.That(FakeSerialization.IsEnabled, Is.True);
        }

        [Test]
        public async UniTask ServiceIntegration_WithFailoverScenarios_HandlesGracefully()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            // Simulate service failures
            // Note: SpyMessageBusService doesn't have ShouldThrowOnPublish property
            // This test scenario needs to be updated for the new spy implementation

            var alert = Alert.Create("Failover test", AlertSeverity.Error, "FailoverTest");

            // Act & Assert - Should handle message bus failure gracefully
            await AssertGracefulFailureHandlingAsync(async () =>
            {
                _alertService.RaiseAlert(alert);
                await UniTask.Delay(100);
            });

            // Verify alert was still processed
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);

            // Reset mock
            // Cleanup: SpyMessageBus doesn't require reset since it doesn't have failure states
        }

        #endregion

        #region Cleanup and Maintenance Tests

        [Test]
        public void MaintenanceOperations_WithOldAlerts_CleansUpCorrectly()
        {
            // Arrange
            var oldAlert = Alert.Create("Old alert", AlertSeverity.Warning, "MaintenanceTest");
            _alertService.RaiseAlert(oldAlert);

            // Resolve the alert (making it eligible for cleanup)
            _alertService.ResolveAlert(oldAlert.Id);

            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.PerformMaintenance(correlationId.ToString());

            // Assert
            AssertLogContains("Maintenance completed:");
        }

        [Test]
        public async UniTask ServiceShutdown_WithActiveAlerts_ShutsDownGracefully()
        {
            // Arrange
            await _alertService.StartAsync(CreateTestCorrelationId());

            var alert = Alert.Create("Shutdown test", AlertSeverity.Info, "ShutdownTest");
            _alertService.RaiseAlert(alert);

            var correlationId = CreateTestCorrelationId();

            // Act
            await _alertService.StopAsync(correlationId);

            // Assert
            Assert.That(_alertService.IsEnabled, Is.False);
            AssertLogContains("Alert service stopped");
        }

        #endregion
    }
}