using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// Plain text formatter for human-readable log output.
    /// Suitable for console output and basic file logging.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class PlainTextFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "PlainText";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.PlainText;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the PlainTextFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public PlainTextFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = false,
                ["TimestampFormat"] = "yyyy-MM-dd HH:mm:ss.fff",
                ["MessageTemplate"] = "[{Timestamp}] [{Level}] [{Channel}] {Message}",
                ["IncludeException"] = true,
                ["IncludeStackTrace"] = true,
                ["IncludePerformanceMetrics"] = false
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var template = GetSetting<string>("MessageTemplate");
            var includeTimestamp = GetSetting<bool>("IncludeTimestamp");
            var includeLevel = GetSetting<bool>("IncludeLevel");
            var includeChannel = GetSetting<bool>("IncludeChannel");
            var includeCorrelationId = GetSetting<bool>("IncludeCorrelationId");
            var timestampFormat = GetSetting<string>("TimestampFormat");
            var includeException = GetSetting<bool>("IncludeException");
            var includeStackTrace = GetSetting<bool>("IncludeStackTrace");

            var result = template;

            if (includeTimestamp)
            {
                result = result.Replace("{Timestamp}", entry.Timestamp.ToString(timestampFormat));
            }

            if (includeLevel)
            {
                result = result.Replace("{Level}", entry.Level.ToString().ToUpper());
            }

            if (includeChannel)
            {
                result = result.Replace("{Channel}", entry.Channel.ToString());
            }

            result = result.Replace("{Message}", entry.Message.ToString());

            if (includeCorrelationId && !correlationId.IsEmpty)
            {
                result += $" [CorrelationId: {correlationId}]";
            }

            // Add exception details if present
            if (includeException && entry.Exception != null)
            {
                result += $"\nException: {entry.Exception.GetType().Name}: {entry.Exception.Message}";
                
                if (includeStackTrace && !string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    result += $"\nStack Trace:\n{entry.Exception.StackTrace}";
                }
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var perfMetrics = new StringBuilder();
                    
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        perfMetrics.Append($" CPU: {cpuMetrics.LastOrDefault().Value:F1}%");
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        perfMetrics.Append($" Memory: {memoryMetrics.LastOrDefault().Value / 1024 / 1024:F1}MB");
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        perfMetrics.Append($" Time: {processingTimeMetrics.LastOrDefault().Value:F2}ms");
                    }
                    
                    if (perfMetrics.Length > 0)
                    {
                        result += $" [{perfMetrics.ToString().TrimStart()}]";
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            return result;
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

            var messageTemplate = GetSetting<string>("MessageTemplate");
            if (string.IsNullOrWhiteSpace(messageTemplate))
            {
                errors.Add(new ValidationError("MessageTemplate cannot be empty", "MessageTemplate"));
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
                ? ValidationResult.Failure(errors, "PlainTextFormatter", warnings)
                : ValidationResult.Success("PlainTextFormatter", warnings);
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
            return format == LogFormat.PlainText;
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