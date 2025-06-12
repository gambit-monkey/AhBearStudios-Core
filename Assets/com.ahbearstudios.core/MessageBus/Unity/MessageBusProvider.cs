using System;
using AhBearStudios.Core.Bootstrap.Installers;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Factories;
using AhBearStudios.Core.MessageBus.Services;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Profilers;
using AhBearStudios.Core.Profiling.Metrics;
using Unity.Collections;
using UnityEngine;

namespace AhBearStudios.Core.MessageBus.Unity
{
    /// <summary>
    /// Unity component that provides message bus services with integrated profiling.
    /// Manages the message bus lifecycle and provides access to it.
    /// </summary>
    public class MessageBusProvider : MonoBehaviour, IDisposable
    {
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
        
        private IMessageBus _messageBus;
        private IMessageBusConfig _runtimeConfig;
        private IMessageDeliveryServiceFactory _deliveryServiceFactory;
        private IMessageSerializerFactory _serializerFactory;
        
        // Profiling components
        private IProfiler _baseProfiler;
        private IMessageBusMetrics _busMetrics;
        private MessageBusProfiler _messageBusProfiler;
        
        private bool _isInitialized;
        private bool _isDisposed;
        
        // Static instance for singleton access
        private static MessageBusProvider _instance;
        private static readonly object _lock = new object();
        
        public IMessageBus MessageBus => _messageBus;
        public IMessageBusMetrics BusMetrics => _busMetrics;
        public MessageBusProfiler Profiler => _messageBusProfiler;
        public bool IsInitialized => _isInitialized && !_isDisposed;
        
        public event Action<MessageBusProvider> Initialized;
        public event Action<MessageBusProvider> Disposed;
        
        [Inject]
        public void Construct(
            IMessageDeliveryServiceFactory deliveryServiceFactory = null,
            IMessageSerializerFactory serializerFactory = null,
            IProfiler baseProfiler = null,
            IMessageBusMetrics busMetrics = null)
        {
            _deliveryServiceFactory = deliveryServiceFactory;
            _serializerFactory = serializerFactory;
            _baseProfiler = baseProfiler;
            _busMetrics = busMetrics;
        }
        
        private void Awake()
        {
            // Ensure singleton behavior
            lock (_lock)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                _instance = this;
            }

            if (_persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (_autoInitialize)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// Ensures factories and profiling components are available
        /// </summary>
        private void EnsureFactories()
        {
            // Create delivery service factory if not injected
            if (_deliveryServiceFactory == null)
            {
                var logger = CreateDefaultLogger();
                var registry = CreateDefaultMessageRegistry();
                var profiler = GetOrCreateProfiler();
                var config = CreateDefaultDeliveryServiceConfiguration();
                var statistics = CreateDefaultStatisticsTracker();
                
                _deliveryServiceFactory = new MessageDeliveryServiceFactory(
                    logger, registry, profiler, config, statistics);
            }

            // Create serializer factory if not injected
            if (_serializerFactory == null)
            {
                var logger = CreateDefaultLogger();
                var registry = CreateDefaultMessageRegistry();
                var metrics = CreateDefaultSerializerMetrics();
                
                _serializerFactory = new MessageSerializerFactory(logger, registry, metrics);
            }
        }
        
        /// <summary>
        /// Gets or creates the base profiler
        /// </summary>
        private IProfiler GetOrCreateProfiler()
        {
            if (_baseProfiler != null)
                return _baseProfiler;
                
            if (!_enableProfiling)
                return CreateNullProfiler();
                
            return CreateDefaultProfiler();
        }
        
        /// <summary>
        /// Gets or creates the message bus metrics
        /// </summary>
        private IMessageBusMetrics GetOrCreateBusMetrics()
        {
            if (_busMetrics != null)
                return _busMetrics;
                
            if (!_enableProfiling)
                return CreateNullMetrics();
                
            if (_enableNativeMetrics)
            {
                return new NativeMessageBusMetrics(_metricsCapacity, Allocator.Persistent);
            }
            else
            {
                return new MessageBusMetrics(_messageBus, _metricsCapacity);
            }
        }
        
        /// <summary>
        /// Creates the message bus profiler if enabled
        /// </summary>
        private void CreateMessageBusProfiler()
        {
            if (!_enableProfiling || !_enableMessageBusProfiler)
                return;
                
            var baseProfiler = GetOrCreateProfiler();
            var busMetrics = GetOrCreateBusMetrics();
            
            if (baseProfiler != null && busMetrics != null && _messageBus != null)
            {
                _messageBusProfiler = new MessageBusProfiler(baseProfiler, busMetrics, _messageBus);
                
                // Configure common alert thresholds
                ConfigureProfilerAlerts();
                
                Debug.Log("[MessageBusProvider] MessageBusProfiler created and configured");
            }
        }
        
        /// <summary>
        /// Configures common profiler alert thresholds
        /// </summary>
        private void ConfigureProfilerAlerts()
        {
            if (_messageBusProfiler == null) return;
            
            // Configure typical performance thresholds
            _messageBusProfiler.RegisterBusMetricAlert(Guid.Empty, "DeliveryTime", 50.0); // 50ms delivery threshold
            _messageBusProfiler.RegisterBusMetricAlert(Guid.Empty, "QueueSize", 1000.0); // 1000 message queue threshold
            _messageBusProfiler.RegisterMessageTypeAlert("IMessage", 100.0); // 100ms per message type
            _messageBusProfiler.RegisterOperationAlert("Publish", 25.0); // 25ms publish threshold
            _messageBusProfiler.RegisterOperationAlert("Subscribe", 10.0); // 10ms subscribe threshold
        }
        
        /// <summary>
        /// Creates a default logger for factory dependencies
        /// </summary>
        private IBurstLogger CreateDefaultLogger()
        {
            return new UnityBurstLogger();
        }
        
        private IMessageRegistry CreateDefaultMessageRegistry()
        {
            return new MessageRegistry();
        }
        
        private IProfiler CreateDefaultProfiler()
        {
            return new UnityProfiler();
        }
        
        private IProfiler CreateNullProfiler()
        {
            return new NullProfiler();
        }
        
        private IMessageBusMetrics CreateNullMetrics()
        {
            return new NullMessageBusMetrics();
        }
        
        private DeliveryServiceConfiguration CreateDefaultDeliveryServiceConfiguration()
        {
            return new DeliveryServiceConfiguration
            {
                MaxRetryAttempts = 3,
                RetryDelayMs = 100,
                EnableReliableDelivery = true,
                BatchSize = 50,
                MaxConcurrentOperations = Environment.ProcessorCount
            };
        }
        
        private IDeliveryStatistics CreateDefaultStatisticsTracker()
        {
            return new DeliveryStatistics();
        }
        
        private ISerializerMetrics CreateDefaultSerializerMetrics()
        {
            return new SerializerMetrics();
        }
        
        /// <summary>
        /// Initializes the message bus with profiling integration
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized || _isDisposed)
                return;

