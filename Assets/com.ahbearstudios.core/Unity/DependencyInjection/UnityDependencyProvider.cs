using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AhBearStudios.Core.DependencyInjection.Factories;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Messages;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Unity;
using ServiceResolutionException = AhBearStudios.Core.DependencyInjection.Exceptions.ServiceResolutionException;

namespace AhBearStudios.Core.DependencyInjection.Providers.Unity
{
    /// <summary>
    /// Production-ready Unity MonoBehaviour that provides dependency injection services.
    /// Implements comprehensive error handling, performance optimizations, and lifecycle management.
    /// Supports singleton and scoped container management with thread-safe operations.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class UnityDependencyProvider : MonoBehaviour, IDisposable
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private bool _persistBetweenScenes = true;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _validateOnStart = true;
        [SerializeField] private ContainerImplementation _containerImplementation = ContainerImplementation.VContainer;
        [SerializeField] private float _initializationTimeout = 30f;

        [Header("Debugging")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _logResolutions = false;
        [SerializeField] private bool _enablePerformanceMetrics = false;

        [Header("Performance")]
        [SerializeField] private int _initialScopeCapacity = 8;
        [SerializeField] private bool _enableMemoryOptimizations = true;
        [SerializeField] private int _maxConcurrentInitializations = 1;
        [SerializeField] private bool _preloadCriticalServices = true;

        [Header("Error Handling")]
        [SerializeField] private int _maxRetryAttempts = 3;
        [SerializeField] private float _retryDelaySeconds = 0.1f;
        [SerializeField] private bool _enableGracefulDegradation = true;

        #endregion

        #region Private Fields

        private IDependencyContainer _container;
        private IMessageBusService _messageBusService;
        
        // Thread-safe state management
        private volatile bool _isInitialized;
        private volatile bool _isDisposed;
        private volatile bool _isInitializing;
        
        private static volatile UnityDependencyProvider _globalInstance;
        private static readonly object _globalLock = new object();
        
        // Thread-safe collections
        private readonly ConcurrentDictionary<string, IDependencyContainer> _scopedContainers 
            = new ConcurrentDictionary<string, IDependencyContainer>();
        
        // Subscription management
        private readonly ConcurrentBag<IDisposable> _messageSubscriptions = new ConcurrentBag<IDisposable>();
        
        // Synchronization
        private readonly SemaphoreSlim _initializationSemaphore;
        private readonly object _containerLock = new object();
        
        // Performance tracking
        private readonly Dictionary<Type, PerformanceMetrics> _resolutionMetrics 
            = new Dictionary<Type, PerformanceMetrics>();
        private readonly object _metricsLock = new object();
        
        // Cancellation support
        private CancellationTokenSource _cancellationTokenSource;
        
        // Events (thread-safe)
        private event Action<UnityDependencyProvider> _initialized;
        private event Action<UnityDependencyProvider> _disposed;
        private event Action<Type, object> _serviceResolved;
        private event Action<Exception> _errorOccurred;

        #endregion

        #region Performance Metrics

        private struct PerformanceMetrics
        {
            public long TotalResolutions;
            public long TotalTime;
            public long MinTime;
            public long MaxTime;
            public double AverageTime => TotalResolutions > 0 ? (double)TotalTime / TotalResolutions : 0;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the main dependency container. Thread-safe.
        /// </summary>
        public IDependencyContainer Container 
        {
            get
            {
                lock (_containerLock)
                {
                    return _container;
                }
            }
        }

        /// <summary>
        /// Gets the message bus instance used by this provider. Thread-safe.
        /// </summary>
        public IMessageBusService MessageBusService => _messageBusService;

        /// <summary>
        /// Gets whether the provider is initialized. Thread-safe.
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;

        /// <summary>
        /// Gets whether the provider has been disposed. Thread-safe.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets whether the provider is currently initializing. Thread-safe.
        /// </summary>
        public bool IsInitializing => _isInitializing;

        /// <summary>
        /// Gets the global instance of the dependency provider. Thread-safe.
        /// </summary>
        public static UnityDependencyProvider Global
        {
            get
            {
                lock (_globalLock)
                {
                    return _globalInstance;
                }
            }
        }

        /// <summary>
        /// Gets the number of active scoped containers. Thread-safe.
        /// </summary>
        public int ActiveScopeCount => _scopedContainers.Count;

        /// <summary>
        /// Gets performance metrics for service resolutions.
        /// </summary>
        public IReadOnlyDictionary<Type, (long count, double avgTime)> PerformanceMetrics
        {
            get
            {
                if (!_enablePerformanceMetrics)
                    return new Dictionary<Type, (long, double)>();

                lock (_metricsLock)
                {
                    return _resolutionMetrics.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (kvp.Value.TotalResolutions, kvp.Value.AverageTime));
                }
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Event fired when the provider is initialized. Thread-safe.
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
        /// Event fired when the provider is disposed. Thread-safe.
        /// </summary>
        public event Action<UnityDependencyProvider> Disposed
        {
            add => _disposed += value;
            remove => _disposed -= value;
        }

        /// <summary>
        /// Event fired when a service is resolved. Thread-safe.
        /// </summary>
        public event Action<Type, object> ServiceResolved
        {
            add => _serviceResolved += value;
            remove => _serviceResolved -= value;
        }

        /// <summary>
        /// Event fired when an error occurs. Thread-safe.
        /// </summary>
        public event Action<Exception> ErrorOccurred
        {
            add => _errorOccurred += value;
            remove => _errorOccurred -= value;
        }

        #endregion

        #region Constructor

        public UnityDependencyProvider()
        {
            _initializationSemaphore = new SemaphoreSlim(_maxConcurrentInitializations, _maxConcurrentInitializations);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_isDisposed)
                return;

            try
            {
                ConfigureSingleton();

                if (_autoInitialize)
                {
                    _ = InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                LogError("Failed during Awake", ex);
                _errorOccurred?.Invoke(ex);
            }
        }

        private void Start()
        {
            if (_validateOnStart && _isInitialized && !_isDisposed)
            {
                _ = ValidateContainerAsync();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _isInitialized && !_isDisposed)
            {
                // Resume operations
                LogDebug("Application resumed, validating container state");
                _ = ValidateContainerAsync();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the dependency provider asynchronously with timeout support.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task representing the initialization operation.</returns>
        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                LogWarning("Cannot initialize disposed provider");
                return false;
            }

            if (_isInitialized)
            {
                LogDebug("Provider already initialized");
                return true;
            }

            if (_isInitializing)
            {
                LogDebug("Initialization already in progress, waiting...");
                await WaitForInitializationAsync(cancellationToken);
                return _isInitialized;
            }

            var timeout = TimeSpan.FromSeconds(_initializationTimeout);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await _initializationSemaphore.WaitAsync(timeoutCts.Token);
                
                try
                {
                    // Double-check pattern
                    if (_isInitialized || _isDisposed)
                        return _isInitialized;

                    _isInitializing = true;
                    return await PerformInitializationAsync(timeoutCts.Token);
                }
                finally
                {
                    _isInitializing = false;
                    _initializationSemaphore.Release();
                }
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                var ex = new TimeoutException($"Initialization timed out after {timeout.TotalSeconds} seconds");
                LogError("Initialization timeout", ex);
                _errorOccurred?.Invoke(ex);
                return false;
            }
            catch (Exception ex)
            {
                LogError("Initialization failed", ex);
                _errorOccurred?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// Synchronous initialization method for backward compatibility.
        /// </summary>
        public void Initialize()
        {
            try
            {
                var task = InitializeAsync();
                if (!task.Wait(TimeSpan.FromSeconds(_initializationTimeout)))
                {
                    throw new TimeoutException($"Initialization timed out after {_initializationTimeout} seconds");
                }

                if (!task.Result)
                {
                    throw new InvalidOperationException("Initialization failed");
                }
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// Initializes the provider with a custom container and message bus.
        /// </summary>
        public async Task<bool> InitializeAsync(IDependencyContainer container, 
            IMessageBusService messageBusService = null, 
            CancellationToken cancellationToken = default)
        {
            if (container == null) 
                throw new ArgumentNullException(nameof(container));
            
            if (_isDisposed)
                throw new InvalidOperationException("Cannot initialize a disposed UnityDependencyProvider");
            
            if (_isInitialized)
                throw new InvalidOperationException("Provider is already initialized");

            try
            {
                await _initializationSemaphore.WaitAsync(cancellationToken);
                
                try
                {
                    _isInitializing = true;
                    
                    lock (_containerLock)
                    {
                        _container = container;
                    }
                    
                    _messageBusService = messageBusService ?? container.MessageBusService ?? await GetOrCreateMessageBusAsync();

                    await RegisterUnityServicesAsync(_container, cancellationToken);
                    SubscribeToContainerMessages();

                    if (_preloadCriticalServices)
                    {
                        await PreloadCriticalServicesAsync(cancellationToken);
                    }

                    _isInitialized = true;
                    _initialized?.Invoke(this);

                    LogDebug("Initialized with custom container");
                    return true;
                }
                finally
                {
                    _isInitializing = false;
                    _initializationSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize with custom container", ex);
                _errorOccurred?.Invoke(ex);
                return false;
            }
        }

        #endregion

        #region Service Resolution

        /// <summary>
        /// Resolves a dependency with performance tracking and retry logic.
        /// </summary>
        public T Resolve<T>()
        {
            EnsureInitialized();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Exception lastException = null;

            for (int attempt = 0; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    T service;
                    lock (_containerLock)
                    {
                        service = _container.Resolve<T>();
                    }

                    stopwatch.Stop();
                    RecordResolutionMetrics<T>(stopwatch.ElapsedTicks);
                    _serviceResolved?.Invoke(typeof(T), service);
                    
                    return service;
                }
                catch (ServiceResolutionException ex)
                {
                    lastException = ex;
                    if (attempt == _maxRetryAttempts || !ShouldRetry(ex))
                        break;

                    Thread.Sleep(TimeSpan.FromSeconds(_retryDelaySeconds * (attempt + 1)));
                }
                catch (Exception ex)
                {
                    lastException = new ServiceResolutionException(typeof(T),
                        $"Unexpected error resolving service of type '{typeof(T).FullName}'", ex);
                    break;
                }
            }

            stopwatch.Stop();
            LogError($"Failed to resolve {typeof(T).Name} after {_maxRetryAttempts + 1} attempts", lastException);
            _errorOccurred?.Invoke(lastException);
            
            if (_enableGracefulDegradation)
            {
                return TryGetFallbackService<T>();
            }
            
            throw lastException;
        }

        /// <summary>
        /// Attempts to resolve a dependency with enhanced error handling.
        /// </summary>
        public bool TryResolve<T>(out T service)
        {
            service = default;

            if (!_isInitialized || _isDisposed)
                return false;

            try
            {
                lock (_containerLock)
                {
                    if (_container.TryResolve(out service))
                    {
                        _serviceResolved?.Invoke(typeof(T), service);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error during TryResolve<{typeof(T).Name}>: {ex.Message}");
                _errorOccurred?.Invoke(ex);
            }

            return false;
        }

        /// <summary>
        /// Resolves a service asynchronously with cancellation support.
        /// </summary>
        public async Task<T> ResolveAsync<T>(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // For now, delegate to synchronous version
            // In future versions, this could support async resolution pipelines
            return await Task.Run(() => Resolve<T>(), cancellationToken);
        }

        /// <summary>
        /// Resolves multiple services of the same type.
        /// </summary>
        public IEnumerable<T> ResolveAll<T>()
        {
            EnsureInitialized();

            try
            {
                lock (_containerLock)
                {
                    return _container.ResolveAll<T>() ?? Enumerable.Empty<T>();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to resolve all services of type {typeof(T).Name}", ex);
                _errorOccurred?.Invoke(ex);
                
                if (_enableGracefulDegradation)
                    return Enumerable.Empty<T>();
                
                throw;
            }
        }

        #endregion

        #region Scope Management

        /// <summary>
        /// Creates a scoped container with enhanced error handling.
        /// </summary>
        public async Task<IDependencyContainer> CreateScopeAsync(string scopeName, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentException("Scope name cannot be null or empty", nameof(scopeName));

            EnsureInitialized();
            cancellationToken.ThrowIfCancellationRequested();

            if (_scopedContainers.ContainsKey(scopeName))
                throw new InvalidOperationException($"Scope '{scopeName}' already exists");

            try
            {
                IDependencyContainer scopedContainer;
                lock (_containerLock)
                {
                    scopedContainer = _container.CreateChildContainer($"{_container.ContainerName}_Scope_{scopeName}");
                }

                if (!_scopedContainers.TryAdd(scopeName, scopedContainer))
                {
                    scopedContainer?.Dispose();
                    throw new InvalidOperationException($"Failed to register scope '{scopeName}' - concurrent creation detected");
                }

                LogDebug($"Created scope '{scopeName}'");
                return scopedContainer;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create scope '{scopeName}'", ex);
                _errorOccurred?.Invoke(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a scoped container by name with thread safety.
        /// </summary>
        public IDependencyContainer GetScope(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName) || !_isInitialized || _isDisposed)
                return null;

            _scopedContainers.TryGetValue(scopeName, out var container);
            return container;
        }

        /// <summary>
        /// Disposes a scoped container safely.
        /// </summary>
        public bool DisposeScope(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName) || !_isInitialized || _isDisposed)
                return false;

            if (_scopedContainers.TryRemove(scopeName, out var container))
            {
                try
                {
                    container?.Dispose();
                    LogDebug($"Disposed scope '{scopeName}'");
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing scope '{scopeName}'", ex);
                    _errorOccurred?.Invoke(ex);
                }
            }

            return false;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the container asynchronously.
        /// </summary>
        public async Task<bool> ValidateContainerAsync(CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || _isDisposed)
            {
                LogWarning("Cannot validate uninitialized or disposed container");
                return false;
            }

            try
            {
                bool isValid;
                await Task.Run(() =>
                {
                    lock (_containerLock)
                    {
                        isValid = _container.ValidateRegistrations();
                    }
                }, cancellationToken);

                LogDebug($"Container validation: {(isValid ? "PASSED" : "FAILED")}");
                return isValid;
            }
            catch (Exception ex)
            {
                LogError("Container validation failed", ex);
                _errorOccurred?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// Synchronous validation for backward compatibility.
        /// </summary>
        public bool ValidateContainer()
        {
            try
            {
                var task = ValidateContainerAsync();
                return task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogError("Container validation failed", ex);
                return false;
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Gets or creates the global dependency provider instance with thread safety.
        /// </summary>
        public static UnityDependencyProvider GetOrCreateGlobal()
        {
            lock (_globalLock)
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
                _globalInstance = provider;
                return provider;
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the provider and all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _cancellationTokenSource?.Cancel();
                _disposed?.Invoke(this);

                // Unsubscribe from messages
                UnsubscribeFromContainerMessages();

                // Dispose scoped containers
                var scopeNames = _scopedContainers.Keys.ToList();
                foreach (var scopeName in scopeNames)
                {
                    DisposeScope(scopeName);
                }

                // Dispose main container
                lock (_containerLock)
                {
                    _container?.Dispose();
                    _container = null;
                }

                // Clear events
                _initialized = null;
                _disposed = null;
                _serviceResolved = null;
                _errorOccurred = null;

                // Dispose synchronization objects
                _initializationSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();

                // Update state
                _isInitialized = false;
                _isDisposed = true;

                // Clear global reference if this is the global instance
                lock (_globalLock)
                {
                    if (_globalInstance == this)
                        _globalInstance = null;
                }

                LogDebug("Disposed successfully");
            }
            catch (Exception ex)
            {
                LogError("Error during disposal", ex);
                _isDisposed = true; // Mark as disposed even if disposal failed
            }
        }

        #endregion

        #region Private Implementation

        private void ConfigureSingleton()
        {
            if (_persistBetweenScenes)
            {
                lock (_globalLock)
                {
                    if (_globalInstance != null && _globalInstance != this)
                    {
                        LogDebug($"Destroying duplicate instance on '{gameObject.name}'");
                        Destroy(gameObject);
                        return;
                    }

                    _globalInstance = this;
                    DontDestroyOnLoad(gameObject);
                }
            }
        }

        private async Task<bool> PerformInitializationAsync(CancellationToken cancellationToken)
        {
            LogDebug($"Initializing with {_containerImplementation}");

            // Initialize message bus
            _messageBusService = await GetOrCreateMessageBusAsync();
            
            // Configure factory
            DependencyContainerFactory.SetDefaultImplementation(_containerImplementation);

            // Create container
            lock (_containerLock)
            {
                _container = DependencyContainerFactory.CreateConfigured(
                    $"Unity_{gameObject.name}",
                    container => RegisterUnityServicesAsync(container, cancellationToken).GetAwaiter().GetResult(),
                    _messageBusService);
            }

            // Subscribe to messages
            SubscribeToContainerMessages();

            // Preload critical services if enabled
            if (_preloadCriticalServices)
            {
                await PreloadCriticalServicesAsync(cancellationToken);
            }

            _isInitialized = true;
            _initialized?.Invoke(this);

            LogDebug("Successfully initialized");
            return true;
        }

        private async Task WaitForInitializationAsync(CancellationToken cancellationToken)
        {
            while (_isInitializing && !_isInitialized && !_isDisposed)
            {
                await Task.Delay(10, cancellationToken);
            }
        }

        private async Task<IMessageBusService> GetOrCreateMessageBusAsync()
        {
            // Try to get from MessageBusProvider
            var messageBusProvider = MessageBusProvider.Instance;
            if (messageBusProvider != null && messageBusProvider.IsInitialized)
            {
                return messageBusProvider.MessageBusService;
            }

            // Use factory default if available
            return DependencyContainerFactory.DefaultMessageBusService;
        }

        private async Task RegisterUnityServicesAsync(IDependencyContainer container, CancellationToken cancellationToken)
        {
            if (container == null)
                return;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Register this provider as IDependencyProvider
                container.RegisterInstance<IDependencyProvider>(new UnityDependencyProviderAdapter(this));

                // Register the message bus
                if (_messageBusService != null)
                {
                    container.RegisterInstance<IMessageBusService>(_messageBusService);
                }

                // Register common Unity services
                await Task.Run(() =>
                {
                    if (Camera.main != null)
                    {
                        container.RegisterInstance(Camera.main);
                    }

                    // Register this MonoBehaviour for Unity-specific operations
                    container.RegisterInstance<UnityDependencyProvider>(this);
                }, cancellationToken);

                LogDebug("Registered Unity services");
            }
            catch (Exception ex)
            {
                LogError("Failed to register Unity services", ex);
                throw;
            }
        }

        private async Task PreloadCriticalServicesAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogDebug("Preloading critical services");
                
                // Preload message bus and dependency provider
                await Task.Run(() =>
                {
                    TryResolve<IMessageBusService>(out _);
                    TryResolve<IDependencyProvider>(out _);
                }, cancellationToken);

                LogDebug("Critical services preloaded");
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to preload some critical services: {ex.Message}");
            }
        }

        private void SubscribeToContainerMessages()
        {
            if (_messageBusService == null)
                return;

            try
            {
                if (_logResolutions)
                {
                    var resolutionSub = _messageBusService.Subscribe<ServiceResolvedMessage>(OnServiceResolved);
                    _messageSubscriptions.Add(resolutionSub);
                    
                    var failedSub = _messageBusService.Subscribe<ServiceResolutionFailedMessage>(OnServiceResolutionFailed);
                    _messageSubscriptions.Add(failedSub);
                }

                if (_enableDebugLogging)
                {
                    var regSub = _messageBusService.Subscribe<ServiceRegisteredMessage>(OnServiceRegistered);
                    _messageSubscriptions.Add(regSub);
                    
                    var builtSub = _messageBusService.Subscribe<ContainerBuiltMessage>(OnContainerBuilt);
                    _messageSubscriptions.Add(builtSub);
                    
                    var childSub = _messageBusService.Subscribe<ChildContainerCreatedMessage>(OnChildContainerCreated);
                    _messageSubscriptions.Add(childSub);
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to subscribe to container messages: {ex.Message}");
            }
        }

        private void UnsubscribeFromContainerMessages()
        {
            try
            {
                while (_messageSubscriptions.TryTake(out var subscription))
                {
                    subscription?.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to unsubscribe from container messages: {ex.Message}");
            }
        }

        private void RecordResolutionMetrics<T>(long elapsedTicks)
        {
            if (!_enablePerformanceMetrics)
                return;

            var type = typeof(T);
            lock (_metricsLock)
            {
                if (!_resolutionMetrics.TryGetValue(type, out var metrics))
                {
                    metrics = new PerformanceMetrics
                    {
                        MinTime = long.MaxValue,
                        MaxTime = long.MinValue
                    };
                }

                metrics.TotalResolutions++;
                metrics.TotalTime += elapsedTicks;
                metrics.MinTime = Math.Min(metrics.MinTime, elapsedTicks);
                metrics.MaxTime = Math.Max(metrics.MaxTime, elapsedTicks);

                _resolutionMetrics[type] = metrics;
            }
        }

        private bool ShouldRetry(Exception exception)
        {
            // Define retry logic based on exception type
            return exception is not ArgumentNullException && 
                   exception is not InvalidOperationException;
        }

        private T TryGetFallbackService<T>()
        {
            // Implement fallback service resolution logic
            // This could involve default implementations or mock services
            LogDebug($"Attempting fallback resolution for {typeof(T).Name}");
            return default;
        }

        private void EnsureInitialized()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UnityDependencyProvider));

            if (!_isInitialized)
            {
                if (_isInitializing)
                {
                    throw new InvalidOperationException(
                        "UnityDependencyProvider is currently initializing. Use InitializeAsync() for proper async initialization.");
                }
                
                throw new InvalidOperationException(
                    "UnityDependencyProvider must be initialized before use. Call Initialize() or set AutoInitialize to true.");
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
                Debug.Log($"[UnityDependencyProvider] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
                Debug.LogWarning($"[UnityDependencyProvider] {message}");
        }

        private void LogError(string message, Exception exception = null)
        {
            var fullMessage = exception != null 
                ? $"[UnityDependencyProvider] {message}: {exception.Message}"
                : $"[UnityDependencyProvider] {message}";
            
            Debug.LogError(fullMessage);
            
            if (exception != null && _enableDebugLogging)
                Debug.LogException(exception);
        }

        #endregion

        #region Message Handlers

        private void OnServiceResolved(ServiceResolvedMessage message)
        {
            if (_logResolutions && !_isDisposed)
            {
                var containerName = Container?.ContainerName;
                if (message.ContainerName == containerName)
                {
                    var timeMs = message.ResolutionTime.TotalMilliseconds;
                    LogDebug($"Resolved {message.ServiceType.Name} in {timeMs:F2}ms");
                    _serviceResolved?.Invoke(message.ServiceType, message.Instance);
                }
            }
        }

        private void OnServiceResolutionFailed(ServiceResolutionFailedMessage message)
        {
            if (_logResolutions && !_isDisposed)
            {
                var containerName = Container?.ContainerName;
                if (message.ContainerName == containerName)
                {
                    LogWarning($"Failed to resolve {message.ServiceType.Name}: {message.ErrorMessage}");
                }
            }
        }

        private void OnServiceRegistered(ServiceRegisteredMessage message)
        {
            if (_enableDebugLogging && !_isDisposed)
            {
                var containerName = Container?.ContainerName;
                if (message.ContainerName == containerName)
                {
                    var implementationType = message.ImplementationType?.Name ?? 
                                           (message.IsFactoryRegistration ? "Factory" : "Unknown");
                    LogDebug($"Registered {message.ServiceType.Name} -> {implementationType} ({message.Lifetime})");
                }
            }
        }

        private void OnContainerBuilt(ContainerBuiltMessage message)
        {
            if (_enableDebugLogging && !_isDisposed)
            {
                var containerName = Container?.ContainerName;
                if (message.ContainerName == containerName)
                {
                    var timeMs = message.BuildTime.TotalMilliseconds;
                    LogDebug($"Container built with {message.RegisteredServicesCount} services in {timeMs:F2}ms");
                }
            }
        }

        private void OnChildContainerCreated(ChildContainerCreatedMessage message)
        {
            if (_enableDebugLogging && !_isDisposed)
            {
                var containerName = Container?.ContainerName;
                if (message.ParentContainerName == containerName)
                {
                    LogDebug($"Created child container '{message.ChildContainerName}'");
                }
            }
        }

        #endregion

        #region Health Monitoring

        /// <summary>
        /// Gets the current health status of the provider.
        /// </summary>
        public ProviderHealthStatus GetHealthStatus()
        {
            if (_isDisposed)
                return ProviderHealthStatus.Disposed;

            if (!_isInitialized)
                return _isInitializing ? ProviderHealthStatus.Initializing : ProviderHealthStatus.NotInitialized;

            try
            {
                // Perform basic health checks
                var container = Container;
                if (container == null)
                    return ProviderHealthStatus.Unhealthy;

                // Check if container can resolve basic services
                if (!TryResolve<IDependencyProvider>(out _))
                    return ProviderHealthStatus.Degraded;

                return ProviderHealthStatus.Healthy;
            }
            catch
            {
                return ProviderHealthStatus.Unhealthy;
            }
        }

        /// <summary>
        /// Performs a comprehensive health check.
        /// </summary>
        public async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var result = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow,
                Status = GetHealthStatus()
            };

            if (result.Status == ProviderHealthStatus.Disposed || 
                result.Status == ProviderHealthStatus.NotInitialized)
            {
                return result;
            }

            try
            {
                // Check container validation
                result.ContainerValid = await ValidateContainerAsync(cancellationToken);
                
                // Check message bus connectivity
                result.MessageBusConnected = _messageBusService != null;
                
                // Check scope count
                result.ActiveScopes = ActiveScopeCount;
                
                // Check performance metrics
                if (_enablePerformanceMetrics)
                {
                    result.PerformanceMetrics = PerformanceMetrics.ToDictionary(
                        kvp => kvp.Key.Name,
                        kvp => new { kvp.Value.count, kvp.Value.avgTime });
                }

                // Determine overall health
                if (!result.ContainerValid)
                    result.Status = ProviderHealthStatus.Degraded;

            }
            catch (Exception ex)
            {
                result.Status = ProviderHealthStatus.Unhealthy;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Resets performance metrics.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            if (!_enablePerformanceMetrics)
                return;

            lock (_metricsLock)
            {
                _resolutionMetrics.Clear();
            }

            LogDebug("Performance metrics reset");
        }

        /// <summary>
        /// Gets detailed container information.
        /// </summary>
        public ContainerInfo GetContainerInfo()
        {
            if (!_isInitialized || _isDisposed)
                return null;

            lock (_containerLock)
            {
                if (_container == null)
                    return null;

                return new ContainerInfo
                {
                    Name = _container.ContainerName,
                    Implementation = _containerImplementation.ToString(),
                    IsBuilt = _container.IsBuilt,
                    ActiveScopes = ActiveScopeCount,
                    HasMessageBus = _messageBusService != null
                };
            }
        }

        /// <summary>
        /// Creates a diagnostic report.
        /// </summary>
        public async Task<string> GenerateDiagnosticReportAsync()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Unity Dependency Provider Diagnostic Report ===");
            report.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine($"GameObject: {gameObject.name}");
            report.AppendLine();

            // Status
            report.AppendLine("=== Status ===");
            report.AppendLine($"Health Status: {GetHealthStatus()}");
            report.AppendLine($"Is Initialized: {IsInitialized}");
            report.AppendLine($"Is Disposed: {IsDisposed}");
            report.AppendLine($"Is Initializing: {IsInitializing}");
            report.AppendLine($"Persist Between Scenes: {_persistBetweenScenes}");
            report.AppendLine();

            // Configuration
            report.AppendLine("=== Configuration ===");
            report.AppendLine($"Container Implementation: {_containerImplementation}");
            report.AppendLine($"Auto Initialize: {_autoInitialize}");
            report.AppendLine($"Validate On Start: {_validateOnStart}");
            report.AppendLine($"Enable Debug Logging: {_enableDebugLogging}");
            report.AppendLine($"Log Resolutions: {_logResolutions}");
            report.AppendLine($"Enable Performance Metrics: {_enablePerformanceMetrics}");
            report.AppendLine($"Enable Graceful Degradation: {_enableGracefulDegradation}");
            report.AppendLine();

            // Container Info
            var containerInfo = GetContainerInfo();
            if (containerInfo != null)
            {
                report.AppendLine("=== Container ===");
                report.AppendLine($"Name: {containerInfo.Name}");
                report.AppendLine($"Implementation: {containerInfo.Implementation}");
                report.AppendLine($"Is Built: {containerInfo.IsBuilt}");
                report.AppendLine($"Has Message Bus: {containerInfo.HasMessageBus}");
                report.AppendLine();
            }

            // Scopes
            report.AppendLine("=== Scopes ===");
            report.AppendLine($"Active Scope Count: {ActiveScopeCount}");
            if (ActiveScopeCount > 0)
            {
                foreach (var scopeName in _scopedContainers.Keys)
                {
                    report.AppendLine($"  - {scopeName}");
                }
            }
            report.AppendLine();

            // Performance Metrics
            if (_enablePerformanceMetrics)
            {
                report.AppendLine("=== Performance Metrics ===");
                var metrics = PerformanceMetrics;
                if (metrics.Count > 0)
                {
                    foreach (var kvp in metrics.OrderByDescending(x => x.Value.count))
                    {
                        report.AppendLine($"  {kvp.Key.Name}: {kvp.Value.count} resolutions, {kvp.Value.avgTime:F2}μs avg");
                    }
                }
                else
                {
                    report.AppendLine("  No metrics recorded");
                }
                report.AppendLine();
            }

            // Health Check
            var healthCheck = await PerformHealthCheckAsync();
            report.AppendLine("=== Health Check ===");
            report.AppendLine($"Overall Status: {healthCheck.Status}");
            report.AppendLine($"Container Valid: {healthCheck.ContainerValid}");
            report.AppendLine($"Message Bus Connected: {healthCheck.MessageBusConnected}");
            if (!string.IsNullOrEmpty(healthCheck.ErrorMessage))
            {
                report.AppendLine($"Error: {healthCheck.ErrorMessage}");
            }

            report.AppendLine("=== End Report ===");
            
            return report.ToString();
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Represents the health status of the provider.
    /// </summary>
    public enum ProviderHealthStatus
    {
        NotInitialized,
        Initializing,
        Healthy,
        Degraded,
        Unhealthy,
        Disposed
    }

    /// <summary>
    /// Represents the result of a health check.
    /// </summary>
    public class HealthCheckResult
    {
        public DateTime Timestamp { get; set; }
        public ProviderHealthStatus Status { get; set; }
        public bool ContainerValid { get; set; }
        public bool MessageBusConnected { get; set; }
        public int ActiveScopes { get; set; }
        public string ErrorMessage { get; set; }
        public object PerformanceMetrics { get; set; }
    }

    /// <summary>
    /// Contains information about the container.
    /// </summary>
    public class ContainerInfo
    {
        public string Name { get; set; }
        public string Implementation { get; set; }
        public bool IsBuilt { get; set; }
        public int ActiveScopes { get; set; }
        public bool HasMessageBus { get; set; }
    }

    #endregion
}