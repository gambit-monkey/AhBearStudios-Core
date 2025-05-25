using System.Collections.Generic;
using System.Runtime.InteropServices;
using AhBearStudios.Pooling.Core.Pooling;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using AhBearStudios.Pooling.Core.Pooling.Native;
using AhBearStudios.Pooling.Core.Pooling.Unity;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

/// <summary>
/// Partial class for pool management in ExampleSceneManager
/// </summary>
public partial class ExampleSceneManager
{
    // Diagnostic service references
    private PoolDiagnostics _poolDiagnostics;
    private PoolProfiler _poolProfiler;
    private PoolLogger _logger;
    private PoolHealthChecker _healthChecker;
    
    // Native pool registry
    private NativePoolRegistry _poolRegistry;
    
    // Estimated size of float3 (3 floats * 4 bytes)
    private const int FLOAT3_SIZE_BYTES = 12;
    
    // Pool tracking flags
    private bool _hasBurstPositionsPool;
    private bool _hasJobPositionsPool;
    private bool _hasNativePositionsPool;
    
    /// <summary>
    /// Creates pools based on the current selected pool type
    /// </summary>
    private void CreatePools()
    {
        // Initialize pooling services if not already initialized
        if (!PoolingServices.HasService<PoolDiagnostics>())
        {
            PoolingServices.Initialize();
        }
        
        // Get service references
        _poolDiagnostics = PoolingServices.Diagnostics;
        _poolProfiler = PoolingServices.Profiler;
        _logger = PoolingServices.Logger;
        _healthChecker = PoolingServices.HealthChecker;
        
        // Set up pool health checker settings
        if (_healthChecker != null)
        {
            _healthChecker.SetCheckInterval(5f); // Check pools every 5 seconds
            _healthChecker.SetAlertFlags(true, true, true); // Enable all alerts
        }
        
        // Log pool initialization
        _logger?.LogInfoInstance("Initializing pools with selected type: " + currentPoolType);
        
        // Create specific pool types
        CreateStandardPools();
        CreateComponentPools();
        CreateNativePools();
    }
    
    /// <summary>
    /// Creates standard GameObject pools
    /// </summary>
    private void CreateStandardPools()
    {
        // Start profiling pool creation
        _poolProfiler?.BeginSample("CreateStandardPools", "ExampleManager");
        
        // Prepare pool configuration
        var poolConfig = new PoolConfig
        {
            InitialCapacity = initialPoolSize,
            MaxSize = maxPoolSize,
            PrewarmOnInit = true,
            ResetOnRelease = true,
            EnableAutoShrink = true,
            ShrinkThreshold = 0.3f
        };
        
        // Create GameObject pools
        cubePool = new GameObjectPool(cubePrefab, poolConfig);
        spherePool = new GameObjectPool(spherePrefab, poolConfig);
        
        // Register pools with diagnostics
        _poolDiagnostics?.RegisterPool(cubePool, "CubePool");
        _poolDiagnostics?.RegisterPool(spherePool, "SpherePool");
        
        // Create ParticleSystem pool with auto-release
        var particleConfig = new PoolConfig
        {
            InitialCapacity = initialPoolSize / 2, // Fewer particles initially
            MaxSize = maxPoolSize,
            PrewarmOnInit = true
        };
        
        particlePool = new ParticleSystemPool(
            particlePrefab, 
            initialPoolSize / 2, 
            null, 
            particleConfig, 
            true, 
            objectReturnDelay);
            
        // Register particle pool with diagnostics
        _poolDiagnostics?.RegisterPool(particlePool, "ParticlePool");
        
        _poolProfiler?.EndSample("CreateStandardPools", "ExampleManager", 3, 0);
    }
    
    /// <summary>
    /// Creates component-specific pools
    /// </summary>
    private void CreateComponentPools()
    {
        _poolProfiler?.BeginSample("CreateComponentPools", "ExampleManager");
        
        // Prepare pool configuration
        var componentPoolConfig = new PoolConfig
        {
            InitialCapacity = initialPoolSize,
            MaxSize = maxPoolSize,
            PrewarmOnInit = true
        };
        
        // Create component pools
        cubeRendererPool = new ComponentPool<Renderer>(cubePrefab, initialPoolSize, null, componentPoolConfig);
        sphereRigidbodyPool = new ComponentPool<Rigidbody>(spherePrefab, initialPoolSize, null, componentPoolConfig);
        
        // Register component pools with diagnostics
        _poolDiagnostics?.RegisterPool(cubeRendererPool, "CubeRendererPool");
        _poolDiagnostics?.RegisterPool(sphereRigidbodyPool, "SphereRigidbodyPool");
        
        _poolProfiler?.EndSample("CreateComponentPools", "ExampleManager", 2, 0);
    }
    
