using UnityEditor;
using UnityEngine;
using AhBearStudios.Unity.Logging.ScriptableObjects;

namespace AhBearStudios.Unity.Logging.Editor
{
    /// <summary>
    /// Custom editor for LoggingConfigurationAsset that provides a user-friendly interface for designers.
    /// </summary>
    [CustomEditor(typeof(LoggingConfigurationAsset))]
    public class LoggingConfigurationEditor : UnityEditor.Editor
    {
        private LoggingConfigurationAsset _config;
        private bool _showAdvancedSettings = false;
        private bool _showTargetConfigs = true;
        private bool _showFormatterConfigs = true;
        private bool _showFilterConfigs = true;
        private bool _showChannelConfigs = true;
        private bool _showPlatformOverrides = false;

        private void OnEnable()
        {
            _config = (LoggingConfigurationAsset)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Logging System Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This asset configures the AhBearStudios Core Logging System. " +
                                   "Changes made here will affect all logging throughout the application.", MessageType.Info);

            EditorGUILayout.Space();

            // Quick Actions
            DrawQuickActions();

            EditorGUILayout.Space();

            // Configuration Status
            DrawConfigurationStatus();

            EditorGUILayout.Space();

            // Basic Settings
            DrawBasicSettings();

            // Advanced Settings
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings");
            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                DrawAdvancedSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Target Configurations
            _showTargetConfigs = EditorGUILayout.Foldout(_showTargetConfigs, $"Target Configurations ({_config.TargetConfigurations.Count})");
            if (_showTargetConfigs)
            {
                EditorGUI.indentLevel++;
                DrawTargetConfigurations();
                EditorGUI.indentLevel--;
            }

            // Formatter Configurations
            _showFormatterConfigs = EditorGUILayout.Foldout(_showFormatterConfigs, $"Formatter Configurations ({_config.FormatterConfigurations.Count})");
            if (_showFormatterConfigs)
            {
                EditorGUI.indentLevel++;
                DrawFormatterConfigurations();
                EditorGUI.indentLevel--;
            }

            // Filter Configurations
            _showFilterConfigs = EditorGUILayout.Foldout(_showFilterConfigs, $"Filter Configurations ({_config.FilterConfigurations.Count})");
            if (_showFilterConfigs)
            {
                EditorGUI.indentLevel++;
                DrawFilterConfigurations();
                EditorGUI.indentLevel--;
            }

            // Channel Configurations
            _showChannelConfigs = EditorGUILayout.Foldout(_showChannelConfigs, $"Channel Configurations ({_config.ChannelConfigurations.Count})");
            if (_showChannelConfigs)
            {
                EditorGUI.indentLevel++;
                DrawChannelConfigurations();
                EditorGUI.indentLevel--;
            }

            // Platform Overrides
            _showPlatformOverrides = EditorGUILayout.Foldout(_showPlatformOverrides, $"Platform Overrides ({_config.PlatformOverrides.Count})");
            if (_showPlatformOverrides)
            {
                EditorGUI.indentLevel++;
                DrawPlatformOverrides();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Configuration", GUILayout.Height(30)))
            {
                ValidateConfiguration();
            }
            
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Configuration", 
                    "Are you sure you want to reset all settings to default values?", 
                    "Reset", "Cancel"))
                {
                    _config.ResetToDefaults();
                    EditorUtility.SetDirty(_config);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Unity Console Target", GUILayout.Height(25)))
            {
                CreateUnityConsoleTarget();
            }
            
            if (GUILayout.Button("Create File Target", GUILayout.Height(25)))
            {
                CreateFileTarget();
            }
            
            if (GUILayout.Button("Create Memory Target", GUILayout.Height(25)))
            {
                CreateMemoryTarget();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfigurationStatus()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            
            var errors = _config.ValidateConfiguration();
            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ Configuration is valid", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ Configuration has {errors.Count} errors", MessageType.Warning);
                foreach (var error in errors)
                {
                    EditorGUILayout.HelpBox($"• {error}", MessageType.Error);
                }
            }

