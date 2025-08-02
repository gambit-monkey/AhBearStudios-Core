using System;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Contains diagnostic information for a pooled object.
    /// </summary>
    public struct PooledObjectDiagnostics
    {
        /// <summary>
        /// Unique identifier of the object.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Name of the pool this object belongs to.
        /// </summary>
        public FixedString64Bytes PoolName { get; set; }
        
        /// <summary>
        /// Current health status.
        /// </summary>
        public HealthStatus HealthStatus { get; set; }
        
        /// <summary>
        /// Number of times used.
        /// </summary>
        public long UseCount { get; set; }
        
        /// <summary>
        /// Total active time in milliseconds.
        /// </summary>
        public long TotalActiveTimeMs { get; set; }
        
        /// <summary>
        /// Number of validation errors.
        /// </summary>
        public int ValidationErrors { get; set; }
        
        /// <summary>
        /// Number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; set; }
        
        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }
        
        /// <summary>
        /// Timestamp when created.
        /// </summary>
        public long CreatedAtTicks { get; set; }
        
        /// <summary>
        /// Timestamp when last used.
        /// </summary>
        public long LastUsedTicks { get; set; }
        
        /// <summary>
        /// Additional diagnostic message.
        /// </summary>
        public FixedString512Bytes DiagnosticMessage { get; set; }
        
        /// <summary>
        /// Whether the object is currently poolable.
        /// </summary>
        public bool IsPoolable { get; set; }
        
        /// <summary>
        /// Whether corruption was detected.
        /// </summary>
        public bool CorruptionDetected { get; set; }
    }
}