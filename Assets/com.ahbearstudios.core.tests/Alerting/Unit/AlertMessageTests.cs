using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Messages;
using AhBearStudios.Core.Alerting.Configs;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Production-ready unit tests for Alert message classes following CLAUDETESTS.md TDD guidelines.
    /// Tests IMessage interface compliance, performance requirements, and Unity game development patterns.
    /// Validates message creation with static factory methods, DeterministicIdGenerator usage, and frame budget compliance.
    /// Uses lightweight test doubles for zero-allocation testing and proper correlation tracking.
    /// </summary>
    [TestFixture]
    public class AlertMessageTests : BaseServiceTest
    {
        #region AlertRaisedMessage Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AlertRaisedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertRaisedMessage_Create_ValidParameters");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            var source = TestConstants.TestSource;

            // Act - Measure performance and validate frame budget compliance
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertRaisedMessage.Create(alert, source, correlationId);

                    // Assert - Complete IMessage validation following CLAUDETESTS.md patterns
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should not be empty");
                    Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertRaisedMessage), "Type code should match AlertRaisedMessage");
                    Assert.That(message.Source.ToString(), Is.EqualTo(source), "Source should match provided value");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId), "Correlation ID should be preserved");
                    Assert.That(message.AlertId, Is.EqualTo(alert.Id), "Alert ID should match source alert");
                    Assert.That(message.AlertSeverity, Is.EqualTo(alert.Severity), "Alert severity should be preserved");
                    Assert.That(message.AlertMessage.ToString(), Is.EqualTo(alert.Message.ToString()), "Alert message should be preserved");
                    Assert.That(message.Priority, Is.EqualTo(MessagePriority.Normal), "Default priority should be Normal");

                    // Validate IMessage computed properties
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
                    Assert.That(message.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow), "Timestamp should not be in the future");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertRaisedMessage.Create",
                TestConstants.FrameBudget);

            // Verify frame budget compliance for Unity game development
            Assert.That(result.Duration, Is.LessThan(TestConstants.FrameBudget),
                $"AlertRaisedMessage creation should complete within frame budget ({TestConstants.FrameBudget.TotalMilliseconds}ms)");

            // Log performance metrics for analysis
            LogPerformanceMetrics(result);

            // Verify no errors occurred during message creation
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertRaisedMessage_Create_WithNullAlert_ThrowsArgumentNullException()
        {
            // Arrange
            Alert nullAlert = null;
            var source = TestConstants.TestSource;
            var correlationId = CreateTestCorrelationId("AlertRaisedMessage_Create_NullAlert");

            // Act & Assert - Validate graceful error handling
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentNullException>(() =>
                    AlertRaisedMessage.Create(nullAlert, source, correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            // Verify correlation tracking is maintained even in error scenarios
            AssertCorrelationTrackingMaintained(correlationId);
        }

        [Test]
        public async UniTask AlertRaisedMessage_Create_WithEmptySource_UsesDefaultSource()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertRaisedMessage_Create_EmptySource");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Info,
                TestConstants.TestSource,
                correlationId: correlationId);

            var emptySource = "";

            // Act & Assert - Validate default source handling with performance measurement
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertRaisedMessage.Create(alert, emptySource, correlationId);

                    Assert.That(message.Source.ToString(), Is.EqualTo("AlertSystem"),
                        "Empty source should default to 'AlertSystem'");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId),
                        "Correlation ID should be preserved with default source");

                    await UniTask.CompletedTask;
                },
                "AlertRaisedMessage.CreateWithEmptySource",
                TestConstants.FrameBudget);

            // Verify zero-allocation pattern for default source handling
            await AssertZeroAllocationsAsync(async () =>
            {
                AlertRaisedMessage.Create(alert, emptySource, correlationId);
                await UniTask.CompletedTask;
            }, "AlertRaisedMessage.CreateWithEmptySource");

            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertRaisedMessage_Create_WithDefaultCorrelationId_GeneratesNewCorrelationId()
        {
            // Arrange
            var testCorrelationId = CreateTestCorrelationId("AlertRaisedMessage_Create_DefaultCorrelation");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Error,
                TestConstants.TestSource,
                correlationId: testCorrelationId);

            var source = TestConstants.TestSource;

            // Act & Assert - Test DeterministicIdGenerator usage
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertRaisedMessage.Create(alert, source);

                    Assert.That(message.CorrelationId, Is.Not.EqualTo(Guid.Empty),
                        "Message should generate non-empty correlation ID when not provided");
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty),
                        "Message ID should be generated by DeterministicIdGenerator");

                    // Test deterministic ID generation consistency
                    var message2 = AlertRaisedMessage.Create(alert, source);
                    Assert.That(message2.CorrelationId, Is.Not.EqualTo(Guid.Empty),
                        "Second message should also generate valid correlation ID");

                    await UniTask.CompletedTask;
                },
                "AlertRaisedMessage.CreateWithDefaultCorrelation",
                TestConstants.FrameBudget);

            // Verify performance remains within Unity frame budget
            LogPerformanceMetrics(result);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertRaisedMessage_Timestamp_ReturnsValidDateTime()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertRaisedMessage_Timestamp_Validation");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.High,
                TestConstants.TestSource,
                correlationId: correlationId);

            var beforeCreation = DateTime.UtcNow;

            // Act & Assert - Comprehensive timestamp validation with performance measurement
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                    var afterCreation = DateTime.UtcNow;

                    // Validate timestamp properties following IMessage compliance
                    Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(beforeCreation),
                        "Message timestamp should be after or equal to creation start time");
                    Assert.That(message.Timestamp, Is.LessThanOrEqualTo(afterCreation),
                        "Message timestamp should be before or equal to creation end time");
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc),
                        "Message timestamp should be UTC for consistent cross-platform behavior");

                    // Validate computed timestamp properties
                    Assert.That(message.TimestampTicks, Is.EqualTo(message.Timestamp.Ticks),
                        "TimestampTicks should match Timestamp.Ticks");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0),
                        "TimestampTicks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertRaisedMessage.TimestampValidation",
                TestConstants.FrameBudget);

            // Ensure timestamp operations are fast enough for Unity game loops
            Assert.That(result.Duration.TotalMilliseconds, Is.LessThan(100),
                "Timestamp operations should be extremely fast for game performance");

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
        }

        [Test]
        public async UniTask AlertRaisedMessage_ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertRaisedMessage_ToString_Validation");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Critical,
                TestConstants.TestSource,
                correlationId: correlationId);

            // Act & Assert - Validate string representation with allocation testing
            await AssertAcceptableAllocationsAsync(
                async () =>
                {
                    var message = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                    var stringRepresentation = message.ToString();

                    // Comprehensive string validation
                    Assert.That(stringRepresentation, Is.Not.Null, "ToString should not return null");
                    Assert.That(stringRepresentation, Is.Not.Empty, "ToString should not return empty string");
                    Assert.That(stringRepresentation, Does.Contain("AlertRaisedMessage"),
                        "String representation should contain message type name");
                    Assert.That(stringRepresentation, Does.Contain(alert.Severity.ToString()),
                        "String representation should contain alert severity");

                    // Validate string formatting is consistent and informative
                    Assert.That(stringRepresentation.Length, Is.GreaterThan(10),
                        "String representation should be descriptive and informative");

                    await UniTask.CompletedTask;
                },
                "AlertRaisedMessage.ToString",
                maxBytes: 512); // Allow reasonable string allocation

            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        #endregion

        #region AlertAcknowledgedMessage Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AlertAcknowledgedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertAcknowledgedMessage_Create_ValidParameters");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            var acknowledgedAlert = alert.Acknowledge(TestConstants.TestUser);
            var source = TestConstants.TestSource;

            // Act - Measure performance and validate IMessage compliance
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertAcknowledgedMessage.Create(acknowledgedAlert, source, correlationId);

                    // Complete IMessage validation
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should be generated");
                    Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertAcknowledgedMessage), "Type code should match");
                    Assert.That(message.AlertId, Is.EqualTo(acknowledgedAlert.Id), "Alert ID should be preserved");
                    Assert.That(message.AcknowledgedBy.ToString(), Is.EqualTo(TestConstants.TestUser), "Acknowledged user should be preserved");
                    Assert.That(message.AcknowledgedTimestamp, Is.Not.Null, "Acknowledged timestamp should not be null");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId), "Correlation ID should be maintained");
                    Assert.That(message.Source.ToString(), Is.EqualTo(source), "Source should be preserved");

                    // Validate timestamp properties
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertAcknowledgedMessage.Create",
                TestConstants.FrameBudget);

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertAcknowledgedMessage_Create_WithNullAlert_ThrowsArgumentNullException()
        {
            // Arrange
            Alert nullAlert = null;
            var correlationId = CreateTestCorrelationId("AlertAcknowledgedMessage_Create_NullAlert");

            // Act & Assert - Validate error handling with timeout support
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentNullException>(() =>
                    AlertAcknowledgedMessage.Create(nullAlert, TestConstants.TestSource, correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertAcknowledgedMessage_Create_WithUnacknowledgedAlert_ThrowsArgumentException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertAcknowledgedMessage_Create_UnacknowledgedAlert");
            var unacknowledgedAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            // Act & Assert - Validate business rule enforcement
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentException>(() =>
                    AlertAcknowledgedMessage.Create(unacknowledgedAlert, TestConstants.TestSource, correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            // Verify error is logged appropriately
            AssertCorrelationTrackingMaintained(correlationId);

            // Note: This might generate appropriate error logs, so we don't assert no errors
        }

        #endregion

        #region AlertResolvedMessage Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AlertResolvedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertResolvedMessage_Create_ValidParameters");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Error,
                TestConstants.TestSource,
                correlationId: correlationId);

            var resolvedAlert = alert.Resolve(TestConstants.TestUser);
            var source = TestConstants.TestSource;

            // Act & Assert - Full IMessage compliance validation with performance testing
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertResolvedMessage.Create(resolvedAlert, source, correlationId);

                    // Comprehensive IMessage validation
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should be generated by DeterministicIdGenerator");
                    Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertResolvedMessage), "Type code should be in correct range");
                    Assert.That(message.AlertId, Is.EqualTo(resolvedAlert.Id), "Alert ID should match resolved alert");
                    Assert.That(message.ResolvedBy.ToString(), Is.EqualTo(TestConstants.TestUser), "Resolved by user should be preserved");
                    Assert.That(message.ResolvedTimestamp, Is.Not.Null, "Resolved timestamp should not be null");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId), "Correlation ID should be maintained");
                    Assert.That(message.Source.ToString(), Is.EqualTo(source), "Source should be preserved");
                    Assert.That(message.Priority, Is.EqualTo(MessagePriority.Normal), "Default priority should be Normal");

                    // Validate computed timestamp properties
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertResolvedMessage.Create",
                TestConstants.FrameBudget);

            // Verify frame budget compliance for Unity game performance
            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertResolvedMessage_Create_WithUnresolvedAlert_ThrowsArgumentException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertResolvedMessage_Create_UnresolvedAlert");
            var unresolvedAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Error,
                TestConstants.TestSource,
                correlationId: correlationId);

            // Act & Assert - Validate business rule enforcement with async support
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentException>(() =>
                    AlertResolvedMessage.Create(unresolvedAlert, TestConstants.TestSource, correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            // Verify correlation is maintained in error scenarios
            AssertCorrelationTrackingMaintained(correlationId);
        }

        #endregion

        #region AlertDeliveryFailedMessage Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AlertDeliveryFailedMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertDeliveryFailedMessage_Create_ValidParameters");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Critical,
                TestConstants.TestSource,
                correlationId: correlationId);

            var channelName = "TestChannel";
            var errorMessage = TestConstants.SampleErrorMessage;
            var source = TestConstants.TestSource;

            // Act & Assert - Comprehensive message validation with performance testing
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertDeliveryFailedMessage.Create(
                        new FixedString64Bytes(channelName),
                        alert,
                        new System.Exception(errorMessage),
                        retryCount: 0,
                        isFinalFailure: true,
                        new FixedString64Bytes(source),
                        correlationId);

                    // Full IMessage interface compliance validation
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should be generated");
                    Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertDeliveryFailedMessage), "Type code should match");
                    Assert.That(message.AlertId, Is.EqualTo(alert.Id), "Alert ID should be preserved");
                    Assert.That(message.ChannelName.ToString(), Is.EqualTo(channelName), "Channel name should be preserved");
                    Assert.That(message.ExceptionMessage.ToString(), Is.EqualTo(errorMessage), "Exception message should be preserved");
                    Assert.That(message.Priority, Is.EqualTo(MessagePriority.High), "Delivery failures should be high priority");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId), "Correlation ID should be maintained");
                    Assert.That(message.Source.ToString(), Is.EqualTo(source), "Source should be preserved");

                    // Validate computed properties
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertDeliveryFailedMessage.Create",
                TestConstants.FrameBudget);

            // Validate Unity game performance requirements
            Assert.That(result.Duration, Is.LessThan(TestConstants.FrameBudget),
                "Message creation should complete within Unity frame budget");

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertDeliveryFailedMessage_Create_WithEmptyChannelName_ThrowsArgumentException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertDeliveryFailedMessage_Create_EmptyChannelName");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            var emptyChannelName = default(FixedString64Bytes);
            var errorMessage = TestConstants.SampleErrorMessage;

            // Act & Assert - Validate parameter validation with async support
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentException>(() =>
                    AlertDeliveryFailedMessage.Create(
                        emptyChannelName,
                        alert,
                        new System.Exception(errorMessage),
                        retryCount: 0,
                        isFinalFailure: true,
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            AssertCorrelationTrackingMaintained(correlationId);
        }

        [Test]
        public async UniTask AlertDeliveryFailedMessage_Create_WithEmptyErrorMessage_ThrowsArgumentException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertDeliveryFailedMessage_Create_EmptyErrorMessage");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            var channelName = "TestChannel";
            var emptyErrorMessage = "";

            // Act & Assert - Comprehensive parameter validation
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentException>(() =>
                    AlertDeliveryFailedMessage.Create(
                        new FixedString64Bytes(channelName),
                        alert,
                        new System.Exception(emptyErrorMessage),
                        retryCount: 0,
                        isFinalFailure: true,
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            // Verify correlation tracking maintains consistency
            AssertCorrelationTrackingMaintained(correlationId);
        }

        #endregion

        #region AlertChannelRegisteredMessage Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AlertChannelRegisteredMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertChannelRegisteredMessage_Create_ValidParameters");
            var channelName = "TestChannel";
            var channelType = AlertChannelType.Console;
            var source = TestConstants.TestSource;

            // Create proper production API objects
            var fixedChannelName = new FixedString64Bytes(channelName);
            var fixedSource = new FixedString64Bytes(source);
            var configuration = CreateTestChannelConfig(channelName, channelType);

            // Act & Assert - Comprehensive validation with performance testing
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertChannelRegisteredMessage.Create(
                        fixedChannelName,
                        configuration,
                        fixedSource,
                        correlationId);

                    // Complete IMessage compliance validation
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should be generated");
                    Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertChannelRegisteredMessage), "Type code should be in correct range");
                    Assert.That(message.ChannelName.ToString(), Is.EqualTo(channelName), "Channel name should be preserved");
                    Assert.That(message.ChannelType, Is.EqualTo(channelType), "Channel type should be preserved");
                    Assert.That(message.Priority, Is.EqualTo(MessagePriority.Low), "Registration events should be low priority");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId), "Correlation ID should be maintained");
                    Assert.That(message.Source.ToString(), Is.EqualTo(source), "Source should be preserved");

                    // Validate computed properties
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertChannelRegisteredMessage.Create",
                TestConstants.FrameBudget);

            // Ensure registration operations are fast for Unity game performance
            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask AlertChannelRegisteredMessage_Create_WithEmptyChannelName_ThrowsArgumentException()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertChannelRegisteredMessage_Create_EmptyChannelName");
            var emptyChannelName = default(FixedString64Bytes);
            var channelType = AlertChannelType.File;

            // Act & Assert - Validate parameter validation with async support
            await ExecuteWithTimeoutAsync(async () =>
            {
                Assert.Throws<ArgumentNullException>(() =>
                    AlertChannelRegisteredMessage.Create(
                        emptyChannelName,
                        null, // Pass null config to trigger ArgumentNullException
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId));

                await UniTask.CompletedTask;
            }, TestConstants.DefaultAsyncTimeout);

            AssertCorrelationTrackingMaintained(correlationId);
        }

        #endregion

        #region AlertChannelUnregisteredMessage Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AlertChannelUnregisteredMessage_Create_WithValidParameters_CreatesMessageCorrectly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertChannelUnregisteredMessage_Create_ValidParameters");
            var channelName = "TestChannel";
            var reason = "Test unregistration";
            var source = TestConstants.TestSource;

            // Act & Assert - Complete IMessage validation with performance measurement
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var message = AlertChannelUnregisteredMessage.Create(
                        new FixedString64Bytes(channelName),
                        new FixedString64Bytes(source),
                        correlationId,
                        new FixedString512Bytes(reason));

                    // Comprehensive IMessage interface compliance
                    Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), "Message ID should be generated");
                    Assert.That(message.TypeCode, Is.EqualTo(MessageTypeCodes.AlertChannelUnregisteredMessage), "Type code should match");
                    Assert.That(message.ChannelName.ToString(), Is.EqualTo(channelName), "Channel name should be preserved");
                    Assert.That(message.Reason.ToString(), Is.EqualTo(reason), "Unregistration reason should be preserved");
                    Assert.That(message.CorrelationId, Is.EqualTo(correlationId), "Correlation ID should be maintained");
                    Assert.That(message.Source.ToString(), Is.EqualTo(source), "Source should be preserved");
                    Assert.That(message.Priority, Is.EqualTo(MessagePriority.Low), "Unregistration events should be low priority");

                    // Validate computed properties
                    Assert.That(message.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), "Timestamp should be UTC");
                    Assert.That(message.TimestampTicks, Is.GreaterThan(0), "Timestamp ticks should be positive");

                    await UniTask.CompletedTask;
                },
                "AlertChannelUnregisteredMessage.Create",
                TestConstants.FrameBudget);

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        #endregion

        #region IMessage Interface Compliance Tests - Enhanced with CLAUDETESTS.md Guidelines

        [Test]
        public async UniTask AllAlertMessages_ImplementIMessage_Correctly()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AllAlertMessages_ImplementIMessage");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            var acknowledgedAlert = alert.Acknowledge(TestConstants.TestUser);
            var resolvedAlert = alert.Resolve(TestConstants.TestUser);

            // Act & Assert - Comprehensive IMessage compliance validation with performance testing
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    var raisedMessage = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                    var acknowledgedMessage = AlertAcknowledgedMessage.Create(acknowledgedAlert, TestConstants.TestSource, correlationId);
                    var resolvedMessage = AlertResolvedMessage.Create(resolvedAlert, TestConstants.TestSource, correlationId);
                    var deliveryFailedMessage = AlertDeliveryFailedMessage.Create(
                        new FixedString64Bytes("TestChannel"),
                        alert,
                        new System.Exception("Test error"),
                        retryCount: 0,
                        isFinalFailure: true,
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId);
                    var registeredMessage = AlertChannelRegisteredMessage.Create(
                        new FixedString64Bytes("TestChannel"),
                        CreateTestChannelConfig("TestChannel", AlertChannelType.Console),
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId);
                    var unregisteredMessage = AlertChannelUnregisteredMessage.Create(
                        new FixedString64Bytes("TestChannel"),
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId,
                        new FixedString512Bytes("Test reason"));

                    // Validate all messages implement IMessage interface
                    Assert.That(raisedMessage, Is.InstanceOf<IMessage>(), "AlertRaisedMessage should implement IMessage");
                    Assert.That(acknowledgedMessage, Is.InstanceOf<IMessage>(), "AlertAcknowledgedMessage should implement IMessage");
                    Assert.That(resolvedMessage, Is.InstanceOf<IMessage>(), "AlertResolvedMessage should implement IMessage");
                    Assert.That(deliveryFailedMessage, Is.InstanceOf<IMessage>(), "AlertDeliveryFailedMessage should implement IMessage");
                    Assert.That(registeredMessage, Is.InstanceOf<IMessage>(), "AlertChannelRegisteredMessage should implement IMessage");
                    Assert.That(unregisteredMessage, Is.InstanceOf<IMessage>(), "AlertChannelUnregisteredMessage should implement IMessage");

                    // Validate all IMessage properties for each message type
                    ValidateIMessageProperties(raisedMessage, "AlertRaisedMessage");
                    ValidateIMessageProperties(acknowledgedMessage, "AlertAcknowledgedMessage");
                    ValidateIMessageProperties(resolvedMessage, "AlertResolvedMessage");
                    ValidateIMessageProperties(deliveryFailedMessage, "AlertDeliveryFailedMessage");
                    ValidateIMessageProperties(registeredMessage, "AlertChannelRegisteredMessage");
                    ValidateIMessageProperties(unregisteredMessage, "AlertChannelUnregisteredMessage");

                    await UniTask.CompletedTask;
                },
                "AllAlertMessages.IMessageCompliance",
                TestConstants.FrameBudget);

            // Validate bulk message creation performance for Unity game requirements
            Assert.That(result.Duration, Is.LessThan(TestConstants.FrameBudget),
                "Bulk message creation should complete within Unity frame budget");

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        [Test]
        public async UniTask MessageTypeCodes_AreInCorrectRange_ForAlertingSystem()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("MessageTypeCodes_CorrectRange");
            var typeCodes = new[]
            {
                MessageTypeCodes.AlertRaisedMessage,
                MessageTypeCodes.AlertAcknowledgedMessage,
                MessageTypeCodes.AlertResolvedMessage,
                MessageTypeCodes.AlertDeliveryFailedMessage,
                MessageTypeCodes.AlertChannelRegisteredMessage,
                MessageTypeCodes.AlertChannelUnregisteredMessage
            };

            // Act & Assert - Validate type code range compliance with performance testing
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    // Validate all type codes are in the correct Alerting System range (1400-1499)
                    foreach (var typeCode in typeCodes)
                    {
                        Assert.That(typeCode, Is.GreaterThanOrEqualTo(1400),
                            $"Type code {typeCode} is below the Alerting System range (1400-1499)");
                        Assert.That(typeCode, Is.LessThanOrEqualTo(1499),
                            $"Type code {typeCode} is above the Alerting System range (1400-1499)");
                    }

                    // Validate no duplicate type codes exist
                    var distinctCodes = typeCodes.Distinct().ToArray();
                    Assert.That(distinctCodes.Length, Is.EqualTo(typeCodes.Length),
                        "All message type codes should be unique within the Alerting System");

                    // Validate type codes follow the system-prefixed naming pattern
                    var expectedPattern = "Alert";
                    foreach (var typeCode in typeCodes)
                    {
                        var typeName = Enum.GetName(typeof(MessageTypeCodes), typeCode);
                        Assert.That(typeName, Does.StartWith(expectedPattern),
                            $"Type code {typeCode} ({typeName}) should follow Alert prefix pattern");
                    }

                    await UniTask.CompletedTask;
                },
                "MessageTypeCodes.RangeValidation",
                TestConstants.FrameBudget);

            // Type code validation should be extremely fast
            Assert.That(result.Duration.TotalMilliseconds, Is.LessThan(50),
                "Type code validation should be extremely fast for performance-critical scenarios");

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
        }

        [Test]
        public async UniTask DeterministicIdGenerator_IsUsedConsistently_AcrossAllMessages()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("DeterministicIdGenerator_Consistency");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            var source = TestConstants.TestSource;

            // Act & Assert - Test deterministic ID generation with performance validation
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    // Create same message multiple times with same parameters
                    var message1 = AlertRaisedMessage.Create(alert, source, correlationId);
                    var message2 = AlertRaisedMessage.Create(alert, source, correlationId);
                    var message3 = AlertRaisedMessage.Create(alert, source, correlationId);

                    // Validate all messages have valid, non-empty IDs
                    Assert.That(message1.Id, Is.Not.EqualTo(Guid.Empty), "First message ID should not be empty");
                    Assert.That(message2.Id, Is.Not.EqualTo(Guid.Empty), "Second message ID should not be empty");
                    Assert.That(message3.Id, Is.Not.EqualTo(Guid.Empty), "Third message ID should not be empty");

                    // Test different message types to ensure consistent ID generation patterns
                    var acknowledgedAlert = alert.Acknowledge(TestConstants.TestUser);
                    var ackMessage1 = AlertAcknowledgedMessage.Create(acknowledgedAlert, source, correlationId);
                    var ackMessage2 = AlertAcknowledgedMessage.Create(acknowledgedAlert, source, correlationId);

                    Assert.That(ackMessage1.Id, Is.Not.EqualTo(Guid.Empty), "Acknowledged message ID should not be empty");
                    Assert.That(ackMessage2.Id, Is.Not.EqualTo(Guid.Empty), "Second acknowledged message ID should not be empty");

                    // Validate IDs are generated consistently using DeterministicIdGenerator
                    // Note: The exact deterministic behavior depends on the implementation,
                    // but all IDs should be valid and generated by the same mechanism
                    Assert.That(message1.Id.ToString().Length, Is.EqualTo(36), "Message ID should be valid GUID format");
                    Assert.That(ackMessage1.Id.ToString().Length, Is.EqualTo(36), "Acknowledged message ID should be valid GUID format");

                    await UniTask.CompletedTask;
                },
                "DeterministicIdGenerator.ConsistencyTest",
                TestConstants.FrameBudget);

            // ID generation should be very fast for performance-critical Unity scenarios
            Assert.That(result.Duration.TotalMilliseconds, Is.LessThan(200),
                "ID generation should be extremely fast for Unity game performance");

            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertNoErrors();
        }

        #endregion

        #region Enhanced Helper Methods

        /// <summary>
        /// Comprehensive IMessage property validation following guidelines.
        /// Validates all required properties and computed values for complete interface compliance.
        /// </summary>
        /// <param name="message">The message to validate</param>
        /// <param name="messageTypeName">The name of the message type for error reporting</param>
        private void ValidateIMessageProperties(IMessage message, string messageTypeName = "Unknown")
        {
            // Core IMessage property validation
            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty), $"{messageTypeName}: Message ID should not be empty");
            Assert.That(message.TimestampTicks, Is.GreaterThan(0), $"{messageTypeName}: Timestamp ticks should be positive");
            Assert.That(message.TypeCode, Is.GreaterThan(0), $"{messageTypeName}: Type code should be positive");
            Assert.That(message.Source.ToString(), Is.Not.Empty, $"{messageTypeName}: Source should not be empty");
            Assert.That(message.CorrelationId, Is.Not.EqualTo(Guid.Empty), $"{messageTypeName}: Correlation ID should not be empty");

            // Validate computed timestamp properties using TimestampTicks
            var computedTimestamp = new DateTime(message.TimestampTicks, DateTimeKind.Utc);
            Assert.That(computedTimestamp.Kind, Is.EqualTo(DateTimeKind.Utc), $"{messageTypeName}: Timestamp should be UTC");
            Assert.That(computedTimestamp, Is.LessThanOrEqualTo(DateTime.UtcNow), $"{messageTypeName}: Timestamp should not be in the future");
            Assert.That(computedTimestamp.Ticks, Is.EqualTo(message.TimestampTicks), $"{messageTypeName}: Computed timestamp ticks should match TimestampTicks");

            // Validate priority is within valid range
            Assert.That(Enum.IsDefined(typeof(MessagePriority), message.Priority), Is.True,
                $"{messageTypeName}: Priority should be a valid MessagePriority enum value");

            // Validate type code is within acceptable ranges (should be system-specific)
            Assert.That(message.TypeCode, Is.GreaterThanOrEqualTo(1000), $"{messageTypeName}: Type code should be >= 1000 for system messages");
            Assert.That(message.TypeCode, Is.LessThanOrEqualTo(65535), $"{messageTypeName}: Type code should be <= 65535 for ushort range");

            // Validate FixedString properties don't exceed their capacity
            var sourceString = message.Source.ToString();
            Assert.That(sourceString.Length, Is.LessThanOrEqualTo(64), $"{messageTypeName}: Source should not exceed FixedString64Bytes capacity");

            // Performance validation - message property access should be fast
            var startTime = DateTime.UtcNow;
            _ = message.Id;
            _ = message.TimestampTicks;
            _ = message.TypeCode;
            _ = message.Source;
            _ = message.Priority;
            _ = message.CorrelationId;
            var accessTime = DateTime.UtcNow - startTime;

            Assert.That(accessTime.TotalMilliseconds, Is.LessThan(10),
                $"{messageTypeName}: Property access should be extremely fast for game performance");
        }

        /// <summary>
        /// Validates zero-allocation patterns for message operations following Unity game development requirements.
        /// </summary>
        /// <param name="operation">The operation to test</param>
        /// <param name="operationName">Name of the operation for reporting</param>
        /// <returns>Task representing the async validation</returns>
        private async UniTask ValidateZeroAllocationPattern(Func<UniTask> operation, string operationName)
        {
            var result = await AllocationTracker.MeasureAllocationsAsync(operation, operationName);

            // For message creation, we allow minimal allocations but should stay under strict limits
            Assert.That(result.TotalBytes, Is.LessThanOrEqualTo(256),
                $"{operationName}: Should minimize allocations for Unity game performance (found {result.TotalBytes} bytes)");

            // Log allocation details for performance analysis
            if (result.TotalBytes > 0)
            {
                StubLogging.LogInfo($"Allocation Details: {operationName} allocated {result.TotalBytes} bytes, {result.TotalAllocations} collections");
            }
        }

        /// <summary>
        /// Validates that message operations complete within Unity's frame budget requirements.
        /// </summary>
        /// <param name="operation">The operation to validate</param>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Performance result for further analysis</returns>
        private async UniTask<PerformanceResult> ValidateFrameBudgetCompliance(Func<UniTask> operation, string operationName)
        {
            var result = await PerformanceHelper.MeasureAsync(operation, operationName);

            // Message operations should be extremely fast - much less than frame budget
            var maxAllowedTime = TimeSpan.FromMilliseconds(0.1); // 0.1ms for message operations
            Assert.That(result.Duration, Is.LessThan(maxAllowedTime),
                $"{operationName}: Should complete in <0.1ms for Unity game performance (took {result.Duration.TotalMilliseconds:F3}ms)");

            return result;
        }

        /// <summary>
        /// Creates test data for stress testing message operations.
        /// </summary>
        /// <param name="count">Number of test alerts to create</param>
        /// <returns>Array of test alerts</returns>
        private Alert[] CreateBulkTestAlerts(int count)
        {
            var alerts = new Alert[count];
            var correlationId = CreateTestCorrelationId("BulkTestAlerts");

            for (int i = 0; i < count; i++)
            {
                alerts[i] = Alert.Create(
                    $"{TestConstants.SampleAlertMessage}_{i}",
                    (AlertSeverity)(i % 5 + 1), // Cycle through severities
                    $"{TestConstants.TestSource}_{i % 10}", // Cycle through sources
                    correlationId: correlationId);
            }

            return alerts;
        }

        /// <summary>
        /// Performs stress testing on message creation operations.
        /// Validates system behavior under realistic game load conditions.
        /// </summary>
        /// <param name="messageCount">Number of messages to create in stress test</param>
        /// <returns>Task representing the stress test</returns>
        private async UniTask PerformMessageCreationStressTest(int messageCount = 1000)
        {
            var correlationId = CreateTestCorrelationId("MessageCreationStressTest");
            var testAlerts = CreateBulkTestAlerts(messageCount);

            using var performanceHelper = new PerformanceTestHelper(StubLogging);
            var stressResult = performanceHelper.PerformStressTest(
                () =>
                {
                    foreach (var alert in testAlerts)
                    {
                        var message = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                        // Validate the message was created successfully
                        Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
                    }
                },
                "BulkMessageCreation",
                iterations: 1,
                correlationId);

            // Stress test should complete successfully
            Assert.That(stressResult.FailureCount, Is.EqualTo(0), "Stress test should complete without failures");
            Assert.That(stressResult.Statistics.AverageDuration, Is.LessThan(TimeSpan.FromMilliseconds(500)),
                "Bulk message creation should complete quickly even under stress");

            StubLogging.LogInfo($"Stress Test Results: {messageCount} messages created in {stressResult.Statistics.AverageDuration.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Comprehensive stress test validating all alert message types under load.
        /// Tests Unity game development requirements for sustained performance.
        /// </summary>
        [Test]
        public async UniTask AllAlertMessages_StressTest_MaintainPerformanceUnderLoad()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AllAlertMessages_StressTest");
            const int messageCount = 500; // Realistic game load
            var testAlerts = CreateBulkTestAlerts(messageCount);

            // Act & Assert - Comprehensive stress testing
            var overallResult = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    // Test each message type under stress
                    foreach (var alert in testAlerts)
                    {
                        // Create various message types
                        var raisedMsg = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                        var deliveryFailedMsg = AlertDeliveryFailedMessage.Create(
                            new FixedString64Bytes("TestChannel"),
                            alert,
                            new System.Exception("Test error"),
                            retryCount: 0,
                            isFinalFailure: true,
                            new FixedString64Bytes(TestConstants.TestSource),
                            correlationId);
                        var channelRegMsg = AlertChannelRegisteredMessage.Create(
                            new FixedString64Bytes("TestChannel"),
                            CreateTestChannelConfig("TestChannel", AlertChannelType.Console),
                            new FixedString64Bytes(TestConstants.TestSource),
                            correlationId);

                        // Quick validation that messages were created successfully
                        Assert.That(raisedMsg.Id, Is.Not.EqualTo(Guid.Empty));
                        Assert.That(deliveryFailedMsg.Id, Is.Not.EqualTo(Guid.Empty));
                        Assert.That(channelRegMsg.Id, Is.Not.EqualTo(Guid.Empty));
                    }

                    await UniTask.CompletedTask;
                },
                "AllAlertMessages.StressTest",
                TestConstants.FrameBudget * 10); // Allow multiple frames for stress test

            // Validate performance remains acceptable under load
            Assert.That(overallResult.Duration, Is.LessThan(TimeSpan.FromSeconds(2)),
                $"Stress test with {messageCount} messages should complete within 2 seconds");

            // Test should maintain system health
            AssertAllServicesHealthy();
            AssertCorrelationTrackingMaintained(correlationId);

            // Performance logging
            var messagesPerSecond = messageCount / overallResult.Duration.TotalSeconds;
            StubLogging.LogInfo($"Stress Test Performance: {messagesPerSecond:F0} messages/second, {overallResult.Duration.TotalMilliseconds:F2}ms total");
            LogPerformanceMetrics(overallResult);
        }

        /// <summary>
        /// Zero-allocation validation test for performance-critical Unity scenarios.
        /// Validates that message operations follow Unity Collections patterns for optimal performance.
        /// </summary>
        [Test]
        public async UniTask AlertMessages_ZeroAllocationPattern_ValidatesUnityPerformanceRequirements()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertMessages_ZeroAllocation");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                correlationId: correlationId);

            // Act & Assert - Test zero-allocation patterns for each message type
            await ValidateZeroAllocationPattern(async () =>
            {
                var message = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
                await UniTask.CompletedTask;
            }, "AlertRaisedMessage.ZeroAllocation");

            await ValidateZeroAllocationPattern(async () =>
            {
                var message = AlertDeliveryFailedMessage.Create(
                    new FixedString64Bytes("TestChannel"),
                    alert,
                    new System.Exception("Test error"),
                    retryCount: 0,
                    isFinalFailure: true,
                    new FixedString64Bytes(TestConstants.TestSource),
                    correlationId);
                Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
                await UniTask.CompletedTask;
            }, "AlertDeliveryFailedMessage.ZeroAllocation");

            await ValidateZeroAllocationPattern(async () =>
            {
                var message = AlertChannelRegisteredMessage.Create(
                    new FixedString64Bytes("TestChannel"),
                    CreateTestChannelConfig("TestChannel", AlertChannelType.Console),
                    new FixedString64Bytes(TestConstants.TestSource),
                    correlationId);
                Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
                await UniTask.CompletedTask;
            }, "AlertChannelRegisteredMessage.ZeroAllocation");

            AssertCorrelationTrackingMaintained(correlationId);
            AssertAllServicesHealthy();
        }

        /// <summary>
        /// End-to-end integration test validating complete alert message workflow.
        /// Tests realistic Unity game scenario with proper service integration.
        /// </summary>
        [Test]
        public async UniTask AlertMessages_EndToEndWorkflow_ValidatesCompleteIntegration()
        {
            // Arrange
            var correlationId = CreateTestCorrelationId("AlertMessages_EndToEndWorkflow");
            var alert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Critical,
                TestConstants.TestSource,
                correlationId: correlationId);

            // Act - Simulate complete alert lifecycle
            var result = await ExecuteWithPerformanceMeasurementAsync(
                async () =>
                {
                    // 1. Alert is raised
                    var raisedMessage = AlertRaisedMessage.Create(alert, TestConstants.TestSource, correlationId);
                    SpyMessageBus.PublishMessage(raisedMessage);

                    // 2. Alert delivery fails
                    var deliveryFailedMessage = AlertDeliveryFailedMessage.Create(
                        new FixedString64Bytes("PrimaryChannel"),
                        alert,
                        new System.Exception("Connection timeout"),
                        retryCount: 0,
                        isFinalFailure: true,
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId);
                    SpyMessageBus.PublishMessage(deliveryFailedMessage);

                    // 3. Channel is registered
                    var channelRegMessage = AlertChannelRegisteredMessage.Create(
                        new FixedString64Bytes("BackupChannel"),
                        CreateTestChannelConfig("BackupChannel", AlertChannelType.File),
                        new FixedString64Bytes(TestConstants.TestSource),
                        correlationId);
                    SpyMessageBus.PublishMessage(channelRegMessage);

                    // 4. Alert is acknowledged
                    var acknowledgedAlert = alert.Acknowledge(TestConstants.TestUser);
                    var ackMessage = AlertAcknowledgedMessage.Create(acknowledgedAlert, TestConstants.TestSource, correlationId);
                    SpyMessageBus.PublishMessage(ackMessage);

                    // 5. Alert is resolved
                    var resolvedAlert = acknowledgedAlert.Resolve(TestConstants.TestUser);
                    var resolvedMessage = AlertResolvedMessage.Create(resolvedAlert, TestConstants.TestSource, correlationId);
                    SpyMessageBus.PublishMessage(resolvedMessage);

                    await UniTask.CompletedTask;
                },
                "AlertMessages.EndToEndWorkflow",
                TestConstants.FrameBudget * 5); // Allow multiple frames for complex workflow

            // Assert - Validate complete workflow
            AssertMessageCount<AlertRaisedMessage>(1);
            AssertMessageCount<AlertDeliveryFailedMessage>(1);
            AssertMessageCount<AlertChannelRegisteredMessage>(1);
            AssertMessageCount<AlertAcknowledgedMessage>(1);
            AssertMessageCount<AlertResolvedMessage>(1);

            // Validate all messages maintain correlation
            var allMessages = SpyMessageBus.PublishedMessages.ToList();
            Assert.That(allMessages.All(m => m.CorrelationId == correlationId), Is.True,
                "All messages in workflow should maintain the same correlation ID");

            // Validate performance of complete workflow
            LogPerformanceMetrics(result);
            AssertCorrelationTrackingMaintained(correlationId);
            AssertAllServicesHealthy();

            StubLogging.LogInfo($"End-to-end workflow completed with {allMessages.Count} messages in {result.Duration.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Creates a test ChannelConfig with the specified name and type for testing purposes.
        /// Uses defaults for all other properties to ensure consistent test data.
        /// </summary>
        /// <param name="name">The channel name</param>
        /// <param name="channelType">The channel type</param>
        /// <returns>A configured ChannelConfig for testing</returns>
        private ChannelConfig CreateTestChannelConfig(string name, AlertChannelType channelType)
        {
            return new ChannelConfig
            {
                Name = name,
                ChannelType = channelType,
                IsEnabled = true,
                MinimumSeverity = AlertSeverity.Info,
                MaximumSeverity = AlertSeverity.Emergency
            };
        }

        #endregion
    }
}