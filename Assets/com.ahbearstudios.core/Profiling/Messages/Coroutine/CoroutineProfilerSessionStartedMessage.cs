using System;
using System.Collections.Generic;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.MessageBus.Registration;

namespace AhBearStudios.Core.Profiling.Messages
{
    /// <summary>
    /// Message published when a coroutine profiler session starts
    /// </summary>
    public struct CoroutineProfilerSessionStartedMessage : IMessage
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
        /// Creates a new coroutine profiler session started message
        /// </summary>
        public CoroutineProfilerSessionStartedMessage(
            ProfilerTag tag,
            Guid sessionId,
            Guid runnerId,
            string runnerName,
            int coroutineId,
            string coroutineTag)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            TypeCode = MessageTypeRegistry.GetTypeCode<CoroutineProfilerSessionStartedMessage>();
            Tag = tag;
            SessionId = sessionId;
            RunnerId = runnerId;
            RunnerName = runnerName;
            CoroutineId = coroutineId;
            CoroutineTag = coroutineTag;
        }
    }
}