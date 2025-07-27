using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.HealthChecks;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Models;
using AhBearStudios.Unity.Logging.ScriptableObjects;
using UnityEngine.Profiling;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Unity.Logging
{
    /// <summary>
    /// MonoBehaviour wrapper for the logging service that provides Unity-specific functionality.
    /// Integrates with Unity's lifecycle events and provides convenient access to logging in Unity context.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    public class UnityLoggingBehaviour : MonoBehaviour
    {
        [Header("Logging Configuration")]
        [SerializeField] private LoggingConfigurationAsset _loggingConfig;
        [SerializeField] private bool _useScriptableObjectConfig = true;
        [SerializeField] private bool _logUnityEvents = true;
        [SerializeField] private bool _logFrameRate = false;
        [SerializeField] private int _frameRateLogInterval = 60; // Log every N frames
        [SerializeField] private bool _logMemoryUsage = false;
        [SerializeField] private float _memoryLogInterval = 10f; // Log every N seconds

        [Header("Performance Monitoring")]
        [SerializeField] private bool _monitorPerformance = true;
        [SerializeField] private bool _logSlowFrames = true;
        [SerializeField] private float _slowFrameThreshold = 33.33f; // ms (30 FPS)
        [SerializeField] private bool _logMemorySpikes = true;
        [SerializeField] private long _memorySpikeThreshold = 10 * 1024 * 1024; // 10MB

        [Header("Health Monitoring")]
        [SerializeField] private bool _enableHealthMonitoring = true;
        [SerializeField] private float _healthCheckInterval = 30f; // seconds
        [SerializeField] private bool _logHealthStatus = true;

        [Header("Debug Features")]
        [SerializeField] private bool _enableDebugGUI = false;
        [SerializeField] private KeyCode _toggleDebugKey = KeyCode.F1;
        [SerializeField] private bool _verboseStartup = false;

        [Header("Application Event Logging")]
        [SerializeField] private bool _logApplicationEvents = true;
        [SerializeField] private bool _logSceneChanges = true;
        [SerializeField] private bool _logQualityChanges = true;

        // Dependencies injected via Reflex
        [Inject] private ILoggingService _loggingService;
        [Inject] private IHealthCheckService _healthCheckService;
        [Inject] private IProfilerService _profilerService;
        [Inject] private LoggingServiceHealthCheck _loggingHealthCheck;

        // Unity-specific state
        private bool _initialized = false;
        private int _frameCount = 0;
        private float _lastMemoryLogTime = 0f;
        private long _lastMemoryUsage = 0;
        private float _lastHealthCheckTime = 0f;
        private bool _showDebugGUI = false;
        private readonly List<string> _recentLogs = new List<string>();
        private const int MaxRecentLogs = 20;

        // Performance tracking
        private float _frameTime = 0f;
        private int _slowFrameCount = 0;
        private int _totalFrameCount = 0;
        private float _averageFrameTime = 0f;
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private const int FrameHistorySize = 100;

        // Application state tracking
        private string _currentSceneName = "";
        private int _lastQualityLevel = -1;
        private bool _applicationPaused = false;
        private bool _applicationFocused = true;

        // Debug GUI state
        private Vector2 _debugScrollPosition = Vector2.zero;
        private GUIStyle _debugBoxStyle;
        private GUIStyle _debugLabelStyle;
        private GUIStyle _debugButtonStyle;
        private bool _stylesInitialized = false;

        /// <summary>
        /// Gets whether the logging behaviour is initialized and ready.
        /// </summary>
        public bool IsInitialized => _initialized && _loggingService != null;

        /// <summary>
        /// Gets the current frame rate as calculated by this component.
        /// </summary>
        public float CurrentFrameRate => Time.deltaTime > 0 ? 1.0f / Time.deltaTime : 0f;

        /// <summary>
        /// Gets the average frame time over the last N frames.
        /// </summary>
        public float AverageFrameTime => _averageFrameTime;

        /// <summary>
        /// Gets the percentage of slow frames detected.
        /// </summary>
        public float SlowFramePercentage => _totalFrameCount > 0 ? (float)_slowFrameCount / _totalFrameCount * 100f : 0f;

        /// <summary>
        /// Gets recent log messages for debug display.
        /// </summary>
        public IReadOnlyList<string> RecentLogs => _recentLogs.AsReadOnly();

        /// <summary>
        /// Gets the current memory usage in MB.
        /// </summary>
        public float CurrentMemoryUsageMB => GC.GetTotalMemory(false) / (1024f * 1024f);

        /// <summary>
        /// Unity Awake method - early initialization.
        /// </summary>
        private void Awake()
        {
            // Ensure this GameObject persists across scene loads if it's a singleton
            if (transform.parent == null && gameObject.name.Contains("Logging"))
            {
                DontDestroyOnLoad(gameObject);
            }

            _currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            _lastQualityLevel = QualitySettings.GetQualityLevel();
        }

        /// <summary>
        /// Unity Start method - initializes logging integration.
        /// </summary>
        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        /// <summary>
        /// Asynchronously initializes the logging behaviour.
        /// </summary>
        private IEnumerator InitializeAsync()
        {
            // Wait for dependency injection to complete
            yield return new WaitUntil(() => _loggingService != null);

            try
            {
                if (_verboseStartup)
                {
                    Debug.Log("UnityLoggingBehaviour: Starting initialization");
                }

                // Initialize memory tracking
                _lastMemoryUsage = GC.GetTotalMemory(false);

                // Register Unity-specific log channels
                RegisterUnityLogChannels();

                // Set up coroutines for periodic logging
                if (_logMemoryUsage)
                {
                    StartCoroutine(LogMemoryUsagePeriodically());
                }

                if (_enableHealthMonitoring)
                {
                    StartCoroutine(MonitorHealthPeriodically());
                }

                // Start performance monitoring
                if (_monitorPerformance)
                {
                    StartCoroutine(UpdatePerformanceMetrics());
                }

                // Register for Unity events
                RegisterUnityEventHandlers();

                _initialized = true;

                if (_logUnityEvents)
                {
                    _loggingService.Log(LogLevel.Info, $"UnityLoggingBehaviour initialized on GameObject '{gameObject.name}'", 
                        default, "UnityLoggingBehaviour", null, null, "Unity.Logging.Lifecycle");
                }

                if (_verboseStartup)
                {
                    Debug.Log("UnityLoggingBehaviour: Initialization complete");
                    LogSystemInfo();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"UnityLoggingBehaviour initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Unity Update method - handles frame-based logging and monitoring.
        /// </summary>
        private void Update()
        {
            if (!IsInitialized) return;

            _frameCount++;
            _totalFrameCount++;
            _frameTime = Time.deltaTime * 1000f; // Convert to milliseconds

            // Update frame time history for averaging
            UpdateFrameTimeHistory();

            // Log frame rate periodically
            if (_logFrameRate && _frameCount % _frameRateLogInterval == 0)
            {
                var fps = CurrentFrameRate;
                var frameData = new Dictionary<string, object>
                {
                    ["FPS"] = fps,
                    ["FrameTimeMS"] = _frameTime,
                    ["AverageFrameTimeMS"] = _averageFrameTime,
                    ["SlowFramePercentage"] = SlowFramePercentage
                };

                _loggingService.Log(LogLevel.Info, $"Performance: {fps:F1} FPS, {_frameTime:F2}ms frame time", 
                    default, "UnityLoggingBehaviour", null, frameData, "Unity.Performance.FrameRate");
            }

            // Monitor slow frames
            if (_logSlowFrames && _frameTime > _slowFrameThreshold)
            {
                _slowFrameCount++;
                using var scope = _profilerService?.BeginScope(new ProfilerTag("Unity.SlowFrame"));
                
                var slowFrameData = new Dictionary<string, object>
                {
                    ["FrameTimeMS"] = _frameTime,
                    ["ThresholdMS"] = _slowFrameThreshold,
                    ["SlowFrameCount"] = _slowFrameCount,
                    ["SlowFramePercentage"] = SlowFramePercentage
                };

                _loggingService.Log(LogLevel.Warning, 
                    $"Slow frame detected: {_frameTime:F2}ms (threshold: {_slowFrameThreshold:F2}ms)", 
                    default, "UnityLoggingBehaviour", null, slowFrameData, "Unity.Performance.SlowFrame");
            }

            // Handle debug GUI toggle
            if (_enableDebugGUI && Input.GetKeyDown(_toggleDebugKey))
            {
                _showDebugGUI = !_showDebugGUI;
                _loggingService.Log(LogLevel.Debug, $"Debug GUI toggled: {(_showDebugGUI ? "ON" : "OFF")}", 
                    default, "UnityLoggingBehaviour", null, null, "Unity.Debug");
            }

            // Monitor memory spikes
            if (_logMemorySpikes)
            {
                MonitorMemorySpikes();
            }

            // Check for quality level changes
            if (_logQualityChanges)
            {
                CheckQualityLevelChanges();
            }

            // Check for scene changes
            if (_logSceneChanges)
            {
                CheckSceneChanges();
            }
        }

        /// <summary>
        /// Unity OnGUI method - displays debug logging information.
        /// </summary>
        private void OnGUI()
        {
            if (!_enableDebugGUI || !_showDebugGUI || !IsInitialized) return;

            InitializeGUIStyles();
            DrawDebugGUI();
        }

        /// <summary>
        /// Unity OnApplicationPause - logs application state changes.
        /// </summary>
        /// <param name="pauseStatus">Whether the application is paused</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!IsInitialized || !_logApplicationEvents) return;

            _applicationPaused = pauseStatus;
            var stateData = new Dictionary<string, object>
            {
                ["Paused"] = pauseStatus,
                ["Platform"] = Application.platform.ToString(),
                ["Timestamp"] = DateTime.UtcNow
            };

            _loggingService.Log(LogLevel.Info, $"Application {(pauseStatus ? "paused" : "resumed")}", 
                default, "UnityLoggingBehaviour", null, stateData, "Unity.Lifecycle.Pause");
        }

        /// <summary>
        /// Unity OnApplicationFocus - logs application focus changes.
        /// </summary>
        /// <param name="hasFocus">Whether the application has focus</param>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!IsInitialized || !_logApplicationEvents) return;

            _applicationFocused = hasFocus;
            var focusData = new Dictionary<string, object>
            {
                ["HasFocus"] = hasFocus,
                ["Platform"] = Application.platform.ToString(),
                ["Timestamp"] = DateTime.UtcNow
            };

            _loggingService.Log(LogLevel.Info, $"Application {(hasFocus ? "gained" : "lost")} focus", 
                default, "UnityLoggingBehaviour", null, focusData, "Unity.Lifecycle.Focus");
        }

        /// <summary>
        /// Unity OnDestroy - logs component destruction and cleanup.
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                if (IsInitialized && _logUnityEvents)
                {
                    _loggingService.Log(LogLevel.Info, $"UnityLoggingBehaviour destroyed on GameObject '{gameObject.name}'", 
                        default, "UnityLoggingBehaviour", null, null, "Unity.Logging.Lifecycle");
                    
                    // Log final statistics
                    LogFinalStatistics();
                }

                // Cleanup
                _recentLogs.Clear();
                _frameTimeHistory.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during UnityLoggingBehaviour cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually logs a message through this Unity component.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        /// <param name="channel">The log channel (optional)</param>
        public void LogMessage(LogLevel level, string message, string channel = "Unity.Manual")
        {
            if (!IsInitialized) return;

            var correlationId = Guid.NewGuid().ToString("N")[..8];
            var contextData = new Dictionary<string, object>
            {
                ["GameObject"] = gameObject.name,
                ["Scene"] = _currentSceneName,
                ["FrameCount"] = Time.frameCount
            };

            var correlationIdFixed = new FixedString64Bytes(correlationId);
            _loggingService.Log(level, message, correlationIdFixed, gameObject.name, null, contextData, channel);

            // Add to recent logs for debug display
            AddToRecentLogs($"[{level}] {message}");
        }

        /// <summary>
        /// Logs Unity system information.
        /// </summary>
        public void LogSystemInfo()
        {
            if (!IsInitialized) return;

            var systemInfo = new Dictionary<string, object>
            {
                ["Platform"] = Application.platform.ToString(),
                ["UnityVersion"] = Application.unityVersion,
                ["ProductName"] = Application.productName,
                ["Version"] = Application.version,
                ["CompanyName"] = Application.companyName,
                ["DeviceName"] = SystemInfo.deviceName,
                ["DeviceModel"] = SystemInfo.deviceModel,
                ["DeviceType"] = SystemInfo.deviceType.ToString(),
                ["ProcessorType"] = SystemInfo.processorType,
                ["ProcessorCount"] = SystemInfo.processorCount,
                ["ProcessorFrequency"] = SystemInfo.processorFrequency,
                ["SystemMemorySize"] = SystemInfo.systemMemorySize,
                ["GraphicsDeviceName"] = SystemInfo.graphicsDeviceName,
                ["GraphicsDeviceType"] = SystemInfo.graphicsDeviceType.ToString(),
                ["GraphicsMemorySize"] = SystemInfo.graphicsMemorySize,
                ["GraphicsDeviceVersion"] = SystemInfo.graphicsDeviceVersion,
                ["OperatingSystem"] = SystemInfo.operatingSystem,
                ["TargetFrameRate"] = Application.targetFrameRate,
                ["VSync"] = QualitySettings.vSyncCount,
                ["QualityLevel"] = QualitySettings.GetQualityLevel()
            };

            _loggingService.Log(LogLevel.Info, "Unity system information", 
                default, "UnityLoggingBehaviour", null, systemInfo, "Unity.SystemInfo");
        }

        /// <summary>
        /// Forces a flush of all logging targets.
        /// </summary>
        public async UniTaskVoid FlushLogs()
        {
            if (IsInitialized)
            {
                await _loggingService.FlushAsync();
                _loggingService.Log(LogLevel.Debug, "Manual log flush triggered from Unity component", 
                    default, "UnityLoggingBehaviour", null, null, "Unity.Debug");
            }
        }

        /// <summary>
        /// Triggers a health check and logs the results.
        /// </summary>
        public async UniTaskVoid CheckHealth()
        {
            if (!IsInitialized || _loggingHealthCheck == null) return;

            try
            {
                var result = await _loggingHealthCheck.CheckHealthAsync();
                
                var healthMessage = $"Logging health check: {result.Status} - {result.Description}";
                var logLevel = result.Status == HealthStatus.Healthy ? LogLevel.Info :
                              result.Status == HealthStatus.Degraded ? LogLevel.Warning : LogLevel.Error;

                var healthData = result.Data.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                healthData["TriggeredBy"] = "UnityLoggingBehaviour";
                healthData["GameObject"] = gameObject.name;

                _loggingService.Log(logLevel, healthMessage, default, "UnityLoggingBehaviour", null, healthData, "Unity.Health");
                
                AddToRecentLogs($"Health: {result.Status}");
            }
            catch (Exception ex)
            {
                _loggingService.LogException("Failed to perform health check from Unity component", ex);
            }
        }

        /// <summary>
        /// Gets current performance statistics.
        /// </summary>
        /// <returns>Dictionary containing performance data</returns>
        public Dictionary<string, object> GetPerformanceStats()
        {
            return new Dictionary<string, object>
            {
                ["CurrentFPS"] = CurrentFrameRate,
                ["AverageFrameTimeMS"] = _averageFrameTime,
                ["SlowFramePercentage"] = SlowFramePercentage,
                ["TotalFrames"] = _totalFrameCount,
                ["SlowFrames"] = _slowFrameCount,
                ["MemoryUsageMB"] = CurrentMemoryUsageMB,
                ["ApplicationPaused"] = _applicationPaused,
                ["ApplicationFocused"] = _applicationFocused,
                ["CurrentScene"] = _currentSceneName,
                ["QualityLevel"] = QualitySettings.GetQualityLevel()
            };
        }

        /// <summary>
        /// Gets the current logging configuration asset.
        /// </summary>
        public LoggingConfigurationAsset LoggingConfig => _loggingConfig;

        /// <summary>
        /// Sets the logging configuration asset.
        /// </summary>
        /// <param name="config">The new logging configuration</param>
        public void SetLoggingConfig(LoggingConfigurationAsset config)
        {
            _loggingConfig = config;
            
            if (IsInitialized)
            {
                _loggingService.Log(LogLevel.Info, $"Logging configuration updated: {(config?.name ?? "null")}", 
                    default, "UnityLoggingBehaviour", null, null, "Unity.ConfigSo");
            }
        }

        /// <summary>
        /// Gets whether the component is using scriptable object configuration.
        /// </summary>
        public bool UseScriptableObjectConfig => _useScriptableObjectConfig;

        /// <summary>
        /// Sets whether to use scriptable object configuration.
        /// </summary>
        /// <param name="useConfig">Whether to use scriptable object configuration</param>
        public void SetUseScriptableObjectConfig(bool useConfig)
        {
            _useScriptableObjectConfig = useConfig;
            
            if (IsInitialized)
            {
                _loggingService.Log(LogLevel.Info, $"ScriptableObject configuration {(useConfig ? "enabled" : "disabled")}", 
                    default, "UnityLoggingBehaviour", null, null, "Unity.ConfigSo");
            }
        }

        /// <summary>
        /// Validates the current logging configuration.
        /// </summary>
        /// <returns>Validation result with any errors</returns>
        public List<string> ValidateLoggingConfig()
        {
            var errors = new List<string>();
            
            if (_useScriptableObjectConfig)
            {
                if (_loggingConfig == null)
                {
                    errors.Add("Logging configuration asset is not assigned");
                }
                else
                {
                    var configErrors = _loggingConfig.ValidateConfiguration();
                    errors.AddRange(configErrors);
                }
            }
            
            return errors;
        }

        /// <summary>
        /// Gets a summary of the current logging configuration.
        /// </summary>
        /// <returns>Configuration summary string</returns>
        public string GetLoggingConfigSummary()
        {
            if (_useScriptableObjectConfig && _loggingConfig != null)
            {
                return _loggingConfig.GetConfigurationSummary();
            }
            else
            {
                return "Using traditional MonoBehaviour configuration";
            }
        }

        /// <summary>
        /// Registers Unity-specific log channels.
        /// </summary>
        private void RegisterUnityLogChannels()
        {
            // Unity-specific channels would be registered here if the logging service supported runtime channel registration
            // For now, we'll just log that Unity channels are being used
            _loggingService.Log(LogLevel.Debug, "Unity-specific log channels initialized", 
                default, "UnityLoggingBehaviour", null, null, "Unity.Logging.Channels");
        }

        /// <summary>
        /// Registers Unity event handlers for automatic logging.
        /// </summary>
        private void RegisterUnityEventHandlers()
        {
            if (_logSceneChanges)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            }

            _loggingService.Log(LogLevel.Debug, "Unity event handlers registered", 
                default, "UnityLoggingBehaviour", null, null, "Unity.Logging.Events");
        }

        /// <summary>
        /// Handles Unity scene loaded events.
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (!IsInitialized) return;

            var sceneData = new Dictionary<string, object>
            {
                ["SceneName"] = scene.name,
                ["ScenePath"] = scene.path,
                ["LoadMode"] = mode.ToString(),
                ["BuildIndex"] = scene.buildIndex,
                ["IsLoaded"] = scene.isLoaded,
                ["GameObjectCount"] = scene.rootCount
            };

            _loggingService.Log(LogLevel.Info, $"Scene loaded: {scene.name} ({mode})", 
                default, "UnityLoggingBehaviour", null, sceneData, "Unity.Scene.Loaded");
            _currentSceneName = scene.name;
        }

        /// <summary>
        /// Handles Unity scene unloaded events.
        /// </summary>
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            if (!IsInitialized) return;

            var sceneData = new Dictionary<string, object>
            {
                ["SceneName"] = scene.name,
                ["ScenePath"] = scene.path,
                ["BuildIndex"] = scene.buildIndex
            };

            _loggingService.Log(LogLevel.Info, $"Scene unloaded: {scene.name}", 
                default, "UnityLoggingBehaviour", null, sceneData, "Unity.Scene.Unloaded");
        }

        /// <summary>
        /// Coroutine that logs memory usage periodically.
        /// </summary>
        private IEnumerator LogMemoryUsagePeriodically()
        {
            while (_initialized)
            {
                yield return new WaitForSeconds(_memoryLogInterval);

                if (!_initialized) break;

                var currentMemory = GC.GetTotalMemory(false);
                var memoryMB = currentMemory / (1024f * 1024f);

                var memoryData = new Dictionary<string, object>
                {
                    ["TotalMemoryMB"] = memoryMB,
                    ["Gen0Collections"] = GC.CollectionCount(0),
                    ["Gen1Collections"] = GC.CollectionCount(1),
                    ["Gen2Collections"] = GC.CollectionCount(2),
                    ["UnityAllocatedMemoryMB"] = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                    ["UnityReservedMemoryMB"] = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f)
                };

                _loggingService.Log(LogLevel.Info, $"Memory usage: {memoryMB:F2} MB", 
                    default, "UnityLoggingBehaviour", null, memoryData, "Unity.Performance.Memory");
                _lastMemoryUsage = currentMemory;
            }
        }

        /// <summary>
        /// Coroutine that monitors system health periodically.
        /// </summary>
        private IEnumerator MonitorHealthPeriodically()
        {
            while (_initialized)
            {
                yield return new WaitForSeconds(_healthCheckInterval);

                if (!_initialized) break;

                try
                {
                    if (_loggingHealthCheck != null)
                    {
                        // Start the async health check without awaiting (fire-and-forget)
                        PerformHealthCheckAsync();
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogException("Periodic health check failed in UnityLoggingBehaviour", ex);
                }
            }
        }

        /// <summary>
        /// Performs the health check asynchronously.
        /// </summary>
        private async UniTaskVoid PerformHealthCheckAsync()
        {
            try
            {
                var result = await _loggingHealthCheck.CheckHealthAsync();
                
                if (_logHealthStatus && IsInitialized)
                {
                    var logLevel = result.Status == HealthStatus.Healthy ? LogLevel.Debug : LogLevel.Warning;
                    var healthData = result.Data.AsValueEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    healthData["PeriodicCheck"] = true;
                    healthData["GameObject"] = gameObject.name;

                    _loggingService.Log(logLevel, $"Periodic health check: {result.Status}", 
                        default, "UnityLoggingBehaviour", null, healthData, "Unity.Health.Periodic");
                }
            }
            catch (Exception ex)
            {
                if (IsInitialized)
                {
                    _loggingService.LogException("Async health check failed", ex);
                }
            }
        }

        /// <summary>
        /// Coroutine that updates performance metrics.
        /// </summary>
        private IEnumerator UpdatePerformanceMetrics()
        {
            while (_initialized)
            {
                yield return new WaitForSeconds(1f); // Update every second

                if (!_initialized) break;

                // Log performance summary
                var perfData = GetPerformanceStats();
                _loggingService.Log(LogLevel.Debug, 
                    $"Performance summary - FPS: {CurrentFrameRate:F1}, Memory: {CurrentMemoryUsageMB:F1}MB", 
                    default, "UnityLoggingBehaviour", null, perfData, "Unity.Performance.Summary");
            }
        }

        /// <summary>
        /// Updates the frame time history for averaging calculations.
        /// </summary>
        private void UpdateFrameTimeHistory()
        {
            _frameTimeHistory.Enqueue(_frameTime);

            if (_frameTimeHistory.Count > FrameHistorySize)
            {
                _frameTimeHistory.Dequeue();
            }

            // Calculate average frame time
            if (_frameTimeHistory.Count > 0)
            {
                _averageFrameTime = _frameTimeHistory.AsValueEnumerable().Average();
            }
        }

        /// <summary>
        /// Monitors for memory allocation spikes.
        /// </summary>
        private void MonitorMemorySpikes()
        {
            var currentMemory = GC.GetTotalMemory(false);
            var memoryDelta = currentMemory - _lastMemoryUsage;

            if (memoryDelta > _memorySpikeThreshold)
            {
                var spikeMB = memoryDelta / (1024f * 1024f);
                var spikeData = new Dictionary<string, object>
                {
                    ["SpikeMB"] = spikeMB,
                    ["ThresholdMB"] = _memorySpikeThreshold / (1024f * 1024f),
                    ["CurrentMemoryMB"] = currentMemory / (1024f * 1024f),
                    ["FrameNumber"] = Time.frameCount
                };

                _loggingService.Log(LogLevel.Warning, 
                    $"Memory spike detected: +{spikeMB:F2} MB in one frame", 
                    default, "UnityLoggingBehaviour", null, spikeData, "Unity.Performance.MemorySpike");
                
                _lastMemoryUsage = currentMemory;
                AddToRecentLogs($"Memory spike: +{spikeMB:F1}MB");
            }
        }

        /// <summary>
        /// Checks for quality level changes.
        /// </summary>
        private void CheckQualityLevelChanges()
        {
            var currentQuality = QualitySettings.GetQualityLevel();
            if (currentQuality != _lastQualityLevel)
            {
                var qualityData = new Dictionary<string, object>
                {
                    ["PreviousLevel"] = _lastQualityLevel,
                    ["NewLevel"] = currentQuality,
                    ["QualityName"] = QualitySettings.names[currentQuality]
                };

                _loggingService.Log(LogLevel.Info, 
                    $"Quality level changed: {_lastQualityLevel} -> {currentQuality} ({QualitySettings.names[currentQuality]})", 
                    default, "UnityLoggingBehaviour", null, qualityData, "Unity.Settings.Quality");

                _lastQualityLevel = currentQuality;
            }
        }

        /// <summary>
        /// Checks for scene changes.
        /// </summary>
        private void CheckSceneChanges()
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != _currentSceneName && !string.IsNullOrEmpty(currentScene))
            {
                _currentSceneName = currentScene;
                // Scene change is already handled by the event system
            }
        }

        /// <summary>
        /// Initializes GUI styles for debug display.
        /// </summary>
        private void InitializeGUIStyles()
        {
            if (_stylesInitialized) return;

            _debugBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                normal = { background = MakeColorTexture(new Color(0, 0, 0, 0.8f)) }
            };

            _debugLabelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 12
            };

            _debugButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11
            };

            _stylesInitialized = true;
        }

        /// <summary>
        /// Creates a solid color texture for GUI styling.
        /// </summary>
        private Texture2D MakeColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Draws the debug GUI overlay.
        /// </summary>
        private void DrawDebugGUI()
        {
            var rect = new Rect(10, 10, 500, 400);
            GUI.Box(rect, "Unity Logging Debug Info", _debugBoxStyle);

            var contentRect = new Rect(rect.x + 10, rect.y + 30, rect.width - 20, rect.height - 40);
            
            GUILayout.BeginArea(contentRect);
            _debugScrollPosition = GUILayout.BeginScrollView(_debugScrollPosition);

            // Performance stats
            GUILayout.Label("=== Performance Stats ===", _debugLabelStyle);
            GUILayout.Label($"FPS: {CurrentFrameRate:F1} | Frame Time: {_frameTime:F2}ms | Avg: {_averageFrameTime:F2}ms", _debugLabelStyle);
            GUILayout.Label($"Slow Frames: {_slowFrameCount} ({SlowFramePercentage:F1}%)", _debugLabelStyle);
            GUILayout.Label($"Memory: {CurrentMemoryUsageMB:F2} MB", _debugLabelStyle);

            GUILayout.Space(10);

            // Application state
            GUILayout.Label("=== Application State ===", _debugLabelStyle);
            GUILayout.Label($"Scene: {_currentSceneName}", _debugLabelStyle);
            GUILayout.Label($"Quality: Level {_lastQualityLevel} ({(QualitySettings.names.Length > _lastQualityLevel && _lastQualityLevel >= 0 ? QualitySettings.names[_lastQualityLevel] : "Unknown")})", _debugLabelStyle);
            GUILayout.Label($"Paused: {_applicationPaused} | Focused: {_applicationFocused}", _debugLabelStyle);

            GUILayout.Space(10);

            // Configuration info
            GUILayout.Label("=== Configuration ===", _debugLabelStyle);
            GUILayout.Label($"Using ScriptableObject: {_useScriptableObjectConfig}", _debugLabelStyle);
            if (_useScriptableObjectConfig && _loggingConfig != null)
            {
                GUILayout.Label($"ConfigSo Asset: {_loggingConfig.name}", _debugLabelStyle);
                GUILayout.Label($"Targets: {_loggingConfig.TargetConfigurations.Count}", _debugLabelStyle);
                GUILayout.Label($"Filters: {_loggingConfig.FilterConfigurations.Count}", _debugLabelStyle);
                GUILayout.Label($"Formatters: {_loggingConfig.FormatterConfigurations.Count}", _debugLabelStyle);
            }
            else if (_useScriptableObjectConfig)
            {
                GUILayout.Label("ConfigSo Asset: Not Assigned", _debugLabelStyle);
            }
            else
            {
                GUILayout.Label("Using MonoBehaviour ConfigSo", _debugLabelStyle);
            }

            GUILayout.Space(10);

            // Health status
            if (_loggingHealthCheck != null && _loggingHealthCheck.IsCacheValid())
            {
                var health = _loggingHealthCheck.GetCachedResult();
                GUILayout.Label($"=== Health Status ===", _debugLabelStyle);
                GUILayout.Label($"Status: {health.Status}", _debugLabelStyle);
                GUILayout.Label($"Description: {health.Description}", _debugLabelStyle);
            }

            GUILayout.Space(10);

            // Manual controls
            GUILayout.Label("=== Controls ===", _debugLabelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Check Health", _debugButtonStyle, GUILayout.Width(100)))
            {
                CheckHealth();
            }
            if (GUILayout.Button("Flush Logs", _debugButtonStyle, GUILayout.Width(100)))
            {
                FlushLogs();
            }
            if (GUILayout.Button("System Info", _debugButtonStyle, GUILayout.Width(100)))
            {
                LogSystemInfo();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Warning", _debugButtonStyle, GUILayout.Width(100)))
            {
                LogMessage(LogLevel.Warning, "Test warning message from debug GUI");
            }
            if (GUILayout.Button("Test Error", _debugButtonStyle, GUILayout.Width(100)))
            {
                LogMessage(LogLevel.Error, "Test error message from debug GUI");
            }
            if (GUILayout.Button("Force GC", _debugButtonStyle, GUILayout.Width(100)))
            {
                GC.Collect();
                LogMessage(LogLevel.Info, "Garbage collection forced from debug GUI");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate ConfigSo", _debugButtonStyle, GUILayout.Width(100)))
            {
                var errors = ValidateLoggingConfig();
                var message = errors.Count == 0 ? "Configuration is valid" : $"Configuration has {errors.Count} errors";
                LogMessage(errors.Count == 0 ? LogLevel.Info : LogLevel.Warning, message);
            }
            if (GUILayout.Button("ConfigSo Summary", _debugButtonStyle, GUILayout.Width(100)))
            {
                var summary = GetLoggingConfigSummary();
                LogMessage(LogLevel.Info, $"Configuration Summary:\n{summary}");
            }
            if (GUILayout.Button("Toggle SO ConfigSo", _debugButtonStyle, GUILayout.Width(100)))
            {
                SetUseScriptableObjectConfig(!_useScriptableObjectConfig);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Recent logs
            GUILayout.Label("=== Recent Logs ===", _debugLabelStyle);
            foreach (var log in _recentLogs.AsValueEnumerable().TakeLast(10))
            {
                GUILayout.Label(log, _debugLabelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Adds a message to the recent logs list for debug display.
        /// </summary>
        /// <param name="message">The message to add</param>
        private void AddToRecentLogs(string message)
        {
            var timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _recentLogs.Add(timestampedMessage);

            // Keep only the most recent logs
            while (_recentLogs.Count > MaxRecentLogs)
            {
                _recentLogs.RemoveAt(0);
            }
        }

        /// <summary>
        /// Logs final statistics when the component is destroyed.
        /// </summary>
        private void LogFinalStatistics()
        {
            var finalStats = new Dictionary<string, object>
            {
                ["TotalFrames"] = _totalFrameCount,
                ["SlowFrames"] = _slowFrameCount,
                ["SlowFramePercentage"] = SlowFramePercentage,
                ["AverageFrameTimeMS"] = _averageFrameTime,
                ["FinalMemoryMB"] = CurrentMemoryUsageMB,
                ["SessionDuration"] = Time.time,
                ["FinalScene"] = _currentSceneName
            };

            _loggingService.Log(LogLevel.Info, "Unity logging session statistics", 
                default, "UnityLoggingBehaviour", null, finalStats, "Unity.Logging.SessionStats");
        }

        /// <summary>
        /// Validates the component configuration in the Unity Editor.
        /// </summary>
        private void OnValidate()
        {
            _frameRateLogInterval = Mathf.Max(1, _frameRateLogInterval);
            _memoryLogInterval = Mathf.Max(1f, _memoryLogInterval);
            _slowFrameThreshold = Mathf.Max(1f, _slowFrameThreshold);
            _memorySpikeThreshold = Math.Max(1024L, _memorySpikeThreshold); // At least 1KB
            _healthCheckInterval = Mathf.Max(1f, _healthCheckInterval);
            
            // Validate scriptable object configuration if enabled
            if (_useScriptableObjectConfig && _loggingConfig != null)
            {
                var configErrors = _loggingConfig.ValidateConfiguration();
                if (configErrors.Count > 0)
                {
                    Debug.LogWarning($"Logging configuration validation failed:\n{string.Join("\n", configErrors)}", this);
                }
            }
        }

        /// <summary>
        /// Unity Editor method to display custom inspector information.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw performance indicators in scene view when selected
            if (IsInitialized && _monitorPerformance)
            {
                var color = SlowFramePercentage > 10f ? Color.red : 
                           SlowFramePercentage > 5f ? Color.yellow : Color.green;
                
                Gizmos.color = color;
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }

        /// <summary>
        /// Public method to get logging statistics for external monitoring.
        /// </summary>
        /// <returns>A dictionary containing current logging and performance statistics</returns>
        public Dictionary<string, object> GetLoggingStatistics()
        {
            if (!IsInitialized)
            {
                return new Dictionary<string, object> { ["Status"] = "Not Initialized" };
            }

            var stats = GetPerformanceStats();
            
            // Add logging-specific stats
            stats["RecentLogCount"] = _recentLogs.Count;
            stats["IsLoggingHealthy"] = _loggingHealthCheck?.GetCachedResult()?.Status.ToString() ?? "Unknown";
            stats["LoggingTargetCount"] = _loggingService.GetTargets().Count;
            
            return stats;
        }

        /// <summary>
        /// Enables or disables specific logging features at runtime.
        /// </summary>
        /// <param name="featureName">The name of the feature to toggle</param>
        /// <param name="enabled">Whether to enable or disable the feature</param>
        public void SetLoggingFeature(string featureName, bool enabled)
        {
            if (!IsInitialized) return;

            switch (featureName.ToLower())
            {
                case "framerate":
                    _logFrameRate = enabled;
                    break;
                case "memory":
                    _logMemoryUsage = enabled;
                    break;
                case "slowframes":
                    _logSlowFrames = enabled;
                    break;
                case "memoryspikes":
                    _logMemorySpikes = enabled;
                    break;
                case "health":
                    _enableHealthMonitoring = enabled;
                    break;
                case "unityevents":
                    _logUnityEvents = enabled;
                    break;
                case "debuggui":
                    _enableDebugGUI = enabled;
                    break;
                default:
                    _loggingService.Log(LogLevel.Warning, $"Unknown logging feature: {featureName}", 
                        default, "UnityLoggingBehaviour", null, null, "Unity.ConfigSo");
                    return;
            }

            _loggingService.Log(LogLevel.Info, $"Logging feature '{featureName}' {(enabled ? "enabled" : "disabled")}", 
                default, "UnityLoggingBehaviour", null, null, "Unity.ConfigSo");
        }

        /// <summary>
        /// Updates logging thresholds at runtime.
        /// </summary>
        /// <param name="slowFrameThreshold">New slow frame threshold in milliseconds</param>
        /// <param name="memorySpikeThreshold">New memory spike threshold in bytes</param>
        public void UpdateThresholds(float slowFrameThreshold, long memorySpikeThreshold)
        {
            if (!IsInitialized) return;

            var oldSlowFrame = _slowFrameThreshold;
            var oldMemorySpike = _memorySpikeThreshold;

            _slowFrameThreshold = Mathf.Max(1f, slowFrameThreshold);
            _memorySpikeThreshold = System.Math.Max(1024L, memorySpikeThreshold);

            var thresholdData = new Dictionary<string, object>
            {
                ["OldSlowFrameThreshold"] = oldSlowFrame,
                ["NewSlowFrameThreshold"] = _slowFrameThreshold,
                ["OldMemorySpikeThreshold"] = oldMemorySpike,
                ["NewMemorySpikeThreshold"] = _memorySpikeThreshold
            };

            _loggingService.Log(LogLevel.Info, "Logging thresholds updated", 
                default, "UnityLoggingBehaviour", null, thresholdData, "Unity.ConfigSo");
        }

        /// <summary>
        /// Exports current session data to a formatted string.
        /// </summary>
        /// <returns>A formatted string containing session information</returns>
        public string ExportSessionData()
        {
            if (!IsInitialized) return "Logging not initialized";

            var sessionData = new System.Text.StringBuilder();
            sessionData.AppendLine("=== Unity Logging Session Data ===");
            sessionData.AppendLine($"GameObject: {gameObject.name}");
            sessionData.AppendLine($"Scene: {_currentSceneName}");
            sessionData.AppendLine($"Session Duration: {Time.time:F1}s");
            sessionData.AppendLine($"Total Frames: {_totalFrameCount}");
            sessionData.AppendLine($"Slow Frames: {_slowFrameCount} ({SlowFramePercentage:F1}%)");
            sessionData.AppendLine($"Average Frame Time: {_averageFrameTime:F2}ms");
            sessionData.AppendLine($"Current FPS: {CurrentFrameRate:F1}");
            sessionData.AppendLine($"Memory Usage: {CurrentMemoryUsageMB:F2}MB");
            sessionData.AppendLine($"Quality Level: {_lastQualityLevel}");
            sessionData.AppendLine($"Application State: Paused={_applicationPaused}, Focused={_applicationFocused}");
            
            if (_loggingHealthCheck != null && _loggingHealthCheck.IsCacheValid())
            {
                var health = _loggingHealthCheck.GetCachedResult();
                sessionData.AppendLine($"Health Status: {health.Status} - {health.Description}");
            }

            sessionData.AppendLine("\n=== Recent Logs ===");
            foreach (var log in _recentLogs.AsValueEnumerable().TakeLast(5))
            {
                sessionData.AppendLine(log);
            }

            return sessionData.ToString();
        }
    }

    /// <summary>
    /// Attribute for dependency injection (placeholder for Reflex integration).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
    }

    /// <summary>
    /// Extension methods for Unity logging integration.
    /// </summary>
    public static class UnityLoggingExtensions
    {
        /// <summary>
        /// Logs a message with GameObject context.
        /// </summary>
        /// <param name="loggingService">The logging service</param>
        /// <param name="gameObject">The source GameObject</param>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        /// <param name="channel">The log channel</param>
        public static void LogFromGameObject(this ILoggingService loggingService, GameObject gameObject, 
            LogLevel level, string message, string channel = "Unity.GameObject")
        {
            if (loggingService == null || gameObject == null) return;

            var contextData = new Dictionary<string, object>
            {
                ["GameObject"] = gameObject.name,
                ["Position"] = gameObject.transform.position.ToString(),
                ["Scene"] = gameObject.scene.name,
                ["Active"] = gameObject.activeInHierarchy
            };

            loggingService.Log(level, message, default, gameObject.name, null, contextData, channel);
        }

        /// <summary>
        /// Logs component lifecycle events.
        /// </summary>
        /// <param name="loggingService">The logging service</param>
        /// <param name="component">The source component</param>
        /// <param name="lifecycleEvent">The lifecycle event name</param>
        public static void LogComponentLifecycle(this ILoggingService loggingService, Component component, string lifecycleEvent)
        {
            if (loggingService == null || component == null) return;

            var contextData = new Dictionary<string, object>
            {
                ["Component"] = component.GetType().Name,
                ["GameObject"] = component.gameObject.name,
                ["LifecycleEvent"] = lifecycleEvent,
                ["Scene"] = component.gameObject.scene.name
            };

            loggingService.Log(LogLevel.Debug, $"{component.GetType().Name} {lifecycleEvent}", 
                default, component.GetType().Name, null, contextData, "Unity.Component.Lifecycle");
        }
    }
}