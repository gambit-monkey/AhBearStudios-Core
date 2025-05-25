using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Unity
{
    /// <summary>
    /// Interface for MonoBehaviours that can be pooled
    /// </summary>
    public interface IPoolableMonoBehaviour 
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
    /// Generic interface for MonoBehaviours that can be pooled with type knowledge
    /// </summary>
    /// <typeparam name="T">Type of the poolable MonoBehaviour</typeparam>
    public interface IPoolableMonoBehaviour<T> : IPoolableMonoBehaviour where T : MonoBehaviour
    {
        // No additional members needed - this interface provides type information
    }
}