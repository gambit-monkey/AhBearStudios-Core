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
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Tests.Shared.Channels;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Comprehensive unit tests for AlertService functionality.
    /// Tests service lifecycle, alert raising, filtering, channel management, and production readiness.
    /// </summary>
    [TestFixture]
    public class AlertServiceTests : BaseServiceTest
    {
        private AlertService _alertService;
        private AlertServiceConfiguration _configuration;
        private IAlertChannelService _channelService;
        private IAlertFilterService _filterService;
        private IAlertSuppressionService _suppressionService;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Create test configuration
            _configuration = new AlertConfigBuilder(MockPooling)
                .ForProduction()
                .WithMinimumSeverity(AlertSeverity.Info)
                .BuildServiceConfiguration();

            // Create integrated services with mock dependencies
            _channelService = new AlertChannelService(
                new AlertChannelServiceConfig(),
                MockLogging,
                MockMessageBus);

            _filterService = new AlertFilterService(MockLogging);
            _suppressionService = new AlertSuppressionService(MockLogging);

            // Create AlertService with all dependencies
            _alertService = new AlertService(
                _configuration,
                _channelService,
                _filterService,
                _suppressionService,
                MockMessageBus,
                MockLogging,
                MockSerialization,
                MockPooling);
        }

        [TearDown]
        public override void TearDown()
        {
            _alertService?.Dispose();
            _channelService?.Dispose();
            _filterService?.Dispose();
            _suppressionService?.Dispose();

            base.TearDown();
        }

        #region Service Lifecycle Tests

        [Test]
        public void Constructor_WithValidConfiguration_InitializesCorrectly()
        {
            // Arrange & Act done in Setup

            // Assert
            Assert.That(_alertService.IsEnabled, Is.True);
            Assert.That(_alertService.Configuration, Is.Not.Null);
            Assert.That(_alertService.ChannelService, Is.Not.Null);
            Assert.That(_alertService.FilterService, Is.Not.Null);
            Assert.That(_alertService.SuppressionService, Is.Not.Null);

            AssertLogContains("Alert service initialized with integrated subsystems");
        }

        [Test]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AlertService(null, _channelService, _filterService, _suppressionService));
        }

        [Test]
        public void Constructor_WithNullChannelService_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AlertService(_configuration, null, _filterService, _suppressionService));
        }

        [Test]
        public async Task StartAsync_WhenNotStarted_StartsServiceSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            await _alertService.StartAsync(correlationId);

            // Assert
            Assert.That(_alertService.IsEnabled, Is.True);
            AssertLogContains("Alert service started");
        }

        [Test]
        public async Task StopAsync_WhenStarted_StopsServiceGracefully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            await _alertService.StartAsync(correlationId);

            // Act
            await _alertService.StopAsync(correlationId);

            // Assert
            Assert.That(_alertService.IsEnabled, Is.False);
            AssertLogContains("Alert service stopped");
        }

        [Test]
        public async Task RestartAsync_WhenRunning_RestartsServiceSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            await _alertService.StartAsync(correlationId);

            // Act
            await _alertService.RestartAsync(correlationId);

            // Assert
            Assert.That(_alertService.IsEnabled, Is.True);
            AssertLogContains("Alert service restarted");
        }

        [Test]
        public void Dispose_WhenCalled_DisposesServiceCleanly()
        {
            // Arrange
            var alertService = new AlertService(
                _configuration,
                _channelService,
                _filterService,
                _suppressionService,
                MockMessageBus,
                MockLogging);

            // Act
            alertService.Dispose();

            // Assert
            Assert.That(alertService.IsEnabled, Is.False);
            AssertLogContains("Alert service disposed");
        }

        #endregion

        #region Alert Raising Tests

        [Test]
        public void RaiseAlert_WithValidStringMessage_RaisesAlertSuccessfully()
        {
            // Arrange
            var message = TestConstants.SampleAlertMessage;
            var severity = AlertSeverity.Warning;
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.RaiseAlert(message, severity, source, correlationId: correlationId);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);
            Assert.That(activeAlerts.First().Severity, Is.EqualTo(severity));
            Assert.That(activeAlerts.First().Message.ToString(), Is.EqualTo(message));

            AssertMessagePublished<AlertRaisedMessage>();
            AssertLogContains($"Alert raised: {severity}");
        }

        [Test]
        public void RaiseAlert_WithFixedStringMessage_RaisesAlertSuccessfully()
        {
            // Arrange
            var message = new FixedString512Bytes(TestConstants.SampleAlertMessage);
            var severity = AlertSeverity.Error;
            var source = new FixedString64Bytes(TestConstants.TestSource);
            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.RaiseAlert(message, severity, source, correlationId: correlationId);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);
            Assert.That(activeAlerts.First().Severity, Is.EqualTo(severity));

            AssertMessagePublished<AlertRaisedMessage>();
        }

        [Test]
        public void RaiseAlert_WithPreconstructedAlert_RaisesAlertSuccessfully()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Critical,
                TestConstants.TestSource,
                correlationId: CreateTestCorrelationId());

            // Act
            _alertService.RaiseAlert(alert);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);
            Assert.That(activeAlerts.First().Id, Is.EqualTo(alert.Id));

            AssertMessagePublished<AlertRaisedMessage>();
        }

        [Test]
        public void RaiseAlert_WhenServiceDisabled_DoesNotRaiseAlert()
        {
            // Arrange
            _alertService.Dispose(); // Disable service
            var alert = Alert.Create(TestConstants.SampleAlertMessage, AlertSeverity.Info, TestConstants.TestSource);

            // Act
            _alertService.RaiseAlert(alert);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Empty);
            AssertMessageCount(0);
        }

        [Test]
        public void RaiseAlert_WithSeverityBelowMinimum_DoesNotRaiseAlert()
        {
            // Arrange
            _alertService.SetMinimumSeverity(AlertSeverity.Warning);
            var alert = Alert.Create(TestConstants.SampleAlertMessage, AlertSeverity.Info, TestConstants.TestSource);

            // Act
            _alertService.RaiseAlert(alert);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Empty);
        }

        [Test]
        public async Task RaiseAlertAsync_WithValidAlert_RaisesAlertAsynchronously()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.High,
                TestConstants.TestSource,
                correlationId: CreateTestCorrelationId());

            // Act
            await _alertService.RaiseAlertAsync(alert);

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            Assert.That(activeAlerts, Is.Not.Empty);
            AssertMessagePublished<AlertRaisedMessage>();
        }

        [Test]
        public async Task RaiseAlertAsync_WithCancellation_HandlescancellationGracefully()
        {
            // Arrange
            var alert = Alert.Create(TestConstants.SampleAlertMessage, AlertSeverity.Info, TestConstants.TestSource);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(1)).Token;

            // Act & Assert
            await UniTask.Delay(10); // Ensure cancellation token is triggered
            await _alertService.RaiseAlertAsync(alert, cancellationToken);

            // Should complete without throwing (cancellation is handled internally)
        }

        #endregion

        #region Alert Management Tests

        [Test]
        public void GetActiveAlerts_WithMultipleAlerts_ReturnsActiveAlertsOnly()
        {
            // Arrange
            var alert1 = Alert.Create("Alert 1", AlertSeverity.Warning, TestConstants.TestSource);
            var alert2 = Alert.Create("Alert 2", AlertSeverity.Error, TestConstants.TestSource);
            var alert3 = Alert.Create("Alert 3", AlertSeverity.Info, TestConstants.TestSource);

            _alertService.RaiseAlert(alert1);
            _alertService.RaiseAlert(alert2);
            _alertService.RaiseAlert(alert3);

            // Acknowledge one alert
            _alertService.AcknowledgeAlert(alert2.Id);

            // Act
            var activeAlerts = _alertService.GetActiveAlerts();

            // Assert
            Assert.That(activeAlerts.Count(), Is.EqualTo(2)); // Only alert1 and alert3 should be active
            Assert.That(activeAlerts.AsValueEnumerable().All(a => a.IsActive), Is.True);
        }

        [Test]
        public void AcknowledgeAlert_WithValidId_AcknowledgesAlert()
        {
            // Arrange
            var alert = Alert.Create(TestConstants.SampleAlertMessage, AlertSeverity.Warning, TestConstants.TestSource);
            _alertService.RaiseAlert(alert);
            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.AcknowledgeAlert(alert.Id, correlationId.ToString());

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            var acknowledgedAlert = activeAlerts.AsValueEnumerable().FirstOrDefault(a => a.Id == alert.Id);

            Assert.That(acknowledgedAlert?.IsAcknowledged, Is.True);
            AssertMessagePublished<AlertAcknowledgedMessage>();
            AssertLogContains($"Alert acknowledged: {alert.Id}");
        }

        [Test]
        public void ResolveAlert_WithValidId_ResolvesAlert()
        {
            // Arrange
            var alert = Alert.Create(TestConstants.SampleAlertMessage, AlertSeverity.Error, TestConstants.TestSource);
            _alertService.RaiseAlert(alert);
            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.ResolveAlert(alert.Id, correlationId.ToString());

            // Assert
            var activeAlerts = _alertService.GetActiveAlerts();
            var resolvedAlert = activeAlerts.AsValueEnumerable().FirstOrDefault(a => a.Id == alert.Id);

            Assert.That(resolvedAlert?.IsResolved, Is.True);
            AssertMessagePublished<AlertResolvedMessage>();
            AssertLogContains($"Alert resolved: {alert.Id}");
        }

        [Test]
        public void GetAlertHistory_WithTimeSpan_ReturnsAlertsInTimeframe()
        {
            // Arrange
            var alert1 = Alert.Create("Recent Alert", AlertSeverity.Warning, TestConstants.TestSource);
            _alertService.RaiseAlert(alert1);

            var timePeriod = TimeSpan.FromMinutes(5);

            // Act
            var history = _alertService.GetAlertHistory(timePeriod);

            // Assert
            Assert.That(history, Is.Not.Empty);
            Assert.That(history.AsValueEnumerable().Any(a => a.Id == alert1.Id), Is.True);
        }

        #endregion

        #region Severity Management Tests

        [Test]
        public void SetMinimumSeverity_WithValidSeverity_UpdatesGlobalMinimum()
        {
            // Arrange
            var newMinimum = AlertSeverity.Warning;

            // Act
            _alertService.SetMinimumSeverity(newMinimum);

            // Assert
            Assert.That(_alertService.GetMinimumSeverity(), Is.EqualTo(newMinimum));
            AssertLogContains($"Global minimum severity set to {newMinimum}");
        }

        [Test]
        public void SetMinimumSeverity_WithSourceSpecific_UpdatesSourceMinimum()
        {
            // Arrange
            var source = new FixedString64Bytes("SpecificSource");
            var sourceSeverity = AlertSeverity.Error;

            // Act
            _alertService.SetMinimumSeverity(source, sourceSeverity);

            // Assert
            Assert.That(_alertService.GetMinimumSeverity(source), Is.EqualTo(sourceSeverity));
            AssertLogContains($"Minimum severity for {source} set to {sourceSeverity}");
        }

        [Test]
        public void GetMinimumSeverity_WithUnknownSource_ReturnsGlobalMinimum()
        {
            // Arrange
            var globalMinimum = AlertSeverity.Warning;
            _alertService.SetMinimumSeverity(globalMinimum);
            var unknownSource = new FixedString64Bytes("UnknownSource");

            // Act
            var result = _alertService.GetMinimumSeverity(unknownSource);

            // Assert
            Assert.That(result, Is.EqualTo(globalMinimum));
        }

        #endregion

        #region Channel Management Tests

        [Test]
        public void RegisterChannel_WithValidChannel_RegistersSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.RegisterChannel(channel, correlationId.ToString());

            // Assert
            var channels = _alertService.GetRegisteredChannels();
            Assert.That(channels.AsValueEnumerable().Any(c => c.Name.ToString() == "TestChannel"), Is.True);
            AssertLogContains("Alert channel registered: TestChannel");
        }

        [Test]
        public void UnregisterChannel_WithValidName_UnregistersSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            _alertService.RegisterChannel(channel);
            var correlationId = CreateTestCorrelationId();

            // Act
            var result = _alertService.UnregisterChannel("TestChannel", correlationId.ToString());

            // Assert
            Assert.That(result, Is.True);
            var channels = _alertService.GetRegisteredChannels();
            Assert.That(channels.AsValueEnumerable().Any(c => c.Name.ToString() == "TestChannel"), Is.False);
            AssertLogContains("Alert channel unregistered: TestChannel");
        }

        [Test]
        public void GetRegisteredChannels_WithMultipleChannels_ReturnsAllChannels()
        {
            // Arrange
            var channel1 = new TestAlertChannel("Channel1");
            var channel2 = new TestAlertChannel("Channel2");

            _alertService.RegisterChannel(channel1);
            _alertService.RegisterChannel(channel2);

            // Act
            var channels = _alertService.GetRegisteredChannels();

            // Assert
            Assert.That(channels.Count, Is.EqualTo(2));
            Assert.That(channels.AsValueEnumerable().Any(c => c.Name.ToString() == "Channel1"), Is.True);
            Assert.That(channels.AsValueEnumerable().Any(c => c.Name.ToString() == "Channel2"), Is.True);
        }

        #endregion

        #region Health and Diagnostics Tests

        [Test]
        public void IsHealthy_WithHealthyService_ReturnsTrue()
        {
            // Arrange & Act
            var isHealthy = _alertService.IsHealthy;

            // Assert
            Assert.That(isHealthy, Is.True);
        }

        [Test]
        public async Task PerformHealthCheckAsync_WhenHealthy_ReturnsHealthyReport()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            var report = await _alertService.PerformHealthCheckAsync(correlationId);

            // Assert
            Assert.That(report, Is.Not.Null);
            Assert.That(report.OverallHealth, Is.True);
            Assert.That(report.ServiceEnabled, Is.True);
            AssertLogContains("Health check completed - Overall: True");
        }

        [Test]
        public void GetDiagnostics_Always_ReturnsCompleteDiagnostics()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            var diagnostics = _alertService.GetDiagnostics(correlationId);

            // Assert
            Assert.That(diagnostics, Is.Not.Null);
            Assert.That(diagnostics.ServiceVersion, Is.Not.Null);
            Assert.That(diagnostics.IsEnabled, Is.True);
            Assert.That(diagnostics.SubsystemStatuses, Is.Not.Null);
        }

        [Test]
        public void ValidateConfiguration_WithValidConfiguration_ReturnsSuccess()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            var result = _alertService.ValidateConfiguration(correlationId.ToString());

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ComponentName, Is.EqualTo("AlertService"));
        }

        #endregion

        #region Performance and Maintenance Tests

        [Test]
        public void PerformMaintenance_WhenCalled_CleansUpOldAlerts()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            _alertService.PerformMaintenance(correlationId.ToString());

            // Assert
            AssertLogContains("Maintenance completed");
        }

        [Test]
        public async Task FlushAsync_WhenCalled_FlushesAllChannels()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            _alertService.RegisterChannel(channel);
            var correlationId = CreateTestCorrelationId();

            // Act
            await _alertService.FlushAsync(correlationId.ToString());

            // Assert
            AssertLogContains("All channels flushed");
        }

        [Test]
        public void GetStatistics_Always_ReturnsValidStatistics()
        {
            // Arrange & Act
            var statistics = _alertService.GetStatistics();

            // Assert
            Assert.That(statistics, Is.Not.Null);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void RaiseAlert_WithNullAlert_DoesNotThrow()
        {
            // Arrange
            Alert nullAlert = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _alertService.RaiseAlert(nullAlert));
        }

        [Test]
        public void RegisterChannel_WithNullChannel_DoesNotThrow()
        {
            // Arrange
            IAlertChannel nullChannel = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _alertService.RegisterChannel(nullChannel));
        }

        [Test]
        public void AcknowledgeAlert_WithNonExistentId_DoesNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => _alertService.AcknowledgeAlert(nonExistentId));
        }

        #endregion
    }
}