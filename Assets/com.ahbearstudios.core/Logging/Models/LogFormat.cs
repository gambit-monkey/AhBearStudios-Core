namespace AhBearStudios.Core.Logging.Models
{
    /// <summary>
    /// Defines the output format types for log messages.
    /// Used by log formatters to determine the structure and encoding of log output.
    /// </summary>
    public enum LogFormat
    {
        /// <summary>
        /// Plain text format with simple string formatting.
        /// Human-readable format suitable for console output and basic file logging.
        /// </summary>
        PlainText,

        /// <summary>
        /// JSON format with structured data representation.
        /// Machine-readable format suitable for log aggregation and analysis tools.
        /// </summary>
        Json,

        /// <summary>
        /// XML format with hierarchical data representation.
        /// Structured format suitable for systems requiring XML-based logging.
        /// </summary>
        Xml,

        /// <summary>
        /// Binary format for high-performance scenarios.
        /// Compact format suitable for high-throughput logging with minimal overhead.
        /// </summary>
        Binary,

        /// <summary>
        /// CSV format for tabular data representation.
        /// Structured format suitable for data analysis and spreadsheet import.
        /// </summary>
        Csv,

        /// <summary>
        /// Syslog format compliant with RFC 5424.
        /// Standard format for Unix/Linux system logging and log forwarding.
        /// </summary>
        Syslog,

        /// <summary>
        /// Common Event LogFormat (CEF) for security information and event management.
        /// Structured format suitable for SIEM systems and security monitoring.
        /// </summary>
        Cef,

        /// <summary>
        /// Key-value pairs format for structured logging.
        /// Simple structured format suitable for log parsing and analysis.
        /// </summary>
        KeyValue,

        /// <summary>
        /// GELF (Graylog Extended Log LogFormat) for centralized logging.
        /// JSON-based format optimized for Graylog and similar log management systems.
        /// </summary>
        Gelf,

        /// <summary>
        /// MessagePack format for efficient binary serialization.
        /// Compact binary format suitable for high-performance logging and transmission.
        /// </summary>
        MessagePack,

        /// <summary>
        /// Protocol Buffers format for cross-platform serialization.
        /// Efficient binary format suitable for distributed systems and microservices.
        /// </summary>
        Protobuf,

        /// <summary>
        /// Custom format defined by user implementation.
        /// Flexible format type for specialized logging requirements.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Extensions for LogFormat enum to provide additional functionality.
    /// </summary>
    public static class LogFormatExtensions
    {
        /// <summary>
        /// Determines if the format is text-based (human-readable).
        /// </summary>
        /// <param name="format">The log format to check</param>
        /// <returns>True if the format is text-based</returns>
        public static bool IsTextBased(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => true,
                LogFormat.Json => true,
                LogFormat.Xml => true,
                LogFormat.Csv => true,
                LogFormat.Syslog => true,
                LogFormat.Cef => true,
                LogFormat.KeyValue => true,
                LogFormat.Gelf => true,
                LogFormat.Binary => false,
                LogFormat.MessagePack => false,
                LogFormat.Protobuf => false,
                LogFormat.Custom => false, // Assume binary for safety
                _ => false
            };
        }

        /// <summary>
        /// Determines if the format is binary-based.
        /// </summary>
        /// <param name="format">The log format to check</param>
        /// <returns>True if the format is binary-based</returns>
        public static bool IsBinaryBased(this LogFormat format)
        {
            return !format.IsTextBased();
        }

        /// <summary>
        /// Determines if the format is structured (contains metadata).
        /// </summary>
        /// <param name="format">The log format to check</param>
        /// <returns>True if the format is structured</returns>
        public static bool IsStructured(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => false,
                LogFormat.Json => true,
                LogFormat.Xml => true,
                LogFormat.Csv => true,
                LogFormat.Syslog => true,
                LogFormat.Cef => true,
                LogFormat.KeyValue => true,
                LogFormat.Gelf => true,
                LogFormat.Binary => true,
                LogFormat.MessagePack => true,
                LogFormat.Protobuf => true,
                LogFormat.Custom => true, // Assume structured for safety
                _ => false
            };
        }

        /// <summary>
        /// Gets the typical file extension for the format.
        /// </summary>
        /// <param name="format">The log format</param>
        /// <returns>The file extension (without dot)</returns>
        public static string GetFileExtension(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => "log",
                LogFormat.Json => "json",
                LogFormat.Xml => "xml",
                LogFormat.Csv => "csv",
                LogFormat.Syslog => "syslog",
                LogFormat.Cef => "cef",
                LogFormat.KeyValue => "kv",
                LogFormat.Gelf => "gelf",
                LogFormat.Binary => "bin",
                LogFormat.MessagePack => "msgpack",
                LogFormat.Protobuf => "pb",
                LogFormat.Custom => "custom",
                _ => "log"
            };
        }

        /// <summary>
        /// Gets the MIME type for the format.
        /// </summary>
        /// <param name="format">The log format</param>
        /// <returns>The MIME type</returns>
        public static string GetMimeType(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => "text/plain",
                LogFormat.Json => "application/json",
                LogFormat.Xml => "application/xml",
                LogFormat.Csv => "text/csv",
                LogFormat.Syslog => "text/plain",
                LogFormat.Cef => "text/plain",
                LogFormat.KeyValue => "text/plain",
                LogFormat.Gelf => "application/json",
                LogFormat.Binary => "application/octet-stream",
                LogFormat.MessagePack => "application/x-msgpack",
                LogFormat.Protobuf => "application/x-protobuf",
                LogFormat.Custom => "application/octet-stream",
                _ => "text/plain"
            };
        }

        /// <summary>
        /// Determines if the format supports compression.
        /// </summary>
        /// <param name="format">The log format to check</param>
        /// <returns>True if the format supports compression</returns>
        public static bool SupportsCompression(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => true,
                LogFormat.Json => true,
                LogFormat.Xml => true,
                LogFormat.Csv => true,
                LogFormat.Syslog => true,
                LogFormat.Cef => true,
                LogFormat.KeyValue => true,
                LogFormat.Gelf => true,
                LogFormat.Binary => false, // Already compact
                LogFormat.MessagePack => false, // Already compact
                LogFormat.Protobuf => false, // Already compact
                LogFormat.Custom => true, // Assume yes for flexibility
                _ => true
            };
        }

        /// <summary>
        /// Gets the typical performance characteristics for the format.
        /// </summary>
        /// <param name="format">The log format</param>
        /// <returns>A tuple of (serialization speed, size efficiency, readability)</returns>
        public static (string speed, string size, string readability) GetPerformanceCharacteristics(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => ("Fast", "Poor", "Excellent"),
                LogFormat.Json => ("Medium", "Medium", "Good"),
                LogFormat.Xml => ("Slow", "Poor", "Good"),
                LogFormat.Csv => ("Fast", "Good", "Good"),
                LogFormat.Syslog => ("Fast", "Medium", "Good"),
                LogFormat.Cef => ("Medium", "Medium", "Good"),
                LogFormat.KeyValue => ("Fast", "Good", "Good"),
                LogFormat.Gelf => ("Medium", "Medium", "Good"),
                LogFormat.Binary => ("Very Fast", "Excellent", "Poor"),
                LogFormat.MessagePack => ("Very Fast", "Excellent", "Poor"),
                LogFormat.Protobuf => ("Very Fast", "Excellent", "Poor"),
                LogFormat.Custom => ("Unknown", "Unknown", "Unknown"),
                _ => ("Unknown", "Unknown", "Unknown")
            };
        }

        /// <summary>
        /// Gets a description of the format.
        /// </summary>
        /// <param name="format">The log format</param>
        /// <returns>A description of the format</returns>
        public static string GetDescription(this LogFormat format)
        {
            return format switch
            {
                LogFormat.PlainText => "Simple text format with basic string formatting",
                LogFormat.Json => "JSON format with structured data representation",
                LogFormat.Xml => "XML format with hierarchical data representation",
                LogFormat.Csv => "CSV format for tabular data representation",
                LogFormat.Syslog => "Syslog format compliant with RFC 5424",
                LogFormat.Cef => "Common Event LogFormat for security information and event management",
                LogFormat.KeyValue => "Key-value pairs format for structured logging",
                LogFormat.Gelf => "Graylog Extended Log LogFormat for centralized logging",
                LogFormat.Binary => "Binary format for high-performance scenarios",
                LogFormat.MessagePack => "MessagePack format for efficient binary serialization",
                LogFormat.Protobuf => "Protocol Buffers format for cross-platform serialization",
                LogFormat.Custom => "Custom format defined by user implementation",
                _ => "Unknown format"
            };
        }
    }
}