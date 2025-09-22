using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using Reflex.Attributes;
using Unity.Collections;
using UnityEngine.UI;

namespace AhBearStudios.Unity.HealthCheck.Components
{
    /// <summary>
    /// Unity UI component for displaying real-time health check status and information.
    /// Provides comprehensive health monitoring visualization with automatic updates and responsive design.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class HealthCheckDisplayComponent : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")] [SerializeField]
        private Transform _healthCheckContainer;

        [SerializeField] private GameObject _healthCheckItemPrefab;
        [SerializeField] private Text _overallHealthText;
        [SerializeField] private Image _overallHealthIcon;
        [SerializeField] private Text _lastUpdateText;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _toggleDetailsButton;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Display Settings")] [SerializeField, Range(0.5f, 30f)]
        private float _updateInterval = 2.0f;

        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private bool _showDetailsOnStart = true;
        [SerializeField] private bool _highlightFailures = true;
        [SerializeField, Range(3, 50)] private int _maxDisplayedChecks = 20;

        [Header("Color Settings")] [SerializeField]
        private Color _healthyColor = Color.green;

        [SerializeField] private Color _degradedColor = Color.yellow;
        [SerializeField] private Color _unhealthyColor = Color.red;
        [SerializeField] private Color _unknownColor = Color.gray;
        [SerializeField] private Color _backgroundColorHealthy = new Color(0, 1, 0, 0.1f);
        [SerializeField] private Color _backgroundColorDegraded = new Color(1, 1, 0, 0.1f);
        [SerializeField] private Color _backgroundColorUnhealthy = new Color(1, 0, 0, 0.1f);

        [Header("Animation Settings")] [SerializeField]
        private bool _enableAnimations = true;

        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _statusChangeDuration = 0.5f;
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Performance Settings")] [SerializeField]
        private bool _enableObjectPooling = true;

        [SerializeField] private bool _batchUpdates = true;
        [SerializeField, Range(1, 10)] private int _maxUpdatesPerFrame = 3;

        [Header("Debug Features")] [SerializeField]
        private bool _enableDebugMode = false;

        [SerializeField] private bool _logStatusChanges = false;
        [SerializeField] private bool _showPerformanceStats = false;

        #endregion

        #region Private Fields

        // Dependencies
        [Inject] private IHealthCheckService _healthCheckService;
        [Inject] private ILoggingService _loggingService;

        // UI Management
        private readonly Dictionary<string, HealthCheckDisplayItem> _displayItems = new();
        private readonly Queue<HealthCheckDisplayItem> _itemPool = new();
        private readonly List<HealthCheckDisplayItem> _animatingItems = new();

        // State Management
        private bool _initialized = false;
        private bool _detailsVisible = true;
        private Coroutine _updateCoroutine;
        private Coroutine _animationCoroutine;
        private HealthStatus _lastOverallStatus = HealthStatus.Unknown;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly FixedString128Bytes _correlationId = GenerateCorrelationId();

        // Performance Tracking
        private int _totalUpdates = 0;
        private float _totalUpdateTime = 0f;
        private readonly Queue<float> _updateTimeHistory = new();
        private const int UPDATE_HISTORY_SIZE = 100;

