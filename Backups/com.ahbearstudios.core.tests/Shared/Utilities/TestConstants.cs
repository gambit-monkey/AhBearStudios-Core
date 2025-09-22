using System;

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

        #region Helper Methods

        /// <summary>
        /// Creates a test GUID with a specific pattern for easy identification in tests.
        /// </summary>
        /// <param name="testContext">The test context name</param>
        /// <returns>A deterministic test GUID</returns>
        public static Guid CreateTestGuid(string testContext)
        {
            return AhBearStudios.Core.Common.Utilities.DeterministicIdGenerator
                .GenerateCorrelationId(TestCorrelationPrefix, testContext ?? "Default");
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

        #endregion
    }
}