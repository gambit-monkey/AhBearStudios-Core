using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AhBearStudios.Core.DependencyInjection.Adapters;
using UnityEngine;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.LogTargets;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.MessageBuses;
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
    public sealed class LogManagerComponent : MonoBehaviour, IBurstLogger, IDisposable
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
        [SerializeField] private int _maxMessagesPerFlush = 200;
        [SerializeField] private bool _enableBurstCompilation = true;
        
        [Header("Coroutine Runner")]
        [SerializeField] private ILogCoroutineRunner _coroutineRunner;

        [Header("Development Settings")]
        [SerializeField] private bool _enableDebugLogging = false;

        #endregion

        #region Private Fields

        // Core logging system
        private NativeQueue<LogMessage> _logQueue;
        private NativeQueue<LogMessage>.ParallelWriter _logQueueWriter;
        private LogBatchProcessor _batchProcessor;
        private ILogFormatter _logFormatter;

        // Dependency injection
        private IDependencyProvider _dependencyProvider;
        private IMessageBus _messageBus;

        // State management
        private bool _isInitialized;
        private bool _isDisposed;
        private int _totalMessagesProcessed;
        private int _flushCount;

        // Auto-flush coroutine
        private Coroutine _autoFlushCoroutine;

        // Message bus subscriptions for cleanup
        private readonly List<IDisposable> _messageSubscriptions = new List<IDisposable>();

        // Log targets
        private readonly List<ILogTarget> _logTargets = new List<ILogTarget>();

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
        public int TargetCount => _logTargets.Count;

        /// <summary>
        /// Gets the number of messages currently queued for processing.
        /// </summary>
        public int QueuedMessageCount => _logQueue.IsCreated ? _logQueue.Count : 0;

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
                
                UpdateTargetMinimumLevels();
                PublishLogLevelChanged(oldLevel, value);
            }
        }

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

            // Auto-initialize if configured
            if (_autoInitialize)
            {
                _ = InitializeAsync();
            }
        }

        private void Update()
        {
            if (!_isInitialized || _isDisposed) return;

            // Update auto-flush if enabled
            if (_enableAutoFlush && _batchProcessor != null)
            {
                var processedCount = _batchProcessor.Update(Time.deltaTime);
                if (processedCount > 0)
                {
                    _totalMessagesProcessed += processedCount;
                }
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

                // Resolve dependencies
                ResolveDependencies();

                // Create default dependencies if needed
                CreateDefaultDependencies();

                // Initialize the logging system
                InitializeLoggingSystem();

                InitializeCoroutineRunner();

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
        
        private void InitializeCoroutineRunner()
        {
            _coroutineRunner = LogCoroutineRunner.Instance;
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

            if (_maxMessagesPerFlush <= 0)
            {
                _maxMessagesPerFlush = 200;
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
                    // Use the adapter to convert UnityDependencyProvider to IDependencyProvider
                    _dependencyProvider = new UnityDependencyProviderAdapter(unityProvider);
            
                    // Resolve optional dependencies with safe fallbacks
                    _messageBus = _dependencyProvider.ResolveOrDefault<IMessageBus>();
                    _logFormatter = _dependencyProvider.ResolveOrDefault<ILogFormatter>();

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
                _messageBus = new NullMessageBus();
                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] Created fallback NullMessageBus");
            }

            // Create default formatter if not resolved
            if (_logFormatter == null)
            {
                _logFormatter = new DefaultLogFormatter();
                if (_enableDebugLogging)
                    Debug.Log("[LogManagerComponent] Created default log formatter");
            }
        }

        /// <summary>
        /// Initializes the core logging system based on the configured preset.
        /// </summary>
        private void InitializeLoggingSystem()
        {
            try
            {
                // Initialize native queue
                _logQueue = new NativeQueue<LogMessage>(Allocator.Persistent);
                _logQueueWriter = _logQueue.AsParallelWriter();

                // Initialize based on preset
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
                    default:
                        InitializeDefaultConfiguration();
                        break;
                }

                // Create batch processor
                _batchProcessor = new LogBatchProcessor(_logTargets, _logQueue, _logFormatter, _messageBus, _maxMessagesPerFlush);

                if (_enableDebugLogging)
                    Debug.Log($"[LogManagerComponent] Initialized logging system with {_loggingPreset} preset");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Failed to initialize logging system: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes the development preset configuration.
        /// </summary>
        private void InitializeDevelopmentPreset()
        {
            // Add Unity console target for development
            var unityConsoleTarget = CreateUnityConsoleTarget();
            _logTargets.Add(unityConsoleTarget);
        }

        /// <summary>
        /// Initializes the production preset configuration.
        /// </summary>
        private void InitializeProductionPreset()
        {
            // Minimal logging for production
            _globalMinimumLevel = LogLevel.Warning;
            
            // Add only essential targets
            var unityConsoleTarget = CreateUnityConsoleTarget();
            unityConsoleTarget.MinimumLevel = LogLevel.Warning;
            _logTargets.Add(unityConsoleTarget);
        }

        /// <summary>
        /// Initializes the high-performance preset configuration.
        /// </summary>
        private void InitializeHighPerformancePreset()
        {
            // Minimal logging for maximum performance
            _globalMinimumLevel = LogLevel.Error;
            
            // Only critical error logging
            var unityConsoleTarget = CreateUnityConsoleTarget();
            unityConsoleTarget.MinimumLevel = LogLevel.Error;
            _logTargets.Add(unityConsoleTarget);
        }

        /// <summary>
        /// Initializes the default configuration.
        /// </summary>
        private void InitializeDefaultConfiguration()
        {
            // Add default Unity console target
            var unityConsoleTarget = CreateUnityConsoleTarget();
            _logTargets.Add(unityConsoleTarget);
        }

        /// <summary>
        /// Creates a Unity console log target using the existing UnityConsoleTarget class.
        /// </summary>
        /// <returns>A configured Unity console log target.</returns>
        private ILogTarget CreateUnityConsoleTarget()
        {
            // Create configuration for Unity console target
            var config = ScriptableObject.CreateInstance<UnityConsoleTargetConfig>();
            config.TargetName = "Unity Console";
            config.MinimumLevel = _globalMinimumLevel;
            config.Enabled = true;
            config.IncludeTimestamps = true;
            config.TimestampFormat = "HH:mm:ss.fff";
            config.ProcessUntaggedMessages = true;
            config.LimitMessageLength = false;
            config.RegisterUnityLogHandler = false; // Don't interfere with Unity's default logging

            // Use the existing UnityConsoleTarget class
            return new UnityConsoleTarget(config, _logFormatter, _messageBus);
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
        /// Creates a fallback logger when initialization fails.
        /// </summary>
        private void CreateFallbackLogger()
        {
            try
            {
                if (!_logQueue.IsCreated)
                {
                    _logQueue = new NativeQueue<LogMessage>(Allocator.Persistent);
                    _logQueueWriter = _logQueue.AsParallelWriter();
                }

                _logTargets.Clear();
                var fallbackTarget = CreateUnityConsoleTarget();
                fallbackTarget.MinimumLevel = LogLevel.Error;
                _logTargets.Add(fallbackTarget);

                if (_logFormatter == null)
                {
                    _logFormatter = new DefaultLogFormatter();
                }

                if (_messageBus == null)
                {
                    _messageBus = new NullMessageBus();
                }

                _batchProcessor = new LogBatchProcessor(_logTargets, _logQueue, _logFormatter, _messageBus, 50);
                
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
            _totalMessagesProcessed = message.ProcessedCount;
        }

        /// <summary>
        /// Handles log flush messages from the message bus.
        /// </summary>
        /// <param name="message">The log flush message.</param>
        private void OnLogFlushMessage(LogFlushMessage message)
        {
            _flushCount++;
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
                _messageBus?.PublishMessage(new LogLevelChangedMessage(oldLevel, newLevel));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogManagerComponent] Failed to publish log level changed message: {ex.Message}");
            }
        }

        #endregion

        #region IBurstLogger Implementation

        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        public void Log(LogLevel level, string message, string tag)
        {
            if (!_isInitialized || _isDisposed || level < _globalMinimumLevel || string.IsNullOrEmpty(message))
                return;

            try
            {
                var logTag = Tagging.GetLogTag(tag ?? "Default");
                var fixedMessage = new FixedString512Bytes(message);
        
                // Create log message with proper constructor parameters
                var logMessage = new LogMessage
                {
                    Level = level,
                    Message = fixedMessage,
                    Tag = logTag,
                    TimestampTicks = DateTime.UtcNow.Ticks
                };
        
                _logQueueWriter.Enqueue(logMessage);
            }
            catch (Exception)
            {
                // Silently handle errors when logging to prevent recursive issues
            }
        }

        /// <summary>
        /// Logs a structured message with properties.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The message content.</param>
        /// <param name="tag">The tag identifying the source or category of the log.</param>
        /// <param name="properties">Key-value properties providing structured context.</param>
        public void Log(LogLevel level, string message, string tag, LogProperties properties)
        {
            if (!_isInitialized || _isDisposed || level < _globalMinimumLevel || string.IsNullOrEmpty(message))
                return;

            try
            {
                var logTag = string.IsNullOrEmpty(tag) ? Tagging.LogTag.Default : Tagging.GetLogTag(tag);
                var fixedMessage = new FixedString512Bytes(message);
                var logMessage = new LogMessage(fixedMessage, level, logTag, properties);
                
                _logQueueWriter.Enqueue(logMessage);
            }
            catch (Exception)
            {
                // Silently handle errors when logging to prevent recursive issues
            }
        }

        /// <summary>
        /// Checks if logging is enabled for the specified log level.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if messages at this level would be logged; otherwise, false.</returns>
        public bool IsEnabled(LogLevel level)
        {
            return _isInitialized && !_isDisposed && level >= _globalMinimumLevel;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Flushes all pending log messages to their targets.
        /// </summary>
        /// <returns>The number of messages flushed.</returns>
        public int Flush()
        {
            if (!_isInitialized || _isDisposed || _batchProcessor == null) return 0;

            try
            {
                var flushedCount = _batchProcessor.Flush();
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
        public void AddTarget(ILogTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");

            _logTargets.Add(target);
            target.MinimumLevel = _globalMinimumLevel;
            _batchProcessor?.AddTarget(target);
        }

        /// <summary>
        /// Removes a log target from the logging system.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns>True if the target was removed successfully.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            if (!_isInitialized || target == null) return false;
            
            var removed = _logTargets.Remove(target);
            if (removed)
            {
                _batchProcessor?.RemoveTarget(target);
            }
            return removed;
        }

        /// <summary>
        /// Creates a new job logger for use in Unity job contexts.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level for this logger.</param>
        /// <param name="defaultTag">The default tag for this logger.</param>
        /// <returns>A new job logger instance.</returns>
        public JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (!_isInitialized) throw new InvalidOperationException("LogManagerComponent is not initialized");
    
            var actualMinLevel = minimumLevel ?? _globalMinimumLevel;
            var actualTag = defaultTag.Equals(default(Tagging.LogTag)) ? Tagging.LogTag.Job : defaultTag;
    
            return new JobLogger(_logQueueWriter, actualMinLevel, actualTag);
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
                _coroutineRunner.StopCoroutine(_autoFlushCoroutine);
                _autoFlushCoroutine = null;
            }

            if (enabled && interval > 0f && _batchProcessor != null)
            {
                _autoFlushCoroutine = _coroutineRunner.StartCoroutine(AutoFlushCoroutine(interval));
            }

            _enableAutoFlush = enabled;
            _autoFlushInterval = interval;
        }
        
        private System.Collections.IEnumerator AutoFlushCoroutine(float interval)
        {
            while (_enableAutoFlush && _batchProcessor != null)
            {
                yield return new WaitForSeconds(interval);

                if (_enableAutoFlush && _batchProcessor != null)
                {
                    _batchProcessor.Flush();
                }
            }
        }

        /// <summary>
        /// Updates the minimum level for all targets.
        /// </summary>
        private void UpdateTargetMinimumLevels()
        {
            foreach (var target in _logTargets)
            {
                try
                {
                    target.MinimumLevel = _globalMinimumLevel;
                }
                catch (Exception)
                {
                    // Silently handle errors when updating target levels
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

            return metrics;
        }

        #endregion

        #region Logging Convenience Methods

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogDebug(string message, string tag = null)
        {
            Log(LogLevel.Debug, message, tag ?? "Debug");
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogInfo(string message, string tag = null)
        {
            Log(LogLevel.Info, message, tag ?? "Info");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogWarning(string message, string tag = null)
        {
            Log(LogLevel.Warning, message, tag ?? "Warning");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogError(string message, string tag = null)
        {
            Log(LogLevel.Error, message, tag ?? "Error");
        }

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogCritical(string message, string tag = null)
        {
            Log(LogLevel.Critical, message, tag ?? "Critical");
        }

        /// <summary>
        /// Logs an exception with optional message and tag.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Optional additional message.</param>
        /// <param name="tag">The log tag.</param>
        public void LogException(Exception exception, string message = null, string tag = null)
        {
            if (exception == null) return;

            var logMessage = string.IsNullOrEmpty(message) 
                ? $"Exception: {exception.Message}\nStackTrace: {exception.StackTrace}"
                : $"{message}\nException: {exception.Message}\nStackTrace: {exception.StackTrace}";

            Log(LogLevel.Error, logMessage, tag ?? "Exception");
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

                // Dispose batch processor
                _batchProcessor?.Dispose();

                // Dispose native containers
                if (_logQueue.IsCreated)
                {
                    _logQueue.Dispose();
                }

                // Clear targets (don't dispose them as they may be shared)
                _logTargets.Clear();

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

        #region Static Factory Methods

        /// <summary>
        /// Gets or creates the global log manager instance.
        /// </summary>
        /// <returns>The global log manager instance.</returns>
        public static LogManagerComponent GetOrCreateGlobal()
        {
            if (_globalInstance != null && !_globalInstance._isDisposed)
                return _globalInstance;

            // Search for existing instance in scene
            var existing = FindObjectOfType<LogManagerComponent>();
            if (existing != null && !existing._isDisposed)
            {
                _globalInstance = existing;
                return existing;
            }

            // Create new instance
            var go = new GameObject("[LogManagerComponent]");
            var logManager = go.AddComponent<LogManagerComponent>();
            logManager._persistBetweenScenes = true;
            logManager._autoInitialize = true;

            DontDestroyOnLoad(go);
            return logManager;
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

            if (_maxMessagesPerFlush <= 0)
                _maxMessagesPerFlush = 200;

            // Update runtime configuration if initialized
            if (_isInitialized && Application.isPlaying)
            {
                GlobalMinimumLevel = _globalMinimumLevel;

                if (_enableAutoFlush != (_batchProcessor?.IsAutoFlushEnabled ?? false))
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

            info.AppendLine("\n=== Log Targets ===");
            for (int i = 0; i < _logTargets.Count; i++)
            {
                var target = _logTargets[i];
                info.AppendLine($"Target {i}: {target.Name} (Min Level: {target.MinimumLevel}, Enabled: {target.IsEnabled})");
            }

            if (_batchProcessor != null)
            {
                info.AppendLine("\n=== Batch Processor Info ===");
                info.AppendLine($"Auto Flush Enabled: {_batchProcessor.IsAutoFlushEnabled}");
                info.AppendLine($"Max Messages Per Flush: {_maxMessagesPerFlush}");
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
        HighPerformance
    }
}