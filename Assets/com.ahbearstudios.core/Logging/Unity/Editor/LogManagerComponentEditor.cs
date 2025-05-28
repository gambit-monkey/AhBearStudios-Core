using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom Unity Editor for the LogManagerComponent.
    /// Provides a rich interface for configuring and managing log settings.
    /// </summary>
    [CustomEditor(typeof(LogManagerComponent))]
    public class LogManagerComponentEditor : UnityEditor.Editor
    {
        #region Private Fields
        
        // Serialized properties
        private SerializedProperty _configProperty;
        private SerializedProperty _logTargetConfigsProperty;
        
        // Foldout states
        private bool _showGeneralSettings = true;
        private bool _showTargetConfigs = true;
        private bool _showAdvancedSettings = false;
        private bool _showRuntimeInfo = false;
        
        // Target references
        private LogManagerComponent _logManagerComponent;
        
        // Runtime monitoring
        private int _lastQueueCount;
        private int _lastTargetCount;
        private LogLevel _lastMinimumLevel;
        private float _lastUpdateTime;
        
        // UI Styles
        private CustomEditorStyles _styles;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the editor when enabled.
        /// </summary>
        private void OnEnable()
        {
            _configProperty = serializedObject.FindProperty("_config");
            _logTargetConfigsProperty = serializedObject.FindProperty("_logTargetConfigs");
            
            _logManagerComponent = target as LogManagerComponent;
            
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            // Initialize styles
            _styles = new CustomEditorStyles();
            
            // Subscribe to editor update for runtime monitoring
            EditorApplication.update += OnEditorUpdate;
        }
        
        /// <summary>
        /// Clean up when the editor is disabled.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        #endregion
        
        #region GUI Rendering
        
        /// <summary>
        /// Main inspector GUI rendering.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Ensure styles are initialized
            _styles.EnsureInitialized();
            
            // Header
            EditorGUILayout.LabelField("Log Manager Configuration", _styles.HeaderStyle);
            EditorGUILayout.Space();
            
            // Main sections
            DrawGeneralSettings();
            
            EditorGUILayout.Space();
            DrawTargetConfigs();
            
            EditorGUILayout.Space();
            DrawAdvancedSettings();
            
            // Runtime section (only in play mode)
            if (Application.isPlaying && _logManagerComponent != null)
            {
                EditorGUILayout.Space();
                DrawRuntimeInfo();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Draw the general settings section.
        /// </summary>
        private void DrawGeneralSettings()
        {
            _showGeneralSettings = EditorGUILayout.Foldout(_showGeneralSettings, "General Settings", true);
            
            if (_showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                
                // Config field
                EditorGUILayout.PropertyField(_configProperty);
                
                if (_configProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("No LogManagerConfig assigned. Default settings will be used.", MessageType.Info);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Create New Config", _styles.ButtonStyle))
                    {
                        CreateNewConfig();
                    }
                    
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Show read-only configuration preview
                    var config = _configProperty.objectReferenceValue as LogManagerConfig;
                    if (config != null)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        
                        EditorGUILayout.EnumPopup("Minimum Log Level", config.MinimumLevel);
                        EditorGUILayout.IntField("Max Messages Per Batch", config.MaxMessagesPerBatch);
                        EditorGUILayout.IntField("Initial Queue Capacity", config.InitialQueueCapacity);
                        EditorGUILayout.Toggle("Auto Flush Enabled", config.EnableAutoFlush);
                        
                        if (config.EnableAutoFlush)
                        {
                            EditorGUILayout.FloatField("Auto Flush Interval", config.AutoFlushInterval);
                        }
                        
                        EditorGUILayout.EnumPopup("Default Tag", config.DefaultTag);
                        
                        EditorGUI.EndDisabledGroup();
                        
                        // Edit config button
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button("Edit Config Asset", _styles.ButtonStyle))
                        {
                            Selection.activeObject = config;
                        }
                        
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draw the target configurations section.
        /// </summary>
        private void DrawTargetConfigs()
        {
            _showTargetConfigs = EditorGUILayout.Foldout(_showTargetConfigs, "Log Target Configurations", true);
            
            if (_showTargetConfigs)
            {
                EditorGUI.indentLevel++;
                
                // Show array of target configs
                EditorGUILayout.PropertyField(_logTargetConfigsProperty, true);
                
                // Add button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Add Target Config", _styles.ButtonStyle))
                {
                    ShowAddTargetMenu();
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draw the advanced settings section.
        /// </summary>
        private void DrawAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);
            
            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox(
                    "LogManagerComponent initializes the logging system when the scene starts. " +
                    "It manages the lifecycle of the JobLoggerManager and handles automatic flushing of logs.", 
                    MessageType.Info);
                
                EditorGUILayout.Space();
                
                // Don't Destroy On Load option
                EditorGUILayout.LabelField("Component Lifetime", _styles.SectionHeaderStyle);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Make Don't Destroy On Load", _styles.ButtonStyle))
                {
                    SetDontDestroyOnLoad();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draw the runtime information section.
        /// </summary>
        private void DrawRuntimeInfo()
        {
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Information", true);
            
            if (_showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                
                JobLoggerManager jobLoggerManager = _logManagerComponent.LoggerManager;
                
                if (jobLoggerManager == null)
                {
                    EditorGUILayout.HelpBox("Logger Manager is not initialized.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                    
                    // Status information
                    EditorGUILayout.LabelField("Logger Status:", _styles.SectionHeaderStyle);
                    EditorGUILayout.LabelField($"Queued Messages: {jobLoggerManager.QueuedMessageCount}", _styles.RuntimeInfoStyle);
                    EditorGUILayout.LabelField($"Active Targets: {jobLoggerManager.TargetCount}", _styles.RuntimeInfoStyle);
                    EditorGUILayout.LabelField($"Global Minimum Level: {jobLoggerManager.GlobalMinimumLevel}", _styles.RuntimeInfoStyle);
                    
                    EditorGUILayout.Space();
                    
                    // Runtime actions
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Flush Logs Now", _styles.ButtonStyle))
                    {
                        int processed = jobLoggerManager.Flush();
                        EditorUtility.DisplayDialog("Flush Result", $"Processed {processed} log messages.", "OK");
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Test messages section
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Send Test Message:", _styles.SectionHeaderStyle);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Debug", _styles.ButtonStyle))
                    {
                        SendTestMessage(jobLoggerManager, LogLevel.Debug);
                    }
                    
                    if (GUILayout.Button("Info", _styles.ButtonStyle))
                    {
                        SendTestMessage(jobLoggerManager, LogLevel.Info);
                    }
                    
                    if (GUILayout.Button("Warning", _styles.ButtonStyle))
                    {
                        SendTestMessage(jobLoggerManager, LogLevel.Warning);
                    }
                    
                    if (GUILayout.Button("Error", _styles.ButtonStyle))
                    {
                        SendTestMessage(jobLoggerManager, LogLevel.Error);
                    }
                    
                    if (GUILayout.Button("Critical", _styles.ButtonStyle))
                    {
                        SendTestMessage(jobLoggerManager, LogLevel.Critical);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Updates the editor UI periodically during play mode to show latest statistics.
        /// </summary>
        private void OnEditorUpdate()
        {
            // Only update periodically to avoid performance impact
            if (Time.realtimeSinceStartup - _lastUpdateTime < 0.5f)
                return;
                
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            if (Application.isPlaying && _logManagerComponent != null)
            {
                JobLoggerManager jobLoggerManager = _logManagerComponent.LoggerManager;
                
                if (jobLoggerManager != null)
                {
                    int queueCount = jobLoggerManager.QueuedMessageCount;
                    int targetCount = jobLoggerManager.TargetCount;
                    LogLevel minimumLevel = jobLoggerManager.GlobalMinimumLevel;
                    
                    // Only repaint if something changed
                    if (_lastQueueCount != queueCount || 
                        _lastTargetCount != targetCount || 
                        _lastMinimumLevel != minimumLevel)
                    {
                        _lastQueueCount = queueCount;
                        _lastTargetCount = targetCount;
                        _lastMinimumLevel = minimumLevel;
                        Repaint();
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a new LogManagerConfig asset.
        /// </summary>
        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Log Manager Configuration",
                "LogManagerConfig",
                "asset",
                "Choose a location to save the log manager configuration."
            );
            
            if (string.IsNullOrEmpty(path))
                return;
            
            // Create and configure the asset
            LogManagerConfig config = CreateInstance<LogManagerConfig>();
            
            // Save the asset
            AssetDatabase.CreateAsset(config, path);
            
            // Configure default properties via SerializedObject
            SerializedObject configSO = new SerializedObject(config);
            configSO.FindProperty("_minimumLevel").enumValueIndex = (int)LogLevel.Info;
            configSO.FindProperty("_maxMessagesPerBatch").intValue = 200;
            configSO.FindProperty("_initialQueueCapacity").intValue = 64;
            configSO.FindProperty("_enableAutoFlush").boolValue = true;
            configSO.FindProperty("_autoFlushInterval").floatValue = 0.5f;
            configSO.FindProperty("_defaultTag").enumValueIndex = (int)Tagging.LogTag.Default;
            configSO.ApplyModifiedProperties();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Assign to our component
            _configProperty.objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();
            
            // Select in Project window
            EditorGUIUtility.PingObject(config);
        }
        
        /// <summary>
        /// Shows a context menu to add a new target config.
        /// </summary>
        private void ShowAddTargetMenu()
        {
            var menu = new GenericMenu();
            
            // Find all concrete target config types
            IEnumerable<Type> targetConfigTypes = GetConcreteLogTargetConfigTypes();
            
            foreach (Type configType in targetConfigTypes)
            {
                string menuName = FormatTypeNameForMenu(configType);
                menu.AddItem(new GUIContent(menuName), false, () => CreateLogTargetConfig(configType));
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Gets all concrete types that inherit from LogTargetConfig.
        /// </summary>
        /// <returns>A collection of LogTargetConfig types.</returns>
        private IEnumerable<Type> GetConcreteLogTargetConfigTypes()
        {
            var result = new List<Type>();
            var assembly = typeof(LogTargetConfig).Assembly;
            
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(LogTargetConfig).IsAssignableFrom(type) && 
                    !type.IsAbstract && 
                    type != typeof(LogTargetConfig))
                {
                    result.Add(type);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a user-friendly menu name from a type.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <returns>A formatted menu name.</returns>
        private string FormatTypeNameForMenu(Type type)
        {
            string name = type.Name;
            
            // Remove "Config" suffix
            if (name.EndsWith("Config"))
            {
                name = name.Substring(0, name.Length - 6);
            }
            
            // Add spaces before capital letters
            name = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();
            
            return name;
        }
        
        /// <summary>
        /// Creates a new log target config asset.
        /// </summary>
        /// <param name="configType">The type of config to create.</param>
        private void CreateLogTargetConfig(Type configType)
        {
            // Generate filename
            string typeName = configType.Name;
            if (typeName.EndsWith("Config"))
            {
                typeName = typeName.Substring(0, typeName.Length - 6);
            }
            
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {typeName} Configuration",
                typeName,
                "asset",
                $"Choose a location to save the {typeName} configuration."
            );
            
            if (string.IsNullOrEmpty(path))
                return;
            
            // Create and save asset
            LogTargetConfig config = CreateInstance(configType) as LogTargetConfig;
            if (config != null)
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Add to the array
                int index = _logTargetConfigsProperty.arraySize;
                _logTargetConfigsProperty.arraySize++;
                _logTargetConfigsProperty.GetArrayElementAtIndex(index).objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                
                // Ping in Project window
                EditorGUIUtility.PingObject(config);
            }
        }
        
        /// <summary>
        /// Sets the target component to DontDestroyOnLoad.
        /// </summary>
        private void SetDontDestroyOnLoad()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Cannot Set DontDestroyOnLoad",
                    "DontDestroyOnLoad can only be set during play mode.",
                    "OK"
                );
                return;
            }
            
            if (_logManagerComponent != null)
            {
                UnityEngine.Object.DontDestroyOnLoad(_logManagerComponent.gameObject);
                EditorUtility.DisplayDialog(
                    "DontDestroyOnLoad Set",
                    "The LogManagerComponent will now persist between scene loads.",
                    "OK"
                );
            }
        }
        
        /// <summary>
        /// Sends a test message through the logger.
        /// </summary>
        /// <param name="manager">The logger manager instance.</param>
        /// <param name="level">The log level to use.</param>
        private void SendTestMessage(JobLoggerManager manager, LogLevel level)
        {
            if (manager == null)
                return;
                
            string message = $"Test message from LogManagerComponentEditor at {DateTime.Now}";
            
            switch (level)
            {
                case LogLevel.Trace:
                    // Trace is typically not exposed directly
                    manager.Log(LogLevel.Trace, Tagging.LogTag.Trace, message);
                    break;
                case LogLevel.Debug:
                    manager.Debug(message, Tagging.LogTag.Debug);
                    break;
                case LogLevel.Info:
                    manager.Info(message, Tagging.LogTag.Info);
                    break;
                case LogLevel.Warning:
                    manager.Warning(message, Tagging.LogTag.Warning);
                    break;
                case LogLevel.Error:
                    manager.Error(message, Tagging.LogTag.Error);
                    break;
                case LogLevel.Critical:
                    manager.Critical(message, Tagging.LogTag.Critical);
                    break;
            }
        }
        
        #endregion
        
        #region Nested Types
        
        /// <summary>
        /// Container for editor styles to avoid GC allocations.
        /// </summary>
        private sealed class CustomEditorStyles
        {
            // Styles
            public GUIStyle HeaderStyle { get; private set; }
            public GUIStyle SectionHeaderStyle { get; private set; }
            public GUIStyle RuntimeInfoStyle { get; private set; }
            public GUIStyle ErrorStyle { get; private set; }
            public GUIStyle ButtonStyle { get; private set; }
            
            // Initialization state
            private bool _initialized;
            
            /// <summary>
            /// Ensures styles are initialized.
            /// </summary>
            public void EnsureInitialized()
            {
                if (_initialized)
                    return;
                
                InitializeStyles();
                _initialized = true;
            }
            
            /// <summary>
            /// Initialize all styles.
            /// </summary>
            private void InitializeStyles()
            {
                HeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 10, 10)
                };
                
                SectionHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 5, 5)
                };
                
                RuntimeInfoStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    padding = new RectOffset(5, 5, 5, 5),
                    margin = new RectOffset(5, 5, 5, 5)
                };
                
                ErrorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.red },
                    wordWrap = true
                };
                
                ButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(10, 10, 5, 5),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
        }
        
        #endregion
    }
}