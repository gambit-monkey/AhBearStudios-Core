using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Utilities;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Utility to create log directories for LogManagerConfig.
    /// This ensures log directories exist before the app starts logging.
    /// </summary>
    [InitializeOnLoad]
    public static class LogDirectoryUtility
    {
        /// <summary>
        /// Static constructor to initialize the utility when Unity loads
        /// </summary>
        static LogDirectoryUtility()
        {
            // Subscribe to play mode state change event
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        // Default implementation of directory operations
        private static readonly IDirectoryOperations _directoryOperations = new SystemDirectoryOperations();
        
        /// <summary>
        /// Handle play mode state changes to create log directories
        /// </summary>
        /// <param name="state">The new play mode state</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Only check when entering play mode
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EnsureLogDirectoriesExist();
            }
        }
        
        /// <summary>
        /// Creates all necessary log directories defined in LogManagerConfig assets
        /// </summary>
        private static void EnsureLogDirectoriesExist()
        {
            // Find all LogManagerConfig assets in the project
            string[] guids = AssetDatabase.FindAssets("t:LogManagerConfig");
            
            // Log configurations with corresponding targets
            Dictionary<LogManagerConfig, List<LogTargetConfig>> configTargets = new Dictionary<LogManagerConfig, List<LogTargetConfig>>();
            
            // First collect all configurations
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                LogManagerConfig config = AssetDatabase.LoadAssetAtPath<LogManagerConfig>(assetPath);
                
                if (config != null)
                {
                    // Collect all targets from the configuration
                    var targets = new List<LogTargetConfig>();
                    
                    // Add all enabled targets
                    foreach (var targetConfig in config.LogTargets)
                    {
                        if (targetConfig != null && targetConfig.Enabled)
                        {
                            targets.Add(targetConfig);
                        }
                    }
                    
                    configTargets[config] = targets;
                }
            }
            
            // Process each configuration and its targets
            foreach (var configPair in configTargets)
            {
                LogManagerConfig config = configPair.Key;
                List<LogTargetConfig> targets = configPair.Value;
                
                // Process all targets that might need directories
                foreach (var target in targets)
                {
                    ProcessLogTargetDirectories(target);
                }
            }
        }
        
        /// <summary>
        /// Process a log target to create any required directories
        /// </summary>
        /// <param name="targetConfig">The log target configuration</param>
        private static void ProcessLogTargetDirectories(LogTargetConfig targetConfig)
        {
            // Here we need to check the specific type of target and extract any path information
            // This depends on the concrete implementations in the project
            
            // Example for a file-based target:
            var filePathProperty = targetConfig.GetType().GetProperty("FilePath");
            if (filePathProperty != null)
            {
                var filePath = filePathProperty.GetValue(targetConfig) as string;
                if (!string.IsNullOrEmpty(filePath))
                {
                    EnsureLogDirectoryExists(filePath);
                }
            }
            
            // Add other target-specific checks as needed
        }
        
        /// <summary>
        /// Creates the directory for a log file if it doesn't exist
        /// </summary>
        /// <param name="filePath">The path to the log file</param>
        private static void EnsureLogDirectoryExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
                
            string fullPath;
            
            // If the path is relative, combine it with the application's persistent data path
            if (!_directoryOperations.IsPathRooted(filePath))
            {
                fullPath = Path.Combine(Application.persistentDataPath, filePath);
            }
            else
            {
                fullPath = filePath;
            }
            
            string directoryPath = _directoryOperations.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directoryPath) && !_directoryOperations.DirectoryExists(directoryPath))
            {
                try
                {
                    _directoryOperations.CreateDirectory(directoryPath);
                    Debug.Log($"Created log directory: {directoryPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create log directory {directoryPath}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Menu item to manually create log directories
        /// </summary>
        [MenuItem("Tools/AhBearStudios/Logging/Create Log Directories")]
        private static void CreateLogDirectories()
        {
            EnsureLogDirectoriesExist();
            EditorUtility.DisplayDialog(
                "Log Directories", 
                "Log directories have been created.", 
                "OK");
        }
        
        /// <summary>
        /// Test method that allows injecting a custom directory operations implementation.
        /// Used for unit testing.
        /// </summary>
        /// <param name="directoryOperations">The directory operations implementation</param>
        /// <param name="filePath">Path to ensure</param>
        internal static void TestEnsureLogDirectoryExists(IDirectoryOperations directoryOperations, string filePath)
        {
            if (directoryOperations == null)
                throw new ArgumentNullException(nameof(directoryOperations));
                
            if (string.IsNullOrEmpty(filePath))
                return;
                
            string fullPath;
            
            if (!directoryOperations.IsPathRooted(filePath))
            {
                fullPath = Path.Combine(Application.persistentDataPath, filePath);
            }
            else
            {
                fullPath = filePath;
            }
            
            string directoryPath = directoryOperations.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directoryPath) && !directoryOperations.DirectoryExists(directoryPath))
            {
                try
                {
                    directoryOperations.CreateDirectory(directoryPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create log directory {directoryPath}: {ex.Message}");
                }
            }
        }
    }
}