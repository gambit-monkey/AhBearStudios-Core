using System;
using AhBearStudios.Core.Profiling.Events;
using UnityEngine;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Examples
{
    /// <summary>
    /// Example usage of the runtime profiler system
    /// </summary>
    public class ProfilerUsageExample : MonoBehaviour
    {
        [Header("System Metrics")]
        [SerializeField] private bool _enableSystemMetrics = true;
        
        [Header("Profiler Tag")]
        [SerializeField] private bool _profileUpdate = true;
        [SerializeField] private bool _profileFixedUpdate = true;
        [SerializeField] private bool _profileLateUpdate = true;
        
        [Header("Custom Metrics")]
        [SerializeField] private bool _trackObjectCount = true;
        [SerializeField] private bool _trackCustomMetric = true;
        
        [Header("Threshold Alerts")]
        [SerializeField] private bool _enableFrameTimeAlert = true;
        [SerializeField] private float _frameTimeThresholdMs = 33.3f; // 30 FPS
        
        [Header("Test Load")]
        [SerializeField] private bool _simulateHeavyLoad = false;
        [SerializeField] private int _heavyLoadIterations = 10000;
        
        // Reference to profiler manager
        private RuntimeProfilerManager _profilerManager;
        
        // Profile tag for update
        private readonly ProfilerTag _updateTag = new ProfilerTag(ProfilerCategory.Internal, "Update");
        private readonly ProfilerTag _fixedUpdateTag = new ProfilerTag(ProfilerCategory.Physics, "FixedUpdate");
        private readonly ProfilerTag _lateUpdateTag = new ProfilerTag(ProfilerCategory.Render, "LateUpdate");
        private readonly ProfilerTag _simulateTag = new ProfilerTag(ProfilerCategory.Internal, "HeavyLoad");
        
        // Direct profiler marker for critical section
        private static readonly ProfilerMarker _criticalSectionMarker = new ProfilerMarker("CriticalSection");
        
        private void Start()
        {
            // Get profiler manager
            _profilerManager = RuntimeProfilerManager.Instance;
            
            // Register for profiler events
            _profilerManager.SessionCompleted += OnProfilerSessionCompleted;
            
            // Start system metrics tracking if enabled
            if (_enableSystemMetrics)
            {
                SystemMetricsTracker.StartDefault();
                
                // Register custom metrics
                if (_trackObjectCount)
                {
                    SystemMetricsTracker.RegisterCustomMetric("Object Count", ProfilerCategory.Memory, "Object Count", "count");
                }
                
                if (_trackCustomMetric)
                {
                    SystemMetricsTracker.RegisterCustomMetric("Custom Metric", ProfilerCategory.Internal, "CustomStat", "units");
                }
            }
            
            // Set up threshold alerts
            if (_enableFrameTimeAlert)
            {
                _profilerManager.RegisterMetricAlert(
                    new ProfilerTag(ProfilerCategory.Render, "Frame Time"), 
                    _frameTimeThresholdMs,
                    OnFrameTimeAlert);
            }
            
            // Register threshold alert for our custom update method
            _profilerManager.RegisterSessionAlert(_updateTag, 5.0, OnSlowUpdateAlert);
            
            Debug.Log("Profiler initialized. Use RuntimeProfilerManager.Instance to access profiler functionality.");
        }
        
        private void OnDestroy()
        {
            // Unregister from profiler events
            if (_profilerManager != null)
            {
                _profilerManager.SessionCompleted -= OnProfilerSessionCompleted;
            }
        }
        
        private void Update()
        {
            // Profile the update method if enabled
            if (_profileUpdate)
            {
                using (var session = _profilerManager.BeginScope(_updateTag))
                {
                    // Normal update logic here
                    NormalUpdateLogic();
                    
                    // Simulate heavy load if enabled
                    if (_simulateHeavyLoad)
                    {
                        SimulateHeavyLoad();
                    }
                }
            }
            else
            {
                // Normal update logic without profiling
                NormalUpdateLogic();
                
                // Simulate heavy load if enabled
                if (_simulateHeavyLoad)
                {
                    SimulateHeavyLoad();
                }
            }
        }
        
        private void FixedUpdate()
        {
            // Profile the fixed update method if enabled
            if (_profileFixedUpdate)
            {
                using (_profilerManager.BeginScope(_fixedUpdateTag))
                {
                    // Fixed update logic here
                    FixedUpdateLogic();
                }
            }
            else
            {
                // Fixed update logic without profiling
                FixedUpdateLogic();
            }
        }
        
        private void LateUpdate()
        {
            // Profile the late update method if enabled
            if (_profileLateUpdate)
            {
                using (_profilerManager.BeginScope(_lateUpdateTag))
                {
                    // Late update logic here
                    LateUpdateLogic();
                }
            }
            else
            {
                // Late update logic without profiling
                LateUpdateLogic();
            }
        }
        
        /// <summary>
        /// Example of normal update logic
        /// </summary>
        private void NormalUpdateLogic()
        {
            // Simulate some normal update work
            System.Threading.Thread.Sleep(1);
            
            // Critical section using direct marker for higher performance
            // Use this approach for high-frequency or performance-critical code
            _criticalSectionMarker.Begin();
            try
            {
                // Critical section work
                System.Threading.Thread.Sleep(1);
            }
            finally
            {
                _criticalSectionMarker.End();
            }
        }
        
        /// <summary>
        /// Example of fixed update logic
        /// </summary>
        private void FixedUpdateLogic()
        {
            // Simulate some fixed update work
            System.Threading.Thread.Sleep(1);
        }
        
        /// <summary>
        /// Example of late update logic
        /// </summary>
        private void LateUpdateLogic()
        {
            // Simulate some late update work
            System.Threading.Thread.Sleep(1);
        }
        
        /// <summary>
        /// Simulate a heavy load for testing
        /// </summary>
        private void SimulateHeavyLoad()
        {
            // Profile this section with custom tag
            using (_profilerManager.BeginScope(_simulateTag))
            {
                // Perform some expensive operations
                double result = 0;
                for (int i = 0; i < _heavyLoadIterations; i++)
                {
                    result += Math.Sin(i * 0.01) * Math.Cos(i * 0.01);
                }
            }
        }
        
        /// <summary>
        /// Handle profiler session completed event
        /// </summary>
        private void OnProfilerSessionCompleted(object sender, ProfilerSessionEventArgs e)
        {
            // Example of how to handle profiler events
            // You can use this to log or display profiling data in your own UI
            
            // Check for specific tag we're interested in
            if (e.Tag == _simulateTag && e.DurationMs > 100)
            {
                Debug.LogWarning($"Heavy load simulation took {e.DurationMs:F2} ms");
            }
        }
        
        /// <summary>
        /// Handle frame time alert
        /// </summary>
        private void OnFrameTimeAlert(MetricEventArgs args)
        {
            Debug.LogWarning($"Frame time alert: {args.Value:F2} ms (> {_frameTimeThresholdMs} ms)");
        }
        
        /// <summary>
        /// Handle slow update alert
        /// </summary>
        private void OnSlowUpdateAlert(ProfilerSessionEventArgs args)
        {
            Debug.LogWarning($"Slow update alert: {args.DurationMs:F2} ms (> 5.0 ms)");
        }
        
        /// <summary>
        /// Example of how to use extension methods for profiling
        /// </summary>
        private void ExampleExtensionMethods()
        {
            // Profile a simple action
            Action someAction = () => {
                // Do something...
                System.Threading.Thread.Sleep(5);
            };
            
            // Profile using extension method
            someAction.Profile(new ProfilerTag(ProfilerCategory.Internal, "SomeAction"));
            
            // Profile a function with return value
            Func<int> someFunc = () => {
                // Calculate something...
                System.Threading.Thread.Sleep(5);
                return 42;
            };
            
            // Profile using extension method and get result
            int result = someFunc.Profile(new ProfilerTag(ProfilerCategory.Internal, "SomeFunc"));
        }
    }
}