    /// <summary>
    /// Creates native pools for use with jobs
    /// </summary>
    private void CreateNativePools()
    {
        _poolProfiler?.BeginSample("CreateNativePools", "ExampleManager");
        
        // Get or create pool registry
        _poolRegistry = NativePoolRegistry.Instance;
        
        // Create different types of native pools
        nativePositionsPool = new NativePool<float3>(
            initialPoolSize, 
            Allocator.Persistent,
            float3.zero);
        _hasNativePositionsPool = true;
            
        burstPositionsPool = new BurstCompatibleNativePool<float3>(
            initialPoolSize, 
            Allocator.Persistent,
            float3.zero);
        _hasBurstPositionsPool = true;
            
        jobPositionsPool = new JobCompatibleNativePool<float3>(
            initialPoolSize, 
            Allocator.Persistent,
            float3.zero);
        _hasJobPositionsPool = true;
            
        // Register native pools with diagnostics - use estimated size instead of sizeof operator
        _poolDiagnostics?.RegisterPool(nativePositionsPool, "NativePositionsPool", FLOAT3_SIZE_BYTES);
        _poolDiagnostics?.RegisterPool(burstPositionsPool, "BurstPositionsPool", FLOAT3_SIZE_BYTES);
        _poolDiagnostics?.RegisterPool(jobPositionsPool, "JobPositionsPool", FLOAT3_SIZE_BYTES);
        
        _poolProfiler?.EndSample("CreateNativePools", "ExampleManager", 3, 0);
    }
    
    /// <summary>
    /// Handles pool type dropdown change
    /// </summary>
    /// <param name="index">Index of the selected pool type</param>
    public void OnPoolTypeChanged(int index)
    {
        currentPoolType = (PoolType)index;
        
        _logger?.LogInfoInstance($"Pool type changed to {currentPoolType}");
        
        // Use profiler to measure time taken to switch pool types
        _poolProfiler?.BeginSample("PoolTypeChange", currentPoolType.ToString());
        
        // Recreate pools based on the selected type
        switch (currentPoolType)
        {
            case PoolType.Standard:
                RecreateStandardPools();
                break;
            case PoolType.ThreadSafe:
                RecreateWithThreadSafePools();
                break;
            case PoolType.Native:
                RecreateWithNativePools();
                break;
            case PoolType.BurstCompatible:
                RecreateWithBurstPools();
                break;
            case PoolType.JobCompatible:
                RecreateWithJobPools();
                break;
        }
        
        _poolProfiler?.EndSample("PoolTypeChange", currentPoolType.ToString(), 0, 0);
        
        // Check for health issues
        _healthChecker?.CheckAllPools();
    }
    
    /// <summary>
    /// Recreates standard pools
    /// </summary>
    private void RecreateStandardPools()
    {
        _poolProfiler?.BeginSample("RecreateStandardPools", "ExampleManager");
        
        // Clean up existing pools
        DisposeCurrentPools();
        
        // Create new standard pools
        CreateStandardPools();
        
        _poolProfiler?.EndSample("RecreateStandardPools", "ExampleManager", 3, 0);
    }
    
    /// <summary>
    /// Recreates pools with thread-safe implementation
    /// </summary>
    private void RecreateWithThreadSafePools()
    {
        _poolProfiler?.BeginSample("RecreateThreadSafePools", "ExampleManager");
        
        // Clean up existing pools
        DisposeCurrentPools();
        
        // Create new thread-safe pools
        var threadSafeConfig = new PoolConfig
        {
            InitialCapacity = initialPoolSize,
            MaxSize = maxPoolSize,
            PrewarmOnInit = true,
            ResetOnRelease = true,
            ThreadingMode = PoolThreadingMode.ThreadLocal // Enable thread safety
        };
        
        // For this example, we'll use standard pools with thread safety enabled
        cubePool = new GameObjectPool(cubePrefab, threadSafeConfig);
        spherePool = new GameObjectPool(spherePrefab, threadSafeConfig);
        
        // Register pools with diagnostics
        _poolDiagnostics?.RegisterPool(cubePool, "ThreadSafeCubePool");
        _poolDiagnostics?.RegisterPool(spherePool, "ThreadSafeSpherePool");
        
        var particleConfig = new PoolConfig
        {
            InitialCapacity = initialPoolSize / 2,
            MaxSize = maxPoolSize,
            PrewarmOnInit = true,
            ThreadingMode = PoolThreadingMode.ThreadLocal
        };
        
        particlePool = new ParticleSystemPool(
            particlePrefab, 
            initialPoolSize / 2, 
            null, 
            particleConfig, 
            true, 
            objectReturnDelay);
            
        // Register particle pool with diagnostics
        _poolDiagnostics?.RegisterPool(particlePool, "ThreadSafeParticlePool");
        
        _poolProfiler?.EndSample("RecreateThreadSafePools", "ExampleManager", 3, 0);
    }
    
