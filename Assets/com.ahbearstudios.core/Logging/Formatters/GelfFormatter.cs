using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using Newtonsoft.Json.Serialization;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// GELF (Graylog Extended Log LogFormat) formatter for centralized logging.
    /// JSON-based format optimized for Graylog and similar log management systems.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class GelfFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;
        private JsonSerializerSettings _jsonOptions;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Gelf";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Gelf;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the GelfFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public GelfFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["Version"] = "1.1",
                ["Host"] = Environment.MachineName,
                ["Facility"] = "AhBearStudios.Core.Logging",
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = true,
                ["IncludeMachineInfo"] = true,
                ["IncludeException"] = true,
                ["IncludeStackTrace"] = true,
                ["UseUtcTimestamp"] = true,
                ["CompactOutput"] = true,
                ["MaxMessageLength"] = 0,
                ["IncludePerformanceMetrics"] = true
            };

            InitializeJsonOptions();
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var gelfMessage = new Dictionary<string, object>
            {
                ["version"] = GetSetting<string>("Version"),
                ["host"] = GetSetting<string>("Host"),
                ["short_message"] = TruncateMessage(entry.Message.ToString()),
                ["full_message"] = entry.Message.ToString(),
                ["level"] = GetGelfLevel(entry.Level),
                ["facility"] = GetSetting<string>("Facility")
            };

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                // GELF timestamp is Unix timestamp with decimal seconds
                var unixTimestamp = ((DateTimeOffset)timestamp).ToUnixTimeMilliseconds() / 1000.0;
                gelfMessage["timestamp"] = unixTimestamp;
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                gelfMessage["_channel"] = entry.Channel.ToString();
            }

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                if (!string.IsNullOrEmpty(corrId))
                {
                    gelfMessage["_correlation_id"] = corrId;
                }
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                gelfMessage["_thread_id"] = entry.ThreadId;
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                gelfMessage["_thread_name"] = threadName;
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                gelfMessage["_machine_name"] = entry.MachineName.ToString();
                gelfMessage["_instance_id"] = entry.InstanceId.ToString();
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                gelfMessage["_exception_type"] = entry.Exception.GetType().Name;
                gelfMessage["_exception_message"] = entry.Exception.Message;
                gelfMessage["_exception_source"] = entry.Exception.Source ?? "";

                if (GetSetting<bool>("IncludeStackTrace") && !string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    gelfMessage["_exception_stack_trace"] = entry.Exception.StackTrace;
                }

                // Add inner exception if present
                if (entry.Exception.InnerException != null)
                {
                    gelfMessage["_inner_exception_type"] = entry.Exception.InnerException.GetType().Name;
                    gelfMessage["_inner_exception_message"] = entry.Exception.InnerException.Message;
                }
            }

            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        gelfMessage["_cpu_usage"] = cpuMetrics.LastOrDefault().Value;
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        gelfMessage["_memory_usage"] = memoryMetrics.LastOrDefault().Value;
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        gelfMessage["_processing_time_ms"] = processingTimeMetrics.LastOrDefault().Value;
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            // Add entry metadata
            gelfMessage["_entry_id"] = entry.Id.ToString();
            gelfMessage["_log_level_name"] = entry.Level.ToString();
            gelfMessage["_formatter"] = "GelfFormatter";

            return JsonConvert.SerializeObject(gelfMessage, _jsonOptions);
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

            var version = GetSetting<string>("Version");
            if (string.IsNullOrWhiteSpace(version))
            {
                errors.Add(new ValidationError("Version cannot be empty", "Version"));
            }

            var host = GetSetting<string>("Host");
            if (string.IsNullOrWhiteSpace(host))
            {
                errors.Add(new ValidationError("Host cannot be empty", "Host"));
            }

            var facility = GetSetting<string>("Facility");
            if (string.IsNullOrWhiteSpace(facility))
            {
                warnings.Add(new ValidationWarning("Facility is empty, using default", "Facility"));
            }

            var maxMessageLength = GetSetting<int>("MaxMessageLength");
            if (maxMessageLength < 0)
            {
                errors.Add(new ValidationError("MaxMessageLength cannot be negative", "MaxMessageLength"));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "GelfFormatter", warnings)
                : ValidationResult.Success("GelfFormatter", warnings);
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

            InitializeJsonOptions();
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<FixedString32Bytes, object> GetSettings()
        {
            return _settings;
        }

        /// <inheritdoc />
        public bool CanHandle(LogFormat format)
        {
            return format == LogFormat.Gelf;
        }

        private int GetGelfLevel(LogLevel level)
        {
            // GELF levels are based on syslog levels
            return level switch
            {
                LogLevel.Critical => 2, // Critical
                LogLevel.Error => 3,    // Error
                LogLevel.Warning => 4,  // Warning
                LogLevel.Info => 6,     // Informational
                LogLevel.Debug => 7,    // Debug
                LogLevel.Trace => 7,    // Debug (trace maps to debug)
                _ => 6                  // Default to informational
            };
        }

        private string TruncateMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            var maxLength = GetSetting<int>("MaxMessageLength");
            if (maxLength > 0 && message.Length > maxLength)
            {
                return message.Substring(0, maxLength) + "...";
            }

            // For GELF, short_message should be a brief summary
            // If the message is longer than 250 characters, truncate for short_message
            if (message.Length > 250)
            {
                return message.Substring(0, 250) + "...";
            }

            return message;
        }

        private void InitializeJsonOptions()
        {
            _jsonOptions = new JsonSerializerSettings
            {
                Formatting = GetSetting<bool>("CompactOutput") ? Formatting.None : Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
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