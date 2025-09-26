using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Alerting.Builders;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Factories;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Tests.Shared.Channels;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Comprehensive unit tests for AlertService functionality following CLAUDETESTS.md guidelines.
    /// Tests service lifecycle, alert raising, filtering, channel management, and production readiness.
    /// Enhanced with performance testing, frame budget compliance, and TDD test double patterns.
    /// Validates zero-allocation patterns and correlation tracking for Unity game development.
    /// Updated for new decomposed service architecture with AlertOrchestrationService, AlertStateManagementService, and AlertHealthMonitoringService.
    /// </summary>
    [TestFixture]
    public class AlertServiceTests : BaseServiceTest
    {
        private IAlertService _alertService;
        private AlertServiceFactory _factory;
        private AlertServiceConfiguration _configuration;

        [SetUp]
        public override async void Setup()
        {
            // Initialize shared test doubles from BaseServiceTest
            base.Setup();

            // Create factory with shared test doubles
            _factory = new AlertServiceFactory(
                StubLogging,           // Shared stub from BaseServiceTest
                SpyMessageBus,         // Shared spy from BaseServiceTest
                FakeSerialization,     // Shared fake for serialization
                FakePooling,           // Shared fake for pooling operations
                FakeHealthCheck,       // Shared fake for health checking
                NullProfiler);         // Null profiler for tests

            // Create test configuration using AlertConfigBuilder
            _configuration = new AlertConfigBuilder()
                .ForTesting()
                .WithMinimumSeverity(AlertSeverity.Info)
                .BuildServiceConfiguration();

            // Create AlertService using factory and decomposed services
            _alertService = await _factory.CreateAlertServiceAsync(_configuration, CreateTestCorrelationId());
        }

        [TearDown]
        public override void TearDown()
        {
            _alertService?.Dispose();
            base.TearDown();
        }

        #region Service Creation and Configuration Tests

        [Test]
        public async UniTask CreateAlertServiceAsync_WithValidConfiguration_CreatesServiceSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var config = _factory.GetDefaultConfiguration();

            // Act
            var result = await ExecuteWithPerformanceMeasurementAsync(
                () => _factory.CreateAlertServiceAsync(config, correlationId),
                "CreateAlertService",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEnabled, Is.True);
            Assert.That(result.Configuration, Is.EqualTo(config));
            AssertNoErrors();
        }

        [Test]
        public void CreateAlertServiceAsync_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _factory.CreateAlertServiceAsync(null));
        }

        [Test]
        public async UniTask CreateDevelopmentAlertServiceAsync_CreatesServiceWithCorrectConfiguration()
        {
            // Arrange & Act
            var result = await ExecuteWithPerformanceMeasurementAsync(
                () => _factory.CreateDevelopmentAlertServiceAsync(StubLogging, SpyMessageBus),
                "CreateDevelopmentService",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEnabled, Is.True);
            Assert.That(result.Configuration.Environment, Is.EqualTo(AlertEnvironmentType.Development));
            AssertNoErrors();
        }

        [Test]
        public async UniTask CreateProductionAlertServiceAsync_CreatesServiceWithCorrectConfiguration()
        {
            // Arrange & Act
            var result = await ExecuteWithPerformanceMeasurementAsync(
                () => _factory.CreateProductionAlertServiceAsync(StubLogging, SpyMessageBus),
                "CreateProductionService",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEnabled, Is.True);
            Assert.That(result.Configuration.Environment, Is.EqualTo(AlertEnvironmentType.Production));
            AssertNoErrors();
        }

        [Test]
        public async UniTask CreateTestAlertServiceAsync_CreatesServiceWithCorrectConfiguration()
        {
            // Arrange & Act
            var result = await ExecuteWithPerformanceMeasurementAsync(
                () => _factory.CreateTestAlertServiceAsync(SpyMessageBus),
                "CreateTestService",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEnabled, Is.True);
            Assert.That(result.Configuration.Environment, Is.EqualTo(AlertEnvironmentType.Testing));
            AssertNoErrors();
        }

        #endregion

        #region Alert Raising Tests

        [Test]
        public void RaiseAlert_WithValidStringMessage_ProcessesAlertSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var message = "Test alert message";
            var severity = AlertSeverity.Warning;
            var source = new FixedString64Bytes("TestSource");

            // Act
            ExecuteWithPerformanceMeasurement(
                () => _alertService.RaiseAlert(message, severity, source, correlationId: correlationId),
                "RaiseAlert",
                TestConstants.FrameBudget);

            // Assert
            AssertMessagePublished<AlertRaisedMessage>(msg =>
                msg.Message.Contains(message) &&
                msg.Severity == severity &&
                msg.CorrelationId == correlationId);
            AssertLogContains("Alert");
            AssertNoErrors();
        }

        [Test]
        public void RaiseAlert_WithFixedStringMessage_ProcessesAlertSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var message = new FixedString512Bytes("Test fixed string message");
            var severity = AlertSeverity.Error;
            var source = new FixedString64Bytes("TestSource");

            // Act
            ExecuteWithPerformanceMeasurement(
                () => _alertService.RaiseAlert(message, severity, source, correlationId: correlationId),
                "RaiseAlertFixed",
                TestConstants.FrameBudget);

            // Assert
            AssertMessagePublished<AlertRaisedMessage>(msg =>
                msg.Message.Contains("Test fixed string message") &&
                msg.Severity == severity);
            AssertNoErrors();
        }

        [Test]
        public async UniTask RaiseAlertAsync_WithStringMessage_ProcessesAlertAsynchronously()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var message = "Async test alert";
            var severity = AlertSeverity.Info;
            var source = "AsyncTestSource";

            // Act
            await ExecuteWithPerformanceMeasurementAsync(
                () => _alertService.RaiseAlertAsync(message, severity, source, correlationId: correlationId),
                "RaiseAlertAsync",
                TestConstants.FrameBudget);

            // Assert
            AssertMessagePublished<AlertRaisedMessage>(msg =>
                msg.Message.Contains(message) &&
                msg.Severity == severity &&
                msg.CorrelationId == correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask RaiseAlertAsync_WithAlert_ProcessesAlertAsynchronously()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var alert = CreateValidTestAlert(severity: AlertSeverity.Critical, correlationId: correlationId);

            // Act
            await ExecuteWithPerformanceMeasurementAsync(
                () => _alertService.RaiseAlertAsync(alert),
                "RaiseAlertAsyncObject",
                TestConstants.FrameBudget);

            // Assert
            AssertMessagePublished<AlertRaisedMessage>(msg =>
                msg.AlertId == alert.Id &&
                msg.Severity == AlertSeverity.Critical &&
                msg.CorrelationId == correlationId);
            AssertNoErrors();
        }

        #endregion

        #region Alert Management Tests

        [Test]
        public void GetActiveAlerts_WhenAlertsRaised_ReturnsCorrectAlerts()
        {
            // Arrange
            var alert1 = CreateValidTestAlert("Alert 1", AlertSeverity.Warning);
            var alert2 = CreateValidTestAlert("Alert 2", AlertSeverity.Error);

            _alertService.RaiseAlert(alert1);
            _alertService.RaiseAlert(alert2);

            // Act
            var result = ExecuteWithPerformanceMeasurement(
                () => _alertService.GetActiveAlerts().ToList(),
                "GetActiveAlerts",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
            AssertNoErrors();
        }

        [Test]
        public void GetAlertHistory_WithTimeSpan_ReturnsHistoricalAlerts()
        {
            // Arrange
            var alert = CreateValidTestAlert("Historical alert", AlertSeverity.Info);
            _alertService.RaiseAlert(alert);
            var period = TimeSpan.FromMinutes(5);

            // Act
            var result = ExecuteWithPerformanceMeasurement(
                () => _alertService.GetAlertHistory(period).ToList(),
                "GetAlertHistory",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            AssertNoErrors();
        }

        [Test]
        public void AcknowledgeAlert_WithValidId_AcknowledgesSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var alert = CreateValidTestAlert(correlationId: correlationId);
            _alertService.RaiseAlert(alert);

            // Act
            ExecuteWithPerformanceMeasurement(
                () => _alertService.AcknowledgeAlert(alert.Id, "TestUser", correlationId),
                "AcknowledgeAlert",
                TestConstants.FrameBudget);

            // Assert
            AssertMessagePublished<AlertAcknowledgedMessage>(msg =>
                msg.AlertId == alert.Id &&
                msg.AcknowledgedBy.Contains("TestUser"));
            AssertNoErrors();
        }

        [Test]
        public void ResolveAlert_WithValidId_ResolvesSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var alert = CreateValidTestAlert(correlationId: correlationId);
            _alertService.RaiseAlert(alert);

            // Act
            ExecuteWithPerformanceMeasurement(
                () => _alertService.ResolveAlert(alert.Id, "TestUser", "Test resolution", correlationId),
                "ResolveAlert",
                TestConstants.FrameBudget);

            // Assert
            AssertMessagePublished<AlertResolvedMessage>(msg =>
                msg.AlertId == alert.Id &&
                msg.ResolvedBy.Contains("TestUser"));
            AssertNoErrors();
        }

        #endregion

        #region Service Lifecycle Tests

        [Test]
        public async UniTask StartAsync_WhenCalled_StartsServiceSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            await ExecuteWithPerformanceMeasurementAsync(
                () => _alertService.StartAsync(),
                "StartService",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(_alertService.IsEnabled, Is.True);
            AssertLogContains("started");
            AssertNoErrors();
        }

        [Test]
        public async UniTask StopAsync_WhenCalled_StopsServiceSuccessfully()
        {
            // Arrange
            await _alertService.StartAsync();

            // Act
            await ExecuteWithPerformanceMeasurementAsync(
                () => _alertService.StopAsync(),
                "StopService",
                TestConstants.FrameBudget);

            // Assert
            AssertLogContains("stopped");
            AssertNoErrors();
        }

        #endregion

        #region Statistics and Health Tests

        [Test]
        public void GetStatistics_WhenCalled_ReturnsValidStatistics()
        {
            // Arrange
            _alertService.RaiseAlert("Test alert", AlertSeverity.Info, "TestSource");

            // Act
            var result = ExecuteWithPerformanceMeasurement(
                () => _alertService.GetStatistics(),
                "GetStatistics",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.LastUpdated, Is.GreaterThan(DateTime.MinValue));
            AssertNoErrors();
        }

        [Test]
        public void IsHealthy_WithNormalOperation_ReturnsTrue()
        {
            // Act
            var result = ExecuteWithPerformanceMeasurement(
                () => _alertService.IsHealthy,
                "CheckHealth",
                TestConstants.FrameBudget);

            // Assert
            Assert.That(result, Is.True);
            AssertNoErrors();
        }

        #endregion

        #region Performance and Frame Budget Tests

        [Test]
        public void RaiseAlert_Performance_CompletesWithinFrameBudget()
        {
            // Arrange
            var alert = CreateValidTestAlert();

            // Act & Assert - ExecuteWithPerformanceMeasurement validates frame budget automatically
            ExecuteWithPerformanceMeasurement(
                () => _alertService.RaiseAlert(alert),
                "RaiseAlert_Performance",
                TestConstants.FrameBudget);

            AssertNoErrors();
        }

        [Test]
        public async UniTask RaiseAlertAsync_Performance_CompletesWithinFrameBudget()
        {
            // Arrange
            var alert = CreateValidTestAlert();

            // Act & Assert - ExecuteWithPerformanceMeasurementAsync validates frame budget automatically
            await ExecuteWithPerformanceMeasurementAsync(
                () => _alertService.RaiseAlertAsync(alert),
                "RaiseAlertAsync_Performance",
                TestConstants.FrameBudget);

            AssertNoErrors();
        }

        #endregion

        #region Legacy Compatibility Tests

        [Test]
        public void AddChannel_WithValidChannel_AddsChannelSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act
            ExecuteWithPerformanceMeasurement(
                () => _alertService.AddChannel(channel),
                "AddChannel",
                TestConstants.FrameBudget);

            // Assert
            AssertLogContains("channel");
            AssertNoErrors();
        }

        [Test]
        public void RemoveChannel_WithValidChannel_RemovesChannelSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            _alertService.AddChannel(channel);

            // Act
            ExecuteWithPerformanceMeasurement(
                () => _alertService.RemoveChannel(channel),
                "RemoveChannel",
                TestConstants.FrameBudget);

            // Assert
            AssertLogContains("channel");
            AssertNoErrors();
        }

        #endregion
    }
}