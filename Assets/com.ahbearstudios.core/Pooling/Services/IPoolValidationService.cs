namespace AhBearStudios.Core.Pooling.Services
{
    /// <summary>
    /// Service interface for pool validation operations.
    /// Handles validation and reset logic for pooled objects.
    /// </summary>
    public interface IPoolValidationService
    {
        /// <summary>
        /// Validates that a pooled object is in a valid state for use.
        /// </summary>
        /// <param name="pooledObject">Object to validate</param>
        /// <returns>True if the object is valid and can be used</returns>
        bool ValidatePooledObject(object pooledObject);

        /// <summary>
        /// Resets a pooled object for reuse, handling circuit breaker logic.
        /// </summary>
        /// <param name="pooledObject">Object to reset</param>
        void ResetPooledObject(object pooledObject);

        /// <summary>
        /// Performs health check on a pooled object and determines if it should be disposed.
        /// </summary>
        /// <param name="pooledObject">Object to check</param>
        /// <returns>True if the object should be disposed</returns>
        bool ShouldDisposeObject(object pooledObject);
    }
}