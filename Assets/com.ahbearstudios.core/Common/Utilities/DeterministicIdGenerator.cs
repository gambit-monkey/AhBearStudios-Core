using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Profiling;

namespace AhBearStudios.Core.Common.Utilities
{
    /// <summary>
    /// Provides deterministic ID generation using stable algorithms.
    /// Generates consistent IDs from input strings for reliable correlation and tracking.
    /// Follows CLAUDE.md patterns for production-ready utilities with zero-allocation optimizations.
    /// </summary>
    public static class DeterministicIdGenerator
    {
        #region Constants

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core pool operations.
        /// </summary>
        private static readonly Guid PoolNamespaceUuid = new("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core general operations.
        /// </summary>
        private static readonly Guid CoreNamespaceUuid = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core logging operations.
        /// </summary>
        private static readonly Guid LoggingNamespaceUuid = new("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core messaging operations.
        /// </summary>
        private static readonly Guid MessagingNamespaceUuid = new("6ba7b813-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core alerting operations.
        /// </summary>
        private static readonly Guid AlertingNamespaceUuid = new("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core health checking operations.
        /// </summary>
        private static readonly Guid HealthCheckingNamespaceUuid = new("6ba7b815-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core serialization operations.
        /// </summary>
        private static readonly Guid SerializationNamespaceUuid = new("6ba7b816-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        /// Namespace UUID for AhBearStudios.Core profiling operations.
        /// </summary>
        private static readonly Guid ProfilingNamespaceUuid = new("6ba7b817-9dad-11d1-80b4-00c04fd430c8");

        #endregion

        #region Performance Infrastructure

        /// <summary>
        /// Thread-static SHA1 instance to avoid creating new instances for each call.
        /// </summary>
        [ThreadStatic]
        private static SHA1 _sha1Instance;

        /// <summary>
        /// Simple LRU cache for frequently used IDs to avoid repeated generation.
        /// </summary>
        private static readonly Dictionary<string, Guid> _idCache = new Dictionary<string, Guid>(100);
        private static readonly LinkedList<string> _cacheOrder = new LinkedList<string>();
        private static readonly object _cacheLock = new object();
        private const int MaxCacheSize = 100;
        private const int MaxInputLength = 1024; // Prevent DoS via long strings

        /// <summary>
        /// Profiler marker for monitoring ID generation performance.
        /// </summary>
        private static readonly ProfilerMarker _generateMarker = new ProfilerMarker("DeterministicIdGenerator.Generate");

        /// <summary>
        /// Gets or creates a thread-local SHA1 instance.
        /// </summary>
        private static SHA1 GetSHA1Instance()
        {
            return _sha1Instance ??= SHA1.Create();
        }

        #endregion

        #region Pool ID Generation

        /// <summary>
        /// Generates a deterministic Guid for a pool based on its type and name.
        /// Same type and name will always generate the same ID.
        /// </summary>
        /// <param name="poolType">The pool type (e.g., typeof(MyPooledObject).FullName)</param>
        /// <param name="poolName">The pool name (optional)</param>
        /// <returns>A deterministic Guid for the pool</returns>
        public static Guid GeneratePoolId(string poolType, string poolName = null)
        {
            if (string.IsNullOrEmpty(poolType))
                throw new ArgumentException("Pool type cannot be null or empty", nameof(poolType));
            
            if (!ValidateInputLength(poolType))
                throw new ArgumentException($"Pool type length exceeds maximum of {MaxInputLength} characters", nameof(poolType));
                
            if (poolName != null && !ValidateInputLength(poolName))
                throw new ArgumentException($"Pool name length exceeds maximum of {MaxInputLength} characters", nameof(poolName));

            var identifier = string.IsNullOrEmpty(poolName) 
                ? poolType 
                : $"{poolType}:{poolName}";

            return GenerateUuidV5(PoolNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for a pool operation.
        /// Same pool and operation type will always generate the same base ID.
        /// </summary>
        /// <param name="poolType">The pool type</param>
        /// <param name="operationType">The operation type (Get, Return, etc.)</param>
        /// <param name="poolName">The pool name (optional)</param>
        /// <returns>A deterministic Guid for the pool operation</returns>
        public static Guid GeneratePoolOperationId(string poolType, string operationType, string poolName = null)
        {
            if (string.IsNullOrEmpty(poolType))
                throw new ArgumentException("Pool type cannot be null or empty", nameof(poolType));
            
            if (string.IsNullOrEmpty(operationType))
                throw new ArgumentException("Operation type cannot be null or empty", nameof(operationType));

            if (!ValidateInputLength(poolType))
                throw new ArgumentException($"Pool type length exceeds maximum of {MaxInputLength} characters", nameof(poolType));
                
            if (!ValidateInputLength(operationType))
                throw new ArgumentException($"Operation type length exceeds maximum of {MaxInputLength} characters", nameof(operationType));
                
            if (poolName != null && !ValidateInputLength(poolName))
                throw new ArgumentException($"Pool name length exceeds maximum of {MaxInputLength} characters", nameof(poolName));

            var identifier = string.IsNullOrEmpty(poolName) 
                ? $"{poolType}:{operationType}" 
                : $"{poolType}:{poolName}:{operationType}";

            return GenerateUuidV5(PoolNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for a pooled object based on its type and pool context.
        /// This provides a stable ID for tracking objects within their pool lifecycle.
        /// </summary>
        /// <param name="objectType">The object type</param>
        /// <param name="poolName">The pool name</param>
        /// <param name="objectIndex">The object's index or sequence number in the pool</param>
        /// <returns>A deterministic Guid for the pooled object</returns>
        public static Guid GeneratePooledObjectId(string objectType, string poolName, int objectIndex)
        {
            if (string.IsNullOrEmpty(objectType))
                throw new ArgumentException("Object type cannot be null or empty", nameof(objectType));

            if (!ValidateInputLength(objectType))
                throw new ArgumentException($"Object type length exceeds maximum of {MaxInputLength} characters", nameof(objectType));
                
            if (poolName != null && !ValidateInputLength(poolName))
                throw new ArgumentException($"Pool name length exceeds maximum of {MaxInputLength} characters", nameof(poolName));

            var identifier = string.IsNullOrEmpty(poolName) 
                ? $"{objectType}:obj:{objectIndex}" 
                : $"{objectType}:{poolName}:obj:{objectIndex}";

            return GenerateUuidV5(PoolNamespaceUuid, identifier);
        }

        #endregion

        #region Message ID Generation

        /// <summary>
        /// Generates a deterministic Guid for messages based on message type and correlation context.
        /// Provides consistent message IDs for tracking and correlation.
        /// </summary>
        /// <param name="messageType">The message type name</param>
        /// <param name="correlationId">The correlation ID for context</param>
        /// <param name="timestamp">Optional timestamp for uniqueness (use for time-based uniqueness)</param>
        /// <returns>A deterministic Guid for the message</returns>
        public static Guid GenerateMessageId(string messageType, string correlationId, DateTime? timestamp = null)
        {
            if (string.IsNullOrEmpty(messageType))
                throw new ArgumentException("Message type cannot be null or empty", nameof(messageType));

            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

            if (!ValidateInputLength(messageType))
                throw new ArgumentException($"Message type length exceeds maximum of {MaxInputLength} characters", nameof(messageType));
                
            if (!ValidateInputLength(correlationId))
                throw new ArgumentException($"Correlation ID length exceeds maximum of {MaxInputLength} characters", nameof(correlationId));

            var identifier = timestamp.HasValue 
                ? $"{messageType}:{correlationId}:{timestamp.Value.Ticks}" 
                : $"{messageType}:{correlationId}";

            return GenerateUuidV5(CoreNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for pool messages with object context.
        /// Provides consistent message IDs for pool operation tracking.
        /// </summary>
        /// <param name="messageType">The message type name</param>
        /// <param name="poolType">The pool type</param>
        /// <param name="poolName">The pool name</param>
        /// <param name="objectId">The object ID involved in the message</param>
        /// <param name="correlationId">The correlation ID for context</param>
        /// <returns>A deterministic Guid for the pool message</returns>
        public static Guid GeneratePoolMessageId(
            string messageType, 
            string poolType, 
            string poolName, 
            Guid objectId, 
            string correlationId)
        {
            if (string.IsNullOrEmpty(messageType))
                throw new ArgumentException("Message type cannot be null or empty", nameof(messageType));

            if (string.IsNullOrEmpty(poolType))
                throw new ArgumentException("Pool type cannot be null or empty", nameof(poolType));

            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

            if (!ValidateInputLength(messageType))
                throw new ArgumentException($"Message type length exceeds maximum of {MaxInputLength} characters", nameof(messageType));
                
            if (!ValidateInputLength(poolType))
                throw new ArgumentException($"Pool type length exceeds maximum of {MaxInputLength} characters", nameof(poolType));
                
            if (!ValidateInputLength(correlationId))
                throw new ArgumentException($"Correlation ID length exceeds maximum of {MaxInputLength} characters", nameof(correlationId));
                
            if (poolName != null && !ValidateInputLength(poolName))
                throw new ArgumentException($"Pool name length exceeds maximum of {MaxInputLength} characters", nameof(poolName));

            var identifier = string.IsNullOrEmpty(poolName) 
                ? $"{messageType}:{poolType}:{objectId:N}:{correlationId}" 
                : $"{messageType}:{poolType}:{poolName}:{objectId:N}:{correlationId}";

            return GenerateUuidV5(PoolNamespaceUuid, identifier);
        }

        #endregion

        #region Core ID Generation

        /// <summary>
        /// Generates a deterministic Guid for any string-based identifier using the core namespace.
        /// Provides consistent IDs for general system entities.
        /// </summary>
        /// <param name="identifier">The string identifier to generate a Guid for</param>
        /// <returns>A deterministic Guid for the identifier</returns>
        public static Guid GenerateCoreId(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

            if (!ValidateInputLength(identifier))
                throw new ArgumentException($"Identifier length exceeds maximum of {MaxInputLength} characters", nameof(identifier));

            return GenerateUuidV5(CoreNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid using a custom namespace and identifier.
        /// Provides maximum flexibility for specialized ID generation.
        /// </summary>
        /// <param name="namespaceUuid">The namespace UUID</param>
        /// <param name="identifier">The string identifier</param>
        /// <returns>A deterministic Guid for the namespace and identifier</returns>
        public static Guid GenerateCustomId(Guid namespaceUuid, string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));

            if (!ValidateInputLength(identifier))
                throw new ArgumentException($"Identifier length exceeds maximum of {MaxInputLength} characters", nameof(identifier));

            return GenerateUuidV5(namespaceUuid, identifier);
        }

        #endregion

        #region Unity Collections Support

        /// <summary>
        /// Generates a deterministic FixedString64Bytes for pool operations.
        /// Optimized for Unity Collections and Burst compilation.
        /// </summary>
        /// <param name="poolType">The pool type</param>
        /// <param name="poolName">The pool name (optional)</param>
        /// <returns>A deterministic FixedString64Bytes for the pool</returns>
        public static FixedString64Bytes GeneratePoolFixedString(string poolType, string poolName = null)
        {
            var guid = GeneratePoolId(poolType, poolName);
            var guidString = guid.ToString("N")[..16]; // Take first 16 chars to fit in FixedString64Bytes
            return new FixedString64Bytes(guidString);
        }

        /// <summary>
        /// Generates a deterministic FixedString128Bytes for correlation IDs.
        /// Optimized for Unity Collections and Burst compilation.
        /// </summary>
        /// <param name="identifier">The base identifier</param>
        /// <param name="context">Additional context (optional)</param>
        /// <returns>A deterministic FixedString128Bytes for correlation</returns>
        public static FixedString128Bytes GenerateCorrelationFixedString(string identifier, string context = null)
        {
            var fullIdentifier = string.IsNullOrEmpty(context) 
                ? identifier 
                : $"{identifier}:{context}";
                
            var guid = GenerateCoreId(fullIdentifier);
            return new FixedString128Bytes(guid.ToString("N"));
        }

        #endregion

        #region Logging System ID Generation

        /// <summary>
        /// Generates a deterministic Guid for log entries based on level, source, and message context.
        /// Provides consistent log entry identification for correlation and debugging.
        /// </summary>
        /// <param name="logLevel">The log level (Debug, Info, Warning, Error, Critical)</param>
        /// <param name="source">The source of the log entry (class or component name)</param>
        /// <param name="messageContext">Optional message context or hash for uniqueness</param>
        /// <returns>A deterministic Guid for the log entry</returns>
        public static Guid GenerateLogEntryId(string logLevel, string source, string messageContext = null)
        {
            if (string.IsNullOrEmpty(logLevel))
                throw new ArgumentException("Log level cannot be null or empty", nameof(logLevel));
            
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            var identifier = string.IsNullOrEmpty(messageContext) 
                ? $"LogEntry:{logLevel}:{source}" 
                : $"LogEntry:{logLevel}:{source}:{messageContext}";

            return GenerateUuidV5(LoggingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for log correlation across operations.
        /// Provides stable correlation IDs for tracking related log entries.
        /// </summary>
        /// <param name="operation">The operation name or identifier</param>
        /// <param name="context">Additional context (user, session, etc.)</param>
        /// <returns>A deterministic Guid for log correlation</returns>
        public static Guid GenerateLogCorrelationId(string operation, string context = null)
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));

            var identifier = string.IsNullOrEmpty(context) 
                ? $"LogCorrelation:{operation}" 
                : $"LogCorrelation:{operation}:{context}";

            return GenerateUuidV5(LoggingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for log scopes based on scope name and context.
        /// Provides consistent scope identification for hierarchical logging.
        /// </summary>
        /// <param name="scopeName">The name of the log scope</param>
        /// <param name="parentScopeId">Optional parent scope ID for nesting</param>
        /// <returns>A deterministic Guid for the log scope</returns>
        public static Guid GenerateLogScopeId(string scopeName, Guid? parentScopeId = null)
        {
            if (string.IsNullOrEmpty(scopeName))
                throw new ArgumentException("Scope name cannot be null or empty", nameof(scopeName));

            var identifier = parentScopeId.HasValue 
                ? $"LogScope:{scopeName}:{parentScopeId.Value:N}" 
                : $"LogScope:{scopeName}";

            return GenerateUuidV5(LoggingNamespaceUuid, identifier);
        }

        #endregion

        #region Messaging System ID Generation

        /// <summary>
        /// Generates a deterministic Guid for messages based on type, source, and correlation.
        /// Provides consistent message identification for routing and tracking.
        /// </summary>
        /// <param name="messageType">The message type name</param>
        /// <param name="source">The source of the message</param>
        /// <param name="correlationId">Optional correlation ID for context</param>
        /// <returns>A deterministic Guid for the message</returns>
        public static Guid GenerateMessageId(string messageType, string source, string correlationId = null)
        {
            if (string.IsNullOrEmpty(messageType))
                throw new ArgumentException("Message type cannot be null or empty", nameof(messageType));

            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            if (!ValidateInputLength(messageType))
                throw new ArgumentException($"Message type length exceeds maximum of {MaxInputLength} characters", nameof(messageType));
                
            if (!ValidateInputLength(source))
                throw new ArgumentException($"Source length exceeds maximum of {MaxInputLength} characters", nameof(source));
                
            if (correlationId != null && !ValidateInputLength(correlationId))
                throw new ArgumentException($"Correlation ID length exceeds maximum of {MaxInputLength} characters", nameof(correlationId));

            var identifier = string.IsNullOrEmpty(correlationId) 
                ? $"Message:{messageType}:{source}" 
                : $"Message:{messageType}:{source}:{correlationId}";

            return GenerateUuidV5(MessagingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for message subscriptions.
        /// Provides consistent subscription identification for management and cleanup.
        /// </summary>
        /// <param name="messageType">The message type being subscribed to</param>
        /// <param name="subscriberName">The name or identifier of the subscriber</param>
        /// <param name="subscriptionContext">Optional context for the subscription</param>
        /// <returns>A deterministic Guid for the subscription</returns>
        public static Guid GenerateSubscriptionId(string messageType, string subscriberName, string subscriptionContext = null)
        {
            if (string.IsNullOrEmpty(messageType))
                throw new ArgumentException("Message type cannot be null or empty", nameof(messageType));

            if (string.IsNullOrEmpty(subscriberName))
                throw new ArgumentException("Subscriber name cannot be null or empty", nameof(subscriberName));

            var identifier = string.IsNullOrEmpty(subscriptionContext) 
                ? $"Subscription:{messageType}:{subscriberName}" 
                : $"Subscription:{messageType}:{subscriberName}:{subscriptionContext}";

            return GenerateUuidV5(MessagingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for message routing rules.
        /// Provides consistent rule identification for routing configuration.
        /// </summary>
        /// <param name="sourcePattern">The source pattern for routing</param>
        /// <param name="targetPattern">The target pattern for routing</param>
        /// <param name="ruleType">The type of routing rule</param>
        /// <returns>A deterministic Guid for the routing rule</returns>
        public static Guid GenerateRoutingRuleId(string sourcePattern, string targetPattern, string ruleType = "Default")
        {
            if (string.IsNullOrEmpty(sourcePattern))
                throw new ArgumentException("Source pattern cannot be null or empty", nameof(sourcePattern));

            if (string.IsNullOrEmpty(targetPattern))
                throw new ArgumentException("Target pattern cannot be null or empty", nameof(targetPattern));

            var identifier = $"RoutingRule:{ruleType}:{sourcePattern}:{targetPattern}";
            return GenerateUuidV5(MessagingNamespaceUuid, identifier);
        }

        #endregion

        #region Alerting System ID Generation

        /// <summary>
        /// Generates a deterministic Guid for alerts based on severity, source, and message content.
        /// Provides consistent alert identification for tracking and deduplication.
        /// </summary>
        /// <param name="severity">The alert severity level</param>
        /// <param name="source">The source of the alert</param>
        /// <param name="messageHash">Hash or key of the alert message for uniqueness</param>
        /// <returns>A deterministic Guid for the alert</returns>
        public static Guid GenerateAlertId(string severity, string source, string messageHash)
        {
            if (string.IsNullOrEmpty(severity))
                throw new ArgumentException("Severity cannot be null or empty", nameof(severity));

            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            if (string.IsNullOrEmpty(messageHash))
                throw new ArgumentException("Message hash cannot be null or empty", nameof(messageHash));

            var identifier = $"Alert:{severity}:{source}:{messageHash}";
            return GenerateUuidV5(AlertingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for alert channels based on type and configuration.
        /// Provides consistent channel identification for alert routing.
        /// </summary>
        /// <param name="channelType">The type of alert channel (Email, Slack, etc.)</param>
        /// <param name="channelConfig">Configuration identifier or hash</param>
        /// <returns>A deterministic Guid for the alert channel</returns>
        public static Guid GenerateAlertChannelId(string channelType, string channelConfig)
        {
            if (string.IsNullOrEmpty(channelType))
                throw new ArgumentException("Channel type cannot be null or empty", nameof(channelType));

            if (string.IsNullOrEmpty(channelConfig))
                throw new ArgumentException("Channel config cannot be null or empty", nameof(channelConfig));

            var identifier = $"AlertChannel:{channelType}:{channelConfig}";
            return GenerateUuidV5(AlertingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for alert filters based on configuration.
        /// Provides consistent filter identification for alert processing.
        /// </summary>
        /// <param name="filterType">The type of alert filter</param>
        /// <param name="filterConfig">Configuration identifier or hash</param>
        /// <returns>A deterministic Guid for the alert filter</returns>
        public static Guid GenerateAlertFilterId(string filterType, string filterConfig)
        {
            if (string.IsNullOrEmpty(filterType))
                throw new ArgumentException("Filter type cannot be null or empty", nameof(filterType));

            if (string.IsNullOrEmpty(filterConfig))
                throw new ArgumentException("Filter config cannot be null or empty", nameof(filterConfig));

            var identifier = $"AlertFilter:{filterType}:{filterConfig}";
            return GenerateUuidV5(AlertingNamespaceUuid, identifier);
        }

        #endregion

        #region Health Checking System ID Generation

        /// <summary>
        /// Generates a deterministic Guid for health checks based on name and target system.
        /// Provides consistent health check identification for tracking and reporting.
        /// </summary>
        /// <param name="checkName">The name of the health check</param>
        /// <param name="targetSystem">The system being checked</param>
        /// <param name="checkType">Optional check type for further categorization</param>
        /// <returns>A deterministic Guid for the health check</returns>
        public static Guid GenerateHealthCheckId(string checkName, string targetSystem, string checkType = null)
        {
            if (string.IsNullOrEmpty(checkName))
                throw new ArgumentException("Check name cannot be null or empty", nameof(checkName));

            if (string.IsNullOrEmpty(targetSystem))
                throw new ArgumentException("Target system cannot be null or empty", nameof(targetSystem));

            var identifier = string.IsNullOrEmpty(checkType) 
                ? $"HealthCheck:{checkName}:{targetSystem}" 
                : $"HealthCheck:{checkType}:{checkName}:{targetSystem}";

            return GenerateUuidV5(HealthCheckingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for circuit breakers based on name and configuration.
        /// Provides consistent circuit breaker identification for state tracking.
        /// </summary>
        /// <param name="circuitBreakerName">The name of the circuit breaker</param>
        /// <param name="targetService">The service being protected</param>
        /// <param name="configHash">Optional configuration hash for uniqueness</param>
        /// <returns>A deterministic Guid for the circuit breaker</returns>
        public static Guid GenerateCircuitBreakerId(string circuitBreakerName, string targetService, string configHash = null)
        {
            if (string.IsNullOrEmpty(circuitBreakerName))
                throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(circuitBreakerName));

            if (string.IsNullOrEmpty(targetService))
                throw new ArgumentException("Target service cannot be null or empty", nameof(targetService));

            var identifier = string.IsNullOrEmpty(configHash) 
                ? $"CircuitBreaker:{circuitBreakerName}:{targetService}" 
                : $"CircuitBreaker:{circuitBreakerName}:{targetService}:{configHash}";

            return GenerateUuidV5(HealthCheckingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for health check runs based on check ID and timestamp context.
        /// Provides consistent run identification for tracking check execution.
        /// </summary>
        /// <param name="healthCheckId">The ID of the health check</param>
        /// <param name="runContext">Context for the run (date, trigger, etc.)</param>
        /// <returns>A deterministic Guid for the health check run</returns>
        public static Guid GenerateHealthCheckRunId(Guid healthCheckId, string runContext)
        {
            if (healthCheckId == Guid.Empty)
                throw new ArgumentException("Health check ID cannot be empty", nameof(healthCheckId));

            if (string.IsNullOrEmpty(runContext))
                throw new ArgumentException("Run context cannot be null or empty", nameof(runContext));

            var identifier = $"HealthCheckRun:{healthCheckId:N}:{runContext}";
            return GenerateUuidV5(HealthCheckingNamespaceUuid, identifier);
        }

        #endregion

        #region Serialization System ID Generation

        /// <summary>
        /// Generates a deterministic Guid for serialization cache keys based on type and context.
        /// Provides consistent cache key identification for serialization optimization.
        /// </summary>
        /// <param name="objectType">The type being serialized</param>
        /// <param name="serializationContext">Context for serialization (version, options, etc.)</param>
        /// <returns>A deterministic Guid for the cache key</returns>
        public static Guid GenerateSerializationCacheId(string objectType, string serializationContext)
        {
            if (string.IsNullOrEmpty(objectType))
                throw new ArgumentException("Object type cannot be null or empty", nameof(objectType));

            if (string.IsNullOrEmpty(serializationContext))
                throw new ArgumentException("Serialization context cannot be null or empty", nameof(serializationContext));

            var identifier = $"SerializationCache:{objectType}:{serializationContext}";
            return GenerateUuidV5(SerializationNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for version tracking in serialization.
        /// Provides consistent version identification for backwards compatibility.
        /// </summary>
        /// <param name="objectType">The type being versioned</param>
        /// <param name="versionNumber">The version number</param>
        /// <param name="versionContext">Optional version context</param>
        /// <returns>A deterministic Guid for version tracking</returns>
        public static Guid GenerateSerializationVersionId(string objectType, string versionNumber, string versionContext = null)
        {
            if (string.IsNullOrEmpty(objectType))
                throw new ArgumentException("Object type cannot be null or empty", nameof(objectType));

            if (string.IsNullOrEmpty(versionNumber))
                throw new ArgumentException("Version number cannot be null or empty", nameof(versionNumber));

            var identifier = string.IsNullOrEmpty(versionContext) 
                ? $"SerializationVersion:{objectType}:{versionNumber}" 
                : $"SerializationVersion:{objectType}:{versionNumber}:{versionContext}";

            return GenerateUuidV5(SerializationNamespaceUuid, identifier);
        }

        #endregion

        #region Profiling System ID Generation

        /// <summary>
        /// Generates a deterministic Guid for profiling scopes based on operation and context.
        /// Provides consistent scope identification for performance tracking.
        /// </summary>
        /// <param name="operation">The operation being profiled</param>
        /// <param name="context">Additional context (method, class, etc.)</param>
        /// <returns>A deterministic Guid for the profiling scope</returns>
        public static Guid GenerateProfilingScopeId(string operation, string context = null)
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));

            var identifier = string.IsNullOrEmpty(context) 
                ? $"ProfilingScope:{operation}" 
                : $"ProfilingScope:{operation}:{context}";

            return GenerateUuidV5(ProfilingNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for performance metrics based on metric name and context.
        /// Provides consistent metric identification for tracking and aggregation.
        /// </summary>
        /// <param name="metricName">The name of the performance metric</param>
        /// <param name="metricContext">Context for the metric (component, operation, etc.)</param>
        /// <returns>A deterministic Guid for the performance metric</returns>
        public static Guid GeneratePerformanceMetricId(string metricName, string metricContext)
        {
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            if (string.IsNullOrEmpty(metricContext))
                throw new ArgumentException("Metric context cannot be null or empty", nameof(metricContext));

            var identifier = $"PerformanceMetric:{metricName}:{metricContext}";
            return GenerateUuidV5(ProfilingNamespaceUuid, identifier);
        }

        #endregion

        #region Enhanced General ID Generation

        /// <summary>
        /// Enhanced correlation ID generation with support for hierarchical operations.
        /// Provides consistent correlation tracking across complex operation chains.
        /// </summary>
        /// <param name="operation">The operation name or identifier</param>
        /// <param name="context">Additional context (user, session, request, etc.)</param>
        /// <param name="parentCorrelationId">Optional parent correlation for hierarchical tracking</param>
        /// <returns>A deterministic Guid for correlation tracking</returns>
        public static Guid GenerateCorrelationId(string operation, string context = null, Guid? parentCorrelationId = null)
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("Operation cannot be null or empty", nameof(operation));

            if (!ValidateInputLength(operation))
                throw new ArgumentException($"Operation length exceeds maximum of {MaxInputLength} characters", nameof(operation));
                
            if (context != null && !ValidateInputLength(context))
                throw new ArgumentException($"Context length exceeds maximum of {MaxInputLength} characters", nameof(context));

            var identifier = parentCorrelationId.HasValue 
                ? $"Correlation:{operation}:{context ?? "none"}:{parentCorrelationId.Value:N}" 
                : $"Correlation:{operation}:{context ?? "none"}";

            return GenerateUuidV5(CoreNamespaceUuid, identifier);
        }

        /// <summary>
        /// Generates a deterministic Guid for session tracking across systems.
        /// Provides consistent session identification for user activity tracking.
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="sessionStart">The session start time or identifier</param>
        /// <param name="sessionContext">Optional session context (device, location, etc.)</param>
        /// <returns>A deterministic Guid for session tracking</returns>
        public static Guid GenerateSessionId(string userId, string sessionStart, string sessionContext = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            if (string.IsNullOrEmpty(sessionStart))
                throw new ArgumentException("Session start cannot be null or empty", nameof(sessionStart));

            if (!ValidateInputLength(userId))
                throw new ArgumentException($"User ID length exceeds maximum of {MaxInputLength} characters", nameof(userId));
                
            if (!ValidateInputLength(sessionStart))
                throw new ArgumentException($"Session start length exceeds maximum of {MaxInputLength} characters", nameof(sessionStart));
                
            if (sessionContext != null && !ValidateInputLength(sessionContext))
                throw new ArgumentException($"Session context length exceeds maximum of {MaxInputLength} characters", nameof(sessionContext));

            var identifier = string.IsNullOrEmpty(sessionContext) 
                ? $"Session:{userId}:{sessionStart}" 
                : $"Session:{userId}:{sessionStart}:{sessionContext}";

            return GenerateUuidV5(CoreNamespaceUuid, identifier);
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Attempts to get an ID from cache, or computes and caches it if not found.
        /// </summary>
        /// <param name="cacheKey">The cache key</param>
        /// <param name="generator">Function to generate the ID if not cached</param>
        /// <returns>The cached or newly generated ID</returns>
        private static Guid GetOrCreateCachedId(string cacheKey, Func<Guid> generator)
        {
            // Quick check without lock for performance
            lock (_cacheLock)
            {
                if (_idCache.TryGetValue(cacheKey, out var cachedId))
                {
                    // Move to end of LRU list
                    _cacheOrder.Remove(cacheKey);
                    _cacheOrder.AddLast(cacheKey);
                    return cachedId;
                }
            }

            // Generate new ID
            var newId = generator();

            // Cache it with LRU eviction
            lock (_cacheLock)
            {
                if (_idCache.Count >= MaxCacheSize)
                {
                    // Remove oldest item
                    var oldest = _cacheOrder.First.Value;
                    _cacheOrder.RemoveFirst();
                    _idCache.Remove(oldest);
                }

                _idCache[cacheKey] = newId;
                _cacheOrder.AddLast(cacheKey);
            }

            return newId;
        }

        /// <summary>
        /// Validates input length to prevent DoS attacks.
        /// </summary>
        /// <param name="input">Input string to validate</param>
        /// <returns>True if input is valid length</returns>
        private static bool ValidateInputLength(string input)
        {
            return input != null && input.Length <= MaxInputLength;
        }

        /// <summary>
        /// Generates a UUID v5 (name-based with SHA-1) from a namespace UUID and name string.
        /// This ensures deterministic generation - same input always produces same output.
        /// Optimized with caching and object pooling.
        /// </summary>
        /// <param name="namespaceId">The namespace UUID</param>
        /// <param name="name">The name string</param>
        /// <returns>A deterministic UUID v5</returns>
        private static Guid GenerateUuidV5(Guid namespaceId, string name)
        {
            using (_generateMarker.Auto())
            {
                // Input validation
                if (!ValidateInputLength(name))
                {
                    throw new ArgumentException($"Input length exceeds maximum of {MaxInputLength} characters", nameof(name));
                }

                // Create cache key
                var cacheKey = $"{namespaceId:N}:{name}";
                
                return GetOrCreateCachedId(cacheKey, () => GenerateUuidV5Internal(namespaceId, name));
            }
        }

        /// <summary>
        /// Internal UUID v5 generation with optimized allocations.
        /// </summary>
        private static Guid GenerateUuidV5Internal(Guid namespaceId, string name)
        {
            // Convert namespace UUID to byte array in network byte order
            var namespaceBytes = namespaceId.ToByteArray();
            SwapByteOrder(namespaceBytes);

            // Convert name to UTF-8 bytes
            var nameBytes = Encoding.UTF8.GetBytes(name);

            // Use ArrayPool for hash input to reduce allocations
            var hashInputLength = namespaceBytes.Length + nameBytes.Length;
            var hashInput = ArrayPool<byte>.Shared.Rent(hashInputLength);
            
            try
            {
                // Combine namespace and name bytes
                Array.Copy(namespaceBytes, 0, hashInput, 0, namespaceBytes.Length);
                Array.Copy(nameBytes, 0, hashInput, namespaceBytes.Length, nameBytes.Length);

                // Generate SHA-1 hash using pooled instance
                var sha1 = GetSHA1Instance();
                var hash = sha1.ComputeHash(hashInput, 0, hashInputLength);

                // Use ArrayPool for UUID bytes
                var uuidBytes = ArrayPool<byte>.Shared.Rent(16);
                try
                {
                    // Take first 16 bytes of hash for UUID
                    Array.Copy(hash, 0, uuidBytes, 0, 16);

                    // Set version (5) and variant bits according to RFC 4122
                    uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x50); // Version 5
                    uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80); // Variant 10

                    // Convert back to network byte order for Guid
                    SwapByteOrder(uuidBytes);

                    return new Guid(new ReadOnlySpan<byte>(uuidBytes, 0, 16));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(uuidBytes);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(hashInput);
            }
        }

        /// <summary>
        /// Swaps byte order for proper UUID network byte order handling.
        /// </summary>
        /// <param name="bytes">The byte array to swap (modified in place)</param>
        private static void SwapByteOrder(byte[] bytes)
        {
            if (bytes.Length >= 4)
            {
                // Swap bytes for first 32-bit field
                (bytes[0], bytes[3]) = (bytes[3], bytes[0]);
                (bytes[1], bytes[2]) = (bytes[2], bytes[1]);
            }

            if (bytes.Length >= 6)
            {
                // Swap bytes for second 16-bit field
                (bytes[4], bytes[5]) = (bytes[5], bytes[4]);
            }

            if (bytes.Length >= 8)
            {
                // Swap bytes for third 16-bit field
                (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
            }
        }

        #endregion
    }
}