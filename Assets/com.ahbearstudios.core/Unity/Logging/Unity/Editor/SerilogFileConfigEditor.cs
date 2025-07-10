using UnityEngine;
using UnityEditor;
using System.IO;
using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor for SerilogFileTargetConfig that provides an enhanced UI with validation,
    /// file path selection, and improved organization of settings.
    /// </summary>
    [CustomEditor(typeof(SerilogFileTargetConfig))]
    public class SerilogFileConfigEditor : UnityEditor.Editor
    {
        #region Private Fields
        
        // SerializedProperty references
        private SerializedProperty _targetNameProp;
        private SerializedProperty _enabledProp;
        private SerializedProperty _minimumLevelProp;
        private SerializedProperty _includeTagsProp;
        private SerializedProperty _excludeTagsProp;
        private SerializedProperty _logFilePathProp;
        private SerializedProperty _useJsonFormatProp;
        private SerializedProperty _logToConsoleProp;
        private SerializedProperty _retainedDaysProp;
        
        // Foldout states
        private bool _generalSettingsFoldout = true;
        private bool _fileSettingsFoldout = true;
        private bool _formatSettingsFoldout = true;
        private bool _tagFiltersFoldout = false;
        
        // Content for UI labels
        private GUIContent _logLevelContent;
        private GUIContent _filePathContent;
        private GUIContent _retainedDaysContent;
        
        // Constants
        private const int MIN_RETAINED_DAYS = 1;
        private const int MAX_RETAINED_DAYS = 90;
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// Initialize the editor when it's first loaded
        /// </summary>
        private void OnEnable()
        {
            // Find all serialized properties
            _targetNameProp = serializedObject.FindProperty("_targetName");
            _enabledProp = serializedObject.FindProperty("_enabled");
            _minimumLevelProp = serializedObject.FindProperty("_minimumLevel");
            _includeTagsProp = serializedObject.FindProperty("_includeTags");
            _excludeTagsProp = serializedObject.FindProperty("_excludeTags");
            _logFilePathProp = serializedObject.FindProperty("_logFilePath");
            _useJsonFormatProp = serializedObject.FindProperty("_useJsonFormat");
            _logToConsoleProp = serializedObject.FindProperty("_logToConsole");
            _retainedDaysProp = serializedObject.FindProperty("_retainedDays");
            
            // Initialize rich content objects for UI elements
            _logLevelContent = new GUIContent(
                "Minimum Log Level",
                "The minimum level of logs that will be processed by this target. Messages with lower levels will be ignored."
            );
            
            _filePathContent = new GUIContent(
                "Log File Path",
                "Path where logs will be written. Can be absolute or relative to Application.persistentDataPath."
            );
            
            _retainedDaysContent = new GUIContent(
                "Retain Logs (Days)",
                "Number of days to retain log files before they are automatically deleted. Set to 0 to disable automatic cleanup."
            );
        }
        
        /// <summary>
        /// Draw the custom inspector UI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            DrawHeader();
            
            EditorGUILayout.Space();
            DrawGeneralSettings();
            
            EditorGUILayout.Space();
            DrawFileSettings();
            
            EditorGUILayout.Space();
            DrawFormatSettings();
            
            EditorGUILayout.Space();
            DrawTagFilters();
            
            EditorGUILayout.Space();
            DrawTestButtons();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
        
        #region Drawing Methods
        
        /// <summary>
        /// Draw the config header
        /// </summary>
        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };
            
            EditorGUILayout.LabelField("Serilog File Configuration", headerStyle);
            
            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField(
                "Configure settings for Serilog file-based logging.",
                descriptionStyle);
        }
        
        /// <summary>
        /// Draw general target settings (name, enabled, log level)
        /// </summary>
        private void DrawGeneralSettings()
        {
            _generalSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_generalSettingsFoldout, "General Settings");
            
            if (_generalSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Target name
                EditorGUILayout.PropertyField(_targetNameProp, 
                    new GUIContent("Target Name", "The unique name for this log target."));
                
                // Warning for empty name
                if (string.IsNullOrWhiteSpace(_targetNameProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Target name should not be empty.", MessageType.Warning);
                }
                
                EditorGUILayout.Space(4);
                
                // Enabled toggle
                EditorGUILayout.PropertyField(_enabledProp, 
                    new GUIContent("Enabled", "Whether this log target is active and will receive log messages."));
                
                EditorGUILayout.Space(4);
                
                // Log level enum popup
                EditorGUILayout.PropertyField(_minimumLevelProp, _logLevelContent);
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw file-specific settings (file path, retention)
        /// </summary>
        private void DrawFileSettings()
        {
            _fileSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_fileSettingsFoldout, "File Settings");
            
            if (_fileSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // File path with browse button
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_logFilePathProp, _filePathContent);
                
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string currentPath = _logFilePathProp.stringValue;
                    string initialDirectory = string.IsNullOrEmpty(currentPath) ? 
                        Application.persistentDataPath : Path.GetDirectoryName(GetFullPath(currentPath));
                        
                    string newPath = EditorUtility.SaveFilePanel(
                        "Select Log File Location",
                        initialDirectory,
                        Path.GetFileName(currentPath) ?? "app.log",
                        "log");
                        
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        // Try to make path relative to persistentDataPath if possible
                        if (newPath.StartsWith(Application.persistentDataPath))
                        {
                            newPath = newPath.Substring(Application.persistentDataPath.Length);
                            if (newPath.StartsWith("/") || newPath.StartsWith("\\"))
                            {
                                newPath = newPath.Substring(1);
                            }
                        }
                        
                        _logFilePathProp.stringValue = newPath;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Show the resolved path for clarity
                SerilogFileTargetConfig targetConfig = (SerilogFileTargetConfig)target;
                EditorGUILayout.HelpBox($"Resolved Path: {targetConfig.LogFilePath}", MessageType.Info);
                
                // Ensure directory exists button
                if (GUILayout.Button("Ensure Directory Exists"))
                {
                    EnsureDirectoryExists(targetConfig.LogFilePath);
                }
                
                EditorGUILayout.Space(4);
                
                // Log retention settings
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(_retainedDaysContent);
                
                _retainedDaysProp.intValue = EditorGUILayout.IntSlider(
                    _retainedDaysProp.intValue, 
                    MIN_RETAINED_DAYS, 
                    MAX_RETAINED_DAYS);
                    
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw format-related settings (JSON, console output)
        /// </summary>
        private void DrawFormatSettings()
        {
            _formatSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_formatSettingsFoldout, "Format Settings");
            
            if (_formatSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // JSON formatting option
                EditorGUILayout.PropertyField(_useJsonFormatProp, 
                    new GUIContent("Use JSON Format", "Whether to format logs as JSON instead of plain text. JSON format improves machine readability but reduces human readability."));
                
                EditorGUILayout.Space(4);
                
                // Console output option
                EditorGUILayout.PropertyField(_logToConsoleProp, 
                    new GUIContent("Log To Console", "Whether to also output logs to the console in addition to the file."));
                
                // Format preview
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Format Preview:", EditorStyles.boldLabel);
                
                string previewText;
                GUIStyle previewStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    richText = true
                };
                
                if (_useJsonFormatProp.boolValue)
                {
                    previewText = "{\n" +
                        "  \"Timestamp\": \"2023-06-15T14:30:25.123+00:00\",\n" +
                        "  \"Level\": \"Information\",\n" +
                        "  \"Message\": \"Player logged in\",\n" +
                        "  \"Tag\": \"Session\",\n" +
                        "  \"Properties\": { \"PlayerId\": \"12345\", \"SessionId\": \"abc-123\" }\n" +
                        "}";
                }
                else
                {
                    previewText = "[2023-06-15 14:30:25.123 +00:00] [Information] [Session] Player logged in { PlayerId: 12345, SessionId: abc-123 }";
                }
                
                EditorGUILayout.SelectableLabel(previewText, previewStyle, GUILayout.Height(100));
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw tag filter settings
        /// </summary>
        private void DrawTagFilters()
        {
            // Check if the tag filter properties exist
            bool hasTagProperties = _includeTagsProp != null && _excludeTagsProp != null;
    
            if (hasTagProperties)
            {
                _tagFiltersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_tagFiltersFoldout, "Tag Filters");
        
                if (_tagFiltersFoldout)
                {
                    EditorGUI.indentLevel++;
            
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
                    // Include tag
                    EditorGUILayout.PropertyField(_includeTagsProp, 
                        new GUIContent("Include Tags", "Only log messages with these tags will be processed. Leave empty to include all tags."), 
                        true); // true to show children
            
                    EditorGUILayout.Space(4);
            
                    // Exclude tag
                    EditorGUILayout.PropertyField(_excludeTagsProp, 
                        new GUIContent("Exclude Tags", "Log messages with these tags will be ignored."), 
                        true); // true to show children
            
                    // Warning about conflicting filters
                    if (_includeTagsProp.arraySize > 0 && _excludeTagsProp.arraySize > 0)
                    {
                        EditorGUILayout.HelpBox("You have both include and exclude tag filters. Make sure they don't conflict.", MessageType.Info);
                    }
            
                    EditorGUILayout.EndVertical();
            
                    EditorGUI.indentLevel--;
                }
        
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else
            {
                // If tag properties don't exist, just display an informational message
                EditorGUILayout.HelpBox(
                    "Tag filtering is not available for this configuration type.", 
                    MessageType.Info);
            }
        }
        
        /// <summary>
        /// Draw test buttons for common operations
        /// </summary>
        private void DrawTestButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Configuration"))
            {
                ValidateConfiguration();
            }
            
            if (GUILayout.Button("Create Test Log"))
            {
                CreateTestLog();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Validates the configuration and shows any warnings or errors
        /// </summary>
        private void ValidateConfiguration()
        {
            SerilogFileTargetConfig targetConfig = (SerilogFileTargetConfig)target;
            bool hasIssues = false;
            string message = "Configuration Validation:\n\n";
            
            // Check target name
            if (string.IsNullOrWhiteSpace(targetConfig.TargetName))
            {
                message += "- Target name is empty. This may cause issues with target identification.\n";
                hasIssues = true;
            }
            
            // Check file path
            if (string.IsNullOrWhiteSpace(_logFilePathProp.stringValue))
            {
                message += "- Log file path is empty. A default path will be used.\n";
                hasIssues = true;
            }
            
            // Check directory exists and is writable
            try
            {
                string directory = Path.GetDirectoryName(targetConfig.LogFilePath);
                
                if (!Directory.Exists(directory))
                {
                    message += $"- Log directory '{directory}' does not exist.\n";
                    hasIssues = true;
                }
                else
                {
                    // Test write access
                    string testFile = Path.Combine(directory, "__test_write_access_file_" + System.Guid.NewGuid().ToString().Substring(0, 8) + ".tmp");
                    try
                    {
                        File.WriteAllText(testFile, "Test");
                        File.Delete(testFile);
                        message += "- Directory is writable. ✓\n";
                    }
                    catch (System.Exception)
                    {
                        message += $"- Directory '{directory}' is not writable!\n";
                        hasIssues = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                message += $"- Error validating file path: {ex.Message}\n";
                hasIssues = true;
            }
            
            // Check retain days
            if (_retainedDaysProp.intValue <= 0)
            {
                message += "- Log retention is disabled. Log files will accumulate over time.\n";
            }
            
            if (hasIssues)
            {
                EditorUtility.DisplayDialog("Validation Results", message, "OK");
            }
            else
            {
                message += "All configuration settings look valid! ✓";
                EditorUtility.DisplayDialog("Validation Successful", message, "OK");
            }
        }
        
        /// <summary>
        /// Creates a test log file to verify configuration
        /// </summary>
        private void CreateTestLog()
        {
            SerilogFileTargetConfig targetConfig = (SerilogFileTargetConfig)target;
            
            string directory = Path.GetDirectoryName(targetConfig.LogFilePath);
            string filename = Path.GetFileNameWithoutExtension(targetConfig.LogFilePath);
            string extension = Path.GetExtension(targetConfig.LogFilePath);
            
            string testLogPath = Path.Combine(directory, 
                filename + "_test_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + extension);
            
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create sample log content with proper log level
                string logLevelName = GetLogLevelDisplayName(targetConfig.MinimumLevel);
                string logContent;
                
                if (targetConfig.UseJsonFormat)
                {
                    logContent = 
                        "{\n" +
                        "  \"Timestamp\": \"" + System.DateTime.Now.ToString("o") + "\",\n" +
                        "  \"Level\": \"" + logLevelName + "\",\n" +
                        "  \"Message\": \"This is a test log entry created from the Unity Editor\",\n" +
                        "  \"Tag\": \"Test\",\n" +
                        "  \"Properties\": { \"Source\": \"UnityEditor\", \"ConfigName\": \"" + targetConfig.name + "\" }\n" +
                        "}\n";
                }
                else
                {
                    logContent = 
                        "[" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " +
                        "[" + logLevelName + "] " +
                        "[Test] " +
                        "This is a test log entry created from the Unity Editor " +
                        "{ Source: UnityEditor, ConfigName: " + targetConfig.name + " }\n";
                }
                
                // Write to file
                File.WriteAllText(testLogPath, logContent);
                
                EditorUtility.DisplayDialog("Test Log Created", 
                    $"A test log file has been created at:\n\n{testLogPath}", "OK");
                
                // Try to reveal in explorer/finder
                EditorUtility.RevealInFinder(testLogPath);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error Creating Test Log", 
                    $"Failed to create test log file: {ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// Get a display-friendly name for a log level
        /// </summary>
        /// <param name="level">The log level to convert</param>
        /// <returns>A display-friendly name</returns>
        private string GetLogLevelDisplayName(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Info => "Information",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                LogLevel.None => "None",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get the full path for a log file path (resolving relative paths)
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The resolved full path</returns>
        private string GetFullPath(string path)
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
        /// Ensures the directory for a log file exists
        /// </summary>
        /// <param name="filePath">The file path</param>
        private void EnsureDirectoryExists(string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    EditorUtility.DisplayDialog("Directory Created", 
                        $"Created log directory:\n\n{directory}", "OK");
                }
                else if (Directory.Exists(directory))
                {
                    EditorUtility.DisplayDialog("Directory Exists", 
                        $"The log directory already exists:\n\n{directory}", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to create directory: {ex.Message}", "OK");
            }
        }
        
        #endregion
    }
}