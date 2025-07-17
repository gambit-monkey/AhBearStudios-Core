using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when a logging scope completes.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly struct LogScopeCompletedMessage : IMessage
    {
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the timestamp when this message was created.
        /// </summary>
        public long TimestampTicks { get; }

        /// <summary>
        /// Gets the type code for this message type.
        /// </summary>
        public ushort TypeCode => MessageTypeCodes.LogScopeCompleted;

        /// <summary>
        /// Gets the source system that published this message.
        /// </summary>
        public FixedString64Bytes Source { get; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Gets the name of the completed scope.
        /// </summary>
        public FixedString64Bytes ScopeName { get; }

        /// <summary>
        /// Gets the correlation ID that was associated with the scope.
        /// </summary>
        public FixedString64Bytes ScopeCorrelationId { get; }

        /// <summary>
        /// Gets the source context for the scope.
        /// </summary>
        public FixedString128Bytes SourceContext { get; }

        /// <summary>
        /// Gets the duration of the scope in ticks.
        /// </summary>
        public long DurationTicks { get; }

        /// <summary>
        /// Gets whether the scope completed successfully.
        /// </summary>
        public bool CompletedSuccessfully { get; }

        /// <summary>
        /// Gets the number of child scopes that were created.
        /// </summary>
        public int ChildScopeCount { get; }

        /// <summary>
        /// Gets the number of log messages written within the scope.
        /// </summary>
        public int MessageCount { get; }

        /// <summary>
        /// Gets the number of errors that occurred within the scope.
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Gets the parent scope name, if any.
        /// </summary>
        public FixedString64Bytes ParentScopeName { get; }

        /// <summary>
        /// Initializes a new instance of the LogScopeCompletedMessage.
        /// </summary>
        /// <param name="scopeName">The name of the completed scope</param>
        /// <param name="scopeCorrelationId">The correlation ID associated with the scope</param>
        /// <param name="sourceContext">The source context for the scope</param>
        /// <param name="duration">The duration of the scope</param>
        /// <param name="completedSuccessfully">Whether the scope completed successfully</param>
        /// <param name="childScopeCount">The number of child scopes created</param>
        /// <param name="messageCount">The number of messages logged within the scope</param>
        /// <param name="errorCount">The number of errors that occurred within the scope</param>
        /// <param name="parentScopeName">The parent scope name, if any</param>
        /// <param name="correlationId">The correlation ID for this message</param>
        public LogScopeCompletedMessage(
            FixedString64Bytes scopeName,
            FixedString64Bytes scopeCorrelationId,
            FixedString128Bytes sourceContext,
            TimeSpan duration,
            bool completedSuccessfully = true,
            int childScopeCount = 0,
            int messageCount = 0,
            int errorCount = 0,
            FixedString64Bytes parentScopeName = default,
            Guid correlationId = default)
        {
            Id = Guid.NewGuid();
            TimestampTicks = DateTime.UtcNow.Ticks;
            Source = new FixedString64Bytes("LoggingSystem");
            Priority = errorCount > 0 ? MessagePriority.High : MessagePriority.Normal;
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId;
            
            ScopeName = scopeName;
            ScopeCorrelationId = scopeCorrelationId;
            SourceContext = sourceContext;
            DurationTicks = duration.Ticks;
            CompletedSuccessfully = completedSuccessfully;
            ChildScopeCount = childScopeCount;
            MessageCount = messageCount;
            ErrorCount = errorCount;
            ParentScopeName = parentScopeName;
        }

        /// <summary>
        /// Gets the duration as a TimeSpan.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(DurationTicks);

        /// <summary>
        /// Creates a LogScopeCompletedMessage from string parameters for convenience.
        /// </summary>
        /// <param name="scopeName">The scope name</param>
        /// <param name="scopeCorrelationId">The scope correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="duration">The scope duration</param>
        /// <param name="completedSuccessfully">Whether the scope completed successfully</param>
        /// <param name="statistics">Optional scope statistics</param>
        /// <param name="parentScopeName">The parent scope name</param>
        /// <param name="correlationId">The message correlation ID</param>
        /// <returns>A new LogScopeCompletedMessage</returns>
        public static LogScopeCompletedMessage Create(
            string scopeName,
            string scopeCorrelationId = null,
            string sourceContext = null,
            TimeSpan? duration = null,
            bool completedSuccessfully = true,
            (int childScopes, int messages, int errors)? statistics = null,
            string parentScopeName = null,
            Guid correlationId = default)
        {
            var stats = statistics ?? (0, 0, 0);
            
            return new LogScopeCompletedMessage(
                new FixedString64Bytes(scopeName ?? "Unknown"),
                new FixedString64Bytes(scopeCorrelationId ?? string.Empty),
                new FixedString128Bytes(sourceContext ?? string.Empty),
                duration ?? TimeSpan.Zero,
                completedSuccessfully,
                stats.childScopes,
                stats.messages,
                stats.errors,
                new FixedString64Bytes(parentScopeName ?? string.Empty),
                correlationId);
        }

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            var status = CompletedSuccessfully ? "Success" : "Failed";
            return $"LogScopeCompleted: {ScopeName} - {Duration.TotalMilliseconds:F2}ms ({status}, {MessageCount} messages, {ErrorCount} errors)";
        }
    }
}