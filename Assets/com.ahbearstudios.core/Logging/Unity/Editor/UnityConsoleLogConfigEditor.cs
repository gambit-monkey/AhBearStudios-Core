using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor for Unity Console Log Configuration.
    /// Provides a rich UI for configuring the Unity Console log target.
    /// </summary>
    [CustomEditor(typeof(UnityConsoleTargetConfig))]
    public class UnityConsoleLogConfigEditor : UnityEditor.Editor
    {
        #region Private Fields
        
        // Configuration object
        private UnityConsoleTargetConfig _config;
        
        // Serialized properties
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
        
        // Level colors for UI
        private readonly Dictionary<LogLevel, Color> _levelColors = new Dictionary<LogLevel, Color>
        {
            { LogLevel.Trace, Color.gray },
            { LogLevel.Debug, Color.white },
            { LogLevel.Info, Color.cyan },
            { LogLevel.Warning, Color.yellow },
            { LogLevel.Error, new Color(1, 0.5f, 0) },
            { LogLevel.Critical, Color.red },
            { LogLevel.None, Color.gray }
        };
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// Initialize when the editor is enabled
        /// </summary>
        private void OnEnable()
        {
            _config = (UnityConsoleTargetConfig)target;
            
            // Find serialized properties
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
        
        #endregion
        
        #region Property Validation
        
        /// <summary>
        /// Validates that all serialized properties were found
        /// </summary>
        private void ValidateSerializedProperties()
        {
            // Check if any properties weren't found and log warnings
            if (_enabledProp == null)
                Debug.LogWarning("Property 'Enabled' not found in UnityConsoleTargetConfig");
            if (_targetNameProp == null)
                Debug.LogWarning("Property 'TargetName' not found in UnityConsoleTargetConfig");
            if (_minimumLevelProp == null)
                Debug.LogWarning("Property 'MinimumLevel' not found in UnityConsoleTargetConfig");
            if (_includeTagsProp == null)
                Debug.LogWarning("Property 'IncludeTags' not found in UnityConsoleTargetConfig");
            if (_excludeTagsProp == null)
                Debug.LogWarning("Property 'ExcludeTags' not found in UnityConsoleTargetConfig");
            if (_formatterProp == null)
                Debug.LogWarning("Property '_formatter' not found in UnityConsoleTargetConfig");
            if (_useColorizedOutputProp == null)
                Debug.LogWarning("Property '_useColorizedOutput' not found in UnityConsoleTargetConfig");
        }
        
        #endregion
        
        #region GUI Drawing
        
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
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Unity Console Log Configuration", headerStyle);
            
            GUIStyle descStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            
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
                    EditorGUILayout.PropertyField(_minimumLevelProp, 
                        new GUIContent("Minimum Level", "The minimum log level to capture"));
                    
                    LogLevel currentLevel = (LogLevel)_minimumLevelProp.enumValueIndex;
                    
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
                                var formatter = _formatterProp.objectReferenceValue;
                                
                                EditorGUILayout.Space(5);
                                
                                if (GUILayout.Button("Edit Formatter Colors"))
                                {
                                    // Open the color editor window
                                    var editorWindowType = Type.GetType("AhBearStudios.Core.Logging.Editor.ColorizedFormatterColorEditor, Assembly-CSharp-Editor");
                                    if (editorWindowType != null)
                                    {
                                        var openWindowMethod = editorWindowType.GetMethod("OpenWindow");
                                        if (openWindowMethod != null)
                                        {
                                            openWindowMethod.Invoke(null, new object[] { formatter });
                                        }
                                    }
                                }
                                
                                // Show formatter preview
                                EditorGUILayout.Space(5);
                                EditorGUILayout.LabelField("Formatter Preview:", EditorStyles.boldLabel);
                                
                                // Only show preview if formatter is valid
                                if (formatter != null)
                                {
                                    // Create sample messages for preview (one for each log level)
                                    DrawFormatterPreview();
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
        /// Draw a preview of formatted log messages
        /// </summary>
        private void DrawFormatterPreview()
        {
            // Create a style that supports rich text
            GUIStyle richTextStyle = new GUIStyle(EditorStyles.textField)
            {
                richText = true,
                wordWrap = true
            };
            
            // Preview for info level
            string infoPreview = "<color=cyan>[INFO]</color> Sample info message";
            EditorGUILayout.SelectableLabel(infoPreview, richTextStyle, GUILayout.Height(20));
            
            // Preview for warning level
            string warnPreview = "<color=yellow>[WARNING]</color> Sample warning message";
            EditorGUILayout.SelectableLabel(warnPreview, richTextStyle, GUILayout.Height(20));
            
            // Preview for error level
            string errorPreview = "<color=red>[ERROR]</color> Sample error message";
            EditorGUILayout.SelectableLabel(errorPreview, richTextStyle, GUILayout.Height(20));
        }
        
        /// <summary>
        /// Draw log level indicator with colored labels
        /// </summary>
        /// <param name="currentLevel">Current log level</param>
        private void DrawLogLevelIndicator(LogLevel currentLevel)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Levels that will be logged:", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Get all log levels except None
            var levels = new[]
            {
                LogLevel.Trace,
                LogLevel.Debug,
                LogLevel.Info,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Critical
            };
            
            foreach (var level in levels)
            {
                bool isActive = level >= currentLevel && currentLevel != LogLevel.None;
                
                GUIStyle levelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = isActive ? _levelColors[level] : Color.gray },
                    alignment = TextAnchor.MiddleCenter
                };
                
                GUILayout.Label(level.GetName(), levelStyle, GUILayout.ExpandWidth(true));
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Description of current level
            string levelDescription = GetLogLevelDescription(currentLevel);
            EditorGUILayout.LabelField(levelDescription, EditorStyles.wordWrappedMiniLabel);
            
            EditorGUILayout.EndVertical();
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
                ClearConsole();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get a descriptive text for a log level
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>A user-friendly description of the log level</returns>
        private string GetLogLevelDescription(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "Trace: Most detailed information, typically only enabled during development.",
                LogLevel.Debug => "Debug: Detailed information useful for debugging purposes.",
                LogLevel.Info => "Info: Informational messages that highlight progress or application state.",
                LogLevel.Warning => "Warning: Potentially harmful situations or unexpected behavior.",
                LogLevel.Error => "Error: Error events that might still allow the application to continue.",
                LogLevel.Critical => "Critical: Very severe error events that will likely cause the application to abort.",
                LogLevel.None => "None: No logs will be processed (logging is disabled).",
                _ => $"Unknown level ({level})"
            };
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
                _minimumLevelProp.enumValueIndex = (int)LogLevel.Debug;
                
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
                    // Get the formatter type using reflection
                    var formatterType = Type.GetType("AhBearStudios.Core.Logging.Formatters.ColorizedConsoleFormatter, Assembly-CSharp");
                    if (formatterType == null)
                    {
                        Debug.LogError("ColorizedConsoleFormatter type not found");
                        return;
                    }
                    
                    // Create the formatter asset
                    ScriptableObject formatter = CreateInstance(formatterType);
                    
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
        /// Clears the Unity console
        /// </summary>
        private void ClearConsole()
        {
            try
            {
                var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
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
        
        #endregion
    }
}