using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Manages profiling integration with Unity's scene lifecycle.
    /// Automatically profiles scene loading, initialization, and cleanup operations.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class ProfilingSceneManager : MonoBehaviour
    {
        [Header("Scene Profiling")]
        [SerializeField] private bool _profileSceneLoading = true;
        [SerializeField] private bool _profileSceneUnloading = true;
        [SerializeField] private bool _profileSceneInitialization = true;
        [SerializeField] private bool _profileGameObjectLifecycle = true;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableDetailedProfiling = false;
        [SerializeField] private int _maxTrackedGameObjects = 1000;
        [SerializeField] private float _profilingTimeout = 30.0f;
        
        [Header("Categories")]
        [SerializeField] private ProfilerCategory _sceneCategory = ProfilerCategory.Loading;
        [SerializeField] private ProfilerCategory _gameObjectCategory = ProfilerCategory.Scripts;
        
        private ProfileManager _profileManager;
        private readonly Dictionary<string, IProfilerSession> _activeSceneSessions = new Dictionary<string, IProfilerSession>();
        private readonly Dictionary<int, IProfilerSession> _activeGameObjectSessions = new Dictionary<int, IProfilerSession>();
        private readonly Dictionary<string, SceneMetrics> _sceneMetrics = new Dictionary<string, SceneMetrics>();
        
        private bool _isInitialized;
        private Coroutine _cleanupCoroutine;
        
        /// <summary>
        /// Gets whether the scene manager is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Gets the current profile manager
        /// </summary>
        public ProfileManager ProfileManager => _profileManager;
        
        /// <summary>
        /// Event fired when a scene profiling session completes
        /// </summary>
        public event Action<string, double> SceneSessionCompleted;
        
        /// <summary>
        /// Event fired when a GameObject profiling session completes
        /// </summary>
        public event Action<GameObject, double> GameObjectSessionCompleted;
        
        private void Awake()
        {
            InitializeSceneManager();
        }
        
        private void Start()
        {
            if (_isInitialized)
            {
                RegisterSceneEvents();
                StartCleanupCoroutine();
            }
        }
        
        private void OnDestroy()
        {
            UnregisterSceneEvents();
            CleanupActiveSessions();
            
            if (_cleanupCoroutine != null)
            {
                StopCoroutine(_cleanupCoroutine);
            }
        }
        
        /// <summary>
        /// Initializes the scene manager
        /// </summary>
        private void InitializeSceneManager()
        {
            try
            {
                _profileManager = FindObjectOfType<ProfileManager>();
                if (_profileManager == null)
                {
                    _profileManager = ProfileManager.Instance;
                }
                
                if (_profileManager == null)
                {
                    Debug.LogWarning("[ProfilingSceneManager] No ProfileManager found - scene profiling disabled");
                    return;
                }
                
                DontDestroyOnLoad(gameObject);
                _isInitialized = true;
                
                Debug.Log("[ProfilingSceneManager] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfilingSceneManager] Failed to initialize: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers for scene management events
        /// </summary>
        private void RegisterSceneEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        
        /// <summary>
        /// Unregisters from scene management events
        /// </summary>
        private void UnregisterSceneEvents()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
        
        /// <summary>
        /// Starts the cleanup coroutine for managing session timeouts
        /// </summary>
        private void StartCleanupCoroutine()
        {
            _cleanupCoroutine = StartCoroutine(CleanupSessionsCoroutine());
        }
        
        /// <summary>
        /// Coroutine that periodically cleans up expired sessions
        /// </summary>
        private IEnumerator CleanupSessionsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(5.0f);
                CleanupExpiredSessions();
            }
        }
        
        /// <summary>
        /// Cleans up sessions that have exceeded the timeout
        /// </summary>
        private void CleanupExpiredSessions()
        {
            var expiredSessions = new List<string>();
            
            foreach (var kvp in _activeSceneSessions)
            {
                if (kvp.Value is ProfilerSession session && session.ElapsedMilliseconds > _profilingTimeout * 1000)
                {
                    expiredSessions.Add(kvp.Key);
                }
            }
            
            foreach (var sessionKey in expiredSessions)
            {
                EndSceneSession(sessionKey, "Timeout");
            }
        }
        
        /// <summary>
        /// Handles scene loaded event
        /// </summary>
        /// <param name="scene">The loaded scene</param>
        /// <param name="mode">The load mode</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_isInitialized || !_profileSceneLoading)
                return;
                
            string sceneName = scene.name;
            string sessionKey = $"SceneLoad_{sceneName}_{Time.realtimeSinceStartup}";
            
            var tag = new ProfilerTag(_sceneCategory, $"Scene.Load.{sceneName}");
            var session = _profileManager?.BeginScope(tag);
            
            if (session != null)
            {
                _activeSceneSessions[sessionKey] = session;
                
                // Track scene metrics
                _sceneMetrics[sceneName] = new SceneMetrics
                {
                    SceneName = sceneName,
                    LoadStartTime = Time.realtimeSinceStartup,
                    LoadMode = mode
                };
                
                Debug.Log($"[ProfilingSceneManager] Started profiling scene load: {sceneName}");
            }
            
            if (_profileSceneInitialization)
            {
                StartCoroutine(ProfileSceneInitialization(scene, sessionKey));
            }
        }
        
        /// <summary>
        /// Profiles scene initialization after loading
        /// </summary>
        /// <param name="scene">The scene to profile</param>
        /// <param name="loadSessionKey">The load session key</param>
        private IEnumerator ProfileSceneInitialization(Scene scene, string loadSessionKey)
        {
            // Wait a frame for the scene to be fully loaded
            yield return null;
            
            string sceneName = scene.name;
            string initSessionKey = $"SceneInit_{sceneName}_{Time.realtimeSinceStartup}";
            
            var tag = new ProfilerTag(_sceneCategory, $"Scene.Initialize.{sceneName}");
            var session = _profileManager?.BeginScope(tag);
            
            if (session != null)
            {
                _activeSceneSessions[initSessionKey] = session;
            }
            
            // Profile GameObject initialization if enabled
            if (_profileGameObjectLifecycle)
            {
                yield return StartCoroutine(ProfileGameObjectsInScene(scene));
            }
            
            // Wait for a few frames to capture initialization
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }
            
            // End initialization session
            EndSceneSession(initSessionKey, "Initialization");
            
            // End load session
            EndSceneSession(loadSessionKey, "Load");
        }
        
        /// <summary>
        /// Profiles GameObjects in a scene
        /// </summary>
        /// <param name="scene">The scene to profile</param>
        private IEnumerator ProfileGameObjectsInScene(Scene scene)
        {
            if (!_enableDetailedProfiling)
                yield break;
                
            var rootObjects = scene.GetRootGameObjects();
            int objectCount = 0;
            
            foreach (var rootObject in rootObjects)
            {
                if (objectCount >= _maxTrackedGameObjects)
                    break;
                    
                var allObjects = rootObject.GetComponentsInChildren<Transform>(true);
                
                foreach (var transform in allObjects)
                {
                    if (objectCount >= _maxTrackedGameObjects)
                        break;
                        
                    ProfileGameObjectInitialization(transform.gameObject);
                    objectCount++;
                    
                    // Yield occasionally to avoid frame spikes
                    if (objectCount % 10 == 0)
                    {
                        yield return null;
                    }
                }
            }
            
            Debug.Log($"[ProfilingSceneManager] Profiled {objectCount} GameObjects in scene {scene.name}");
        }
        
        /// <summary>
        /// Profiles a single GameObject's initialization
        /// </summary>
        /// <param name="gameObject">The GameObject to profile</param>
        private void ProfileGameObjectInitialization(GameObject gameObject)
        {
            if (!_profileGameObjectLifecycle || gameObject == null)
                return;
                
            var tag = new ProfilerTag(_gameObjectCategory, $"GameObject.Init.{gameObject.name}");
            var session = _profileManager?.BeginScope(tag);
            
            if (session != null)
            {
                int instanceId = gameObject.GetInstanceID();
                _activeGameObjectSessions[instanceId] = session;
                
                // End the session after a short delay
                StartCoroutine(EndGameObjectSessionDelayed(instanceId, gameObject, 0.1f));
            }
        }
        
        /// <summary>
        /// Ends a GameObject session after a delay
        /// </summary>
        /// <param name="instanceId">GameObject instance ID</param>
        /// <param name="gameObject">The GameObject</param>
        /// <param name="delay">Delay in seconds</param>
        private IEnumerator EndGameObjectSessionDelayed(int instanceId, GameObject gameObject, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_activeGameObjectSessions.TryGetValue(instanceId, out var session))
            {
                double duration = 0;
                if (session is ProfilerSession profilerSession)
                {
                    duration = profilerSession.ElapsedMilliseconds;
                }
                
                session.Dispose();
                _activeGameObjectSessions.Remove(instanceId);
                
                GameObjectSessionCompleted?.Invoke(gameObject, duration);
            }
        }
        
        /// <summary>
        /// Handles scene unloaded event
        /// </summary>
        /// <param name="scene">The unloaded scene</param>
        private void OnSceneUnloaded(Scene scene)
        {
            if (!_isInitialized || !_profileSceneUnloading)
                return;
                
            string sceneName = scene.name;
            string sessionKey = $"SceneUnload_{sceneName}_{Time.realtimeSinceStartup}";
            
            var tag = new ProfilerTag(_sceneCategory, $"Scene.Unload.{sceneName}");
            var session = _profileManager?.BeginScope(tag);
            
            if (session != null)
            {
                _activeSceneSessions[sessionKey] = session;
                
                // End the session after a short delay
                StartCoroutine(EndSceneSessionDelayed(sessionKey, "Unload", 0.1f));
            }
            
            // Clean up scene metrics
            _sceneMetrics.Remove(sceneName);
        }
        
        /// <summary>
        /// Handles active scene changed event
        /// </summary>
        /// <param name="previousScene">The previous active scene</param>
        /// <param name="newScene">The new active scene</param>
        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            if (!_isInitialized)
                return;
                
            string sessionKey = $"SceneChange_{previousScene.name}_to_{newScene.name}_{Time.realtimeSinceStartup}";
            
            var tag = new ProfilerTag(_sceneCategory, "Scene.Change");
            var session = _profileManager?.BeginScope(tag);
            
            if (session != null)
            {
                _activeSceneSessions[sessionKey] = session;
                StartCoroutine(EndSceneSessionDelayed(sessionKey, "Change", 0.1f));
            }
        }
        
        /// <summary>
        /// Ends a scene session after a delay
        /// </summary>
        /// <param name="sessionKey">Session key</param>
        /// <param name="operation">Operation name</param>
        /// <param name="delay">Delay in seconds</param>
        private IEnumerator EndSceneSessionDelayed(string sessionKey, string operation, float delay)
        {
            yield return new WaitForSeconds(delay);
            EndSceneSession(sessionKey, operation);
        }
        
        /// <summary>
        /// Ends a scene profiling session
        /// </summary>
        /// <param name="sessionKey">Session key</param>
        /// <param name="operation">Operation name</param>
        private void EndSceneSession(string sessionKey, string operation)
        {
            if (_activeSceneSessions.TryGetValue(sessionKey, out var session))
            {
                double duration = 0;
                if (session is ProfilerSession profilerSession)
                {
                    duration = profilerSession.ElapsedMilliseconds;
                }
                
                session.Dispose();
                _activeSceneSessions.Remove(sessionKey);
                
                SceneSessionCompleted?.Invoke(sessionKey, duration);
                
                Debug.Log($"[ProfilingSceneManager] {operation} session completed: {sessionKey} ({duration:F2}ms)");
            }
        }
        
        /// <summary>
        /// Cleans up all active sessions
        /// </summary>
        private void CleanupActiveSessions()
        {
            foreach (var session in _activeSceneSessions.Values)
            {
                session?.Dispose();
            }
            _activeSceneSessions.Clear();
            
            foreach (var session in _activeGameObjectSessions.Values)
            {
                session?.Dispose();
            }
            _activeGameObjectSessions.Clear();
        }
        
        /// <summary>
        /// Manually starts profiling a scene operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="sceneName">Name of the scene</param>
        /// <returns>Session key for ending the session</returns>
        public string BeginSceneOperation(string operationName, string sceneName = null)
        {
            if (!_isInitialized || _profileManager == null)
                return null;
                
            sceneName = sceneName ?? SceneManager.GetActiveScene().name;
            string sessionKey = $"Manual_{operationName}_{sceneName}_{Time.realtimeSinceStartup}";
            
            var tag = new ProfilerTag(_sceneCategory, $"Scene.{operationName}.{sceneName}");
            var session = _profileManager.BeginScope(tag);
            
            if (session != null)
            {
                _activeSceneSessions[sessionKey] = session;
            }
            
            return sessionKey;
        }
        
        /// <summary>
        /// Manually ends a scene operation
        /// </summary>
        /// <param name="sessionKey">Session key from BeginSceneOperation</param>
        public void EndSceneOperation(string sessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
                return;
                
            EndSceneSession(sessionKey, "Manual");
        }
        
        /// <summary>
        /// Profiles a scene operation with automatic cleanup
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="operation">Operation to profile</param>
        /// <param name="sceneName">Name of the scene</param>
        public void ProfileSceneOperation(string operationName, Action operation, string sceneName = null)
        {
            if (!_isInitialized || _profileManager == null || operation == null)
            {
                operation?.Invoke();
                return;
            }
            
            sceneName = sceneName ?? SceneManager.GetActiveScene().name;
            var tag = new ProfilerTag(_sceneCategory, $"Scene.{operationName}.{sceneName}");
            
            using (_profileManager.BeginScope(tag))
            {
                operation.Invoke();
            }
        }
        
        /// <summary>
        /// Gets scene metrics for a specific scene
        /// </summary>
        /// <param name="sceneName">Name of the scene</param>
        /// <returns>Scene metrics if available</returns>
        public SceneMetrics GetSceneMetrics(string sceneName)
        {
            _sceneMetrics.TryGetValue(sceneName, out var metrics);
            return metrics;
        }
        
        /// <summary>
        /// Gets all current scene metrics
        /// </summary>
        /// <returns>Dictionary of scene metrics</returns>
        public IReadOnlyDictionary<string, SceneMetrics> GetAllSceneMetrics()
        {
            return _sceneMetrics;
        }
        
        /// <summary>
        /// Gets the number of active profiling sessions
        /// </summary>
        /// <returns>Number of active sessions</returns>
        public int GetActiveSessionCount()
        {
            return _activeSceneSessions.Count + _activeGameObjectSessions.Count;
        }
        
        /// <summary>
        /// Sets whether scene loading profiling is enabled
        /// </summary>
        /// <param name="enabled">Whether to enable scene loading profiling</param>
        public void SetSceneLoadingProfilingEnabled(bool enabled)
        {
            _profileSceneLoading = enabled;
        }
        
        /// <summary>
        /// Sets whether GameObject lifecycle profiling is enabled
        /// </summary>
        /// <param name="enabled">Whether to enable GameObject lifecycle profiling</param>
        public void SetGameObjectProfilingEnabled(bool enabled)
        {
            _profileGameObjectLifecycle = enabled;
        }
        
        /// <summary>
        /// Sets the maximum number of tracked GameObjects
        /// </summary>
        /// <param name="maxCount">Maximum number of GameObjects to track</param>
        public void SetMaxTrackedGameObjects(int maxCount)
        {
            _maxTrackedGameObjects = Mathf.Max(1, maxCount);
        }
        
        /// <summary>
        /// Gets the singleton instance (creates one if none exists)
        /// </summary>
        public static ProfilingSceneManager Instance
        {
            get
            {
                var instance = FindObjectOfType<ProfilingSceneManager>();
                if (instance == null)
                {
                    var go = new GameObject("[ProfilingSceneManager]");
                    instance = go.AddComponent<ProfilingSceneManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
    }
    
    /// <summary>
    /// Metrics data for a scene
    /// </summary>
    [Serializable]
    public struct SceneMetrics
    {
        /// <summary>
        /// Name of the scene
        /// </summary>
        public string SceneName;
        
        /// <summary>
        /// Time when loading started
        /// </summary>
        public float LoadStartTime;
        
        /// <summary>
        /// Time when loading completed
        /// </summary>
        public float LoadEndTime;
        
        /// <summary>
        /// Scene load mode
        /// </summary>
        public LoadSceneMode LoadMode;
        
        /// <summary>
        /// Number of root GameObjects in the scene
        /// </summary>
        public int RootGameObjectCount;
        
        /// <summary>
        /// Total number of GameObjects in the scene
        /// </summary>
        public int TotalGameObjectCount;
        
        /// <summary>
        /// Number of components in the scene
        /// </summary>
        public int ComponentCount;
        
        /// <summary>
        /// Estimated memory usage of the scene
        /// </summary>
        public long EstimatedMemoryUsage;
        
        /// <summary>
        /// Gets the total load duration in seconds
        /// </summary>
        public float LoadDuration => LoadEndTime - LoadStartTime;
        
        /// <summary>
        /// Gets whether the scene is currently loaded
        /// </summary>
        public bool IsLoaded => LoadEndTime > 0;
    }
}