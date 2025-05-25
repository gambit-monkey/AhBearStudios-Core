#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using AhBearStudios.Pooling.Core.Pooling.Core;
using UnityEditor;
using UnityEngine;

namespace AhBearStudios.Pooling.Core.Pooling.Editor
{
    /// <summary>
    /// Partial class handling UI functionality for the pool visualizer
    /// </summary>
    public partial class PoolVisualizerWindow
    {
        private Dictionary<string, object> _selectedPoolMetrics;

        /// <summary>
        /// Draws the tabs header for switching between different views
        /// </summary>
        private void DrawTabsHeader()
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle tabStyle = _tabStyle;
            GUIStyle activeTabStyle = _activeTabStyle;

            if (GUILayout.Toggle(_currentTab == ViewTab.Overview, "Overview",
                    _currentTab == ViewTab.Overview ? activeTabStyle : tabStyle))
            {
                _currentTab = ViewTab.Overview;
            }

            if (GUILayout.Toggle(_currentTab == ViewTab.Details, "Pool Details",
                    _currentTab == ViewTab.Details ? activeTabStyle : tabStyle))
            {
                _currentTab = ViewTab.Details;
            }

            if (GUILayout.Toggle(_currentTab == ViewTab.Performance, "Performance",
                    _currentTab == ViewTab.Performance ? activeTabStyle : tabStyle))
            {
                _currentTab = ViewTab.Performance;
            }

            if (GUILayout.Toggle(_currentTab == ViewTab.Health, "Health",
                    _currentTab == ViewTab.Health ? activeTabStyle : tabStyle))
            {
                _currentTab = ViewTab.Health;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the toolbar with filter and refresh options
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Filter
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
            string newFilter = EditorGUILayout.TextField(_filterText, GUILayout.Width(150));
            if (newFilter != _filterText)
            {
                _filterText = newFilter;
                ApplyFilters();
                SortPoolMetrics();
            }

            // Type filter
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(40));
            PoolTypeFilter newTypeFilter = (PoolTypeFilter)EditorGUILayout.EnumPopup(_typeFilter, GUILayout.Width(100));
            if (newTypeFilter != _typeFilter)
            {
                _typeFilter = newTypeFilter;
                ApplyFilters();
                SortPoolMetrics();
            }

            // Sort by
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sort:", GUILayout.Width(40));
            SortOption newSortBy = (SortOption)EditorGUILayout.EnumPopup(_sortBy, GUILayout.Width(100));
            if (newSortBy != _sortBy)
            {
                _sortBy = newSortBy;
                SortPoolMetrics();
            }

            // Ascending/Descending
            bool newSortDescending =
                GUILayout.Toggle(_sortDescending, "Desc", EditorStyles.toolbarButton, GUILayout.Width(40));
            if (newSortDescending != _sortDescending)
            {
                _sortDescending = newSortDescending;
                SortPoolMetrics();
            }

            GUILayout.FlexibleSpace();

            // Options
            _showInactivePools = GUILayout.Toggle(_showInactivePools, "Show Inactive", EditorStyles.toolbarButton,
                GUILayout.Width(90));
            _showMemoryUsage =
                GUILayout.Toggle(_showMemoryUsage, "Memory", EditorStyles.toolbarButton, GUILayout.Width(70));
            _showPerformanceMetrics = GUILayout.Toggle(_showPerformanceMetrics, "Performance",
                EditorStyles.toolbarButton, GUILayout.Width(90));
            _showHealthIssues = GUILayout.Toggle(_showHealthIssues, "Health", EditorStyles.toolbarButton,
                GUILayout.Width(60));

            // Auto refresh
            bool newAutoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton,
                GUILayout.Width(90));
            if (newAutoRefresh != _autoRefresh)
            {
                _autoRefresh = newAutoRefresh;
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
            }

            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the global statistics view
        /// </summary>
        private void DrawGlobalStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Global Statistics", _headerStyle);

            // Basic stats
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Pools:", GUILayout.Width(100));
            EditorGUILayout.LabelField(_poolMetrics.Count.ToString());
            EditorGUILayout.EndHorizontal();

            // Active items across all pools
            int totalActiveItems = 0;
            int totalCapacity = 0;
            long totalMemoryUsage = 0;

