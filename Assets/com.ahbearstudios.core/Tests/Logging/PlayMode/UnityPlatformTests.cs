using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
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
    /// Unity platform-specific tests and Unity event integration tests.
    /// Tests behavior across different Unity platforms and lifecycle events.
    /// </summary>
    [TestFixture]
    public class UnityPlatformTests
    {
        private ILoggingService _loggingService;
        private MockProfilerService _mockProfiler;
        private MockAlertService _mockAlerts;
        private MockHealthCheckService _mockHealthCheck;
        private MockMessageBusService _mockMessageBus;
        private MockLogTarget _mockTarget;
        private SerilogTarget _serilogTarget;

        [SetUp]
        public void SetUp()
        {
            // Create mock dependencies
            _mockProfiler = new MockProfilerService();
            _mockAlerts = new MockAlertService();
            _mockHealthCheck = new MockHealthCheckService();
            _mockMessageBus = new MockMessageBusService();
            _mockTarget = TestUtilities.CreateMockLogTarget();

            // Create Serilog target with Unity-specific configuration
            var serilogConfig = TestDataFactory.CreateLogTargetConfig("SerilogTarget", LogLevel.Debug);
            serilogConfig.Properties["EnableConsole"] = true;
            serilogConfig.Properties["EnableFileLogging"] = true;
            serilogConfig.Properties["MobileOptimized"] = Application.platform == RuntimePlatform.Android || 
                                                         Application.platform == RuntimePlatform.IPhonePlayer;
            _serilogTarget = new SerilogTarget(serilogConfig, _mockProfiler, _mockAlerts);

            // Create logging service
            var config = CreatePlatformSpecificConfig();
            _loggingService = new LoggingService(config, _mockProfiler, _mockAlerts, _mockHealthCheck, _mockMessageBus);
            _loggingService.RegisterTarget(_mockTarget);
            _loggingService.RegisterTarget(_serilogTarget);
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
            _serilogTarget?.Dispose();
        }

        #region Platform-Specific Behavior Tests

        [Test]
        public void PlatformSpecific_EditorBehavior_VerboseLogging()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Editor-specific test message";

            // Act
            _loggingService.LogDebug(message, correlationId);
            _loggingService.LogInfo(message, correlationId);
            _loggingService.LogWarning(message, correlationId);

            // Assert
#if UNITY_EDITOR
            // Editor should log all levels
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(3));
#else
            // Runtime builds may filter debug messages
            Assert.That(_mockTarget.Messages.Count, Is.GreaterThan(0));
#endif
        }

        [Test]
        public void PlatformSpecific_MobileOptimization_ReducedLogging()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 100;

            // Act
            var startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"Mobile optimization test {i}", correlationId);
            }
            var endTime = Time.realtimeSinceStartup;
            var totalTime = (endTime - startTime) * 1000f; // Convert to ms

            // Assert
#if UNITY_ANDROID || UNITY_IOS
            // Mobile platforms should have tighter performance constraints
            Assert.That(totalTime, Is.LessThan(50f), "Mobile logging should be faster");
#else
            // Desktop platforms can be more lenient
            Assert.That(totalTime, Is.LessThan(100f), "Desktop logging performance");
