using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Targets
{
    /// <summary>
    /// ScriptableObject configuration for Unity Console log target.
    /// Provides Unity-serializable configuration for Unity Debug.Log integration.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Targets/Unity Console Target", 
        fileName = "UnityConsoleTargetConfig", 
        order = 1)]
    public class UnityConsoleTargetConfig : LogTargetScriptableObject
    {
        [Header("Unity Console Settings")]
        [SerializeField] private bool _useColors = true;
        [SerializeField] private bool _showStackTraces = false;
        [SerializeField] private bool _includeTimestamp = true;
        [SerializeField] private bool _includeThreadId = false;
        [SerializeField] private bool _collapseIdenticalLogs = false;
        [SerializeField] private bool _groupByContext = false;

        [Header("Color Configuration")]
        [SerializeField] private Color _debugColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _infoColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color _criticalColor = Color.red;

        [Header("Message Formatting")]
        [SerializeField] private string _messageFormat = "[{Level}] {Message}";
        [SerializeField] private LogLevel _stackTraceLogLevel = LogLevel.Error;
        [SerializeField] private bool _includeExceptionDetails = true;

        [Header("Unity Editor Integration")]
        [SerializeField] private bool _enableEditorIntegration = true;
        [SerializeField] private bool _enablePlayModeIntegration = true;
        [SerializeField] private bool _enableBuildIntegration = false;

        /// <summary>
        /// Gets whether colors are enabled.
        /// </summary>
        public bool UseColors => _useColors;

        /// <summary>
        /// Gets whether stack traces are shown.
        /// </summary>
        public bool ShowStackTraces => _showStackTraces;

        /// <summary>
        /// Gets whether timestamps are included.
        /// </summary>
        public bool IncludeTimestamp => _includeTimestamp;

        /// <summary>
        /// Gets whether thread IDs are included.
        /// </summary>
        public bool IncludeThreadId => _includeThreadId;

        /// <summary>
        /// Gets whether identical logs should be collapsed.
        /// </summary>
        public bool CollapseIdenticalLogs => _collapseIdenticalLogs;

        /// <summary>
        /// Gets whether logs should be grouped by context.
        /// </summary>
        public bool GroupByContext => _groupByContext;

        /// <summary>
        /// Gets the color for debug messages.
        /// </summary>
        public Color DebugColor => _debugColor;

        /// <summary>
        /// Gets the color for info messages.
        /// </summary>
        public Color InfoColor => _infoColor;

        /// <summary>
        /// Gets the color for warning messages.
        /// </summary>
        public Color WarningColor => _warningColor;

        /// <summary>
        /// Gets the color for error messages.
        /// </summary>
        public Color ErrorColor => _errorColor;

        /// <summary>
        /// Gets the color for critical messages.
        /// </summary>
        public Color CriticalColor => _criticalColor;

        /// <summary>
        /// Gets the message format template.
        /// </summary>
        public string MessageFormat => _messageFormat;

        /// <summary>
        /// Gets the minimum log level for stack traces.
        /// </summary>
        public LogLevel StackTraceLogLevel => _stackTraceLogLevel;

        /// <summary>
        /// Gets whether exception details should be included.
        /// </summary>
        public bool IncludeExceptionDetails => _includeExceptionDetails;

        /// <summary>
        /// Gets whether Unity Editor integration is enabled.
        /// </summary>
        public bool EnableEditorIntegration => _enableEditorIntegration;

        /// <summary>
        /// Gets whether Play Mode integration is enabled.
        /// </summary>
        public bool EnablePlayModeIntegration => _enablePlayModeIntegration;

        /// <summary>
        /// Gets whether Build integration is enabled.
        /// </summary>
        public bool EnableBuildIntegration => _enableBuildIntegration;

        /// <summary>
        /// Initializes default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _targetType = "UnityConsole";
            _useAsyncWrite = false; // Unity Console doesn't need async write
            _bufferSize = 1; // No buffering needed for Unity Console
        }

        /// <summary>
        /// Creates Unity Console specific properties.
        /// </summary>
        /// <returns>Dictionary of Unity Console properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["UseColors"] = _useColors;
            properties["ShowStackTraces"] = _showStackTraces;
            properties["IncludeTimestamp"] = _includeTimestamp;
            properties["IncludeThreadId"] = _includeThreadId;
            properties["CollapseIdenticalLogs"] = _collapseIdenticalLogs;
            properties["GroupByContext"] = _groupByContext;
            properties["MessageFormat"] = _messageFormat;
            properties["StackTraceLogLevel"] = _stackTraceLogLevel.ToString();
            properties["IncludeExceptionDetails"] = _includeExceptionDetails;
            properties["EnableEditorIntegration"] = _enableEditorIntegration;
            properties["EnablePlayModeIntegration"] = _enablePlayModeIntegration;
            properties["EnableBuildIntegration"] = _enableBuildIntegration;
            
            // Color properties
            properties["DebugColor"] = ColorUtility.ToHtmlStringRGB(_debugColor);
            properties["InfoColor"] = ColorUtility.ToHtmlStringRGB(_infoColor);
            properties["WarningColor"] = ColorUtility.ToHtmlStringRGB(_warningColor);
            properties["ErrorColor"] = ColorUtility.ToHtmlStringRGB(_errorColor);
            properties["CriticalColor"] = ColorUtility.ToHtmlStringRGB(_criticalColor);
            
            return properties;
        }

        /// <summary>
        /// Validates Unity Console specific configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(_messageFormat))
            {
                errors.Add("Message format cannot be empty");
            }

            if (!_enableEditorIntegration && !_enablePlayModeIntegration && !_enableBuildIntegration)
            {
                errors.Add("At least one integration mode must be enabled");
            }

            return errors;
        }

        /// <summary>
        /// Resets to Unity Console specific defaults.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "Unity Console";
            _description = "Unity Debug.Log console output target";
            _targetType = "UnityConsole";
            _useAsyncWrite = false;
            _bufferSize = 1;
            _useColors = true;
            _showStackTraces = false;
            _includeTimestamp = true;
            _includeThreadId = false;
            _collapseIdenticalLogs = false;
            _groupByContext = false;
            _debugColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            _infoColor = Color.white;
            _warningColor = Color.yellow;
            _errorColor = new Color(1f, 0.5f, 0.5f, 1f);
            _criticalColor = Color.red;
            _messageFormat = "[{Level}] {Message}";
            _stackTraceLogLevel = LogLevel.Error;
            _includeExceptionDetails = true;
            _enableEditorIntegration = true;
            _enablePlayModeIntegration = true;
            _enableBuildIntegration = false;
        }

        /// <summary>
        /// Performs Unity Console specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Ensure message format is not empty
            if (string.IsNullOrWhiteSpace(_messageFormat))
            {
                _messageFormat = "[{Level}] {Message}";
            }

            // Unity Console doesn't need async write or large buffers
            _useAsyncWrite = false;
            _bufferSize = 1;

            // Ensure at least one integration mode is enabled
            if (!_enableEditorIntegration && !_enablePlayModeIntegration && !_enableBuildIntegration)
            {
                _enableEditorIntegration = true;
                _enablePlayModeIntegration = true;
            }
        }

        /// <summary>
        /// Gets the color for a specific log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>The color for the log level</returns>
        public Color GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => _debugColor,
                LogLevel.Info => _infoColor,
                LogLevel.Warning => _warningColor,
                LogLevel.Error => _errorColor,
                LogLevel.Critical => _criticalColor,
                _ => _infoColor
            };
        }

        /// <summary>
        /// Sets the color for a specific log level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="color">The color to set</param>
        public void SetColorForLevel(LogLevel level, Color color)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    _debugColor = color;
                    break;
                case LogLevel.Info:
                    _infoColor = color;
                    break;
                case LogLevel.Warning:
                    _warningColor = color;
                    break;
                case LogLevel.Error:
                    _errorColor = color;
                    break;
                case LogLevel.Critical:
                    _criticalColor = color;
                    break;
            }
        }
    }
}