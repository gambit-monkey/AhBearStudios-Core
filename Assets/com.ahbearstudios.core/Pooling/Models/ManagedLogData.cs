using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Represents managed data that cannot be stored in native collections.
    /// Used for storing exception, properties, and scope data associated with log entries.
    /// </summary>
    public sealed class ManagedLogData : IPooledObject
    {
        #region Private Fields
        
        private DateTime _activeStartTime;
        private bool _isActive;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// The exception associated with the log entry.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The structured properties associated with the log entry.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// The scope context associated with the log entry.
        /// </summary>
        public object Scope { get; set; }

        /// <summary>
        /// The unique storage ID for this data.
        /// </summary>
        public Guid StorageId { get; set; }

        #endregion
        
        #region IPooledObject Implementation

        /// <summary>
        /// Gets or sets the pool name for this object.
        /// </summary>
        public string PoolName { get; set; } = "ManagedLogData";
        
        /// <summary>
        /// Gets or sets the unique identifier for this pooled object instance.
        /// </summary>
        public Guid PoolId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this object was last used.
        /// </summary>
        public DateTime LastUsed { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when this object was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the number of times this object has been used.
        /// </summary>
        public long UseCount { get; set; }
        
        /// <summary>
        /// Gets or sets the total time this object has been active (in use).
        /// </summary>
        public TimeSpan TotalActiveTime { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the last validation check.
        /// </summary>
        public DateTime LastValidationTime { get; set; }
        
        /// <summary>
        /// Gets or sets the priority for pool eviction decisions.
        /// </summary>
        public int Priority { get; set; } = 2; // Medium priority
        
        /// <summary>
        /// Gets or sets the number of validation errors encountered.
        /// </summary>
        public int ValidationErrorCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether corruption has been detected in this object.
        /// </summary>
        public bool CorruptionDetected { get; set; }
        
        /// <summary>
        /// Gets or sets the number of consecutive failures for this object.
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of ManagedLogData.
        /// </summary>
        public ManagedLogData()
        {
            var now = DateTime.UtcNow;
            PoolId = DeterministicIdGenerator.GeneratePooledObjectId("ManagedLogData", "LogData", GetHashCode());
            CreatedAt = now;
            LastUsed = now;
            LastValidationTime = now;
        }
        
        #endregion
        
        #region IPooledObject Methods

        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// </summary>
        public void OnGet()
        {
            var now = DateTime.UtcNow;
            LastUsed = now;
            _activeStartTime = now;
            _isActive = true;
            UseCount++;
            ConsecutiveFailures = 0;
        }

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        public void OnReturn()
        {
            var now = DateTime.UtcNow;
            LastUsed = now;
            
            // Calculate and update active time
            if (_isActive)
            {
                var sessionTime = now - _activeStartTime;
                TotalActiveTime = TotalActiveTime.Add(sessionTime);
                _isActive = false;
            }
            
            // Validate the object state on return
            if (!IsValid())
            {
                ValidationErrorCount++;
                ConsecutiveFailures++;
            }
        }

        /// <summary>
        /// Resets the object to its initial state for reuse.
        /// </summary>
        public void Reset()
        {
            Exception = null;
            Properties = null;
            Scope = null;
            StorageId = Guid.Empty;
            ConsecutiveFailures = 0;
            _isActive = false;
        }

        /// <summary>
        /// Validates that the object is in a valid state.
        /// </summary>
        /// <returns>True if the object is valid</returns>
        public bool IsValid()
        {
            LastValidationTime = DateTime.UtcNow;
            
            // ManagedLogData is generally always valid unless corrupted
            return !CorruptionDetected;
        }
        
        /// <summary>
        /// Gets the estimated memory usage of this object in bytes.
        /// </summary>
        /// <returns>Estimated memory footprint in bytes</returns>
        public long GetEstimatedMemoryUsage()
        {
            // Base object overhead
            long memoryUsage = 64;
            
            // Exception (if present)
            if (Exception != null)
            {
                memoryUsage += 512; // Rough estimate for exception with stack trace
            }
            
            // Properties dictionary
            if (Properties != null && Properties.Count > 0)
            {
                memoryUsage += 32; // Dictionary overhead
                memoryUsage += Properties.Count * 64; // Estimate per key-value pair
            }
            
            // Scope object (hard to estimate, use conservative value)
            if (Scope != null)
            {
                memoryUsage += 256;
            }
            
            // Fields and properties
            memoryUsage += 16 * 2; // Guids
            memoryUsage += 8 * 5; // DateTimes
            memoryUsage += 8; // TimeSpan
            memoryUsage += 8; // long UseCount
            memoryUsage += 4 * 3; // ints
            memoryUsage += 2 * 3; // bools
            
            return memoryUsage;
        }
        
        /// <summary>
        /// Gets the health status of this pooled object.
        /// </summary>
        /// <returns>Health status information</returns>
        public HealthStatus GetHealthStatus()
        {
            if (CorruptionDetected)
                return HealthStatus.Critical;
                
            if (ConsecutiveFailures > 5)
                return HealthStatus.Critical;
                
            if (ConsecutiveFailures > 0)
                return HealthStatus.Degraded;
                
            if (ValidationErrorCount > 10)
                return HealthStatus.Warning;
                
            return HealthStatus.Healthy;
        }
        
        /// <summary>
        /// Determines if this object can currently be pooled.
        /// </summary>
        /// <returns>True if the object can be returned to the pool</returns>
        public bool CanBePooled()
        {
            return !CorruptionDetected && ConsecutiveFailures < 10;
        }
        
        /// <summary>
        /// Determines if this object should trigger a circuit breaker.
        /// </summary>
        /// <returns>True if circuit breaker should be triggered</returns>
        public bool ShouldCircuitBreak()
        {
            return ConsecutiveFailures >= 5 || CorruptionDetected;
        }
        
        /// <summary>
        /// Checks if this object has a critical issue that requires alerting.
        /// </summary>
        /// <returns>True if a critical issue exists</returns>
        public bool HasCriticalIssue()
        {
            return CorruptionDetected || ConsecutiveFailures >= 5;
        }
        
        /// <summary>
        /// Gets an alert message describing any critical issues with this object.
        /// </summary>
        /// <returns>Alert message or null if no issues</returns>
        public FixedString512Bytes? GetAlertMessage()
        {
            if (!HasCriticalIssue())
                return null;
                
            var message = new FixedString512Bytes();
            message.Append($"ManagedLogData {PoolId}: ");
            
            if (CorruptionDetected)
            {
                message.Append("Corruption detected. ");
            }
            
            if (ConsecutiveFailures >= 5)
            {
                message.Append($"High consecutive failures ({ConsecutiveFailures}). ");
            }
            
            message.Append($"Health: {GetHealthStatus()}");
            
            return message;
        }
        
        /// <summary>
        /// Gets comprehensive diagnostic information about this object.
        /// </summary>
        /// <returns>Diagnostic data for troubleshooting and monitoring</returns>
        public PooledObjectDiagnostics GetDiagnosticInfo()
        {
            var diagnosticMessage = new FixedString512Bytes();
            diagnosticMessage.Append($"ManagedLogData Uses:{UseCount} ");
            diagnosticMessage.Append($"Active:{TotalActiveTime.TotalSeconds:F1}s ");
            
            if (Exception != null)
            {
                diagnosticMessage.Append("Has Exception ");
            }
            
            if (Properties != null && Properties.Count > 0)
            {
                diagnosticMessage.Append($"Props:{Properties.Count} ");
            }
            
            diagnosticMessage.Append($"Errors:{ValidationErrorCount}");
            
            return new PooledObjectDiagnostics
            {
                Id = PoolId,
                PoolName = new FixedString64Bytes(PoolName),
                HealthStatus = GetHealthStatus(),
                UseCount = UseCount,
                TotalActiveTimeMs = (long)TotalActiveTime.TotalMilliseconds,
                ValidationErrors = ValidationErrorCount,
                ConsecutiveFailures = ConsecutiveFailures,
                MemoryUsageBytes = GetEstimatedMemoryUsage(),
                CreatedAtTicks = CreatedAt.Ticks,
                LastUsedTicks = LastUsed.Ticks,
                DiagnosticMessage = diagnosticMessage,
                IsPoolable = CanBePooled(),
                CorruptionDetected = CorruptionDetected
            };
        }
        
        #endregion
    }
}