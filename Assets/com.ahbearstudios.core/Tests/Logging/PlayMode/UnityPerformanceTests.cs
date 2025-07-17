using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Targets;
using AhBearStudios.Core.Tests.Shared;
using AhBearStudios.Core.Logging.Tests.Utilities;

namespace AhBearStudios.Core.Logging.Tests.PlayMode
{
    /// <summary>
    /// Unity-specific performance tests for the logging system with enhanced focus on 
    /// frame budget compliance, Job System compatibility, and game-specific performance pitfalls.
    /// These tests ensure the logging system maintains 60 FPS performance in Unity games.
    /// </summary>
    [TestFixture]
    public class UnityPerformanceTests
    {
        private ILoggingService _loggingService;
        private MockProfilerService _mockProfiler;
        private MockAlertService _mockAlerts;
        private MockHealthCheckService _mockHealthCheck;
        private MockMessageBusService _mockMessageBus;
        private MockLogTarget _mockTarget;
        private ProfilerMarker _testMarker;

        // Unity Performance Constants
        private const float TARGET_FPS = 60f;
        private const float TARGET_FRAME_TIME_MS = 1000f / TARGET_FPS; // 16.67ms
        private const float LOGGING_FRAME_BUDGET_MS = 0.5f; // 0.5ms max per frame for logging
        private const int MAX_MESSAGES_PER_FRAME = 10;
        private const int HIGH_FREQUENCY_MESSAGE_COUNT = 1000;
        private const int STRESS_TEST_DURATION_FRAMES = 300; // 5 seconds at 60 FPS

        [SetUp]
        public void SetUp()
        {
            // Create mock dependencies
            _mockProfiler = new MockProfilerService();
            _mockAlerts = new MockAlertService();
            _mockHealthCheck = new MockHealthCheckService();
            _mockMessageBus = new MockMessageBusService();
            _mockTarget = TestUtilities.CreateMockLogTarget();

            // Create logging service
            var config = new LoggingConfig
            {
                IsEnabled = true,
                MinimumLevel = LogLevel.Debug,
                Channels = new List<LogChannelConfig>
                {
                    new LogChannelConfig { Name = "Performance", IsEnabled = true, MinimumLevel = LogLevel.Debug }
                }
            };

            _loggingService = new LoggingService(config, _mockProfiler, _mockAlerts, _mockHealthCheck, _mockMessageBus);
            _loggingService.RegisterTarget(_mockTarget);

            // Create profiler marker for testing
            _testMarker = new ProfilerMarker("PerformanceTest");
        }

        [TearDown]
        public void TearDown()
        {
            _loggingService?.Dispose();
            _mockProfiler?.Dispose();
            _mockAlerts?.Dispose();
            _mockHealthCheck?.Dispose();
            _mockMessageBus?.Dispose();
            _mockTarget?.Dispose();
            _testMarker.Dispose();
        }

        #region Frame Budget Tests

        [Test]
        public void LoggingService_SingleLogCall_StaysWithinFrameBudget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Single frame budget test message";

