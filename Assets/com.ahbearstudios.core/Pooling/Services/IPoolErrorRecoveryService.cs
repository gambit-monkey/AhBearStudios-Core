using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for pool error handling and recovery operations.
    /// Provides comprehensive error handling with automatic recovery mechanisms for pool operations.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface IPoolErrorRecoveryService : IDisposable
    {
        #region Pool Registration
        
        /// <summary>
        /// Registers a pool for error recovery monitoring.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="pool">Pool instance to monitor</param>
        void RegisterPool(string poolTypeName, IObjectPool pool);
        
        /// <summary>
        /// Unregisters a pool from error recovery monitoring.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        void UnregisterPool(string poolTypeName);
        
        #endregion
        
        #region Error Recovery Operations
        
        /// <summary>
        /// Executes an operation with comprehensive error handling and automatic recovery.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>Result of the operation</returns>
        UniTask<T> ExecuteWithErrorHandling<T>(
            string poolTypeName,
            string operationType,
            Func<UniTask<T>> operation,
            int maxRetries = 3);
        
        /// <summary>
        /// Executes an operation with comprehensive error handling and automatic recovery (void return).
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>Task representing the operation</returns>
        UniTask ExecuteWithErrorHandling(
            string poolTypeName,
            string operationType,
            Func<UniTask> operation,
            int maxRetries = 3);
        
        #endregion
        
        #region Manual Recovery
        
        /// <summary>
        /// Forces recovery for a specific pool type.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type to recover</param>
        /// <returns>Task representing the recovery operation</returns>
        UniTask ForcePoolRecovery(string poolTypeName);
        
        /// <summary>
        /// Performs emergency recovery by recreating a pool entirely.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type for emergency recovery</param>
        /// <returns>Task representing the emergency recovery operation</returns>
        UniTask PerformEmergencyRecovery(string poolTypeName);
        
        #endregion
        
        #region Statistics and Health
        
        /// <summary>
        /// Gets comprehensive error handling and recovery statistics.
        /// </summary>
        /// <returns>Dictionary of error recovery statistics by pool type</returns>
        Dictionary<string, object> GetErrorRecoveryStatistics();
        
        /// <summary>
        /// Gets error recovery metrics for a specific pool.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <returns>Error recovery metrics or null if not registered</returns>
        object GetPoolErrorRecoveryMetrics(string poolTypeName);
        
        /// <summary>
        /// Checks the overall health of the error recovery system.
        /// </summary>
        /// <returns>True if the recovery system is functioning well</returns>
        bool IsRecoverySystemHealthy();
        
        #endregion
    }
}