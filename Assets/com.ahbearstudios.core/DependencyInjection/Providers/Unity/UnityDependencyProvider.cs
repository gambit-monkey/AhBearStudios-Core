
using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.DependencyInjection.Exceptions;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Messages;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Unity;

namespace AhBearStudios.Core.DependencyInjection.Providers.Unity
{
    /// <summary>
    /// Unity MonoBehaviour that provides dependency injection services to the scene.
    /// Acts as a bridge between Unity's component system and the DI framework.
    /// Supports singleton and scoped container management with composition-based architecture.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class UnityDependencyProvider : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _validateOnStart = true;
        [SerializeField] private ContainerImplementation _containerImplementation = ContainerImplementation.VContainer;

        [Header("Debugging")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _logResolutions = false;

        [Header("Performance")]
        [SerializeField] private int _initialScopeCapacity = 4;
        [SerializeField] private bool _enableMemoryOptimizations = true;

        #endregion

        #region Private Fields

        private IDependencyContainer _container;
        private IMessageBus _messageBus;
        private bool _isInitialized;
        private bool _isDisposed;
        
        private static UnityDependencyProvider _globalInstance;
        
        // Use Dictionary for scoped containers
        private Dictionary<string, IDependencyContainer> _scopedContainers;
        
        // Message subscriptions for cleanup
        private readonly List<IDisposable> _messageSubscriptions = new List<IDisposable>();
        
        // Events for lifecycle management
        private event Action<UnityDependencyProvider> _initialized;
        private event Action<UnityDependencyProvider> _disposed;
        private event Action<Type, object> _serviceResolved;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the main dependency container.
        /// </summary>
        public IDependencyContainer Container => _container;

        /// <summary>
        /// Gets the message bus instance used by this provider.
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Gets whether the provider is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;

        /// <summary>
        /// Gets whether the provider has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets the global instance of the dependency provider.
        /// </summary>
        public static UnityDependencyProvider Global => _globalInstance;

        /// <summary>
        /// Gets the number of active scoped containers.
        /// </summary>
        public int ActiveScopeCount => _scopedContainers?.Count ?? 0;

        #endregion

        #region Public Events

        /// <summary>
        /// Event fired when the provider is initialized.
        /// </summary>
        public event Action<UnityDependencyProvider> Initialized
        {
            add
            {
                _initialized += value;
                // If already initialized, fire immediately
                if (_isInitialized && !_isDisposed)
                    value?.Invoke(this);
            }
            remove => _initialized -= value;
        }

        /// <summary>
        /// Event fired when the provider is disposed.
        /// </summary>
        public event Action<UnityDependencyProvider> Disposed
        {
            add => _disposed += value;
            remove => _disposed -= value;
        }

        /// <summary>
        /// Event fired when a service is resolved.
        /// </summary>
        public event Action<Type, object> ServiceResolved
        {
            add => _serviceResolved += value;
            remove => _serviceResolved -= value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_isDisposed)
                return;

            if (_persistBetweenScenes)
            {
                if (_globalInstance != null && _globalInstance != this)
                {
                    if (_enableDebugLogging)
                        Debug.Log($"[UnityDependencyProvider] Destroying duplicate instance on '{gameObject.name}'");
                    
                    Destroy(gameObject);
                    return;
                }

                _globalInstance = this;
                DontDestroyOnLoad(gameObject);
            }

            // Initialize collections with appropriate capacity
            _scopedContainers = new Dictionary<string, IDependencyContainer>(_initialScopeCapacity);

            if (_autoInitialize)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (_validateOnStart && _isInitialized && !_isDisposed)
            {
                ValidateContainer();
            }
        }

