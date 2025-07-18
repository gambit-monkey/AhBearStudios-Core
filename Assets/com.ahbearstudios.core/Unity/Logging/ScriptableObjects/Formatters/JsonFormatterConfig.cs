using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects.Formatters
{
    /// <summary>
    /// ScriptableObject configuration for JSON log formatter.
    /// Provides Unity-serializable configuration for JSON output formatting.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AhBearStudios/Core/Logging/Formatters/JSON Formatter", 
        fileName = "JsonFormatterConfig", 
        order = 1)]
    public class JsonFormatterConfig : LogFormatterScriptableObject
    {
        [Header("JSON Settings")]
        [SerializeField] private bool _prettyPrint = false;
        [SerializeField] private bool _includeMetadata = true;
        [SerializeField] private bool _includeProperties = true;
        [SerializeField] private bool _includeScope = true;
        [SerializeField] private string _indentString = "  ";
        [SerializeField] private int _maxDepth = 10;

        [Header("Field Naming")]
        [SerializeField] private bool _useCamelCase = true;
        [SerializeField] private bool _useShortFieldNames = false;
        [SerializeField] private string _timestampFieldName = "timestamp";
        [SerializeField] private string _levelFieldName = "level";
        [SerializeField] private string _messageFieldName = "message";
        [SerializeField] private string _channelFieldName = "channel";
        [SerializeField] private string _sourceContextFieldName = "source";
        [SerializeField] private string _correlationIdFieldName = "correlationId";
        [SerializeField] private string _threadIdFieldName = "threadId";
        [SerializeField] private string _exceptionFieldName = "exception";
        [SerializeField] private string _propertiesFieldName = "properties";

        [Header("Value Formatting")]
        [SerializeField] private bool _formatEnumsAsStrings = true;
        [SerializeField] private bool _formatDatesAsIso8601 = true;
        [SerializeField] private bool _includeNullValues = false;
        [SerializeField] private bool _escapeHtmlCharacters = true;
        [SerializeField] private bool _formatNumbersAsStrings = false;

        [Header("Exception Formatting")]
        [SerializeField] private bool _includeExceptionType = true;
        [SerializeField] private bool _includeExceptionMessage = true;
        [SerializeField] private bool _includeExceptionStackTrace = true;
        [SerializeField] private bool _includeInnerExceptions = true;
        [SerializeField] private bool _flattenExceptionData = false;

        [Header("Performance Settings")]
        [SerializeField] private bool _enableStringEscaping = true;
        [SerializeField] private bool _enableMinification = false;
        [SerializeField] private int _initialBufferSize = 1024;
        [SerializeField] private bool _reuseBuffers = true;

        /// <summary>
        /// Gets whether to use pretty printing.
        /// </summary>
        public bool PrettyPrint => _prettyPrint;

        /// <summary>
        /// Gets whether to include metadata.
        /// </summary>
        public bool IncludeMetadata => _includeMetadata;

        /// <summary>
        /// Gets whether to include properties.
        /// </summary>
        public bool IncludeProperties => _includeProperties;

        /// <summary>
        /// Gets whether to include scope information.
        /// </summary>
        public bool IncludeScope => _includeScope;

        /// <summary>
        /// Gets the indent string for pretty printing.
        /// </summary>
        public string IndentString => _indentString;

        /// <summary>
        /// Gets the maximum depth for nested objects.
        /// </summary>
        public int MaxDepth => _maxDepth;

        /// <summary>
        /// Gets whether to use camel case for field names.
        /// </summary>
        public bool UseCamelCase => _useCamelCase;

        /// <summary>
        /// Gets whether to use short field names.
        /// </summary>
        public bool UseShortFieldNames => _useShortFieldNames;

        /// <summary>
        /// Gets the timestamp field name.
        /// </summary>
        public string TimestampFieldName => _timestampFieldName;

        /// <summary>
        /// Gets the level field name.
        /// </summary>
        public string LevelFieldName => _levelFieldName;

        /// <summary>
        /// Gets the message field name.
        /// </summary>
        public string MessageFieldName => _messageFieldName;

        /// <summary>
        /// Gets the channel field name.
        /// </summary>
        public string ChannelFieldName => _channelFieldName;

        /// <summary>
        /// Gets the source context field name.
        /// </summary>
        public string SourceContextFieldName => _sourceContextFieldName;

        /// <summary>
        /// Gets the correlation ID field name.
        /// </summary>
        public string CorrelationIdFieldName => _correlationIdFieldName;

        /// <summary>
        /// Gets the thread ID field name.
        /// </summary>
        public string ThreadIdFieldName => _threadIdFieldName;

        /// <summary>
        /// Gets the exception field name.
        /// </summary>
        public string ExceptionFieldName => _exceptionFieldName;

        /// <summary>
        /// Gets the properties field name.
        /// </summary>
        public string PropertiesFieldName => _propertiesFieldName;

        /// <summary>
        /// Gets whether to format enums as strings.
        /// </summary>
        public bool FormatEnumsAsStrings => _formatEnumsAsStrings;

        /// <summary>
        /// Gets whether to format dates as ISO 8601.
        /// </summary>
        public bool FormatDatesAsIso8601 => _formatDatesAsIso8601;

        /// <summary>
        /// Gets whether to include null values.
        /// </summary>
        public bool IncludeNullValues => _includeNullValues;

        /// <summary>
        /// Gets whether to escape HTML characters.
        /// </summary>
        public bool EscapeHtmlCharacters => _escapeHtmlCharacters;

        /// <summary>
        /// Gets whether to format numbers as strings.
        /// </summary>
        public bool FormatNumbersAsStrings => _formatNumbersAsStrings;

        /// <summary>
        /// Gets whether to include exception type.
        /// </summary>
        public bool IncludeExceptionType => _includeExceptionType;

        /// <summary>
        /// Gets whether to include exception message.
        /// </summary>
        public bool IncludeExceptionMessage => _includeExceptionMessage;

        /// <summary>
        /// Gets whether to include exception stack trace.
        /// </summary>
        public bool IncludeExceptionStackTrace => _includeExceptionStackTrace;

        /// <summary>
        /// Gets whether to include inner exceptions.
        /// </summary>
        public bool IncludeInnerExceptions => _includeInnerExceptions;

        /// <summary>
        /// Gets whether to flatten exception data.
        /// </summary>
        public bool FlattenExceptionData => _flattenExceptionData;

        /// <summary>
        /// Gets whether string escaping is enabled.
        /// </summary>
        public bool EnableStringEscaping => _enableStringEscaping;

        /// <summary>
        /// Gets whether minification is enabled.
        /// </summary>
        public bool EnableMinification => _enableMinification;

        /// <summary>
        /// Gets the initial buffer size.
        /// </summary>
        public int InitialBufferSize => _initialBufferSize;

        /// <summary>
        /// Gets whether to reuse buffers.
        /// </summary>
        public bool ReuseBuffers => _reuseBuffers;

        /// <summary>
        /// Initializes default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _format = LogFormat.Json;
        }

        /// <summary>
        /// Creates JSON formatter specific properties.
        /// </summary>
        /// <returns>Dictionary of JSON formatter properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["PrettyPrint"] = _prettyPrint;
            properties["IncludeMetadata"] = _includeMetadata;
            properties["IncludeProperties"] = _includeProperties;
            properties["IncludeScope"] = _includeScope;
            properties["IndentString"] = _indentString;
            properties["MaxDepth"] = _maxDepth;
            properties["UseCamelCase"] = _useCamelCase;
            properties["UseShortFieldNames"] = _useShortFieldNames;
            properties["TimestampFieldName"] = _timestampFieldName;
            properties["LevelFieldName"] = _levelFieldName;
            properties["MessageFieldName"] = _messageFieldName;
            properties["ChannelFieldName"] = _channelFieldName;
            properties["SourceContextFieldName"] = _sourceContextFieldName;
            properties["CorrelationIdFieldName"] = _correlationIdFieldName;
            properties["ThreadIdFieldName"] = _threadIdFieldName;
            properties["ExceptionFieldName"] = _exceptionFieldName;
            properties["PropertiesFieldName"] = _propertiesFieldName;
            properties["FormatEnumsAsStrings"] = _formatEnumsAsStrings;
            properties["FormatDatesAsIso8601"] = _formatDatesAsIso8601;
            properties["IncludeNullValues"] = _includeNullValues;
            properties["EscapeHtmlCharacters"] = _escapeHtmlCharacters;
            properties["FormatNumbersAsStrings"] = _formatNumbersAsStrings;
            properties["IncludeExceptionType"] = _includeExceptionType;
            properties["IncludeExceptionMessage"] = _includeExceptionMessage;
            properties["IncludeExceptionStackTrace"] = _includeExceptionStackTrace;
            properties["IncludeInnerExceptions"] = _includeInnerExceptions;
            properties["FlattenExceptionData"] = _flattenExceptionData;
            properties["EnableStringEscaping"] = _enableStringEscaping;
            properties["EnableMinification"] = _enableMinification;
            properties["InitialBufferSize"] = _initialBufferSize;
            properties["ReuseBuffers"] = _reuseBuffers;
            
            return properties;
        }

        /// <summary>
        /// Validates JSON formatter specific configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (_maxDepth <= 0)
            {
                errors.Add("Max depth must be greater than zero");
            }

            if (_initialBufferSize <= 0)
            {
                errors.Add("Initial buffer size must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(_indentString))
            {
                errors.Add("Indent string cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(_timestampFieldName))
            {
                errors.Add("Timestamp field name cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(_levelFieldName))
            {
                errors.Add("Level field name cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(_messageFieldName))
            {
                errors.Add("Message field name cannot be empty");
            }

            return errors;
        }

        /// <summary>
        /// Resets to JSON formatter specific defaults.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _name = "JSON Formatter";
            _description = "JSON output formatter";
            _format = LogFormat.Json;
            _prettyPrint = false;
            _includeMetadata = true;
            _includeProperties = true;
            _includeScope = true;
            _indentString = "  ";
            _maxDepth = 10;
            _useCamelCase = true;
            _useShortFieldNames = false;
            _timestampFieldName = "timestamp";
            _levelFieldName = "level";
            _messageFieldName = "message";
            _channelFieldName = "channel";
            _sourceContextFieldName = "source";
            _correlationIdFieldName = "correlationId";
            _threadIdFieldName = "threadId";
            _exceptionFieldName = "exception";
            _propertiesFieldName = "properties";
            _formatEnumsAsStrings = true;
            _formatDatesAsIso8601 = true;
            _includeNullValues = false;
            _escapeHtmlCharacters = true;
            _formatNumbersAsStrings = false;
            _includeExceptionType = true;
            _includeExceptionMessage = true;
            _includeExceptionStackTrace = true;
            _includeInnerExceptions = true;
            _flattenExceptionData = false;
            _enableStringEscaping = true;
            _enableMinification = false;
            _initialBufferSize = 1024;
            _reuseBuffers = true;
        }

        /// <summary>
        /// Performs JSON formatter specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values
            _maxDepth = Mathf.Max(1, _maxDepth);
            _initialBufferSize = Mathf.Max(64, _initialBufferSize);

            // Validate strings
            if (string.IsNullOrWhiteSpace(_indentString))
            {
                _indentString = "  ";
            }

            if (string.IsNullOrWhiteSpace(_timestampFieldName))
            {
                _timestampFieldName = "timestamp";
            }

            if (string.IsNullOrWhiteSpace(_levelFieldName))
            {
                _levelFieldName = "level";
            }

            if (string.IsNullOrWhiteSpace(_messageFieldName))
            {
                _messageFieldName = "message";
            }

            if (string.IsNullOrWhiteSpace(_channelFieldName))
            {
                _channelFieldName = "channel";
            }

            if (string.IsNullOrWhiteSpace(_sourceContextFieldName))
            {
                _sourceContextFieldName = "source";
            }

            if (string.IsNullOrWhiteSpace(_correlationIdFieldName))
            {
                _correlationIdFieldName = "correlationId";
            }

            if (string.IsNullOrWhiteSpace(_threadIdFieldName))
            {
                _threadIdFieldName = "threadId";
            }

            if (string.IsNullOrWhiteSpace(_exceptionFieldName))
            {
                _exceptionFieldName = "exception";
            }

            if (string.IsNullOrWhiteSpace(_propertiesFieldName))
            {
                _propertiesFieldName = "properties";
            }

            // Ensure format is set to JSON
            _format = LogFormat.Json;
        }

        /// <summary>
        /// Creates a sample JSON output for preview.
        /// </summary>
        /// <returns>Sample JSON formatted message</returns>
        public override string CreateSampleOutput()
        {
            var indent = _prettyPrint ? _indentString : "";
            var newline = _prettyPrint ? "\n" : "";
            var space = _prettyPrint ? " " : "";
            
            var json = $"{{{newline}";
            json += $"{indent}\"{_timestampFieldName}\":{space}\"2023-12-01T10:30:45.123Z\",{newline}";
            json += $"{indent}\"{_levelFieldName}\":{space}\"INFO\",{newline}";
            json += $"{indent}\"{_messageFieldName}\":{space}\"This is a sample JSON log message\",{newline}";
            json += $"{indent}\"{_channelFieldName}\":{space}\"SampleChannel\",{newline}";
            json += $"{indent}\"{_sourceContextFieldName}\":{space}\"SampleClass\",{newline}";
            json += $"{indent}\"{_correlationIdFieldName}\":{space}\"12345678-1234-1234-1234-123456789012\"{newline}";
            
            if (_includeProperties)
            {
                json += $",{newline}{indent}\"{_propertiesFieldName}\":{space}{{{newline}";
                json += $"{indent}{indent}\"key1\":{space}\"value1\",{newline}";
                json += $"{indent}{indent}\"key2\":{space}\"value2\"{newline}";
                json += $"{indent}}}{newline}";
            }
            
            json += $"}}";
            
            return json;
        }

        /// <summary>
        /// Gets field name mappings for different naming conventions.
        /// </summary>
        /// <returns>Dictionary of field name mappings</returns>
        public Dictionary<string, string> GetFieldNameMappings()
        {
            var mappings = new Dictionary<string, string>();
            
            if (_useShortFieldNames)
            {
                mappings["timestamp"] = "ts";
                mappings["level"] = "lvl";
                mappings["message"] = "msg";
                mappings["channel"] = "ch";
                mappings["source"] = "src";
                mappings["correlationId"] = "cid";
                mappings["threadId"] = "tid";
                mappings["exception"] = "ex";
                mappings["properties"] = "props";
            }
            else
            {
                mappings["timestamp"] = _timestampFieldName;
                mappings["level"] = _levelFieldName;
                mappings["message"] = _messageFieldName;
                mappings["channel"] = _channelFieldName;
                mappings["source"] = _sourceContextFieldName;
                mappings["correlationId"] = _correlationIdFieldName;
                mappings["threadId"] = _threadIdFieldName;
                mappings["exception"] = _exceptionFieldName;
                mappings["properties"] = _propertiesFieldName;
            }
            
            return mappings;
        }

        /// <summary>
        /// Applies compact settings for production use.
        /// </summary>
        [ContextMenu("Apply Compact Settings")]
        public void ApplyCompactSettings()
        {
            _prettyPrint = false;
            _useShortFieldNames = true;
            _includeNullValues = false;
            _enableMinification = true;
            _includeMetadata = false;
            _includeScope = false;
            _includeExceptionStackTrace = false;
            _includeInnerExceptions = false;
            ValidateInEditor();
        }

        /// <summary>
        /// Applies verbose settings for debugging.
        /// </summary>
        [ContextMenu("Apply Verbose Settings")]
        public void ApplyVerboseSettings()
        {
            _prettyPrint = true;
            _useShortFieldNames = false;
            _includeNullValues = true;
            _enableMinification = false;
            _includeMetadata = true;
            _includeScope = true;
            _includeExceptionStackTrace = true;
            _includeInnerExceptions = true;
            ValidateInEditor();
        }
    }
}