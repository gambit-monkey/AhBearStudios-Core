using System.Collections;
using UnityEngine;

namespace AhBearStudios.Core.Logging.Interfaces
{
    /// <summary>
    /// Interface for running coroutines in the logging system.
    /// Provides abstraction for Unity coroutine functionality.
    /// </summary>
    public interface ILogCoroutineRunner
    {
        /// <summary>
        /// Starts a coroutine for auto-flushing log batches at regular intervals.
        /// </summary>
        /// <param name="routine">The coroutine to start</param>
        /// <returns>The started coroutine</returns>
        Coroutine StartCoroutine(IEnumerator routine);
        
        /// <summary>
        /// Stops a running coroutine.
        /// </summary>
        /// <param name="routine">The coroutine to stop</param>
        void StopCoroutine(Coroutine routine);
    }
}