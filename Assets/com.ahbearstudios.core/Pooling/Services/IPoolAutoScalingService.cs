using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for automatic pool scaling operations.
    /// Monitors pool utilization and automatically adjusts pool sizes based on demand patterns.
    /// Designed for Unity game development with 60+ FPS performance requirements.
    /// </summary>
    public interface IPoolAutoScalingService : IDisposable
    {
        #region Auto-Scaling Control
        
        /// <summary>
        /// Starts automatic pool scaling based on performance metrics.
        /// </summary>
        /// <param name="checkInterval">Interval between scaling checks</param>
        void StartAutoScaling(TimeSpan checkInterval);
        
        /// <summary>
        /// Stops automatic pool scaling.
        /// </summary>
        void StopAutoScaling();
        
        /// <summary>
        /// Gets whether auto-scaling is currently active.
        /// </summary>
        bool IsAutoScalingActive { get; }
        
        #endregion
        
        #region Pool Registration
        
        /// <summary>
        /// Registers a pool for auto-scaling monitoring.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <param name="pool">Pool instance to monitor</param>
        void RegisterPool(string poolTypeName, IObjectPool pool);
        
        /// <summary>
        /// Unregisters a pool from auto-scaling monitoring.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        void UnregisterPool(string poolTypeName);
        
        #endregion
        
        #region Manual Scaling
        
        /// <summary>
        /// Forces a scaling check for all pools.
        /// </summary>
        /// <returns>Task representing the scaling operation</returns>
        UniTask PerformScalingCheck();
        
        /// <summary>
        /// Forces a scaling check for a specific pool.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type to check</param>
        /// <returns>Task representing the scaling operation</returns>
        UniTask PerformScalingCheck(string poolTypeName);
        
        #endregion
        
        #region Statistics and Monitoring
        
        /// <summary>
        /// Gets automatic scaling statistics for monitoring.
        /// </summary>
        /// <returns>Dictionary of scaling statistics by pool type</returns>
        Dictionary<string, object> GetAutoScalingStatistics();
        
        /// <summary>
        /// Gets scaling metrics for a specific pool.
        /// </summary>
        /// <param name="poolTypeName">Name of the pool type</param>
        /// <returns>Scaling metrics or null if not registered</returns>
        object GetPoolScalingMetrics(string poolTypeName);
        
        #endregion
    }
}