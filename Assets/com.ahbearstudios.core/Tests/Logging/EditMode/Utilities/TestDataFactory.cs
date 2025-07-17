using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Messaging.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Tests.Utilities
{
    /// <summary>
    /// Factory for creating consistent test data for logging system tests.
    /// </summary>
    public static class TestDataFactory
    {
        /// <summary>
        /// Creates a test LogEntry with default values.
        /// </summary>
        public static LogEntry CreateLogEntry(
            LogLevel level = LogLevel.Info,
            string channel = "Test",
            string message = "Test message",
            string correlationId = null,
            string sourceContext = null,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            return LogEntry.Create(
                level: level,
                channel: channel,
                message: message,
                correlationId: correlationId ?? Guid.NewGuid().ToString("N")[..8],
                sourceContext: sourceContext ?? "TestClass",
                exception: exception,
                properties: properties);
        }

        /// <summary>
        /// Creates a test NativeLogEntry with default values.
        /// </summary>
        [BurstCompile]
        public static NativeLogEntry CreateNativeLogEntry(
            LogLevel level = LogLevel.Info,
            string channel = "Test",
            string message = "Test message",
            string correlationId = null,
            string sourceContext = null)
        {
            return NativeLogEntry.Create(
                level: level,
                channel: new FixedString64Bytes(channel),
                message: new FixedString512Bytes(message),
                correlationId: new FixedString128Bytes(correlationId ?? Guid.NewGuid().ToString("N")[..8]),
                sourceContext: new FixedString128Bytes(sourceContext ?? "TestClass"));
        }

        /// <summary>
        /// Creates a test LogMessage with default values.
        /// </summary>
        public static LogMessage CreateLogMessage(
            LogLevel level = LogLevel.Info,
            string channel = "Test",
            string message = "Test message",
            string correlationId = null,
            string sourceContext = null,
            Exception exception = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            return LogMessage.Create(
                level: level,
                channel: channel,
                message: message,
                correlationId: correlationId ?? Guid.NewGuid().ToString("N")[..8],
                sourceContext: sourceContext ?? "TestClass",
                exception: exception,
                properties: properties);
        }

        /// <summary>
        /// Creates a test LogTargetConfig with default values.
        /// </summary>
        public static ILogTargetConfig CreateLogTargetConfig(
            string name = "TestTarget",
            LogLevel minimumLevel = LogLevel.Debug,
            bool isEnabled = true,
            List<string> channels = null,
            bool useAsyncWrite = false,
            int maxConcurrentAsyncOperations = 1,
            Dictionary<string, object> properties = null)
        {
            return new TestLogTargetConfig
            {
                Name = name,
                MinimumLevel = minimumLevel,
                IsEnabled = isEnabled,
                Channels = channels ?? new List<string> { "Test" },
                UseAsyncWrite = useAsyncWrite,
                MaxConcurrentAsyncOperations = maxConcurrentAsyncOperations,
                Properties = properties ?? new Dictionary<string, object>(),
                HealthCheckIntervalSeconds = 30,
                ErrorRateThreshold = 0.1,
                EnablePerformanceMetrics = true,
                FrameBudgetThresholdMs = 0.5
            };
        }

        /// <summary>
        /// Creates a batch of test LogEntry objects.
        /// </summary>
        public static List<LogEntry> CreateLogEntryBatch(
            int count = 10,
            LogLevel level = LogLevel.Info,
            string channelPrefix = "Test",
            string messagePrefix = "Test message")
        {
            var entries = new List<LogEntry>();
            for (int i = 0; i < count; i++)
            {
                entries.Add(CreateLogEntry(
                    level: level,
                    channel: $"{channelPrefix}{i}",
                    message: $"{messagePrefix} {i}",
                    correlationId: Guid.NewGuid().ToString("N")[..8]));
            }
            return entries;
        }

        /// <summary>
        /// Creates a batch of test LogMessage objects.
        /// </summary>
        public static List<LogMessage> CreateLogMessageBatch(
            int count = 10,
            LogLevel level = LogLevel.Info,
            string channelPrefix = "Test",
            string messagePrefix = "Test message")
        {
            var messages = new List<LogMessage>();
            for (int i = 0; i < count; i++)
            {
                messages.Add(CreateLogMessage(
                    level: level,
                    channel: $"{channelPrefix}{i}",
                    message: $"{messagePrefix} {i}",
                    correlationId: Guid.NewGuid().ToString("N")[..8]));
            }
            return messages;
        }

        /// <summary>
        /// Creates test structured properties for logging.
        /// </summary>
        public static Dictionary<string, object> CreateTestProperties(
            string userId = "test-user",
            string sessionId = "test-session",
            int requestId = 12345)
        {
            return new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["SessionId"] = sessionId,
                ["RequestId"] = requestId,
                ["Timestamp"] = DateTime.UtcNow,
                ["Environment"] = "Test",
                ["Version"] = "1.0.0-test"
            };
        }

        /// <summary>
        /// Creates test exceptions for logging.
        /// </summary>
        public static Exception CreateTestException(
            string message = "Test exception",
            string stackTrace = null)
        {
            try
            {
                throw new InvalidOperationException(message);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// Creates test exceptions with inner exceptions.
        /// </summary>
        public static Exception CreateTestExceptionWithInner(
            string message = "Test exception",
            string innerMessage = "Inner test exception")
        {
            try
            {
                try
                {
                    throw new ArgumentException(innerMessage);
                }
                catch (Exception inner)
                {
                    throw new InvalidOperationException(message, inner);
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// Creates correlation IDs for testing.
        /// </summary>
        public static FixedString64Bytes CreateCorrelationId(string prefix = "test")
        {
            return new FixedString64Bytes($"{prefix}-{Guid.NewGuid().ToString("N")[..8]}");
        }

        /// <summary>
        /// Creates a collection of different log levels for testing.
        /// </summary>
        public static LogLevel[] GetAllLogLevels()
        {
            return new[] { LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error, LogLevel.Critical, LogLevel.Trace };
        }

        /// <summary>
        /// Creates performance test data for high-volume scenarios.
        /// </summary>
        public static List<LogEntry> CreatePerformanceTestData(
            int count = 1000,
            bool includeExceptions = false,
            bool includeProperties = false)
        {
            var entries = new List<LogEntry>();
            var random = new Random(42); // Fixed seed for reproducible tests
            var levels = GetAllLogLevels();

            for (int i = 0; i < count; i++)
            {
                var level = levels[random.Next(levels.Length)];
                var exception = includeExceptions && random.NextDouble() < 0.1 ? CreateTestException() : null;
                var properties = includeProperties && random.NextDouble() < 0.3 ? CreateTestProperties() : null;

                entries.Add(CreateLogEntry(
                    level: level,
                    channel: $"PerfTest{i % 10}",
                    message: $"Performance test message {i}",
                    correlationId: Guid.NewGuid().ToString("N")[..8],
                    sourceContext: $"PerfTestClass{i % 5}",
                    exception: exception,
                    properties: properties));
            }

            return entries;
        }

        /// <summary>
        /// Creates Unity-specific test data for Job System compatibility.
        /// </summary>
        public static NativeArray<NativeLogEntry> CreateNativeLogEntryArray(
            int count = 10,
            Allocator allocator = Allocator.Temp)
        {
            var array = new NativeArray<NativeLogEntry>(count, allocator);
            
            for (int i = 0; i < count; i++)
            {
                array[i] = CreateNativeLogEntry(
                    level: LogLevel.Info,
                    channel: $"Native{i}",
                    message: $"Native test message {i}",
                    correlationId: Guid.NewGuid().ToString("N")[..8],
                    sourceContext: "NativeTestClass");
            }

            return array;
        }
    }

    /// <summary>
    /// Test implementation of ILogTargetConfig for testing.
    /// </summary>
    public class TestLogTargetConfig : ILogTargetConfig
    {
        public string Name { get; set; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public List<string> Channels { get; set; }
        public bool UseAsyncWrite { get; set; }
        public int MaxConcurrentAsyncOperations { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public int HealthCheckIntervalSeconds { get; set; }
        public double ErrorRateThreshold { get; set; }
        public bool EnablePerformanceMetrics { get; set; }
        public double FrameBudgetThresholdMs { get; set; }
    }
}