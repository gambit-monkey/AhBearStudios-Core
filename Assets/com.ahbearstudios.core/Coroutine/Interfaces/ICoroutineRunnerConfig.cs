namespace AhBearStudios.Core.Coroutine.Interfaces
{
    /// <summary>
    /// Configuration interface for customizing coroutine runner behavior.
    /// Provides comprehensive control over performance, features, and lifecycle management.
    /// </summary>
    public interface ICoroutineRunnerConfig
    {
        /// <summary>
        /// Gets or sets the name of the coroutine runner.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the runner should persist across scene changes.
        /// </summary>
        bool Persistent { get; set; }

        /// <summary>
        /// Gets or sets the initial capacity for coroutine tracking collections.
        /// </summary>
        int InitialCapacity { get; set; }

        /// <summary>
        /// Gets or sets whether to enable detailed statistics collection.
        /// </summary>
        bool EnableStatistics { get; set; }

        /// <summary>
        /// Gets or sets whether to enable debug logging.
        /// </summary>
        bool EnableDebugLogging { get; set; }

        /// <summary>
        /// Gets or sets whether to enable performance profiling.
        /// </summary>
        bool EnableProfiling { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of coroutines allowed to run simultaneously.
        /// Set to 0 for unlimited.
        /// </summary>
        int MaxConcurrentCoroutines { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically dispose the runner when empty.
        /// </summary>
        bool AutoDisposeWhenEmpty { get; set; }

        /// <summary>
        /// Gets or sets the timeout for automatic disposal when empty (in seconds).
        /// Only used if AutoDisposeWhenEmpty is true.
        /// </summary>
        float AutoDisposeTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether to enable pause/resume functionality.
        /// </summary>
        bool EnablePauseResume { get; set; }

        /// <summary>
        /// Gets or sets whether to use high-precision timing for coroutine statistics.
        /// </summary>
        bool UseHighPrecisionTiming { get; set; }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same settings.</returns>
        ICoroutineRunnerConfig Clone();

        /// <summary>
        /// Validates the configuration settings.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        bool Validate();
    }
}