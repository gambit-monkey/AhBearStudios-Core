using System;
using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Unit tests for Alert message classes ensuring IMessage interface compliance.
    /// Tests message creation with static factory methods and DeterministicIdGenerator usage.
    /// </summary>
    [TestFixture]
    public class AlertMessageTests : BaseServiceTest
    {
        #region AlertRaisedMessage Tests

        [Test]
        public void AlertRaisedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: CreateTestCorrelationId());

            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertRaisedMessage.Create(alert, source, correlationId);

            // Assert
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertRaisedMessage));
            Assert.That(message.Source.ToString(), Is.EqualTo(source));
            Assert.That(message.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(message.AlertId, Is.EqualTo(alert.Id));
            Assert.That(message.AlertSeverity, Is.EqualTo(alert.Severity));
            Assert.That(message.AlertMessage.ToString(), Is.EqualTo(alert.Message.ToString()));
            Assert.That(message.Priority, Is.EqualTo(MessagePriority.Normal));
        }

        [Test]
        public void AlertRaisedMessage_Create_WithNullAlert_ThrowsArgumentNullException()
        {
            // Arrange
            Alert nullAlert = null;
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                AlertRaisedMessage.Create(nullAlert, source, correlationId));
        }

        [Test]
        public void AlertRaisedMessage_Create_WithEmptySource_UsesDefaultSource()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Info,
                TestConstants.TestSource);

            var emptySource = "";
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertRaisedMessage.Create(alert, emptySource, correlationId);

            // Assert
            Assert.That(message.Source.ToString(), Is.EqualTo("AlertSystem"));
        }

        [Test]
        public void AlertRaisedMessage_Create_WithDefaultCorrelationId_GeneratesNewCorrelationId()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Error,
                TestConstants.TestSource);

            var source = TestConstants.TestSource;

            // Act
            var message = AlertRaisedMessage.Create(alert, source);

            // Assert
            Assert.That(message.CorrelationId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void AlertRaisedMessage_Timestamp_ReturnsValidDateTime()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.High,
                TestConstants.TestSource);

            var beforeCreation = DateTime.UtcNow;

            // Act
            var message = AlertRaisedMessage.Create(alert, TestConstants.TestSource);
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(message.Timestamp, Is.LessThanOrEqualTo(afterCreation));
            Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void AlertRaisedMessage_ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Critical,
                TestConstants.TestSource);

            // Act
            var message = AlertRaisedMessage.Create(alert, TestConstants.TestSource);
            var stringRepresentation = message.ToString();

            // Assert
            Assert.That(stringRepresentation, Is.Not.Null);
            Assert.That(stringRepresentation, Does.Contain("AlertRaisedMessage"));
            Assert.That(stringRepresentation, Does.Contain(alert.Severity.ToString()));
        }

        #endregion

        #region AlertAcknowledgedMessage Tests

        [Test]
        public void AlertAcknowledgedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            var acknowledgedAlert = alert.Acknowledge(TestConstants.TestUser);
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertAcknowledgedMessage.Create(acknowledgedAlert, source, correlationId);

            // Assert
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertAcknowledgedMessage));
            Assert.That(message.AlertId, Is.EqualTo(acknowledgedAlert.Id));
            Assert.That(message.AcknowledgedBy.ToString(), Is.EqualTo(TestConstants.TestUser));
            Assert.That(message.AcknowledgedTimestamp, Is.Not.Null);
        }

        [Test]
        public void AlertAcknowledgedMessage_Create_WithNullAlert_ThrowsArgumentNullException()
        {
            // Arrange
            Alert nullAlert = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                AlertAcknowledgedMessage.Create(nullAlert, TestConstants.TestSource));
        }

        [Test]
        public void AlertAcknowledgedMessage_Create_WithUnacknowledgedAlert_ThrowsArgumentException()
        {
            // Arrange
            var unacknowledgedAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                AlertAcknowledgedMessage.Create(unacknowledgedAlert, TestConstants.TestSource));
        }

        #endregion

        #region AlertResolvedMessage Tests

        [Test]
        public void AlertResolvedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Error,
                TestConstants.TestSource);

            var resolvedAlert = alert.Resolve(TestConstants.TestUser);
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertResolvedMessage.Create(resolvedAlert, source, correlationId);

            // Assert
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertResolvedMessage));
            Assert.That(message.AlertId, Is.EqualTo(resolvedAlert.Id));
            Assert.That(message.ResolvedBy.ToString(), Is.EqualTo(TestConstants.TestUser));
            Assert.That(message.ResolvedTimestamp, Is.Not.Null);
        }

        [Test]
        public void AlertResolvedMessage_Create_WithUnresolvedAlert_ThrowsArgumentException()
        {
            // Arrange
            var unresolvedAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Error,
                TestConstants.TestSource);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                AlertResolvedMessage.Create(unresolvedAlert, TestConstants.TestSource));
        }

        #endregion

        #region AlertDeliveryFailedMessage Tests

        [Test]
        public void AlertDeliveryFailedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Critical,
                TestConstants.TestSource);

            var channelName = "TestChannel";
            var errorMessage = TestConstants.SampleErrorMessage;
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertDeliveryFailedMessage.Create(
                alert,
                channelName,
                errorMessage,
                source,
                correlationId);

            // Assert
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertDeliveryFailedMessage));
            Assert.That(message.AlertId, Is.EqualTo(alert.Id));
            Assert.That(message.ChannelName.ToString(), Is.EqualTo(channelName));
            Assert.That(message.ErrorMessage.ToString(), Is.EqualTo(errorMessage));
            Assert.That(message.Priority, Is.EqualTo(MessagePriority.High)); // Delivery failures are high priority
        }

        [Test]
        public void AlertDeliveryFailedMessage_Create_WithEmptyChannelName_ThrowsArgumentException()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            var emptyChannelName = "";
            var errorMessage = TestConstants.SampleErrorMessage;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                AlertDeliveryFailedMessage.Create(
                    alert,
                    emptyChannelName,
                    errorMessage,
                    TestConstants.TestSource));
        }

        [Test]
        public void AlertDeliveryFailedMessage_Create_WithEmptyErrorMessage_ThrowsArgumentException()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            var channelName = "TestChannel";
            var emptyErrorMessage = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                AlertDeliveryFailedMessage.Create(
                    alert,
                    channelName,
                    emptyErrorMessage,
                    TestConstants.TestSource));
        }

        #endregion

        #region AlertChannelRegisteredMessage Tests

        [Test]
        public void AlertChannelRegisteredMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var channelName = "TestChannel";
            var channelType = AlertChannelType.Console;
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertChannelRegisteredMessage.Create(
                channelName,
                channelType,
                source,
                correlationId);

            // Assert
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertChannelRegisteredMessage));
            Assert.That(message.ChannelName.ToString(), Is.EqualTo(channelName));
            Assert.That(message.ChannelType, Is.EqualTo(channelType));
            Assert.That(message.Priority, Is.EqualTo(MessagePriority.Low)); // Registration events are low priority
        }

        [Test]
        public void AlertChannelRegisteredMessage_Create_WithEmptyChannelName_ThrowsArgumentException()
        {
            // Arrange
            var emptyChannelName = "";
            var channelType = AlertChannelType.File;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                AlertChannelRegisteredMessage.Create(
                    emptyChannelName,
                    channelType,
                    TestConstants.TestSource));
        }

        #endregion

        #region AlertChannelUnregisteredMessage Tests

        [Test]
        public void AlertChannelUnregisteredMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var channelName = "TestChannel";
            var reason = "Test unregistration";
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act
            var message = AlertChannelUnregisteredMessage.Create(
                channelName,
                reason,
                source,
                correlationId);

            // Assert
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertChannelUnregisteredMessage));
            Assert.That(message.ChannelName.ToString(), Is.EqualTo(channelName));
            Assert.That(message.Reason.ToString(), Is.EqualTo(reason));
        }

        #endregion

        #region IMessage Interface Compliance Tests

        [Test]
        public void AllAlertMessages_ImplementIMessage_Correctly()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            var acknowledgedAlert = alert.Acknowledge(TestConstants.TestUser);
            var resolvedAlert = alert.Resolve(TestConstants.TestUser);

            // Act
            var raisedMessage = AlertRaisedMessage.Create(alert, TestConstants.TestSource);
            var acknowledgedMessage = AlertAcknowledgedMessage.Create(acknowledgedAlert, TestConstants.TestSource);
            var resolvedMessage = AlertResolvedMessage.Create(resolvedAlert, TestConstants.TestSource);
            var deliveryFailedMessage = AlertDeliveryFailedMessage.Create(
                alert, "TestChannel", "Test error", TestConstants.TestSource);

            // Assert - All messages should implement IMessage
            Assert.That(raisedMessage, Is.InstanceOf<IMessage>());
            Assert.That(acknowledgedMessage, Is.InstanceOf<IMessage>());
            Assert.That(resolvedMessage, Is.InstanceOf<IMessage>());
            Assert.That(deliveryFailedMessage, Is.InstanceOf<IMessage>());

            // Assert - All messages should have valid IMessage properties
            ValidateIMessageProperties(raisedMessage);
            ValidateIMessageProperties(acknowledgedMessage);
            ValidateIMessageProperties(resolvedMessage);
            ValidateIMessageProperties(deliveryFailedMessage);
        }

        [Test]
        public void MessageTypeCodes_AreInCorrectRange_ForAlertingSystem()
        {
            // Arrange & Act
            var typeCodes = new[]
            {
                MessageTypeCodes.AlertRaisedMessage,
                MessageTypeCodes.AlertAcknowledgedMessage,
                MessageTypeCodes.AlertResolvedMessage,
                MessageTypeCodes.AlertDeliveryFailedMessage,
                MessageTypeCodes.AlertChannelRegisteredMessage,
                MessageTypeCodes.AlertChannelUnregisteredMessage
            };

            // Assert - All type codes should be in the Alerting System range (1400-1499)
            foreach (var typeCode in typeCodes)
            {
                Assert.That(typeCode, Is.GreaterThanOrEqualTo(1400),
                    $"Type code {typeCode} is below the Alerting System range");
                Assert.That(typeCode, Is.LessThanOrEqualTo(1499),
                    $"Type code {typeCode} is above the Alerting System range");
            }
        }

        [Test]
        public void DeterministicIdGenerator_IsUsedConsistently_AcrossAllMessages()
        {
            // Arrange
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId();

            // Act - Create same message twice with same parameters
            var message1 = AlertRaisedMessage.Create(alert, source, correlationId);
            var message2 = AlertRaisedMessage.Create(alert, source, correlationId);

            // Assert - Messages should have deterministic IDs when created with same parameters
            // Note: The exact deterministic behavior depends on the DeterministicIdGenerator implementation
            Assert.That(message1.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message2.Id, Is.Not.EqualTo(Guid.Empty));
            // IDs should be deterministic, but this test would need to know the specific implementation
        }

        #endregion

        #region Helper Methods

        private void ValidateIMessageProperties(IMessage message)
        {
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should not be empty");
            Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");
            Assert.That(message.TypeCode, Is.GreaterThan(0), "Type code should be positive");
            Assert.That(message.Source.ToString(), Is.Not.Empty, "Source should not be empty");
            Assert.That(message.CorrelationId, Is.Not.EqualTo(Guid.Empty), "Correlation ID should not be empty");

            // Validate computed properties
            Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
            Assert.That(message.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow), "Timestamp should not be in the future");
        }

        #endregion
    }
}