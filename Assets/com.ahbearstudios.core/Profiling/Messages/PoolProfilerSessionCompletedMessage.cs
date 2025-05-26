using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message sent when a pool profiler session is completed
    /// </summary>
    public struct PoolProfilerSessionCompletedMessage : IMessage
    {
        /// <summary>
        /// Gets a unique identifier for this message instance
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Gets the timestamp when this message was created (UTC ticks)
        /// </summary>
        public long TimestampTicks { get; }
        
        /// <summary>
        /// Gets the type code that uniquely identifies this message type
        /// </summary>
        public ushort TypeCode => 10021; // Assign an appropriate type code

        /// <summary>
        /// The profiler tag associated with this session
        /// </summary>
        public ProfilerTag Tag { get; }
        
        /// <summary>
        /// Session identifier
        /// </summary>
        public Guid SessionId { get; }
        
        /// <summary>
        /// Pool identifier
        /// </summary>
        public Guid PoolId { get; }
        
        /// <summary>
        /// Pool name
        /// </summary>
        public string PoolName { get; }
        
        /// <summary>
        /// Number of active items at the time of profiling
        /// </summary>
        public int ActiveCount { get; }
        
        /// <summary>
        /// Number of free items at the time of profiling
        /// </summary>
        public int FreeCount { get; }
        
        /// <summary>
        /// The duration of the session in milliseconds
        /// </summary>
        public double DurationMs { get; }
        
        /// <summary>
        /// Custom metrics recorded during the session
        /// </summary>
        public IReadOnlyDictionary<string, double> Metrics { get; }
        
        /// <summary>
        /// The pool operation type
        /// </summary>
        public string OperationType { get; }

        /// <summary>
        /// Creates a new PoolProfilerSessionCompletedMessage
        /// </summary>
        /// <param name="tag">The profiler tag</param>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="poolId">Pool identifier</param>
        /// <param name="poolName">Pool name</param>
        /// <param name="activeCount">Active item count</param>
        /// <param name="freeCount">Free item count</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <param name="metrics">Custom metrics</param>
        /// <param name="operationType">Pool operation type</param>
        public PoolProfilerSessionCompletedMessage(
            ProfilerTag tag, 
            Guid sessionId, 
            Guid poolId,
            string poolName,
            int activeCount,
            int freeCount,
            double durationMs,
            IReadOnlyDictionary<string, double> metrics,
            string operationType)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Tag = tag;
            SessionId = sessionId;
            PoolId = poolId;
            PoolName = poolName;
            ActiveCount = activeCount;
            FreeCount = freeCount;
            DurationMs = durationMs;
            Metrics = metrics ?? new Dictionary<string, double>();
            OperationType = operationType;
        }
    }
}