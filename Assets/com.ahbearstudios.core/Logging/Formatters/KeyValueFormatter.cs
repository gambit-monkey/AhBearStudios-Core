using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AhBearStudios.Core.Common.Models;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// Key-value pairs formatter for structured logging.
    /// Simple structured format suitable for log parsing and analysis.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class KeyValueFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "KeyValue";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.KeyValue;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the KeyValueFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public KeyValueFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["KeyValueSeparator"] = "=",
                ["PairSeparator"] = " ",
                ["QuoteValues"] = true,
                ["QuoteChar"] = "\"",
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = false,
                ["IncludeMachineInfo"] = false,
                ["IncludeException"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["TimestampFormat"] = "yyyy-MM-ddTHH:mm:ss.fffZ",
                ["UseUtcTimestamp"] = true,
                ["EscapeQuotes"] = true
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var kvSeparator = GetSetting<string>("KeyValueSeparator");
            var pairSeparator = GetSetting<string>("PairSeparator");
            var quoteValues = GetSetting<bool>("QuoteValues");
            var quoteChar = GetSetting<string>("QuoteChar");
            var escapeQuotes = GetSetting<bool>("EscapeQuotes");
            var pairs = new List<string>();

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                var timestampFormat = GetSetting<string>("TimestampFormat");
                var timestampValue = FormatValue(timestamp.ToString(timestampFormat), quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"timestamp{kvSeparator}{timestampValue}");
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                var levelValue = FormatValue(entry.Level.ToString().ToLower(), quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"level{kvSeparator}{levelValue}");
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                var channelValue = FormatValue(entry.Channel.ToString(), quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"channel{kvSeparator}{channelValue}");
            }

            var messageValue = FormatValue(entry.Message.ToString(), quoteValues, quoteChar, escapeQuotes);
            pairs.Add($"message{kvSeparator}{messageValue}");

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                if (!string.IsNullOrEmpty(corrId))
                {
                    var corrIdValue = FormatValue(corrId, quoteValues, quoteChar, escapeQuotes);
                    pairs.Add($"correlationId{kvSeparator}{corrIdValue}");
                }
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                var threadIdValue = FormatValue(entry.ThreadId.ToString(), quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"threadId{kvSeparator}{threadIdValue}");

                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                var threadNameValue = FormatValue(threadName, quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"threadName{kvSeparator}{threadNameValue}");
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                var machineNameValue = FormatValue(entry.MachineName.ToString(), quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"machineName{kvSeparator}{machineNameValue}");

                var instanceIdValue = FormatValue(entry.InstanceId.ToString(), quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"instanceId{kvSeparator}{instanceIdValue}");
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                var exceptionTypeValue = FormatValue(entry.Exception.GetType().Name, quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"exceptionType{kvSeparator}{exceptionTypeValue}");

                var exceptionMessageValue = FormatValue(entry.Exception.Message, quoteValues, quoteChar, escapeQuotes);
                pairs.Add($"exceptionMessage{kvSeparator}{exceptionMessageValue}");

                if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    var stackTraceValue = FormatValue(entry.Exception.StackTrace, quoteValues, quoteChar, escapeQuotes);
                    pairs.Add($"exceptionStackTrace{kvSeparator}{stackTraceValue}");
                }
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        var cpuValue = FormatValue(cpuMetrics.LastOrDefault().Value.ToString("F1"), quoteValues, quoteChar, escapeQuotes);
                        pairs.Add($"cpuUsage{kvSeparator}{cpuValue}");
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        var memoryValue = FormatValue((memoryMetrics.LastOrDefault().Value / 1024 / 1024).ToString("F1"), quoteValues, quoteChar, escapeQuotes);
                        pairs.Add($"memoryUsageMB{kvSeparator}{memoryValue}");
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        var timeValue = FormatValue(processingTimeMetrics.LastOrDefault().Value.ToString("F2"), quoteValues, quoteChar, escapeQuotes);
                        pairs.Add($"processingTimeMs{kvSeparator}{timeValue}");
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            return string.Join(pairSeparator, pairs);
        }

        /// <inheritdoc />
        public IEnumerable<string> FormatBatch(IReadOnlyCollection<LogEntry> entries, 
            FixedString64Bytes correlationId = default)
        {
            return entries.Select(entry => Format(entry, correlationId));
        }

        /// <inheritdoc />
        public ValidationResult Validate(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            var kvSeparator = GetSetting<string>("KeyValueSeparator");
            if (string.IsNullOrEmpty(kvSeparator))
            {
                errors.Add(new ValidationError("KeyValueSeparator cannot be empty", "KeyValueSeparator"));
            }

            var pairSeparator = GetSetting<string>("PairSeparator");
            if (string.IsNullOrEmpty(pairSeparator))
            {
                errors.Add(new ValidationError("PairSeparator cannot be empty", "PairSeparator"));
            }

            var quoteChar = GetSetting<string>("QuoteChar");
            if (GetSetting<bool>("QuoteValues") && string.IsNullOrEmpty(quoteChar))
            {
                errors.Add(new ValidationError("QuoteChar cannot be empty when QuoteValues is true", "QuoteChar"));
            }

            var timestampFormat = GetSetting<string>("TimestampFormat");
            if (string.IsNullOrWhiteSpace(timestampFormat))
            {
                warnings.Add(new ValidationWarning("TimestampFormat is empty, using default format", "TimestampFormat"));
            }

            try
            {
                DateTime.Now.ToString(timestampFormat);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError($"Invalid timestamp format: {ex.Message}", "TimestampFormat"));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "KeyValueFormatter", warnings)
                : ValidationResult.Success("KeyValueFormatter", warnings);
        }

        /// <inheritdoc />
        public void Configure(IReadOnlyDictionary<FixedString32Bytes, object> settings, 
            FixedString64Bytes correlationId = default)
        {
            if (settings == null) return;

            foreach (var setting in settings)
            {
                _settings[setting.Key] = setting.Value;
            }

            if (settings.ContainsKey("IsEnabled"))
            {
                _isEnabled = GetSetting<bool>("IsEnabled");
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<FixedString32Bytes, object> GetSettings()
        {
            return _settings;
        }

        /// <inheritdoc />
        public bool CanHandle(LogFormat format)
        {
            return format == LogFormat.KeyValue;
        }

        private string FormatValue(string value, bool quoteValues, string quoteChar, bool escapeQuotes)
        {
            if (string.IsNullOrEmpty(value))
            {
                return quoteValues ? $"{quoteChar}{quoteChar}" : "";
            }

            if (escapeQuotes && value.Contains(quoteChar))
            {
                value = value.Replace(quoteChar, "\\" + quoteChar);
            }

            // Also escape newlines and tabs for key-value format
            value = value.Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");

            return quoteValues ? $"{quoteChar}{value}{quoteChar}" : value;
        }

        private T GetSetting<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }
    }
}