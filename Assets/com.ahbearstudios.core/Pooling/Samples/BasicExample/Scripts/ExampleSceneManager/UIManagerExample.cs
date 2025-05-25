using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Partial class for UI handling in the ExampleSceneManager
/// </summary>
public partial class ExampleSceneManager
{
    /// <summary>
    /// Initializes UI elements and binds events
    /// </summary>
    private void InitializeUI()
    {
        // Initialize dropdown for pool types
        poolTypeDropdown.ClearOptions();
        var options = new List<string>
        {
            "Standard GameObject Pool",
            "Thread-Safe Pool",
            "Native Pool",
            "Burst-Compatible Native Pool",
            "Job-Compatible Native Pool"
        };
        poolTypeDropdown.AddOptions(options);
        poolTypeDropdown.value = 0;
        poolTypeDropdown.onValueChanged.AddListener(OnPoolTypeChanged);
        
        // Set up button listeners
        spawnCubeButton.onClick.AddListener(SpawnCube);
        spawnSphereButton.onClick.AddListener(SpawnSphere);
        spawnParticleButton.onClick.AddListener(SpawnParticleEffect);
        spawnMultipleButton.onClick.AddListener(SpawnMultipleObjects);
        clearButton.onClick.AddListener(ClearAllObjects);
        stressTestButton.onClick.AddListener(ToggleStressTest);
        
        // Set default spawn count
        spawnCountInput.text = "1";
        
        // Set stress test button text
        stressTestText = stressTestButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (stressTestText != null)
        {
            stressTestText.text = "Start Stress Test";
        }
    }
    
    /// <summary>
    /// Updates FPS calculation
    /// </summary>
    private void UpdateFPS()
    {
        frameCounter++;
        timeCounter += Time.unscaledDeltaTime;
        
        if (timeCounter >= refreshTime)
        {
            fps = frameCounter / timeCounter;
            frameCounter = 0;
            timeCounter = 0.0f;
        }
        
        fpsText.text = $"FPS: {fps:F1}";
    }
    
    /// <summary>
    /// Updates memory usage display using Unity Profiler
    /// </summary>
    private void UpdateMemoryUsageProfiler()
    {
        #if UNITY_EDITOR
        long totalAllocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        long bytesChange = totalAllocatedBytes - lastTotalAllocatedBytes;
        lastTotalAllocatedBytes = totalAllocatedBytes;
        
        float mbUsed = totalAllocatedBytes / (1024f * 1024f);
        memoryUsageText.text = $"Memory: {mbUsed:F2} MB (Δ: {bytesChange / 1024:F0} KB)";
        #endif
    }
    
    /// <summary>
    /// Updates memory usage display at runtime
    /// </summary>
    private void UpdateMemoryUsageRuntime()
    {
        // Estimate memory usage based on active objects
        long estimatedBytes = 0;
    
        // Add memory for cube pool
        if (cubePool != null)
        {
            estimatedBytes += cubePool.ActiveCount * GetAverageObjectMemory(cubePrefab);
        }
    
        // Add memory for sphere pool
        if (spherePool != null)
        {
            estimatedBytes += spherePool.ActiveCount * GetAverageObjectMemory(spherePrefab);
        }
    
        // Add memory for particle pool
        if (particlePool != null)
        {
            estimatedBytes += particlePool.ActiveCount * GetAverageObjectMemory(particlePrefab);
        }
    
        // Add memory for native pools
        if (nativePositionsPool != null && !nativePositionsPool.IsDisposed)
        {
            estimatedBytes += nativePositionsPool.ActiveCount * Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<float3>();
        }
    
        // For structs, we need to use try-catch to avoid NullReferenceException
        try
        {
            if (!burstPositionsPool.IsDisposed)
            {
                estimatedBytes += burstPositionsPool.ActiveCount * Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<float3>();
            }
        }
        catch (System.NullReferenceException)
        {
            // Pool wasn't initialized
        }
    
        try
        {
            if (!jobPositionsPool.IsDisposed)
            {
                estimatedBytes += jobPositionsPool.ActiveCount * Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<float3>();
            }
        }
        catch (System.NullReferenceException)
        {
            // Pool wasn't initialized
        }
    
        float mbUsed = estimatedBytes / (1024f * 1024f);
        memoryUsageText.text = $"Est. Memory: {mbUsed:F2} MB";
    }
    
    /// <summary>
    /// Estimates memory usage for a GameObject prefab
    /// </summary>
    /// <param name="prefab">Prefab to estimate memory for</param>
    /// <returns>Estimated memory in bytes</returns>
    private long GetAverageObjectMemory(GameObject prefab)
    {
        if (prefab == null) return 0;
        
        // Rough estimate - in a real scenario, this would be more precise
        long baseMemory = 1024; // 1KB base
        
        // Add for each component (rough estimate)
        baseMemory += prefab.GetComponents<Component>().Length * 256;
        
        // Add for mesh if present
        MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            baseMemory += (meshFilter.sharedMesh.vertexCount * 32); // Approx 32 bytes per vertex
        }
        
        // Add for materials
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            baseMemory += renderer.sharedMaterials.Length * 1024; // 1KB per material (rough estimate)
        }
        
        // Add for particle system if present
        ParticleSystem particleSystem = prefab.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            baseMemory += 2048; // 2KB base for particle system
            baseMemory += (int)(particleSystem.main.maxParticles * 64); // Approx 64 bytes per particle
        }
        
        return baseMemory;
    }
    
    /// <summary>
    /// Updates pool statistics display using Unity Profiler
    /// </summary>
    private void UpdateStatsDisplayProfiler()
    {
        UpdateStatsDisplayCommon();
    }
    
    /// <summary>
    /// Updates pool statistics display at runtime
    /// </summary>
    private void UpdateStatsDisplayRuntime()
    {
        UpdateStatsDisplayCommon();
    }
    
    /// <summary>
    /// Common logic for updating stats display
    /// </summary>
    /// <summary>
    /// Common logic for updating stats display
    /// </summary>
    private void UpdateStatsDisplayCommon()
    {
        int totalActive = 0;
        int totalCreated = 0;

        // Count active objects from all pools
        if (cubePool != null)
        {
            totalActive += cubePool.ActiveCount;
            totalCreated += cubePool.TotalCreated;
        }

        if (spherePool != null)
        {
            totalActive += spherePool.ActiveCount;
            totalCreated += spherePool.TotalCreated;
        }

        if (particlePool != null)
        {
            totalActive += particlePool.ActiveCount;
            totalCreated += particlePool.TotalCreated;
        }

        if (nativePositionsPool != null && !nativePositionsPool.IsDisposed)
        {
            totalActive += nativePositionsPool.ActiveCount;
            totalCreated += nativePositionsPool.TotalCreated;
        }

        // For structs, we need to use try-catch to avoid NullReferenceException
        try
        {
            if (!burstPositionsPool.IsDisposed)
            {
                totalActive += burstPositionsPool.ActiveCount;
                totalCreated += burstPositionsPool.TotalCreated;
            }
        }
        catch (System.NullReferenceException)
        {
            // Pool wasn't initialized
        }

        try
        {
            if (!jobPositionsPool.IsDisposed)
            {
                totalActive += jobPositionsPool.ActiveCount;
                totalCreated += jobPositionsPool.TotalCreated;
            }
        }
        catch (System.NullReferenceException)
        {
            // Pool wasn't initialized
        }

        // Update UI
        activeObjectsText.text = $"Active Objects: {totalActive}";
        totalCreatedText.text = $"Total Created: {totalCreated}";
    }
}