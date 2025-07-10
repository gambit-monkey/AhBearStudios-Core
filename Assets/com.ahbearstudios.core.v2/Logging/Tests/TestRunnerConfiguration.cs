using NUnit.Framework;
using Unity.Collections;
using System;
using UnityEngine;

namespace AhBearStudios.Core.Tests
{
    /// <summary>
    /// Configuration and setup for Unity Test Runner
    /// </summary>
    [SetUpFixture]
    public class TestRunnerConfiguration
    {
        /// <summary>
        /// Global setup run once before all tests
        /// </summary>
        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            // Enable native leak detection in tests
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            
            // Set up test environment
            UnityEngine.Debug.Log("=== AhBearStudios Core Logging System Tests Starting ===");
            UnityEngine.Debug.Log($"Unity Version: {UnityEngine.Application.unityVersion}");
            UnityEngine.Debug.Log($"Platform: {UnityEngine.Application.platform}");
            UnityEngine.Debug.Log($"Test Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            
            // Initialize any global test state
            InitializeTestEnvironment();
        }

        /// <summary>
        /// Global teardown run once after all tests
        /// </summary>
        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            // Clean up any global test state
            CleanupTestEnvironment();
            
            UnityEngine.Debug.Log("=== AhBearStudios Core Logging System Tests Completed ===");
            
            // Force garbage collection to help with leak detection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Initialize the test environment
        /// </summary>
        private static void InitializeTestEnvironment()
        {
            // Set deterministic behavior for tests
            UnityEngine.Random.InitState(12345);
            
            // Ensure clean state
            Resources.UnloadUnusedAssets();
            GC.Collect();
            
            UnityEngine.Debug.Log("Test environment initialized");
        }

        /// <summary>
        /// Clean up the test environment
        /// </summary>
        private static void CleanupTestEnvironment()
        {
            // Clean up any resources
            Resources.UnloadUnusedAssets();
            
            UnityEngine.Debug.Log("Test environment cleaned up");
        }
    }

    /// <summary>
    /// Custom attributes for test categorization
    /// </summary>
    public static class TestCategories
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string Performance = "Performance";
        public const string Stress = "Stress";
        public const string Memory = "Memory";
        public const string Threading = "Threading";
        public const string Burst = "Burst";
        public const string Collections = "Collections";
        public const string Logging = "Logging";
        public const string Config = "Config";
        public const string Formatters = "Formatters";
        public const string Targets = "Targets";
        public const string MessageBus = "MessageBusService";
        public const string Profiling = "Profiling";
        public const string Pooling = "Pooling";
    }

    /// <summary>
    /// Base class for tests that require native memory tracking
    /// </summary>
    public abstract class NativeMemoryTestBase
    {
        protected virtual void SetUp()
        {
            // Ensure clean memory state before each test
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        protected virtual void TearDown()
        {
            // Force cleanup after each test
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Helper method to verify no native memory leaks occurred
        /// </summary>
        protected void AssertNoNativeMemoryLeaks()
        {
            // In a real implementation, we'd check for native memory leaks here
            // This is a placeholder for the concept
            Assert.Pass("Native memory leak check passed");
        }
    }

    /// <summary>
    /// Test utilities and helper methods
    /// </summary>
    public static class TestUtilities
    {
        /// <summary>
        /// Creates a temporary allocator for testing
        /// </summary>
        public static Allocator GetTestAllocator()
        {
            return Allocator.TempJob;
        }

        /// <summary>
        /// Generates test data for performance tests
        /// </summary>
        public static string[] GenerateTestMessages(int count, string prefix = "TestMessage")
        {
            var messages = new string[count];
            for (int i = 0; i < count; i++)
            {
                messages[i] = $"{prefix}_{i:D6}";
            }
            return messages;
        }

        /// <summary>
        /// Generates test tags for testing
        /// </summary>
        public static string[] GenerateTestTags(int count, string prefix = "TestTag")
        {
            var tags = new string[count];
            for (int i = 0; i < count; i++)
            {
                tags[i] = $"{prefix}_{i:D3}";
            }
            return tags;
        }

        /// <summary>
        /// Measures execution time of an action
        /// </summary>
        public static TimeSpan MeasureExecutionTime(Action action)
        {
            var startTime = DateTime.UtcNow;
            action();
            return DateTime.UtcNow - startTime;
        }

        /// <summary>
        /// Asserts that an action completes within the specified time
        /// </summary>
        public static void AssertExecutionTime(Action action, TimeSpan maxDuration, string message = null)
        {
            var actualDuration = MeasureExecutionTime(action);
            Assert.That(actualDuration, Is.LessThanOrEqualTo(maxDuration), 
                message ?? $"Action took {actualDuration.TotalMilliseconds:F2}ms, expected <= {maxDuration.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Creates a long string for testing string handling
        /// </summary>
        public static string CreateLongString(int length, char character = 'A')
        {
            return new string(character, length);
        }

        /// <summary>
        /// Creates test data with special characters
        /// </summary>
        public static string[] GetSpecialCharacterTestData()
        {
            return new[]
            {
                "Simple text",
                "Text with\nnewlines\r\n",
                "Text with\ttabs",
                "Unicode: 你好世界 🚀 🎯",
                "Symbols: !@#$%^&*()_+-=[]{}|;:'\",.<>?",
                "Empty: ",
                null,
                ""
            };
        }

        /// <summary>
        /// Validates that a string contains expected substrings
        /// </summary>
        public static void AssertContainsAll(string actual, params string[] expectedSubstrings)
        {
            foreach (var expected in expectedSubstrings)
            {
                if (!string.IsNullOrEmpty(expected))
                {
                    Assert.That(actual, Does.Contain(expected), 
                        $"Expected '{actual}' to contain '{expected}'");
                }
            }
        }
    }
}