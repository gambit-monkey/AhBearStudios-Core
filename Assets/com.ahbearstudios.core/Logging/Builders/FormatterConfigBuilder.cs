using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Configs;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Builders
{
    /// <summary>
    /// Builder for creating FormatterConfig instances with fluent API and validation.
    /// Follows the established AhBearStudios Core builder pattern for complex configuration objects.
    /// </summary>
    public sealed class FormatterConfigBuilder
    {
        private string _name;
        private bool _isEnabled = true;
        private string _formatterType = "Generic";
        private LogFormat _format = LogFormat.PlainText;
        private string _messageTemplate;
        private string _timestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private bool _includeTimestamp = true;
        private bool _includeLogLevel = true;
        private bool _includeChannel = true;
        private bool _includeSourceContext = false;
        private bool _includeSource = false;
        private bool _includeCorrelationId = false;
        private bool _includeThreadInfo = false;
        private bool _includeMachineInfo = false;
        private bool _includeExceptionDetails = true;
        private bool _includeStackTrace = true;
        private bool _includeScopes = false;
        private bool _includeProperties = false;
        private bool _includePerformanceMetrics = false;
        private bool _useUtcTimestamps = true;
        private bool _singleLine = false;
        private bool _compactOutput = false;
        private bool _prettyPrint = false;
        private string _indentation = "  ";
        private string _fieldSeparator = ",";
        private string _lineSeparator;
        private string _nullValue = "null";
        private int _maxMessageLength = 0;
        private int _maxExceptionLength = 0;
        private int _maxPropertyCount = 0;
        private int _maxStackTraceLines = 0;
        private char _escapeCharacter = '\\';
        private char _quoteCharacter = '"';
        private readonly Dictionary<string, string> _fieldMappings = new();
        private readonly Dictionary<string, string> _propertyTransformations = new();
        private readonly List<string> _excludedProperties = new();
        private readonly List<string> _includedProperties = new();
        private string _propertyNameFormat = "Original";
        private string _encoding = "UTF-8";
        private string _culture = "en-US";
        private readonly Dictionary<string, object> _properties = new();
        private readonly List<string> _tags = new();
        private string _description = string.Empty;

        /// <summary>
        /// Private constructor to enforce factory pattern.
        /// </summary>
        private FormatterConfigBuilder(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _lineSeparator = Environment.NewLine;
        }

        /// <summary>
        /// Creates a new FormatterConfigBuilder instance.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <returns>A new FormatterConfigBuilder instance</returns>
        public static FormatterConfigBuilder Create(string name)
        {
            return new FormatterConfigBuilder(name);
        }

        /// <summary>
        /// Sets whether the formatter is enabled.
        /// </summary>
        /// <param name="isEnabled">Whether the formatter is enabled</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithEnabled(bool isEnabled)
        {
            _isEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Sets the formatter type.
        /// </summary>
        /// <param name="formatterType">The formatter type identifier</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithFormatterType(string formatterType)
        {
            _formatterType = formatterType ?? throw new ArgumentNullException(nameof(formatterType));
            return this;
        }

        /// <summary>
        /// Sets the output format.
        /// </summary>
        /// <param name="format">The output format</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithFormat(LogFormat format)
        {
            _format = format;
            _messageTemplate = GetDefaultTemplate(format);
            return this;
        }

        /// <summary>
        /// Sets the message template.
        /// </summary>
        /// <param name="messageTemplate">The message template</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithMessageTemplate(string messageTemplate)
        {
            _messageTemplate = messageTemplate ?? throw new ArgumentNullException(nameof(messageTemplate));
            return this;
        }

        /// <summary>
        /// Sets the timestamp format.
        /// </summary>
        /// <param name="timestampFormat">The timestamp format string</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithTimestampFormat(string timestampFormat)
        {
            _timestampFormat = timestampFormat ?? throw new ArgumentNullException(nameof(timestampFormat));
            return this;
        }

        /// <summary>
        /// Sets whether to include timestamp in output.
        /// </summary>
        /// <param name="includeTimestamp">Whether to include timestamp</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeTimestamp(bool includeTimestamp = true)
        {
            _includeTimestamp = includeTimestamp;
            return this;
        }

        /// <summary>
        /// Sets whether to include log level in output.
        /// </summary>
        /// <param name="includeLogLevel">Whether to include log level</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeLogLevel(bool includeLogLevel = true)
        {
            _includeLogLevel = includeLogLevel;
            return this;
        }

        /// <summary>
        /// Sets whether to include channel in output.
        /// </summary>
        /// <param name="includeChannel">Whether to include channel</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeChannel(bool includeChannel = true)
        {
            _includeChannel = includeChannel;
            return this;
        }

        /// <summary>
        /// Sets whether to include source context in output.
        /// </summary>
        /// <param name="includeSourceContext">Whether to include source context</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeSourceContext(bool includeSourceContext = true)
        {
            _includeSourceContext = includeSourceContext;
            return this;
        }

        /// <summary>
        /// Sets whether to include source system in output.
        /// </summary>
        /// <param name="includeSource">Whether to include source system</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeSource(bool includeSource = true)
        {
            _includeSource = includeSource;
            return this;
        }

        /// <summary>
        /// Sets whether to include correlation ID in output.
        /// </summary>
        /// <param name="includeCorrelationId">Whether to include correlation ID</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeCorrelationId(bool includeCorrelationId = true)
        {
            _includeCorrelationId = includeCorrelationId;
            return this;
        }

        /// <summary>
        /// Sets whether to include thread information in output.
        /// </summary>
        /// <param name="includeThreadInfo">Whether to include thread info</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeThreadInfo(bool includeThreadInfo = true)
        {
            _includeThreadInfo = includeThreadInfo;
            return this;
        }

        /// <summary>
        /// Sets whether to include machine information in output.
        /// </summary>
        /// <param name="includeMachineInfo">Whether to include machine info</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeMachineInfo(bool includeMachineInfo = true)
        {
            _includeMachineInfo = includeMachineInfo;
            return this;
        }

        /// <summary>
        /// Sets whether to include exception details in output.
        /// </summary>
        /// <param name="includeExceptionDetails">Whether to include exception details</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeExceptionDetails(bool includeExceptionDetails = true)
        {
            _includeExceptionDetails = includeExceptionDetails;
            return this;
        }

        /// <summary>
        /// Sets whether to include stack traces in output.
        /// </summary>
        /// <param name="includeStackTrace">Whether to include stack traces</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeStackTrace(bool includeStackTrace = true)
        {
            _includeStackTrace = includeStackTrace;
            return this;
        }

        /// <summary>
        /// Sets whether to include scope information in output.
        /// </summary>
        /// <param name="includeScopes">Whether to include scopes</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeScopes(bool includeScopes = true)
        {
            _includeScopes = includeScopes;
            return this;
        }

        /// <summary>
        /// Sets whether to include structured properties in output.
        /// </summary>
        /// <param name="includeProperties">Whether to include properties</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeProperties(bool includeProperties = true)
        {
            _includeProperties = includeProperties;
            return this;
        }

        /// <summary>
        /// Sets whether to include performance metrics in output.
        /// </summary>
        /// <param name="includePerformanceMetrics">Whether to include performance metrics</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludePerformanceMetrics(bool includePerformanceMetrics = true)
        {
            _includePerformanceMetrics = includePerformanceMetrics;
            return this;
        }

        /// <summary>
        /// Sets whether to use UTC timestamps.
        /// </summary>
        /// <param name="useUtcTimestamps">Whether to use UTC timestamps</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithUtcTimestamps(bool useUtcTimestamps = true)
        {
            _useUtcTimestamps = useUtcTimestamps;
            return this;
        }

        /// <summary>
        /// Sets whether to use single-line output.
        /// </summary>
        /// <param name="singleLine">Whether to use single-line output</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithSingleLine(bool singleLine = true)
        {
            _singleLine = singleLine;
            return this;
        }

        /// <summary>
        /// Sets whether to use compact output.
        /// </summary>
        /// <param name="compactOutput">Whether to use compact output</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithCompactOutput(bool compactOutput = true)
        {
            _compactOutput = compactOutput;
            return this;
        }

        /// <summary>
        /// Sets whether to use pretty-printing.
        /// </summary>
        /// <param name="prettyPrint">Whether to use pretty-printing</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithPrettyPrint(bool prettyPrint = true)
        {
            _prettyPrint = prettyPrint;
            _compactOutput = !prettyPrint; // Auto-adjust compact output
            return this;
        }

        /// <summary>
        /// Sets the indentation string for structured output.
        /// </summary>
        /// <param name="indentation">The indentation string</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithIndentation(string indentation)
        {
            _indentation = indentation ?? throw new ArgumentNullException(nameof(indentation));
            return this;
        }

        /// <summary>
        /// Sets the field separator for delimited formats.
        /// </summary>
        /// <param name="fieldSeparator">The field separator</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithFieldSeparator(string fieldSeparator)
        {
            _fieldSeparator = fieldSeparator ?? throw new ArgumentNullException(nameof(fieldSeparator));
            return this;
        }

        /// <summary>
        /// Sets the line separator for multi-line output.
        /// </summary>
        /// <param name="lineSeparator">The line separator</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithLineSeparator(string lineSeparator)
        {
            _lineSeparator = lineSeparator ?? throw new ArgumentNullException(nameof(lineSeparator));
            return this;
        }

        /// <summary>
        /// Sets the null value representation.
        /// </summary>
        /// <param name="nullValue">The null value representation</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithNullValue(string nullValue)
        {
            _nullValue = nullValue ?? throw new ArgumentNullException(nameof(nullValue));
            return this;
        }

        /// <summary>
        /// Sets the maximum message length.
        /// </summary>
        /// <param name="maxMessageLength">The maximum message length (0 = no limit)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithMaxMessageLength(int maxMessageLength)
        {
            _maxMessageLength = Math.Max(0, maxMessageLength);
            return this;
        }

        /// <summary>
        /// Sets the maximum exception length.
        /// </summary>
        /// <param name="maxExceptionLength">The maximum exception length (0 = no limit)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithMaxExceptionLength(int maxExceptionLength)
        {
            _maxExceptionLength = Math.Max(0, maxExceptionLength);
            return this;
        }

        /// <summary>
        /// Sets the maximum property count.
        /// </summary>
        /// <param name="maxPropertyCount">The maximum property count (0 = no limit)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithMaxPropertyCount(int maxPropertyCount)
        {
            _maxPropertyCount = Math.Max(0, maxPropertyCount);
            return this;
        }

        /// <summary>
        /// Sets the maximum stack trace lines.
        /// </summary>
        /// <param name="maxStackTraceLines">The maximum stack trace lines (0 = no limit)</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithMaxStackTraceLines(int maxStackTraceLines)
        {
            _maxStackTraceLines = Math.Max(0, maxStackTraceLines);
            return this;
        }

        /// <summary>
        /// Sets the escape character.
        /// </summary>
        /// <param name="escapeCharacter">The escape character</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithEscapeCharacter(char escapeCharacter)
        {
            _escapeCharacter = escapeCharacter;
            return this;
        }

        /// <summary>
        /// Sets the quote character.
        /// </summary>
        /// <param name="quoteCharacter">The quote character</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithQuoteCharacter(char quoteCharacter)
        {
            _quoteCharacter = quoteCharacter;
            return this;
        }

        /// <summary>
        /// Adds field mappings for structured output.
        /// </summary>
        /// <param name="fieldMappings">The field mappings to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithFieldMappings(IReadOnlyDictionary<string, string> fieldMappings)
        {
            if (fieldMappings != null)
            {
                foreach (var kvp in fieldMappings)
                {
                    _fieldMappings[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a single field mapping.
        /// </summary>
        /// <param name="originalName">The original field name</param>
        /// <param name="mappedName">The mapped field name</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithFieldMapping(string originalName, string mappedName)
        {
            if (!string.IsNullOrEmpty(originalName) && !string.IsNullOrEmpty(mappedName))
            {
                _fieldMappings[originalName] = mappedName;
            }
            return this;
        }

        /// <summary>
        /// Adds property transformations.
        /// </summary>
        /// <param name="propertyTransformations">The property transformations to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithPropertyTransformations(IReadOnlyDictionary<string, string> propertyTransformations)
        {
            if (propertyTransformations != null)
            {
                foreach (var kvp in propertyTransformations)
                {
                    _propertyTransformations[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Excludes properties from output.
        /// </summary>
        /// <param name="excludedProperties">The properties to exclude</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithExcludedProperties(params string[] excludedProperties)
        {
            if (excludedProperties != null)
            {
                _excludedProperties.AddRange(excludedProperties.Where(p => !string.IsNullOrEmpty(p)));
            }
            return this;
        }

        /// <summary>
        /// Includes specific properties in output.
        /// </summary>
        /// <param name="includedProperties">The properties to include</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithIncludedProperties(params string[] includedProperties)
        {
            if (includedProperties != null)
            {
                _includedProperties.AddRange(includedProperties.Where(p => !string.IsNullOrEmpty(p)));
            }
            return this;
        }

        /// <summary>
        /// Sets the property name format.
        /// </summary>
        /// <param name="propertyNameFormat">The property name format</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithPropertyNameFormat(string propertyNameFormat)
        {
            _propertyNameFormat = propertyNameFormat ?? throw new ArgumentNullException(nameof(propertyNameFormat));
            return this;
        }

        /// <summary>
        /// Sets the encoding.
        /// </summary>
        /// <param name="encoding">The encoding name</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithEncoding(string encoding)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            return this;
        }

        /// <summary>
        /// Sets the culture.
        /// </summary>
        /// <param name="culture">The culture name</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithCulture(string culture)
        {
            _culture = culture ?? throw new ArgumentNullException(nameof(culture));
            return this;
        }

        /// <summary>
        /// Adds properties to the formatter configuration.
        /// </summary>
        /// <param name="properties">The properties to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithProperties(IReadOnlyDictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a single property to the formatter configuration.
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithProperty(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _properties[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Adds tags to the formatter.
        /// </summary>
        /// <param name="tags">The tags to add</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithTags(params string[] tags)
        {
            if (tags != null)
            {
                _tags.AddRange(tags.Where(t => !string.IsNullOrEmpty(t)));
            }
            return this;
        }

        /// <summary>
        /// Sets the formatter description.
        /// </summary>
        /// <param name="description">The formatter description</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder WithDescription(string description)
        {
            _description = description ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Configures the formatter for plain text output.
        /// </summary>
        /// <param name="includeTimestamp">Whether to include timestamp</param>
        /// <param name="includeLogLevel">Whether to include log level</param>
        /// <param name="includeChannel">Whether to include channel</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder ForPlainText(bool includeTimestamp = true, bool includeLogLevel = true, bool includeChannel = true)
        {
            return WithFormatterType("PlainText")
                .WithFormat(LogFormat.PlainText)
                .WithMessageTemplate("[{Timestamp}] [{Level}] [{Channel}] {Message}")
                .IncludeTimestamp(includeTimestamp)
                .IncludeLogLevel(includeLogLevel)
                .IncludeChannel(includeChannel)
                .WithSingleLine(true)
                .WithDescription("Plain text formatter with customizable fields");
        }

        /// <summary>
        /// Configures the formatter for JSON output.
        /// </summary>
        /// <param name="prettyPrint">Whether to use pretty-printing</param>
        /// <param name="includeAllFields">Whether to include all available fields</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder ForJson(bool prettyPrint = false, bool includeAllFields = false)
        {
            return WithFormatterType("Json")
                .WithFormat(LogFormat.Json)
                .IncludeTimestamp(true)
                .IncludeLogLevel(true)
                .IncludeChannel(true)
                .IncludeSourceContext(includeAllFields)
                .IncludeSource(includeAllFields)
                .IncludeCorrelationId(includeAllFields)
                .IncludeThreadInfo(includeAllFields)
                .IncludeMachineInfo(includeAllFields)
                .IncludeProperties(includeAllFields)
                .IncludeScopes(includeAllFields)
                .WithPrettyPrint(prettyPrint)
                .WithDescription("JSON formatter with structured output");
        }

        /// <summary>
        /// Configures the formatter for CSV output.
        /// </summary>
        /// <param name="delimiter">The field delimiter</param>
        /// <param name="includeHeaders">Whether to include headers</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder ForCsv(string delimiter = ",", bool includeHeaders = true)
        {
            return WithFormatterType("Csv")
                .WithFormat(LogFormat.Csv)
                .WithFieldSeparator(delimiter)
                .IncludeTimestamp(true)
                .IncludeLogLevel(true)
                .IncludeChannel(true)
                .WithSingleLine(true)
                .WithProperty("IncludeHeaders", includeHeaders)
                .WithDescription($"CSV formatter with '{delimiter}' delimiter");
        }

        /// <summary>
        /// Configures the formatter for Unity-optimized output.
        /// </summary>
        /// <param name="includePerformanceMetrics">Whether to include performance metrics</param>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder ForUnity(bool includePerformanceMetrics = true)
        {
            return WithFormatterType("Unity")
                .WithFormat(LogFormat.PlainText)
                .WithMessageTemplate("[{Timestamp:HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}")
                .WithTimestampFormat("HH:mm:ss.fff")
                .IncludeTimestamp(true)
                .IncludeLogLevel(true)
                .IncludeChannel(true)
                .IncludeThreadInfo(true)
                .IncludePerformanceMetrics(includePerformanceMetrics)
                .WithSingleLine(true)
                .WithCompactOutput(true)
                .WithDescription("Unity-optimized formatter for game development");
        }

        /// <summary>
        /// Configures the formatter for mobile-optimized output.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder ForMobile()
        {
            return WithFormatterType("Mobile")
                .WithFormat(LogFormat.Json)
                .IncludeTimestamp(true)
                .IncludeLogLevel(true)
                .IncludeChannel(false)
                .IncludeSourceContext(false)
                .IncludeSource(false)
                .IncludeCorrelationId(false)
                .IncludeThreadInfo(false)
                .IncludeMachineInfo(false)
                .IncludeStackTrace(false)
                .IncludeScopes(false)
                .IncludeProperties(false)
                .IncludePerformanceMetrics(false)
                .WithCompactOutput(true)
                .WithMaxMessageLength(500)
                .WithMaxExceptionLength(1000)
                .WithDescription("Mobile-optimized formatter with minimal overhead");
        }

        /// <summary>
        /// Includes all available fields in the output.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeAllFields()
        {
            return IncludeTimestamp(true)
                .IncludeLogLevel(true)
                .IncludeChannel(true)
                .IncludeSourceContext(true)
                .IncludeSource(true)
                .IncludeCorrelationId(true)
                .IncludeThreadInfo(true)
                .IncludeMachineInfo(true)
                .IncludeExceptionDetails(true)
                .IncludeStackTrace(true)
                .IncludeScopes(true)
                .IncludeProperties(true)
                .IncludePerformanceMetrics(true);
        }

        /// <summary>
        /// Includes only essential fields in the output.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public FormatterConfigBuilder IncludeEssentialFields()
        {
            return IncludeTimestamp(true)
                .IncludeLogLevel(true)
                .IncludeChannel(true)
                .IncludeSourceContext(false)
                .IncludeSource(false)
                .IncludeCorrelationId(false)
                .IncludeThreadInfo(false)
                .IncludeMachineInfo(false)
                .IncludeExceptionDetails(true)
                .IncludeStackTrace(false)
                .IncludeScopes(false)
                .IncludeProperties(false)
                .IncludePerformanceMetrics(false);
        }

        /// <summary>
        /// Gets the default message template for the specified format.
        /// </summary>
        /// <param name="format">The log format</param>
        /// <returns>The default template</returns>
        private static string GetDefaultTemplate(LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => "[{Timestamp}] [{Level}] [{Channel}] {Message}",
                LogFormat.Json => "{}",
                LogFormat.Xml => "<LogEntry />",
                LogFormat.Csv => "{Timestamp},{Level},{Channel},{Message}",
                LogFormat.Syslog => "<{Priority}>{Timestamp} {MachineName} {Source}: {Message}",
                LogFormat.Cef => "CEF:0|{Vendor}|{Product}|{Version}|{SignatureId}|{Name}|{Severity}|{Extensions}",
                LogFormat.KeyValue => "timestamp={Timestamp} level={Level} channel={Channel} message={Message}",
                LogFormat.Gelf => "{}",
                _ => "[{Timestamp}] [{Level}] [{Channel}] {Message}"
            };
        }

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>A validation result</returns>
        public ValidationResult Validate()
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrWhiteSpace(_name))
                errors.Add(new ValidationError("Formatter name cannot be empty", nameof(_name)));

            if (string.IsNullOrWhiteSpace(_messageTemplate))
                errors.Add(new ValidationError("Message template cannot be empty", nameof(_messageTemplate)));

            if (_maxMessageLength < 0)
                errors.Add(new ValidationError("Maximum message length cannot be negative", nameof(_maxMessageLength)));

            if (_maxExceptionLength < 0)
                errors.Add(new ValidationError("Maximum exception length cannot be negative", nameof(_maxExceptionLength)));

            if (_maxPropertyCount < 0)
                errors.Add(new ValidationError("Maximum property count cannot be negative", nameof(_maxPropertyCount)));

            if (_maxStackTraceLines < 0)
                errors.Add(new ValidationError("Maximum stack trace lines cannot be negative", nameof(_maxStackTraceLines)));

            if (string.IsNullOrEmpty(_timestampFormat))
                warnings.Add(new ValidationWarning("Empty timestamp format may cause formatting issues", nameof(_timestampFormat)));

            if (_maxMessageLength > 0 && _maxMessageLength < 100)
                warnings.Add(new ValidationWarning("Very short message length may truncate important information", nameof(_maxMessageLength)));

            if (_compactOutput && _prettyPrint)
                warnings.Add(new ValidationWarning("Compact output and pretty print are conflicting settings", nameof(_compactOutput)));

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, _name, warnings)
                : ValidationResult.Success(_name, warnings);
        }

        /// <summary>
        /// Builds the FormatterConfig instance.
        /// </summary>
        /// <returns>A configured FormatterConfig instance</returns>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        public FormatterConfig Build()
        {
            var validationResult = Validate();
            validationResult.ThrowIfInvalid();

            return new FormatterConfig(
                name: _name,
                isEnabled: _isEnabled,
                formatterType: _formatterType,
                format: _format,
                messageTemplate: _messageTemplate,
                timestampFormat: _timestampFormat,
                includeTimestamp: _includeTimestamp,
                includeLogLevel: _includeLogLevel,
                includeChannel: _includeChannel,
                includeSourceContext: _includeSourceContext,
                includeSource: _includeSource,
                includeCorrelationId: _includeCorrelationId,
                includeThreadInfo: _includeThreadInfo,
                includeMachineInfo: _includeMachineInfo,
                includeExceptionDetails: _includeExceptionDetails,
                includeStackTrace: _includeStackTrace,
                includeScopes: _includeScopes,
                includeProperties: _includeProperties,
                includePerformanceMetrics: _includePerformanceMetrics,
                useUtcTimestamps: _useUtcTimestamps,
                singleLine: _singleLine,
                compactOutput: _compactOutput,
                prettyPrint: _prettyPrint,
                indentation: _indentation,
                fieldSeparator: _fieldSeparator,
                lineSeparator: _lineSeparator,
                nullValue: _nullValue,
                maxMessageLength: _maxMessageLength,
                maxExceptionLength: _maxExceptionLength,
                maxPropertyCount: _maxPropertyCount,
                maxStackTraceLines: _maxStackTraceLines,
                escapeCharacter: _escapeCharacter,
                quoteCharacter: _quoteCharacter,
                fieldMappings: _fieldMappings,
                propertyTransformations: _propertyTransformations,
                excludedProperties: _excludedProperties.AsReadOnly(),
                includedProperties: _includedProperties.AsReadOnly(),
                propertyNameFormat: _propertyNameFormat,
                encoding: _encoding,
                culture: _culture,
                properties: _properties,
                tags: _tags.AsReadOnly(),
                description: _description);
        }

        /// <summary>
        /// Builds the FormatterConfig instance with validation bypass.
        /// </summary>
        /// <returns>A configured FormatterConfig instance</returns>
        public FormatterConfig BuildUnsafe()
        {
            return new FormatterConfig(
                name: _name,
                isEnabled: _isEnabled,
                formatterType: _formatterType,
                format: _format,
                messageTemplate: _messageTemplate,
                timestampFormat: _timestampFormat,
                includeTimestamp: _includeTimestamp,
                includeLogLevel: _includeLogLevel,
                includeChannel: _includeChannel,
                includeSourceContext: _includeSourceContext,
                includeSource: _includeSource,
                includeCorrelationId: _includeCorrelationId,
                includeThreadInfo: _includeThreadInfo,
                includeMachineInfo: _includeMachineInfo,
                includeExceptionDetails: _includeExceptionDetails,
                includeStackTrace: _includeStackTrace,
                includeScopes: _includeScopes,
                includeProperties: _includeProperties,
                includePerformanceMetrics: _includePerformanceMetrics,
                useUtcTimestamps: _useUtcTimestamps,
                singleLine: _singleLine,
                compactOutput: _compactOutput,
                prettyPrint: _prettyPrint,
                indentation: _indentation,
                fieldSeparator: _fieldSeparator,
                lineSeparator: _lineSeparator,
                nullValue: _nullValue,
                maxMessageLength: _maxMessageLength,
                maxExceptionLength: _maxExceptionLength,
                maxPropertyCount: _maxPropertyCount,
                maxStackTraceLines: _maxStackTraceLines,
                escapeCharacter: _escapeCharacter,
                quoteCharacter: _quoteCharacter,
                fieldMappings: _fieldMappings,
                propertyTransformations: _propertyTransformations,
                excludedProperties: _excludedProperties.AsReadOnly(),
                includedProperties: _includedProperties.AsReadOnly(),
                propertyNameFormat: _propertyNameFormat,
                encoding: _encoding,
                culture: _culture,
                properties: _properties,
                tags: _tags.AsReadOnly(),
                description: _description);
        }
    }
}