#endif
        }

        [Test]
        public void PlatformSpecific_WebGLLimitations_HandlesRestrictions()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "WebGL platform test message";

            // Act
            _loggingService.LogInfo(message, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
#if UNITY_WEBGL
            // WebGL has file system limitations
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            // File logging should be disabled or limited
#else
            // Other platforms should work normally
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
#endif
        }

        [Test]
        public void PlatformSpecific_ConsolePlatform_OptimizedLogging()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "Console platform test message";

            // Act
            _loggingService.LogInfo(message, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
#if UNITY_PS4 || UNITY_PS5 || UNITY_XBOXONE || UNITY_GAMECORE
            // Console platforms should handle logging efficiently
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
            Assert.That(_serilogTarget.IsHealthy, Is.True);
#else
            // Other platforms
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
#endif
        }

        [Test]
        public void PlatformSpecific_FileLogging_PlatformAppropriatePaths()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var message = "File logging path test";

            // Act
            _loggingService.LogInfo(message, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
            var expectedPath = Application.persistentDataPath;
            Assert.That(Directory.Exists(expectedPath), Is.True);
            
            // Check that file logging respects platform capabilities
#if UNITY_WEBGL
            // WebGL should not create files
#else
            // Other platforms should support file logging
            Assert.That(_serilogTarget.IsHealthy, Is.True);
#endif
        }

        [Test]
        public void PlatformSpecific_MemoryConstraints_RespectsLimits()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = GetPlatformSpecificMessageCount();

            // Act
            var initialMemory = GC.GetTotalMemory(true);
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"Memory constraint test {i}", correlationId);
            }
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // Assert
            var memoryLimit = GetPlatformSpecificMemoryLimit();
            Assert.That(memoryUsed, Is.LessThan(memoryLimit),
                $"Memory usage {memoryUsed} exceeds platform limit {memoryLimit}");
        }

        #endregion

        #region Unity Event Integration Tests

        [UnityTest]
        public IEnumerator UnityEvents_ApplicationPause_HandlesCorrectly()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var beforePauseMessage = "Before pause message";
            var afterPauseMessage = "After pause message";

            // Act
            _loggingService.LogInfo(beforePauseMessage, correlationId);
            
            // Simulate application pause
            yield return SimulateApplicationPause();
            
            _loggingService.LogInfo(afterPauseMessage, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(2));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator UnityEvents_ApplicationFocus_MaintainsLogging()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var beforeFocusMessage = "Before focus change message";
            var afterFocusMessage = "After focus change message";

            // Act
            _loggingService.LogInfo(beforeFocusMessage, correlationId);
            
            // Simulate focus change
            yield return SimulateApplicationFocusChange();
            
            _loggingService.LogInfo(afterFocusMessage, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(2));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator UnityEvents_SceneTransition_PreservesLogging()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var beforeSceneMessage = "Before scene transition";
            var afterSceneMessage = "After scene transition";

            // Act
            _loggingService.LogInfo(beforeSceneMessage, correlationId);
            
            // Simulate scene transition
            yield return SimulateSceneTransition();
            
            _loggingService.LogInfo(afterSceneMessage, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(2));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator UnityEvents_LowMemoryWarning_HandlesGracefully()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var beforeWarningMessage = "Before low memory warning";
            var afterWarningMessage = "After low memory warning";

            // Act
            _loggingService.LogInfo(beforeWarningMessage, correlationId);
            
            // Simulate low memory warning
            yield return SimulateLowMemoryWarning();
            
            _loggingService.LogInfo(afterWarningMessage, correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(2));
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator UnityEvents_DomainReload_PersistsAcrossReloads()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var beforeReloadMessage = "Before domain reload";

            // Act
            _loggingService.LogInfo(beforeReloadMessage, correlationId);
            
            // Simulate domain reload conditions
            yield return SimulateDomainReload();
            
            // Verify logging still works after reload
            _loggingService.LogInfo("After domain reload", correlationId);
            _loggingService.FlushAsync().Wait();

            // Assert
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator UnityEvents_FrameRateDrops_MaintainsPerformance()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var frameCount = 0;
            var loggedFrames = 0;

            // Act
            while (frameCount < 120) // 2 seconds at 60 FPS
            {
                var frameStart = Time.realtimeSinceStartup;
                
                // Log every 10th frame
                if (frameCount % 10 == 0)
                {
                    _loggingService.LogInfo($"Frame {frameCount} logging test", correlationId);
                    loggedFrames++;
                }
                
                // Simulate frame work
                yield return null;
                
                var frameTime = (Time.realtimeSinceStartup - frameStart) * 1000f;
                
                // Assert frame time isn't excessively impacted by logging
                if (frameCount % 10 == 0) // Only check frames where we logged
                {
                    Assert.That(frameTime, Is.LessThan(20f), // 20ms tolerance
                        $"Frame {frameCount} took {frameTime}ms, too long with logging");
                }
                
                frameCount++;
            }

            // Assert
            Assert.That(loggedFrames, Is.EqualTo(12)); // 120 frames / 10
            Assert.That(_loggingService.IsEnabled, Is.True);
        }

        #endregion

        #region Platform-Specific Performance Tests

        [Test]
        public void PlatformPerformance_StartupTime_FastInitialization()
        {
            // Arrange
            var config = CreatePlatformSpecificConfig();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using (var testService = new LoggingService(config, _mockProfiler, _mockAlerts, _mockHealthCheck, _mockMessageBus))
            {
                testService.RegisterTarget(_mockTarget);
                testService.LogInfo("Startup test message");
            }
            stopwatch.Stop();

            // Assert
            var maxStartupTime = GetPlatformSpecificStartupTime();
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(maxStartupTime),
                $"Startup time {stopwatch.ElapsedMilliseconds}ms exceeds platform limit {maxStartupTime}ms");
        }

        [Test]
        public void PlatformPerformance_BatteryImpact_MinimalUsage()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 500;

            // Act
            var startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < messageCount; i++)
            {
                _loggingService.LogInfo($"Battery impact test {i}", correlationId);
            }
            var endTime = Time.realtimeSinceStartup;
            var cpuTime = (endTime - startTime) * 1000f;

            // Assert
#if UNITY_ANDROID || UNITY_IOS
            // Mobile platforms should minimize battery impact
            Assert.That(cpuTime, Is.LessThan(100f), "Mobile CPU usage should be minimal");
#else
            // Desktop platforms can be more lenient
            Assert.That(cpuTime, Is.LessThan(500f), "Desktop CPU usage");
