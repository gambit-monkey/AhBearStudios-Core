using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Builders;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Unity;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Components
{
    /// <summary>
    /// Unity component that manages the logging system lifecycle using JobLoggerManager and factory patterns.
    /// Provides centralized logging management with dependency injection support and robust configuration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LogManagerComponent : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Initialization")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private LogLevel _globalMinimumLevel = LogLevel.Debug;
        
        [Header("Performance Settings")]
        [SerializeField] private int _initialCapacity = 128;
        [SerializeField] private int _maxMessagesPerFlush = 200;
        [SerializeField] private int _maxMessagesPerFrame = 100;
        
        [Header("Auto-Flush Configuration")]
        [SerializeField] private bool _enableAutoFlush = true;
        [SerializeField] private float _autoFlushInterval = 1.0f;
        
        [Header("Development Presets")]
        [SerializeField] private bool _useDevelopmentPreset = false;
        [SerializeField] private bool _useProductionPreset = false;
        [SerializeField] private bool _useHighPerformancePreset = false;
        
        [Header("Default Target Creation")]
        [SerializeField] private bool _createDefaultUnityConsole = true;
        [SerializeField] private bool _createDefaultFileLogger = false;
        [SerializeField] private string _defaultLogFilePath = "Logs/app.log";
        
        [Header("Target Configurations")]
        [SerializeField] private ScriptableObject[] _targetConfigs = new ScriptableObject[0];
        
        #endregion
        
        #region Private Fields
        
        private JobLoggerManager _loggerManager;
        private UnityLoggerAdapter _unityLoggerAdapter;
        private JobLoggerFactory _jobLoggerFactory;
        private IMessageBus _messageBus;
        private ILogFormatter _logFormatter;
        private IDependencyProvider _dependencyProvider;
        
        private readonly List<ILogTargetConfig> _validatedConfigs = new List<ILogTargetConfig>();
        
        private bool _isInitialized;
        private bool _isDisposing;
        private float _accumulatedDeltaTime;
        
        // Cached references for performance
        private JobLogger _cachedJobLogger;
        private bool _hasDefaultJobLogger;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets whether the logging system has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized && _loggerManager != null;
        
        /// <summary>
        /// Gets the number of active log targets.
        /// </summary>
        public int TargetCount => _loggerManager?.TargetCount ?? 0;
        
        /// <summary>
        /// Gets the number of queued messages waiting to be processed.
        /// </summary>
        public int QueuedMessageCount => _loggerManager?.QueuedMessageCount ?? 0;
        
        /// <summary>
        /// Gets or sets the global minimum log level.
        /// </summary>
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set
            {
                _globalMinimumLevel = value;
                if (_isInitialized && _loggerManager != null)
                {
                    _loggerManager.GlobalMinimumLevel = value;
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
        /// Gets the job logger factory instance.
        /// </summary>
        public JobLoggerFactory JobLoggerFactory => _jobLoggerFactory;
        
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
            if (!_isInitialized || _isDisposing || _loggerManager == null)
                return;
                
            _accumulatedDeltaTime += Time.deltaTime;
            
            // Process pending log messages with frame limiting
            _loggerManager.Update(_accumulatedDeltaTime);
            
            // Reset accumulated time after processing
            _accumulatedDeltaTime = 0f;
        }
        
        private void OnDestroy()
        {
            CleanupLoggingSystem();
        }
        
        private void OnApplicationQuit()
        {
            if (_isInitialized)
            {
                Flush();
                CleanupLoggingSystem();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes the logging system asynchronously.
        /// </summary>
        public void InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[LogManagerComponent] Already initialized", this);
                return;
            }
            
            try
            {
                ResolveDependencies();
                ValidateAndCacheConfigs();
                InitializeLoggingSystem();
                
                _isInitialized = true;
                
                Debug.Log($"[LogManagerComponent] Logging system initialized with {TargetCount} targets", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Failed to initialize: {ex.Message}", this);
                CreateFallbackLogger();
            }
        }
        
        /// <summary>
        /// Resolves dependencies through dependency injection or creates defaults.
        /// </summary>
        private void ResolveDependencies()
        {
            // Try to resolve from dependency container first
            TryResolveDependenciesFromContainer();
            
            // Create defaults for any missing dependencies
            CreateDefaultDependencies();
        }
        
        /// <summary>
        /// Attempts to resolve dependencies from a dependency injection container.
        /// </summary>
        private void TryResolveDependenciesFromContainer()
        {
            _dependencyProvider = FindObjectOfType<MonoBehaviour>() as IDependencyProvider;
            
            if (_dependencyProvider != null)
            {
                _messageBus ??= _dependencyProvider.TryResolve<IMessageBus>();
                _logFormatter ??= _dependencyProvider.TryResolve<ILogFormatter>();
            }
        }
        
        /// <summary>
        /// Creates default implementations for missing dependencies.
        /// </summary>
        private void CreateDefaultDependencies()
        {
            _messageBus ??= CreateDefaultMessageBus();
            _logFormatter ??= CreateDefaultLogFormatter();
            _jobLoggerFactory = new JobLoggerFactory();
        }
        
        /// <summary>
        /// Validates target configurations and caches valid ones.
        /// </summary>
        private void ValidateAndCacheConfigs()
        {
            _validatedConfigs.Clear();
            
            if (_targetConfigs != null)
            {
                foreach (var configObject in _targetConfigs)
                {
                    if (ValidateTargetConfig(configObject, out var validConfig))
                    {
                        _validatedConfigs.Add(validConfig);
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
                return false;
                
            validConfig = configObject as ILogTargetConfig;
            if (validConfig == null)
            {
                Debug.LogWarning($"[LogManagerComponent] Config object {configObject.name} does not implement ILogTargetConfig", this);
                return false;
            }
            
            if (string.IsNullOrEmpty(validConfig.TargetName))
            {
                Debug.LogWarning($"[LogManagerComponent] Config {configObject.name} has empty target name", this);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Initializes the core logging system using preset patterns or custom configuration.
        /// </summary>
        private void InitializeLoggingSystem()
        {
            // Use preset creation patterns from JobLoggerFactory if specified
            if (_useDevelopmentPreset)
            {
                var (manager, logger) = _jobLoggerFactory.CreateForDevelopment(_defaultLogFilePath, Tagging.LogTag.Unity, _messageBus);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else if (_useProductionPreset)
            {
                var (manager, logger) = _jobLoggerFactory.CreateForProduction(_defaultLogFilePath, Tagging.LogTag.Unity, _messageBus);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else if (_useHighPerformancePreset)
            {
                var (manager, logger) = _jobLoggerFactory.CreateHighPerformance(_defaultLogFilePath, Tagging.LogTag.Unity, _messageBus);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else
            {
                CreateCustomLoggerManager();
            }
            
            ConfigureAutoFlush();
            InitializeUnityLoggerIntegration();
        }
        
        /// <summary>
        /// Creates a custom logger manager using configurations or defaults.
        /// </summary>
        private void CreateCustomLoggerManager()
        {
            if (_validatedConfigs.Count > 0)
            {
                // Use builder pattern with configurations
                var (manager, logger) = _jobLoggerFactory.CreateComplete(
                    builderCollection => ConfigureTargetsFromConfigs(builderCollection),
                    _logFormatter,
                    _globalMinimumLevel,
                    Tagging.LogTag.Unity,
                    _messageBus
                );
                
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else
            {
                // Create with default targets
                CreateDefaultLoggerManager();
            }
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
                    if (config is SerilogFileTargetConfig serilogConfig)
                    {
                        var builder = new SerilogFileConfigBuilder()
                            .WithTargetName(serilogConfig.TargetName)
                            .WithEnabled(serilogConfig.Enabled)
                            .WithMinimumLevel(serilogConfig.MinimumLevel)
                            .WithLogFilePath(serilogConfig.LogFilePath)
                            .WithJsonFormat(serilogConfig.UseJsonFormat)
                            .WithTagFilters(serilogConfig.IncludedTags, serilogConfig.ExcludedTags, serilogConfig.ProcessUntaggedMessages)
                            .WithTimestamps(serilogConfig.IncludeTimestamps, serilogConfig.TimestampFormat)
                            .WithPerformance(serilogConfig.AutoFlush, serilogConfig.BufferSize, serilogConfig.FlushIntervalSeconds)
                            .WithUnityIntegration(serilogConfig.CaptureUnityLogs)
                            .WithMessageFormatting(serilogConfig.IncludeStackTraces, serilogConfig.IncludeSourceContext, serilogConfig.IncludeThreadId)
                            .WithStructuredLogging(serilogConfig.EnableStructuredLogging)
                            .WithMessageLengthLimit(serilogConfig.LimitMessageLength, serilogConfig.MaxMessageLength);
                            
                        builderCollection.AddSerilogFile(builder);
                    }
                    else if (config is UnityConsoleTargetConfig consoleConfig)
                    {
                        var builder = new UnityConsoleConfigBuilder()
                            .WithTargetName(consoleConfig.TargetName)
                            .WithEnabled(consoleConfig.Enabled)
                            .WithMinimumLevel(consoleConfig.MinimumLevel)
                            .WithColorizedOutput(consoleConfig.UseColorizedOutput)
                            .WithUnityLogHandlerRegistration(consoleConfig.RegisterUnityLogHandler, consoleConfig.DuplicateToOriginalHandler)
                            .WithTagFilters(consoleConfig.IncludedTags, consoleConfig.ExcludedTags, consoleConfig.ProcessUntaggedMessages)
                            .WithTimestamps(consoleConfig.IncludeTimestamps, consoleConfig.TimestampFormat)
                            .WithPerformance(consoleConfig.AutoFlush, consoleConfig.BufferSize, consoleConfig.FlushIntervalSeconds)
                            .WithUnityIntegration(consoleConfig.CaptureUnityLogs)
                            .WithMessageFormatting(consoleConfig.IncludeStackTraces, consoleConfig.IncludeSourceContext, consoleConfig.IncludeThreadId)
                            .WithStructuredLogging(consoleConfig.EnableStructuredLogging)
                            .WithMessageLengthLimit(consoleConfig.LimitMessageLength, consoleConfig.MaxMessageLength);
                            
                        builderCollection.AddUnityConsole(builder);
                    }
                    else
                    {
                        // Try to create target directly from config
                        var target = CreateTargetFromConfig(config);
                        if (target != null)
                        {
                            builderCollection.AddTarget(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LogManagerComponent] Failed to configure target from {config.TargetName}: {ex.Message}", this);
                }
            }
        }
        
        /// <summary>
        /// Creates a default logger manager when no configurations are provided.
        /// </summary>
        private void CreateDefaultLoggerManager()
        {
            if (_createDefaultUnityConsole && _createDefaultFileLogger)
            {
                // Create both console and file logger
                var (manager, logger) = _jobLoggerFactory.CreateForDevelopment(_defaultLogFilePath, Tagging.LogTag.Unity, _messageBus);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else if (_createDefaultUnityConsole)
            {
                // Console only
                var (manager, logger) = _jobLoggerFactory.CreateConsoleOnly(_globalMinimumLevel, true, Tagging.LogTag.Unity, _messageBus);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else if (_createDefaultFileLogger)
            {
                // File only
                var (manager, logger) = _jobLoggerFactory.CreateFileOnly(_defaultLogFilePath, _globalMinimumLevel, Tagging.LogTag.Unity, _messageBus);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
            }
            else
            {
                // Ultimate fallback - use JobLoggerManager directly
                _loggerManager = JobLoggerManager.CreateWithDefaultFormatter(
                    _initialCapacity,
                    _maxMessagesPerFlush,
                    _globalMinimumLevel,
                    _messageBus
                );
            }
        }
        
        /// <summary>
        /// Creates a log target from a configuration.
        /// </summary>
        /// <param name="config">The configuration to create a target from.</param>
        /// <returns>The created log target or null if creation failed.</returns>
        private ILogTarget CreateTargetFromConfig(ILogTargetConfig config)
        {
            if (config == null)
                return null;
                
            try
            {
                return _loggerManager.CreateTargetFromConfig(config);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Failed to create target from config {config.TargetName}: {ex.Message}", this);
                return null;
            }
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
        /// Initializes Unity logger integration.
        /// </summary>
        private void InitializeUnityLoggerIntegration()
        {
            var consoleConfig = FindUnityConsoleConfig();
            if (consoleConfig?.CaptureUnityLogs == true)
            {
                // Unity logger adapter would be initialized here
                // This depends on the actual UnityLoggerAdapter implementation
                // _unityLoggerAdapter = new UnityLoggerAdapter(_loggerManager);
                // _unityLoggerAdapter.Initialize(consoleConfig);
            }
        }
        
        /// <summary>
        /// Finds the Unity console configuration from validated configs.
        /// </summary>
        /// <returns>Unity console configuration or null if not found.</returns>
        private UnityConsoleTargetConfig FindUnityConsoleConfig()
        {
            return _validatedConfigs.OfType<UnityConsoleTargetConfig>().FirstOrDefault();
        }
        
        /// <summary>
        /// Creates a fallback logger when initialization fails.
        /// </summary>
        private void CreateFallbackLogger()
        {
            try
            {
                var (manager, logger) = _jobLoggerFactory.CreateConsoleOnly(LogLevel.Warning, false, Tagging.LogTag.Unity, null);
                _loggerManager = manager;
                _cachedJobLogger = logger;
                _hasDefaultJobLogger = true;
                _isInitialized = true;
                
                Debug.LogWarning("[LogManagerComponent] Fallback logger created", this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Failed to create fallback logger: {ex.Message}", this);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Cleans up the logging system and disposes resources.
        /// </summary>
        private void CleanupLoggingSystem()
        {
            if (_isDisposing)
                return;
                
            _isDisposing = true;
            
            try
            {
                Flush();
                
                _unityLoggerAdapter?.Dispose();
                _unityLoggerAdapter = null;
                
                _loggerManager?.Dispose();
                _loggerManager = null;
                
                _cachedJobLogger = default;
                _hasDefaultJobLogger = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Error during cleanup: {ex.Message}", this);
            }
            finally
            {
                _isInitialized = false;
                _isDisposing = false;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Flushes all log targets and returns the number of messages flushed.
        /// </summary>
        /// <returns>Number of messages flushed.</returns>
        public int Flush()
        {
            if (!_isInitialized || _loggerManager == null)
                return 0;
                
            try
            {
                return _loggerManager.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LogManagerComponent] Error during flush: {ex.Message}", this);
                return 0;
            }
        }
        
        /// <summary>
        /// Adds a custom log target to the manager.
        /// </summary>
        /// <param name="target">The target to add.</param>
        public void AddCustomTarget(ILogTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
                
            if (!_isInitialized || _loggerManager == null)
            {
                Debug.LogWarning("[LogManagerComponent] Cannot add target - system not initialized", this);
                return;
            }
            
            _loggerManager.AddTarget(target);
        }
        
        /// <summary>
        /// Creates and adds a target from configuration.
        /// </summary>
        /// <param name="config">The configuration to create a target from.</param>
        public void AddTargetFromConfig(ILogTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            var target = CreateTargetFromConfig(config);
            if (target != null)
            {
                AddCustomTarget(target);
            }
        }
        
        /// <summary>
        /// Removes a target from the manager.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns>True if the target was removed; otherwise, false.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            if (target == null || !_isInitialized || _loggerManager == null)
                return false;
                
            return _loggerManager.RemoveTarget(target);
        }
        
        /// <summary>
        /// Sets the global minimum log level for all targets.
        /// </summary>
        /// <param name="level">The minimum log level to set.</param>
        public void SetGlobalMinimumLevel(LogLevel level)
        {
            _globalMinimumLevel = level;
            
            if (_isInitialized && _loggerManager != null)
            {
                _loggerManager.GlobalMinimumLevel = level;
            }
        }
        
        /// <summary>
        /// Creates a job logger for use in Unity job contexts.
        /// </summary>
        /// <param name="minimumLevel">Optional minimum level override.</param>
        /// <param name="defaultTag">Default tag for messages.</param>
        /// <returns>A configured job logger.</returns>
        public JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (!_isInitialized || _loggerManager == null)
                throw new InvalidOperationException("LogManagerComponent is not initialized");
                
            return _loggerManager.CreateJobLogger(minimumLevel, defaultTag);
        }
        
        /// <summary>
        /// Gets the default job logger created during initialization.
        /// </summary>
        /// <returns>The default job logger if available.</returns>
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
        public JobLogger CreateParallelJobLogger(NativeQueue<LogMessage> queue, LogLevel minimumLevel, Tagging.LogTag defaultTag = default)
        {
            if (_jobLoggerFactory == null)
                throw new InvalidOperationException("JobLoggerFactory is not available");
                
            return _jobLoggerFactory.CreateParallel(queue, minimumLevel, defaultTag);
        }
        
        /// <summary>
        /// Updates Unity logger configuration.
        /// </summary>
        /// <param name="config">The new configuration to apply.</param>
        public void UpdateUnityLoggerConfig(UnityConsoleTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
                
            _unityLoggerAdapter?.UpdateConfiguration(config);
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
            
            if (_isInitialized && _loggerManager != null)
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
            if (!_isInitialized || _loggerManager == null)
                return;
                
            _loggerManager.Log(level, tag, message);
        }
        
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogDebug(string message, Tagging.LogTag tag = default)
        {
            if (!_isInitialized || _loggerManager == null)
                return;
                
            _loggerManager.Debug(message, tag.IsValid ? tag : Tagging.LogTag.Debug);
        }
        
        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogInfo(string message, Tagging.LogTag tag = default)
        {
            if (!_isInitialized || _loggerManager == null)
                return;
                
            _loggerManager.Info(message, tag.IsValid ? tag : Tagging.LogTag.Info);
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogWarning(string message, Tagging.LogTag tag = default)
        {
            if (!_isInitialized || _loggerManager == null)
                return;
                
            _loggerManager.Warning(message, tag.IsValid ? tag : Tagging.LogTag.Warning);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogError(string message, Tagging.LogTag tag = default)
        {
            if (!_isInitialized || _loggerManager == null)
                return;
                
            _loggerManager.Error(message, tag.IsValid ? tag : Tagging.LogTag.Error);
        }
        
        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">Optional tag for the message.</param>
        public void LogCritical(string message, Tagging.LogTag tag = default)
        {
            if (!_isInitialized || _loggerManager == null)
                return;
                
            _loggerManager.Critical(message, tag.IsValid ? tag : Tagging.LogTag.Critical);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Creates a default message bus implementation.
        /// </summary>
        /// <returns>A default message bus instance.</returns>
        private IMessageBus CreateDefaultMessageBus()
        {
            // Use the JobLoggerManager's null message bus factory
            return _loggerManager?.CreateNullMessageBus();
        }
        
        /// <summary>
        /// Creates a default log formatter implementation.
        /// </summary>
        /// <returns>A default log formatter instance.</returns>
        private ILogFormatter CreateDefaultLogFormatter()
        {
            // This would typically create a DefaultLogFormatter instance
            // Return null for now and let the system handle defaults
            return null;
        }
        
        #endregion
        
        #region Editor Support
        
        private void OnValidate()
        {
            _autoFlushInterval = Mathf.Max(0.1f, _autoFlushInterval);
            _initialCapacity = Mathf.Max(16, _initialCapacity);
            _maxMessagesPerFlush = Mathf.Max(1, _maxMessagesPerFlush);
            _maxMessagesPerFrame = Mathf.Max(1, _maxMessagesPerFrame);
            
            if (!string.IsNullOrEmpty(_defaultLogFilePath) && !_defaultLogFilePath.EndsWith(".log"))
            {
                _defaultLogFilePath += ".log";
            }
            
            // Ensure only one preset is selected
            if (_useDevelopmentPreset)
            {
                _useProductionPreset = false;
                _useHighPerformancePreset = false;
            }
            else if (_useProductionPreset)
            {
                _useDevelopmentPreset = false;
                _useHighPerformancePreset = false;
            }
            else if (_useHighPerformancePreset)
            {
                _useDevelopmentPreset = false;
                _useProductionPreset = false;
            }
        }
        
        #endregion
    }
    
    #region Supporting Interfaces
    
    /// <summary>
    /// Interface for dependency injection providers.
    /// </summary>
    public interface IDependencyProvider
    {
        /// <summary>
        /// Attempts to resolve a dependency of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The resolved instance or null if not available.</returns>
        T TryResolve<T>() where T : class;
    }
    
    #endregion
}