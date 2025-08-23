using UnityEngine;
using UnityEditor;
using AhBearStudios.Unity.Serialization.ScriptableObjects;
using AhBearStudios.Core.Serialization;

namespace AhBearStudios.Unity.Serialization.Editor
{
    /// <summary>
    /// Custom property drawer for SerializationConfigAsset to provide enhanced editor experience.
    /// Displays configuration options in a user-friendly format with validation and warnings.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializationConfigAsset))]
    public class SerializationConfigPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the default object field
            var objectRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(objectRect, property, label);

            // If there's a value, show configuration summary
            if (property.objectReferenceValue != null)
            {
                var config = property.objectReferenceValue as SerializationConfigAsset;
                if (config != null)
                {
                    DrawConfigurationSummary(position, config);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var baseHeight = EditorGUIUtility.singleLineHeight;
            
            if (property.objectReferenceValue != null)
            {
                return baseHeight + 80f; // Extra space for summary
            }
            
            return baseHeight;
        }

        private void DrawConfigurationSummary(Rect position, SerializationConfigAsset config)
        {
            var summaryRect = new Rect(position.x + 15, position.y + EditorGUIUtility.singleLineHeight + 2, position.width - 15, 75);
            
            // Background
            EditorGUI.DrawRect(summaryRect, new Color(0.8f, 0.8f, 0.8f, 0.2f));
            
            var contentRect = new Rect(summaryRect.x + 5, summaryRect.y + 2, summaryRect.width - 10, summaryRect.height - 4);
            
            // Configuration summary
            var oldFontSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 10;
            
            GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 12), $"Threading: {config.ThreadingMode}", EditorStyles.miniLabel);
            GUI.Label(new Rect(contentRect.x, contentRect.y + 12, contentRect.width, 12), $"Buffer Pool: {config.BufferPoolSize} bytes", EditorStyles.miniLabel);
            GUI.Label(new Rect(contentRect.x, contentRect.y + 24, contentRect.width, 12), $"Compression: {(config.EnableCompression ? "✓" : "✗")}", EditorStyles.miniLabel);
            GUI.Label(new Rect(contentRect.x, contentRect.y + 36, contentRect.width, 12), $"Encryption: {(config.EnableEncryption ? "✓" : "✗")}", EditorStyles.miniLabel);
            
