using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Logging.Models;

namespace AhBearStudios.Core.Logging.Configs
{
    /// <summary>
    /// Configuration record for log formatters with comprehensive formatting options.
    /// Provides immutable configuration for formatter behavior and output customization.
    /// </summary>
    public sealed record FormatterConfig
    {
        /// <summary>
        /// Gets the formatter name for identification.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets whether the formatter is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the formatter type identifier.
        /// </summary>
        public string FormatterType { get; init; }

        /// <summary>
        /// Gets the output format for this formatter.
        /// </summary>
        public LogFormat Format { get; init; }

        /// <summary>
        /// Gets the message template for formatting.
        /// </summary>
        public string MessageTemplate { get; init; }

        /// <summary>
        /// Gets the timestamp format string.
        /// </summary>
        public string TimestampFormat { get; init; }

        /// <summary>
        /// Gets whether to include the timestamp in output.
        /// </summary>
        public bool IncludeTimestamp { get; init; }

        /// <summary>
        /// Gets whether to include the log level in output.
        /// </summary>
        public bool IncludeLogLevel { get; init; }

        /// <summary>
        /// Gets whether to include the channel in output.
        /// </summary>
        public bool IncludeChannel { get; init; }

        /// <summary>
        /// Gets whether to include the source context in output.
        /// </summary>
        public bool IncludeSourceContext { get; init; }

        /// <summary>
        /// Gets whether to include the source system in output.
        /// </summary>
        public bool IncludeSource { get; init; }

        /// <summary>
        /// Gets whether to include the correlation ID in output.
        /// </summary>
        public bool IncludeCorrelationId { get; init; }

        /// <summary>
        /// Gets whether to include thread information in output.
        /// </summary>
        public bool IncludeThreadInfo { get; init; }

        /// <summary>
        /// Gets whether to include machine information in output.
        /// </summary>
        public bool IncludeMachineInfo { get; init; }

        /// <summary>
        /// Gets whether to include exception details in output.
        /// </summary>
        public bool IncludeExceptionDetails { get; init; }

        /// <summary>
        /// Gets whether to include stack traces in output.
        /// </summary>
        public bool IncludeStackTrace { get; init; }

        /// <summary>
        /// Gets whether to include scope information in output.
        /// </summary>
        public bool IncludeScopes { get; init; }

        /// <summary>
        /// Gets whether to include structured properties in output.
        /// </summary>
        public bool IncludeProperties { get; init; }

        /// <summary>
        /// Gets whether to include performance metrics in output.
        /// </summary>
        public bool IncludePerformanceMetrics { get; init; }

        /// <summary>
        /// Gets whether to use UTC timestamps.
        /// </summary>
        public bool UseUtcTimestamps { get; init; }

        /// <summary>
        /// Gets whether to use single-line output.
        /// </summary>
        public bool SingleLine { get; init; }

        /// <summary>
        /// Gets whether to use compact output (minimal whitespace).
        /// </summary>
        public bool CompactOutput { get; init; }

        /// <summary>
        /// Gets whether to use pretty-printing (formatted output).
        /// </summary>
        public bool PrettyPrint { get; init; }

        /// <summary>
        /// Gets the indentation string for structured output.
        /// </summary>
        public string Indentation { get; init; }

        /// <summary>
        /// Gets the field separator for delimited formats.
        /// </summary>
        public string FieldSeparator { get; init; }

        /// <summary>
        /// Gets the line separator for multi-line output.
        /// </summary>
        public string LineSeparator { get; init; }

        /// <summary>
        /// Gets the null value representation.
        /// </summary>
        public string NullValue { get; init; }

        /// <summary>
        /// Gets the maximum message length (0 = no limit).
        /// </summary>
        public int MaxMessageLength { get; init; }

        /// <summary>
        /// Gets the maximum exception length (0 = no limit).
        /// </summary>
        public int MaxExceptionLength { get; init; }

        /// <summary>
        /// Gets the maximum property count (0 = no limit).
        /// </summary>
        public int MaxPropertyCount { get; init; }

        /// <summary>
        /// Gets the maximum stack trace lines (0 = no limit).
        /// </summary>
        public int MaxStackTraceLines { get; init; }

