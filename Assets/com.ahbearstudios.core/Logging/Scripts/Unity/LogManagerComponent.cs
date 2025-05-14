
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AhBearStudios.Core.Logging.Config;
using AhBearStudios.Core.Logging.LogTargets;
using AhBearStudios.Core.Logging.Unity;
using AhBearStudios.Core.Logging.Formatters;
using AhBearStudios.Core.Logging.Jobs;

namespace AhBearStudios.Core.Logging
{
    /// <summary>
    /// MonoBehaviour component that integrates the JobLoggerManager with the Unity lifecycle.
    /// Provides automatic initialization, updates, and cleanup of the logging system.
    /// </summary>
    public class LogManagerComponent : MonoBehaviour
    {
        [Tooltip("Main configuration for the log manager")] [SerializeField]
        private LogManagerConfig _config;

        [Tooltip("Log target configurations to initialize")] [SerializeField]
        private LogTargetConfig[] _logTargetConfigs = new LogTargetConfig[0];

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
        /// Gets the logger manager instance.
        /// </summary>
        public JobLoggerManager LoggerManager => _loggerManager;

        /// <summary>
        /// Initialize the log manager and its targets.
        /// </summary>
        private void Awake()
        {
            InitializeLogManager();
        }

        /// <summary>
        /// Update the log manager to handle auto-flushing.
        /// </summary>
        private void Update()
        {
            if (_loggerManager != null)
            {
                _loggerManager.Update(Time.deltaTime);
            }
        }

        /// <summary>
        /// Initialize the log manager and configure its targets.
        /// </summary>
        private void InitializeLogManager()
        {
            // Create a list to hold initial targets
            var initialTargets = new List<ILogTarget>();

            // Track if we've created a manager that needs to be disposed in case of exception
            JobLoggerManager tempManager = null;

            try
            {
                // Check if we have a config
                if (_config == null)
                {
                    UnityEngine.Debug.LogWarning("No LogManagerConfig assigned. Using default settings.");

                    // Create a Unity Console target as the default target
                    var defaultTarget = new UnityConsoleTarget("DefaultUnityConsole", LogLevel.Error);
                    initialTargets.Add(defaultTarget);
                    _ownedTargets.Add(defaultTarget);
                }
                
                // If no targets are configured, add a default Unity console target
                if (initialTargets.Count == 0)
                {
                    var defaultTarget = new UnityConsoleTarget("DefaultUnityConsole",
                        _config != null ? _config.MinimumLevel : LogLevel.Error);
                    initialTargets.Add(defaultTarget);
                    _ownedTargets.Add(defaultTarget);
                    UnityEngine.Debug.LogWarning("No log targets were enabled. Adding default Unity console target.");
                }

                // Create the formatter
                var formatter = new DefaultLogFormatter();

                // Get configuration values
                int initialCapacity = _config?.InitialQueueCapacity ?? 64;
                int maxMessagesPerBatch = _config?.MaxMessagesPerBatch ?? 200;
                byte minimumLevel = _config?.MinimumLevel ?? LogLevel.Info;

                // Create the log manager with initial targets
                try
                {
                    tempManager = new JobLoggerManager(
                        initialTargets,
                        formatter,
                        initialCapacity,
                        maxMessagesPerBatch,
                        minimumLevel
                    );

                    // Configure auto-flush
                    if (_config?.EnableAutoFlush ?? true)
                    {
                        float autoFlushInterval = _config?.AutoFlushInterval ?? 0.5f;
                        tempManager.EnableAutoFlush(autoFlushInterval);
                    }

                    // Only assign to the member variable after successful initialization
                    _loggerManager = tempManager;
                    tempManager = null; // Clear temp reference so we don't dispose it

                    // Initialize custom log targets from configurations after manager is successfully created
                    InitializeCustomLogTargets();
                    
                    // Initialize Unity Logger Adapter
                    InitializeUnityLoggerIntegration();
                }
                catch (Exception ex)
                {
                    // Specific handling for manager creation failure
                    UnityEngine.Debug.LogError($"Failed to create JobLoggerManager: {ex.Message}");

                    // Clean up any targets we've already created
                    CleanupTargets(initialTargets);

                    throw; // Re-throw to be caught by outer try/catch
                }
            }
            catch (Exception ex)
            {
                // Clean up any resources that might have been created
                CleanupTargets(initialTargets);

                // Dispose the temporary manager if it was created and not assigned to _loggerManager
                if (tempManager != null)
                {
                    try
                    {
                        tempManager.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        UnityEngine.Debug.LogError($"Error disposing temporary log manager: {disposeEx.Message}");
                    }
                }

                UnityEngine.Debug.LogError($"Failed to initialize log system: {ex.Message}");

                // Instead of rethrowing, create a fallback minimal logger for critical messages
                CreateFallbackLogger();
            }
        }

