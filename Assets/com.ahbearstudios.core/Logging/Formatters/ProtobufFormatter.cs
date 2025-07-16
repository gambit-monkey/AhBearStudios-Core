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
    /// Protocol Buffers formatter for cross-platform serialization.
    /// Efficient binary format suitable for distributed systems and microservices.
    /// Supports optional performance metrics through IProfilerService integration.
    /// Note: This is a basic implementation. For production use, consider using Google.Protobuf library.
    /// </summary>
    public sealed class ProtobufFormatter : ILogFormatter
    {
        private readonly Dictionary<FixedString32Bytes, object> _settings;
        private readonly IProfilerService _profilerService;
        private bool _isEnabled = true;

        /// <inheritdoc />
        public FixedString64Bytes Name => "Protobuf";

        /// <inheritdoc />
        public LogFormat LogFormat => LogFormat.Protobuf;

        /// <inheritdoc />
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Initializes a new instance of the ProtobufFormatter class.
        /// </summary>
        /// <param name="profilerService">Optional profiler service for performance metrics</param>
        public ProtobufFormatter(IProfilerService profilerService = null)
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
            // This is a simplified protobuf-like format
            // For production use, integrate with Google.Protobuf library
            var serialized = SerializeToProtobuf(entry, correlationId);
            
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
            warnings.Add(new ValidationWarning("This is a simplified Protobuf implementation. Consider using Google.Protobuf for production.", "Implementation"));

            return errors.Count > 0 
                ? ValidationResult.Failure(errors, "ProtobufFormatter", warnings)
                : ValidationResult.Success("ProtobufFormatter", warnings);
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
            return format == LogFormat.Protobuf;
        }

        private byte[] SerializeToProtobuf(LogEntry entry, FixedString64Bytes correlationId)
        {
            // This is a simplified protobuf serialization
            // For production, use Google.Protobuf library with proper .proto definitions
            
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);

            // Field 1: Version (varint)
            if (GetSetting<int>("Version") != 0)
            {
                WriteTag(writer, 1, WireType.Varint);
                WriteVarint(writer, (ulong)GetSetting<int>("Version"));
            }

            // Field 2: Timestamp (varint)
            if (GetSetting<bool>("IncludeTimestamp"))
            {
                WriteTag(writer, 2, WireType.Varint);
                var useUtc = GetSetting<bool>("UseUtcTimestamp");
                var timestamp = useUtc ? entry.Timestamp.ToUniversalTime() : entry.Timestamp;
                WriteVarint(writer, (ulong)timestamp.ToBinary());
            }

            // Field 3: Level (varint)
            if (GetSetting<bool>("IncludeLevel"))
            {
                WriteTag(writer, 3, WireType.Varint);
                WriteVarint(writer, (ulong)entry.Level);
            }

            // Field 4: Channel (length-delimited)
            if (GetSetting<bool>("IncludeChannel"))
            {
                WriteTag(writer, 4, WireType.LengthDelimited);
                WriteString(writer, entry.Channel.ToString());
            }

            // Field 5: Message (length-delimited)
            WriteTag(writer, 5, WireType.LengthDelimited);
            WriteString(writer, entry.Message.ToString());

            // Field 6: Correlation ID (length-delimited)
            if (GetSetting<bool>("IncludeCorrelationId"))
            {
                var corrId = !correlationId.IsEmpty ? correlationId.ToString() : entry.CorrelationId.ToString();
                if (!string.IsNullOrEmpty(corrId))
                {
                    WriteTag(writer, 6, WireType.LengthDelimited);
                    WriteString(writer, corrId);
                }
            }

            // Field 7: Thread ID (varint)
            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                WriteTag(writer, 7, WireType.Varint);
                WriteVarint(writer, (ulong)entry.ThreadId);
            }

            // Field 8: Thread Name (length-delimited)
            if (GetSetting<bool>("IncludeThreadInfo"))
            {
                WriteTag(writer, 8, WireType.LengthDelimited);
                
                // Get thread name from current thread if available
                var currentThread = Thread.CurrentThread;
                var threadName = !string.IsNullOrEmpty(currentThread.Name) 
                    ? currentThread.Name 
                    : $"Thread-{entry.ThreadId}";
                WriteString(writer, threadName);
            }

            // Field 9: Machine Name (length-delimited)
            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                WriteTag(writer, 9, WireType.LengthDelimited);
                WriteString(writer, entry.MachineName.ToString());
            }

            // Field 10: Instance ID (length-delimited)
            if (GetSetting<bool>("IncludeMachineInfo"))
            {
                WriteTag(writer, 10, WireType.LengthDelimited);
                WriteString(writer, entry.InstanceId.ToString());
            }

            // Field 11: Exception (embedded message)
            if (GetSetting<bool>("IncludeException") && entry.Exception != null)
            {
                WriteTag(writer, 11, WireType.LengthDelimited);
                WriteException(writer, entry.Exception);
            }

            // Fields 12-14: Performance Metrics (if available and enabled)
            if (GetSetting<bool>("IncludePerformanceMetrics") && _profilerService != null)
            {
                try
                {
                    var cpuMetrics = _profilerService.GetMetrics("CPU.Usage");
                    if (cpuMetrics != null)
                    {
                        // Field 12: CPU Usage (fixed64)
                        WriteTag(writer, 12, WireType.Fixed64);
                        WriteFixed64(writer, BitConverter.DoubleToInt64Bits(cpuMetrics.LastOrDefault().Value));
                    }
                    
                    var memoryMetrics = _profilerService.GetMetrics("Memory.Allocated");
                    if (memoryMetrics != null)
                    {
                        // Field 13: Memory Usage (varint)
                        WriteTag(writer, 13, WireType.Varint);
                        WriteVarint(writer, (ulong)memoryMetrics.LastOrDefault().Value);
                    }
                    
                    var processingTimeMetrics = _profilerService.GetMetrics("ProcessingTime");
                    if (processingTimeMetrics != null)
                    {
                        // Field 14: Processing Time (fixed64)
                        WriteTag(writer, 14, WireType.Fixed64);
                        WriteFixed64(writer, BitConverter.DoubleToInt64Bits(processingTimeMetrics.LastOrDefault().Value));
                    }
                }
                catch
                {
                    // Silently ignore profiler errors to prevent log formatting failures
                }
            }

            // Field 15: Entry ID (length-delimited)
            WriteTag(writer, 15, WireType.LengthDelimited);
            WriteString(writer, entry.Id.ToString());

            return stream.ToArray();
        }

        private void WriteException(System.IO.BinaryWriter writer, Exception exception)
        {
            using var exceptionStream = new System.IO.MemoryStream();
            using var exceptionWriter = new System.IO.BinaryWriter(exceptionStream);

            // Field 1: Type (length-delimited)
            WriteTag(exceptionWriter, 1, WireType.LengthDelimited);
            WriteString(exceptionWriter, exception.GetType().Name);

            // Field 2: Message (length-delimited)
            WriteTag(exceptionWriter, 2, WireType.LengthDelimited);
            WriteString(exceptionWriter, exception.Message);

            // Field 3: Source (length-delimited)
            if (!string.IsNullOrEmpty(exception.Source))
            {
                WriteTag(exceptionWriter, 3, WireType.LengthDelimited);
                WriteString(exceptionWriter, exception.Source);
            }

            // Field 4: Stack Trace (length-delimited)
            if (GetSetting<bool>("IncludeStackTrace") && !string.IsNullOrEmpty(exception.StackTrace))
            {
                WriteTag(exceptionWriter, 4, WireType.LengthDelimited);
                WriteString(exceptionWriter, exception.StackTrace);
            }

            var exceptionData = exceptionStream.ToArray();
            WriteVarint(writer, (ulong)exceptionData.Length);
            writer.Write(exceptionData);
        }

        private void WriteTag(System.IO.BinaryWriter writer, int fieldNumber, WireType wireType)
        {
            var tag = (ulong)((fieldNumber << 3) | (int)wireType);
            WriteVarint(writer, tag);
        }

        private void WriteVarint(System.IO.BinaryWriter writer, ulong value)
        {
            while (value >= 0x80)
            {
                writer.Write((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            writer.Write((byte)value);
        }

        private void WriteFixed64(System.IO.BinaryWriter writer, long value)
        {
            writer.Write(value);
        }

        private void WriteString(System.IO.BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteVarint(writer, 0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            WriteVarint(writer, (ulong)bytes.Length);
            writer.Write(bytes);
        }

        private enum WireType
        {
            Varint = 0,
            Fixed64 = 1,
            LengthDelimited = 2,
            StartGroup = 3,
            EndGroup = 4,
            Fixed32 = 5
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