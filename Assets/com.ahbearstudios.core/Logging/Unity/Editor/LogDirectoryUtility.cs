using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Utilities;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Utility to create log directories for LogManagerConfig.
    /// This ensures log directories exist before the app starts logging.
    /// </summary>
    [InitializeOnLoad]
    public static class LogDirectoryUtility
    {
        #region Private Fields
        
        /// <summary>
        /// Directory operations implementation.
        /// </summary>
        private static IDirectoryOperations _directoryOperations = new SystemDirectoryOperations();
        
        /// <summary>
        /// Cache of known file path property names for different log target types.
        /// </summary>
        private static readonly Dictionary<Type, string[]> _knownFilePathProperties = new Dictionary<Type, string[]>
        {
            // Add known property names for different log target types
            // These can be extended as new target types are added
            { typeof(LogTargetConfig), new[] { "FilePath", "OutputPath", "LogPath", "FileName", "OutputFile" } }
        };
        
        /// <summary>
        /// Cache of reflected property info to avoid repeated reflection calls.
        /// </summary>
        private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();
        
        #endregion
        
        #region Static Constructor
        
        /// <summary>
        /// Static constructor to initialize the utility when Unity loads.
        /// </summary>
        static LogDirectoryUtility()
        {
            // Subscribe to play mode state change event
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Sets a custom directory operations implementation for testing.
        /// </summary>
        /// <param name="directoryOperations">The directory operations implementation to use.</param>
        public static void SetDirectoryOperations(IDirectoryOperations directoryOperations)
        {
            _directoryOperations = directoryOperations ?? throw new ArgumentNullException(nameof(directoryOperations));
        }
        
        /// <summary>
        /// Registers known file path property names for a specific log target type.
        /// </summary>
        /// <param name="targetType">The log target type.</param>
        /// <param name="propertyNames">Array of property names that contain file paths.</param>
        public static void RegisterFilePathProperties(Type targetType, params string[] propertyNames)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            
            if (propertyNames == null || propertyNames.Length == 0)
                return;
            
            _knownFilePathProperties[targetType] = propertyNames;
            
            // Clear cached properties for this type to force re-reflection
            _propertyCache.Remove(targetType);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Handle play mode state changes to create log directories.
        /// </summary>
        /// <param name="state">The new play mode state.</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Only check when entering play mode
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                try
                {
                    EnsureLogDirectoriesExist();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LogDirectoryUtility: Failed to ensure log directories exist: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Creates all necessary log directories defined in LogManagerConfig assets.
        /// </summary>
        private static void EnsureLogDirectoriesExist()
        {
            var createdDirectories = new HashSet<string>();
            
            // Find all LogManagerConfig assets in the project
            string[] guids = AssetDatabase.FindAssets("t:LogManagerConfig");
            
            if (guids.Length == 0)
            {
                Debug.Log("LogDirectoryUtility: No LogManagerConfig assets found in project.");
                return;
            }
            
            foreach (string guid in guids)
            {
                try
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    LogManagerConfig config = AssetDatabase.LoadAssetAtPath<LogManagerConfig>(assetPath);
                    
                    if (config != null)
                    {
                        ProcessLogManagerConfig(config, createdDirectories);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"LogDirectoryUtility: Failed to process LogManagerConfig asset {guid}: {ex.Message}");
                }
            }
            
            if (createdDirectories.Count > 0)
            {
                Debug.Log($"LogDirectoryUtility: Created {createdDirectories.Count} log directories: {string.Join(", ", createdDirectories)}");
            }
        }
        
        /// <summary>
        /// Processes a LogManagerConfig to create required directories.
        /// </summary>
        /// <param name="config">The log manager configuration.</param>
        /// <param name="createdDirectories">Set to track created directories and avoid duplicates.</param>
        private static void ProcessLogManagerConfig(LogManagerConfig config, HashSet<string> createdDirectories)
        {
            if (config.LogTargets == null)
                return;
            
            foreach (var targetConfig in config.LogTargets)
            {
                if (targetConfig != null && targetConfig.Enabled)
                {
                    try
                    {
                        ProcessLogTargetDirectories(targetConfig, createdDirectories);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"LogDirectoryUtility: Failed to process log target '{targetConfig.TargetName}': {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Process a log target to create any required directories.
        /// </summary>
        /// <param name="targetConfig">The log target configuration.</param>
        /// <param name="createdDirectories">Set to track created directories and avoid duplicates.</param>
        private static void ProcessLogTargetDirectories(LogTargetConfig targetConfig, HashSet<string> createdDirectories)
        {
            // Method 1: Check if target implements ILogTargetDirectoryProvider
            if (targetConfig is ILogTargetDirectoryProvider directoryProvider && directoryProvider.RequiresDirectories)
            {
                foreach (string directoryPath in directoryProvider.GetRequiredDirectories())
                {
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        EnsureLogDirectoryExists(directoryPath, createdDirectories);
                    }
                }
                return;
            }
            
            // Method 2: Use reflection to find file path properties
            var filePaths = ExtractFilePathsFromTarget(targetConfig);
            foreach (string filePath in filePaths)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    EnsureLogDirectoryExists(filePath, createdDirectories);
                }
            }
        }
        
        /// <summary>
        /// Extracts file paths from a log target using reflection.
        /// </summary>
        /// <param name="targetConfig">The log target configuration.</param>
        /// <returns>Collection of file paths found in the target.</returns>
        private static IEnumerable<string> ExtractFilePathsFromTarget(LogTargetConfig targetConfig)
        {
            var filePaths = new List<string>();
            var targetType = targetConfig.GetType();
            
            try
            {
                // Get cached properties or reflect them
                if (!_propertyCache.TryGetValue(targetType, out var properties))
                {
                    properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    _propertyCache[targetType] = properties;
                }
                
                // Get known property names for this type or its base types
                var knownPropertyNames = GetKnownPropertyNames(targetType);
                
                foreach (var property in properties)
                {
                    // Skip if property is not a string
                    if (property.PropertyType != typeof(string))
                        continue;
                    
                    // Skip if property doesn't have a getter
                    if (!property.CanRead)
                        continue;
                    
                    // Check if this property name is in our known list
                    bool isKnownPathProperty = knownPropertyNames.Contains(property.Name);
                    
                    // Also check if property name suggests it contains a path
                    bool seemsLikePathProperty = IsPathLikePropertyName(property.Name);
                    
                    if (isKnownPathProperty || seemsLikePathProperty)
                    {
                        try
                        {
                            var value = property.GetValue(targetConfig) as string;
                            if (!string.IsNullOrEmpty(value))
                            {
                                filePaths.Add(value);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"LogDirectoryUtility: Failed to get value from property '{property.Name}' on {targetType.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LogDirectoryUtility: Failed to extract file paths from {targetType.Name}: {ex.Message}");
            }
            
            return filePaths;
        }
        
        /// <summary>
        /// Gets known property names for a type, including base types.
        /// </summary>
        /// <param name="targetType">The type to get property names for.</param>
        /// <returns>Collection of known property names.</returns>
        private static IEnumerable<string> GetKnownPropertyNames(Type targetType)
        {
            var propertyNames = new HashSet<string>();
            
            // Check the type and its base types
            var currentType = targetType;
            while (currentType != null && currentType != typeof(object))
            {
                if (_knownFilePathProperties.TryGetValue(currentType, out var names))
                {
                    foreach (var name in names)
                    {
                        propertyNames.Add(name);
                    }
                }
                currentType = currentType.BaseType;
            }
            
            return propertyNames;
        }
        
        /// <summary>
        /// Determines if a property name suggests it contains a file path.
        /// </summary>
        /// <param name="propertyName">The property name to check.</param>
        /// <returns>True if the property name suggests it contains a file path.</returns>
        private static bool IsPathLikePropertyName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return false;
            
            var lowerName = propertyName.ToLowerInvariant();
            
            // Common patterns for path-related properties
            return lowerName.Contains("path") ||
                   lowerName.Contains("file") ||
                   lowerName.Contains("directory") ||
                   lowerName.Contains("folder") ||
                   lowerName.Contains("output") ||
                   lowerName.Contains("destination") ||
                   lowerName.EndsWith("dir") ||
                   lowerName.EndsWith("location");
        }
        
        /// <summary>
        /// Creates the directory for a log file if it doesn't exist.
        /// </summary>
        /// <param name="filePath">The path to the log file.</param>
        /// <param name="createdDirectories">Set to track created directories and avoid duplicates.</param>
        private static void EnsureLogDirectoryExists(string filePath, HashSet<string> createdDirectories)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            
            try
            {
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
                
                if (!string.IsNullOrEmpty(directoryPath) && 
                    !_directoryOperations.DirectoryExists(directoryPath) &&
                    !createdDirectories.Contains(directoryPath))
                {
                    _directoryOperations.CreateDirectory(directoryPath);
                    createdDirectories.Add(directoryPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LogDirectoryUtility: Failed to create directory for path '{filePath}': {ex.Message}");
            }
        }
        
        #endregion
        
        #region Menu Items
        
        /// <summary>
        /// Menu item to manually create log directories.
        /// </summary>
        [MenuItem("Tools/AhBearStudios/Logging/Create Log Directories")]
        private static void CreateLogDirectoriesMenuItem()
        {
            try
            {
                EnsureLogDirectoriesExist();
                EditorUtility.DisplayDialog(
                    "Log Directories", 
                    "Log directories have been created successfully.", 
                    "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Error", 
                    $"Failed to create log directories: {ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Menu item to clear the property cache.
        /// </summary>
        [MenuItem("Tools/AhBearStudios/Logging/Clear Property Cache")]
        private static void ClearPropertyCacheMenuItem()
        {
            _propertyCache.Clear();
            Debug.Log("LogDirectoryUtility: Property cache cleared.");
        }
        
        /// <summary>
        /// Menu item to show debug information about detected log targets.
        /// </summary>
        [MenuItem("Tools/AhBearStudios/Logging/Debug Log Target Detection")]
        private static void DebugLogTargetDetectionMenuItem()
        {
            try
            {
                var detectedPaths = new List<string>();
                string[] guids = AssetDatabase.FindAssets("t:LogManagerConfig");
                
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    LogManagerConfig config = AssetDatabase.LoadAssetAtPath<LogManagerConfig>(assetPath);
                    
                    if (config?.LogTargets != null)
                    {
                        foreach (var targetConfig in config.LogTargets)
                        {
                            if (targetConfig != null && targetConfig.Enabled)
                            {
                                var paths = ExtractFilePathsFromTarget(targetConfig);
                                detectedPaths.AddRange(paths);
                                
                                Debug.Log($"Target '{targetConfig.TargetName}' ({targetConfig.GetType().Name}): " +
                                         $"Detected paths: [{string.Join(", ", paths)}]");
                            }
                        }
                    }
                }
                
                Debug.Log($"Total detected paths: {detectedPaths.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to debug log target detection: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Internal Test Methods
        
        /// <summary>
        /// Test method that allows injecting a custom directory operations implementation.
        /// Used for unit testing.
        /// </summary>
        /// <param name="directoryOperations">The directory operations implementation.</param>
        /// <param name="filePath">Path to ensure.</param>
        internal static void TestEnsureLogDirectoryExists(IDirectoryOperations directoryOperations, string filePath)
        {
            if (directoryOperations == null)
                throw new ArgumentNullException(nameof(directoryOperations));
            
            var originalOperations = _directoryOperations;
            try
            {
                _directoryOperations = directoryOperations;
                var createdDirectories = new HashSet<string>();
                EnsureLogDirectoryExists(filePath, createdDirectories);
            }
            finally
            {
                _directoryOperations = originalOperations;
            }
        }
        
        /// <summary>
        /// Test method to extract file paths from a target configuration.
        /// Used for unit testing.
        /// </summary>
        /// <param name="targetConfig">The target configuration to test.</param>
        /// <returns>Collection of extracted file paths.</returns>
        internal static IEnumerable<string> TestExtractFilePathsFromTarget(LogTargetConfig targetConfig)
        {
            return ExtractFilePathsFromTarget(targetConfig);
        }
        
        #endregion
    }
}