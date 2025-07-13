using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Logging;
using Reflex.Attributes;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AhBearStudios.Unity.HealthCheck.Components
{
    /// <summary>
    /// Unity UI component for displaying real-time circuit breaker status and statistics.
    /// Provides comprehensive circuit breaker monitoring with automatic updates and interactive controls.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class CircuitBreakerDisplayComponent : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Transform _circuitBreakerContainer;
        [SerializeField] private GameObject _circuitBreakerItemPrefab;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Text _statisticsText;
        [SerializeField] private Text _lastUpdateText;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _resetAllButton;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Display Settings")]
        [SerializeField, Range(1f, 30f)] private float _updateInterval = 3.0f;
        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private bool _showStatistics = true;
        [SerializeField] private bool _enableInteractiveControls = true;
        [SerializeField, Range(5, 100)] private int _maxDisplayedCircuitBreakers = 50;

        [Header("Circuit Breaker State Colors")]
        [SerializeField] private Color _closedColor = Color.green;
        [SerializeField] private Color _openColor = Color.red;
        [SerializeField] private Color _halfOpenColor = Color.yellow;
        [SerializeField] private Color _unknownColor = Color.gray;

        [Header("Background Colors")]
        [SerializeField] private Color _closedBackgroundColor = new Color(0, 1, 0, 0.1f);
        [SerializeField] private Color _openBackgroundColor = new Color(1, 0, 0, 0.1f);
        [SerializeField] private Color _halfOpenBackgroundColor = new Color(1, 1, 0, 0.1f);

        [Header("Animation Settings")]
        [SerializeField] private bool _enableAnimations = true;
        [SerializeField] private float _stateChangeDuration = 0.5f;
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Performance Settings")]
        [SerializeField] private bool _enableObjectPooling = true;
        [SerializeField] private bool _batchUpdates = true;
        [SerializeField, Range(1, 10)] private int _maxUpdatesPerFrame = 5;

        [Header("Debug Features")]
        [SerializeField] private bool _enableDebugMode = false;
        [SerializeField] private bool _logStateChanges = false;
        [SerializeField] private bool _showPerformanceStats = false;

        #endregion

        #region Private Fields

        // Dependencies
        [Inject] private IHealthCheckService _healthCheckService;
        [Inject] private ILoggingService _loggingService;

        // UI Management
        private readonly Dictionary<string, CircuitBreakerDisplayItem> _displayItems = new();
        private readonly Queue<CircuitBreakerDisplayItem> _itemPool = new();
        
        // State Management
        private bool _initialized = false;
        private Coroutine _updateCoroutine;
        private Dictionary<string, CircuitBreakerState> _lastStates = new();
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly FixedString128Bytes _correlationId = GenerateCorrelationId();

        // Performance Tracking
        private int _totalUpdates = 0;
        private float _totalUpdateTime = 0f;
        private readonly Queue<float> _updateTimeHistory = new();
        private const int UPDATE_HISTORY_SIZE = 100;

        // Statistics
        private int _totalCircuitBreakers = 0;
        private int _openCircuitBreakers = 0;
        private int _halfOpenCircuitBreakers = 0;
        private int _closedCircuitBreakers = 0;

        // Event Handlers
        private System.Action<object, CircuitBreakerStateChangedEventArgs> _stateChangedHandler;

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
            _updateInterval = Mathf.Max(1f, _updateInterval);
            _maxDisplayedCircuitBreakers = Mathf.Max(5, _maxDisplayedCircuitBreakers);
            _maxUpdatesPerFrame = Mathf.Max(1, _maxUpdatesPerFrame);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually refreshes the circuit breaker display
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
        /// Resets all circuit breakers to closed state
        /// </summary>
        public void ResetAllCircuitBreakers()
        {
            if (!_initialized || !_enableInteractiveControls)
            {
                _loggingService?.LogWarning("Cannot reset circuit breakers - component not initialized or controls disabled", _correlationId);
                return;
            }

            StartCoroutine(ResetAllCircuitBreakersAsync());
        }

        /// <summary>
        /// Resets a specific circuit breaker by operation name
        /// </summary>
        /// <param name="operationName">The name of the operation whose circuit breaker should be reset</param>
        public void ResetCircuitBreaker(string operationName)
        {
            if (!_initialized || !_enableInteractiveControls || string.IsNullOrEmpty(operationName))
            {
                _loggingService?.LogWarning($"Cannot reset circuit breaker '{operationName}' - invalid state or parameters", _correlationId);
                return;
            }

            try
            {
                _healthCheckService.ResetCircuitBreaker(operationName);
                _loggingService?.LogInfo($"Circuit breaker reset manually: {operationName}", _correlationId);
                
                // Trigger immediate refresh to show the change
                RefreshDisplay();
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, $"Failed to reset circuit breaker: {operationName}", _correlationId);
            }
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

                _loggingService?.LogInfo($"Circuit breaker display auto-refresh {(enabled ? "enabled" : "disabled")}", _correlationId);
            }
        }

        /// <summary>
        /// Gets current circuit breaker statistics
        /// </summary>
        /// <returns>Dictionary containing circuit breaker statistics</returns>
        public Dictionary<string, object> GetCircuitBreakerStatistics()
        {
            return new Dictionary<string, object>
            {
                ["TotalCircuitBreakers"] = _totalCircuitBreakers,
                ["OpenCircuitBreakers"] = _openCircuitBreakers,
                ["HalfOpenCircuitBreakers"] = _halfOpenCircuitBreakers,
                ["ClosedCircuitBreakers"] = _closedCircuitBreakers,
                ["OpenPercentage"] = _totalCircuitBreakers > 0 ? (float)_openCircuitBreakers / _totalCircuitBreakers * 100f : 0f,
                ["LastUpdateTime"] = _lastUpdateTime,
                ["TotalUpdates"] = _totalUpdates,
                ["AverageUpdateTime"] = _updateTimeHistory.Count > 0 ? _updateTimeHistory.Average() : 0f
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

            if (_circuitBreakerContainer == null) errors.Add("Circuit Breaker Container");
            if (_circuitBreakerItemPrefab == null) errors.Add("Circuit Breaker Item Prefab");
            if (_summaryText == null) errors.Add("Summary Text");

            if (errors.Any())
            {
                var errorMessage = $"Missing required components: {string.Join(", ", errors)}";
                Debug.LogError($"[CircuitBreakerDisplayComponent] {errorMessage}");
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Initializes event handler delegates
        /// </summary>
        private void InitializeEventHandlers()
        {
            _stateChangedHandler = OnCircuitBreakerStateChanged;
        }

        /// <summary>
        /// Asynchronously initializes the component with health check service integration
        /// </summary>
        private IEnumerator InitializeAsync()
        {
            _loggingService?.LogInfo("Initializing CircuitBreakerDisplayComponent", _correlationId);

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

                // Subscribe to circuit breaker events
                SubscribeToEvents();

                // Perform initial update
                yield return StartCoroutine(UpdateDisplayAsync(forceUpdate: true));

                // Start automatic updates if enabled
                if (_autoRefresh)
                {
                    StartUpdateCoroutine();
                }

                _initialized = true;
                _loggingService?.LogInfo("CircuitBreakerDisplayComponent initialized successfully", _correlationId);
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to initialize CircuitBreakerDisplayComponent", _correlationId);
                Debug.LogError($"[CircuitBreakerDisplayComponent] Initialization failed: {ex.Message}");
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

            if (_resetAllButton != null)
            {
                _resetAllButton.onClick.RemoveAllListeners();
                _resetAllButton.onClick.AddListener(ResetAllCircuitBreakers);
                _resetAllButton.interactable = _enableInteractiveControls;
            }

            // Initialize text fields
            UpdateSummaryDisplay(0, 0, 0, 0);
            UpdateStatisticsDisplay();

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
        /// Pre-populates the object pool with circuit breaker display items
        /// </summary>
        private void PrePopulateItemPool()
        {
            if (_circuitBreakerItemPrefab == null || _circuitBreakerContainer == null) return;

            var initialPoolSize = Mathf.Min(10, _maxDisplayedCircuitBreakers);
            
            for (int i = 0; i < initialPoolSize; i++)
            {
                var item = CreateCircuitBreakerDisplayItem();
                item.gameObject.SetActive(false);
                _itemPool.Enqueue(item);
            }

            _loggingService?.LogDebug($"Pre-populated circuit breaker object pool with {initialPoolSize} items", _correlationId);
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
        /// Updates the circuit breaker display with current data
        /// </summary>
        private IEnumerator UpdateDisplayAsync(bool forceUpdate = false)
        {
            var startTime = Time.realtimeSinceStartup;

            try
            {
                // Get current circuit breaker states
                var circuitBreakerStates = _healthCheckService.GetAllCircuitBreakerStates();

                // Update statistics
                CalculateStatistics(circuitBreakerStates);

                // Update summary display
                UpdateSummaryDisplay(_totalCircuitBreakers, _openCircuitBreakers, _halfOpenCircuitBreakers, _closedCircuitBreakers);

                // Update statistics display if enabled
                if (_showStatistics)
                {
                    UpdateStatisticsDisplay();
                }

                // Update individual circuit breaker displays
                if (_batchUpdates)
                {
                    yield return StartCoroutine(BatchUpdateCircuitBreakerItems(circuitBreakerStates, forceUpdate));
                }
                else
                {
                    yield return StartCoroutine(UpdateCircuitBreakerItems(circuitBreakerStates, forceUpdate));
                }

                // Update performance tracking
                UpdatePerformanceTracking(startTime);
                UpdateLastUpdateTime();

                // Store current states for comparison
                _lastStates = new Dictionary<string, CircuitBreakerState>(circuitBreakerStates);
                _totalUpdates++;
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Error updating circuit breaker display", _correlationId);
            }
        }

        /// <summary>
        /// Calculates circuit breaker statistics
        /// </summary>
        private void CalculateStatistics(Dictionary<string, CircuitBreakerState> states)
        {
            _totalCircuitBreakers = states.Count;
            _openCircuitBreakers = states.Count(kvp => kvp.Value == CircuitBreakerState.Open);
            _halfOpenCircuitBreakers = states.Count(kvp => kvp.Value == CircuitBreakerState.HalfOpen);
            _closedCircuitBreakers = states.Count(kvp => kvp.Value == CircuitBreakerState.Closed);
        }

        /// <summary>
        /// Updates individual circuit breaker display items with batching
        /// </summary>
        private IEnumerator BatchUpdateCircuitBreakerItems(Dictionary<string, CircuitBreakerState> states, bool forceUpdate)
        {
            var batches = CreateUpdateBatches(states.ToList());
            
            foreach (var batch in batches)
            {
                var updatedThisFrame = 0;
                
                foreach (var kvp in batch)
                {
                    UpdateSingleCircuitBreakerItem(kvp.Key, kvp.Value, forceUpdate);
                    updatedThisFrame++;
                    
                    if (updatedThisFrame >= _maxUpdatesPerFrame)
                    {
                        yield return null; // Wait for next frame
                        updatedThisFrame = 0;
                    }
                }
                
                yield return null; // Wait between batches
            }

            // Clean up items for circuit breakers that no longer exist
            CleanupOldCircuitBreakerItems(states);
        }

        /// <summary>
        /// Updates individual circuit breaker display items without batching
        /// </summary>
        private IEnumerator UpdateCircuitBreakerItems(Dictionary<string, CircuitBreakerState> states, bool forceUpdate)
        {
            foreach (var kvp in states)
            {
                UpdateSingleCircuitBreakerItem(kvp.Key, kvp.Value, forceUpdate);
                yield return null; // Spread updates across frames
            }

            CleanupOldCircuitBreakerItems(states);
        }

        /// <summary>
        /// Updates a single circuit breaker display item
        /// </summary>
        private void UpdateSingleCircuitBreakerItem(string operationName, CircuitBreakerState state, bool forceUpdate)
        {
            if (!_displayItems.TryGetValue(operationName, out var displayItem))
            {
                displayItem = GetOrCreateCircuitBreakerDisplayItem(operationName);
                _displayItems[operationName] = displayItem;
            }

            var hasStateChanged = !_lastStates.ContainsKey(operationName) || _lastStates[operationName] != state;
            displayItem.UpdateDisplay(operationName, state, hasStateChanged, forceUpdate);

            // Log state changes if enabled
            if (hasStateChanged && _logStateChanges && _lastStates.ContainsKey(operationName))
            {
                _loggingService?.LogInfo($"Circuit breaker state changed: {operationName} -> {state}", _correlationId);
            }
        }

        /// <summary>
        /// Creates update batches to optimize performance
        /// </summary>
        private List<List<KeyValuePair<string, CircuitBreakerState>>> CreateUpdateBatches(List<KeyValuePair<string, CircuitBreakerState>> items)
        {
            var batches = new List<List<KeyValuePair<string, CircuitBreakerState>>>();
            var batchSize = Mathf.Max(1, _maxUpdatesPerFrame);
            
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }
            
            return batches;
        }

        /// <summary>
        /// Removes display items for circuit breakers that no longer exist
        /// </summary>
        private void CleanupOldCircuitBreakerItems(Dictionary<string, CircuitBreakerState> currentStates)
        {
            var itemsToRemove = _displayItems.Keys.Where(name => !currentStates.ContainsKey(name)).ToList();

            foreach (var name in itemsToRemove)
            {
                var item = _displayItems[name];
                _displayItems.Remove(name);
                ReturnItemToPool(item);
            }
        }

        /// <summary>
        /// Resets all circuit breakers asynchronously
        /// </summary>
        private IEnumerator ResetAllCircuitBreakersAsync()
        {
            try
            {
                _loggingService?.LogInfo("Resetting all circuit breakers", _correlationId);

                var circuitBreakerStates = _healthCheckService.GetAllCircuitBreakerStates();
                var resetCount = 0;

                foreach (var operationName in circuitBreakerStates.Keys)
                {
                    try
                    {
                        _healthCheckService.ResetCircuitBreaker(operationName);
                        resetCount++;
                        
                        // Yield every few operations to avoid blocking
                        if (resetCount % 5 == 0)
                        {
                            yield return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogException(ex, $"Failed to reset circuit breaker: {operationName}", _correlationId);
                    }
                }

                _loggingService?.LogInfo($"Reset {resetCount} circuit breakers", _correlationId);
                
                // Trigger immediate refresh to show the changes
                yield return StartCoroutine(UpdateDisplayAsync(forceUpdate: true));
            }
            catch (Exception ex)
            {
                _loggingService?.LogException(ex, "Failed to reset all circuit breakers", _correlationId);
            }
        }

        #endregion

        #region Private Item Management Methods

        /// <summary>
        /// Gets an existing item or creates a new one for the specified circuit breaker
        /// </summary>
        private CircuitBreakerDisplayItem GetOrCreateCircuitBreakerDisplayItem(string operationName)
        {
            if (_itemPool.Count > 0)
            {
                var pooledItem = _itemPool.Dequeue();
                pooledItem.gameObject.SetActive(true);
                pooledItem.Initialize(operationName, _enableInteractiveControls, ResetCircuitBreaker);
                return pooledItem;
            }

            return CreateCircuitBreakerDisplayItem(operationName);
        }

        /// <summary>
        /// Creates a new circuit breaker display item
        /// </summary>
        private CircuitBreakerDisplayItem CreateCircuitBreakerDisplayItem(string operationName = null)
        {
            var itemObject = Instantiate(_circuitBreakerItemPrefab, _circuitBreakerContainer);
            var displayItem = itemObject.GetComponent<CircuitBreakerDisplayItem>();
            
            if (displayItem == null)
            {
                displayItem = itemObject.AddComponent<CircuitBreakerDisplayItem>();
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                displayItem.Initialize(operationName, _enableInteractiveControls, ResetCircuitBreaker);
            }

            return displayItem;
        }

        /// <summary>
        /// Returns a display item to the object pool
        /// </summary>
        private void ReturnItemToPool(CircuitBreakerDisplayItem item)
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

        #region Private Display Update Methods

        /// <summary>
        /// Updates the summary display with current statistics
        /// </summary>
        private void UpdateSummaryDisplay(int total, int open, int halfOpen, int closed)
        {
            if (_summaryText != null)
            {
                var openPercentage = total > 0 ? (float)open / total * 100f : 0f;
                _summaryText.text = $"Circuit Breakers: {total} Total | {closed} Closed | {halfOpen} Half-Open | {open} Open ({openPercentage:F1}%)";
                
                // Set color based on open percentage
                if (openPercentage >= 50f)
                    _summaryText.color = _openColor;
                else if (openPercentage >= 25f)
                    _summaryText.color = _halfOpenColor;
                else
                    _summaryText.color = _closedColor;
            }
        }

        /// <summary>
        /// Updates the statistics display with performance metrics
        /// </summary>
        private void UpdateStatisticsDisplay()
        {
            if (_statisticsText != null && _showStatistics)
            {
                var avgUpdateTime = _updateTimeHistory.Count > 0 ? _updateTimeHistory.Average() : 0f;
                var stats = $"Updates: {_totalUpdates} | Avg Time: {avgUpdateTime:F3}s | Items: {_displayItems.Count} | Pool: {_itemPool.Count}";
                _statisticsText.text = stats;
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
                _loggingService?.LogDebug($"Circuit breaker display update performance - Current: {updateTime:F3}s, Average: {avgTime:F3}s", _correlationId);
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

        #endregion

        #region Private Event Methods

        /// <summary>
        /// Subscribes to health check service events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_healthCheckService != null)
            {
                _healthCheckService.CircuitBreakerStateChanged += _stateChangedHandler;
            }
        }

        /// <summary>
        /// Unsubscribes from health check service events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_healthCheckService != null)
            {
                _healthCheckService.CircuitBreakerStateChanged -= _stateChangedHandler;
            }
        }

        /// <summary>
        /// Handles circuit breaker state changed events
        /// </summary>
        private void OnCircuitBreakerStateChanged(object sender, CircuitBreakerStateChangedEventArgs args)
        {
            if (_logStateChanges)
            {
                _loggingService?.LogInfo($"Circuit breaker event: {args.OperationName} -> {args.NewState}", _correlationId);
            }

            // Trigger immediate refresh on state changes
            if (_initialized && gameObject.activeInHierarchy)
            {
                StartCoroutine(UpdateDisplayAsync(forceUpdate: true));
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets the appropriate color for a circuit breaker state
        /// </summary>
        private Color GetCircuitBreakerStateColor(CircuitBreakerState state)
        {
            return state switch
            {
                CircuitBreakerState.Closed => _closedColor,
                CircuitBreakerState.Open => _openColor,
                CircuitBreakerState.HalfOpen => _halfOpenColor,
                _ => _unknownColor
            };
        }

        /// <summary>
        /// Gets the appropriate background color for a circuit breaker state
        /// </summary>
        private Color GetCircuitBreakerStateBackgroundColor(CircuitBreakerState state)
        {
            return state switch
            {
                CircuitBreakerState.Closed => _closedBackgroundColor,
                CircuitBreakerState.Open => _openBackgroundColor,
                CircuitBreakerState.HalfOpen => _halfOpenBackgroundColor,
                _ => Color.clear
            };
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

            _loggingService?.LogInfo("CircuitBreakerDisplayComponent resources cleaned up", _correlationId);
        }

        /// <summary>
        /// Generates a unique correlation ID for logging
        /// </summary>
        private static FixedString128Bytes GenerateCorrelationId()
        {
            var guid = Guid.NewGuid().ToString("N")[..24];
            return new FixedString128Bytes($"CB-DISPLAY-{guid}");
        }

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!_enableDebugMode || !_showPerformanceStats) return;

            var rect = new Rect(320, 10, 300, 150);
            GUI.Box(rect, "Circuit Breaker Stats");

            var stats = GetCircuitBreakerStatistics();
            var y = 30;
            var lineHeight = 20;

            GUI.Label(new Rect(330, y, 280, lineHeight), $"Total: {stats["TotalCircuitBreakers"]}");
            y += lineHeight;
            
            GUI.Label(new Rect(330, y, 280, lineHeight), $"Open: {stats["OpenCircuitBreakers"]} ({stats["OpenPercentage"]:F1}%)");
            y += lineHeight;
            
            GUI.Label(new Rect(330, y, 280, lineHeight), $"Half-Open: {stats["HalfOpenCircuitBreakers"]}");
            y += lineHeight;
            
            GUI.Label(new Rect(330, y, 280, lineHeight), $"Closed: {stats["ClosedCircuitBreakers"]}");
            y += lineHeight;
            
            GUI.Label(new Rect(330, y, 280, lineHeight), $"Updates: {stats["TotalUpdates"]}");
        }

        #endregion
    }

    /// <summary>
    /// Individual circuit breaker display item component
    /// </summary>
    public class CircuitBreakerDisplayItem : MonoBehaviour
    {
        [SerializeField] private Text _operationNameText;
        [SerializeField] private Text _stateText;
        [SerializeField] private Text _failureCountText;
        [SerializeField] private Text _lastFailureTimeText;
        [SerializeField] private Image _stateIcon;
        [SerializeField] private Image _background;
        [SerializeField] private Button _resetButton;

        private string _operationName;
        private CircuitBreakerState _lastState = CircuitBreakerState.Closed;
        private System.Action<string> _resetCallback;

        public void Initialize(string operationName, bool enableResetButton, System.Action<string> resetCallback)
        {
            _operationName = operationName;
            _resetCallback = resetCallback;
            
            if (_operationNameText != null)
                _operationNameText.text = operationName;

            if (_resetButton != null)
            {
                _resetButton.gameObject.SetActive(enableResetButton);
                _resetButton.onClick.RemoveAllListeners();
                if (enableResetButton && resetCallback != null)
                {
                    _resetButton.onClick.AddListener(() => resetCallback(operationName));
                }
            }
        }

        public void UpdateDisplay(string operationName, CircuitBreakerState state, bool hasStateChanged, bool forceUpdate = false)
        {
            if (!forceUpdate && !hasStateChanged && _lastState == state)
            {
                return; // No changes to display
            }

            _lastState = state;

            if (_stateText != null)
                _stateText.text = state.ToString();

            // Update colors based on state
            var color = GetCircuitBreakerStateColor(state);
            var backgroundColor = GetCircuitBreakerStateBackgroundColor(state);
            
            if (_stateIcon != null)
                _stateIcon.color = color;

            if (_background != null)
                _background.color = backgroundColor;

            // Update reset button state
            if (_resetButton != null)
            {
                _resetButton.interactable = state != CircuitBreakerState.Closed;
            }

            // Try to get additional circuit breaker info if available
            // This would require access to the health check service, which could be injected
            // For now, we'll just show basic state information
        }

        public void Reset()
        {
            _operationName = null;
            _lastState = CircuitBreakerState.Closed;
            _resetCallback = null;
            
            if (_operationNameText != null) _operationNameText.text = "";
            if (_stateText != null) _stateText.text = "";
            if (_failureCountText != null) _failureCountText.text = "";
            if (_lastFailureTimeText != null) _lastFailureTimeText.text = "";
            
            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.gameObject.SetActive(false);
            }
        }

        private Color GetCircuitBreakerStateColor(CircuitBreakerState state)
        {
            return state switch
            {
                CircuitBreakerState.Closed => Color.green,
                CircuitBreakerState.Open => Color.red,
                CircuitBreakerState.HalfOpen => Color.yellow,
                _ => Color.gray
            };
        }

        private Color GetCircuitBreakerStateBackgroundColor(CircuitBreakerState state)
        {
            return state switch
            {
                CircuitBreakerState.Closed => new Color(0, 1, 0, 0.1f),
                CircuitBreakerState.Open => new Color(1, 0, 0, 0.1f),
                CircuitBreakerState.HalfOpen => new Color(1, 1, 0, 0.1f),
                _ => Color.clear
            };
        }
    }
}