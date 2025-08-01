using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Serialization.Components;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using MemoryPack;
using Reflex.Attributes;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Components
{
    /// <summary>
    /// Manages serialization during scene transitions to ensure data persistence.
    /// Coordinates saving current scene state and loading target scene state,
    /// handles cross-scene data transfer, and provides transition progress tracking.
    /// </summary>
    [AddComponentMenu("AhBearStudios/Serialization/Scene Transition Manager")]
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField]
        private bool _enableAutomaticTransitionPersistence = true;
        
        [SerializeField]
        private bool _enableCrossSceneDataTransfer = true;
        
        [SerializeField]
        private float _transitionSaveTimeout = 5f;
        
        [SerializeField]
        private float _transitionLoadTimeout = 10f;
        
        [Header("Performance")]
        [SerializeField]
        private bool _useAsyncTransitions = true;
        
        [SerializeField]
        private int _maxConcurrentTransitionOperations = 2;
        
        [SerializeField]
        private bool _enableTransitionCaching = true;
        
        [SerializeField]
        private int _maxCachedTransitions = 10;
        
        [Header("Cross-Scene Data")]
        [SerializeField]
        private List<string> _persistentDataKeys = new List<string>();
        
        [SerializeField]
        private bool _transferPlayerData = true;
        
        [SerializeField]
        private bool _transferGameState = true;
        
        [SerializeField]
        private bool _transferUIState = false;

        [Inject]
        private ISerializationService _serializationService;
        
        [Inject]
        private ILoggingService _logger;

        private readonly Dictionary<string, byte[]> _crossSceneData = new Dictionary<string, byte[]>();
        private readonly List<SceneTransition> _transitionHistory = new List<SceneTransition>();
        private readonly Dictionary<string, SceneSerializationManager> _sceneManagers = new Dictionary<string, SceneSerializationManager>();
        
        private static SceneTransitionManager _instance;
        private bool _isTransitionInProgress = false;
        private SceneTransition _currentTransition;
        private FixedString64Bytes _correlationId;

        /// <summary>
        /// Singleton instance of the SceneTransitionManager.
        /// </summary>
        public static SceneTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneTransitionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SceneTransitionManager");
                        _instance = go.AddComponent<SceneTransitionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when a scene transition starts.
        /// </summary>
        public event Action<SceneTransition> OnTransitionStarted;

        /// <summary>
        /// Event raised when a scene transition completes successfully.
        /// </summary>
        public event Action<SceneTransition> OnTransitionCompleted;

        /// <summary>
        /// Event raised when a scene transition fails.
        /// </summary>
        public event Action<SceneTransition, Exception> OnTransitionFailed;

        /// <summary>
        /// Event raised to update transition progress.
        /// </summary>
        public event Action<SceneTransition, float> OnTransitionProgress;

        /// <summary>
        /// Gets whether a scene transition is currently in progress.
        /// </summary>
        public bool IsTransitionInProgress => _isTransitionInProgress;

        /// <summary>
        /// Gets the current transition information.
        /// </summary>
        public SceneTransition CurrentTransition => _currentTransition;

        /// <summary>
        /// Gets the number of cached transitions.
        /// </summary>
        public int CachedTransitionCount => _transitionHistory.Count;

        /// <summary>
        /// Gets or sets whether automatic transition persistence is enabled.
        /// </summary>
        public bool EnableAutomaticTransitionPersistence
        {
            get => _enableAutomaticTransitionPersistence;
            set => _enableAutomaticTransitionPersistence = value;
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                _correlationId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_serializationService == null || _logger == null)
            {
                Debug.LogWarning("[SceneTransitionManager] Dependency injection failed. Manager may not function correctly.", this);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// Initiates a scene transition with automatic persistence.
        /// </summary>
        /// <param name="targetSceneName">Name of the target scene</param>
        /// <param name="loadMode">Scene load mode</param>
        /// <param name="transitionData">Optional transition-specific data</param>
        /// <returns>UniTask that completes when transition finishes</returns>
        public async UniTask<SceneTransitionResult> TransitionToSceneAsync(string targetSceneName, LoadSceneMode loadMode = LoadSceneMode.Single, TransitionData transitionData = null)
        {
            if (_isTransitionInProgress)
            {
                _logger?.LogWarning("Scene transition already in progress", _correlationId, nameof(SceneTransitionManager), null);
                return new SceneTransitionResult { IsSuccess = false, ErrorMessage = "Transition already in progress" };
            }

            _isTransitionInProgress = true;
            var startTime = DateTime.UtcNow;

            try
            {
                var transition = new SceneTransition
                {
                    TransitionId = Guid.NewGuid().ToString("N"),
                    SourceSceneName = SceneManager.GetActiveScene().name,
                    TargetSceneName = targetSceneName,
                    LoadMode = loadMode,
                    StartTime = startTime,
                    TransitionData = transitionData,
                    Status = TransitionStatus.Starting
                };

                _currentTransition = transition;
                OnTransitionStarted?.Invoke(transition);

                _logger?.LogInfo($"Starting scene transition: {transition.SourceSceneName} -> {transition.TargetSceneName}", 
                    _correlationId, nameof(SceneTransitionManager), null);

                var result = await ExecuteSceneTransitionAsync(transition);

                if (result.IsSuccess)
                {
                    transition.Status = TransitionStatus.Completed;
                    transition.EndTime = DateTime.UtcNow;
                    transition.TotalDuration = transition.EndTime.Value - transition.StartTime;

                    // Add to history
                    _transitionHistory.Add(transition);
                    if (_enableTransitionCaching && _transitionHistory.Count > _maxCachedTransitions)
                    {
                        _transitionHistory.RemoveAt(0);
                    }

                    OnTransitionCompleted?.Invoke(transition);
                    
                    _logger?.LogInfo($"Scene transition completed successfully in {transition.TotalDuration?.TotalMilliseconds:F2}ms", 
                        _correlationId, nameof(SceneTransitionManager), null);
                }
                else
                {
                    transition.Status = TransitionStatus.Failed;
                    transition.ErrorMessage = result.ErrorMessage;
                    OnTransitionFailed?.Invoke(transition, new InvalidOperationException(result.ErrorMessage));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Scene transition failed: {ex.Message}", _correlationId, nameof(SceneTransitionManager), null);
                
                if (_currentTransition != null)
                {
                    _currentTransition.Status = TransitionStatus.Failed;
                    _currentTransition.ErrorMessage = ex.Message;
                    OnTransitionFailed?.Invoke(_currentTransition, ex);
                }

                return new SceneTransitionResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            finally
            {
                _isTransitionInProgress = false;
                _currentTransition = null;
            }
        }

        /// <summary>
        /// Stores data for cross-scene transfer.
        /// </summary>
        /// <param name="key">Unique key for the data</param>
        /// <param name="data">Data to store</param>
        public void StoreCrossSceneData(string key, object data)
        {
            if (string.IsNullOrEmpty(key) || data == null)
                return;

            try
            {
                var config = CreateSerializationConfig();
                var serializationTask = _serializationService.SerializeAsync(data, config);
                var result = serializationTask.AsUniTask().GetAwaiter().GetResult();

                if (result.IsSuccess)
                {
                    _crossSceneData[key] = result.Data;
                    _logger?.LogInfo($"Cross-scene data stored: {key} ({result.Data.Length} bytes)", 
                        _correlationId, nameof(SceneTransitionManager), null);
                }
                else
                {
                    _logger?.LogError($"Failed to serialize cross-scene data for key {key}: {result.ErrorMessage}", 
                        _correlationId, nameof(SceneTransitionManager), null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to store cross-scene data for key {key}: {ex.Message}", 
                    _correlationId, nameof(SceneTransitionManager), null);
            }
        }

        /// <summary>
        /// Retrieves data from cross-scene storage.
        /// </summary>
        /// <typeparam name="T">Type of data to retrieve</typeparam>
        /// <param name="key">Key of the data to retrieve</param>
        /// <returns>UniTask containing the retrieved data</returns>
        public async UniTask<T> GetCrossSceneDataAsync<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key) || !_crossSceneData.TryGetValue(key, out var data))
                return default(T);

            try
            {
                var config = CreateSerializationConfig();
                var result = await _serializationService.DeserializeAsync<T>(data, config);

                if (result.IsSuccess)
                {
                    _logger?.LogInfo($"Cross-scene data retrieved: {key}", _correlationId, nameof(SceneTransitionManager), null);
                    return result.Data;
                }
                else
                {
                    _logger?.LogError($"Failed to deserialize cross-scene data for key {key}: {result.ErrorMessage}", 
                        _correlationId, nameof(SceneTransitionManager), null);
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to retrieve cross-scene data for key {key}: {ex.Message}", 
                    _correlationId, nameof(SceneTransitionManager), null);
                return default(T);
            }
        }

        /// <summary>
        /// Clears cross-scene data for a specific key.
        /// </summary>
        /// <param name="key">Key to clear</param>
        public void ClearCrossSceneData(string key)
        {
            if (_crossSceneData.Remove(key))
            {
                _logger?.LogInfo($"Cross-scene data cleared: {key}", _correlationId, nameof(SceneTransitionManager), null);
            }
        }

        /// <summary>
        /// Clears all cross-scene data.
        /// </summary>
        public void ClearAllCrossSceneData()
        {
            var count = _crossSceneData.Count;
            _crossSceneData.Clear();
            
            _logger?.LogInfo($"All cross-scene data cleared ({count} entries)", _correlationId, nameof(SceneTransitionManager), null);
        }

        /// <summary>
        /// Gets statistics about the transition system.
        /// </summary>
        /// <returns>Current transition statistics</returns>
        public SceneTransitionStatistics GetTransitionStatistics()
        {
            var stats = new SceneTransitionStatistics
            {
                TotalTransitions = _transitionHistory.Count,
                SuccessfulTransitions = _transitionHistory.AsValueEnumerable().Count(t => t.Status == TransitionStatus.Completed),
                FailedTransitions = _transitionHistory.AsValueEnumerable().Count(t => t.Status == TransitionStatus.Failed),
                CrossSceneDataEntries = _crossSceneData.Count,
                CrossSceneDataSize = _crossSceneData.Values.AsValueEnumerable().Sum(data => data.Length),
                IsTransitionInProgress = _isTransitionInProgress,
                CurrentTransitionId = _currentTransition?.TransitionId,
                AverageTransitionTime = _transitionHistory.Count > 0 ? 
                    _transitionHistory.AsValueEnumerable()
                        .Where(t => t.TotalDuration.HasValue)
                        .Average(t => t.TotalDuration.Value.TotalMilliseconds) : 0
            };

            return stats;
        }

        /// <summary>
        /// Gets transition history.
        /// </summary>
        /// <param name="maxEntries">Maximum number of entries to return</param>
        /// <returns>List of recent transitions</returns>
        public List<SceneTransition> GetTransitionHistory(int maxEntries = 10)
        {
            return _transitionHistory.TakeLast(maxEntries).ToList();
        }

        private async UniTask<SceneTransitionResult> ExecuteSceneTransitionAsync(SceneTransition transition)
        {
            var result = new SceneTransitionResult
            {
                TransitionId = transition.TransitionId,
                SourceSceneName = transition.SourceSceneName,
                TargetSceneName = transition.TargetSceneName
            };

            try
            {
                // Phase 1: Save current scene state
                OnTransitionProgress?.Invoke(transition, 0.1f);
                transition.Status = TransitionStatus.SavingSourceScene;

                if (_enableAutomaticTransitionPersistence)
                {
                    var saveResult = await SaveCurrentSceneStateAsync(transition);
                    if (!saveResult.IsSuccess)
                    {
                        result.ErrorMessage = $"Failed to save source scene: {saveResult.ErrorMessage}";
                        return result;
                    }
                    result.SourceSceneSaveTime = saveResult.ProcessingTime;
                }

                // Phase 2: Prepare cross-scene data
                OnTransitionProgress?.Invoke(transition, 0.3f);
                transition.Status = TransitionStatus.PreparingCrossSceneData;

                if (_enableCrossSceneDataTransfer)
                {
                    await PrepareCrossSceneDataAsync(transition);
                }

                // Phase 3: Load target scene
                OnTransitionProgress?.Invoke(transition, 0.5f);
                transition.Status = TransitionStatus.LoadingTargetScene;

                var sceneLoadResult = await LoadTargetSceneAsync(transition);
                if (!sceneLoadResult.IsSuccess)
                {
                    result.ErrorMessage = $"Failed to load target scene: {sceneLoadResult.ErrorMessage}";
                    return result;
                }
                result.SceneLoadTime = sceneLoadResult.ProcessingTime;

                // Phase 4: Apply cross-scene data
                OnTransitionProgress?.Invoke(transition, 0.7f);
                transition.Status = TransitionStatus.ApplyingCrossSceneData;

                if (_enableCrossSceneDataTransfer)
                {
                    await ApplyCrossSceneDataAsync(transition);
                }

                // Phase 5: Load target scene state
                OnTransitionProgress?.Invoke(transition, 0.9f);
                transition.Status = TransitionStatus.LoadingTargetSceneState;

                if (_enableAutomaticTransitionPersistence)
                {
                    var loadResult = await LoadTargetSceneStateAsync(transition);
                    result.TargetSceneLoadTime = loadResult.ProcessingTime;
                }

                // Phase 6: Complete
                OnTransitionProgress?.Invoke(transition, 1.0f);

                result.IsSuccess = true;
                result.TotalProcessingTime = DateTime.UtcNow - transition.StartTime;

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async UniTask<SceneOperationResult> SaveCurrentSceneStateAsync(SceneTransition transition)
        {
            try
            {
                var sourceScene = SceneManager.GetActiveScene();
                var sceneManager = FindSceneManager(sourceScene.name);

                if (sceneManager != null)
                {
                    var saveResult = await sceneManager.SaveSceneDataAsync();
                    return new SceneOperationResult
                    {
                        IsSuccess = saveResult.IsSuccess,
                        ProcessingTime = saveResult.ProcessingTime,
                        ErrorMessage = saveResult.ErrorMessage
                    };
                }
                else
                {
                    _logger?.LogWarning($"No SceneSerializationManager found for scene: {sourceScene.name}", 
                        _correlationId, nameof(SceneTransitionManager), null);
                    return new SceneOperationResult { IsSuccess = true, ProcessingTime = TimeSpan.Zero };
                }
            }
            catch (Exception ex)
            {
                return new SceneOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        private async UniTask<SceneOperationResult> LoadTargetSceneAsync(SceneTransition transition)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                if (_useAsyncTransitions)
                {
                    var asyncOperation = SceneManager.LoadSceneAsync(transition.TargetSceneName, transition.LoadMode);
                    
                    while (!asyncOperation.isDone)
                    {
                        var progress = 0.5f + (asyncOperation.progress * 0.2f); // 50-70% of total progress
                        OnTransitionProgress?.Invoke(transition, progress);
                        await UniTask.Yield();
                    }
                }
                else
                {
                    SceneManager.LoadScene(transition.TargetSceneName, transition.LoadMode);
                }

                var processingTime = DateTime.UtcNow - startTime;
                return new SceneOperationResult { IsSuccess = true, ProcessingTime = processingTime };
            }
            catch (Exception ex)
            {
                return new SceneOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        private async UniTask<SceneOperationResult> LoadTargetSceneStateAsync(SceneTransition transition)
        {
            try
            {
                // Wait a frame for the new scene to initialize
                await UniTask.Yield();

                var targetScene = SceneManager.GetActiveScene();
                var sceneManager = FindSceneManager(targetScene.name);

                if (sceneManager != null)
                {
                    var loadResult = await sceneManager.LoadSceneDataAsync();
                    return new SceneOperationResult
                    {
                        IsSuccess = loadResult.IsSuccess,
                        ProcessingTime = loadResult.ProcessingTime,
                        ErrorMessage = loadResult.ErrorMessage
                    };
                }
                else
                {
                    _logger?.LogWarning($"No SceneSerializationManager found for scene: {targetScene.name}", 
                        _correlationId, nameof(SceneTransitionManager), null);
                    return new SceneOperationResult { IsSuccess = true, ProcessingTime = TimeSpan.Zero };
                }
            }
            catch (Exception ex)
            {
                return new SceneOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        private async UniTask PrepareCrossSceneDataAsync(SceneTransition transition)
        {
            if (transition.TransitionData?.CrossSceneKeys != null)
            {
                foreach (var key in transition.TransitionData.CrossSceneKeys)
                {
                    if (_persistentDataKeys.Contains(key))
                    {
                        // Prepare persistent data for transfer
                        _logger?.LogInfo($"Preparing cross-scene data for key: {key}", 
                            _correlationId, nameof(SceneTransitionManager), null);
                    }
                }
            }

            await UniTask.Yield(); // Placeholder for actual implementation
        }

        private async UniTask ApplyCrossSceneDataAsync(SceneTransition transition)
        {
            if (_crossSceneData.Count > 0)
            {
                _logger?.LogInfo($"Applying {_crossSceneData.Count} cross-scene data entries", 
                    _correlationId, nameof(SceneTransitionManager), null);

                // Apply cross-scene data to new scene
                foreach (var kvp in _crossSceneData)
                {
                    // This would typically involve finding target components and applying the data
                    _logger?.LogInfo($"Applied cross-scene data: {kvp.Key}", 
                        _correlationId, nameof(SceneTransitionManager), null);
                }
            }

            await UniTask.Yield(); // Placeholder for actual implementation
        }

        private SceneSerializationManager FindSceneManager(string sceneName)
        {
            if (_sceneManagers.TryGetValue(sceneName, out var cachedManager) && cachedManager != null)
            {
                return cachedManager;
            }

            var manager = FindObjectOfType<SceneSerializationManager>();
            if (manager != null)
            {
                _sceneManagers[sceneName] = manager;
            }

            return manager;
        }

        private SerializationConfig CreateSerializationConfig()
        {
            return new SerializationConfig
            {
                Format = SerializationFormat.MemoryPack,
                EnableCompression = true,
                EnableEncryption = false,
                ThreadingMode = SerializationThreadingMode.MultiThreaded,
                BufferPoolSize = 1024 * 1024, // 1MB
                MaxConcurrentOperations = _maxConcurrentTransitionOperations
            };
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Clear cached scene manager references when scenes are loaded
            _sceneManagers.Clear();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Remove scene manager reference for unloaded scene
            _sceneManagers.Remove(scene.name);
        }

        /// <summary>
        /// Manual transition trigger for editor or debugging purposes.
        /// </summary>
        /// <param name="sceneName">Target scene name</param>
        [ContextMenu("Test Scene Transition")]
        public void TestSceneTransition(string sceneName = "")
        {
            if (Application.isPlaying && !string.IsNullOrEmpty(sceneName))
            {
                _ = TransitionToSceneAsync(sceneName);
            }
        }
    }

    /// <summary>
    /// Information about a scene transition.
    /// </summary>
    [MemoryPackable]
    public partial class SceneTransition
    {
        public string TransitionId { get; set; }
        public string SourceSceneName { get; set; }
        public string TargetSceneName { get; set; }
        public LoadSceneMode LoadMode { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? TotalDuration { get; set; }
        public TransitionStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public TransitionData TransitionData { get; set; }
    }

    /// <summary>
    /// Data to be transferred during scene transitions.
    /// </summary>
    [MemoryPackable]
    public partial class TransitionData
    {
        public List<string> CrossSceneKeys { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
        public bool PreservePlayerState { get; set; }
        public bool PreserveUIState { get; set; }
        public bool PreserveGameState { get; set; }
    }

    /// <summary>
    /// Result of a scene transition operation.
    /// </summary>
    public struct SceneTransitionResult
    {
        public bool IsSuccess;
        public string TransitionId;
        public string SourceSceneName;
        public string TargetSceneName;
        public TimeSpan TotalProcessingTime;
        public TimeSpan SourceSceneSaveTime;
        public TimeSpan SceneLoadTime;
        public TimeSpan TargetSceneLoadTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Result of a scene operation (save/load).
    /// </summary>
    public struct SceneOperationResult
    {
        public bool IsSuccess;
        public TimeSpan ProcessingTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Statistics about the scene transition system.
    /// </summary>
    public struct SceneTransitionStatistics
    {
        public int TotalTransitions;
        public int SuccessfulTransitions;
        public int FailedTransitions;
        public int CrossSceneDataEntries;
        public int CrossSceneDataSize;
        public bool IsTransitionInProgress;
        public string CurrentTransitionId;
        public double AverageTransitionTime;
    }

    /// <summary>
    /// Status of a scene transition.
    /// </summary>
    public enum TransitionStatus
    {
        Starting,
        SavingSourceScene,
        PreparingCrossSceneData,
        LoadingTargetScene,
        ApplyingCrossSceneData,
        LoadingTargetSceneState,
        Completed,
        Failed
    }
}