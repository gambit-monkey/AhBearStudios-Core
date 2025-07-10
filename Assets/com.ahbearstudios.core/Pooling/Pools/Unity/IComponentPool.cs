using UnityEngine;

namespace AhBearStudios.Core.Pooling.Pools.Unity
{
    /// <summary>
    /// Interface for Unity Component pools that manage GameObject components.
    /// Provides specialized handling for Unity Component lifecycle with automatic instantiation and destruction.
    /// </summary>
    /// <typeparam name="T">Type of Component to pool</typeparam>
    public interface IComponentPool<T> : IPool<T>, IShrinkablePool where T : Component
    {
        /// <summary>
        /// Creates a new component instance
        /// </summary>
        /// <returns>A new component instance</returns>
        T CreateNew();

        /// <summary>
        /// Prewarms the pool by creating the specified number of component instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        void PrewarmPool(int count);

        /// <summary>
        /// Destroys a component instance, removing it from the pool permanently
        /// </summary>
        /// <param name="item">Component to destroy</param>
        void DestroyItem(T item);

        /// <summary>
        /// Tries to automatically shrink the pool based on elapsed time and thresholds
        /// </summary>
        void TryAutoShrink();
        
        /// <summary>
        /// Gets the parent transform where pooled components will be parented when inactive
        /// </summary>
        Transform ParentTransform { get; }
        
        /// <summary>
        /// Gets or sets whether components should be activated when acquired
        /// </summary>
        bool SetActiveOnAcquire { get; set; }
        
        /// <summary>
        /// Gets or sets whether components should be deactivated when released
        /// </summary>
        bool SetInactiveOnRelease { get; set; }
    }
}