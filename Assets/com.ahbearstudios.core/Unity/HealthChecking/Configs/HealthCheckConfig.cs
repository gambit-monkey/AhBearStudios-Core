using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.HealthChecking.Models;
using Unity.Collections;

namespace AhBearStudios.Unity.HealthCheck.ScriptableObjects
{
    /// <summary>
    /// Unity ScriptableObject for configuring the health check system.
    /// Provides serializable configuration for health checks, circuit breakers, and degradation settings.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthCheckConfig", menuName = "AhBearStudios/Configuration/Health Check ConfigSo", order = 100)]
    public sealed class HealthCheckConfigAsset : ScriptableObject
    {
        #region Serialized Fields

        [Header("General Health Check Settings")]
        [SerializeField, Tooltip("Default interval between automatic health checks")]
        private float _defaultHealthCheckInterval = 60f;
        
        [SerializeField, Tooltip("Default timeout for health check execution")]
        private float _defaultTimeout = 30f;
        
        [SerializeField, Tooltip("Whether to enable automatic health checks")]
        private bool _enableAutomaticChecks = true;
        
        [SerializeField, Tooltip("Whether to enable health check history tracking")]
        private bool _enableHistoryTracking = true;
        
        [SerializeField, Tooltip("How long to retain health check history")]
        private float _historyRetentionHours = 24f;

        [Header("Circuit Breaker Settings")]
        [SerializeField, Tooltip("Whether to enable circuit breaker functionality")]
        private bool _enableCircuitBreakers = true;
        
        [SerializeField, Tooltip("Default number of failures before opening circuit breaker")]
        private int _defaultFailureThreshold = 5;
        
        [SerializeField, Tooltip("Default time to keep circuit breaker open (seconds)")]
        private float _defaultOpenTimeoutSeconds = 30f;
        
        [SerializeField, Tooltip("Default number of successful attempts needed to close circuit breaker")]
        private int _defaultSuccessThreshold = 2;
        
        [SerializeField, Tooltip("Whether to automatically integrate circuit breakers with health checks")]
        private bool _autoHealthCheckIntegration = true;

        [Header("Graceful Degradation Settings")]
        [SerializeField, Tooltip("Whether to enable graceful degradation")]
        private bool _enableGracefulDegradation = true;
        
        [SerializeField, Tooltip("Failure rate threshold for minor degradation (0.0 - 1.0)")]
        [Range(0f, 1f)] private float _minorDegradationThreshold = 0.10f;
        
        [SerializeField, Tooltip("Failure rate threshold for moderate degradation (0.0 - 1.0)")]
        [Range(0f, 1f)] private float _moderateDegradationThreshold = 0.25f;
        
        [SerializeField, Tooltip("Failure rate threshold for severe degradation (0.0 - 1.0)")]
        [Range(0f, 1f)] private float _severeDegradationThreshold = 0.50f;
        
        [SerializeField, Tooltip("Failure rate threshold for system disable (0.0 - 1.0)")]
        [Range(0f, 1f)] private float _disableDegradationThreshold = 0.75f;
        
        [SerializeField, Tooltip("Whether to automatically apply degradation based on health status")]
        private bool _automaticDegradation = true;
        
        [SerializeField, Tooltip("Whether to monitor for automatic recovery")]
        private bool _recoveryMonitoring = true;

        [Header("Alerting Settings")]
        [SerializeField, Tooltip("Whether to enable health check alerts")]
        private bool _enableAlerts = true;
        
        [SerializeField, Tooltip("Number of consecutive failures before triggering alert")]
        private int _alertThreshold = 3;
        
        [SerializeField, Tooltip("Whether to alert on circuit breaker state changes")]
        private bool _circuitBreakerAlerts = true;
        
        [SerializeField, Tooltip("Whether to alert on degradation level changes")]
        private bool _degradationAlerts = true;

        [Header("Scheduling Settings")]
        [SerializeField, Tooltip("Maximum number of concurrent health checks")]
        private int _maxConcurrentChecks = 10;
        
        [SerializeField, Tooltip("Maximum number of retry attempts for failed checks")]
        private int _maxRetryAttempts = 3;
        
        [SerializeField, Tooltip("Backoff delay between retry attempts (seconds)")]
        private float _retryBackoffSeconds = 5f;

        [Header("Unity-Specific Settings")]
        [SerializeField, Tooltip("Whether to enable Unity-specific health checks")]
        private bool _enableUnityHealthChecks = true;
        