        /// <summary>
        /// Gets the escape character for special characters.
        /// </summary>
        public char EscapeCharacter { get; init; }

        /// <summary>
        /// Gets the quote character for string values.
        /// </summary>
        public char QuoteCharacter { get; init; }

        /// <summary>
        /// Gets the custom field mappings for structured output.
        /// </summary>
        public IReadOnlyDictionary<string, string> FieldMappings { get; init; }

        /// <summary>
        /// Gets the custom property transformations.
        /// </summary>
        public IReadOnlyDictionary<string, string> PropertyTransformations { get; init; }

        /// <summary>
        /// Gets the excluded properties from output.
        /// </summary>
        public IReadOnlyList<string> ExcludedProperties { get; init; }

        /// <summary>
        /// Gets the included properties for output (empty = all).
        /// </summary>
        public IReadOnlyList<string> IncludedProperties { get; init; }

        /// <summary>
        /// Gets the property name format (camelCase, PascalCase, snake_case, etc.).
        /// </summary>
        public string PropertyNameFormat { get; init; }

        /// <summary>
        /// Gets the encoding name for output.
        /// </summary>
        public string Encoding { get; init; }

        /// <summary>
        /// Gets the culture name for formatting.
        /// </summary>
        public string Culture { get; init; }

        /// <summary>
        /// Gets additional formatter-specific properties.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; }

        /// <summary>
        /// Gets the tags for formatter categorization.
        /// </summary>
        public IReadOnlyList<string> Tags { get; init; }

