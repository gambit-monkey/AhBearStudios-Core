using System;
using Unity.Collections;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Logging.Messages
{
    /// <summary>
    /// Message published when a logging scope completes.
    /// Replaces direct EventHandler usage for loose coupling through IMessageBus.
    /// </summary>
    public readonly record struct LoggingScopeCompletedMessage : IMessage
    {
        #region IMessage Implementation
        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when this message was created.
        /// </summary>
        public long TimestampTicks { get; init; }

        /// <summary>
        /// Gets the type code for this message type.
        /// </summary>
        public ushort TypeCode { get; init; }

        /// <summary>
        /// Gets the source system that published this message.
        /// </summary>
        public FixedString64Bytes Source { get; init; }

        /// <summary>
        /// Gets the priority level for message processing.
        /// </summary>
        public MessagePriority Priority { get; init; }

        /// <summary>
        /// Gets the correlation ID for tracking.
        /// </summary>
        public Guid CorrelationId { get; init; }

        #endregion

        #region Message-Specific Properties

        /// <summary>
        /// Gets the name of the completed scope.
        /// </summary>
        public FixedString64Bytes ScopeName { get; init; }

        /// <summary>
        /// Gets the correlation ID that was associated with the scope.
        /// </summary>
        public FixedString64Bytes ScopeCorrelationId { get; init; }

        /// <summary>
        /// Gets the source context for the scope.
        /// </summary>
        public FixedString128Bytes SourceContext { get; init; }

        /// <summary>
        /// Gets the duration of the scope in ticks.
        /// </summary>
        public long DurationTicks { get; init; }

        /// <summary>
        /// Gets whether the scope completed successfully.
        /// </summary>
        public bool CompletedSuccessfully { get; init; }

        /// <summary>
        /// Gets the number of child scopes that were created.
        /// </summary>
        public int ChildScopeCount { get; init; }

        /// <summary>
        /// Gets the number of log messages written within the scope.
        /// </summary>
        public int MessageCount { get; init; }

        /// <summary>
        /// Gets the number of errors that occurred within the scope.
        /// </summary>
        public int ErrorCount { get; init; }

        /// <summary>
        /// Gets the parent scope name, if any.
        /// </summary>
        public FixedString64Bytes ParentScopeName { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets the DateTime representation of the message timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the duration as a TimeSpan.
        /// </summary>
        public TimeSpan Duration => new TimeSpan(DurationTicks);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new LoggingScopeCompletedMessage with proper validation and defaults.
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
        /// <param name="source">Source component creating this message</param>
        /// <returns>New LoggingScopeCompletedMessage instance</returns>
        public static LoggingScopeCompletedMessage CreateFromFixedStrings(
            FixedString64Bytes scopeName,
            FixedString64Bytes scopeCorrelationId,
            FixedString128Bytes sourceContext,
            TimeSpan duration,
            bool completedSuccessfully = true,
            int childScopeCount = 0,
            int messageCount = 0,
            int errorCount = 0,
            FixedString64Bytes parentScopeName = default,
            Guid correlationId = default,
            FixedString64Bytes source = default)
        {
            // ID generation with explicit parameters to avoid ambiguity
            var sourceString = source.IsEmpty ? "LoggingSystem" : source.ToString();
            var messageId = DeterministicIdGenerator.GenerateMessageId("LoggingScopeCompletedMessage", sourceString, correlationId: null);
            var finalCorrelationId = correlationId == default 
                ? DeterministicIdGenerator.GenerateCorrelationId("LoggingScopeCompletion", scopeName.ToString())
                : correlationId;
            
            return new LoggingScopeCompletedMessage
            {
                Id = messageId,
                TimestampTicks = DateTime.UtcNow.Ticks,
                TypeCode = MessageTypeCodes.LoggingScopeCompletedMessage,
                Source = source.IsEmpty ? "LoggingSystem" : source,
                Priority = errorCount > 0 ? MessagePriority.High : MessagePriority.Normal,
                CorrelationId = finalCorrelationId,
                
                ScopeName = scopeName,
                ScopeCorrelationId = scopeCorrelationId,
                SourceContext = sourceContext,
                DurationTicks = duration.Ticks,
                CompletedSuccessfully = completedSuccessfully,
                ChildScopeCount = childScopeCount,
                MessageCount = messageCount,
                ErrorCount = errorCount,
                ParentScopeName = parentScopeName
            };
        }

        /// <summary>
        /// Creates a LoggingScopeCompletedMessage from string parameters for convenience.
        /// </summary>
        /// <param name="scopeName">The scope name</param>
        /// <param name="scopeCorrelationId">The scope correlation ID</param>
        /// <param name="sourceContext">The source context</param>
        /// <param name="duration">The scope duration</param>
        /// <param name="completedSuccessfully">Whether the scope completed successfully</param>
        /// <param name="statistics">Optional scope statistics</param>
        /// <param name="parentScopeName">The parent scope name</param>
        /// <param name="correlationId">The message correlation ID</param>
        /// <param name="source">Source component creating this message</param>
        /// <returns>A new LoggingScopeCompletedMessage</returns>
        public static LoggingScopeCompletedMessage Create(
            string scopeName,
            string scopeCorrelationId = null,
            string sourceContext = null,
            TimeSpan? duration = null,
            bool completedSuccessfully = true,
            (int childScopes, int messages, int errors)? statistics = null,
            string parentScopeName = null,
            Guid correlationId = default,
            string source = null)
        {
            var stats = statistics ?? (0, 0, 0);
            
            return CreateFromFixedStrings(
                new FixedString64Bytes(scopeName ?? "Unknown"),
                new FixedString64Bytes(scopeCorrelationId ?? string.Empty),
                new FixedString128Bytes(sourceContext ?? string.Empty),
                duration ?? TimeSpan.Zero,
                completedSuccessfully,
                stats.childScopes,
                stats.messages,
                stats.errors,
                new FixedString64Bytes(parentScopeName ?? string.Empty),
                correlationId,
                new FixedString64Bytes(source ?? "LoggingSystem"));
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Returns a string representation of this message.
        /// </summary>
        /// <returns>A formatted string</returns>
        public override string ToString()
        {
            var status = CompletedSuccessfully ? "Success" : "Failed";
            var scopeName = ScopeName.IsEmpty ? "Unknown" : ScopeName.ToString();
            return $"LogScopeCompleted: {scopeName} - {Duration.TotalMilliseconds:F2}ms ({status}, {MessageCount} messages, {ErrorCount} errors)";
        }

        #endregion
    }
}