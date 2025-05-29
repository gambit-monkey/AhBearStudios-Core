using UnityEngine;
using UnityEditor;
using System.Linq;
using AhBearStudios.Core.Logging.Configuration;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom editor for LogManagerConfig to provide a better Unity Inspector experience.
    /// </summary>
    [CustomEditor(typeof(LogManagerConfig))]
    public class LogManagerConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _minimumLevel;
        private SerializedProperty _maxMessagesPerBatch;
        private SerializedProperty _initialQueueCapacity;
        private SerializedProperty _enableAutoFlush;
        private SerializedProperty _autoFlushInterval;
        private SerializedProperty _defaultTag;
        private SerializedProperty _logTargets;
        private SerializedProperty _enableMessageBusIntegration;
        private SerializedProperty _configurationName;
        private SerializedProperty _validateTargetsOnStartup;
        private SerializedProperty _autoCreateDirectories;
        
        private bool _showAdvancedSettings = false;
        private bool _showTargetValidation = true;
        
        private void OnEnable()
        {
            // Find serialized properties
            _minimumLevel = serializedObject.FindProperty("_minimumLevel");
            _maxMessagesPerBatch = serializedObject.FindProperty("_maxMessagesPerBatch");
            _initialQueueCapacity = serializedObject.FindProperty("_initialQueueCapacity");
            _enableAutoFlush = serializedObject.FindProperty("_enableAutoFlush");
            _autoFlushInterval = serializedObject.FindProperty("_autoFlushInterval");
            _defaultTag = serializedObject.FindProperty("_defaultTag");
            _logTargets = serializedObject.FindProperty("_logTargets");
            _enableMessageBusIntegration = serializedObject.FindProperty("_enableMessageBusIntegration");
            _configurationName = serializedObject.FindProperty("_configurationName");
            _validateTargetsOnStartup = serializedObject.FindProperty("_validateTargetsOnStartup");
            _autoCreateDirectories = serializedObject.FindProperty("_autoCreateDirectories");
        }
        
        public override void OnInspectorGUI()
        {
            var config = (LogManagerConfig)target;
            
            serializedObject.Update();
            
            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log Manager Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Configuration Info
            DrawConfigurationInfo(config);
            
            // Global Settings
            DrawGlobalSettings();
            
            // Log Targets
            DrawLogTargets(config);
            
            // Advanced Settings
            DrawAdvancedSettings();
            
            // Validation
            if (_showTargetValidation)
            {
                DrawValidation(config);
            }
            
            // Buttons
            DrawActionButtons(config);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawConfigurationInfo(LogManagerConfig config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration Info", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(_configurationName, new GUIContent("Configuration Name"));
            
            // Show summary
            var enabledCount = config.GetEnabledTargetCount();
            var totalCount = config.LogTargets?.Length ?? 0;
            
            EditorGUILayout.LabelField("Enabled Targets", $"{enabledCount} of {totalCount}");
            EditorGUILayout.LabelField("Min Log Level", config.MinimumLevel.ToString());
            EditorGUILayout.LabelField("Auto Flush", config.EnableAutoFlush ? $"Every {config.AutoFlushInterval:F2}s" : "Disabled");
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawGlobalSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Global Logging Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(_minimumLevel, new GUIContent("Minimum Log Level", "The minimum log level that will be processed"));
            EditorGUILayout.PropertyField(_maxMessagesPerBatch, new GUIContent("Max Messages Per Batch", "Maximum number of messages to process per batch"));
            EditorGUILayout.PropertyField(_initialQueueCapacity, new GUIContent("Initial Queue Capacity", "Initial capacity of the log queue"));
            EditorGUILayout.PropertyField(_defaultTag, new GUIContent("Default Tag", "Default tag to use when no tag is specified"));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(_enableAutoFlush, new GUIContent("Enable Auto Flush", "Enable automatic flushing of logs"));
            if (_enableAutoFlush.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_autoFlushInterval, new GUIContent("Auto Flush Interval", "Interval in seconds between auto-flush operations"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawLogTargets(LogManagerConfig config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header with add button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Log Targets", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Target", GUILayout.Width(100)))
            {
                ShowAddTargetMenu(config);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (config.LogTargets == null || config.LogTargets.Length == 0)
            {
                EditorGUILayout.HelpBox("No log targets configured. Add targets to enable logging output.", MessageType.Warning);
            }
            else
            {
                // Draw each target
                for (int i = 0; i < config.LogTargets.Length; i++)
                {
                    DrawLogTarget(config, i);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawLogTarget(LogManagerConfig config, int index)
        {
            var target = config.LogTargets[index];
            if (target == null)
            {
                EditorGUILayout.HelpBox($"Target {index} is null. Remove it from the array.", MessageType.Error);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // Enabled toggle
            var wasEnabled = target.Enabled;
            var isEnabled = EditorGUILayout.Toggle(wasEnabled, GUILayout.Width(20));
            if (isEnabled != wasEnabled)
            {
                target.Enabled = isEnabled;
                EditorUtility.SetDirty(target);
            }
            
            // Target name and type
            var targetName = string.IsNullOrEmpty(target.TargetName) ? "Unnamed Target" : target.TargetName;
            var targetType = target.GetType().Name.Replace("Config", "").Replace("LogTarget", "");
            
            EditorGUILayout.LabelField($"{targetName} ({targetType})", isEnabled ? EditorStyles.label : EditorStyles.centeredGreyMiniLabel);
            
            // Edit button
            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                Selection.activeObject = target;
            }
            
            // Remove button
            GUI.color = Color.red;
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Remove Log Target", 
                    $"Are you sure you want to remove '{targetName}'?", "Remove", "Cancel"))
                {
                    config.RemoveLogTarget(target);
                    GUI.color = Color.white;
                    return;
                }
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // Show target details
            if (isEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Min Level", target.MinimumLevel.ToString());
                if (target.IncludedTags?.Length > 0)
                {
                    EditorGUILayout.LabelField("Included Tags", string.Join(", ", target.IncludedTags));
                }
                if (target.ExcludedTags?.Length > 0)
                {
                    EditorGUILayout.LabelField("Excluded Tags", string.Join(", ", target.ExcludedTags));
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ShowAddTargetMenu(LogManagerConfig config)
        {
            var menu = new GenericMenu();
            
            // Find all LogTargetConfig types
            var targetConfigTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(LogTargetConfig)) && !type.IsAbstract)
                .OrderBy(type => type.Name);
            
            foreach (var targetType in targetConfigTypes)
            {
                var typeName = targetType.Name.Replace("Config", "").Replace("LogTarget", "");
                menu.AddItem(new GUIContent(typeName), false, () => CreateNewTarget(config, targetType));
            }
            
            if (!targetConfigTypes.Any())
            {
                menu.AddDisabledItem(new GUIContent("No target types found"));
            }
            
            menu.ShowAsContext();
        }
        
        private void CreateNewTarget(LogManagerConfig config, System.Type targetType)
        {
            var newTarget = CreateInstance(targetType) as LogTargetConfig;
            if (newTarget != null)
            {
                var typeName = targetType.Name.Replace("Config", "").Replace("LogTarget", "");
                newTarget.TargetName = $"{typeName}Target";
                newTarget.name = newTarget.TargetName;
                
                // Save the new target as a sub-asset
                AssetDatabase.AddObjectToAsset(newTarget, config);
                config.AddLogTarget(newTarget);
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Select the new target for editing
                Selection.activeObject = newTarget;
            }
        }
        
        private void DrawAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);
            
            if (_showAdvancedSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.PropertyField(_enableMessageBusIntegration, 
                    new GUIContent("Enable Message Bus Integration", "Whether to enable message bus integration for logging events"));
                
                EditorGUILayout.PropertyField(_validateTargetsOnStartup, 
                    new GUIContent("Validate Targets On Startup", "Whether to validate log targets on startup"));
                
                EditorGUILayout.PropertyField(_autoCreateDirectories, 
                    new GUIContent("Auto Create Directories", "Whether to create directories for file-based targets automatically"));
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawValidation(LogManagerConfig config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            _showTargetValidation = EditorGUILayout.Toggle(_showTargetValidation, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
            
            if (config.ValidateConfiguration())
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Configuration has issues. Check the console for details.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons(LogManagerConfig config)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Configuration"))
            {
                if (config.ValidateConfiguration())
                {
                    EditorUtility.DisplayDialog("Validation", "Configuration is valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation", "Configuration has issues. Check the console for details.", "OK");
                }
            }
            
            if (GUILayout.Button("Create Test Targets"))
            {
                CreateTestTargets(config);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateTestTargets(LogManagerConfig config)
        {
            // This is a helper method for development - remove in production
            if (EditorUtility.DisplayDialog("Create Test Targets", 
                "This will create example log targets for testing. Continue?", "Create", "Cancel"))
            {
                // Implementation would depend on what target types you have available
                Debug.Log("Test target creation not implemented - add your specific target types here");
            }
        }
    }
}