            foreach (var metrics in _poolMetrics)
            {
                totalActiveItems += metrics.TryGetValue("ActiveCount", out object activeObj)
                    ? Convert.ToInt32(activeObj)
                    : 0;
                totalCapacity += metrics.TryGetValue("TotalItems", out object totalObj) ? Convert.ToInt32(totalObj) : 0;
                totalMemoryUsage += metrics.TryGetValue("EstimatedMemoryBytes", out object memObj)
                    ? Convert.ToInt64(memObj)
                    : 0;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Items:", GUILayout.Width(100));
            EditorGUILayout.LabelField(
                $"{totalActiveItems:N0} / {totalCapacity:N0} ({(totalCapacity > 0 ? (float)totalActiveItems / totalCapacity * 100 : 0):F1}%)");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Memory Usage:", GUILayout.Width(100));
            EditorGUILayout.LabelField(FormatMemorySize(totalMemoryUsage));
            EditorGUILayout.EndHorizontal();

            // Health summary
            int totalHealthIssues = _healthIssues?.Count ?? 0;
            int highSeverityIssues = _healthIssues?.Count(i => i.Severity >= 75) ?? 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Health Issues:", GUILayout.Width(100));

            if (totalHealthIssues == 0)
            {
                EditorGUILayout.LabelField("No issues detected", EditorStyles.boldLabel);
            }
            else
            {
                GUIStyle style = highSeverityIssues > 0 ? _errorStyle : _warningStyle;
                EditorGUILayout.LabelField($"{totalHealthIssues} issues ({highSeverityIssues} critical)", style);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the detailed view for a selected pool
        /// </summary>
        private void DrawPoolDetails()
        {
            if (_selectedPoolMetrics == null)
            {
                EditorGUILayout.HelpBox("Select a pool from the Overview tab to view its details.", MessageType.Info);
                return;
            }

            string poolName = _selectedPoolMetrics.TryGetValue("PoolName", out object nameObj)
                ? (string)nameObj
                : "Unknown";
            string poolType = _selectedPoolMetrics.TryGetValue("PoolType", out object typeObj)
                ? (string)typeObj
                : "Unknown";
            string itemType = _selectedPoolMetrics.TryGetValue("ItemType", out object itemObj)
                ? (string)itemObj
                : "Unknown";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Pool: {poolName}", _headerStyle);
            EditorGUILayout.LabelField($"Pool Type: {poolType}");
            EditorGUILayout.LabelField($"Item Type: {itemType}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Basic metrics
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Usage Statistics", _boldLabelStyle);

            int activeCount = _selectedPoolMetrics.TryGetValue("ActiveCount", out object activeObj)
                ? Convert.ToInt32(activeObj)
                : 0;
            int totalItems = _selectedPoolMetrics.TryGetValue("TotalItems", out object totalObj)
                ? Convert.ToInt32(totalObj)
                : 0;
            int capacity = _selectedPoolMetrics.TryGetValue("Capacity", out object capacityObj)
                ? Convert.ToInt32(capacityObj)
                : 0;
            int leaked = _selectedPoolMetrics.TryGetValue("LeakedItems", out object leakedObj)
                ? Convert.ToInt32(leakedObj)
                : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Items:", GUILayout.Width(150));
            EditorGUILayout.LabelField(
                $"{activeCount} / {totalItems} ({(totalItems > 0 ? (float)activeCount / totalItems * 100 : 0):F1}%)");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Capacity:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{capacity}");
            EditorGUILayout.EndHorizontal();

            if (leaked > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Leaked Items:", GUILayout.Width(150));
                EditorGUILayout.LabelField($"{leaked}", _errorStyle);
                EditorGUILayout.EndHorizontal();
            }

            // Draw usage progress bar
            float usageRatio = totalItems > 0 ? (float)activeCount / totalItems : 0;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Usage:", GUILayout.Width(150));
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), usageRatio,
                $"{usageRatio * 100:F1}%");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Memory usage
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Memory Usage", _boldLabelStyle);

            long memoryUsage = _selectedPoolMetrics.TryGetValue("EstimatedMemoryBytes", out object memObj)
                ? Convert.ToInt64(memObj)
                : 0;
            long activeMemory = _selectedPoolMetrics.TryGetValue("EstimatedActiveMemoryBytes", out object activememObj)
                ? Convert.ToInt64(activememObj)
                : 0;
            int itemSize = _selectedPoolMetrics.TryGetValue("EstimatedItemSizeBytes", out object itemSizeObj)
                ? Convert.ToInt32(itemSizeObj)
                : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Memory:", GUILayout.Width(150));
            EditorGUILayout.LabelField(FormatMemorySize(memoryUsage));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Memory:", GUILayout.Width(150));
            EditorGUILayout.LabelField(FormatMemorySize(activeMemory));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Per Item Size:", GUILayout.Width(150));
            EditorGUILayout.LabelField(FormatMemorySize(itemSize));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Performance metrics
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Performance Metrics", _boldLabelStyle);

            float avgAcquireTime = _selectedPoolMetrics.TryGetValue("AverageAcquireTimeMs", out object acquireObj)
                ? Convert.ToSingle(acquireObj)
                : 0;
            float maxAcquireTime = _selectedPoolMetrics.TryGetValue("MaxAcquireTimeMs", out object maxAcquireObj)
                ? Convert.ToSingle(maxAcquireObj)
                : 0;
            float avgLifetime = _selectedPoolMetrics.TryGetValue("AverageObjectLifetimeSec", out object lifetimeObj)
                ? Convert.ToSingle(lifetimeObj)
                : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Avg Acquire Time:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{avgAcquireTime:F3} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Acquire Time:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{maxAcquireTime:F3} ms");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Avg Object Lifetime:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{avgLifetime:F2} seconds");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Health issues for this pool
            DrawPoolHealthIssues(poolName);
        }

