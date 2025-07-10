#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Profiling.Unity;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Unity.Configuration;
using UnityEngine.UI;

namespace AhBearStudios.Core.Profiling.Editor
{
    /// <summary>
    /// Custom editor for ProfileManager component with enhanced inspector UI
    /// </summary>
    [CustomEditor(typeof(ProfileManager))]
    public class ProfileManagerEditor : UnityEditor.Editor
    {
        private ProfileManager _profileManager;
        private bool _showMetrics = true;
        private bool _showSessions = true;
        private bool _showConfiguration = false;
        private Vector2 _metricsScrollPos;
        private Vector2 _sessionsScrollPos;
        
        private readonly Dictionary<string, bool> _metricFoldouts = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _sessionFoldouts = new Dictionary<string, bool>();
        
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;
        
        private void OnEnable()
        {
            _profileManager = target as ProfileManager;
        }
        
        public override void OnInspectorGUI()
        {
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }
            
            DrawHeader();
            DrawControls();
            
            EditorGUILayout.Space(10);
            
            if (_profileManager.IsEnabled)
            {
                DrawRuntimeData();
            }
            else
            {
                EditorGUILayout.HelpBox("Profiling is currently disabled. Enable profiling to see runtime data.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            DrawConfiguration();
            
            // Repaint regularly when profiling is active
            if (_profileManager.IsEnabled && Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// Initializes custom GUI styles
        /// </summary>
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25
            };
            
            _stylesInitialized = true;
        }
        
        /// <summary>
        /// Draws the header section
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.LabelField("Profile Manager", _headerStyle);
            
            if (Application.isPlaying)
            {
                string status = _profileManager.IsEnabled ? "ACTIVE" : "INACTIVE";
                var statusColor = _profileManager.IsEnabled ? Color.green : Color.red;
                
                var originalColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField($"Status: {status}", EditorStyles.boldLabel);
                GUI.color = originalColor;
                
                if (_profileManager.IsEnabled)
                {
                    float fps = 1.0f / Time.unscaledDeltaTime;
                    float frameTime = Time.unscaledDeltaTime * 1000f;
                    EditorGUILayout.LabelField($"FPS: {fps:F1} | Frame Time: {frameTime:F2}ms");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Status: EDITOR MODE", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the control buttons
        /// </summary>
        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = Application.isPlaying;
            
            if (_profileManager.IsEnabled)
            {
                if (GUILayout.Button("Stop Profiling", _buttonStyle))
                {
                    _profileManager.StopProfiling();
                }
            }
            else
            {
                if (GUILayout.Button("Start Profiling", _buttonStyle))
                {
                    _profileManager.StartProfiling();
                }
            }
            
            if (GUILayout.Button("Reset Stats", _buttonStyle))
            {
                _profileManager.ResetStats();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // Quick setup buttons
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Runtime UI", _buttonStyle))
            {
                AddRuntimeUI();
            }
            
            if (GUILayout.Button("Create Configuration", _buttonStyle))
            {
                CreateConfiguration();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the runtime data section
        /// </summary>
        private void DrawRuntimeData()
        {
            if (!Application.isPlaying)
                return;
                
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Runtime Data", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            _showMetrics = GUILayout.Toggle(_showMetrics, "Metrics", EditorStyles.miniButton, GUILayout.Width(60));
            _showSessions = GUILayout.Toggle(_showSessions, "Sessions", EditorStyles.miniButton, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            
            if (_showMetrics)
            {
                DrawMetricsData();
            }
            
            if (_showSessions)
            {
                DrawSessionsData();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the metrics data
        /// </summary>
        private void DrawMetricsData()
        {
            EditorGUILayout.LabelField("System Metrics", EditorStyles.boldLabel);
            
            if (_profileManager.SystemMetrics == null)
            {
                EditorGUILayout.HelpBox("System metrics not initialized", MessageType.Warning);
                return;
            }
            
            var metrics = _profileManager.SystemMetrics.GetAllMetrics();
            if (!metrics.Any())
            {
                EditorGUILayout.HelpBox("No system metrics available", MessageType.Info);
                return;
            }
            
            _metricsScrollPos = EditorGUILayout.BeginScrollView(_metricsScrollPos, GUILayout.MaxHeight(200));
            
            foreach (var metric in metrics)
            {
                DrawMetricItem(metric);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draws a single metric item
        /// </summary>
        /// <param name="metric">The system metric to draw</param>
        private void DrawMetricItem(SystemMetric metric)
        {
            var key = metric.Tag.FullName;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (!_metricFoldouts.ContainsKey(key))
                _metricFoldouts[key] = false;
                
            _metricFoldouts[key] = EditorGUILayout.Foldout(_metricFoldouts[key], metric.Tag.Name, true);
            
            GUILayout.FlexibleSpace();
            
            // Color code the value based on performance
            var valueColor = GetColorForValue(metric.LastValue, metric.Unit);
            var originalColor = GUI.color;
            GUI.color = valueColor;
            
            EditorGUILayout.LabelField($"{metric.LastValue:F2} {metric.Unit}", EditorStyles.boldLabel, GUILayout.Width(80));
            
            GUI.color = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            if (_metricFoldouts[key])
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"Average: {metric.AverageValue:F2} {metric.Unit}");
                EditorGUILayout.LabelField($"Maximum: {metric.MaxValue:F2} {metric.Unit}");
                EditorGUILayout.LabelField($"Samples: {metric.SampleCount}");
                
                // Draw a simple progress bar
                var rect = EditorGUILayout.GetControlRect(false, 4);
                var progress = GetNormalizedValue(metric.LastValue, metric.Unit);
                EditorGUI.ProgressBar(rect, progress, "");
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the sessions data
        /// </summary>
        private void DrawSessionsData()
        {
            EditorGUILayout.LabelField("Profiling Sessions", EditorStyles.boldLabel);
            
            var allMetrics = _profileManager.GetAllMetrics();
            if (!allMetrics.Any())
            {
                EditorGUILayout.HelpBox("No profiling sessions available", MessageType.Info);
                return;
            }
            
            _sessionsScrollPos = EditorGUILayout.BeginScrollView(_sessionsScrollPos, GUILayout.MaxHeight(200));
            
            foreach (var kvp in allMetrics.Take(20)) // Limit to 20 items for performance
            {
                DrawSessionItem(kvp.Key, kvp.Value);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draws a single session item
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="metrics">The metrics data</param>
        private void DrawSessionItem(ProfilerTag tag, DefaultMetricsData metrics)
        {
            var key = tag.FullName;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            if (!_sessionFoldouts.ContainsKey(key))
                _sessionFoldouts[key] = false;
                
            _sessionFoldouts[key] = EditorGUILayout.Foldout(_sessionFoldouts[key], tag.Name, true);
            
            GUILayout.FlexibleSpace();
            
            // Color code the value based on performance
            var valueColor = GetColorForValue(metrics.LastValue, "ms");
            var originalColor = GUI.color;
            GUI.color = valueColor;
            
            EditorGUILayout.LabelField($"{metrics.LastValue:F2} ms", EditorStyles.boldLabel, GUILayout.Width(80));
            
            GUI.color = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            if (_sessionFoldouts[key])
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"Average: {metrics.AverageValue:F2} ms");
                EditorGUILayout.LabelField($"Minimum: {metrics.MinValue:F2} ms");
                EditorGUILayout.LabelField($"Maximum: {metrics.MaxValue:F2} ms");
                EditorGUILayout.LabelField($"Samples: {metrics.SampleCount}");
                EditorGUILayout.LabelField($"Category: {tag.Category}");
                
                // Draw a simple progress bar
                var rect = EditorGUILayout.GetControlRect(false, 4);
                var progress = GetNormalizedValue(metrics.LastValue, "ms");
                EditorGUI.ProgressBar(rect, progress, "");
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the configuration section
        /// </summary>
        private void DrawConfiguration()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            _showConfiguration = EditorGUILayout.Foldout(_showConfiguration, "Configuration", true, EditorStyles.boldLabel);
            
            if (_showConfiguration)
            {
                DrawDefaultInspector();
                
                EditorGUILayout.Space(10);
                
                if (_profileManager.Configuration == null)
                {
                    EditorGUILayout.HelpBox("No configuration assigned. Click 'Create Configuration' to create one.", MessageType.Warning);
                    
                    if (GUILayout.Button("Create Configuration"))
                    {
                        CreateConfiguration();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Configuration Asset", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField("Config", _profileManager.Configuration, typeof(ProfilerConfiguration), false);
                    
                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    {
                        Selection.activeObject = _profileManager.Configuration;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Gets a color for a metric value based on performance thresholds
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="unit">The unit of measurement</param>
        /// <returns>Color representing performance level</returns>
        private Color GetColorForValue(double value, string unit)
        {
            if (unit.Equals("ms", StringComparison.OrdinalIgnoreCase))
            {
                if (value >= 33.33) return Color.red;    // 30 FPS or worse
                if (value >= 16.67) return Color.yellow; // 60 FPS or worse
                return Color.green;
            }
            else if (unit.Equals("KB", StringComparison.OrdinalIgnoreCase))
            {
                if (value >= 1000) return Color.red;   // 1MB+ allocation
                if (value >= 100) return Color.yellow; // 100KB+ allocation
                return Color.green;
            }
            
            return Color.white;
        }
        
        /// <summary>
        /// Gets a normalized value (0-1) for progress bars
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="unit">The unit of measurement</param>
        /// <returns>Normalized value between 0 and 1</returns>
        private float GetNormalizedValue(double value, string unit)
        {
            if (unit.Equals("ms", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Clamp01((float)value / 50f); // 50ms max for visualization
            }
            else if (unit.Equals("KB", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Clamp01((float)value / 2000f); // 2MB max for visualization
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Adds a runtime UI to the scene
        /// </summary>
        private void AddRuntimeUI()
        {
            var existingUI = FindFirstObjectByType<RuntimeProfilerUI>();
            if (existingUI != null)
            {
                EditorUtility.DisplayDialog("Runtime UI", "Runtime Profiler UI already exists in the scene.", "OK");
                Selection.activeGameObject = existingUI.gameObject;
                return;
            }
            
            var uiGO = CreateRuntimeUI();
            if (uiGO != null)
            {
                Selection.activeGameObject = uiGO;
                EditorUtility.DisplayDialog("Runtime UI", "Runtime Profiler UI has been added to the scene.", "OK");
            }
        }
        
        /// <summary>
        /// Creates the runtime UI GameObject
        /// </summary>
        /// <returns>The created GameObject</returns>
        private GameObject CreateRuntimeUI()
        {
            // Create the main UI GameObject
            var uiGO = new GameObject("Runtime Profiler UI");
            var canvas = uiGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            uiGO.AddComponent<CanvasScaler>();
            uiGO.AddComponent<GraphicRaycaster>();
            
            // Add the RuntimeProfilerUI component
            var runtimeUI = uiGO.AddComponent<RuntimeProfilerUI>();
            
            // Create basic UI structure
            CreateBasicUIStructure(uiGO, runtimeUI);
            
            return uiGO;
        }
        
        /// <summary>
        /// Creates basic UI structure for the runtime profiler
        /// </summary>
        /// <param name="parent">Parent GameObject</param>
        /// <param name="runtimeUI">Runtime UI component</param>
        private void CreateBasicUIStructure(GameObject parent, RuntimeProfilerUI runtimeUI)
        {
            // This would create the basic UI structure
            // For brevity, this is simplified - in a real implementation,
            // you would create the full UI hierarchy with panels, buttons, etc.
            
            var panel = new GameObject("Panel");
            panel.transform.SetParent(parent.transform, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.7f);
            rect.anchorMax = new Vector2(0.4f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        }
        
        /// <summary>
        /// Creates a profiler configuration asset
        /// </summary>
        private void CreateConfiguration()
        {
            var config = CreateInstance<ProfilerConfiguration>();
            config.InitializeDefaults();
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Profiler Configuration",
                "ProfilerConfiguration",
                "asset",
                "Please enter a file name to save the configuration to");
                
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                
                // Assign to the ProfileManager
                var profileManagerProperty = serializedObject.FindProperty("_configuration");
                profileManagerProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }
    }
    
    /// <summary>
    /// Custom editor for ProfiledGameObject component
    /// </summary>
    [CustomEditor(typeof(ProfiledGameObject))]
    public class ProfiledGameObjectEditor : UnityEditor.Editor
    {
        private ProfiledGameObject _profiledGameObject;
        private bool _showStats = false;
        
        private void OnEnable()
        {
            _profiledGameObject = target as ProfiledGameObject;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            if (Application.isPlaying && _profiledGameObject.ProfilingEnabled)
            {
                DrawRuntimeStats();
            }
            
            EditorGUILayout.Space(10);
            DrawUtilityButtons();
        }
        
        /// <summary>
        /// Draws runtime statistics for the profiled GameObject
        /// </summary>
        private void DrawRuntimeStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _showStats = EditorGUILayout.Foldout(_showStats, "Runtime Statistics", true, EditorStyles.boldLabel);
            
            if (_showStats)
            {
                var stats = _profiledGameObject.GetProfilingStats();
                
                if (stats.Any())
                {
                    foreach (var kvp in stats)
                    {
                        EditorGUILayout.LabelField(kvp.Key, kvp.Value.ToString());
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No statistics available");
                }
                
                if (GUILayout.Button("Refresh"))
                {
                    Repaint();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws utility buttons
        /// </summary>
        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add to All Children"))
            {
                AddToAllChildren();
            }
            
            if (GUILayout.Button("Remove from All Children"))
            {
                RemoveFromAllChildren();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Adds ProfiledGameObject to all child GameObjects
        /// </summary>
        private void AddToAllChildren()
        {
            var children = _profiledGameObject.GetComponentsInChildren<Transform>(true);
            int added = 0;
            
            foreach (var child in children)
            {
                if (child != _profiledGameObject.transform && child.GetComponent<ProfiledGameObject>() == null)
                {
                    child.gameObject.AddComponent<ProfiledGameObject>();
                    added++;
                }
            }
            
            EditorUtility.DisplayDialog("Add to Children", $"Added ProfiledGameObject to {added} child GameObjects.", "OK");
        }
        
        /// <summary>
        /// Removes ProfiledGameObject from all child GameObjects
        /// </summary>
        private void RemoveFromAllChildren()
        {
            var children = _profiledGameObject.GetComponentsInChildren<ProfiledGameObject>(true);
            int removed = 0;
            
            foreach (var child in children)
            {
                if (child != _profiledGameObject)
                {
                    DestroyImmediate(child);
                    removed++;
                }
            }
            
            EditorUtility.DisplayDialog("Remove from Children", $"Removed ProfiledGameObject from {removed} child GameObjects.", "OK");
        }
    }
    
    /// <summary>
    /// Menu items for profiler tools
    /// </summary>
    public static class ProfilerMenuItems
    {
        [MenuItem("Tools/AhBear Studios/Profiling/Create Profile Manager")]
        public static void CreateProfileManager()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<ProfileManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }
            
            var go = new GameObject("Profile Manager");
            go.AddComponent<ProfileManager>();
            Selection.activeGameObject = go;
        }
        
        [MenuItem("Tools/AhBear Studios/Profiling/Create Runtime UI")]
        public static void CreateRuntimeUI()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<RuntimeProfilerUI>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }
            
            // Implementation would create the runtime UI
            Debug.Log("Create Runtime UI - Implementation needed");
        }
        
        [MenuItem("Tools/AhBear Studios/Profiling/Add Profiling to Selected")]
        public static void AddProfilingToSelected()
        {
            var selected = Selection.gameObjects;
            int added = 0;
            
            foreach (var go in selected)
            {
                if (go.GetComponent<ProfiledGameObject>() == null)
                {
                    go.AddComponent<ProfiledGameObject>();
                    added++;
                }
            }
            
            EditorUtility.DisplayDialog("Add Profiling", $"Added profiling to {added} GameObjects.", "OK");
        }
        
        [MenuItem("Tools/AhBear Studios/Profiling/Remove Profiling from Selected")]
        public static void RemoveProfilingFromSelected()
        {
            var selected = Selection.gameObjects;
            int removed = 0;
            
            foreach (var go in selected)
            {
                var profiled = go.GetComponent<ProfiledGameObject>();
                if (profiled != null)
                {
                    UnityEngine.Object.DestroyImmediate(profiled);
                    removed++;
                }
            }
            
            EditorUtility.DisplayDialog("Remove Profiling", $"Removed profiling from {removed} GameObjects.", "OK");
        }
    }
}
#endif