using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

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
        
        // Overlay UI instance
        private GameObject _overlayUIInstance;
        
        private void Start()
        {
            // Get or create the runtime profiler manager
            var profilerManager = RuntimeProfilerManager.Instance;
            profilerManager.LogToConsole = _logToConsole && Debug.isDebugBuild;
            
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
                profilerManager.StartProfiling();
            }
            else
            {
                profilerManager.StopProfiling();
            }
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
        /// Initialize system metrics tracking
        /// </summary>
        private void InitializeSystemMetrics()
        {
            var systemMetrics = RuntimeProfilerManager.Instance.SystemMetrics;
            systemMetrics = new SystemMetricsTracker(_systemMetricsSampleInterval);
            
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
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Rendering, "Main Thread"), 
                        "Main Thread", "ms");
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Rendering, "Frame Time"), 
                        "FrameTime", "ms");
                }
                
                if (_trackDrawCalls)
                {
                    systemMetrics.RegisterMetric(new ProfilerTag(ProfilerCategory.Rendering, "Draw Calls"), 
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
            RuntimeProfilerManager.Instance.SystemMetrics = systemMetrics;
        }
        
        /// <summary>
        /// Set up threshold alerts
        /// </summary>
        private void SetupThresholdAlerts()
        {
            var profilerManager = RuntimeProfilerManager.Instance;
            
            // Set up frame time alert
            if (_enableFrameTimeAlert)
            {
                var frameTimeTag = new ProfilerTag(ProfilerCategory.Rendering, "Frame Time");
                profilerManager.RegisterMetricAlert(frameTimeTag, _frameTimeThresholdMs, OnFrameTimeAlert);
            }
            
            // Set up GC allocation alert
            if (_enableGCAllocationAlert)
            {
                var gcTag = new ProfilerTag(ProfilerCategory.Memory, "GC.Alloc");
                profilerManager.RegisterMetricAlert(gcTag, _gcAllocationThresholdKB, OnGCAllocationAlert);
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
        
        /// <summary>
        /// Handle frame time alert
        /// </summary>
        private void OnFrameTimeAlert(MetricEventArgs args)
        {
            Debug.LogWarning($"[ProfilerAlert] Frame time spike: {args.Value:F2} ms");
        }
        
        /// <summary>
        /// Handle GC allocation alert
        /// </summary>
        private void OnGCAllocationAlert(MetricEventArgs args)
        {
            Debug.LogWarning($"[ProfilerAlert] GC allocation spike: {args.Value:F2} KB");
        }
        
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