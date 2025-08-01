using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;

namespace AhBearStudios.Core.Common.Models
{
    /// <summary>
    /// Represents correlation information for tracking operations across system boundaries.
    /// Designed for high-performance scenarios with minimal allocations using Unity.Collections v2.
    /// Provides comprehensive context for distributed tracing and log correlation.
    /// </summary>
    [BurstCompile]
    public readonly struct CorrelationInfo : IDisposable
    {
        /// <summary>
        /// Gets the primary correlation ID for this operation.
        /// </summary>
        public readonly FixedString128Bytes CorrelationId;

        /// <summary>
        /// Gets the parent correlation ID, if this is a child operation.
        /// </summary>
        public readonly FixedString128Bytes ParentCorrelationId;

        /// <summary>
        /// Gets the root correlation ID for the entire operation chain.
        /// </summary>
        public readonly FixedString128Bytes RootCorrelationId;

        /// <summary>
        /// Gets the span ID for distributed tracing.
        /// </summary>
        public readonly FixedString64Bytes SpanId;

        /// <summary>
        /// Gets the trace ID for distributed tracing.
        /// </summary>
        public readonly FixedString64Bytes TraceId;

        /// <summary>
        /// Gets the operation name or identifier.
        /// </summary>
        public readonly FixedString128Bytes Operation;

        /// <summary>
        /// Gets the user ID associated with this operation.
        /// </summary>
        public readonly FixedString64Bytes UserId;

        /// <summary>
        /// Gets the session ID associated with this operation.
        /// </summary>
        public readonly FixedString64Bytes SessionId;

        /// <summary>
        /// Gets the request ID for HTTP or service requests.
        /// </summary>
        public readonly FixedString64Bytes RequestId;

        /// <summary>
        /// Gets the service name that initiated this operation.
        /// </summary>
        public readonly FixedString64Bytes ServiceName;

        /// <summary>
        /// Gets the timestamp when this correlation was created.
        /// </summary>
        public readonly DateTime CreatedAt;

        /// <summary>
        /// Gets the depth in the operation hierarchy (0 for root).
        /// </summary>
        public readonly int Depth;

        /// <summary>
        /// Gets whether this correlation has additional properties.
        /// </summary>
        public readonly bool HasProperties;

        // Non-Burst compatible fields for rich data (managed separately)
        private readonly IReadOnlyDictionary<string, object> _properties;

        /// <summary>
        /// Gets additional contextual properties for correlation (not Burst-compatible).
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => _properties ?? EmptyProperties;

        /// <summary>
        /// Empty properties dictionary to avoid allocations.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, object> EmptyProperties = 
            new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the CorrelationInfo struct.
        /// </summary>
        /// <param name="correlationId">The primary correlation ID</param>
        /// <param name="parentCorrelationId">The parent correlation ID</param>
        /// <param name="rootCorrelationId">The root correlation ID</param>
        /// <param name="spanId">The span ID for distributed tracing</param>
        /// <param name="traceId">The trace ID for distributed tracing</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="serviceName">The service name</param>
        /// <param name="createdAt">The creation timestamp</param>
        /// <param name="depth">The depth in the operation hierarchy</param>
        /// <param name="properties">Additional contextual properties</param>
        public CorrelationInfo(
            FixedString128Bytes correlationId,
            FixedString128Bytes parentCorrelationId = default,
            FixedString128Bytes rootCorrelationId = default,
            FixedString64Bytes spanId = default,
            FixedString64Bytes traceId = default,
            FixedString128Bytes operation = default,
            FixedString64Bytes userId = default,
            FixedString64Bytes sessionId = default,
            FixedString64Bytes requestId = default,
            FixedString64Bytes serviceName = default,
            DateTime createdAt = default,
            int depth = 0,
            IReadOnlyDictionary<string, object> properties = null)
        {
            CorrelationId = correlationId;
            ParentCorrelationId = parentCorrelationId;
            RootCorrelationId = rootCorrelationId.IsEmpty ? correlationId : rootCorrelationId;
            SpanId = spanId;
            TraceId = traceId;
            Operation = operation;
            UserId = userId;
            SessionId = sessionId;
            RequestId = requestId;
            ServiceName = serviceName.IsEmpty ? new FixedString64Bytes("Unknown") : serviceName;
            CreatedAt = createdAt == default ? DateTime.UtcNow : createdAt;
            Depth = depth;
            HasProperties = properties != null && properties.Count > 0;
            _properties = properties;
        }

        /// <summary>
        /// Creates a new CorrelationInfo with a generated correlation ID.
        /// </summary>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="serviceName">The service name</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <returns>A new CorrelationInfo instance</returns>
        [BurstCompile]
        public static CorrelationInfo Create(
            string operation = null,
            string userId = null,
            string sessionId = null,
            string requestId = null,
            string serviceName = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            var correlationId = new FixedString128Bytes(Guid.NewGuid().ToString("N"));
            var spanId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
            var traceId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation ?? string.Empty),
                userId: new FixedString64Bytes(userId ?? string.Empty),
                sessionId: new FixedString64Bytes(sessionId ?? string.Empty),
                requestId: new FixedString64Bytes(requestId ?? string.Empty),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                properties: properties);
        }

        /// <summary>
        /// Creates a child CorrelationInfo that inherits from this one.
        /// </summary>
        /// <param name="childOperation">The child operation name</param>
        /// <param name="additionalProperties">Additional properties to merge</param>
        /// <returns>A new child CorrelationInfo instance</returns>
        public CorrelationInfo CreateChild(
            string childOperation = null,
            IReadOnlyDictionary<string, object> additionalProperties = null)
        {
            var childCorrelationId = new FixedString128Bytes(Guid.NewGuid().ToString("N"));
            var childSpanId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);

            var mergedProperties = new Dictionary<string, object>();
            if (HasProperties)
            {
                foreach (var kvp in Properties)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }

            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    mergedProperties[kvp.Key] = kvp.Value;
                }
            }

            return new CorrelationInfo(
                correlationId: childCorrelationId,
                parentCorrelationId: CorrelationId,
                rootCorrelationId: RootCorrelationId,
                spanId: childSpanId,
                traceId: TraceId,
                operation: new FixedString128Bytes(childOperation ?? Operation.ToString()),
                userId: UserId,
                sessionId: SessionId,
                requestId: RequestId,
                serviceName: ServiceName,
                depth: Depth + 1,
                properties: mergedProperties.Count > 0 ? mergedProperties : null);
        }

        /// <summary>
        /// Creates a CorrelationInfo from string values.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="parentCorrelationId">The parent correlation ID</param>
        /// <param name="rootCorrelationId">The root correlation ID</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="serviceName">The service name</param>
        /// <param name="depth">The depth in the operation hierarchy</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <returns>A new CorrelationInfo instance</returns>
        public static CorrelationInfo FromStrings(
            string correlationId,
            string parentCorrelationId = null,
            string rootCorrelationId = null,
            string operation = null,
            string userId = null,
            string sessionId = null,
            string requestId = null,
            string serviceName = null,
            int depth = 0,
            IReadOnlyDictionary<string, object> properties = null)
        {
            return new CorrelationInfo(
                correlationId: new FixedString128Bytes(correlationId ?? Guid.NewGuid().ToString("N")),
                parentCorrelationId: new FixedString128Bytes(parentCorrelationId ?? string.Empty),
                rootCorrelationId: new FixedString128Bytes(rootCorrelationId ?? string.Empty),
                spanId: new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]),
                traceId: new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]),
                operation: new FixedString128Bytes(operation ?? string.Empty),
                userId: new FixedString64Bytes(userId ?? string.Empty),
                sessionId: new FixedString64Bytes(sessionId ?? string.Empty),
                requestId: new FixedString64Bytes(requestId ?? string.Empty),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                depth: depth,
                properties: properties);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a request operation.
        /// </summary>
        /// <param name="requestId">The request identifier</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">Optional user identifier</param>
        /// <param name="sessionId">Optional session identifier</param>
        /// <param name="serviceName">Optional service name</param>
        /// <returns>A new CorrelationInfo instance optimized for request tracking</returns>
        public static CorrelationInfo ForRequest(
            string requestId,
            string operation,
            string userId = null,
            string sessionId = null,
            string serviceName = null)
        {
            var correlationId = new FixedString128Bytes(requestId);
            var spanId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
            var traceId = new FixedString64Bytes(requestId[..16]);

            var requestProperties = new Dictionary<string, object>
            {
                ["CorrelationType"] = "Request",
                ["RequestStartTime"] = DateTime.UtcNow
            };

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation),
                userId: new FixedString64Bytes(userId ?? string.Empty),
                sessionId: new FixedString64Bytes(sessionId ?? string.Empty),
                requestId: new FixedString64Bytes(requestId),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                properties: requestProperties);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a scope operation.
        /// </summary>
        /// <param name="scopeId">The scope identifier</param>
        /// <param name="operation">The operation name</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID</param>
        /// <param name="serviceName">Optional service name</param>
        /// <returns>A new CorrelationInfo instance optimized for scope tracking</returns>
        public static CorrelationInfo ForScope(
            string scopeId,
            string operation,
            string parentCorrelationId = null,
            string serviceName = null)
        {
            var correlationId = new FixedString128Bytes(scopeId);
            var spanId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
            var traceId = new FixedString64Bytes(scopeId[..16]);

            var scopeProperties = new Dictionary<string, object>
            {
                ["CorrelationType"] = "Scope",
                ["ScopeStartTime"] = DateTime.UtcNow,
                ["ScopeId"] = scopeId
            };

            return new CorrelationInfo(
                correlationId: correlationId,
                parentCorrelationId: new FixedString128Bytes(parentCorrelationId ?? string.Empty),
                rootCorrelationId: string.IsNullOrEmpty(parentCorrelationId) ? correlationId : new FixedString128Bytes(parentCorrelationId),
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                depth: string.IsNullOrEmpty(parentCorrelationId) ? 0 : 1,
                properties: scopeProperties);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a background operation.
        /// </summary>
        /// <param name="operation">The background operation name</param>
        /// <param name="serviceName">Optional service name</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <returns>A new CorrelationInfo instance optimized for background operations</returns>
        public static CorrelationInfo ForBackground(
            string operation,
            string serviceName = null,
            IReadOnlyDictionary<string, object> properties = null)
        {
            var correlationId = new FixedString128Bytes(Guid.NewGuid().ToString("N"));
            var spanId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
            var traceId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);

            var backgroundProperties = new Dictionary<string, object>
            {
                ["CorrelationType"] = "Background",
                ["BackgroundStartTime"] = DateTime.UtcNow,
                ["ThreadId"] = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    backgroundProperties[kvp.Key] = kvp.Value;
                }
            }

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                properties: backgroundProperties);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a health check operation.
        /// </summary>
        /// <param name="healthCheckName">The health check name</param>
        /// <param name="serviceName">Optional service name</param>
        /// <returns>A new CorrelationInfo instance optimized for health check operations</returns>
        public static CorrelationInfo ForHealthCheck(
            string healthCheckName,
            string serviceName = null)
        {
            var correlationId = new FixedString128Bytes(Guid.NewGuid().ToString("N"));
            var spanId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);
            var traceId = new FixedString64Bytes(Guid.NewGuid().ToString("N")[..16]);

            var healthCheckProperties = new Dictionary<string, object>
            {
                ["CorrelationType"] = "HealthCheck",
                ["HealthCheckStartTime"] = DateTime.UtcNow,
                ["HealthCheckName"] = healthCheckName
            };

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(healthCheckName),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                properties: healthCheckProperties);
        }

        /// <summary>
        /// Generates a new correlation ID for general use.
        /// </summary>
        /// <returns>A new CorrelationInfo instance with generated IDs</returns>
        public static CorrelationInfo Generate()
        {
            return Create();
        }

        /// <summary>
        /// Determines if this correlation is a root correlation (no parent).
        /// </summary>
        /// <returns>True if this is a root correlation</returns>
        [BurstCompile]
        public bool IsRoot()
        {
            return ParentCorrelationId.IsEmpty || Depth == 0;
        }

        /// <summary>
        /// Determines if this correlation is a child correlation (has parent).
        /// </summary>
        /// <returns>True if this is a child correlation</returns>
        [BurstCompile]
        public bool IsChild()
        {
            return !ParentCorrelationId.IsEmpty && Depth > 0;
        }

        /// <summary>
        /// Gets the age of this correlation.
        /// </summary>
        /// <returns>The age as a TimeSpan</returns>
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Converts native strings to managed strings for interop scenarios.
        /// </summary>
        /// <returns>A tuple containing the managed string representations</returns>
        public (string correlationId, string parentCorrelationId, string rootCorrelationId, string spanId, string traceId, string operation, string userId, string sessionId, string requestId, string serviceName) ToManagedStrings()
        {
            return (
                CorrelationId.ToString(),
                ParentCorrelationId.ToString(),
                RootCorrelationId.ToString(),
                SpanId.ToString(),
                TraceId.ToString(),
                Operation.ToString(),
                UserId.ToString(),
                SessionId.ToString(),
                RequestId.ToString(),
                ServiceName.ToString()
            );
        }

        /// <summary>
        /// Converts this correlation info to a dictionary for structured logging.
        /// </summary>
        /// <returns>A dictionary representation of the correlation info</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                ["CorrelationId"] = CorrelationId.ToString(),
                ["RootCorrelationId"] = RootCorrelationId.ToString(),
                ["SpanId"] = SpanId.ToString(),
                ["TraceId"] = TraceId.ToString(),
                ["ServiceName"] = ServiceName.ToString(),
                ["CreatedAt"] = CreatedAt,
                ["Depth"] = Depth,
                ["IsRoot"] = IsRoot(),
                ["Age"] = Age.TotalMilliseconds
            };

            if (!ParentCorrelationId.IsEmpty)
                dictionary["ParentCorrelationId"] = ParentCorrelationId.ToString();

            if (!Operation.IsEmpty)
                dictionary["Operation"] = Operation.ToString();

            if (!UserId.IsEmpty)
                dictionary["UserId"] = UserId.ToString();

            if (!SessionId.IsEmpty)
                dictionary["SessionId"] = SessionId.ToString();

            if (!RequestId.IsEmpty)
                dictionary["RequestId"] = RequestId.ToString();

            if (HasProperties)
            {
                foreach (var kvp in Properties)
                {
                    dictionary[$"Properties.{kvp.Key}"] = kvp.Value;
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Gets the size in bytes of the native portion of this correlation info.
        /// </summary>
        /// <returns>The size in bytes</returns>
        [BurstCompile]
        public int GetNativeSize()
        {
            return CorrelationId.Length + ParentCorrelationId.Length + RootCorrelationId.Length +
                   SpanId.Length + TraceId.Length + Operation.Length +
                   UserId.Length + SessionId.Length + RequestId.Length + ServiceName.Length +
                   8 + sizeof(int) + sizeof(bool); // DateTime (8 bytes) + Depth + HasProperties
        }

        /// <summary>
        /// Disposes any managed resources (for IDisposable compliance).
        /// </summary>
        public void Dispose()
        {
            // Native strings are stack-allocated, no disposal needed
            // Managed properties are handled by GC
        }

        /// <summary>
        /// Returns a string representation of this correlation info.
        /// </summary>
        /// <returns>A formatted string representation</returns>
        public override string ToString()
        {
            var depthIndicator = IsRoot() ? "ROOT" : $"CHILD({Depth})";
            return $"[{depthIndicator}] {CorrelationId} - {Operation} ({ServiceName})";
        }

        /// <summary>
        /// Determines equality based on correlation ID.
        /// </summary>
        /// <param name="other">The other CorrelationInfo to compare</param>
        /// <returns>True if correlations are equal</returns>
        public bool Equals(CorrelationInfo other)
        {
            return CorrelationId.Equals(other.CorrelationId);
        }

        /// <summary>
        /// Gets the hash code based on correlation ID.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return CorrelationId.GetHashCode();
        }
    }
}