    /// <summary>
    /// Recreates pools with native implementation
    /// </summary>
    private void RecreateWithNativePools()
    {
        _poolProfiler?.BeginSample("RecreateNativePools", "ExampleManager");
        
        // Clean up existing native pools
        DisposeNativePools();
        
        // Create new native pools with larger capacities for performance testing
        nativePositionsPool = new NativePool<float3>(
            initialPoolSize * 2, 
            Allocator.Persistent,
            float3.zero);
        _hasNativePositionsPool = true;
            
        // Register with diagnostics - use estimated size
        _poolDiagnostics?.RegisterPool(nativePositionsPool, "EnhancedNativePositionsPool", FLOAT3_SIZE_BYTES);
        
        // Standard pools will still be used for GameObjects
        RecreateStandardPools();
        
        _poolProfiler?.EndSample("RecreateNativePools", "ExampleManager", 4, 0);
    }
    
    /// <summary>
    /// Recreates pools with Burst-compatible implementation
    /// </summary>
    private void RecreateWithBurstPools()
    {
        _poolProfiler?.BeginSample("RecreateBurstPools", "ExampleManager");
        
        // Clean up existing burst pools
        DisposeNativePools();
    
        // Create new burst-compatible pools with enhanced capacity
        burstPositionsPool = new BurstCompatibleNativePool<float3>(
            initialPoolSize * 2, 
            Allocator.Persistent,
            float3.zero);
        _hasBurstPositionsPool = true;
            
        // Register with diagnostics - use estimated size
        _poolDiagnostics?.RegisterPool(burstPositionsPool, "EnhancedBurstPositionsPool", FLOAT3_SIZE_BYTES);
        
        // Standard pools will still be used for GameObjects
        RecreateStandardPools();
        
        _poolProfiler?.EndSample("RecreateBurstPools", "ExampleManager", 4, 0);
    }
    
    /// <summary>
    /// Recreates pools with Job-compatible implementation
    /// </summary>
    private void RecreateWithJobPools()
    {
        _poolProfiler?.BeginSample("RecreateJobPools", "ExampleManager");
        
        // Clean up existing job pools
        DisposeNativePools();
    
        // Create new job-compatible pools with enhanced capacity
        jobPositionsPool = new JobCompatibleNativePool<float3>(
            initialPoolSize * 2, 
            Allocator.Persistent,
            float3.zero);
        _hasJobPositionsPool = true;
            
        // Register the pool with the registry to enable job access
        if (_hasJobPositionsPool)
        {
            var handle = _poolRegistry.Register(jobPositionsPool);
            _logger?.LogInfoInstance($"Registered job pool with ID: {handle.PoolId}");
        }
        
        // Register with diagnostics - use estimated size
        _poolDiagnostics?.RegisterPool(jobPositionsPool, "EnhancedJobPositionsPool", FLOAT3_SIZE_BYTES);
        
        // Standard pools will still be used for GameObjects
        RecreateStandardPools();
        
        _poolProfiler?.EndSample("RecreateJobPools", "ExampleManager", 4, 0);
    }
    
    /// <summary>
    /// Disposes all current pools
    /// </summary>
    private void DisposeCurrentPools()
    {
        _poolProfiler?.BeginSample("DisposePools", "ExampleManager");
        
        // Unregister and dispose standard pools
        if (cubePool != null)
        {
            _poolDiagnostics?.UnregisterPool(cubePool);
            cubePool.Dispose();
            cubePool = null;
        }
        
        if (spherePool != null)
        {
            _poolDiagnostics?.UnregisterPool(spherePool);
            spherePool.Dispose();
            spherePool = null;
        }
        
        if (particlePool != null)
        {
            _poolDiagnostics?.UnregisterPool(particlePool);
            particlePool.Dispose();
            particlePool = null;
        }
        
        // Unregister and dispose component pools
        if (cubeRendererPool != null)
        {
            _poolDiagnostics?.UnregisterPool(cubeRendererPool);
            cubeRendererPool.Dispose();
            cubeRendererPool = null;
        }
        
        if (sphereRigidbodyPool != null)
        {
            _poolDiagnostics?.UnregisterPool(sphereRigidbodyPool);
            sphereRigidbodyPool.Dispose();
            sphereRigidbodyPool = null;
        }
        
        // Dispose native pools
        DisposeNativePools();
        
        _poolProfiler?.EndSample("DisposePools", "ExampleManager", 0, 0);
    }
    
