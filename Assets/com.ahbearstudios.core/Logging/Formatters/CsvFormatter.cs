using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Common.Models;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// CSV formatter for tabular log output.
    /// Structured format suitable for data analysis and spreadsheet import.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class CsvFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Csv";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Csv;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the CsvFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public CsvFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["Delimiter"] = ",",
                ["QuoteChar"] = "\"",
                ["IncludeHeaders"] = true,
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = false,
                ["IncludeMachineInfo"] = false,
                ["IncludeException"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["TimestampFormat"] = "yyyy-MM-dd HH:mm:ss.fff",
                ["UseUtcTimestamp"] = true,
                ["EscapeQuotes"] = true
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var delimiter = GetSetting<string>("Delimiter");
            var quoteChar = GetSetting<string>("QuoteChar");
            var escapeQuotes = GetSetting<bool>("EscapeQuotes");
            var values = new List<string>();

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                var timestampFormat = GetSetting<string>("TimestampFormat");
                values.Add(EscapeValue(timestamp.ToString(timestampFormat), quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                values.Add(EscapeValue(entry.Level.ToString(), quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                values.Add(EscapeValue(entry.Channel.ToString(), quoteChar, escapeQuotes));
            }

            values.Add(EscapeValue(entry.Message.ToString(), quoteChar, escapeQuotes));

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                values.Add(EscapeValue(corrId, quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                values.Add(EscapeValue(entry.ThreadId.ToString(), quoteChar, escapeQuotes));
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                values.Add(EscapeValue(threadName, quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                values.Add(EscapeValue(entry.MachineName.ToString(), quoteChar, escapeQuotes));
                values.Add(EscapeValue(entry.InstanceId.ToString(), quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeException"))
            {
                if (entry.Exception != null)
                {
                    values.Add(EscapeValue(entry.Exception.GetType().Name, quoteChar, escapeQuotes));
                    values.Add(EscapeValue(entry.Exception.Message, quoteChar, escapeQuotes));
                }
                else
                {
                    values.Add(EscapeValue("", quoteChar, escapeQuotes));
                    values.Add(EscapeValue("", quoteChar, escapeQuotes));
                }
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics"))
            {
                if (_profilerService != null)
                {
                    try
                    {
                        var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                        values.Add(EscapeValue(cpuMetrics?.LastOrDefault().Value.ToString("F1") ?? "", quoteChar, escapeQuotes));
                        
                        var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                        values.Add(EscapeValue(memoryMetrics != null ? (memoryMetrics.LastOrDefault().Value / 1024 / 1024).ToString("F1") : "", quoteChar, escapeQuotes));
                        
                        var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                        values.Add(EscapeValue(processingTimeMetrics?.LastOrDefault().Value.ToString("F2") ?? "", quoteChar, escapeQuotes));
                    }
                    catch
                    {
                        // Silently ignore profiler errors and add empty values
                        values.Add(EscapeValue("", quoteChar, escapeQuotes));
                        values.Add(EscapeValue("", quoteChar, escapeQuotes));
                        values.Add(EscapeValue("", quoteChar, escapeQuotes));
                    }
                }
                else
                {
                    // No profiler service available, add empty values
                    values.Add(EscapeValue("", quoteChar, escapeQuotes));
                    values.Add(EscapeValue("", quoteChar, escapeQuotes));
                    values.Add(EscapeValue("", quoteChar, escapeQuotes));
                }
            }

            return string.Join(delimiter, values);
        }

        /// <inheritdoc />
        public IEnumerable<string> FormatBatch(IReadOnlyCollection<LogEntry> entries, 
            FixedString64Bytes correlationId = default)
        {
            var results = new List<string>();

            // Add header if enabled and this is the first batch
            if (GetSetting<bool>("IncludeHeaders"))
            {
                results.Add(GetHeader());
            }

            results.AddRange(entries.Select(entry => Format(entry, correlationId)));
            return results;
        }

        /// <inheritdoc />
        public ValidationResult Validate(FixedString64Bytes correlationId = default)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();

            var delimiter = GetSetting<string>("Delimiter");
            if (string.IsNullOrEmpty(delimiter))
            {
                errors.Add(new ValidationError("Delimiter cannot be empty", "Delimiter"));
            }

            var quoteChar = GetSetting<string>("QuoteChar");
            if (string.IsNullOrEmpty(quoteChar))
            {
                errors.Add(new ValidationError("QuoteChar cannot be empty", "QuoteChar"));
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
                ? ValidationResult.Failure(errors, "CsvFormatter", warnings)
                : ValidationResult.Success("CsvFormatter", warnings);
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
            return format == LogFormat.Csv;
        }

        private string GetHeader()
        {
            var delimiter = GetSetting<string>("Delimiter");
            var quoteChar = GetSetting<string>("QuoteChar");
            var escapeQuotes = GetSetting<bool>("EscapeQuotes");
            var headers = new List<string>();

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                headers.Add(EscapeValue("Timestamp", quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                headers.Add(EscapeValue("Level", quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                headers.Add(EscapeValue("Channel", quoteChar, escapeQuotes));
            }

            headers.Add(EscapeValue("Message", quoteChar, escapeQuotes));

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                headers.Add(EscapeValue("CorrelationId", quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                headers.Add(EscapeValue("ThreadId", quoteChar, escapeQuotes));
                headers.Add(EscapeValue("ThreadName", quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                headers.Add(EscapeValue("MachineName", quoteChar, escapeQuotes));
                headers.Add(EscapeValue("InstanceId", quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludeException"))
            {
                headers.Add(EscapeValue("ExceptionType", quoteChar, escapeQuotes));
                headers.Add(EscapeValue("ExceptionMessage", quoteChar, escapeQuotes));
            }

            if (GetSetting<bool>("IncludePerformanceMetrics"))
            {
                headers.Add(EscapeValue("CpuUsage", quoteChar, escapeQuotes));
                headers.Add(EscapeValue("MemoryUsageMB", quoteChar, escapeQuotes));
                headers.Add(EscapeValue("ProcessingTimeMs", quoteChar, escapeQuotes));
            }

            return string.Join(delimiter, headers);
        }

        private string EscapeValue(string value, string quoteChar, bool escapeQuotes)
        {
            if (string.IsNullOrEmpty(value))
            {
                return $"{quoteChar}{quoteChar}";
            }

            if (escapeQuotes && value.Contains(quoteChar))
            {
                value = value.Replace(quoteChar, quoteChar + quoteChar);
            }

            return $"{quoteChar}{value}{quoteChar}";
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