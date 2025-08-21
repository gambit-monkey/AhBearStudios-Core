using System;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Messages;

/// <summary>
/// Operations performed on routing rules.
/// </summary>
public enum RoutingRuleOperation
{
    /// <summary>
    /// Rule was added.
    /// </summary>
    Added,

    /// <summary>
    /// Rule was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// Rule was modified.
    /// </summary>
    Modified
}

/// <summary>
/// Message sent when routing rules are added, removed, or modified.
/// Replaces RoutingRuleEventArgs with IMessage pattern for consistent event handling.
/// </summary>
public readonly record struct MessageBusRoutingRuleChangedMessage : IMessage
{
    /// <summary>
    /// Gets the unique identifier for this message instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when this message was created, in UTC ticks.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the unique type code for this message type.
    /// </summary>
    public ushort TypeCode { get; init; } = MessageTypeCodes.MessageBusRoutingRuleChangedMessage;

    /// <summary>
    /// Gets the source system or component that created this message.
    /// </summary>
    public FixedString64Bytes Source { get; init; }

    /// <summary>
    /// Gets the priority level for message processing.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// Gets optional correlation ID for message tracing across systems.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the routing rule.
    /// </summary>
    public Guid RuleId { get; init; }

    /// <summary>
    /// Gets the name of the routing rule.
    /// </summary>
    public FixedString64Bytes RuleName { get; init; }

    /// <summary>
    /// Gets the operation performed on the rule.
    /// </summary>
    public RoutingRuleOperation Operation { get; init; }

    /// <summary>
    /// Gets the timestamp when the operation occurred.
    /// </summary>
    public DateTime ChangedAt { get; init; }

    /// <summary>
    /// Gets the message type this rule applies to.
    /// </summary>
    public Type MessageType { get; init; }

    /// <summary>
    /// Gets the priority of the rule.
    /// </summary>
    public int RulePriority { get; init; }

    /// <summary>
    /// Gets whether the rule is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Gets additional context about the change.
    /// </summary>
    public FixedString512Bytes ChangeContext { get; init; }

    /// <summary>
    /// Initializes a new instance of the MessageBusRoutingRuleChangedMessage struct.
    /// </summary>
    public MessageBusRoutingRuleChangedMessage()
    {
        Id = default;
        TimestampTicks = default;
        Source = default;
        Priority = default;
        CorrelationId = default;
        RuleId = default;
        RuleName = default;
        Operation = default;
        ChangedAt = default;
        MessageType = default;
        RulePriority = default;
        IsEnabled = default;
        ChangeContext = default;
    }

    /// <summary>
    /// Gets the DateTime representation of the timestamp.
    /// </summary>
    public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the MessageBusRoutingRuleChangedMessage.
    /// </summary>
    /// <param name="ruleId">The unique identifier of the rule</param>
    /// <param name="ruleName">The name of the rule</param>
    /// <param name="operation">The operation performed</param>
    /// <param name="messageType">The message type this rule applies to</param>
    /// <param name="rulePriority">The priority of the rule</param>
    /// <param name="isEnabled">Whether the rule is enabled</param>
    /// <param name="changeContext">Additional context</param>
    /// <param name="source">Source component</param>
    /// <param name="correlationId">Correlation ID for tracking</param>
    /// <returns>New MessageBusRoutingRuleChangedMessage instance</returns>
    public static MessageBusRoutingRuleChangedMessage Create(
        Guid ruleId,
        string ruleName,
        RoutingRuleOperation operation,
        Type messageType = null,
        int rulePriority = 100,
        bool isEnabled = true,
        string changeContext = null,
        FixedString64Bytes source = default,
        Guid correlationId = default)
    {
        return new MessageBusRoutingRuleChangedMessage
        {
            Id = Guid.NewGuid(),
            TimestampTicks = DateTime.UtcNow.Ticks,
            TypeCode = MessageTypeCodes.MessageBusRoutingRuleChangedMessage,
            Source = source.IsEmpty ? "MessageBusService" : source,
            Priority = MessagePriority.Low, // Routing rule changes are informational
            CorrelationId = correlationId == default ? Guid.NewGuid() : correlationId,
            RuleId = ruleId,
            RuleName = ruleName?.Length <= 64 ? ruleName : ruleName?[..64] ?? throw new ArgumentNullException(nameof(ruleName)),
            Operation = operation,
            ChangedAt = DateTime.UtcNow,
            MessageType = messageType,
            RulePriority = rulePriority,
            IsEnabled = isEnabled,
            ChangeContext = changeContext?.Length <= 256 ? changeContext : changeContext?[..256] ?? string.Empty
        };
    }
}