        /// <summary>
        /// Draws health issues for a specific pool
        /// </summary>
        private void DrawPoolHealthIssues(string poolName)
        {
            if (_healthIssues == null || _healthIssues.Count == 0)
            {
                return;
            }

            var poolIssues = _healthIssues.Where(i => i.PoolName == poolName).ToList();
            if (poolIssues.Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Health Issues", _boldLabelStyle);

            foreach (var issue in poolIssues)
            {
                GUIStyle style = issue.Severity >= 75 ? _errorStyle : _warningStyle;
                EditorGUILayout.LabelField($"{issue.IssueType} ({issue.Severity}/100):", style);
                EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the performance view
        /// </summary>
        private void DrawPerformanceView()
        {
            if (PoolingServices.Profiler == null)
            {
                EditorGUILayout.HelpBox("Pool Profiler service is not available.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pool Performance Analysis", _headerStyle);

            // Get samples from profiler
            var samples = PoolingServices.Profiler.GetSamples();

            if (samples.Count == 0)
            {
                EditorGUILayout.HelpBox("No performance data collected yet. Use pools in play mode to collect data.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Show operation stats
            EditorGUILayout.LabelField("Operation Performance", _boldLabelStyle);

            var acquireStats = PoolingServices.Profiler.GetOperationStats("Acquire");
            var releaseStats = PoolingServices.Profiler.GetOperationStats("Release");
            var createStats = PoolingServices.Profiler.GetOperationStats("Create");
            var expandStats = PoolingServices.Profiler.GetOperationStats("Expand");

            DrawPerformanceStats("Acquire", acquireStats);
            DrawPerformanceStats("Release", releaseStats);
            DrawPerformanceStats("Create", createStats);
            DrawPerformanceStats("Expand", expandStats);

            EditorGUILayout.EndVertical();

            // Pool-specific performance if a pool is selected
            if (_selectedPoolMetrics != null)
            {
                string poolName = _selectedPoolMetrics.TryGetValue("PoolName", out object nameObj)
                    ? (string)nameObj
                    : null;

                if (!string.IsNullOrEmpty(poolName))
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Performance for: {poolName}", _boldLabelStyle);

                    var poolStats = PoolingServices.Profiler.GetPoolStats(poolName);
                    DrawPerformanceStats("All Operations", poolStats);

                    EditorGUILayout.EndVertical();
                }
            }

            // Button to reset performance metrics
            if (GUILayout.Button("Reset Performance Metrics"))
            {
                if (PoolingServices.Profiler != null)
                {
                    PoolingServices.Profiler.ClearSamples();
                }

                if (PoolingServices.Diagnostics != null)
                {
                    PoolingServices.Diagnostics.ResetPerformanceMetrics();
                }
            }
        }

        /// <summary>
        /// Draws performance statistics
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="stats">Performance stats (min, max, avg)</param>
        private void DrawPerformanceStats(string operationName, (float min, float max, float avg) stats)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(operationName, GUILayout.Width(100));

            if (stats.min == 0 && stats.max == 0 && stats.avg == 0)
            {
                EditorGUILayout.LabelField("No data");
            }
            else
            {
                EditorGUILayout.LabelField($"Min: {stats.min:F3}ms | Avg: {stats.avg:F3}ms | Max: {stats.max:F3}ms");
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the health view
        /// </summary>
        private void DrawHealthView()
        {
            if (PoolingServices.HealthChecker == null)
            {
                EditorGUILayout.HelpBox("Pool Health Checker service is not available.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pool Health Analysis", _headerStyle);

            if (_healthIssues == null || _healthIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("No health issues detected.", MessageType.Info);
                EditorGUILayout.EndVertical();

                // Draw health check options
                DrawHealthCheckOptions();

                return;
            }

            // Group issues by pool and severity
            var issuesByPool = _healthIssues.GroupBy(i => i.PoolName).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var poolName in issuesByPool.Keys)
            {
                var poolIssues = issuesByPool[poolName];
                int maxSeverity = poolIssues.Max(i => i.Severity);

                GUIStyle foldoutStyle = maxSeverity >= 75 ? _errorStyle : _warningStyle;
                bool isSelectedPool = _selectedPoolMetrics != null &&
                                      _selectedPoolMetrics.TryGetValue("PoolName", out object selectedName) &&
                                      (string)selectedName == poolName;

                string foldoutKey = $"healthFoldout_{poolName}";
                if (!_categoryFoldouts.ContainsKey(foldoutKey))
                {
                    _categoryFoldouts[foldoutKey] = isSelectedPool; // Auto-expand if this is the selected pool
                }

                _categoryFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                    _categoryFoldouts[foldoutKey],
                    $"{poolName} ({poolIssues.Count} issues, max severity: {maxSeverity})",
                    true,
                    foldoutStyle);

                if (_categoryFoldouts[foldoutKey])
                {
                    EditorGUI.indentLevel++;

                    foreach (var issue in poolIssues.OrderByDescending(i => i.Severity))
                    {
                        GUIStyle issueStyle = issue.Severity >= 75 ? _errorStyle : _warningStyle;
                        EditorGUILayout.LabelField($"{issue.IssueType} (Severity: {issue.Severity}/100)", issueStyle);
                        EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.Space();
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();

            // Draw health check options
            DrawHealthCheckOptions();
        }

        /// <summary>
        /// Draws health check options
        /// </summary>
        private void DrawHealthCheckOptions()
        {
            if (PoolingServices.HealthChecker == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Health Check Options", _boldLabelStyle);

            // Check interval
            float checkInterval = 5f;
            if (Application.isPlaying)
            {
                // Try to get the current interval from the health checker
                // This is a placeholder - implement a proper way to get the current interval
                checkInterval = 5f; // Default value
            }

            float newCheckInterval = EditorGUILayout.Slider("Check Interval", checkInterval, 0.1f, 10f);
            if (Math.Abs(newCheckInterval - checkInterval) > 0.01f)
            {
                if (Application.isPlaying)
                {
                    PoolingServices.HealthChecker.SetCheckInterval(newCheckInterval);
                }
            }

            // Alert settings - use default values since we can't access the serialized properties directly
            bool alertOnLeaks = true;
            bool alertOnHighUsage = true;
            bool logWarnings = true;

            // In a real implementation, you might want to cache these values or retrieve them from HealthChecker

            bool newAlertOnLeaks = EditorGUILayout.Toggle("Alert on Leaks", alertOnLeaks);
            bool newAlertOnHighUsage = EditorGUILayout.Toggle("Alert on High Usage", alertOnHighUsage);
            bool newLogWarnings = EditorGUILayout.Toggle("Log Warnings", logWarnings);

            if (newAlertOnLeaks != alertOnLeaks || newAlertOnHighUsage != alertOnHighUsage ||
                newLogWarnings != logWarnings)
            {
                if (Application.isPlaying)
                {
                    PoolingServices.HealthChecker.SetAlertFlags(newAlertOnLeaks, newAlertOnHighUsage, newLogWarnings);
                }
            }

            // Manual check button
            if (GUILayout.Button("Run Health Check Now"))
            {
                if (Application.isPlaying && PoolingServices.HealthChecker != null)
                {
                    PoolingServices.HealthChecker.CheckAllPools();
                    RefreshData();
                }
            }

            // Clear issues button
            if (GUILayout.Button("Clear Health Issues"))
            {
                if (PoolingServices.HealthChecker != null)
                {
                    PoolingServices.HealthChecker.ClearIssues();
                    _healthIssues.Clear();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif