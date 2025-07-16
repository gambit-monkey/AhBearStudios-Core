using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// Common Event LogFormat (CEF) formatter for security information and event management.
    /// Structured format suitable for SIEM systems and security monitoring.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class CefFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Cef";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Cef;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the CefFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public CefFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["Version"] = 0,
                ["DeviceVendor"] = "AhBearStudios",
                ["DeviceProduct"] = "Core.Logging",
                ["DeviceVersion"] = "1.0",
                ["SignatureId"] = "LOG_EVENT",
                ["Name"] = "Log Event",
                ["Severity"] = 1,
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = true,
                ["IncludeMachineInfo"] = true,
                ["IncludeException"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["UseUtcTimestamp"] = true,
                ["MaxExtensionLength"] = 2048
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var sb = new StringBuilder();

            // CEF Header: CEF:Version|Device Vendor|Device Product|Device Version|Signature ID|Name|Severity|Extension
            sb.Append($"CEF:{GetSetting<int>("Version")}");
            sb.Append($"|{EscapeHeaderField(GetSetting<string>("DeviceVendor"))}");
            sb.Append($"|{EscapeHeaderField(GetSetting<string>("DeviceProduct"))}");
            sb.Append($"|{EscapeHeaderField(GetSetting<string>("DeviceVersion"))}");
            sb.Append($"|{EscapeHeaderField(GetSetting<string>("SignatureId"))}");
            sb.Append($"|{EscapeHeaderField(GetSetting<string>("Name"))}");
            sb.Append($"|{GetCefSeverity(entry.Level)}");
            sb.Append($"|{BuildExtension(entry, correlationId)}");

            return sb.ToString();
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

            var version = GetSetting<int>("Version");
            if (version < 0)
            {
                errors.Add(new ValidationError("Version cannot be negative", "Version"));
            }

            var deviceVendor = GetSetting<string>("DeviceVendor");
            if (string.IsNullOrWhiteSpace(deviceVendor))
            {
                errors.Add(new ValidationError("DeviceVendor cannot be empty", "DeviceVendor"));
            }

            var deviceProduct = GetSetting<string>("DeviceProduct");
            if (string.IsNullOrWhiteSpace(deviceProduct))
            {
                errors.Add(new ValidationError("DeviceProduct cannot be empty", "DeviceProduct"));
            }

            var deviceVersion = GetSetting<string>("DeviceVersion");
            if (string.IsNullOrWhiteSpace(deviceVersion))
            {
                errors.Add(new ValidationError("DeviceVersion cannot be empty", "DeviceVersion"));
            }

            var signatureId = GetSetting<string>("SignatureId");
            if (string.IsNullOrWhiteSpace(signatureId))
            {
                errors.Add(new ValidationError("SignatureId cannot be empty", "SignatureId"));
            }

            var name = GetSetting<string>("Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new ValidationError("Name cannot be empty", "Name"));
            }

            var severity = GetSetting<int>("Severity");
            if (severity < 0 || severity > 10)
            {
                errors.Add(new ValidationError("Severity must be between 0 and 10", "Severity"));
            }

            var maxExtensionLength = GetSetting<int>("MaxExtensionLength");
            if (maxExtensionLength < 0)
            {
                errors.Add(new ValidationError("MaxExtensionLength cannot be negative", "MaxExtensionLength"));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "CefFormatter", warnings)
                : ValidationResult.Success("CefFormatter", warnings);
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
            return format == LogFormat.Cef;
        }

        private string BuildExtension(LogEntry entry, FixedString64Bytes correlationId)
        {
            var extensions = new List<string>();

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                // CEF timestamp format: MMM dd yyyy HH:mm:ss
                var formattedTime = timestamp.ToString("MMM dd yyyy HH:mm:ss");
                extensions.Add($"rt={formattedTime}");
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                extensions.Add($"cs1Label=LogLevel cs1={entry.Level}");
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                extensions.Add($"cs2Label=Channel cs2={EscapeExtensionValue(entry.Channel.ToString())}");
            }

            // Message is the primary field
            extensions.Add($"msg={EscapeExtensionValue(entry.Message.ToString())}");

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                if (!string.IsNullOrEmpty(corrId))
                {
                    extensions.Add($"cs3Label=CorrelationId cs3={EscapeExtensionValue(corrId)}");
                }
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                extensions.Add($"cs4Label=ThreadId cs4={entry.ThreadId}");
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                extensions.Add($"cs5Label=ThreadName cs5={EscapeExtensionValue(threadName)}");
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                extensions.Add($"dhost={EscapeExtensionValue(entry.MachineName.ToString())}");
                extensions.Add($"cs6Label=InstanceId cs6={EscapeExtensionValue(entry.InstanceId.ToString())}");
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                extensions.Add($"cn1Label=ExceptionType cn1={EscapeExtensionValue(entry.Exception.GetType().Name)}");
                extensions.Add($"cn2Label=ExceptionMessage cn2={EscapeExtensionValue(entry.Exception.Message)}");
                
                if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    var stackTrace = entry.Exception.StackTrace;
                    var maxLength = GetSetting<int>("MaxExtensionLength");
                    if (maxLength > 0 && stackTrace.Length > maxLength)
                    {
                        stackTrace = stackTrace.Substring(0, maxLength) + "...";
                    }
                    extensions.Add($"cn3Label=StackTrace cn3={EscapeExtensionValue(stackTrace)}");
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
                        extensions.Add($"cfp1Label=CpuUsage cfp1={cpuMetrics.LastOrDefault().Value:F1}");
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        extensions.Add($"cfp2Label=MemoryUsage cfp2={memoryMetrics.LastOrDefault().Value / 1024 / 1024:F1}");
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        extensions.Add($"cfp3Label=ProcessingTime cfp3={processingTimeMetrics.LastOrDefault().Value:F2}");
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            // Add entry ID
            extensions.Add($"externalId={entry.Id}");

            var result = string.Join(" ", extensions);
            var maxExtensionLength = GetSetting<int>("MaxExtensionLength");
            if (maxExtensionLength > 0 && result.Length > maxExtensionLength)
            {
                result = result.Substring(0, maxExtensionLength) + "...";
            }

            return result;
        }

        private int GetCefSeverity(LogLevel level)
        {
            return level switch
            {
                LogLevel.Critical => 10,
                LogLevel.Error => 8,
                LogLevel.Warning => 6,
                LogLevel.Info => 4,
                LogLevel.Debug => 2,
                LogLevel.Trace => 1,
                _ => 4
            };
        }

        private string EscapeHeaderField(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value.Replace("\\", "\\\\")
                        .Replace("|", "\\|");
        }

        private string EscapeExtensionValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value.Replace("\\", "\\\\")
                        .Replace("=", "\\=")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
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