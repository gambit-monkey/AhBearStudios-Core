using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Tags;
using System;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Extensions;

namespace AhBearStudios.Core.Logging.Editor
{
    /// <summary>
    /// Custom editor for LogManagerConfig.
    /// Provides rich editing capabilities for log manager configuration.
    /// </summary>
    [CustomEditor(typeof(LogManagerConfig))]
    public class LogManagerConfigEditor : UnityEditor.Editor
    {
        // Serialized properties
        private SerializedProperty _minimumLevelProp;
        private SerializedProperty _maxMessagesPerBatchProp;
        private SerializedProperty _initialQueueCapacityProp;
        private SerializedProperty _enableAutoFlushProp;
        private SerializedProperty _autoFlushIntervalProp;
        private SerializedProperty _defaultTagProp;
        
        // Foldout states
        private bool _generalSettingsFoldout = true;
        private bool _performanceSettingsFoldout = false;
        private bool _advancedSettingsFoldout = false;
        
        /// <summary>
        /// Initialize when the editor is enabled
        /// </summary>
        private void OnEnable()
        {
            // Find serialized properties
            _minimumLevelProp = serializedObject.FindProperty("_minimumLevel");
            _maxMessagesPerBatchProp = serializedObject.FindProperty("_maxMessagesPerBatch");
            _initialQueueCapacityProp = serializedObject.FindProperty("_initialQueueCapacity");
            _enableAutoFlushProp = serializedObject.FindProperty("_enableAutoFlush");
            _autoFlushIntervalProp = serializedObject.FindProperty("_autoFlushInterval");
            _defaultTagProp = serializedObject.FindProperty("_defaultTag");
        }
        
        /// <summary>
        /// Draw the custom inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            
            EditorGUILayout.Space();
            
            DrawGeneralSettings();
            
            EditorGUILayout.Space();
            
            DrawPerformanceSettings();
            
            EditorGUILayout.Space();
            
            DrawAdvancedSettings();
            
            EditorGUILayout.Space();
            
            DrawMemoryUsageEstimate();
            
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
            
            EditorGUILayout.LabelField("Log Manager Configuration", headerStyle);
            
            GUIStyle descStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField(
                "This asset configures core behavior for the logging system. Changes will take effect when the application is restarted or new scenes are loaded.",
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
                
                // Minimum Log Level
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Minimum Log Level", "The minimum log level that will be processed"), GUILayout.Width(150));
                
                EditorGUI.BeginChangeCheck();
                int currentLevel = _minimumLevelProp.intValue;
                int newLevel = EditorGUILayout.IntSlider(currentLevel, 0, 4);
                if (EditorGUI.EndChangeCheck())
                {
                    _minimumLevelProp.intValue = newLevel;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Show level names
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Debug", GUILayout.Width(60));
                EditorGUILayout.LabelField("Info", GUILayout.Width(60));
                EditorGUILayout.LabelField("Warning", GUILayout.Width(60));
                EditorGUILayout.LabelField("Error", GUILayout.Width(60));
                EditorGUILayout.LabelField("Critical", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // Auto Flush Settings
                EditorGUILayout.PropertyField(_enableAutoFlushProp, new GUIContent("Enable Auto Flush", "Automatically flush logs at regular intervals"));
                
                if (_enableAutoFlushProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_autoFlushIntervalProp, new GUIContent("Auto Flush Interval", "Interval in seconds between auto-flush operations"));
                    EditorGUI.indentLevel--;
                }
                
                // Default Tag
                EditorGUILayout.PropertyField(_defaultTagProp, new GUIContent("Default Tag", "The default tag to use when no tag is specified"));
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw performance settings section
        /// </summary>
        private void DrawPerformanceSettings()
        {
            _performanceSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_performanceSettingsFoldout, "Performance Settings");
            
            if (_performanceSettingsFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox(
                    "These settings control the memory usage and processing behavior of the logging system. " +
                    "Higher values may improve performance but will use more memory.", 
                    MessageType.Info);
                
                // Initial queue capacity
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_initialQueueCapacityProp, new GUIContent("Initial Queue Capacity", "Initial capacity of the log message queue"));
                if (EditorGUI.EndChangeCheck())
                {
                    // Ensure power of two
                    int newValue = NearestPowerOfTwo(_initialQueueCapacityProp.intValue);
                    if (newValue != _initialQueueCapacityProp.intValue)
                    {
                        _initialQueueCapacityProp.intValue = newValue;
                    }
                }
                
                // Max messages per batch
                EditorGUILayout.PropertyField(_maxMessagesPerBatchProp, new GUIContent("Max Messages Per Batch", "Maximum number of messages to process in a single batch"));
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// Draw advanced settings section
        /// </summary>
        private void DrawAdvancedSettings()
        {
            _advancedSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_advancedSettingsFoldout, "Advanced Settings");
            
            if (_advancedSettingsFoldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Reset to defaults button
                if (GUILayout.Button("Reset To Defaults"))
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
        /// Draw memory usage estimate section
        /// </summary>
        private void DrawMemoryUsageEstimate()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int queueSize = _initialQueueCapacityProp.intValue;
            int messageSize = 512 + 32 + 8 + 4; // Estimated size in bytes (message + tag + timestamp + level)
            int estimatedBytes = queueSize * messageSize;
            float estimatedKB = estimatedBytes / 1024f;
            
            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            
            EditorGUILayout.LabelField($"Estimated Memory Usage: {estimatedKB:F2} KB", infoStyle);
            EditorGUILayout.LabelField($"Initial Queue Capacity: {queueSize} messages", infoStyle);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        private void ResetToDefaults()
        {
            _minimumLevelProp.intValue = LogLevel.Info.ToInt();
            _maxMessagesPerBatchProp.intValue = 200;
            _initialQueueCapacityProp.intValue = 64;
            _enableAutoFlushProp.boolValue = true;
            _autoFlushIntervalProp.floatValue = 0.5f;
            _defaultTagProp.enumValueIndex = (int)Tagging.LogTag.Default;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Find the nearest power of two to the given value
        /// </summary>
        /// <param name="value">The input value</param>
        /// <returns>The nearest power of two (always >= value)</returns>
        private int NearestPowerOfTwo(int value)
        {
            if (value <= 0)
                return 1;
                
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            
            return value;
        }
    }
}