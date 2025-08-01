using System;
using System.Collections.Generic;
using System.IO;
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
    /// Binary formatter for high-performance logging scenarios.
    /// Compact format suitable for high-throughput logging with minimal overhead.
    /// Supports optional performance metrics through IProfilerService integration.
    /// </summary>
    public sealed class BinaryFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Binary";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Binary;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the BinaryFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public BinaryFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
                ["Compression"] = false,
                ["IncludeTimestamp"] = true,
                ["IncludeLevel"] = true,
                ["IncludeChannel"] = true,
                ["IncludeCorrelationId"] = true,
                ["IncludeThreadInfo"] = true,
                ["IncludeMachineInfo"] = true,
                ["IncludeException"] = true,
                ["IncludeStackTrace"] = true,
                ["IncludePerformanceMetrics"] = false,
                ["UseUtcTimestamp"] = true,
                ["Encoding"] = "UTF-8",
                ["Version"] = 1
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            // Write version header
            writer.Write(GetSetting<int>("Version"));

            // Write flags to indicate what data is included
            var flags = BuildFlags();
            writer.Write(flags);

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                writer.Write(timestamp.ToBinary());
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                writer.Write((int)entry.Level);
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                WriteFixedString(writer, entry.Channel);
            }

            WriteFixedString(writer, entry.Message);

            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId : entry.CorrelationId;
                WriteFixedString(writer, corrId);
            }

            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                writer.Write(entry.ThreadId);
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                WriteString(writer, threadName);
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                WriteFixedString(writer, entry.MachineName);
                WriteFixedString(writer, entry.InstanceId);
            }

            // Write exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                writer.Write(true); // Exception present flag
                WriteString(writer, entry.Exception.GetType().Name);
                WriteString(writer, entry.Exception.Message);
                WriteString(writer, entry.Exception.Source ?? "");

                if (GetSetting<bool>("IncludeStackTrace"))
                {
                    WriteString(writer, entry.Exception.StackTrace ?? "");
                }
            }
            else
            {
                writer.Write(false); // No exception flag
            }

            // Write performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                writer.Write(true); // Performance metrics present flag
                
                try
                {
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    writer.Write(cpuMetrics?.LastOrDefault().Value ?? 0.0);
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    writer.Write(memoryMetrics?.LastOrDefault().Value ?? 0L);
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    writer.Write(processingTimeMetrics?.LastOrDefault().Value ?? 0.0);
                }
                catch
                {
                    // Write default values if profiler fails
                    writer.Write(0.0); // CPU usage
                    writer.Write(0L);  // Memory usage
                    writer.Write(0.0); // Processing time
                }
            }
            else
            {
                writer.Write(false); // No performance metrics flag
            }

            // Write entry ID
            writer.Write(entry.Id.ToByteArray());

            // Convert to base64 string for text-based transport
            var binaryData = stream.ToArray();
            return Convert.ToBase64String(binaryData);
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
            if (version < 1)
            {
                errors.Add(new ValidationError("Version must be greater than 0", "Version"));
            }

            var encoding = GetSetting<string>("Encoding");
            if (string.IsNullOrWhiteSpace(encoding))
            {
                warnings.Add(new ValidationWarning("Encoding is empty, using default UTF-8", "Encoding"));
            }

            try
            {
                Encoding.GetEncoding(encoding);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError($"Invalid encoding: {ex.Message}", "Encoding"));
            }

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "BinaryFormatter", warnings)
                : ValidationResult.Success("BinaryFormatter", warnings);
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
            return format == LogFormat.Binary;
        }

        private int BuildFlags()
        {
            int flags = 0;
            
            if (GetSetting<bool>("IncludeTimestamp")) flags |= 1;
            if (GetSetting<bool>("IncludeLevel")) flags |= 2;
            if (GetSetting<bool>("IncludeChannel")) flags |= 4;
            if (GetSetting<bool>("IncludeCorrelationId")) flags |= 8;
            if (GetSetting<bool>("IncludeThreadInfo")) flags |= 16;
            if (GetSetting<bool>("IncludeMachineInfo")) flags |= 32;
            if (GetSetting<bool>("IncludeException")) flags |= 64;
            if (GetSetting<bool>("IncludeStackTrace")) flags |= 128;
            if (GetSetting<bool>("UseUtcTimestamp")) flags |= 256;
            if (GetSetting<bool>("Compression")) flags |= 512;
            if (GetSetting<bool>("IncludePerformanceMetrics")) flags |= 1024;
            
            return flags;
        }

        private void WriteFixedString<T>(BinaryWriter writer, T fixedString) where T : struct
        {
            var str = fixedString.ToString();
            WriteString(writer, str);
        }

        private void WriteString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(0); // Length = 0
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
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