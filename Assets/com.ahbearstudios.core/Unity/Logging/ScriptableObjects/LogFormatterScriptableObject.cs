using System;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Unity.Logging.ScriptableObjects
{
    /// <summary>
    /// Base ScriptableObject for log formatter configurations.
    /// Provides Unity-serializable configuration for log formatters.
    /// </summary>
    public abstract class LogFormatterScriptableObject : LoggingScriptableObjectBase
    {
        [Header("Formatter Settings")]
        [SerializeField] protected LogFormat _format = LogFormat.PlainText;
        [SerializeField] protected string _template = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}";
        [SerializeField] protected bool _includeTimestamp = true;
        [SerializeField] protected bool _includeLogLevel = true;
        [SerializeField] protected bool _includeChannel = true;
        [SerializeField] protected bool _includeSourceContext = true;
        [SerializeField] protected bool _includeCorrelationId = true;
        [SerializeField] protected bool _includeThreadId = false;
        [SerializeField] protected bool _includeException = true;
        [SerializeField] protected bool _includeStackTrace = false;

        [Header("Timestamp Settings")]
        [SerializeField] protected string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        [SerializeField] protected bool _useUtcTime = false;
        [SerializeField] protected string _timeZone = "";

        [Header("Performance Settings")]
        [SerializeField] protected bool _enableCaching = true;
        [SerializeField] protected int _maxCacheSize = 1000;
        [SerializeField] protected bool _enableStringPooling = true;
        [SerializeField] protected bool _enableBatchFormatting = true;

        [Header("Cultural Settings")]
        [SerializeField] protected string _culture = "en-US";
        [SerializeField] protected bool _useInvariantCulture = true;

        /// <summary>
        /// Gets the output format type.
        /// </summary>
        public LogFormat Format => _format;

        /// <summary>
        /// Gets the format template.
        /// </summary>
        public string Template => _template;

        /// <summary>
        /// Gets whether to include timestamp.
        /// </summary>
        public bool IncludeTimestamp => _includeTimestamp;

        /// <summary>
        /// Gets whether to include log level.
        /// </summary>
        public bool IncludeLogLevel => _includeLogLevel;

        /// <summary>
        /// Gets whether to include channel.
        /// </summary>
        public bool IncludeChannel => _includeChannel;

        /// <summary>
        /// Gets whether to include source context.
        /// </summary>
        public bool IncludeSourceContext => _includeSourceContext;

        /// <summary>
        /// Gets whether to include correlation ID.
        /// </summary>
        public bool IncludeCorrelationId => _includeCorrelationId;

        /// <summary>
        /// Gets whether to include thread ID.
        /// </summary>
        public bool IncludeThreadId => _includeThreadId;

        /// <summary>
        /// Gets whether to include exception details.
        /// </summary>
        public bool IncludeException => _includeException;

        /// <summary>
        /// Gets whether to include stack trace.
        /// </summary>
        public bool IncludeStackTrace => _includeStackTrace;

        /// <summary>
        /// Gets the timestamp format.
        /// </summary>
        public string TimestampFormat => _timestampFormat;

        /// <summary>
        /// Gets whether to use UTC time.
        /// </summary>
        public bool UseUtcTime => _useUtcTime;

        /// <summary>
        /// Gets the time zone.
        /// </summary>
        public string TimeZone => _timeZone;

        /// <summary>
        /// Gets whether caching is enabled.
        /// </summary>
        public bool EnableCaching => _enableCaching;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Gets whether string pooling is enabled.
        /// </summary>
        public bool EnableStringPooling => _enableStringPooling;

        /// <summary>
        /// Gets whether batch formatting is enabled.
        /// </summary>
        public bool EnableBatchFormatting => _enableBatchFormatting;

        /// <summary>
        /// Gets the culture setting.
        /// </summary>
        public string Culture => _culture;

        /// <summary>
        /// Gets whether to use invariant culture.
        /// </summary>
        public bool UseInvariantCulture => _useInvariantCulture;

        /// <summary>
        /// Creates formatter-specific properties dictionary.
        /// Override in derived classes to add specific properties.
        /// </summary>
        /// <returns>Dictionary of formatter-specific properties</returns>
        public override Dictionary<string, object> ToProperties()
        {
            var properties = base.ToProperties();
            
            properties["Format"] = _format.ToString();
            properties["Template"] = _template;
            properties["IncludeTimestamp"] = _includeTimestamp;
            properties["IncludeLogLevel"] = _includeLogLevel;
            properties["IncludeChannel"] = _includeChannel;
            properties["IncludeSourceContext"] = _includeSourceContext;
            properties["IncludeCorrelationId"] = _includeCorrelationId;
            properties["IncludeThreadId"] = _includeThreadId;
            properties["IncludeException"] = _includeException;
            properties["IncludeStackTrace"] = _includeStackTrace;
            properties["TimestampFormat"] = _timestampFormat;
            properties["UseUtcTime"] = _useUtcTime;
            properties["TimeZone"] = _timeZone;
            properties["EnableCaching"] = _enableCaching;
            properties["MaxCacheSize"] = _maxCacheSize;
            properties["EnableStringPooling"] = _enableStringPooling;
            properties["EnableBatchFormatting"] = _enableBatchFormatting;
            properties["Culture"] = _culture;
            properties["UseInvariantCulture"] = _useInvariantCulture;
            
            return properties;
        }

        /// <summary>
        /// Validates the formatter configuration.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public override List<string> ValidateConfiguration()
        {
            var errors = base.ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(_template))
            {
                errors.Add("Template cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(_timestampFormat))
            {
                errors.Add("Timestamp format cannot be empty");
            }

            if (_maxCacheSize <= 0)
            {
                errors.Add("Max cache size must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(_culture))
            {
                errors.Add("Culture cannot be empty");
            }

            // Validate timestamp format
            try
            {
                var testTime = DateTime.Now;
                testTime.ToString(_timestampFormat);
            }
            catch (FormatException)
            {
                errors.Add("Invalid timestamp format");
            }

            // Validate culture
            try
            {
                var cultureInfo = new System.Globalization.CultureInfo(_culture);
            }
            catch (System.Globalization.CultureNotFoundException)
            {
                errors.Add("Invalid culture specified");
            }

            return errors;
        }

        /// <summary>
        /// Resets the formatter configuration to default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            base.ResetToDefaults();
            
            _format = LogFormat.PlainText;
            _template = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}";
            _includeTimestamp = true;
            _includeLogLevel = true;
            _includeChannel = true;
            _includeSourceContext = true;
            _includeCorrelationId = true;
            _includeThreadId = false;
            _includeException = true;
            _includeStackTrace = false;
            _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            _useUtcTime = false;
            _timeZone = "";
            _enableCaching = true;
            _maxCacheSize = 1000;
            _enableStringPooling = true;
            _enableBatchFormatting = true;
            _culture = "en-US";
            _useInvariantCulture = true;
        }

        /// <summary>
        /// Performs formatter-specific validation.
        /// </summary>
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Clamp numeric values
            _maxCacheSize = Mathf.Max(1, _maxCacheSize);

            // Validate strings
            if (string.IsNullOrWhiteSpace(_template))
            {
                _template = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}";
            }

            if (string.IsNullOrWhiteSpace(_timestampFormat))
            {
                _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
            }

            if (string.IsNullOrWhiteSpace(_culture))
            {
                _culture = "en-US";
            }
        }

        /// <summary>
        /// Gets available template variables.
        /// </summary>
        /// <returns>List of available template variables</returns>
        public virtual List<string> GetAvailableTemplateVariables()
        {
            return new List<string>
            {
                "{Timestamp}",
                "{Level}",
                "{Message}",
                "{Channel}",
                "{SourceContext}",
                "{CorrelationId}",
                "{ThreadId}",
                "{Exception}",
                "{StackTrace}",
                "{NewLine}"
            };
        }

        /// <summary>
        /// Creates a sample formatted message for preview.
        /// </summary>
        /// <returns>Sample formatted message</returns>
        public virtual string CreateSampleOutput()
        {
            var sampleTemplate = _template;
            var timestamp = DateTime.Now.ToString(_timestampFormat);
            
            sampleTemplate = sampleTemplate.Replace("{Timestamp}", timestamp);
            sampleTemplate = sampleTemplate.Replace("{Level}", "INFO");
            sampleTemplate = sampleTemplate.Replace("{Message}", "This is a sample log message");
            sampleTemplate = sampleTemplate.Replace("{Channel}", "SampleChannel");
            sampleTemplate = sampleTemplate.Replace("{SourceContext}", "SampleClass");
            sampleTemplate = sampleTemplate.Replace("{CorrelationId}", "12345678-1234-1234-1234-123456789012");
            sampleTemplate = sampleTemplate.Replace("{ThreadId}", "1");
            sampleTemplate = sampleTemplate.Replace("{Exception}", "System.Exception: Sample exception");
            sampleTemplate = sampleTemplate.Replace("{StackTrace}", "   at SampleClass.SampleMethod()");
            sampleTemplate = sampleTemplate.Replace("{NewLine}", "\n");
            
            return sampleTemplate;
        }

        /// <summary>
        /// Updates the template with a new format.
        /// </summary>
        /// <param name="newTemplate">The new template format</param>
        public void UpdateTemplate(string newTemplate)
        {
            if (!string.IsNullOrWhiteSpace(newTemplate))
            {
                _template = newTemplate;
                ValidateInEditor();
            }
        }

        /// <summary>
        /// Gets predefined template formats.
        /// </summary>
        /// <returns>Dictionary of predefined templates</returns>
        public virtual Dictionary<string, string> GetPredefinedTemplates()
        {
            return new Dictionary<string, string>
            {
                ["Simple"] = "{Level}: {Message}",
                ["Standard"] = "[{Timestamp:HH:mm:ss}] [{Level}] {Message}",
                ["Detailed"] = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}",
                ["Full"] = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] [{SourceContext}] [{CorrelationId}] {Message}",
                ["Compact"] = "{Timestamp:HH:mm:ss} {Level:u3} {Message}",
                ["Development"] = "[{Timestamp:HH:mm:ss.fff}] [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}",
                ["Production"] = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{CorrelationId}] {Message}"
            };
        }

        /// <summary>
        /// Applies a predefined template.
        /// </summary>
        /// <param name="templateName">Name of the predefined template</param>
        public void ApplyPredefinedTemplate(string templateName)
        {
            var templates = GetPredefinedTemplates();
            if (templates.TryGetValue(templateName, out var template))
            {
                UpdateTemplate(template);
            }
        }
    }
}