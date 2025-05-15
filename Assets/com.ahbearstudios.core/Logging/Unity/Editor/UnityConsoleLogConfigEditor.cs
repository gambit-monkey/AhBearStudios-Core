using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Config;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Tags;
using AhBearStudios.Core.Logging.Formatters;
using System;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor for Unity Console Log Configuration.
    /// </summary>
    [CustomEditor(typeof(UnityConsoleLogConfig))]
    public class UnityConsoleLogConfigEditor : UnityEditor.Editor
    {
        // Configuration object
        private UnityConsoleLogConfig _config;
        
        // Serialized properties - use field names, not property names
        private SerializedProperty _enabledProp;
        private SerializedProperty _targetNameProp;
        private SerializedProperty _minimumLevelProp;
        private SerializedProperty _includeTagsProp;
        private SerializedProperty _excludeTagsProp;
        private SerializedProperty _formatterProp;
        private SerializedProperty _useColorizedOutputProp;
        
        // Foldout states
        private bool _generalSettingsFoldout = true;
        private bool _formatterSettingsFoldout = true;
        private bool _tagFiltersFoldout = false;
        private bool _debugSettingsFoldout = false;
        
        /// <summary>
        /// Initialize when the editor is enabled
        /// </summary>
        private void OnEnable()
        {
            _config = (UnityConsoleLogConfig)target;
            
            // Important: Use field names, not property names
            _enabledProp = serializedObject.FindProperty("Enabled");
            _targetNameProp = serializedObject.FindProperty("TargetName");
            _minimumLevelProp = serializedObject.FindProperty("MinimumLevel");
            _includeTagsProp = serializedObject.FindProperty("IncludeTags");
            _excludeTagsProp = serializedObject.FindProperty("ExcludeTags");
            _formatterProp = serializedObject.FindProperty("_formatter");
            _useColorizedOutputProp = serializedObject.FindProperty("_useColorizedOutput");
            
            // Validate all properties were found
            ValidateSerializedProperties();
        }

        /// <summary>
        /// Validates that all serialized properties were found
        /// </summary>
        private void ValidateSerializedProperties()
        {
            // Check if any properties weren't found and log warnings
            if (_enabledProp == null)
                Debug.LogWarning("Property 'Enabled' not found in UnityConsoleLogConfig");
            if (_targetNameProp == null)
                Debug.LogWarning("Property 'TargetName' not found in UnityConsoleLogConfig");
            if (_minimumLevelProp == null)
                Debug.LogWarning("Property 'MinimumLevel' not found in UnityConsoleLogConfig");
            if (_includeTagsProp == null)
                Debug.LogWarning("Property 'IncludeTags' not found in UnityConsoleLogConfig");
            if (_excludeTagsProp == null)
                Debug.LogWarning("Property 'ExcludeTags' not found in UnityConsoleLogConfig");
            if (_formatterProp == null)
                Debug.LogWarning("Property '_formatter' not found in UnityConsoleLogConfig");
            if (_useColorizedOutputProp == null)
                Debug.LogWarning("Property '_useColorizedOutput' not found in UnityConsoleLogConfig");
        }
        
        /// <summary>
        /// Draw the custom inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            
            EditorGUILayout.Space();
            
            // Only draw sections if required properties exist
            if (_enabledProp != null && _targetNameProp != null && _minimumLevelProp != null)
                DrawGeneralSettings();
            
            EditorGUILayout.Space();
            
            if (_formatterProp != null && _useColorizedOutputProp != null)
                DrawFormatterSettings();
            
            EditorGUILayout.Space();
            
            if (_includeTagsProp != null && _excludeTagsProp != null)
                DrawTagFilters();
            
            EditorGUILayout.Space();
            
            DrawDebugSettings();
            
            EditorGUILayout.Space();
            
            DrawTestButtons();
            
            // Apply modifications
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Draw the header section
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            
            EditorGUILayout.LabelField("Unity Console Log Configuration", headerStyle);
            
            GUIStyle descStyle = new GUIStyle(EditorStyles.label);
            descStyle.wordWrap = true;
            descStyle.alignment = TextAnchor.MiddleCenter;
            
            EditorGUILayout.LabelField(
                "Configure how logs are displayed in the Unity Console.",
                descStyle);
                
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw general settings section
        /// </summary>
        private void DrawGeneralSettings()
        {
            _generalSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_generalSettingsFoldout, "General Settings");
            
            if (_generalSettingsFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Safe access to properties
                if (_enabledProp != null)
                {
                    EditorGUILayout.PropertyField(_enabledProp, new GUIContent("Enabled", "Enable/disable this log target"));
                }
                
                if (_targetNameProp != null)
                {
                    EditorGUILayout.PropertyField(_targetNameProp, new GUIContent("Target Name", "The name of this log target"));
                }
                
                if (_minimumLevelProp != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Minimum Level", "The minimum log level to capture"), GUILayout.Width(120));
                    
                    int currentLevel = _minimumLevelProp.intValue;
                    EditorGUI.BeginChangeCheck();
                    int newLevel = EditorGUILayout.IntSlider(currentLevel, 0, 4);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _minimumLevelProp.intValue = newLevel;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Draw level indicator
                    DrawLogLevelIndicator(currentLevel);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw formatter settings section
        /// </summary>
        private void DrawFormatterSettings()
        {
            _formatterSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_formatterSettingsFoldout, "Formatter Settings");
            
            if (_formatterSettingsFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Safe access to properties
                if (_useColorizedOutputProp != null)
                {
                    EditorGUILayout.PropertyField(_useColorizedOutputProp, 
                        new GUIContent("Use Colorized Output", "Enable colorized formatting in the Unity Console"));
                    
                    if (_useColorizedOutputProp.boolValue && _formatterProp != null)
                    {
                        EditorGUILayout.Space(5);
                        
                        // Formatter object field
                        EditorGUILayout.PropertyField(_formatterProp, 
                            new GUIContent("Formatter", "The formatter to use for console logs"));
                        
                        // If no formatter is assigned, show a message and create button
                        if (_formatterProp.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox(
                                "No formatter assigned. Click the button below to create and assign a new Colorized Console Formatter.",
                                MessageType.Info);
                                
                            if (GUILayout.Button("Create and Assign Formatter"))
                            {
                                CreateAndAssignColorizedFormatter();
                            }
                        }
                        else
                        {
                            try
                            {
                                // Show formatter editor button
                                ColorizedConsoleFormatter formatter = (ColorizedConsoleFormatter)_formatterProp.objectReferenceValue;
                                
                                EditorGUILayout.Space(5);
                                
                                if (GUILayout.Button("Edit Formatter Colors"))
                                {
                                    // Open the color editor window
                                    ColorizedFormatterColorEditor.OpenWindow(formatter);
                                }
                                
                                // Show formatter preview
                                EditorGUILayout.Space(5);
                                EditorGUILayout.LabelField("Formatter Preview:", EditorStyles.boldLabel);
                                
                                // Only show preview if formatter is valid
                                if (formatter != null)
                                {
                                    // Create a sample message for preview
                                    var previewMessage = new LogMessage
                                    {
                                        Level = LogLevel.Info,
                                        Tag = Tagging.LogTag.System,
                                        TimestampTicks = DateTime.Now.Ticks,
                                        Message = "Sample log message preview"
                                    };
                                    
                                    // Format and display
                                    string formattedMessage = formatter.Format(previewMessage).ToString();
                                    
                                    // Create style that supports rich text
                                    GUIStyle richTextStyle = new GUIStyle(EditorStyles.textField);
                                    richTextStyle.richText = true;
                                    richTextStyle.wordWrap = true;
                                    
                                    EditorGUILayout.SelectableLabel(formattedMessage, richTextStyle, GUILayout.Height(40));
                                }
                            }
                            catch (Exception ex)
                            {
                                EditorGUILayout.HelpBox(
                                    $"Error displaying formatter: {ex.Message}",
                                    MessageType.Error);
                            }
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw log level indicator with colored labels
        /// </summary>
        /// <param name="currentLevel">Current log level</param>
        private void DrawLogLevelIndicator(int currentLevel)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUIStyle levelStyle = new GUIStyle(EditorStyles.miniLabel);
            levelStyle.fontStyle = FontStyle.Bold;
            
            // Debug level
            levelStyle.normal.textColor = currentLevel <= 0 ? Color.white : Color.gray;
            EditorGUILayout.LabelField("Debug", levelStyle, GUILayout.Width(60));
            
            // Info level
            levelStyle.normal.textColor = currentLevel <= 1 ? Color.cyan : Color.gray;
            EditorGUILayout.LabelField("Info", levelStyle, GUILayout.Width(60));
            
            // Warning level
            levelStyle.normal.textColor = currentLevel <= 2 ? Color.yellow : Color.gray;
            EditorGUILayout.LabelField("Warning", levelStyle, GUILayout.Width(60));
            
            // Error level
            levelStyle.normal.textColor = currentLevel <= 3 ? new Color(1, 0.5f, 0) : Color.gray;
            EditorGUILayout.LabelField("Error", levelStyle, GUILayout.Width(60));
            
            // Critical level
            levelStyle.normal.textColor = currentLevel <= 4 ? Color.red : Color.gray;
            EditorGUILayout.LabelField("Critical", levelStyle, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            
            // Show current level name
            EditorGUILayout.LabelField($"Current level: {LogLevelToString((byte)currentLevel)}", EditorStyles.miniLabel);
        }
        
        /// <summary>
        /// Draw tag filters section
        /// </summary>
        private void DrawTagFilters()
        {
            _tagFiltersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_tagFiltersFoldout, "Tag Filters");
            
            if (_tagFiltersFoldout && _includeTagsProp != null && _excludeTagsProp != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox(
                    "Configure which tags to include or exclude. If no tags are specified in either list, all tags are logged.",
                    MessageType.Info);
                
                // Safe access to properties
                EditorGUILayout.PropertyField(_includeTagsProp, 
                    new GUIContent("Include Tags", "Only logs with these tags will be processed (if list is not empty)"), true);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(_excludeTagsProp, 
                    new GUIContent("Exclude Tags", "Logs with these tags will be ignored"), true);
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw debug settings section
        /// </summary>
        private void DrawDebugSettings()
        {
            _debugSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_debugSettingsFoldout, "Debug & Advanced");
            
            if (_debugSettingsFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Validate configuration button
                if (GUILayout.Button("Validate Configuration"))
                {
                    ValidateConfiguration();
                }
                
                EditorGUILayout.Space(5);
                
                // Reset to defaults button
                if (GUILayout.Button("Reset to Defaults"))
                {
                    if (EditorUtility.DisplayDialog("Reset to Defaults", 
                        "Are you sure you want to reset all settings to their default values?", 
                        "Reset", "Cancel"))
                    {
                        ResetToDefaults();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw test buttons section
        /// </summary>
        private void DrawTestButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Test In Console", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "Send test messages to the Unity Console to preview how they will be formatted.",
                MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Log Test Messages"))
            {
                // Log test messages at different levels
                Debug.Log("Debug test message");
                Debug.Log("Info test message");
                Debug.LogWarning("Warning test message");
                Debug.LogError("Error test message");
                Debug.LogError("Critical test message");
            }
            
            if (GUILayout.Button("Clear Console"))
            {
                // Clear the console using reflection
                try
                {
                    var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                    if (logEntries != null)
                    {
                        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        if (clearMethod != null)
                        {
                            clearMethod.Invoke(null, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to clear console: {ex.Message}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Validate the configuration
        /// </summary>
        private void ValidateConfiguration()
        {
            bool isValid = true;
            string message = "Configuration validated:\n\n";
            
            // Check if target name is set
            if (_targetNameProp != null && string.IsNullOrEmpty(_targetNameProp.stringValue))
            {
                isValid = false;
                message += "- Target name is not set.\n";
            }
            
            // Check if colorized output is enabled but no formatter is assigned
            if (_useColorizedOutputProp != null && _formatterProp != null && 
                _useColorizedOutputProp.boolValue && _formatterProp.objectReferenceValue == null)
            {
                isValid = false;
                message += "- Colorized output is enabled but no formatter is assigned.\n";
            }
            
            // Check for null properties
            if (_enabledProp == null || _targetNameProp == null || _minimumLevelProp == null ||
                _includeTagsProp == null || _excludeTagsProp == null ||
                _formatterProp == null || _useColorizedOutputProp == null)
            {
                isValid = false;
                message += "- Some properties could not be found. Editor may need to be reloaded.\n";
            }
            
            if (isValid)
            {
                message += "No issues found! Configuration is valid.";
                EditorUtility.DisplayDialog("Validation Results", message, "OK");
            }
            else
            {
                message += "\nWould you like to fix these issues?";
                if (EditorUtility.DisplayDialog("Validation Results", message, "Fix Issues", "Cancel"))
                {
                    // Fix target name if empty
                    if (_targetNameProp != null && string.IsNullOrEmpty(_targetNameProp.stringValue))
                    {
                        _targetNameProp.stringValue = "Unity Console";
                    }
                    
                    // Create and assign formatter if needed
                    if (_useColorizedOutputProp != null && _formatterProp != null &&
                        _useColorizedOutputProp.boolValue && _formatterProp.objectReferenceValue == null)
                    {
                        CreateAndAssignColorizedFormatter();
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        private void ResetToDefaults()
        {
            if (_enabledProp != null)
                _enabledProp.boolValue = true;
                
            if (_targetNameProp != null)
                _targetNameProp.stringValue = "Unity Console";
                
            if (_minimumLevelProp != null)
                _minimumLevelProp.intValue = LogLevel.Debug;
                
            if (_includeTagsProp != null)
                _includeTagsProp.ClearArray();
                
            if (_excludeTagsProp != null)
                _excludeTagsProp.ClearArray();
                
            if (_useColorizedOutputProp != null)
                _useColorizedOutputProp.boolValue = true;
            
            // Create a new formatter if one doesn't exist
            if (_formatterProp != null && _formatterProp.objectReferenceValue == null)
            {
                CreateAndAssignColorizedFormatter();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Create and assign a new colorized formatter
        /// </summary>
        private void CreateAndAssignColorizedFormatter()
        {
            if (_formatterProp == null)
                return;
                
            // Create a save path
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Colorized Console Formatter",
                "ColorizedConsoleFormatter",
                "asset",
                "Choose where to save the new formatter");
                
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    // Create the formatter asset
                    ColorizedConsoleFormatter formatter = CreateInstance<ColorizedConsoleFormatter>();
                    
                    // Save the asset
                    AssetDatabase.CreateAsset(formatter, path);
                    AssetDatabase.SaveAssets();
                    
                    // Assign it to our config
                    _formatterProp.objectReferenceValue = formatter;
                    
                    // Refresh
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create formatter: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Convert a log level byte to a string representation
        /// </summary>
        /// <param name="level">Log level</param>
        /// <returns>String representation of the log level</returns>
        private string LogLevelToString(byte level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "Debug";
                case LogLevel.Info:
                    return "Info";
                case LogLevel.Warning:
                    return "Warning";
                case LogLevel.Error:
                    return "Error";
                case LogLevel.Critical:
                    return "Critical";
                default:
                    return $"Level ({level})";
            }
        }
    }
}