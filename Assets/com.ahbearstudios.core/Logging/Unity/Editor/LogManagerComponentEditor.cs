using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Adapters;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom Unity Editor for the LogManagerComponent.
    /// Provides a rich interface for configuring and managing log settings with enhanced adapter monitoring.
    /// Uses compositional approach with ILogTargetConfig for better flexibility.
    /// </summary>
    [CustomEditor(typeof(LogManagerComponent))]
    public sealed class LogManagerComponentEditor : UnityEditor.Editor
    {
        #region Private Fields

        // Serialized properties
        private SerializedProperty _configProperty;
        private SerializedProperty _logTargetConfigsProperty;

        // Foldout states - cached to avoid GC
        private bool _showGeneralSettings = true;
        private bool _showTargetConfigs = true;
        private bool _showAdvancedSettings;
        private bool _showRuntimeInfo;
        private bool _showAdapterInfo;
        private bool _showPerformanceMetrics;

        // Component reference
        private LogManagerComponent _logManagerComponent;

        // Runtime monitoring cache
        private int _lastQueueCount;
        private int _lastTargetCount;
        private LogLevel _lastMinimumLevel;
        private double _lastUpdateTime;
        private Dictionary<string, object> _lastPerformanceMetrics;

        // UI Styles - memory-optimized
        private EditorStylesCache _stylesCache;

        // Reflection cache for type discovery
        private static Type[] _logTargetConfigTypes;
        private static readonly object _typeCacheLock = new object();

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Initialize the editor when enabled.
        /// </summary>
        private void OnEnable()
        {
            // Cache serialized properties
            _configProperty = serializedObject.FindProperty("_config");
            _logTargetConfigsProperty = serializedObject.FindProperty("_logTargetConfigs");

            // Cache component reference
            _logManagerComponent = target as LogManagerComponent;

            // Initialize update time and performance cache
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            _lastPerformanceMetrics = new Dictionary<string, object>();

            // Initialize styles cache
            _stylesCache = new EditorStylesCache();

            // Subscribe to editor updates for runtime monitoring
            EditorApplication.update += OnEditorUpdate;

            // Ensure type cache is initialized
            EnsureTypeCache();
        }

        /// <summary>
        /// Clean up when the editor is disabled.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _stylesCache?.Dispose();
        }

        #endregion

        #region GUI Rendering

        /// <summary>
        /// Main inspector GUI rendering.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (_logManagerComponent == null)
                return;

            serializedObject.Update();

            // Ensure styles are ready
            _stylesCache.EnsureInitialized();

            // Header with initialization status
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Log Manager Configuration", _stylesCache.HeaderStyle);
                GUILayout.FlexibleSpace();

                if (Application.isPlaying)
                {
                    var status = _logManagerComponent.IsInitialized ? "Initialized" : "Initializing";
                    var statusColor = _logManagerComponent.IsInitialized ? Color.green : Color.yellow;
                    var previousColor = GUI.color;
                    GUI.color = statusColor;
                    EditorGUILayout.LabelField($"Status: {status}", _stylesCache.StatusStyle);
                    GUI.color = previousColor;
                }
            }

            EditorGUILayout.Space();

            // Main configuration sections
            DrawGeneralSettings();
            EditorGUILayout.Space();

            DrawTargetConfigs();
            EditorGUILayout.Space();

            DrawAdvancedSettings();

            // Runtime section (play mode only)
            if (Application.isPlaying && _logManagerComponent.IsInitialized)
            {
                EditorGUILayout.Space();
                DrawRuntimeInfo();
                EditorGUILayout.Space();
                DrawAdapterInfo();
                EditorGUILayout.Space();
                DrawPerformanceMetrics();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the general settings section.
        /// </summary>
        private void DrawGeneralSettings()
        {
            _showGeneralSettings = EditorGUILayout.Foldout(_showGeneralSettings, "General Settings", true,
                _stylesCache.FoldoutStyle);

            if (!_showGeneralSettings)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                // Config asset reference
                EditorGUILayout.PropertyField(_configProperty,
                    new GUIContent("Manager Config", "Main configuration asset for the log manager"));

                var config = _configProperty.objectReferenceValue as LogManagerConfig;

                if (config == null)
                {
                    DrawConfigCreationHelp();
                }
                else
                {
                    DrawConfigPreview(config);
                }
            }
        }

        /// <summary>
        /// Draw help section for creating configuration.
        /// </summary>
        private void DrawConfigCreationHelp()
        {
            EditorGUILayout.HelpBox("No LogManagerConfig assigned. Default settings will be used.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Create New Config", _stylesCache.ButtonStyle, GUILayout.Width(150)))
                {
                    CreateNewManagerConfig();
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// Draw read-only preview of configuration settings.
        /// </summary>
        /// <param name="config">The configuration to preview.</param>
        private void DrawConfigPreview(LogManagerConfig config)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup(new GUIContent("Minimum Log Level"), config.MinimumLevel);
                EditorGUILayout.IntField(new GUIContent("Max Messages Per Batch"), config.MaxMessagesPerBatch);
                EditorGUILayout.IntField(new GUIContent("Initial Queue Capacity"), config.InitialQueueCapacity);
                EditorGUILayout.Toggle(new GUIContent("Auto Flush Enabled"), config.EnableAutoFlush);

                if (config.EnableAutoFlush)
                {
                    EditorGUILayout.FloatField(new GUIContent("Auto Flush Interval"), config.AutoFlushInterval);
                }

                EditorGUILayout.EnumPopup(new GUIContent("Default Tag"), config.DefaultTag);
            }

            // Edit config button
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Edit Config Asset", _stylesCache.ButtonStyle, GUILayout.Width(150)))
                {
                    Selection.activeObject = config;
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// Draw the target configurations section with compositional ILogTargetConfig support.
        /// </summary>
        private void DrawTargetConfigs()
        {
            _showTargetConfigs = EditorGUILayout.Foldout(_showTargetConfigs, "Log Target Configurations", true,
                _stylesCache.FoldoutStyle);

            if (!_showTargetConfigs)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                // Show array of target configs with improved validation
                DrawTargetConfigArray();

                EditorGUILayout.Space();

                // Action buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Target Config", _stylesCache.ButtonStyle))
                    {
                        ShowAddTargetMenu();
                    }

                    if (_logTargetConfigsProperty.arraySize > 0)
                    {
                        if (GUILayout.Button("Validate All", _stylesCache.ButtonStyle))
                        {
                            ValidateAllTargetConfigs();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw the target config array with enhanced validation and composition support.
        /// </summary>
        private void DrawTargetConfigArray()
        {
            EditorGUILayout.PropertyField(_logTargetConfigsProperty, new GUIContent("Target Configurations"), true);

            // Validate each config during drawing
            for (int i = 0; i < _logTargetConfigsProperty.arraySize; i++)
            {
                var elementProperty = _logTargetConfigsProperty.GetArrayElementAtIndex(i);
                var targetConfig = elementProperty.objectReferenceValue as ILogTargetConfig;

                if (targetConfig == null)
                {
                    EditorGUILayout.HelpBox($"Target Config {i} is null or invalid.", MessageType.Warning);
                }
                else if (!targetConfig.Enabled)
                {
                    EditorGUILayout.HelpBox($"Target Config {i} ({targetConfig.TargetName}) is disabled.",
                        MessageType.Info);
                }
            }
        }

        /// <summary>
        /// Draw the advanced settings section.
        /// </summary>
        private void DrawAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true,
                _stylesCache.FoldoutStyle);

            if (!_showAdvancedSettings)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.HelpBox(
                    "LogManagerComponent initializes the logging system when the scene starts. " +
                    "It manages the lifecycle of the JobLoggerManager and handles automatic flushing of logs.",
                    MessageType.Info);

                EditorGUILayout.Space();

                // Component lifetime management
                EditorGUILayout.LabelField("Component Lifetime", _stylesCache.SectionHeaderStyle);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Make Persistent", _stylesCache.ButtonStyle))
                    {
                        SetDontDestroyOnLoad();
                    }

                    if (GUILayout.Button("Reset to Scene", _stylesCache.ButtonStyle))
                    {
                        ResetToScene();
                    }
                }

                EditorGUILayout.Space();

                // Debug information
                if (Application.isPlaying && _logManagerComponent.IsInitialized)
                {
                    EditorGUILayout.LabelField("Debug Information", _stylesCache.SectionHeaderStyle);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        string debugInfo = _logManagerComponent.GetDebugInfo();
                        EditorGUILayout.TextArea(debugInfo, EditorStyles.wordWrappedLabel, GUILayout.Height(60));
                    }
                }
            }
        }

        /// <summary>
        /// Draw the runtime information section with enhanced monitoring.
        /// </summary>
        private void DrawRuntimeInfo()
        {
            _showRuntimeInfo =
                EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Information", true, _stylesCache.FoldoutStyle);

            if (!_showRuntimeInfo)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var loggerManager = _logManagerComponent.LoggerManager;

                if (loggerManager == null)
                {
                    EditorGUILayout.HelpBox("Logger Manager is not initialized.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawLoggerStatus(loggerManager);
                    EditorGUILayout.Space();
                    DrawRuntimeActions(loggerManager);
                    EditorGUILayout.Space();
                    DrawTestMessageSection(loggerManager);
                }
            }
        }

        private void DrawAdapterInfo()
        {
            _showAdapterInfo =
                EditorGUILayout.Foldout(_showAdapterInfo, "Adapter Information", true, _stylesCache.FoldoutStyle);

            if (!_showAdapterInfo)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Logging Adapters:", _stylesCache.SectionHeaderStyle);

                    // Unity Logger Adapter Status
                    var unityAdapter = _logManagerComponent.UnityLoggerAdapter;
                    if (unityAdapter != null)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Unity Logger Adapter:", _stylesCache.RuntimeInfoStyle);
                            GUILayout.FlexibleSpace();

                            string status = unityAdapter.IsRegisteredWithUnity ? "Registered" : "Not Registered";
                            var statusColor = unityAdapter.IsRegisteredWithUnity ? Color.green : Color.red;
                            var previousColor = GUI.color;
                            GUI.color = statusColor;
                            EditorGUILayout.LabelField(status, _stylesCache.StatusStyle);
                            GUI.color = previousColor;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Unity Logger Adapter: Not Available",
                            _stylesCache.RuntimeInfoStyle);
                    }

                    // Burst Logger Adapter Status
                    var burstAdapter = _logManagerComponent.BurstLoggerAdapter;
                    if (burstAdapter != null)
                    {
                        // Cast to the specific type to access the properties
                        var jobToBurstAdapter = burstAdapter as JobLoggerToBurstAdapter;
                        if (jobToBurstAdapter != null)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Burst Logger Adapter:", _stylesCache.RuntimeInfoStyle);
                                GUILayout.FlexibleSpace();

                                string status = jobToBurstAdapter.IsEnabledGlobal ? "Enabled" : "Disabled";
                                var statusColor = jobToBurstAdapter.IsEnabledGlobal ? Color.green : Color.yellow;
                                var previousColor = GUI.color;
                                GUI.color = statusColor;
                                EditorGUILayout.LabelField(status, _stylesCache.StatusStyle);
                                GUI.color = previousColor;
                            }

                            EditorGUILayout.LabelField($"Minimum Level: {jobToBurstAdapter.MinimumLevel}",
                                _stylesCache.RuntimeInfoStyle);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Burst Logger Adapter: Available (Unknown Type)",
                                _stylesCache.RuntimeInfoStyle);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Burst Logger Adapter: Not Available",
                            _stylesCache.RuntimeInfoStyle);
                    }

                    EditorGUILayout.Space();

                    // Adapter Control Buttons
                    EditorGUILayout.LabelField("Adapter Controls:", _stylesCache.SectionHeaderStyle);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (unityAdapter != null)
                        {
                            if (GUILayout.Button("Re-register Unity Logger", _stylesCache.ButtonStyle))
                            {
                                unityAdapter.RegisterWithUnity();
                            }

                            if (GUILayout.Button("Restore Original Handler", _stylesCache.ButtonStyle))
                            {
                                unityAdapter.RestoreOriginalHandler();
                            }
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (burstAdapter != null)
                        {
                            var jobToBurstAdapter = burstAdapter as JobLoggerToBurstAdapter;
                            if (jobToBurstAdapter != null)
                            {
                                if (GUILayout.Button(
                                        jobToBurstAdapter.IsEnabledGlobal
                                            ? "Disable Burst Adapter"
                                            : "Enable Burst Adapter", _stylesCache.ButtonStyle))
                                {
                                    jobToBurstAdapter.IsEnabledGlobal = !jobToBurstAdapter.IsEnabledGlobal;
                                }

                                if (GUILayout.Button("Flush Burst Adapter", _stylesCache.ButtonStyle))
                                {
                                    jobToBurstAdapter.Flush();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw performance metrics section showing detailed logging statistics.
        /// </summary>
        private void DrawPerformanceMetrics()
        {
            _showPerformanceMetrics = EditorGUILayout.Foldout(_showPerformanceMetrics, "Performance Metrics", true,
                _stylesCache.FoldoutStyle);

            if (!_showPerformanceMetrics)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Logging Performance:", _stylesCache.SectionHeaderStyle);

                    var metrics = _logManagerComponent.GetPerformanceMetrics();
                    if (metrics != null && metrics.Count > 0)
                    {
                        // Display core metrics
                        DisplayMetric(metrics, "TotalMessagesProcessed", "Total Messages Processed");
                        DisplayMetric(metrics, "FlushCount", "Flush Count");
                        DisplayMetric(metrics, "QueuedMessageCount", "Queued Messages");
                        DisplayMetric(metrics, "TargetCount", "Active Targets");

                        // Display performance ratios if available
                        if (metrics.ContainsKey("TotalMessagesProcessed") && metrics.ContainsKey("FlushCount"))
                        {
                            var totalMessages = Convert.ToInt32(metrics["TotalMessagesProcessed"]);
                            var flushCount = Convert.ToInt32(metrics["FlushCount"]);
                            if (flushCount > 0)
                            {
                                float avgMessagesPerFlush = (float)totalMessages / flushCount;
                                EditorGUILayout.LabelField($"Avg Messages per Flush: {avgMessagesPerFlush:F1}",
                                    _stylesCache.RuntimeInfoStyle);
                            }
                        }

                        // Display any custom metrics
                        foreach (var metric in metrics)
                        {
                            if (!IsStandardMetric(metric.Key))
                            {
                                EditorGUILayout.LabelField($"{metric.Key}: {metric.Value}",
                                    _stylesCache.RuntimeInfoStyle);
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No performance metrics available", _stylesCache.RuntimeInfoStyle);
                    }

                    EditorGUILayout.Space();

                    // Performance control buttons
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Reset Metrics", _stylesCache.ButtonStyle))
                        {
                            _logManagerComponent.ResetPerformanceMetrics();
                        }

                        if (GUILayout.Button("Force Flush", _stylesCache.ButtonStyle))
                        {
                            var processed = _logManagerComponent.Flush();
                            EditorUtility.DisplayDialog("Flush Complete", $"Processed {processed} messages.", "OK");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw logger status information.
        /// </summary>
        /// <param name="loggerManager">The logger manager instance.</param>
        private void DrawLoggerStatus(JobLoggerManager loggerManager)
        {
            EditorGUILayout.LabelField("Logger Status:", _stylesCache.SectionHeaderStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Queued Messages: {loggerManager.QueuedMessageCount}",
                    _stylesCache.RuntimeInfoStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Active Targets: {loggerManager.TargetCount}",
                    _stylesCache.RuntimeInfoStyle);
            }

            EditorGUILayout.LabelField($"Global Minimum Level: {loggerManager.GlobalMinimumLevel}",
                _stylesCache.RuntimeInfoStyle);
        }

        /// <summary>
        /// Draw runtime action buttons.
        /// </summary>
        /// <param name="loggerManager">The logger manager instance.</param>
        private void DrawRuntimeActions(JobLoggerManager loggerManager)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Flush Logs Now", _stylesCache.ButtonStyle))
                {
                    FlushLogsWithFeedback(loggerManager);
                }

                if (GUILayout.Button("Update Targets", _stylesCache.ButtonStyle))
                {
                    loggerManager.UpdateTargetMinimumLevels();
                    EditorUtility.DisplayDialog("Update Complete", "Target minimum levels have been updated.", "OK");
                }
            }
        }

        /// <summary>
        /// Draw test message section.
        /// </summary>
        /// <param name="loggerManager">The logger manager instance.</param>
        private void DrawTestMessageSection(JobLoggerManager loggerManager)
        {
            EditorGUILayout.LabelField("Send Test Message:", _stylesCache.SectionHeaderStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Debug", _stylesCache.ButtonStyle))
                    SendTestMessage(loggerManager, LogLevel.Debug);

                if (GUILayout.Button("Info", _stylesCache.ButtonStyle))
                    SendTestMessage(loggerManager, LogLevel.Info);

                if (GUILayout.Button("Warning", _stylesCache.ButtonStyle))
                    SendTestMessage(loggerManager, LogLevel.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Error", _stylesCache.ButtonStyle))
                    SendTestMessage(loggerManager, LogLevel.Error);

                if (GUILayout.Button("Critical", _stylesCache.ButtonStyle))
                    SendTestMessage(loggerManager, LogLevel.Critical);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Updates the editor UI periodically during play mode to show latest statistics.
        /// Optimized to minimize GC allocations and unnecessary repaints.
        /// </summary>
        private void OnEditorUpdate()
        {
            // Throttle updates to every 0.5 seconds
            const double updateInterval = 0.5;
            if (EditorApplication.timeSinceStartup - _lastUpdateTime < updateInterval)
                return;

            _lastUpdateTime = EditorApplication.timeSinceStartup;

            if (!Application.isPlaying || !_logManagerComponent?.IsInitialized == true)
                return;

            var loggerManager = _logManagerComponent.LoggerManager;
            if (loggerManager == null)
                return;

            // Cache current values
            int queueCount = loggerManager.QueuedMessageCount;
            int targetCount = loggerManager.TargetCount;
            LogLevel minimumLevel = loggerManager.GlobalMinimumLevel;
            var currentMetrics = _logManagerComponent.GetPerformanceMetrics();

            // Check if performance metrics changed
            bool metricsChanged = false;
            if (currentMetrics != null && _lastPerformanceMetrics != null)
            {
                metricsChanged = !MetricsEqual(currentMetrics, _lastPerformanceMetrics);
                if (metricsChanged)
                {
                    _lastPerformanceMetrics.Clear();
                    foreach (var metric in currentMetrics)
                    {
                        _lastPerformanceMetrics[metric.Key] = metric.Value;
                    }
                }
            }

            // Only repaint if something changed
            if (_lastQueueCount != queueCount ||
                _lastTargetCount != targetCount ||
                _lastMinimumLevel != minimumLevel ||
                metricsChanged)
            {
                _lastQueueCount = queueCount;
                _lastTargetCount = targetCount;
                _lastMinimumLevel = minimumLevel;
                Repaint();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Displays a performance metric with proper formatting.
        /// </summary>
        /// <param name="metrics">The metrics dictionary.</param>
        /// <param name="key">The metric key.</param>
        /// <param name="displayName">The display name for the metric.</param>
        private void DisplayMetric(Dictionary<string, object> metrics, string key, string displayName)
        {
            if (metrics.ContainsKey(key))
            {
                EditorGUILayout.LabelField($"{displayName}: {metrics[key]}", _stylesCache.RuntimeInfoStyle);
            }
        }

        /// <summary>
        /// Checks if a metric key is a standard metric.
        /// </summary>
        /// <param name="key">The metric key to check.</param>
        /// <returns>True if it's a standard metric, false otherwise.</returns>
        private static bool IsStandardMetric(string key)
        {
            return key == "TotalMessagesProcessed" ||
                   key == "FlushCount" ||
                   key == "QueuedMessageCount" ||
                   key == "TargetCount";
        }

        /// <summary>
        /// Compares two metrics dictionaries for equality.
        /// </summary>
        /// <param name="metrics1">First metrics dictionary.</param>
        /// <param name="metrics2">Second metrics dictionary.</param>
        /// <returns>True if the dictionaries are equal, false otherwise.</returns>
        private static bool MetricsEqual(Dictionary<string, object> metrics1, Dictionary<string, object> metrics2)
        {
            if (metrics1.Count != metrics2.Count)
                return false;

            foreach (var kvp in metrics1)
            {
                if (!metrics2.ContainsKey(kvp.Key) || !Equals(kvp.Value, metrics2[kvp.Key]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Ensures the type cache for ILogTargetConfig implementations is initialized.
        /// Thread-safe and called only once.
        /// </summary>
        private static void EnsureTypeCache()
        {
            if (_logTargetConfigTypes != null)
                return;

            lock (_typeCacheLock)
            {
                if (_logTargetConfigTypes != null)
                    return;

                _logTargetConfigTypes = GetConcreteLogTargetConfigTypes().ToArray();
            }
        }

        /// <summary>
        /// Gets all concrete types that implement ILogTargetConfig.
        /// Uses composition-friendly approach to find all implementations.
        /// </summary>
        /// <returns>A collection of ILogTargetConfig implementation types.</returns>
        private static IEnumerable<Type> GetConcreteLogTargetConfigTypes()
        {
            var targetConfigInterface = typeof(ILogTargetConfig);
            var scriptableObjectType = typeof(ScriptableObject);

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Enumerable.Empty<Type>();
                    }
                })
                .Where(type =>
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    targetConfigInterface.IsAssignableFrom(type) &&
                    scriptableObjectType.IsAssignableFrom(type))
                .OrderBy(type => type.Name);
        }

        /// <summary>
        /// Creates a new LogManagerConfig asset with safe defaults.
        /// </summary>
        private void CreateNewManagerConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Log Manager Configuration",
                "LogManagerConfig",
                "asset",
                "Choose a location to save the log manager configuration.");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // Create and configure the asset
                var config = CreateInstance<LogManagerConfig>();

                // Configure with safe defaults via SerializedObject
                var configSO = new SerializedObject(config);
                configSO.FindProperty("_minimumLevel").enumValueIndex = (int)LogLevel.Info;
                configSO.FindProperty("_maxMessagesPerBatch").intValue = 200;
                configSO.FindProperty("_initialQueueCapacity").intValue = 64;
                configSO.FindProperty("_enableAutoFlush").boolValue = true;
                configSO.FindProperty("_autoFlushInterval").floatValue = 0.5f;
                configSO.FindProperty("_defaultTag").enumValueIndex = (int)Tagging.LogTag.Default;
                configSO.ApplyModifiedProperties();

                // Save and assign
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _configProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();

                EditorGUIUtility.PingObject(config);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create LogManagerConfig: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a context menu to add a new target config with composition support.
        /// </summary>
        private void ShowAddTargetMenu()
        {
            var menu = new GenericMenu();

            if (_logTargetConfigTypes == null || _logTargetConfigTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No target config types found"));
            }
            else
            {
                foreach (var configType in _logTargetConfigTypes)
                {
                    string menuName = FormatTypeNameForMenu(configType);
                    menu.AddItem(new GUIContent(menuName), false, () => CreateLogTargetConfig(configType));
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Creates a user-friendly menu name from a type.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <returns>A formatted menu name.</returns>
        private static string FormatTypeNameForMenu(Type type)
        {
            string name = type.Name;

            // Remove "Config" suffix
            if (name.EndsWith("Config"))
            {
                name = name.Substring(0, name.Length - 6);
            }

            // Add spaces before capital letters using efficient string manipulation
            var result = new System.Text.StringBuilder(name.Length + 10);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    result.Append(' ');
                }

                result.Append(name[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Creates a new log target config asset using composition approach.
        /// </summary>
        /// <param name="configType">The type of config to create.</param>
        private void CreateLogTargetConfig(Type configType)
        {
            string typeName = configType.Name;
            if (typeName.EndsWith("Config"))
            {
                typeName = typeName.Substring(0, typeName.Length - 6);
            }

            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {typeName} Configuration",
                typeName,
                "asset",
                $"Choose a location to save the {typeName} configuration.");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // Create using composition-friendly approach
                var config = CreateInstance(configType) as ScriptableObject;
                if (config is ILogTargetConfig targetConfig)
                {
                    // Set default name if available
                    if (!string.IsNullOrEmpty(targetConfig.TargetName))
                    {
                        targetConfig.TargetName = typeName;
                    }

                    AssetDatabase.CreateAsset(config, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // Add to array
                    int index = _logTargetConfigsProperty.arraySize;
                    _logTargetConfigsProperty.arraySize++;
                    _logTargetConfigsProperty.GetArrayElementAtIndex(index).objectReferenceValue = config;
                    serializedObject.ApplyModifiedProperties();

                    EditorGUIUtility.PingObject(config);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create {configType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates all target configurations and shows results.
        /// </summary>
        private void ValidateAllTargetConfigs()
        {
            int validCount = 0;
            int invalidCount = 0;
            var issues = new List<string>();

            for (int i = 0; i < _logTargetConfigsProperty.arraySize; i++)
            {
                var elementProperty = _logTargetConfigsProperty.GetArrayElementAtIndex(i);
                var targetConfig = elementProperty.objectReferenceValue as ILogTargetConfig;

                if (targetConfig == null)
                {
                    invalidCount++;
                    issues.Add($"Config {i}: Null or invalid reference");
                }
                else if (string.IsNullOrEmpty(targetConfig.TargetName))
                {
                    invalidCount++;
                    issues.Add($"Config {i}: Missing target name");
                }
                else
                {
                    validCount++;
                }
            }

            string message = $"Validation Results:\n• Valid configs: {validCount}\n• Invalid configs: {invalidCount}";
            if (issues.Count > 0)
            {
                message += "\n\nIssues found:\n" + string.Join("\n", issues);
            }

            EditorUtility.DisplayDialog("Target Config Validation", message, "OK");
        }

        /// <summary>
        /// Sets the target component to DontDestroyOnLoad with proper validation.
        /// </summary>
        private void SetDontDestroyOnLoad()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Cannot Set Persistent",
                    "DontDestroyOnLoad can only be set during play mode.",
                    "OK");
                return;
            }

            if (_logManagerComponent?.gameObject != null)
            {
                DontDestroyOnLoad(_logManagerComponent.gameObject);
                EditorUtility.DisplayDialog(
                    "Component Made Persistent",
                    "The LogManagerComponent will now persist between scene loads.",
                    "OK");
            }
        }

        /// <summary>
        /// Resets the component to be destroyed on scene load.
        /// </summary>
        private void ResetToScene()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Reset to Scene",
                    "This setting only affects play mode behavior.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Reset to Scene",
                "Component will be destroyed when the scene unloads. Restart play mode to apply.",
                "OK");
        }

        /// <summary>
        /// Flushes logs and provides user feedback.
        /// </summary>
        /// <param name="manager">The logger manager instance.</param>
        private void FlushLogsWithFeedback(JobLoggerManager manager)
        {
            try
            {
                int processed = manager.Flush();
                EditorUtility.DisplayDialog("Flush Result", $"Processed {processed} log messages.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to flush logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a test message through the logger with proper error handling.
        /// </summary>
        /// <param name="manager">The logger manager instance.</param>
        /// <param name="level">The log level to use.</param>
        private void SendTestMessage(JobLoggerManager manager, LogLevel level)
        {
            if (manager == null)
                return;

            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string message = $"Test message from Editor at {timestamp}";

                switch (level)
                {
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
                    default:
                        manager.Log(level, Tagging.LogTag.Default, message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send test message: {ex.Message}");
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Memory-optimized cache for editor styles to avoid GC allocations.
        /// Implements IDisposable for proper resource cleanup.
        /// </summary>
        private sealed class EditorStylesCache : IDisposable
        {
            public GUIStyle HeaderStyle { get; private set; }
            public GUIStyle SectionHeaderStyle { get; private set; }
            public GUIStyle RuntimeInfoStyle { get; private set; }
            public GUIStyle StatusStyle { get; private set; }
            public GUIStyle ButtonStyle { get; private set; }
            public GUIStyle FoldoutStyle { get; private set; }

            private bool _initialized;
            private bool _disposed;

            /// <summary>
            /// Ensures styles are initialized only when needed.
            /// </summary>
            public void EnsureInitialized()
            {
                if (_initialized || _disposed)
                    return;

                InitializeStyles();
                _initialized = true;
            }

            /// <summary>
            /// Initialize all styles with optimized settings.
            /// </summary>
            private void InitializeStyles()
            {
                HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 10, 10)
                };

                SectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(0, 0, 5, 2)
                };

                RuntimeInfoStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    padding = new RectOffset(5, 5, 2, 2)
                };

                StatusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.green : Color.blue }
                };

                ButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(10, 10, 4, 4),
                    margin = new RectOffset(2, 2, 2, 2)
                };

                FoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            /// <summary>
            /// Cleanup resources.
            /// </summary>
            public void Dispose()
            {
                if (_disposed)
                    return;

                // GUIStyle doesn't need explicit disposal in Unity, but we mark as disposed
                _disposed = true;
            }
        }

        #endregion
    }
}