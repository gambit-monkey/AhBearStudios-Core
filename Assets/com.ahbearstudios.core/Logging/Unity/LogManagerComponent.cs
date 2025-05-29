using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.DependencyInjection;
using UnityEngine;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Adapters;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.MessageBuses;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Unity component that manages the logging system lifecycle using JobLoggerManager and factory patterns.
    /// Provides centralized logging management with dependency injection support and robust configuration.
    /// This component orchestrates all logging subsystems and provides a unified interface for logging operations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LogManagerComponent : MonoBehaviour, IDisposable
    {
        #region Inspector Fields
        
        [Header("Initialization")]
        [SerializeField, Tooltip("Initialize the logging system when this component awakens")]
        private bool _initializeOnAwake = true;
        
        [SerializeField, Tooltip("Global minimum log level that will be processed by all targets")]
        private LogLevel _globalMinimumLevel = LogLevel.Debug;
        
        [Header("Performance Settings")]
        [SerializeField, Tooltip("Initial capacity for the internal log message queue")]
        private int _initialCapacity = 128;
        
        [SerializeField, Tooltip("Maximum number of messages to process in a single flush operation")]
        private int _maxMessagesPerFlush = 200;
        
        [SerializeField, Tooltip("Maximum number of messages to process per frame to avoid hitches")]
        private int _maxMessagesPerFrame = 100;
        
        [Header("Auto-Flush Configuration")]
        [SerializeField, Tooltip("Enable automatic flushing of log messages on a timer")]
        private bool _enableAutoFlush = true;
        
        [SerializeField, Tooltip("Interval in seconds between automatic flush operations")]
        private float _autoFlushInterval = 1.0f;
        
        [Header("Development Presets")]
        [SerializeField, Tooltip("Use development logging preset (debug level, console + file)")]
        private bool _useDevelopmentPreset = false;
        
        [SerializeField, Tooltip("Use production logging preset (warning level, optimized file)")]
        private bool _useProductionPreset = false;
        
        [SerializeField, Tooltip("Use high performance preset (minimal overhead, error level only)")]
        private bool _useHighPerformancePreset = false;
        
        [Header("Default Target Creation")]
        [SerializeField, Tooltip("Create a default Unity console logger target")]
        private bool _createDefaultUnityConsole = true;
        
        [SerializeField, Tooltip("Create a default file logger target")]
        private bool _createDefaultFileLogger = false;
        
        [SerializeField, Tooltip("Default path for file logger relative to persistent data path")]
        private string _defaultLogFilePath = "Logs/app.log";
        
        [Header("Target Configurations")]
        [SerializeField, Tooltip("ScriptableObject configurations for log targets")]
        private ScriptableObject[] _targetConfigs = new ScriptableObject[0];
        
        [Header("Dependency Injection")]
        [SerializeField, Tooltip("Use dependency injection to resolve logging dependencies")]
        private bool _useDependencyInjection = true;
        
        #endregion
        
        #region Private Fields
        
        private JobLoggerManager _loggerManager;
        private UnityLoggerAdapter _unityLoggerAdapter;
        private JobLoggerToBurstAdapter _burstLoggerAdapter;
        private IMessageBus _messageBus;
        private ILogFormatter _logFormatter;
        private IDependencyInjector _dependencyInjector;
        private ILogConfigRegistry _configRegistry;
        
        private readonly List<ILogTargetConfig> _validatedConfigs = new List<ILogTargetConfig>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        
        private bool _isInitialized;
        private bool _isDisposing;
        private float _accumulatedDeltaTime;
        
        // Cached references for performance
        private JobLogger _cachedJobLogger;
        private bool _hasDefaultJobLogger;
        
        // Performance tracking
        private int _totalMessagesProcessed;
        private float _lastFlushTime;
        private int _flushCount;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets whether the logging system has been initialized successfully.
        /// </summary>
        public bool IsInitialized => _isInitialized && _loggerManager != null && !_isDisposing;
        
        /// <summary>
        /// Gets the number of active log targets currently registered.
        /// </summary>
        public int TargetCount => _loggerManager?.TargetCount ?? 0;
        
        /// <summary>
        /// Gets the number of queued messages waiting to be processed.
        /// </summary>
        public int QueuedMessageCount => _loggerManager?.QueuedMessageCount ?? 0;
        
        /// <summary>
        /// Gets or sets the global minimum log level for all targets.
        /// Changes to this property are applied immediately to the underlying logger manager.
        /// </summary>
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set
            {
                if (_globalMinimumLevel != value)
                {
                    var oldLevel = _globalMinimumLevel;
                    _globalMinimumLevel = value;
                    
                    if (IsInitialized)
                    {
                        _loggerManager.GlobalMinimumLevel = value;
                        PublishLogLevelChanged(oldLevel, value);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the job logger manager instance.
        /// </summary>
        public JobLoggerManager LoggerManager => _loggerManager;
        
        /// <summary>
        /// Gets the Unity logger adapter instance.
        /// </summary>
        public UnityLoggerAdapter UnityLoggerAdapter => _unityLoggerAdapter;
        
        /// <summary>
        /// Gets the burst logger adapter instance.
        /// </summary>
        public IBurstLogger BurstLoggerAdapter => _burstLoggerAdapter;
        
        /// <summary>
        /// Gets the message bus instance used by the logging system.
        /// </summary>
        public IMessageBus MessageBus => _messageBus;
        
        /// <summary>
        /// Gets the total number of messages processed since initialization.
        /// </summary>
        public int TotalMessagesProcessed => _totalMessagesProcessed;
        
        /// <summary>
        /// Gets the total number of flush operations performed.
        /// </summary>
        public int FlushCount => _flushCount;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeAsync();
            }
        }
        
        private void Update()
        {
            if (!IsInitialized)
                return;
                
            _accumulatedDeltaTime += Time.unscaledDeltaTime;
            
            // Process pending log messages with frame limiting
            var processedCount = _loggerManager.Update(_accumulatedDeltaTime);
            _totalMessagesProcessed += processedCount;
            
            // Reset accumulated time after processing
            _accumulatedDeltaTime = 0f;
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        private void OnApplicationQuit()
        {
            if (IsInitialized)
            {
                Flush();
                Dispose();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (IsInitialized && pauseStatus)
            {
                // Flush logs when pausing to ensure nothing is lost
                Flush();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (IsInitialized && !hasFocus)
            {
                // Flush logs when losing focus
                Flush();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes the logging system asynchronously.
        /// This method is safe to call multiple times - subsequent calls will be ignored.
        /// </summary>
        public void InitializeAsync()
        {
            if (_isInitialized || _isDisposing)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Already initialized or disposing", this);
                return;
            }
            
            try
            {
                ValidateConfiguration();
                ResolveDependencies();
                ValidateAndCacheConfigs();
                InitializeLoggingSystem();
                SubscribeToMessages();
                
                _isInitialized = true;
                _lastFlushTime = Time.unscaledTime;
                
                Debug.Log($"[{nameof(LogManagerComponent)}] Logging system initialized successfully with {TargetCount} targets", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to initialize: {ex.Message}\n{ex.StackTrace}", this);
                CreateFallbackLogger();
            }
        }
        
        /// <summary>
        /// Validates the configuration settings before initialization.
        /// </summary>
        private void ValidateConfiguration()
        {
            // Ensure only one preset is selected
            var presetCount = new[] { _useDevelopmentPreset, _useProductionPreset, _useHighPerformancePreset }.Count(p => p);
            if (presetCount > 1)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Multiple presets selected, using development preset", this);
                _useDevelopmentPreset = true;
                _useProductionPreset = false;
                _useHighPerformancePreset = false;
            }
            
            // Validate performance settings
            _initialCapacity = Mathf.Max(16, _initialCapacity);
            _maxMessagesPerFlush = Mathf.Max(1, _maxMessagesPerFlush);
            _maxMessagesPerFrame = Mathf.Max(1, _maxMessagesPerFrame);
            _autoFlushInterval = Mathf.Max(0.1f, _autoFlushInterval);
            
            // Validate file path
            if (_createDefaultFileLogger && string.IsNullOrWhiteSpace(_defaultLogFilePath))
            {
                _defaultLogFilePath = "Logs/app.log";
            }
        }
        
        /// <summary>
        /// Resolves dependencies through dependency injection or creates defaults.
        /// </summary>
        private void ResolveDependencies()
        {
            if (_useDependencyInjection)
            {
                TryResolveDependenciesFromContainer();
            }
            
            CreateDefaultDependencies();
        }
        
        /// <summary>
        /// Attempts to resolve dependencies from a dependency injection container.
        /// </summary>
        private void TryResolveDependenciesFromContainer()
        {
            try
            {
                // Try to find a dependency injector in the scene
                var injectorComponent = FindObjectOfType<MonoBehaviour>() as IDependencyInjector;
                if (injectorComponent != null)
                {
                    _dependencyInjector = injectorComponent;
                    
                    // Resolve optional dependencies
                    try { _messageBus ??= _dependencyInjector.Resolve<IMessageBus>(); } catch { }
                    try { _logFormatter ??= _dependencyInjector.Resolve<ILogFormatter>(); } catch { }
                    try { _configRegistry ??= _dependencyInjector.Resolve<ILogConfigRegistry>(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Failed to resolve dependencies: {ex.Message}", this);
            }
        }
        
        /// <summary>
        /// Creates default implementations for missing dependencies.
        /// </summary>
        private void CreateDefaultDependencies()
        {
            // Create null message bus if none was resolved
            _messageBus ??= new NullMessageBus();
            
            // Create default log formatter if none was resolved
            _logFormatter ??= new DefaultLogFormatter();
            
            // Create default config registry if none was resolved
            _configRegistry ??= DefaultLogConfigRegistry.Instance;
        }
        
        /// <summary>
        /// Validates target configurations and caches valid ones.
        /// </summary>
        private void ValidateAndCacheConfigs()
        {
            _validatedConfigs.Clear();
            
            if (_targetConfigs != null && _targetConfigs.Length > 0)
            {
                foreach (var configObject in _targetConfigs)
                {
                    if (ValidateTargetConfig(configObject, out var validConfig))
                    {
                        _validatedConfigs.Add(validConfig);
                        
                        // Register with config registry if available
                        try
                        {
                            _configRegistry?.RegisterConfig(validConfig.TargetName, validConfig);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[{nameof(LogManagerComponent)}] Failed to register config {validConfig.TargetName}: {ex.Message}", this);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Validates a target configuration ScriptableObject.
        /// </summary>
        /// <param name="configObject">The configuration object to validate.</param>
        /// <param name="validConfig">The validated configuration if successful.</param>
        /// <returns>True if validation succeeded; otherwise, false.</returns>
        private bool ValidateTargetConfig(ScriptableObject configObject, out ILogTargetConfig validConfig)
        {
            validConfig = null;
            
            if (configObject == null)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Null config object found", this);
                return false;
            }
                
            validConfig = configObject as ILogTargetConfig;
            if (validConfig == null)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Config object {configObject.name} does not implement ILogTargetConfig", this);
                return false;
            }
            
            if (string.IsNullOrEmpty(validConfig.TargetName))
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Config {configObject.name} has empty target name", this);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Initializes the core logging system using preset patterns or custom configuration.
        /// </summary>
        private void InitializeLoggingSystem()
        {
            // Use preset creation patterns if specified
            if (_useDevelopmentPreset)
            {
                InitializeDevelopmentPreset();
            }
            else if (_useProductionPreset)
            {
                InitializeProductionPreset();
            }
            else if (_useHighPerformancePreset)
            {
                InitializeHighPerformancePreset();
            }
            else
            {
                InitializeCustomConfiguration();
            }
            
            ConfigureAutoFlush();
            InitializeAdapters();
        }
        
        /// <summary>
        /// Initializes the development preset configuration.
        /// </summary>
        private void InitializeDevelopmentPreset()
        {
            var (manager, logger) = JobLoggerFactory.CreateForDevelopment(
                _defaultLogFilePath, 
                Tagging.LogTag.Unity, 
                _messageBus
            );
            
            _loggerManager = manager;
            _cachedJobLogger = logger;
            _hasDefaultJobLogger = true;
            
            _disposables.Add(_loggerManager);
        }
        
        /// <summary>
        /// Initializes the production preset configuration.
        /// </summary>
        private void InitializeProductionPreset()
        {
            var (manager, logger) = JobLoggerFactory.CreateForProduction(
                _defaultLogFilePath, 
                Tagging.LogTag.Unity, 
                _messageBus
            );
            
            _loggerManager = manager;
            _cachedJobLogger = logger;
            _hasDefaultJobLogger = true;
            
            _disposables.Add(_loggerManager);
        }
        
        /// <summary>
        /// Initializes the high performance preset configuration.
        /// </summary>
        private void InitializeHighPerformancePreset()
        {
            var (manager, logger) = JobLoggerFactory.CreateHighPerformance(
                _defaultLogFilePath, 
                Tagging.LogTag.Unity, 
                _messageBus
            );
            
            _loggerManager = manager;
            _cachedJobLogger = logger;
            _hasDefaultJobLogger = true;
            
            _disposables.Add(_loggerManager);
        }
        
        /// <summary>
        /// Initializes custom configuration using ScriptableObject configs or defaults.
        /// </summary>
        private void InitializeCustomConfiguration()
        {
            if (_validatedConfigs.Count > 0)
            {
                InitializeFromConfigurations();
            }
            else if (_createDefaultUnityConsole || _createDefaultFileLogger)
            {
                InitializeDefaultConfiguration();
            }
            else
            {
                // Ultimate fallback - create basic logger manager
                _loggerManager = JobLoggerManager.CreateWithDefaultFormatter(
                    _initialCapacity,
                    _maxMessagesPerFlush,
                    _globalMinimumLevel,
                    _messageBus
                );
                _disposables.Add(_loggerManager);
            }
        }
        
        /// <summary>
        /// Initializes from validated configurations using the builder pattern.
        /// </summary>
        private void InitializeFromConfigurations()
        {
            var (manager, logger) = JobLoggerFactory.CreateComplete(
                builderCollection => ConfigureTargetsFromConfigs(builderCollection),
                _logFormatter,
                _globalMinimumLevel,
                Tagging.LogTag.Unity,
                _messageBus
            );
            
            _loggerManager = manager;
            _cachedJobLogger = logger;
            _hasDefaultJobLogger = true;
            
            _disposables.Add(_loggerManager);
        }
        
        /// <summary>
        /// Configures targets from validated configurations using the builder pattern.
        /// </summary>
        /// <param name="builderCollection">The target builder collection to configure.</param>
        private void ConfigureTargetsFromConfigs(JobLoggerManager.LogTargetBuilderCollection builderCollection)
        {
            foreach (var config in _validatedConfigs)
            {
                try
                {
                    // Create target directly from config and add to collection
                    var target = config.CreateTarget(_messageBus);
                    if (target != null)
                    {
                        builderCollection.AddTarget(target);
                        _disposables.Add(target);
                    }
                    else
                    {
                        Debug.LogWarning($"[{nameof(LogManagerComponent)}] Failed to create target from config {config.TargetName}", this);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to configure target from {config.TargetName}: {ex.Message}", this);
                }
            }
        }
        
        /// <summary>
        /// Initializes default configuration when no custom configs are provided.
        /// </summary>
        private void InitializeDefaultConfiguration()
        {
            // Create default targets based on inspector settings
            var targets = new List<ILogTarget>();
            
            if (_createDefaultUnityConsole)
            {
                try
                {
                    var consoleConfig = ScriptableObject.CreateInstance<UnityConsoleTargetConfig>();
                    consoleConfig.TargetName = "DefaultUnityConsole";
                    consoleConfig.Enabled = true;
                    consoleConfig.MinimumLevel = _globalMinimumLevel;
                    consoleConfig.UseColorizedOutput = true;
                    consoleConfig.CaptureUnityLogs = true;
                    
                    var consoleTarget = consoleConfig.CreateTarget(_messageBus);
                    if (consoleTarget != null)
                    {
                        targets.Add(consoleTarget);
                        _disposables.Add(consoleTarget);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to create default Unity console target: {ex.Message}", this);
                }
            }
            
            if (_createDefaultFileLogger)
            {
                try
                {
                    var fileConfig = ScriptableObject.CreateInstance<SerilogFileTargetConfig>();
                    fileConfig.TargetName = "DefaultFileLogger";
                    fileConfig.Enabled = true;
                    fileConfig.MinimumLevel = _globalMinimumLevel;
                    fileConfig.LogFilePath = _defaultLogFilePath;
                    fileConfig.UseJsonFormat = false;
                    fileConfig.AutoFlush = true;
                    
                    var fileTarget = fileConfig.CreateTarget(_messageBus);
                    if (fileTarget != null)
                    {
                        targets.Add(fileTarget);
                        _disposables.Add(fileTarget);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to create default file target: {ex.Message}", this);
                }
            }
            
            // Create logger manager with the targets
            if (targets.Count > 0)
            {
                _loggerManager = JobLoggerManager.CreateWithTargetsAndDefaultFormatter(
                    targets,
                    _initialCapacity,
                    _maxMessagesPerFlush,
                    _globalMinimumLevel,
                    _messageBus
                );
                
                // Create a default job logger
                _cachedJobLogger = _loggerManager.CreateJobLogger(_globalMinimumLevel, Tagging.LogTag.Unity);
                _hasDefaultJobLogger = true;
            }
            else
            {
                // Ultimate fallback - create basic logger manager
                _loggerManager = JobLoggerManager.CreateWithDefaultFormatter(
                    _initialCapacity,
                    _maxMessagesPerFlush,
                    _globalMinimumLevel,
                    _messageBus
                );
            }
            
            _disposables.Add(_loggerManager);
        }
        
        /// <summary>
        /// Configures auto-flush behavior.
        /// </summary>
        private void ConfigureAutoFlush()
        {
            if (_enableAutoFlush && _autoFlushInterval > 0f)
            {
                _loggerManager.EnableAutoFlush(_autoFlushInterval);
            }
            else
            {
                _loggerManager.DisableAutoFlush();
            }
        }
        
        /// <summary>
        /// Initializes adapters for Unity and Burst integration.
        /// </summary>
        private void InitializeAdapters()
        {
            try
            {
                // Initialize Unity logger adapter if Unity console is configured
                var unityConsoleConfig = _validatedConfigs.OfType<UnityConsoleTargetConfig>().FirstOrDefault() ??
                                        (_createDefaultUnityConsole ? CreateDefaultUnityConsoleConfig() : null);
                
                if (unityConsoleConfig?.CaptureUnityLogs == true)
                {
                    _burstLoggerAdapter = new JobLoggerToBurstAdapter(_loggerManager, _globalMinimumLevel);
                    _unityLoggerAdapter = new UnityLoggerAdapter(_burstLoggerAdapter, unityConsoleConfig);
                    
                    _disposables.Add(_burstLoggerAdapter);
                    _disposables.Add(_unityLoggerAdapter);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to initialize adapters: {ex.Message}", this);
            }
        }
        
        /// <summary>
        /// Creates a default Unity console configuration for adapter initialization.
        /// </summary>
        /// <returns>Default Unity console configuration.</returns>
        private UnityConsoleTargetConfig CreateDefaultUnityConsoleConfig()
        {
            var config = ScriptableObject.CreateInstance<UnityConsoleTargetConfig>();
            config.TargetName = "AdapterUnityConsole";
            config.Enabled = true;
            config.MinimumLevel = _globalMinimumLevel;
            config.UseColorizedOutput = true;
            config.CaptureUnityLogs = true;
            config.RegisterUnityLogHandler = true;
            config.DuplicateToOriginalHandler = false;
            
            return config;
        }
        
        /// <summary>
        /// Creates a fallback logger when initialization fails.
        /// </summary>
        private void CreateFallbackLogger()
        {
            try
            {
                var (manager, logger) = JobLoggerFactory.CreateConsoleOnly(LogLevel.Warning, false, Tagging.LogTag.Unity, new NullMessageBus());
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
                _isInitialized = true;
                
                _disposables.Add(_loggerManager);
                
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Fallback logger created", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to create fallback logger: {ex.Message}", this);
            }
        }
        
        #endregion
        
        #region Message Subscription
        
        /// <summary>
        /// Subscribes to relevant messages from the message bus.
        /// </summary>
        private void SubscribeToMessages()
        {
            if (_messageBus == null)
                return;
                
            try
            {
                // Subscribe to log processing messages for metrics
                var processingSubscription = _messageBus.SubscribeToMessage<LogProcessingMessage>(OnLogProcessingMessage);
                if (processingSubscription != null)
                {
                    _disposables.Add(processingSubscription);
                }
                
                // Subscribe to flush messages for tracking
                var flushSubscription = _messageBus.SubscribeToMessage<LogFlushMessage>(OnLogFlushMessage);
                if (flushSubscription != null)
                {
                    _disposables.Add(flushSubscription);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Failed to subscribe to some messages: {ex.Message}", this);
            }
        }
        
        /// <summary>
        /// Handles log processing messages for metrics tracking.
        /// </summary>
        private void OnLogProcessingMessage(LogProcessingMessage message)
        {
            _totalMessagesProcessed += message.ProcessedCount;
        }
        
        /// <summary>
        /// Handles log flush messages for tracking.
        /// </summary>
        private void OnLogFlushMessage(LogFlushMessage message)
        {
            _flushCount++;
            _lastFlushTime = Time.unscaledTime;
        }
        
        /// <summary>
        /// Publishes a log level changed message.
        /// </summary>
        private void PublishLogLevelChanged(LogLevel oldLevel, LogLevel newLevel)
        {
            try
            {
                var message = new LogLevelChangedMessage(oldLevel, newLevel);
                _messageBus?.PublishMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{nameof(LogManagerComponent)}] Failed to publish log level changed message: {ex.Message}", this);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Flushes all log targets and returns the number of messages flushed.
        /// This operation is thread-safe and can be called from any thread.
        /// </summary>
        /// <returns>Number of messages flushed, or 0 if the system is not initialized.</returns>
        public int Flush()
        {
            if (!IsInitialized)
                return 0;
                
            try
            {
                var flushedCount = _loggerManager.Flush();
                
                // Publish flush message
                try
                {
                    var flushMessage = new LogFlushMessage(flushedCount, Time.unscaledTime - _lastFlushTime);
                    _messageBus?.PublishMessage(flushMessage);
                }
                catch
                {
                    // Ignore message publishing errors
                }
                
                return flushedCount;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(LogManagerComponent)}] Error during flush: {ex.Message}", this);
                return 0;
            }
        }
        
        /// <summary>
        /// Adds a custom log target to the manager.
        /// The target will be automatically disposed when this component is destroyed.
        /// </summary>
        /// <param name="target">The target to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the system is not initialized.</exception>
        public void AddCustomTarget(ILogTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
                
            if (!IsInitialized)
                throw new InvalidOperationException("LogManagerComponent is not initialized");
            
            _loggerManager.AddTarget(target);
            _disposables.Add(target);
        }
        
        /// <summary>
        /// Creates and adds a target from configuration.
        /// </summary>
        /// <param name="config">The configuration to create a target from.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the system is not initialized.</exception>
        public void AddTargetFromConfig(ILogTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            if (!IsInitialized)
                throw new InvalidOperationException("LogManagerComponent is not initialized");
                
            var target = config.CreateTarget(_messageBus);
            if (target != null)
            {
                AddCustomTarget(target);
                
                // Register with config registry
                try
                {
                    _configRegistry?.RegisterConfig(config.TargetName, config);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{nameof(LogManagerComponent)}] Failed to register config: {ex.Message}", this);
                }
            }
        }
        
        /// <summary>
        /// Removes a target from the manager.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns>True if the target was removed; otherwise, false.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            if (target == null || !IsInitialized)
                return false;
                
            var removed = _loggerManager.RemoveTarget(target);
            if (removed)
            {
                _disposables.Remove(target);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Creates a job logger for use in Unity job contexts.
        /// </summary>
        /// <param name="minimumLevel">Optional minimum level override.</param>
        /// <param name="defaultTag">Default tag for messages.</param>
        /// <returns>A configured job logger.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system is not initialized.</exception>
        public JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("LogManagerComponent is not initialized");
                
            return _loggerManager.CreateJobLogger(minimumLevel, defaultTag);
        }
        
        /// <summary>
        /// Gets the default job logger created during initialization.
        /// </summary>
        /// <returns>The default job logger if available.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no default job logger is available.</exception>
        public JobLogger GetDefaultJobLogger()
        {
            if (!_hasDefaultJobLogger)
                throw new InvalidOperationException("No default job logger available");
                
            return _cachedJobLogger;
        }
        
        /// <summary>
        /// Creates a parallel job logger using the factory.
        /// </summary>
        /// <param name="queue">The native queue to use.</param>
        /// <param name="minimumLevel">Minimum log level.</param>
        /// <param name="defaultTag">Default tag for messages.</param>
        /// <returns>A parallel job logger.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system is not initialized.</exception>
        /// <exception cref="ArgumentException">Thrown if the queue is not created.</exception>
        public JobLogger CreateParallelJobLogger(NativeQueue<LogMessage> queue, LogLevel minimumLevel, Tagging.LogTag defaultTag = default)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("LogManagerComponent is not initialized");
                
            if (!queue.IsCreated)
                throw new ArgumentException("Queue must be created before creating a JobLogger", nameof(queue));
                
            return JobLoggerFactory.CreateParallel(queue, minimumLevel, defaultTag);
        }
        
        /// <summary>
        /// Updates Unity logger configuration.
        /// </summary>
        /// <param name="config">The new configuration to apply.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        public void UpdateUnityLoggerConfig(UnityConsoleTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            // Update the adapter if it exists
            if (_unityLoggerAdapter != null)
            {
                // Dispose old adapter and create new one
                _disposables.Remove(_unityLoggerAdapter);
                _unityLoggerAdapter.Dispose();
                
                try
                {
                    _unityLoggerAdapter = new UnityLoggerAdapter(_burstLoggerAdapter, config);
                    _disposables.Add(_unityLoggerAdapter);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to update Unity logger config: {ex.Message}", this);
                }
            }
        }
        
        /// <summary>
        /// Sets auto-flush behavior.
        /// </summary>
        /// <param name="enabled">Whether auto-flush is enabled.</param>
        /// <param name="interval">Flush interval in seconds.</param>
        public void SetAutoFlush(bool enabled, float interval = 1.0f)
        {
            _enableAutoFlush = enabled;
            _autoFlushInterval = Mathf.Max(0.1f, interval);
            
            if (IsInitialized)
            {
                if (enabled)
                {
                    _loggerManager.EnableAutoFlush(_autoFlushInterval);
                }
                else
                {
                    _loggerManager.DisableAutoFlush();
                }
            }
        }
        
        /// <summary>
        /// Gets logging performance metrics.
        /// </summary>
        /// <returns>Dictionary containing performance metrics.</returns>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["IsInitialized"] = IsInitialized,
                ["TargetCount"] = TargetCount,
                ["QueuedMessageCount"] = QueuedMessageCount,
                ["TotalMessagesProcessed"] = _totalMessagesProcessed,
                ["FlushCount"] = _flushCount,
                ["LastFlushTime"] = _lastFlushTime,
                ["GlobalMinimumLevel"] = _globalMinimumLevel.ToString(),
                ["AutoFlushEnabled"] = _enableAutoFlush,
                ["AutoFlushInterval"] = _autoFlushInterval
            };
            
            if (IsInitialized)
            {
                try
                {
                    // Add additional metrics from the logger manager if available
                    metrics["ManagerTargetCount"] = _loggerManager.TargetCount;
                    metrics["ManagerQueuedMessages"] = _loggerManager.QueuedMessageCount;
                }
                catch (Exception ex)
                {
                    metrics["MetricsError"] = ex.Message;
                }
            }
            
            return metrics;
        }
        
        /// <summary>
        /// Resets performance metrics counters.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _totalMessagesProcessed = 0;
            _flushCount = 0;
            _lastFlushTime = Time.unscaledTime;
        }
        
        #endregion
        
        #region Logging Methods
        
        /// <summary>
        /// Logs a message with the specified level and tag.
        /// This method is thread-safe and can be called from any thread.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message to log.</param>
        public void Log(LogLevel level, Tagging.LogTag tag, string message)
        {
            if (!IsInitialized || string.IsNullOrEmpty(message))
                return;
                
            try
            {
                _loggerManager.Log(level, tag, message);
            }
            catch (Exception ex)
            {
                // Fallback to Unity's built-in logging if our system fails
                Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to log message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogDebug(string message, Tagging.LogTag tag = default)
        {
            var actualTag = tag == default ? Tagging.LogTag.Debug : tag;
            Log(LogLevel.Debug, actualTag, message);
        }
        
        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogInfo(string message, Tagging.LogTag tag = default)
        {
            var actualTag = tag == default ? Tagging.LogTag.Info : tag;
            Log(LogLevel.Info, actualTag, message);
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogWarning(string message, Tagging.LogTag tag = default)
        {
            var actualTag = tag == default ? Tagging.LogTag.Warning : tag;
            Log(LogLevel.Warning, actualTag, message);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogError(string message, Tagging.LogTag tag = default)
        {
            var actualTag = tag == default ? Tagging.LogTag.Error : tag;
            Log(LogLevel.Error, actualTag, message);
        }
        
        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogCritical(string message, Tagging.LogTag tag = default)
        {
            var actualTag = tag == default ? Tagging.LogTag.Critical : tag;
            Log(LogLevel.Critical, actualTag, message);
        }
        
        /// <summary>
        /// Logs an exception with full stack trace.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Optional additional message.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogException(Exception exception, string message = null, Tagging.LogTag tag = default)
        {
            if (exception == null)
                return;
                
            var actualTag = tag == default ? Tagging.LogTag.Exception : tag;
            var fullMessage = string.IsNullOrEmpty(message) 
                ? exception.ToString() 
                : $"{message}\n{exception}";
                
            Log(LogLevel.Critical, actualTag, fullMessage);
        }
        
        /// <summary>
        /// Logs a message with structured properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="properties">Structured properties to include.</param>
        public void LogWithProperties(LogLevel level, Tagging.LogTag tag, string message, LogProperties properties)
        {
            if (!IsInitialized || string.IsNullOrEmpty(message))
                return;
                
            try
            {
                _loggerManager.Log(level, tag, message, properties);
            }
            catch (Exception ex)
            {
                // Fallback to simple logging
                Debug.LogError($"[{nameof(LogManagerComponent)}] Failed to log structured message: {ex.Message}");
                Log(level, tag, message);
            }
        }
        
        #endregion
        
        #region Configuration Management
        
        /// <summary>
        /// Gets the configuration registry used by this component.
        /// </summary>
        /// <returns>The configuration registry instance.</returns>
        public ILogConfigRegistry GetConfigRegistry()
        {
            return _configRegistry;
        }
        
        /// <summary>
        /// Registers a configuration with the registry.
        /// </summary>
        /// <param name="name">Name for the configuration.</param>
        /// <param name="config">Configuration to register.</param>
        /// <exception cref="ArgumentNullException">Thrown if parameters are null.</exception>
        public void RegisterConfig(string name, ILogTargetConfig config)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            _configRegistry?.RegisterConfig(name, config);
        }
        
        /// <summary>
        /// Gets a configuration by name.
        /// </summary>
        /// <param name="name">Name of the configuration.</param>
        /// <returns>The configuration if found, null otherwise.</returns>
        public ILogTargetConfig GetConfig(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
                
            return _configRegistry?.GetConfig(name);
        }
        
        /// <summary>
        /// Gets a strongly-typed configuration by name.
        /// </summary>
        /// <typeparam name="TConfig">Type of configuration to retrieve.</typeparam>
        /// <param name="name">Name of the configuration.</param>
        /// <returns>The typed configuration if found, null otherwise.</returns>
        public TConfig GetConfig<TConfig>(string name) where TConfig : class, ILogTargetConfig
        {
            return _configRegistry?.GetConfig<TConfig>(name);
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Disposes of all resources and cleans up the logging system.
        /// This method is safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposing)
                return;
                
            _isDisposing = true;
            
            try
            {
                // Final flush before disposal
                Flush();
                
                // Dispose all registered disposables in reverse order
                for (int i = _disposables.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _disposables[i]?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{nameof(LogManagerComponent)}] Error disposing resource: {ex.Message}", this);
                    }
                }
                
                _disposables.Clear();
                _validatedConfigs.Clear();
                
                // Clear references
                _loggerManager = null;
                _unityLoggerAdapter = null;
                _burstLoggerAdapter = null;
                _cachedJobLogger = default;
                _hasDefaultJobLogger = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(LogManagerComponent)}] Error during disposal: {ex.Message}", this);
            }
            finally
            {
                _isInitialized = false;
                _isDisposing = false;
            }
        }
        
        #endregion
        
        #region Editor Support
        
        private void OnValidate()
        {
            ValidateConfiguration();
        }
        
        /// <summary>
        /// Gets debug information about the current state of the logging system.
        /// </summary>
        /// <returns>Debug information string.</returns>
        public string GetDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"LogManagerComponent Debug Info:");
            info.AppendLine($"  Initialized: {IsInitialized}");
            info.AppendLine($"  Disposing: {_isDisposing}");
            info.AppendLine($"  Target Count: {TargetCount}");
            info.AppendLine($"  Queued Messages: {QueuedMessageCount}");
            info.AppendLine($"  Total Processed: {_totalMessagesProcessed}");
            info.AppendLine($"  Flush Count: {_flushCount}");
            info.AppendLine($"  Global Min Level: {_globalMinimumLevel}");
            info.AppendLine($"  Auto Flush: {_enableAutoFlush} ({_autoFlushInterval}s)");
            info.AppendLine($"  Has Default Logger: {_hasDefaultJobLogger}");
            info.AppendLine($"  Unity Adapter: {(_unityLoggerAdapter != null ? "Active" : "None")}");
            info.AppendLine($"  Burst Adapter: {(_burstLoggerAdapter != null ? "Active" : "None")}");
            info.AppendLine($"  Validated Configs: {_validatedConfigs.Count}");
            info.AppendLine($"  Disposables: {_disposables.Count}");
            
            return info.ToString();
        }
        
        #endregion
    }
}