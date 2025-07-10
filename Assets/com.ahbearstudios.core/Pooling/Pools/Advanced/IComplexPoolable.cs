namespace AhBearStudios.Core.Pooling.Pools.Advanced
{
    /// <summary>
    /// Interface for objects that support complex pooling features
    /// </summary>
    public interface IComplexPoolable : IPoolable
    {
        /// <summary>
        /// Called when the object's properties should be reset to default values
        /// </summary>
        void ResetProperties();
        
        /// <summary>
        /// Called to validate that the object is in a valid state
        /// </summary>
        /// <returns>True if the object is valid, false otherwise</returns>
        bool Validate();
        
        /// <summary>
        /// Gets the number of times this object has been reused
        /// </summary>
        int ReuseCount { get; }
        
        /// <summary>
        /// Increments the reuse count
        /// </summary>
        void IncrementReuseCount();
    }

}