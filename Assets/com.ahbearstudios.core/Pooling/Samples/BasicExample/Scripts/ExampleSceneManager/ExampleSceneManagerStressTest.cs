using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;
using System.Diagnostics;
using System;

/// <summary>
/// Partial class for stress testing in ExampleSceneManager
/// </summary>
public partial class ExampleSceneManager
{
    /// <summary>
    /// Job to generate random positions within a defined range
    /// </summary>
    public struct GeneratePositionsJob : IJob
    {
        /// <summary>
        /// Array to store the generated positions
        /// </summary>
        public NativeArray<float3> Positions;
        
        /// <summary>
        /// Minimum bounds for position generation
        /// </summary>
        public float3 MinBounds;
        
        /// <summary>
        /// Maximum bounds for position generation
        /// </summary>
        public float3 MaxBounds;
        
        /// <summary>
        /// Seed for random number generation
        /// </summary>
        public uint Seed;
        
        /// <summary>
        /// Executes the job to generate random positions
        /// </summary>
        public void Execute()
        {
            var random = new Unity.Mathematics.Random(Seed);
            
            for (int i = 0; i < Positions.Length; i++)
            {
                Positions[i] = random.NextFloat3(MinBounds, MaxBounds);
            }
        }
    }

    /// <summary>
    /// Toggles the stress test on or off
    /// </summary>
    public void ToggleStressTest()
    {
        if (stressTestActive)
        {
            // Stop the stress test
            if (stressTestCoroutine != null)
            {
                StopCoroutine(stressTestCoroutine);
            }
            
            stressTestActive = false;
            stressTestText.text = "Start Stress Test";
        }
        else
        {
            // Start the stress test
            stressTestCoroutine = StartCoroutine(RunStressTest());
            stressTestActive = true;
            stressTestText.text = "Stop Stress Test";
        }
    }
    
    /// <summary>
    /// Coroutine that runs the stress test
    /// </summary>
    /// <returns>IEnumerator for coroutine</returns>
    private IEnumerator RunStressTest()
    {
        // Safety check
        if (stressTestCount <= 0)
            stressTestCount = 1000;
            
        if (stressTestInterval <= 0)
            stressTestInterval = 0.01f;
            
        // Prepare a native array for the job
        if (generatedPositions.IsCreated)
            generatedPositions.Dispose();
            
        generatedPositions = new NativeArray<float3>(stressTestCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        // Prepare job data
        var job = new GeneratePositionsJob
        {
            Positions = generatedPositions,
            MinBounds = new float3(-10f, 0.5f, -10f),
            MaxBounds = new float3(10f, 10f, 10f),
            Seed = (uint)UnityEngine.Random.Range(1, 1000000)
        };
        
        // Schedule the job and mark as in progress
        positionJobHandle = job.Schedule();
        jobInProgress = true;
        
        // Wait for the job to complete before proceeding
        yield return new WaitUntil(() => !jobInProgress);
        
        // Start a stopwatch to measure performance
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        // Spawn objects in batches
        int spawned = 0;
        int batchSize = Mathf.Min(25, stressTestCount);
        
        while (spawned < stressTestCount && stressTestActive)
        {
            // Create a batch of spawn requests
            int requestsToCreate = Mathf.Min(batchSize, stressTestCount - spawned);
            
            // Ensure enough capacity in the spawn requests list
            if (spawnRequests.Capacity < requestsToCreate)
            {
                spawnRequests.Capacity = requestsToCreate * 2;
            }
            
            // Clear the list before adding new requests
            spawnRequests.Clear();
            
            // Add batch of spawn requests
            for (int i = 0; i < requestsToCreate; i++)
            {
                int index = spawned + i;
                if (index >= generatedPositions.Length)
                    break;
                    
                // Convert float3 to Vector3
                Vector3 position = new Vector3(
                    generatedPositions[index].x,
                    generatedPositions[index].y,
                    generatedPositions[index].z
                );
                
                Quaternion rotation = Quaternion.Euler(
                    UnityEngine.Random.Range(0f, 360f),
                    UnityEngine.Random.Range(0f, 360f),
                    UnityEngine.Random.Range(0f, 360f));
                    
                // Alternate between object types
                ObjectType type = index % 3 == 0 ? ObjectType.Particle : 
                                  index % 2 == 0 ? ObjectType.Cube : ObjectType.Sphere;
                                  
                SpawnRequest request = new SpawnRequest(type, position, rotation);
                spawnRequests.Add(request);
            }
            
            // Wait for specified interval
            yield return new WaitForSeconds(stressTestInterval);
            
            spawned += requestsToCreate;
        }
        
        stopwatch.Stop();
        
        // Clean up native array
        if (generatedPositions.IsCreated)
        {
            generatedPositions.Dispose();
        }
        
        // Log performance results
        float elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000f;
        float objectsPerSecond = stressTestCount / elapsedSeconds;
        
        UnityEngine.Debug.Log($"Stress test complete: Spawned {stressTestCount} objects in {elapsedSeconds:F2} seconds ({objectsPerSecond:F2} objects/sec)");
        
        // Reset state if test completed normally
        if (stressTestActive)
        {
            stressTestActive = false;
            stressTestText.text = "Start Stress Test";
        }
    }
    
    /// <summary>
    /// Updates memory usage display specially for stress test
    /// </summary>
    private void UpdateMemoryUsageForStressTest()
    {
        if (!stressTestActive)
            return;
            
        // Use profiler if available, otherwise use estimation
        if (useProfilerForMemory)
        {
            UpdateMemoryUsageProfiler();
        }
        else
        {
            UpdateMemoryUsageRuntime();
        }
        
        // Add stress test indicator to memory text
        memoryUsageText.text = "STRESS: " + memoryUsageText.text;
    }
}