using System.Threading;
using ZLinq;
using AhBearStudios.Core.Messaging.Messages;
using AhBearStudios.Core.Messaging.Models;
using Cysharp.Threading.Tasks;
using Unity.Collections;

namespace AhBearStudios.Core.Messaging.Services
{
    #region Message Registry Types

    /// <summary>
    /// Comprehensive information about a registered message type.
    /// Provides metadata for routing, serialization, and system management.
    /// </summary>
    public sealed record MessageTypeInfo
    {
        /// <summary>
        /// Gets the message type.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the unique type code assigned to this message type.
        /// </summary>
        public ushort TypeCode { get; }

        /// <summary>
        /// Gets the simple name of the message type.
        /// </summary>
        public FixedString64Bytes Name { get; }

        /// <summary>
        /// Gets the full name of the message type.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the category this message type belongs to.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the description of this message type.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the default priority for messages of this type.
        /// </summary>
        public MessagePriority DefaultPriority { get; }

        /// <summary>
        /// Gets whether this message type is serializable.
        /// </summary>
        public bool IsSerializable { get; }

        /// <summary>
        /// Gets when this message type was registered.
        /// </summary>
        public DateTime RegisteredAt { get; }

        /// <summary>
        /// Initializes a new instance of the MessageTypeInfo record.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="typeCode">The type code</param>
        /// <param name="name">The type name</param>
        /// <param name="fullName">The full type name</param>
        /// <param name="category">The category</param>
        /// <param name="description">The description</param>
        /// <param name="defaultPriority">The default priority</param>
        /// <param name="isSerializable">Whether the type is serializable</param>
        /// <param name="registeredAt">The registration timestamp</param>
        public MessageTypeInfo(
            Type messageType,
            ushort typeCode,
            string name,
            string fullName,
            string category,
            string description,
            MessagePriority defaultPriority,
            bool isSerializable,
            DateTime registeredAt)
        {
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            TypeCode = typeCode;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Category = category ?? string.Empty;
            Description = description ?? string.Empty;
            DefaultPriority = defaultPriority;
            IsSerializable = isSerializable;
            RegisteredAt = registeredAt;
        }
    }

    /// <summary>
    /// Statistics for message registry performance and usage.
    /// </summary>
    public readonly struct MessageRegistryStatistics
    {
        /// <summary>
        /// Gets the total number of registrations performed.
        /// </summary>
        public readonly long TotalRegistrations;

        /// <summary>
        /// Gets the total number of lookups performed.
        /// </summary>
        public readonly long TotalLookups;

        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        public readonly long CacheHits;

        /// <summary>
        /// Gets the number of cache misses.
        /// </summary>
        public readonly long CacheMisses;

        /// <summary>
        /// Gets the cache hit rate as a percentage (0.0 to 1.0).
        /// </summary>
        public readonly double CacheHitRate;

        /// <summary>
        /// Gets the current lookup rate per second.
        /// </summary>
        public readonly double LookupsPerSecond;

        /// <summary>
        /// Gets the number of currently registered types.
        /// </summary>
        public readonly int RegisteredTypeCount;

        /// <summary>
        /// Gets the current cache size.
        /// </summary>
        public readonly int CacheSize;

        /// <summary>
        /// Gets the timestamp when statistics were collected.
        /// </summary>
        public readonly long TimestampTicks;

        /// <summary>
        /// Initializes a new instance of MessageRegistryStatistics.
        /// </summary>
        public MessageRegistryStatistics(
            long totalRegistrations,
            long totalLookups,
            long cacheHits,
            long cacheMisses,
            double cacheHitRate,
            double lookupsPerSecond,
            int registeredTypeCount,
            int cacheSize,
            long timestampTicks)
        {
            TotalRegistrations = totalRegistrations;
            TotalLookups = totalLookups;
            CacheHits = cacheHits;
            CacheMisses = cacheMisses;
            CacheHitRate = cacheHitRate;
            LookupsPerSecond = lookupsPerSecond;
            RegisteredTypeCount = registeredTypeCount;
            CacheSize = cacheSize;
            TimestampTicks = timestampTicks;
        }

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets an empty statistics instance.
        /// </summary>
        public static MessageRegistryStatistics Empty => new(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow.Ticks);
    }

