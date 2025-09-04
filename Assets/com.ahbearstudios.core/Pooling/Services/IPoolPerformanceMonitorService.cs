using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Models;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for pool performance monitoring and budget enforcement.
    /// Tracks operation performance against budgets and provides comprehensive metrics.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface IPoolPerformanceMonitorService : IDisposable
    {
        #region Pool Registration
        
        /// <summary>
        /// Registers a pool for performance monitoring.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="pool">Pool instance to monitor</param>
        /// <param name="budget">Performance budget to enforce (optional)</param>
        void RegisterPool(string poolTypeName, IObjectPool pool, PerformanceBudget budget = null);
        
        /// <summary>
        /// Unregisters a pool from performance monitoring.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        void UnregisterPool(string poolTypeName);
        
        #endregion
        
        #region Performance Monitoring
        
        /// <summary>
        /// Executes an operation with performance budget monitoring.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation (get, return, validation, etc.)</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="budget">Performance budget to enforce (optional)</param>
        /// <returns>Result of the operation</returns>
        UniTask<T> ExecuteWithPerformanceBudget<T>(
            string poolTypeName,
            string operationType,
            Func<UniTask<T>> operation,
            PerformanceBudget budget = null);
        
        /// <summary>
        /// Executes an operation with performance budget monitoring (void return).
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="operation">Operation to execute</param>
        /// <param name="budget">Performance budget to enforce</param>
        UniTask ExecuteWithPerformanceBudget(
            string poolTypeName,
            string operationType,
            Func<UniTask> operation,
            PerformanceBudget budget = null);
        
        /// <summary>
        /// Records a performance measurement manually.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="operationType">Type of operation</param>
        /// <param name="operationTime">Time taken for the operation</param>
        void RecordPerformanceMetric(string poolTypeName, string operationType, TimeSpan operationTime);
        
        #endregion
        
        #region Statistics and Health
        
        /// <summary>
        /// Gets comprehensive performance statistics for all pool types.
        /// </summary>
        /// <returns>Dictionary of performance statistics by pool type</returns>
        Dictionary<string, object> GetPerformanceStatistics();
        
        /// <summary>
        /// Gets performance statistics for a specific pool type.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <returns>Performance statistics or null if not registered</returns>
        object GetPoolPerformanceStatistics(string poolTypeName);
        
        /// <summary>
        /// Checks if any pools are consistently violating performance budgets.
        /// </summary>
        /// <returns>True if performance is acceptable across all pools</returns>
        bool IsPerformanceAcceptable();
        
        /// <summary>
        /// Gets pools that are currently violating performance budgets.
        /// </summary>
        /// <returns>Array of pool type names with performance issues</returns>
        string[] GetPoolsWithPerformanceIssues();
        
        #endregion
        
        #region Budget Management
        
        /// <summary>
        /// Updates the performance budget for a pool.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="budget">New performance budget</param>
        void UpdatePerformanceBudget(string poolTypeName, PerformanceBudget budget);
        
        /// <summary>
        /// Gets the performance budget for a pool.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <returns>Performance budget or null if not set</returns>
        PerformanceBudget GetPerformanceBudget(string poolTypeName);
        
        #endregion
    }
}