        /// <summary>
        /// Gets the formatter description.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Initializes a new instance of the FormatterConfig record.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <param name="isEnabled">Whether the formatter is enabled</param>
        /// <param name="formatterType">The formatter type identifier</param>
        /// <param name="format">The output format</param>
        /// <param name="messageTemplate">The message template</param>
        /// <param name="timestampFormat">The timestamp format</param>
        /// <param name="includeTimestamp">Whether to include timestamp</param>
        /// <param name="includeLogLevel">Whether to include log level</param>
        /// <param name="includeChannel">Whether to include channel</param>
        /// <param name="includeSourceContext">Whether to include source context</param>
        /// <param name="includeSource">Whether to include source system</param>
        /// <param name="includeCorrelationId">Whether to include correlation ID</param>
        /// <param name="includeThreadInfo">Whether to include thread info</param>
        /// <param name="includeMachineInfo">Whether to include machine info</param>
        /// <param name="includeExceptionDetails">Whether to include exception details</param>
        /// <param name="includeStackTrace">Whether to include stack traces</param>
        /// <param name="includeScopes">Whether to include scopes</param>
        /// <param name="includeProperties">Whether to include properties</param>
        /// <param name="includePerformanceMetrics">Whether to include performance metrics</param>
        /// <param name="useUtcTimestamps">Whether to use UTC timestamps</param>
        /// <param name="singleLine">Whether to use single-line output</param>
        /// <param name="compactOutput">Whether to use compact output</param>
        /// <param name="prettyPrint">Whether to use pretty-printing</param>
        /// <param name="indentation">The indentation string</param>
        /// <param name="fieldSeparator">The field separator</param>
        /// <param name="lineSeparator">The line separator</param>
        /// <param name="nullValue">The null value representation</param>
        /// <param name="maxMessageLength">The maximum message length</param>
        /// <param name="maxExceptionLength">The maximum exception length</param>
        /// <param name="maxPropertyCount">The maximum property count</param>
        /// <param name="maxStackTraceLines">The maximum stack trace lines</param>
        /// <param name="escapeCharacter">The escape character</param>
        /// <param name="quoteCharacter">The quote character</param>
        /// <param name="fieldMappings">The field mappings</param>
        /// <param name="propertyTransformations">The property transformations</param>
        /// <param name="excludedProperties">The excluded properties</param>
        /// <param name="includedProperties">The included properties</param>
        /// <param name="propertyNameFormat">The property name format</param>
        /// <param name="encoding">The encoding name</param>
        /// <param name="culture">The culture name</param>
        /// <param name="properties">Additional properties</param>
        /// <param name="tags">Formatter tags</param>
        /// <param name="description">Formatter description</param>
        public FormatterConfig(
            string name,
            bool isEnabled = true,
            string formatterType = "Generic",
            LogFormat format = LogFormat.PlainText,
            string messageTemplate = null,
            string timestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
            bool includeTimestamp = true,
            bool includeLogLevel = true,
            bool includeChannel = true,
            bool includeSourceContext = false,
            bool includeSource = false,
            bool includeCorrelationId = false,
            bool includeThreadInfo = false,
            bool includeMachineInfo = false,
            bool includeExceptionDetails = true,
            bool includeStackTrace = true,
            bool includeScopes = false,
            bool includeProperties = false,
            bool includePerformanceMetrics = false,
            bool useUtcTimestamps = true,
            bool singleLine = false,
            bool compactOutput = false,
            bool prettyPrint = false,
            string indentation = "  ",
            string fieldSeparator = ",",
            string lineSeparator = null,
            string nullValue = "null",
            int maxMessageLength = 0,
            int maxExceptionLength = 0,
            int maxPropertyCount = 0,
            int maxStackTraceLines = 0,
            char escapeCharacter = '\\',
            char quoteCharacter = '"',
            IReadOnlyDictionary<string, string> fieldMappings = null,
            IReadOnlyDictionary<string, string> propertyTransformations = null,
            IReadOnlyList<string> excludedProperties = null,
            IReadOnlyList<string> includedProperties = null,
            string propertyNameFormat = "Original",
            string encoding = "UTF-8",
            string culture = "en-US",
            IReadOnlyDictionary<string, object> properties = null,
            IReadOnlyList<string> tags = null,
            string description = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsEnabled = isEnabled;
            FormatterType = formatterType ?? "Generic";
            Format = format;
            MessageTemplate = messageTemplate ?? GetDefaultTemplate(format);
            TimestampFormat = timestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff";
            IncludeTimestamp = includeTimestamp;
            IncludeLogLevel = includeLogLevel;
            IncludeChannel = includeChannel;
            IncludeSourceContext = includeSourceContext;
            IncludeSource = includeSource;
            IncludeCorrelationId = includeCorrelationId;
            IncludeThreadInfo = includeThreadInfo;
            IncludeMachineInfo = includeMachineInfo;
            IncludeExceptionDetails = includeExceptionDetails;
            IncludeStackTrace = includeStackTrace;
            IncludeScopes = includeScopes;
            IncludeProperties = includeProperties;
            IncludePerformanceMetrics = includePerformanceMetrics;
            UseUtcTimestamps = useUtcTimestamps;
            SingleLine = singleLine;
            CompactOutput = compactOutput;
            PrettyPrint = prettyPrint;
            Indentation = indentation ?? "  ";
            FieldSeparator = fieldSeparator ?? ",";
            LineSeparator = lineSeparator ?? Environment.NewLine;
            NullValue = nullValue ?? "null";
            MaxMessageLength = Math.Max(0, maxMessageLength);
            MaxExceptionLength = Math.Max(0, maxExceptionLength);
            MaxPropertyCount = Math.Max(0, maxPropertyCount);
            MaxStackTraceLines = Math.Max(0, maxStackTraceLines);
            EscapeCharacter = escapeCharacter;
            QuoteCharacter = quoteCharacter;
            FieldMappings = fieldMappings ?? new Dictionary<string, string>();
            PropertyTransformations = propertyTransformations ?? new Dictionary<string, string>();
            ExcludedProperties = excludedProperties ?? Array.Empty<string>();
            IncludedProperties = includedProperties ?? Array.Empty<string>();
            PropertyNameFormat = propertyNameFormat ?? "Original";
            Encoding = encoding ?? "UTF-8";
            Culture = culture ?? "en-US";
            Properties = properties ?? new Dictionary<string, object>();
            Tags = tags ?? Array.Empty<string>();
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Creates a configuration for plain text formatting.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <param name="includeTimestamp">Whether to include timestamp</param>
        /// <param name="includeLogLevel">Whether to include log level</param>
        /// <param name="includeChannel">Whether to include channel</param>
        /// <returns>A plain text FormatterConfig</returns>
        public static FormatterConfig ForPlainText(
            string name = "PlainText",
            bool includeTimestamp = true,
            bool includeLogLevel = true,
            bool includeChannel = true)
        {
            return new FormatterConfig(
                name: name,
                formatterType: "PlainText",
                format: LogFormat.PlainText,
                messageTemplate: "[{Timestamp}] [{Level}] [{Channel}] {Message}",
                includeTimestamp: includeTimestamp,
                includeLogLevel: includeLogLevel,
                includeChannel: includeChannel,
                singleLine: true,
                description: "Plain text formatter with customizable fields");
        }

        /// <summary>
        /// Creates a configuration for JSON formatting.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <param name="prettyPrint">Whether to use pretty-printing</param>
        /// <param name="includeAllFields">Whether to include all available fields</param>
        /// <returns>A JSON FormatterConfig</returns>
        public static FormatterConfig ForJson(
            string name = "Json",
            bool prettyPrint = false,
            bool includeAllFields = false)
        {
            return new FormatterConfig(
                name: name,
                formatterType: "Json",
                format: LogFormat.Json,
                includeTimestamp: true,
                includeLogLevel: true,
                includeChannel: true,
                includeSourceContext: includeAllFields,
                includeSource: includeAllFields,
                includeCorrelationId: includeAllFields,
                includeThreadInfo: includeAllFields,
                includeMachineInfo: includeAllFields,
                includeProperties: includeAllFields,
                includeScopes: includeAllFields,
                prettyPrint: prettyPrint,
                compactOutput: !prettyPrint,
                description: "JSON formatter with structured output");
        }

        /// <summary>
        /// Creates a configuration for CSV formatting.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <param name="delimiter">The field delimiter</param>
        /// <param name="includeHeaders">Whether to include headers</param>
        /// <returns>A CSV FormatterConfig</returns>
        public static FormatterConfig ForCsv(
            string name = "Csv",
            string delimiter = ",",
            bool includeHeaders = true)
        {
            var properties = new Dictionary<string, object>
            {
                ["IncludeHeaders"] = includeHeaders
            };

            return new FormatterConfig(
                name: name,
                formatterType: "Csv",
                format: LogFormat.Csv,
                fieldSeparator: delimiter,
                includeTimestamp: true,
                includeLogLevel: true,
                includeChannel: true,
                singleLine: true,
                properties: properties,
                description: $"CSV formatter with '{delimiter}' delimiter");
        }

        /// <summary>
        /// Creates a configuration for Unity-optimized formatting.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <param name="includePerformanceMetrics">Whether to include performance metrics</param>
        /// <returns>A Unity-optimized FormatterConfig</returns>
        public static FormatterConfig ForUnity(
            string name = "Unity",
            bool includePerformanceMetrics = true)
        {
            return new FormatterConfig(
                name: name,
                formatterType: "Unity",
                format: LogFormat.PlainText,
                messageTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}",
                timestampFormat: "HH:mm:ss.fff",
                includeTimestamp: true,
                includeLogLevel: true,
                includeChannel: true,
                includeThreadInfo: true,
                includePerformanceMetrics: includePerformanceMetrics,
                singleLine: true,
                compactOutput: true,
                description: "Unity-optimized formatter for game development");
        }

