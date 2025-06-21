using System;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;

namespace AhBearStudios.Core.Logging.Targets
{
    /// <summary>
    /// Routes managed <see cref="LogEntry"/> instances into a burst-safe buffer
    /// via <see cref="IBurstLoggingService"/> for later batch processing.
    /// </summary>
    public sealed class BurstLogTarget : ILogTarget
    {
        private readonly IBurstLoggingService _burstLogger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BurstLogTarget"/> class.
        /// </summary>
        /// <param name="burstLogger">The burst logging service to receive entries.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="burstLogger"/> is null.</exception>
        public BurstLogTarget(IBurstLoggingService burstLogger)
        {
            _burstLogger = burstLogger ?? throw new ArgumentNullException(nameof(burstLogger));
            Name = nameof(BurstLogTarget);
            MinimumLevel = LogLevel.Debug;
            IsEnabled = true;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public LogLevel MinimumLevel { get; set; }

        /// <inheritdoc/>
        public bool IsEnabled { get; set; }

        /// <inheritdoc/>
        public void WriteBatch(NativeList<LogMessage> entries)
        {
            if (_disposed) return;
            if (!IsEnabled) return;

            for (int i = 0, count = entries.Length; i < count; i++)
            {
                var msg = entries[i];
                if (msg.Level < MinimumLevel) 
                    continue;

                // Use Tag + Properties overload
                _burstLogger.Log(
                    msg.Tag,
                    msg.Message,
                    msg.Level,
                    msg.Properties
                );
            }
        }

        /// <inheritdoc/>
        public void Write(in LogMessage entry)
        {
            if (_disposed) 
                return;
            if (!IsEnabled || entry.Level < MinimumLevel) 
                return;

            _burstLogger.Log(
                entry.Tag,
                entry.Message,
                entry.Level,
                entry.Properties
            );
        }


        /// <inheritdoc/>
        public void Flush()
        {
            // no-op: burst buffer is managed by LogBatchProcessor
        }

        /// <inheritdoc/>
        public bool IsLevelEnabled(LogLevel level)
        {
            return IsEnabled && level >= MinimumLevel;
        }

        /// <inheritdoc/>
        public void AddTagFilter(Tagging.TagCategory tagCategory) { /* no-op */ }
        /// <inheritdoc/>
        public void RemoveTagFilter(Tagging.TagCategory tagCategory) { /* no-op */ }
        /// <inheritdoc/>
        public void ClearTagFilters() { /* no-op */ }
        /// <inheritdoc/>
        public void AddLogTagFilter(Tagging.LogTag logTag) { /* no-op */ }
        /// <inheritdoc/>
        public void RemoveLogTagFilter(Tagging.LogTag logTag) { /* no-op */ }
        /// <inheritdoc/>
        public bool IsLogTagEnabled(Tagging.LogTag logTag) => IsEnabled;
        /// <inheritdoc/>
        public bool ShouldProcessMessage(in LogMessage logMessage)
        {
            return IsEnabled && logMessage.Level >= MinimumLevel;
        }
        /// <inheritdoc/>
        public void SetTagFilters(string[] includedTags, string[] excludedTags, bool processUntaggedMessages) { /* no-op */ }

        /// <inheritdoc/>
        public void Dispose()
        {
            _disposed = true;
        }
    }
}
