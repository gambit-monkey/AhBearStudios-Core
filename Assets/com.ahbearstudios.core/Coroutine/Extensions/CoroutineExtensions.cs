using System;
using System.Collections;
using AhBearStudios.Core.Coroutine.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Coroutine.Extensions
{
    /// <summary>
    /// Extension methods for enhanced coroutine functionality.
    /// </summary>
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Starts a coroutine with automatic timeout handling.
        /// </summary>
        /// <param name="runner">The coroutine runner to use.</param>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="timeoutSeconds">Timeout in seconds.</param>
        /// <param name="onTimeout">Optional callback for timeout.</param>
        /// <param name="tag">Optional tag for the coroutine.</param>
        /// <returns>A handle for the coroutine.</returns>
        public static ICoroutineHandle StartCoroutineWithTimeout(
            this ICoroutineRunner runner,
            IEnumerator routine,
            float timeoutSeconds,
            Action onTimeout = null,
            string tag = null)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));
            if (routine == null)
                throw new ArgumentNullException(nameof(routine));

            return runner.StartCoroutine(
                TimeoutCoroutine(routine, timeoutSeconds, onTimeout),
                tag);
        }

        /// <summary>
        /// Starts a coroutine that executes until a condition is met.
        /// </summary>
        /// <param name="runner">The coroutine runner to use.</param>
        /// <param name="condition">The condition to wait for.</param>
        /// <param name="action">The action to execute each frame while waiting.</param>
        /// <param name="onComplete">Optional callback when condition is met.</param>
        /// <param name="tag">Optional tag for the coroutine.</param>
        /// <returns>A handle for the coroutine.</returns>
        public static ICoroutineHandle StartWaitUntil(
            this ICoroutineRunner runner,
            Func<bool> condition,
            Action action = null,
            Action onComplete = null,
            string tag = null)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            return runner.StartCoroutine(
                WaitUntilCoroutine(condition, action),
                onComplete,
                tag);
        }

        /// <summary>
        /// Starts a coroutine that executes while a condition is true.
        /// </summary>
        /// <param name="runner">The coroutine runner to use.</param>
        /// <param name="condition">The condition to check.</param>
        /// <param name="action">The action to execute each frame while condition is true.</param>
        /// <param name="onComplete">Optional callback when condition becomes false.</param>
        /// <param name="tag">Optional tag for the coroutine.</param>
        /// <returns>A handle for the coroutine.</returns>
        public static ICoroutineHandle StartWaitWhile(
            this ICoroutineRunner runner,
            Func<bool> condition,
            Action action = null,
            Action onComplete = null,
            string tag = null)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            return runner.StartCoroutine(
                WaitWhileCoroutine(condition, action),
                onComplete,
                tag);
        }

        /// <summary>
        /// Chains multiple coroutines to execute sequentially.
        /// </summary>
        /// <param name="runner">The coroutine runner to use.</param>
        /// <param name="routines">The coroutines to chain.</param>
        /// <param name="onComplete">Optional callback when all routines complete.</param>
        /// <param name="tag">Optional tag for the coroutine.</param>
        /// <returns>A handle for the chained coroutine.</returns>
        public static ICoroutineHandle StartCoroutineChain(
            this ICoroutineRunner runner,
            IEnumerator[] routines,
            Action onComplete = null,
            string tag = null)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));
            if (routines == null)
                throw new ArgumentNullException(nameof(routines));

            return runner.StartCoroutine(
                ChainCoroutines(routines),
                onComplete,
                tag);
        }

        #region Private Coroutine Implementations

        private static IEnumerator TimeoutCoroutine(IEnumerator routine, float timeoutSeconds, Action onTimeout)
        {
            float elapsedTime = 0f;
            bool completed = false;

            // Start the main routine
            while (routine.MoveNext() && elapsedTime < timeoutSeconds)
            {
                yield return routine.Current;
                elapsedTime += Time.deltaTime;
            }

            // Check if we completed or timed out
            if (elapsedTime >= timeoutSeconds)
            {
                onTimeout?.Invoke();
            }
        }

        private static IEnumerator WaitUntilCoroutine(Func<bool> condition, Action action)
        {
            while (!condition())
            {
                action?.Invoke();
                yield return null;
            }
        }

        private static IEnumerator WaitWhileCoroutine(Func<bool> condition, Action action)
        {
            while (condition())
            {
                action?.Invoke();
                yield return null;
            }
        }

        private static IEnumerator ChainCoroutines(IEnumerator[] routines)
        {
            foreach (var routine in routines)
            {
                if (routine != null)
                {
                    while (routine.MoveNext())
                    {
                        yield return routine.Current;
                    }
                }
            }
        }

        #endregion
    }
}