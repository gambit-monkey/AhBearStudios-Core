using System;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Config;
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
        // Serialized properties
        private SerializedProperty _configProperty;
        private SerializedProperty _logTargetConfigsProperty;
        
        // Foldout states
        private bool _showGeneralSettings = true;
        private bool _showTargetConfigs = true;
        private bool _showAdvancedSettings = false;
        private bool _showRuntimeInfo = false;
        
        // References for runtime info display
        private LogManagerComponent _logManager;
        private JobLoggerManager _jobLoggerManager;
        
        // Cache for debugging/monitoring info
        private int _lastQueueCount;
        private int _lastTargetCount;
        private float _lastUpdateTime;
        
        // Style cache
        private GUIStyle _headerStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _runtimeInfoStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _buttonStyle;
        
        /// <summary>
        /// Initialize the editor's serialized properties.
        /// </summary>
        private void OnEnable()
        {
            _configProperty = serializedObject.FindProperty("_config");
            _logTargetConfigsProperty = serializedObject.FindProperty("_logTargetConfigs");
            
            _logManager = (LogManagerComponent)target;
            _jobLoggerManager = Application.isPlaying ? _logManager.LoggerManager : null;
            
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            // Force repaint for runtime stats updates
            EditorApplication.update += OnEditorUpdate;
        }
        
        /// <summary>
        /// Clean up when the editor is disabled.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        /// <summary>
        /// Periodically update runtime statistics and repaint the editor.
        /// </summary>
        private void OnEditorUpdate()
        {
            // Only update UI periodically to avoid unnecessary performance impact
            if (Time.realtimeSinceStartup - _lastUpdateTime < 0.5f)
                return;
                
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            if (Application.isPlaying && _logManager != null)
            {
                _jobLoggerManager = _logManager.LoggerManager;
                
                if (_jobLoggerManager != null)
                {
                    if (_lastQueueCount != _jobLoggerManager.QueuedMessageCount || 
                        _lastTargetCount != _jobLoggerManager.TargetCount)
                    {
                        _lastQueueCount = _jobLoggerManager.QueuedMessageCount;
                        _lastTargetCount = _jobLoggerManager.TargetCount;
                        Repaint();
                    }
                }
            }
        }
        
        /// <summary>
        /// Main inspector GUI rendering.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            InitializeStyles();
            
            EditorGUILayout.LabelField("Log Manager Configuration", _headerStyle);
            EditorGUILayout.Space();
            
            DrawConfigurationSection();
            
            EditorGUILayout.Space();
            DrawTargetConfigsSection();
            
            EditorGUILayout.Space();
            DrawAdvancedSection();
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawRuntimeSection();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Initialize GUI styles used by the editor.
        /// </summary>
        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 10, 10)
                };
            }
            
            if (_sectionHeaderStyle == null)
            {
                _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }
            
            if (_runtimeInfoStyle == null)
            {
                _runtimeInfoStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    padding = new RectOffset(5, 5, 5, 5),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }
            
            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.red },
                    wordWrap = true
                };
            }
            
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(10, 10, 5, 5),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
        }
        
        /// <summary>
        /// Draw the main configuration section.
        /// </summary>
        private void DrawConfigurationSection()
        {
            _showGeneralSettings = EditorGUILayout.Foldout(_showGeneralSettings, "General Settings", true);
            
            if (_showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                
                // Config Field
                EditorGUILayout.PropertyField(_configProperty);
                
                // Create config button if none assigned
                if (_configProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("No LogManagerConfig assigned. Default settings will be used.", MessageType.Info);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Create New Config", _buttonStyle))
                    {
                        CreateNewConfig();
                    }
                    
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Show quick settings if config is assigned
                    var config = _configProperty.objectReferenceValue as LogManagerConfig;
                    if (config != null)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.LabelField("Minimum Log Level", LogLevelToString(config.MinimumLevel));
                        EditorGUILayout.Toggle("Auto Flush Enabled", config.EnableAutoFlush);
                        
                        if (config.EnableAutoFlush)
                        {
                            EditorGUILayout.FloatField("Auto Flush Interval", config.AutoFlushInterval);
                        }
                        
                        EditorGUI.EndDisabledGroup();
                        
                        // Edit config button
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button("Edit Config Asset", _buttonStyle))
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
        private void DrawTargetConfigsSection()
        {
            _showTargetConfigs = EditorGUILayout.Foldout(_showTargetConfigs, "Log Target Configurations", true);
            
            if (_showTargetConfigs)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_logTargetConfigsProperty, true);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Add New Target Config", _buttonStyle))
                {
                    AddNewTargetConfigMenu();
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draw the advanced settings section.
        /// </summary>
        private void DrawAdvancedSection()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);
            
            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                // Description
                EditorGUILayout.HelpBox(
                    "The LogManagerComponent handles initialization, updates, and cleanup of the logging system. " +
                    "It is integrated with the Unity lifecycle and manages automatic flushing of logs.", 
                    MessageType.Info);
                
                EditorGUILayout.Space();
                
                // Component lifetime
                EditorGUILayout.LabelField("Component Lifetime", _sectionHeaderStyle);
                
                EditorGUILayout.BeginHorizontal();
                    
                if (GUILayout.Button("Make Don't Destroy On Load", _buttonStyle))
                {
                    SetDontDestroyOnLoad();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Draw the runtime information section (only when playing).
        /// </summary>
        private void DrawRuntimeSection()
        {
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Information", true);
            
            if (_showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                
                if (_jobLoggerManager == null)
                {
                    EditorGUILayout.HelpBox("Logger Manager not initialized or not accessible at runtime.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField("Logger Status:", _sectionHeaderStyle);
                    EditorGUILayout.LabelField($"Queued Messages: {_jobLoggerManager.QueuedMessageCount}", _runtimeInfoStyle);
                    EditorGUILayout.LabelField($"Active Targets: {_jobLoggerManager.TargetCount}", _runtimeInfoStyle);
                    EditorGUILayout.LabelField($"Global Minimum Level: {LogLevelToString(_jobLoggerManager.GlobalMinimumLevel)}", _runtimeInfoStyle);
                    
                    EditorGUILayout.Space();
                    
                    // Runtime actions
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Flush Logs Now", _buttonStyle))
                    {
                        int processed = _logManager.Flush();
                        EditorUtility.DisplayDialog("Flush Result", $"Processed {processed} log messages.", "OK");
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Log message test section
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Send Test Message:", _sectionHeaderStyle);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Debug Log", _buttonStyle))
                    {
                        SendTestMessage(LogLevel.Debug);
                    }
                    
                    if (GUILayout.Button("Info Log", _buttonStyle))
                    {
                        SendTestMessage(LogLevel.Info);
                    }
                    
                    if (GUILayout.Button("Warning", _buttonStyle))
                    {
                        SendTestMessage(LogLevel.Warning);
                    }
                    
                    if (GUILayout.Button("Error", _buttonStyle))
                    {
                        SendTestMessage(LogLevel.Error);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Create and set up a new LogManagerConfig asset.
        /// </summary>
        private void CreateNewConfig()
        {
            // Create save dialog
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Log Manager Configuration",
                "LogManagerConfig",
                "asset",
                "Choose a location to save the log manager configuration."
            );
    
            if (string.IsNullOrEmpty(path))
                return;
        
            // Create the config
            var config = CreateInstance<LogManagerConfig>();
    
            // Save the asset first so we can modify its serialized properties
            AssetDatabase.CreateAsset(config, path);
    
            // Use SerializedObject to modify the private serialized fields
            SerializedObject serializedConfig = new SerializedObject(config);
    
            // Set default values through the serialized properties
            serializedConfig.FindProperty("_minimumLevel").intValue = LogLevel.Info;
            serializedConfig.FindProperty("_enableAutoFlush").boolValue = true;
            serializedConfig.FindProperty("_autoFlushInterval").floatValue = 0.5f;
            serializedConfig.FindProperty("_initialQueueCapacity").intValue = 64;
            serializedConfig.FindProperty("_maxMessagesPerBatch").intValue = 200;
            serializedConfig.FindProperty("_defaultTag").enumValueIndex = (int)Tagging.LogTag.Default;
    
            // Apply the changes
            serializedConfig.ApplyModifiedProperties();
    
            // Save the changes to disk
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
    
            // Assign to our component
            _configProperty.objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();
    
            // Select the new config in the Project window
            EditorGUIUtility.PingObject(config);
        }
        
        /// <summary>
        /// Shows a dropdown menu to add a new target config.
        /// </summary>
        private void AddNewTargetConfigMenu()
        {
            var menu = new GenericMenu();
            
            // Get all LogTargetConfig types - this makes the editor extensible
            var assembly = typeof(LogTargetConfig).Assembly;
            var configTypes = assembly.GetTypes();
            
            foreach (var type in configTypes)
            {
                // Only include concrete types that inherit from LogTargetConfig
                if (type.IsClass && !type.IsAbstract && typeof(LogTargetConfig).IsAssignableFrom(type) && type != typeof(LogTargetConfig))
                {
                    string menuPath = GetMenuPathFromType(type);
                    menu.AddItem(new GUIContent(menuPath), false, () => CreateTargetConfig(type));
                }
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Creates and sets up a new target config asset of the specified type.
        /// </summary>
        /// <param name="configType">The type of target config to create.</param>
        private void CreateTargetConfig(Type configType)
        {
            // Generate a filename based on the type
            string filename = configType.Name;
            if (filename.EndsWith("Config")) filename = filename.Substring(0, filename.Length - 6);
            
            // Create save dialog
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {filename} Configuration",
                filename,
                "asset",
                $"Choose a location to save the {filename} configuration."
            );
            
            if (string.IsNullOrEmpty(path))
                return;
                
            // Create the config instance
            var config = (LogTargetConfig)CreateInstance(configType);
            
            // Save the asset
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Add to the array
            int index = _logTargetConfigsProperty.arraySize;
            _logTargetConfigsProperty.arraySize++;
            _logTargetConfigsProperty.GetArrayElementAtIndex(index).objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();
            
            // Select the new config in the Project window
            EditorGUIUtility.PingObject(config);
        }
        
        /// <summary>
        /// Gets a user-friendly menu path from a type name.
        /// </summary>
        /// <param name="type">The type to convert.</param>
        /// <returns>A formatted menu path.</returns>
        private string GetMenuPathFromType(Type type)
        {
            string name = type.Name;
            
            // Remove "Config" suffix if present
            if (name.EndsWith("Config"))
            {
                name = name.Substring(0, name.Length - 6);
            }
            
            // Add spaces before uppercase letters
            name = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();
            
            return name;
        }
        
        /// <summary>
        /// Sets the LogManagerComponent to DontDestroyOnLoad.
        /// </summary>
        private void SetDontDestroyOnLoad()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Cannot Set DontDestroyOnLoad", 
                    "DontDestroyOnLoad can only be set during play mode.", "OK");
                return;
            }
            
            LogManagerComponent component = (LogManagerComponent)target;
            UnityEngine.Object.DontDestroyOnLoad(component.gameObject);
            EditorUtility.DisplayDialog("DontDestroyOnLoad Set", 
                "This LogManagerComponent will now persist between scenes.", "OK");
        }
        
        /// <summary>
        /// Sends a test message at the specified log level.
        /// </summary>
        /// <param name="level">The log level for the test message.</param>
        private void SendTestMessage(byte level)
        {
            if (_jobLoggerManager == null)
                return;
            
            string levelStr = LogLevelToString(level);
            string message = $"Test {levelStr} message from LogManagerComponentEditor at {DateTime.Now}";
            
            switch (level)
            {
                case LogLevel.Debug:
                    _jobLoggerManager.Debug(message);
                    break;
                case LogLevel.Info:
                    _jobLoggerManager.Info(message);
                    break;
                case LogLevel.Warning:
                    _jobLoggerManager.Warning(message);
                    break;
                case LogLevel.Error:
                    _jobLoggerManager.Error(message);
                    break;
                case LogLevel.Critical:
                    _jobLoggerManager.Critical(message);
                    break;
            }
        }
        
        /// <summary>
        /// Converts a LogLevel byte value to a readable string.
        /// </summary>
        /// <param name="level">The log level value.</param>
        /// <returns>A string representation of the log level.</returns>
        private string LogLevelToString(byte level)
        {
            if (level == LogLevel.Debug) return "Debug";
            if (level == LogLevel.Info) return "Info";
            if (level == LogLevel.Warning) return "Warning";
            if (level == LogLevel.Error) return "Error";
            if (level == LogLevel.Critical) return "Critical";
            return $"Unknown ({level})";
        }
    }
}