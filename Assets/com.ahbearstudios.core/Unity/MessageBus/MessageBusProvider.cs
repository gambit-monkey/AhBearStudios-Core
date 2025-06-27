using System;
using AhBearStudios.Core.Bootstrap.Installers;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Factories;
using AhBearStudios.Core.MessageBus.Registration;
using AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe;
using AhBearStudios.Core.MessageBus.Services;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Profilers;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.MessageBus.Unity
{
    /// <summary>
    /// Unity component that provides message bus services with integrated profiling.
    /// Manages the message bus lifecycle using composition and dependency injection.
    /// Follows separation of concerns and uses only existing repository dependencies.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MessageBusProvider : MonoBehaviour, IDisposable
    {
        #region Serialized Fields
        [Header("Configuration")]
        [SerializeField] private MessageBusConfig _config;
        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private int _initialCapacity = 100;
        
        [Header("Profiling")]
        [SerializeField] private bool _enableProfiling = true;
        [SerializeField] private bool _enableNativeMetrics = true;
        [SerializeField] private bool _enableMessageBusProfiler = true;
        [SerializeField] private int _metricsCapacity = 256;
        #endregion

        #region Private Fields
        private IMessageBusService _messageBusService;
        private IMessageBusConfig _runtimeConfig;
        
        // Injected dependencies (existing types from repository)
        private IMessageDeliveryServiceFactory _deliveryServiceFactory;
        private IMessageSerializerFactory _serializerFactory;
        private IBurstLogger _logger;
        private IMessageRegistry _messageRegistry;
        private IProfiler _baseProfiler;
        private IMessageBusMetrics _busMetrics;
        private DeliveryServiceConfiguration _deliveryConfig;
        private IDeliveryStatistics _deliveryStatistics;
        private ISerializerMetrics _serializerMetrics;
        
        // Profiling components
        private MessageBusProfiler _messageBusProfiler;
        
        private bool _isInitialized;
        private bool _isDisposed;
        
        // Static instance for singleton access
        private static MessageBusProvider _instance;
        private static readonly object _lockObject = new object();
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the message bus instance.
        /// </summary>
        public IMessageBusService MessageBusService => _messageBusService;
        
        /// <summary>
        /// Gets the bus metrics instance.
        /// </summary>
        public IMessageBusMetrics BusMetrics => _busMetrics;
        
        /// <summary>
        /// Gets the message bus profiler instance.
        /// </summary>
        public MessageBusProfiler Profiler => _messageBusProfiler;
        
        /// <summary>
        /// Gets whether the provider is initialized and ready for use.
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;
        
        /// <summary>
        /// Gets the unique identifier of the message bus.
        /// </summary>
        public Guid Id => _messageBusService?.Id ?? Guid.Empty;
        
        /// <summary>
        /// Gets the name of the message bus.
        /// </summary>
        public string Name => _messageBusService?.Name ?? "MessageBusService";
        
        /// <summary>
        /// Gets the singleton instance of the message bus provider.
        /// </summary>
        public static MessageBusProvider Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (_lockObject)
                {
                    if (_instance != null)
                        return _instance;

                    // Try to find existing instance
                    _instance = FindObjectOfType<MessageBusProvider>();

                    if (_instance == null)
                    {
                        // Create new instance
                        var go = new GameObject("[MessageBusProvider]");
                        _instance = go.AddComponent<MessageBusProvider>();
                        DontDestroyOnLoad(go);
                    }

                    return _instance;
                }
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the provider is initialized.
        /// </summary>
        public event Action<MessageBusProvider> Initialized;
        
        /// <summary>
        /// Event raised when the provider is disposed.
        /// </summary>
        public event Action<MessageBusProvider> Disposed;
        #endregion

        #region Dependency Injection
        /// <summary>
        /// Constructor injection for dependencies using existing repository types.
        /// </summary>
        [Inject]
        public void Construct(
            IMessageDeliveryServiceFactory deliveryServiceFactory = null,
            IMessageSerializerFactory serializerFactory = null,
            IBurstLogger logger = null,
            IMessageRegistry messageRegistry = null,
            IProfiler baseProfiler = null,
            IMessageBusMetrics busMetrics = null,
            DeliveryServiceConfiguration deliveryConfig = null,
            IDeliveryStatistics deliveryStatistics = null,
            ISerializerMetrics serializerMetrics = null)
        {
            _deliveryServiceFactory = deliveryServiceFactory;
            _serializerFactory = serializerFactory;
            _logger = logger;
            _messageRegistry = messageRegistry;
            _baseProfiler = baseProfiler;
            _busMetrics = busMetrics;
            _deliveryConfig = deliveryConfig;
            _deliveryStatistics = deliveryStatistics;
            _serializerMetrics = serializerMetrics;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure singleton behavior
            if (!EnsureSingleton())
                return;

            if (_persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (_autoInitialize)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }

        private void OnValidate()
        {
            _initialCapacity = Mathf.Max(1, _initialCapacity);
            _metricsCapacity = Mathf.Max(16, _metricsCapacity);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the message bus with profiling integration.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized || _isDisposed)
                return;

            try
            {
                CreateOrResolveDependencies();
                CreateMessageBus();
                InitializeProfilingComponents();
                ConfigureProfilingAlerts();

                _isInitialized = true;
                Initialized?.Invoke(this);

                Debug.Log("[MessageBusProvider] Initialized successfully with profiling support");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageBusProvider] Failed to initialize: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets profiling metrics for the message bus.
        /// </summary>
        /// <returns>Current metrics data for the message bus.</returns>
        public MessageBusMetricsData GetMetrics()
        {
            if (_busMetrics == null || _messageBusService == null)
                return default;
                
            return _busMetrics.GetMetricsData(_messageBusService.Id);
        }

        /// <summary>
        /// Starts profiling if not already started.
        /// </summary>
        public void StartProfiling()
        {
            _messageBusProfiler?.StartProfiling();
        }

        /// <summary>
        /// Stops profiling and returns summary.
        /// </summary>
        public void StopProfiling()
        {
            _messageBusProfiler?.StopProfiling();
        }

        /// <summary>
        /// Resets profiling statistics.
        /// </summary>
        public void ResetProfilingStats()
        {
            _messageBusProfiler?.ResetStats();
            _busMetrics?.ResetBusStats(_messageBusService?.Id ?? Guid.Empty);
        }

        /// <summary>
        /// Disposes the message bus and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                // Dispose profiling components first
                if (_messageBusProfiler is IDisposable disposableProfiler)
                {
                    disposableProfiler.Dispose();
                }
                
                if (_busMetrics is IDisposable disposableMetrics)
                {
                    disposableMetrics.Dispose();
                }
                
                // Dispose message bus
                if (_messageBusService is IDisposable disposableBus)
                {
                    disposableBus.Dispose();
                }
                
                // Dispose other components
                if (_deliveryStatistics is IDisposable disposableStats)
                {
                    disposableStats.Dispose();
                }

                // Clear references
                ClearReferences();

                _isDisposed = true;
                _isInitialized = false;

                Disposed?.Invoke(this);

                Debug.Log("[MessageBusProvider] Disposed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageBusProvider] Error during disposal: {ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        private bool EnsureSingleton()
        {
            lock (_lockObject)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return false;
                }
                _instance = this;
                return true;
            }
        }

        private void CreateOrResolveDependencies()
        {
            // Create logger if not injected (existing type from repository)
            _logger ??= CreateDefaultLogger();

            // Create message registry if not injected (existing type from repository)
            _messageRegistry ??= CreateDefaultMessageRegistry();

            // Create profiler if not injected (existing type from repository)
            _baseProfiler ??= CreateProfiler();

            // Create delivery configuration if not injected (existing type from repository)
            _deliveryConfig ??= CreateDefaultDeliveryServiceConfiguration();

            // Create delivery statistics if not injected (existing type from repository)
            _deliveryStatistics ??= CreateDefaultDeliveryStatistics();

            // Create serializer metrics if not injected (existing type from repository)
            _serializerMetrics ??= CreateDefaultSerializerMetrics();

            // Create bus metrics if not injected (existing type from repository)
            _busMetrics ??= CreateBusMetrics();

            // Create factories if not injected (existing types from repository)
            CreateFactories();
        }

        private void CreateFactories()
        {
            // Create delivery service factory using existing implementation
            _deliveryServiceFactory ??= new MessageDeliveryServiceFactory(
                _logger,
                _messageRegistry,
                _baseProfiler,
                _deliveryConfig,
                _deliveryStatistics);

            // Create serializer factory using existing implementation
            _serializerFactory ??= new MessageSerializerFactory(
                _logger,
                _messageRegistry,
                _serializerMetrics);
        }

        private void CreateMessageBus()
        {
            // Use the configured MessageBusConfig or create a default one
            _runtimeConfig = _config != null ? _config : CreateDefaultConfig();

            // Use MessagePipeBusService - the actual implementation from the repository
            _messageBusService = new MessagePipeBusService(
                null, // IDependencyProvider - can be null for basic usage
                _logger,
                _baseProfiler,
                _messageRegistry);
        }

        private void InitializeProfilingComponents()
        {
            if (!_enableProfiling || !_enableMessageBusProfiler)
                return;

            if (_baseProfiler != null && _busMetrics != null && _messageBusService != null)
            {
                _messageBusProfiler = new MessageBusProfiler(_baseProfiler, _busMetrics, _messageBusService);
                
                Debug.Log("[MessageBusProvider] MessageBusProfiler created and configured");
            }

            // Configure bus metrics with the message bus instance
            if (_busMetrics != null && _messageBusService != null)
            {
                var busId = _messageBusService.Id;
                var busName = _messageBusService.Name ?? "MessageBusService";
                var busType = _messageBusService.GetType().Name;
                
                _busMetrics.UpdateBusConfiguration(busId, 0, 0, busName, busType);
                
                Debug.Log($"[MessageBusProvider] Bus metrics configured for {busName} ({busId})");
            }
        }

        private void ConfigureProfilingAlerts()
        {
            if (_messageBusProfiler == null) 
                return;
            
            // Configure typical performance thresholds
            _messageBusProfiler.RegisterBusMetricAlert(Guid.Empty, "DeliveryTime", 50.0); // 50ms delivery threshold
            _messageBusProfiler.RegisterBusMetricAlert(Guid.Empty, "QueueSize", 1000.0); // 1000 message queue threshold
            _messageBusProfiler.RegisterMessageTypeAlert("IMessage", 100.0); // 100ms per message type
            _messageBusProfiler.RegisterOperationAlert("Publish", 25.0); // 25ms publish threshold
            _messageBusProfiler.RegisterOperationAlert("Subscribe", 10.0); // 10ms subscribe threshold
        }

        private IBurstLogger CreateDefaultLogger()
        {
            // Use existing UnityBurstLogger from repository
            return new UnityBurstLogger();
        }

        private IMessageRegistry CreateDefaultMessageRegistry()
        {
            // Use existing DefaultMessageRegistry from repository
            return new DefaultMessageRegistry(_logger);
        }

        private IProfiler CreateProfiler()
        {
            if (!_enableProfiling)
                return new NullProfiler();
                
            // Use existing UnityProfiler from repository
            return new UnityProfiler();
        }

        private IMessageBusMetrics CreateBusMetrics()
        {
            if (!_enableProfiling)
                return new NullMessageBusMetrics();
                
            if (_enableNativeMetrics)
            {
                // Use existing NativeMessageBusMetrics from repository
                return new NativeMessageBusMetrics(_metricsCapacity, Allocator.Persistent);
            }
            else
            {
                // Use existing MessageBusMetrics from repository
                return new MessageBusMetrics(_messageBusService, _metricsCapacity);
            }
        }

        private DeliveryServiceConfiguration CreateDefaultDeliveryServiceConfiguration()
        {
            // Use existing DeliveryServiceConfiguration from repository
            return new DeliveryServiceConfiguration
            {
                MaxRetryAttempts = 3,
                RetryDelayMs = 100,
                EnableReliableDelivery = true,
                BatchSize = 50,
                MaxConcurrentOperations = Environment.ProcessorCount
            };
        }

        private IDeliveryStatistics CreateDefaultDeliveryStatistics()
        {
            // Use existing DeliveryStatistics from repository
            return new DeliveryStatistics();
        }

        private ISerializerMetrics CreateDefaultSerializerMetrics()
        {
            // Use existing SerializerMetrics from repository
            return new SerializerMetrics();
        }

        private IMessageBusConfig CreateDefaultConfig()
        {
            // Use existing MessageBusConfig from repository
            return new MessageBusConfig
            {
                Name = "DefaultMessageBusService",
                InitialCapacity = _initialCapacity,
                EnableReliableDelivery = true,
                EnableBatching = true,
                BatchSize = 50,
                FlushInterval = TimeSpan.FromMilliseconds(100)
            };
        }

        private void ClearReferences()
        {
            _deliveryServiceFactory = null;
            _serializerFactory = null;
            _runtimeConfig = null;
            _logger = null;
            _messageRegistry = null;
            _baseProfiler = null;
            _busMetrics = null;
            _messageBusProfiler = null;
            _deliveryConfig = null;
            _deliveryStatistics = null;
            _serializerMetrics = null;
            _messageBusService = null;
        }
        #endregion
    }
}