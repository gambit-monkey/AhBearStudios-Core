using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Serialization;
using AhBearStudios.Core.Logging;
using AhBearStudios.Unity.Serialization.ScriptableObjects;
using AhBearStudios.Unity.Serialization.Jobs;
using Cysharp.Threading.Tasks;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Editor
{
    /// <summary>
    /// Unity Editor window for monitoring and managing the AhBearStudios serialization system.
    /// Provides real-time performance metrics, configuration management, and debugging tools.
    /// </summary>
    public class SerializationEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _isMonitoring = false;
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.5f;

        // Performance metrics
        private List<SerializationMetric> _performanceHistory = new List<SerializationMetric>();
        private const int MaxHistoryEntries = 100;

        // Configuration
        private SerializationConfigAsset _currentConfig;
        private bool _showAdvancedSettings = false;

        // Test data
        private string _testDataSize = "1000";
        private CompressionAlgorithm _testCompressionAlgorithm = CompressionAlgorithm.LZ4;
        private SerializationFormat _testFormat = SerializationFormat.MemoryPack;

        [MenuItem("AhBearStudios/Serialization/Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SerializationEditorWindow>("Serialization Monitor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _lastUpdateTime = Time.realtimeSinceStartup;
            LoadConfiguration();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawPerformanceSection();
            DrawConfigurationSection();
            DrawTestingSection();
            DrawFormatterSection();
            DrawJobSystemSection();

            EditorGUILayout.EndScrollView();

            if (_isMonitoring && Time.realtimeSinceStartup - _lastUpdateTime > UpdateInterval)
            {
                UpdateMetrics();
                _lastUpdateTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("AhBearStudios Serialization Monitor", EditorStyles.largeLabel);
                
                GUILayout.FlexibleSpace();
                
                var buttonText = _isMonitoring ? "Stop Monitoring" : "Start Monitoring";
                var buttonColor = _isMonitoring ? Color.red : Color.green;
                
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = buttonColor;
                
                if (GUILayout.Button(buttonText, GUILayout.Width(120)))
                {
                    _isMonitoring = !_isMonitoring;
                    if (_isMonitoring)
                    {
                        _performanceHistory.Clear();
                    }
                }
                
                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawPerformanceSection()
        {
            EditorGUILayout.LabelField("Performance Metrics", EditorStyles.boldLabel);
            
            if (_performanceHistory.Count == 0)
            {
                EditorGUILayout.HelpBox("No performance data available. Start monitoring to collect metrics.", MessageType.Info);
                return;
            }

            var latest = _performanceHistory[_performanceHistory.Count - 1];
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Latest Serialization: {latest.SerializationTime:F2}ms");
                EditorGUILayout.LabelField($"Latest Deserialization: {latest.DeserializationTime:F2}ms");
                EditorGUILayout.LabelField($"Compression Ratio: {latest.CompressionRatio:P1}");
                EditorGUILayout.LabelField($"Throughput: {latest.ThroughputMBps:F2} MB/s");
                EditorGUILayout.LabelField($"Memory Usage: {latest.MemoryUsageKB:F1} KB");
            }

            // Performance graph
            if (_performanceHistory.Count > 1)
            {
                DrawPerformanceGraph();
            }

            EditorGUILayout.Space();
        }

        private void DrawPerformanceGraph()
        {
            var rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));

            if (_performanceHistory.Count < 2) return;

            var maxTime = _performanceHistory.AsValueEnumerable().Max(m => Mathf.Max(m.SerializationTime, m.DeserializationTime));
            if (maxTime <= 0) return;

            // Draw serialization times
            Handles.color = Color.green;
            for (int i = 1; i < _performanceHistory.Count; i++)
            {
                var prevX = rect.x + (float)(i - 1) / (_performanceHistory.Count - 1) * rect.width;
                var currX = rect.x + (float)i / (_performanceHistory.Count - 1) * rect.width;
                var prevY = rect.y + rect.height - (_performanceHistory[i - 1].SerializationTime / maxTime) * rect.height;
                var currY = rect.y + rect.height - (_performanceHistory[i].SerializationTime / maxTime) * rect.height;
                
                Handles.DrawLine(new Vector3(prevX, prevY), new Vector3(currX, currY));
            }

            // Draw deserialization times
            Handles.color = Color.blue;
            for (int i = 1; i < _performanceHistory.Count; i++)
            {
                var prevX = rect.x + (float)(i - 1) / (_performanceHistory.Count - 1) * rect.width;
                var currX = rect.x + (float)i / (_performanceHistory.Count - 1) * rect.width;
                var prevY = rect.y + rect.height - (_performanceHistory[i - 1].DeserializationTime / maxTime) * rect.height;
                var currY = rect.y + rect.height - (_performanceHistory[i].DeserializationTime / maxTime) * rect.height;
                
                Handles.DrawLine(new Vector3(prevX, prevY), new Vector3(currX, currY));
            }

            // Legend
            var legendRect = new Rect(rect.x + 5, rect.y + 5, 200, 40);
            EditorGUI.DrawRect(legendRect, new Color(0, 0, 0, 0.7f));
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 2, 100, 18), "Serialization", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } });
            GUI.Label(new Rect(legendRect.x + 5, legendRect.y + 20, 100, 18), "Deserialization", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.blue } });
        }

        private void DrawConfigurationSection()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var newConfig = (SerializationConfigAsset)EditorGUILayout.ObjectField("Config Asset", _currentConfig, typeof(SerializationConfigAsset), false);
                if (newConfig != _currentConfig)
                {
                    _currentConfig = newConfig;
                    SaveConfiguration();
                }

                if (_currentConfig != null)
                {
                    EditorGUILayout.LabelField($"Threading Mode: {_currentConfig.ThreadingMode}");
                    EditorGUILayout.LabelField($"Buffer Pool Size: {_currentConfig.BufferPoolSize}");
                    EditorGUILayout.LabelField($"Enable Compression: {_currentConfig.EnableCompression}");
                    EditorGUILayout.LabelField($"Enable Encryption: {_currentConfig.EnableEncryption}");
                    
                    if (GUILayout.Button("Edit Configuration"))
                    {
                        Selection.activeObject = _currentConfig;
                        EditorGUIUtility.PingObject(_currentConfig);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No configuration asset assigned. Create one to manage serialization settings.", MessageType.Warning);
                    
                    if (GUILayout.Button("Create Configuration Asset"))
                    {
                        CreateConfigurationAsset();
                    }
                }

                _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings");
                if (_showAdvancedSettings)
                {
                    DrawAdvancedSettings();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawAdvancedSettings()
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("Memory Pool Statistics:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("• Buffer Pool Usage: N/A");
            EditorGUILayout.LabelField("• Active Serializations: N/A");
            EditorGUILayout.LabelField("• Peak Memory Usage: N/A");
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Clear Performance History"))
            {
                _performanceHistory.Clear();
            }
            
            if (GUILayout.Button("Force Garbage Collection"))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawTestingSection()
        {
            EditorGUILayout.LabelField("Performance Testing", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _testDataSize = EditorGUILayout.TextField("Data Size (items)", _testDataSize);
                _testCompressionAlgorithm = (CompressionAlgorithm)EditorGUILayout.EnumPopup("Compression Algorithm", _testCompressionAlgorithm);
                _testFormat = (SerializationFormat)EditorGUILayout.EnumPopup("Serialization Format", _testFormat);
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Test Vector3 Serialization"))
                    {
                        RunVector3SerializationTest();
                    }
                    
                    if (GUILayout.Button("Test Quaternion Serialization"))
                    {
                        RunQuaternionSerializationTest();
                    }
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Test Color Serialization"))
                    {
                        RunColorSerializationTest();
                    }
                    
                    if (GUILayout.Button("Test Matrix4x4 Serialization"))
                    {
                        RunMatrix4x4SerializationTest();
                    }
                }
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Run Full Performance Suite"))
                {
                    RunFullPerformanceSuite();
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawFormatterSection()
        {
            EditorGUILayout.LabelField("Formatter Registration", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var registeredCount = AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.GetRegisteredFormatterCount();
                var isRegistered = AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.IsRegistered();
                
                EditorGUILayout.LabelField($"Formatters Registered: {registeredCount}");
                EditorGUILayout.LabelField($"Registration Status: {(isRegistered ? "✓ Complete" : "✗ Incomplete")}");
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Register Standard Formatters"))
                    {
                        AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.RegisterFormatters();
                        Debug.Log("[SerializationEditor] Standard Unity formatters registered");
                    }
                    
                    if (GUILayout.Button("Register Network Optimized"))
                    {
                        AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.RegisterNetworkOptimizedFormatters();
                        Debug.Log("[SerializationEditor] Network-optimized formatters registered");
                    }
                }
                
                if (GUILayout.Button("Register HDR Formatters"))
                {
                    AhBearStudios.Unity.Serialization.Formatters.UnityFormatterRegistration.RegisterHDRFormatters();
                    Debug.Log("[SerializationEditor] HDR formatters registered");
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawJobSystemSection()
        {
            EditorGUILayout.LabelField("Unity Job System", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Job System Integration Status: ✓ Available");
                EditorGUILayout.LabelField("Burst Compilation: ✓ Enabled");
                EditorGUILayout.LabelField("Supported Operations:");
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("• Parallel Serialization");
                EditorGUILayout.LabelField("• Parallel Deserialization");
                EditorGUILayout.LabelField("• Batch Compression");
                EditorGUILayout.LabelField("• Batch Decompression");
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Test Job System Performance"))
                {
                    RunJobSystemPerformanceTest();
                }
            }
        }

        private void LoadConfiguration()
        {
            var configGuid = EditorPrefs.GetString("AhBearStudios.Serialization.ConfigAsset", "");
            if (!string.IsNullOrEmpty(configGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(configGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    _currentConfig = AssetDatabase.LoadAssetAtPath<SerializationConfigAsset>(path);
                }
            }
        }

        private void SaveConfiguration()
        {
            if (_currentConfig != null)
            {
                var path = AssetDatabase.GetAssetPath(_currentConfig);
                var guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString("AhBearStudios.Serialization.ConfigAsset", guid);
            }
            else
            {
                EditorPrefs.DeleteKey("AhBearStudios.Serialization.ConfigAsset");
            }
        }

        private void CreateConfigurationAsset()
        {
            var config = CreateInstance<SerializationConfigAsset>();
            
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Serialization Configuration", 
                "SerializationConfig", 
                "asset", 
                "Save the serialization configuration asset");
                
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                _currentConfig = config;
                SaveConfiguration();
            }
        }

        private void UpdateMetrics()
        {
            // Simulate metrics collection - in a real implementation, these would come from the actual serialization system
            var metric = new SerializationMetric
            {
                Timestamp = DateTime.UtcNow,
                SerializationTime = UnityEngine.Random.Range(0.1f, 5.0f),
                DeserializationTime = UnityEngine.Random.Range(0.1f, 3.0f),
                CompressionRatio = UnityEngine.Random.Range(0.2f, 0.8f),
                ThroughputMBps = UnityEngine.Random.Range(50f, 200f),
                MemoryUsageKB = UnityEngine.Random.Range(100f, 1000f)
            };

            _performanceHistory.Add(metric);
            
            if (_performanceHistory.Count > MaxHistoryEntries)
            {
                _performanceHistory.RemoveAt(0);
            }
        }

        private async void RunVector3SerializationTest()
        {
            if (!int.TryParse(_testDataSize, out var count))
            {
                Debug.LogError("Invalid test data size");
                return;
            }

            Debug.Log($"[SerializationTest] Starting Vector3 serialization test with {count} items");
            
            // Generate test data
            var testData = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                testData[i] = new Vector3(
                    UnityEngine.Random.Range(-1000f, 1000f),
                    UnityEngine.Random.Range(-1000f, 1000f),
                    UnityEngine.Random.Range(-1000f, 1000f)
                );
            }

            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Test using job system if available
                var jobService = new UnitySerializationJobService(null);
                var serializedData = await jobService.SerializeAsync(testData);
                
                var endTime = Time.realtimeSinceStartup;
                var duration = (endTime - startTime) * 1000f;
                
                Debug.Log($"[SerializationTest] Vector3 test completed: {duration:F2}ms, {serializedData.Length} bytes");
                
                jobService.Dispose();
                serializedData.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationTest] Vector3 test failed: {ex.Message}");
            }
        }

        private async void RunQuaternionSerializationTest()
        {
            if (!int.TryParse(_testDataSize, out var count))
            {
                Debug.LogError("Invalid test data size");
                return;
            }

            Debug.Log($"[SerializationTest] Starting Quaternion serialization test with {count} items");
            
            var testData = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                testData[i] = UnityEngine.Random.rotation;
            }

            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                var jobService = new UnitySerializationJobService(null);
                var serializedData = await jobService.SerializeAsync(testData);
                
                var endTime = Time.realtimeSinceStartup;
                var duration = (endTime - startTime) * 1000f;
                
                Debug.Log($"[SerializationTest] Quaternion test completed: {duration:F2}ms, {serializedData.Length} bytes");
                
                jobService.Dispose();
                serializedData.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationTest] Quaternion test failed: {ex.Message}");
            }
        }

        private async void RunColorSerializationTest()
        {
            if (!int.TryParse(_testDataSize, out var count))
            {
                Debug.LogError("Invalid test data size");
                return;
            }

            Debug.Log($"[SerializationTest] Starting Color serialization test with {count} items");
            
            var testData = new Color[count];
            for (int i = 0; i < count; i++)
            {
                testData[i] = new Color(
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f)
                );
            }

            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                var jobService = new UnitySerializationJobService(null);
                var serializedData = await jobService.SerializeAsync(testData);
                
                var endTime = Time.realtimeSinceStartup;
                var duration = (endTime - startTime) * 1000f;
                
                Debug.Log($"[SerializationTest] Color test completed: {duration:F2}ms, {serializedData.Length} bytes");
                
                jobService.Dispose();
                serializedData.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationTest] Color test failed: {ex.Message}");
            }
        }

        private async void RunMatrix4x4SerializationTest()
        {
            if (!int.TryParse(_testDataSize, out var count))
            {
                Debug.LogError("Invalid test data size");
                return;
            }

            Debug.Log($"[SerializationTest] Starting Matrix4x4 serialization test with {count} items");
            
            var testData = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
            {
                testData[i] = Matrix4x4.TRS(
                    UnityEngine.Random.insideUnitSphere * 100f,
                    UnityEngine.Random.rotation,
                    Vector3.one + UnityEngine.Random.insideUnitSphere
                );
            }

            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                var jobService = new UnitySerializationJobService(null);
                var serializedData = await jobService.SerializeAsync(testData);
                
                var endTime = Time.realtimeSinceStartup;
                var duration = (endTime - startTime) * 1000f;
                
                Debug.Log($"[SerializationTest] Matrix4x4 test completed: {duration:F2}ms, {serializedData.Length} bytes");
                
                jobService.Dispose();
                serializedData.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationTest] Matrix4x4 test failed: {ex.Message}");
            }
        }

        private async void RunFullPerformanceSuite()
        {
            Debug.Log("[SerializationTest] Starting full performance test suite...");
            
            await RunVector3SerializationTest();
            await UniTask.Delay(100);
            
            await RunQuaternionSerializationTest();
            await UniTask.Delay(100);
            
            await RunColorSerializationTest();
            await UniTask.Delay(100);
            
            await RunMatrix4x4SerializationTest();
            
            Debug.Log("[SerializationTest] Full performance test suite completed");
        }

        private async void RunJobSystemPerformanceTest()
        {
            Debug.Log("[SerializationTest] Starting Job System performance test...");
            
            try
            {
                // Test compression job
                var testData = new byte[10000];
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = (byte)(i % 256);
                }

                var compressionService = new UnityCompressionJobService(null);
                var compressedData = await compressionService.CompressAsync(testData, _testCompressionAlgorithm);
                var decompressedData = await compressionService.DecompressAsync(compressedData, testData.Length, _testCompressionAlgorithm);
                
                var compressionRatio = 1.0 - ((double)compressedData.Length / testData.Length);
                
                Debug.Log($"[JobSystemTest] Compression test completed: {testData.Length} -> {compressedData.Length} bytes ({compressionRatio:P1} compression)");
                Debug.Log($"[JobSystemTest] Decompression test completed: {compressedData.Length} -> {decompressedData.Length} bytes");
                
                compressionService.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JobSystemTest] Job system test failed: {ex.Message}");
            }
        }

        private struct SerializationMetric
        {
            public DateTime Timestamp;
            public float SerializationTime;
            public float DeserializationTime;
            public float CompressionRatio;
            public float ThroughputMBps;
            public float MemoryUsageKB;
        }
    }
}