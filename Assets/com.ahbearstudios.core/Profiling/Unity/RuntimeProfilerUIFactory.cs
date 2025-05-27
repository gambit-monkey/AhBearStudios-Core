using System;
using UnityEngine;
using UnityEngine.UI;
using AhBearStudios.Core.Profiling.Unity.Configuration;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Factory for creating and configuring the Runtime Profiler UI.
    /// Handles the creation, setup, and configuration of UI components for the profiling system.
    /// </summary>
    public class RuntimeProfilerUIFactory
    {
        // Standard UI prefab asset paths
        private const string CANVAS_PREFAB_PATH = "Profiling/UI/ProfilerCanvas";
        private const string METRIC_ITEM_PREFAB_PATH = "Profiling/UI/ProfilerMetricItem";
        private const string SESSION_ITEM_PREFAB_PATH = "Profiling/UI/ProfilerSessionItem";
        private const string GRAPH_ITEM_PREFAB_PATH = "Profiling/UI/ProfilerGraphItem";
        
        // Configuration 
        private readonly ProfilerConfiguration _configuration;
        private readonly ProfileManager _profileManager;
        
        // UI state
        private bool _showMetrics = true;
        private bool _showSessions = true;
        private bool _showGraphs = false;
        private int _maxVisibleItems = 20;
        private float _updateInterval = 0.5f;
        
        /// <summary>
        /// Creates a new RuntimeProfilerUIFactory with the given profile manager.
        /// </summary>
        /// <param name="profileManager">The profile manager to use for this UI</param>
        /// <param name="configuration">The profiler configuration, or null to use the profile manager's configuration</param>
        public RuntimeProfilerUIFactory(ProfileManager profileManager, ProfilerConfiguration configuration = null)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _configuration = configuration ?? profileManager.Configuration;
        }
        
        /// <summary>
        /// Sets whether metrics should be displayed.
        /// </summary>
        /// <param name="showMetrics">Whether to show metrics</param>
        /// <returns>This factory for method chaining</returns>
        public RuntimeProfilerUIFactory SetShowMetrics(bool showMetrics)
        {
            _showMetrics = showMetrics;
            return this;
        }
        
        /// <summary>
        /// Sets whether sessions should be displayed.
        /// </summary>
        /// <param name="showSessions">Whether to show sessions</param>
        /// <returns>This factory for method chaining</returns>
        public RuntimeProfilerUIFactory SetShowSessions(bool showSessions)
        {
            _showSessions = showSessions;
            return this;
        }
        
        /// <summary>
        /// Sets whether graphs should be displayed.
        /// </summary>
        /// <param name="showGraphs">Whether to show graphs</param>
        /// <returns>This factory for method chaining</returns>
        public RuntimeProfilerUIFactory SetShowGraphs(bool showGraphs)
        {
            _showGraphs = showGraphs;
            return this;
        }
        
        /// <summary>
        /// Sets the maximum number of visible items in the UI.
        /// </summary>
        /// <param name="maxItems">Maximum number of visible items</param>
        /// <returns>This factory for method chaining</returns>
        public RuntimeProfilerUIFactory SetMaxVisibleItems(int maxItems)
        {
            _maxVisibleItems = Mathf.Clamp(maxItems, 1, 100);
            return this;
        }
        
        /// <summary>
        /// Sets the update interval for the UI.
        /// </summary>
        /// <param name="intervalSeconds">Update interval in seconds</param>
        /// <returns>This factory for method chaining</returns>
        public RuntimeProfilerUIFactory SetUpdateInterval(float intervalSeconds)
        {
            _updateInterval = Mathf.Max(0.1f, intervalSeconds);
            return this;
        }
        
        /// <summary>
        /// Creates a new RuntimeProfilerUI in the scene.
        /// </summary>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>The created RuntimeProfilerUI instance</returns>
        public RuntimeProfilerUI Create(Transform parent = null)
        {
            // Create canvas and root objects
            GameObject canvasObject = new GameObject("[ProfilerUI]");
            if (parent != null)
                canvasObject.transform.SetParent(parent, false);
            
            // Create canvas
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            // Add canvas scaler for responsive UI
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add raycaster for button interactions
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // Create main panel
            GameObject panelObject = CreatePanel(canvasObject.transform, "Panel", new Vector2(400, 600));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -10);
            
            // Add panel background
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Add header text
            GameObject headerObject = CreateText(panelRect, "HeaderText", "PROFILER", 18);
            RectTransform headerRect = headerObject.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = new Vector2(0, 30);
            headerRect.anchoredPosition = new Vector2(0, -15);
            
            // Add toggle button
            GameObject buttonObject = CreateButton(panelRect, "ToggleButton", "Hide");
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.sizeDelta = new Vector2(60, 25);
            buttonRect.anchoredPosition = new Vector2(-5, -15);
            
            // Create scroll view for metrics
            GameObject metricsScrollObject = CreateScrollView(panelRect, "MetricsScrollView", new Vector2(0, 30));
            RectTransform metricsScrollRect = metricsScrollObject.GetComponent<RectTransform>();
            metricsScrollRect.anchorMin = new Vector2(0, 0.5f);
            metricsScrollRect.anchorMax = new Vector2(1, 1);
            metricsScrollRect.offsetMin = new Vector2(5, 5);
            metricsScrollRect.offsetMax = new Vector2(-5, -35);
            
            // Get the content container for metrics
            RectTransform metricsContainer = metricsScrollObject.transform.Find("Viewport/Content") as RectTransform;
            
            // Create scroll view for sessions
            GameObject sessionsScrollObject = CreateScrollView(panelRect, "SessionsScrollView", new Vector2(0, 30));
            RectTransform sessionsScrollRect = sessionsScrollObject.GetComponent<RectTransform>();
            sessionsScrollRect.anchorMin = new Vector2(0, 0);
            sessionsScrollRect.anchorMax = new Vector2(1, 0.5f);
            sessionsScrollRect.offsetMin = new Vector2(5, 5);
            sessionsScrollRect.offsetMax = new Vector2(-5, -5);
            
            // Get the content container for sessions
            RectTransform sessionsContainer = sessionsScrollObject.transform.Find("Viewport/Content") as RectTransform;
            
            // Create item prefabs directly if not loaded from Resources
            GameObject metricItemPrefab = CreateMetricItemPrefab();
            GameObject sessionItemPrefab = CreateSessionItemPrefab();
            GameObject graphItemPrefab = CreateGraphItemPrefab();
            
            // Add the RuntimeProfilerUI component
            RuntimeProfilerUI profilerUI = canvasObject.AddComponent<RuntimeProfilerUI>();
            
            // Set up references
            profilerUI._canvas = canvas;
            profilerUI._rootPanel = panelRect;
            profilerUI._headerText = headerObject.GetComponent<Text>();
            profilerUI._toggleButton = buttonObject.GetComponent<Button>();
            profilerUI._metricsScrollView = metricsScrollObject.GetComponent<ScrollRect>();
            profilerUI._metricsContainer = metricsContainer;
            profilerUI._sessionsContainer = sessionsContainer;
            
            // Set up prefabs
            profilerUI._metricItemPrefab = metricItemPrefab;
            profilerUI._sessionItemPrefab = sessionItemPrefab;
            profilerUI._graphItemPrefab = graphItemPrefab;
            
            // Configure from factory settings
            profilerUI._showMetrics = _showMetrics;
            profilerUI._showSessions = _showSessions;
            profilerUI._showGraphs = _showGraphs;
            profilerUI._maxVisibleItems = _maxVisibleItems;
            profilerUI._updateInterval = _updateInterval;
            
            // Apply initial visibility
            sessionsScrollObject.SetActive(_showSessions);
            metricsScrollObject.SetActive(_showMetrics);
            
            return profilerUI;
        }
        
        /// <summary>
        /// Creates a panel GameObject with RectTransform.
        /// </summary>
        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            
            return panel;
        }
        
        /// <summary>
        /// Creates a Text GameObject.
        /// </summary>
        private GameObject CreateText(Transform parent, string name, string text, int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            
            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return textObject;
        }
        
        /// <summary>
        /// Creates a Button GameObject.
        /// </summary>
        private GameObject CreateButton(Transform parent, string name, string text)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            button.colors = colors;
            
            GameObject textObject = CreateText(buttonObject.transform, "Text", text, 14);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return buttonObject;
        }
        
        /// <summary>
        /// Creates a ScrollView GameObject.
        /// </summary>
        private GameObject CreateScrollView(Transform parent, string name, Vector2 itemSize)
        {
            GameObject scrollObject = new GameObject(name);
            scrollObject.transform.SetParent(parent, false);
            
            // Add the scroll rect component
            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            
            // Create the viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);
            
            // Add mask to viewport
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            // Create the content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.pivot = new Vector2(0, 1);
            
            // Add vertical layout group to content
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 2;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            
            // Add content size fitter to automatically adjust height
            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Configure scroll rect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            return scrollObject;
        }
        
        /// <summary>
        /// Creates a metric item prefab.
        /// </summary>
        private GameObject CreateMetricItemPrefab()
        {
            GameObject prefab = new GameObject("MetricItemPrefab");
            prefab.SetActive(false);
            
            // Set up the RectTransform
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 25);
            
            // Add background image
            Image background = prefab.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Add name text
            GameObject nameObject = CreateText(prefab.transform, "NameText", "Metric", 14);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.6f, 1);
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(0, 0);
            
            Text nameText = nameObject.GetComponent<Text>();
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 10;
            nameText.resizeTextMaxSize = 14;
            
            // Add value text
            GameObject valueObject = CreateText(prefab.transform, "ValueText", "0", 14);
            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.6f, 0);
            valueRect.anchorMax = new Vector2(0.8f, 1);
            valueRect.offsetMin = new Vector2(5, 0);
            valueRect.offsetMax = new Vector2(-5, 0);
            
            Text valueText = valueObject.GetComponent<Text>();
            valueText.alignment = TextAnchor.MiddleRight;
            
            // Add unit text
            GameObject unitObject = CreateText(prefab.transform, "UnitText", "ms", 12);
            RectTransform unitRect = unitObject.GetComponent<RectTransform>();
            unitRect.anchorMin = new Vector2(0.8f, 0);
            unitRect.anchorMax = new Vector2(1, 1);
            unitRect.offsetMin = new Vector2(0, 0);
            unitRect.offsetMax = new Vector2(-5, 0);
            
            Text unitText = unitObject.GetComponent<Text>();
            unitText.alignment = TextAnchor.MiddleRight;
            unitText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Add progress bar
            GameObject progressObject = new GameObject("ProgressBar");
            progressObject.transform.SetParent(prefab.transform, false);
            
            RectTransform progressRect = progressObject.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0, 0.1f);
            progressRect.anchorMax = new Vector2(1, 0.2f);
            progressRect.offsetMin = new Vector2(5, 0);
            progressRect.offsetMax = new Vector2(-5, 0);
            
            Image background2 = progressObject.AddComponent<Image>();
            background2.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(progressObject.transform, false);
            
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = Color.green;
            
            Slider slider = progressObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = background2;
            slider.interactable = false;
            
            return prefab;
        }
        
        /// <summary>
        /// Creates a session item prefab.
        /// </summary>
        private GameObject CreateSessionItemPrefab()
        {
            GameObject prefab = new GameObject("SessionItemPrefab");
            prefab.SetActive(false);
            
            // Set up the RectTransform
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 25);
            
            // Add background image
            Image background = prefab.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Add name text
            GameObject nameObject = CreateText(prefab.transform, "NameText", "Session", 14);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 1);
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(0, 0);
            
            Text nameText = nameObject.GetComponent<Text>();
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 10;
            nameText.resizeTextMaxSize = 14;
            
            // Add value text
            GameObject valueObject = CreateText(prefab.transform, "ValueText", "0ms", 14);
            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.5f);
            valueRect.anchorMax = new Vector2(0.7f, 1);
            valueRect.offsetMin = new Vector2(5, 0);
            valueRect.offsetMax = new Vector2(-5, 0);
            
            Text valueText = valueObject.GetComponent<Text>();
            valueText.alignment = TextAnchor.MiddleRight;
            
            // Add avg text
            GameObject avgObject = CreateText(prefab.transform, "AvgText", "Avg: 0ms", 12);
            RectTransform avgRect = avgObject.GetComponent<RectTransform>();
            avgRect.anchorMin = new Vector2(0.7f, 0.5f);
            avgRect.anchorMax = new Vector2(0.85f, 1);
            avgRect.offsetMin = new Vector2(5, 0);
            avgRect.offsetMax = new Vector2(-5, 0);
            
            Text avgText = avgObject.GetComponent<Text>();
            avgText.alignment = TextAnchor.MiddleRight;
            avgText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Add max text
            GameObject maxObject = CreateText(prefab.transform, "MaxText", "Max: 0ms", 12);
            RectTransform maxRect = maxObject.GetComponent<RectTransform>();
            maxRect.anchorMin = new Vector2(0.85f, 0.5f);
            maxRect.anchorMax = new Vector2(1, 1);
            maxRect.offsetMin = new Vector2(5, 0);
            maxRect.offsetMax = new Vector2(-5, 0);
            
            Text maxText = maxObject.GetComponent<Text>();
            maxText.alignment = TextAnchor.MiddleRight;
            maxText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Add progress bar
            GameObject progressObject = new GameObject("ProgressBar");
            progressObject.transform.SetParent(prefab.transform, false);
            
            RectTransform progressRect = progressObject.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0, 0.1f);
            progressRect.anchorMax = new Vector2(1, 0.2f);
            progressRect.offsetMin = new Vector2(5, 0);
            progressRect.offsetMax = new Vector2(-5, 0);
            
            Image background2 = progressObject.AddComponent<Image>();
            background2.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(progressObject.transform, false);
            
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = Color.green;
            
            Slider slider = progressObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = background2;
            slider.interactable = false;
            
            return prefab;
        }
        
        /// <summary>
        /// Creates a graph item prefab.
        /// </summary>
        private GameObject CreateGraphItemPrefab()
        {
            GameObject prefab = new GameObject("GraphItemPrefab");
            prefab.SetActive(false);
            
            // Set up the RectTransform
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 100);
            
            // Add background image
            Image background = prefab.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Add name text
            GameObject nameObject = CreateText(prefab.transform, "NameText", "Graph", 14);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.8f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(-5, 0);
            
            Text nameText = nameObject.GetComponent<Text>();
            nameText.alignment = TextAnchor.MiddleLeft;
            
            // Add graph area
            GameObject graphContainer = new GameObject("GraphContainer");
            graphContainer.transform.SetParent(prefab.transform, false);
            
            RectTransform graphRect = graphContainer.AddComponent<RectTransform>();
            graphRect.anchorMin = new Vector2(0, 0);
            graphRect.anchorMax = new Vector2(1, 0.8f);
            graphRect.offsetMin = new Vector2(5, 5);
            graphRect.offsetMax = new Vector2(-5, -5);
            
            Image graphImage = graphContainer.AddComponent<Image>();
            graphImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            
            return prefab;
        }
    }
}