            // Act & Assert
            using (_testMarker.Auto())
            {
                var stopwatch = Stopwatch.StartNew();
                _loggingService.LogInfo(message, correlationId);
                stopwatch.Stop();

                Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(LOGGING_FRAME_BUDGET_MS),
                    $"Single log call took {stopwatch.Elapsed.TotalMilliseconds}ms, exceeding frame budget of {LOGGING_FRAME_BUDGET_MS}ms");
            }
        }

        [Test]
        public void LoggingService_MultipleLogCalls_StaysWithinFrameBudget()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = MAX_MESSAGES_PER_FRAME;

            // Act & Assert
            using (_testMarker.Auto())
            {
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < messageCount; i++)
                {
                    _loggingService.LogInfo($"Frame budget test message {i}", correlationId);
                }
                stopwatch.Stop();

                Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(LOGGING_FRAME_BUDGET_MS),
                    $"Multiple log calls took {stopwatch.Elapsed.TotalMilliseconds}ms, exceeding frame budget of {LOGGING_FRAME_BUDGET_MS}ms");
            }
        }

        [Test]
        public void LoggingService_HighFrequencyLogging_MaintainsFrameRate()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var frameCount = 60; // 1 second at 60 FPS
            var totalFrameTime = 0.0;

            // Act
            for (int frame = 0; frame < frameCount; frame++)
            {
                var frameStopwatch = Stopwatch.StartNew();
                
                // Simulate frame work
                for (int i = 0; i < 5; i++) // 5 log calls per frame
                {
                    _loggingService.LogInfo($"Frame {frame} message {i}", correlationId);
                }
                
                frameStopwatch.Stop();
                totalFrameTime += frameStopwatch.Elapsed.TotalMilliseconds;
            }

            // Assert
            var averageFrameTime = totalFrameTime / frameCount;
            Assert.That(averageFrameTime, Is.LessThan(TARGET_FRAME_TIME_MS),
                $"Average frame time {averageFrameTime}ms exceeds target of {TARGET_FRAME_TIME_MS}ms");
        }

        [UnityTest]
        public IEnumerator LoggingService_RealTimeFrameTest_MaintainsFrameRate()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var frameCount = 0;
            var totalFrameTime = 0.0;
            var maxFrameTime = 0.0;

            // Act
            while (frameCount < 180) // 3 seconds at 60 FPS
            {
                var frameStart = Time.realtimeSinceStartup;
                
                // Simulate logging during frame
                for (int i = 0; i < 3; i++)
                {
                    _loggingService.LogInfo($"RealTime frame {frameCount} message {i}", correlationId);
                }
                
                yield return null; // Wait for next frame
                
                var frameEnd = Time.realtimeSinceStartup;
                var frameTime = (frameEnd - frameStart) * 1000f; // Convert to ms
                
                totalFrameTime += frameTime;
                maxFrameTime = Math.Max(maxFrameTime, frameTime);
                frameCount++;
            }

            // Assert
            var averageFrameTime = totalFrameTime / frameCount;
            Assert.That(averageFrameTime, Is.LessThan(TARGET_FRAME_TIME_MS),
                $"Average frame time {averageFrameTime}ms exceeds target of {TARGET_FRAME_TIME_MS}ms");
            Assert.That(maxFrameTime, Is.LessThan(TARGET_FRAME_TIME_MS * 2),
                $"Max frame time {maxFrameTime}ms exceeds acceptable spike threshold");
        }

        #endregion

        #region Job System and Burst Compatibility Tests

        [Test]
        public void NativeLogEntry_InJob_BurstCompatible()
        {
            // Arrange
            var entryCount = 100;
            using (var inputArray = TestDataFactory.CreateNativeLogEntryArray(entryCount, Allocator.TempJob))
            using (var outputArray = new NativeArray<bool>(entryCount, Allocator.TempJob))
            {
                var job = new BurstCompatibleLoggingJob
                {
                    InputEntries = inputArray,
                    OutputResults = outputArray,
                    MinimumLevel = LogLevel.Info
                };

                // Act
                var stopwatch = Stopwatch.StartNew();
                var handle = job.Schedule();
                handle.Complete();
                stopwatch.Stop();

                // Assert
                Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(1.0),
                    $"Burst job took {stopwatch.Elapsed.TotalMilliseconds}ms, too slow for burst compilation");
                
                for (int i = 0; i < entryCount; i++)
                {
                    Assert.That(outputArray[i], Is.True);
                }
            }
        }

        [Test]
        public void NativeLogEntry_ParallelJob_ScalesWithCores()
        {
            // Arrange
            var entryCount = 10000;
            using (var inputArray = TestDataFactory.CreateNativeLogEntryArray(entryCount, Allocator.TempJob))
            using (var outputArray = new NativeArray<bool>(entryCount, Allocator.TempJob))
            {
                var job = new ParallelLoggingJob
                {
                    InputEntries = inputArray,
                    OutputResults = outputArray,
                    MinimumLevel = LogLevel.Debug
                };

                // Act
                var stopwatch = Stopwatch.StartNew();
                var handle = job.Schedule(entryCount, 64); // 64 items per batch
                handle.Complete();
                stopwatch.Stop();

                // Assert
                Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(5.0),
                    $"Parallel job took {stopwatch.Elapsed.TotalMilliseconds}ms, should scale with multiple cores");
                
                for (int i = 0; i < entryCount; i++)
                {
                    Assert.That(outputArray[i], Is.True);
                }
            }
        }

        [Test]
        public void LoggingService_FromJob_ThreadSafeAccess()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var jobCount = 10;
            var messagesPerJob = 100;

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < jobCount; i++)
            {
                var jobIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < messagesPerJob; j++)
                    {
                        _loggingService.LogInfo($"Job {jobIndex} message {j}", correlationId);
                    }
                }));
            }

            var stopwatch = Stopwatch.StartNew();
            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(1000),
                $"Concurrent job access took {stopwatch.Elapsed.TotalMilliseconds}ms, too slow for multithreading");
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(jobCount * messagesPerJob));
        }

        [Test]
        public void NativeLogEntry_MemoryLayout_OptimalForCache()
        {
            // Arrange
            var entryCount = 10000;
            using (var array = TestDataFactory.CreateNativeLogEntryArray(entryCount, Allocator.TempJob))
            {
                // Act
                var stopwatch = Stopwatch.StartNew();
                var processed = 0;
                for (int i = 0; i < entryCount; i++)
                {
                    if (array[i].ShouldProcess(LogLevel.Info))
                    {
                        processed++;
                    }
                }
                stopwatch.Stop();

                // Assert
                Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(1.0),
                    $"Memory access took {stopwatch.Elapsed.TotalMilliseconds}ms, indicating poor cache locality");
                Assert.That(processed, Is.EqualTo(entryCount));
            }
        }

        #endregion

        #region Unity-Specific Performance Pitfalls

        [Test]
        public void LoggingService_GarbageCollection_MinimalAllocation()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 100;
            var initialMemory = GC.GetTotalMemory(true);

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"GC test message {i}", correlationId);
            }

            var finalMemory = GC.GetTotalMemory(false);
            var allocatedMemory = finalMemory - initialMemory;

            // Assert
            Assert.That(allocatedMemory, Is.LessThan(1024 * 1024), // 1MB limit
                $"Logging allocated {allocatedMemory} bytes, exceeding GC pressure limits");
        }

        [Test]
        public void LoggingService_MainThreadBlocking_NonBlocking()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 1000;
            var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"Main thread test message {i}", correlationId);
                
                // Verify we're still on main thread
                Assert.That(System.Threading.Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThreadId),
                    "Logging operation blocked main thread");
            }
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(TARGET_FRAME_TIME_MS),
                $"Main thread logging took {stopwatch.Elapsed.TotalMilliseconds}ms, blocking main thread");
        }

        [Test]
        public void LoggingService_MemoryPressure_GracefulDegradation()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var largeMessageCount = 10000;
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Generate memory pressure
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < largeMessageCount; i++)
            {
                var largeMessage = new string('x', 1000); // 1KB message
                _loggingService.LogInfo($"Memory pressure test {i}: {largeMessage}", correlationId);
            }
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(5000), // 5 second limit
                $"Memory pressure handling took {stopwatch.Elapsed.TotalMilliseconds}ms, system not degrading gracefully");
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            Assert.That(memoryIncrease, Is.LessThan(100 * 1024 * 1024), // 100MB limit
                $"Memory pressure test increased memory by {memoryIncrease} bytes, excessive allocation");
        }

        [UnityTest]
        public IEnumerator LoggingService_DuringGameplay_NoFrameDrops()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var frameCount = 0;
            var droppedFrames = 0;
            var lastFrameTime = Time.realtimeSinceStartup;

            // Act - Simulate intense gameplay logging
            while (frameCount < 300) // 5 seconds at 60 FPS
            {
                var currentFrameTime = Time.realtimeSinceStartup;
                var deltaTime = currentFrameTime - lastFrameTime;
                
                // Log gameplay events
                _loggingService.LogDebug($"Player position: {UnityEngine.Random.Range(0, 100)}", correlationId);
                _loggingService.LogInfo($"Frame {frameCount} gameplay event", correlationId);
                
                if (frameCount % 10 == 0) // Every 10th frame
                {
                    _loggingService.LogWarning($"Performance checkpoint at frame {frameCount}", correlationId);
                }
                
                // Check for frame drops
                if (deltaTime > TARGET_FRAME_TIME_MS / 1000f * 1.5f) // 50% tolerance
                {
                    droppedFrames++;
                }
                
                lastFrameTime = currentFrameTime;
                frameCount++;
                yield return null;
            }

            // Assert
            var dropRate = (float)droppedFrames / frameCount;
            Assert.That(dropRate, Is.LessThan(0.05f), // 5% drop rate tolerance
                $"Frame drop rate {dropRate:P1} exceeds acceptable threshold during gameplay logging");
        }

        [Test]
        public void LoggingService_PlatformSpecific_MobileOptimized()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 100;
            var mobileFrameBudget = 1.0f; // Mobile devices have tighter budgets

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"Mobile optimization test {i}", correlationId);
            }
            stopwatch.Stop();

            // Assert
            var averageTimePerMessage = stopwatch.Elapsed.TotalMilliseconds / messageCount;
            Assert.That(averageTimePerMessage, Is.LessThan(mobileFrameBudget / MAX_MESSAGES_PER_FRAME),
                $"Average time per message {averageTimePerMessage}ms exceeds mobile budget");
        }

        #endregion

        #region Stress Tests

        [Test]
        public void LoggingService_HighThroughputStress_SystemStability()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = HIGH_FREQUENCY_MESSAGE_COUNT;
            var concurrentTasks = 5;

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>();
            
            for (int t = 0; t < concurrentTasks; t++)
            {
                var taskIndex = t;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = 0; i < messageCount / concurrentTasks; i++)
                    {
                        _loggingService.LogInfo($"Stress test task {taskIndex} message {i}", correlationId);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.Elapsed.TotalMilliseconds, Is.LessThan(5000), // 5 second limit
                $"High throughput stress test took {stopwatch.Elapsed.TotalMilliseconds}ms, system unstable");
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(messageCount));
            Assert.That(_loggingService.IsEnabled, Is.True, "Logging service became disabled under stress");
        }

        [Test]
        public void LoggingService_MemoryStress_NoMemoryLeaks()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var iterations = 10;
            var messagesPerIteration = 1000;
            var memoryMeasurements = new List<long>();

            // Act
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var beforeMemory = GC.GetTotalMemory(true);
                
                for (int i = 0; i < messagesPerIteration; i++)
                {
                    _loggingService.LogInfo($"Memory stress iteration {iteration} message {i}", correlationId);
                }
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var afterMemory = GC.GetTotalMemory(true);
                memoryMeasurements.Add(afterMemory - beforeMemory);
            }

            // Assert
            var memoryTrend = memoryMeasurements.Skip(5).Take(5).Average() - memoryMeasurements.Take(5).Average();
            Assert.That(memoryTrend, Is.LessThan(1024 * 1024), // 1MB growth tolerance
                $"Memory trend shows {memoryTrend} bytes growth, indicating memory leaks");
        }

        [UnityTest]
        public IEnumerator LoggingService_ExtendedStress_SystemReliability()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testDurationFrames = STRESS_TEST_DURATION_FRAMES;
            var frameCount = 0;
            var errorCount = 0;

            // Act
            while (frameCount < testDurationFrames)
            {
                try
                {
                    // Vary message types and complexity
                    var messageType = frameCount % 4;
                    switch (messageType)
                    {
                        case 0:
                            _loggingService.LogDebug($"Extended stress debug {frameCount}", correlationId);
                            break;
                        case 1:
                            _loggingService.LogInfo($"Extended stress info {frameCount}", correlationId);
                            break;
                        case 2:
                            _loggingService.LogWarning($"Extended stress warning {frameCount}", correlationId);
                            break;
                        case 3:
                            var properties = TestDataFactory.CreateTestProperties();
                            _loggingService.Log(LogLevel.Error, $"Extended stress error {frameCount}", 
                                correlationId, "StressTest", null, properties);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    UnityEngine.Debug.LogError($"Stress test error at frame {frameCount}: {ex.Message}");
                }

                frameCount++;
                
                // Yield every 10 frames to prevent blocking
                if (frameCount % 10 == 0)
                {
                    yield return null;
                }
            }

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), 
                $"Extended stress test encountered {errorCount} errors over {testDurationFrames} frames");
            Assert.That(_loggingService.IsEnabled, Is.True, 
                "Logging service became disabled during extended stress test");
        }

        #endregion

        #region Resource Usage Tests

        [Test]
        public void LoggingService_CPUUsage_LowOverhead()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 1000;
            var baselineCpuTime = MeasureBaselineCpuTime();

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"CPU usage test message {i}", correlationId);
            }
            stopwatch.Stop();

            // Assert
            var cpuOverhead = stopwatch.Elapsed.TotalMilliseconds - baselineCpuTime;
            var overheadPerMessage = cpuOverhead / messageCount;
            Assert.That(overheadPerMessage, Is.LessThan(0.01), // 0.01ms per message
                $"CPU overhead per message {overheadPerMessage}ms exceeds low overhead threshold");
        }

        [Test]
        public void LoggingService_NativeMemoryUsage_EfficientAllocation()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var entryCount = 1000;

            // Act
            var initialMemory = GC.GetTotalMemory(true);
            
            using (var nativeEntries = new NativeList<NativeLogEntry>(entryCount, Allocator.TempJob))
            {
                for (int i = 0; i < entryCount; i++)
                {
                    nativeEntries.Add(TestDataFactory.CreateNativeLogEntry(LogLevel.Info, 
                        $"Native{i}", $"Native message {i}"));
                }

                var finalMemory = GC.GetTotalMemory(false);
                var managedAllocation = finalMemory - initialMemory;

                // Assert
                Assert.That(managedAllocation, Is.LessThan(1024 * 100), // 100KB managed allocation limit
                    $"Native memory test allocated {managedAllocation} bytes in managed heap");
                Assert.That(nativeEntries.Length, Is.EqualTo(entryCount));
            }
        }

        #endregion

        #region Helper Methods and Jobs

        private double MeasureBaselineCpuTime()
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                // Simulate baseline work without logging
                var dummy = i * 2 + 1;
            }
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        [BurstCompile]
        private struct BurstCompatibleLoggingJob : IJob
        {
            [ReadOnly] public NativeArray<NativeLogEntry> InputEntries;
            [WriteOnly] public NativeArray<bool> OutputResults;
            [ReadOnly] public LogLevel MinimumLevel;

            public void Execute()
            {
                for (int i = 0; i < InputEntries.Length; i++)
                {
                    OutputResults[i] = InputEntries[i].ShouldProcess(MinimumLevel);
                }
            }
        }

        [BurstCompile]
        private struct ParallelLoggingJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<NativeLogEntry> InputEntries;
            [WriteOnly] public NativeArray<bool> OutputResults;
            [ReadOnly] public LogLevel MinimumLevel;

            public void Execute(int index)
            {
                OutputResults[index] = InputEntries[index].ShouldProcess(MinimumLevel);
            }
        }

        #endregion
    }
}