        [SerializeField, Tooltip("Whether to enable Unity performance monitoring")]
        private bool _enableUnityPerformanceMonitoring = true;
        
        [SerializeField, Tooltip("Whether to enable Unity memory monitoring")]
        private bool _enableUnityMemoryMonitoring = true;
        
        [SerializeField, Tooltip("Whether to log health events to Unity console")]
        private bool _logToUnityConsole = true;

        [Header("Performance Settings")]
        [SerializeField, Tooltip("Whether to enable object pooling for performance")]
        private bool _enableObjectPooling = true;
        
        [SerializeField, Tooltip("Whether to batch health check updates")]
        private bool _enableBatchUpdates = true;
        
        [SerializeField, Tooltip("Maximum health check updates per frame")]
        private int _maxUpdatesPerFrame = 5;

        [Header("Debug Settings")]
        [SerializeField, Tooltip("Whether to enable debug mode")]
        private bool _enableDebugMode = false;
        
        [SerializeField, Tooltip("Whether to enable verbose logging")]
        private bool _verboseLogging = false;
        
        [SerializeField, Tooltip("Whether to validate configuration on start")]
        private bool _validateOnStart = true;

        [Header("Individual Health Check Configurations")]
        [SerializeField, Tooltip("Configurations for specific health checks")]
        private List<IndividualHealthCheckConfig> _individualHealthCheckConfigs = new();

        #endregion

        #region Properties

        public TimeSpan DefaultHealthCheckInterval => TimeSpan.FromSeconds(_defaultHealthCheckInterval);
        public TimeSpan DefaultTimeout => TimeSpan.FromSeconds(_defaultTimeout);
        public bool EnableAutomaticChecks => _enableAutomaticChecks;
        public bool EnableHistoryTracking => _enableHistoryTracking;
        public TimeSpan HistoryRetention => TimeSpan.FromHours(_historyRetentionHours);
        public bool EnableCircuitBreakers => _enableCircuitBreakers;
        public int DefaultFailureThreshold => _defaultFailureThreshold;
        public TimeSpan DefaultOpenTimeout => TimeSpan.FromSeconds(_defaultOpenTimeoutSeconds);
        public int DefaultSuccessThreshold => _defaultSuccessThreshold;
        public bool AutoHealthCheckIntegration => _autoHealthCheckIntegration;
        public bool EnableGracefulDegradation => _enableGracefulDegradation;
        public float MinorDegradationThreshold => _minorDegradationThreshold;
        public float ModerateDegradationThreshold => _moderateDegradationThreshold;
        public float SevereDegradationThreshold => _severeDegradationThreshold;
        public float DisableDegradationThreshold => _disableDegradationThreshold;
        public bool AutomaticDegradation => _automaticDegradation;
        public bool RecoveryMonitoring => _recoveryMonitoring;
        public bool EnableAlerts => _enableAlerts;
        public int AlertThreshold => _alertThreshold;
        public bool CircuitBreakerAlerts => _circuitBreakerAlerts;
        public bool DegradationAlerts => _degradationAlerts;
        public int MaxConcurrentChecks => _maxConcurrentChecks;
        public int MaxRetryAttempts => _maxRetryAttempts;
        public TimeSpan RetryBackoff => TimeSpan.FromSeconds(_retryBackoffSeconds);
        public bool EnableUnityHealthChecks => _enableUnityHealthChecks;
        public bool EnableUnityPerformanceMonitoring => _enableUnityPerformanceMonitoring;
        public bool EnableUnityMemoryMonitoring => _enableUnityMemoryMonitoring;
        public bool LogToUnityConsole => _logToUnityConsole;
        public bool EnableObjectPooling => _enableObjectPooling;
        public bool EnableBatchUpdates => _enableBatchUpdates;
        public int MaxUpdatesPerFrame => _maxUpdatesPerFrame;
        public bool EnableDebugMode => _enableDebugMode;
        public bool VerboseLogging => _verboseLogging;
        public bool ValidateOnStart => _validateOnStart;
        public IReadOnlyList<IndividualHealthCheckConfig> IndividualHealthCheckConfigs => _individualHealthCheckConfigs.AsReadOnly();

        #endregion

        #region Public Methods