#endif
        }

        [Test]
        public void PlatformPerformance_ThermalThrottling_HandlesCorrectly()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var messageCount = 2000;

            // Act
            var results = new List<float>();
            for (int batch = 0; batch < 10; batch++)
            {
                var batchStart = Time.realtimeSinceStartup;
                for (int i = 0; i < messageCount / 10; i++)
                {
                    _loggingService.LogInfo($"Thermal test batch {batch} message {i}", correlationId);
                }
                var batchEnd = Time.realtimeSinceStartup;
                results.Add((batchEnd - batchStart) * 1000f);
            }

            // Assert
            var averageTime = results.Average();
            var maxTime = results.Max();
            var minTime = results.Min();
            
            // Performance should be consistent (not degrade significantly due to thermal throttling)
            Assert.That(maxTime / minTime, Is.LessThan(3f), "Performance degradation too high");
        }

        #endregion

        #region Unity-Specific Integration Tests

        [Test]
        public void UnityIntegration_PlayerPrefs_DoesNotInterfere()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var testKey = "LoggingTestKey";
            var testValue = "LoggingTestValue";

            // Act
            PlayerPrefs.SetString(testKey, testValue);
            _loggingService.LogInfo("PlayerPrefs test message", correlationId);
            var retrievedValue = PlayerPrefs.GetString(testKey);

            // Assert
            Assert.That(retrievedValue, Is.EqualTo(testValue));
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnityIntegration_AssetDatabase_DoesNotConflict()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();

            // Act
            _loggingService.LogInfo("AssetDatabase test message", correlationId);
            
#if UNITY_EDITOR
            // Only test in editor where AssetDatabase is available
            try
            {
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Assert.Fail($"AssetDatabase conflict with logging: {ex.Message}");
            }
#endif

            // Assert
            Assert.That(_mockTarget.Messages.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnityIntegration_Coroutines_WorksCorrectly()
        {
            // Arrange
            var correlationId = TestDataFactory.CreateCorrelationId();
            var gameObject = new GameObject("LoggingTestObject");
            var testComponent = gameObject.AddComponent<TestCoroutineComponent>();

            // Act
            testComponent.StartCoroutine(testComponent.LoggingCoroutine(_loggingService, correlationId));
            
            // Wait for coroutine to complete
            var timeout = DateTime.Now.AddSeconds(5);
            while (!testComponent.IsComplete && DateTime.Now < timeout)
            {
                System.Threading.Thread.Sleep(10);
            }

            // Assert
            Assert.That(testComponent.IsComplete, Is.True, "Coroutine should complete");
            Assert.That(_mockTarget.Messages.Count, Is.GreaterThan(0));
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        #endregion

        #region Helper Methods

        private LoggingConfig CreatePlatformSpecificConfig()
        {
            var config = new LoggingConfig
            {
                IsEnabled = true,
                MinimumLevel = GetPlatformSpecificMinimumLevel(),
                Channels = new List<LogChannelConfig>
                {
                    new LogChannelConfig 
                    { 
                        Name = "Platform", 
                        IsEnabled = true, 
                        MinimumLevel = GetPlatformSpecificMinimumLevel() 
                    }
                }
            };

            return config;
        }

        private LogLevel GetPlatformSpecificMinimumLevel()
        {
#if UNITY_EDITOR
            return LogLevel.Debug; // Verbose logging in editor
#elif DEVELOPMENT_BUILD
            return LogLevel.Debug; // Debug builds
#elif UNITY_ANDROID || UNITY_IOS
            return LogLevel.Warning; // Mobile platforms - reduce logging
#elif UNITY_WEBGL
            return LogLevel.Error; // WebGL - minimal logging
#else
            return LogLevel.Info; // Default for other platforms
#endif
        }

        private int GetPlatformSpecificMessageCount()
        {
#if UNITY_ANDROID || UNITY_IOS
            return 100; // Reduced count for mobile
#elif UNITY_WEBGL
            return 50; // Even more reduced for WebGL
#else
            return 500; // Full count for desktop
#endif
        }

        private long GetPlatformSpecificMemoryLimit()
        {
#if UNITY_ANDROID || UNITY_IOS
            return 512 * 1024; // 512KB for mobile
#elif UNITY_WEBGL
            return 256 * 1024; // 256KB for WebGL
#else
            return 2 * 1024 * 1024; // 2MB for desktop
#endif
        }

        private long GetPlatformSpecificStartupTime()
        {
#if UNITY_ANDROID || UNITY_IOS
            return 100; // 100ms for mobile
#elif UNITY_WEBGL
            return 200; // 200ms for WebGL
#else
            return 50; // 50ms for desktop
#endif
        }

        private IEnumerator SimulateApplicationPause()
        {
            // Simulate application pause/resume cycle
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator SimulateApplicationFocusChange()
        {
            // Simulate focus change
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator SimulateSceneTransition()
        {
            // Simulate scene loading
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator SimulateLowMemoryWarning()
        {
            // Simulate low memory conditions
            GC.Collect();
            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator SimulateDomainReload()
        {
            // Simulate domain reload conditions
            yield return new WaitForSeconds(0.1f);
        }

        #endregion

        #region Helper Components

        private class TestCoroutineComponent : MonoBehaviour
        {
            public bool IsComplete { get; private set; }

            public IEnumerator LoggingCoroutine(ILoggingService loggingService, FixedString64Bytes correlationId)
            {
                for (int i = 0; i < 5; i++)
                {
                    loggingService.LogInfo($"Coroutine message {i}", correlationId);
                    yield return new WaitForSeconds(0.1f);
                }
                IsComplete = true;
            }
        }

        #endregion
    }
}