    #endregion

    #region Message Routing Types

    /// <summary>
    /// Defines a routing rule for message processing.
    /// Immutable record for thread-safe operations.
    /// </summary>
    public sealed record RoutingRule
    {
        /// <summary>
        /// Gets the unique identifier for this routing rule.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the name of this routing rule.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of this routing rule.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the message type this rule applies to.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the category filter (optional).
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the source filter (optional).
        /// </summary>
        public string SourceFilter { get; }

        /// <summary>
        /// Gets the minimum priority level for messages to match this rule.
        /// </summary>
        public MessagePriority MinPriority { get; }

        /// <summary>
        /// Gets the priority of this rule for execution order (higher = first).
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets whether this rule is currently enabled.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Gets when this rule was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the custom condition function (optional).
        /// </summary>
        public Func<IMessage, CancellationToken, UniTask<bool>> Condition { get; }

        /// <summary>
        /// Initializes a new instance of the RoutingRule record.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <param name="name">The rule name</param>
        /// <param name="description">The rule description</param>
        /// <param name="messageType">The message type</param>
        /// <param name="category">The category filter</param>
        /// <param name="sourceFilter">The source filter</param>
        /// <param name="minPriority">The minimum priority</param>
        /// <param name="priority">The rule priority</param>
        /// <param name="isEnabled">Whether the rule is enabled</param>
        /// <param name="createdAt">The creation timestamp</param>
        /// <param name="condition">The custom condition</param>
        public RoutingRule(
            Guid id,
            string name,
            string description,
            Type messageType,
            string category = null,
            string sourceFilter = null,
            MessagePriority minPriority = MessagePriority.Normal,
            int priority = 100,
            bool isEnabled = true,
            DateTime createdAt = default,
            Func<IMessage, CancellationToken, UniTask<bool>> condition = null)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            Category = category;
            SourceFilter = sourceFilter;
            MinPriority = minPriority;
            Priority = priority;
            IsEnabled = isEnabled;
            CreatedAt = createdAt == default ? DateTime.UtcNow : createdAt;
            Condition = condition;
        }
    }

    /// <summary>
    /// Represents a route handler for processing messages.
    /// </summary>
    public sealed record RouteHandler
    {
        /// <summary>
        /// Gets the unique identifier for this handler.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of this handler.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the handler function.
        /// </summary>
        public Func<IMessage, CancellationToken, UniTask<bool>> Handler { get; }

        /// <summary>
        /// Gets when this handler was registered.
        /// </summary>
        public DateTime RegisteredAt { get; }

        /// <summary>
        /// Initializes a new instance of the RouteHandler record.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <param name="name">The handler name</param>
        /// <param name="handler">The handler function</param>
        /// <param name="registeredAt">The registration timestamp</param>
        public RouteHandler(
            Guid id,
            string name,
            Func<IMessage, CancellationToken, UniTask<bool>> handler,
            DateTime registeredAt)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            RegisteredAt = registeredAt;
        }
    }

    /// <summary>
    /// Result of a message routing operation.
    /// </summary>
    public sealed record RouteResult
    {
        /// <summary>
        /// Gets the message ID that was routed.
        /// </summary>
        public Guid MessageId { get; }

        /// <summary>
        /// Gets the number of successful routes.
        /// </summary>
        public int SuccessfulRoutes { get; }

        /// <summary>
        /// Gets the number of failed routes.
        /// </summary>
        public int FailedRoutes { get; }

        /// <summary>
        /// Gets the detailed execution results.
        /// </summary>
        public RouteExecution[] Executions { get; }

        /// <summary>
        /// Gets when the routing was completed.
        /// </summary>
        public DateTime CompletedAt { get; }

        /// <summary>
        /// Gets whether all routes were successful.
        /// </summary>
        public bool IsFullySuccessful => FailedRoutes == 0;

        /// <summary>
        /// Gets whether any routes were successful.
        /// </summary>
        public bool HasAnySuccess => SuccessfulRoutes > 0;

        /// <summary>
        /// Gets the total number of routes attempted.
        /// </summary>
        public int TotalRoutes => SuccessfulRoutes + FailedRoutes;

        /// <summary>
        /// Initializes a new instance of the RouteResult record.
        /// </summary>
        /// <param name="messageId">The message ID</param>
        /// <param name="successfulRoutes">Number of successful routes</param>
        /// <param name="failedRoutes">Number of failed routes</param>
        /// <param name="executions">Detailed execution results</param>
        /// <param name="completedAt">Completion timestamp</param>
        public RouteResult(
            Guid messageId,
            int successfulRoutes,
            int failedRoutes,
            RouteExecution[] executions,
            DateTime completedAt)
        {
            MessageId = messageId;
            SuccessfulRoutes = successfulRoutes;
            FailedRoutes = failedRoutes;
            Executions = executions ?? Array.Empty<RouteExecution>();
            CompletedAt = completedAt;
        }

        /// <summary>
        /// Creates a result indicating no routes were found.
        /// </summary>
        /// <param name="messageId">The message ID</param>
        /// <returns>No routes result</returns>
        public static RouteResult NoRoutes(Guid messageId) =>
            new(messageId, 0, 0, Array.Empty<RouteExecution>(), DateTime.UtcNow);
    }

    /// <summary>
    /// Result of executing a specific routing rule.
    /// </summary>
    public sealed record RouteExecution
    {
        /// <summary>
        /// Gets the execution ID.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the rule ID that was executed.
        /// </summary>
        public Guid RuleId { get; }

        /// <summary>
        /// Gets the name of the rule that was executed.
        /// </summary>
        public string RuleName { get; }

        /// <summary>
        /// Gets whether the execution was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the error message if execution failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the detailed handler execution results.
        /// </summary>
        public HandlerExecutionResult[] HandlerResults { get; }

        /// <summary>
        /// Initializes a new instance of the RouteExecution record.
        /// </summary>
        /// <param name="executionId">The execution ID</param>
        /// <param name="ruleId">The rule ID</param>
        /// <param name="ruleName">The rule name</param>
        /// <param name="isSuccess">Whether execution was successful</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <param name="duration">Execution duration</param>
        /// <param name="handlerResults">Handler execution results</param>
        public RouteExecution(
            Guid executionId,
            Guid ruleId,
            string ruleName,
            bool isSuccess,
            string errorMessage,
            TimeSpan duration,
            HandlerExecutionResult[] handlerResults = null)
        {
            ExecutionId = executionId;
            RuleId = ruleId;
            RuleName = ruleName ?? string.Empty;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? string.Empty;
            Duration = duration;
            HandlerResults = handlerResults ?? Array.Empty<HandlerExecutionResult>();
        }

        /// <summary>
        /// Creates a successful execution result.
        /// </summary>
        /// <param name="executionId">The execution ID</param>
        /// <param name="ruleId">The rule ID</param>
        /// <param name="ruleName">The rule name</param>
        /// <param name="handlerResults">Handler results</param>
        /// <param name="duration">Execution duration</param>
        /// <returns>Successful execution result</returns>
        public static RouteExecution Success(
            Guid executionId,
            Guid ruleId,
            string ruleName,
            HandlerExecutionResult[] handlerResults,
            TimeSpan duration) =>
            new(executionId, ruleId, ruleName, true, null, duration, handlerResults);

        /// <summary>
        /// Creates a failed execution result.
        /// </summary>
        /// <param name="executionId">The execution ID</param>
        /// <param name="ruleId">The rule ID</param>
        /// <param name="ruleName">The rule name</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="duration">Execution duration</param>
        /// <returns>Failed execution result</returns>
        public static RouteExecution Failed(
            Guid executionId,
            Guid ruleId,
            string ruleName,
            string errorMessage,
            TimeSpan duration) =>
            new(executionId, ruleId, ruleName, false, errorMessage, duration);

        /// <summary>
        /// Creates a partial success execution result.
        /// </summary>
        /// <param name="executionId">The execution ID</param>
        /// <param name="ruleId">The rule ID</param>
        /// <param name="ruleName">The rule name</param>
        /// <param name="handlerResults">Handler results</param>
        /// <param name="duration">Execution duration</param>
        /// <returns>Partial success execution result</returns>
        public static RouteExecution PartialSuccess(
            Guid executionId,
            Guid ruleId,
            string ruleName,
            HandlerExecutionResult[] handlerResults,
            TimeSpan duration) =>
            new(executionId, ruleId, ruleName, handlerResults.AsValueEnumerable().Any(r => r.Success), "Some handlers failed", duration, handlerResults);
    }

    /// <summary>
    /// Result of executing a specific handler within a routing rule.
    /// </summary>
    public sealed record HandlerExecutionResult
    {
        /// <summary>
        /// Gets the handler ID.
        /// </summary>
        public Guid HandlerId { get; }

        /// <summary>
        /// Gets the handler name.
        /// </summary>
        public string HandlerName { get; }

        /// <summary>
        /// Gets whether the handler execution was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error message if handler failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the handler execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Initializes a new instance of the HandlerExecutionResult record.
        /// </summary>
        /// <param name="handlerId">The handler ID</param>
        /// <param name="handlerName">The handler name</param>
        /// <param name="success">Whether execution was successful</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <param name="duration">Execution duration</param>
        public HandlerExecutionResult(
            Guid handlerId,
            string handlerName,
            bool success,
            string errorMessage,
            TimeSpan duration)
        {
            HandlerId = handlerId;
            HandlerName = handlerName ?? string.Empty;
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
            Duration = duration;
        }
    }

    /// <summary>
    /// Statistics for message routing performance and usage.
    /// </summary>
    public readonly struct MessageRoutingStatistics
    {
        /// <summary>
        /// Gets the total number of messages routed.
        /// </summary>
        public readonly long TotalMessagesRouted;

        /// <summary>
        /// Gets the total number of rules evaluated.
        /// </summary>
        public readonly long TotalRulesEvaluated;

        /// <summary>
        /// Gets the total number of routing failures.
        /// </summary>
        public readonly long TotalRoutingFailures;

        /// <summary>
        /// Gets the number of cache hits.
        /// </summary>
        public readonly long CacheHits;

        /// <summary>
        /// Gets the number of cache misses.
        /// </summary>
        public readonly long CacheMisses;

        /// <summary>
        /// Gets the cache hit rate as a percentage (0.0 to 1.0).
        /// </summary>
        public readonly double CacheHitRate;

        /// <summary>
        /// Gets the current routing rate per second.
        /// </summary>
        public readonly double RoutingRate;

        /// <summary>
        /// Gets the average number of rules evaluated per message.
        /// </summary>
        public readonly double AverageRulesPerMessage;

        /// <summary>
        /// Gets the number of active routing rules.
        /// </summary>
        public readonly int ActiveRuleCount;

        /// <summary>
        /// Gets the number of registered route handlers.
        /// </summary>
        public readonly int HandlerCount;

        /// <summary>
        /// Gets the current queue size for background routing.
        /// </summary>
        public readonly int QueueSize;

        /// <summary>
        /// Gets the timestamp when statistics were collected.
        /// </summary>
        public readonly long TimestampTicks;

        /// <summary>
        /// Initializes a new instance of MessageRoutingStatistics.
        /// </summary>
        public MessageRoutingStatistics(
            long totalMessagesRouted,
            long totalRulesEvaluated,
            long totalRoutingFailures,
            long cacheHits,
            long cacheMisses,
            double cacheHitRate,
            double routingRate,
            double averageRulesPerMessage,
            int activeRuleCount,
            int handlerCount,
            int queueSize,
            long timestampTicks)
        {
            TotalMessagesRouted = totalMessagesRouted;
            TotalRulesEvaluated = totalRulesEvaluated;
            TotalRoutingFailures = totalRoutingFailures;
            CacheHits = cacheHits;
            CacheMisses = cacheMisses;
            CacheHitRate = cacheHitRate;
            RoutingRate = routingRate;
            AverageRulesPerMessage = averageRulesPerMessage;
            ActiveRuleCount = activeRuleCount;
            HandlerCount = handlerCount;
            QueueSize = queueSize;
            TimestampTicks = timestampTicks;
        }

        /// <summary>
        /// Gets the DateTime representation of the timestamp.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the success rate as a percentage (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalMessagesRouted > 0 
            ? (double)(TotalMessagesRouted - TotalRoutingFailures) / TotalMessagesRouted 
            : 1.0;

        /// <summary>
        /// Gets an empty statistics instance.
        /// </summary>
        public static MessageRoutingStatistics Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow.Ticks);
    }

    #endregion

    #region Event Arguments

    /// <summary>
    /// Event arguments for message type registration events.
    /// </summary>
    public sealed class MessageTypeRegisteredEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the registered message type.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the message type information.
        /// </summary>
        public MessageTypeInfo TypeInfo { get; }

        /// <summary>
        /// Initializes a new instance of MessageTypeRegisteredEventArgs.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="typeInfo">The type information</param>
        public MessageTypeRegisteredEventArgs(Type messageType, MessageTypeInfo typeInfo)
        {
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        }
    }

    /// <summary>
    /// Event arguments for message type unregistration events.
    /// </summary>
    public sealed class MessageTypeUnregisteredEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unregistered message type.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// Gets the message type information.
        /// </summary>
        public MessageTypeInfo TypeInfo { get; }

        /// <summary>
        /// Initializes a new instance of MessageTypeUnregisteredEventArgs.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="typeInfo">The type information</param>
        public MessageTypeUnregisteredEventArgs(Type messageType, MessageTypeInfo typeInfo)
        {
            MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        }
    }

    /// <summary>
    /// Event arguments for registry cleared events.
    /// </summary>
    public sealed class RegistryClearedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the types that were removed.
        /// </summary>
        public Type[] RemovedTypes { get; }

        /// <summary>
        /// Initializes a new instance of RegistryClearedEventArgs.
        /// </summary>
        /// <param name="removedTypes">The removed types</param>
        public RegistryClearedEventArgs(Type[] removedTypes)
        {
            RemovedTypes = removedTypes ?? Array.Empty<Type>();
        }
    }

    /// <summary>
    /// Event arguments for message routed events.
    /// </summary>
    public sealed class MessageRoutedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the routed message.
        /// </summary>
        public IMessage Message { get; }

        /// <summary>
        /// Gets the routing result.
        /// </summary>
        public RouteResult Result { get; }

        /// <summary>
        /// Initializes a new instance of MessageRoutedEventArgs.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="result">The routing result</param>
        public MessageRoutedEventArgs(IMessage message, RouteResult result)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }

    /// <summary>
    /// Event arguments for routing rule events.
    /// </summary>
    public sealed class RoutingRuleEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the routing rule.
        /// </summary>
        public RoutingRule Rule { get; }

        /// <summary>
        /// Gets the operation performed.
        /// </summary>
        public RoutingRuleOperation Operation { get; }

        /// <summary>
        /// Initializes a new instance of RoutingRuleEventArgs.
        /// </summary>
        /// <param name="rule">The routing rule</param>
        /// <param name="operation">The operation</param>
        public RoutingRuleEventArgs(RoutingRule rule, RoutingRuleOperation operation)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            Operation = operation;
        }
    }

    /// <summary>
    /// Event arguments for route handler events.
    /// </summary>
    public sealed class RouteHandlerEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the route handler.
        /// </summary>
        public RouteHandler Handler { get; }

        /// <summary>
        /// Gets the operation performed.
        /// </summary>
        public RouteHandlerOperation Operation { get; }

        /// <summary>
        /// Initializes a new instance of RouteHandlerEventArgs.
        /// </summary>
        /// <param name="handler">The route handler</param>
        /// <param name="operation">The operation</param>
        public RouteHandlerEventArgs(RouteHandler handler, RouteHandlerOperation operation)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Operation = operation;
        }
    }

    /// <summary>
    /// Event arguments for routes cleared events.
    /// </summary>
    public sealed class RoutesClearedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the routing rules that were removed.
        /// </summary>
        public RoutingRule[] RemovedRules { get; }

        /// <summary>
        /// Gets the route handlers that were removed.
        /// </summary>
        public RouteHandler[] RemovedHandlers { get; }

        /// <summary>
        /// Initializes a new instance of RoutesClearedEventArgs.
        /// </summary>
        /// <param name="removedRules">The removed rules</param>
        /// <param name="removedHandlers">The removed handlers</param>
        public RoutesClearedEventArgs(RoutingRule[] removedRules, RouteHandler[] removedHandlers)
        {
            RemovedRules = removedRules ?? Array.Empty<RoutingRule>();
            RemovedHandlers = removedHandlers ?? Array.Empty<RouteHandler>();
        }
    }

    #endregion

    #region Enumerations

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
    /// Operations performed on route handlers.
    /// </summary>
    public enum RouteHandlerOperation
    {
        /// <summary>
        /// Handler was registered.
        /// </summary>
        Registered,

        /// <summary>
        /// Handler was unregistered.
        /// </summary>
        Unregistered
    }

    #endregion

    #region Attributes

    /// <summary>
    /// Attribute to specify a custom type code for a message type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class MessageTypeCodeAttribute : Attribute
    {
        /// <summary>
        /// Gets the type code.
        /// </summary>
        public ushort TypeCode { get; }

        /// <summary>
        /// Initializes a new instance of MessageTypeCodeAttribute.
        /// </summary>
        /// <param name="typeCode">The type code</param>
        public MessageTypeCodeAttribute(ushort typeCode)
        {
            TypeCode = typeCode;
        }
    }

    /// <summary>
    /// Attribute to specify a category for a message type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class MessageCategoryAttribute : Attribute
    {
        /// <summary>
        /// Gets the category.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Initializes a new instance of MessageCategoryAttribute.
        /// </summary>
        /// <param name="category">The category</param>
        public MessageCategoryAttribute(string category)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
        }
    }

    /// <summary>
    /// Attribute to specify a description for a message type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class MessageDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of MessageDescriptionAttribute.
        /// </summary>
        /// <param name="description">The description</param>
        public MessageDescriptionAttribute(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// Attribute to specify a default priority for a message type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class MessagePriorityAttribute : Attribute
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        public MessagePriority Priority { get; }

        /// <summary>
        /// Initializes a new instance of MessagePriorityAttribute.
        /// </summary>
        /// <param name="priority">The priority</param>
        public MessagePriorityAttribute(MessagePriority priority)
        {
            Priority = priority;
        }
    }

    /// <summary>
    /// Attribute to specify serialization settings for a message type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class MessageSerializableAttribute : Attribute
    {
        /// <summary>
        /// Gets whether the message type is serializable.
        /// </summary>
        public bool IsSerializable { get; }

        /// <summary>
        /// Initializes a new instance of MessageSerializableAttribute.
        /// </summary>
        /// <param name="isSerializable">Whether the type is serializable</param>
        public MessageSerializableAttribute(bool isSerializable = true)
        {
            IsSerializable = isSerializable;
        }
    }

    #endregion
}