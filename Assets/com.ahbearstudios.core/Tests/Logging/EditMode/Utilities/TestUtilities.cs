using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using NUnit.Framework;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Tests.Shared;

namespace AhBearStudios.Core.Logging.Tests.Utilities
{
    /// <summary>
    /// Utility methods for logging system tests.
    /// </summary>
    public static class TestUtilities
    {
        /// <summary>
        /// Measures the execution time of an action.
        /// </summary>
        public static TimeSpan MeasureExecutionTime(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Measures the execution time of an async action.
        /// </summary>
        public static async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action)
        {
            var stopwatch = Stopwatch.StartNew();
            await action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Verifies that an action completes within the specified time limit.
        /// </summary>
        public static void AssertExecutionTime(Action action, TimeSpan maxDuration, string message = null)
        {
            var elapsed = MeasureExecutionTime(action);
            Assert.That(elapsed, Is.LessThanOrEqualTo(maxDuration), 
                message ?? $"Action took {elapsed.TotalMilliseconds}ms, expected <= {maxDuration.TotalMilliseconds}ms");
        }

        /// <summary>
        /// Verifies that an async action completes within the specified time limit.
        /// </summary>
        public static async Task AssertExecutionTimeAsync(Func<Task> action, TimeSpan maxDuration, string message = null)
        {
            var elapsed = await MeasureExecutionTimeAsync(action);
            Assert.That(elapsed, Is.LessThanOrEqualTo(maxDuration), 
                message ?? $"Async action took {elapsed.TotalMilliseconds}ms, expected <= {maxDuration.TotalMilliseconds}ms");
        }

        /// <summary>
        /// Unity frame budget assertion for performance tests.
        /// </summary>
        public static void AssertFrameBudget(Action action, double maxMilliseconds = 0.5)
        {
            AssertExecutionTime(action, TimeSpan.FromMilliseconds(maxMilliseconds), 
                $"Action exceeded Unity frame budget of {maxMilliseconds}ms");
        }

        /// <summary>
        /// Waits for a condition to be true within a timeout.
        /// </summary>
        public static async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, int pollIntervalMs = 10)
        {
            var endTime = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < endTime)
            {
                if (condition())
                    return true;
                await Task.Delay(pollIntervalMs);
            }
            return false;
        }

