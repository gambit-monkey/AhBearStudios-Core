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
    /// Syslog formatter compliant with RFC 5424.
    /// Standard format for Unix/Linux system logging and log forwarding.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class SyslogFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Syslog";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Syslog;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the SyslogFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public SyslogFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["Facility"] = 16, // Local use 0 (RFC 5424)
                ["UseUtcTimestamp"] = true,
                ["IncludeHostname"] = true,
                ["IncludeAppName"] = true,
                ["IncludeProcId"] = true,
                ["IncludeMsgId"] = true,
                ["IncludeStructuredData"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["AppName"] = "AhBearStudios",
                ["Version"] = 1, // RFC 5424 version
                ["MaxMessageLength"] = 1024
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var facility = GetSetting<int>("Facility");
            var version = GetSetting<int>("Version");
            var useUtc = GetSetting<bool>("UseUtcTimestamp");
            var includeHostname = GetSetting<bool>("IncludeHostname");
            var includeAppName = GetSetting<bool>("IncludeAppName");
            var includeProcId = GetSetting<bool>("IncludeProcId");
            var includeMsgId = GetSetting<bool>("IncludeMsgId");
            var includeStructuredData = GetSetting<bool>("IncludeStructuredData");
            var appName = GetSetting<string>("AppName");
            var maxMessageLength = GetSetting<int>("MaxMessageLength");

            var sb = new StringBuilder();

            // Priority = Facility * 8 + Severity
            var severity = GetSyslogSeverity(entry.Level);
            var priority = facility * 8 + severity;
            sb.Append($"<{priority}>");

            // Version
            sb.Append($"{version} ");

            // Timestamp (RFC 3339 format)
            var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
            sb.Append($"{timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} ");

            // Hostname
            if (includeHostname)
            {
                var hostname = entry.MachineName.ToString();
                if (string.IsNullOrEmpty(hostname))
                {
                    hostname = "-";
                }
                sb.Append($"{hostname} ");
            }
            else
            {
                sb.Append("- ");
            }

            // App-Name
            if (includeAppName)
            {
                sb.Append($"{appName ?? "-"} ");
            }
            else
            {
                sb.Append("- ");
            }

            // ProcID
            if (includeProcId)
            {
                sb.Append($"{entry.ThreadId} ");
            }
            else
            {
                sb.Append("- ");
            }

            // MsgID
            if (includeMsgId)
            {
                var msgId = entry.Channel.ToString();
                if (string.IsNullOrEmpty(msgId))
                {
                    msgId = "-";
                }
                sb.Append($"{msgId} ");
            }
            else
            {
                sb.Append("- ");
            }

            // Structured Data
            if (includeStructuredData)
            {
                sb.Append(BuildStructuredData(entry, correlationId));
            }
            else
            {
                sb.Append("- ");
            }

            // Message
            var message = entry.Message.ToString();
            if (maxMessageLength > 0 && message.Length > maxMessageLength)
            {
                message = message.Substring(0, maxMessageLength) + "...";
            }

            // Add exception details if present
            if (entry.Exception != null)
            {
                message += $" Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}";
            }

            sb.Append(message);

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

            var facility = GetSetting<int>("Facility");
            if (facility < 0 || facility > 23)
            {
                errors.Add(new ValidationError("Facility must be between 0 and 23", "Facility"));
            }

            var version = GetSetting<int>("Version");
            if (version != 1)
            {
                warnings.Add(new ValidationWarning("Version should be 1 for RFC 5424 compliance", "Version"));
            }

            var maxMessageLength = GetSetting<int>("MaxMessageLength");
            if (maxMessageLength < 0)
            {
                errors.Add(new ValidationError("MaxMessageLength cannot be negative", "MaxMessageLength"));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "SyslogFormatter", warnings)
                : ValidationResult.Success("SyslogFormatter", warnings);
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
            return format == LogFormat.Syslog;
        }

        private int GetSyslogSeverity(LogLevel level)
        {
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

        private string BuildStructuredData(LogEntry entry, FixedString64Bytes correlationId)
        {
            var sb = new StringBuilder();
            sb.Append("[ahbear@32473"); // Private Enterprise Number for structured data

            // Add correlation ID if available
            var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
            if (!string.IsNullOrEmpty(corrId))
            {
                sb.Append($" correlationId=\"{EscapeStructuredDataValue(corrId)}\"");
            }

            // Add thread name if available
            var currentThread = Thread.CurrentThread;
            var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                ? currentThread.Name 
                : $"Thread-{entry.ThreadId}";
            if (!string.IsNullOrEmpty(threadName))
            {
                sb.Append($" threadName=\"{EscapeStructuredDataValue(threadName)}\"");
            }

            // Add instance ID if available
            var instanceId = entry.InstanceId.ToString();
            if (!string.IsNullOrEmpty(instanceId))
            {
                sb.Append($" instanceId=\"{EscapeStructuredDataValue(instanceId)}\"");
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        sb.Append($" cpuUsage=\"{cpuMetrics.LastOrDefault().Value:F1}\"");
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        sb.Append($" memoryUsageMB=\"{memoryMetrics.LastOrDefault().Value / 1024 / 1024:F1}\"");
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        sb.Append($" processingTimeMs=\"{processingTimeMetrics.LastOrDefault().Value:F2}\"");
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            sb.Append("] ");
            return sb.ToString();
        }

        private string EscapeStructuredDataValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("]", "\\]");
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