using System;
using AhBearStudios.Core.HealthChecking.Models;
using AhBearStudios.Core.Pooling.Models;

namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service implementation for pool validation operations.
    /// Handles validation and reset logic for pooled objects following production-ready patterns.
    /// </summary>
    public class PoolValidationService : IPoolValidationService
    {
        /// <summary>
        /// Validates that a pooled object is in a valid state for use.
        /// </summary>
        /// <param name="pooledObject">Object to validate</param>
        /// <returns>True if the object is valid and can be used</returns>
        public bool ValidatePooledObject(object pooledObject)
        {
            if (pooledObject is not IPooledObject pooled)
                return false;

            // Basic validity check
            if (!pooled.IsValid())
                return false;

            // Health status check
            var healthStatus = pooled.GetHealthStatus();
            if (healthStatus == HealthStatus.Critical ||
                healthStatus == HealthStatus.Unhealthy)
            {
                return false;
            }

            // Check if object can still be pooled
            return pooled.CanBePooled();
        }

        /// <summary>
        /// Resets a pooled object for reuse, handling circuit breaker logic.
        /// </summary>
        /// <param name="pooledObject">Object to reset</param>
        public void ResetPooledObject(object pooledObject)
        {
            if (pooledObject is not IPooledObject pooled)
                return;

            // Check if object should be disposed due to circuit breaker
            if (ShouldDisposeObject(pooled))
            {
                DisposeIfPossible(pooled);
                return;
            }

            // Normal reset
            pooled.Reset();
        }

        /// <summary>
        /// Performs health check on a pooled object and determines if it should be disposed.
        /// </summary>
        /// <param name="pooledObject">Object to check</param>
        /// <returns>True if the object should be disposed</returns>
        public bool ShouldDisposeObject(object pooledObject)
        {
            if (pooledObject is not IPooledObject pooled)
                return true; // Dispose objects that don't implement IPooledObject

            // Check circuit breaker conditions
            if (pooled.ShouldCircuitBreak())
                return true;

            // Check for critical issues
            if (pooled.HasCriticalIssue())
                return true;

            // Check if object can be pooled
            if (!pooled.CanBePooled())
                return true;

            return false;
        }

        /// <summary>
        /// Disposes an object if it implements IDisposable.
        /// </summary>
        /// <param name="obj">Object to dispose</param>
        private static void DisposeIfPossible(object obj)
        {
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}