using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// Structured formatter for database and advanced logging scenarios.
    /// Provides comprehensive structured data representation with flexible output formats.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class StructuredFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;
        private JsonSerializerSettings _jsonOptions;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Structured";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Json; // Default to JSON for structured data

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the StructuredFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public StructuredFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["OutputFormat"] = "json", // json, xml, keyvalue
                ["PrettyPrint"] = false,
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = true,
                ["IncludeMachineInfo"] = true,
                ["IncludeException"] = true,
                ["IncludeStackTrace"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["IncludeCustomProperties"] = true,
                ["TimestampFormat"] = "yyyy-MM-ddTHH:mm:ss.fffZ",
                ["UseUtcTimestamp"] = true,
                ["MaxDepth"] = 10,
                ["MaxStringLength"] = 1000
            };

            InitializeJsonOptions();
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var outputFormat = GetSetting<string>("OutputFormat");
            var structuredData = BuildStructuredData(entry, correlationId);

            return outputFormat.ToLower() switch
            {
                "json" => JsonConvert.SerializeObject(structuredData, _jsonOptions),
                "xml" => ConvertToXml(structuredData),
                "keyvalue" => ConvertToKeyValue(structuredData),
                _ => JsonConvert.SerializeObject(structuredData, _jsonOptions)
            };
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

            var outputFormat = GetSetting<string>("OutputFormat");
            var validFormats = new[] { "json", "xml", "keyvalue" };
            if (!validFormats.Contains(outputFormat?.ToLower()))
            {
                errors.Add(new ValidationError($"Invalid output format: {outputFormat}. Valid formats: {string.Join(", ", validFormats)}", "OutputFormat"));
            }

            var maxDepth = GetSetting<int>("MaxDepth");
            if (maxDepth < 1 || maxDepth > 50)
            {
                errors.Add(new ValidationError("MaxDepth must be between 1 and 50", "MaxDepth"));
            }

            var maxStringLength = GetSetting<int>("MaxStringLength");
            if (maxStringLength < 1)
            {
                errors.Add(new ValidationError("MaxStringLength must be greater than 0", "MaxStringLength"));
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
                ? ValidationResult.Failure(errors, "StructuredFormatter", warnings)
                : ValidationResult.Success("StructuredFormatter", warnings);
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
            return format == LogFormat.Json || format == LogFormat.Xml || format == LogFormat.KeyValue;
        }

        private Dictionary<string, object> BuildStructuredData(LogEntry entry, FixedString64Bytes correlationId)
        {
            var data = new Dictionary<string, object>();

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                var timestampFormat = GetSetting<string>("TimestampFormat");
                data["timestamp"] = timestamp.ToString(timestampFormat);
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                data["level"] = entry.Level.ToString().ToLower();
                data["levelValue"] = (int)entry.Level;
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                data["channel"] = entry.Channel.ToString();
            }

            data["message"] = TruncateString(entry.Message.ToString());

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                if (!string.IsNullOrEmpty(corrId))
                {
                    data["correlationId"] = corrId;
                }
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                    
                data["thread"] = new Dictionary<string, object>
                {
                    ["id"] = entry.ThreadId,
                    ["name"] = threadName
                };
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                data["machine"] = new Dictionary<string, object>
                {
                    ["name"] = entry.MachineName.ToString(),
                    ["instanceId"] = entry.InstanceId.ToString()
                };
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                var exceptionData = new Dictionary<string, object>
                {
                    ["type"] = entry.Exception.GetType().Name,
                    ["message"] = TruncateString(entry.Exception.Message),
                    ["source"] = entry.Exception.Source
                };

                if (GetSetting<bool>("IncludeStackTrace") && !string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    exceptionData["stackTrace"] = TruncateString(entry.Exception.StackTrace);
                }

                // Add inner exception if present
                if (entry.Exception.InnerException != null)
                {
                    exceptionData["innerException"] = new Dictionary<string, object>
                    {
                        ["type"] = entry.Exception.InnerException.GetType().Name,
                        ["message"] = TruncateString(entry.Exception.InnerException.Message)
                    };
                }

                data["exception"] = exceptionData;
            }

            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var performanceData = new Dictionary<string, object>();
                    
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        performanceData["cpuUsage"] = cpuMetrics.LastOrDefault().Value;
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        performanceData["memoryUsage"] = memoryMetrics.LastOrDefault().Value;
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        performanceData["processingTime"] = processingTimeMetrics.LastOrDefault().Value;
                    }
                    
                    if (performanceData.Count > 0)
                    {
                        data["performance"] = performanceData;
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            // Add metadata
            data["metadata"] = new Dictionary<string, object>
            {
                ["id"] = entry.Id.ToString(),
                ["formatter"] = "StructuredFormatter",
                ["version"] = "1.0",
                ["schemaVersion"] = "1.0"
            };

            return data;
        }

        private string ConvertToXml(Dictionary<string, object> data)
        {
            // Simple XML conversion - in a real implementation, you might want to use XDocument
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<LogEntry>");
            
            foreach (var kvp in data)
            {
                AppendXmlElement(sb, kvp.Key, kvp.Value, 1);
            }
            
            sb.AppendLine("</LogEntry>");
            return sb.ToString();
        }

        private void AppendXmlElement(System.Text.StringBuilder sb, string key, object value, int depth)
        {
            var indent = new string(' ', depth * 2);
            
            if (value is Dictionary<string, object> dict)
            {
                sb.AppendLine($"{indent}<{key}>");
                foreach (var kvp in dict)
                {
                    AppendXmlElement(sb, kvp.Key, kvp.Value, depth + 1);
                }
                sb.AppendLine($"{indent}</{key}>");
            }
            else
            {
                var escapedValue = System.Security.SecurityElement.Escape(value?.ToString() ?? "");
                sb.AppendLine($"{indent}<{key}>{escapedValue}</{key}>");
            }
        }

        private string ConvertToKeyValue(Dictionary<string, object> data)
        {
            var pairs = new List<string>();
            FlattenDictionary(data, "", pairs);
            return string.Join(" ", pairs);
        }

        private void FlattenDictionary(Dictionary<string, object> dict, string prefix, List<string> pairs)
        {
            foreach (var kvp in dict)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                
                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    FlattenDictionary(nestedDict, key, pairs);
                }
                else
                {
                    var value = kvp.Value?.ToString() ?? "";
                    var escapedValue = value.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t");
                    pairs.Add($"{key}=\"{escapedValue}\"");
                }
            }
        }

        private string TruncateString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var maxLength = GetSetting<int>("MaxStringLength");
            if (maxLength > 0 && value.Length > maxLength)
            {
                return value.Substring(0, maxLength) + "...";
            }

            return value;
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