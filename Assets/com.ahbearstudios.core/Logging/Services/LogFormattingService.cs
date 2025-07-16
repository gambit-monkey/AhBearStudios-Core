using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using AhBearStudios.Core.Logging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// High-performance message formatting service for log messages.
    /// Provides efficient string formatting with minimal allocations and structured logging support.
    /// Supports template-based formatting with placeholder replacement and custom formatters.
    /// </summary>
    public sealed class LogFormattingService : ILogFormattingService
    {
        private readonly Dictionary<string, ILogFormatter> _customFormatters;
        private readonly StringBuilder _stringBuilder;
        private readonly object _formatterLock = new object();
        private readonly FormattingCache _cache;

        /// <summary>
        /// Gets the default message format template.
        /// </summary>
        public string DefaultMessageFormat { get; }

        /// <summary>
        /// Gets the timestamp format string.
        /// </summary>
        public string TimestampFormat { get; }

        /// <summary>
        /// Gets whether high-performance mode is enabled.
        /// </summary>
        public bool HighPerformanceMode { get; }

        /// <summary>
        /// Gets whether caching is enabled for formatted messages.
        /// </summary>
        public bool CachingEnabled { get; }

        /// <summary>
        /// Gets the maximum cache size for formatted messages.
        /// </summary>
        public int MaxCacheSize { get; }

        /// <summary>
        /// Gets formatting performance metrics.
        /// </summary>
        public FormattingMetrics Metrics { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LogFormattingService.
        /// </summary>
        /// <param name="defaultMessageFormat">The default message format template</param>
        /// <param name="timestampFormat">The timestamp format string</param>
        /// <param name="highPerformanceMode">Whether to enable high-performance mode</param>
        /// <param name="cachingEnabled">Whether to enable message caching</param>
        /// <param name="maxCacheSize">The maximum cache size</param>
        public LogFormattingService(
            string defaultMessageFormat = null,
            string timestampFormat = null,
            bool highPerformanceMode = false,
            bool cachingEnabled = true,
            int maxCacheSize = 1000)
        {
            DefaultMessageFormat = defaultMessageFormat ?? "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Channel}] {Message}";
            TimestampFormat = timestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff";
            HighPerformanceMode = highPerformanceMode;
            CachingEnabled = cachingEnabled;
            MaxCacheSize = maxCacheSize;

            _customFormatters = new Dictionary<string, ILogFormatter>();
            _stringBuilder = new StringBuilder(1024);
            _cache = CachingEnabled ? new FormattingCache(MaxCacheSize) : null;
            
            Metrics = new FormattingMetrics();

            // Register built-in formatters
            RegisterBuiltInFormatters();
        }

        /// <summary>
        /// Formats a log message using the default format template.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <returns>The formatted message string</returns>
        public string FormatMessage(in LogMessage logMessage)
        {
            return FormatMessage(logMessage, DefaultMessageFormat);
        }

        /// <summary>
        /// Formats a log message using the specified format template.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <param name="format">The format template to use</param>
        /// <returns>The formatted message string</returns>
        public string FormatMessage(in LogMessage logMessage, string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = DefaultMessageFormat;
            }

            Metrics.IncrementFormattingRequests();

            // Check cache first
            if (CachingEnabled && _cache.TryGetFormatted(logMessage, format, out var cachedResult))
            {
                Metrics.IncrementCacheHits();
                return cachedResult;
            }

            var startTime = HighPerformanceMode ? DateTime.UtcNow : default;

            try
            {
                var formattedMessage = PerformFormatting(logMessage, format);

                // Cache the result
                if (CachingEnabled)
                {
                    _cache.CacheFormatted(logMessage, format, formattedMessage);
                }

                if (HighPerformanceMode)
                {
                    Metrics.UpdateFormattingTime(DateTime.UtcNow - startTime);
                }

                return formattedMessage;
            }
            catch (Exception ex)
            {
                Metrics.IncrementFormattingErrors();
                return $"[FORMATTING ERROR] {logMessage.Message} | Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Formats multiple log messages efficiently.
        /// </summary>
        /// <param name="logMessages">The log messages to format</param>
        /// <param name="format">The format template to use</param>
        /// <returns>An array of formatted message strings</returns>
        public string[] FormatMessages(IReadOnlyList<LogMessage> logMessages, string format = null)
        {
            if (logMessages == null || logMessages.Count == 0)
            {
                return Array.Empty<string>();
            }

            format ??= DefaultMessageFormat;
            var results = new string[logMessages.Count];

            for (int i = 0; i < logMessages.Count; i++)
            {
                results[i] = FormatMessage(logMessages[i], format);
            }

            return results;
        }

        /// <summary>
        /// Formats a log message for structured logging output.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <returns>A dictionary containing structured log data</returns>
        public Dictionary<string, object> FormatStructured(in LogMessage logMessage)
        {
            var structured = new Dictionary<string, object>
            {
                ["@timestamp"] = logMessage.Timestamp.ToString("O"), // ISO 8601 format
                ["@level"] = logMessage.Level.ToString(),
                ["@channel"] = logMessage.Channel,
                ["@message"] = logMessage.Message,
                ["@thread"] = logMessage.ThreadId
            };

            if (!string.IsNullOrEmpty(logMessage.CorrelationId.ToString()))
            {
                structured["@correlationId"] = logMessage.CorrelationId;
            }

            if (!string.IsNullOrEmpty(logMessage.SourceContext.ToString()))
            {
                structured["@sourceContext"] = logMessage.SourceContext;
            }

            if (logMessage.Exception != null)
            {
                structured["@exception"] = new
                {
                    type = logMessage.Exception.GetType().Name,
                    message = logMessage.Exception.Message,
                    stackTrace = logMessage.Exception.StackTrace
                };
            }

            // Add custom properties
            if (logMessage.Properties != null)
            {
                foreach (var kvp in logMessage.Properties)
                {
                    structured[kvp.Key] = kvp.Value;
                }
            }

            return structured;
        }

        /// <summary>
        /// Registers a custom formatter for a specific placeholder.
        /// </summary>
        /// <param name="placeholder">The placeholder name (without braces)</param>
        /// <param name="formatter">The custom formatter</param>
        public void RegisterFormatter(string placeholder, ILogFormatter formatter)
        {
            if (string.IsNullOrEmpty(placeholder))
                throw new ArgumentException("Placeholder cannot be null or empty", nameof(placeholder));

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            lock (_formatterLock)
            {
                _customFormatters[placeholder] = formatter;
            }
        }

        /// <summary>
        /// Unregisters a custom formatter.
        /// </summary>
        /// <param name="placeholder">The placeholder name to unregister</param>
        /// <returns>True if the formatter was removed, false if it wasn't found</returns>
        public bool UnregisterFormatter(string placeholder)
        {
            if (string.IsNullOrEmpty(placeholder))
                return false;

            lock (_formatterLock)
            {
                return _customFormatters.Remove(placeholder);
            }
        }

        /// <summary>
        /// Gets all registered custom formatters.
        /// </summary>
        /// <returns>A dictionary of placeholder names and their formatters</returns>
        public IReadOnlyDictionary<string, ILogFormatter> GetRegisteredFormatters()
        {
            lock (_formatterLock)
            {
                return new Dictionary<string, ILogFormatter>(_customFormatters);
            }
        }

        /// <summary>
        /// Clears the formatting cache.
        /// </summary>
        public void ClearCache()
        {
            _cache?.Clear();
        }

        /// <summary>
        /// Gets the current formatting performance metrics.
        /// </summary>
        /// <returns>A snapshot of current metrics</returns>
        public FormattingMetrics GetMetrics()
        {
            return Metrics.CreateSnapshot();
        }

        /// <summary>
        /// Resets the formatting performance metrics.
        /// </summary>
        public void ResetMetrics()
        {
            Metrics = new FormattingMetrics();
        }

        /// <summary>
        /// Performs the actual message formatting.
        /// </summary>
        /// <param name="logMessage">The log message to format</param>
        /// <param name="format">The format template</param>
        /// <returns>The formatted message string</returns>
        private string PerformFormatting(in LogMessage logMessage, string format)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append(format);

                // Replace standard placeholders
                ReplacePlaceholder(_stringBuilder, "Timestamp", FormatTimestamp(logMessage.Timestamp));
                ReplacePlaceholder(_stringBuilder, "Level", logMessage.Level.ToString());
                ReplacePlaceholder(_stringBuilder, "Channel", logMessage.Channel.ToString());
                ReplacePlaceholder(_stringBuilder, "Message", logMessage.Message.ToString());
                ReplacePlaceholder(_stringBuilder, "CorrelationId", logMessage.CorrelationId.ToString());
                ReplacePlaceholder(_stringBuilder, "SourceContext", logMessage.SourceContext.ToString());
                ReplacePlaceholder(_stringBuilder, "ThreadId", logMessage.ThreadId.ToString());

                // Replace exception placeholder if present
                if (logMessage.Exception != null)
                {
                    ReplacePlaceholder(_stringBuilder, "Exception", FormatException(logMessage.Exception));
                }

                // Replace custom property placeholders
                if (logMessage.Properties != null)
                {
                    foreach (var kvp in logMessage.Properties)
                    {
                        ReplacePlaceholder(_stringBuilder, kvp.Key, kvp.Value?.ToString() ?? string.Empty);
                    }
                }

                // Apply custom formatters
                ApplyCustomFormatters(_stringBuilder, logMessage);

                return _stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Replaces a placeholder in the string builder.
        /// </summary>
        /// <param name="sb">The string builder</param>
        /// <param name="placeholder">The placeholder name</param>
        /// <param name="value">The replacement value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReplacePlaceholder(StringBuilder sb, string placeholder, string value)
        {
            var placeholderPattern = $"{{{placeholder}}}";
            sb.Replace(placeholderPattern, value ?? string.Empty);
        }

        /// <summary>
        /// Formats a timestamp using the configured format.
        /// </summary>
        /// <param name="timestamp">The timestamp to format</param>
        /// <returns>The formatted timestamp string</returns>
        private string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats an exception for display.
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <returns>The formatted exception string</returns>
        private static string FormatException(Exception exception)
        {
            if (exception == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"Exception: {exception.GetType().Name}");
            sb.AppendLine($"Message: {exception.Message}");
            
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                sb.AppendLine($"Stack Trace: {exception.StackTrace}");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Applies custom formatters to the string builder.
        /// </summary>
        /// <param name="sb">The string builder</param>
        /// <param name="logMessage">The log message</param>
        private void ApplyCustomFormatters(StringBuilder sb, in LogMessage logMessage)
        {
            lock (_formatterLock)
            {
                foreach (var kvp in _customFormatters)
                {
                    var placeholder = $"{{{kvp.Key}}}";
                    if (sb.ToString().Contains(placeholder))
                    {
                        var formattedValue = kvp.Value.Format(logMessage);
                        sb.Replace(placeholder, formattedValue);
                    }
                }
            }
        }

        /// <summary>
        /// Registers built-in formatters.
        /// </summary>
        private void RegisterBuiltInFormatters()
        {
            RegisterFormatter("Timestamp:yyyy-MM-dd HH:mm:ss.fff", new TimestampFormatter("yyyy-MM-dd HH:mm:ss.fff"));
            RegisterFormatter("Level:Upper", new LevelFormatter(true));
            RegisterFormatter("Level:Lower", new LevelFormatter(false));
            RegisterFormatter("Message:Truncate", new TruncatingFormatter(100));
        }
    }

    /// <summary>
    /// Interface for custom log formatters.
    /// </summary>
    public interface ILogFormatter
    {
        /// <summary>
        /// Formats a log message component.
        /// </summary>
        /// <param name="logMessage">The log message</param>
        /// <returns>The formatted string</returns>
        string Format(in LogMessage logMessage);
    }

    /// <summary>
    /// Formatter for timestamp values.
    /// </summary>
    public sealed class TimestampFormatter : ILogFormatter
    {
        private readonly string _format;

        public TimestampFormatter(string format)
        {
            _format = format ?? "yyyy-MM-dd HH:mm:ss.fff";
        }

        public string Format(in LogMessage logMessage)
        {
            return logMessage.Timestamp.ToString(_format, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Formatter for log level values.
    /// </summary>
    public sealed class LevelFormatter : ILogFormatter
    {
        private readonly bool _uppercase;

        public LevelFormatter(bool uppercase)
        {
            _uppercase = uppercase;
        }

        public string Format(in LogMessage logMessage)
        {
            var level = logMessage.Level.ToString();
            return _uppercase ? level.ToUpperInvariant() : level.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Formatter that truncates messages to a specified length.
    /// </summary>
    public sealed class TruncatingFormatter : ILogFormatter
    {
        private readonly int _maxLength;

        public TruncatingFormatter(int maxLength)
        {
            _maxLength = maxLength;
        }

        public string Format(in LogMessage logMessage)
        {
            // Create a local copy to avoid the struct member access restriction
            var message = logMessage.Message.ToString();
            return message.Length > _maxLength 
                ? message.Substring(0, _maxLength) + "..." 
                : message;
        }
    }

    /// <summary>
    /// Cache for formatted log messages.
    /// </summary>
    internal sealed class FormattingCache
    {
        private readonly Dictionary<string, string> _cache;
        private readonly int _maxSize;

        public FormattingCache(int maxSize)
        {
            _maxSize = maxSize;
            _cache = new Dictionary<string, string>(maxSize);
        }

        public bool TryGetFormatted(in LogMessage logMessage, string format, out string result)
        {
            var key = CreateCacheKey(logMessage, format);
            return _cache.TryGetValue(key, out result);
        }

        public void CacheFormatted(in LogMessage logMessage, string format, string result)
        {
            if (_cache.Count >= _maxSize)
            {
                // Simple eviction: clear half the cache
                var keysToRemove = new List<string>();
                var count = 0;
                foreach (var key in _cache.Keys)
                {
                    keysToRemove.Add(key);
                    if (++count >= _maxSize / 2) break;
                }

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
            }

            var cacheKey = CreateCacheKey(logMessage, format);
            _cache[cacheKey] = result;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        private static string CreateCacheKey(in LogMessage logMessage, string format)
        {
            return $"{logMessage.Level}|{logMessage.Channel.ToString()}|{logMessage.Message.ToString()}|{format}";
        }
    }

    /// <summary>
    /// Performance metrics for log formatting operations.
    /// </summary>
    public sealed class FormattingMetrics
    {
        private volatile int _formattingRequests = 0;
        private volatile int _cacheHits = 0;
        private volatile int _formattingErrors = 0;
        private long _totalFormattingTicks = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// Gets the total number of formatting requests.
        /// </summary>
        public int FormattingRequests => _formattingRequests;

        /// <summary>
        /// Gets the total number of cache hits.
        /// </summary>
        public int CacheHits => _cacheHits;

        /// <summary>
        /// Gets the total number of formatting errors.
        /// </summary>
        public int FormattingErrors => _formattingErrors;

        /// <summary>
        /// Gets the cache hit ratio.
        /// </summary>
        public double CacheHitRatio => _formattingRequests > 0 ? (double)_cacheHits / _formattingRequests : 0.0;

        /// <summary>
        /// Gets the average formatting time.
        /// </summary>
        public TimeSpan AverageFormattingTime => _formattingRequests > 0 
            ? TimeSpan.FromTicks(_totalFormattingTicks / _formattingRequests) 
            : TimeSpan.Zero;

        /// <summary>
        /// Gets the total uptime of the formatting service.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        internal void IncrementFormattingRequests() => Interlocked.Increment(ref _formattingRequests);
        internal void IncrementCacheHits() => Interlocked.Increment(ref _cacheHits);
        internal void IncrementFormattingErrors() => Interlocked.Increment(ref _formattingErrors);
        internal void UpdateFormattingTime(TimeSpan formattingTime) => Interlocked.Add(ref _totalFormattingTicks, formattingTime.Ticks);

        /// <summary>
        /// Creates a snapshot of the current metrics.
        /// </summary>
        /// <returns>A new FormattingMetrics instance with current values</returns>
        public FormattingMetrics CreateSnapshot()
        {
            return new FormattingMetrics
            {
                _formattingRequests = _formattingRequests,
                _cacheHits = _cacheHits,
                _formattingErrors = _formattingErrors,
                _totalFormattingTicks = _totalFormattingTicks
            };
        }
    }
}