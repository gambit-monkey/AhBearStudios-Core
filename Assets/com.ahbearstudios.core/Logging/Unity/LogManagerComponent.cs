using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Adapters;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.LogTargets;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Unity;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// MonoBehaviour component that integrates the JobLoggerManager with the Unity lifecycle.
    /// Provides automatic initialization, updates, and cleanup of the logging system.
    /// </summary>
    public class LogManagerComponent : MonoBehaviour
    {
        #region Serialized Fields
        
        [Tooltip("Main configuration for the log manager")]
        [SerializeField] private LogManagerConfig _config;

        [Tooltip("Log target configurations to initialize")]
        [SerializeField] private LogTargetConfig[] _logTargetConfigs = Array.Empty<LogTargetConfig>();
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// The logger manager instance.
        /// </summary>
        private JobLoggerManager _loggerManager;

        /// <summary>
        /// The Unity logger adapter for integrating with Unity's logging system.
        /// </summary>
        private UnityLoggerAdapter _unityLoggerAdapter;

        /// <summary>
        /// List of log targets added to the manager.
        /// </summary>
        private readonly List<ILogTarget> _ownedTargets = new List<ILogTarget>();
        
        /// <summary>
        /// Auto-flush timer.
        /// </summary>
        private float _autoFlushTimer;
        
        /// <summary>
        /// Auto-flush interval.
        /// </summary>
        private float _autoFlushInterval;
        
        /// <summary>
        /// Flag indicating if auto-flush is enabled.
        /// </summary>
        private bool _autoFlushEnabled;
        
        /// <summary>
        /// The default log formatter.
        /// </summary>
        private ILogFormatter _defaultFormatter;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets the logger manager instance.
        /// </summary>
        public JobLoggerManager LoggerManager => _loggerManager;
        
        /// <summary>
        /// Gets the Unity logger adapter instance.
        /// </summary>
        public UnityLoggerAdapter UnityLoggerAdapter => _unityLoggerAdapter;
        
        #endregion
        
        #region Unity Lifecycle Methods
        
        /// <summary>
        /// Initialize the log manager and its targets.
        /// </summary>
        private void Awake()
        {
            _defaultFormatter = new DefaultLogFormatter();
            InitializeLogManager();
        }

        /// <summary>
        /// Update the log manager to handle auto-flushing.
        /// </summary>
        private void Update()
        {
            if (_loggerManager != null && _autoFlushEnabled)
            {
                _autoFlushTimer += Time.deltaTime;
                
                if (_autoFlushTimer >= _autoFlushInterval)
                {
                    _loggerManager.Flush();
                    _autoFlushTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// Clean up resources when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            CleanupLogManager();
        }
        
        /// <summary>
        /// Ensure cleanup when application quits.
        /// </summary>
        private void OnApplicationQuit()
        {
            CleanupLogManager();
        }
        
        #endregion
        
        #region Initialization Methods
        
        /// <summary>
        /// Initialize the log manager and configure its targets.
        /// </summary>
        private void InitializeLogManager()
        {
            // Create a list to hold initial targets
            var initialTargets = new List<ILogTarget>();

            try
            {
                // Create default target if no config or no targets
                if (_config == null || _logTargetConfigs == null || _logTargetConfigs.Length == 0)
                {
                    Debug.LogWarning("No LogManagerConfig or targets assigned. Using default settings.");
                    
                    // Create a Unity Console target as the default target
                    var defaultTarget = CreateDefaultTarget();
                    initialTargets.Add(defaultTarget);
                    _ownedTargets.Add(defaultTarget);
                }
                
                // Create the log manager
                CreateLoggerManager(initialTargets);
                
                // Initialize custom log targets from configurations
                InitializeCustomLogTargets();
                
                // Initialize Unity Logger Integration
                InitializeUnityLoggerIntegration();
                
                // Configure auto-flush
                ConfigureAutoFlush();
            }
            catch (Exception ex)
            {
                // Clean up any resources that might have been created
                CleanupTargets(initialTargets);
                
                Debug.LogError($"Failed to initialize log system: {ex.Message}");

                // Create a fallback minimal logger for critical messages
                CreateFallbackLogger();
            }
        }
        
        /// <summary>
        /// Creates the JobLoggerManager instance.
        /// </summary>
        /// <param name="initialTargets">Initial log targets to use.</param>
        private void CreateLoggerManager(List<ILogTarget> initialTargets)
        {
            // Get configuration values
            int initialCapacity = _config?.InitialQueueCapacity ?? 64;
            int maxMessagesPerBatch = _config?.MaxMessagesPerBatch ?? 200;
            LogLevel minimumLevel = _config?.MinimumLevel ?? LogLevel.Info;
            
            try
            {
                _loggerManager = new JobLoggerManager(
                    _defaultFormatter, 
                    initialCapacity,
                    maxMessagesPerBatch,
                    minimumLevel
                );
                
                // Add initial targets
                foreach (var target in initialTargets)
                {
                    _loggerManager.AddTarget(target);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create JobLoggerManager: {ex.Message}");
                
                // Clean up any targets we've already created
                CleanupTargets(initialTargets);
                
                throw; // Re-throw to be caught by outer try/catch
            }
        }
        
        /// <summary>
        /// Creates a default console target when no configuration is provided.
        /// </summary>
        /// <returns>A default console log target.</returns>
        private ILogTarget CreateDefaultTarget()
        {
            return new UnityConsoleTarget(
                "DefaultUnityConsole",
                _config?.MinimumLevel ?? LogLevel.Error
            );
        }
        
        /// <summary>
        /// Configures auto-flush settings from the configuration.
        /// </summary>
        private void ConfigureAutoFlush()
        {
            if (_config == null || !_config.EnableAutoFlush) 
            {
                _autoFlushEnabled = false;
                return;
            }
            
            _autoFlushEnabled = true;
            _autoFlushInterval = _config.AutoFlushInterval;
            _autoFlushTimer = 0f;
        }
        
        /// <summary>
        /// Initializes integration with Unity's logging system by setting up the UnityLoggerAdapter.
        /// </summary>
        private void InitializeUnityLoggerIntegration()
        {
            if (_loggerManager == null)
                return;
                
            try
            {
                // Find any UnityConsoleLogConfig in the target configs
                UnityConsoleLogConfig unityConfig = FindUnityConsoleLogConfig();
                
                // Create an adapter from JobLogger to IBurstLogger
                var burstLoggerAdapter = new JobLoggerToBurstAdapter(_loggerManager);
                
                // Create the Unity logger adapter with the IBurstLogger adapter
                _unityLoggerAdapter = new UnityLoggerAdapter(burstLoggerAdapter, unityConfig);
                
                // Note: The adapter automatically registers with Unity if specified in the config
                if (_unityLoggerAdapter.IsRegisteredWithUnity)
                {
                    Debug.Log("Unity logger integration initialized successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Unity logger integration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Finds or creates a UnityConsoleLogConfig to use for Unity logger integration.
        /// </summary>
        /// <returns>A UnityConsoleLogConfig instance or null.</returns>
        private UnityConsoleLogConfig FindUnityConsoleLogConfig()
        {
            // Find in target configs first
            foreach (var config in _logTargetConfigs)
            {
                if (config is UnityConsoleLogConfig consoleConfig)
                {
                    return consoleConfig;
                }
            }
            
            // Then try to find one in the project
            var foundConfigs = Resources.FindObjectsOfTypeAll<UnityConsoleLogConfig>();
            if (foundConfigs.Length > 0)
            {
                return foundConfigs[0];
            }
            
            // None found
            Debug.Log("No UnityConsoleLogConfig found. Using default settings for Unity log integration.");
            return null;
        }
        
        /// <summary>
        /// Creates a minimal fallback logger when normal initialization fails.
        /// This ensures we have at least a basic logging capability even after errors.
        /// </summary>
        private void CreateFallbackLogger()
        {
            try
            {
                // Create a single console target for error messages
                var fallbackTarget = new UnityConsoleTarget("FallbackConsole", LogLevel.Error);
                _ownedTargets.Add(fallbackTarget);

                // Create a very simple logger with minimal configuration
                _loggerManager = new JobLoggerManager(
                    new DefaultLogFormatter(),
                    16,  // Small queue size
                    50,  // Small batch size
                    LogLevel.Error  // Only log errors and critical messages
                );
                
                _loggerManager.AddTarget(fallbackTarget);
                
                // Configure minimal auto-flush
                _autoFlushEnabled = true;
                _autoFlushInterval = 1.0f;
                _autoFlushTimer = 0f;

                Debug.LogWarning("Using fallback logger due to initialization errors.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create fallback logger: {ex.Message}");
                // At this point we give up on logging
            }
        }
        
        /// <summary>
        /// Initializes custom log targets from configurations.
        /// </summary>
        private void InitializeCustomLogTargets()
        {
            if (_logTargetConfigs == null || _logTargetConfigs.Length == 0 || _loggerManager == null)
            {
                return;
            }

            foreach (var config in _logTargetConfigs)
            {
                if (config == null)
                    continue;

                try
                {
                    // Create the target from configuration
                    var target = config.CreateTarget();
                    if (target == null)
                        continue;

                    // Add it to the manager
                    _loggerManager.AddTarget(target);
                    _ownedTargets.Add(target);

                    Debug.Log($"Initialized custom log target: {target.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to initialize log target from config {config.name}: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Cleanup Methods
        
        /// <summary>
        /// Clean up the log manager and its targets.
        /// </summary>
        private void CleanupLogManager()
        {
            // Avoid multiple cleanup calls
            if (_loggerManager == null)
                return;
                
            // Dispose the Unity logger adapter if it exists
            if (_unityLoggerAdapter != null)
            {
                try
                {
                    _unityLoggerAdapter.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing Unity logger adapter: {ex.Message}");
                }
                finally
                {
                    _unityLoggerAdapter = null;
                }
            }
            
            // Flush any remaining logs
            try
            {
                _loggerManager.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during final log flush: {ex.Message}");
            }

            // Create a copy of the owned targets for safe cleanup
            CleanupTargets(_ownedTargets);

            // Dispose the manager - this will release the native resources
            try
            {
                _loggerManager.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing logger manager: {ex.Message}");
            }
            finally
            {
                _loggerManager = null;
            }
        }
        
        /// <summary>
        /// Helper method to clean up targets.
        /// </summary>
        /// <param name="targets">List of targets to dispose.</param>
        private void CleanupTargets(List<ILogTarget> targets)
        {
            if (targets == null || targets.Count == 0)
                return;

            // Create a copy of the list to avoid "Collection was modified" error during enumeration
            var targetsToDispose = new List<ILogTarget>(targets);

            foreach (var target in targetsToDispose)
            {
                try
                {
                    if (target != null)
                    {
                        target.Dispose();
                        // Only remove from _ownedTargets if we're disposing our own targets
                        if (targets == _ownedTargets || _ownedTargets.Contains(target))
                        {
                            _ownedTargets.Remove(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing log target {target?.GetType().Name}: {ex.Message}");
                }
            }
            
            // If we're cleaning up _ownedTargets, ensure it's cleared
            if (targets == _ownedTargets)
            {
                _ownedTargets.Clear();
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually flushes any queued log messages.
        /// </summary>
        /// <returns>The number of messages processed.</returns>
        public int Flush()
        {
            return _loggerManager?.Flush() ?? 0;
        }

        /// <summary>
        /// Adds a custom log target to the manager.
        /// The target will be owned by this component and disposed when it is destroyed.
        /// </summary>
        /// <param name="target">The log target to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        public void AddCustomTarget(ILogTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (_loggerManager != null)
            {
                _loggerManager.AddTarget(target);
                _ownedTargets.Add(target);
            }
        }

        /// <summary>
        /// Adds a log target from a configuration.
        /// The target will be owned by this component and disposed when it is destroyed.
        /// </summary>
        /// <param name="config">The configuration to create the target from.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        public void AddTargetFromConfig(LogTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_loggerManager != null)
            {
                var target = config.CreateTarget();
                if (target != null)
                {
                    _loggerManager.AddTarget(target);
                    _ownedTargets.Add(target);
                }
            }
        }

        /// <summary>
        /// Removes a log target from the manager.
        /// If the target was added via AddCustomTarget, it will be disposed.
        /// </summary>
        /// <param name="target">The log target to remove.</param>
        /// <returns>True if the target was removed; otherwise, false.</returns>
        public bool RemoveTarget(ILogTarget target)
        {
            if (target == null || _loggerManager == null)
                return false;

            bool removed = _loggerManager.RemoveTarget(target);

            if (removed && _ownedTargets.Contains(target))
            {
                _ownedTargets.Remove(target);
                try
                {
                    target.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing removed log target {target.Name}: {ex.Message}");
                }
            }

            return removed;
        }

        /// <summary>
        /// Sets the global minimum log level for all targets.
        /// </summary>
        /// <param name="level">The new minimum log level.</param>
        public void SetGlobalMinimumLevel(LogLevel level)
        {
            if (_loggerManager != null)
            {
                _loggerManager.GlobalMinimumLevel = level;
            }
        }

        /// <summary>
        /// Creates a JobLogger for use in Unity Jobs.
        /// </summary>
        /// <param name="minimumLevel">Optional minimum level override for this logger.</param>
        /// <param name="defaultTag">Default tag to use when none is specified.</param>
        /// <returns>A configured JobLogger.</returns>
        public Jobs.JobLogger CreateJobLogger(LogLevel? minimumLevel = null, Tagging.LogTag defaultTag = default)
        {
            if (_loggerManager == null)
            {
                throw new InvalidOperationException("LoggerManager is not initialized");
            }

            // Use the config's default tag if none specified and we have a config
            if (defaultTag == default && _config != null)
            {
                defaultTag = _config.DefaultTag;
            }
            else if (defaultTag == default)
            {
                defaultTag = Tagging.LogTag.Job; // Fallback default
            }

            return _loggerManager.CreateJobLogger(minimumLevel, defaultTag);
        }
        
        /// <summary>
        /// Updates the Unity logger adapter with a new configuration.
        /// </summary>
        /// <param name="config">The new Unity console log configuration.</param>
        public void UpdateUnityLoggerConfig(UnityConsoleLogConfig config)
        {
            if (_unityLoggerAdapter != null && _loggerManager != null)
            {
                // Dispose the existing adapter
                _unityLoggerAdapter.Dispose();
                
                // Create a new one with the updated config using our adapter
                var burstLoggerAdapter = new JobLoggerToBurstAdapter(_loggerManager);
                _unityLoggerAdapter = new UnityLoggerAdapter(burstLoggerAdapter, config);
            }
        }
        
        /// <summary>
        /// Enables or disables auto-flush with the specified interval.
        /// </summary>
        /// <param name="enabled">Whether auto-flush should be enabled.</param>
        /// <param name="interval">The auto-flush interval in seconds.</param>
        public void SetAutoFlush(bool enabled, float interval = 0.5f)
        {
            _autoFlushEnabled = enabled;
            if (enabled && interval > 0)
            {
                _autoFlushInterval = interval;
                _autoFlushTimer = 0f;
            }
        }
        
        /// <summary>
        /// Log a message directly through the logger manager.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="tag">The log tag.</param>
        /// <param name="message">The message to log.</param>
        public void Log(LogLevel level, Tagging.LogTag tag, string message)
        {
            _loggerManager?.Log(level, tag, message);
        }
        
        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogDebug(string message, Tagging.LogTag tag = Tagging.LogTag.Debug)
        {
            _loggerManager?.Debug(message, tag);
        }
        
        /// <summary>
        /// Log an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogInfo(string message, Tagging.LogTag tag = Tagging.LogTag.Info)
        {
            _loggerManager?.Info(message, tag);
        }
        
        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogWarning(string message, Tagging.LogTag tag = Tagging.LogTag.Warning)
        {
            _loggerManager?.Warning(message, tag);
        }
        
        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogError(string message, Tagging.LogTag tag = Tagging.LogTag.Error)
        {
            _loggerManager?.Error(message, tag);
        }
        
        /// <summary>
        /// Log a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="tag">The log tag.</param>
        public void LogCritical(string message, Tagging.LogTag tag = Tagging.LogTag.Critical)
        {
            _loggerManager?.Critical(message, tag);
        }
        
        #endregion
    }
}