        public List<string> ValidateConfiguration()
        {
            var errors = new List<string>();

            if (_defaultHealthCheckInterval <= 0) errors.Add("Default health check interval must be greater than 0");
            if (_defaultTimeout <= 0) errors.Add("Default timeout must be greater than 0");
            if (_historyRetentionHours <= 0) errors.Add("History retention must be greater than 0 hours");

            if (_enableCircuitBreakers)
            {
                if (_defaultFailureThreshold <= 0) errors.Add("Default failure threshold must be greater than 0");
                if (_defaultOpenTimeoutSeconds <= 0) errors.Add("Default open timeout must be greater than 0 seconds");
                if (_defaultSuccessThreshold <= 0) errors.Add("Default success threshold must be greater than 0");
            }

            if (_enableGracefulDegradation)
            {
                if (_minorDegradationThreshold >= _moderateDegradationThreshold) errors.Add("Minor degradation threshold must be less than moderate threshold");
                if (_moderateDegradationThreshold >= _severeDegradationThreshold) errors.Add("Moderate degradation threshold must be less than severe threshold");
                if (_severeDegradationThreshold >= _disableDegradationThreshold) errors.Add("Severe degradation threshold must be less than disable threshold");
                if (_disableDegradationThreshold > 1.0f) errors.Add("Disable degradation threshold cannot be greater than 1.0");
            }

            if (_maxConcurrentChecks <= 0) errors.Add("Max concurrent checks must be greater than 0");
            if (_maxRetryAttempts < 0) errors.Add("Max retry attempts cannot be negative");
            if (_retryBackoffSeconds < 0) errors.Add("Retry backoff seconds cannot be negative");
            if (_maxUpdatesPerFrame <= 0) errors.Add("Max updates per frame must be greater than 0");
            if (_enableAlerts && _alertThreshold <= 0) errors.Add("Alert threshold must be greater than 0 when alerts are enabled");

            var healthCheckNames = new HashSet<string>();
            foreach (var config in _individualHealthCheckConfigs)
            {
                errors.AddRange(config.Validate());
                if (!string.IsNullOrEmpty(config.HealthCheckName))
                {
                    if (healthCheckNames.Contains(config.HealthCheckName))
                        errors.Add($"Duplicate health check name: {config.HealthCheckName}");
                    else
                        healthCheckNames.Add(config.HealthCheckName);
                }
            }

            return errors;
        }

        public IndividualHealthCheckConfig GetHealthCheckConfig(string healthCheckName)
        {
            if (string.IsNullOrEmpty(healthCheckName)) return null;
            return _individualHealthCheckConfigs.Find(config => string.Equals(config.HealthCheckName, healthCheckName, StringComparison.OrdinalIgnoreCase));
        }

        public void SetHealthCheckConfig(IndividualHealthCheckConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.HealthCheckName)) return;

