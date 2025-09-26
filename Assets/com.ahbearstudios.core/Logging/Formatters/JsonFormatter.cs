using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AhBearStudios.Core.Common.Models;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// JSON formatter for structured log output.
    /// Machine-readable format suitable for log aggregation and analysis tools.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class JsonFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;
        private JsonSerializerSettings _jsonOptions;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Json";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Json;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the JsonFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public JsonFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["PrettyPrint"] = false,
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = false,
                ["IncludeMachineInfo"] = false,
                ["IncludeException"] = true,
                ["IncludeStackTrace"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["TimestampFormat"] = "yyyy-MM-ddTHH:mm:ss.fffZ",
                ["UseUtcTimestamp"] = true
            };

            InitializeJsonOptions();
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var logObject = new Dictionary<string, object>();

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                var timestampFormat = GetSetting<string>("TimestampFormat");
                logObject["timestamp"] = timestamp.ToString(timestampFormat);
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                logObject["level"] = entry.Level.ToString().ToLower();
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                logObject["channel"] = entry.Channel.ToString();
            }

            logObject["message"] = entry.Message.ToString();

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                if (!correlationId.IsEmpty)
                {
                    logObject["correlationId"] = correlationId.ToString();
                }
                else if (!entry.CorrelationId.IsEmpty)
                {
                    logObject["correlationId"] = entry.CorrelationId.ToString();
                }
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                logObject["threadId"] = entry.ThreadId;
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                if (!string.IsNullOrEmpty(currentThread.Name))
                {
                    logObject["threadName"] = currentThread.Name;
                }
                else
                {
                    logObject["threadName"] = $"Thread-{entry.ThreadId}";
                }
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                logObject["machineName"] = entry.MachineName.ToString();
                logObject["instanceId"] = entry.InstanceId.ToString();
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                var exceptionObject = new Dictionary<string, object>
                {
                    ["type"] = entry.Exception.GetType().Name,
                    ["message"] = entry.Exception.Message
                };

                if (GetSetting<bool>("IncludeStackTrace") && !string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    exceptionObject["stackTrace"] = entry.Exception.StackTrace;
                }

                logObject["exception"] = exceptionObject;
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var performanceMetrics = new Dictionary<string, object>();
                    
                    // Query basic performance metrics
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        performanceMetrics["cpuUsage"] = cpuMetrics.FirstOrDefault().Value;
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        performanceMetrics["memoryUsage"] = memoryMetrics.FirstOrDefault().Value;
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        performanceMetrics["processingTime"] = processingTimeMetrics.FirstOrDefault().Value;
                    }
                    
                    if (performanceMetrics.Count > 0)
                    {
                        logObject["performanceMetrics"] = performanceMetrics;
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            return JsonConvert.SerializeObject(logObject, _jsonOptions);
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
                ? ValidationResult.Failure(errors, "JsonFormatter", warnings)
                : ValidationResult.Success("JsonFormatter", warnings);
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
            return format == LogFormat.Json;
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