            // Performance warning
            if (config.ThreadingMode == SerializationThreadingMode.SingleThreaded)
            {
                var warningRect = new Rect(contentRect.x, contentRect.y + 50, contentRect.width, 12);
                GUI.Label(warningRect, "⚠ Single-threaded mode may impact performance", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.yellow } });
            }
            
            GUI.skin.label.fontSize = oldFontSize;
        }
    }

    /// <summary>
    /// Custom editor for SerializationConfigAsset to provide enhanced configuration interface.
    /// </summary>
    [CustomEditor(typeof(SerializationConfigAsset))]
    public class SerializationConfigAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _threadingMode;
        private SerializedProperty _bufferPoolSize;
        private SerializedProperty _enableCompression;
        private SerializedProperty _enableEncryption;
        private SerializedProperty _maxConcurrentOperations;
        private SerializedProperty _compressionLevel;
        private SerializedProperty _encryptionKey;
        private SerializedProperty _performanceProfile;
        private SerializedProperty _enableDetailedLogging;
        private SerializedProperty _logPerformanceMetrics;

        private bool _showAdvancedSettings = false;
        private bool _showPerformanceSettings = false;
        private bool _showSecuritySettings = false;

        private void OnEnable()
        {
            _threadingMode = serializedObject.FindProperty("_threadingMode");
            _bufferPoolSize = serializedObject.FindProperty("_bufferPoolSize");
            _enableCompression = serializedObject.FindProperty("_enableCompression");
            _enableEncryption = serializedObject.FindProperty("_enableEncryption");
            _maxConcurrentOperations = serializedObject.FindProperty("_maxConcurrentOperations");
            _compressionLevel = serializedObject.FindProperty("_compressionLevel");
            _encryptionKey = serializedObject.FindProperty("_encryptionKey");
            _performanceProfile = serializedObject.FindProperty("_performanceProfile");
            _enableDetailedLogging = serializedObject.FindProperty("_enableDetailedLogging");
            _logPerformanceMetrics = serializedObject.FindProperty("_logPerformanceMetrics");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AhBearStudios Serialization Configuration", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            DrawBasicSettings();
            DrawPerformanceSettings();
            DrawSecuritySettings();
            DrawAdvancedSettings();
            DrawValidationAndWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Basic Configuration", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_threadingMode, new GUIContent("Threading Mode", "Controls how serialization operations are threaded"));
                EditorGUILayout.PropertyField(_bufferPoolSize, new GUIContent("Buffer Pool Size", "Size of the memory buffer pool in bytes"));
                EditorGUILayout.PropertyField(_maxConcurrentOperations, new GUIContent("Max Concurrent Operations", "Maximum number of concurrent serialization operations"));
            }

            EditorGUILayout.Space();
        }

        private void DrawPerformanceSettings()
        {
            _showPerformanceSettings = EditorGUILayout.Foldout(_showPerformanceSettings, "Performance Settings", true);
            
            if (_showPerformanceSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(_performanceProfile, new GUIContent("Performance Profile", "Predefined performance configuration"));
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.PropertyField(_enableCompression, new GUIContent("Enable Compression", "Enable data compression to reduce storage size"));
                    
                    if (_enableCompression.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(_compressionLevel, new GUIContent("Compression Level", "Higher values = better compression but slower"));
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.PropertyField(_logPerformanceMetrics, new GUIContent("Log Performance Metrics", "Enable performance metrics logging"));
                    
                    // Performance recommendations
                    if (_threadingMode.enumValueIndex == (int)SerializationThreadingMode.SingleThreaded)
                    {
                        EditorGUILayout.HelpBox("Consider using MultiThreaded mode for better performance in production builds.", MessageType.Info);
                    }
                    
                    if (_bufferPoolSize.intValue < 1024 * 1024) // Less than 1MB
                    {
                        EditorGUILayout.HelpBox("Buffer pool size is quite small. Consider increasing for better performance with large data sets.", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawSecuritySettings()
        {
            _showSecuritySettings = EditorGUILayout.Foldout(_showSecuritySettings, "Security Settings", true);
            
            if (_showSecuritySettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(_enableEncryption, new GUIContent("Enable Encryption", "Enable data encryption for secure storage"));
                    
                    if (_enableEncryption.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.PropertyField(_encryptionKey, new GUIContent("Encryption Key", "Base64 encoded encryption key"));
                        
                        if (string.IsNullOrEmpty(_encryptionKey.stringValue))
                        {
                            EditorGUILayout.HelpBox("Encryption is enabled but no key is provided. Generate a secure key.", MessageType.Error);
                            
                            if (GUILayout.Button("Generate Secure Key"))
                            {
                                GenerateEncryptionKey();
                            }
                        }
                        else
                        {
                            try
                            {
                                var keyBytes = System.Convert.FromBase64String(_encryptionKey.stringValue);
                                if (keyBytes.Length < 16)
                                {
                                    EditorGUILayout.HelpBox("Encryption key is too short. Use at least 128-bit (16 bytes) key.", MessageType.Error);
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox($"✓ Valid encryption key ({keyBytes.Length * 8}-bit)", MessageType.Info);
                                }
                            }
                            catch
                            {
                                EditorGUILayout.HelpBox("Invalid Base64 encryption key format.", MessageType.Error);
                            }
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);
            
            if (_showAdvancedSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(_enableDetailedLogging, new GUIContent("Enable Detailed Logging", "Enable detailed logging for debugging"));
                    
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.LabelField("Debug Actions", EditorStyles.miniBoldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Validate Configuration"))
                        {
                            ValidateConfiguration();
                        }
                        
                        if (GUILayout.Button("Reset to Defaults"))
                        {
                            ResetToDefaults();
                        }
                    }
                    
                    if (GUILayout.Button("Test Configuration"))
                    {
                        TestConfiguration();
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawValidationAndWarnings()
        {
            var config = target as SerializationConfigAsset;
            if (config == null) return;

            // Validation warnings
            var hasWarnings = false;
            
            if (config.EnableEncryption && string.IsNullOrEmpty(config.EncryptionKey))
            {
                EditorGUILayout.HelpBox("⚠ Encryption is enabled but no encryption key is provided.", MessageType.Warning);
                hasWarnings = true;
            }
            
            if (config.BufferPoolSize > 100 * 1024 * 1024) // > 100MB
            {
                EditorGUILayout.HelpBox("⚠ Very large buffer pool size may consume significant memory.", MessageType.Warning);
                hasWarnings = true;
            }
            
            if (config.MaxConcurrentOperations > 32)
            {
                EditorGUILayout.HelpBox("⚠ High concurrent operations count may impact performance.", MessageType.Warning);
                hasWarnings = true;
            }
            
            if (!hasWarnings)
            {
                EditorGUILayout.HelpBox("✓ Configuration appears valid.", MessageType.Info);
            }
        }

        private void GenerateEncryptionKey()
        {
            var keyBytes = new byte[32]; // 256-bit key
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            
            _encryptionKey.stringValue = System.Convert.ToBase64String(keyBytes);
            EditorUtility.SetDirty(target);
        }

        private void ValidateConfiguration()
        {
            var config = target as SerializationConfigAsset;
            if (config == null) return;

            var issues = new System.Collections.Generic.List<string>();
            
            if (config.BufferPoolSize < 1024)
                issues.Add("Buffer pool size is very small");
                
            if (config.EnableEncryption && string.IsNullOrEmpty(config.EncryptionKey))
                issues.Add("Encryption enabled but no key provided");
                
            if (config.MaxConcurrentOperations < 1)
                issues.Add("Max concurrent operations must be at least 1");

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Configuration Valid", "✓ Configuration validation passed successfully.", "OK");
            }
            else
            {
                var message = "Configuration validation found issues:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Configuration Issues", message, "OK");
            }
        }

        private void ResetToDefaults()
        {
            if (EditorUtility.DisplayDialog("Reset Configuration", "Reset all settings to default values?", "Reset", "Cancel"))
            {
                var config = target as SerializationConfigAsset;
                if (config != null)
                {
                    config.ResetToDefaults();
                    EditorUtility.SetDirty(target);
                    serializedObject.Update();
                }
            }
        }

        private void TestConfiguration()
        {
            var config = target as SerializationConfigAsset;
            if (config == null) return;

            Debug.Log($"[SerializationConfig] Testing configuration: {config.name}");
            Debug.Log($"Threading Mode: {config.ThreadingMode}");
            Debug.Log($"Buffer Pool Size: {config.BufferPoolSize:N0} bytes");
            Debug.Log($"Compression: {(config.EnableCompression ? "Enabled" : "Disabled")}");
            Debug.Log($"Encryption: {(config.EnableEncryption ? "Enabled" : "Disabled")}");
            Debug.Log($"Max Concurrent Operations: {config.MaxConcurrentOperations}");
            
            EditorUtility.DisplayDialog("Configuration Test", "Configuration details have been logged to the console.", "OK");
        }
    }
}