        // Event Handlers
        private System.Action<object, HealthStatusChangedEventArgs> _healthStatusChangedHandler;
        private System.Action<object, CircuitBreakerStateChangedEventArgs> _circuitBreakerChangedHandler;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateRequiredComponents();
            InitializeEventHandlers();
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                SubscribeToEvents();
                StartUpdateCoroutine();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            StopUpdateCoroutine();
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        private void OnValidate()
        {
            // Clamp values to valid ranges
            _updateInterval = Mathf.Max(0.5f, _updateInterval);
            _maxDisplayedChecks = Mathf.Max(3, _maxDisplayedChecks);
            _maxUpdatesPerFrame = Mathf.Max(1, _maxUpdatesPerFrame);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually refreshes the health check display
        /// </summary>
        public void RefreshDisplay()
        {
            if (!_initialized)
            {
                _loggingService?.LogWarning("Cannot refresh display - component not initialized", _correlationId);
                return;
            }

            StartCoroutine(UpdateDisplayAsync(forceUpdate: true));
        }

        /// <summary>
        /// Toggles the visibility of detailed health check information
        /// </summary>
        public void ToggleDetails()
        {
            _detailsVisible = !_detailsVisible;

            if (_enableAnimations)
            {
                StartCoroutine(AnimateDetailsToggle());
            }
            else
            {
                _healthCheckContainer.gameObject.SetActive(_detailsVisible);
            }

            UpdateToggleButtonText();
            _loggingService?.LogDebug($"Health check details toggled: {_detailsVisible}", _correlationId);
        }

        /// <summary>
        /// Sets the auto-refresh state
        /// </summary>
        /// <param name="enabled">Whether auto-refresh should be enabled</param>
        public void SetAutoRefresh(bool enabled)
        {
            if (_autoRefresh != enabled)
            {
                _autoRefresh = enabled;

                if (_autoRefresh)
                {
                    StartUpdateCoroutine();
                }
                else
                {
                    StopUpdateCoroutine();
                }

                _loggingService?.LogInfo($"Auto-refresh {(enabled ? "enabled" : "disabled")}", _correlationId);
            }
        }

        /// <summary>
        /// Gets the current overall health status being displayed
        /// </summary>
        /// <returns>The current overall health status</returns>
        public HealthStatus GetCurrentOverallHealth()
        {
            return _lastOverallStatus;
        }

        /// <summary>
        /// Gets performance statistics for the display component
        /// </summary>
        /// <returns>Dictionary containing performance metrics</returns>
        public Dictionary<string, object> GetPerformanceStats()
        {
            var avgUpdateTime = _updateTimeHistory.Count > 0 ? _updateTimeHistory.Average() : 0f;

            return new Dictionary<string, object>
            {
                ["TotalUpdates"] = _totalUpdates,
                ["AverageUpdateTime"] = avgUpdateTime,
                ["LastUpdateTime"] = _lastUpdateTime,
                ["DisplayedItemsCount"] = _displayItems.Count,
                ["PooledItemsCount"] = _itemPool.Count,
                ["AnimatingItemsCount"] = _animatingItems.Count,
                ["AutoRefreshEnabled"] = _autoRefresh,
                ["DetailsVisible"] = _detailsVisible
            };
        }

        #endregion

        #region Private Initialization Methods

        /// <summary>
        /// Validates that all required UI components are assigned
        /// </summary>
        private void ValidateRequiredComponents()
        {
            var errors = new List<string>();

            if (_healthCheckContainer == null) errors.Add("Health Check Container");
            if (_healthCheckItemPrefab == null) errors.Add("Health Check Item Prefab");
            if (_overallHealthText == null) errors.Add("Overall Health Text");
            if (_overallHealthIcon == null) errors.Add("Overall Health Icon");

            if (errors.Any())
            {
                var errorMessage = $"Missing required components: {string.Join(", ", errors)}";
                Debug.LogError($"[HealthCheckDisplayComponent] {errorMessage}");
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Initializes event handler delegates
        /// </summary>
        private void InitializeEventHandlers()
        {
            _healthStatusChangedHandler = OnHealthStatusChanged;
            _circuitBreakerChangedHandler = OnCircuitBreakerStateChanged;
        }

        /// <summary>
        /// Asynchronously initializes the component with health check service integration
        /// </summary>
        private IEnumerator InitializeAsync()
        {
            _loggingService?.LogInfo("Initializing HealthCheckDisplayComponent", _correlationId);

            // Wait for dependency injection to complete
            yield return new WaitUntil(() => _healthCheckService != null && _loggingService != null);

            try
            {
                // Initialize UI components
                InitializeUI();

                // Pre-populate object pool if enabled
                if (_enableObjectPooling)
                {
                    PrePopulateItemPool();
                }

                // Set initial state
                _detailsVisible = _showDetailsOnStart;
                _healthCheckContainer.gameObject.SetActive(_detailsVisible);
                UpdateToggleButtonText();

                // Subscribe to health events
                SubscribeToEvents();

                // Perform initial update
                yield return StartCoroutine(UpdateDisplayAsync(forceUpdate: true));

                // Start automatic updates if enabled
                if (_autoRefresh)
                {
                    StartUpdateCoroutine();
                }

                _initialized = true;
                _loggingService?.LogInfo("HealthCheckDisplayComponent initialized successfully", _correlationId);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to initialize HealthCheckDisplayComponent", _correlationId);
                Debug.LogError($"[HealthCheckDisplayComponent] Initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes UI components and button event handlers
        /// </summary>
        private void InitializeUI()
        {
            // Setup button event handlers
            if (_refreshButton != null)
            {
                _refreshButton.onClick.RemoveAllListeners();
                _refreshButton.onClick.AddListener(RefreshDisplay);
            }

            if (_toggleDetailsButton != null)
            {
                _toggleDetailsButton.onClick.RemoveAllListeners();
                _toggleDetailsButton.onClick.AddListener(ToggleDetails);
            }

            // Initialize text fields
            if (_overallHealthText != null)
            {
                _overallHealthText.text = "Initializing...";
            }

            if (_lastUpdateText != null)
            {
                _lastUpdateText.text = "Never";
            }

            // Initialize scroll rect
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f; // Start at top
            }
        }

        /// <summary>
        /// Pre-populates the object pool with health check display items
        /// </summary>
        private void PrePopulateItemPool()
        {
            if (_healthCheckItemPrefab == null || _healthCheckContainer == null) return;

            var initialPoolSize = Mathf.Min(10, _maxDisplayedChecks);

            for (int i = 0; i < initialPoolSize; i++)
            {
                var item = CreateHealthCheckDisplayItem();
                item.gameObject.SetActive(false);
                _itemPool.Enqueue(item);
            }

            _loggingService?.LogDebug($"Pre-populated object pool with {initialPoolSize} items", _correlationId);
        }

        #endregion

        #region Private Update Methods

        /// <summary>
        /// Starts the automatic update coroutine
        /// </summary>
        private void StartUpdateCoroutine()
        {
            if (_updateCoroutine == null && _autoRefresh && _initialized)
            {
                _updateCoroutine = StartCoroutine(AutoUpdateLoop());
            }
        }

        /// <summary>
        /// Stops the automatic update coroutine
        /// </summary>
        private void StopUpdateCoroutine()
        {
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
                _updateCoroutine = null;
            }
        }

        /// <summary>
        /// Main automatic update loop
        /// </summary>
        private IEnumerator AutoUpdateLoop()
        {
            while (_autoRefresh && _initialized)
            {
                yield return new WaitForSeconds(_updateInterval);

                if (_autoRefresh && _initialized) // Check again after wait
                {
                    yield return StartCoroutine(UpdateDisplayAsync());
                }
            }
        }

        /// <summary>
        /// Updates the health check display with current data
        /// </summary>
        private IEnumerator UpdateDisplayAsync(bool forceUpdate = false)
        {
            var startTime = Time.realtimeSinceStartup;

            try
            {
                // Get current health data
                var overallHealth = await _healthCheckService.GetOverallHealthStatusAsync();
                var lastResults = _healthCheckService.GetLastResults().ToList();
                var statistics = _healthCheckService.GetStatistics();

                // Update overall health display
                yield return StartCoroutine(UpdateOverallHealthDisplay(overallHealth));

                // Update individual health check displays
                if (_batchUpdates)
                {
                    yield return StartCoroutine(BatchUpdateHealthCheckItems(lastResults, forceUpdate));
                }
                else
                {
                    yield return StartCoroutine(UpdateHealthCheckItems(lastResults, forceUpdate));
                }

                // Update statistics and timing
                UpdatePerformanceTracking(startTime);
                UpdateLastUpdateTime();

                _totalUpdates++;
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Error updating health check display", _correlationId);
            }
        }

        /// <summary>
        /// Updates the overall health status display
        /// </summary>
        private IEnumerator UpdateOverallHealthDisplay(HealthStatus overallHealth)
        {
            if (_lastOverallStatus != overallHealth)
            {
                var previousStatus = _lastOverallStatus;
                _lastOverallStatus = overallHealth;

                // Update text
                if (_overallHealthText != null)
                {
                    _overallHealthText.text = $"Overall Health: {overallHealth}";
                    _overallHealthText.color = GetHealthStatusColor(overallHealth);
                }

                // Update icon
                if (_overallHealthIcon != null)
                {
                    _overallHealthIcon.color = GetHealthStatusColor(overallHealth);
                }

                // Animate status change if enabled
                if (_enableAnimations && previousStatus != HealthStatus.Unknown)
                {
                    yield return StartCoroutine(AnimateStatusChange(_overallHealthIcon, overallHealth));
                }

                // Log status change if enabled
                if (_logStatusChanges)
                {
                    _loggingService?.LogInfo($"Overall health status changed: {previousStatus} -> {overallHealth}",
                        _correlationId);
                }
            }
        }

        /// <summary>
        /// Updates individual health check display items with batching
        /// </summary>
        private IEnumerator BatchUpdateHealthCheckItems(List<HealthCheckResult> results, bool forceUpdate)
        {
            var batches = CreateUpdateBatches(results);

            foreach (var batch in batches)
            {
                var updatedThisFrame = 0;

                foreach (var result in batch)
                {
                    UpdateSingleHealthCheckItem(result, forceUpdate);
                    updatedThisFrame++;

                    if (updatedThisFrame >= _maxUpdatesPerFrame)
                    {
                        yield return null; // Wait for next frame
                        updatedThisFrame = 0;
                    }
                }

                yield return null; // Wait between batches
            }

            // Clean up items for health checks that no longer exist
            CleanupOldHealthCheckItems(results);
        }

        /// <summary>
        /// Updates individual health check display items without batching
        /// </summary>
        private IEnumerator UpdateHealthCheckItems(List<HealthCheckResult> results, bool forceUpdate)
        {
            foreach (var result in results)
            {
                UpdateSingleHealthCheckItem(result, forceUpdate);
                yield return null; // Spread updates across frames
            }

            CleanupOldHealthCheckItems(results);
        }

        /// <summary>
        /// Updates a single health check display item
        /// </summary>
        private void UpdateSingleHealthCheckItem(HealthCheckResult result, bool forceUpdate)
        {
            if (!_displayItems.TryGetValue(result.Name, out var displayItem))
            {
                displayItem = GetOrCreateHealthCheckDisplayItem(result.Name);
                _displayItems[result.Name] = displayItem;
            }

            displayItem.UpdateDisplay(result, forceUpdate);

            // Handle highlighting for failures
            if (_highlightFailures && result.Status == HealthStatus.Unhealthy)
            {
                HighlightFailedItem(displayItem);
            }
        }

        /// <summary>
        /// Creates update batches to optimize performance
        /// </summary>
        private List<List<HealthCheckResult>> CreateUpdateBatches(List<HealthCheckResult> results)
        {
            var batches = new List<List<HealthCheckResult>>();
            var batchSize = Mathf.Max(1, _maxUpdatesPerFrame);

            for (int i = 0; i < results.Count; i += batchSize)
            {
                var batch = results.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }

            return batches;
        }

        /// <summary>
        /// Removes display items for health checks that no longer exist
        /// </summary>
        private void CleanupOldHealthCheckItems(List<HealthCheckResult> currentResults)
        {
            var currentNames = new HashSet<string>(currentResults.Select(r => r.Name));
            var itemsToRemove = _displayItems.Keys.Where(name => !currentNames.Contains(name)).ToList();

            foreach (var name in itemsToRemove)
            {
                var item = _displayItems[name];
                _displayItems.Remove(name);
                ReturnItemToPool(item);
            }
        }

        #endregion

        #region Private Item Management Methods

        /// <summary>
        /// Gets an existing item or creates a new one for the specified health check
        /// </summary>
        private HealthCheckDisplayItem GetOrCreateHealthCheckDisplayItem(string healthCheckName)
        {
            if (_itemPool.Count > 0)
            {
                var pooledItem = _itemPool.Dequeue();
                pooledItem.gameObject.SetActive(true);
                pooledItem.Initialize(healthCheckName);
                return pooledItem;
            }

            return CreateHealthCheckDisplayItem(healthCheckName);
        }

        /// <summary>
        /// Creates a new health check display item
        /// </summary>
        private HealthCheckDisplayItem CreateHealthCheckDisplayItem(string healthCheckName = null)
        {
            var itemObject = Instantiate(_healthCheckItemPrefab, _healthCheckContainer);
            var displayItem = itemObject.GetComponent<HealthCheckDisplayItem>();

            if (displayItem == null)
            {
                displayItem = itemObject.AddComponent<HealthCheckDisplayItem>();
            }

            if (!string.IsNullOrEmpty(healthCheckName))
            {
                displayItem.Initialize(healthCheckName);
            }

            return displayItem;
        }

        /// <summary>
        /// Returns a display item to the object pool
        /// </summary>
        private void ReturnItemToPool(HealthCheckDisplayItem item)
        {
            if (item != null && _enableObjectPooling)
            {
                item.gameObject.SetActive(false);
                item.Reset();
                _itemPool.Enqueue(item);
            }
            else if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        #endregion

        #region Private Animation Methods

        /// <summary>
        /// Animates the details panel toggle
        /// </summary>
        private IEnumerator AnimateDetailsToggle()
        {
            var canvasGroup = _healthCheckContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = _healthCheckContainer.gameObject.AddComponent<CanvasGroup>();
            }

            if (_detailsVisible)
            {
                // Fade in
                _healthCheckContainer.gameObject.SetActive(true);
                yield return StartCoroutine(AnimateCanvasGroup(canvasGroup, 0f, 1f, _fadeInDuration));
            }
            else
            {
                // Fade out
                yield return StartCoroutine(AnimateCanvasGroup(canvasGroup, 1f, 0f, _fadeInDuration));
                _healthCheckContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Animates a status change with visual feedback
        /// </summary>
        private IEnumerator AnimateStatusChange(Image targetImage, HealthStatus newStatus)
        {
            if (targetImage == null) yield break;

            var originalScale = targetImage.transform.localScale;
            var targetColor = GetHealthStatusColor(newStatus);

            // Scale up animation
            yield return StartCoroutine(AnimateScale(targetImage.transform, originalScale, originalScale * 1.2f,
                _statusChangeDuration * 0.3f));

            // Color change
            targetImage.color = targetColor;

            // Scale back down
            yield return StartCoroutine(AnimateScale(targetImage.transform, targetImage.transform.localScale,
                originalScale, _statusChangeDuration * 0.7f));
        }

        /// <summary>
        /// Animates a canvas group's alpha value
        /// </summary>
        private IEnumerator AnimateCanvasGroup(CanvasGroup canvasGroup, float fromAlpha, float toAlpha, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var easedProgress = _animationCurve.Evaluate(progress);

                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, easedProgress);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
        }

        /// <summary>
        /// Animates the scale of a transform
        /// </summary>
        private IEnumerator AnimateScale(Transform target, Vector3 fromScale, Vector3 toScale, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var easedProgress = _animationCurve.Evaluate(progress);

                target.localScale = Vector3.Lerp(fromScale, toScale, easedProgress);
                yield return null;
            }

            target.localScale = toScale;
        }

        /// <summary>
        /// Highlights a failed health check item
        /// </summary>
        private void HighlightFailedItem(HealthCheckDisplayItem item)
        {
            if (item != null && _enableAnimations)
            {
                StartCoroutine(PulseHighlight(item));
            }
        }

        /// <summary>
        /// Creates a pulsing highlight effect for failed items
        /// </summary>
        private IEnumerator PulseHighlight(HealthCheckDisplayItem item)
        {
            var background = item.GetComponent<Image>();
            if (background == null) yield break;

            var originalColor = background.color;
            var highlightColor = _backgroundColorUnhealthy;

            for (int i = 0; i < 3; i++) // Pulse 3 times
            {
                yield return StartCoroutine(AnimateColor(background, originalColor, highlightColor, 0.3f));
                yield return StartCoroutine(AnimateColor(background, highlightColor, originalColor, 0.3f));
            }
        }

        /// <summary>
        /// Animates color change on an Image component
        /// </summary>
        private IEnumerator AnimateColor(Image image, Color fromColor, Color toColor, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;

                image.color = Color.Lerp(fromColor, toColor, progress);
                yield return null;
            }

            image.color = toColor;
        }

        #endregion

        #region Private Event Methods

        /// <summary>
        /// Subscribes to health check service events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_healthCheckService != null)
            {
                _healthCheckService.HealthStatusChanged += _healthStatusChangedHandler;
                _healthCheckService.CircuitBreakerStateChanged += _circuitBreakerChangedHandler;
            }
        }

        /// <summary>
        /// Unsubscribes from health check service events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_healthCheckService != null)
            {
                _healthCheckService.HealthStatusChanged -= _healthStatusChangedHandler;
                _healthCheckService.CircuitBreakerStateChanged -= _circuitBreakerChangedHandler;
            }
        }

        /// <summary>
        /// Handles health status changed events
        /// </summary>
        private void OnHealthStatusChanged(object sender, HealthStatusChangedEventArgs args)
        {
            if (_logStatusChanges)
            {
                _loggingService?.LogInfo($"Health status change detected: {args.SystemName} -> {args.NewStatus}",
                    _correlationId);
            }

            // Trigger immediate refresh on status changes
            if (_initialized && gameObject.activeInHierarchy)
            {
                StartCoroutine(UpdateDisplayAsync(forceUpdate: true));
            }
        }

        /// <summary>
        /// Handles circuit breaker state changed events
        /// </summary>
        private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs args)
        {
            if (_logStatusChanges)
            {
                _loggingService?.LogInfo($"Circuit breaker state change: {args.OperationName} -> {args.NewState}",
                    _correlationId);
            }

            // Refresh display to show circuit breaker changes
            if (_initialized && gameObject.activeInHierarchy)
            {
                StartCoroutine(UpdateDisplayAsync(forceUpdate: true));
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets the appropriate color for a health status
        /// </summary>
        private Color GetHealthStatusColor(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => _healthyColor,
                HealthStatus.Degraded => _degradedColor,
                HealthStatus.Unhealthy => _unhealthyColor,
                _ => _unknownColor
            };
        }

        /// <summary>
        /// Gets the appropriate background color for a health status
        /// </summary>
        private Color GetHealthStatusBackgroundColor(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => _backgroundColorHealthy,
                HealthStatus.Degraded => _backgroundColorDegraded,
                HealthStatus.Unhealthy => _backgroundColorUnhealthy,
                _ => Color.clear
            };
        }

