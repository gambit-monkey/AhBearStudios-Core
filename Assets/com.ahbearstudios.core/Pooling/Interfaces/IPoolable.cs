namespace AhBearStudios.Core.Pooling.Interfaces
{
    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    /// <typeparam name="T">The type implementing this interface</typeparam>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is acquired from the pool
        /// </summary>
        void OnAcquire();
        
        /// <summary>
        /// Called when the object is released back to the pool
        /// </summary>
        void OnRelease();
        
        /// <summary>
        /// Resets the object state to its initial values
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Called when the item is being destroyed
        /// </summary>
        void OnDestroy();
    }
    /// <summary>
    /// Generic interface for objects that can be pooled with type knowledge
    /// </summary>
    /// <typeparam name="T">Type of the poolable object</typeparam>
    public interface IPoolable<T> : IPoolable
    {
    }
}