        /// <summary>
        /// Creates a configuration for mobile-optimized formatting.
        /// </summary>
        /// <param name="name">The formatter name</param>
        /// <returns>A mobile-optimized FormatterConfig</returns>
        public static FormatterConfig ForMobile(string name = "Mobile")
        {
            return new FormatterConfig(
                name: name,
                formatterType: "Mobile",
                format: LogFormat.Json,
                includeTimestamp: true,
                includeLogLevel: true,
                includeChannel: false,
                includeSourceContext: false,
                includeSource: false,
                includeCorrelationId: false,
                includeThreadInfo: false,
                includeMachineInfo: false,
                includeStackTrace: false,
                includeScopes: false,
                includeProperties: false,
                includePerformanceMetrics: false,
                compactOutput: true,
                maxMessageLength: 500,
                maxExceptionLength: 1000,
                description: "Mobile-optimized formatter with minimal overhead");
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
        /// Gets a property value by key.
        /// </summary>
        /// <typeparam name="T">The type of the property value</typeparam>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">The default value if not found</param>
        /// <returns>The property value or default value</returns>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key) || !Properties.TryGetValue(key, out var value))
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Determines if the formatter has the specified tag.
        /// </summary>
        /// <param name="tag">The tag to check for</param>
        /// <returns>True if the formatter has the tag</returns>
        public bool HasTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && Tags.Contains(tag);
        }

