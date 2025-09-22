using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;
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
    /// Unit tests for alert channels testing delivery mechanisms, health status tracking,
    /// and channel service management functionality.
    /// </summary>
    [TestFixture]
    public class AlertChannelTests : BaseServiceTest
    {
        private Alert _testAlert;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _testAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: CreateTestCorrelationId());
        }

        #region TestAlertChannel Tests

        [Test]
        public void TestAlertChannel_Constructor_WithValidName_InitializesCorrectly()
        {
            // Arrange & Act
            var channel = new TestAlertChannel("TestChannel");

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("TestChannel"));
            Assert.That(channel.IsEnabled, Is.True);
            Assert.That(channel.IsHealthy, Is.True);
            Assert.That(channel.MinimumSeverity, Is.EqualTo(AlertSeverity.Debug));
        }

        [Test]
        public void TestAlertChannel_Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new TestAlertChannel(""));
        }

        [Test]
        public async Task TestAlertChannel_SendAlertAsync_WithValidAlert_SendsSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var correlationId = CreateTestCorrelationId();

            // Act
            await channel.SendAlertAsync(_testAlert, correlationId);

            // Assert
            Assert.That(channel.SentAlerts, Is.Not.Empty);
            Assert.That(channel.SentAlerts.First().Id, Is.EqualTo(_testAlert.Id));
            Assert.That(channel.SendCallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task TestAlertChannel_SendAlertAsync_WhenDisabled_DoesNotSend()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            channel.SetEnabled(false);

            // Act
            await channel.SendAlertAsync(_testAlert, Guid.NewGuid());

            // Assert
            Assert.That(channel.SentAlerts, Is.Empty);
            Assert.That(channel.SendCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task TestAlertChannel_SendAlertAsync_WithSeverityBelowMinimum_DoesNotSend()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            channel.SetMinimumSeverity(AlertSeverity.Error);

            var lowSeverityAlert = Alert.Create(
                "Low severity alert",
                AlertSeverity.Info, // Below Error threshold
                TestConstants.TestSource);

            // Act
            await channel.SendAlertAsync(lowSeverityAlert, Guid.NewGuid());

            // Assert
            Assert.That(channel.SentAlerts, Is.Empty);
        }

        [Test]
        public async Task TestAlertChannel_SendAlertAsync_WhenUnhealthy_ThrowsException()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            channel.SetHealthy(false);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                channel.SendAlertAsync(_testAlert, Guid.NewGuid()).AsTask());
        }

        [Test]
        public async Task TestAlertChannel_SendAlertAsync_WithCancellation_HandlesGracefully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(1)).Token;

            // Act & Assert
            await UniTask.Delay(10); // Ensure cancellation token is triggered
            await channel.SendAlertAsync(_testAlert, Guid.NewGuid(), cancellationToken);

            // Should complete without throwing (cancellation is handled internally)
        }

        [Test]
        public async Task TestAlertChannel_FlushAsync_WithPendingAlerts_FlushesSuccessfully()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            await channel.SendAlertAsync(_testAlert, Guid.NewGuid());

            // Act
            await channel.FlushAsync(Guid.NewGuid());

            // Assert
            Assert.That(channel.FlushCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TestAlertChannel_SetEnabled_UpdatesEnabledState()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act
            channel.SetEnabled(false);

            // Assert
            Assert.That(channel.IsEnabled, Is.False);

            // Act
            channel.SetEnabled(true);

            // Assert
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public void TestAlertChannel_SetHealthy_UpdatesHealthyState()
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
        public void TestAlertChannel_SetMinimumSeverity_UpdatesMinimumSeverity()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");
            var newSeverity = AlertSeverity.Critical;

            // Act
            channel.SetMinimumSeverity(newSeverity);

            // Assert
            Assert.That(channel.MinimumSeverity, Is.EqualTo(newSeverity));
        }

        #endregion

        #region ConsoleAlertChannel Tests

        [Test]
        public void ConsoleAlertChannel_Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange
            var settings = new ConsoleChannelSettings
            {
                Name = "ConsoleChannel",
                MinimumSeverity = AlertSeverity.Warning
            };

            // Act
            var channel = new ConsoleAlertChannel(settings);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("ConsoleChannel"));
            Assert.That(channel.MinimumSeverity, Is.EqualTo(AlertSeverity.Warning));
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public void ConsoleAlertChannel_Constructor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ConsoleAlertChannel(null));
        }

        [Test]
        public async Task ConsoleAlertChannel_SendAlertAsync_WithValidAlert_WritesToConsole()
        {
            // Arrange
            var settings = new ConsoleChannelSettings { Name = "Console" };
            var channel = new ConsoleAlertChannel(settings);

            // Act
            await channel.SendAlertAsync(_testAlert, Guid.NewGuid());

            // Assert
            // Note: Actual console output is difficult to test directly.
            // The test verifies the method completes without throwing.
            Assert.That(channel.IsHealthy, Is.True);
        }

        #endregion

        #region MemoryAlertChannel Tests

        [Test]
        public void MemoryAlertChannel_Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange
            var settings = new MemoryChannelSettings
            {
                MaxStoredAlerts = 1000
            };

            // Act
            var channel = new MemoryAlertChannel(settings);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("MemoryChannel"));
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public async Task MemoryAlertChannel_SendAlertAsync_WithValidAlert_StoresInMemory()
        {
            // Arrange
            var settings = new MemoryChannelSettings { MaxStoredAlerts = 10 };
            var channel = new MemoryAlertChannel(settings);

            // Act
            await channel.SendAlertAsync(_testAlert, Guid.NewGuid());

            // Assert
            var storedAlerts = channel.GetStoredAlerts();
            Assert.That(storedAlerts, Is.Not.Empty);
            Assert.That(storedAlerts.First().Id, Is.EqualTo(_testAlert.Id));
        }

        [Test]
        public async Task MemoryAlertChannel_SendAlertAsync_ExceedingCapacity_MaintainsCapacityLimit()
        {
            // Arrange
            var settings = new MemoryChannelSettings { MaxStoredAlerts = 2 };
            var channel = new MemoryAlertChannel(settings);

            var alert1 = Alert.Create("Alert 1", AlertSeverity.Info, TestConstants.TestSource);
            var alert2 = Alert.Create("Alert 2", AlertSeverity.Warning, TestConstants.TestSource);
            var alert3 = Alert.Create("Alert 3", AlertSeverity.Error, TestConstants.TestSource);

            // Act
            await channel.SendAlertAsync(alert1, Guid.NewGuid());
            await channel.SendAlertAsync(alert2, Guid.NewGuid());
            await channel.SendAlertAsync(alert3, Guid.NewGuid());

            // Assert
            var storedAlerts = channel.GetStoredAlerts();
            Assert.That(storedAlerts.Count(), Is.EqualTo(2)); // Should maintain capacity limit
        }

        [Test]
        public void MemoryAlertChannel_Clear_RemovesAllStoredAlerts()
        {
            // Arrange
            var settings = new MemoryChannelSettings();
            var channel = new MemoryAlertChannel(settings);

            // Add an alert first
            channel.SendAlertAsync(_testAlert, Guid.NewGuid()).GetAwaiter().GetResult();

            // Act
            channel.Clear();

            // Assert
            var storedAlerts = channel.GetStoredAlerts();
            Assert.That(storedAlerts, Is.Empty);
        }

        #endregion

        #region FileAlertChannel Tests

        [Test]
        public void FileAlertChannel_Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange
            var settings = new FileChannelSettings
            {
                Name = "FileChannel",
                FilePath = "/tmp/test-alerts.log"
            };

            // Act
            var channel = new FileAlertChannel(settings);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("FileChannel"));
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public void FileAlertChannel_Constructor_WithEmptyFilePath_ThrowsArgumentException()
        {
            // Arrange
            var settings = new FileChannelSettings
            {
                Name = "FileChannel",
                FilePath = ""
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FileAlertChannel(settings));
        }

        [Test]
        public async Task FileAlertChannel_SendAlertAsync_WithValidAlert_WritesToFile()
        {
            // Arrange
            var tempFilePath = System.IO.Path.GetTempFileName();
            var settings = new FileChannelSettings
            {
                Name = "FileChannel",
                FilePath = tempFilePath
            };

            var channel = new FileAlertChannel(settings);

            try
            {
                // Act
                await channel.SendAlertAsync(_testAlert, Guid.NewGuid());

                // Assert
                Assert.That(System.IO.File.Exists(tempFilePath), Is.True);
                var fileContent = await System.IO.File.ReadAllTextAsync(tempFilePath);
                Assert.That(fileContent, Does.Contain(_testAlert.Message.ToString()));
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
                channel.Dispose();
            }
        }

        #endregion

        #region LogAlertChannel Tests

        [Test]
        public void LogAlertChannel_Constructor_WithValidSettings_InitializesCorrectly()
        {
            // Arrange
            var settings = new LogChannelSettings
            {
                Name = "LogChannel"
            };

            // Act
            var channel = new LogAlertChannel(settings, MockLogging);

            // Assert
            Assert.That(channel.Name.ToString(), Is.EqualTo("LogChannel"));
            Assert.That(channel.IsEnabled, Is.True);
        }

        [Test]
        public void LogAlertChannel_Constructor_WithNullLoggingService_ThrowsArgumentNullException()
        {
            // Arrange
            var settings = new LogChannelSettings { Name = "LogChannel" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LogAlertChannel(settings, null));
        }

        [Test]
        public async Task LogAlertChannel_SendAlertAsync_WithValidAlert_LogsToLoggingService()
        {
            // Arrange
            var settings = new LogChannelSettings { Name = "LogChannel" };
            var channel = new LogAlertChannel(settings, MockLogging);

            // Act
            await channel.SendAlertAsync(_testAlert, Guid.NewGuid());

            // Assert
            Assert.That(MockLogging.CallCount, Is.GreaterThan(0));
            Assert.That(MockLogging.HasLogWithMessage(_testAlert.Message.ToString()), Is.True);
        }

        #endregion

        #region NullAlertChannel Tests

        [Test]
        public void NullAlertChannel_Instance_IsHealthyAndEnabled()
        {
            // Arrange & Act
            var channel = NullAlertChannel.Instance;

            // Assert
            Assert.That(channel.IsEnabled, Is.True);
            Assert.That(channel.IsHealthy, Is.True);
            Assert.That(channel.Name.ToString(), Is.EqualTo("NullChannel"));
        }

        [Test]
        public async Task NullAlertChannel_SendAlertAsync_DoesNothing()
        {
            // Arrange
            var channel = NullAlertChannel.Instance;

            // Act & Assert
            Assert.DoesNotThrowAsync(() => channel.SendAlertAsync(_testAlert, Guid.NewGuid()).AsTask());
        }

        [Test]
        public async Task NullAlertChannel_FlushAsync_DoesNothing()
        {
            // Arrange
            var channel = NullAlertChannel.Instance;

            // Act & Assert
            Assert.DoesNotThrowAsync(() => channel.FlushAsync(Guid.NewGuid()).AsTask());
        }

        #endregion

        #region AlertChannelService Tests

        [Test]
        public void AlertChannelService_Constructor_WithValidConfiguration_InitializesCorrectly()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();

            // Act
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);

            // Assert
            Assert.That(service.IsEnabled, Is.True);
            Assert.That(service.RegisteredChannelCount, Is.EqualTo(0));
        }

        [Test]
        public async Task AlertChannelService_RegisterChannelAsync_WithValidChannel_RegistersSuccessfully()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);
            var channel = new TestAlertChannel("TestChannel");
            var correlationId = CreateTestCorrelationId();

            // Act
            await service.RegisterChannelAsync(channel, null, correlationId);

            // Assert
            Assert.That(service.RegisteredChannelCount, Is.EqualTo(1));
            Assert.That(service.GetAllChannels().Any(c => c.Name.ToString() == "TestChannel"), Is.True);
        }

        [Test]
        public async Task AlertChannelService_UnregisterChannelAsync_WithValidName_UnregistersSuccessfully()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);
            var channel = new TestAlertChannel("TestChannel");

            await service.RegisterChannelAsync(channel, null, CreateTestCorrelationId());

            // Act
            var result = await service.UnregisterChannelAsync("TestChannel", CreateTestCorrelationId());

            // Assert
            Assert.That(result, Is.True);
            Assert.That(service.RegisteredChannelCount, Is.EqualTo(0));
        }

        [Test]
        public async Task AlertChannelService_PerformHealthChecksAsync_UpdatesChannelHealth()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);
            var channel = new TestAlertChannel("TestChannel");

            await service.RegisterChannelAsync(channel, null, CreateTestCorrelationId());

            // Act
            await service.PerformHealthChecksAsync(CreateTestCorrelationId());

            // Assert
            Assert.That(service.HealthyChannelCount, Is.EqualTo(1));
        }

        [Test]
        public void AlertChannelService_GetAllChannels_ReturnsAllRegisteredChannels()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);

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

        #region Error Handling Tests

        [Test]
        public async Task AlertChannel_SendAlertAsync_WithNullAlert_DoesNotThrow()
        {
            // Arrange
            var channel = new TestAlertChannel("TestChannel");

            // Act & Assert
            Assert.DoesNotThrowAsync(() => channel.SendAlertAsync(null, Guid.NewGuid()).AsTask());
        }

        [Test]
        public void AlertChannelService_RegisterChannelAsync_WithNullChannel_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.RegisterChannelAsync(null, null, Guid.NewGuid()).AsTask());
        }

        [Test]
        public async Task AlertChannelService_UnregisterChannelAsync_WithNonExistentChannel_ReturnsFalse()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);

            // Act
            var result = await service.UnregisterChannelAsync("NonExistentChannel", Guid.NewGuid());

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void AlertChannelService_Dispose_DisposesAllChannels()
        {
            // Arrange
            var config = new AlertChannelServiceConfig();
            var service = new AlertChannelService(config, MockLogging, MockMessageBus);
            var channel = new TestAlertChannel("TestChannel");

            service.RegisterChannelAsync(channel, null, CreateTestCorrelationId()).GetAwaiter().GetResult();

            // Act
            service.Dispose();

            // Assert
            Assert.That(service.IsEnabled, Is.False);
            Assert.That(service.RegisteredChannelCount, Is.EqualTo(0));
        }

        #endregion
    }
}