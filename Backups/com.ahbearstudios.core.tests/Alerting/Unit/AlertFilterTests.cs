using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using ZLinq;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Alerting.Filters;
using AhBearStudios.Core.Alerting.Services;
using AhBearStudios.Core.Tests.Shared.Base;
using AhBearStudios.Core.Tests.Shared.Utilities;

namespace AhBearStudios.Core.Tests.Alerting.Unit
{
    /// <summary>
    /// Unit tests for alert filters testing filter evaluation logic, composite filters,
    /// and filter service management functionality.
    /// </summary>
    [TestFixture]
    public class AlertFilterTests : BaseServiceTest
    {
        private Alert _testAlert;
        private FilterContext _testContext;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _testAlert = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource,
                TestConstants.SampleTag,
                CreateTestCorrelationId());

            _testContext = FilterContext.WithCorrelation(CreateTestCorrelationId());
        }

        #region SeverityAlertFilter Tests

        [Test]
        public void SeverityAlertFilter_Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Error);

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("SeverityFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.High));
            Assert.That(filter.IsEnabled, Is.True);
        }

        [Test]
        public void SeverityAlertFilter_Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => new SeverityAlertFilter("", AlertSeverity.Warning));
        }

        [Test]
        public void SeverityAlertFilter_Evaluate_WithAlertAboveThreshold_AllowsAlert()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Info);
            var highSeverityAlert = Alert.Create(
                "High severity alert",
                AlertSeverity.Error, // Above Info threshold
                TestConstants.TestSource);

            // Act
            var result = filter.Evaluate(highSeverityAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
            Assert.That(result.ModifiedAlert, Is.Null);
        }

        [Test]
        public void SeverityAlertFilter_Evaluate_WithAlertBelowThreshold_SuppressesAlert()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Error);
            var lowSeverityAlert = Alert.Create(
                "Low severity alert",
                AlertSeverity.Info, // Below Error threshold
                TestConstants.TestSource);

            // Act
            var result = filter.Evaluate(lowSeverityAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        [Test]
        public void SeverityAlertFilter_Evaluate_WithAlertAtThreshold_AllowsAlert()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);
            var thresholdAlert = Alert.Create(
                "Threshold alert",
                AlertSeverity.Warning, // Exactly at threshold
                TestConstants.TestSource);

            // Act
            var result = filter.Evaluate(thresholdAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void SeverityAlertFilter_CanHandle_WithValidAlert_ReturnsTrue()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            // Act
            var canHandle = filter.CanHandle(_testAlert);

            // Assert
            Assert.That(canHandle, Is.True);
        }

        [Test]
        public void SeverityAlertFilter_CanHandle_WithNullAlert_ReturnsFalse()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            // Act
            var canHandle = filter.CanHandle(null);

            // Assert
            Assert.That(canHandle, Is.False);
        }

        #endregion

        #region SourceAlertFilter Tests

        [Test]
        public void SourceAlertFilter_Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new SourceAlertFilter("SourceFilter", TestConstants.TestSource);

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("SourceFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.Medium));
            Assert.That(filter.IsEnabled, Is.True);
        }

        [Test]
        public void SourceAlertFilter_Evaluate_WithMatchingSource_AllowsAlert()
        {
            // Arrange
            var filter = new SourceAlertFilter("SourceFilter", TestConstants.TestSource);

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void SourceAlertFilter_Evaluate_WithNonMatchingSource_SuppressesAlert()
        {
            // Arrange
            var filter = new SourceAlertFilter("SourceFilter", "DifferentSource");

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        [Test]
        public void SourceAlertFilter_Evaluate_WithWildcardPattern_MatchesCorrectly()
        {
            // Arrange
            var filter = new SourceAlertFilter("SourceFilter", "Test*", useWildcard: true);

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        #endregion

        #region ContentAlertFilter Tests

        [Test]
        public void ContentAlertFilter_Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new ContentAlertFilter("ContentFilter", "test");

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("ContentFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.Low));
        }

        [Test]
        public void ContentAlertFilter_Evaluate_WithMatchingContent_AllowsAlert()
        {
            // Arrange
            var filter = new ContentAlertFilter("ContentFilter", "alert");

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void ContentAlertFilter_Evaluate_WithNonMatchingContent_SuppressesAlert()
        {
            // Arrange
            var filter = new ContentAlertFilter("ContentFilter", "nonexistent");

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        [Test]
        public void ContentAlertFilter_Evaluate_WithCaseInsensitiveMatch_AllowsAlert()
        {
            // Arrange
            var filter = new ContentAlertFilter("ContentFilter", "ALERT", caseSensitive: false);

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        #endregion

        #region TagAlertFilter Tests

        [Test]
        public void TagAlertFilter_Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new TagAlertFilter("TagFilter", TestConstants.SampleTag);

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("TagFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.Medium));
        }

        [Test]
        public void TagAlertFilter_Evaluate_WithMatchingTag_AllowsAlert()
        {
            // Arrange
            var filter = new TagAlertFilter("TagFilter", TestConstants.SampleTag);

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void TagAlertFilter_Evaluate_WithNonMatchingTag_SuppressesAlert()
        {
            // Arrange
            var filter = new TagAlertFilter("TagFilter", "DifferentTag");

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        [Test]
        public void TagAlertFilter_Evaluate_WithEmptyAlertTag_SuppressesAlert()
        {
            // Arrange
            var alertWithoutTag = Alert.Create(
                TestConstants.SampleAlertMessage,
                AlertSeverity.Warning,
                TestConstants.TestSource);

            var filter = new TagAlertFilter("TagFilter", TestConstants.SampleTag);

            // Act
            var result = filter.Evaluate(alertWithoutTag, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        #endregion

        #region RateLimitAlertFilter Tests

        [Test]
        public void RateLimitAlertFilter_Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new RateLimitAlertFilter("RateFilter", 100, TimeSpan.FromMinutes(1));

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("RateFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.High));
        }

        [Test]
        public void RateLimitAlertFilter_Constructor_WithZeroMaxCount_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new RateLimitAlertFilter("RateFilter", 0, TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void RateLimitAlertFilter_Evaluate_WithinRateLimit_AllowsAlert()
        {
            // Arrange
            var filter = new RateLimitAlertFilter("RateFilter", 10, TimeSpan.FromMinutes(1));

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void RateLimitAlertFilter_Evaluate_ExceedingRateLimit_SuppressesAlert()
        {
            // Arrange
            var filter = new RateLimitAlertFilter("RateFilter", 1, TimeSpan.FromMinutes(1));

            // Act - Send two alerts to exceed the limit
            var result1 = filter.Evaluate(_testAlert, _testContext);
            var result2 = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result1.Decision, Is.EqualTo(FilterDecision.Allow));
            Assert.That(result2.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        [Test]
        public void RateLimitAlertFilter_Evaluate_AfterTimeWindowReset_AllowsAlert()
        {
            // Arrange
            var shortTimeWindow = TimeSpan.FromMilliseconds(10);
            var filter = new RateLimitAlertFilter("RateFilter", 1, shortTimeWindow);

            // Act - Send alert to reach limit
            var result1 = filter.Evaluate(_testAlert, _testContext);

            // Wait for time window to reset
            System.Threading.Thread.Sleep(20);

            // Send another alert after window reset
            var result2 = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result1.Decision, Is.EqualTo(FilterDecision.Allow));
            Assert.That(result2.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        #endregion

        #region CompositeAlertFilter Tests

        [Test]
        public void CompositeAlertFilter_Constructor_WithValidFilters_InitializesCorrectly()
        {
            // Arrange
            var filter1 = new SeverityAlertFilter("Severity", AlertSeverity.Warning);
            var filter2 = new SourceAlertFilter("Source", TestConstants.TestSource);

            // Act
            var composite = new CompositeAlertFilter("CompositeFilter", filter1, filter2);

            // Assert
            Assert.That(composite.Name.ToString(), Is.EqualTo("CompositeFilter"));
            Assert.That(composite.FilterCount, Is.EqualTo(2));
        }

        [Test]
        public void CompositeAlertFilter_Evaluate_WithAllFiltersAllowing_AllowsAlert()
        {
            // Arrange
            var filter1 = new SeverityAlertFilter("Severity", AlertSeverity.Info); // Will allow Warning
            var filter2 = new SourceAlertFilter("Source", TestConstants.TestSource); // Will allow matching source

            var composite = new CompositeAlertFilter("CompositeFilter", filter1, filter2);

            // Act
            var result = composite.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void CompositeAlertFilter_Evaluate_WithOneFilterSuppressing_SuppressesAlert()
        {
            // Arrange
            var filter1 = new SeverityAlertFilter("Severity", AlertSeverity.Error); // Will suppress Warning
            var filter2 = new SourceAlertFilter("Source", TestConstants.TestSource); // Will allow matching source

            var composite = new CompositeAlertFilter("CompositeFilter", filter1, filter2);

            // Act
            var result = composite.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        [Test]
        public void CompositeAlertFilter_AddFilter_AddsFilterCorrectly()
        {
            // Arrange
            var composite = new CompositeAlertFilter("CompositeFilter");
            var filter = new SeverityAlertFilter("Severity", AlertSeverity.Warning);

            // Act
            composite.AddFilter(filter);

            // Assert
            Assert.That(composite.FilterCount, Is.EqualTo(1));
        }

        [Test]
        public void CompositeAlertFilter_RemoveFilter_RemovesFilterCorrectly()
        {
            // Arrange
            var filter = new SeverityAlertFilter("Severity", AlertSeverity.Warning);
            var composite = new CompositeAlertFilter("CompositeFilter", filter);

            // Act
            var removed = composite.RemoveFilter("Severity");

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(composite.FilterCount, Is.EqualTo(0));
        }

        #endregion

        #region BlockAlertFilter Tests

        [Test]
        public void BlockAlertFilter_Constructor_WithValidName_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new BlockAlertFilter("BlockFilter");

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("BlockFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.Highest));
        }

        [Test]
        public void BlockAlertFilter_Evaluate_WithAnyAlert_SuppressesAlert()
        {
            // Arrange
            var filter = new BlockAlertFilter("BlockFilter");

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Suppress));
        }

        #endregion

        #region PassThroughAlertFilter Tests

        [Test]
        public void PassThroughAlertFilter_Constructor_WithValidName_InitializesCorrectly()
        {
            // Arrange & Act
            var filter = new PassThroughAlertFilter("PassThroughFilter");

            // Assert
            Assert.That(filter.Name.ToString(), Is.EqualTo("PassThroughFilter"));
            Assert.That(filter.Priority, Is.EqualTo(FilterPriority.Lowest));
        }

        [Test]
        public void PassThroughAlertFilter_Evaluate_WithAnyAlert_AllowsAlert()
        {
            // Arrange
            var filter = new PassThroughAlertFilter("PassThroughFilter");

            // Act
            var result = filter.Evaluate(_testAlert, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        #endregion

        #region AlertFilterService Tests

        [Test]
        public void AlertFilterService_Constructor_WithValidLoggingService_InitializesCorrectly()
        {
            // Arrange & Act
            var service = new AlertFilterService(MockLogging);

            // Assert
            Assert.That(service.IsEnabled, Is.True);
            Assert.That(service.FilterCount, Is.EqualTo(0));
        }

        [Test]
        public void AlertFilterService_RegisterFilter_WithValidFilter_RegistersSuccessfully()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);
            var correlationId = CreateTestCorrelationId();

            // Act
            service.RegisterFilter(filter, null, correlationId);

            // Assert
            Assert.That(service.FilterCount, Is.EqualTo(1));
            Assert.That(service.IsFilterRegistered("SeverityFilter"), Is.True);
        }

        [Test]
        public void AlertFilterService_RegisterFilter_WithNullFilter_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                service.RegisterFilter(null, null, Guid.NewGuid()));
        }

        [Test]
        public void AlertFilterService_UnregisterFilter_WithValidName_UnregistersSuccessfully()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            service.RegisterFilter(filter, null, CreateTestCorrelationId());

            // Act
            var result = service.UnregisterFilter("SeverityFilter", CreateTestCorrelationId());

            // Assert
            Assert.That(result, Is.True);
            Assert.That(service.FilterCount, Is.EqualTo(0));
            Assert.That(service.IsFilterRegistered("SeverityFilter"), Is.False);
        }

        [Test]
        public void AlertFilterService_UnregisterFilter_WithNonExistentName_ReturnsFalse()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);

            // Act
            var result = service.UnregisterFilter("NonExistentFilter", CreateTestCorrelationId());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void AlertFilterService_ApplyFilters_WithRegisteredFilters_AppliesInPriorityOrder()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);

            var highPriorityFilter = new BlockAlertFilter("HighPriority"); // Highest priority
            var lowPriorityFilter = new PassThroughAlertFilter("LowPriority"); // Lowest priority

            service.RegisterFilter(lowPriorityFilter, null, CreateTestCorrelationId());
            service.RegisterFilter(highPriorityFilter, null, CreateTestCorrelationId());

            // Act
            var result = service.ApplyFilters(_testAlert, _testContext);

            // Assert
            // High priority BlockFilter should suppress the alert before PassThrough is reached
            Assert.That(result, Is.Null); // Alert was suppressed
        }

        [Test]
        public void AlertFilterService_GetFilterStatistics_ReturnsValidStatistics()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            service.RegisterFilter(filter, null, CreateTestCorrelationId());

            // Act some filtering
            service.ApplyFilters(_testAlert, _testContext);

            // Act
            var statistics = service.GetFilterStatistics();

            // Assert
            Assert.That(statistics, Is.Not.Null);
        }

        [Test]
        public void AlertFilterService_ResetPerformanceMetrics_ResetsMetrics()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);
            var correlationId = CreateTestCorrelationId();

            // Act
            service.ResetPerformanceMetrics(correlationId);

            // Assert - Should not throw and should complete successfully
            Assert.That(service.IsEnabled, Is.True);
        }

        #endregion

        #region Filter Priority Tests

        [Test]
        public void Filters_HaveCorrectPriorityOrdering()
        {
            // Arrange
            var blockFilter = new BlockAlertFilter("Block");
            var severityFilter = new SeverityAlertFilter("Severity", AlertSeverity.Warning);
            var rateLimitFilter = new RateLimitAlertFilter("RateLimit", 100, TimeSpan.FromMinutes(1));
            var sourceFilter = new SourceAlertFilter("Source", TestConstants.TestSource);
            var tagFilter = new TagAlertFilter("Tag", TestConstants.SampleTag);
            var contentFilter = new ContentAlertFilter("Content", "test");
            var passThroughFilter = new PassThroughAlertFilter("PassThrough");

            // Act & Assert - Verify priority ordering
            Assert.That(blockFilter.Priority, Is.EqualTo(FilterPriority.Highest));
            Assert.That(severityFilter.Priority, Is.EqualTo(FilterPriority.High));
            Assert.That(rateLimitFilter.Priority, Is.EqualTo(FilterPriority.High));
            Assert.That(sourceFilter.Priority, Is.EqualTo(FilterPriority.Medium));
            Assert.That(tagFilter.Priority, Is.EqualTo(FilterPriority.Medium));
            Assert.That(contentFilter.Priority, Is.EqualTo(FilterPriority.Low));
            Assert.That(passThroughFilter.Priority, Is.EqualTo(FilterPriority.Lowest));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Filter_Evaluate_WithNullAlert_ReturnsAllowByDefault()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            // Act
            var result = filter.Evaluate(null, _testContext);

            // Assert
            Assert.That(result.Decision, Is.EqualTo(FilterDecision.Allow));
        }

        [Test]
        public void Filter_Evaluate_WithNullContext_HandlesGracefully()
        {
            // Arrange
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => filter.Evaluate(_testAlert, null));
        }

        [Test]
        public void AlertFilterService_ApplyFilters_WithNullAlert_ReturnsNull()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);

            // Act
            var result = service.ApplyFilters(null, _testContext);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void AlertFilterService_Dispose_DisposesCleanly()
        {
            // Arrange
            var service = new AlertFilterService(MockLogging);
            var filter = new SeverityAlertFilter("SeverityFilter", AlertSeverity.Warning);

            service.RegisterFilter(filter, null, CreateTestCorrelationId());

            // Act
            service.Dispose();

            // Assert
            Assert.That(service.IsEnabled, Is.False);
            Assert.That(service.FilterCount, Is.EqualTo(0));
        }

        #endregion
    }
}