        /// <summary>
        /// Creates a new FormatterConfig with modified properties.
        /// </summary>
        /// <param name="isEnabled">New enabled state</param>
        /// <param name="format">New format</param>
        /// <param name="messageTemplate">New message template</param>
        /// <param name="singleLine">New single-line setting</param>
        /// <param name="additionalProperties">Additional properties to add</param>
        /// <returns>A new FormatterConfig with the modified properties</returns>
        public FormatterConfig WithModifications(
            bool? isEnabled = null,
            LogFormat? format = null,
            string messageTemplate = null,
            bool? singleLine = null,
            IReadOnlyDictionary<string, object> additionalProperties = null)
        {
            var newProperties = new Dictionary<string, object>(Properties);
            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    newProperties[kvp.Key] = kvp.Value;
                }
            }

            return this with
            {
                IsEnabled = isEnabled ?? IsEnabled,
                Format = format ?? Format,
                MessageTemplate = messageTemplate ?? MessageTemplate,
                SingleLine = singleLine ?? SingleLine,
                Properties = newProperties
            };
        }

        /// <summary>
        /// Validates the formatter configuration.
        /// </summary>
        /// <returns>A validation result</returns>
        public ValidationResult Validate()
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add(new ValidationError("Formatter name cannot be empty", nameof(Name)));

            if (string.IsNullOrWhiteSpace(MessageTemplate))
                errors.Add(new ValidationError("Message template cannot be empty", nameof(MessageTemplate)));

            if (MaxMessageLength < 0)
                errors.Add(new ValidationError("Maximum message length cannot be negative", nameof(MaxMessageLength)));

            if (MaxExceptionLength < 0)
                errors.Add(new ValidationError("Maximum exception length cannot be negative", nameof(MaxExceptionLength)));

            if (MaxPropertyCount < 0)
                errors.Add(new ValidationError("Maximum property count cannot be negative", nameof(MaxPropertyCount)));

            if (MaxStackTraceLines < 0)
                errors.Add(new ValidationError("Maximum stack trace lines cannot be negative", nameof(MaxStackTraceLines)));

            if (string.IsNullOrEmpty(TimestampFormat))
                warnings.Add(new ValidationWarning("Empty timestamp format may cause formatting issues", nameof(TimestampFormat)));

            if (MaxMessageLength > 0 && MaxMessageLength < 100)
                warnings.Add(new ValidationWarning("Very short message length may truncate important information", nameof(MaxMessageLength)));

            if (CompactOutput && PrettyPrint)
                warnings.Add(new ValidationWarning("Compact output and pretty print are conflicting settings", nameof(CompactOutput)));

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, Name, warnings)
                : ValidationResult.Success(Name, warnings);
        }

        /// <summary>
        /// Gets a summary description of the formatter configuration.
        /// </summary>
        /// <returns>A formatter summary string</returns>
        public string GetSummary()
        {
            var parts = new List<string>
            {
                $"Type: {FormatterType}",
                $"LogFormat: {Format}",
                $"Output: {(SingleLine ? "SingleLine" : "MultiLine")}"
            };

            if (CompactOutput)
                parts.Add("Compact");
            if (PrettyPrint)
                parts.Add("Pretty");
            if (IncludeTimestamp)
                parts.Add("Timestamp");
            if (IncludeCorrelationId)
                parts.Add("CorrelationId");
            if (IncludeProperties)
                parts.Add("Properties");
            if (IncludeScopes)
                parts.Add("Scopes");

            return string.Join(" | ", parts);
        }
    }
}