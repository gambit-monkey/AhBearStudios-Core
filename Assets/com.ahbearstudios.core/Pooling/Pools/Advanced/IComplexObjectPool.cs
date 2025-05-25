using System;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core;
using AhBearStudios.Pooling.Diagnostics;

namespace AhBearStudios.Pooling.Pools.Advanced
{
    /// <summary>
    /// Interface for complex object pool implementations that provides advanced features like
    /// dependency management, property storage, and multiple object acquisition/release operations.
    /// </summary>
    /// <typeparam name="T">The type of objects managed by the pool</typeparam>
    public interface IComplexObjectPool<T> : IPool<T>, IShrinkablePool
    {
        /// <summary>
        /// Prewarms the pool by creating the specified number of objects
        /// </summary>
        /// <param name="count">Number of objects to create</param>
        void PrewarmPool(int count);

        /// <summary>
        /// Acquires multiple items from the pool at once
        /// </summary>
        /// <param name="count">Number of items to acquire</param>
        /// <returns>A list of acquired items</returns>
        List<T> AcquireMultiple(int count);

        /// <summary>
        /// Acquires an item from the pool and applies the setup action to it
        /// </summary>
        /// <param name="setupAction">Action to configure the acquired item</param>
        /// <returns>The acquired and configured item</returns>
        T AcquireAndSetup(Action<T> setupAction);

        /// <summary>
        /// Releases multiple items back to the pool at once
        /// </summary>
        /// <param name="items">The items to release</param>
        void ReleaseMultiple(IEnumerable<T> items);

        /// <summary>
        /// Registers a dependency for an item that will be disposed when the item is destroyed
        /// </summary>
        /// <param name="item">The item to register a dependency for</param>
        /// <param name="dependency">The dependency to register</param>
        void RegisterDependency(T item, IDisposable dependency);

        /// <summary>
        /// Sets a property on an item
        /// </summary>
        /// <param name="item">The item to set a property on</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="value">The value of the property</param>
        void SetProperty(T item, string propertyName, object value);

        /// <summary>
        /// Gets a property from an item
        /// </summary>
        /// <param name="item">The item to get a property from</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="defaultValue">The default value to return if the property doesn't exist</param>
        /// <returns>The property value or the default value if not found</returns>
        object GetProperty(T item, string propertyName, object defaultValue = null);
    }
}