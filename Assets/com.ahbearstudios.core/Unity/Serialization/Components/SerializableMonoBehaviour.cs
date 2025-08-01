using System;
using UnityEngine;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Serialization.Jobs;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Reflex.Attributes;

namespace AhBearStudios.Unity.Serialization.Components
{
    /// <summary>
    /// Base class for MonoBehaviour components that require high-performance serialization.
    /// Provides automatic serialization/deserialization using the AhBearStudios serialization system
    /// with UniTask async operations and zero-allocation performance optimizations.
    /// </summary>
    public abstract class SerializableMonoBehaviour : MonoBehaviour, ISerializable
    {
        [SerializeField, HideInInspector]
        private string _persistentDataKey;
        
        [SerializeField]
        private bool _autoSaveOnDestroy = true;
        
        [SerializeField]
        private bool _autoLoadOnStart = true;
        
        [SerializeField]
        private SerializationFormat _serializationFormat = SerializationFormat.MemoryPack;
        
        [SerializeField]
        private bool _useCompression = false;
        
        [SerializeField]
        private bool _useEncryption = false;

        [Inject]
        private ISerializationService _serializationService;
        
        [Inject]
        private ILoggingService _logger;

        private bool _isInitialized = false;
        private FixedString64Bytes _correlationId;

        /// <summary>
        /// Gets or sets the persistent data key used for saving/loading this component's data.
        /// If not set, uses the GameObject's instance ID and component type.
        /// </summary>
        public string PersistentDataKey
        {
            get
            {
                if (string.IsNullOrEmpty(_persistentDataKey))
                {
                    _persistentDataKey = $"{gameObject.GetInstanceID()}_{GetType().Name}";
                }
                return _persistentDataKey;
            }
            set => _persistentDataKey = value;
        }

        /// <summary>
        /// Gets the serialization format used by this component.
        /// </summary>
        public SerializationFormat SerializationFormat => _serializationFormat;

        /// <summary>
        /// Gets whether compression is enabled for this component.
        /// </summary>
        public bool UseCompression => _useCompression;

        /// <summary>
        /// Gets whether encryption is enabled for this component.
        /// </summary>
        public bool UseEncryption => _useEncryption;

        /// <summary>
        /// Event raised when the component's data is successfully serialized.
        /// </summary>
        public event Action<SerializationResult> OnDataSerialized;

        /// <summary>
        /// Event raised when the component's data is successfully deserialized.
        /// </summary>
        public event Action<SerializationResult> OnDataDeserialized;

        /// <summary>
        /// Event raised when serialization or deserialization fails.
        /// </summary>
        public event Action<Exception> OnSerializationError;

        protected virtual void Awake()
        {
            _correlationId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..32]);
            
