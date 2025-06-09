
using System;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Configuration;
using AhBearStudios.Core.MessageBus.Factories;
using AhBearStudios.Core.MessageBus.MessageBuses;
using AhBearStudios.Core.Profiling.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.MessageBus.Unity
{
    /// <summary>
    /// Unity component that provides message bus services.
    /// Manages the message bus lifecycle and provides access to it.
    /// </summary>
    public class MessageBusProvider : MonoBehaviour, IDisposable
    {
        [Header("Configuration")] [SerializeField]
        private MessageBusConfig _config;

        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private int _initialCapacity = 100;

        private IMessageBus _messageBus;
        private IMessageBusConfig _runtimeConfig;
        private IMessageDeliveryServiceFactory _deliveryServiceFactory;
        private IMessageSerializerFactory _serializerFactory;
        private bool _isInitialized;
        private bool _isDisposed;

        // Static instance for singleton access
        private static MessageBusProvider _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the message bus instance
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Gets whether the message bus is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;

        /// <summary>
        /// Event fired when the message bus is initialized
        /// </summary>
        public event Action<MessageBusProvider> Initialized;

        /// <summary>
        /// Event fired when the message bus is disposed
        /// </summary>
        public event Action<MessageBusProvider> Disposed;

        [Inject]
        public void Construct(
            IMessageDeliveryServiceFactory deliveryServiceFactory = null,
            IMessageSerializerFactory serializerFactory = null)
        {
            _deliveryServiceFactory = deliveryServiceFactory;
            _serializerFactory = serializerFactory;
        }

        private void Awake()
        {
            // Ensure singleton behavior
            lock (_lock)
            {
                if (_instance != null && _instance != this)
                {
                    Debug.LogWarning("[MessageBusProvider] Multiple instances detected. Destroying duplicate.");
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
        /// Ensures factories are available, creating them with minimal dependencies if needed
        /// </summary>
        private void EnsureFactories()
        {
            // Create delivery service factory if not injected
            if (_deliveryServiceFactory == null)
            {
                Debug.LogWarning(
                    "[MessageBusProvider] MessageDeliveryServiceFactory not injected. Creating with default dependencies.");

                // Create minimal dependencies for MessageDeliveryServiceFactory
                var logger = CreateDefaultLogger();
                var messageRegistry = CreateDefaultMessageRegistry();
                var statisticsTracker = CreateDefaultStatisticsTracker();
                var config = _runtimeConfig ?? CreateDefaultConfig();

                _deliveryServiceFactory =
                    new MessageDeliveryServiceFactory(logger, messageRegistry, statisticsTracker, config);
            }

            // Create serializer factory if not injected
            if (_serializerFactory == null)
            {
                Debug.LogWarning(
                    "[MessageBusProvider] MessageSerializerFactory not injected. Creating with default dependencies.");

                // Create minimal dependencies for MessageSerializerFactory
                var logger = CreateDefaultLogger();
                var messageRegistry = CreateDefaultMessageRegistry();
                var metrics = CreateDefaultSerializerMetrics();

                _serializerFactory = new MessageSerializerFactory(logger, messageRegistry, metrics);
            }
        }

        /// <summary>
        /// Creates a default logger for factory dependencies
        /// </summary>
        private IBurstLogger CreateDefaultLogger()
        {
            // Create a simple Unity logger implementation
            return new UnityBurstLogger();
        }

        /// <summary>
        /// Creates a default message registry for factory dependencies
        /// </summary>
        private IMessageRegistry CreateDefaultMessageRegistry()
        {
            // Create a basic message registry
            var registry = new MessageRegistry();
            registry.DiscoverMessages(); // Auto-discover message types
            return registry;
        }

        /// <summary>
        /// Creates a default statistics tracker for delivery service factory
        /// </summary>
        private IDeliveryStatistics CreateDefaultStatisticsTracker()
        {
            // Return a basic statistics implementation
            return new EnhancedDeliveryStatistics();
        }

        /// <summary>
        /// Creates default serializer metrics
        /// </summary>
        private ISerializerMetrics CreateDefaultSerializerMetrics()
        {
            // Return a basic metrics implementation or null if interface allows it
            return new DefaultSerializerMetrics();
        }

        /// <summary>
        /// Initializes the message bus
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

                _isInitialized = true;
                Initialized?.Invoke(this);

                Debug.Log("[MessageBusProvider] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MessageBusProvider] Failed to initialize: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Creates a message bus instance based on the configuration
        /// </summary>
        private IMessageBus CreateMessageBus(IMessageBusConfig config)
        {
            // Try to create a MessagePipe-based message bus if available
            // Otherwise fallback to NullMessageBus
            try
            {
                // Look for MessagePipe implementation in the MessageBuses/MessagePipe folder
                var messagePipeType =
                    System.Type.GetType("AhBearStudios.Core.MessageBus.MessageBuses.MessagePipe.MessagePipeMessageBus");
                if (messagePipeType != null)
                {
                    return (IMessageBus)Activator.CreateInstance(messagePipeType, config, _deliveryServiceFactory,
                        _serializerFactory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MessageBusProvider] Failed to create MessagePipe implementation: {ex.Message}");
            }

            // Fallback to NullMessageBus
            Debug.LogWarning(
                "[MessageBusProvider] Using NullMessageBus as fallback. Consider implementing a proper message bus.");
            return new NullMessageBus();
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        private IMessageBusConfig CreateDefaultConfig()
        {
            var configBuilder = new MessageBusConfigBuilder();
            return configBuilder
                .WithInitialCapacity(_initialCapacity)
                .WithLoggingEnabled(true)
                .WithProfilingEnabled(Application.isEditor)
                .Build();
        }

        /// <summary>
        /// Gets the singleton instance (creates one if none exists)
        /// </summary>
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
        /// Disposes the message bus and cleans up resources
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _messageBus?.Dispose();
                _deliveryServiceFactory = null;
                _serializerFactory = null;
                _runtimeConfig = null;

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
            if (_instance == this)
            {
                lock (_lock)
                {
                    if (_instance == this)
                        _instance = null;
                }
            }

            Dispose();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }

        /// <summary>
        /// Validates the configuration in the editor
        /// </summary>
        private void OnValidate()
        {
            if (_initialCapacity <= 0)
                _initialCapacity = 100;
        }
    }
}