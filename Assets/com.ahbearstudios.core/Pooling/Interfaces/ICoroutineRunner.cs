using System;
using System.Collections;
using UnityEngine;
using AhBearStudios.Core.Pooling.Pools.Unity;

namespace AhBearStudios.Core.Pooling.Interfaces
{
    /// <summary>
    /// Interface for running coroutines in the pooling system.
    /// Abstracts Unity coroutine functionality for better testability and composition.
    /// </summary>
    public interface ICoroutineRunner
    {
        /// <summary>
        /// Starts a particle system release coroutine that automatically releases
        /// the particle system back to the pool when all particles have completed.
        /// </summary>
        /// <param name="pool">The particle system pool</param>
        /// <param name="particleSystem">The particle system to release</param>
        /// <param name="includeChildren">Whether to wait for child particles</param>
        /// <returns>ID of the started coroutine for cancellation</returns>
        int StartReleaseWhenCompleteCoroutine(IParticleSystemPool<ParticleSystem> pool, ParticleSystem particleSystem, bool includeChildren);
        
        /// <summary>
        /// Starts a generic delayed release coroutine that releases an item
        /// back to its pool after a specified time delay.
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="item">The item to release</param>
        /// <param name="delay">Delay in seconds before releasing</param>
        /// <returns>ID of the started coroutine for cancellation</returns>
        int StartDelayedReleaseCoroutine<T>(IPool<T> pool, T item, float delay);
        
        /// <summary>
        /// Starts a generic conditional release coroutine that releases an item
        /// back to its pool when a specified condition is met.
        /// </summary>
        /// <typeparam name="T">Type of pooled object</typeparam>
        /// <param name="pool">The pool</param>
        /// <param name="item">The item to release</param>
        /// <param name="condition">Func that returns true when the item should be released</param>
        /// <returns>ID of the started coroutine for cancellation</returns>
        int StartConditionalReleaseCoroutine<T>(IPool<T> pool, T item, Func<T, bool> condition);
        
        /// <summary>
        /// Cancels a coroutine by ID.
        /// </summary>
        /// <param name="id">ID of the coroutine to cancel</param>
        /// <returns>True if cancelled, false if not found or already completed</returns>
        bool CancelCoroutine(int id);
        
        /// <summary>
        /// Starts a general coroutine. This is part of the interface to allow
        /// for abstraction and better testability.
        /// </summary>
        /// <param name="routine">The coroutine to start</param>
        /// <returns>The started coroutine</returns>
        Coroutine StartCoroutine(IEnumerator routine);
        
        /// <summary>
        /// Stops a running coroutine. This is part of the interface to allow
        /// for abstraction and better testability.
        /// </summary>
        /// <param name="routine">The coroutine to stop</param>
        void StopCoroutine(Coroutine routine);
    }
}