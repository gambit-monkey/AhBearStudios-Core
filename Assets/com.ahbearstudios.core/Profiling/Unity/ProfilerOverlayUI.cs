using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Runtime UI for displaying profiler data
    /// </summary>
    public class ProfilerOverlayUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _container;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _fpsText;
        [SerializeField] private Text _frameTimeText;
        [SerializeField] private RectTransform _metricsContainer;
        [SerializeField] private RectTransform _sessionsContainer;
        [SerializeField] private GameObject _metricPrefab;
        [SerializeField] private GameObject _sessionPrefab;
        [SerializeField] private Toggle _autoUpdateToggle;
        [SerializeField] private Slider _refreshRateSlider;
        [SerializeField] private Text _refreshRateText;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Toggle _showDetailsToggle;
        
        [Header("Configuration")]
        [SerializeField] private float _refreshInterval = 0.5f;
        [SerializeField] private bool _autoUpdate = true;
        [SerializeField] private bool _showDetails = true;
        [SerializeField] private Color _normalColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private float _warningThreshold = 16.7f; // 60 FPS
        [SerializeField] private float _errorThreshold = 33.3f;   // 30 FPS
        
        // Reference to profiler manager
        private RuntimeProfilerManager _profilerManager;
        
        // UI elements for metrics
        private readonly Dictionary<ProfilerTag, MetricUIItem> _metricItems = new Dictionary<ProfilerTag, MetricUIItem>();
        
        // UI elements for sessions
        private readonly Dictionary<ProfilerTag, SessionUIItem> _sessionItems = new Dictionary<ProfilerTag, SessionUIItem>();
        
        // Tracking refresh timing
        private float _timeSinceLastRefresh;
        
        /// <summary>
        /// UI component for displaying a metric
        /// </summary>
        [Serializable]
        private class MetricUIItem
        {
            public GameObject Root;
            public Text NameText;
            public Text ValueText;
            public Text UnitText;
            public Slider ProgressBar;
            public Text AvgText;
            public Text MaxText;
        }
        
        /// <summary>
        /// UI component for displaying a session
        /// </summary>
        [Serializable]
        private class SessionUIItem
        {
            public GameObject Root;
            public Text NameText;
            public Text ValueText;
            public Slider ProgressBar;
            public Text AvgText;
            public Text MaxText;
        }
        
        private void Start()
        {
            // Get profiler manager
            _profilerManager = RuntimeProfilerManager.Instance;
            
            // Set up UI references if not already assigned
            if (_container == null)
                _container = GetComponent<RectTransform>();
                
            if (_titleText == null)
                _titleText = transform.Find("Title")?.GetComponent<Text>();
                
            if (_fpsText == null)
                _fpsText = transform.Find("FPSText")?.GetComponent<Text>();
                
            if (_frameTimeText == null)
                _frameTimeText = transform.Find("FrameTimeText")?.GetComponent<Text>();
                
            if (_metricsContainer == null)
                _metricsContainer = transform.Find("MetricsContainer")?.GetComponent<RectTransform>();
                
            if (_sessionsContainer == null)
                _sessionsContainer = transform.Find("SessionsContainer")?.GetComponent<RectTransform>();
                
            // Set up UI callbacks
            if (_autoUpdateToggle != null)
            {
                _autoUpdateToggle.isOn = _autoUpdate;
                _autoUpdateToggle.onValueChanged.AddListener(SetAutoUpdate);
            }
            
            if (_refreshRateSlider != null)
            {
                _refreshRateSlider.value = _refreshInterval;
                _refreshRateSlider.onValueChanged.AddListener(SetRefreshRate);
                UpdateRefreshRateText();
            }
            
            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(ResetStats);
            }
            
            if (_showDetailsToggle != null)
            {
                _showDetailsToggle.isOn = _showDetails;
                _showDetailsToggle.onValueChanged.AddListener(SetShowDetails);
            }
            
            // Initialize
            _timeSinceLastRefresh = _refreshInterval;
            
            // Register for profiler events
            _profilerManager.SessionCompleted += OnSessionCompleted;
        }
        
        private void OnDestroy()
        {
            // Unregister profiler events
            if (_profilerManager != null)
            {
                _profilerManager.SessionCompleted -= OnSessionCompleted;
            }
        }
        
        private void Update()
        {
            if (!_autoUpdate)
                return;
                
            _timeSinceLastRefresh += Time.unscaledDeltaTime;
            
            if (_timeSinceLastRefresh >= _refreshInterval)
            {
                RefreshUI();
                _timeSinceLastRefresh = 0f;
            }
        }
        
        /// <summary>
        /// Set auto update mode
        /// </summary>
        public void SetAutoUpdate(bool autoUpdate)
        {
            _autoUpdate = autoUpdate;
        }
        
        /// <summary>
        /// Set refresh rate
        /// </summary>
        public void SetRefreshRate(float interval)
        {
            _refreshInterval = interval;
            UpdateRefreshRateText();
        }
        
        /// <summary>
        /// Update refresh rate text
        /// </summary>
        private void UpdateRefreshRateText()
        {
            if (_refreshRateText != null)
            {
                _refreshRateText.text = $"{_refreshInterval:F1}s";
            }
        }
        
        /// <summary>
        /// Reset profiler stats
        /// </summary>
        public void ResetStats()
        {
            _profilerManager.ResetStats();
            _metricItems.Clear();
            _sessionItems.Clear();
            
            // Clear metric container
            foreach (Transform child in _metricsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Clear session container
            foreach (Transform child in _sessionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Set show details mode
        /// </summary>
        public void SetShowDetails(bool showDetails)
        {
            _showDetails = showDetails;
            
            // Update UI items to show/hide details
            foreach (var item in _metricItems.Values)
            {
                SetMetricDetailsVisible(item, _showDetails);
            }
            
            foreach (var item in _sessionItems.Values)
            {
                SetSessionDetailsVisible(item, _showDetails);
            }
        }
        
        /// <summary>
        /// Refresh the UI with current data
        /// </summary>
        public void RefreshUI()
        {
            // Update FPS display
            if (_fpsText != null)
            {
                float fps = 1.0f / Time.unscaledDeltaTime;
                _fpsText.text = $"{fps:F1} FPS";
                _fpsText.color = GetColorForValue(Time.unscaledDeltaTime * 1000f);
            }
            
            // Update frame time display
            if (_frameTimeText != null)
            {
                float frameTimeMs = Time.unscaledDeltaTime * 1000f;
                _frameTimeText.text = $"{frameTimeMs:F1} ms";
                _frameTimeText.color = GetColorForValue(frameTimeMs);
            }
            
            // Update metrics
            RefreshMetrics();
            
            // Update sessions
            RefreshSessions();
        }
        
        /// <summary>
        /// Refresh metrics display
        /// </summary>
        private void RefreshMetrics()
        {
            // Get all metrics
            var metrics = _profilerManager.SystemMetrics.GetAllMetrics();
            
            foreach (var metric in metrics)
            {
                // Create or get UI item
                if (!_metricItems.TryGetValue(metric.Tag, out var item))
                {
                    item = CreateMetricUIItem(metric);
                    _metricItems[metric.Tag] = item;
                }
                
                // Update values
                UpdateMetricUIItem(item, metric);
            }
        }
        
        /// <summary>
        /// Refresh sessions display
        /// </summary>
        private void RefreshSessions()
        {
            // Get all profiling stats
            var stats = _profilerManager.GetAllStats();
            
            foreach (var kvp in stats)
            {
                var tag = kvp.Key;
                var profileStats = kvp.Value;
                
                // Create or get UI item
                if (!_sessionItems.TryGetValue(tag, out var item))
                {
                    item = CreateSessionUIItem(tag);
                    _sessionItems[tag] = item;
                }
                
                // Update values
                UpdateSessionUIItem(item, tag, profileStats);
            }
        }
        
        /// <summary>
        /// Create a new metric UI item
        /// </summary>
        private MetricUIItem CreateMetricUIItem(SystemMetric metric)
        {
            if (_metricPrefab == null || _metricsContainer == null)
                return null;
                
            // Instantiate prefab
            var go = Instantiate(_metricPrefab, _metricsContainer);
            
            // Set up UI item
            var item = new MetricUIItem
            {
                Root = go,
                NameText = go.transform.Find("NameText")?.GetComponent<Text>(),
                ValueText = go.transform.Find("ValueText")?.GetComponent<Text>(),
                UnitText = go.transform.Find("UnitText")?.GetComponent<Text>(),
                ProgressBar = go.transform.Find("ProgressBar")?.GetComponent<Slider>(),
                AvgText = go.transform.Find("DetailsPanel/AvgText")?.GetComponent<Text>(),
                MaxText = go.transform.Find("DetailsPanel/MaxText")?.GetComponent<Text>()
            };
            
            // Set name
            if (item.NameText != null)
            {
                item.NameText.text = metric.Tag.FullName;
            }
            
            // Set unit
            if (item.UnitText != null)
            {
                item.UnitText.text = metric.Unit;
            }
            
            // Set details visibility
            SetMetricDetailsVisible(item, _showDetails);
            
            return item;
        }
        
        /// <summary>
        /// Create a new session UI item
        /// </summary>
        private SessionUIItem CreateSessionUIItem(ProfilerTag tag)
        {
            if (_sessionPrefab == null || _sessionsContainer == null)
                return null;
                
            // Instantiate prefab
            var go = Instantiate(_sessionPrefab, _sessionsContainer);
            
            // Set up UI item
            var item = new SessionUIItem
            {
                Root = go,
                NameText = go.transform.Find("NameText")?.GetComponent<Text>(),
                ValueText = go.transform.Find("ValueText")?.GetComponent<Text>(),
                ProgressBar = go.transform.Find("ProgressBar")?.GetComponent<Slider>(),
                AvgText = go.transform.Find("DetailsPanel/AvgText")?.GetComponent<Text>(),
                MaxText = go.transform.Find("DetailsPanel/MaxText")?.GetComponent<Text>()
            };
            
            // Set name
            if (item.NameText != null)
            {
                item.NameText.text = tag.FullName;
            }
            
            // Set details visibility
            SetSessionDetailsVisible(item, _showDetails);
            
            return item;
        }
        
        /// <summary>
        /// Update metric UI item with current values
        /// </summary>
        private void UpdateMetricUIItem(MetricUIItem item, SystemMetric metric)
        {
            if (item == null)
                return;
                
            // Update value text
            if (item.ValueText != null)
            {
                item.ValueText.text = $"{metric.LastValue:F2}";
                item.ValueText.color = GetColorForValue(metric.LastValue);
            }
            
            // Update progress bar
            if (item.ProgressBar != null)
            {
                item.ProgressBar.value = GetNormalizedValue(metric.LastValue);
                SetSliderColor(item.ProgressBar, GetColorForValue(metric.LastValue));
            }
            
            // Update details
            if (item.AvgText != null)
            {
                item.AvgText.text = $"Avg: {metric.AverageValue:F2}";
            }
            
            if (item.MaxText != null)
            {
                item.MaxText.text = $"Max: {metric.MaxValue:F2}";
            }
        }
        
        /// <summary>
        /// Update session UI item with current values
        /// </summary>
        private void UpdateSessionUIItem(SessionUIItem item, ProfilerTag tag, ProfileStats stats)
        {
            if (item == null)
                return;
                
            // Update value text
            if (item.ValueText != null)
            {
                item.ValueText.text = $"{stats.LastValue:F2} ms";
                item.ValueText.color = GetColorForValue(stats.LastValue);
            }
            
            // Update progress bar
            if (item.ProgressBar != null)
            {
                item.ProgressBar.value = GetNormalizedValue(stats.LastValue);
                SetSliderColor(item.ProgressBar, GetColorForValue(stats.LastValue));
            }
            
            // Update details
            if (item.AvgText != null)
            {
                item.AvgText.text = $"Avg: {stats.AverageValue:F2} ms";
            }
            
            if (item.MaxText != null)
            {
                item.MaxText.text = $"Max: {stats.MaxValue:F2} ms";
            }
        }
        
        /// <summary>
        /// Set metric details visibility
        /// </summary>
        private void SetMetricDetailsVisible(MetricUIItem item, bool visible)
        {
            if (item == null)
                return;
                
            var detailsPanel = item.Root.transform.Find("DetailsPanel");
            if (detailsPanel != null)
            {
                detailsPanel.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Set session details visibility
        /// </summary>
        private void SetSessionDetailsVisible(SessionUIItem item, bool visible)
        {
            if (item == null)
                return;
                
            var detailsPanel = item.Root.transform.Find("DetailsPanel");
            if (detailsPanel != null)
            {
                detailsPanel.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Get color based on value thresholds
        /// </summary>
        private Color GetColorForValue(double value)
        {
            if (value >= _errorThreshold)
                return _errorColor;
                
            if (value >= _warningThreshold)
                return _warningColor;
                
            return _normalColor;
        }
        
        /// <summary>
        /// Get normalized value (0-1) for progress bars
        /// </summary>
        private float GetNormalizedValue(double value)
        {
            float max = (float)_errorThreshold * 1.5f;
            return Mathf.Clamp01((float)value / max);
        }
        
        /// <summary>
        /// Set slider fill color
        /// </summary>
        private void SetSliderColor(Slider slider, Color color)
        {
            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }
        
        /// <summary>
        /// Handle session completed event
        /// </summary>
        private void OnSessionCompleted(object sender, ProfilerSessionEventArgs e)
        {
            // If auto-update is off, we need to manually refresh the UI
            if (!_autoUpdate)
            {
                RefreshUI();
            }
        }
    }
}