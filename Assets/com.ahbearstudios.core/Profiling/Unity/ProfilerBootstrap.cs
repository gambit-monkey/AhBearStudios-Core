using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using AhBearStudios.Core.Profiling.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Component that initializes and configures the runtime profiler system
    /// </summary>
    public class ProfilerBootstrap : MonoBehaviour
    {
        [Header("Profiler Configuration")]
        [SerializeField] private bool _enableOnStart = true;
        [SerializeField] private bool _logToConsole = true;
        [SerializeField] private float _systemMetricsSampleInterval = 0.5f;
        
        [Header("Default Metrics")]
        [SerializeField] private bool _trackDefaultMetrics = true;
        [SerializeField] private bool _trackGC = true;
        [SerializeField] private bool _trackFrameTime = true;
        [SerializeField] private bool _trackDrawCalls = true;
        [SerializeField] private bool _trackPhysics = true;
        
        [Header("UI")]
        [SerializeField] private bool _createOverlayUI = true;
        [SerializeField] private GameObject _overlayUIPrefab;
        [SerializeField] private bool _showUIOnStart = true;
        [SerializeField] private KeyCode _toggleUIKey = KeyCode.F3;
        
        [Header("Threshold Alerts")]
        [SerializeField] private bool _enableFrameTimeAlert = true;
        [SerializeField] private float _frameTimeThresholdMs = 33.3f;
        [SerializeField] private bool _enableGCAllocationAlert = true;
        [SerializeField] private float _gcAllocationThresholdKB = 1000f;
        [SerializeField] private bool _enableCustomSessionAlerts = true;
        [SerializeField] private float _defaultSessionThresholdMs = 10.0f;
        
        // Overlay UI instance
        private GameObject _overlayUIInstance;
        
        // Reference to profiler manager
        private RuntimeProfilerManager _profilerManager;
        
        private void Start()
        {
            // Get or create the runtime profiler manager
            _profilerManager = RuntimeProfilerManager.Instance;
            _profilerManager.LogToConsole = _logToConsole && Debug.isDebugBuild;
            
            // Register for profiler events
            RegisterEventHandlers();
            
            // Initialize system metrics
            InitializeSystemMetrics();
            
            // Set up threshold alerts
            SetupThresholdAlerts();
            
            // Create overlay UI if requested
            if (_createOverlayUI)
            {
                CreateOverlayUI();
            }
            
            // Start profiling if enabled
            if (_enableOnStart)
            {
                _profilerManager.StartProfiling();
            }
            else
            {
                _profilerManager.StopProfiling();
            }
        }
        
        private void OnDestroy()
        {
            // Unregister events
            UnregisterEventHandlers();
        }
        
        private void Update()
        {
            // Toggle UI visibility with key
            if (_overlayUIInstance != null && Input.GetKeyDown(_toggleUIKey))
            {
                _overlayUIInstance.SetActive(!_overlayUIInstance.activeSelf);
            }
        }
        
        /// <summary>
        /// Register for event handlers
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (_profilerManager != null)
            {
                _profilerManager.ProfilingStarted += OnProfilingStarted;
                _profilerManager.ProfilingStopped += OnProfilingStopped;
                _profilerManager.StatsReset += OnStatsReset;
                _profilerManager.MetricAlertTriggered += OnMetricAlertTriggered;
                _profilerManager.SessionAlertTriggered += OnSessionAlertTriggered;
                _profilerManager.SessionCompleted += OnSessionCompleted;
            }
        }
        
        /// <summary>
        /// Unregister event handlers
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_profilerManager != null)
            {
                _profilerManager.ProfilingStarted -= OnProfilingStarted;
                _profilerManager.ProfilingStopped -= OnProfilingStopped;
                _profilerManager.StatsReset -= OnStatsReset;
                _profilerManager.MetricAlertTriggered -= OnMetricAlertTriggered;
                _profilerManager.SessionAlertTriggered -= OnSessionAlertTriggered;
                _profilerManager.SessionCompleted -= OnSessionCompleted;
            }
        }
        
        /// <summary>
        /// Initialize system metrics tracking
        /// </summary>
        private void InitializeSystemMetrics()
        {
            var systemMetrics = new SystemMetricsTracker(_systemMetricsSampleInterval);
            
            // Register default metrics if enabled
            if (_trackDefaultMetrics)
            {
                if (_trackGC)
                {
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Memory, "GC.Alloc"), 
                        "GC.Alloc.Size", "KB");
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Memory, "GC.Count"), 
                        "GC.Alloc.Count", "count");
                }
                
                if (_trackFrameTime)
                {
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Render, "Main Thread"), 
                        "Main Thread", "ms");
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Render, "Frame Time"), 
                        "FrameTime", "ms");
                }
                
                if (_trackDrawCalls)
                {
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Render, "Draw Calls"), 
                        "Batches Count", "count");
                }
                
                if (_trackPhysics)
                {
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Physics, "Physics.Step"), 
                        "Physics.Step", "ms");
                }
            }
            
            // Start metrics tracking
            systemMetrics.Start();
            
            // Update profiler manager with new tracker
            _profilerManager.SystemMetrics = systemMetrics;
        }
        
        /// <summary>
        /// Set up threshold alerts
        /// </summary>
        private void SetupThresholdAlerts()
        {
            // Set up frame time alert
            if (_enableFrameTimeAlert)
            {
                var frameTimeTag = new ProfilerTag(ProfilerCategory.Render, "Frame Time");
                _profilerManager.RegisterMetricAlert(frameTimeTag, _frameTimeThresholdMs, null);
            }
            
            // Set up GC allocation alert
            if (_enableGCAllocationAlert)
            {
                var gcTag = new ProfilerTag(ProfilerCategory.Memory, "GC.Alloc");
                _profilerManager.RegisterMetricAlert(gcTag, _gcAllocationThresholdKB, null);
            }
        }
        
        /// <summary>
        /// Create the overlay UI
        /// </summary>
        private void CreateOverlayUI()
        {
            if (_overlayUIPrefab != null)
            {
                _overlayUIInstance = Instantiate(_overlayUIPrefab);
                _overlayUIInstance.name = "ProfilerOverlayUI";
                DontDestroyOnLoad(_overlayUIInstance);
                
                // Set initial state
                _overlayUIInstance.SetActive(_showUIOnStart);
            }
            else
            {
                Debug.LogWarning("Overlay UI prefab not assigned");
            }
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Handler for profiling started event
        /// </summary>
        private void OnProfilingStarted(object sender, EventArgs e)
        {
            Debug.Log("[ProfilerBootstrap] Profiling started");
        }
        
        /// <summary>
        /// Handler for profiling stopped event
        /// </summary>
        private void OnProfilingStopped(object sender, EventArgs e)
        {
            Debug.Log("[ProfilerBootstrap] Profiling stopped");
        }
        
        /// <summary>
        /// Handler for stats reset event
        /// </summary>
        private void OnStatsReset(object sender, EventArgs e)
        {
            Debug.Log("[ProfilerBootstrap] Profiling stats reset");
        }
        
        /// <summary>
        /// Handler for metric alert event
        /// </summary>
        private void OnMetricAlertTriggered(object sender, MetricEventArgs e)
        {
            // Handle specific metric alerts
            if (e.MetricTag.FullName.Contains("Frame Time"))
            {
                OnFrameTimeAlert(e);
            }
            else if (e.MetricTag.FullName.Contains("GC.Alloc"))
            {
                OnGCAllocationAlert(e);
            }
            
            // Log all alerts to the debug console
            Debug.LogWarning($"[ProfilerBootstrap] Metric alert: {e.MetricTag.FullName} = {e.Value:F2} {e.Unit}");
        }
        
        /// <summary>
        /// Handler for session alert event
        /// </summary>
        private void OnSessionAlertTriggered(object sender, ProfilerSessionEventArgs e)
        {
            Debug.LogWarning($"[ProfilerBootstrap] Session alert: {e.Tag.FullName} took {e.DurationMs:F2} ms");
        }
        
        /// <summary>
        /// Handler for session completed event
        /// </summary>
        private void OnSessionCompleted(object sender, ProfilerSessionEventArgs e)
        {
            // Auto-register session alerts for any slow sessions
            if (_enableCustomSessionAlerts && e.DurationMs > _defaultSessionThresholdMs)
            {
                // Only register if this is a particularly slow session
                // and we've never registered an alert for it before
                if (e.DurationMs > _defaultSessionThresholdMs * 2)
                {
                    _profilerManager.RegisterSessionAlert(e.Tag, _defaultSessionThresholdMs, null);
                    Debug.Log($"[ProfilerBootstrap] Auto-registered threshold alert for slow session: {e.Tag.FullName} > {_defaultSessionThresholdMs} ms");
                }
            }
        }
        
        /// <summary>
        /// Handle frame time alert
        /// </summary>
        private void OnFrameTimeAlert(MetricEventArgs args)
        {
            Debug.LogWarning($"[ProfilerBootstrap] Frame time spike: {args.Value:F2} ms");
            
            // Additional actions could be performed here
            // like notifying a performance management system
            // or triggering dynamic quality adjustments
        }
        
        /// <summary>
        /// Handle GC allocation alert
        /// </summary>
        private void OnGCAllocationAlert(MetricEventArgs args)
        {
            Debug.LogWarning($"[ProfilerBootstrap] GC allocation spike: {args.Value:F2} KB");
            
            // Additional actions could be performed here
            // like triggering memory cleanup or caching optimizations
        }
        
        #endregion
        
#if UNITY_EDITOR
        /// <summary>
        /// Editor menu item to create a profiler bootstrap
        /// </summary>
        [MenuItem("Tools/Runtime Profiler/Create Profiler")]
        public static void CreateProfilerBootstrap()
        {
            // Check if a profiler already exists
            var existingProfiler = FindObjectOfType<ProfilerBootstrap>();
            if (existingProfiler != null)
            {
                Debug.Log("Profiler Bootstrap already exists in scene");
                Selection.activeGameObject = existingProfiler.gameObject;
                return;
            }
            
            // Create new game object with component
            var go = new GameObject("ProfilerBootstrap");
            go.AddComponent<ProfilerBootstrap>();
            
            // Select the new game object
            Selection.activeGameObject = go;
            
            Debug.Log("Created Profiler Bootstrap in scene");
        }
#endif
    }
}