    /// <summary>
    /// Disposes native pools
    /// </summary>
    private void DisposeNativePools()
    {
        _poolProfiler?.BeginSample("DisposeNativePools", "ExampleManager");
        
        // Unregister and dispose native pools if they exist
        if (_hasNativePositionsPool && !nativePositionsPool.IsDisposed)
        {
            _poolDiagnostics?.UnregisterPool(nativePositionsPool);
            nativePositionsPool.Dispose();
            _hasNativePositionsPool = false;
        }
        
        if (_hasBurstPositionsPool && !burstPositionsPool.IsDisposed)
        {
            _poolDiagnostics?.UnregisterPool(burstPositionsPool);
            burstPositionsPool.Dispose();
            _hasBurstPositionsPool = false;
        }
        
        if (_hasJobPositionsPool && !jobPositionsPool.IsDisposed)
        {
            _poolDiagnostics?.UnregisterPool(jobPositionsPool);
            jobPositionsPool.Dispose();
            _hasJobPositionsPool = false;
        }
        
        _poolProfiler?.EndSample("DisposeNativePools", "ExampleManager", 0, 0);
    }
    
    /// <summary>
    /// Cleans up resources when the scene is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Complete any in-progress jobs first
        if (jobInProgress && generatedPositions.IsCreated)
        {
            positionJobHandle.Complete();
            jobInProgress = false;
        }

        // Clean up job data
        if (generatedPositions.IsCreated)
        {
            generatedPositions.Dispose();
        }

        // Clean up spawn requests
        if (spawnRequests.IsCreated)
        {
            spawnRequests.Dispose();
        }

        // Clean up GameObject pools
        cubePool?.Dispose();
        spherePool?.Dispose();
        particlePool?.Dispose();

        // Clean up component pools
        cubeRendererPool?.Dispose();
        sphereRigidbodyPool?.Dispose();

        // Clean up native pools
        if (nativePositionsPool != null && !nativePositionsPool.IsDisposed)
        {
            nativePositionsPool.Dispose();
        }

        // For struct types like BurstCompatibleNativePool and JobCompatibleNativePool
        // we need to directly check IsDisposed property without null checking
        if (burstPositionsPool.IsCreated && !burstPositionsPool.IsDisposed)
        {
            burstPositionsPool.Dispose();
        }

        if (jobPositionsPool.IsCreated && !jobPositionsPool.IsDisposed)
        {
            jobPositionsPool.Dispose();
        }
        
        // Ensure all pools are properly disposed
        DisposeCurrentPools();
        
        // Log final metrics before shutdown
        if (_poolDiagnostics != null)
        {
            var allMetrics = _poolDiagnostics.GetAllMetrics();
            _logger?.LogInfoInstance($"Final pool metrics: {allMetrics.Count} pools tracked");
            
            // Log any outstanding health issues
            if (_healthChecker != null)
            {
                var issues = _healthChecker.GetIssues();
                if (issues.Count > 0)
                {
                    _logger?.LogWarningInstance($"Outstanding pool health issues: {issues.Count}");
                }
            }
        }
    }
    
    /// <summary>
    /// Gets diagnostic metrics for all pools
    /// </summary>
    /// <returns>List of pool metrics</returns>
    public List<Dictionary<string, object>> GetPoolMetrics()
    {
        if (_poolDiagnostics != null)
        {
            return _poolDiagnostics.GetAllMetrics();
        }
        
        return new List<Dictionary<string, object>>();
    }
    
    /// <summary>
    /// Gets health issues for all pools
    /// </summary>
    /// <returns>List of pool health issues</returns>
    public List<PoolHealthIssue> GetPoolHealthIssues()
    {
        if (_healthChecker != null)
        {
            return _healthChecker.GetIssues();
        }
        
        return new List<PoolHealthIssue>();
    }
}