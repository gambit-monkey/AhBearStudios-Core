using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Pools.Unity;
using UnityEngine;

namespace AhBearStudios.Pooling.Extensions
{
    /// <summary>
    /// Extension methods for Unity-specific pool operations
    /// </summary>
    public static class UnityPoolExtensions
    {
        /// <summary>
        /// Acquires a component and positions it
        /// </summary>
        /// <typeparam name="T">Type of component</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="position">Position to set</param>
        /// <param name="rotation">Rotation to set</param>
        /// <returns>The acquired component</returns>
        public static T AcquireAndPosition<T>(this ComponentPool<T> pool, Vector3 position, Quaternion rotation) where T : Component
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            var component = pool.Acquire();
            component.transform.position = position;
            component.transform.rotation = rotation;
            return component;
        }
        
        /// <summary>
        /// Acquires a component and sets it as a child of another transform
        /// </summary>
        /// <typeparam name="T">Type of component</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="parent">Parent transform</param>
        /// <param name="localPosition">Local position</param>
        /// <returns>The acquired component</returns>
        public static T AcquireAsChild<T>(this ComponentPool<T> pool, Transform parent, Vector3 localPosition = default) where T : Component
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
                
            var component = pool.Acquire();
            var transform = component.transform;
            transform.SetParent(parent);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            return component;
        }
        
        /// <summary>
        /// Acquires a collection of components with random positions within a sphere
        /// </summary>
        /// <typeparam name="T">Type of component</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="count">Number of components to acquire</param>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the sphere</param>
        /// <returns>List of acquired components</returns>
        public static List<T> AcquireSphere<T>(this ComponentPool<T> pool, int count, Vector3 center, float radius) where T : Component
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                
            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive");
                
            var items = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                var component = pool.Acquire();
                component.transform.position = center + UnityEngine.Random.insideUnitSphere * radius;
                items.Add(component);
            }
            
            return items;
        }
        
        /// <summary>
        /// Finds all components of a specified type in a GameObject and releases them to their pools
        /// </summary>
        /// <typeparam name="T">Type of component</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="gameObject">GameObject to search</param>
        /// <param name="includeInactive">Whether to include inactive components</param>
        /// <returns>Number of components released</returns>
        public static int ReleaseAllFromGameObject<T>(this ComponentPool<T> pool, GameObject gameObject, bool includeInactive = false) where T : Component
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));
                
            var components = gameObject.GetComponentsInChildren<T>(includeInactive);
            int count = 0;
            
            foreach (var component in components)
            {
                pool.Release(component);
                count++;
            }
            
            return count;
        }
    }
}