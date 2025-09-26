using System;
using System.Collections.Generic;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Fakes;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Spies;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;
using Unity.Collections;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Shared constants and values used across all test classes.
    /// Provides consistent test data and configuration values.
    /// </summary>
    public static class TestConstants
    {
        #region Test Timing Constants

        /// <summary>
        /// Unity 60 FPS frame budget: 16.67ms
        /// </summary>
        public static readonly TimeSpan FrameBudget = TimeSpan.FromMilliseconds(16.67);

        /// <summary>
        /// Warning threshold for operations approaching frame budget: 10ms
        /// </summary>
        public static readonly TimeSpan WarningThreshold = TimeSpan.FromMilliseconds(10.0);

        /// <summary>
        /// Default timeout for async test operations: 5 seconds
        /// </summary>
        public static readonly TimeSpan DefaultAsyncTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Short delay for testing timing-sensitive operations: 100ms
        /// </summary>
        public static readonly TimeSpan ShortDelay = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Medium delay for integration tests: 500ms
        /// </summary>
        public static readonly TimeSpan MediumDelay = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Default polling interval for condition checking: 50ms
        /// </summary>
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(50);

        #endregion

        #region Test Data Constants

        /// <summary>
        /// Default test correlation ID prefix
        /// </summary>
        public const string TestCorrelationPrefix = "Test";

        /// <summary>
        /// Default test source name
        /// </summary>
        public const string TestSource = "TestSystem";

        /// <summary>
        /// Default test user name
        /// </summary>
        public const string TestUser = "TestUser";

        /// <summary>
        /// Default test operation name
        /// </summary>
        public const string TestOperation = "TestOperation";

        /// <summary>
        /// Sample alert message for testing
        /// </summary>
        public const string SampleAlertMessage = "Test alert message";

        /// <summary>
        /// Sample error message for testing
        /// </summary>
        public const string SampleErrorMessage = "Test error occurred";

        /// <summary>
        /// Sample tag for testing
        /// </summary>
        public const string SampleTag = "TestTag";

        #endregion

        #region Performance Test Constants

        /// <summary>
        /// Maximum acceptable allocation for non-critical operations: 1KB
        /// </summary>
        public const long MaxAcceptableAllocation = 1024;

        /// <summary>
        /// Default number of iterations for performance tests
        /// </summary>
        public const int DefaultPerformanceIterations = 100;

        /// <summary>
        /// Default number of iterations for stress tests
        /// </summary>
        public const int DefaultStressTestIterations = 1000;

        /// <summary>
        /// Maximum memory usage for single operation: 1MB
        /// </summary>
        public const double MaxMemoryUsageMB = 1.0;

        /// <summary>
        /// Default batch size for bulk operation tests
        /// </summary>
        public const int DefaultBatchSize = 50;

        #endregion

        #region Alert System Test Constants

        /// <summary>
        /// Default minimum severity for testing
        /// </summary>
        public const AlertSeverity DefaultMinimumSeverity = AlertSeverity.Info;

        /// <summary>
        /// Maximum number of active alerts to test
        /// </summary>
        public const int MaxActiveAlerts = 1000;

        /// <summary>
        /// Default alert history size for testing
        /// </summary>
        public const int DefaultHistorySize = 100;

        /// <summary>
        /// Circuit breaker failure threshold for testing
        /// </summary>
        public const int CircuitBreakerThreshold = 5;

        /// <summary>
        /// Default health check interval for testing
        /// </summary>
        public static readonly TimeSpan DefaultHealthCheckInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Default maintenance interval for testing
        /// </summary>
        public static readonly TimeSpan DefaultMaintenanceInterval = TimeSpan.FromMinutes(5);

        #endregion

        #region Mock Service Configuration

        /// <summary>
        /// Default mock service response delay
        /// </summary>
        public static readonly TimeSpan MockServiceDelay = TimeSpan.FromMilliseconds(1);

        /// <summary>
        /// Default capacity for mock pooling service
        /// </summary>
        public const int MockPoolInitialCapacity = 10;

        /// <summary>
        /// Maximum capacity for mock pooling service
        /// </summary>
        public const int MockPoolMaxCapacity = 100;

        /// <summary>
        /// Default mock health status
        /// </summary>
        public const bool DefaultMockHealthStatus = true;

        #endregion

        #region File and Path Constants

        /// <summary>
        /// Test configuration file name
        /// </summary>
        public const string TestConfigFileName = "test-config.json";

        /// <summary>
        /// Test log file name
        /// </summary>
        public const string TestLogFileName = "test.log";

        /// <summary>
        /// Test data directory name
        /// </summary>
        public const string TestDataDirectory = "TestData";

        #endregion

        #region Message Bus Test Constants

        /// <summary>
        /// Default message priority for testing
        /// </summary>
        public const MessagePriority DefaultMessagePriority = MessagePriority.Normal;

        /// <summary>
        /// Maximum message batch size for testing
        /// </summary>
        public const int MaxMessageBatchSize = 100;

        /// <summary>
        /// Default message processing timeout
        /// </summary>
        public static readonly TimeSpan MessageProcessingTimeout = TimeSpan.FromSeconds(1);

        #endregion

        #region Validation Constants

        /// <summary>
        /// Minimum acceptable correlation ID value
        /// </summary>
        public static readonly Guid MinCorrelationId = Guid.Empty;

        /// <summary>
        /// Maximum string length for test data
        /// </summary>
        public const int MaxTestStringLength = 1000;

        /// <summary>
        /// Minimum required string length for test data
        /// </summary>
        public const int MinTestStringLength = 1;

        #endregion

        #region Unity Collections Constants

        /// <summary>
        /// Maximum FixedString64Bytes length
        /// </summary>
        public const int MaxFixedString64Length = 64;

        /// <summary>
        /// Maximum FixedString32Bytes length
        /// </summary>
        public const int MaxFixedString32Length = 32;

        /// <summary>
        /// Maximum FixedString512Bytes length
        /// </summary>
        public const int MaxFixedString512Length = 512;

        #endregion

        #region Correlation Tracking Constants

        /// <summary>
        /// Default correlation context for test operations
        /// </summary>
        public const string DefaultCorrelationContext = "TestContext";

        /// <summary>
        /// Maximum correlation chain depth for distributed testing
        /// </summary>
        public const int MaxCorrelationChainDepth = 10;

        /// <summary>
        /// Correlation ID pattern for test identification
        /// </summary>
        public const string CorrelationIdPattern = "TEST-{0:N}";

        /// <summary>
        /// Default correlation timeout for async operations
        /// </summary>
        public static readonly TimeSpan CorrelationTimeout = TimeSpan.FromSeconds(30);

        #endregion

        #region Zero-Allocation Pattern Constants

        /// <summary>
        /// Maximum allocations allowed for zero-allocation operations
        /// </summary>
        public const long ZeroAllocationThreshold = 0;

        /// <summary>
        /// Tolerance for allocation measurements (in bytes)
        /// </summary>
        public const long AllocationMeasurementTolerance = 32;

        /// <summary>
        /// Default Unity Collections NativeArray capacity for tests
        /// </summary>
        public const int DefaultNativeArrayCapacity = 100;

        /// <summary>
        /// Maximum buffer size for Unity Collections zero-allocation patterns
        /// </summary>
        public const int MaxUnityCollectionsBuffer = 4096;

        #endregion

        #region TDD Test Double Constants

        /// <summary>
        /// Default logging capacity for StubLoggingService in tests
        /// </summary>
        public const int DefaultLoggingCapacity = 1000;

        /// <summary>
        /// Default message bus capacity for SpyMessageBusService in tests
        /// </summary>
        public const int DefaultMessageBusCapacity = 500;

        /// <summary>
        /// Default serialization buffer size for FakeSerializationService
        /// </summary>
        public const int DefaultSerializationBufferSize = 8192;

        /// <summary>
        /// Default health check timeout for StubHealthCheckService
        /// </summary>
        public static readonly TimeSpan DefaultHealthCheckTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Maximum number of interactions to record in spy services
        /// </summary>
        public const int MaxSpyInteractions = 10000;

        /// <summary>
        /// Default test double configuration name
        /// </summary>
        public const string DefaultTestDoubleConfig = "TestDoubleConfig";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test GUID with a specific pattern for easy identification in tests.
        /// Uses DeterministicIdGenerator for consistent test correlation tracking.
        /// </summary>
        /// <param name="testContext">The test context name</param>
        /// <returns>A deterministic test GUID</returns>
        public static Guid CreateTestGuid(string testContext)
        {
            return DeterministicIdGenerator.GenerateCorrelationId(TestCorrelationPrefix, testContext ?? "Default");
        }

        /// <summary>
        /// Creates a correlated test GUID chain for distributed system testing.
        /// Essential for CLAUDETESTS.md compliance and correlation tracking.
        /// </summary>
        /// <param name="baseContext">Base context for the correlation chain</param>
        /// <param name="chainLength">Number of correlated IDs to create</param>
        /// <returns>List of correlated test GUIDs</returns>
        public static List<Guid> CreateCorrelatedTestGuids(string baseContext, int chainLength = 3)
        {
            var correlatedIds = new List<Guid>();
            for (int i = 0; i < Math.Min(chainLength, MaxCorrelationChainDepth); i++)
            {
                var context = $"{baseContext}_Step_{i}";
                correlatedIds.Add(DeterministicIdGenerator.GenerateCorrelationId(TestCorrelationPrefix, context));
            }
            return correlatedIds;
        }

        /// <summary>
        /// Creates a test correlation ID with specific pattern for easy identification.
        /// Follows CLAUDETESTS.md guidelines for correlation tracking.
        /// </summary>
        /// <param name="operationName">Name of the test operation</param>
        /// <param name="testInstance">Test instance identifier</param>
        /// <returns>Formatted correlation ID</returns>
        public static Guid CreateTestCorrelationId(string operationName, string testInstance = null)
        {
            var instance = testInstance ?? DateTime.UtcNow.Ticks.ToString();
            return DeterministicIdGenerator.GenerateCorrelationId(operationName, instance);
        }

        /// <summary>
        /// Validates that a string fits within Unity Collections constraints.
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <returns>True if the string is valid</returns>
        public static bool IsValidFixedString(string value, int maxLength)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.Length <= maxLength &&
                   value.Length >= MinTestStringLength;
        }

        /// <summary>
        /// Truncates a string to fit within Unity Collections constraints.
        /// </summary>
        /// <param name="value">The string to truncate</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <returns>Truncated string</returns>
        public static string TruncateForFixedString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Creates a FixedString64Bytes for Unity Collections zero-allocation patterns.
        /// Validates string length and truncates if necessary.
        /// </summary>
        /// <param name="value">The string value to convert</param>
        /// <returns>FixedString64Bytes instance</returns>
        public static FixedString64Bytes CreateFixedString64(string value)
        {
            var truncated = TruncateForFixedString(value, MaxFixedString64Length);
            return new FixedString64Bytes(truncated);
        }

        /// <summary>
        /// Creates a FixedString32Bytes for Unity Collections zero-allocation patterns.
        /// </summary>
        /// <param name="value">The string value to convert</param>
        /// <returns>FixedString32Bytes instance</returns>
        public static FixedString32Bytes CreateFixedString32(string value)
        {
            var truncated = TruncateForFixedString(value, MaxFixedString32Length);
            return new FixedString32Bytes(truncated);
        }

        /// <summary>
        /// Validates Unity Collections allocation patterns for zero-allocation compliance.
        /// Essential for Unity game development performance requirements.
        /// </summary>
        /// <param name="operation">Operation to validate</param>
        /// <param name="operationName">Name for reporting</param>
        /// <returns>True if operation produces zero allocations</returns>
        public static bool ValidateZeroAllocationPattern(System.Action operation, string operationName)
        {
            // Force cleanup before measurement
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            var initialMemory = System.GC.GetTotalMemory(false);

            try
            {
                operation();
            }
            catch
            {
                return false; // Failed operations don't count as zero-allocation
            }

            var finalMemory = System.GC.GetTotalMemory(false);
            var allocatedBytes = Math.Max(0, finalMemory - initialMemory);

            return allocatedBytes <= AllocationMeasurementTolerance;
        }

        #endregion

        #region TDD Test Double Factory Methods

        /// <summary>
        /// Creates a pre-configured StubLoggingService for consistent testing.
        /// Follows CLAUDETESTS.md guidelines for lightweight test doubles.
        /// </summary>
        /// <param name="capacity">Log capacity (default from constants)</param>
        /// <returns>Configured StubLoggingService instance</returns>
        public static StubLoggingService CreateStubLoggingService(int capacity = DefaultLoggingCapacity)
        {
            return new StubLoggingService();
        }

        /// <summary>
        /// Creates a pre-configured SpyMessageBusService for interaction recording.
        /// </summary>
        /// <returns>Configured SpyMessageBusService instance</returns>
        public static SpyMessageBusService CreateSpyMessageBusService()
        {
            return new SpyMessageBusService();
        }

        /// <summary>
        /// Creates a pre-configured FakeSerializationService for simplified serialization testing.
        /// </summary>
        /// <returns>Configured FakeSerializationService instance</returns>
        public static FakeSerializationService CreateFakeSerializationService()
        {
            return new FakeSerializationService();
        }

        /// <summary>
        /// Creates a pre-configured StubHealthCheckService for health monitoring tests.
        /// </summary>
        /// <returns>Configured StubHealthCheckService instance</returns>
        public static StubHealthCheckService CreateStubHealthCheckService()
        {
            return new StubHealthCheckService();
        }

        /// <summary>
        /// Creates a complete set of test doubles for integration testing.
        /// Provides all necessary TDD-compliant test doubles following CLAUDETESTS.md guidelines.
        /// </summary>
        /// <param name="correlationId">Optional correlation ID for tracking</param>
        /// <returns>Dictionary of configured test doubles</returns>
        public static Dictionary<System.Type, object> CreateTestDoubleSet(Guid correlationId = default)
        {
            var testDoubles = new Dictionary<System.Type, object>
            {
                { typeof(StubLoggingService), CreateStubLoggingService() },
                { typeof(SpyMessageBusService), CreateSpyMessageBusService() },
                { typeof(FakeSerializationService), CreateFakeSerializationService() },
                { typeof(StubHealthCheckService), CreateStubHealthCheckService() },
                { typeof(NullProfilerService), NullProfilerService.Instance }
            };

            return testDoubles;
        }

        /// <summary>
        /// Validates frame budget compliance for test operations.
        /// Ensures operations complete within Unity's 60 FPS target (16.67ms).
        /// </summary>
        /// <param name="operation">Operation to validate</param>
        /// <param name="operationName">Name for reporting</param>
        /// <param name="customBudget">Custom budget (optional, defaults to FrameBudget)</param>
        /// <returns>True if operation completes within budget</returns>
        public static bool ValidateFrameBudgetCompliance(System.Action operation, string operationName, TimeSpan? customBudget = null)
        {
            var budget = customBudget ?? FrameBudget;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
            }

            return stopwatch.Elapsed <= budget;
        }

        /// <summary>
        /// Creates test data for stress testing with realistic game load patterns.
        /// Generates data suitable for 1000+ operation stress tests.
        /// </summary>
        /// <typeparam name="T">Type of test data to create</typeparam>
        /// <param name="factory">Factory function to create test data</param>
        /// <param name="count">Number of items to create</param>
        /// <returns>List of test data items</returns>
        public static List<T> CreateStressTestData<T>(System.Func<int, T> factory, int count = DefaultStressTestIterations)
        {
            var data = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                data.Add(factory(i));
            }
            return data;
        }

        #endregion
    }
}