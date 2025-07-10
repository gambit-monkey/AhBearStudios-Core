using System;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message sent when a batch of log messages has been processed.
    /// Represents a message-based alternative to LogProcessingEventArgs.
    /// </summary>
    public struct LogProcessingMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks).
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type.
        /// </summary>
        public ushort TypeCode => 10104; // Assign appropriate code from your message registry
        
        /// <summary>
        /// Gets the number of messages processed in this batch.
        /// </summary>
        public int ProcessedCount { get; }
        
        /// <summary>
        /// Gets the number of messages still queued for processing.
        /// </summary>
        public int RemainingCount { get; }
        
        /// <summary>
        /// Gets the time it took to process this batch in milliseconds.
        /// </summary>
        public float ProcessingTimeMs { get; }
        
        /// <summary>
        /// Creates a new LogProcessingMessage instance.
        /// </summary>
        /// <param name="processedCount">The number of messages processed.</param>
        /// <param name="remainingCount">The number of messages remaining.</param>
        /// <param name="processingTimeMs">The processing time in milliseconds.</param>
        public LogProcessingMessage(int processedCount, int remainingCount, float processingTimeMs)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            ProcessedCount = processedCount;
            RemainingCount = remainingCount;
            ProcessingTimeMs = processingTimeMs;
        }
    }
}