using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AhBearStudios.Core.Logging.Unity;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Data;
using AhBearStudios.Core.Logging.Jobs;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    [CustomEditor(typeof(LogManagerComponent))]
    public class LogManagerComponentEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<Type, bool> _typeCache = new Dictionary<Type, bool>();
        private LogManagerComponent _target;
        private bool _showGeneralSettings = true;
        private bool _showTargetConfigs = true;
        private bool _showAdvancedSettings = false;
        private bool _showRuntimeInfo = true;
        private bool _showPerformanceMetrics = true;
        private bool _showJobLoggerSection = false;
        private bool _showCoroutineSettings = false;
        private Dictionary<string, object> _lastMetrics;
        private double _lastMetricsUpdateTime;
        private const double METRICS_UPDATE_INTERVAL = 0.5; // Update every 0.5 seconds
        
        // Job Logger test fields
        private LogLevel _testJobLoggerLevel = LogLevel.Info;
        private Tagging.LogTag _testJobLoggerTag = default;
        private string _testJobLoggerMessage = "Test job logger message";
        
        // Test message fields
        private LogLevel _testMessageLevel = LogLevel.Info;
        private string _testMessage = "Test message from editor";
        private Tagging.LogTag _testMessageTag = default;

        private void OnEnable()
        {
            _target = (LogManagerComponent)target;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            
            // Header
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Log Manager Component", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
            }
            
            EditorGUILayout.Space();

            // Status indicator
            DrawStatusIndicator();
            EditorGUILayout.Space();

            // Main sections
            DrawGeneralSettings();
            DrawTargetConfigs();
            DrawCoroutineSettings();
            DrawAdvancedSettings();
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawRuntimeInfo();
                DrawPerformanceMetrics();
                DrawJobLoggerSection();
                DrawTestMessageSection();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatusIndicator()
        {
            var isInitialized = Application.isPlaying && _target.IsInitialized;
            var color = isInitialized ? Color.green : (Application.isPlaying ? Color.red : Color.yellow);
            var status = isInitialized ? "Initialized" : (Application.isPlaying ? "Not Initialized" : "Editor Mode");
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Status: {status}", EditorStyles.boldLabel);
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField($"Targets: {_target.TargetCount}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Queued: {_target.QueuedMessageCount}", GUILayout.Width(80));
                }
            }
            
            GUI.backgroundColor = originalColor;
        }

        private void DrawGeneralSettings()
        {
            _showGeneralSettings = EditorGUILayout.Foldout(_showGeneralSettings, "General Settings", true);
            if (!_showGeneralSettings) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_loggingPreset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_globalMinimumLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_persistAcrossScenes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_initializeOnAwake"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Auto Flush Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoFlushEnabled"));
                
                using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("_autoFlushEnabled").boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_autoFlushInterval"));
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_targetCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxQueueSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_initialCapacity"));
            }
        }

        private void DrawCoroutineSettings()
        {
            _showCoroutineSettings = EditorGUILayout.Foldout(_showCoroutineSettings, "Coroutine System Settings", true);
            if (!_showCoroutineSettings) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_useExistingCoroutineManager"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_coroutineRunnerName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableProfiling"));
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    var hasCoroutineRunner = _target.GetType()
                        .GetField("_coroutineRunner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                        .GetValue(_target) != null;
                    
                    EditorGUILayout.LabelField($"Coroutine Runner Active: {hasCoroutineRunner}");
                }
            }
        }

        private void DrawTargetConfigs()
        {
            _showTargetConfigs = EditorGUILayout.Foldout(_showTargetConfigs, "Target Configurations", true);
            if (!_showTargetConfigs) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Unity Console Target", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_unityConsoleConfig"));
                
                var unityConsoleConfig = serializedObject.FindProperty("_unityConsoleConfig").objectReferenceValue;
                if (unityConsoleConfig != null)
                {
                    DrawUnityConsoleConfigPreview(unityConsoleConfig as UnityConsoleTargetConfig);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Serilog File Target", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_serilogFileConfig"));
                
                var serilogFileConfig = serializedObject.FindProperty("_serilogFileConfig").objectReferenceValue;
                if (serilogFileConfig != null)
                {
                    DrawSerilogFileConfigPreview(serilogFileConfig as SerilogFileTargetConfig);
                }

                EditorGUILayout.Space();
                DrawConfigCreationHelp();
            }
        }

        private void DrawUnityConsoleConfigPreview(UnityConsoleTargetConfig config)
        {
            if (config == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Name: {config.TargetName}");
                EditorGUILayout.LabelField($"Enabled: {config.Enabled}");
                EditorGUILayout.LabelField($"Min Level: {config.MinimumLevel}");
                
                // Add any Unity Console specific properties here
                // For example, if it has color settings, formatting options, etc.
            }
        }

        private void DrawSerilogFileConfigPreview(SerilogFileTargetConfig config)
        {
            if (config == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Name: {config.TargetName}");
                EditorGUILayout.LabelField($"Enabled: {config.Enabled}");
                EditorGUILayout.LabelField($"Min Level: {config.MinimumLevel}");
                
                // Try to access file-specific properties through reflection or serialized properties
                // This is safer than assuming specific property names
                var serializedConfig = new SerializedObject(config);
                
                var filePathProp = serializedConfig.FindProperty("_filePath") ?? 
                                   serializedConfig.FindProperty("filePath") ?? 
                                   serializedConfig.FindProperty("FilePath");
                if (filePathProp != null)
                {
                    EditorGUILayout.LabelField($"File Path: {filePathProp.stringValue}");
                }
                
                var maxFileSizeProp = serializedConfig.FindProperty("_maxFileSizeBytes") ?? 
                                      serializedConfig.FindProperty("maxFileSizeBytes") ?? 
                                      serializedConfig.FindProperty("MaxFileSizeBytes");
                if (maxFileSizeProp != null && maxFileSizeProp.longValue > 0)
                {
                    var sizeMB = maxFileSizeProp.longValue / (1024 * 1024);
                    EditorGUILayout.LabelField($"Max File Size: {sizeMB}MB");
                }
                
                serializedConfig.Dispose();
            }
        }

        private void DrawConfigCreationHelp()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Target Configuration Help", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Create new configurations in Project view", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• Right-click → Create → AhBear Studios → Logging → Target Config", EditorStyles.miniLabel);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Unity Console Config"))
                    {
                        CreateNewTargetConfig<UnityConsoleTargetConfig>("_unityConsoleConfig");
                    }
                    
                    if (GUILayout.Button("Create Serilog File Config"))
                    {
                        CreateNewTargetConfig<SerilogFileTargetConfig>("_serilogFileConfig");
                    }
                }
            }
        }

        private void DrawAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);
            if (!_showAdvancedSettings) return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (Application.isPlaying)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Force Flush"))
                        {
                            var flushed = _target.Flush();
                            Debug.Log($"Flushed {flushed} messages");
                        }

                        if (GUILayout.Button("Set DontDestroyOnLoad"))
                        {
                            DontDestroyOnLoad(_target.gameObject);
                        }
                        
                        if (GUILayout.Button("Reset to Scene"))
                        {
                            var go = _target.gameObject;
                            var parent = go.transform.parent;
                            DestroyImmediate(go);
                            
                            var newGo = new GameObject("LogManager");
                            newGo.transform.SetParent(parent);
                            newGo.AddComponent<LogManagerComponent>();
                        }
                    }

                    EditorGUILayout.Space();
                    
                    // Global minimum level runtime adjustment
                    var newLevel = (LogLevel)EditorGUILayout.EnumPopup("Runtime Global Min Level", _target.GlobalMinimumLevel);
                    if (newLevel != _target.GlobalMinimumLevel)
                    {
                        _target.GlobalMinimumLevel = newLevel;
                    }
                    
                    // Auto flush runtime controls
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Enable Auto Flush"))
                        {
                            _target.SetAutoFlush(true, 1.0f);
                        }
                        
                        if (GUILayout.Button("Disable Auto Flush"))
                        {
                            _target.SetAutoFlush(false);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Advanced runtime controls available in Play Mode", MessageType.Info);
                }
            }
        }

        private void DrawRuntimeInfo()
        {
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Information", true);
            if (!_showRuntimeInfo || !Application.isPlaying) return;

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Core Status", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Initialized: {_target.IsInitialized}");
                    EditorGUILayout.LabelField($"Target Count: {_target.TargetCount}");
                    EditorGUILayout.LabelField($"Queued Messages: {_target.QueuedMessageCount}");
                    EditorGUILayout.LabelField($"Global Min Level: {_target.GlobalMinimumLevel}");
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Total Processed: {_target.TotalMessagesProcessed}");
                    EditorGUILayout.LabelField($"Flush Count: {_target.FlushCount}");
                    
                    if (_target.LogManagerService != null)
                    {
                        EditorGUILayout.LabelField($"Service Available: Yes");
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Service Available: No");
                    }
                    
                    if (_target.MessageBusService != null)
                    {
                        EditorGUILayout.LabelField($"Message Bus: Connected");
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Message Bus: Not Available");
                    }
                }
            }
        }

        private void DrawPerformanceMetrics()
        {
            _showPerformanceMetrics = EditorGUILayout.Foldout(_showPerformanceMetrics, "Performance Metrics", true);
            if (!_showPerformanceMetrics || !Application.isPlaying) return;

            using (new EditorGUI.IndentLevelScope())
            {
                var metrics = _target.GetPerformanceMetrics();
                
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var metric in metrics)
                    {
                        DisplayMetric(metrics, metric.Key, FormatMetricName(metric.Key));
                    }
                    
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Refresh Metrics"))
                    {
                        _lastMetrics = null; // Force refresh
                    }
                }
                
                // Performance warning checks
                if (metrics.TryGetValue("QueuedMessages", out var queuedObj) && 
                    queuedObj is int queued && queued > 100)
                {
                    EditorGUILayout.HelpBox($"High message queue count: {queued}. Consider increasing flush frequency.", MessageType.Warning);
                }
            }
        }

        private void DrawJobLoggerSection()
        {
            _showJobLoggerSection = EditorGUILayout.Foldout(_showJobLoggerSection, "Job Logger Management", true);
            if (!_showJobLoggerSection || !Application.isPlaying) return;

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Create Job Logger", EditorStyles.boldLabel);
                    
                    _testJobLoggerLevel = (LogLevel)EditorGUILayout.EnumPopup("Minimum Level", _testJobLoggerLevel);
                    _testJobLoggerTag = (Tagging.LogTag)EditorGUILayout.EnumPopup("Default Tag", _testJobLoggerTag);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Create Development"))
                        {
                            var logger = _target.CreateDevelopmentJobLogger(_testJobLoggerTag);
                            Debug.Log($"Created development job logger: {logger}");
                        }
                        
                        if (GUILayout.Button("Create Production"))
                        {
                            var logger = _target.CreateProductionJobLogger(_testJobLoggerTag);
                            Debug.Log($"Created production job logger: {logger}");
                        }
                        
                        if (GUILayout.Button("Create Console"))
                        {
                            var logger = _target.CreateConsoleJobLogger(_testJobLoggerTag);
                            Debug.Log($"Created console job logger: {logger}");
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Test Job Logger", EditorStyles.boldLabel);
                    _testJobLoggerMessage = EditorGUILayout.TextField("Test Message", _testJobLoggerMessage);
                    
                    if (GUILayout.Button("Send Test Job Message"))
                    {
                        try
                        {
                            var logger = _target.CreateJobLogger(_testJobLoggerLevel, _testJobLoggerTag);
                            Debug.Log($"Created job logger for testing: {_testJobLoggerMessage}");
                            EditorGUILayout.HelpBox("Job logger created. Check console for output.", MessageType.Info);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to create job logger: {ex}");
                        }
                    }
                }
            }
        }

        private void DrawTestMessageSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Test Messages", EditorStyles.boldLabel);
                
                _testMessageLevel = (LogLevel)EditorGUILayout.EnumPopup("Level", _testMessageLevel);
                _testMessageTag = (Tagging.LogTag)EditorGUILayout.EnumPopup("Tag", _testMessageTag);
                _testMessage = EditorGUILayout.TextField("Message", _testMessage);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Send Test Message"))
                    {
                        _target.Log(_testMessageLevel, _testMessage, _testMessageTag);
                    }
                    
                    if (GUILayout.Button("Send Exception Test"))
                    {
                        try
                        {
                            throw new InvalidOperationException("Test exception from editor");
                        }
                        catch (Exception ex)
                        {
                            _target.LogException(ex, "Test exception logging", _testMessageTag);
                        }
                    }
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Debug"))
                    {
                        _target.LogDebug("Test debug message", _testMessageTag);
                    }
                    
                    if (GUILayout.Button("Info"))
                    {
                        _target.LogInfo("Test info message", _testMessageTag);
                    }
                    
                    if (GUILayout.Button("Warning"))
                    {
                        _target.LogWarning("Test warning message", _testMessageTag);
                    }
                    
                    if (GUILayout.Button("Error"))
                    {
                        _target.LogError("Test error message", _testMessageTag);
                    }
                    
                    if (GUILayout.Button("Critical"))
                    {
                        _target.LogCritical("Test critical message", _testMessageTag);
                    }
                }
            }
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || _target == null) return;

            var currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastMetricsUpdateTime > METRICS_UPDATE_INTERVAL)
            {
                _lastMetricsUpdateTime = currentTime;
                
                var currentMetrics = _target.GetPerformanceMetrics();
                if (!MetricsEqual(_lastMetrics, currentMetrics))
                {
                    _lastMetrics = currentMetrics;
                    Repaint();
                }
            }
        }

        private void DisplayMetric(Dictionary<string, object> metrics, string key, string displayName)
        {
            if (metrics.TryGetValue(key, out var value))
            {
                var valueString = value?.ToString() ?? "null";
                
                // Highlight changing values
                var hasChanged = _lastMetrics != null && 
                    _lastMetrics.TryGetValue(key, out var lastValue) && 
                    !Equals(value, lastValue);
                
                if (hasChanged)
                {
                    var originalColor = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField($"{displayName}: {valueString}");
                    GUI.color = originalColor;
                }
                else
                {
                    EditorGUILayout.LabelField($"{displayName}: {valueString}");
                }
            }
        }

        private bool MetricsEqual(Dictionary<string, object> metrics1, Dictionary<string, object> metrics2)
        {
            if (metrics1 == null || metrics2 == null) return metrics1 == metrics2;
            if (metrics1.Count != metrics2.Count) return false;

            foreach (var kvp in metrics1)
            {
                if (!metrics2.TryGetValue(kvp.Key, out var value2) || !Equals(kvp.Value, value2))
                {
                    return false;
                }
            }

            return true;
        }

        private string FormatMetricName(string key)
        {
            // Convert camelCase to Title Case with spaces
            var result = "";
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]))
                {
                    result += " ";
                }
                result += key[i];
            }
            return result;
        }

        private void CreateNewTargetConfig<T>(string propertyName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            var path = EditorUtility.SaveFilePanelInProject(
                $"Save {typeof(T).Name}",
                $"New{typeof(T).Name}",
                "asset",
                $"Create a new {typeof(T).Name}");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var property = serializedObject.FindProperty(propertyName);
                property.objectReferenceValue = asset;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}