        /// <summary>
        /// Updates the toggle button text based on current state
        /// </summary>
        private void UpdateToggleButtonText()
        {
            if (_toggleDetailsButton != null)
            {
                var buttonText = _toggleDetailsButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = _detailsVisible ? "Hide Details" : "Show Details";
                }
            }
        }

        /// <summary>
        /// Updates performance tracking metrics
        /// </summary>
        private void UpdatePerformanceTracking(float startTime)
        {
            var updateTime = Time.realtimeSinceStartup - startTime;
            _totalUpdateTime += updateTime;

            _updateTimeHistory.Enqueue(updateTime);
            if (_updateTimeHistory.Count > UPDATE_HISTORY_SIZE)
            {
                _updateTimeHistory.Dequeue();
            }

            if (_showPerformanceStats && _enableDebugMode)
            {
                var avgTime = _updateTimeHistory.Average();
                _loggingService?.LogDebug(
                    $"Display update performance - Current: {updateTime:F3}s, Average: {avgTime:F3}s", _correlationId);
            }
        }

        /// <summary>
        /// Updates the last update time display
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            _lastUpdateTime = DateTime.Now;

            if (_lastUpdateText != null)
            {
                _lastUpdateText.text = $"Last Update: {_lastUpdateTime:HH:mm:ss}";
            }
        }

        /// <summary>
        /// Cleans up resources and stops coroutines
        /// </summary>
        private void CleanupResources()
        {
            StopUpdateCoroutine();
            UnsubscribeFromEvents();

            // Clear display items
            foreach (var item in _displayItems.Values)
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _displayItems.Clear();

            // Clear object pool
            while (_itemPool.Count > 0)
            {
                var item = _itemPool.Dequeue();
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _loggingService?.LogInfo("HealthCheckDisplayComponent resources cleaned up", _correlationId);
        }

        /// <summary>
        /// Generates a unique correlation ID for logging
        /// </summary>
        private static FixedString128Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..24];
            return new FixedString128Bytes($"HC-DISPLAY-{guid}");
        }

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!_enableDebugMode || !_showPerformanceStats) return;

            var rect = new Rect(10, 10, 300, 150);
            GUI.Box(rect, "Health Display Stats");

            var stats = GetPerformanceStats();
            var y = 30;
            var lineHeight = 20;

            GUI.Label(new Rect(20, y, 280, lineHeight), $"Total Updates: {stats["TotalUpdates"]}");
            y += lineHeight;

            GUI.Label(new Rect(20, y, 280, lineHeight), $"Avg Update Time: {(float)stats["AverageUpdateTime"]:F3}s");
            y += lineHeight;

            GUI.Label(new Rect(20, y, 280, lineHeight), $"Displayed Items: {stats["DisplayedItemsCount"]}");
            y += lineHeight;

            GUI.Label(new Rect(20, y, 280, lineHeight), $"Pooled Items: {stats["PooledItemsCount"]}");
            y += lineHeight;

            GUI.Label(new Rect(20, y, 280, lineHeight), $"Overall Health: {_lastOverallStatus}");
        }

        #endregion
    }

    /// <summary>
    /// Individual health check display item component
    /// </summary>
    public class HealthCheckDisplayItem : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _durationText;
        [SerializeField] private Image _statusIcon;
        [SerializeField] private Image _background;

        private string _healthCheckName;
        private HealthCheckResult _lastResult;

        public void Initialize(string healthCheckName)
        {
            _healthCheckName = healthCheckName;

            if (_nameText != null)
                _nameText.text = healthCheckName;
        }

        public void UpdateDisplay(HealthCheckResult result, bool forceUpdate = false)
        {
            if (!forceUpdate && _lastResult != null &&
                _lastResult.Status == result.Status &&
                _lastResult.Message == result.Message)
            {
                return; // No changes to display
            }

            _lastResult = result;

            if (_statusText != null)
                _statusText.text = result.Status.ToString();

            if (_messageText != null)
                _messageText.text = result.Message;

            if (_durationText != null)
                _durationText.text = $"{result.Duration.TotalMilliseconds:F0}ms";

            var color = GetHealthStatusColor(result.Status);

            if (_statusIcon != null)
                _statusIcon.color = color;

            if (_background != null)
                _background.color = GetHealthStatusBackgroundColor(result.Status);
        }

        public void Reset()
        {
            _healthCheckName = null;
            _lastResult = null;

            if (_nameText != null) _nameText.text = "";
            if (_statusText != null) _statusText.text = "";
            if (_messageText != null) _messageText.text = "";
            if (_durationText != null) _durationText.text = "";
        }

        private Color GetHealthStatusColor(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => Color.green,
                HealthStatus.Degraded => Color.yellow,
                HealthStatus.Unhealthy => Color.red,
                _ => Color.gray
            };
        }

        private Color GetHealthStatusBackgroundColor(HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Healthy => new Color(0, 1, 0, 0.1f),
                HealthStatus.Degraded => new Color(1, 1, 0, 0.1f),
                HealthStatus.Unhealthy => new Color(1, 0, 0, 0.1f),
                _ => Color.clear
            };
        }
    }
}