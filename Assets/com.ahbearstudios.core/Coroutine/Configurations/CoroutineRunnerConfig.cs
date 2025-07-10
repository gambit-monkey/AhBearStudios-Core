using System;
using AhBearStudios.Core.Coroutine.Interfaces;

namespace AhBearStudios.Core.Coroutine.Configurations
{
    /// <summary>
    /// Default implementation of coroutine runner configuration.
    /// Provides sensible defaults while allowing full customization.
    /// </summary>
    public sealed class CoroutineRunnerConfig : ICoroutineRunnerConfig
    {
        /// <inheritdoc />
        public string Name { get; set; } = "DefaultRunner";

        /// <inheritdoc />
        public bool Persistent { get; set; } = false;

        /// <inheritdoc />
        public int InitialCapacity { get; set; } = 64;

        /// <inheritdoc />
        public bool EnableStatistics { get; set; } = true;

        /// <inheritdoc />
        public bool EnableDebugLogging { get; set; } = false;

        /// <inheritdoc />
        public bool EnableProfiling { get; set; } = false;

        /// <inheritdoc />
        public int MaxConcurrentCoroutines { get; set; } = 0; // Unlimited

        /// <inheritdoc />
        public bool AutoDisposeWhenEmpty { get; set; } = false;

        /// <inheritdoc />
        public float AutoDisposeTimeout { get; set; } = 30f; // 30 seconds

        /// <inheritdoc />
        public bool EnablePauseResume { get; set; } = true;

        /// <inheritdoc />
        public bool UseHighPrecisionTiming { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the CoroutineRunnerConfig class with default values.
        /// </summary>
        public CoroutineRunnerConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the CoroutineRunnerConfig class with the specified name.
        /// </summary>
        /// <param name="name">The name for the coroutine runner.</param>
        public CoroutineRunnerConfig(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Initializes a new instance of the CoroutineRunnerConfig class with the specified parameters.
        /// </summary>
        /// <param name="name">The name for the coroutine runner.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes.</param>
        /// <param name="initialCapacity">Initial capacity for coroutine tracking collections.</param>
        public CoroutineRunnerConfig(string name, bool persistent, int initialCapacity = 64)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Persistent = persistent;
            InitialCapacity = initialCapacity;
        }

        /// <inheritdoc />
        public ICoroutineRunnerConfig Clone()
        {
            return new CoroutineRunnerConfig
            {
                Name = Name,
                Persistent = Persistent,
                InitialCapacity = InitialCapacity,
                EnableStatistics = EnableStatistics,
                EnableDebugLogging = EnableDebugLogging,
                EnableProfiling = EnableProfiling,
                MaxConcurrentCoroutines = MaxConcurrentCoroutines,
                AutoDisposeWhenEmpty = AutoDisposeWhenEmpty,
                AutoDisposeTimeout = AutoDisposeTimeout,
                EnablePauseResume = EnablePauseResume,
                UseHighPrecisionTiming = UseHighPrecisionTiming
            };
        }

        /// <inheritdoc />
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (InitialCapacity < 1)
                return false;

            if (MaxConcurrentCoroutines < 0)
                return false;

            if (AutoDisposeTimeout < 0f)
                return false;

            return true;
        }

        /// <summary>
        /// Creates a configuration optimized for performance scenarios.
        /// </summary>
        /// <param name="name">The name for the runner.</param>
        /// <param name="initialCapacity">Initial capacity for collections.</param>
        /// <returns>A performance-optimized configuration.</returns>
        public static CoroutineRunnerConfig CreatePerformanceConfig(string name, int initialCapacity = 128)
        {
            return new CoroutineRunnerConfig(name, true, initialCapacity)
            {
                EnableStatistics = true,
                EnableDebugLogging = false,
                EnableProfiling = false,
                UseHighPrecisionTiming = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for debugging scenarios.
        /// </summary>
        /// <param name="name">The name for the runner.</param>
        /// <returns>A debug-optimized configuration.</returns>
        public static CoroutineRunnerConfig CreateDebugConfig(string name)
        {
            return new CoroutineRunnerConfig(name, false)
            {
                EnableStatistics = true,
                EnableDebugLogging = true,
                EnableProfiling = true,
                UseHighPrecisionTiming = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for lightweight scenarios.
        /// </summary>
        /// <param name="name">The name for the runner.</param>
        /// <returns>A lightweight configuration.</returns>
        public static CoroutineRunnerConfig CreateLightweightConfig(string name)
        {
            return new CoroutineRunnerConfig(name, false, 16)
            {
                EnableStatistics = false,
                EnableDebugLogging = false,
                EnableProfiling = false,
                UseHighPrecisionTiming = false
            };
        }

        /// <summary>
        /// Creates a configuration optimized for temporary runners.
        /// </summary>
        /// <param name="name">The name for the runner.</param>
        /// <param name="autoDisposeTimeout">Timeout for automatic disposal.</param>
        /// <returns>A temporary runner configuration.</returns>
        public static CoroutineRunnerConfig CreateTemporaryConfig(string name, float autoDisposeTimeout = 30f)
        {
            return new CoroutineRunnerConfig(name, false)
            {
                AutoDisposeWhenEmpty = true,
                AutoDisposeTimeout = autoDisposeTimeout,
                EnableStatistics = false,
                EnableDebugLogging = false
            };
        }
    }
}