            if (_serializationService == null || _logger == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Dependency injection failed. SerializableMonoBehaviour may not function correctly.", this);
            }
        }

        protected virtual async void Start()
        {
            if (_autoLoadOnStart)
            {
                await LoadDataAsync();
            }
            
            _isInitialized = true;
        }

        protected virtual void OnDestroy()
        {
            if (_autoSaveOnDestroy && _isInitialized)
            {
                // Fire and forget save operation
                _ = SaveDataAsync();
            }
        }

        /// <summary>
        /// Serializes the component's data asynchronously using the configured serialization settings.
        /// </summary>
        /// <returns>UniTask containing the serialization result</returns>
        public virtual async UniTask<SerializationResult> SerializeAsync()
        {
            try
            {
                _logger?.LogInfo($"Starting serialization for {GetType().Name}", _correlationId, GetType().FullName, null);

                var data = GetSerializableData();
                if (data == null)
                {
                    var emptyResult = new SerializationResult
                    {
                        IsSuccess = true,
                        Data = new byte[0],
                        Statistics = new SerializationStatistics
                        {
                            BytesProcessed = 0,
                            ProcessingTime = TimeSpan.Zero,
                            CompressionRatio = 1.0,
                            Format = _serializationFormat
                        }
                    };
                    
                    OnDataSerialized?.Invoke(emptyResult);
                    return emptyResult;
                }

                var config = CreateSerializationConfig();
                var result = await _serializationService.SerializeAsync(data, config);

                if (result.IsSuccess)
                {
                    _logger?.LogInfo($"Serialization completed for {GetType().Name}: {result.Statistics.BytesProcessed} bytes in {result.Statistics.ProcessingTime.TotalMilliseconds:F2}ms", 
                        _correlationId, GetType().FullName, null);
                    OnDataSerialized?.Invoke(result);
                }
                else
                {
                    _logger?.LogError($"Serialization failed for {GetType().Name}: {result.ErrorMessage}", 
                        _correlationId, GetType().FullName, null);
                    OnSerializationError?.Invoke(new InvalidOperationException(result.ErrorMessage));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Serialization exception for {GetType().Name}: {ex.Message}", 
                    _correlationId, GetType().FullName, null);
                OnSerializationError?.Invoke(ex);
                
                return new SerializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = new byte[0]
                };
            }
        }

        /// <summary>
        /// Deserializes the component's data asynchronously from the provided byte array.
        /// </summary>
        /// <param name="data">The serialized data to deserialize</param>
        /// <returns>UniTask containing the deserialization result</returns>
        public virtual async UniTask<SerializationResult> DeserializeAsync(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    _logger?.LogWarning($"No data provided for deserialization of {GetType().Name}", 
                        _correlationId, GetType().FullName, null);
                    return new SerializationResult { IsSuccess = false, ErrorMessage = "No data provided" };
                }

                _logger?.LogInfo($"Starting deserialization for {GetType().Name}: {data.Length} bytes", 
                    _correlationId, GetType().FullName, null);

                var config = CreateSerializationConfig();
                var result = await _serializationService.DeserializeAsync<object>(data, config);

                if (result.IsSuccess && result.Data != null)
                {
                    await SetSerializableDataAsync(result.Data);
                    
                    _logger?.LogInfo($"Deserialization completed for {GetType().Name} in {result.Statistics.ProcessingTime.TotalMilliseconds:F2}ms", 
                        _correlationId, GetType().FullName, null);
                    OnDataDeserialized?.Invoke(result);
                }
                else
                {
                    _logger?.LogError($"Deserialization failed for {GetType().Name}: {result.ErrorMessage}", 
                        _correlationId, GetType().FullName, null);
                    OnSerializationError?.Invoke(new InvalidOperationException(result.ErrorMessage));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Deserialization exception for {GetType().Name}: {ex.Message}", 
                    _correlationId, GetType().FullName, null);
                OnSerializationError?.Invoke(ex);
                
                return new SerializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Saves the component's data to persistent storage using the configured key.
        /// </summary>
        /// <returns>UniTask that completes when the save operation finishes</returns>
        public virtual async UniTask SaveDataAsync()
        {
            try
            {
                var result = await SerializeAsync();
                if (result.IsSuccess)
                {
                    // Save to PlayerPrefs as Base64 for simplicity
                    // In a production system, this might use a more sophisticated storage mechanism
                    var base64Data = Convert.ToBase64String(result.Data);
                    PlayerPrefs.SetString(PersistentDataKey, base64Data);
                    PlayerPrefs.Save();
                    
                    _logger?.LogInfo($"Data saved for {GetType().Name} with key: {PersistentDataKey}", 
                        _correlationId, GetType().FullName, null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to save data for {GetType().Name}: {ex.Message}", 
                    _correlationId, GetType().FullName, null);
                OnSerializationError?.Invoke(ex);
            }
        }

        /// <summary>
        /// Loads the component's data from persistent storage using the configured key.
        /// </summary>
        /// <returns>UniTask that completes when the load operation finishes</returns>
        public virtual async UniTask LoadDataAsync()
        {
            try
            {
                if (!PlayerPrefs.HasKey(PersistentDataKey))
                {
                    _logger?.LogInfo($"No saved data found for {GetType().Name} with key: {PersistentDataKey}", 
                        _correlationId, GetType().FullName, null);
                    return;
                }

                var base64Data = PlayerPrefs.GetString(PersistentDataKey);
                if (string.IsNullOrEmpty(base64Data))
                {
                    _logger?.LogWarning($"Empty data found for {GetType().Name} with key: {PersistentDataKey}", 
                        _correlationId, GetType().FullName, null);
                    return;
                }

                var data = Convert.FromBase64String(base64Data);
                await DeserializeAsync(data);
                
                _logger?.LogInfo($"Data loaded for {GetType().Name} with key: {PersistentDataKey}", 
                    _correlationId, GetType().FullName, null);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load data for {GetType().Name}: {ex.Message}", 
                    _correlationId, GetType().FullName, null);
                OnSerializationError?.Invoke(ex);
            }
        }

        /// <summary>
        /// Clears the persistent data for this component.
        /// </summary>
        public virtual void ClearPersistentData()
        {
            if (PlayerPrefs.HasKey(PersistentDataKey))
            {
                PlayerPrefs.DeleteKey(PersistentDataKey);
                PlayerPrefs.Save();
                
                _logger?.LogInfo($"Persistent data cleared for {GetType().Name} with key: {PersistentDataKey}", 
                    _correlationId, GetType().FullName, null);
            }
        }

        /// <summary>
        /// Gets the current size of the persistent data in bytes.
        /// </summary>
        /// <returns>Size in bytes, or 0 if no data exists</returns>
        public virtual int GetPersistentDataSize()
        {
            if (!PlayerPrefs.HasKey(PersistentDataKey))
                return 0;

            var base64Data = PlayerPrefs.GetString(PersistentDataKey);
            if (string.IsNullOrEmpty(base64Data))
                return 0;

            try
            {
                var data = Convert.FromBase64String(base64Data);
                return data.Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Override this method to provide the data that should be serialized.
        /// </summary>
        /// <returns>The object to serialize, or null if no data should be saved</returns>
        protected abstract object GetSerializableData();

        /// <summary>
        /// Override this method to handle deserialized data.
        /// This method is called on the main thread after successful deserialization.
        /// </summary>
        /// <param name="data">The deserialized data object</param>
        /// <returns>UniTask that completes when the data has been applied</returns>
        protected abstract UniTask SetSerializableDataAsync(object data);

        /// <summary>
        /// Creates the serialization configuration for this component.
        /// Override to customize serialization settings.
        /// </summary>
        /// <returns>Serialization configuration</returns>
        protected virtual SerializationConfig CreateSerializationConfig()
        {
            return new SerializationConfig
            {
                Format = _serializationFormat,
                EnableCompression = _useCompression,
                EnableEncryption = _useEncryption,
                ThreadingMode = SerializationThreadingMode.MultiThreaded,
                BufferPoolSize = 1024 * 1024, // 1MB default
                MaxConcurrentOperations = 4
            };
        }

        /// <summary>
        /// Validates that the component is properly configured for serialization.
        /// </summary>
        /// <returns>True if the component is valid for serialization</returns>
        public virtual bool ValidateSerializationSetup()
        {
            if (_serializationService == null)
            {
                Debug.LogError($"[{GetType().Name}] SerializationService is not injected. Ensure Reflex DI is properly configured.", this);
                return false;
            }

            if (string.IsNullOrEmpty(PersistentDataKey))
            {
                Debug.LogWarning($"[{GetType().Name}] PersistentDataKey is empty. Using default key.", this);
            }

            return true;
        }

        /// <summary>
        /// Manual method to trigger data save (for use in editor or specific scenarios).
        /// </summary>
        [ContextMenu("Save Data")]
        public void SaveDataManual()
        {
            if (Application.isPlaying)
            {
                _ = SaveDataAsync();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Manual save can only be performed in play mode.", this);
            }
        }

        /// <summary>
        /// Manual method to trigger data load (for use in editor or specific scenarios).
        /// </summary>
        [ContextMenu("Load Data")]
        public void LoadDataManual()
        {
            if (Application.isPlaying)
            {
                _ = LoadDataAsync();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Manual load can only be performed in play mode.", this);
            }
        }

        /// <summary>
        /// Manual method to clear persistent data (for use in editor or specific scenarios).
        /// </summary>
        [ContextMenu("Clear Persistent Data")]
        public void ClearPersistentDataManual()
        {
            ClearPersistentData();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to validate the component configuration.
        /// </summary>
        [ContextMenu("Validate Configuration")]
        public void ValidateConfigurationEditor()
        {
            var isValid = ValidateSerializationSetup();
            var message = isValid ? "✓ Component configuration is valid." : "✗ Component configuration has issues.";
            Debug.Log($"[{GetType().Name}] {message}", this);
        }

        /// <summary>
        /// Editor-only method to show component information.
        /// </summary>
        [ContextMenu("Show Component Info")]
        public void ShowComponentInfoEditor()
        {
            Debug.Log($"[{GetType().Name}] Component Information:\n" +
                     $"Persistent Data Key: {PersistentDataKey}\n" +
                     $"Serialization Format: {_serializationFormat}\n" +
                     $"Use Compression: {_useCompression}\n" +
                     $"Use Encryption: {_useEncryption}\n" +
                     $"Auto Save on Destroy: {_autoSaveOnDestroy}\n" +
                     $"Auto Load on Start: {_autoLoadOnStart}\n" +
                     $"Persistent Data Size: {GetPersistentDataSize()} bytes", this);
        }
#endif
    }
}