using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.HealthChecking.Checks;
using AhBearStudios.Core.HealthChecking.Configs;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using Unity.Collections;
using UnityEngine.Profiling;

namespace AhBearStudios.Unity.HealthCheck.HealthChecks
{
    /// <summary>
    /// Unity-specific system health check monitoring Unity API availability and basic functionality.
    /// Monitors Unity subsystems, graphics, audio, and platform-specific features.
    /// </summary>
    public sealed class UnitySystemHealthCheck : IHealthCheck
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly FixedString64Bytes _healthCheckName = "UnitySystem";
        private readonly FixedString64Bytes _correlationId;
        private readonly object _lockObject = new object();
        private DateTime _lastCheckTime = DateTime.MinValue;
        private HealthCheckResult _cachedResult;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30);

        #endregion

        #region IHealthCheck Properties

        public FixedString64Bytes Name => _healthCheckName;
        public string Description => "Monitors Unity system functionality including graphics, audio, and core subsystems";
        public HealthCheckCategory Category => HealthCheckCategory.System;
        public TimeSpan Timeout => TimeSpan.FromSeconds(15);
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
        public HealthCheckConfiguration Configuration { get; private set; }

        #endregion

        #region Constructor

        public UnitySystemHealthCheck(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _correlationId = GenerateCorrelationId();
            _cachedResult = HealthCheckResult.Unknown(_healthCheckName.ToString(), correlationId: _correlationId);

            Configuration = new HealthCheckConfiguration
            {
                Name = _healthCheckName,
                Interval = TimeSpan.FromMinutes(2),
                Timeout = Timeout,
                Enabled = true,
                Category = Category
            };
        }

        #endregion

        #region IHealthCheck Implementation

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (DateTime.UtcNow - _lastCheckTime < _cacheTimeout && _cachedResult != null)
                {
                    return _cachedResult;
                }
            }

            var healthData = new Dictionary<string, object>();
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Check Unity application state
                CheckApplicationState(healthData, issues, warnings);

                // Check graphics system
                await CheckGraphicsSystemAsync(healthData, issues, warnings, cancellationToken);

                // Check audio system
                CheckAudioSystem(healthData, issues, warnings);

                // Check platform-specific features
                CheckPlatformFeatures(healthData, issues, warnings);

                // Check Unity services
                CheckUnityServices(healthData, issues, warnings);

                var result = DetermineHealthStatus(issues, warnings, healthData);

                lock (_lockObject)
                {
                    _lastCheckTime = DateTime.UtcNow;
                    _cachedResult = result;
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = HealthCheckResult.Unhealthy(
                    _healthCheckName.ToString(),
                    $"Unity system health check failed: {ex.Message}",
                    correlationId: _correlationId,
                    exception: ex,
                    data: healthData);

                _loggingService?.LogException(ex, "Unity system health check failed", _correlationId);
                return result;
            }
        }

        public void Configure(HealthCheckConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["UnityVersion"] = Application.unityVersion,
                ["Platform"] = Application.platform.ToString(),
                ["ProductName"] = Application.productName,
                ["CompanyName"] = Application.companyName,
                ["GraphicsDeviceType"] = SystemInfo.graphicsDeviceType.ToString(),
                ["OperatingSystem"] = SystemInfo.operatingSystem,
                ["ProcessorType"] = SystemInfo.processorType,
                ["SystemMemorySize"] = SystemInfo.systemMemorySize,
                ["GraphicsMemorySize"] = SystemInfo.graphicsMemorySize
            };
        }

        #endregion

        #region Private Health Check Methods

        private void CheckApplicationState(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                healthData["ApplicationIsPlaying"] = Application.isPlaying;
                healthData["ApplicationIsFocused"] = Application.isFocused;
                healthData["ApplicationIsPaused"] = Application.isPaused;
                healthData["ApplicationRunInBackground"] = Application.runInBackground;
                healthData["ApplicationTargetFrameRate"] = Application.targetFrameRate;
                healthData["TimeScale"] = Time.timeScale;

                // Check for unusual states
                if (Time.timeScale <= 0)
                {
                    warnings.Add("Time scale is zero or negative");
                }

                if (Application.targetFrameRate > 0 && Application.targetFrameRate < 30)
                {
                    warnings.Add($"Low target frame rate: {Application.targetFrameRate}");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check application state: {ex.Message}");
            }
        }

        private async Task CheckGraphicsSystemAsync(Dictionary<string, object> healthData, List<string> issues, List<string> warnings, CancellationToken cancellationToken)
        {
            try
            {
                // Basic graphics info
                healthData["GraphicsDeviceType"] = SystemInfo.graphicsDeviceType.ToString();
                healthData["GraphicsDeviceName"] = SystemInfo.graphicsDeviceName;
                healthData["GraphicsDeviceVersion"] = SystemInfo.graphicsDeviceVersion;
                healthData["GraphicsMemorySize"] = SystemInfo.graphicsMemorySize;
                healthData["MaxTextureSize"] = SystemInfo.maxTextureSize;
                healthData["SupportsComputeShaders"] = SystemInfo.supportsComputeShaders;
                healthData["SupportsInstancing"] = SystemInfo.supportsInstancing;

                // Check for unsupported graphics API
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    issues.Add("Graphics device type is null");
                }

                // Check graphics memory
                if (SystemInfo.graphicsMemorySize < 512)
                {
                    warnings.Add($"Low graphics memory: {SystemInfo.graphicsMemorySize}MB");
                }

                // Check screen resolution
                healthData["ScreenWidth"] = Screen.width;
                healthData["ScreenHeight"] = Screen.height;
                healthData["ScreenDPI"] = Screen.dpi;
                healthData["ScreenFullscreen"] = Screen.fullScreen;

                if (Screen.width <= 0 || Screen.height <= 0)
                {
                    issues.Add("Invalid screen resolution");
                }

                await Task.CompletedTask; // Satisfy async signature
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check graphics system: {ex.Message}");
            }
        }

        private void CheckAudioSystem(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                var audioConfig = AudioSettings.GetConfiguration();
                
                healthData["AudioSampleRate"] = audioConfig.sampleRate;
                healthData["AudioSpeakerMode"] = audioConfig.speakerMode.ToString();
                healthData["AudioDspBufferSize"] = audioConfig.dspBufferSize;
                healthData["AudioNumRealVoices"] = audioConfig.numRealVoices;
                healthData["AudioNumVirtualVoices"] = audioConfig.numVirtualVoices;

                // Check for audio issues
                if (audioConfig.sampleRate <= 0)
                {
                    issues.Add("Invalid audio sample rate");
                }

                if (audioConfig.dspBufferSize <= 0)
                {
                    issues.Add("Invalid audio DSP buffer size");
                }

                // Check audio listener
                var audioListener = UnityEngine.Object.FindObjectOfType<AudioListener>();
                healthData["AudioListenerPresent"] = audioListener != null;
                
                if (audioListener == null)
                {
                    warnings.Add("No AudioListener found in scene");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check audio system: {ex.Message}");
            }
        }

        private void CheckPlatformFeatures(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                healthData["Platform"] = Application.platform.ToString();
                healthData["OperatingSystem"] = SystemInfo.operatingSystem;
                healthData["ProcessorType"] = SystemInfo.processorType;
                healthData["ProcessorCount"] = SystemInfo.processorCount;
                healthData["SystemMemorySize"] = SystemInfo.systemMemorySize;
                healthData["DeviceModel"] = SystemInfo.deviceModel;
                healthData["DeviceType"] = SystemInfo.deviceType.ToString();

                // Platform-specific checks
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                        CheckWindowsFeatures(healthData, issues, warnings);
                        break;
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        CheckMacOSFeatures(healthData, issues, warnings);
                        break;
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        CheckLinuxFeatures(healthData, issues, warnings);
                        break;
                    case RuntimePlatform.Android:
                        CheckAndroidFeatures(healthData, issues, warnings);
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        CheckiOSFeatures(healthData, issues, warnings);
                        break;
                }

                // Check system memory
                if (SystemInfo.systemMemorySize < 1024)
                {
                    warnings.Add($"Low system memory: {SystemInfo.systemMemorySize}MB");
                }

                // Check processor count
                if (SystemInfo.processorCount < 2)
                {
                    warnings.Add($"Low processor count: {SystemInfo.processorCount}");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check platform features: {ex.Message}");
            }
        }

        private void CheckWindowsFeatures(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            healthData["WindowsFeatureCheck"] = true;
            // Add Windows-specific checks here
        }

        private void CheckMacOSFeatures(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            healthData["MacOSFeatureCheck"] = true;
            // Add macOS-specific checks here
        }

        private void CheckLinuxFeatures(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            healthData["LinuxFeatureCheck"] = true;
            // Add Linux-specific checks here
        }

        private void CheckAndroidFeatures(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            healthData["AndroidFeatureCheck"] = true;
            healthData["AndroidAPILevel"] = SystemInfo.operatingSystem;
            // Add Android-specific checks here
        }

        private void CheckiOSFeatures(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            healthData["iOSFeatureCheck"] = true;
            healthData["iOSVersion"] = SystemInfo.operatingSystem;
            // Add iOS-specific checks here
        }

        private void CheckUnityServices(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                // Check Unity Cloud Build
                healthData["UnityCloudBuildEnabled"] = UnityEngine.Cloud.Analytics.Analytics.enabled;
                
                // Check Unity Analytics
                healthData["UnityAnalyticsEnabled"] = UnityEngine.Analytics.Analytics.enabled;
                
                // Note: Additional Unity Services checks could be added here
                // such as Unity Ads, Unity IAP, Unity Remote ConfigSo, etc.
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to check Unity services: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        private HealthCheckResult DetermineHealthStatus(List<string> issues, List<string> warnings, Dictionary<string, object> healthData)
        {
            healthData["IssuesCount"] = issues.Count;
            healthData["WarningsCount"] = warnings.Count;

            if (issues.Count > 0)
            {
                var message = $"Unity system has {issues.Count} critical issue(s)";
                return HealthCheckResult.Unhealthy(_healthCheckName.ToString(), message, data: healthData, correlationId: _correlationId);
            }

            if (warnings.Count > 0)
            {
                var message = $"Unity system has {warnings.Count} warning(s) but is operational";
                return HealthCheckResult.Degraded(_healthCheckName.ToString(), message, data: healthData, correlationId: _correlationId);
            }

            return HealthCheckResult.Healthy(_healthCheckName.ToString(), "Unity system is operating normally", data: healthData, correlationId: _correlationId);
        }

        private static FixedString64Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..16];
            return new FixedString64Bytes($"US-{guid}");
        }

        #endregion
    }

    /// <summary>
    /// Unity performance health check monitoring frame rate, memory usage, and rendering performance.
    /// Provides detailed performance metrics and identifies performance bottlenecks.
    /// </summary>
    public sealed class UnityPerformanceHealthCheck : IHealthCheck
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly FixedString64Bytes _healthCheckName = "UnityPerformance";
        private readonly FixedString64Bytes _correlationId;
        
        // Performance tracking
        private readonly Queue<float> _frameTimeHistory = new();
        private readonly Queue<long> _memoryHistory = new();
        private const int HISTORY_SIZE = 100;
        
        // Thresholds
        private const float DEGRADED_FPS_THRESHOLD = 30f;
        private const float UNHEALTHY_FPS_THRESHOLD = 15f;
        private const long DEGRADED_MEMORY_THRESHOLD = 1024 * 1024 * 1024; // 1GB
        private const long UNHEALTHY_MEMORY_THRESHOLD = 2048 * 1024 * 1024; // 2GB

        #endregion

        #region IHealthCheck Properties

        public FixedString64Bytes Name => _healthCheckName;
        public string Description => "Monitors Unity performance including frame rate, memory usage, and rendering statistics";
        public HealthCheckCategory Category => HealthCheckCategory.Performance;
        public TimeSpan Timeout => TimeSpan.FromSeconds(10);
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
        public HealthCheckConfiguration Configuration { get; private set; }

        #endregion

        #region Constructor

        public UnityPerformanceHealthCheck(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _correlationId = GenerateCorrelationId();

            Configuration = new HealthCheckConfiguration
            {
                Name = _healthCheckName,
                Interval = TimeSpan.FromSeconds(30),
                Timeout = Timeout,
                Enabled = true,
                Category = Category
            };
        }

        #endregion

        #region IHealthCheck Implementation

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var healthData = new Dictionary<string, object>();
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Update performance history
                UpdatePerformanceHistory();

                // Check frame rate performance
                CheckFrameRatePerformance(healthData, issues, warnings);

                // Check memory usage
                CheckMemoryUsage(healthData, issues, warnings);

                // Check rendering performance
                CheckRenderingPerformance(healthData, issues, warnings);

                // Check garbage collection
                CheckGarbageCollection(healthData, issues, warnings);

                var result = DetermineHealthStatus(issues, warnings, healthData);
                return result;
            }
            catch (Exception ex)
            {
                var result = HealthCheckResult.Unhealthy(
                    _healthCheckName.ToString(),
                    $"Performance health check failed: {ex.Message}",
                    correlationId: _correlationId,
                    exception: ex,
                    data: healthData);

                _loggingService?.LogException(ex, "Unity performance health check failed", _correlationId);
                return result;
            }
        }

        public void Configure(HealthCheckConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["TargetFrameRate"] = Application.targetFrameRate,
                ["VSyncCount"] = QualitySettings.vSyncCount,
                ["QualityLevel"] = QualitySettings.GetQualityLevel(),
                ["AntiAliasing"] = QualitySettings.antiAliasing,
                ["ShadowQuality"] = QualitySettings.shadows.ToString(),
                ["TextureQuality"] = QualitySettings.globalTextureMipmapLimit
            };
        }

        #endregion

        #region Private Performance Check Methods

        private void UpdatePerformanceHistory()
        {
            var currentFrameTime = Time.unscaledDeltaTime;
            var currentMemory = Profiler.GetTotalAllocatedMemory(false);

            _frameTimeHistory.Enqueue(currentFrameTime);
            _memoryHistory.Enqueue(currentMemory);

            if (_frameTimeHistory.Count > HISTORY_SIZE)
                _frameTimeHistory.Dequeue();

            if (_memoryHistory.Count > HISTORY_SIZE)
                _memoryHistory.Dequeue();
        }

        private void CheckFrameRatePerformance(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                var currentFPS = 1f / Time.unscaledDeltaTime;
                var averageFPS = _frameTimeHistory.Count > 0 ? 1f / _frameTimeHistory.Average() : currentFPS;
                var minFPS = _frameTimeHistory.Count > 0 ? 1f / _frameTimeHistory.Max() : currentFPS;

                healthData["CurrentFPS"] = currentFPS;
                healthData["AverageFPS"] = averageFPS;
                healthData["MinFPS"] = minFPS;
                healthData["TargetFrameRate"] = Application.targetFrameRate;
                healthData["VSyncCount"] = QualitySettings.vSyncCount;

                // Check frame rate issues
                if (averageFPS < UNHEALTHY_FPS_THRESHOLD)
                {
                    issues.Add($"Very low average FPS: {averageFPS:F1} (threshold: {UNHEALTHY_FPS_THRESHOLD})");
                }
                else if (averageFPS < DEGRADED_FPS_THRESHOLD)
                {
                    warnings.Add($"Low average FPS: {averageFPS:F1} (threshold: {DEGRADED_FPS_THRESHOLD})");
                }

                if (minFPS < 10f)
                {
                    warnings.Add($"Very low minimum FPS detected: {minFPS:F1}");
                }

                // Check frame time consistency
                if (_frameTimeHistory.Count > 10)
                {
                    var frameTimeVariance = CalculateVariance(_frameTimeHistory);
                    healthData["FrameTimeVariance"] = frameTimeVariance;

                    if (frameTimeVariance > 0.01f) // High variance indicates stuttering
                    {
                        warnings.Add($"High frame time variance detected: {frameTimeVariance:F4}");
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check frame rate performance: {ex.Message}");
            }
        }

        private void CheckMemoryUsage(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                var totalMemory = Profiler.GetTotalAllocatedMemory(false);
                var reservedMemory = Profiler.GetTotalReservedMemory(false);
                var unusedMemory = Profiler.GetTotalUnusedReservedMemory(false);
                var monoMemory = Profiler.GetMonoUsedSize();
                var monoHeapSize = Profiler.GetMonoHeapSize();

                healthData["TotalAllocatedMemoryMB"] = totalMemory / (1024f * 1024f);
                healthData["TotalReservedMemoryMB"] = reservedMemory / (1024f * 1024f);
                healthData["TotalUnusedMemoryMB"] = unusedMemory / (1024f * 1024f);
                healthData["MonoUsedMemoryMB"] = monoMemory / (1024f * 1024f);
                healthData["MonoHeapSizeMB"] = monoHeapSize / (1024f * 1024f);

                // Calculate memory statistics
                if (_memoryHistory.Count > 0)
                {
                    var averageMemory = _memoryHistory.Average();
                    var maxMemory = _memoryHistory.Max();
                    var memoryGrowth = _memoryHistory.Count > 1 ? 
                        (_memoryHistory.Last() - _memoryHistory.First()) / (float)_memoryHistory.Count : 0;

                    healthData["AverageMemoryMB"] = averageMemory / (1024f * 1024f);
                    healthData["MaxMemoryMB"] = maxMemory / (1024f * 1024f);
                    healthData["MemoryGrowthPerFrameMB"] = memoryGrowth / (1024f * 1024f);

                    // Check memory thresholds
                    if (totalMemory > UNHEALTHY_MEMORY_THRESHOLD)
                    {
                        issues.Add($"Very high memory usage: {totalMemory / (1024f * 1024f):F1}MB");
                    }
                    else if (totalMemory > DEGRADED_MEMORY_THRESHOLD)
                    {
                        warnings.Add($"High memory usage: {totalMemory / (1024f * 1024f):F1}MB");
                    }

                    // Check for memory leaks
                    if (memoryGrowth > 1024 * 1024) // Growing by more than 1MB per frame average
                    {
                        warnings.Add($"Potential memory leak detected: {memoryGrowth / (1024f * 1024f):F2}MB growth per frame");
                    }
                }

                // Check mono heap efficiency
                var heapUtilization = monoHeapSize > 0 ? (float)monoMemory / monoHeapSize : 0f;
                healthData["MonoHeapUtilization"] = heapUtilization;

                if (heapUtilization < 0.5f && monoHeapSize > 50 * 1024 * 1024) // Less than 50% utilized and heap > 50MB
                {
                    warnings.Add($"Low mono heap utilization: {heapUtilization:P1}");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Failed to check memory usage: {ex.Message}");
            }
        }

        private void CheckRenderingPerformance(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                // Get rendering statistics if available
                var triangles = UnityEngine.Rendering.FrameDebugger.enabled ? 0 : 0; // Would need actual implementation
                var drawCalls = 0; // Would need actual implementation through profiler
                var batches = 0; // Would need actual implementation

                healthData["EstimatedTriangles"] = triangles;
                healthData["EstimatedDrawCalls"] = drawCalls;
                healthData["EstimatedBatches"] = batches;

                // Check quality settings impact
                var qualityLevel = QualitySettings.GetQualityLevel();
                var shadowQuality = QualitySettings.shadows;
                var antiAliasing = QualitySettings.antiAliasing;

                healthData["QualityLevel"] = qualityLevel;
                healthData["ShadowQuality"] = shadowQuality.ToString();
                healthData["AntiAliasing"] = antiAliasing;

                // Check for performance-impacting settings
                if (antiAliasing >= 8)
                {
                    warnings.Add($"High anti-aliasing setting: {antiAliasing}x");
                }

                if (shadowQuality == ShadowQuality.All && QualitySettings.shadowDistance > 150f)
                {
                    warnings.Add($"High shadow distance with all shadows enabled: {QualitySettings.shadowDistance}");
                }

                // Check screen resolution impact
                var pixelCount = Screen.width * Screen.height;
                healthData["ScreenPixelCount"] = pixelCount;

                if (pixelCount > 1920 * 1080 * 1.5f) // Above 1080p by significant margin
                {
                    warnings.Add($"High resolution may impact performance: {Screen.width}x{Screen.height}");
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to check rendering performance: {ex.Message}");
            }
        }

        private void CheckGarbageCollection(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            try
            {
                // Note: GC statistics would require custom profiling or Unity Profiler API
                // This is a simplified version
                var totalMemory = GC.GetTotalMemory(false);
                healthData["GCTotalMemory"] = totalMemory;

                // Force a collection for testing (be careful with this in production)
                var memoryBeforeGC = GC.GetTotalMemory(false);
                var memoryAfterGC = GC.GetTotalMemory(true);
                var freedMemory = memoryBeforeGC - memoryAfterGC;

                healthData["MemoryBeforeGC"] = memoryBeforeGC;
                healthData["MemoryAfterGC"] = memoryAfterGC;
                healthData["FreedMemory"] = freedMemory;

                if (freedMemory > 50 * 1024 * 1024) // More than 50MB freed
                {
                    warnings.Add($"High GC pressure detected: {freedMemory / (1024f * 1024f):F1}MB freed");
                }

                // Check for generation 2 collections (these are expensive)
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                healthData["Gen0Collections"] = gen0Collections;
                healthData["Gen1Collections"] = gen1Collections;
                healthData["Gen2Collections"] = gen2Collections;

                // Note: In a real implementation, you'd want to track these over time
                // and alert on unusual increases
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to check garbage collection: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        private float CalculateVariance(Queue<float> values)
        {
            if (values.Count < 2) return 0f;

            var mean = values.Average();
            var variance = values.Select(x => (x - mean) * (x - mean)).Average();
            return variance;
        }

        private HealthCheckResult DetermineHealthStatus(List<string> issues, List<string> warnings, Dictionary<string, object> healthData)
        {
            healthData["IssuesCount"] = issues.Count;
            healthData["WarningsCount"] = warnings.Count;

            if (issues.Count > 0)
            {
                var message = $"Unity performance has {issues.Count} critical issue(s)";
                return HealthCheckResult.Unhealthy(_healthCheckName.ToString(), message, data: healthData, correlationId: _correlationId);
            }

            if (warnings.Count > 0)
            {
                var message = $"Unity performance has {warnings.Count} warning(s) but is acceptable";
                return HealthCheckResult.Degraded(_healthCheckName.ToString(), message, data: healthData, correlationId: _correlationId);
            }

            return HealthCheckResult.Healthy(_healthCheckName.ToString(), "Unity performance is optimal", data: healthData, correlationId: _correlationId);
        }

        private static FixedString64Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..16];
            return new FixedString64Bytes($"UP-{guid}");
        }

        #endregion
    }

    /// <summary>
    /// Unity memory health check specifically monitoring memory allocation patterns and potential leaks.
    /// Provides detailed memory analysis and garbage collection monitoring.
    /// </summary>
    public sealed class UnityMemoryHealthCheck : IHealthCheck
    {
        #region Private Fields

        private readonly ILoggingService _loggingService;
        private readonly FixedString64Bytes _healthCheckName = "UnityMemory";
        private readonly FixedString64Bytes _correlationId;
        
        // Memory tracking
        private readonly Queue<MemorySnapshot> _memorySnapshots = new();
        private const int SNAPSHOT_HISTORY_SIZE = 50;
        
        // Thresholds
        private const long DEGRADED_ALLOCATION_RATE = 10 * 1024 * 1024; // 10MB/s
        private const long UNHEALTHY_ALLOCATION_RATE = 50 * 1024 * 1024; // 50MB/s
        private const long DEGRADED_TOTAL_MEMORY = 1024 * 1024 * 1024; // 1GB
        private const long UNHEALTHY_TOTAL_MEMORY = 2048 * 1024 * 1024; // 2GB

        #endregion

        #region Private Classes

        private class MemorySnapshot
        {
            public DateTime Timestamp { get; set; }
            public long TotalAllocated { get; set; }
            public long TotalReserved { get; set; }
            public long MonoUsed { get; set; }
            public long MonoHeap { get; set; }
            public int Gen0Collections { get; set; }
            public int Gen1Collections { get; set; }
            public int Gen2Collections { get; set; }
        }

        #endregion

        #region IHealthCheck Properties

        public FixedString64Bytes Name => _healthCheckName;
        public string Description => "Monitors Unity memory allocation patterns, garbage collection, and potential memory leaks";
        public HealthCheckCategory Category => HealthCheckCategory.Performance;
        public TimeSpan Timeout => TimeSpan.FromSeconds(5);
        public IEnumerable<FixedString64Bytes> Dependencies => Array.Empty<FixedString64Bytes>();
        public HealthCheckConfiguration Configuration { get; private set; }

        #endregion

        #region Constructor

        public UnityMemoryHealthCheck(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _correlationId = GenerateCorrelationId();

            Configuration = new HealthCheckConfiguration
            {
                Name = _healthCheckName,
                Interval = TimeSpan.FromSeconds(15),
                Timeout = Timeout,
                Enabled = true,
                Category = Category
            };
        }

        #endregion

        #region IHealthCheck Implementation

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var healthData = new Dictionary<string, object>();
            var issues = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Take memory snapshot
                var snapshot = TakeMemorySnapshot();
                _memorySnapshots.Enqueue(snapshot);

                if (_memorySnapshots.Count > SNAPSHOT_HISTORY_SIZE)
                    _memorySnapshots.Dequeue();

                // Analyze current memory state
                AnalyzeCurrentMemoryState(snapshot, healthData, issues, warnings);

                // Analyze memory trends
                if (_memorySnapshots.Count > 1)
                {
                    AnalyzeMemoryTrends(healthData, issues, warnings);
                }

                // Analyze garbage collection patterns
                AnalyzeGarbageCollection(snapshot, healthData, issues, warnings);

                // Check for memory leaks
                CheckForMemoryLeaks(healthData, issues, warnings);

                var result = DetermineHealthStatus(issues, warnings, healthData);
                return result;
            }
            catch (Exception ex)
            {
                var result = HealthCheckResult.Unhealthy(
                    _healthCheckName.ToString(),
                    $"Memory health check failed: {ex.Message}",
                    correlationId: _correlationId,
                    exception: ex,
                    data: healthData);

                _loggingService?.LogException(ex, "Unity memory health check failed", _correlationId);
                return result;
            }
        }

        public void Configure(HealthCheckConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Dictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                ["SystemMemorySize"] = SystemInfo.systemMemorySize,
                ["GraphicsMemorySize"] = SystemInfo.graphicsMemorySize,
                ["Platform"] = Application.platform.ToString(),
                ["SnapshotHistorySize"] = SNAPSHOT_HISTORY_SIZE,
                ["DegradedAllocationRate"] = DEGRADED_ALLOCATION_RATE,
                ["UnhealthyAllocationRate"] = UNHEALTHY_ALLOCATION_RATE
            };
        }

        #endregion

        #region Private Memory Analysis Methods

        private MemorySnapshot TakeMemorySnapshot()
        {
            return new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                TotalAllocated = Profiler.GetTotalAllocatedMemory(false),
                TotalReserved = Profiler.GetTotalReservedMemory(false),
                MonoUsed = Profiler.GetMonoUsedSize(),
                MonoHeap = Profiler.GetMonoHeapSize(),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }

        private void AnalyzeCurrentMemoryState(MemorySnapshot snapshot, Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            // Record current memory statistics
            healthData["TotalAllocatedMB"] = snapshot.TotalAllocated / (1024f * 1024f);
            healthData["TotalReservedMB"] = snapshot.TotalReserved / (1024f * 1024f);
            healthData["MonoUsedMB"] = snapshot.MonoUsed / (1024f * 1024f);
            healthData["MonoHeapMB"] = snapshot.MonoHeap / (1024f * 1024f);
            healthData["UnusedReservedMB"] = (snapshot.TotalReserved - snapshot.TotalAllocated) / (1024f * 1024f);

            // Check memory thresholds
            if (snapshot.TotalAllocated > UNHEALTHY_TOTAL_MEMORY)
            {
                issues.Add($"Very high total memory usage: {snapshot.TotalAllocated / (1024f * 1024f):F1}MB");
            }
            else if (snapshot.TotalAllocated > DEGRADED_TOTAL_MEMORY)
            {
                warnings.Add($"High total memory usage: {snapshot.TotalAllocated / (1024f * 1024f):F1}MB");
            }

            // Check mono heap efficiency
            var heapUtilization = snapshot.MonoHeap > 0 ? (float)snapshot.MonoUsed / snapshot.MonoHeap : 0f;
            healthData["MonoHeapUtilization"] = heapUtilization;

            if (heapUtilization < 0.3f && snapshot.MonoHeap > 100 * 1024 * 1024)
            {
                warnings.Add($"Low mono heap utilization: {heapUtilization:P1} with {snapshot.MonoHeap / (1024f * 1024f):F1}MB heap");
            }

            // Check for excessive reserved memory
            var wastedMemory = snapshot.TotalReserved - snapshot.TotalAllocated;
            var wastedPercentage = snapshot.TotalReserved > 0 ? (float)wastedMemory / snapshot.TotalReserved : 0f;
            healthData["WastedMemoryPercentage"] = wastedPercentage;

            if (wastedPercentage > 0.5f && wastedMemory > 100 * 1024 * 1024)
            {
                warnings.Add($"High percentage of unused reserved memory: {wastedPercentage:P1}");
            }
        }

        private void AnalyzeMemoryTrends(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            var snapshots = _memorySnapshots.ToArray();
            var timeSpan = snapshots.Last().Timestamp - snapshots.First().Timestamp;
            
            if (timeSpan.TotalSeconds < 1) return; // Not enough time elapsed

            // Calculate allocation rate
            var memoryGrowth = snapshots.Last().TotalAllocated - snapshots.First().TotalAllocated;
            var allocationRate = memoryGrowth / timeSpan.TotalSeconds;

            healthData["AllocationRateBytesPerSecond"] = allocationRate;
            healthData["AllocationRateMBPerSecond"] = allocationRate / (1024f * 1024f);

            if (allocationRate > UNHEALTHY_ALLOCATION_RATE)
            {
                issues.Add($"Very high allocation rate: {allocationRate / (1024f * 1024f):F2}MB/s");
            }
            else if (allocationRate > DEGRADED_ALLOCATION_RATE)
            {
                warnings.Add($"High allocation rate: {allocationRate / (1024f * 1024f):F2}MB/s");
            }

            // Calculate memory growth trend
            var growthTrend = CalculateLinearTrend(snapshots.Select(s => (double)s.TotalAllocated).ToArray());
            healthData["MemoryGrowthTrend"] = growthTrend;

            if (growthTrend > 1024 * 1024) // Growing by more than 1MB per snapshot
            {
                warnings.Add($"Positive memory growth trend detected: {growthTrend / (1024f * 1024f):F2}MB per check");
            }

            // Check for memory spikes
            var memoryValues = snapshots.Select(s => s.TotalAllocated).ToArray();
            var maxMemory = memoryValues.Max();
            var avgMemory = memoryValues.Average();
            var memorySpike = maxMemory - avgMemory;

            healthData["MaxMemoryMB"] = maxMemory / (1024f * 1024f);
            healthData["AvgMemoryMB"] = avgMemory / (1024f * 1024f);
            healthData["MemorySpikeMB"] = memorySpike / (1024f * 1024f);

            if (memorySpike > avgMemory * 0.5) // Spike is more than 50% of average
            {
                warnings.Add($"Large memory spike detected: {memorySpike / (1024f * 1024f):F1}MB above average");
            }
        }

        private void AnalyzeGarbageCollection(MemorySnapshot snapshot, Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            healthData["Gen0Collections"] = snapshot.Gen0Collections;
            healthData["Gen1Collections"] = snapshot.Gen1Collections;
            healthData["Gen2Collections"] = snapshot.Gen2Collections;

            if (_memorySnapshots.Count > 1)
            {
                var previousSnapshot = _memorySnapshots.ElementAt(_memorySnapshots.Count - 2);
                var timeSpan = snapshot.Timestamp - previousSnapshot.Timestamp;

                if (timeSpan.TotalSeconds > 0)
                {
                    var gen0Rate = (snapshot.Gen0Collections - previousSnapshot.Gen0Collections) / timeSpan.TotalSeconds;
                    var gen1Rate = (snapshot.Gen1Collections - previousSnapshot.Gen1Collections) / timeSpan.TotalSeconds;
                    var gen2Rate = (snapshot.Gen2Collections - previousSnapshot.Gen2Collections) / timeSpan.TotalSeconds;

                    healthData["Gen0CollectionsPerSecond"] = gen0Rate;
                    healthData["Gen1CollectionsPerSecond"] = gen1Rate;
                    healthData["Gen2CollectionsPerSecond"] = gen2Rate;

                    // Check for excessive GC activity
                    if (gen0Rate > 10) // More than 10 gen0 collections per second
                    {
                        warnings.Add($"High Gen0 GC rate: {gen0Rate:F1} collections/second");
                    }

                    if (gen1Rate > 1) // More than 1 gen1 collection per second
                    {
                        warnings.Add($"High Gen1 GC rate: {gen1Rate:F1} collections/second");
                    }

                    if (gen2Rate > 0.1) // More than 1 gen2 collection per 10 seconds
                    {
                        issues.Add($"High Gen2 GC rate: {gen2Rate:F2} collections/second");
                    }
                }
            }
        }

        private void CheckForMemoryLeaks(Dictionary<string, object> healthData, List<string> issues, List<string> warnings)
        {
            if (_memorySnapshots.Count < 10) return; // Need sufficient history

            var snapshots = _memorySnapshots.ToArray();
            var recentSnapshots = snapshots.Skip(snapshots.Length - 10).ToArray();

            // Check if memory consistently grows without significant GC
            var memoryIncreases = 0;
            var totalGCActivity = 0;

            for (int i = 1; i < recentSnapshots.Length; i++)
            {
                var current = recentSnapshots[i];
                var previous = recentSnapshots[i - 1];

                if (current.TotalAllocated > previous.TotalAllocated)
                    memoryIncreases++;

                var gcActivity = (current.Gen0Collections - previous.Gen0Collections) +
                               (current.Gen1Collections - previous.Gen1Collections) +
                               (current.Gen2Collections - previous.Gen2Collections);
                totalGCActivity += gcActivity;
            }

            var increasePercentage = (float)memoryIncreases / (recentSnapshots.Length - 1);
            healthData["MemoryIncreasePercentage"] = increasePercentage;
            healthData["RecentGCActivity"] = totalGCActivity;

            // Potential memory leak if memory consistently grows with little GC activity
            if (increasePercentage > 0.8f && totalGCActivity < 5)
            {
                warnings.Add($"Potential memory leak: {increasePercentage:P0} of recent checks show memory growth with minimal GC");
            }

            // Check for linear memory growth
            var memoryValues = recentSnapshots.Select(s => (double)s.TotalAllocated).ToArray();
            var correlation = CalculateCorrelation(memoryValues);
            healthData["MemoryGrowthCorrelation"] = correlation;

            if (correlation > 0.9) // Strong positive correlation suggests linear growth
            {
                warnings.Add($"Strong linear memory growth pattern detected (correlation: {correlation:F2})");
            }
        }

        #endregion

        #region Private Helper Methods

        private double CalculateLinearTrend(double[] values)
        {
            if (values.Length < 2) return 0;

            var n = values.Length;
            var sumX = n * (n - 1) / 2.0; // Sum of indices 0, 1, 2, ...
            var sumY = values.Sum();
            var sumXY = values.Select((y, x) => x * y).Sum();
            var sumXX = Enumerable.Range(0, n).Select(x => x * x).Sum();

            var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            return slope;
        }

        private double CalculateCorrelation(double[] values)
        {
            if (values.Length < 2) return 0;

            var indices = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();
            var meanX = indices.Average();
            var meanY = values.Average();

            var numerator = indices.Zip(values, (x, y) => (x - meanX) * (y - meanY)).Sum();
            var denominator = Math.Sqrt(indices.Sum(x => (x - meanX) * (x - meanX)) * values.Sum(y => (y - meanY) * (y - meanY)));

            return denominator != 0 ? numerator / denominator : 0;
        }

        private HealthCheckResult DetermineHealthStatus(List<string> issues, List<string> warnings, Dictionary<string, object> healthData)
        {
            healthData["IssuesCount"] = issues.Count;
            healthData["WarningsCount"] = warnings.Count;

            if (issues.Count > 0)
            {
                var message = $"Unity memory has {issues.Count} critical issue(s)";
                return HealthCheckResult.Unhealthy(_healthCheckName.ToString(), message, data: healthData, correlationId: _correlationId);
            }

            if (warnings.Count > 0)
            {
                var message = $"Unity memory has {warnings.Count} warning(s) but is manageable";
                return HealthCheckResult.Degraded(_healthCheckName.ToString(), message, data: healthData, correlationId: _correlationId);
            }

            return HealthCheckResult.Healthy(_healthCheckName.ToString(), "Unity memory usage is optimal", data: healthData, correlationId: _correlationId);
        }

        private static FixedString64Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..16];
            return new FixedString64Bytes($"UM-{guid}");
        }

        #endregion
    }
}