using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Adapters;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Providers.Unity;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Unity MonoBehaviour component that manages the logging system lifecycle.
    /// Integrates with the dependency injection system for proper service resolution.
    /// Provides high-performance job-based logging with burst compilation support.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class LogManagerComponent : MonoBehaviour, IDisposable
    {
        #region Serialized Configuration

        [Header("Logging Configuration")]
        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Debug;
        [SerializeField] private LoggingPreset _loggingPreset = LoggingPreset.Development;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _persistBetweenScenes = false;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableAutoFlush = true;
        [SerializeField] private float _autoFlushInterval = 1.0f;
        [SerializeField] private int _initialMessageQueueCapacity = 1024;
        [SerializeField] private bool _enableBurstCompilation = true;

        [Header("Target Configurations")]
        [SerializeField] private ScriptableObject[] _targetConfigurations = Array.Empty<ScriptableObject>();

        [Header("Development Settings")]
        [SerializeField] private bool _enableDebugLogging = false;

        #endregion

        #region Private Fields

        // Core logging system
        private JobLoggerManager _loggerManager;
        private UnityLoggerAdapter _unityLoggerAdapter;
        private IBurstLogger _burstLoggerAdapter;

        // Dependency injection
        private IDependencyProvider _dependencyProvider;
        private IMessageBus _messageBus;
        private ILogFormatter _logFormatter;
        private ILogConfigRegistry _configRegistry;

        // State management
        private bool _isInitialized;
        private bool _isDisposed;
        private int _totalMessagesProcessed;
        private int _flushCount;

        // Auto-flush coroutine
        private Coroutine _autoFlushCoroutine;

        // Message bus subscriptions for cleanup
        private readonly List<IDisposable> _messageSubscriptions = new List<IDisposable>();

        // Static instance for global access
        private static LogManagerComponent _globalInstance;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the logging system is fully initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;

        /// <summary>
        /// Gets the number of configured log targets.
        /// </summary>
        public int TargetCount => _loggerManager?.TargetCount ?? 0;

        /// <summary>
        /// Gets the number of messages currently queued for processing.
        /// </summary>
        public int QueuedMessageCount => _loggerManager?.QueuedMessageCount ?? 0;

        /// <summary>
        /// Gets or sets the global minimum logging level.
        /// </summary>
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set
            {
                if (_globalMinimumLevel == value) return;
                
                var oldLevel = _globalMinimumLevel;
                _globalMinimumLevel = value;
                
                if (_loggerManager != null)
                {
                    _loggerManager.SetGlobalMinimumLevel(value);
                }
                
                PublishLogLevelChanged(oldLevel, value);
            }
        }

        /// <summary>
        /// Gets the underlying job logger manager.
        /// </summary>
        public JobLoggerManager LoggerManager => _loggerManager;

        /// <summary>
        /// Gets the Unity logger adapter for standard Unity logging.
        /// </summary>
        public UnityLoggerAdapter UnityLoggerAdapter => _unityLoggerAdapter;

        /// <summary>
        /// Gets the burst-compatible logger adapter for high-performance scenarios.
        /// </summary>
        public IBurstLogger BurstLoggerAdapter => _burstLoggerAdapter;

        /// <summary>
        /// Gets the message bus used for logging events.
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Gets the total number of messages processed since initialization.
        /// </summary>
        public int TotalMessagesProcessed => _totalMessagesProcessed;

        /// <summary>
        /// Gets the number of flush operations performed.
        /// </summary>
        public int FlushCount => _flushCount;

        /// <summary>
        /// Gets the global instance of the log manager component.
        /// </summary>
        public static LogManagerComponent Global => _globalInstance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_isDisposed) return;

            // Handle singleton pattern for global persistence
            if (_persistBetweenScenes)
            {
                if (_globalInstance != null && _globalInstance != this)
                {
                    if (_enableDebugLogging)
                        Debug.Log($"[LogManagerComponent] Destroying duplicate instance on '{gameObject.name}'");
                    
                    Destroy(gameObject);
                    return;
                }

                _globalInstance = this;
                DontDestroyOnLoad(gameObject);
            }

            // Resolve dependencies first
            ResolveDependencies();

            // Auto-initialize if configured
            if (_autoInitialize)
            {
                _ = InitializeAsync();
            }
        }

        private void Update()
        {
            if (!_isInitialized || _isDisposed) return;

            // Update performance metrics
            if (_loggerManager != null)
            {
                _totalMessagesProcessed = _loggerManager.TotalMessagesProcessed;
            }
        }

        private void OnDestroy()
        {
            if (_globalInstance == this)
            {
                _globalInstance = null;
            }

            Dispose();
        }

        private void OnApplicationQuit()
        {
            if (_isInitialized && !_isDisposed)
            {
                // Final flush before application quits
                Flush();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!_isInitialized || _isDisposed) return;

            if (pauseStatus)
            {
                // Flush logs when pausing
                Flush();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!_isInitialized || _isDisposed) return;

            if (!hasFocus)
            {
                // Flush logs when losing focus
                Flush();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the logging system asynchronously.
        /// </summary>
        /// <returns>A task representing the initialization operation.</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized || _isDisposed)
            {
                if (_enableDebugLogging)
                    Debug.LogWarning($"[LogManagerComponent] Already initialized or disposed on '{gameObject.name}'");
                return;
            }

            try
            {
                if (_enableDebugLogging)
                    Debug.Log($"[LogManagerComponent] Starting async initialization on '{gameObject.name}'");

                // Validate configuration
                ValidateConfiguration();

                // Resolve additional dependencies if not already resolved
                if (_dependencyProvider == null)
                {
                    ResolveDependencies();
                }

                // Create default dependencies if needed
                CreateDefaultDependencies();

                // Validate and cache configurations
                ValidateAndCacheConfigs();

                // Initialize the logging system
                InitializeLoggingSystem();

                // Initialize adapters
                InitializeAdapters();

                // Configure auto-flush
                ConfigureAutoFlush();

                // Subscribe to messages
                SubscribeToMessages();

                _isInitialized = true;

                if (_enableDebugLogging)
                    Debug.Log($"[LogManagerComponent] Successfully initialized on '{gameObject.name}'");

                // Allow a frame for other systems to complete initialization
                await Task.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Failed to initialize: {ex.Message}", this);
                CreateFallbackLogger();
                throw;
            }
        }

        /// <summary>
        /// Validates the component configuration.
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_initialMessageQueueCapacity < 64)
            {
                Debug.LogWarning($"[LogManagerComponent] Message queue capacity ({_initialMessageQueueCapacity}) is very low, setting to minimum of 64");
                _initialMessageQueueCapacity = 64;
            }

            if (_autoFlushInterval <= 0f)
            {
                Debug.LogWarning($"[LogManagerComponent] Invalid auto-flush interval ({_autoFlushInterval}), setting to default 1.0s");
                _autoFlushInterval = 1.0f;
            }
        }

        /// <summary>
        /// Resolves dependencies using the dependency injection system.
        /// </summary>
        private void ResolveDependencies()
        {
            try
            {
                // Try to get the dependency provider from the scene
                var unityProvider = UnityDependencyProvider.Global;
                if (unityProvider != null && unityProvider.IsInitialized)
                {
                    _dependencyProvider = unityProvider;
                    
                    // Resolve optional dependencies with safe fallbacks
                    _messageBus = _dependencyProvider.ResolveOrDefault<IMessageBus>();
                    _logFormatter = _dependencyProvider.ResolveOrDefault<ILogFormatter>();
                    _configRegistry = _dependencyProvider.ResolveOrDefault<ILogConfigRegistry>();

                    if (_enableDebugLogging)
                        Debug.Log($"[LogManagerComponent] Successfully resolved dependencies from UnityDependencyProvider");
                }
                else
                {
                    if (_enableDebugLogging)
                        Debug.LogWarning($"[LogManagerComponent] No UnityDependencyProvider found, using fallback dependencies");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogManagerComponent] Failed to resolve dependencies: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Creates default dependencies when DI system is not available.
        /// </summary>
        private void CreateDefaultDependencies()
        {
            // Create message bus if not resolved
            if (_messageBus == null)
            {
                _messageBus = new MessageBus.Unity.UnityMessageBus();
                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] Created fallback UnityMessageBus");
            }

            // Create config registry if not resolved
            if (_configRegistry == null)
            {
                _configRegistry = new Configuration.LogConfigRegistry();
                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] Created fallback LogConfigRegistry");
            }

            // Register this component's configuration in the registry
            if (_configRegistry != null)
            {
                foreach (var configObject in _targetConfigurations)
                {
                    if (ValidateTargetConfig(configObject, out var validConfig))
                    {
                        var configName = $"{configObject.GetType().Name}_{configObject.GetInstanceID()}";
                        _configRegistry.RegisterConfig(configName, validConfig);
                    }
                }
            }
        }

        /// <summary>
        /// Validates and caches target configurations.
        /// </summary>
        private void ValidateAndCacheConfigs()
        {
            if (_targetConfigurations == null || _targetConfigurations.Length == 0)
            {
                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] No target configurations specified, will use default configuration");
                return;
            }

            int validConfigs = 0;
            foreach (var configObject in _targetConfigurations)
            {
                if (ValidateTargetConfig(configObject, out _))
                {
                    validConfigs++;
                }
            }

            if (_enableDebugLogging)
                Debug.Log($"[LogManagerComponent] Validated {validConfigs} out of {_targetConfigurations.Length} target configurations");
        }

        /// <summary>
        /// Validates a target configuration object.
        /// </summary>
        /// <param name="configObject">The configuration object to validate.</param>
        /// <param name="validConfig">The validated configuration interface.</param>
        /// <returns>True if the configuration is valid.</returns>
        private bool ValidateTargetConfig(ScriptableObject configObject, out ILogTargetConfig validConfig)
        {
            validConfig = null;

            if (configObject == null)
            {
                Debug.LogWarning("[LogManagerComponent] Null configuration object found in target configurations");
                return false;
            }

            if (configObject is ILogTargetConfig config)
            {
                validConfig = config;
                return true;
            }

            Debug.LogWarning($"[LogManagerComponent] Configuration object '{configObject.name}' does not implement ILogTargetConfig", configObject);
            return false;
        }

        /// <summary>
        /// Initializes the core logging system based on the configured preset.
        /// </summary>
        private void InitializeLoggingSystem()
        {
            switch (_loggingPreset)
            {
                case LoggingPreset.Development:
                    InitializeDevelopmentPreset();
                    break;
                case LoggingPreset.Production:
                    InitializeProductionPreset();
                    break;
                case LoggingPreset.HighPerformance:
                    InitializeHighPerformancePreset();
                    break;
                case LoggingPreset.Custom:
                    InitializeCustomConfiguration();
                    break;
                default:
                    InitializeDefaultConfiguration();
                    break;
            }
        }

        /// <summary>
        /// Initializes the development preset configuration.
        /// </summary>
        private void InitializeDevelopmentPreset()
        {
            _loggerManager = new JobLoggerManager(
                _initialMessageQueueCapacity,
                _globalMinimumLevel,
                enableBurst: _enableBurstCompilation
            );

            // Add Unity console target
            var unityConsoleConfig = CreateDefaultUnityConsoleConfig();
            _loggerManager.AddTarget(unityConsoleConfig.CreateTarget());

            if (_enableDebugLogging)
                Debug.Log("[LogManagerComponent] Initialized with Development preset");
        }

        /// <summary>
        /// Initializes the production preset configuration.
        /// </summary>
        private void InitializeProductionPreset()
        {
            _loggerManager = new JobLoggerManager(
                _initialMessageQueueCapacity,
                LogLevel.Info, // Higher minimum level for production
                enableBurst: true
            );

            // Only add essential targets for production
            var unityConsoleConfig = CreateDefaultUnityConsoleConfig();
            unityConsoleConfig.MinimumLevel = LogLevel.Warning; // Only warnings and errors
            _loggerManager.AddTarget(unityConsoleConfig.CreateTarget());

            if (_enableDebugLogging)
                Debug.Log("[LogManagerComponent] Initialized with Production preset");
        }

        /// <summary>
        /// Initializes the high-performance preset configuration.
        /// </summary>
        private void InitializeHighPerformancePreset()
        {
            _loggerManager = new JobLoggerManager(
                Math.Max(_initialMessageQueueCapacity, 2048), // Larger queue for high-performance
                LogLevel.Warning, // Minimal logging
                enableBurst: true
            );

            // Minimal targets for maximum performance
            if (_enableDebugLogging)
                Debug.Log("[LogManagerComponent] Initialized with High Performance preset");
        }

        /// <summary>
        /// Initializes custom configuration from target configurations.
        /// </summary>
        private void InitializeCustomConfiguration()
        {
            _loggerManager = new JobLoggerManager(
                _initialMessageQueueCapacity,
                _globalMinimumLevel,
                enableBurst: _enableBurstCompilation
            );

            if (_targetConfigurations?.Length > 0)
            {
                InitializeFromConfigurations();
            }
            else
            {
                // Fallback to default if no configurations
                InitializeDefaultConfiguration();
            }

            if (_enableDebugLogging)
                Debug.Log("[LogManagerComponent] Initialized with Custom configuration");
        }

        /// <summary>
        /// Initializes targets from the configured target configurations.
        /// </summary>
        private void InitializeFromConfigurations()
        {
            var builderCollection = _loggerManager.CreateBuilderCollection();
            ConfigureTargetsFromConfigs(builderCollection);
            builderCollection.Apply();
        }

        /// <summary>
        /// Configures targets from the validated configurations.
        /// </summary>
        /// <param name="builderCollection">The builder collection to configure.</param>
        private void ConfigureTargetsFromConfigs(JobLoggerManager.LogTargetBuilderCollection builderCollection)
        {
            foreach (var configObject in _targetConfigurations)
            {
                if (ValidateTargetConfig(configObject, out var validConfig))
                {
                    try
                    {
                        var target = validConfig.CreateTarget();
                        builderCollection.AddTarget(target);
                        
                        if (_enableDebugLogging)
                            Debug.Log($"[LogManagerComponent] Added target from configuration: {configObject.name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LogManagerComponent] Failed to create target from configuration '{configObject.name}': {ex.Message}", configObject);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the default configuration.
        /// </summary>
        private void InitializeDefaultConfiguration()
        {
            _loggerManager = new JobLoggerManager(
                _initialMessageQueueCapacity,
                _globalMinimumLevel,
                enableBurst: _enableBurstCompilation
            );

            // Add default Unity console target
            var unityConsoleConfig = CreateDefaultUnityConsoleConfig();
            _loggerManager.AddTarget(unityConsoleConfig.CreateTarget());

            if (_enableDebugLogging)
                Debug.Log("[LogManagerComponent] Initialized with Default configuration");
        }

        /// <summary>
        /// Configures the auto-flush functionality.
        /// </summary>
        private void ConfigureAutoFlush()
        {
            if (_enableAutoFlush && _autoFlushInterval > 0f)
            {
                SetAutoFlush(true, _autoFlushInterval);
            }
        }

        /// <summary>
        /// Initializes the logger adapters.
        /// </summary>
        private void InitializeAdapters()
        {
            if (_loggerManager == null) return;

            // Initialize Unity logger adapter
            _unityLoggerAdapter = new UnityLoggerAdapter(_loggerManager.GetDefaultJobLogger());

            // Initialize burst logger adapter
            _burstLoggerAdapter = _loggerManager.CreateBurstLogger(_globalMinimumLevel);

            if (_enableDebugLogging)
                Debug.Log("[LogManagerComponent] Initialized logger adapters");
        }

        /// <summary>
        /// Creates a default Unity console target configuration.
        /// </summary>
        /// <returns>A configured Unity console target configuration.</returns>
        private UnityConsoleTargetConfig CreateDefaultUnityConsoleConfig()
        {
            var config = ScriptableObject.CreateInstance<UnityConsoleTargetConfig>();
            config.MinimumLevel = _globalMinimumLevel;
            config.UseStackTrace = Application.isEditor;
            config.EnableColors = Application.isEditor;
            config.Formatter = _logFormatter;
            return config;
        }

        /// <summary>
        /// Creates a fallback logger when initialization fails.
        /// </summary>
        private void CreateFallbackLogger()
        {
            try
            {
                _loggerManager = new JobLoggerManager(64, LogLevel.Error, enableBurst: false);
                var fallbackConfig = CreateDefaultUnityConsoleConfig();
                fallbackConfig.MinimumLevel = LogLevel.Error;
                _loggerManager.AddTarget(fallbackConfig.CreateTarget());
                
                Debug.LogWarning("[LogManagerComponent] Created fallback logger with minimal configuration");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Failed to create fallback logger: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribes to relevant message bus messages.
        /// </summary>
        private void SubscribeToMessages()
        {
            if (_messageBus == null) return;

            try
            {
                // Subscribe to log processing messages
                var processingSubscription = _messageBus.SubscribeToMessage<LogProcessingMessage>(OnLogProcessingMessage);
                _messageSubscriptions.Add(processingSubscription);

                // Subscribe to log flush messages
                var flushSubscription = _messageBus.SubscribeToMessage<LogFlushMessage>(OnLogFlushMessage);
                _messageSubscriptions.Add(flushSubscription);

                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] Subscribed to message bus events");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogManagerComponent] Failed to subscribe to messages: {ex.Message}");
            }
        }

        #endregion

        #region Message Handlers

        /// <summary>
        /// Handles log processing messages from the message bus.
        /// </summary>
        /// <param name="message">The log processing message.</param>
        private void OnLogProcessingMessage(LogProcessingMessage message)
        {
            if (message?.LoggerName == _loggerManager?.Name)
            {
                _totalMessagesProcessed = message.TotalProcessed;
            }
        }

        /// <summary>
        /// Handles log flush messages from the message bus.
        /// </summary>
        /// <param name="message">The log flush message.</param>
        private void OnLogFlushMessage(LogFlushMessage message)
        {
            if (message?.LoggerName == _loggerManager?.Name)
            {
                _flushCount++;
            }
        }

        /// <summary>
        /// Publishes a log level changed message.
        /// </summary>
        /// <param name="oldLevel">The previous log level.</param>
        /// <param name="newLevel">The new log level.</param>
        private void PublishLogLevelChanged(LogLevel oldLevel, LogLevel newLevel)
        {
            try
            {
                _messageBus?.PublishMessage(new LogLevelChangedMessage
                {
                    LoggerName = _loggerManager?.Name ?? "LogManagerComponent",
                    OldLevel = oldLevel,
                    NewLevel = newLevel,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogManagerComponent] Failed to publish log level changed message: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Flushes all pending log messages to their targets.
        /// </summary>
        /// <returns>The number of messages flushed.</returns>
        public int Flush()
        {
            if (!_isInitialized || _isDisposed) return 0;

            try
            {
                var flushedCount = _loggerManager?.Flush() ?? 0;
                _flushCount++;
                return flushedCount;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Error during flush: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Adds a custom log target to the logging system.
        /// </summary>
        /// <param name="target">The log target to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when target is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when not initialized.</exception>
        public void AddCustomTarget(ILogTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");

            _loggerManager?.AddTarget(target);
        }

        /// <summary>
        /// Adds a log target from a configuration.
        /// </summary>
        /// <param name="config">The target configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when not initialized.</exception>
        public void AddTargetFromConfig(ILogTargetConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");

            var target = config.CreateTarget();
            _loggerManager?.AddTarget(target);
        }

        /// <summary>
        /// Removes a log target from the logging system.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns>True if the target was removed successfully.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            if (!_isInitialized || target == null) return false;
            return _loggerManager?.RemoveTarget(target) ?? false;
        }

        /// <summary>
        /// Creates a new job logger with optional parameters.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level for this logger.</param>
        /// <param name="defaultTag">The default tag for this logger.</param>
        /// <returns>A new job logger instance.</returns>
        public JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");
            return _loggerManager.CreateJobLogger(minimumLevel ?? _globalMinimumLevel, defaultTag);
        }

        /// <summary>
        /// Gets the default job logger from the manager.
        /// </summary>
        /// <returns>The default job logger.</returns>
        public JobLogger GetDefaultJobLogger()
        {
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");
            return _loggerManager.GetDefaultJobLogger();
        }

        /// <summary>
        /// Creates a parallel job logger for high-performance scenarios.
        /// </summary>
        /// <param name="queue">The native queue for log messages.</param>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <param name="defaultTag">The default tag.</param>
        /// <returns>A parallel job logger.</returns>
        public JobLogger CreateParallelJobLogger(NativeQueue<LogMessage> queue, LogLevel minimumLevel, Tagging.LogTag defaultTag = default)
        {
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");
            return _loggerManager.CreateParallelJobLogger(queue, minimumLevel, defaultTag);
        }

        /// <summary>
        /// Updates the Unity logger configuration.
        /// </summary>
        /// <param name="config">The new configuration.</param>
        public void UpdateUnityLoggerConfig(UnityConsoleTargetConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (!_isInitialized) return;

            _unityLoggerAdapter?.UpdateConfiguration(config);
        }

        /// <summary>
        /// Sets the auto-flush configuration.
        /// </summary>
        /// <param name="enabled">Whether auto-flush is enabled.</param>
        /// <param name="interval">The flush interval in seconds.</param>
        public void SetAutoFlush(bool enabled, float interval = 1.0f)
        {
            if (_autoFlushCoroutine != null)
            {
                StopCoroutine(_autoFlushCoroutine);
                _autoFlushCoroutine = null;
            }

            if (enabled && interval > 0f)
            {
                _autoFlushCoroutine = StartCoroutine(AutoFlushCoroutine(interval));
            }

            _enableAutoFlush = enabled;
            _autoFlushInterval = interval;
        }

        /// <summary>
        /// Auto-flush coroutine that periodically flushes log messages.
        /// </summary>
        /// <param name="interval">The flush interval in seconds.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator AutoFlushCoroutine(float interval)
        {
            var waitForSeconds = new WaitForSeconds(interval);
            
            while (_isInitialized && !_isDisposed)
            {
                yield return waitForSeconds;
                
                if (_isInitialized && !_isDisposed)
                {
                    Flush();
                }
            }
        }

        /// <summary>
        /// Gets performance metrics for the logging system.
        /// </summary>
        /// <returns>A dictionary containing performance metrics.</returns>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["IsInitialized"] = _isInitialized,
                ["TotalMessagesProcessed"] = _totalMessagesProcessed,
                ["FlushCount"] = _flushCount,
                ["TargetCount"] = TargetCount,
                ["QueuedMessageCount"] = QueuedMessageCount,
                ["GlobalMinimumLevel"] = _globalMinimumLevel.ToString(),
                ["AutoFlushEnabled"] = _enableAutoFlush,
                ["AutoFlushInterval"] = _autoFlushInterval
            };

            // Add logger manager metrics if available
            if (_loggerManager != null)
            {
                var managerMetrics = _loggerManager.GetPerformanceMetrics();
                foreach (var kvp in managerMetrics)
                {
                    metrics[$"Manager_{kvp.Key}"] = kvp.Value;
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
            _loggerManager?.ResetPerformanceMetrics();
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message to log.</param>
        public void Log(LogLevel level, Tagging.LogTag tag, string message)
        {
            if (!_isInitialized || level < _globalMinimumLevel) return;
            _unityLoggerAdapter?.Log(level, tag, message);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogDebug(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Debug, tag, message);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogInfo(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Info, tag, message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogWarning(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Warning, tag, message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogError(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Error, tag, message);
        }

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogCritical(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Critical, tag, message);
        }

        /// <summary>
        /// Logs an exception with optional message and tag.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Optional additional message.</param>
        /// <param name="tag">The log tag.</param>
        public void LogException(Exception exception, string message = null, Tagging.LogTag tag = default)
        {
            if (!_isInitialized) return;
            _unityLoggerAdapter?.LogException(exception, message, tag);
        }

        /// <summary>
        /// Logs a message with additional properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="properties">Additional properties.</param>
        public void LogWithProperties(LogLevel level, Tagging.LogTag tag, string message, LogProperties properties)
        {
            if (!_isInitialized || level < _globalMinimumLevel) return;
            _unityLoggerAdapter?.LogWithProperties(level, tag, message, properties);
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Gets the configuration registry.
        /// </summary>
        /// <returns>The configuration registry.</returns>
        public ILogConfigRegistry GetConfigRegistry()
        {
            return _configRegistry;
        }

        /// <summary>
        /// Registers a configuration in the registry.
        /// </summary>
        /// <param name="name">The configuration name.</param>
        /// <param name="config">The configuration to register.</param>
        public void RegisterConfig(string name, ILogTargetConfig config)
        {
            _configRegistry?.RegisterConfig(name, config);
        }

        /// <summary>
        /// Gets a configuration by name.
        /// </summary>
        /// <param name="name">The configuration name.</param>
        /// <returns>The configuration or null if not found.</returns>
        public ILogTargetConfig GetConfig(string name)
        {
            return _configRegistry?.GetConfig(name);
        }

        /// <summary>
        /// Gets a typed configuration by name.
        /// </summary>
        /// <typeparam name="TConfig">The configuration type.</typeparam>
        /// <param name="name">The configuration name.</param>
        /// <returns>The typed configuration or default if not found.</returns>
        public TConfig GetConfig<TConfig>(string name) where TConfig : class, ILogTargetConfig
        {
            return _configRegistry?.GetConfig<TConfig>(name);
        }

        #endregion

        #region Disposal

        /// <summary>
        /// Disposes the logging manager and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                // Stop auto-flush coroutine
                if (_autoFlushCoroutine != null)
                {
                    StopCoroutine(_autoFlushCoroutine);
                    _autoFlushCoroutine = null;
                }

                // Final flush
                if (_isInitialized)
                {
                    Flush();
                }

                // Unsubscribe from messages
                foreach (var subscription in _messageSubscriptions)
                {
                    subscription?.Dispose();
                }
                _messageSubscriptions.Clear();

                // Dispose adapters
                _unityLoggerAdapter?.Dispose();
                _burstLoggerAdapter?.Dispose();

                // Dispose logger manager
                _loggerManager?.Dispose();

                // Dispose message bus if we created it
                if (_messageBus is IDisposable disposableMessageBus)
                {
                    disposableMessageBus.Dispose();
                }

                _isDisposed = true;
                _isInitialized = false;

                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] Successfully disposed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Error during disposal: {ex.Message}");
            }
        }

        #endregion

        #region Unity Editor Support

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Validate configuration in editor
            if (_initialMessageQueueCapacity < 64)
                _initialMessageQueueCapacity = 64;

            if (_autoFlushInterval <= 0f)
                _autoFlushInterval = 1.0f;

            // Update runtime configuration if initialized
            if (_isInitialized && Application.isPlaying)
            {
                if (_loggerManager != null)
                {
                    _loggerManager.SetGlobalMinimumLevel(_globalMinimumLevel);
                }

                if (_enableAutoFlush != (_autoFlushCoroutine != null))
                {
                    SetAutoFlush(_enableAutoFlush, _autoFlushInterval);
                }
            }
        }

        /// <summary>
        /// Gets debug information about the logging system.
        /// </summary>
        /// <returns>A formatted debug string.</returns>
        public string GetDebugInfo()
        {
            if (!_isInitialized)
                return "LogManagerComponent: Not Initialized";

            var metrics = GetPerformanceMetrics();
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== LogManagerComponent Debug Info ===");
            
            foreach (var kvp in metrics)
            {
                info.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

            if (_loggerManager != null)
            {
                info.AppendLine("\n=== Logger Manager Info ===");
                info.AppendLine(_loggerManager.GetDebugInfo());
            }

            return info.ToString();
        }
#endif

        #endregion
    }

    /// <summary>
    /// Enumeration of available logging presets.
    /// </summary>
    public enum LoggingPreset
    {
        /// <summary>
        /// Development preset with verbose logging and debugging features.
        /// </summary>
        Development,
        
        /// <summary>
        /// Production preset with optimized performance and minimal logging.
        /// </summary>
        Production,
        
        /// <summary>
        /// High-performance preset with maximum optimization and minimal overhead.
        /// </summary>
        HighPerformance,
        
        /// <summary>
        /// Custom preset using manually configured target configurations.
        /// </summary>
        Custom
    }
}