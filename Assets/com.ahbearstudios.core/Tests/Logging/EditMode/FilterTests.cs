using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using NUnit.Framework;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Filters;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Tests.Utilities;
using AhBearStudios.Core.Tests.Shared;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Comprehensive tests for logging filters including LevelFilter, SourceFilter, 
    /// and advanced filtering capabilities. Tests priority ordering, performance,
    /// and configuration management.
    /// </summary>
    [TestFixture]
    public class FilterTests
    {
        private MockProfilerService _mockProfiler;
        private FilterConfig _defaultConfig;

        [SetUp]
        public void SetUp()
        {
            _mockProfiler = new MockProfilerService();
            _defaultConfig = new FilterConfig
            {
                Name = "TestFilter",
                IsEnabled = true,
                Priority = 100,
                Properties = new Dictionary<string, object>()
            };
        }

        [TearDown]
        public void TearDown()
        {
            _mockProfiler?.Dispose();
            TestUtilities.CleanupTempFiles();
        }

        #region LevelFilter Tests

        [Test]
        public void LevelFilter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MinimumLevel"] = LogLevel.Warning;
            config.Properties["MaximumLevel"] = LogLevel.Critical;

            // Act
            var filter = new LevelFilter(config);

            // Assert
            Assert.That(filter.Name, Is.EqualTo("TestFilter"));
            Assert.That(filter.IsEnabled, Is.True);
            Assert.That(filter.Priority, Is.EqualTo(100));
        }

        [Test]
        public void LevelFilter_ShouldLog_WithMinimumLevel_FiltersCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MinimumLevel"] = LogLevel.Warning;
            var filter = new LevelFilter(config);

            var debugMessage = TestDataFactory.CreateLogMessage(LogLevel.Debug, "Test", "Debug message");
            var infoMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Info message");
            var warningMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Test", "Warning message");
            var errorMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error message");

            // Act & Assert
            Assert.That(filter.ShouldLog(debugMessage), Is.False);
            Assert.That(filter.ShouldLog(infoMessage), Is.False);
            Assert.That(filter.ShouldLog(warningMessage), Is.True);
            Assert.That(filter.ShouldLog(errorMessage), Is.True);
        }

        [Test]
        public void LevelFilter_ShouldLog_WithMinimumAndMaximumLevel_FiltersRange()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MinimumLevel"] = LogLevel.Info;
            config.Properties["MaximumLevel"] = LogLevel.Warning;
            var filter = new LevelFilter(config);

            var debugMessage = TestDataFactory.CreateLogMessage(LogLevel.Debug, "Test", "Debug message");
            var infoMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Info message");
            var warningMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Test", "Warning message");
            var errorMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error message");

            // Act & Assert
            Assert.That(filter.ShouldLog(debugMessage), Is.False);
            Assert.That(filter.ShouldLog(infoMessage), Is.True);
            Assert.That(filter.ShouldLog(warningMessage), Is.True);
            Assert.That(filter.ShouldLog(errorMessage), Is.False);
        }

        [Test]
        public void LevelFilter_ShouldLog_WithExcludeMode_InvertsFiltering()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MinimumLevel"] = LogLevel.Warning;
            config.Properties["ExcludeMode"] = true;
            var filter = new LevelFilter(config);

            var debugMessage = TestDataFactory.CreateLogMessage(LogLevel.Debug, "Test", "Debug message");
            var warningMessage = TestDataFactory.CreateLogMessage(LogLevel.Warning, "Test", "Warning message");
            var errorMessage = TestDataFactory.CreateLogMessage(LogLevel.Error, "Test", "Error message");

            // Act & Assert
            Assert.That(filter.ShouldLog(debugMessage), Is.True);  // Below minimum, so included in exclude mode
            Assert.That(filter.ShouldLog(warningMessage), Is.False); // At minimum, so excluded
            Assert.That(filter.ShouldLog(errorMessage), Is.False);   // Above minimum, so excluded
        }

        [Test]
        public void LevelFilter_Performance_HighThroughputFiltering()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MinimumLevel"] = LogLevel.Warning;
            var filter = new LevelFilter(config);
            var messages = TestDataFactory.CreateLogMessageBatch(1000, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var message in messages)
                {
                    filter.ShouldLog(message);
                }
            }, TimeSpan.FromMilliseconds(10)); // 10ms for 1000 messages
        }

        [Test]
        public void LevelFilter_FrameBudget_QuickFiltering()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MinimumLevel"] = LogLevel.Error;
            var filter = new LevelFilter(config);
            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Frame budget test");

            // Act & Assert
            TestUtilities.AssertFrameBudget(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    filter.ShouldLog(message);
                }
            }, 0.01); // 0.01ms for 100 filter operations
        }

        #endregion

        #region SourceFilter Tests

        [Test]
        public void SourceFilter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["AllowedSources"] = new List<string> { "TestClass", "AnotherClass" };

            // Act
            var filter = new SourceFilter(config);

            // Assert
            Assert.That(filter.Name, Is.EqualTo("TestFilter"));
            Assert.That(filter.IsEnabled, Is.True);
        }

        [Test]
        public void SourceFilter_ShouldLog_WithAllowedSources_FiltersCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["AllowedSources"] = new List<string> { "TestClass", "AnotherClass" };
            var filter = new SourceFilter(config);

            var allowedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Allowed message",
                "test-correlation", "TestClass");
            var anotherAllowedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Another allowed message",
                "test-correlation", "AnotherClass");
            var blockedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Blocked message",
                "test-correlation", "BlockedClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(allowedMessage), Is.True);
            Assert.That(filter.ShouldLog(anotherAllowedMessage), Is.True);
            Assert.That(filter.ShouldLog(blockedMessage), Is.False);
        }

        [Test]
        public void SourceFilter_ShouldLog_WithBlockedSources_FiltersCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["BlockedSources"] = new List<string> { "BlockedClass", "AnotherBlockedClass" };
            var filter = new SourceFilter(config);

            var allowedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Allowed message",
                "test-correlation", "TestClass");
            var blockedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Blocked message",
                "test-correlation", "BlockedClass");
            var anotherBlockedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Another blocked message",
                "test-correlation", "AnotherBlockedClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(allowedMessage), Is.True);
            Assert.That(filter.ShouldLog(blockedMessage), Is.False);
            Assert.That(filter.ShouldLog(anotherBlockedMessage), Is.False);
        }

        [Test]
        public void SourceFilter_ShouldLog_WithRegexPattern_MatchesPattern()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["SourcePattern"] = @"^Test.*Class$";
            config.Properties["UseRegex"] = true;
            var filter = new SourceFilter(config);

            var matchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Matching message",
                "test-correlation", "TestMyClass");
            var anotherMatchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Another matching message",
                "test-correlation", "TestAnotherClass");
            var nonMatchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Non-matching message",
                "test-correlation", "MyTestClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(matchingMessage), Is.True);
            Assert.That(filter.ShouldLog(anotherMatchingMessage), Is.True);
            Assert.That(filter.ShouldLog(nonMatchingMessage), Is.False);
        }

        [Test]
        public void SourceFilter_ShouldLog_WithHierarchicalMatching_MatchesHierarchy()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["HierarchicalSources"] = new List<string> { "AhBearStudios.Core" };
            config.Properties["UseHierarchical"] = true;
            var filter = new SourceFilter(config);

            var matchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Matching message",
                "test-correlation", "AhBearStudios.Core.Logging.LoggingService");
            var anotherMatchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Another matching message",
                "test-correlation", "AhBearStudios.Core.Messaging.MessageBus");
            var nonMatchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Non-matching message",
                "test-correlation", "OtherNamespace.SomeClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(matchingMessage), Is.True);
            Assert.That(filter.ShouldLog(anotherMatchingMessage), Is.True);
            Assert.That(filter.ShouldLog(nonMatchingMessage), Is.False);
        }

        [Test]
        public void SourceFilter_Performance_EfficientPatternMatching()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["SourcePattern"] = @"^AhBearStudios\.Core\..*";
            config.Properties["UseRegex"] = true;
            var filter = new SourceFilter(config);
            var messages = new List<LogMessage>();

            for (int i = 0; i < 1000; i++)
            {
                var sourceContext = i % 2 == 0 ? "AhBearStudios.Core.TestClass" : "OtherNamespace.TestClass";
                messages.Add(TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", $"Message {i}",
                    "test-correlation", sourceContext));
            }

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var message in messages)
                {
                    filter.ShouldLog(message);
                }
            }, TimeSpan.FromMilliseconds(50)); // 50ms for 1000 regex operations
        }

        #endregion

        #region CorrelationFilter Tests

        [Test]
        public void CorrelationFilter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["RequiredCorrelationIds"] = new List<string> { "correlation-1", "correlation-2" };

            // Act
            var filter = new CorrelationFilter(config);

            // Assert
            Assert.That(filter.Name, Is.EqualTo("TestFilter"));
            Assert.That(filter.IsEnabled, Is.True);
        }

        [Test]
        public void CorrelationFilter_ShouldLog_WithRequiredCorrelationIds_FiltersCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["RequiredCorrelationIds"] = new List<string> { "correlation-1", "correlation-2" };
            var filter = new CorrelationFilter(config);

            var allowedMessage1 = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Allowed message 1",
                "correlation-1", "TestClass");
            var allowedMessage2 = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Allowed message 2",
                "correlation-2", "TestClass");
            var blockedMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Blocked message",
                "correlation-3", "TestClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(allowedMessage1), Is.True);
            Assert.That(filter.ShouldLog(allowedMessage2), Is.True);
            Assert.That(filter.ShouldLog(blockedMessage), Is.False);
        }

        [Test]
        public void CorrelationFilter_ShouldLog_WithCorrelationPattern_MatchesPattern()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["CorrelationPattern"] = @"^req-\d{4}$";
            config.Properties["UseRegex"] = true;
            var filter = new CorrelationFilter(config);

            var matchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Matching message",
                "req-1234", "TestClass");
            var anotherMatchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Another matching message",
                "req-5678", "TestClass");
            var nonMatchingMessage = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Non-matching message",
                "request-1234", "TestClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(matchingMessage), Is.True);
            Assert.That(filter.ShouldLog(anotherMatchingMessage), Is.True);
            Assert.That(filter.ShouldLog(nonMatchingMessage), Is.False);
        }

        [Test]
        public void CorrelationFilter_ShouldLog_WithEmptyCorrelationId_HandlesProperly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["RequiredCorrelationIds"] = new List<string> { "correlation-1" };
            config.Properties["AllowEmptyCorrelationId"] = false;
            var filter = new CorrelationFilter(config);

            var messageWithEmptyCorrelation = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Empty correlation",
                "", "TestClass");
            var messageWithNullCorrelation = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Null correlation",
                null, "TestClass");

            // Act & Assert
            Assert.That(filter.ShouldLog(messageWithEmptyCorrelation), Is.False);
            Assert.That(filter.ShouldLog(messageWithNullCorrelation), Is.False);
        }

        #endregion

        #region RateLimitFilter Tests

        [Test]
        public void RateLimitFilter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MaxMessagesPerSecond"] = 100;
            config.Properties["BurstSize"] = 10;

            // Act
            var filter = new RateLimitFilter(config);

            // Assert
            Assert.That(filter.Name, Is.EqualTo("TestFilter"));
            Assert.That(filter.IsEnabled, Is.True);
        }

        [Test]
        public void RateLimitFilter_ShouldLog_WithinRateLimit_AllowsMessages()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MaxMessagesPerSecond"] = 10;
            config.Properties["BurstSize"] = 5;
            var filter = new RateLimitFilter(config);

            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Rate limit test message");

            // Act & Assert
            for (int i = 0; i < 5; i++) // Within burst size
            {
                Assert.That(filter.ShouldLog(message), Is.True, 
                    $"Message {i} should be allowed within burst size");
            }
        }

        [Test]
        public void RateLimitFilter_ShouldLog_ExceedsRateLimit_BlocksMessages()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MaxMessagesPerSecond"] = 5;
            config.Properties["BurstSize"] = 3;
            var filter = new RateLimitFilter(config);

            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Rate limit test message");

            // Act
            var allowedCount = 0;
            var blockedCount = 0;

            for (int i = 0; i < 10; i++)
            {
                if (filter.ShouldLog(message))
                {
                    allowedCount++;
                }
                else
                {
                    blockedCount++;
                }
            }

            // Assert
            Assert.That(allowedCount, Is.LessThanOrEqualTo(3), "Should not exceed burst size");
            Assert.That(blockedCount, Is.GreaterThan(0), "Should block some messages");
        }

        [Test]
        public void RateLimitFilter_ShouldLog_AfterTimeWindow_ResetsLimit()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MaxMessagesPerSecond"] = 2;
            config.Properties["BurstSize"] = 1;
            var filter = new RateLimitFilter(config);

            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Rate limit reset test");

            // Act
            var initialResult = filter.ShouldLog(message);
            var secondResult = filter.ShouldLog(message); // Should be blocked
            
            // Wait for rate limit window to reset
            System.Threading.Thread.Sleep(1100); // Wait just over 1 second
            
            var afterResetResult = filter.ShouldLog(message);

            // Assert
            Assert.That(initialResult, Is.True, "First message should be allowed");
            Assert.That(secondResult, Is.False, "Second message should be blocked");
            Assert.That(afterResetResult, Is.True, "Message after reset should be allowed");
        }

        [Test]
        public void RateLimitFilter_Performance_EfficientRateLimiting()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["MaxMessagesPerSecond"] = 1000;
            config.Properties["BurstSize"] = 100;
            var filter = new RateLimitFilter(config);
            var messages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var message in messages)
                {
                    filter.ShouldLog(message);
                }
            }, TimeSpan.FromMilliseconds(10)); // 10ms for 100 rate limit checks
        }

        #endregion

        #region SamplingFilter Tests

        [Test]
        public void SamplingFilter_Constructor_WithValidConfig_InitializesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["SamplingRate"] = 0.1; // 10% sampling rate

            // Act
            var filter = new SamplingFilter(config);

            // Assert
            Assert.That(filter.Name, Is.EqualTo("TestFilter"));
            Assert.That(filter.IsEnabled, Is.True);
        }

        [Test]
        public void SamplingFilter_ShouldLog_WithSamplingRate_SamplesCorrectly()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["SamplingRate"] = 0.5; // 50% sampling rate
            var filter = new SamplingFilter(config);

            var messageCount = 1000;
            var messages = TestDataFactory.CreateLogMessageBatch(messageCount, LogLevel.Info);

            // Act
            var allowedCount = 0;
            foreach (var message in messages)
            {
                if (filter.ShouldLog(message))
                {
                    allowedCount++;
                }
            }

            // Assert
            var actualSamplingRate = (double)allowedCount / messageCount;
            Assert.That(actualSamplingRate, Is.InRange(0.4, 0.6), // 40-60% tolerance
                $"Sampling rate {actualSamplingRate:P1} should be approximately 50%");
        }

        [Test]
        public void SamplingFilter_ShouldLog_WithLevelBasedSampling_SamplesPerLevel()
        {
            // Arrange
            var config = _defaultConfig;
            config.Properties["LevelBasedSampling"] = true;
            config.Properties["DebugSamplingRate"] = 0.1;
            config.Properties["InfoSamplingRate"] = 0.5;
            config.Properties["WarningSamplingRate"] = 0.8;
            config.Properties["ErrorSamplingRate"] = 1.0;
            var filter = new SamplingFilter(config);

            var debugMessages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Debug);
            var infoMessages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Info);
            var errorMessages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Error);

            // Act
            var debugAllowed = debugMessages.Count(m => filter.ShouldLog(m));
            var infoAllowed = infoMessages.Count(m => filter.ShouldLog(m));
            var errorAllowed = errorMessages.Count(m => filter.ShouldLog(m));

            // Assert
            Assert.That(debugAllowed, Is.InRange(5, 20), "Debug messages should be sampled at ~10%");
            Assert.That(infoAllowed, Is.InRange(40, 60), "Info messages should be sampled at ~50%");
            Assert.That(errorAllowed, Is.InRange(95, 100), "Error messages should be sampled at ~100%");
        }

        #endregion

        #region Advanced Filter Tests

        [Test]
        public void LogFilterService_AddFilter_WithPriority_MaintainsOrder()
        {
            // Arrange
            var filterService = new LogFilterService();
            var highPriorityFilter = new TestFilter("HighPriority", 1);
            var mediumPriorityFilter = new TestFilter("MediumPriority", 50);
            var lowPriorityFilter = new TestFilter("LowPriority", 100);

            // Act
            filterService.AddFilter(lowPriorityFilter);
            filterService.AddFilter(highPriorityFilter);
            filterService.AddFilter(mediumPriorityFilter);

            var filters = filterService.GetFilters();

            // Assert
            Assert.That(filters.Count, Is.EqualTo(3));
            Assert.That(filters[0].Name, Is.EqualTo("HighPriority"));
            Assert.That(filters[1].Name, Is.EqualTo("MediumPriority"));
            Assert.That(filters[2].Name, Is.EqualTo("LowPriority"));
        }

        [Test]
        public void LogFilterService_ProcessMessage_WithMultipleFilters_AppliesAllFilters()
        {
            // Arrange
            var filterService = new LogFilterService();
            var levelFilter = new TestFilter("LevelFilter", 10, shouldLog: true);
            var sourceFilter = new TestFilter("SourceFilter", 20, shouldLog: true);
            var blockingFilter = new TestFilter("BlockingFilter", 30, shouldLog: false);

            filterService.AddFilter(levelFilter);
            filterService.AddFilter(sourceFilter);
            filterService.AddFilter(blockingFilter);

            var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", "Multi-filter test");

            // Act
            var result = filterService.ProcessMessage(message);

            // Assert
            Assert.That(result, Is.False, "Message should be blocked by the blocking filter");
        }

        [Test]
        public void LogFilterService_Performance_EfficientFilterExecution()
        {
            // Arrange
            var filterService = new LogFilterService();
            for (int i = 0; i < 10; i++)
            {
                filterService.AddFilter(new TestFilter($"Filter{i}", i * 10, shouldLog: true));
            }

            var messages = TestDataFactory.CreateLogMessageBatch(1000, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertExecutionTime(() =>
            {
                foreach (var message in messages)
                {
                    filterService.ProcessMessage(message);
                }
            }, TimeSpan.FromMilliseconds(100)); // 100ms for 1000 messages through 10 filters
        }

        [Test]
        public void LogFilterService_GetStatistics_ReturnsCorrectStatistics()
        {
            // Arrange
            var filterService = new LogFilterService();
            var filter1 = new TestFilter("Filter1", 10, shouldLog: true);
            var filter2 = new TestFilter("Filter2", 20, shouldLog: false);

            filterService.AddFilter(filter1);
            filterService.AddFilter(filter2);

            var messages = TestDataFactory.CreateLogMessageBatch(100, LogLevel.Info);

            // Act
            foreach (var message in messages)
            {
                filterService.ProcessMessage(message);
            }

            var statistics = filterService.GetStatistics();

            // Assert
            Assert.That(statistics.TotalFilters, Is.EqualTo(2));
            Assert.That(statistics.TotalMessagesProcessed, Is.EqualTo(100));
            Assert.That(statistics.TotalMessagesFiltered, Is.EqualTo(100)); // All filtered out by Filter2
        }

        #endregion

        #region Concurrent and Stress Tests

        [Test]
        public void AllFilters_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var levelFilter = new LevelFilter(_defaultConfig);
            var sourceFilter = new SourceFilter(_defaultConfig);
            var correlationFilter = new CorrelationFilter(_defaultConfig);
            var rateLimitFilter = new RateLimitFilter(_defaultConfig);

            var messageCount = 100;
            var taskCount = 10;

            // Act
            var tasks = new List<Task>();
            for (int t = 0; t < taskCount; t++)
            {
                var taskIndex = t;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < messageCount; i++)
                    {
                        var message = TestDataFactory.CreateLogMessage(LogLevel.Info, "Test", 
                            $"Concurrent message {taskIndex}-{i}");
                        
                        levelFilter.ShouldLog(message);
                        sourceFilter.ShouldLog(message);
                        correlationFilter.ShouldLog(message);
                        rateLimitFilter.ShouldLog(message);
                    }
                }));
            }

            // Assert
            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
        }

        [Test]
        public void AllFilters_Memory_EfficientAllocation()
        {
            // Arrange
            var levelFilter = new LevelFilter(_defaultConfig);
            var sourceFilter = new SourceFilter(_defaultConfig);
            var correlationFilter = new CorrelationFilter(_defaultConfig);
            var messages = TestDataFactory.CreateLogMessageBatch(1000, LogLevel.Info);

            // Act & Assert
            TestUtilities.AssertMemoryUsage(() =>
            {
                foreach (var message in messages)
                {
                    levelFilter.ShouldLog(message);
                    sourceFilter.ShouldLog(message);
                    correlationFilter.ShouldLog(message);
                }
            }, 1024 * 1024); // 1MB limit
        }

        #endregion

        #region Helper Classes

        private class TestFilter : ILogFilter
        {
            private readonly bool _shouldLog;

            public TestFilter(string name, int priority, bool shouldLog = true)
            {
                Name = name;
                Priority = priority;
                _shouldLog = shouldLog;
            }

            public string Name { get; }
            public int Priority { get; }

            public bool ShouldLog(in LogMessage message)
            {
                return _shouldLog;
            }
        }

        #endregion
    }
}