        /// <summary>
        /// Creates a bridge adapter between JobLogger and IBurstLogger
        /// </summary>
        private class JobLoggerToBurstAdapter : IBurstLogger
        {
            private readonly JobLoggerManager _manager;
    
            public JobLoggerToBurstAdapter(JobLoggerManager manager)
            {
                _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            }
    
            public void Log(byte level, string message, string tag)
            {
                // JobLoggerManager.Log expects (level, tag, message) order
                // and a Tagging.LogTag type for the tag parameter
        
                // Handle the tag conversion - try to parse as LogTag enum first
                if (System.Enum.TryParse<Tags.Tagging.LogTag>(tag, true, out var logTag))
                {
                    _manager.Log(level, logTag, message);
                }
                else
                {
                    // Fallback to Default tag with the original string in the message
                    _manager.Log(level, Tags.Tagging.LogTag.Default, $"{tag}: {message}");
                }
            }
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
                UnityConsoleLogConfig unityConfig = null;
                foreach (var config in _logTargetConfigs)
                {
                    if (config is UnityConsoleLogConfig consoleConfig)
                    {
                        unityConfig = consoleConfig;
                        break;
                    }
                }
                
                // If no config was found among targets, try to find one in the project
                if (unityConfig == null)
                {
                    unityConfig = Resources.FindObjectsOfTypeAll<UnityConsoleLogConfig>().Length > 0 
                        ? Resources.FindObjectsOfTypeAll<UnityConsoleLogConfig>()[0] 
                        : null;
                }
                
                // If still no config, create a default one
                if (unityConfig == null)
                {
                    Debug.Log("No UnityConsoleLogConfig found. Using default settings for Unity log integration.");
                    // We don't create a ScriptableObject here to avoid editor-only functionality at runtime
                }
                
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
                var targets = new List<ILogTarget> { fallbackTarget };
                _loggerManager = new JobLoggerManager(
                    targets,
                    new DefaultLogFormatter(),
                    16, // Small queue size
                    50, // Small batch size
                    LogLevel.Error // Only log errors and critical messages
                );

                _loggerManager.EnableAutoFlush(1.0f);

                UnityEngine.Debug.LogWarning("Using fallback logger due to initialization errors.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to create fallback logger: {ex.Message}");
                // At this point we give up on logging
            }
        }

        /// <summary>
        /// Helper method to clean up targets in case of initialization failure.
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
                    UnityEngine.Debug.LogError($"Error disposing log target {target?.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clean up the log manager and its targets.
        /// </summary>
        private void CleanupLogManager()
        {
            // Dispose the Unity logger adapter if it exists
            if (_unityLoggerAdapter != null)
            {
                try
                {
                    _unityLoggerAdapter.Dispose();
                    _unityLoggerAdapter = null;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error disposing Unity logger adapter: {ex.Message}");
                }
            }
            
            if (_loggerManager != null)
            {
                // Flush any remaining logs
                try
                {
                    _loggerManager.Flush();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error during final log flush: {ex.Message}");
                }

                // Create a copy of the owned targets for safe cleanup
                var targetsToCleanup = new List<ILogTarget>(_ownedTargets);

                // Dispose owned targets - using a copy to avoid modification during enumeration
                foreach (var target in targetsToCleanup)
                {
                    try
                    {
                        if (target != null)
                        {
                            target.Dispose();
                            _ownedTargets.Remove(target);
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(
                            $"Error disposing log target {target?.GetType().Name}: {ex.Message}");
                    }
                }

                // Ensure _ownedTargets is now empty
                _ownedTargets.Clear();

                // Dispose the manager - this will release the native resources
                try
                {
                    _loggerManager.Dispose();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error disposing logger manager: {ex.Message}");
                }
                finally
                {
                    _loggerManager = null;
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
        /// Resolves a file path, making it absolute if it's relative.
        /// </summary>
        /// <param name="path">The file path to resolve.</param>
        /// <returns>The resolved absolute path.</returns>
        private string ResolveFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Path.Combine(Application.persistentDataPath, "Logs/app.log");

            // If the path is relative, combine it with the application's persistent data path
            if (!Path.IsPathRooted(path))
            {
                return Path.Combine(Application.persistentDataPath, path);
            }

            return path;
        }

        /// <summary>
        /// Initializes custom log targets from configurations.
        /// </summary>
        private void InitializeCustomLogTargets()
        {
            if (_logTargetConfigs == null || _logTargetConfigs.Length == 0)
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
                _loggerManager.AddTarget(target);
                _ownedTargets.Add(target);
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
        public void SetGlobalMinimumLevel(byte level)
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
        public Jobs.JobLogger CreateJobLogger(byte? minimumLevel = null, Tags.Tagging.LogTag defaultTag = default)
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
                defaultTag = Tags.Tagging.LogTag.Job; // Fallback default
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
        /// Gets the Unity logger adapter instance.
        /// </summary>
        public UnityLoggerAdapter UnityLoggerAdapter => _unityLoggerAdapter;
    }
}