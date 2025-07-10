using System;
using AhBearStudios.Core.Logging.Data;
using Unity.Collections;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Logging.Messages;
using AhBearStudios.Core.Logging.Tags;
using Unity.Burst;

namespace AhBearStudios.Core.Logging.Services
{
    /// <summary>
    /// Burst-compatible, allocation-free logger that buffers log messages into a native queue.
    /// </summary>
    public sealed class BurstLoggingService : IBurstLoggingService, IDisposable
    {
        private readonly NativeQueue<LogMessage> _logQueue;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BurstLoggingService"/> class.
        /// </summary>
        /// <param name="initialCapacity">
        /// Optional initial capacity hint (not used by NativeQueue but reserved for future tuning).
        /// </param>
        public BurstLoggingService(int initialCapacity)
        {
            _logQueue = new NativeQueue<LogMessage>(Allocator.Persistent);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Log(in FixedString512Bytes message, LogLevel level)
        {
            if (_disposed) return;
            _logQueue.Enqueue(new LogMessage(message, level));
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Log(in Tagging.LogTag category, in FixedString512Bytes message, LogLevel level)
        {
            if (_disposed) return;
            _logQueue.Enqueue(new LogMessage(message, level, category, 0));
        }
        
        /// <inheritdoc/>
        [BurstCompile]
        public void Log(Tagging.LogTag tag, in FixedString512Bytes message, LogLevel level, in LogProperties properties)
        {
            if (_disposed) return;
            // pick the ctor that matches (message, level, tag, properties)
            _logQueue.Enqueue(new LogMessage(message, level, tag, properties));
        }

        /// <inheritdoc/>
        public void ClearBuffer()
        {
            if (_disposed) return;
            while (_logQueue.TryDequeue(out _)) { }
        }

        /// <inheritdoc/>
        public int PendingCount
        {
            get
            {
                if (_disposed) return 0;
                return _logQueue.Count;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public bool TryDequeue(out LogMessage message)
        {
            if (_disposed)
            {
                message = default;
                return false;
            }
            return _logQueue.TryDequeue(out message);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            if (_logQueue.IsCreated)
            {
                _logQueue.Dispose();
            }
            _disposed = true;
        }
    }
}
