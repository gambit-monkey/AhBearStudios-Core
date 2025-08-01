using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Serialization.Jobs;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using MemoryPack;
using Reflex.Attributes;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Components
{
    /// <summary>
    /// Central manager for persistent data operations across the application.
    /// Provides centralized save/load functionality with automatic batching, compression,
    /// and coordination between multiple serializable components.
    /// </summary>
    [AddComponentMenu("AhBearStudios/Serialization/Persistent Data Manager")]
    public class PersistentDataManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private bool _enableAutomaticSaving = true;
        
        [SerializeField]
        private float _autoSaveInterval = 30f;
        
        [SerializeField]
        private int _maxBackupVersions = 5;
        
        [SerializeField]
        private bool _enableCompression = true;
        
        [SerializeField]
        private bool _enableEncryption = false;
        
        [SerializeField]
        private string _saveFilePrefix = "GameData";
        
        [Header("Performance")]
        [SerializeField]
        private int _maxConcurrentOperations = 4;
        
        [SerializeField]
        private bool _useBatchSerialization = true;
        
        [SerializeField]
        private int _batchSize = 10;

        [Inject]
        private ISerializationService _serializationService;
        
        [Inject]
        private ILoggingService _logger;

        private readonly Dictionary<string, ISerializable> _registeredComponents = new Dictionary<string, ISerializable>();
        private readonly List<PendingSaveOperation> _pendingSaveOperations = new List<PendingSaveOperation>();
        private readonly Dictionary<string, PersistentDataEntry> _dataCache = new Dictionary<string, PersistentDataEntry>();
        
        private float _lastAutoSaveTime;
        private bool _isInitialized = false;
        private bool _isSaving = false;
        private bool _isLoading = false;
        private FixedString64Bytes _correlationId;
        private UnitySerializationJobService _jobService;

        /// <summary>
        /// Event raised when a save operation completes successfully.
        /// </summary>
        public event Action<SaveOperationResult> OnSaveCompleted;

        /// <summary>
        /// Event raised when a load operation completes successfully.
        /// </summary>
        public event Action<LoadOperationResult> OnLoadCompleted;

        /// <summary>
        /// Event raised when a save or load operation fails.
        /// </summary>
        public event Action<string, Exception> OnOperationFailed;

        /// <summary>
        /// Event raised when automatic save triggers.
        /// </summary>
        public event Action OnAutoSaveTriggered;

        /// <summary>
        /// Gets whether automatic saving is enabled.
        /// </summary>
        public bool EnableAutomaticSaving
        {
            get => _enableAutomaticSaving;
            set => _enableAutomaticSaving = value;
        }

        /// <summary>
        /// Gets or sets the automatic save interval in seconds.
        /// </summary>
        public float AutoSaveInterval
        {
            get => _autoSaveInterval;
            set => _autoSaveInterval = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Gets whether the manager is currently performing a save operation.
        /// </summary>
        public bool IsSaving => _isSaving;

        /// <summary>
        /// Gets whether the manager is currently performing a load operation.
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Gets the number of registered serializable components.
        /// </summary>
        public int RegisteredComponentCount => _registeredComponents.Count;

        /// <summary>
        /// Gets the number of pending save operations.
        /// </summary>
        public int PendingSaveOperationCount => _pendingSaveOperations.Count;

        private void Awake()
        {
            _correlationId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
            
            if (_serializationService == null || _logger == null)
            {
                Debug.LogWarning("[PersistentDataManager] Dependency injection failed. Manager may not function correctly.", this);
            }
        }

        private async void Start()
        {
            await InitializeAsync();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Handle automatic saving
            if (_enableAutomaticSaving && !_isSaving && Time.time - _lastAutoSaveTime >= _autoSaveInterval)
            {
                _ = TriggerAutoSaveAsync();
            }

            // Process pending save operations
            if (_pendingSaveOperations.Count > 0 && !_isSaving)
            {
                _ = ProcessPendingSaveOperationsAsync();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _enableAutomaticSaving)
            {
                // Save immediately when application is paused
                _ = SaveAllDataAsync();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _enableAutomaticSaving)
            {
                // Save immediately when application loses focus
                _ = SaveAllDataAsync();
            }
        }

        private void OnDestroy()
        {
            if (_isInitialized && _enableAutomaticSaving)
            {
                // Final save attempt (synchronous)
                try
                {
                    SaveAllDataSync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PersistentDataManager] Failed to save data on destroy: {ex.Message}", this);
                }
            }

            _jobService?.Dispose();
        }

        /// <summary>
        /// Initializes the persistent data manager.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            try
            {
                _logger?.LogInfo("Initializing PersistentDataManager", _correlationId, nameof(PersistentDataManager), null);

                _jobService = new UnitySerializationJobService(_logger);
                
                // Auto-register all SerializableMonoBehaviour components in the scene
                await AutoRegisterComponentsAsync();
                
                _lastAutoSaveTime = Time.time;
                _isInitialized = true;

                _logger?.LogInfo($"PersistentDataManager initialized with {_registeredComponents.Count} components", 
                    _correlationId, nameof(PersistentDataManager), null);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to initialize PersistentDataManager: {ex.Message}", 
                    _correlationId, nameof(PersistentDataManager), null);
                OnOperationFailed?.Invoke("Initialization", ex);
            }
        }

        /// <summary>
        /// Registers a serializable component with the manager.
        /// </summary>
        /// <param name="key">Unique key for the component</param>
        /// <param name="component">The serializable component</param>
        public void RegisterComponent(string key, ISerializable component)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            _registeredComponents[key] = component;
            
            _logger?.LogInfo($"Registered component: {key}", _correlationId, nameof(PersistentDataManager), null);
        }

        /// <summary>
        /// Unregisters a serializable component from the manager.
        /// </summary>
        /// <param name="key">Key of the component to unregister</param>
        public void UnregisterComponent(string key)
        {
            if (_registeredComponents.Remove(key))
            {
                _logger?.LogInfo($"Unregistered component: {key}", _correlationId, nameof(PersistentDataManager), null);
            }
        }

        /// <summary>
        /// Saves data for a specific component asynchronously.
        /// </summary>
        /// <param name="key">Key of the component to save</param>
        /// <returns>UniTask containing the save result</returns>
        public async UniTask<SaveOperationResult> SaveComponentDataAsync(string key)
        {
            if (!_registeredComponents.TryGetValue(key, out var component))
            {
                var error = $"Component not found: {key}";
                _logger?.LogError(error, _correlationId, nameof(PersistentDataManager), null);
                return new SaveOperationResult { IsSuccess = false, ErrorMessage = error };
            }

            try
            {
                var result = await component.SerializeAsync();
                
                if (result.IsSuccess)
                {
                    await SaveDataToPersistentStorageAsync(key, result.Data);
                    
                    var saveResult = new SaveOperationResult
                    {
                        IsSuccess = true,
                        ComponentKey = key,
                        DataSize = result.Data.Length,
                        ProcessingTime = result.Statistics.ProcessingTime
                    };

                    OnSaveCompleted?.Invoke(saveResult);
                    return saveResult;
                }
                else
                {
                    OnOperationFailed?.Invoke(key, new InvalidOperationException(result.ErrorMessage));
                    return new SaveOperationResult { IsSuccess = false, ErrorMessage = result.ErrorMessage };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to save component data for {key}: {ex.Message}", 
                    _correlationId, nameof(PersistentDataManager), null);
                OnOperationFailed?.Invoke(key, ex);
                return new SaveOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Loads data for a specific component asynchronously.
        /// </summary>
        /// <param name="key">Key of the component to load</param>
        /// <returns>UniTask containing the load result</returns>
        public async UniTask<LoadOperationResult> LoadComponentDataAsync(string key)
        {
            if (!_registeredComponents.TryGetValue(key, out var component))
            {
                var error = $"Component not found: {key}";
                _logger?.LogError(error, _correlationId, nameof(PersistentDataManager), null);
                return new LoadOperationResult { IsSuccess = false, ErrorMessage = error };
            }

            try
            {
                var data = await LoadDataFromPersistentStorageAsync(key);
                
                if (data != null)
                {
                    var result = await component.DeserializeAsync(data);
                    
                    if (result.IsSuccess)
                    {
                        var loadResult = new LoadOperationResult
                        {
                            IsSuccess = true,
                            ComponentKey = key,
                            DataSize = data.Length,
                            ProcessingTime = result.Statistics.ProcessingTime
                        };

                        OnLoadCompleted?.Invoke(loadResult);
                        return loadResult;
                    }
                    else
                    {
                        OnOperationFailed?.Invoke(key, new InvalidOperationException(result.ErrorMessage));
                        return new LoadOperationResult { IsSuccess = false, ErrorMessage = result.ErrorMessage };
                    }
                }
                else
                {
                    return new LoadOperationResult { IsSuccess = false, ErrorMessage = "No data found" };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load component data for {key}: {ex.Message}", 
                    _correlationId, nameof(PersistentDataManager), null);
                OnOperationFailed?.Invoke(key, ex);
                return new LoadOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Saves all registered component data asynchronously.
        /// </summary>
        /// <returns>UniTask containing the batch save result</returns>
        public async UniTask<BatchOperationResult> SaveAllDataAsync()
        {
            if (_isSaving)
            {
                _logger?.LogWarning("Save operation already in progress", _correlationId, nameof(PersistentDataManager), null);
                return new BatchOperationResult { IsSuccess = false, ErrorMessage = "Save operation already in progress" };
            }

            _isSaving = true;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger?.LogInfo($"Starting batch save of {_registeredComponents.Count} components", 
                    _correlationId, nameof(PersistentDataManager), null);

                var results = new List<SaveOperationResult>();

                if (_useBatchSerialization && _registeredComponents.Count > _batchSize)
                {
                    results.AddRange(await SaveDataInBatchesAsync());
                }
                else
                {
                    foreach (var kvp in _registeredComponents)
                    {
                        var result = await SaveComponentDataAsync(kvp.Key);
                        results.Add(result);
                    }
                }

                var batchResult = new BatchOperationResult
                {
                    IsSuccess = results.All(r => r.IsSuccess),
                    SuccessfulOperations = results.Count(r => r.IsSuccess),
                    FailedOperations = results.Count(r => !r.IsSuccess),
                    TotalDataSize = results.Sum(r => r.DataSize),
                    ProcessingTime = DateTime.UtcNow - startTime
                };

                _logger?.LogInfo($"Batch save completed: {batchResult.SuccessfulOperations}/{results.Count} successful in {batchResult.ProcessingTime.TotalMilliseconds:F2}ms", 
                    _correlationId, nameof(PersistentDataManager), null);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Batch save failed: {ex.Message}", _correlationId, nameof(PersistentDataManager), null);
                OnOperationFailed?.Invoke("BatchSave", ex);
                return new BatchOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Loads all registered component data asynchronously.
        /// </summary>
        /// <returns>UniTask containing the batch load result</returns>
        public async UniTask<BatchOperationResult> LoadAllDataAsync()
        {
            if (_isLoading)
            {
                _logger?.LogWarning("Load operation already in progress", _correlationId, nameof(PersistentDataManager), null);
                return new BatchOperationResult { IsSuccess = false, ErrorMessage = "Load operation already in progress" };
            }

            _isLoading = true;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger?.LogInfo($"Starting batch load of {_registeredComponents.Count} components", 
                    _correlationId, nameof(PersistentDataManager), null);

                var results = new List<LoadOperationResult>();

                foreach (var kvp in _registeredComponents)
                {
                    var result = await LoadComponentDataAsync(kvp.Key);
                    results.Add(result);
                }

                var batchResult = new BatchOperationResult
                {
                    IsSuccess = results.All(r => r.IsSuccess),
                    SuccessfulOperations = results.Count(r => r.IsSuccess),
                    FailedOperations = results.Count(r => !r.IsSuccess),
                    TotalDataSize = results.Sum(r => r.DataSize),
                    ProcessingTime = DateTime.UtcNow - startTime
                };

                _logger?.LogInfo($"Batch load completed: {batchResult.SuccessfulOperations}/{results.Count} successful in {batchResult.ProcessingTime.TotalMilliseconds:F2}ms", 
                    _correlationId, nameof(PersistentDataManager), null);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Batch load failed: {ex.Message}", _correlationId, nameof(PersistentDataManager), null);
                OnOperationFailed?.Invoke("BatchLoad", ex);
                return new BatchOperationResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Queues a component for delayed saving.
        /// </summary>
        /// <param name="key">Key of the component to save</param>
        /// <param name="priority">Priority of the save operation</param>
        public void QueueSaveOperation(string key, SavePriority priority = SavePriority.Normal)
        {
            var operation = new PendingSaveOperation
            {
                ComponentKey = key,
                Priority = priority,
                QueueTime = DateTime.UtcNow
            };

            _pendingSaveOperations.Add(operation);
            
            // Sort by priority (High = 0, Normal = 1, Low = 2)
            _pendingSaveOperations.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// Clears all persistent data for all registered components.
        /// </summary>
        public void ClearAllPersistentData()
        {
            foreach (var key in _registeredComponents.Keys)
            {
                ClearPersistentData(key);
            }
            
            _dataCache.Clear();
            _logger?.LogInfo("All persistent data cleared", _correlationId, nameof(PersistentDataManager), null);
        }

        /// <summary>
        /// Clears persistent data for a specific component.
        /// </summary>
        /// <param name="key">Key of the component</param>
        public void ClearPersistentData(string key)
        {
            PlayerPrefs.DeleteKey(GetSaveKey(key));
            _dataCache.Remove(key);
            
            _logger?.LogInfo($"Persistent data cleared for: {key}", _correlationId, nameof(PersistentDataManager), null);
        }

        /// <summary>
        /// Gets statistics about the persistent data system.
        /// </summary>
        /// <returns>System statistics</returns>
        public PersistentDataStats GetSystemStats()
        {
            var totalSize = 0;
            var dataCount = 0;

            foreach (var key in _registeredComponents.Keys)
            {
                var saveKey = GetSaveKey(key);
                if (PlayerPrefs.HasKey(saveKey))
                {
                    var data = PlayerPrefs.GetString(saveKey);
                    if (!string.IsNullOrEmpty(data))
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(data);
                            totalSize += bytes.Length;
                            dataCount++;
                        }
                        catch
                        {
                            // Ignore corrupted entries
                        }
                    }
                }
            }

            return new PersistentDataStats
            {
                RegisteredComponents = _registeredComponents.Count,
                SavedDataEntries = dataCount,
                TotalDataSize = totalSize,
                PendingOperations = _pendingSaveOperations.Count,
                CacheSize = _dataCache.Count
            };
        }

        private async UniTask AutoRegisterComponentsAsync()
        {
            await UniTask.SwitchToMainThread();
            
            var serializableComponents = FindObjectsOfType<SerializableMonoBehaviour>();
            
            foreach (var component in serializableComponents)
            {
                var key = component.PersistentDataKey;
                if (!string.IsNullOrEmpty(key))
                {
                    RegisterComponent(key, component);
                }
            }
        }

        private async UniTask TriggerAutoSaveAsync()
        {
            _lastAutoSaveTime = Time.time;
            OnAutoSaveTriggered?.Invoke();
            
            _logger?.LogInfo("Auto-save triggered", _correlationId, nameof(PersistentDataManager), null);
            await SaveAllDataAsync();
        }

        private async UniTask ProcessPendingSaveOperationsAsync()
        {
            if (_pendingSaveOperations.Count == 0) return;

            var operations = _pendingSaveOperations.ToArray();
            _pendingSaveOperations.Clear();

            foreach (var operation in operations)
            {
                await SaveComponentDataAsync(operation.ComponentKey);
                
                // Yield control to prevent frame drops
                if (operations.Length > 5)
                {
                    await UniTask.Yield();
                }
            }
        }

        private async UniTask<List<SaveOperationResult>> SaveDataInBatchesAsync()
        {
            var results = new List<SaveOperationResult>();
            var components = _registeredComponents.ToArray();
            
            for (int i = 0; i < components.Length; i += _batchSize)
            {
                var batch = components.Skip(i).Take(_batchSize);
                var batchResults = await UniTask.WhenAll(batch.Select(kvp => SaveComponentDataAsync(kvp.Key)));
                results.AddRange(batchResults);
                
                // Brief pause between batches
                await UniTask.Delay(10);
            }

            return results;
        }

        private async UniTask SaveDataToPersistentStorageAsync(string key, byte[] data)
        {
            var saveKey = GetSaveKey(key);
            var base64Data = Convert.ToBase64String(data);
            
            // Cache the data
            _dataCache[key] = new PersistentDataEntry
            {
                Data = data,
                SaveTime = DateTime.UtcNow,
                Key = key
            };

            await UniTask.SwitchToMainThread();
            PlayerPrefs.SetString(saveKey, base64Data);
            PlayerPrefs.Save();
        }

        private async UniTask<byte[]> LoadDataFromPersistentStorageAsync(string key)
        {
            // Check cache first
            if (_dataCache.TryGetValue(key, out var cachedEntry))
            {
                return cachedEntry.Data;
            }

            await UniTask.SwitchToMainThread();
            
            var saveKey = GetSaveKey(key);
            if (!PlayerPrefs.HasKey(saveKey))
                return null;

            var base64Data = PlayerPrefs.GetString(saveKey);
            if (string.IsNullOrEmpty(base64Data))
                return null;

            try
            {
                var data = Convert.FromBase64String(base64Data);
                
                // Update cache
                _dataCache[key] = new PersistentDataEntry
                {
                    Data = data,
                    SaveTime = DateTime.UtcNow,
                    Key = key
                };

                return data;
            }
            catch
            {
                return null;
            }
        }

        private void SaveAllDataSync()
        {
            // Synchronous fallback for critical save scenarios
            foreach (var kvp in _registeredComponents)
            {
                try
                {
                    // This would need to be implemented as a synchronous operation
                    // For now, it's a placeholder
                    _logger?.LogWarning($"Synchronous save not fully implemented for: {kvp.Key}", 
                        _correlationId, nameof(PersistentDataManager), null);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to save {kvp.Key} synchronously: {ex.Message}", 
                        _correlationId, nameof(PersistentDataManager), null);
                }
            }
        }

        private string GetSaveKey(string componentKey)
        {
            return $"{_saveFilePrefix}_{componentKey}";
        }

        /// <summary>
        /// Manual save trigger for editor or debugging purposes.
        /// </summary>
        [ContextMenu("Save All Data")]
        public void SaveAllDataManual()
        {
            if (Application.isPlaying)
            {
                _ = SaveAllDataAsync();
            }
        }

        /// <summary>
        /// Manual load trigger for editor or debugging purposes.
        /// </summary>
        [ContextMenu("Load All Data")]
        public void LoadAllDataManual()
        {
            if (Application.isPlaying)
            {
                _ = LoadAllDataAsync();
            }
        }

        /// <summary>
        /// Manual clear trigger for editor or debugging purposes.
        /// </summary>
        [ContextMenu("Clear All Data")]
        public void ClearAllDataManual()
        {
            ClearAllPersistentData();
        }
    }

    /// <summary>
    /// Result structure for save operations.
    /// </summary>
    public struct SaveOperationResult
    {
        public bool IsSuccess;
        public string ComponentKey;
        public int DataSize;
        public TimeSpan ProcessingTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Result structure for load operations.
    /// </summary>
    public struct LoadOperationResult
    {
        public bool IsSuccess;
        public string ComponentKey;
        public int DataSize;
        public TimeSpan ProcessingTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Result structure for batch operations.
    /// </summary>
    public struct BatchOperationResult
    {
        public bool IsSuccess;
        public int SuccessfulOperations;
        public int FailedOperations;
        public int TotalDataSize;
        public TimeSpan ProcessingTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Statistics about the persistent data system.
    /// </summary>
    public struct PersistentDataStats
    {
        public int RegisteredComponents;
        public int SavedDataEntries;
        public int TotalDataSize;
        public int PendingOperations;
        public int CacheSize;
    }

    /// <summary>
    /// Pending save operation information.
    /// </summary>
    public struct PendingSaveOperation
    {
        public string ComponentKey;
        public SavePriority Priority;
        public DateTime QueueTime;
    }

    /// <summary>
    /// Cached persistent data entry.
    /// </summary>
    public struct PersistentDataEntry
    {
        public byte[] Data;
        public DateTime SaveTime;
        public string Key;
    }

    /// <summary>
    /// Priority levels for save operations.
    /// </summary>
    public enum SavePriority
    {
        High = 0,
        Normal = 1,
        Low = 2
    }
}