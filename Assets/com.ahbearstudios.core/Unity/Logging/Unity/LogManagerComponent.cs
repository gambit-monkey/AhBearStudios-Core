using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Extensions;
using AhBearStudios.Core.Coroutine.Interfaces;
using AhBearStudios.Core.Coroutine.Unity;
using AhBearStudios.Core.Coroutine.Configurations;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Unity component for managing the core logging system.
    /// Provides centralized configuration and lifecycle management for logging operations.
    /// Uses composition pattern with proper dependency injection.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class LogManagerComponent : MonoBehaviour, IDisposable
    {
        #region Constants

        private const string COROUTINE_TAG = "LogManager";
        private const string AUTO_FLUSH_COROUTINE_TAG = "LogManager.AutoFlush";
        private const ushort LOG_ENTRY_TYPE_CODE = 1001;

        #endregion

        #region Serialized Fields

        [Header("Configuration")] [SerializeField]
        private LoggingPreset _loggingPreset = LoggingPreset.Development;

        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Debug;
        [SerializeField] private bool _persistAcrossScenes = true;
        [SerializeField] private bool _initializeOnAwake = true;

        [Header("Auto Flush Settings")] [SerializeField]
        private bool _autoFlushEnabled = true;

        [SerializeField] private float _autoFlushInterval = 1.0f;

        [Header("Performance")] [SerializeField]
        private int _targetCount = 100;

        [SerializeField] private int _maxQueueSize = 1000;
        [SerializeField] private int _initialCapacity = 256;

        [Header("Dependencies")] [SerializeField]
        private bool _useExistingCoroutineManager = true;

        [SerializeField] private string _coroutineRunnerName = "LogManager";
        [SerializeField] private bool _enableProfiling = false;

        [Header("Target Configurations")] [SerializeField]
        private UnityConsoleTargetConfig _unityConsoleConfig;

        [SerializeField] private SerilogFileTargetConfig _serilogFileConfig;

        #endregion

        #region Private Fields

        // Core services (composition pattern)
        private ILogManagerService _logManagerService;
        private ICoroutineManager _coroutineManager;
        private ICoroutineRunner _coroutineRunner;
        private ICoroutineHandle _autoFlushHandle;
        private IMessageBus _messageBus;
        private IProfiler _profiler;
        private IDependencyProvider _dependencyProvider;

        // Native collections for performance
        private NativeList<LogMessage> _messageQueue;
        private NativeHashMap<int, byte> _processingFlags;

        // Managed collections for targets
        private readonly List<ILogTarget> _targets = new List<ILogTarget>();

        // State tracking
        private bool _isInitialized;
        private bool _isDisposed;
        private long _totalMessagesProcessed;
        private int _flushCount;

        // Singleton management
        private static LogManagerComponent _global;
        private static readonly object _globalLock = new object();

        // Message subscriptions for cleanup
        private readonly List<IDisposable> _messageSubscriptions = new List<IDisposable>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the log manager is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;

        /// <summary>
        /// Gets the current target count.
        /// </summary>
        public int TargetCount => _targets?.Count ?? 0;

        /// <summary>
        /// Gets the number of queued messages.
        /// </summary>
        public int QueuedMessageCount => _messageQueue.IsCreated ? _messageQueue.Length : 0;

        /// <summary>
        /// Gets or sets the global minimum log level.
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
                    UpdateTargetMinimumLevels();
                    PublishLogLevelChanged(oldLevel, value);
                }
            }
        }

        /// <summary>
        /// Gets the message bus instance.
        /// </summary>
        public IMessageBus MessageBus => _messageBus;

        /// <summary>
        /// Gets the total number of messages processed.
        /// </summary>
        public long TotalMessagesProcessed => _totalMessagesProcessed;

        /// <summary>
        /// Gets the number of flushes performed.
        /// </summary>
        public int FlushCount => _flushCount;

        /// <summary>
        /// Gets the global log manager instance.
        /// </summary>
        public static LogManagerComponent Global
        {
            get
            {
                if (_global == null)
                {
                    lock (_globalLock)
                    {
                        if (_global == null)
                        {
                            _global = GetOrCreateGlobal();
                        }
                    }
                }

                return _global;
            }
        }

        /// <summary>
        /// Gets the log manager service instance.
        /// </summary>
        public ILogManagerService LogManagerService => _logManagerService;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_isDisposed) return;

            if (_persistAcrossScenes)
            {
                lock (_globalLock)
                {
                    if (_global != null && _global != this)
                    {
                        Destroy(gameObject);
                        return;
                    }

                    DontDestroyOnLoad(gameObject);
                    _global = this;
                }
            }

            InitializeNativeCollections();

            if (_initializeOnAwake)
            {
                _ = InitializeAsync();
            }
        }

        private void Update()
        {
            if (!IsInitialized) return;

            ProcessQueuedMessages();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnApplicationQuit()
        {
            if (!_isDisposed)
            {
                Flush();
                Dispose();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsInitialized)
            {
                Flush();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsInitialized)
            {
                Flush();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the log manager asynchronously using composition pattern.
        /// </summary>
        /// <returns>Task representing the initialization operation.</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized || _isDisposed) return;

            try
            {
                ValidateConfiguration();
                InitializeCoroutineRunner();
                InitializeLoggingService();
                ConfigureTargets();
                ConfigureAutoFlush();
                SubscribeToMessages();

                _isInitialized = true;
        
                LogInfo("LogManagerComponent initialized successfully", Tagging.LogTag.System);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize LogManagerComponent: {ex}");
                CreateFallbackConfiguration();
            }
        }

        /// <summary>
        /// Creates fallback configuration when initialization fails.
        /// </summary>
        private void CreateFallbackConfiguration()
        {
            _isInitialized = true;
            AddTarget(CreateDefaultUnityConsoleTarget());
            LogWarning("Using fallback logger configuration", 
                Tagging.LogTag.System);
        }


        /// <summary>
        /// Initializes native collections with proper capacity and allocator.
        /// </summary>
        private void InitializeNativeCollections()
        {
            if (!_messageQueue.IsCreated)
            {
                _messageQueue = new NativeList<LogMessage>(_initialCapacity, Allocator.Persistent);
            }

            if (!_processingFlags.IsCreated)
            {
                _processingFlags = new NativeHashMap<int, byte>(_initialCapacity, Allocator.Persistent);
            }
        }

        /// <summary>
        /// Initializes the coroutine runner using the new coroutine system.
        /// </summary>
        private void InitializeCoroutineRunner()
        {
            try
            {
                if (_useExistingCoroutineManager)
                {
                    _coroutineManager = _dependencyProvider?.ResolveOrDefault<ICoroutineManager>();

                    if (_coroutineManager == null)
                    {
                        _coroutineManager = CoreCoroutineManager.Instance;
                    }

                    if (_coroutineManager?.IsActive == true)
                    {
                        var config = new CoroutineRunnerConfig(_coroutineRunnerName, _persistAcrossScenes,
                            _initialCapacity / 4)
                        {
                            EnableStatistics = _enableProfiling,
                            EnableDebugLogging = _enableProfiling,
                            EnableProfiling = _enableProfiling
                        };

                        _coroutineRunner = _coroutineManager.GetRunner(_coroutineRunnerName) ??
                                           _coroutineManager.CreateRunner(_coroutineRunnerName, _persistAcrossScenes);
                    }
                }

                if (_coroutineRunner == null)
                {
                    LogWarning("CoreCoroutineManager not available, using Unity StartCoroutine fallback",
                        Tagging.LogTag.System);
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize coroutine runner: {ex.Message}",
                    Tagging.LogTag.System);
            }
        }

        /// <summary>
        /// Validates the configuration parameters.
        /// </summary>
        private void ValidateConfiguration()
        {
            _autoFlushInterval = Mathf.Max(0.1f, _autoFlushInterval);
            _targetCount = Mathf.Max(1, _targetCount);
            _maxQueueSize = Mathf.Max(10, _maxQueueSize);
            _initialCapacity = Mathf.Max(16, _initialCapacity);
        }
        

        /// <summary>
        /// Initializes the logging service based on preset configuration.
        /// </summary>
        private void InitializeLoggingService()
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
                default:
                    InitializeDefaultConfiguration();
                    break;
            }
        }

        /// <summary>
        /// Configures log targets based on the current configuration.
        /// </summary>
        private void ConfigureTargets()
        {
            // Configure Unity Console Target
            if (_unityConsoleConfig != null && _unityConsoleConfig.Enabled)
            {
                var unityTarget = _unityConsoleConfig.CreateTarget(_messageBus);
                AddTarget(unityTarget);
                LogInfo($"Added Unity Console target: {_unityConsoleConfig.TargetName}",
                    Tagging.LogTag.System);
            }

            // Configure Serilog File Target
            if (_serilogFileConfig != null && _serilogFileConfig.Enabled)
            {
                var serilogTarget = _serilogFileConfig.CreateTarget(_messageBus);
                AddTarget(serilogTarget);
                LogInfo($"Added Serilog file target: {_serilogFileConfig.TargetName}",
                    Tagging.LogTag.System);
            }

            // Fallback to default Unity Console if no targets configured
            if (_targets.Count == 0)
            {
                AddTarget(CreateDefaultUnityConsoleTarget());
                LogWarning("No targets configured, added default Unity Console target",
                    Tagging.LogTag.System);
            }
        }

        /// <summary>
        /// Initializes development preset configuration.
        /// </summary>
        private void InitializeDevelopmentPreset()
        {
            _globalMinimumLevel = LogLevel.Debug;
        }

        /// <summary>
        /// Initializes production preset configuration.
        /// </summary>
        private void InitializeProductionPreset()
        {
            _globalMinimumLevel = LogLevel.Info;
        }

        /// <summary>
        /// Initializes high performance preset configuration.
        /// </summary>
        private void InitializeHighPerformancePreset()
        {
            _globalMinimumLevel = LogLevel.Warning;
        }

        /// <summary>
        /// Initializes default configuration.
        /// </summary>
        private void InitializeDefaultConfiguration()
        {
            // Default configuration handled by ConfigureTargets
        }

        /// <summary>
        /// Creates a default Unity console log target.
        /// </summary>
        /// <returns>Unity console log target instance.</returns>
        private ILogTarget CreateDefaultUnityConsoleTarget()
        {
            var config = ScriptableObject.CreateInstance<UnityConsoleTargetConfig>();
            config.TargetName = "DefaultUnityConsole";
            config.MinimumLevel = _globalMinimumLevel;
            config.Enabled = true;

            return config.CreateTarget(_messageBus);
        }

        /// <summary>
        /// Configures auto flush functionality.
        /// </summary>
        private void ConfigureAutoFlush()
        {
            if (_autoFlushEnabled)
            {
                SetAutoFlush(true, _autoFlushInterval);
            }
        }

        /// <summary>
        /// Subscribes to message bus events.
        /// </summary>
        private void SubscribeToMessages()
        {
            if (_messageBus == null) return;

            var subscription1 = _messageBus.Subscribe<LogProcessingMessage>(OnLogProcessingMessage);
            var subscription2 = _messageBus.Subscribe<LogFlushMessage>(OnLogFlushMessage);
            var subscription3 = _messageBus.Subscribe<LogLevelChangedMessage>(OnLogLevelChanged);

            _messageSubscriptions.Add(subscription1);
            _messageSubscriptions.Add(subscription2);
            _messageSubscriptions.Add(subscription3);
        }

        #endregion

        #region Message Handling


        /// <summary>
        /// Handles log processing messages.
        /// Monitors processing performance and triggers conditional flushing based on processing state.
        /// </summary>
        /// <param name="message">The log processing message containing batch statistics.</param>
        private void OnLogProcessingMessage(LogProcessingMessage message)
        {
            if (message.Id == Guid.Empty) return;

            // Update performance tracking
            _totalMessagesProcessed += message.ProcessedCount;

            // Conditional flush logic based on processing state
            bool shouldFlush = false;

            // Flush if no messages remaining (batch complete)
            if (message.RemainingCount == 0)
            {
                shouldFlush = true;
            }
            // Flush if processing is taking too long (performance threshold)
            else if (message.ProcessingTimeMs > 100.0f) // 100ms threshold
            {
                shouldFlush = true;
            }
            // Flush if we've processed a significant batch
            else if (message.ProcessedCount >= _targetCount)
            {
                shouldFlush = true;
            }

            if (shouldFlush)
            {
                Flush();
            }

            // Optionally publish processing statistics for monitoring
            if (_enableProfiling && _messageBus != null)
            {
                // Could publish a performance metrics message here
                LogDebug(
                    $"Processed {message.ProcessedCount} messages in {message.ProcessingTimeMs:F2}ms, {message.RemainingCount} remaining",
                    Tagging.LogTag.System);
            }
        }

        /// <summary>
        /// Handles log flush messages.
        /// </summary>
        /// <param name="message">The log flush message.</param>
        private void OnLogFlushMessage(LogFlushMessage message)
        {
            Flush();
        }

        /// <summary>
        /// Handles log level changed messages.
        /// </summary>
        /// <param name="message">The log level changed message.</param>
        private void OnLogLevelChanged(LogLevelChangedMessage message)
        {
            if (message.Id != Guid.Empty)
            {
                _globalMinimumLevel = message.NewLevel;
                UpdateTargetMinimumLevels();
            }
        }

        /// <summary>
        /// Publishes log level changed message.
        /// </summary>
        /// <param name="oldLevel">The old log level.</param>
        /// <param name="newLevel">The new log level.</param>
        private void PublishLogLevelChanged(LogLevel oldLevel, LogLevel newLevel)
        {
            _messageBus?.Publish(new LogLevelChangedMessage(oldLevel, newLevel));
        }

        #endregion

        #region Logging Interface Implementation

        /// <summary>
        /// Logs a message with the specified level and tag.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void Log(LogLevel level, string message, Tagging.LogTag tag = default)
        {
            Log(level, message, tag, default);
        }

        /// <summary>
        /// Logs a message with the specified level, tag, and properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="properties">Additional log properties.</param>
        public void Log(LogLevel level, string message, Tagging.LogTag tag, LogProperties properties)
        {
            if (!IsEnabled(level) || string.IsNullOrEmpty(message)) return;

            var logMessage = new LogMessage(
                new FixedString512Bytes(message),
                level,
                tag,
                properties,
                LOG_ENTRY_TYPE_CODE);

            EnqueueMessage(logMessage);
        }

        /// <summary>
        /// Checks if logging is enabled for the specified level.
        /// </summary>
        /// <param name="level">The log level to check.</param>
        /// <returns>True if logging is enabled for the level.</returns>
        public bool IsEnabled(LogLevel level)
        {
            return IsInitialized && level >= _globalMinimumLevel;
        }

        /// <summary>
        /// Flushes all queued messages.
        /// </summary>
        /// <returns>The number of messages flushed.</returns>
        public int Flush()
        {
            if (!_messageQueue.IsCreated) return 0;

            var flushedCount = 0;
            var startTime = Time.realtimeSinceStartup;

            using var profilerScope = _profiler?.BeginSample("LogManager.Flush");

            try
            {
                // Create a batch list for each target
                var targetBatches = new Dictionary<ILogTarget, NativeList<LogMessage>>();

                foreach (var target in _targets)
                {
                    targetBatches[target] = new NativeList<LogMessage>(_messageQueue.Length, Allocator.Temp);
                }

                // Process each message and add to appropriate target batches
                for (int i = 0; i < _messageQueue.Length; i++)
                {
                    var message = _messageQueue[i];

                    foreach (var target in _targets)
                    {
                        if (target.IsLevelEnabled(message.Level) && target.ShouldProcessMessage(message))
                        {
                            targetBatches[target].Add(message);
                        }
                    }

                    flushedCount++;
                    _totalMessagesProcessed++;
                }

                // Write batches to each target
                foreach (var kvp in targetBatches)
                {
                    var target = kvp.Key;
                    var batch = kvp.Value;

                    if (batch.Length > 0)
                    {
                        try
                        {
                            target.WriteBatch(batch);
                            target.Flush();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error writing batch to target {target.Name}: {ex}");
                        }
                    }

                    batch.Dispose();
                }

                _messageQueue.Clear();
                _processingFlags.Clear();
                _flushCount++;

                // Publish flush message
                var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
                _messageBus?.Publish(new LogFlushMessage(flushedCount, duration));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during flush: {ex}");
            }

            return flushedCount;
        }

        /// <summary>
        /// Adds a log target to the system.
        /// </summary>
        /// <param name="target">The target to add.</param>
        public void AddTarget(ILogTarget target)
        {
            if (target == null || _targets.Contains(target)) return;

            _targets.Add(target);
            UpdateTargetMinimumLevels();

            LogInfo($"Added log target: {target.Name}",
                Tagging.LogTag.System);
        }

        /// <summary>
        /// Removes a log target from the system.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns>True if the target was removed.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            var removed = _targets.Remove(target);

            if (removed)
            {
                LogInfo($"Removed log target: {target?.Name}",
                    Tagging.LogTag.System);
            }

            return removed;
        }

        /// <summary>
        /// Creates a job logger with the specified parameters.
        /// </summary>
        /// <param name="minimumLevel">The minimum log level.</param>
        /// <param name="defaultTag">The default tag.</param>
        /// <returns>A new job logger instance.</returns>
        public JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            var effectiveLevel = minimumLevel ?? _globalMinimumLevel;

            // Try to get JobLogger from the service first
            if (_logManagerService != null)
            {
                return _logManagerService.CreateJobLogger(effectiveLevel, defaultTag);
            }

            // Check if we have a JobLoggerManager available via dependency injection
            var jobLoggerManager = _dependencyProvider?.ResolveOrDefault<JobLoggerManager>();
            if (jobLoggerManager != null)
            {
                return JobLoggerFactory.CreateFromManager(jobLoggerManager, effectiveLevel, defaultTag);
            }

            // Use factory convenience methods based on current logging preset
            switch (_loggingPreset)
            {
                case LoggingPreset.Development:
                    var (devManager, devLogger) = JobLoggerFactory.CreateForDevelopment(
                        "Logs/job_debug.log", defaultTag, _messageBus);
                    return devLogger;

                case LoggingPreset.Production:
                    var (prodManager, prodLogger) = JobLoggerFactory.CreateForProduction(
                        "Logs/job_app.log", defaultTag, _messageBus);
                    return prodLogger;

                case LoggingPreset.HighPerformance:
                    var (hpManager, hpLogger) = JobLoggerFactory.CreateHighPerformance(
                        "Logs/job_hp.log", defaultTag, _messageBus);
                    return hpLogger;

                default:
                    // Default to console-only for unknown presets
                    var (defaultManager, defaultLogger) = JobLoggerFactory.CreateConsoleOnly(
                        effectiveLevel, true, defaultTag, _messageBus);
                    return defaultLogger;
            }
        }

        /// <summary>
        /// Creates a development job logger with file and console output.
        /// </summary>
        /// <param name="defaultTag">The default tag.</param>
        /// <returns>A development-configured job logger.</returns>
        public JobLogger CreateDevelopmentJobLogger(Tagging.LogTag defaultTag = default)
        {
            var (manager, logger) = JobLoggerFactory.CreateForDevelopment(
                "Logs/job_debug.log", defaultTag, _messageBus);
            return logger;
        }

        /// <summary>
        /// Creates a production job logger optimized for performance.
        /// </summary>
        /// <param name="defaultTag">The default tag.</param>
        /// <returns>A production-configured job logger.</returns>
        public JobLogger CreateProductionJobLogger(Tagging.LogTag defaultTag = default)
        {
            var (manager, logger) = JobLoggerFactory.CreateForProduction(
                "Logs/job_app.log", defaultTag, _messageBus);
            return logger;
        }

        /// <summary>
        /// Creates a console-only job logger for lightweight scenarios.
        /// </summary>
        /// <param name="defaultTag">The default tag.</param>
        /// <returns>A console-only job logger.</returns>
        public JobLogger CreateConsoleJobLogger(Tagging.LogTag defaultTag = default)
        {
            var (manager, logger) = JobLoggerFactory.CreateConsoleOnly(
                _globalMinimumLevel, true, defaultTag, _messageBus);
            return logger;
        }

        #endregion

        #region Auto Flush

        /// <summary>
        /// Sets the auto flush configuration.
        /// </summary>
        /// <param name="enabled">Whether auto flush is enabled.</param>
        /// <param name="interval">The flush interval in seconds.</param>
        public void SetAutoFlush(bool enabled, float interval = 1.0f)
        {
            _autoFlushEnabled = enabled;
            _autoFlushInterval = Mathf.Max(0.1f, interval);

            StopAutoFlush();

            if (enabled && IsInitialized)
            {
                StartAutoFlush();
            }
        }

        /// <summary>
        /// Starts the auto flush coroutine.
        /// </summary>
        private void StartAutoFlush()
        {
            if (_coroutineRunner != null)
            {
                _autoFlushHandle = _coroutineRunner.StartCoroutine(
                    AutoFlushCoroutine(_autoFlushInterval),
                    AUTO_FLUSH_COROUTINE_TAG);
            }
            else
            {
                StartCoroutine(AutoFlushCoroutine(_autoFlushInterval));
            }
        }

        /// <summary>
        /// Stops the auto flush coroutine.
        /// </summary>
        private void StopAutoFlush()
        {
            if (_autoFlushHandle != null)
            {
                _autoFlushHandle.Stop();
                _autoFlushHandle = null;
            }
        }

        /// <summary>
        /// Auto flush coroutine implementation.
        /// </summary>
        /// <param name="interval">The flush interval.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator AutoFlushCoroutine(float interval)
        {
            var waitForSeconds = new WaitForSeconds(interval);

            while (_autoFlushEnabled && !_isDisposed)
            {
                yield return waitForSeconds;

                if (QueuedMessageCount > 0)
                {
                    Flush();
                }
            }
        }

        #endregion

        #region Message Processing

        /// <summary>
        /// Enqueues a message for processing.
        /// </summary>
        /// <param name="message">The message to enqueue.</param>
        private void EnqueueMessage(LogMessage message)
        {
            if (!_messageQueue.IsCreated) return;

            if (_messageQueue.Length >= _maxQueueSize)
            {
                // Force flush if queue is full
                Flush();
            }

            _messageQueue.Add(message);

            // Publish log entry message
            _messageBus?.Publish(new LogEntryMessage(message));
        }

        /// <summary>
        /// Processes queued messages in batches.
        /// </summary>
        private void ProcessQueuedMessages()
        {
            if (!_messageQueue.IsCreated || _messageQueue.Length == 0) return;

            using var profilerScope = _profiler?.BeginSample("LogManager.ProcessMessages");

            var processedCount = 0;
            var maxProcessPerFrame = Mathf.Min(_targetCount, _messageQueue.Length);

            while (processedCount < maxProcessPerFrame && _messageQueue.Length > 0)
            {
                var message = _messageQueue[0];
                _messageQueue.RemoveAtSwapBack(0);

                ProcessMessage(message);
                processedCount++;
                _totalMessagesProcessed++;
            }
        }

        /// <summary>
        /// Processes a single log message.
        /// </summary>
        /// <param name="message">The message to process.</param>
        private void ProcessMessage(LogMessage message)
        {
            using var profilerScope = _profiler?.BeginSample("LogManager.ProcessMessage");

            var targetCount = 0;
            string primaryTargetName = "Unknown";

            foreach (var target in _targets)
            {
                try
                {
                    if (target.IsLevelEnabled(message.Level) && target.ShouldProcessMessage(message))
                    {
                        target.Write(message);

                        if (targetCount == 0)
                        {
                            primaryTargetName = target.Name;
                        }

                        targetCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error writing to log target {target?.Name}: {ex}");
                }
            }

            // Publish log written message if message was written to at least one target
            if (targetCount > 0)
            {
                _messageBus?.Publish(new LogEntryWrittenMessage(message, targetCount, primaryTargetName));
            }
        }

        /// <summary>
        /// Updates minimum levels for all targets.
        /// </summary>
        private void UpdateTargetMinimumLevels()
        {
            foreach (var target in _targets)
            {
                // Update minimum level if target supports it
                if (target.MinimumLevel != _globalMinimumLevel)
                {
                    try
                    {
                        // This would require extending the ILogTarget interface
                        // For now, we'll just log that we'd like to update it
                        LogDebug($"Would update target {target.Name} minimum level to {_globalMinimumLevel}",
                            Tagging.LogTag.System);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error updating target minimum level: {ex.Message}",
                            Tagging.LogTag.System);
                    }
                }
            }
        }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Gets performance metrics for the logging system.
        /// </summary>
        /// <returns>Dictionary of performance metrics.</returns>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            return new Dictionary<string, object>
            {
                ["TotalMessagesProcessed"] = _totalMessagesProcessed,
                ["QueuedMessages"] = QueuedMessageCount,
                ["FlushCount"] = _flushCount,
                ["TargetCount"] = TargetCount,
                ["IsAutoFlushEnabled"] = _autoFlushEnabled,
                ["AutoFlushInterval"] = _autoFlushInterval,
                ["IsCoroutineSystemActive"] = _coroutineRunner != null,
                ["MessageQueueCapacity"] = _messageQueue.IsCreated ? _messageQueue.Capacity : 0,
                ["GlobalMinimumLevel"] = _globalMinimumLevel.ToString()
            };
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogDebug(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Debug, message, tag);
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogInfo(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Info, message, tag);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogWarning(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Warning, message, tag);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogError(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Error, message, tag);
        }

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogCritical(string message, Tagging.LogTag tag = default)
        {
            Log(LogLevel.Critical, message, tag);
        }

        /// <summary>
        /// Logs an exception with optional message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Optional additional message.</param>
        /// <param name="tag">The log tag.</param>
        public void LogException(Exception exception, string message = null, Tagging.LogTag tag = default)
        {
            if (exception == null) return;

            var logMessage = string.IsNullOrEmpty(message)
                ? exception.ToString()
                : $"{message}\n{exception}";

            Log(LogLevel.Error, logMessage, tag);
        }

        #endregion

        #region Disposal

        /// <summary>
        /// Disposes of the LogManagerComponent and all resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            try
            {
                // Stop auto flush
                StopAutoFlush();

                // Stop all coroutines
                _coroutineRunner?.StopCoroutinesByTag(COROUTINE_TAG);

                // Flush remaining messages
                Flush();

                // Dispose targets
                foreach (var target in _targets)
                {
                    if (target is IDisposable disposableTarget)
                    {
                        disposableTarget.Dispose();
                    }
                }

                _targets.Clear();

                // Dispose native collections
                if (_messageQueue.IsCreated)
                {
                    _messageQueue.Dispose();
                }

                if (_processingFlags.IsCreated)
                {
                    _processingFlags.Dispose();
                }

                // Unsubscribe from messages
                foreach (var subscription in _messageSubscriptions)
                {
                    subscription?.Dispose();
                }

                _messageSubscriptions.Clear();

                // Dispose services
                if (_logManagerService is IDisposable disposableService)
                {
                    disposableService.Dispose();
                }

                _isInitialized = false;

                lock (_globalLock)
                {
                    if (_global == this)
                    {
                        _global = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during LogManagerComponent disposal: {ex}");
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets or creates the global LogManagerComponent instance.
        /// </summary>
        /// <returns>The global instance.</returns>
        private static LogManagerComponent GetOrCreateGlobal()
        {
            var existing = FindObjectOfType<LogManagerComponent>();

            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("LogManager");
            var component = go.AddComponent<LogManagerComponent>();
            DontDestroyOnLoad(go);

            return component;
        }

        #endregion

        #region Editor Support

        /// <summary>
        /// Validates configuration in the editor.
        /// </summary>
        private void OnValidate()
        {
            ValidateConfiguration();
        }

        /// <summary>
        /// Gets debug information about the component state.
        /// </summary>
        /// <returns>Debug information string.</returns>
        public string GetDebugInfo()
        {
            var metrics = GetPerformanceMetrics();
            var info = "LogManagerComponent Debug Info:\n";
            info += $"  Initialized: {IsInitialized}\n";
            info += $"  Using Coroutine System: {_coroutineRunner != null}\n";
            info += $"  Coroutine Runner: {_coroutineRunnerName}\n";
            info += $"  Native Collections Created: {_messageQueue.IsCreated}\n";

            foreach (var metric in metrics)
            {
                info += $"  {metric.Key}: {metric.Value}\n";
            }

            return info;
        }

        #endregion
    }
}