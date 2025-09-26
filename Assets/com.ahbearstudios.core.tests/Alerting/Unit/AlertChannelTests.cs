using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Channels;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Tests.Shared.Channels;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Comprehensive unit tests for alert channels following CLAUDETESTS.md guidelines.
    /// Tests delivery mechanisms, health status tracking, channel service management functionality,
    /// performance compliance, and integration with shared test doubles. Validates complete
    /// alert channel lifecycle with correlation tracking, frame budget compliance, and TDD patterns.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    [TestFixture]
    public class AlertChannelTests : BaseServiceTest
    {
        private Alert _testAlert;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Create test alert with proper correlation tracking
            var correlationId = CreateTestCorrelationId("AlertChannelTest");
            _testAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            StubLogging.LogInfo($"AlertChannelTests setup completed with test alert ID: {_testAlert.Id}", correlationId.ToString());
        }

        protected override void OnTearDown()
        {
            _testAlert = null;
        }

        #region TestAlertChannel Tests

        [Test]
        public void Constructor_WithValidName_InitializesCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channelName = "TestChannel";

            // Act
            var channel = new TestAlertChannel(channelName);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo(channelName));
            Assert.That(channel.IsEnabled, Is.True);
            Assert.That(channel.IsHealthy, Is.True);
            Assert.That(channel.MinimumSeverity, Is.EqualTo(AlertSeverity.Debug));

            // Verify no errors occurred during construction
            AssertNoErrors();
        }

        [Test]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new TestAlertChannel(""));
            Assert.That(exception.ParamName, Does.Contain("name").IgnoreCase);

            AssertNoErrors();
        }

        [Test]
        public async UniTask SendAlertAsync_WithValidAlert_SendsSuccessfully()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new TestAlertChannel("TestChannel");

            // Act
            await channel.SendAlertAsync(_testAlert, correlationId);

            // Assert
            Assert.That(channel.SentAlerts, Is.Not.Empty, "Channel should have sent alerts");
            Assert.That(channel.SentAlerts.First().Id, Is.EqualTo(_testAlert.Id), "Sent alert should match test alert");
            Assert.That(channel.AlertCalls.Count, Is.EqualTo(1), "Alert call count should be 1");

            // Verify correlation tracking
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask SendAlertAsync_WhenDisabled_DoesNotSend()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new TestAlertChannel("TestChannel");
            channel.Disable();

            // Act
            await channel.SendAlertAsync(_testAlert, correlationId);

            // Assert
            Assert.That(channel.SentAlerts, Is.Empty, "Disabled channel should not send alerts");
            Assert.That(channel.AlertCalls.Count, Is.EqualTo(0), "Disabled channel should not increment call count");
            Assert.That(channel.IsEnabled, Is.False, "Channel should remain disabled");

            AssertNoErrors();
        }

        [Test]
        public async UniTask SendAlertAsync_WithSeverityBelowMinimum_DoesNotSend()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new TestAlertChannel("TestChannel");
            channel.MinimumSeverity = AlertSeverity.Critical;

            var lowSeverityAlert = Alert.Create(
                "Low severity alert",
                AlertSeverity.Info, // Below Error threshold
                TestConstants.TestSource,
                correlationId: correlationId);

            // Act
            await channel.SendAlertAsync(lowSeverityAlert, correlationId);

            // Assert
            Assert.That(channel.SentAlerts, Is.Empty, "Channel should not send alerts below minimum severity");
            Assert.That(channel.AlertCalls.Count, Is.EqualTo(0), "Alert call count should remain 0");
            Assert.That(channel.MinimumSeverity, Is.EqualTo(AlertSeverity.Critical), "Minimum severity should be preserved");

            AssertNoErrors();
        }

        [Test]
        public async UniTask SendAlertAsync_WhenUnhealthy_ThrowsException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new TestAlertChannel("TestChannel");
            channel.SetHealthy(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await channel.SendAlertAsync(_testAlert, correlationId));

            Assert.That(exception.Message, Does.Contain("unhealthy").IgnoreCase, "Exception should indicate unhealthy channel");
            Assert.That(channel.IsHealthy, Is.False, "Channel should remain unhealthy");
            Assert.That(channel.SentAlerts, Is.Empty, "Unhealthy channel should not send alerts");

            AssertNoErrors();
        }

        [Test]
        public async UniTask SendAlertAsync_WithCancellationToken_HandlesGracefully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(1)).Token;

            // Act & Assert
            await UniTask.Delay(10); // Ensure cancellation token is triggered
            await channel.SendAlertAsync(_testAlert, CreateTestCorrelationId(), cancellationToken);

            // Should complete without throwing (cancellation is handled internally)
        }

        [Test]
        public async UniTask FlushAsync_WithPendingAlerts_FlushesSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var correlationId = CreateTestCorrelationId();
            await channel.SendAlertAsync(_testAlert, correlationId);

            // Act & Assert - FlushAsync should complete without throwing
            await AssertGracefulFailureHandlingAsync(() => channel.FlushAsync(correlationId));

            // Verify channel remains healthy after flush
            Assert.That(channel.IsHealthy, Is.True, "Channel should remain healthy after flush");
            Assert.That(channel.IsEnabled, Is.True, "Channel should remain enabled after flush");
            AssertNoErrors();
        }

        [Test]
        public void EnableDisable_WithMethodCalls_UpdatesEnabledState()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act
            channel.Disable();

            // Assert
            Assert.That(channel.IsEnabled, Is.False);

            // Act
            channel.Enable();

            // Assert
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public void SetHealthy_WithBooleanValues_UpdatesHealthyState()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act
            channel.SetHealthy(false);

            // Assert
            Assert.That(channel.IsHealthy, Is.False);

            // Act
            channel.SetHealthy(true);

            // Assert
            Assert.That(channel.IsHealthy, Is.True);
        }

        [Test]
        public void MinimumSeverity_WithValidSeverity_UpdatesMinimumSeverity()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var newSeverity = AlertSeverity.Critical;

            // Act
            channel.MinimumSeverity = newSeverity;

            // Assert
            Assert.That(channel.MinimumSeverity, Is.EqualTo(newSeverity));
        }

        /// <summary>
        /// Tests channel performance under load following CLAUDETESTS.md frame budget compliance.
        /// Validates that alert sending completes within Unity's 16.67ms frame budget.
        /// </summary>
        [Test]
        public async UniTask SendAlertAsync_WithMultipleAlerts_CompletesWithinFrameBudget()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new TestAlertChannel("PerformanceTestChannel");
            const int alertCount = 10;

            // Act & Assert - Test frame budget compliance
            await AssertFrameBudgetComplianceAsync(
                async () =>
                {
                    for (int i = 0; i < alertCount; i++)
                    {
                        var alert = Alert.Create(
                            $"Performance test alert {i}",
                            AlertSeverity.Info,
                            TestConstants.TestSource,
                            correlationId: correlationId);
                        await channel.SendAlertAsync(alert, correlationId);
                    }
                },
                "MultipleAlertSending");

            // Verify all alerts were sent
            Assert.That(channel.SentAlerts.Count, Is.EqualTo(alertCount), $"Should have sent {alertCount} alerts");
            Assert.That(channel.AlertCalls.Count, Is.EqualTo(alertCount), $"Alert call count should be {alertCount}");

            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        /// <summary>
        /// Tests allocation behavior during alert channel operations for zero-allocation patterns.
        /// </summary>
        [Test]
        public async UniTask SendAlertAsync_WithUnityCollections_ProducesAcceptableAllocations()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new TestAlertChannel("AllocationTestChannel");

            // Act & Assert - Measure allocations
            await AssertAcceptableAllocationsAsync(
                async () =>
                {
                    await channel.SendAlertAsync(_testAlert, correlationId);
                },
                "SingleAlertSending",
                maxBytes: 2048); // Allow reasonable allocations for channel operations

            AssertNoErrors();
        }

        #endregion

        #region ConsoleAlertChannel Tests

        [Test]
        public void Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act
            var channel = new ConsoleAlertChannel(SpyMessageBus);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("ConsoleChannel"), "Channel name should be ConsoleChannel");
            Assert.That(channel.IsEnabled, Is.True, "Channel should be enabled by default");
            Assert.That(channel.IsHealthy, Is.True, "Channel should be healthy by default");

            AssertNoErrors();
        }

        [Test]
        public void Constructor_WithNullMessageBus_ThrowsArgumentNullException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new ConsoleAlertChannel(null));
            Assert.That(exception.ParamName, Does.Contain("messageBusService").IgnoreCase, "Exception should reference messageBusService parameter");

            AssertNoErrors();
        }

        [Test]
        public async UniTask SendAlertAsync_WithValidAlert_WritesToConsole()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var channel = new ConsoleAlertChannel(SpyMessageBus);

            // Act
            await channel.SendAlertAsync(_testAlert, correlationId);

            // Assert
            // Note: Actual console output is difficult to test directly.
            // The test verifies the method completes without throwing.
            Assert.That(channel.IsHealthy, Is.True, "Channel should remain healthy after sending");
            Assert.That(channel.IsEnabled, Is.True, "Channel should remain enabled after sending");

            // Verify correlation tracking
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        #endregion

        #region MemoryAlertChannel Tests

        [Test]
        public void Constructor_WithValidMemorySettings_InitializesCorrectly()
        {
            // Arrange
            var settings = new MemoryChannelSettings
            {
                MaxStoredAlerts = 1000
            };

            // Act
            var channel = new MemoryAlertChannel(settings, SpyMessageBus);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("MemoryChannel"));
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public async UniTask SendAlertAsync_WithValidAlert_StoresInMemory()
        {
            // Arrange
            var settings = new MemoryChannelSettings { MaxStoredAlerts = 10 };
            var channel = new MemoryAlertChannel(settings, SpyMessageBus);

            // Act
            await channel.SendAlertAsync(_testAlert, CreateTestCorrelationId());

            // Assert
            var storedAlerts = channel.StoredAlerts;
            Assert.That(storedAlerts, Is.Not.Empty);
            Assert.That(storedAlerts.First().Id, Is.EqualTo(_testAlert.Id));
        }

        [Test]
        public async UniTask SendAlertAsync_ExceedingCapacity_MaintainsCapacityLimit()
        {
            // Arrange
            var settings = new MemoryChannelSettings { MaxStoredAlerts = 2 };
            var channel = new MemoryAlertChannel(settings, SpyMessageBus);

            var alert1 = Alert.Create("Alert 1", AlertSeverity.Info, TestConstants.TestSource);
            var alert2 = Alert.Create("Alert 2", AlertSeverity.Warning, TestConstants.TestSource);
            var alert3 = Alert.Create("Alert 3", AlertSeverity.Critical, TestConstants.TestSource);

            // Act
            await channel.SendAlertAsync(alert1, CreateTestCorrelationId());
            await channel.SendAlertAsync(alert2, CreateTestCorrelationId());
            await channel.SendAlertAsync(alert3, CreateTestCorrelationId());

            // Assert
            var storedAlerts = channel.StoredAlerts;
            Assert.That(storedAlerts.Count(), Is.EqualTo(2)); // Should maintain capacity limit
        }

        [Test]
        public void Clear_WithStoredAlerts_RemovesAllStoredAlerts()
        {
            // Arrange
            var settings = new MemoryChannelSettings();
            var channel = new MemoryAlertChannel(settings, SpyMessageBus);

            // Add an alert first
            channel.SendAlertAsync(_testAlert, CreateTestCorrelationId()).GetAwaiter().GetResult();

            // Act
            channel.ResetStatistics();

            // Assert
            var storedAlerts = channel.StoredAlerts;
            Assert.That(storedAlerts, Is.Empty);
        }

        #endregion

        #region LogAlertChannel Tests

        [Test]
        public void Constructor_WithValidLogSettings_InitializesCorrectly()
        {
            // Arrange & Act
            var channel = new LogAlertChannel(StubLogging, SpyMessageBus);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("LogChannel"));
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public void Constructor_WithNullLoggingService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LogAlertChannel(null, SpyMessageBus));
        }

        [Test]
        public async UniTask SendAlertAsync_WithValidAlert_LogsToLoggingService()
        {
            // Arrange
            var channel = new LogAlertChannel(StubLogging, SpyMessageBus);

            // Act
            await channel.SendAlertAsync(_testAlert, CreateTestCorrelationId());

            // Assert
            Assert.That(StubLogging.RecordedLogs.Count, Is.GreaterThan(0));
            Assert.That(StubLogging.HasLogWithMessage(_testAlert.Message.ToString()), Is.True);
        }

        #endregion

        #region AlertChannelService Tests

        [Test]
        public void Constructor_WithValidConfiguration_InitializesCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId();
            var config = new AlertChannelServiceConfig();

            // Act
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);

            // Assert
            Assert.That(service.IsEnabled, Is.True, "Service should be enabled after construction");
            Assert.That(service.ChannelCount, Is.EqualTo(0), "Service should start with no registered channels");

            // Verify service integrates properly with test doubles
            AssertAllServicesHealthy();
            AssertNoErrors();
        }

        [Test]
        public async UniTask RegisterChannelAsync_WithValidChannel_RegistersSuccessfully()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            var channel = new TestAlertChannel("TestChannel");
            var correlationId = CreateTestCorrelationId();

            // Act
            await service.RegisterChannelAsync(channel, null, correlationId);

            // Assert
            Assert.That(service.ChannelCount, Is.EqualTo(1));
            Assert.That(service.GetAllChannels().Any(c => c.Name.ToString() == "TestChannel"), Is.True);
        }

        [Test]
        public async UniTask UnregisterChannelAsync_WithValidName_UnregistersSuccessfully()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            var channel = new TestAlertChannel("TestChannel");

            await service.RegisterChannelAsync(channel, null, CreateTestCorrelationId());

            // Act
            var result = await service.UnregisterChannelAsync("TestChannel", CreateTestCorrelationId());

            // Assert
            Assert.That(result, Is.True);
            Assert.That(service.ChannelCount, Is.EqualTo(0));
        }

        [Test]
        public async UniTask PerformHealthChecksAsync_WithRegisteredChannels_UpdatesChannelHealth()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            var channel = new TestAlertChannel("TestChannel");

            await service.RegisterChannelAsync(channel, null, CreateTestCorrelationId());

            // Act
            await service.PerformHealthChecksAsync(CreateTestCorrelationId());

            // Assert
            Assert.That(service.HealthyChannelCount, Is.EqualTo(1));
        }

        [Test]
        public void GetAllChannels_WithMultipleRegistered_ReturnsAllRegisteredChannels()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);

            var channel1 = new TestAlertChannel("Channel1");
            var channel2 = new TestAlertChannel("Channel2");

            service.RegisterChannelAsync(channel1, null, CreateTestCorrelationId()).GetAwaiter().GetResult();
            service.RegisterChannelAsync(channel2, null, CreateTestCorrelationId()).GetAwaiter().GetResult();

            // Act
            var allChannels = service.GetAllChannels();

            // Assert
            Assert.That(allChannels.Count(), Is.EqualTo(2));
            Assert.That(allChannels.Any(c => c.Name.ToString() == "Channel1"), Is.True);
            Assert.That(allChannels.Any(c => c.Name.ToString() == "Channel2"), Is.True);
        }

        #endregion

        #region Performance Stress Tests

        /// <summary>
        /// Tests channel service performance under heavy load following CLAUDETESTS.md stress testing patterns.
        /// </summary>
        [Test]
        public async UniTask RegisterChannelAsync_WithManyChannels_CompletesWithinFrameBudget()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            const int channelCount = 50;
            var correlationId = CreateTestCorrelationId();

            // Act & Assert - Test frame budget compliance
            await AssertFrameBudgetComplianceAsync(
                async () =>
                {
                    for (int i = 0; i < channelCount; i++)
                    {
                        var channel = new TestAlertChannel($"Channel_{i}");
                        await service.RegisterChannelAsync(channel, null, correlationId);
                    }
                },
                "ManyChannelRegistration");

            // Verify all channels were registered
            Assert.That(service.ChannelCount, Is.EqualTo(channelCount));
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        /// <summary>
        /// Tests concurrent alert sending performance and allocation patterns.
        /// </summary>
        [Test]
        public async UniTask SendAlertAsync_ConcurrentOperations_MaintainsPerformance()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            var channel = new TestAlertChannel("ConcurrentTestChannel");
            await service.RegisterChannelAsync(channel, null, CreateTestCorrelationId());

            const int concurrentAlerts = 20;
            var correlationId = CreateTestCorrelationId();

            // Act & Assert - Test concurrent performance
            await AssertAcceptableAllocationsAsync(
                async () =>
                {
                    var tasks = new UniTask[concurrentAlerts];
                    for (int i = 0; i < concurrentAlerts; i++)
                    {
                        var alert = Alert.Create($"Concurrent alert {i}", AlertSeverity.Info, TestConstants.TestSource, correlationId: correlationId);
                        tasks[i] = channel.SendAlertAsync(alert, correlationId);
                    }
                    await UniTask.WhenAll(tasks);
                },
                "ConcurrentAlertSending",
                maxBytes: 4096); // Allow reasonable allocations for concurrent operations

            Assert.That(channel.AlertCalls.Count, Is.EqualTo(concurrentAlerts), "All concurrent alerts should be recorded");
            AssertNoErrors();
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async UniTask AlertChannel_SendAlertAsync_WithNullAlert_DoesNotThrow()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act & Assert - Should handle gracefully
            await AssertGracefulFailureHandlingAsync(() => channel.SendAlertAsync(null, CreateTestCorrelationId()));
            AssertNoErrors();
        }

        [Test]
        public void RegisterChannelAsync_WithNullChannel_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await service.RegisterChannelAsync(null, null, CreateTestCorrelationId()));
        }

        [Test]
        public async UniTask UnregisterChannelAsync_WithNonExistentChannel_ReturnsFalse()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);

            // Act
            var result = await service.UnregisterChannelAsync("NonExistentChannel", CreateTestCorrelationId());

            // Assert
            Assert.That(result, Is.False);
            AssertNoErrors();
        }

        /// <summary>
        /// Tests edge case handling with invalid correlation IDs following CLAUDETESTS.md error handling patterns.
        /// </summary>
        [Test]
        public async UniTask SendAlertAsync_WithEmptyCorrelationId_HandlesGracefully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act & Assert - Should handle empty correlation ID gracefully
            await AssertGracefulFailureHandlingAsync(() => channel.SendAlertAsync(_testAlert, Guid.Empty));
            AssertNoErrors();
        }

        /// <summary>
        /// Tests service resilience when all channels are disabled.
        /// </summary>
        [Test]
        public async UniTask AlertChannelService_WithAllChannelsDisabled_HandlesGracefully()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);

            var channel1 = new TestAlertChannel("Channel1");
            var channel2 = new TestAlertChannel("Channel2");
            channel1.Disable();
            channel2.Disable();

            await service.RegisterChannelAsync(channel1, null, CreateTestCorrelationId());
            await service.RegisterChannelAsync(channel2, null, CreateTestCorrelationId());

            // Act & Assert - Health check should handle disabled channels
            await AssertGracefulFailureHandlingAsync(() => service.PerformHealthChecksAsync(CreateTestCorrelationId()));

            Assert.That(service.HealthyChannelCount, Is.EqualTo(0), "No channels should be healthy when all are disabled");
            AssertNoErrors();
        }

        /// <summary>
        /// Tests channel behavior with extreme severity values.
        /// </summary>
        [Test]
        public async UniTask SendAlertAsync_WithExtremeSeverityValues_HandlesCorrectly()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            channel.MinimumSeverity = AlertSeverity.Critical;

            var debugAlert = Alert.Create("Debug message", AlertSeverity.Debug, TestConstants.TestSource);
            var criticalAlert = Alert.Create("Critical message", AlertSeverity.Critical, TestConstants.TestSource);

            // Act
            await channel.SendAlertAsync(debugAlert, CreateTestCorrelationId());
            await channel.SendAlertAsync(criticalAlert, CreateTestCorrelationId());

            // Assert
            Assert.That(channel.SentAlerts.Count, Is.EqualTo(1), "Only critical alert should be sent");
            Assert.That(channel.SentAlerts.First().Severity, Is.EqualTo(AlertSeverity.Critical));
            AssertNoErrors();
        }

        /// <summary>
        /// Tests memory channel behavior at capacity limit edge cases.
        /// </summary>
        [Test]
        public async UniTask MemoryChannel_AtExactCapacity_HandlesCorrectly()
        {
            // Arrange
            var settings = new MemoryChannelSettings { MaxStoredAlerts = 1 };
            var channel = new MemoryAlertChannel(settings, SpyMessageBus);

            var alert1 = Alert.Create("Alert 1", AlertSeverity.Info, TestConstants.TestSource);
            var alert2 = Alert.Create("Alert 2", AlertSeverity.Warning, TestConstants.TestSource);

            // Act
            await channel.SendAlertAsync(alert1, CreateTestCorrelationId());
            var storedBefore = channel.StoredAlerts.Count();

            await channel.SendAlertAsync(alert2, CreateTestCorrelationId());
            var storedAfter = channel.StoredAlerts.Count();

            // Assert
            Assert.That(storedBefore, Is.EqualTo(1), "Should store exactly one alert before capacity exceeded");
            Assert.That(storedAfter, Is.EqualTo(1), "Should maintain exactly one alert after capacity exceeded");
            Assert.That(channel.StoredAlerts.First().Id, Is.EqualTo(alert2.Id), "Should keep most recent alert");
        }

        /// <summary>
        /// Tests service behavior with rapid channel registration/unregistration.
        /// </summary>
        [Test]
        public async UniTask AlertChannelService_RapidRegisterUnregister_MaintainsConsistency()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            var correlationId = CreateTestCorrelationId();

            // Act - Rapid register/unregister cycle
            for (int i = 0; i < 10; i++)
            {
                var channel = new TestAlertChannel($"RapidChannel_{i}");
                await service.RegisterChannelAsync(channel, null, correlationId);

                if (i % 2 == 0) // Unregister every other channel
                {
                    await service.UnregisterChannelAsync($"RapidChannel_{i}", correlationId);
                }
            }

            // Assert
            Assert.That(service.ChannelCount, Is.EqualTo(5), "Should have 5 channels remaining after rapid operations");
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Dispose_WithRegisteredChannels_DisposesAllChannels()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, StubLogging, SpyMessageBus);
            var channel = new TestAlertChannel("TestChannel");

            service.RegisterChannelAsync(channel, null, CreateTestCorrelationId()).GetAwaiter().GetResult();

            // Act
            service.Dispose();

            // Assert
            Assert.That(service.IsEnabled, Is.False);
            Assert.That(service.ChannelCount, Is.EqualTo(0));
        }

        #endregion
    }
}