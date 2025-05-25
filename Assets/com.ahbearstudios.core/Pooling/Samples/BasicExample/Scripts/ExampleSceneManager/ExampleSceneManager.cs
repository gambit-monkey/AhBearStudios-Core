using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Unity;
using AhBearStudios.Pooling.Core.Pooling.Managed;
using AhBearStudios.Pooling.Core.Pooling.Native;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;

/// <summary>
/// Manager for the example scene that demonstrates various pool implementations
/// </summary>
public partial class ExampleSceneManager : MonoBehaviour
{
    [Header("Prefabs")] [Tooltip("Prefab used for spawning cubes")] [SerializeField]
    private GameObject cubePrefab;

    [Tooltip("Prefab used for spawning spheres")] [SerializeField]
    private GameObject spherePrefab;

    [Tooltip("Prefab used for spawning particle effects")] [SerializeField]
    private GameObject particlePrefab;

    [Header("UI References")] [Tooltip("Text element that displays the current FPS")] [SerializeField]
    private TextMeshProUGUI fpsText;

    [Tooltip("Text element that displays memory usage information")] [SerializeField]
    private TextMeshProUGUI memoryUsageText;

    [Tooltip("Text element that displays the count of active objects")] [SerializeField]
    private TextMeshProUGUI activeObjectsText;

    [Tooltip("Text element that displays the total number of created objects")] [SerializeField]
    private TextMeshProUGUI totalCreatedText;

    [Tooltip("Dropdown to select different pool implementation types")] [SerializeField]
    private TMP_Dropdown poolTypeDropdown;

    [Tooltip("Input field to specify how many objects to spawn")] [SerializeField]
    private TMP_InputField spawnCountInput;

    [Tooltip("Button to spawn a single cube")] [SerializeField]
    private Button spawnCubeButton;

    [Tooltip("Button to spawn a single sphere")] [SerializeField]
    private Button spawnSphereButton;

    [Tooltip("Button to spawn a particle effect")] [SerializeField]
    private Button spawnParticleButton;

    [Tooltip("Button to spawn multiple objects at once")] [SerializeField]
    private Button spawnMultipleButton;

    [Tooltip("Button to clear all spawned objects")] [SerializeField]
    private Button clearButton;

    [Tooltip("Button to toggle the stress test")] [SerializeField]
    private Button stressTestButton;

    [Tooltip("Text element that displays on the stress test button")] [SerializeField]
    private TextMeshProUGUI stressTestText;

    [Header("Settings")] [Tooltip("Initial size of the object pools")] [SerializeField]
    private int initialPoolSize = 50;

    [Tooltip("Maximum size the pools can grow to")] [SerializeField]
    private int maxPoolSize = 1000;

    [Tooltip("Rate at which objects are spawned per second")] [SerializeField]
    private float spawnRate = 10f;

    [Tooltip("Delay in seconds before objects are automatically returned to the pool")] [SerializeField]
    private float objectReturnDelay = 5f;

    [Tooltip("Whether to use Unity Profiler for memory usage tracking (Editor only)")] [SerializeField]
    private bool useProfilerForMemory = false;

    [Tooltip("Number of objects to spawn during stress test")] [SerializeField]
    private int stressTestCount = 1000;

    [Tooltip("Time interval between spawns during stress test")] [SerializeField]
    private float stressTestInterval = 0.01f;

    // GameObject pool references
    private GameObjectPool cubePool;
    private GameObjectPool spherePool;
    private ParticleSystemPool particlePool;

    // Component pool references
    private ComponentPool<Renderer> cubeRendererPool;
    private ComponentPool<Rigidbody> sphereRigidbodyPool;

    // Native pool references
    private NativePool<float3> nativePositionsPool;
    private BurstCompatibleNativePool<float3> burstPositionsPool;
    private JobCompatibleNativePool<float3> jobPositionsPool;

    // Queue for batch spawning
    private UnsafeList<SpawnRequest> spawnRequests;

    // Current selected pool type
    private PoolType currentPoolType = PoolType.Standard;

    // Stats tracking
    private float deltaTime = 0.0f;
    private int frameCounter = 0;
    private float timeCounter = 0.0f;
    private float refreshTime = 0.5f;
    private float fps = 0.0f;
    private long lastTotalAllocatedBytes = 0;

    // Stress test
    private bool stressTestActive = false;
    private Coroutine stressTestCoroutine;

    // Job-related
    private JobHandle positionJobHandle;
    private NativeArray<float3> generatedPositions;
    private bool jobInProgress = false;

    // Diagnostics
    private PoolProfiler profiler;
    private PoolDiagnostics diagnostics;
    private PoolLogger logger;
    private PoolHealthChecker healthChecker;

    /// <summary>
    /// Initializes pools and UI when the scene starts
    /// </summary>
    private void Awake()
    {
        // Initialize PoolingServices
        PoolingServices.Initialize();
        
        // Get diagnostic services
        profiler = PoolingServices.Profiler;
        diagnostics = PoolingServices.Diagnostics;
        logger = PoolingServices.Logger;
        healthChecker = PoolingServices.HealthChecker;
        
        // If health checker wasn't created by the service locator, create it now
        if (healthChecker == null && Application.isPlaying)
        {
            GameObject healthCheckerGO = new GameObject("PoolHealthChecker");
            healthChecker = healthCheckerGO.AddComponent<PoolHealthChecker>();
            DontDestroyOnLoad(healthCheckerGO);
            
            // Register the new health checker with the service locator
            PoolingServices.RegisterService(healthChecker);
        }
        
        // Configure health checker
        if (healthChecker != null)
        {
            healthChecker.SetCheckInterval(5.0f);
            healthChecker.SetAlertFlags(true, true, true);
        }

        // Initialize spawn request queue with Collections v2
        spawnRequests = new UnsafeList<SpawnRequest>(64, Allocator.Persistent);

        // Create pools with initial configuration
        CreatePools();

        // Initialize UI elements
        InitializeUI();
    }

    /// <summary>
    /// Handles per-frame updates for stats and batch spawning
    /// </summary>
    private void Update()
    {
        // Update FPS calculation
        UpdateFPS();

        // Update memory usage display
        if (useProfilerForMemory)
        {
            UpdateMemoryUsageProfiler();
            UpdateStatsDisplayProfiler();
        }
        else
        {
            UpdateMemoryUsageRuntime();
            UpdateStatsDisplayRuntime();
        }

        // Additional memory updates for stress test
        if (stressTestActive)
        {
            UpdateMemoryUsageForStressTest();
        }

        // Process pending spawn requests
        ProcessBatchSpawn();

        // Handle any completed job results
        ProcessJobResults();
    }

    /// <summary>
    /// Gets a random position for spawning objects
    /// </summary>
    /// <returns>Random position within spawn bounds</returns>
    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(0.5f, 10f),
            UnityEngine.Random.Range(-10f, 10f)
        );
    }
}