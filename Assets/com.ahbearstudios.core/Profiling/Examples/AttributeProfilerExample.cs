using System;
using AhBearStudios.Core.Profiling.Attributes;
using AhBearStudios.Core.Profiling.Factories;
using AhBearStudios.Core.Profiling.Unity;
using UnityEngine;

namespace AhBearStudios.Core.Profiling.Examples
{
    /// <summary>
    /// Example class showing how to use attribute-based profiling
    /// </summary>
    [ProfileClass(ProfilerCategory.Gameplay, "AI", includeInherited: true, includePrivate: true)]
    public class AttributeProfilerExample : MonoBehaviour
    {
        [SerializeField] private bool _enableAttributeProfiling = true;
        [SerializeField] private float _updateInterval = 1.0f;
        [SerializeField] private int _heavyLoadIterations = 1000;
        
        private float _timeSinceLastAction;
        
        private void Start()
        {
            // Register this instance with the attribute profiler if enabled
            if (_enableAttributeProfiling)
            {
                var profiler = FindObjectOfType<AttributeProfilerBehaviour>();
                if (profiler != null)
                {
                    profiler.ProfileInstance(this);
                }
            }
            
            // Call profiled methods directly
            Initialize();
            CalculateInitialState(100);
        }
        
        private void Update()
        {
            _timeSinceLastAction += Time.deltaTime;
            
            if (_timeSinceLastAction >= _updateInterval)
            {
                // These methods will be profiled based on the class attribute
                PerformComplexCalculation();
                ProcessData(UnityEngine.Random.Range(0, 100));
                
                // Reset timer
                _timeSinceLastAction = 0f;
            }
        }
        
        // Will be profiled as "AI.Initialize"
        private void Initialize()
        {
            Debug.Log("Initializing AI system");
            System.Threading.Thread.Sleep(5);
        }
        
        // Will be profiled as "AI.CalculateInitialState"
        private void CalculateInitialState(int complexity)
        {
            Debug.Log($"Calculating initial state with complexity {complexity}");
            
            // Simulate some work
            double result = 0;
            for (int i = 0; i < complexity * 100; i++)
            {
                result += Math.Sin(i * 0.01);
            }
        }
        
        // Will be profiled as "AI.PerformComplexCalculation" 
        private void PerformComplexCalculation()
        {
            Debug.Log("Performing complex calculation");
            
            // Simulate some complex work
            double result = 0;
            for (int i = 0; i < _heavyLoadIterations; i++)
            {
                result += Math.Cos(i * 0.01) * Math.Sin(i * 0.01);
            }
        }
        
        // Will be profiled as "AI.ProcessData"
        private void ProcessData(int amount)
        {
            Debug.Log($"Processing {amount} data items");
            
            // Simulate some work
            System.Threading.Thread.Sleep(amount / 20);
        }
        
        // This method will NOT be profiled due to the attribute
        [DoNotProfile]
        private void InternalCalculation()
        {
            // This method won't be profiled
            System.Threading.Thread.Sleep(5);
        }
        
        // This method has its own custom profile attribute
        [ProfileMethod(ProfilerCategory.AI, "CustomProcessing")]
        private int ProcessWithCustomTag(string data)
        {
            Debug.Log($"Processing data: {data}");
            
            // Simulate some work
            System.Threading.Thread.Sleep(10);
            return data.Length;
        }
        
        // Example of how to manually invoke a profiled method
        public void DemonstrateManualInvocation()
        {
            // Get the method by reflection
            var method = GetType().GetMethod("ProcessWithCustomTag", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            // Invoke with profiling
            var result = ProfileInvoker.InvokeFunction<string, int>(this, method, "Test data");
            
            Debug.Log($"Manual invocation result: {result}");
            
            // Or use the method name directly
            ProfileInvoker.InvokeMethod(this, "PerformComplexCalculation");
        }
    }
    
    /// <summary>
    /// Examples of more granular method profiling
    /// </summary>
    public class MethodProfilerExample : MonoBehaviour
    {
        [SerializeField] private int _iterations = 1000;
        
        // Profile just this specific method
        [ProfileMethod(ProfilerCategory.Gameplay, "PlayerUpdate")]
        private void UpdatePlayerState()
        {
            // Method body
            System.Threading.Thread.Sleep(5);
        }
        
        // Profile with custom name
        [ProfileMethod(ProfilerCategory.Physics, "CollisionCheck")]
        private bool CheckCollisions(Collider[] colliders)
        {
            // Method body
            System.Threading.Thread.Sleep(colliders.Length / 10);
            return colliders.Length > 0;
        }
        
        // Profile with full control over the name
        [ProfileMethod(ProfilerCategory.Custom, "HighPrecisionCalculation")]
        private double CalculateTrajectory(Vector3 start, Vector3 end, float speed)
        {
            // Method body
            var distance = Vector3.Distance(start, end);
            double result = 0;
            
            for (int i = 0; i < _iterations; i++)
            {
                result += Math.Sin(distance * i * 0.01) * Math.Cos(speed * i * 0.01);
            }
            
            return result;
        }
        
        private void Start()
        {
            // Register with attribute profiler
            var profiler = FindObjectOfType<AttributeProfilerBehaviour>();
            if (profiler != null)
            {
                profiler.ScanForProfiledMembers();
            }
            
            // Call profiled methods
            UpdatePlayerState();
            
            var colliders = new Collider[10];
            CheckCollisions(colliders);
            
            CalculateTrajectory(Vector3.zero, Vector3.one * 10f, 5f);
        }
    }
}