        private void OnDestroy()
        {
            if (_isDisposed)
                return;

            if (_globalInstance == this)
            {
                _globalInstance = null;
            }

            Dispose();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the dependency provider with the configured container implementation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when already initialized or disposed.</exception>
        public void Initialize()
        {
            if (_isDisposed)
                throw new InvalidOperationException("Cannot initialize a disposed UnityDependencyProvider");

            if (_isInitialized)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning($"[UnityDependencyProvider] Already initialized on '{gameObject.name}'");
                return;
            }

            try
            {
                if (_enableDebugLogging)
                    Debug.Log($"[UnityDependencyProvider] Initializing with {_containerImplementation} on '{gameObject.name}'");

                // Initialize message bus first
                InitializeMessageBus();

                // Set the factory to use our preferred implementation
                DependencyContainerFactory.SetDefaultImplementation(_containerImplementation);

                // Create the container with our message bus
                _container = DependencyContainerFactory.CreateConfigured(
                    $"Unity_{gameObject.name}", 
                    RegisterUnityServices,
                    _messageBus);

                // Subscribe to container messages if debugging is enabled
                if (_enableDebugLogging || _logResolutions)
                {
                    SubscribeToContainerMessages();
                }

                _isInitialized = true;
                _initialized?.Invoke(this);

                if (_enableDebugLogging)
                    Debug.Log($"[UnityDependencyProvider] Successfully initialized on '{gameObject.name}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDependencyProvider] Failed to initialize on '{gameObject.name}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes the provider with a custom container and message bus.
        /// </summary>
        /// <param name="container">The container to use.</param>
        /// <param name="messageBus">The message bus to use (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when container is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when already initialized or disposed.</exception>
        public void Initialize(IDependencyContainer container, IMessageBus messageBus = null)
        {
            if (container == null) 
                throw new ArgumentNullException(nameof(container));
            
            if (_isDisposed)
                throw new InvalidOperationException("Cannot initialize a disposed UnityDependencyProvider");
            
            if (_isInitialized)
                throw new InvalidOperationException("Provider is already initialized");

            try
            {
                _container = container;
                _messageBus = messageBus ?? container.MessageBus ?? GetOrCreateMessageBus();

                RegisterUnityServices(_container);

                if (_enableDebugLogging || _logResolutions)
                {
                    SubscribeToContainerMessages();
                }

                _isInitialized = true;
                _initialized?.Invoke(this);

                if (_enableDebugLogging)
                    Debug.Log($"[UnityDependencyProvider] Initialized with custom container on '{gameObject.name}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDependencyProvider] Failed to initialize with custom container: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Service Resolution

        /// <summary>
        /// Resolves a dependency of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The resolved instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when not initialized or disposed.</exception>
        /// <exception cref="ServiceResolutionException">Thrown when the service cannot be resolved.</exception>
        public T Resolve<T>()
        {
            EnsureInitialized();

            try
            {
                var service = _container.Resolve<T>();
                _serviceResolved?.Invoke(typeof(T), service);
                return service;
            }
            catch (ServiceResolutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceResolutionException(typeof(T),
                    $"Unexpected error resolving service of type '{typeof(T).FullName}'", ex);
            }
        }

        /// <summary>
        /// Attempts to resolve a dependency, returning false if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="service">The resolved service if successful.</param>
        /// <returns>True if resolution was successful, false otherwise.</returns>
        public bool TryResolve<T>(out T service)
        {
            if (!_isInitialized || _isDisposed)
            {
                service = default;
                return false;
            }

            return _container.TryResolve(out service);
        }

        /// <summary>
        /// Resolves a service or returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="defaultValue">The default value to return if resolution fails.</param>
        /// <returns>The resolved service or default value.</returns>
        public T ResolveOrDefault<T>(T defaultValue = default)
        {
            if (!_isInitialized || _isDisposed)
                return defaultValue;

            return _container.ResolveOrDefault(defaultValue);
        }

        #endregion

        #region Scope Management

        /// <summary>
        /// Creates a scoped container for temporary dependency overrides.
        /// </summary>
        /// <param name="scopeName">The name of the scope.</param>
        /// <returns>A scoped container.</returns>
        /// <exception cref="ArgumentException">Thrown when scopeName is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when scope already exists, provider not initialized, or disposed.</exception>
        public IDependencyContainer CreateScope(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentException("Scope name cannot be null or empty", nameof(scopeName));

            EnsureInitialized();

            if (_scopedContainers.ContainsKey(scopeName))
                throw new InvalidOperationException($"Scope '{scopeName}' already exists");

            try
            {
                var scopedContainer = _container.CreateChildContainer($"{_container.ContainerName}_Scope_{scopeName}");
                _scopedContainers[scopeName] = scopedContainer;

                if (_enableDebugLogging)
                    Debug.Log($"[UnityDependencyProvider] Created scope '{scopeName}'");

                return scopedContainer;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDependencyProvider] Failed to create scope '{scopeName}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a scoped container by name.
        /// </summary>
        /// <param name="scopeName">The name of the scope.</param>
        /// <returns>The scoped container if found, null otherwise.</returns>
        public IDependencyContainer GetScope(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName) || !_isInitialized || _isDisposed)
                return null;

            _scopedContainers.TryGetValue(scopeName, out var container);
            return container;
        }

        /// <summary>
        /// Disposes a scoped container.
        /// </summary>
        /// <param name="scopeName">The name of the scope to dispose.</param>
        /// <returns>True if the scope was found and disposed, false otherwise.</returns>
        public bool DisposeScope(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName) || !_isInitialized || _isDisposed)
                return false;

            if (_scopedContainers.TryGetValue(scopeName, out var container))
            {
                _scopedContainers.Remove(scopeName);
                container?.Dispose();

                if (_enableDebugLogging)
                    Debug.Log($"[UnityDependencyProvider] Disposed scope '{scopeName}'");

                return true;
            }

            return false;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the container's registrations.
        /// </summary>
        /// <returns>True if validation passes, false otherwise.</returns>
        public bool ValidateContainer()
        {
            if (!_isInitialized || _isDisposed)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning("[UnityDependencyProvider] Cannot validate uninitialized or disposed container");
                return false;
            }

            try
            {
                var isValid = _container.ValidateRegistrations();

                if (_enableDebugLogging)
                {
                    Debug.Log($"[UnityDependencyProvider] Container validation: {(isValid ? "PASSED" : "FAILED")}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDependencyProvider] Container validation failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Gets or creates the global dependency provider instance.
        /// </summary>
        /// <returns>The global dependency provider.</returns>
        public static UnityDependencyProvider GetOrCreateGlobal()
        {
            if (_globalInstance != null && !_globalInstance._isDisposed)
                return _globalInstance;

            // Search for existing instance in scene
            var existing = FindObjectOfType<UnityDependencyProvider>();
            if (existing != null && !existing._isDisposed)
            {
                _globalInstance = existing;
                return existing;
            }

            // Create new instance
            var go = new GameObject("[UnityDependencyProvider]");
            var provider = go.AddComponent<UnityDependencyProvider>();
            provider._persistBetweenScenes = true;
            provider._autoInitialize = true;

            DontDestroyOnLoad(go);
            return provider;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the message bus, either from existing provider or creates new one.
        /// </summary>
        private void InitializeMessageBus()
        {
            _messageBus = GetOrCreateMessageBus();
        }

        /// <summary>
        /// Gets or creates a message bus instance.
        /// </summary>
        /// <returns>A message bus instance.</returns>
        private IMessageBus GetOrCreateMessageBus()
        {
            // Try to get from MessageBusProvider
            var messageBusProvider = MessageBusProvider.Instance;
            if (messageBusProvider != null && messageBusProvider.IsInitialized)
            {
                return messageBusProvider.MessageBus;
            }

            // Use factory default if available
            return DependencyContainerFactory.DefaultMessageBus;
        }

        /// <summary>
        /// Registers built-in Unity services with the container.
        /// </summary>
        /// <param name="container">The container to register services with.</param>
        private void RegisterUnityServices(IDependencyContainer container)
        {
            if (container == null)
                return;

            try
            {
                // Register this provider as IDependencyProvider
                container.RegisterInstance<IDependencyProvider>(new UnityDependencyProviderAdapter(this));

                // Register the message bus
                if (_messageBus != null)
                {
                    container.RegisterInstance<IMessageBus>(_messageBus);
                }

                // Register common Unity services
                if (Camera.main != null)
                {
                    container.RegisterInstance(Camera.main);
                }

                // Register this MonoBehaviour for Unity-specific operations
                container.RegisterInstance<UnityDependencyProvider>(this);

                if (_enableDebugLogging)
                    Debug.Log("[UnityDependencyProvider] Registered Unity services");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDependencyProvider] Failed to register Unity services: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribes to container messages through the MessageBus for debugging purposes.
        /// </summary>
        private void SubscribeToContainerMessages()
        {
            if (_messageBus == null)
                return;

            try
            {
                // Subscribe to service resolution messages
                if (_logResolutions)
                {
                    var resolutionSubscription = _messageBus.Subscribe<ServiceResolvedMessage>(OnServiceResolved);
                    _messageSubscriptions.Add(resolutionSubscription);
                    
                    var failedResolutionSubscription = _messageBus.Subscribe<ServiceResolutionFailedMessage>(OnServiceResolutionFailed);
                    _messageSubscriptions.Add(failedResolutionSubscription);
                }

                // Subscribe to service registration messages
                if (_enableDebugLogging)
                {
                    var registrationSubscription = _messageBus.Subscribe<ServiceRegisteredMessage>(OnServiceRegistered);
                    _messageSubscriptions.Add(registrationSubscription);
                    
                    var containerBuiltSubscription = _messageBus.Subscribe<ContainerBuiltMessage>(OnContainerBuilt);
                    _messageSubscriptions.Add(containerBuiltSubscription);
                    
                    var childContainerSubscription = _messageBus.Subscribe<ChildContainerCreatedMessage>(OnChildContainerCreated);
                    _messageSubscriptions.Add(childContainerSubscription);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityDependencyProvider] Failed to subscribe to container messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Unsubscribes from all container messages.
        /// </summary>
        private void UnsubscribeFromContainerMessages()
        {
            try
            {
                foreach (var subscription in _messageSubscriptions)
                {
                    subscription?.Dispose();
                }
                _messageSubscriptions.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityDependencyProvider] Failed to unsubscribe from container messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles service resolution messages.
        /// </summary>
        private void OnServiceResolved(ServiceResolvedMessage message)
        {
            if (_logResolutions && !_isDisposed && message.ContainerName == _container?.ContainerName)
            {
                Debug.Log($"[UnityDependencyProvider] Resolved {message.ServiceType.Name} in {message.ResolutionTime.TotalMilliseconds:F2}ms");
                _serviceResolved?.Invoke(message.ServiceType, message.Instance);
            }
        }

        /// <summary>
        /// Handles service resolution failure messages.
        /// </summary>
        private void OnServiceResolutionFailed(ServiceResolutionFailedMessage message)
        {
            if (_logResolutions && !_isDisposed && message.ContainerName == _container?.ContainerName)
            {
                Debug.LogWarning($"[UnityDependencyProvider] Failed to resolve {message.ServiceType.Name}: {message.ErrorMessage}");
            }
        }

        /// <summary>
        /// Handles service registration messages.
        /// </summary>
        private void OnServiceRegistered(ServiceRegisteredMessage message)
        {
            if (_enableDebugLogging && !_isDisposed && message.ContainerName == _container?.ContainerName)
            {
                var implementationType = message.ImplementationType?.Name ?? (message.IsFactoryRegistration ? "Factory" : "Unknown");
                Debug.Log($"[UnityDependencyProvider] Registered {message.ServiceType.Name} -> {implementationType} ({message.Lifetime})");
            }
        }

        /// <summary>
        /// Handles container built messages.
        /// </summary>
        private void OnContainerBuilt(ContainerBuiltMessage message)
        {
            if (_enableDebugLogging && !_isDisposed && message.ContainerName == _container?.ContainerName)
            {
                Debug.Log($"[UnityDependencyProvider] Container built with {message.RegisteredServicesCount} services in {message.BuildTime.TotalMilliseconds:F2}ms");
            }
        }

        /// <summary>
        /// Handles child container creation messages.
        /// </summary>
        private void OnChildContainerCreated(ChildContainerCreatedMessage message)
        {
            if (_enableDebugLogging && !_isDisposed && message.ParentContainerName == _container?.ContainerName)
            {
                Debug.Log($"[UnityDependencyProvider] Created child container '{message.ChildContainerName}'");
            }
        }

        /// <summary>
        /// Ensures the provider is initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when not initialized or disposed.</exception>
        private void EnsureInitialized()
        {
            if (_isDisposed)
                throw new InvalidOperationException("UnityDependencyProvider has been disposed");

            if (!_isInitialized)
                throw new InvalidOperationException("UnityDependencyProvider must be initialized before use. Call Initialize() or set AutoInitialize to true.");
        }

        /// <summary>
        /// Disposes the container and all scoped containers.
        /// </summary>
        private void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _disposed?.Invoke(this);

                // Unsubscribe from messages first
                if (_enableDebugLogging || _logResolutions)
                {
                    UnsubscribeFromContainerMessages();
                }

                // Dispose scoped containers
                if (_scopedContainers != null)
                {
                    foreach (var kvp in _scopedContainers)
                    {
                        try
                        {
                            kvp.Value?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[UnityDependencyProvider] Error disposing scope '{kvp.Key}': {ex.Message}");
                        }
                    }
                    _scopedContainers.Clear();
                }

                // Dispose main container
                if (_container != null)
                {
                    _container.Dispose();
                    _container = null;
                }

                // Clear events
                _initialized = null;
                _disposed = null;
                _serviceResolved = null;

                _isInitialized = false;
                _isDisposed = true;

                if (_enableDebugLogging)
                    Debug.Log("[UnityDependencyProvider] Disposed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityDependencyProvider] Error disposing container: {ex.Message}");
                _isDisposed = true; // Mark as disposed even if disposal failed
            }
        }

        #endregion
    }

    /// <summary>
    /// Adapter that implements IDependencyProvider using UnityDependencyProvider.
    /// </summary>
    internal sealed class UnityDependencyProviderAdapter : IDependencyProvider
    {
        private readonly UnityDependencyProvider _provider;

        /// <summary>
        /// Initializes a new instance of the UnityDependencyProviderAdapter class.
        /// </summary>
        /// <param name="provider">The Unity dependency provider to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
        public UnityDependencyProviderAdapter(UnityDependencyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        public T Resolve<T>()
        {
            return _provider.Resolve<T>();
        }
    }
}