using System;
using Unity.Collections;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.Pooling
{
    /// <summary>
    /// Interface for objects that require special pooling behavior with production-ready features.
    /// Provides lifecycle management, health monitoring, and diagnostic capabilities.
    /// </summary>
    public interface IPooledObject
    {
        #region Core Lifecycle
        
        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// Use this to initialize or prepare the object for use.
        /// </summary>
        void OnGet();
        
        /// <summary>
        /// Called when the object is returned to the pool.
        /// Use this to clean up temporary state before pooling.
        /// </summary>
        void OnReturn();
        
        /// <summary>
        /// Resets the object to its initial state.
        /// Called during return to pool or maintenance operations.
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Validates that the object is in a valid state for use.
        /// </summary>
        /// <returns>True if the object is valid and can be used</returns>
        bool IsValid();
        
        #endregion
        
        #region Pool Information
        
        /// <summary>
        /// Gets or sets the name of the pool this object belongs to.
        /// </summary>
        string PoolName { get; set; }
        
        /// <summary>
        /// Gets or sets the unique identifier for this pooled object instance.
        /// </summary>
        Guid PoolId { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when this object was last used.
        /// </summary>
        DateTime LastUsed { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when this object was created.
        /// </summary>
        DateTime CreatedAt { get; set; }
        
        #endregion
        
        #region Performance Monitoring
        
        /// <summary>
        /// Gets or sets the number of times this object has been used.
        /// </summary>
        long UseCount { get; set; }
        
        /// <summary>
        /// Gets or sets the total time this object has been active (in use).
        /// </summary>
        TimeSpan TotalActiveTime { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the last validation check.
        /// </summary>
        DateTime LastValidationTime { get; set; }
        
        /// <summary>
        /// Gets or sets the priority for pool eviction decisions.
        /// Higher values indicate higher priority to keep in pool.
        /// </summary>
        int Priority { get; set; }
        
        /// <summary>
        /// Gets the estimated memory usage of this object in bytes.
        /// </summary>
        /// <returns>Estimated memory footprint in bytes</returns>
        long GetEstimatedMemoryUsage();
        
        #endregion
        
        #region Health and Validation
        
        /// <summary>
        /// Gets or sets the number of validation errors encountered.
        /// </summary>
        int ValidationErrorCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether corruption has been detected in this object.
        /// </summary>
        bool CorruptionDetected { get; set; }
        
        /// <summary>
        /// Gets the health status of this pooled object.
        /// </summary>
        /// <returns>Health status information</returns>
        HealthStatus GetHealthStatus();
        
        /// <summary>
        /// Determines if this object can currently be pooled.
        /// Some objects may become unpoolable due to state or external factors.
        /// </summary>
        /// <returns>True if the object can be returned to the pool</returns>
        bool CanBePooled();
        
        #endregion
        
        #region Circuit Breaker Support
        
        /// <summary>
        /// Gets or sets the number of consecutive failures for this object.
        /// </summary>
        int ConsecutiveFailures { get; set; }
        
        /// <summary>
        /// Determines if this object should trigger a circuit breaker.
        /// </summary>
        /// <returns>True if circuit breaker should be triggered</returns>
        bool ShouldCircuitBreak();
        
        #endregion
        
        #region Alert Integration
        
        /// <summary>
        /// Checks if this object has a critical issue that requires alerting.
        /// </summary>
        /// <returns>True if a critical issue exists</returns>
        bool HasCriticalIssue();
        
        /// <summary>
        /// Gets an alert message describing any critical issues with this object.
        /// </summary>
        /// <returns>Alert message or null if no issues</returns>
        FixedString512Bytes? GetAlertMessage();
        
        #endregion
        
        #region Diagnostics
        
        /// <summary>
        /// Gets comprehensive diagnostic information about this object.
        /// </summary>
        /// <returns>Diagnostic data for troubleshooting and monitoring</returns>
        PooledObjectDiagnostics GetDiagnosticInfo();
        
        #endregion
    }
}