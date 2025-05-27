using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using AhBearStudios.Core.Profiling.Data;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Runtime UI component for visualizing profiler metrics and data.
    /// Provides real-time display of performance metrics with configurable layout.
    /// </summary>
    public class RuntimeProfilerUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _rootPanel;
        [SerializeField] private Text _headerText;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private ScrollRect _metricsScrollView;
        [SerializeField] private RectTransform _metricsContainer;
        [SerializeField] private RectTransform _sessionsContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _metricItemPrefab;
        [SerializeField] private GameObject _sessionItemPrefab;
        [SerializeField] private GameObject _graphItemPrefab;
        
        [Header("Layout Settings")]
        [SerializeField] private bool _showMetrics = true;
        [SerializeField] private bool _showSessions = true;
        [SerializeField] private bool _showGraphs = false;
        [SerializeField] private int _maxVisibleItems = 20;
        [SerializeField] private float _itemHeight = 25f;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _normalColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private float _warningThreshold = 16.67f; // 60 FPS
        [SerializeField] private float _criticalThreshold = 33.33f; // 30 FPS
        
        [Header("Update Settings")]
        [SerializeField] private float _updateInterval = 0.5f;
        [SerializeField] private bool _autoUpdate = true;
        
        private ProfileManager _profileManager;
        private readonly Dictionary<ProfilerTag, MetricDisplayItem> _metricItems = new Dictionary<ProfilerTag, MetricDisplayItem>();
        private readonly Dictionary<ProfilerTag, SessionDisplayItem> _sessionItems = new Dictionary<ProfilerTag, SessionDisplayItem>();
        private readonly Dictionary<ProfilerTag, GraphDisplayItem> _graphItems = new Dictionary<ProfilerTag, GraphDisplayItem>();
        
        private float _lastUpdateTime;
        private bool _isVisible = true;
        private bool _isInitialized;
        
        /// <summary>
        /// Gets whether the UI is currently visible
        /// </summary>
        public bool IsVisible => _isVisible;
        
        /// <summary>
        /// Gets the current update interval
        /// </summary>
        public float UpdateInterval => _updateInterval;
        
        /// <summary>
        /// Event fired when UI visibility changes
        /// </summary>
        public event Action<bool> VisibilityChanged;
        
        private void Awake()
        {
            InitializeUI();
        }
        
        private void Start()
        {
            FindProfileManager();
            
            if (_profileManager != null)
            {
                SubscribeToEvents();
                _isInitialized = true;
            }
            else
            {
                Debug.LogWarning("[RuntimeProfilerUI] No ProfileManager found in scene");
            }
        }
        
        private void Update()
        {
            if (!_isInitialized || !_autoUpdate || !_isVisible)
                return;
                
            if (Time.unscaledTime - _lastUpdateTime >= _updateInterval)
            {
                UpdateDisplay();
                _lastUpdateTime = Time.unscaledTime;
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// Initializes UI components
        /// </summary>
        private void InitializeUI()
        {
            // Ensure we have required components
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
                
            if (_rootPanel == null)
                _rootPanel = GetComponent<RectTransform>();
                
            // Set up canvas for overlay
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 1000;
            }
            
            // Set up toggle button
            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(ToggleVisibility);
            }
            
            // Initialize header
            UpdateHeader();
            
            // Set initial visibility
            SetVisibility(_isVisible);
        }
        
        /// <summary>
        /// Finds the ProfileManager in the scene
        /// </summary>
        private void FindProfileManager()
        {
            _profileManager = FindObjectOfType<ProfileManager>();
            
            if (_profileManager == null)
            {
                // Try to get the singleton instance
                _profileManager = ProfileManager.Instance;
            }
        }
        
        /// <summary>
        /// Subscribes to ProfileManager events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_profileManager == null)
                return;
                
            _profileManager.ProfilingStarted += OnProfilingStarted;
            _profileManager.ProfilingStopped += OnProfilingStopped;
        }
        
        /// <summary>
        /// Unsubscribes from ProfileManager events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_profileManager == null)
                return;
                
            _profileManager.ProfilingStarted -= OnProfilingStarted;
            _profileManager.ProfilingStopped -= OnProfilingStopped;
        }
        
        /// <summary>
        /// Updates the main display
        /// </summary>
        private void UpdateDisplay()
        {
            if (_profileManager == null)
                return;
                
            UpdateHeader();
            
            if (_showMetrics)
                UpdateMetricsDisplay();
                
            if (_showSessions)
                UpdateSessionsDisplay();
                
            if (_showGraphs)
                UpdateGraphsDisplay();
        }
        
        /// <summary>
        /// Updates the header text
        /// </summary>
        private void UpdateHeader()
        {
            if (_headerText == null)
                return;
                
            string status = _profileManager?.IsEnabled == true ? "ACTIVE" : "INACTIVE";
            float fps = 1.0f / Time.unscaledDeltaTime;
            float frameTime = Time.unscaledDeltaTime * 1000f;
            
            _headerText.text = $"PROFILER ({status}) | {fps:F1} FPS | {frameTime:F1}ms";
            _headerText.color = GetColorForFrameTime(frameTime);
        }
        
        /// <summary>
        /// Updates the metrics display
        /// </summary>
        private void UpdateMetricsDisplay()
        {
            if (_profileManager?.SystemMetrics == null)
                return;
                
            var metrics = _profileManager.SystemMetrics.GetAllMetrics();
            var displayedCount = 0;
            
            foreach (var metric in metrics)
            {
                if (displayedCount >= _maxVisibleItems)
                    break;
                    
                UpdateMetricItem(metric);
                displayedCount++;
            }
            
            // Remove obsolete items
            RemoveObsoleteMetricItems(metrics.Select(m => m.Tag).ToHashSet());
        }
        
        /// <summary>
        /// Updates the sessions display
        /// </summary>
        private void UpdateSessionsDisplay()
        {
            if (_profileManager == null)
                return;
                
            var allMetrics = _profileManager.GetAllMetrics();
            var displayedCount = 0;
            
            foreach (var kvp in allMetrics)
            {
                if (displayedCount >= _maxVisibleItems)
                    break;
                    
                UpdateSessionItem(kvp.Key, kvp.Value);
                displayedCount++;
            }
            
            // Remove obsolete items
            RemoveObsoleteSessionItems(allMetrics.Keys.ToHashSet());
        }
        
        /// <summary>
        /// Updates the graphs display
        /// </summary>
        private void UpdateGraphsDisplay()
        {
            // Implementation for graph display would go here
            // This could show mini-graphs of metrics over time
        }
        
        /// <summary>
        /// Updates or creates a metric display item
        /// </summary>
        /// <param name="metric">The system metric to display</param>
        private void UpdateMetricItem(SystemMetric metric)
        {
            if (!_metricItems.TryGetValue(metric.Tag, out var item))
            {
                item = CreateMetricItem(metric);
                _metricItems[metric.Tag] = item;
            }
            
            if (item != null)
            {
                UpdateMetricItemValues(item, metric);
            }
        }
        
        /// <summary>
        /// Updates or creates a session display item
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="metrics">The metrics data</param>
        private void UpdateSessionItem(ProfilerTag tag, DefaultMetricsData metrics)
        {
            if (!_sessionItems.TryGetValue(tag, out var item))
            {
                item = CreateSessionItem(tag);
                _sessionItems[tag] = item;
            }
            
            if (item != null)
            {
                UpdateSessionItemValues(item, metrics);
            }
        }
        
        /// <summary>
        /// Creates a new metric display item
        /// </summary>
        /// <param name="metric">The system metric</param>
        /// <returns>The created display item</returns>
        private MetricDisplayItem CreateMetricItem(SystemMetric metric)
        {
            if (_metricItemPrefab == null || _metricsContainer == null)
                return null;
                
            var instance = Instantiate(_metricItemPrefab, _metricsContainer);
            var rectTransform = instance.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _itemHeight);
            
            var item = new MetricDisplayItem
            {
                GameObject = instance,
                NameText = instance.transform.Find("NameText")?.GetComponent<Text>(),
                ValueText = instance.transform.Find("ValueText")?.GetComponent<Text>(),
                UnitText = instance.transform.Find("UnitText")?.GetComponent<Text>(),
                ProgressBar = instance.transform.Find("ProgressBar")?.GetComponent<Slider>(),
                BackgroundImage = instance.GetComponent<Image>()
            };
            
            // Set up the name and unit
            if (item.NameText != null)
                item.NameText.text = metric.Tag.Name;
                
            if (item.UnitText != null)
                item.UnitText.text = metric.Unit;
            
            return item;
        }
        
        /// <summary>
        /// Creates a new session display item
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <returns>The created display item</returns>
        private SessionDisplayItem CreateSessionItem(ProfilerTag tag)
        {
            if (_sessionItemPrefab == null || _sessionsContainer == null)
                return null;
                
            var instance = Instantiate(_sessionItemPrefab, _sessionsContainer);
            var rectTransform = instance.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _itemHeight);
            
            var item = new SessionDisplayItem
            {
                GameObject = instance,
                NameText = instance.transform.Find("NameText")?.GetComponent<Text>(),
                ValueText = instance.transform.Find("ValueText")?.GetComponent<Text>(),
                AvgText = instance.transform.Find("AvgText")?.GetComponent<Text>(),
                MaxText = instance.transform.Find("MaxText")?.GetComponent<Text>(),
                ProgressBar = instance.transform.Find("ProgressBar")?.GetComponent<Slider>(),
                BackgroundImage = instance.GetComponent<Image>()
            };
            
            // Set up the name
            if (item.NameText != null)
                item.NameText.text = tag.Name;
            
            return item;
        }
        
        /// <summary>
        /// Updates the values of a metric display item
        /// </summary>
        /// <param name="item">The display item</param>
        /// <param name="metric">The system metric</param>
        private void UpdateMetricItemValues(MetricDisplayItem item, SystemMetric metric)
        {
            if (item.ValueText != null)
            {
                item.ValueText.text = $"{metric.LastValue:F2}";
                item.ValueText.color = GetColorForValue(metric.LastValue);
            }
            
            if (item.ProgressBar != null)
            {
                float normalizedValue = GetNormalizedValue(metric.LastValue);
                item.ProgressBar.value = normalizedValue;
                
                var fillImage = item.ProgressBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = GetColorForValue(metric.LastValue);
            }
            
            if (item.BackgroundImage != null)
            {
                var color = item.BackgroundImage.color;
                color.a = 0.1f;
                item.BackgroundImage.color = color;
            }
        }
        
        /// <summary>
        /// Updates the values of a session display item
        /// </summary>
        /// <param name="item">The display item</param>
        /// <param name="metrics">The metrics data</param>
        private void UpdateSessionItemValues(SessionDisplayItem item, DefaultMetricsData metrics)
        {
            if (item.ValueText != null)
            {
                item.ValueText.text = $"{metrics.LastValue:F2}ms";
                item.ValueText.color = GetColorForValue(metrics.LastValue);
            }
            
            if (item.AvgText != null)
            {
                item.AvgText.text = $"Avg: {metrics.AverageValue:F2}ms";
            }
            
            if (item.MaxText != null)
            {
                item.MaxText.text = $"Max: {metrics.MaxValue:F2}ms";
            }
            
            if (item.ProgressBar != null)
            {
                float normalizedValue = GetNormalizedValue(metrics.LastValue);
                item.ProgressBar.value = normalizedValue;
                
                var fillImage = item.ProgressBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = GetColorForValue(metrics.LastValue);
            }
        }
        
        /// <summary>
        /// Removes obsolete metric items
        /// </summary>
        /// <param name="activeTags">Set of currently active tags</param>
        private void RemoveObsoleteMetricItems(HashSet<ProfilerTag> activeTags)
        {
            var toRemove = new List<ProfilerTag>();
            
            foreach (var kvp in _metricItems)
            {
                if (!activeTags.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                    if (kvp.Value?.GameObject != null)
                        Destroy(kvp.Value.GameObject);
                }
            }
            
            foreach (var tag in toRemove)
            {
                _metricItems.Remove(tag);
            }
        }
        
        /// <summary>
        /// Removes obsolete session items
        /// </summary>
        /// <param name="activeTags">Set of currently active tags</param>
        private void RemoveObsoleteSessionItems(HashSet<ProfilerTag> activeTags)
        {
            var toRemove = new List<ProfilerTag>();
            
            foreach (var kvp in _sessionItems)
            {
                if (!activeTags.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                    if (kvp.Value?.GameObject != null)
                        Destroy(kvp.Value.GameObject);
                }
            }
            
            foreach (var tag in toRemove)
            {
                _sessionItems.Remove(tag);
            }
        }
        
        /// <summary>
        /// Gets a color based on the metric value
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <returns>Color representing the performance level</returns>
        private Color GetColorForValue(double value)
        {
            if (value >= _criticalThreshold)
                return _criticalColor;
            else if (value >= _warningThreshold)
                return _warningColor;
            else
                return _normalColor;
        }
        
        /// <summary>
        /// Gets a color specifically for frame time
        /// </summary>
        /// <param name="frameTimeMs">Frame time in milliseconds</param>
        /// <returns>Color representing the performance level</returns>
        private Color GetColorForFrameTime(float frameTimeMs)
        {
            return GetColorForValue(frameTimeMs);
        }
        
        /// <summary>
        /// Gets a normalized value (0-1) for progress bars
        /// </summary>
        /// <param name="value">The raw value</param>
        /// <returns>Normalized value between 0 and 1</returns>
        private float GetNormalizedValue(double value)
        {
            float maxValue = _criticalThreshold * 1.5f;
            return Mathf.Clamp01((float)value / maxValue);
        }
        
        /// <summary>
        /// Toggles UI visibility
        /// </summary>
        public void ToggleVisibility()
        {
            SetVisibility(!_isVisible);
        }
        
        /// <summary>
        /// Sets UI visibility
        /// </summary>
        /// <param name="visible">Whether the UI should be visible</param>
        public void SetVisibility(bool visible)
        {
            _isVisible = visible;
            
            if (_rootPanel != null)
                _rootPanel.gameObject.SetActive(_isVisible);
                
            VisibilityChanged?.Invoke(_isVisible);
        }
        
        /// <summary>
        /// Sets the update interval
        /// </summary>
        /// <param name="interval">Update interval in seconds</param>
        public void SetUpdateInterval(float interval)
        {
            _updateInterval = Mathf.Max(0.1f, interval);
        }
        
        /// <summary>
        /// Sets whether to show metrics
        /// </summary>
        /// <param name="show">Whether to show metrics</param>
        public void SetShowMetrics(bool show)
        {
            _showMetrics = show;
            
            if (_metricsContainer != null)
                _metricsContainer.gameObject.SetActive(show);
        }
        
        /// <summary>
        /// Sets whether to show sessions
        /// </summary>
        /// <param name="show">Whether to show sessions</param>
        public void SetShowSessions(bool show)
        {
            _showSessions = show;
            
            if (_sessionsContainer != null)
                _sessionsContainer.gameObject.SetActive(show);
        }
        
        /// <summary>
        /// Forces an immediate update of the display
        /// </summary>
        public void ForceUpdate()
        {
            UpdateDisplay();
        }
        
        /// <summary>
        /// Clears all display items
        /// </summary>
        public void ClearDisplay()
        {
            // Clear metric items
            foreach (var item in _metricItems.Values)
            {
                if (item?.GameObject != null)
                    Destroy(item.GameObject);
            }
            _metricItems.Clear();
            
            // Clear session items
            foreach (var item in _sessionItems.Values)
            {
                if (item?.GameObject != null)
                    Destroy(item.GameObject);
            }
            _sessionItems.Clear();
            
            // Clear graph items
            foreach (var item in _graphItems.Values)
            {
                if (item?.GameObject != null)
                    Destroy(item.GameObject);
            }
            _graphItems.Clear();
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Handles profiling started event
        /// </summary>
        /// <param name="profileManager">The profile manager</param>
        private void OnProfilingStarted(ProfileManager profileManager)
        {
            UpdateHeader();
        }
        
        /// <summary>
        /// Handles profiling stopped event
        /// </summary>
        /// <param name="profileManager">The profile manager</param>
        private void OnProfilingStopped(ProfileManager profileManager)
        {
            UpdateHeader();
        }
        
        #endregion
        
        #region Display Item Classes
        
        /// <summary>
        /// Represents a metric display item in the UI
        /// </summary>
        private class MetricDisplayItem
        {
            public GameObject GameObject;
            public Text NameText;
            public Text ValueText;
            public Text UnitText;
            public Slider ProgressBar;
            public Image BackgroundImage;
        }
        
        /// <summary>
        /// Represents a session display item in the UI
        /// </summary>
        private class SessionDisplayItem
        {
            public GameObject GameObject;
            public Text NameText;
            public Text ValueText;
            public Text AvgText;
            public Text MaxText;
            public Slider ProgressBar;
            public Image BackgroundImage;
        }
        
        /// <summary>
        /// Represents a graph display item in the UI
        /// </summary>
        private class GraphDisplayItem
        {
            public GameObject GameObject;
            public Text NameText;
            public Image GraphImage;
            public RectTransform GraphContainer;
        }
        
        #endregion
    }
}