using System;
using System.Collections.Generic;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Pools.Unity
{
    /// <summary>
    /// Interface for Unity GameObject pools that manage GameObject instances.
    /// Provides specialized handling for Unity GameObject lifecycle with automatic instantiation and destruction.
    /// </summary>
    public interface IGameObjectPool<GameObject> : IPool<GameObject>, IShrinkablePool
    {
        /// <summary>
        /// Creates a new GameObject instance
        /// </summary>
        /// <returns>A new GameObject instance</returns>
        GameObject CreateNewInstance();

        /// <summary>
        /// Prewarms the pool by creating the specified number of GameObject instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        void PrewarmPool(int count);

        /// <summary>
        /// Destroys a GameObject instance, removing it from the pool permanently
        /// </summary>
        /// <param name="item">GameObject to destroy</param>
        void DestroyItem(GameObject item);

        /// <summary>
        /// Tries to automatically shrink the pool based on elapsed time and thresholds
        /// </summary>
        void TryAutoShrink();

        /// <summary>
        /// Gets the parent transform where pooled GameObjects will be parented when inactive
        /// </summary>
        Transform ParentTransform { get; }

        /// <summary>
        /// Gets whether this pool is using a parent transform
        /// </summary>
        bool UsesParentTransform { get; }

        /// <summary>
        /// Gets or sets whether GameObjects should be disabled when released
        /// </summary>
        bool DisableOnRelease { get; }

        /// <summary>
        /// Gets or sets whether GameObjects should be reset when released
        /// </summary>
        bool ResetOnRelease { get; }

        /// <summary>
        /// Acquires a GameObject and positions it at the specified location
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The acquired and positioned object</returns>
        GameObject Acquire(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Acquires a GameObject and positions it at the specified transform
        /// </summary>
        /// <param name="transform">Transform to match position and rotation</param>
        /// <returns>The acquired and positioned GameObject</returns>
        GameObject AcquireAtTransform(Transform transform);

        /// <summary>
        /// Sets the parent transform for pooled GameObjects
        /// </summary>
        /// <param name="parent">The parent transform to use</param>
        void SetParentTransform(Transform parent);

        /// <summary>
        /// Sets the prefab used to create new instances when the pool needs to grow
        /// </summary>
        /// <param name="prefab">The prefab to use</param>
        void SetPrefab(GameObject prefab);

        /// <summary>
        /// Gets the prefab this pool is using to create instances
        /// </summary>
        /// <returns>The prefab</returns>
        GameObject GetPrefab();

        /// <summary>
        /// Checks if a specific object instance belongs to this pool
        /// </summary>
        /// <param name="objectInstance">The object to check</param>
        /// <returns>True if the object belongs to this pool, false otherwise</returns>
        bool ContainsInstance(UnityEngine.Object objectInstance);

        /// <summary>
        /// Gets Unity-specific metrics for this pool
        /// </summary>
        /// <returns>Dictionary of Unity-specific metrics</returns>
        Dictionary<string, object> GetUnityMetrics();

        /// <summary>
        /// Attempts to reduce fragmentation in the pool
        /// </summary>
        /// <returns>True if defragmentation was performed, false otherwise</returns>
        bool TryDefragment();
        
        /// <summary>
        /// Releases multiple objects back to the pool with custom release action
        /// </summary>
        /// <param name="items">Collection of objects to release</param>
        /// <param name="onRelease">Optional action to perform on each object when releasing</param>
        void ReleaseMultiple(IEnumerable<GameObject> items, Action<GameObject> onRelease);
    }
}