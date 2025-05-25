using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;
using UnityEngine;

namespace AhBearStudios.Pooling.Pools.Unity
{
    /// <summary>
    /// Base interface for Unity ParticleSystem pools that manage particle effect instances.
    /// </summary>
    public interface IParticleSystemPool : IPool, IShrinkablePool
    {
        /// <summary>
        /// Gets the parent transform where pooled ParticleSystems will be parented when inactive
        /// </summary>
        Transform ParentTransform { get; }

        /// <summary>
        /// Gets whether this pool is using a parent transform
        /// </summary>
        bool UsesParentTransform { get; }

        /// <summary>
        /// Gets whether objects from this pool should be reset when released
        /// </summary>
        bool ResetOnRelease { get; }

        /// <summary>
        /// Gets whether objects from this pool should be disabled when released
        /// </summary>
        bool DisableOnRelease { get; }

        /// <summary>
        /// Sets the parent transform for pooled ParticleSystems
        /// </summary>
        /// <param name="parentTransform">The parent transform to use</param>
        void SetParentTransform(Transform parentTransform);
        
        /// <summary>
        /// Prewarms the pool by creating the specified number of ParticleSystem instances
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        void PrewarmPool(int count);

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
        /// Checks if a specific object instance belongs to this pool
        /// </summary>
        /// <param name="objectInstance">The object to check</param>
        /// <returns>True if the object belongs to this pool, false otherwise</returns>
        bool ContainsInstance(UnityEngine.Object objectInstance);
    }

    /// <summary>
    /// Generic interface for ParticleSystem pools with specialized handling for Unity ParticleSystem lifecycle
    /// including automatic play, positioning, and auto-release capabilities.
    /// </summary>
    /// <typeparam name="T">Type of particle system in the pool</typeparam>
    public interface IParticleSystemPool<T> : IParticleSystemPool, IPool<T> where T : UnityEngine.Object
    {
        /// <summary>
        /// Creates a new ParticleSystem instance
        /// </summary>
        /// <returns>A new ParticleSystem instance</returns>
        T CreateNewInstance();

        /// <summary>
        /// Destroys a ParticleSystem instance, removing it from the pool permanently
        /// </summary>
        /// <param name="item">ParticleSystem to destroy</param>
        void DestroyItem(T item);

        /// <summary>
        /// Sets the prefab used to create new instances when the pool needs to grow
        /// </summary>
        /// <param name="prefab">The prefab to use</param>
        void SetPrefab(T prefab);

        /// <summary>
        /// Gets the prefab this pool is using to create instances
        /// </summary>
        /// <returns>The prefab</returns>
        T GetPrefab();

        /// <summary>
        /// Acquires a ParticleSystem and positions it at the specified location
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The acquired and positioned object</returns>
        T Acquire(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Acquires a ParticleSystem and positions it at the specified transform
        /// </summary>
        /// <param name="transform">Transform to match position and rotation</param>
        /// <returns>The acquired and positioned ParticleSystem</returns>
        T AcquireAtTransform(Transform transform);

        /// <summary>
        /// Releases multiple objects back to the pool with custom release action
        /// </summary>
        /// <param name="items">Collection of objects to release</param>
        /// <param name="onRelease">Optional action to perform on each object when releasing</param>
        void ReleaseMultiple(IEnumerable<T> items, Action<T> onRelease);

        /// <summary>
        /// Plays a particle system at the specified location
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The playing particle system</returns>
        T PlayAt(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Plays a particle system at the specified transform
        /// </summary>
        /// <param name="transform">Transform to match</param>
        /// <returns>The playing particle system</returns>
        T PlayAtTransform(Transform transform);

        /// <summary>
        /// Plays a particle system and automatically releases it when complete
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <param name="customLifetime">Optional custom lifetime in seconds (overrides calculated lifetime)</param>
        /// <returns>The playing particle system</returns>
        T PlayAutoRelease(Vector3 position, Quaternion rotation, float? customLifetime = null);

        /// <summary>
        /// Plays a particle system at a transform and automatically releases it when complete
        /// </summary>
        /// <param name="transform">Transform to match</param>
        /// <param name="customLifetime">Optional custom lifetime in seconds (overrides calculated lifetime)</param>
        /// <returns>The playing particle system</returns>
        T PlayAutoReleaseAtTransform(Transform transform, float? customLifetime = null);

        /// <summary>
        /// Plays a particle system with children and automatically releases it when all particles are gone
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">World rotation</param>
        /// <returns>The playing particle system</returns>
        T PlayWithChildrenAutoRelease(Vector3 position, Quaternion rotation);
    }
}