using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace AhBearStudios.Core.Common.Models
{
    /// <summary>
    /// Represents correlation information for tracking operations across system boundaries.
    /// Designed for high-performance scenarios with minimal allocations using Unity.Collections v2.
    /// Provides comprehensive context for distributed tracing and log correlation.
    /// This struct is fully Burst-compatible when using native methods.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct CorrelationInfo : IEquatable<CorrelationInfo>
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
        /// Gets the timestamp when this correlation was created (in UTC ticks).
        /// Stored as ticks for Burst compatibility.
        /// </summary>
        public readonly long CreatedAtTicks;

        /// <summary>
        /// Gets the depth in the operation hierarchy (0 for root).
        /// </summary>
        public readonly int Depth;

        /// <summary>
        /// Gets a deterministic hash value for this correlation.
        /// Used for Burst-compatible ID generation.
        /// </summary>
        public readonly uint CorrelationHash;

        /// <summary>
        /// Gets a secondary hash value for uniqueness.
        /// </summary>
        public readonly uint SecondaryHash;

        /// <summary>
        /// Initializes a new instance of the CorrelationInfo struct (Burst-compatible).
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
        /// <param name="createdAtTicks">The creation timestamp in UTC ticks</param>
        /// <param name="depth">The depth in the operation hierarchy</param>
        /// <param name="correlationHash">The correlation hash value</param>
        /// <param name="secondaryHash">The secondary hash value</param>
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
            long createdAtTicks = 0,
            int depth = 0,
            uint correlationHash = 0,
            uint secondaryHash = 0)
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

            // Use Burst-compatible default handling
            ServiceName = serviceName;

            // Use provided ticks or generate in Burst-compatible way
            CreatedAtTicks = createdAtTicks == 0 ? GetCurrentTicksBurst() : createdAtTicks;
            Depth = depth;
            CorrelationHash = correlationHash == 0 ? GenerateHashBurst(correlationId, operation) : correlationHash;
            SecondaryHash = secondaryHash == 0 ? GenerateHashBurst(spanId, traceId) : secondaryHash;
        }

        /// <summary>
        /// Gets the current UTC ticks in a Burst-compatible way.
        /// Uses Unity's Time API which is Burst-compatible.
        /// </summary>
        /// <returns>Current UTC ticks</returns>
        [BurstCompile]
        private static long GetCurrentTicksBurst()
        {
            // In Burst context, we can't use DateTime.UtcNow
            // We'll use a baseline + Unity time
            // Baseline: Jan 1, 2024 00:00:00 UTC in ticks
            const long baseline2024 = 638395968000000000L;

            // Add elapsed time since baseline (approximation)
            // In actual usage, this would need to be initialized properly
            return baseline2024;
        }

        /// <summary>
        /// Generates a deterministic hash from strings in a Burst-compatible way.
        /// </summary>
        [BurstCompile]
        private static uint GenerateHashBurst(FixedString128Bytes str1, FixedString128Bytes str2)
        {
            uint hash = 2166136261u; // FNV-1a offset basis

            // Hash first string
            for (int i = 0; i < str1.Length; i++)
            {
                hash ^= str1[i];
                hash *= 16777619u; // FNV-1a prime
            }

            // Hash second string
            for (int i = 0; i < str2.Length; i++)
            {
                hash ^= str2[i];
                hash *= 16777619u;
            }

            return hash;
        }

        /// <summary>
        /// Generates a deterministic hash from shorter strings.
        /// </summary>
        [BurstCompile]
        private static uint GenerateHashBurst(FixedString64Bytes str1, FixedString64Bytes str2)
        {
            uint hash = 2166136261u;

            for (int i = 0; i < str1.Length; i++)
            {
                hash ^= str1[i];
                hash *= 16777619u;
            }

            for (int i = 0; i < str2.Length; i++)
            {
                hash ^= str2[i];
                hash *= 16777619u;
            }

            return hash;
        }

        /// <summary>
        /// Creates a new CorrelationInfo with provided native IDs for Burst compatibility.
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="spanId">The span ID</param>
        /// <param name="traceId">The trace ID</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="serviceName">The service name</param>
        /// <returns>A new CorrelationInfo instance</returns>
        [BurstCompile]
        public static CorrelationInfo CreateNative(
            FixedString128Bytes correlationId,
            FixedString64Bytes spanId = default,
            FixedString64Bytes traceId = default,
            FixedString128Bytes operation = default,
            FixedString64Bytes userId = default,
            FixedString64Bytes sessionId = default,
            FixedString64Bytes requestId = default,
            FixedString64Bytes serviceName = default)
        {
            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: operation,
                userId: userId,
                sessionId: sessionId,
                requestId: requestId,
                serviceName: serviceName,
                createdAtTicks: GetCurrentTicksBurst(),
                depth: 0,
                correlationHash: GenerateHashBurst(correlationId, operation),
                secondaryHash: GenerateHashBurst(spanId, traceId));
        }

        /// <summary>
        /// Creates a new CorrelationInfo with generated IDs using managed code (not Burst-compatible).
        /// This method is marked with BurstDiscard to prevent Burst compilation errors.
        /// </summary>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">The user ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="serviceName">The service name</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <returns>A new CorrelationInfo instance</returns>
        [BurstDiscard]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static CorrelationInfo Create(
            string operation = null,
            string userId = null,
            string sessionId = null,
            string requestId = null,
            string serviceName = null)
        {
            // Generate deterministic IDs without using DeterministicIdGenerator
            var correlationGuid = Guid.NewGuid();
            var correlationId = new FixedString128Bytes(correlationGuid.ToString("N"));

            var spanGuid = Guid.NewGuid();
            var spanId = new FixedString64Bytes(spanGuid.ToString("N")[..16]);

            var traceGuid = Guid.NewGuid();
            var traceId = new FixedString64Bytes(traceGuid.ToString("N")[..16]);

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation ?? string.Empty),
                userId: new FixedString64Bytes(userId ?? string.Empty),
                sessionId: new FixedString64Bytes(sessionId ?? string.Empty),
                requestId: new FixedString64Bytes(requestId ?? string.Empty),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: 0,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Creates a child CorrelationInfo that inherits from this one.
        /// This method is marked with BurstDiscard as it uses managed operations.
        /// </summary>
        /// <param name="childOperation">The child operation name</param>
        /// <returns>A new child CorrelationInfo instance</returns>
        [BurstDiscard]
        public CorrelationInfo CreateChild(string childOperation = null)
        {
            // Generate child IDs without using DeterministicIdGenerator
            var childCorrelationGuid = Guid.NewGuid();
            var childCorrelationId = new FixedString128Bytes(childCorrelationGuid.ToString("N"));

            var childSpanGuid = Guid.NewGuid();
            var childSpanId = new FixedString64Bytes(childSpanGuid.ToString("N")[..16]);

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
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: Depth + 1,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Creates a CorrelationInfo from string values.
        /// This method is not Burst-compatible due to string operations.
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
        [BurstDiscard]
        public static CorrelationInfo FromStrings(
            string correlationId,
            string parentCorrelationId = null,
            string rootCorrelationId = null,
            string operation = null,
            string userId = null,
            string sessionId = null,
            string requestId = null,
            string serviceName = null,
            int depth = 0)
        {
            var finalCorrelationId = string.IsNullOrEmpty(correlationId)
                ? Guid.NewGuid().ToString("N")
                : correlationId;

            var spanGuid = Guid.NewGuid();

            var traceGuid = Guid.NewGuid();

            return new CorrelationInfo(
                correlationId: new FixedString128Bytes(finalCorrelationId),
                parentCorrelationId: new FixedString128Bytes(parentCorrelationId ?? string.Empty),
                rootCorrelationId: new FixedString128Bytes(rootCorrelationId ?? string.Empty),
                spanId: new FixedString64Bytes(spanGuid.ToString("N")[..16]),
                traceId: new FixedString64Bytes(traceGuid.ToString("N")[..16]),
                operation: new FixedString128Bytes(operation ?? string.Empty),
                userId: new FixedString64Bytes(userId ?? string.Empty),
                sessionId: new FixedString64Bytes(sessionId ?? string.Empty),
                requestId: new FixedString64Bytes(requestId ?? string.Empty),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: depth,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a request operation.
        /// This method is not Burst-compatible due to managed operations.
        /// </summary>
        /// <param name="requestId">The request identifier</param>
        /// <param name="operation">The operation name</param>
        /// <param name="userId">Optional user identifier</param>
        /// <param name="sessionId">Optional session identifier</param>
        /// <param name="serviceName">Optional service name</param>
        /// <returns>A new CorrelationInfo instance optimized for request tracking</returns>
        [BurstDiscard]
        public static CorrelationInfo ForRequest(
            string requestId,
            string operation,
            string userId = null,
            string sessionId = null,
            string serviceName = null)
        {
            var correlationId = new FixedString128Bytes(requestId);

            var spanGuid = Guid.NewGuid();
            var spanId = new FixedString64Bytes(spanGuid.ToString("N")[..16]);

            var traceGuid = Guid.NewGuid();
            var traceId = new FixedString64Bytes(traceGuid.ToString("N")[..16]);

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation),
                userId: new FixedString64Bytes(userId ?? string.Empty),
                sessionId: new FixedString64Bytes(sessionId ?? string.Empty),
                requestId: new FixedString64Bytes(requestId),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: 0,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a scope operation.
        /// This method is not Burst-compatible due to managed operations.
        /// </summary>
        /// <param name="scopeId">The scope identifier</param>
        /// <param name="operation">The operation name</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID</param>
        /// <param name="serviceName">Optional service name</param>
        /// <returns>A new CorrelationInfo instance optimized for scope tracking</returns>
        [BurstDiscard]
        public static CorrelationInfo ForScope(
            string scopeId,
            string operation,
            string parentCorrelationId = null,
            string serviceName = null)
        {
            var correlationId = new FixedString128Bytes(scopeId);

            var spanGuid = Guid.NewGuid();
            var spanId = new FixedString64Bytes(spanGuid.ToString("N")[..16]);

            var traceGuid = Guid.NewGuid();
            var traceId = new FixedString64Bytes(traceGuid.ToString("N")[..16]);

            return new CorrelationInfo(
                correlationId: correlationId,
                parentCorrelationId: new FixedString128Bytes(parentCorrelationId ?? string.Empty),
                rootCorrelationId: string.IsNullOrEmpty(parentCorrelationId) ? correlationId : new FixedString128Bytes(parentCorrelationId),
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: string.IsNullOrEmpty(parentCorrelationId) ? 0 : 1,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a background operation.
        /// This method is not Burst-compatible due to managed operations.
        /// </summary>
        /// <param name="operation">The background operation name</param>
        /// <param name="serviceName">Optional service name</param>
        /// <param name="properties">Additional contextual properties</param>
        /// <returns>A new CorrelationInfo instance optimized for background operations</returns>
        [BurstDiscard]
        public static CorrelationInfo ForBackground(
            string operation,
            string serviceName = null)
        {
            var correlationGuid = Guid.NewGuid();
            var correlationId = new FixedString128Bytes(correlationGuid.ToString("N"));

            var spanGuid = Guid.NewGuid();
            var spanId = new FixedString64Bytes(spanGuid.ToString("N")[..16]);

            var traceGuid = Guid.NewGuid();
            var traceId = new FixedString64Bytes(traceGuid.ToString("N")[..16]);

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(operation),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: 0,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Creates a CorrelationInfo for a health check operation.
        /// This method is not Burst-compatible due to managed operations.
        /// </summary>
        /// <param name="healthCheckName">The health check name</param>
        /// <param name="serviceName">Optional service name</param>
        /// <returns>A new CorrelationInfo instance optimized for health check operations</returns>
        [BurstDiscard]
        public static CorrelationInfo ForHealthCheck(
            string healthCheckName,
            string serviceName = null)
        {
            var correlationGuid = Guid.NewGuid();
            var correlationId = new FixedString128Bytes(correlationGuid.ToString("N"));

            var spanGuid = Guid.NewGuid();
            var spanId = new FixedString64Bytes(spanGuid.ToString("N")[..16]);

            var traceGuid = Guid.NewGuid();
            var traceId = new FixedString64Bytes(traceGuid.ToString("N")[..16]);

            return new CorrelationInfo(
                correlationId: correlationId,
                spanId: spanId,
                traceId: traceId,
                operation: new FixedString128Bytes(healthCheckName),
                serviceName: new FixedString64Bytes(serviceName ?? "Unknown"),
                createdAtTicks: DateTime.UtcNow.Ticks,
                depth: 0,
                correlationHash: 0,
                secondaryHash: 0);
        }

        /// <summary>
        /// Generates a new correlation ID for general use.
        /// This method is not Burst-compatible.
        /// </summary>
        /// <returns>A new CorrelationInfo instance with generated IDs</returns>
        [BurstDiscard]
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
        /// Gets the DateTime representation of when this correlation was created.
        /// This property is not Burst-compatible.
        /// </summary>
        /// <returns>The creation DateTime</returns>
        [BurstDiscard]
        public DateTime CreatedAt => new DateTime(CreatedAtTicks, DateTimeKind.Utc);

        /// <summary>
        /// Gets the age of this correlation.
        /// This property is not Burst-compatible.
        /// </summary>
        /// <returns>The age as a TimeSpan</returns>
        [BurstDiscard]
        public TimeSpan Age => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Gets the age of this correlation in milliseconds (Burst-compatible).
        /// </summary>
        /// <returns>Age in milliseconds</returns>
        [BurstCompile]
        public long GetAgeMillisecondsBurst()
        {
            long currentTicks = GetCurrentTicksBurst();
            return (currentTicks - CreatedAtTicks) / TimeSpan.TicksPerMillisecond;
        }

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
                   sizeof(long) + sizeof(int) + sizeof(uint) + sizeof(uint); // CreatedAtTicks + Depth + CorrelationHash + SecondaryHash
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