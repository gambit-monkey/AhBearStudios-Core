using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Serialization.Jobs;
using AhBearStudios.Unity.Serialization.Components;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using MemoryPack;
using Reflex.Attributes;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Components
{
    /// <summary>
    /// Manages serialization and persistence for entire Unity scenes.
    /// Coordinates saving/loading of all serializable components within a scene,
    /// handles scene transitions, and provides centralized scene state management.
    /// </summary>
    [AddComponentMenu("AhBearStudios/Serialization/Scene Serialization Manager")]
    public class SceneSerializationManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField]
        private string _sceneIdentifier;
        
        [SerializeField]
        private bool _autoSaveOnSceneUnload = true;
        
        [SerializeField]
        private bool _autoLoadOnSceneLoad = true;
        
        [SerializeField]
        private bool _enableSceneTransitionPersistence = true;
        
        [SerializeField]
        private float _sceneStateSnapshotInterval = 10f;
        
        [Header("Performance")]
        [SerializeField]
        private bool _useAsyncSceneOperations = true;
        
        [SerializeField]
        private int _maxConcurrentSceneOperations = 2;
        
        [SerializeField]
        private bool _enableSceneStateCaching = true;
        
        [SerializeField]
        private int _maxCachedSceneStates = 5;
        
        [Header("Filtering")]
        [SerializeField]
        private List<string> _includedGameObjectTags = new List<string>();
        
        [SerializeField]
        private List<string> _excludedGameObjectTags = new List<string>();
        
        [SerializeField]
        private List<LayerMask> _includedLayers = new List<LayerMask>();
        
        [SerializeField]
        private bool _useTagWhitelist = false;

        [Inject]
        private ISerializationService _serializationService;
        
        [Inject]
        private ILoggingService _logger;

        private readonly Dictionary<string, SceneObjectData> _sceneObjects = new Dictionary<string, SceneObjectData>();
        private readonly List<SceneStateSnapshot> _sceneStateHistory = new List<SceneStateSnapshot>();
        private readonly Dictionary<string, PersistentDataManager> _dataManagers = new Dictionary<string, PersistentDataManager>();
        
        private SceneData _currentSceneData;
        private float _lastSnapshotTime;
        private bool _isInitialized = false;
        private bool _isSceneOperationInProgress = false;
        private FixedString64Bytes _correlationId;
        private Scene _managedScene;

        /// <summary>
        /// Event raised when scene serialization completes successfully.
        /// </summary>
        public event Action<SceneSerializationResult> OnSceneSerialized;

        /// <summary>
        /// Event raised when scene deserialization completes successfully.
        /// </summary>
        public event Action<SceneSerializationResult> OnSceneDeserialized;

        /// <summary>
        /// Event raised when a scene state snapshot is created.
        /// </summary>
        public event Action<SceneStateSnapshot> OnSceneSnapshotCreated;

        /// <summary>
        /// Event raised when scene operation fails.
        /// </summary>
        public event Action<string, Exception> OnSceneOperationFailed;

        /// <summary>
        /// Gets the unique identifier for this scene.
        /// </summary>
        public string SceneIdentifier
        {
            get
            {
                if (string.IsNullOrEmpty(_sceneIdentifier))
                {
                    _sceneIdentifier = gameObject.scene.name + "_" + gameObject.scene.buildIndex;
                }
                return _sceneIdentifier;
            }
            set => _sceneIdentifier = value;
        }

        /// <summary>
        /// Gets whether a scene operation is currently in progress.
        /// </summary>
        public bool IsSceneOperationInProgress => _isSceneOperationInProgress;

        /// <summary>
        /// Gets the number of tracked scene objects.
        /// </summary>
        public int TrackedObjectCount => _sceneObjects.Count;

        /// <summary>
        /// Gets the number of cached scene state snapshots.
        /// </summary>
        public int CachedSnapshotCount => _sceneStateHistory.Count;

        private void Awake()
        {
            _correlationId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
            _managedScene = gameObject.scene;
            
            if (_serializationService == null || _logger == null)
            {
                Debug.LogWarning("[SceneSerializationManager] Dependency injection failed. Manager may not function correctly.", this);
            }
        }

        private async void Start()
        {
            await InitializeAsync();
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Periodic scene state snapshots
            if (Time.time - _lastSnapshotTime >= _sceneStateSnapshotInterval)
            {
                _ = CreateSceneStateSnapshotAsync();
            }
        }

        private void OnDestroy()
        {
            if (_isInitialized && _autoSaveOnSceneUnload)
            {
                // Final save attempt
                _ = SaveSceneDataAsync();
            }
        }

        /// <summary>
        /// Initializes the scene serialization manager.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            try
            {
                _logger?.LogInfo($"Initializing SceneSerializationManager for scene: {SceneIdentifier}", 
                    _correlationId, nameof(SceneSerializationManager), null);

                await ScanSceneForSerializableObjectsAsync();
                
                if (_autoLoadOnSceneLoad)
                {
                    await LoadSceneDataAsync();
                }

                _lastSnapshotTime = Time.time;
                _isInitialized = true;

                _logger?.LogInfo($"SceneSerializationManager initialized with {_sceneObjects.Count} tracked objects", 
                    _correlationId, nameof(SceneSerializationManager), null);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to initialize SceneSerializationManager: {ex.Message}", 
                    _correlationId, nameof(SceneSerializationManager), null);
                OnSceneOperationFailed?.Invoke("Initialization", ex);
            }
        }

        /// <summary>
        /// Saves all scene data asynchronously.
        /// </summary>
        /// <returns>UniTask containing the serialization result</returns>
        public async UniTask<SceneSerializationResult> SaveSceneDataAsync()
        {
            if (_isSceneOperationInProgress)
            {
                _logger?.LogWarning("Scene operation already in progress", _correlationId, nameof(SceneSerializationManager), null);
                return new SceneSerializationResult { IsSuccess = false, ErrorMessage = "Operation already in progress" };
            }

            _isSceneOperationInProgress = true;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger?.LogInfo($"Starting scene data save for: {SceneIdentifier}", 
                    _correlationId, nameof(SceneSerializationManager), null);

                // Update scene data before saving
                await UpdateCurrentSceneDataAsync();

                // Serialize scene data
                var config = CreateSerializationConfig();
                var serializationResult = await _serializationService.SerializeAsync(_currentSceneData, config);

                if (serializationResult.IsSuccess)
                {
                    // Save to persistent storage
                    await SaveSceneDataToPersistentStorageAsync(serializationResult.Data);

                    var result = new SceneSerializationResult
                    {
                        IsSuccess = true,
                        SceneIdentifier = SceneIdentifier,
                        ObjectCount = _sceneObjects.Count,
                        DataSize = serializationResult.Data.Length,
                        ProcessingTime = DateTime.UtcNow - startTime,
                        OperationType = SceneOperationType.Save
                    };

                    OnSceneSerialized?.Invoke(result);
                    
                    _logger?.LogInfo($"Scene data saved successfully: {result.DataSize} bytes in {result.ProcessingTime.TotalMilliseconds:F2}ms", 
                        _correlationId, nameof(SceneSerializationManager), null);

                    return result;
                }
                else
                {
                    var errorResult = new SceneSerializationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = serializationResult.ErrorMessage,
                        OperationType = SceneOperationType.Save
                    };

                    OnSceneOperationFailed?.Invoke("Save", new InvalidOperationException(serializationResult.ErrorMessage));
                    return errorResult;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to save scene data: {ex.Message}", 
                    _correlationId, nameof(SceneSerializationManager), null);
                OnSceneOperationFailed?.Invoke("Save", ex);
                
                return new SceneSerializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    OperationType = SceneOperationType.Save
                };
            }
            finally
            {
                _isSceneOperationInProgress = false;
            }
        }

        /// <summary>
        /// Loads scene data asynchronously.
        /// </summary>
        /// <returns>UniTask containing the deserialization result</returns>
        public async UniTask<SceneSerializationResult> LoadSceneDataAsync()
        {
            if (_isSceneOperationInProgress)
            {
                _logger?.LogWarning("Scene operation already in progress", _correlationId, nameof(SceneSerializationManager), null);
                return new SceneSerializationResult { IsSuccess = false, ErrorMessage = "Operation already in progress" };
            }

            _isSceneOperationInProgress = true;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger?.LogInfo($"Starting scene data load for: {SceneIdentifier}", 
                    _correlationId, nameof(SceneSerializationManager), null);

                // Load data from persistent storage
                var data = await LoadSceneDataFromPersistentStorageAsync();
                
                if (data == null || data.Length == 0)
                {
                    _logger?.LogInfo($"No saved scene data found for: {SceneIdentifier}", 
                        _correlationId, nameof(SceneSerializationManager), null);
                    
                    return new SceneSerializationResult
                    {
                        IsSuccess = true,
                        SceneIdentifier = SceneIdentifier,
                        ObjectCount = 0,
                        DataSize = 0,
                        ProcessingTime = DateTime.UtcNow - startTime,
                        OperationType = SceneOperationType.Load
                    };
                }

                // Deserialize scene data
                var config = CreateSerializationConfig();
                var deserializationResult = await _serializationService.DeserializeAsync<SceneData>(data, config);

                if (deserializationResult.IsSuccess && deserializationResult.Data != null)
                {
                    // Apply scene data
                    await ApplySceneDataAsync(deserializationResult.Data);

                    var result = new SceneSerializationResult
                    {
                        IsSuccess = true,
                        SceneIdentifier = SceneIdentifier,
                        ObjectCount = deserializationResult.Data.SceneObjects?.Count ?? 0,
                        DataSize = data.Length,
                        ProcessingTime = DateTime.UtcNow - startTime,
                        OperationType = SceneOperationType.Load
                    };

                    OnSceneDeserialized?.Invoke(result);
                    
                    _logger?.LogInfo($"Scene data loaded successfully: {result.ObjectCount} objects from {result.DataSize} bytes in {result.ProcessingTime.TotalMilliseconds:F2}ms", 
                        _correlationId, nameof(SceneSerializationManager), null);

                    return result;
                }
                else
                {
                    var errorResult = new SceneSerializationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = deserializationResult.ErrorMessage,
                        OperationType = SceneOperationType.Load
                    };

                    OnSceneOperationFailed?.Invoke("Load", new InvalidOperationException(deserializationResult.ErrorMessage));
                    return errorResult;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load scene data: {ex.Message}", 
                    _correlationId, nameof(SceneSerializationManager), null);
                OnSceneOperationFailed?.Invoke("Load", ex);
                
                return new SceneSerializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    OperationType = SceneOperationType.Load
                };
            }
            finally
            {
                _isSceneOperationInProgress = false;
            }
        }

        /// <summary>
        /// Creates a snapshot of the current scene state.
        /// </summary>
        /// <returns>UniTask containing the created snapshot</returns>
        public async UniTask<SceneStateSnapshot> CreateSceneStateSnapshotAsync()
        {
            try
            {
                await UpdateCurrentSceneDataAsync();

                var snapshot = new SceneStateSnapshot
                {
                    SceneIdentifier = SceneIdentifier,
                    Timestamp = DateTime.UtcNow,
                    ObjectCount = _sceneObjects.Count,
                    SceneData = _currentSceneData
                };

                // Add to history and manage cache size
                _sceneStateHistory.Add(snapshot);
                if (_enableSceneStateCaching && _sceneStateHistory.Count > _maxCachedSceneStates)
                {
                    _sceneStateHistory.RemoveAt(0);
                }

                _lastSnapshotTime = Time.time;
                OnSceneSnapshotCreated?.Invoke(snapshot);

                _logger?.LogInfo($"Scene state snapshot created: {snapshot.ObjectCount} objects at {snapshot.Timestamp:HH:mm:ss}", 
                    _correlationId, nameof(SceneSerializationManager), null);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to create scene snapshot: {ex.Message}", 
                    _correlationId, nameof(SceneSerializationManager), null);
                OnSceneOperationFailed?.Invoke("Snapshot", ex);
                return null;
            }
        }

        /// <summary>
        /// Restores scene state from a specific snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from</param>
        /// <returns>UniTask that completes when restoration is finished</returns>
        public async UniTask RestoreFromSnapshotAsync(SceneStateSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SceneData == null)
            {
                throw new ArgumentException("Invalid snapshot data");
            }

            try
            {
                _logger?.LogInfo($"Restoring scene from snapshot: {snapshot.Timestamp:HH:mm:ss}", 
                    _correlationId, nameof(SceneSerializationManager), null);

                await ApplySceneDataAsync(snapshot.SceneData);

                _logger?.LogInfo("Scene restoration completed successfully", 
                    _correlationId, nameof(SceneSerializationManager), null);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to restore from snapshot: {ex.Message}", 
                    _correlationId, nameof(SceneSerializationManager), null);
                OnSceneOperationFailed?.Invoke("Restore", ex);
                throw;
            }
        }

        /// <summary>
        /// Registers a GameObject for scene-level tracking.
        /// </summary>
        /// <param name="gameObject">GameObject to track</param>
        /// <param name="customKey">Optional custom key for the object</param>
        public void RegisterSceneObject(GameObject gameObject, string customKey = null)
        {
            if (gameObject == null) return;

            var key = customKey ?? GenerateObjectKey(gameObject);
            if (_sceneObjects.ContainsKey(key)) return;

            var objectData = new SceneObjectData
            {
                GameObject = gameObject,
                InstanceId = gameObject.GetInstanceID(),
                Name = gameObject.name,
                Tag = gameObject.tag,
                Layer = gameObject.layer,
                RegistrationTime = DateTime.UtcNow
            };

            _sceneObjects[key] = objectData;
            
            _logger?.LogInfo($"Registered scene object: {key}", _correlationId, nameof(SceneSerializationManager), null);
        }

        /// <summary>
        /// Unregisters a GameObject from scene-level tracking.
        /// </summary>
        /// <param name="gameObject">GameObject to untrack</param>
        public void UnregisterSceneObject(GameObject gameObject)
        {
            if (gameObject == null) return;

            var key = GenerateObjectKey(gameObject);
            if (_sceneObjects.Remove(key))
            {
                _logger?.LogInfo($"Unregistered scene object: {key}", _correlationId, nameof(SceneSerializationManager), null);
            }
        }

        /// <summary>
        /// Gets scene statistics and information.
        /// </summary>
        /// <returns>Current scene statistics</returns>
        public SceneStatistics GetSceneStatistics()
        {
            var stats = new SceneStatistics
            {
                SceneIdentifier = SceneIdentifier,
                TrackedObjects = _sceneObjects.Count,
                SerializableComponents = CountSerializableComponents(),
                DataManagers = _dataManagers.Count,
                CachedSnapshots = _sceneStateHistory.Count,
                LastSnapshotTime = _sceneStateHistory.Count > 0 ? _sceneStateHistory.Last().Timestamp : (DateTime?)null,
                EstimatedDataSize = EstimateSceneDataSize(),
                IsInitialized = _isInitialized
            };

            return stats;
        }

        /// <summary>
        /// Clears all cached scene data and snapshots.
        /// </summary>
        public void ClearSceneCache()
        {
            _sceneStateHistory.Clear();
            _currentSceneData = null;
            
            _logger?.LogInfo("Scene cache cleared", _correlationId, nameof(SceneSerializationManager), null);
        }

        private async UniTask ScanSceneForSerializableObjectsAsync()
        {
            await UniTask.SwitchToMainThread();

            _sceneObjects.Clear();
            _dataManagers.Clear();

            // Find all GameObjects in the scene
            var rootObjects = _managedScene.GetRootGameObjects();
            
            foreach (var rootObject in rootObjects)
            {
                await ScanGameObjectHierarchyAsync(rootObject);
            }

            _logger?.LogInfo($"Scene scan completed: found {_sceneObjects.Count} trackable objects", 
                _correlationId, nameof(SceneSerializationManager), null);
        }

        private async UniTask ScanGameObjectHierarchyAsync(GameObject gameObject)
        {
            await UniTask.Yield(); // Prevent frame blocking

            // Check if object should be tracked
            if (ShouldTrackGameObject(gameObject))
            {
                RegisterSceneObject(gameObject);

                // Check for PersistentDataManager
                var dataManager = gameObject.GetComponent<PersistentDataManager>();
                if (dataManager != null)
                {
                    var key = GenerateObjectKey(gameObject);
                    _dataManagers[key] = dataManager;
                }
            }

            // Recursively scan children
            foreach (Transform child in gameObject.transform)
            {
                await ScanGameObjectHierarchyAsync(child.gameObject);
            }
        }

        private bool ShouldTrackGameObject(GameObject gameObject)
        {
            // Check tag filtering
            if (_useTagWhitelist)
            {
                if (_includedGameObjectTags.Count > 0 && !_includedGameObjectTags.Contains(gameObject.tag))
                    return false;
            }
            else
            {
                if (_excludedGameObjectTags.Contains(gameObject.tag))
                    return false;
            }

            // Check layer filtering
            if (_includedLayers.Count > 0)
            {
                var gameObjectLayer = 1 << gameObject.layer;
                var isIncluded = _includedLayers.AsValueEnumerable().Any(layerMask => (layerMask.value & gameObjectLayer) != 0);
                if (!isIncluded)
                    return false;
            }

            // Check for serializable components
            var serializableComponents = gameObject.GetComponents<ISerializable>();
            return serializableComponents.Length > 0;
        }

        private async UniTask UpdateCurrentSceneDataAsync()
        {
            _currentSceneData = new SceneData
            {
                SceneIdentifier = SceneIdentifier,
                SceneName = _managedScene.name,
                SceneBuildIndex = _managedScene.buildIndex,
                Timestamp = DateTime.UtcNow.Ticks,
                SceneObjects = new List<SerializableSceneObject>()
            };

            foreach (var kvp in _sceneObjects)
            {
                if (kvp.Value.GameObject == null) continue;

                try
                {
                    var sceneObject = await CreateSerializableSceneObjectAsync(kvp.Key, kvp.Value.GameObject);
                    if (sceneObject != null)
                    {
                        _currentSceneData.SceneObjects.Add(sceneObject);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to serialize scene object {kvp.Key}: {ex.Message}", 
                        _correlationId, nameof(SceneSerializationManager), null);
                }

                // Yield periodically to prevent frame blocking
                if (_currentSceneData.SceneObjects.Count % 10 == 0)
                {
                    await UniTask.Yield();
                }
            }
        }

        private async UniTask<SerializableSceneObject> CreateSerializableSceneObjectAsync(string key, GameObject gameObject)
        {
            var sceneObject = new SerializableSceneObject
            {
                Key = key,
                GameObjectData = new GameObjectData
                {
                    Name = gameObject.name,
                    Tag = gameObject.tag,
                    Layer = gameObject.layer,
                    IsActive = gameObject.activeInHierarchy,
                    Timestamp = DateTime.UtcNow.Ticks
                }
            };

            // Get serializable components
            var serializableComponents = gameObject.GetComponents<ISerializable>();
            sceneObject.SerializableComponentData = new List<ComponentSerializationData>();

            foreach (var component in serializableComponents)
            {
                try
                {
                    var result = await component.SerializeAsync();
                    if (result.IsSuccess)
                    {
                        sceneObject.SerializableComponentData.Add(new ComponentSerializationData
                        {
                            ComponentType = component.GetType().FullName,
                            SerializedData = result.Data
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to serialize component {component.GetType().Name}: {ex.Message}", 
                        _correlationId, nameof(SceneSerializationManager), null);
                }
            }

            return sceneObject;
        }

        private async UniTask ApplySceneDataAsync(SceneData sceneData)
        {
            if (sceneData?.SceneObjects == null) return;

            await UniTask.SwitchToMainThread();

            foreach (var sceneObject in sceneData.SceneObjects)
            {
                try
                {
                    await ApplySerializableSceneObjectAsync(sceneObject);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to apply scene object {sceneObject.Key}: {ex.Message}", 
                        _correlationId, nameof(SceneSerializationManager), null);
                }

                // Yield periodically
                await UniTask.Yield();
            }
        }

        private async UniTask ApplySerializableSceneObjectAsync(SerializableSceneObject sceneObject)
        {
            if (!_sceneObjects.TryGetValue(sceneObject.Key, out var objectData) || objectData.GameObject == null)
                return;

            var gameObject = objectData.GameObject;

            // Apply GameObject data
            if (sceneObject.GameObjectData != null)
            {
                gameObject.SetActive(sceneObject.GameObjectData.IsActive);
                gameObject.name = sceneObject.GameObjectData.Name;
                gameObject.tag = sceneObject.GameObjectData.Tag;
                gameObject.layer = sceneObject.GameObjectData.Layer;
            }

            // Apply component data
            if (sceneObject.SerializableComponentData != null)
            {
                var serializableComponents = gameObject.GetComponents<ISerializable>();
                var componentMap = serializableComponents.ToDictionary(c => c.GetType().FullName, c => c);

                foreach (var componentData in sceneObject.SerializableComponentData)
                {
                    if (componentMap.TryGetValue(componentData.ComponentType, out var component))
                    {
                        try
                        {
                            await component.DeserializeAsync(componentData.SerializedData);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning($"Failed to deserialize component {componentData.ComponentType}: {ex.Message}", 
                                _correlationId, nameof(SceneSerializationManager), null);
                        }
                    }
                }
            }
        }

        private async UniTask SaveSceneDataToPersistentStorageAsync(byte[] data)
        {
            var key = GetSceneSaveKey();
            var base64Data = Convert.ToBase64String(data);

            await UniTask.SwitchToMainThread();
            PlayerPrefs.SetString(key, base64Data);
            PlayerPrefs.Save();
        }

        private async UniTask<byte[]> LoadSceneDataFromPersistentStorageAsync()
        {
            await UniTask.SwitchToMainThread();

            var key = GetSceneSaveKey();
            if (!PlayerPrefs.HasKey(key))
                return null;

            var base64Data = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(base64Data))
                return null;

            try
            {
                return Convert.FromBase64String(base64Data);
            }
            catch
            {
                return null;
            }
        }

        private SerializationConfig CreateSerializationConfig()
        {
            return new SerializationConfig
            {
                Format = SerializationFormat.MemoryPack,
                EnableCompression = true,
                EnableEncryption = false,
                ThreadingMode = SerializationThreadingMode.MultiThreaded,
                BufferPoolSize = 2 * 1024 * 1024, // 2MB for scene data
                MaxConcurrentOperations = _maxConcurrentSceneOperations
            };
        }

        private string GenerateObjectKey(GameObject gameObject)
        {
            return $"{gameObject.name}_{gameObject.GetInstanceID()}";
        }

        private string GetSceneSaveKey()
        {
            return $"SceneData_{SceneIdentifier}";
        }

        private int CountSerializableComponents()
        {
            return _sceneObjects.Values.AsValueEnumerable()
                .Where(obj => obj.GameObject != null)
                .Sum(obj => obj.GameObject.GetComponents<ISerializable>().Length);
        }

        private int EstimateSceneDataSize()
        {
            return _sceneObjects.Count * 500; // Rough estimate of 500 bytes per object
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene == _managedScene && _autoSaveOnSceneUnload)
            {
                _ = SaveSceneDataAsync();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene == _managedScene && _autoLoadOnSceneLoad)
            {
                _ = LoadSceneDataAsync();
            }
        }

        /// <summary>
        /// Manual save trigger for editor or debugging purposes.
        /// </summary>
        [ContextMenu("Save Scene Data")]
        public void SaveSceneDataManual()
        {
            if (Application.isPlaying)
            {
                _ = SaveSceneDataAsync();
            }
        }

        /// <summary>
        /// Manual load trigger for editor or debugging purposes.
        /// </summary>
        [ContextMenu("Load Scene Data")]
        public void LoadSceneDataManual()
        {
            if (Application.isPlaying)
            {
                _ = LoadSceneDataAsync();
            }
        }

        /// <summary>
        /// Manual snapshot trigger for editor or debugging purposes.
        /// </summary>
        [ContextMenu("Create Scene Snapshot")]
        public void CreateSceneSnapshotManual()
        {
            if (Application.isPlaying)
            {
                _ = CreateSceneStateSnapshotAsync();
            }
        }
    }

    /// <summary>
    /// MemoryPack-compatible structure for scene data.
    /// </summary>
    [MemoryPackable]
    public partial class SceneData
    {
        public string SceneIdentifier { get; set; }
        public string SceneName { get; set; }
        public int SceneBuildIndex { get; set; }
        public long Timestamp { get; set; }
        public List<SerializableSceneObject> SceneObjects { get; set; }
    }

    /// <summary>
    /// MemoryPack-compatible structure for serializable scene objects.
    /// </summary>
    [MemoryPackable]
    public partial class SerializableSceneObject
    {
        public string Key { get; set; }
        public GameObjectData GameObjectData { get; set; }
        public List<ComponentSerializationData> SerializableComponentData { get; set; }
    }

    /// <summary>
    /// MemoryPack-compatible structure for component serialization data.
    /// </summary>
    [MemoryPackable]
    public partial class ComponentSerializationData
    {
        public string ComponentType { get; set; }
        public byte[] SerializedData { get; set; }
    }

    /// <summary>
    /// Runtime data about tracked scene objects.
    /// </summary>
    public struct SceneObjectData
    {
        public GameObject GameObject;
        public int InstanceId;
        public string Name;
        public string Tag;
        public int Layer;
        public DateTime RegistrationTime;
    }

    /// <summary>
    /// Scene state snapshot for point-in-time restoration.
    /// </summary>
    public struct SceneStateSnapshot
    {
        public string SceneIdentifier;
        public DateTime Timestamp;
        public int ObjectCount;
        public SceneData SceneData;
    }

    /// <summary>
    /// Result structure for scene serialization operations.
    /// </summary>
    public struct SceneSerializationResult
    {
        public bool IsSuccess;
        public string SceneIdentifier;
        public int ObjectCount;
        public int DataSize;
        public TimeSpan ProcessingTime;
        public SceneOperationType OperationType;
        public string ErrorMessage;
    }

    /// <summary>
    /// Statistics about scene serialization system.
    /// </summary>
    public struct SceneStatistics
    {
        public string SceneIdentifier;
        public int TrackedObjects;
        public int SerializableComponents;
        public int DataManagers;
        public int CachedSnapshots;
        public DateTime? LastSnapshotTime;
        public int EstimatedDataSize;
        public bool IsInitialized;
    }

    /// <summary>
    /// Types of scene operations.
    /// </summary>
    public enum SceneOperationType
    {
        Save,
        Load,
        Snapshot,
        Restore
    }
}