using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Targets
{
    /// <summary>
    /// ScriptableObject configuration for Serilog target.
    /// Provides Unity-serializable configuration for Serilog integration.
    /// Follows AhBearStudios Core Architecture Unity integration patterns.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Targets/Serilog Target Configuration", 
        fileName = "SerilogTargetConfig", 
        order = 10)]
    public class SerilogTargetConfig : ScriptableObject
    {
        [Header("Basic Settings")]
        [SerializeField] private string _targetName = "Serilog";
        [SerializeField] private LogLevel _minimumLevel = LogLevel.Debug;
        [SerializeField] private bool _isEnabled = true;
        [SerializeField] private bool _useAsyncWrite = true;
        [SerializeField] private int _maxConcurrentAsyncOperations = 4;

        [Header("Console Output")]
        [SerializeField] private bool _enableConsole = true;
        [SerializeField] private string _consoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        [Header("File Output")]
        [SerializeField] private bool _enableFileLogging = true;
        [SerializeField] private string _filePath = "Logs/serilog.log";
        [SerializeField] private bool _rollOnFileSizeLimit = true;
        [SerializeField] private int _fileSizeLimitMB = 10;
        [SerializeField] private int _retainedFileCountLimit = 5;
        [SerializeField] private bool _sharedFileAccess = false;
        [SerializeField] private bool _useJsonFormatter = true;

        [Header("Debug Output")]
        [SerializeField] private bool _enableDebug = false;
        [SerializeField] private string _debugOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        [Header("Performance Settings")]
        [SerializeField] private bool _enablePerformanceMetrics = true;
        [SerializeField] private float _frameBudgetThresholdMs = 0.5f;
        [SerializeField] private float _errorRateThreshold = 0.1f;
        [SerializeField] private float _healthCheckIntervalSeconds = 60f;

        [Header("Platform-Specific Settings")]
        [SerializeField] private bool _mobileOptimized = false;
        [SerializeField] private bool _webGLCompatible = false;
        [SerializeField] private bool _desktopOptimized = true;

        [Header("Channel Filtering")]
        [SerializeField] private List<string> _channels = new List<string>();
        [SerializeField] private bool _useChannelWhitelist = false;

        [Header("Advanced Settings")]
        [SerializeField] private string _outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
        [SerializeField] private bool _enableStructuredLogging = true;
        [SerializeField] private bool _includeCorrelationId = true;
        [SerializeField] private bool _includeSourceContext = true;
        [SerializeField] private bool _includeThreadId = true;

        /// <summary>
        /// Gets the target name.
        /// </summary>
        public string TargetName => _targetName;

        /// <summary>
        /// Gets the minimum log level.
        /// </summary>
        public LogLevel MinimumLevel => _minimumLevel;

        /// <summary>
        /// Gets whether the target is enabled.
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Gets whether async write is enabled.
        /// </summary>
        public bool UseAsyncWrite => _useAsyncWrite;

        /// <summary>
        /// Gets the maximum concurrent async operations.
        /// </summary>
        public int MaxConcurrentAsyncOperations => _maxConcurrentAsyncOperations;

        /// <summary>
        /// Gets whether console output is enabled.
        /// </summary>
        public bool EnableConsole => _enableConsole;

        /// <summary>
        /// Gets the console output template.
        /// </summary>
        public string ConsoleOutputTemplate => _consoleOutputTemplate;

        /// <summary>
        /// Gets whether file logging is enabled.
        /// </summary>
        public bool EnableFileLogging => _enableFileLogging;

        /// <summary>
        /// Gets the file path.
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Gets whether to roll on file size limit.
        /// </summary>
        public bool RollOnFileSizeLimit => _rollOnFileSizeLimit;

        /// <summary>
        /// Gets the file size limit in bytes.
        /// </summary>
        public long FileSizeLimitBytes => _fileSizeLimitMB * 1024L * 1024L;

        /// <summary>
        /// Gets the retained file count limit.
        /// </summary>
        public int RetainedFileCountLimit => _retainedFileCountLimit;

        /// <summary>
        /// Gets whether shared file access is enabled.
        /// </summary>
        public bool SharedFileAccess => _sharedFileAccess;

        /// <summary>
        /// Gets whether to use JSON formatter.
        /// </summary>
        public bool UseJsonFormatter => _useJsonFormatter;

        /// <summary>
        /// Gets whether debug output is enabled.
        /// </summary>
        public bool EnableDebug => _enableDebug;

        /// <summary>
        /// Gets the debug output template.
        /// </summary>
        public string DebugOutputTemplate => _debugOutputTemplate;

        /// <summary>
        /// Gets whether performance metrics are enabled.
        /// </summary>
        public bool EnablePerformanceMetrics => _enablePerformanceMetrics;

        /// <summary>
        /// Gets the frame budget threshold in milliseconds.
        /// </summary>
        public float FrameBudgetThresholdMs => _frameBudgetThresholdMs;

        /// <summary>
        /// Gets the error rate threshold.
        /// </summary>
        public float ErrorRateThreshold => _errorRateThreshold;

        /// <summary>
        /// Gets the health check interval in seconds.
        /// </summary>
        public float HealthCheckIntervalSeconds => _healthCheckIntervalSeconds;

        /// <summary>
        /// Gets whether mobile optimization is enabled.
        /// </summary>
        public bool MobileOptimized => _mobileOptimized;

        /// <summary>
        /// Gets whether WebGL compatibility is enabled.
        /// </summary>
        public bool WebGLCompatible => _webGLCompatible;

        /// <summary>
        /// Gets whether desktop optimization is enabled.
        /// </summary>
        public bool DesktopOptimized => _desktopOptimized;

        /// <summary>
        /// Gets the list of channels to filter.
        /// </summary>
        public IReadOnlyList<string> Channels => _channels.AsReadOnly();

        /// <summary>
        /// Gets whether to use channel whitelist filtering.
        /// </summary>
        public bool UseChannelWhitelist => _useChannelWhitelist;

        /// <summary>
        /// Gets the output template.
        /// </summary>
        public string OutputTemplate => _outputTemplate;

        /// <summary>
        /// Gets whether structured logging is enabled.
        /// </summary>
        public bool EnableStructuredLogging => _enableStructuredLogging;

        /// <summary>
        /// Gets whether to include correlation ID.
        /// </summary>
        public bool IncludeCorrelationId => _includeCorrelationId;

        /// <summary>
        /// Gets whether to include source context.
        /// </summary>
        public bool IncludeSourceContext => _includeSourceContext;

        /// <summary>
        /// Gets whether to include thread ID.
        /// </summary>
        public bool IncludeThreadId => _includeThreadId;

        /// <summary>
        /// Creates a dictionary of properties for LogTargetConfig.
        /// </summary>
        /// <returns>Dictionary of configuration properties</returns>
        public Dictionary<string, object> ToProperties()
        {
            var properties = new Dictionary<string, object>
            {
                ["EnableConsole"] = _enableConsole,
                ["ConsoleTemplate"] = _consoleOutputTemplate,
                ["EnableFileLogging"] = _enableFileLogging,
                ["FilePath"] = _filePath,
                ["RollOnFileSizeLimit"] = _rollOnFileSizeLimit,
                ["FileSizeLimitBytes"] = FileSizeLimitBytes,
                ["RetainedFileCountLimit"] = _retainedFileCountLimit,
                ["Shared"] = _sharedFileAccess,
                ["UseJsonFormatter"] = _useJsonFormatter,
                ["EnableDebug"] = _enableDebug,
                ["DebugTemplate"] = _debugOutputTemplate,
                ["EnablePerformanceMetrics"] = _enablePerformanceMetrics,
                ["FrameBudgetThresholdMs"] = _frameBudgetThresholdMs,
                ["ErrorRateThreshold"] = _errorRateThreshold,
                ["HealthCheckIntervalSeconds"] = _healthCheckIntervalSeconds,
                ["MobileOptimized"] = _mobileOptimized,
                ["WebGLCompatible"] = _webGLCompatible,
                ["DesktopOptimized"] = _desktopOptimized,
                ["UseChannelWhitelist"] = _useChannelWhitelist,
                ["OutputTemplate"] = _outputTemplate,
                ["EnableStructuredLogging"] = _enableStructuredLogging,
                ["IncludeCorrelationId"] = _includeCorrelationId,
                ["IncludeSourceContext"] = _includeSourceContext,
                ["IncludeThreadId"] = _includeThreadId
            };

            return properties;
        }

        /// <summary>
        /// Creates a LogTargetConfig from this ScriptableObject.
        /// </summary>
        /// <returns>A configured LogTargetConfig</returns>
        public AhBearStudios.Core.Logging.Configs.LogTargetConfig ToLogTargetConfig()
        {
            return new AhBearStudios.Core.Logging.Configs.LogTargetConfig
            {
                Name = _targetName,
                TargetType = "Serilog",
                MinimumLevel = _minimumLevel,
                IsEnabled = _isEnabled,
                UseAsyncWrite = _useAsyncWrite,
                MaxConcurrentAsyncOperations = _maxConcurrentAsyncOperations,
                Channels = _channels,
                Properties = ToProperties()
            };
        }

        /// <summary>
        /// Validates the configuration and returns any errors.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_targetName))
            {
                errors.Add("Target name cannot be empty");
            }

            if (_enableFileLogging && string.IsNullOrWhiteSpace(_filePath))
            {
                errors.Add("File path is required when file logging is enabled");
            }

            if (_fileSizeLimitMB <= 0)
            {
                errors.Add("File size limit must be greater than zero");
            }

            if (_retainedFileCountLimit <= 0)
            {
                errors.Add("Retained file count limit must be greater than zero");
            }

            if (_maxConcurrentAsyncOperations <= 0)
            {
                errors.Add("Max concurrent async operations must be greater than zero");
            }

            if (_frameBudgetThresholdMs <= 0)
            {
                errors.Add("Frame budget threshold must be greater than zero");
            }

            if (_errorRateThreshold < 0 || _errorRateThreshold > 1)
            {
                errors.Add("Error rate threshold must be between 0 and 1");
            }

            if (_healthCheckIntervalSeconds <= 0)
            {
                errors.Add("Health check interval must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(_outputTemplate))
            {
                errors.Add("Output template cannot be empty");
            }

            return errors;
        }

        /// <summary>
        /// Resets the configuration to default values.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            _targetName = "Serilog";
            _minimumLevel = LogLevel.Debug;
            _isEnabled = true;
            _useAsyncWrite = true;
            _maxConcurrentAsyncOperations = 4;
            _enableConsole = true;
            _consoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            _enableFileLogging = true;
            _filePath = "Logs/serilog.log";
            _rollOnFileSizeLimit = true;
            _fileSizeLimitMB = 10;
            _retainedFileCountLimit = 5;
            _sharedFileAccess = false;
            _useJsonFormatter = true;
            _enableDebug = false;
            _debugOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            _enablePerformanceMetrics = true;
            _frameBudgetThresholdMs = 0.5f;
            _errorRateThreshold = 0.1f;
            _healthCheckIntervalSeconds = 60f;
            _mobileOptimized = false;
            _webGLCompatible = false;
            _desktopOptimized = true;
            _channels.Clear();
            _useChannelWhitelist = false;
            _outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
            _enableStructuredLogging = true;
            _includeCorrelationId = true;
            _includeSourceContext = true;
            _includeThreadId = true;
        }

        /// <summary>
        /// Validates the configuration in the Unity Editor.
        /// </summary>
        private void OnValidate()
        {
            // Clamp values to valid ranges
            _fileSizeLimitMB = Mathf.Max(1, _fileSizeLimitMB);
            _retainedFileCountLimit = Mathf.Max(1, _retainedFileCountLimit);
            _maxConcurrentAsyncOperations = Mathf.Max(1, _maxConcurrentAsyncOperations);
            _frameBudgetThresholdMs = Mathf.Max(0.1f, _frameBudgetThresholdMs);
            _errorRateThreshold = Mathf.Clamp01(_errorRateThreshold);
            _healthCheckIntervalSeconds = Mathf.Max(1f, _healthCheckIntervalSeconds);

            // Validate strings
            if (string.IsNullOrWhiteSpace(_targetName))
            {
                _targetName = "Serilog";
            }

            if (string.IsNullOrWhiteSpace(_filePath))
            {
                _filePath = "Logs/serilog.log";
            }

            if (string.IsNullOrWhiteSpace(_outputTemplate))
            {
                _outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
            }

            if (string.IsNullOrWhiteSpace(_consoleOutputTemplate))
            {
                _consoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            }

            if (string.IsNullOrWhiteSpace(_debugOutputTemplate))
            {
                _debugOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            }

            // Platform-specific optimizations
#if UNITY_ANDROID || UNITY_IOS
            _mobileOptimized = true;
            _fileSizeLimitMB = Mathf.Min(_fileSizeLimitMB, 5); // Limit to 5MB on mobile
#endif

#if UNITY_WEBGL
            _webGLCompatible = true;
            _enableFileLogging = false; // Disable file logging on WebGL
#endif

#if UNITY_STANDALONE
            _desktopOptimized = true;
#endif
        }
    }
}