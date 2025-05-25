
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace AhBearStudios.Pooling.Core.Pooling.Editor
{
    /// <summary>
    /// Custom editor for the PoolManager component
    /// </summary>
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerEditor : UnityEditor.Editor
    {
        private bool _showRuntimeInfo;
        private bool _showPerformanceMetrics = true;
        private bool _showHealthStatus = true;
        private Vector2 _scrollPosition;
        
        /// <summary>
        /// Called when the editor becomes enabled
        /// </summary>
        public void OnEnable()
        {
            // Ensure PoolingServices is initialized in play mode
            if (Application.isPlaying && !PoolingServices.HasService<PoolDiagnostics>())
            {
                PoolingServices.Initialize();
            }
        }
        
        /// <summary>
        /// Draws the inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            PoolManager poolManager = (PoolManager)target;
            
            // In play mode, show runtime info
            if (Application.isPlaying && poolManager != null)
            {
                EditorGUILayout.Space();
                
                _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Info", true);
                
                if (_showRuntimeInfo)
                {
                    DrawRuntimeInfo();
                }
                
                // Button to open the Pool Visualizer
                if (GUILayout.Button("Open Pool Visualizer"))
                {
                    PoolVisualizerWindow.ShowWindow();
                }
            }
            else
            {
                // Not in play mode - show help message
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime pool information.", MessageType.Info);
            }
            
            // Add selected scene objects to pool button
            if (Selection.gameObjects.Length > 1 && Selection.gameObjects.Contains(poolManager.gameObject))
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Add Selected Objects to Pool"))
                {
                    AddSelectedObjectsToPool(poolManager);
                }
            }
        }
        
        /// <summary>
        /// Draws runtime information about the pools
        /// </summary>
        private void DrawRuntimeInfo()
        {
            if (!PoolingServices.HasService<PoolDiagnostics>())
            {
                EditorGUILayout.HelpBox("Pool diagnostics service not available.", MessageType.Warning);
                return;
            }
            
            List<Dictionary<string, object>> allMetrics = PoolingServices.Diagnostics.GetAllMetrics();
            
            if (allMetrics == null || allMetrics.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools registered yet.", MessageType.Info);
                return;
            }
            
            // Display options
            EditorGUILayout.BeginHorizontal();
            _showPerformanceMetrics = EditorGUILayout.ToggleLeft("Show Performance", _showPerformanceMetrics, GUILayout.Width(150));
            _showHealthStatus = EditorGUILayout.ToggleLeft("Show Health", _showHealthStatus, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Get health issues if needed
            List<PoolHealthIssue> healthIssues = null;
            if (_showHealthStatus && PoolingServices.HealthChecker != null)
            {
                healthIssues = PoolingServices.HealthChecker.GetIssues();
            }
            
            // Draw scrollable area for pool metrics
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            
            foreach (var metrics in allMetrics)
            {
                string poolKey = metrics.TryGetValue("PoolName", out object nameObj) ? (string)nameObj : "Unknown Pool";
                DrawPoolMetric(poolKey, metrics, healthIssues);
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
            
            // Total stats
            int totalActiveItems = allMetrics.Sum(m => m.TryGetValue("ActiveCount", out object activeObj) ? Convert.ToInt32(activeObj) : 0);
            int totalCapacity = allMetrics.Sum(m => m.TryGetValue("TotalItems", out object totalObj) ? Convert.ToInt32(totalObj) : 0);
            long totalMemoryUsage = allMetrics.Sum(m => m.TryGetValue("EstimatedMemoryBytes", out object memObj) ? Convert.ToInt64(memObj) : 0);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Pools: {allMetrics.Count}");
            EditorGUILayout.LabelField($"Active Items: {totalActiveItems} / {totalCapacity} ({(totalCapacity > 0 ? (float)totalActiveItems / totalCapacity * 100 : 0):F1}%)");
            EditorGUILayout.LabelField($"Memory Usage: {FormatMemorySize(totalMemoryUsage)}");
            
            // Button to refresh data
            if (GUILayout.Button("Refresh Data"))
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// Draws metrics for a specific pool
        /// </summary>
        /// <param name="poolKey">Pool identifier</param>
        /// <param name="metrics">Pool metrics</param>
        /// <param name="healthIssues">Health issues (optional)</param>
        private void DrawPoolMetric(string poolKey, Dictionary<string, object> metrics, List<PoolHealthIssue> healthIssues = null)
        {
            string poolType = metrics.TryGetValue("PoolType", out object typeObj) ? (string)typeObj : "Unknown";
            string itemType = metrics.TryGetValue("ItemType", out object itemObj) ? (string)itemObj : "Unknown";
            
            int activeCount = metrics.TryGetValue("ActiveCount", out object activeObj) ? Convert.ToInt32(activeObj) : 0;
            int totalItems = metrics.TryGetValue("TotalItems", out object totalObj) ? Convert.ToInt32(totalObj) : 0;
            int capacity = metrics.TryGetValue("Capacity", out object capObj) ? Convert.ToInt32(capObj) : 0;
            
            // Draw header with foldout
            bool foldout = EditorPrefs.GetBool($"PoolMetricFoldout_{poolKey}", true);
            bool newFoldout = EditorGUILayout.Foldout(foldout, $"{poolKey} ({activeCount}/{totalItems})", true, EditorStyles.boldLabel);
            
            if (newFoldout != foldout)
            {
                EditorPrefs.SetBool($"PoolMetricFoldout_{poolKey}", newFoldout);
            }
            
            if (!newFoldout)
            {
                return;
            }
            
            EditorGUI.indentLevel++;
            
            // Basic info
            EditorGUILayout.LabelField($"Pool Type: {poolType.Split('.').Last()}");
            EditorGUILayout.LabelField($"Item Type: {itemType.Split('.').Last()}");
            EditorGUILayout.LabelField($"Capacity: {capacity}");
            
            // Usage info
            float usageRatio = totalItems > 0 ? (float)activeCount / totalItems : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Usage:");
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), usageRatio, $"{usageRatio * 100:F1}%");
            EditorGUILayout.EndHorizontal();
            
            // Memory usage
            long memoryUsage = metrics.TryGetValue("EstimatedMemoryBytes", out object memObj) ? Convert.ToInt64(memObj) : 0;
            EditorGUILayout.LabelField($"Memory Usage: {FormatMemorySize(memoryUsage)}");
            
            // Performance metrics if enabled
            if (_showPerformanceMetrics)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
                
                float avgAcquireTime = metrics.TryGetValue("AverageAcquireTimeMs", out object acquireObj) ? Convert.ToSingle(acquireObj) : 0;
                float avgLifetime = metrics.TryGetValue("AverageObjectLifetimeSec", out object lifetimeObj) ? Convert.ToSingle(lifetimeObj) : 0;
                
                EditorGUILayout.LabelField($"Avg Acquire Time: {avgAcquireTime:F3} ms");
                EditorGUILayout.LabelField($"Avg Object Lifetime: {avgLifetime:F2} seconds");
            }
            
            // Health issues if enabled
            if (_showHealthStatus && healthIssues != null && healthIssues.Count > 0)
            {
                var poolIssues = healthIssues.Where(i => i.PoolName == poolKey).ToList();
                
                if (poolIssues.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Health Issues", EditorStyles.boldLabel);
                    
                    foreach (var issue in poolIssues)
                    {
                        GUIStyle issueStyle = issue.Severity >= 75 ? 
                            new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red }, fontStyle = FontStyle.Bold } : 
                            new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.6f, 0f) } };
                        
                        EditorGUILayout.LabelField($"{issue.IssueType}: {issue.Description}", issueStyle);
                    }
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// Adds selected scene objects to the pool
        /// </summary>
        /// <param name="poolManager">Target pool manager</param>
        private void AddSelectedObjectsToPool(PoolManager poolManager)
        {
            List<GameObject> objectsToAdd = new List<GameObject>();
            
            foreach (GameObject selectedObj in Selection.gameObjects)
            {
                if (selectedObj != poolManager.gameObject)
                {
                    objectsToAdd.Add(selectedObj);
                }
            }
            
            if (objectsToAdd.Count == 0)
            {
                return;
            }
            
            // Create serialized property for prefabs list
            SerializedProperty prefabsProperty = serializedObject.FindProperty("_prefabs");
            
            if (prefabsProperty == null)
            {
                Debug.LogError("Could not find prefabs property on PoolManager");
                return;
            }
            
            // Add each object to the prefabs list
            foreach (GameObject obj in objectsToAdd)
            {
                bool found = false;
                
                // Check if object already exists in the list
                for (int i = 0; i < prefabsProperty.arraySize; i++)
                {
                    SerializedProperty element = prefabsProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == obj)
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    prefabsProperty.arraySize++;
                    prefabsProperty.GetArrayElementAtIndex(prefabsProperty.arraySize - 1).objectReferenceValue = obj;
                }
            }
            
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"Added {objectsToAdd.Count} objects to the pool manager.");
        }
        
        /// <summary>
        /// Formats memory size to a human readable string
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted string</returns>
        private string FormatMemorySize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F1} GB";
        }
    }
}
#endif