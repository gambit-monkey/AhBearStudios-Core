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
    /// MessagePack formatter for efficient binary serialization.
    /// Compact binary format suitable for high-performance logging and transmission.
    /// Supports optional performance metrics through IProfilerService integration.
    /// Note: This is a basic implementation. For production use, consider using the MessagePack-CSharp library.
    /// </summary>
    public sealed class MessagePackFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "MessagePack";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.MessagePack;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the MessagePackFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public MessagePackFormatter(IProfilerService profilerService = null)
        {
            _profilerService = profilerService;
            _settings = new Dictionary<FixedString32Bytes, object>
            {
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
                ["Compression"] = false,
                ["Version"] = 1
            };
        }

        /// <inheritdoc />
        public string Format(LogEntry entry, FixedString64Bytes correlationId = default)
        {
            // This is a simplified MessagePack-like format
            // For production use, integrate with MessagePack-CSharp library
            var data = BuildMessagePackData(entry, correlationId);
            var serialized = SerializeToMessagePack(data);
            
            // Return as base64 for text-based transport
            return Convert.ToBase64String(serialized);
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

            // Add warning about simplified implementation
            warnings.Add(new ValidationWarning("This is a simplified MessagePack implementation. Consider using MessagePack-CSharp for production.", "Implementation"));

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "MessagePackFormatter", warnings)
                : ValidationResult.Success("MessagePackFormatter", warnings);
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
            return format == LogFormat.MessagePack;
        }

        private Dictionary<string, object> BuildMessagePackData(LogEntry entry, FixedString64Bytes correlationId)
        {
            var data = new Dictionary<string, object>
            {
                ["version"] = GetSetting<int>("Version"),
                ["formatter"] = "MessagePackFormatter"
            };

            if (GetSetting<bool>("IncludeTimestamp"))
            {
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                data["timestamp"] = timestamp.ToBinary();
            }

            if (GetSetting<bool>("IncludeLevel"))
            {
                data["level"] = (int)entry.Level;
                data["levelName"] = entry.Level.ToString();
            }

            if (GetSetting<bool>("IncludeChannel"))
            {
                data["channel"] = entry.Channel.ToString();
            }

            data["message"] = entry.Message.ToString();

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
                data["threadId"] = entry.ThreadId;
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                data["threadName"] = threadName;
            }

            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                data["machineName"] = entry.MachineName.ToString();
                data["instanceId"] = entry.InstanceId.ToString();
            }

            // Add exception details if present
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                var exceptionData = new Dictionary<string, object>
                {
                    ["type"] = entry.Exception.GetType().Name,
                    ["message"] = entry.Exception.Message,
                    ["source"] = entry.Exception.Source ?? ""
                };

                if (GetSetting<bool>("IncludeStackTrace") && !string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    exceptionData["stackTrace"] = entry.Exception.StackTrace;
                }

                data["exception"] = exceptionData;
            }

            // Add performance metrics if available and enabled
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        data["cpuUsage"] = cpuMetrics.LastOrDefault().Value;
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        data["memoryUsage"] = memoryMetrics.LastOrDefault().Value;
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        data["processingTime"] = processingTimeMetrics.LastOrDefault().Value;
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            // Add entry ID
            data["entryId"] = entry.Id.ToString();

            return data;
        }

        private byte[] SerializeToMessagePack(Dictionary<string, object> data)
        {
            // This is a simplified MessagePack serialization
            // For production, use MessagePack-CSharp library: MessagePackSerializer.Serialize(data)
            
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);

            // Write map header (simplified)
            writer.Write((byte)0x80); // Fixed map format
            writer.Write((byte)data.Count); // Number of key-value pairs

            foreach (var kvp in data)
            {
                // Write key
                WriteString(writer, kvp.Key);
                
                // Write value
                WriteValue(writer, kvp.Value);
            }

            return stream.ToArray();
        }

        private void WriteString(System.IO.BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write((byte)0xc0); // nil
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length <= 31)
            {
                writer.Write((byte)(0xa0 | bytes.Length)); // fixstr
            }
            else if (bytes.Length <= 255)
            {
                writer.Write((byte)0xd9); // str8
                writer.Write((byte)bytes.Length);
            }
            else if (bytes.Length <= 65535)
            {
                writer.Write((byte)0xda); // str16
                writer.Write((ushort)bytes.Length);
            }
            else
            {
                writer.Write((byte)0xdb); // str32
                writer.Write((uint)bytes.Length);
            }
            
            writer.Write(bytes);
        }

        private void WriteValue(System.IO.BinaryWriter writer, object value)
        {
            switch (value)
            {
                case null:
                    writer.Write((byte)0xc0); // nil
                    break;
                case bool b:
                    writer.Write((byte)(b ? 0xc3 : 0xc2)); // true/false
                    break;
                case int i:
                    if (i >= 0 && i <= 127)
                    {
                        writer.Write((byte)i); // positive fixint
                    }
                    else
                    {
                        writer.Write((byte)0xd0); // int8
                        writer.Write((sbyte)i);
                    }
                    break;
                case long l:
                    writer.Write((byte)0xd3); // int64
                    writer.Write(l);
                    break;
                case double d:
                    writer.Write((byte)0xcb); // float64
                    writer.Write(d);
                    break;
                case string s:
                    WriteString(writer, s);
                    break;
                case Dictionary<string, object> dict:
                    WriteDictionary(writer, dict);
                    break;
                default:
                    WriteString(writer, value?.ToString() ?? "");
                    break;
            }
        }

        private void WriteDictionary(System.IO.BinaryWriter writer, Dictionary<string, object> dict)
        {
            if (dict.Count <= 15)
            {
                writer.Write((byte)(0x80 | dict.Count)); // fixmap
            }
            else if (dict.Count <= 65535)
            {
                writer.Write((byte)0xde); // map16
                writer.Write((ushort)dict.Count);
            }
            else
            {
                writer.Write((byte)0xdf); // map32
                writer.Write((uint)dict.Count);
            }

            foreach (var kvp in dict)
            {
                WriteString(writer, kvp.Key);
                WriteValue(writer, kvp.Value);
            }
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