            // Show configuration summary
            var summary = _config.GetConfigurationSummary();
            EditorGUILayout.TextArea(summary, GUILayout.Height(60));
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_isLoggingEnabled"), new GUIContent("Enable Logging"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_globalMinimumLevel"), new GUIContent("Global Minimum Level"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_currentScenario"), new GUIContent("Current Scenario"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_useScenarioOverrides"), new GUIContent("Use Scenario Overrides"));
        }

        private void DrawAdvancedSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxQueueSize"), new GUIContent("Max Queue Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_flushIntervalSeconds"), new GUIContent("Flush Interval (seconds)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_highPerformanceMode"), new GUIContent("High Performance Mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_burstCompatibility"), new GUIContent("Burst Compatibility"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_structuredLogging"), new GUIContent("Structured Logging"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_batchingEnabled"), new GUIContent("Enable Batching"));
            
            if (_config.BatchingEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_batchSize"), new GUIContent("Batch Size"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoCorrelationId"), new GUIContent("Auto Correlation ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_correlationIdFormat"), new GUIContent("Correlation ID Format"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_messageFormat"), new GUIContent("Message Format"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_includeTimestamps"), new GUIContent("Include Timestamps"));
            
            if (_config.IncludeTimestamps)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_timestampFormat"), new GUIContent("Timestamp Format"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_cachingEnabled"), new GUIContent("Enable Caching"));
            
            if (_config.CachingEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxCacheSize"), new GUIContent("Max Cache Size"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawTargetConfigurations()
        {
            var targetConfigsProperty = serializedObject.FindProperty("_targetConfigurations");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
            
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                ShowTargetCreationMenu();
            }
            
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < targetConfigsProperty.arraySize; i++)
            {
                var targetProperty = targetConfigsProperty.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(targetProperty, new GUIContent($"Target {i + 1}"));
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    targetConfigsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFormatterConfigurations()
        {
            var formatterConfigsProperty = serializedObject.FindProperty("_formatterConfigurations");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Formatters", EditorStyles.boldLabel);
            
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                ShowFormatterCreationMenu();
            }
            
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < formatterConfigsProperty.arraySize; i++)
            {
                var formatterProperty = formatterConfigsProperty.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(formatterProperty, new GUIContent($"Formatter {i + 1}"));
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    formatterConfigsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFilterConfigurations()
        {
            var filterConfigsProperty = serializedObject.FindProperty("_filterConfigurations");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                ShowFilterCreationMenu();
            }
            
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < filterConfigsProperty.arraySize; i++)
            {
                var filterProperty = filterConfigsProperty.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(filterProperty, new GUIContent($"Filter {i + 1}"));
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    filterConfigsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawChannelConfigurations()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_channelConfigurations"), new GUIContent("Channels"), true);
        }

        private void DrawPlatformOverrides()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_platformOverrides"), new GUIContent("Platform Overrides"), true);
        }

        private void ValidateConfiguration()
        {
            var errors = _config.ValidateConfiguration();
            
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("Configuration Valid", "The logging configuration is valid!", "OK");
            }
            else
            {
                var errorMessage = "Configuration validation failed:\n\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("Configuration Invalid", errorMessage, "OK");
            }
        }

        private void ShowTargetCreationMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Unity Console Target"), false, CreateUnityConsoleTarget);
            menu.AddItem(new GUIContent("File Target"), false, CreateFileTarget);
            menu.AddItem(new GUIContent("Memory Target"), false, CreateMemoryTarget);
            menu.ShowAsContext();
        }

        private void ShowFormatterCreationMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("JSON Formatter"), false, CreateJsonFormatter);
            menu.AddItem(new GUIContent("Plain Text Formatter"), false, CreatePlainTextFormatter);
            menu.ShowAsContext();
        }

        private void ShowFilterCreationMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Level Filter"), false, CreateLevelFilter);
            menu.ShowAsContext();
        }

        private void CreateUnityConsoleTarget()
        {
            var target = CreateInstance<Targets.UnityConsoleTargetConfig>();
            target.name = "Unity Console Target";
            target.ResetToDefaults();
            
            AssetDatabase.CreateAsset(target, $"Assets/UnityConsoleTarget_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _config.AddTargetConfiguration(target);
            EditorUtility.SetDirty(_config);
        }

        private void CreateFileTarget()
        {
            var target = CreateInstance<Targets.FileTargetConfig>();
            target.name = "File Target";
            target.ResetToDefaults();
            
            AssetDatabase.CreateAsset(target, $"Assets/FileTarget_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _config.AddTargetConfiguration(target);
            EditorUtility.SetDirty(_config);
        }

        private void CreateMemoryTarget()
        {
            var target = CreateInstance<Targets.MemoryTargetConfig>();
            target.name = "Memory Target";
            target.ResetToDefaults();
            
            AssetDatabase.CreateAsset(target, $"Assets/MemoryTarget_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _config.AddTargetConfiguration(target);
            EditorUtility.SetDirty(_config);
        }

        private void CreateJsonFormatter()
        {
            var formatter = CreateInstance<Formatters.JsonFormatterConfig>();
            formatter.name = "JSON Formatter";
            formatter.ResetToDefaults();
            
            AssetDatabase.CreateAsset(formatter, $"Assets/JsonFormatter_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _config.AddFormatterConfiguration(formatter);
            EditorUtility.SetDirty(_config);
        }

        private void CreatePlainTextFormatter()
        {
            var formatter = CreateInstance<Formatters.PlainTextFormatterConfig>();
            formatter.name = "Plain Text Formatter";
            formatter.ResetToDefaults();
            
            AssetDatabase.CreateAsset(formatter, $"Assets/PlainTextFormatter_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _config.AddFormatterConfiguration(formatter);
            EditorUtility.SetDirty(_config);
        }

        private void CreateLevelFilter()
        {
            var filter = CreateInstance<Filters.LevelFilterConfig>();
            filter.name = "Level Filter";
            filter.ResetToDefaults();
            
            AssetDatabase.CreateAsset(filter, $"Assets/LevelFilter_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            _config.AddFilterConfiguration(filter);
            EditorUtility.SetDirty(_config);
        }
    }
}