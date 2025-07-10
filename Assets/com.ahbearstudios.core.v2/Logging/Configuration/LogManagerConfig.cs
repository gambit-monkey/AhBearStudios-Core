
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Configuration
{
    /// <summary>
    /// ScriptableObject that implements ILoggerConfig for configuring the overall logging system.
    /// This configuration manages both global logging settings and individual log targets.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LogManagerConfig", 
        menuName = "AhBearStudios/Logging/Log Manager Config", 
        order = 0)]
    public class LogManagerConfig : ScriptableObject, ILoggerConfig
    {
        [Header("Global Logging Settings")]
        [Tooltip("The minimum log level that will be processed")]
        [SerializeField] private LogLevel _minimumLevel = LogLevel.Info;
        
        [Tooltip("Maximum number of messages to process per batch")]
        [SerializeField] private int _maxMessagesPerBatch = 200;
        
        [Tooltip("Initial capacity of the log queue")]
        [SerializeField] private int _initialQueueCapacity = 64;
        
        [Tooltip("Enable automatic flushing of logs")]
        [SerializeField] private bool _enableAutoFlush = true;
        
        [Tooltip("Interval in seconds between auto-flush operations")]
        [SerializeField] private float _autoFlushInterval = 0.5f;
        
        [Tooltip("Default tag to use when no tag is specified")]
        [SerializeField] private Tagging.LogTag _defaultTag = Tagging.LogTag.Default;
        
        [Header("Async Logging Settings")]
        [Tooltip("Whether async logging is enabled")]
        [SerializeField] private bool _enableAsyncLogging = false;
        
        [Tooltip("Capacity of the async queue for log messages")]
        [SerializeField] private int _asyncQueueCapacity = 1000;
        
        [Tooltip("Timeout in seconds for async flush operations")]
        [SerializeField] private float _asyncFlushTimeoutSeconds = 5.0f;
        
        [Header("Log Targets")]
        [Tooltip("List of log targets that will receive log messages")]
        [SerializeField] private ILogTargetConfig[] _logTargets = Array.Empty<ILogTargetConfig>();
        
        [Header("Advanced Settings")]
        [Tooltip("Whether to enable message bus integration for logging events")]
        [SerializeField] private bool _enableMessageBusIntegration = false;
        
        [Tooltip("Configuration name for identification")]
        [SerializeField] private string _configurationName = "Default";
        
        [Tooltip("Whether to validate log targets on startup")]
        [SerializeField] private bool _validateTargetsOnStartup = true;
        
        [Tooltip("Whether to create directories for file-based targets automatically")]
        [SerializeField] private bool _autoCreateDirectories = true;
        
        #region ILoggerConfig Implementation
        
        /// <summary>
        /// The minimum log level that will be processed.
        /// </summary>
        public LogLevel MinimumLevel => _minimumLevel;
        
        /// <summary>
        /// Maximum number of messages to process per batch.
        /// </summary>
        public int MaxMessagesPerBatch => _maxMessagesPerBatch;
        
        /// <summary>
        /// Default tag to use when no tag is specified.
        /// </summary>
        public Tagging.LogTag DefaultTag => _defaultTag;
        
        /// <summary>
        /// Gets or sets whether async logging is enabled.
        /// </summary>
        public bool EnableAsyncLogging
        {
            get => _enableAsyncLogging;
            set
            {
                _enableAsyncLogging = value;
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
        
        /// <summary>
        /// Gets or sets the async queue capacity.
        /// </summary>
        public int AsyncQueueCapacity
        {
            get => _asyncQueueCapacity;
            set
            {
                _asyncQueueCapacity = Mathf.Max(1, value); // Ensure positive value
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }
        
        /// <summary>
        /// Gets or sets the flush timeout for async operations.
        /// </summary>
        public float AsyncFlushTimeoutSeconds
        {
            get => _asyncFlushTimeoutSeconds;
            set
            {
                _asyncFlushTimeoutSeconds = Mathf.Max(0.1f, value); // Ensure reasonable minimum timeout
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
        }

        #endregion
        
        #region Additional Properties
        
        /// <summary>
        /// Initial capacity of the log queue.
        /// </summary>
        public int InitialQueueCapacity => _initialQueueCapacity;
        
        /// <summary>
        /// Whether to enable automatic flushing of logs.
        /// </summary>
        public bool EnableAutoFlush => _enableAutoFlush;
        
        /// <summary>
        /// Interval in seconds between auto-flush operations.
        /// </summary>
        public float AutoFlushInterval => _autoFlushInterval;
        
        /// <summary>
        /// Array of log target configurations.
        /// </summary>
        public ILogTargetConfig[] LogTargets => _logTargets ?? Array.Empty<ILogTargetConfig>();
        
        /// <summary>
        /// Whether message bus integration is enabled.
        /// </summary>
        public bool EnableMessageBusIntegration => _enableMessageBusIntegration;
        
        /// <summary>
        /// Name of this configuration for identification purposes.
        /// </summary>
        public string ConfigurationName => _configurationName;
        
        /// <summary>
        /// Whether to validate log targets on startup.
        /// </summary>
        public bool ValidateTargetsOnStartup => _validateTargetsOnStartup;
        
        /// <summary>
        /// Whether to automatically create directories for file-based targets.
        /// </summary>
        public bool AutoCreateDirectories => _autoCreateDirectories;
        
        #endregion
        
        #region Target Management
        
        /// <summary>
        /// Gets all enabled log target configurations.
        /// </summary>
        /// <returns>Collection of enabled log target configurations.</returns>
        public IEnumerable<ILogTargetConfig> GetEnabledTargets()
        {
            return LogTargets?.Where(target => target != null && target.Enabled) ?? Enumerable.Empty<ILogTargetConfig>();
        }
        
        /// <summary>
        /// Gets log target configurations by name.
        /// </summary>
        /// <param name="targetName">Name of the target to find.</param>
        /// <returns>The log target configuration if found, null otherwise.</returns>
        public ILogTargetConfig GetTargetByName(string targetName)
        {
            if (string.IsNullOrEmpty(targetName) || LogTargets == null)
                return null;
            
            return LogTargets.FirstOrDefault(target => 
                target != null && 
                string.Equals(target.TargetName, targetName, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Gets the count of enabled log targets.
        /// </summary>
        /// <returns>Number of enabled log targets.</returns>
        public int GetEnabledTargetCount()
        {
            return GetEnabledTargets().Count();
        }
        
        /// <summary>
        /// Checks if any log targets are configured and enabled.
        /// </summary>
        /// <returns>True if there are enabled targets, false otherwise.</returns>
        public bool HasEnabledTargets()
        {
            return GetEnabledTargetCount() > 0;
        }
        
        /// <summary>
        /// Creates actual log target instances from the configurations.
        /// </summary>
        /// <returns>Collection of created log target instances.</returns>
        public IEnumerable<ILogTarget> CreateTargetInstances()
        {
            var targets = new List<ILogTarget>();
            
            foreach (var targetConfig in GetEnabledTargets())
            {
                try
                {
                    var target = targetConfig.CreateTarget();
                    if (target != null)
                    {
                        targets.Add(target);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create log target '{targetConfig.TargetName}': {ex.Message}");
                }
            }
            
            return targets;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the configuration and reports any issues.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public bool ValidateConfiguration()
        {
            bool isValid = true;
            
            // Check if we have any targets
            if (!HasEnabledTargets())
            {
                Debug.LogWarning($"LogManagerConfig '{name}': No enabled log targets configured. Logging will not work.");
                isValid = false;
            }
            
            // Validate individual targets
            foreach (var target in LogTargets)
            {
                if (target == null)
                {
                    Debug.LogWarning($"LogManagerConfig '{name}': Null log target found in configuration.");
                    isValid = false;
                    continue;
                }
                
                // Check for duplicate target names
                var duplicates = LogTargets.Where(t => t != null && t != target && t.TargetName == target.TargetName).ToList();
                if (duplicates.Any())
                {
                    Debug.LogWarning($"LogManagerConfig '{name}': Duplicate target name '{target.TargetName}' found.");
                    isValid = false;
                }
                
                // Validate target-specific settings
                if (string.IsNullOrEmpty(target.TargetName))
                {
                    Debug.LogWarning($"LogManagerConfig '{name}': Log target has empty name.");
                    isValid = false;
                }
            }
            
            // Validate global settings
            if (_maxMessagesPerBatch <= 0)
            {
                Debug.LogWarning($"LogManagerConfig '{name}': MaxMessagesPerBatch must be positive.");
                isValid = false;
            }
            
            if (_initialQueueCapacity <= 0)
            {
                Debug.LogWarning($"LogManagerConfig '{name}': InitialQueueCapacity must be positive.");
                isValid = false;
            }
            
            if (_enableAutoFlush && _autoFlushInterval <= 0)
            {
                Debug.LogWarning($"LogManagerConfig '{name}': AutoFlushInterval must be positive when auto-flush is enabled.");
                isValid = false;
            }
            
            // Validate async settings
            if (_enableAsyncLogging)
            {
                if (_asyncQueueCapacity <= 0)
                {
                    Debug.LogWarning($"LogManagerConfig '{name}': AsyncQueueCapacity must be positive when async logging is enabled.");
                    isValid = false;
                }
                
                if (_asyncFlushTimeoutSeconds <= 0)
                {
                    Debug.LogWarning($"LogManagerConfig '{name}': AsyncFlushTimeoutSeconds must be positive when async logging is enabled.");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        #endregion
        
        #region Unity Editor Integration
        
        /// <summary>
        /// Called when values are changed in the inspector.
        /// Validates settings and provides feedback.
        /// </summary>
        private void OnValidate()
        {
            // Ensure positive values
            if (_maxMessagesPerBatch <= 0)
            {
                _maxMessagesPerBatch = 200;
                Debug.LogWarning($"MaxMessagesPerBatch must be positive. Reset to {_maxMessagesPerBatch}.");
            }
            
            if (_initialQueueCapacity <= 0)
            {
                _initialQueueCapacity = 64;
                Debug.LogWarning($"InitialQueueCapacity must be positive. Reset to {_initialQueueCapacity}.");
            }
            
            if (_autoFlushInterval <= 0)
            {
                _autoFlushInterval = 0.5f;
                Debug.LogWarning($"AutoFlushInterval must be positive. Reset to {_autoFlushInterval}.");
            }
            
            // Validate async settings
            if (_asyncQueueCapacity <= 0)
            {
                _asyncQueueCapacity = 1000;
                Debug.LogWarning($"AsyncQueueCapacity must be positive. Reset to {_asyncQueueCapacity}.");
            }
            
            if (_asyncFlushTimeoutSeconds <= 0)
            {
                _asyncFlushTimeoutSeconds = 5.0f;
                Debug.LogWarning($"AsyncFlushTimeoutSeconds must be positive. Reset to {_asyncFlushTimeoutSeconds}.");
            }
            
            // Ensure configuration name is not empty
            if (string.IsNullOrEmpty(_configurationName))
            {
                _configurationName = "Config_" + System.Guid.NewGuid().ToString().Substring(0, 8);
                Debug.LogWarning($"Configuration name cannot be empty. Generated: {_configurationName}");
            }
            
            // Remove null entries from log targets array
            if (_logTargets != null)
            {
                _logTargets = _logTargets.Where(target => target != null).ToArray();
            }
        }
        
        #endregion
        
        #region Editor Utilities
        
        #if UNITY_EDITOR
        /// <summary>
        /// Adds a log target configuration to this manager config.
        /// </summary>
        /// <param name="targetConfig">The target configuration to add.</param>
        public void AddLogTarget(ILogTargetConfig targetConfig)
        {
            if (targetConfig == null)
                return;
            
            var targetsList = _logTargets?.ToList() ?? new List<ILogTargetConfig>();
            
            // Check for duplicate names
            if (targetsList.Any(t => t != null && t.TargetName == targetConfig.TargetName))
            {
                Debug.LogWarning($"Target with name '{targetConfig.TargetName}' already exists.");
                return;
            }
            
            targetsList.Add(targetConfig);
            _logTargets = targetsList.ToArray();
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// Removes a log target configuration from this manager config.
        /// </summary>
        /// <param name="targetConfig">The target configuration to remove.</param>
        public void RemoveLogTarget(ILogTargetConfig targetConfig)
        {
            if (targetConfig == null || _logTargets == null)
                return;
            
            var targetsList = _logTargets.ToList();
            if (targetsList.Remove(targetConfig))
            {
                _logTargets = targetsList.ToArray();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        /// <summary>
        /// Removes a log target by name.
        /// </summary>
        /// <param name="targetName">Name of the target to remove.</param>
        public void RemoveLogTargetByName(string targetName)
        {
            var target = GetTargetByName(targetName);
            if (target != null)
            {
                RemoveLogTarget(target);
            }
        }
        #endif
        
        #endregion
    }
}