#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Pooling.Core.Pooling.Native;
using AhBearStudios.Pooling.Core.Pooling.Unity;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace AhBearStudios.Pooling.Core.Pooling.Editor
{
    /// <summary>
    /// Editor window for visualizing all registered pools in the project.
    /// Provides real-time monitoring of pool statistics, memory usage, performance metrics,
    /// and health diagnostics.
    /// </summary>
    public partial class PoolVisualizerWindow : EditorWindow
    {
        // UI state tracking
        private Vector2 _scrollPosition;
        private bool _showInactivePools = true;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private float _lastRefreshTime;
        
        // Display options
        private bool _showPerformanceMetrics = true;
        private bool _showMemoryUsage = true;
        private bool _showHealthIssues = true;
        private bool _showCharts = true;
        private bool _showGlobalStats = true;
        
        // Metrics and filtering
        private List<Dictionary<string, object>> _poolMetrics = new List<Dictionary<string, object>>();
        private string _filterText = "";
        private PoolTypeFilter _typeFilter = PoolTypeFilter.All;
        
        // Health issue tracking
        private List<PoolHealthIssue> _healthIssues = new List<PoolHealthIssue>();
        
        // UI grouping and foldouts
        private Dictionary<string, bool> _categoryFoldouts = new Dictionary<string, bool>();
        
        // Sort options
        private enum SortOption
        {
            Name,
            MemoryUsage,
            ActiveItems,
            TotalItems,
            LeakedItems,
            AverageLifetime
        }
        private SortOption _sortBy = SortOption.ActiveItems;
        private bool _sortDescending = true;
        
        // View tabs
        private enum ViewTab
        {
            Overview,
            Details,
            Performance,
            Health
        }
        private ViewTab _currentTab = ViewTab.Overview;
        
        /// <summary>
        /// Defines the filter options for pool types
        /// </summary>
        private enum PoolTypeFilter
        {
            All,
            GameObject,
            Component,
            ParticleSystem,
            Native
        }
        
        // Style cache
        private GUIStyle _headerStyle;
        private GUIStyle _boldLabelStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _activeTabStyle;
        private Texture2D _greenTexture;
        private Texture2D _yellowTexture;
        private Texture2D _redTexture;
        
        /// <summary>
        /// Opens the Pool Visualizer window
        /// </summary>
        [MenuItem("Tools/AhBear Studios/Pool Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<PoolVisualizerWindow>("Pool Visualizer");
        }
        
        /// <summary>
        /// Initialize when the window becomes enabled
        /// </summary>
        private void OnEnable()
        {
            _lastRefreshTime = 0;
            _greenTexture = MakeTexture(2, 2, new Color(0.2f, 0.8f, 0.2f));
            _yellowTexture = MakeTexture(2, 2, new Color(0.8f, 0.8f, 0.2f));
            _redTexture = MakeTexture(2, 2, new Color(0.8f, 0.2f, 0.2f));
            
            // Initialize PoolingServices if in play mode
            if (Application.isPlaying && !PoolingServices.HasService<PoolProfiler>())
            {
                PoolingServices.Initialize();
            }
            
            RefreshData();
        }
        
        /// <summary>
        /// Clean up resources when window is disabled
        /// </summary>
        private void OnDisable()
        {
            DestroyImmediate(_greenTexture);
            DestroyImmediate(_yellowTexture);
            DestroyImmediate(_redTexture);
        }
        
        /// <summary>
        /// Create a texture for UI elements
        /// </summary>
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Initialize styles
        /// </summary>
        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel);
                _headerStyle.alignment = TextAnchor.MiddleLeft;
                _headerStyle.fontSize = 14;
                _headerStyle.margin = new RectOffset(4, 4, 10, 4);
            }
            
            if (_boldLabelStyle == null)
            {
                _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            }
            
            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(EditorStyles.label);
                _warningStyle.normal.textColor = new Color(0.9f, 0.6f, 0.1f);
                _warningStyle.fontStyle = FontStyle.Bold;
            }
            
            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(EditorStyles.label);
                _errorStyle.normal.textColor = new Color(0.9f, 0.2f, 0.1f);
                _errorStyle.fontStyle = FontStyle.Bold;
            }
            
            if (_tabStyle == null)
            {
                _tabStyle = new GUIStyle(EditorStyles.toolbarButton);
                _tabStyle.fixedHeight = 25;
                _tabStyle.fontSize = 12;
            }
            
            if (_activeTabStyle == null)
            {
                _activeTabStyle = new GUIStyle(_tabStyle);
                _activeTabStyle.normal.background = EditorGUIUtility.whiteTexture;
                _activeTabStyle.normal.textColor = Color.black;
            }
        }
        
        /// <summary>
        /// Draws the GUI for the window
        /// </summary>
        private void OnGUI()
        {
            InitializeStyles();
            
            // Auto refresh handling
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                RefreshData();
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
                Repaint();
            }
            
            DrawToolbar();
            DrawTabsHeader();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_currentTab)
            {
                case ViewTab.Overview:
                    if (_showGlobalStats) DrawGlobalStats();
                    DrawPoolMetricsTable();
                    break;
                case ViewTab.Details:
                    DrawPoolDetails();
                    break;
                case ViewTab.Performance:
                    DrawPerformanceView();
                    break;
                case ViewTab.Health:
                    DrawHealthView();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Gets color based on pool usage ratio
        /// </summary>
        /// <param name="usage">Usage ratio (0.0-1.0)</param>
        /// <returns>Color representing the usage level</returns>
        private Color GetUsageColor(float usage)
        {
            // Green (0%) to Yellow (70%) to Red (100%)
            if (usage < 0.7f)
            {
                // Lerp from green to yellow
                return Color.Lerp(Color.green, Color.yellow, usage / 0.7f);
            }
            else
            {
                // Lerp from yellow to red
                return Color.Lerp(Color.yellow, Color.red, (usage - 0.7f) / 0.3f);
            }
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