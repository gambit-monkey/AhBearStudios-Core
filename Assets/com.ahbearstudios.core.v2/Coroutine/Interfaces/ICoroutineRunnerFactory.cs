namespace AhBearStudios.Core.Coroutine.Interfaces
{
    /// <summary>
    /// Factory interface for creating coroutine runners with different configurations.
    /// Provides a standardized way to create coroutine runners with various optimization profiles.
    /// </summary>
    public interface ICoroutineRunnerFactory
    {
        /// <summary>
        /// Creates a new standard coroutine runner with the specified configuration.
        /// </summary>
        /// <param name="name">The name for the new runner.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <returns>The created coroutine runner.</returns>
        /// <exception cref="System.ArgumentException">Thrown when name is null or empty.</exception>
        ICoroutineRunner CreateRunner(string name, bool persistent = false);

        /// <summary>
        /// Creates a temporary coroutine runner that will be automatically disposed.
        /// Useful for short-lived operations or scene-specific coroutines.
        /// </summary>
        /// <param name="name">The name for the temporary runner.</param>
        /// <param name="lifetimeSeconds">How long the runner should exist (0 = until scene change).</param>
        /// <returns>The created temporary coroutine runner.</returns>
        /// <exception cref="System.ArgumentException">Thrown when name is null or empty.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when lifetimeSeconds is negative.</exception>
        ICoroutineRunner CreateTemporaryRunner(string name, float lifetimeSeconds = 0f);

        /// <summary>
        /// Creates a coroutine runner optimized for high-performance scenarios.
        /// Pre-allocates collections and uses optimized settings for better performance.
        /// </summary>
        /// <param name="name">The name for the performance runner.</param>
        /// <param name="initialCapacity">Initial capacity for coroutine tracking collections.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <returns>The created performance-optimized coroutine runner.</returns>
        /// <exception cref="System.ArgumentException">Thrown when name is null or empty.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when initialCapacity is less than 1.</exception>
        ICoroutineRunner CreatePerformanceRunner(string name, int initialCapacity = 64, bool persistent = false);

        /// <summary>
        /// Creates a coroutine runner with custom configuration options.
        /// Provides fine-grained control over runner behavior and performance characteristics.
        /// </summary>
        /// <param name="config">Configuration object defining runner behavior.</param>
        /// <returns>The created configured coroutine runner.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when config is null.</exception>
        ICoroutineRunner CreateConfiguredRunner(ICoroutineRunnerConfig config);

        /// <summary>
        /// Creates a lightweight coroutine runner for minimal overhead scenarios.
        /// Optimized for simple coroutines with minimal tracking and statistics.
        /// </summary>
        /// <param name="name">The name for the lightweight runner.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <returns>The created lightweight coroutine runner.</returns>
        /// <exception cref="System.ArgumentException">Thrown when name is null or empty.</exception>
        ICoroutineRunner CreateLightweightRunner(string name, bool persistent = false);

        /// <summary>
        /// Creates a coroutine runner specifically optimized for pooling operations.
        /// Includes specialized features for object lifecycle management.
        /// </summary>
        /// <param name="name">The name for the pooling runner.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <returns>The created pooling-optimized coroutine runner.</returns>
        /// <exception cref="System.ArgumentException">Thrown when name is null or empty.</exception>
        ICoroutineRunner CreatePoolingRunner(string name, bool persistent = true);

        /// <summary>
        /// Creates a coroutine runner with built-in debugging and profiling features.
        /// Includes enhanced logging, performance tracking, and diagnostic capabilities.
        /// </summary>
        /// <param name="name">The name for the debug runner.</param>
        /// <param name="enableProfiling">Whether to enable detailed performance profiling.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <returns>The created debug-enabled coroutine runner.</returns>
        /// <exception cref="System.ArgumentException">Thrown when name is null or empty.</exception>
        ICoroutineRunner CreateDebugRunner(string name, bool enableProfiling = true, bool persistent = false);
    }
}