using System;
using System.Collections.Generic;
using AhBearStudios.Core.DependencyInjection.Unity;
using AhBearStudios.Core.MessageBus.Unity;
using AhBearStudios.Core.Profiling.Data;
using UnityEngine;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Factories;
using AhBearStudios.Core.Profiling.Metrics;
using AhBearStudios.Core.Profiling.Profilers;
using AhBearStudios.Core.Profiling.Unity.Configuration;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Unity integration manager for the profiling system.
    /// Handles initialization, configuration, and lifecycle management.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ProfileManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _enableOnStart = true;
        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private ProfilerConfiguration _configuration;
        
        [Header("Dependencies")]
        [SerializeField] private MessageBusProvider _messageBusProvider;
        [SerializeField] private DependencyProvider _dependencyProvider;
        
        [Header("Metrics")]
        [SerializeField] private float _metricsUpdateInterval = 0.1f;
        [SerializeField] private bool _enableSystemMetrics = true;
        [SerializeField] private bool _enablePoolMetrics = true;
        [SerializeField] private bool _enableSerializationMetrics = true;
        
        private IProfiler _profiler;
        private IPoolMetrics _poolMetrics;
        private ISerializerMetrics _serializerMetrics;
        private SystemMetricsTracker _systemMetrics;
        private ThresholdAlertSystem _alertSystem;
        private ProfilerStatsCollection _statsCollection;
        
        private float _lastMetricsUpdate;
        private bool _isInitialized;
        
        /// <summary>
        /// Gets the current profiler instance
        /// </summary>
        public IProfiler Profiler => _profiler;
        
        /// <summary>
        /// Gets the pool metrics instance
        /// </summary>
        public IPoolMetrics PoolMetrics => _poolMetrics;
        
        /// <summary>
        /// Gets the serializer metrics instance
        /// </summary>
        public ISerializerMetrics SerializerMetrics => _serializerMetrics;
        
        /// <summary>
        /// Gets the system metrics tracker
        /// </summary>
        public SystemMetricsTracker SystemMetrics => _systemMetrics;
        
        /// <summary>
        /// Gets whether profiling is currently enabled
        /// </summary>
        public bool IsEnabled => _profiler?.IsEnabled ?? false;
        
        /// <summary>
        /// Gets the profiler configuration
        /// </summary>
        public ProfilerConfiguration Configuration => _configuration;
        
        /// <summary>
        /// Event fired when the profiler is initialized
        /// </summary>
        public event Action<ProfileManager> Initialized;
        
        /// <summary>
        /// Event fired when profiling is started
        /// </summary>
        public event Action<ProfileManager> ProfilingStarted;
        
        /// <summary>
        /// Event fired when profiling is stopped
        /// </summary>
        public event Action<ProfileManager> ProfilingStopped;
        
        private void Awake()
        {
            if (_persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            InitializeProfiler();
        }
        
        private void Start()
        {
            if (_enableOnStart && _isInitialized)
            {
                StartProfiling();
            }
        }
        
        private void Update()
        {
            if (!_isInitialized)
                return;
                
            UpdateMetrics();
            UpdateAlertSystem();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        /// <summary>
        /// Initializes the profiling system
        /// </summary>
        private void InitializeProfiler()
        {
            try
            {
                // Ensure we have dependencies
                if (_messageBusProvider == null)
                {
                    _messageBusProvider = FindFirstObjectByType<MessageBusProvider>();
                }
                
                if (_dependencyProvider == null)
                {
                    _dependencyProvider = FindFirstObjectByType<DependencyProvider>();
                }
                
                // Create configuration if not assigned
                if (_configuration == null)
                {
                    _configuration = ScriptableObject.CreateInstance<ProfilerConfiguration>();
                    _configuration.InitializeDefaults();
                }
                
                // Initialize components
                InitializeComponents();
                
                _isInitialized = true;
                Initialized?.Invoke(this);
                
                Debug.Log("[ProfileManager] Profiler system initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileManager] Failed to initialize profiler: {ex.Message}");
                _isInitialized = false;
            }
        }
        
        /// <summary>
        /// Initializes all profiling components
        /// </summary>
        private void InitializeComponents()
        {
            var messageBus = _messageBusProvider?.MessageBus;
            var dependencyProvider = _dependencyProvider?.DependencyProvider;
            
            // Initialize stats collection
            _statsCollection = new ProfilerStatsCollection();
            
            // Initialize metrics
            if (_enablePoolMetrics)
            {
                _poolMetrics = PoolMetricsFactory.CreateStandard(messageBus);
            }
            
            if (_enableSerializationMetrics)
            {
                _serializerMetrics = new SerializerMetricsData();
            }
            
            if (_enableSystemMetrics)
            {
                _systemMetrics = new SystemMetricsTracker(_metricsUpdateInterval);
                RegisterDefaultSystemMetrics();
            }
            
            // Initialize profiler
            if (dependencyProvider != null)
            {
                var profilerFactory = new ProfilerFactory(dependencyProvider);
                _profiler = profilerFactory.CreateProfiler();
            }
            else if (messageBus != null)
            {
                _profiler = new DefaultProfiler(messageBus);
            }
            else
            {
                Debug.LogWarning("[ProfileManager] No message bus available, using null profiler");
                var profilerFactory = new ProfilerFactory(null);
                _profiler = profilerFactory.GetNullProfiler();
            }
            
            // Initialize alert system
            if (messageBus != null)
            {
                _alertSystem = new ThresholdAlertSystem(this, messageBus, _configuration.LogAlertsToConsole);
                RegisterDefaultAlerts();
            }
        }
        
        /// <summary>
        /// Registers default system metrics
        /// </summary>
        private void RegisterDefaultSystemMetrics()
        {
            if (_systemMetrics == null || _configuration == null)
                return;
                
            foreach (var metricConfig in _configuration.SystemMetrics)
            {
                if (metricConfig.Enabled)
                {
                    _systemMetrics.RegisterMetric(
                        new ProfilerTag(metricConfig.Category, metricConfig.Name),
                        metricConfig.StatName,
                        metricConfig.Unit
                    );
                }
            }
        }
        
        /// <summary>
        /// Registers default threshold alerts
        /// </summary>
        private void RegisterDefaultAlerts()
        {
            if (_alertSystem == null || _configuration == null)
                return;
                
            foreach (var alertConfig in _configuration.ThresholdAlerts)
            {
                if (alertConfig.Enabled)
                {
                    var tag = new ProfilerTag(alertConfig.Category, alertConfig.Name);
                    
                    if (alertConfig.IsSessionAlert)
                    {
                        _alertSystem.RegisterSessionThreshold(tag, alertConfig.Threshold, alertConfig.CooldownSeconds);
                    }
                    else
                    {
                        _alertSystem.RegisterMetricThreshold(tag, alertConfig.Threshold, alertConfig.CooldownSeconds);
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates metrics systems
        /// </summary>
        private void UpdateMetrics()
        {
            float currentTime = Time.unscaledTime;
            
            if (currentTime - _lastMetricsUpdate >= _metricsUpdateInterval)
            {
                _systemMetrics?.Update(_metricsUpdateInterval);
                _lastMetricsUpdate = currentTime;
            }
        }
        
        /// <summary>
        /// Updates the alert system
        /// </summary>
        private void UpdateAlertSystem()
        {
            _alertSystem?.Update(Time.unscaledDeltaTime);
        }
        
        /// <summary>
        /// Starts profiling
        /// </summary>
        public void StartProfiling()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[ProfileManager] Cannot start profiling - system not initialized");
                return;
            }
            
            _profiler?.StartProfiling();
            _systemMetrics?.Start();
            _alertSystem?.Start();
            
            ProfilingStarted?.Invoke(this);
            
            if (_configuration.LogToConsole)
            {
                Debug.Log("[ProfileManager] Profiling started");
            }
        }
        
        /// <summary>
        /// Stops profiling
        /// </summary>
        public void StopProfiling()
        {
            _profiler?.StopProfiling();
            _systemMetrics?.Stop();
            _alertSystem?.Stop();
            
            ProfilingStopped?.Invoke(this);
            
            if (_configuration.LogToConsole)
            {
                Debug.Log("[ProfileManager] Profiling stopped");
            }
        }
        
        /// <summary>
        /// Resets all profiling statistics
        /// </summary>
        public void ResetStats()
        {
            _profiler?.ResetStats();
            _poolMetrics?.ResetStats();
            _serializerMetrics?.Reset();
            _statsCollection?.Reset();
            
            if (_configuration.LogToConsole)
            {
                Debug.Log("[ProfileManager] Profiling stats reset");
            }
        }
        
        /// <summary>
        /// Gets metrics for a specific tag
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <returns>Metrics data for the tag</returns>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            return _profiler?.GetMetrics(tag) ?? new DefaultMetricsData();
        }
        
        /// <summary>
        /// Gets all current metrics
        /// </summary>
        /// <returns>Dictionary of all metrics</returns>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllMetrics()
        {
            return _profiler?.GetAllMetrics() ?? new Dictionary<ProfilerTag, DefaultMetricsData>();
        }
        
        /// <summary>
        /// Begins a profiling scope
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <returns>Profiler session</returns>
        public ProfilerSession BeginScope(ProfilerTag tag)
        {
            return _profiler?.BeginScope(tag);
        }
        
        /// <summary>
        /// Begins a profiling scope with category and name
        /// </summary>
        /// <param name="category">The profiler category</param>
        /// <param name="name">The scope name</param>
        /// <returns>Profiler session</returns>
        public ProfilerSession BeginScope(ProfilerCategory category, string name)
        {
            return _profiler?.BeginScope(category, name);
        }
        
        /// <summary>
        /// Registers a metric threshold alert
        /// </summary>
        /// <param name="tag">The metric tag</param>
        /// <param name="threshold">The threshold value</param>
        /// <param name="cooldownSeconds">Cooldown period in seconds</param>
        public void RegisterMetricAlert(ProfilerTag tag, double threshold, float cooldownSeconds = 5.0f)
        {
            _alertSystem?.RegisterMetricThreshold(tag, threshold, cooldownSeconds);
        }
        
        /// <summary>
        /// Registers a session threshold alert
        /// </summary>
        /// <param name="tag">The session tag</param>
        /// <param name="thresholdMs">The threshold in milliseconds</param>
        /// <param name="cooldownSeconds">Cooldown period in seconds</param>
        public void RegisterSessionAlert(ProfilerTag tag, double thresholdMs, float cooldownSeconds = 5.0f)
        {
            _alertSystem?.RegisterSessionThreshold(tag, thresholdMs, cooldownSeconds);
        }
        
        /// <summary>
        /// Cleans up resources
        /// </summary>
        private void Cleanup()
        {
            try
            {
                _systemMetrics?.Dispose();
                _alertSystem?.Dispose();
                
                _systemMetrics = null;
                _alertSystem = null;
                _profiler = null;
                _poolMetrics = null;
                _serializerMetrics = null;
                _statsCollection = null;
                
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileManager] Error during cleanup: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the singleton instance (creates one if none exists)
        /// </summary>
        public static ProfileManager Instance
        {
            get
            {
                var instance = FindFirstObjectByType<ProfileManager>();
                if (instance == null)
                {
                    var go = new GameObject("[ProfileManager]");
                    instance = go.AddComponent<ProfileManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
    }
}