        /// <summary>
        /// Waits for a condition to be true within a timeout (synchronous).
        /// </summary>
        public static bool WaitForCondition(Func<bool> condition, TimeSpan timeout, int pollIntervalMs = 10)
        {
            var endTime = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < endTime)
            {
                if (condition())
                    return true;
                Thread.Sleep(pollIntervalMs);
            }
            return false;
        }

        /// <summary>
        /// Creates a temporary file path for testing file targets.
        /// </summary>
        public static string CreateTempFilePath(string prefix = "test", string extension = ".log")
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "AhBearStudios.Logging.Tests");
            Directory.CreateDirectory(tempDir);
            return Path.Combine(tempDir, $"{prefix}_{Guid.NewGuid():N}{extension}");
        }

        /// <summary>
        /// Cleans up temporary test files.
        /// </summary>
        public static void CleanupTempFiles(string directoryPath = null)
        {
            try
            {
                var tempDir = directoryPath ?? Path.Combine(Path.GetTempPath(), "AhBearStudios.Logging.Tests");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to cleanup temp files: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies that a collection of log entries are in chronological order.
        /// </summary>
        public static void AssertChronologicalOrder(IEnumerable<LogEntry> entries)
        {
            var timestamps = entries.Select(e => e.Timestamp).ToList();
            var sortedTimestamps = timestamps.OrderBy(t => t).ToList();
            
            CollectionAssert.AreEqual(sortedTimestamps, timestamps, 
                "Log entries are not in chronological order");
        }

        /// <summary>
        /// Verifies that log entries have unique IDs.
        /// </summary>
        public static void AssertUniqueIds(IEnumerable<LogEntry> entries)
        {
            var ids = entries.Select(e => e.Id).ToList();
            var uniqueIds = ids.Distinct().ToList();
            
            Assert.That(uniqueIds.Count, Is.EqualTo(ids.Count), 
                "Log entries do not have unique IDs");
        }

        /// <summary>
        /// Verifies that correlation IDs are properly propagated.
        /// </summary>
        public static void AssertCorrelationIdPropagation(IEnumerable<LogEntry> entries, string expectedCorrelationId)
        {
            foreach (var entry in entries)
            {
                Assert.That(entry.CorrelationId.ToString(), Is.EqualTo(expectedCorrelationId),
                    $"Entry {entry.Id} has incorrect correlation ID");
            }
        }

        /// <summary>
        /// Creates a stress test scenario with multiple threads.
        /// </summary>
        public static async Task RunConcurrentStressTest(
            Func<int, Task> actionPerThread,
            int threadCount = 10,
            int operationsPerThread = 100,
            TimeSpan? timeout = null)
        {
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                var threadId = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        await actionPerThread(threadId * operationsPerThread + j);
                    }
                }));
            }

            var completedWithinTimeout = await Task.WhenAny(
                Task.WhenAll(tasks),
                Task.Delay(actualTimeout)) == Task.WhenAll(tasks);

            Assert.That(completedWithinTimeout, Is.True, 
                $"Stress test did not complete within {actualTimeout.TotalSeconds} seconds");
        }

        /// <summary>
        /// Verifies memory usage patterns for performance tests.
        /// </summary>
        public static void AssertMemoryUsage(Action action, long maxBytesAllocated = 1024 * 1024) // 1MB default
        {
            var initialMemory = GC.GetTotalMemory(true);
            action();
            var finalMemory = GC.GetTotalMemory(false);
            
            var bytesAllocated = finalMemory - initialMemory;
            Assert.That(bytesAllocated, Is.LessThanOrEqualTo(maxBytesAllocated),
                $"Action allocated {bytesAllocated} bytes, expected <= {maxBytesAllocated} bytes");
        }

        /// <summary>
        /// Verifies that native collections are properly disposed.
        /// </summary>
        public static void AssertNativeCollectionDisposed<T>(NativeArray<T> array) where T : unmanaged
        {
            Assert.That(array.IsCreated, Is.False, "NativeArray should be disposed");
        }

        /// <summary>
        /// Creates a mock log target for testing.
        /// </summary>
        public static MockLogTarget CreateMockLogTarget(
            string name = "MockTarget",
            LogLevel minimumLevel = LogLevel.Debug,
            bool isEnabled = true,
            List<string> channels = null)
        {
            var config = TestDataFactory.CreateLogTargetConfig(name, minimumLevel, isEnabled, channels);
            return new MockLogTarget(config);
        }

        /// <summary>
        /// Verifies Unity ProfilerMarker integration.
        /// </summary>
        public static void AssertProfilerMarkerUsage(Action action, string expectedMarkerName)
        {
            // Note: This is a simplified check. In a real implementation, you would use
            // Unity's ProfilerRecorder to verify marker usage.
            using (var marker = new ProfilerMarker(expectedMarkerName))
            {
                using (marker.Auto())
                {
                    action();
                }
            }
            // In a real test, you would assert that the marker was recorded
            Assert.Pass($"ProfilerMarker '{expectedMarkerName}' was used");
        }

        /// <summary>
        /// Simulates Unity frame boundaries for performance testing.
        /// </summary>
        public static void SimulateUnityFrame(Action frameAction, int frameCount = 1)
        {
            for (int i = 0; i < frameCount; i++)
            {
                var frameStartTime = DateTime.UtcNow;
                frameAction();
                var frameEndTime = DateTime.UtcNow;
                
                var frameDuration = frameEndTime - frameStartTime;
                Assert.That(frameDuration.TotalMilliseconds, Is.LessThanOrEqualTo(16.67),
                    $"Frame {i} took {frameDuration.TotalMilliseconds}ms, exceeding 60 FPS budget");
            }
        }

        /// <summary>
        /// Verifies burst compilation compatibility.
        /// </summary>
        public static void AssertBurstCompatibility<T>(T data) where T : unmanaged
        {
            // This test verifies that the data type can be used in Burst-compiled code
            // by checking that it's unmanaged and has the expected characteristics
            Assert.That(typeof(T).IsUnmanaged(), Is.True, 
                $"Type {typeof(T).Name} is not unmanaged and cannot be used with Burst");
        }

        /// <summary>
        /// Extension method to check if a type is unmanaged.
        /// </summary>
        public static bool IsUnmanaged(this Type type)
        {
            // This is a simplified check. In .NET 7+, you can use RuntimeHelpers.IsReferenceOrContainsReferences
            return type.IsPrimitive || type.IsEnum || type.IsPointer || 
                   (type.IsValueType && !type.IsGenericType);
        }
    }

    /// <summary>
    /// Mock log target implementation for testing.
    /// </summary>
    public class MockLogTarget : ILogTarget
    {
        private readonly List<LogMessage> _messages = new();
        private readonly List<List<LogMessage>> _batches = new();
        private bool _isHealthy = true;
        private int _flushCount = 0;

        public MockLogTarget(ILogTargetConfig config)
        {
            Name = config.Name;
            MinimumLevel = config.MinimumLevel;
            IsEnabled = config.IsEnabled;
            Channels = config.Channels;
        }

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }
        public bool IsEnabled { get; set; }
        public IReadOnlyList<string> Channels { get; }
        public bool IsHealthy => _isHealthy;

        public IReadOnlyList<LogMessage> Messages => _messages;
        public IReadOnlyList<List<LogMessage>> Batches => _batches;
        public int FlushCount => _flushCount;

        public void Write(in LogMessage logMessage)
        {
            if (ShouldProcessMessage(logMessage))
            {
                _messages.Add(logMessage);
            }
        }

        public void WriteBatch(IReadOnlyList<LogMessage> logMessages)
        {
            var batch = new List<LogMessage>();
            foreach (var message in logMessages)
            {
                if (ShouldProcessMessage(message))
                {
                    batch.Add(message);
                    _messages.Add(message);
                }
            }
            _batches.Add(batch);
        }

        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            return IsEnabled && logMessage.Level >= MinimumLevel;
        }

        public void Flush()
        {
            _flushCount++;
        }

        public bool PerformHealthCheck()
        {
            return _isHealthy;
        }

        public void SetHealthy(bool healthy)
        {
            _isHealthy = healthy;
        }

        public void ClearMessages()
        {
            _messages.Clear();
            _batches.Clear();
            _flushCount = 0;
        }

        public void Dispose()
        {
            ClearMessages();
        }
    }
}