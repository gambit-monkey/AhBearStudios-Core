#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Pooling.Core;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Native;
using UnityEditor;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Unity.Editor
{
    /// <summary>
    /// Partial class handling pool metrics visualization functionality
    /// </summary>
    public partial class PoolVisualizerWindow
    {
        /// <summary>
        /// Draws the table of pool metrics in the main overview
        /// </summary>
        private void DrawPoolMetricsTable()
        {
            EditorGUILayout.Space();
            
            if (_poolMetrics.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools registered. Pools will appear here when created in play mode.", MessageType.Info);
                return;
            }
            
            // Table headers
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Pool Name", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Active/Total", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Usage", EditorStyles.boldLabel, GUILayout.Width(100));
            
            if (_showMemoryUsage)
            {
                GUILayout.Label("Memory", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            
            if (_showPerformanceMetrics)
            {
                GUILayout.Label("Acquire Time", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label("Avg Lifetime", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            
            if (_showHealthIssues)
            {
                GUILayout.Label("Health", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Draw pool metrics by category
            var categorizedMetrics = CategorizePoolMetrics();
            
            foreach (var category in categorizedMetrics.Keys.OrderBy(k => k))
            {
                if (!_categoryFoldouts.ContainsKey(category))
                {
                    _categoryFoldouts[category] = true;
                }
                
                _categoryFoldouts[category] = EditorGUILayout.Foldout(_categoryFoldouts[category], $"{category} ({categorizedMetrics[category].Count})", true, _boldLabelStyle);
                
                if (_categoryFoldouts[category])
                {
                    foreach (var poolMetrics in categorizedMetrics[category])
                    {
                        DrawPoolRow(poolMetrics);
                    }
                }
            }
        }
        
        /// <summary>
        /// Draws a single row for a pool in the metrics table
        /// </summary>
        /// <param name="metrics">Dictionary containing the metrics for this pool</param>
        private void DrawPoolRow(Dictionary<string, object> metrics)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Pool Name
            string poolName = metrics.TryGetValue("PoolName", out object nameObj) ? (string)nameObj : "Unknown";
            if (GUILayout.Button(poolName, EditorStyles.label, GUILayout.Width(200)))
            {
                // Select this pool for detailed view
                _selectedPoolMetrics = metrics;
                _currentTab = ViewTab.Details;
            }
            
            // Pool Type
            string poolType = metrics.TryGetValue("PoolType", out object typeObj) ? (string)typeObj : "Unknown";
            string shortType = poolType.Split('.').Last();
            GUILayout.Label(shortType, GUILayout.Width(100));
            
            // Active/Total Items
            int activeCount = metrics.TryGetValue("ActiveCount", out object activeObj) ? Convert.ToInt32(activeObj) : 0;
            int totalItems = metrics.TryGetValue("TotalItems", out object totalObj) ? Convert.ToInt32(totalObj) : 0;
            GUILayout.Label($"{activeCount}/{totalItems}", GUILayout.Width(80));
            
            // Usage Bar
            float usageRatio = totalItems > 0 ? (float)activeCount / totalItems : 0;
            Color barColor = GetUsageColor(usageRatio);
            EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Width(100 * usageRatio)), barColor);
            EditorGUILayout.EndHorizontal();
            
            // Memory Usage (if enabled)
            if (_showMemoryUsage)
            {
                long memoryUsage = metrics.TryGetValue("EstimatedMemoryBytes", out object memObj) ? Convert.ToInt64(memObj) : 0;
                GUILayout.Label(FormatMemorySize(memoryUsage), GUILayout.Width(80));
            }
            
            // Performance Metrics (if enabled)
            if (_showPerformanceMetrics)
            {
                // Acquire Time
                float avgAcquireTime = metrics.TryGetValue("AverageAcquireTimeMs", out object acquireObj) ? Convert.ToSingle(acquireObj) : 0;
                string acquireDisplay = avgAcquireTime < 0.01f ? "<0.01ms" : $"{avgAcquireTime:F2}ms";
                GUILayout.Label(acquireDisplay, GUILayout.Width(80));
                
                // Average Lifetime
                float avgLifetime = metrics.TryGetValue("AverageObjectLifetimeSec", out object lifetimeObj) ? Convert.ToSingle(lifetimeObj) : 0;
                string lifetimeDisplay = avgLifetime < 0.01f ? "<0.01s" : $"{avgLifetime:F2}s";
                GUILayout.Label(lifetimeDisplay, GUILayout.Width(80));
            }
            
            // Health Status (if enabled)
            if (_showHealthIssues)
            {
                int healthIssueCount = 0;
                int maxSeverity = 0;
                
                if (_healthIssues != null && _healthIssues.Count > 0)
                {
                    // Count issues for this pool
                    foreach (var issue in _healthIssues)
                    {
                        if (issue.PoolName == poolName)
                        {
                            healthIssueCount++;
                            maxSeverity = Math.Max(maxSeverity, issue.Severity);
                        }
                    }
                }
                
                if (healthIssueCount == 0)
                {
                    // Draw green status
                    EditorGUILayout.LabelField("Healthy", GUILayout.Width(80));
                }
                else
                {
                    // Draw issue indicator based on severity
                    GUIStyle style = maxSeverity >= 75 ? _errorStyle : _warningStyle;
                    if (GUILayout.Button($"{healthIssueCount} Issues", style, GUILayout.Width(80)))
                    {
                        _selectedPoolMetrics = metrics;
                        _currentTab = ViewTab.Health;
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Refreshes pool metrics data
        /// </summary>
        private void RefreshData()
        {
            _poolMetrics.Clear();
            _healthIssues.Clear();
            
            if (!Application.isPlaying)
            {
                return;
            }
            
            // Ensure PoolingServices is initialized
            if (!PoolingServices.HasService<PoolDiagnostics>())
            {
                PoolingServices.Initialize();
            }
            
            // Get metrics from PoolDiagnostics
            if (PoolingServices.Diagnostics != null)
            {
                var allMetrics = PoolingServices.Diagnostics.GetAllMetrics();
                if (allMetrics != null)
                {
                    _poolMetrics.AddRange(allMetrics);
                }
            }
            
            // Get health issues from HealthChecker
            if (PoolingServices.HealthChecker != null)
            {
                var issues = PoolingServices.HealthChecker.GetIssues();
                if (issues != null)
                {
                    _healthIssues.AddRange(issues);
                }
            }
            
            ApplyFilters();
            SortPoolMetrics();
        }
        
        /// <summary>
        /// Applies filtering to the pool metrics
        /// </summary>
        private void ApplyFilters()
        {
            if (string.IsNullOrEmpty(_filterText) && _typeFilter == PoolTypeFilter.All && _showInactivePools)
            {
                // No filtering needed
                return;
            }
            
            // Apply name filter
            if (!string.IsNullOrEmpty(_filterText))
            {
                _poolMetrics = _poolMetrics.Where(metrics =>
                {
                    string poolName = metrics.TryGetValue("PoolName", out object nameObj) ? (string)nameObj : string.Empty;
                    string poolType = metrics.TryGetValue("PoolType", out object typeObj) ? (string)typeObj : string.Empty;
                    string itemType = metrics.TryGetValue("ItemType", out object itemObj) ? (string)itemObj : string.Empty;
                    
                    return poolName.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           poolType.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           itemType.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0;
                }).ToList();
            }
            
            // Apply type filter
            if (_typeFilter != PoolTypeFilter.All)
            {
                _poolMetrics = _poolMetrics.Where(metrics =>
                {
                    string poolType = metrics.TryGetValue("PoolType", out object typeObj) ? (string)typeObj : string.Empty;
                    string itemType = metrics.TryGetValue("ItemType", out object itemObj) ? (string)itemObj : string.Empty;
                    
                    switch (_typeFilter)
                    {
                        case PoolTypeFilter.GameObject:
                            return itemType.Contains("GameObject");
                        case PoolTypeFilter.Component:
                            return itemType.Contains("Component") && !itemType.Contains("ParticleSystem");
                        case PoolTypeFilter.ParticleSystem:
                            return itemType.Contains("ParticleSystem");
                        case PoolTypeFilter.Native:
                            return poolType.Contains("Native") || itemType.Contains("Native");
                        default:
                            return true;
                    }
                }).ToList();
            }
            
            // Filter inactive pools if needed
            if (!_showInactivePools)
            {
                _poolMetrics = _poolMetrics.Where(metrics =>
                {
                    int activeCount = metrics.TryGetValue("ActiveCount", out object activeObj) ? Convert.ToInt32(activeObj) : 0;
                    return activeCount > 0;
                }).ToList();
            }
        }
        
        /// <summary>
        /// Sorts the pool metrics based on current sort settings
        /// </summary>
        private void SortPoolMetrics()
        {
            switch (_sortBy)
            {
                case SortOption.Name:
                    _poolMetrics = _poolMetrics.OrderBy(m => m.TryGetValue("PoolName", out object name) ? (string)name : string.Empty).ToList();
                    break;
                case SortOption.MemoryUsage:
                    _poolMetrics = _poolMetrics.OrderBy(m => m.TryGetValue("EstimatedMemoryBytes", out object mem) ? Convert.ToInt64(mem) : 0).ToList();
                    break;
                case SortOption.ActiveItems:
                    _poolMetrics = _poolMetrics.OrderBy(m => m.TryGetValue("ActiveCount", out object active) ? Convert.ToInt32(active) : 0).ToList();
                    break;
                case SortOption.TotalItems:
                    _poolMetrics = _poolMetrics.OrderBy(m => m.TryGetValue("TotalItems", out object total) ? Convert.ToInt32(total) : 0).ToList();
                    break;
                case SortOption.LeakedItems:
                    _poolMetrics = _poolMetrics.OrderBy(m => m.TryGetValue("LeakedItems", out object leaked) ? Convert.ToInt32(leaked) : 0).ToList();
                    break;
                case SortOption.AverageLifetime:
                    _poolMetrics = _poolMetrics.OrderBy(m => m.TryGetValue("AverageObjectLifetimeSec", out object lifetime) ? Convert.ToSingle(lifetime) : 0).ToList();
                    break;
            }
            
            if (_sortDescending)
            {
                _poolMetrics.Reverse();
            }
        }
        
        /// <summary>
        /// Categorizes pool metrics by type for display organization
        /// </summary>
        /// <returns>Dictionary with categories as keys and lists of pool metrics as values</returns>
        private Dictionary<string, List<Dictionary<string, object>>> CategorizePoolMetrics()
        {
            var result = new Dictionary<string, List<Dictionary<string, object>>>();
            
            foreach (var metrics in _poolMetrics)
            {
                string poolType = metrics.TryGetValue("PoolType", out object typeObj) ? (string)typeObj : string.Empty;
                string itemType = metrics.TryGetValue("ItemType", out object itemObj) ? (string)itemObj : string.Empty;
                
                string category = "Other";
                
                if (itemType.Contains("GameObject"))
                {
                    category = "GameObject Pools";
                }
                else if (itemType.Contains("Component"))
                {
                    category = "Component Pools";
                }
                else if (itemType.Contains("ParticleSystem"))
                {
                    category = "Particle System Pools";
                }
                else if (poolType.Contains("Native") || itemType.Contains("Native"))
                {
                    category = "Native Pools";
                }
                
                if (!result.ContainsKey(category))
                {
                    result[category] = new List<Dictionary<string, object>>();
                }
                
                result[category].Add(metrics);
            }
            
            return result;
        }
    }
}
#endif