            var existingIndex = _individualHealthCheckConfigs.FindIndex(c => string.Equals(c.HealthCheckName, config.HealthCheckName, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
                _individualHealthCheckConfigs[existingIndex] = config;
            else
                _individualHealthCheckConfigs.Add(config);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public bool RemoveHealthCheckConfig(string healthCheckName)
        {
            if (string.IsNullOrEmpty(healthCheckName)) return false;
            var removed = _individualHealthCheckConfigs.RemoveAll(config => string.Equals(config.HealthCheckName, healthCheckName, StringComparison.OrdinalIgnoreCase)) > 0;
#if UNITY_EDITOR
            if (removed) UnityEditor.EditorUtility.SetDirty(this);
#endif
            return removed;
        }

        public void ResetToDefaults()
        {
            _defaultHealthCheckInterval = 60f; _defaultTimeout = 30f; _enableAutomaticChecks = true; _enableHistoryTracking = true; _historyRetentionHours = 24f;
            _enableCircuitBreakers = true; _defaultFailureThreshold = 5; _defaultOpenTimeoutSeconds = 30f; _defaultSuccessThreshold = 2; _autoHealthCheckIntegration = true;
            _enableGracefulDegradation = true; _minorDegradationThreshold = 0.10f; _moderateDegradationThreshold = 0.25f; _severeDegradationThreshold = 0.50f; _disableDegradationThreshold = 0.75f; _automaticDegradation = true; _recoveryMonitoring = true;
            _enableAlerts = true; _alertThreshold = 3; _circuitBreakerAlerts = true; _degradationAlerts = true;
            _maxConcurrentChecks = 10; _maxRetryAttempts = 3; _retryBackoffSeconds = 5f;
            _enableUnityHealthChecks = true; _enableUnityPerformanceMonitoring = true; _enableUnityMemoryMonitoring = true; _logToUnityConsole = true;
            _enableObjectPooling = true; _enableBatchUpdates = true; _maxUpdatesPerFrame = 5;
            _enableDebugMode = false; _verboseLogging = false; _validateOnStart = true;
            _individualHealthCheckConfigs.Clear();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public HealthCheckConfigAsset Clone()
        {
            var clone = CreateInstance<HealthCheckConfigAsset>();
            clone._defaultHealthCheckInterval = _defaultHealthCheckInterval; clone._defaultTimeout = _defaultTimeout; clone._enableAutomaticChecks = _enableAutomaticChecks; clone._enableHistoryTracking = _enableHistoryTracking; clone._historyRetentionHours = _historyRetentionHours;
            clone._enableCircuitBreakers = _enableCircuitBreakers; clone._defaultFailureThreshold = _defaultFailureThreshold; clone._defaultOpenTimeoutSeconds = _defaultOpenTimeoutSeconds; clone._defaultSuccessThreshold = _defaultSuccessThreshold; clone._autoHealthCheckIntegration = _autoHealthCheckIntegration;
            clone._enableGracefulDegradation = _enableGracefulDegradation; clone._minorDegradationThreshold = _minorDegradationThreshold; clone._moderateDegradationThreshold = _moderateDegradationThreshold; clone._severeDegradationThreshold = _severeDegradationThreshold; clone._disableDegradationThreshold = _disableDegradationThreshold; clone._automaticDegradation = _automaticDegradation; clone._recoveryMonitoring = _recoveryMonitoring;
            clone._enableAlerts = _enableAlerts; clone._alertThreshold = _alertThreshold; clone._circuitBreakerAlerts = _circuitBreakerAlerts; clone._degradationAlerts = _degradationAlerts;
            clone._maxConcurrentChecks = _maxConcurrentChecks; clone._maxRetryAttempts = _maxRetryAttempts; clone._retryBackoffSeconds = _retryBackoffSeconds;
            clone._enableUnityHealthChecks = _enableUnityHealthChecks; clone._enableUnityPerformanceMonitoring = _enableUnityPerformanceMonitoring; clone._enableUnityMemoryMonitoring = _enableUnityMemoryMonitoring; clone._logToUnityConsole = _logToUnityConsole;
            clone._enableObjectPooling = _enableObjectPooling; clone._enableBatchUpdates = _enableBatchUpdates; clone._maxUpdatesPerFrame = _maxUpdatesPerFrame;
            clone._enableDebugMode = _enableDebugMode; clone._verboseLogging = _verboseLogging; clone._validateOnStart = _validateOnStart;
            clone._individualHealthCheckConfigs = new List<IndividualHealthCheckConfig>();
            foreach (var config in _individualHealthCheckConfigs) clone._individualHealthCheckConfigs.Add(config.Clone());
            return clone;
        }

        public static HealthCheckConfigAsset CreateDevelopmentPreset()
        {
            var config = CreateInstance<HealthCheckConfigAsset>();
            config.name = "HealthCheck_Development";
            config._defaultHealthCheckInterval = 30f; config._defaultTimeout = 15f; config._enableAutomaticChecks = true; config._enableHistoryTracking = true; config._historyRetentionHours = 8f;
            config._enableCircuitBreakers = true; config._defaultFailureThreshold = 3; config._defaultOpenTimeoutSeconds = 15f; config._defaultSuccessThreshold = 1;
            config._enableGracefulDegradation = true; config._minorDegradationThreshold = 0.05f; config._moderateDegradationThreshold = 0.15f; config._severeDegradationThreshold = 0.30f; config._disableDegradationThreshold = 0.50f;
            config._enableAlerts = true; config._alertThreshold = 2; config._circuitBreakerAlerts = true; config._degradationAlerts = true;
            config._enableDebugMode = true; config._verboseLogging = true; config._validateOnStart = true;
            return config;
        }

        public static HealthCheckConfigAsset CreateProductionPreset()
        {
            var config = CreateInstance<HealthCheckConfigAsset>();
            config.name = "HealthCheck_Production";
            config._defaultHealthCheckInterval = 120f; config._defaultTimeout = 45f; config._enableAutomaticChecks = true; config._enableHistoryTracking = true; config._historyRetentionHours = 72f;
            config._enableCircuitBreakers = true; config._defaultFailureThreshold = 5; config._defaultOpenTimeoutSeconds = 60f; config._defaultSuccessThreshold = 3;
            config._enableGracefulDegradation = true; config._minorDegradationThreshold = 0.10f; config._moderateDegradationThreshold = 0.25f; config._severeDegradationThreshold = 0.50f; config._disableDegradationThreshold = 0.75f;
            config._enableAlerts = true; config._alertThreshold = 5; config._circuitBreakerAlerts = true; config._degradationAlerts = true;
            config._enableDebugMode = false; config._verboseLogging = false; config._validateOnStart = true;
            config._enableObjectPooling = true; config._enableBatchUpdates = true; config._maxUpdatesPerFrame = 3;
            return config;
        }

        public static HealthCheckConfigAsset CreateTestingPreset()
        {
            var config = CreateInstance<HealthCheckConfigAsset>();
            config.name = "HealthCheck_Testing";
            config._defaultHealthCheckInterval = 10f; config._defaultTimeout = 5f; config._enableAutomaticChecks = true; config._enableHistoryTracking = false;
            config._enableCircuitBreakers = true; config._defaultFailureThreshold = 1; config._defaultOpenTimeoutSeconds = 5f; config._defaultSuccessThreshold = 1;
            config._enableGracefulDegradation = false; config._enableAlerts = false;
            config._maxConcurrentChecks = 20; config._maxRetryAttempts = 0;
            config._enableDebugMode = true; config._verboseLogging = true; config._validateOnStart = true;
            return config;
        }

        #endregion

        #region Unity Event Handlers

        private void OnValidate()
        {
            _defaultHealthCheckInterval = Mathf.Max(1f, _defaultHealthCheckInterval); _defaultTimeout = Mathf.Max(1f, _defaultTimeout); _historyRetentionHours = Mathf.Max(0.1f, _historyRetentionHours);
            _defaultFailureThreshold = Mathf.Max(1, _defaultFailureThreshold); _defaultOpenTimeoutSeconds = Mathf.Max(1f, _defaultOpenTimeoutSeconds); _defaultSuccessThreshold = Mathf.Max(1, _defaultSuccessThreshold);
            _minorDegradationThreshold = Mathf.Clamp01(_minorDegradationThreshold); _moderateDegradationThreshold = Mathf.Clamp01(_moderateDegradationThreshold); _severeDegradationThreshold = Mathf.Clamp01(_severeDegradationThreshold); _disableDegradationThreshold = Mathf.Clamp01(_disableDegradationThreshold);
            if (_minorDegradationThreshold >= _moderateDegradationThreshold) _minorDegradationThreshold = Mathf.Max(0.01f, _moderateDegradationThreshold - 0.05f);
            if (_moderateDegradationThreshold >= _severeDegradationThreshold) _moderateDegradationThreshold = Mathf.Max(_minorDegradationThreshold + 0.05f, _severeDegradationThreshold - 0.05f);
            if (_severeDegradationThreshold >= _disableDegradationThreshold) _severeDegradationThreshold = Mathf.Max(_moderateDegradationThreshold + 0.05f, _disableDegradationThreshold - 0.05f);
            _alertThreshold = Mathf.Max(1, _alertThreshold); _maxConcurrentChecks = Mathf.Max(1, _maxConcurrentChecks); _maxRetryAttempts = Mathf.Max(0, _maxRetryAttempts); _retryBackoffSeconds = Mathf.Max(0f, _retryBackoffSeconds); _maxUpdatesPerFrame = Mathf.Max(1, _maxUpdatesPerFrame);
        }

        #endregion

        #region Context Menu Items

#if UNITY_EDITOR
        [ContextMenu("Validate Configuration")]
        private void ValidateConfigurationContextMenu()
        {
            var errors = ValidateConfiguration();
            if (errors.Count == 0) Debug.Log("[HealthCheckConfigAsset] Configuration is valid.");
            else Debug.LogError($"[HealthCheckConfigAsset] Configuration has {errors.Count} error(s):\n{string.Join("\n", errors)}");
        }

        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaultsContextMenu()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Reset Health Check Configuration", "This will reset all settings to their default values. This action cannot be undone.", "Reset", "Cancel"))
            {
                ResetToDefaults();
                Debug.Log("[HealthCheckConfigAsset] Configuration reset to defaults.");
            }
        }

        [ContextMenu("Add Sample Health Check Configs")]
        private void AddSampleHealthCheckConfigs()
        {
            var sampleConfigs = new[]
            {
                new IndividualHealthCheckConfig { HealthCheckName = "SystemResource", Enabled = true, Interval = TimeSpan.FromSeconds(30), Timeout = TimeSpan.FromSeconds(10), Category = HealthCheckCategory.System, FailureThreshold = 3, OpenTimeout = TimeSpan.FromSeconds(60), SuccessThreshold = 2, Description = "Monitors system resource usage including CPU, memory, and disk space", Priority = 50 },
                new IndividualHealthCheckConfig { HealthCheckName = "UnityPerformance", Enabled = true, Interval = TimeSpan.FromSeconds(15), Timeout = TimeSpan.FromSeconds(5), Category = HealthCheckCategory.Performance, FailureThreshold = 5, OpenTimeout = TimeSpan.FromSeconds(30), SuccessThreshold = 3, Description = "Monitors Unity performance metrics including frame rate and rendering", Priority = 75 },
                new IndividualHealthCheckConfig { HealthCheckName = "UnityMemory", Enabled = true, Interval = TimeSpan.FromSeconds(20), Timeout = TimeSpan.FromSeconds(5), Category = HealthCheckCategory.Performance, FailureThreshold = 4, OpenTimeout = TimeSpan.FromSeconds(45), SuccessThreshold = 2, Description = "Monitors Unity memory allocation patterns and garbage collection", Priority = 60 },
                new IndividualHealthCheckConfig { HealthCheckName = "Messaging", Enabled = true, Interval = TimeSpan.FromSeconds(60), Timeout = TimeSpan.FromSeconds(15), Category = HealthCheckCategory.System, FailureThreshold = 2, OpenTimeout = TimeSpan.FromSeconds(45), SuccessThreshold = 1, Description = "Monitors message bus functionality and communication patterns", Priority = 80 },
                new IndividualHealthCheckConfig { HealthCheckName = "UnitySystem", Enabled = true, Interval = TimeSpan.FromSeconds(45), Timeout = TimeSpan.FromSeconds(15), Category = HealthCheckCategory.System, FailureThreshold = 3, OpenTimeout = TimeSpan.FromSeconds(60), SuccessThreshold = 2, Description = "Monitors Unity subsystems including graphics, audio, and platform features", Priority = 40 }
            };

            foreach (var config in sampleConfigs)
            {
                config.AddTag("Sample"); config.AddTag("Auto-Generated");
                if (config.Category == HealthCheckCategory.Performance) config.AddTag("Performance");
                if (config.HealthCheckName.StartsWith("Unity")) config.AddTag("Unity");
                SetHealthCheckConfig(config);
            }
            Debug.Log("[HealthCheckConfigAsset] Added sample health check configurations with tags and descriptions.");
        }
#endif

        #endregion

        #region Private Helper Methods

        private void CopyFromConfiguration(HealthCheckConfigAsset source)
        {
            if (source == null) return;
            _defaultHealthCheckInterval = source._defaultHealthCheckInterval; _defaultTimeout = source._defaultTimeout; _enableAutomaticChecks = source._enableAutomaticChecks; _enableHistoryTracking = source._enableHistoryTracking; _historyRetentionHours = source._historyRetentionHours;
            _enableCircuitBreakers = source._enableCircuitBreakers; _defaultFailureThreshold = source._defaultFailureThreshold; _defaultOpenTimeoutSeconds = source._defaultOpenTimeoutSeconds; _defaultSuccessThreshold = source._defaultSuccessThreshold; _autoHealthCheckIntegration = source._autoHealthCheckIntegration;
            _enableGracefulDegradation = source._enableGracefulDegradation; _minorDegradationThreshold = source._minorDegradationThreshold; _moderateDegradationThreshold = source._moderateDegradationThreshold; _severeDegradationThreshold = source._severeDegradationThreshold; _disableDegradationThreshold = source._disableDegradationThreshold; _automaticDegradation = source._automaticDegradation; _recoveryMonitoring = source._recoveryMonitoring;
            _enableAlerts = source._enableAlerts; _alertThreshold = source._alertThreshold; _circuitBreakerAlerts = source._circuitBreakerAlerts; _degradationAlerts = source._degradationAlerts;
            _maxConcurrentChecks = source._maxConcurrentChecks; _maxRetryAttempts = source._maxRetryAttempts; _retryBackoffSeconds = source._retryBackoffSeconds;
            _enableUnityHealthChecks = source._enableUnityHealthChecks; _enableUnityPerformanceMonitoring = source._enableUnityPerformanceMonitoring; _enableUnityMemoryMonitoring = source._enableUnityMemoryMonitoring; _logToUnityConsole = source._logToUnityConsole;
            _enableObjectPooling = source._enableObjectPooling; _enableBatchUpdates = source._enableBatchUpdates; _maxUpdatesPerFrame = source._maxUpdatesPerFrame;
            _enableDebugMode = source._enableDebugMode; _verboseLogging = source._verboseLogging; _validateOnStart = source._validateOnStart;
            _individualHealthCheckConfigs = new List<IndividualHealthCheckConfig>();
            foreach (var config in source._individualHealthCheckConfigs) _individualHealthCheckConfigs.Add(config.Clone());
        }

        #endregion
    }

    [Serializable]
    public class IndividualHealthCheckConfig
    {
        [Header("Basic Settings")]
        [SerializeField, Tooltip("Name of the health check")] private string _healthCheckName = "";
        [SerializeField, Tooltip("Whether this health check is enabled")] private bool _enabled = true;
        [SerializeField, Tooltip("Health check interval in seconds")] private float _interval = 60f;
        [SerializeField, Tooltip("Health check timeout in seconds")] private float _timeout = 30f;
        [SerializeField, Tooltip("Category of this health check")] private HealthCheckCategory _category = HealthCheckCategory.Custom;

        [Header("Circuit Breaker Settings")]
        [SerializeField, Tooltip("Number of failures before opening circuit breaker")] private int _failureThreshold = 5;
        [SerializeField, Tooltip("Time to keep circuit breaker open (seconds)")] private float _openTimeout = 30f;
        [SerializeField, Tooltip("Number of successful attempts needed to close circuit breaker")] private int _successThreshold = 2;

        [Header("Advanced Settings")]
        [SerializeField, Tooltip("Priority for this health check (lower numbers = higher priority)")] private int _priority = 100;
        [SerializeField, Tooltip("Whether to retry failed health checks")] private bool _enableRetry = true;
        [SerializeField, Tooltip("Maximum retry attempts")] private int _maxRetryAttempts = 3;
        [SerializeField, Tooltip("Backoff delay between retries (seconds)")] private float _retryBackoffSeconds = 5f;

        [Header("Dependencies")]
        [SerializeField, Tooltip("Names of other health checks this one depends on")] private List<string> _dependencies = new();

        [Header("Metadata")]
        [SerializeField, Tooltip("Description of what this health check monitors")] private string _description = "";
        [SerializeField, Tooltip("Tags for categorizing and filtering")] private List<string> _tags = new();

        public string HealthCheckName { get => _healthCheckName; set => _healthCheckName = value ?? ""; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public TimeSpan Interval { get => TimeSpan.FromSeconds(_interval); set => _interval = (float)value.TotalSeconds; }
        public TimeSpan Timeout { get => TimeSpan.FromSeconds(_timeout); set => _timeout = (float)value.TotalSeconds; }
        public HealthCheckCategory Category { get => _category; set => _category = value; }
        public int FailureThreshold { get => _failureThreshold; set => _failureThreshold = Mathf.Max(1, value); }
        public TimeSpan OpenTimeout { get => TimeSpan.FromSeconds(_openTimeout); set => _openTimeout = (float)value.TotalSeconds; }
        public int SuccessThreshold { get => _successThreshold; set => _successThreshold = Mathf.Max(1, value); }
        public int Priority { get => _priority; set => _priority = value; }
        public bool EnableRetry { get => _enableRetry; set => _enableRetry = value; }
        public int MaxRetryAttempts { get => _maxRetryAttempts; set => _maxRetryAttempts = Mathf.Max(0, value); }
        public TimeSpan RetryBackoff { get => TimeSpan.FromSeconds(_retryBackoffSeconds); set => _retryBackoffSeconds = (float)value.TotalSeconds; }
        public IReadOnlyList<string> Dependencies => _dependencies.AsReadOnly();
        public string Description { get => _description; set => _description = value ?? ""; }
        public IReadOnlyList<string> Tags => _tags.AsReadOnly();

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(_healthCheckName)) errors.Add("Health check name cannot be empty");
            if (_interval <= 0) errors.Add($"Health check '{_healthCheckName}': Interval must be greater than 0");
            if (_timeout <= 0) errors.Add($"Health check '{_healthCheckName}': Timeout must be greater than 0");
            if (_failureThreshold <= 0) errors.Add($"Health check '{_healthCheckName}': Failure threshold must be greater than 0");
            if (_openTimeout <= 0) errors.Add($"Health check '{_healthCheckName}': Open timeout must be greater than 0");
            if (_successThreshold <= 0) errors.Add($"Health check '{_healthCheckName}': Success threshold must be greater than 0");
            if (_enableRetry && _maxRetryAttempts < 0) errors.Add($"Health check '{_healthCheckName}': Max retry attempts cannot be negative");
            if (_enableRetry && _retryBackoffSeconds < 0) errors.Add($"Health check '{_healthCheckName}': Retry backoff cannot be negative");
            if (_dependencies.Contains(_healthCheckName)) errors.Add($"Health check '{_healthCheckName}': Cannot depend on itself");
            return errors;
        }

        public void AddDependency(string dependencyName)
        {
            if (!string.IsNullOrWhiteSpace(dependencyName) && !_dependencies.Contains(dependencyName) && dependencyName != _healthCheckName)
                _dependencies.Add(dependencyName);
        }

        public bool RemoveDependency(string dependencyName) => _dependencies.Remove(dependencyName);

        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag)) _tags.Add(tag);
        }

        public bool RemoveTag(string tag) => _tags.Remove(tag);
        public bool HasTag(string tag) => _tags.Contains(tag);

        public IndividualHealthCheckConfig Clone()
        {
            return new IndividualHealthCheckConfig
            {
                _healthCheckName = _healthCheckName, _enabled = _enabled, _interval = _interval, _timeout = _timeout, _category = _category,
                _failureThreshold = _failureThreshold, _openTimeout = _openTimeout, _successThreshold = _successThreshold, _priority = _priority,
                _enableRetry = _enableRetry, _maxRetryAttempts = _maxRetryAttempts, _retryBackoffSeconds = _retryBackoffSeconds,
                _dependencies = new List<string>(_dependencies), _description = _description, _tags = new List<string>(_tags)
            };
        }

        public FixedString64Bytes ToFixedString() => new FixedString64Bytes(_healthCheckName);

        public string GetSummary() => $"{_healthCheckName} ({_category}) - Interval: {_interval}s, Timeout: {_timeout}s, Priority: {_priority}, Enabled: {_enabled}";

        public float CalculateLoadFactor()
        {
            if (!_enabled || _interval <= 0) return 0f;
            return 60f / _interval;
        }

        public bool IsPerformanceCritical() => _enabled && _interval <= 30f && _priority <= 50;

        public List<string> GetConfigurationWarnings()
        {
            var warnings = new List<string>();
            if (_enabled)
            {
                if (_interval < 5f) warnings.Add($"Very frequent interval ({_interval}s) may impact performance");
                if (_timeout >= _interval) warnings.Add($"Timeout ({_timeout}s) should be less than interval ({_interval}s)");
                if (_failureThreshold == 1) warnings.Add("Failure threshold of 1 may cause frequent circuit breaker trips");
                if (_openTimeout < 10f) warnings.Add($"Short open timeout ({_openTimeout}s) may not allow sufficient recovery time");
                if (_maxRetryAttempts > 5) warnings.Add($"High retry attempts ({_maxRetryAttempts}) may increase latency");
                if (_dependencies.Count > 3) warnings.Add($"High number of dependencies ({_dependencies.Count}) may create complex dependency chains");
            }
            return warnings;
        }

        private void OnValidate()
        {
            _interval = Mathf.Max(1f, _interval); _timeout = Mathf.Max(1f, _timeout); _failureThreshold = Mathf.Max(1, _failureThreshold);
            _openTimeout = Mathf.Max(1f, _openTimeout); _successThreshold = Mathf.Max(1, _successThreshold);
            _maxRetryAttempts = Mathf.Max(0, _maxRetryAttempts); _retryBackoffSeconds = Mathf.Max(0f, _retryBackoffSeconds);
            _dependencies.RemoveAll(string.IsNullOrWhiteSpace); _tags.RemoveAll(string.IsNullOrWhiteSpace);
            _dependencies.Remove(_healthCheckName); _dependencies = _dependencies.Distinct().ToList(); _tags = _tags.Distinct().ToList();
        }

        public override string ToString() => GetSummary();
        public override bool Equals(object obj) => obj is IndividualHealthCheckConfig other && string.Equals(_healthCheckName, other._healthCheckName, StringComparison.OrdinalIgnoreCase);
        public override int GetHashCode() => _healthCheckName?.ToLowerInvariant().GetHashCode() ?? 0;
    }
}