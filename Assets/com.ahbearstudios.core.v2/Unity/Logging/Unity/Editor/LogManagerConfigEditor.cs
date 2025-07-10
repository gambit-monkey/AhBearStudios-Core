using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Logging.Configuration;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Unity.Editor
{
    /// <summary>
    /// Custom editor for LogManagerConfig providing enhanced Unity Inspector experience.
    /// Supports compositional ILogTargetConfig interface for flexible target configuration.
    /// Optimized for memory efficiency and minimal GC allocations.
    /// </summary>
    [CustomEditor(typeof(LogManagerConfig))]
    public sealed class LogManagerConfigEditor : UnityEditor.Editor
    {
        #region Private Fields
        
        // Serialized properties - cached for performance
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
        
        // UI state - memory optimized
        private bool _showAdvancedSettings;
        private bool _showTargetValidation = true;
        
        // Cached references
        private LogManagerConfig _config;
        
        // Type discovery cache - thread-safe lazy initialization
        private static Type[] _logTargetConfigTypes;
        private static readonly object _typeCacheLock = new object();
        
        // Styles cache
        private EditorStylesCache _stylesCache;
        
        // Validation cache
        private ValidationResult _lastValidationResult;
        private double _lastValidationTime;
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// Initialize editor properties and caches.
        /// </summary>
        private void OnEnable()
        {
            // Cache the target reference
            _config = target as LogManagerConfig;
            
            // Find and cache all serialized properties
            CacheSerializedProperties();
            
            // Initialize styles cache
            _stylesCache = new EditorStylesCache();
            
            // Ensure type cache is populated
            EnsureTypeCache();
        }
        
        /// <summary>
        /// Clean up resources when editor is disabled.
        /// </summary>
        private void OnDisable()
        {
            _stylesCache?.Dispose();
        }
        
        #endregion
        
        #region Inspector GUI
        
        /// <summary>
        /// Main inspector GUI rendering with optimized layout.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (_config == null)
                return;
                
            serializedObject.Update();
            
            // Ensure styles are initialized
            _stylesCache.EnsureInitialized();
            
            // Main content sections
            DrawHeader();
            DrawConfigurationInfo();
            DrawGlobalSettings();
            DrawLogTargets();
            DrawAdvancedSettings();
            DrawValidationSection();
            DrawActionButtons();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Draw the editor header with configuration overview.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Log Manager Configuration", _stylesCache.HeaderStyle);
                
                GUILayout.FlexibleSpace();
                
                // Show configuration status
                var statusText = GetConfigurationStatusText();
                var statusStyle = GetConfigurationStatusStyle();
                EditorGUILayout.LabelField(statusText, statusStyle);
            }
            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Draw configuration information summary.
        /// </summary>
        private void DrawConfigurationInfo()
        {
            using (new EditorGUILayout.VerticalScope(_stylesCache.SectionBoxStyle))
            {
                EditorGUILayout.LabelField("Configuration Info", _stylesCache.SectionHeaderStyle);
                
                EditorGUILayout.PropertyField(_configurationName, new GUIContent("Configuration Name"));
                
                // Show summary statistics
                DrawConfigurationSummary();
            }
            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Draw configuration summary statistics.
        /// </summary>
        private void DrawConfigurationSummary()
        {
            var enabledCount = GetEnabledTargetCount();
            var totalCount = GetTotalTargetCount();
            
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Target Summary", $"{enabledCount} enabled of {totalCount} total");
                EditorGUILayout.LabelField("Min Log Level", _config.MinimumLevel.ToString());
                
                var flushText = _config.EnableAutoFlush 
                    ? $"Every {_config.AutoFlushInterval:F2}s" 
                    : "Disabled";
                EditorGUILayout.LabelField("Auto Flush", flushText);
            }
        }
        
        /// <summary>
        /// Draw global logging settings section.
        /// </summary>
        private void DrawGlobalSettings()
        {
            using (new EditorGUILayout.VerticalScope(_stylesCache.SectionBoxStyle))
            {
                EditorGUILayout.LabelField("Global Logging Settings", _stylesCache.SectionHeaderStyle);
                
                DrawBasicSettings();
                EditorGUILayout.Space();
                DrawAutoFlushSettings();
            }
            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Draw basic logging settings.
        /// </summary>
        private void DrawBasicSettings()
        {
            EditorGUILayout.PropertyField(_minimumLevel, 
                new GUIContent("Minimum Log Level", "The minimum log level that will be processed"));
            
            EditorGUILayout.PropertyField(_maxMessagesPerBatch, 
                new GUIContent("Max Messages Per Batch", "Maximum number of messages to process per batch"));
            
            EditorGUILayout.PropertyField(_initialQueueCapacity, 
                new GUIContent("Initial Queue Capacity", "Initial capacity of the log message queue"));
            
            EditorGUILayout.PropertyField(_defaultTag, 
                new GUIContent("Default Tag", "Default tag to use when no tag is specified"));
        }
        
        /// <summary>
        /// Draw auto-flush configuration settings.
        /// </summary>
        private void DrawAutoFlushSettings()
        {
            EditorGUILayout.PropertyField(_enableAutoFlush, 
                new GUIContent("Enable Auto Flush", "Enable automatic flushing of logs"));
            
            if (_enableAutoFlush.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_autoFlushInterval, 
                        new GUIContent("Auto Flush Interval", "Interval in seconds between auto-flush operations"));
                    
                    // Validate interval
                    if (_autoFlushInterval.floatValue <= 0.0f)
                    {
                        EditorGUILayout.HelpBox("Auto flush interval must be greater than 0.", MessageType.Warning);
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw log targets section with compositional ILogTargetConfig support.
        /// </summary>
        private void DrawLogTargets()
        {
            using (new EditorGUILayout.VerticalScope(_stylesCache.SectionBoxStyle))
            {
                DrawLogTargetsHeader();
                DrawLogTargetsList();
            }
            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Draw log targets section header with add button.
        /// </summary>
        private void DrawLogTargetsHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Log Targets", _stylesCache.SectionHeaderStyle);
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Add Target", _stylesCache.ButtonStyle, GUILayout.Width(100)))
                {
                    ShowAddTargetMenu();
                }
                
                if (GetTotalTargetCount() > 0)
                {
                    if (GUILayout.Button("Validate All", _stylesCache.ButtonStyle, GUILayout.Width(80)))
                    {
                        ValidateAllTargets();
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw the list of configured log targets.
        /// </summary>
        private void DrawLogTargetsList()
        {
            var totalCount = GetTotalTargetCount();
            
            if (totalCount == 0)
            {
                EditorGUILayout.HelpBox("No log targets configured. Add targets to enable logging output.", MessageType.Warning);
                return;
            }
            
            // Draw each target with enhanced UI
            for (int i = 0; i < totalCount; i++)
            {
                DrawLogTargetItem(i);
            }
        }
        
        /// <summary>
        /// Draw individual log target item with composition support.
        /// </summary>
        /// <param name="index">The target index.</param>
        private void DrawLogTargetItem(int index)
        {
            var targetProperty = _logTargets.GetArrayElementAtIndex(index);
            var targetConfig = targetProperty.objectReferenceValue as ILogTargetConfig;
            
            using (new EditorGUILayout.VerticalScope(_stylesCache.TargetItemStyle))
            {
                if (targetConfig == null)
                {
                    DrawNullTargetItem(index);
                    return;
                }
                
                DrawValidTargetItem(targetConfig, index);
            }
        }
        
        /// <summary>
        /// Draw UI for null/invalid target configuration.
        /// </summary>
        /// <param name="index">The target index.</param>
        private void DrawNullTargetItem(int index)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox($"Target {index} is null or invalid.", MessageType.Error);
                
                GUI.color = Color.red;
                if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(20)))
                {
                    RemoveTargetAtIndex(index);
                }
                GUI.color = Color.white;
            }
        }
        
        /// <summary>
        /// Draw UI for valid target configuration.
        /// </summary>
        /// <param name="targetConfig">The target configuration.</param>
        /// <param name="index">The target index.</param>
        private void DrawValidTargetItem(ILogTargetConfig targetConfig, int index)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Enabled toggle
                var wasEnabled = targetConfig.Enabled;
                var isEnabled = EditorGUILayout.Toggle(wasEnabled, GUILayout.Width(20));
                
                if (isEnabled != wasEnabled)
                {
                    targetConfig.Enabled = isEnabled;
                    EditorUtility.SetDirty(targetConfig as UnityEngine.Object);
                }
                
                // Target information
                DrawTargetInfo(targetConfig, isEnabled);
                
                // Action buttons
                DrawTargetActionButtons(targetConfig, index);
            }
            
            // Show target details when enabled
            if (targetConfig.Enabled)
            {
                DrawTargetDetails(targetConfig);
            }
        }
        
        /// <summary>
        /// Draw target information display.
        /// </summary>
        /// <param name="targetConfig">The target configuration.</param>
        /// <param name="isEnabled">Whether the target is enabled.</param>
        private void DrawTargetInfo(ILogTargetConfig targetConfig, bool isEnabled)
        {
            var targetName = string.IsNullOrEmpty(targetConfig.TargetName) 
                ? "Unnamed Target" 
                : targetConfig.TargetName;
            
            var targetType = GetFriendlyTypeName(targetConfig.GetType());
            var displayText = $"{targetName} ({targetType})";
            
            var labelStyle = isEnabled 
                ? _stylesCache.EnabledLabelStyle 
                : _stylesCache.DisabledLabelStyle;
            
            EditorGUILayout.LabelField(displayText, labelStyle);
        }
        
        /// <summary>
        /// Draw action buttons for target configuration.
        /// </summary>
        /// <param name="targetConfig">The target configuration.</param>
        /// <param name="index">The target index.</param>
        private void DrawTargetActionButtons(ILogTargetConfig targetConfig, int index)
        {
            // Edit button
            if (GUILayout.Button("Edit", _stylesCache.ButtonStyle, GUILayout.Width(50)))
            {
                Selection.activeObject = targetConfig as UnityEngine.Object;
            }
            
            // Remove button
            GUI.color = Color.red;
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                var targetName = string.IsNullOrEmpty(targetConfig.TargetName) 
                    ? "this target" 
                    : $"'{targetConfig.TargetName}'";
                
                if (EditorUtility.DisplayDialog("Remove Log Target", 
                    $"Are you sure you want to remove {targetName}?", "Remove", "Cancel"))
                {
                    RemoveTargetAtIndex(index);
                }
            }
            GUI.color = Color.white;
        }
        
        /// <summary>
        /// Draw detailed information for enabled targets.
        /// </summary>
        /// <param name="targetConfig">The target configuration.</param>
        private void DrawTargetDetails(ILogTargetConfig targetConfig)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Min Level", targetConfig.MinimumLevel.ToString());
                
                if (targetConfig.IncludedTags?.Length > 0)
                {
                    EditorGUILayout.LabelField("Included Tags", string.Join(", ", targetConfig.IncludedTags));
                }
                
                if (targetConfig.ExcludedTags?.Length > 0)
                {
                    EditorGUILayout.LabelField("Excluded Tags", string.Join(", ", targetConfig.ExcludedTags));
                }
            }
        }
        
        /// <summary>
        /// Draw advanced settings section with foldout.
        /// </summary>
        private void DrawAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, 
                "Advanced Settings", true, _stylesCache.FoldoutStyle);
            
            if (!_showAdvancedSettings)
                return;
                
            using (new EditorGUILayout.VerticalScope(_stylesCache.SectionBoxStyle))
            {
                EditorGUILayout.PropertyField(_enableMessageBusIntegration, 
                    new GUIContent("Enable Message Bus Integration", 
                        "Whether to enable message bus integration for logging events"));
                
                EditorGUILayout.PropertyField(_validateTargetsOnStartup, 
                    new GUIContent("Validate Targets On Startup", 
                        "Whether to validate log targets during initialization"));
                
                EditorGUILayout.PropertyField(_autoCreateDirectories, 
                    new GUIContent("Auto Create Directories", 
                        "Whether to create directories for file-based targets automatically"));
            }
            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Draw validation section with results display.
        /// </summary>
        private void DrawValidationSection()
        {
            using (new EditorGUILayout.VerticalScope(_stylesCache.SectionBoxStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Validation", _stylesCache.SectionHeaderStyle);
                    _showTargetValidation = EditorGUILayout.Toggle(_showTargetValidation, GUILayout.Width(20));
                }
                
                if (_showTargetValidation)
                {
                    DrawValidationResults();
                }
            }
            
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// Draw validation results with caching for performance.
        /// </summary>
        private void DrawValidationResults()
        {
            // Use cached validation result if recent
            const double cacheTimeout = 2.0; // 2 seconds
            var currentTime = EditorApplication.timeSinceStartup;
            
            if (_lastValidationResult == null || 
                (currentTime - _lastValidationTime) > cacheTimeout)
            {
                _lastValidationResult = ValidateConfiguration();
                _lastValidationTime = currentTime;
            }
            
            var messageType = _lastValidationResult.IsValid ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(_lastValidationResult.Message, messageType);
            
            // Show detailed issues if any
            if (_lastValidationResult.Issues?.Count > 0)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var issue in _lastValidationResult.Issues)
                    {
                        EditorGUILayout.LabelField($"• {issue}", _stylesCache.IssueStyle);
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw action buttons section.
        /// </summary>
        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Configuration", _stylesCache.ButtonStyle))
                {
                    var result = ValidateConfiguration();
                    EditorUtility.DisplayDialog("Validation Result", result.Message, "OK");
                }
                
                if (GUILayout.Button("Create Example Targets", _stylesCache.ButtonStyle))
                {
                    CreateExampleTargets();
                }
                
                if (GUILayout.Button("Reset to Defaults", _stylesCache.ButtonStyle))
                {
                    ResetToDefaults();
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Cache all serialized properties for performance.
        /// </summary>
        private void CacheSerializedProperties()
        {
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
        
        /// <summary>
        /// Ensure the type cache for ILogTargetConfig implementations is populated.
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
                    
                _logTargetConfigTypes = GetLogTargetConfigTypes().ToArray();
            }
        }
        
        /// <summary>
        /// Get all types that implement ILogTargetConfig and derive from ScriptableObject.
        /// Uses composition-friendly approach for type discovery.
        /// </summary>
        /// <returns>Collection of ILogTargetConfig implementation types.</returns>
        private static IEnumerable<Type> GetLogTargetConfigTypes()
        {
            var targetInterface = typeof(ILogTargetConfig);
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
                    targetInterface.IsAssignableFrom(type) &&
                    scriptableObjectType.IsAssignableFrom(type))
                .OrderBy(type => type.Name);
        }
        
        /// <summary>
        /// Show context menu for adding new target configurations.
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
                foreach (var targetType in _logTargetConfigTypes)
                {
                    var friendlyName = GetFriendlyTypeName(targetType);
                    menu.AddItem(new GUIContent(friendlyName), false, () => CreateNewTarget(targetType));
                }
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Create a new target configuration of the specified type.
        /// </summary>
        /// <param name="targetType">The type of target to create.</param>
        private void CreateNewTarget(Type targetType)
        {
            try
            {
                var newTarget = CreateInstance(targetType) as ILogTargetConfig;
                if (newTarget == null)
                {
                    Debug.LogError($"Failed to create target of type {targetType.Name}");
                    return;
                }
                
                var targetObject = newTarget as ScriptableObject;
                if (targetObject == null)
                {
                    Debug.LogError($"Target type {targetType.Name} is not a ScriptableObject");
                    return;
                }
                
                // Configure with defaults
                var friendlyName = GetFriendlyTypeName(targetType);
                newTarget.TargetName = $"{friendlyName}Target";
                targetObject.name = newTarget.TargetName;
                
                // Add to configuration
                AddTargetToConfig(targetObject);
                
                // Save and select
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = targetObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create target configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add a target configuration to the config asset.
        /// </summary>
        /// <param name="targetObject">The target configuration object.</param>
        private void AddTargetToConfig(ScriptableObject targetObject)
        {
            // Add as sub-asset to keep everything organized
            AssetDatabase.AddObjectToAsset(targetObject, _config);
            
            // Add to the array
            int newIndex = _logTargets.arraySize;
            _logTargets.arraySize++;
            _logTargets.GetArrayElementAtIndex(newIndex).objectReferenceValue = targetObject;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Remove target at specified index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        private void RemoveTargetAtIndex(int index)
        {
            if (index < 0 || index >= _logTargets.arraySize)
                return;
                
            var targetProperty = _logTargets.GetArrayElementAtIndex(index);
            var targetObject = targetProperty.objectReferenceValue;
            
            // Clear the reference first
            targetProperty.objectReferenceValue = null;
            
            // Remove from array
            _logTargets.DeleteArrayElementAtIndex(index);
            
            // Remove sub-asset if it exists
            if (targetObject != null)
            {
                AssetDatabase.RemoveObjectFromAsset(targetObject);
                DestroyImmediate(targetObject);
            }
            
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Get user-friendly type name for display.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <returns>Formatted type name.</returns>
        private static string GetFriendlyTypeName(Type type)
        {
            string name = type.Name;
            
            // Remove common suffixes
            var suffixes = new[] { "Config", "LogTarget", "Target" };
            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    break;
                }
            }
            
            // Add spaces before capital letters
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
        /// Get the count of enabled targets.
        /// </summary>
        /// <returns>Number of enabled targets.</returns>
        private int GetEnabledTargetCount()
        {
            int count = 0;
            
            for (int i = 0; i < _logTargets.arraySize; i++)
            {
                var targetProperty = _logTargets.GetArrayElementAtIndex(i);
                var targetConfig = targetProperty.objectReferenceValue as ILogTargetConfig;
                
                if (targetConfig?.Enabled == true)
                {
                    count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Get the total count of targets.
        /// </summary>
        /// <returns>Total number of targets.</returns>
        private int GetTotalTargetCount()
        {
            return _logTargets?.arraySize ?? 0;
        }
        
        /// <summary>
        /// Get configuration status text for header display.
        /// </summary>
        /// <returns>Status text.</returns>
        private string GetConfigurationStatusText()
        {
            var enabledCount = GetEnabledTargetCount();
            var totalCount = GetTotalTargetCount();
            
            if (totalCount == 0)
                return "No Targets";
            
            if (enabledCount == 0)
                return "All Disabled";
            
            return enabledCount == totalCount 
                ? $"All {totalCount} Enabled" 
                : $"{enabledCount}/{totalCount} Enabled";
        }
        
        /// <summary>
        /// Get configuration status style for header display.
        /// </summary>
        /// <returns>GUI style for status display.</returns>
        private GUIStyle GetConfigurationStatusStyle()
        {
            var enabledCount = GetEnabledTargetCount();
            var totalCount = GetTotalTargetCount();
            
            if (totalCount == 0 || enabledCount == 0)
                return _stylesCache.WarningStatusStyle;
            
            return enabledCount == totalCount 
                ? _stylesCache.SuccessStatusStyle 
                : _stylesCache.InfoStatusStyle;
        }
        
        /// <summary>
        /// Validate the configuration and return detailed results.
        /// </summary>
        /// <returns>Validation result with details.</returns>
        private ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();
            var issues = new List<string>();
            
            // Check basic configuration
            if (_config.MaxMessagesPerBatch <= 0)
            {
                issues.Add("Max messages per batch must be greater than 0");
            }
            
            if (_config.InitialQueueCapacity <= 0)
            {
                issues.Add("Initial queue capacity must be greater than 0");
            }
            
            if (_config.EnableAutoFlush && _config.AutoFlushInterval <= 0.0f)
            {
                issues.Add("Auto flush interval must be greater than 0 when auto flush is enabled");
            }
            
            // Check targets
            var totalTargets = GetTotalTargetCount();
            var enabledTargets = GetEnabledTargetCount();
            
            if (totalTargets == 0)
            {
                issues.Add("No log targets configured");
            }
            else if (enabledTargets == 0)
            {
                issues.Add("No targets are enabled");
            }
            
            // Check for null targets
            for (int i = 0; i < totalTargets; i++)
            {
                var targetProperty = _logTargets.GetArrayElementAtIndex(i);
                if (targetProperty.objectReferenceValue == null)
                {
                    issues.Add($"Target {i} is null");
                }
            }
            
            result.IsValid = issues.Count == 0;
            result.Issues = issues;
            result.Message = result.IsValid 
                ? "Configuration is valid and ready to use." 
                : $"Configuration has {issues.Count} issue(s) that should be addressed.";
            
            return result;
        }
        
        /// <summary>
        /// Validate all targets and show results.
        /// </summary>
        private void ValidateAllTargets()
        {
            var result = ValidateConfiguration();
            var title = result.IsValid ? "Validation Successful" : "Validation Issues Found";
            
            var message = result.Message;
            if (result.Issues?.Count > 0)
            {
                message += "\n\nIssues:\n" + string.Join("\n", result.Issues.Select(i => $"• {i}"));
            }
            
            EditorUtility.DisplayDialog(title, message, "OK");
        }
        
        /// <summary>
        /// Create example targets for testing and development.
        /// </summary>
        private void CreateExampleTargets()
        {
            if (!EditorUtility.DisplayDialog("Create Example Targets", 
                "This will create example log targets for testing. Continue?", "Create", "Cancel"))
            {
                return;
            }
            
            EditorUtility.DisplayDialog("Create Example Targets", 
                "Example target creation requires specific target implementations to be available.", "OK");
        }
        
        /// <summary>
        /// Reset configuration to default values.
        /// </summary>
        private void ResetToDefaults()
        {
            if (!EditorUtility.DisplayDialog("Reset to Defaults", 
                "This will reset all settings to default values. Continue?", "Reset", "Cancel"))
            {
                return;
            }
            
            _minimumLevel.enumValueIndex = (int)LogLevel.Info;
            _maxMessagesPerBatch.intValue = 200;
            _initialQueueCapacity.intValue = 64;
            _enableAutoFlush.boolValue = true;
            _autoFlushInterval.floatValue = 0.5f;
            _enableMessageBusIntegration.boolValue = false;
            _validateTargetsOnStartup.boolValue = true;
            _autoCreateDirectories.boolValue = true;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
        
        #region Nested Types
        
        /// <summary>
        /// Container for validation results with detailed information.
        /// </summary>
        private sealed class ValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }
            public List<string> Issues { get; set; }
        }
        
        /// <summary>
        /// Memory-optimized cache for editor styles to minimize GC allocations.
        /// </summary>
        private sealed class EditorStylesCache : IDisposable
        {
            public GUIStyle HeaderStyle { get; private set; }
            public GUIStyle SectionHeaderStyle { get; private set; }
            public GUIStyle SectionBoxStyle { get; private set; }
            public GUIStyle TargetItemStyle { get; private set; }
            public GUIStyle ButtonStyle { get; private set; }
            public GUIStyle FoldoutStyle { get; private set; }
            public GUIStyle EnabledLabelStyle { get; private set; }
            public GUIStyle DisabledLabelStyle { get; private set; }
            public GUIStyle SuccessStatusStyle { get; private set; }
            public GUIStyle InfoStatusStyle { get; private set; }
            public GUIStyle WarningStatusStyle { get; private set; }
            public GUIStyle IssueStyle { get; private set; }
            
            private bool _initialized;
            private bool _disposed;
            
            /// <summary>
            /// Ensure styles are initialized only when needed.
            /// </summary>
            public void EnsureInitialized()
            {
                if (_initialized || _disposed)
                    return;
                    
                InitializeStyles();
                _initialized = true;
            }
            
            /// <summary>
            /// Initialize all GUI styles with optimized settings.
            /// </summary>
            private void InitializeStyles()
            {
                HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft
                };
                
                SectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
                
                SectionBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };
                
                TargetItemStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 5, 5),
                    margin = new RectOffset(0, 0, 2, 2)
                };
                
                ButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(8, 8, 4, 4)
                };
                
                FoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
                
                EnabledLabelStyle = new GUIStyle(EditorStyles.label);
                
                DisabledLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleLeft
                };
                
                SuccessStatusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.green : new Color(0, 0.7f, 0) },
                    alignment = TextAnchor.MiddleRight
                };
                
                InfoStatusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue },
                    alignment = TextAnchor.MiddleRight
                };
                
                WarningStatusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.yellow : new Color(0.8f, 0.6f, 0) },
                    alignment = TextAnchor.MiddleRight
                };
                
                IssueStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.red : new Color(0.8f, 0, 0) }
                };
            }
            
            /// <summary>
            /// Clean up resources.
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                }
            }
        }
        
        #endregion
    }
}