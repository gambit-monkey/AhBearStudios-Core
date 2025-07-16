using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Unity.Collections;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Profiling;

namespace AhBearStudios.Core.Logging.Formatters
{
    /// <summary>
    /// XML formatter for hierarchical log output.
    /// Structured format suitable for systems requiring XML-based logging.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class XmlFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Xml";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Xml;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the XmlFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public XmlFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["PrettyPrint"] = true,
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
                ["UseUtcTimestamp"] = true,
                ["RootElementName"] = "LogEntry",
                ["IncludeXmlDeclaration"] = true
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            var rootElementName = GetSetting<string>("RootElementName");
            var logElement = new XElement(rootElementName);

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                var timestampFormat = GetSetting<string>("TimestampFormat");
                logElement.Add(new XElement("Timestamp", timestamp.ToString(timestampFormat)));
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                logElement.Add(new XElement("Level", entry.Level.ToString()));
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                logElement.Add(new XElement("Channel", entry.Channel.ToString()));
            }

            logElement.Add(new XElement("Message", entry.Message.ToString()));

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                if (!string.IsNullOrEmpty(corrId))
                {
                    logElement.Add(new XElement("CorrelationId", corrId));
                }
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                var threadElement = new XElement("Thread");
                threadElement.Add(new XElement("Id", entry.ThreadId));
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                threadElement.Add(new XElement("Name", threadName));
                logElement.Add(threadElement);
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                var machineElement = new XElement("Machine");
                machineElement.Add(new XElement("Name", entry.MachineName.ToString()));
                machineElement.Add(new XElement("InstanceId", entry.InstanceId.ToString()));
                logElement.Add(machineElement);
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                var exceptionElement = new XElement("Exception");
                exceptionElement.Add(new XElement("Type", entry.Exception.GetType().Name));
                exceptionElement.Add(new XElement("Message", entry.Exception.Message));

                if (GetSetting<bool>("IncludeStackTrace") && !string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    exceptionElement.Add(new XElement("StackTrace", new XCData(entry.Exception.StackTrace)));
                }

                logElement.Add(exceptionElement);
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var performanceElement = new XElement("Performance");
                    
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        performanceElement.Add(new XElement("CpuUsage", cpuMetrics.LastOrDefault().Value.ToString("F1")));
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        performanceElement.Add(new XElement("MemoryUsageMB", (memoryMetrics.LastOrDefault().Value / 1024 / 1024).ToString("F1")));
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        performanceElement.Add(new XElement("ProcessingTimeMs", processingTimeMetrics.LastOrDefault().Value.ToString("F2")));
                    }
                    
                    if (performanceElement.HasElements)
                    {
                        logElement.Add(performanceElement);
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            var document = new XDocument(logElement);
            
            var settings = new XmlWriterSettings
            {
                Indent = GetSetting<bool>("PrettyPrint"),
                OmitXmlDeclaration = !GetSetting<bool>("IncludeXmlDeclaration"),
                Encoding = Encoding.UTF8
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            document.WriteTo(xmlWriter);
            xmlWriter.Flush();
            
            return stringWriter.ToString();
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

            var rootElementName = GetSetting<string>("RootElementName");
            if (string.IsNullOrWhiteSpace(rootElementName))
            {
                errors.Add(new ValidationError("RootElementName cannot be empty", "RootElementName"));
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
                ? ValidationResult.Failure(errors, "XmlFormatter", warnings)
                : ValidationResult.Success("XmlFormatter", warnings);
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
            return format == LogFormat.Xml;
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