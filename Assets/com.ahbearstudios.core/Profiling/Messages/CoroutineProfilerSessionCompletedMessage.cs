using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a coroutine profiler session completes
    /// </summary>
    public struct CoroutineProfilerSessionCompletedMessage : IMessage
    {
        /// <inheritdoc />
        public Guid Id { get; }
        
        /// <inheritdoc />
        public long TimestampTicks { get; }
        
        /// <inheritdoc />
        public ushort TypeCode { get; }
        
        /// <summary>
        /// Profiler tag for this session
        /// </summary>
        public readonly ProfilerTag Tag;
        
        /// <summary>
        /// Session identifier
        /// </summary>
        public readonly Guid SessionId;
        
        /// <summary>
        /// Runner identifier
        /// </summary>
        public readonly Guid RunnerId;
        
        /// <summary>
        /// Runner name
        /// </summary>
        public readonly string RunnerName;
        
        /// <summary>
        /// Coroutine identifier
        /// </summary>
        public readonly int CoroutineId;
        
        /// <summary>
        /// Coroutine tag
        /// </summary>
        public readonly string CoroutineTag;
        
        /// <summary>
        /// Duration of the session in milliseconds
        /// </summary>
        public readonly double DurationMs;
        
        /// <summary>
        /// Custom metrics recorded during the session
        /// </summary>
        public readonly IReadOnlyDictionary<string, double> CustomMetrics;
        
        /// <summary>
        /// Operation type that was profiled
        /// </summary>
        public readonly string OperationType;
        
        /// <summary>
        /// Whether the operation completed successfully
        /// </summary>
        public readonly bool Success;
        
        /// <summary>
        /// Creates a new coroutine profiler session completed message
        /// </summary>
        public CoroutineProfilerSessionCompletedMessage(
            ProfilerTag tag,
            Guid sessionId,
            Guid runnerId,
            string runnerName,
            int coroutineId,
            string coroutineTag,
            double durationMs,
            IReadOnlyDictionary<string, double> customMetrics,
            string operationType,
            bool success = true)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = 0; // Will be assigned by message registry
            Tag = tag;
            SessionId = sessionId;
            RunnerId = runnerId;
            RunnerName = runnerName;
            CoroutineId = coroutineId;
            CoroutineTag = coroutineTag;
            DurationMs = durationMs;
            CustomMetrics = customMetrics;
            OperationType = operationType;
            Success = success;
        }
    }
}