            try
            {
                EnsureFactories();

                // Use the configured MessageBusConfig or create a default one
                _runtimeConfig = _config != null ? _config : CreateDefaultConfig();

                // Create the message bus using the proper implementation
                _messageBus = CreateMessageBus(_runtimeConfig);
                
                // Initialize profiling components after message bus creation
                _baseProfiler = GetOrCreateProfiler();
                _busMetrics = GetOrCreateBusMetrics();
                
                // Create the specialized message bus profiler
                CreateMessageBusProfiler();
                
                // Configure bus metrics with the message bus instance
                if (_busMetrics != null && _messageBus != null)
                {
                    var busId = _messageBus.Id;
                    var busName = _messageBus.Name ?? "MessageBus";
                    var busType = _messageBus.GetType().Name;
                    
                    _busMetrics.UpdateBusConfiguration(busId, 0, 0, busName, busType);
                    
                    Debug.Log($"[MessageBusProvider] Bus metrics configured for {busName} ({busId})");
                }

                _isInitialized = true;
                Initialized?.Invoke(this);

                Debug.Log("[MessageBusProvider] Initialized successfully with profiling support");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageBusProvider] Failed to initialize: {ex.Message}");
                Debug.LogException(ex);
            }
        }
        
        private IMessageBus CreateMessageBus(IMessageBusConfig config)
        {
            return new MessageBus.MessageBuses.MessageBus(
                config,
                _deliveryServiceFactory,
                _serializerFactory);
        }
        
        private IMessageBusConfig CreateDefaultConfig()
        {
            return new MessageBusConfig
            {
                Name = "DefaultMessageBus",
                InitialCapacity = _initialCapacity,
                EnableReliableDelivery = true,
                EnableBatching = true,
                BatchSize = 50,
                FlushInterval = TimeSpan.FromMilliseconds(100)
            };
        }
        
        public static MessageBusProvider Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (_lock)
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
        
        /// <summary>
        /// Gets profiling metrics for the message bus
        /// </summary>
        public MessageBusMetricsData GetMetrics()
        {
            if (_busMetrics == null || _messageBus == null)
                return default;
                
            return _busMetrics.GetMetricsData(_messageBus.Id);
        }
        
        /// <summary>
        /// Starts profiling if not already started
        /// </summary>
        public void StartProfiling()
        {
            _messageBusProfiler?.StartProfiling();
        }
        
        /// <summary>
        /// Stops profiling and returns summary
        /// </summary>
        public void StopProfiling()
        {
            _messageBusProfiler?.StopProfiling();
        }
        
        /// <summary>
        /// Resets profiling statistics
        /// </summary>
        public void ResetProfilingStats()
        {
            _messageBusProfiler?.ResetStats();
        }
        
        /// <summary>
        /// Disposes the message bus and cleans up resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                // Dispose profiling components first
                _messageBusProfiler?.Dispose();
                
                if (_busMetrics is IDisposable disposableMetrics)
                {
                    disposableMetrics.Dispose();
                }
                
                // Dispose message bus
                _messageBus?.Dispose();
                
                // Clear references
                _deliveryServiceFactory = null;
                _serializerFactory = null;
                _runtimeConfig = null;
                _baseProfiler = null;
                _busMetrics = null;
                _messageBusProfiler = null;

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
    }
}