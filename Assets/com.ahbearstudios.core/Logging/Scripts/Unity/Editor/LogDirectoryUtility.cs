using UnityEngine;
using UnityEditor;
using System.IO;
using AhBearStudios.Core.Logging.Config;

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
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                LogManagerConfig config = AssetDatabase.LoadAssetAtPath<LogManagerConfig>(assetPath);
                
                // if (config != null && config.EnableFileLogger)
                // {
                //     EnsureLogDirectoryExists(config.FilePath);
                // }
            }
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
            if (!Path.IsPathRooted(filePath))
            {
                fullPath = Path.Combine(Application.persistentDataPath, filePath);
            }
            else
            {
                fullPath = filePath;
            }
            
            string directoryPath = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log($"Created log directory: {directoryPath}");
                }
                catch (System.Exception ex)
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
    }
}