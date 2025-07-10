using System;
using AhBearStudios.Core.Coroutine.Configurations;
using AhBearStudios.Core.Coroutine.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Coroutine.Factories
{
    /// <summary>
    /// Factory implementation for creating various types of coroutine runners.
    /// Provides optimized runner creation for different use cases and performance requirements.
    /// </summary>
    public sealed class CoroutineRunnerFactory : ICoroutineRunnerFactory
    {
        #region Private Fields

        private readonly Transform _rootTransform;
        private int _runnerCounter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the CoroutineRunnerFactory class.
        /// </summary>
        public CoroutineRunnerFactory()
        {
            // Create a root container for all factory-created runners
            var rootGO = new GameObject("[CoroutineRunners]");
            _rootTransform = rootGO.transform;
            UnityEngine.Object.DontDestroyOnLoad(rootGO);
        }

        /// <summary>
        /// Initializes a new instance of the CoroutineRunnerFactory class with a specific root transform.
        /// </summary>
        /// <param name="rootTransform">The root transform to parent runners under.</param>
        public CoroutineRunnerFactory(Transform rootTransform)
        {
            _rootTransform = rootTransform ?? throw new ArgumentNullException(nameof(rootTransform));
        }

        #endregion

        #region ICoroutineRunnerFactory Implementation

        /// <inheritdoc />
        public ICoroutineRunner CreateRunner(string name, bool persistent = false)
        {
            ValidateName(name);

            var config = new CoroutineRunnerConfig(name, persistent);
            return CreateRunnerInternal(config);
        }

        /// <inheritdoc />
        public ICoroutineRunner CreateTemporaryRunner(string name, float lifetimeSeconds = 0f)
        {
            ValidateName(name);

            if (lifetimeSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds), "Lifetime cannot be negative");

            var config = CoroutineRunnerConfig.CreateTemporaryConfig(name, lifetimeSeconds);
            var runner = CreateRunnerInternal(config);

            // Set up automatic disposal if lifetime is specified
            if (lifetimeSeconds > 0f)
            {
                runner.StartDelayedAction(lifetimeSeconds, () =>
                {
                    if (runner is IDisposable disposableRunner)
                    {
                        disposableRunner.Dispose();
                    }
                }, "AutoDispose");
            }

            return runner;
        }

        /// <inheritdoc />
        public ICoroutineRunner CreatePerformanceRunner(string name, int initialCapacity = 64, bool persistent = false)
        {
            ValidateName(name);

            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be at least 1");

            var config = CoroutineRunnerConfig.CreatePerformanceConfig(name, initialCapacity);
            config.Persistent = persistent;

            return CreateRunnerInternal(config);
        }

        /// <inheritdoc />
        public ICoroutineRunner CreateConfiguredRunner(ICoroutineRunnerConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.Validate())
                throw new ArgumentException("Configuration validation failed", nameof(config));

            return CreateRunnerInternal(config);
        }

        /// <inheritdoc />
        public ICoroutineRunner CreateLightweightRunner(string name, bool persistent = false)
        {
            ValidateName(name);

            var config = CoroutineRunnerConfig.CreateLightweightConfig(name);
            config.Persistent = persistent;

            return CreateRunnerInternal(config);
        }

        /// <inheritdoc />
        public ICoroutineRunner CreatePoolingRunner(string name, bool persistent = true)
        {
            ValidateName(name);

            // Create a configuration optimized for pooling operations
            var config = new CoroutineRunnerConfig(name, persistent, 128)
            {
                EnableStatistics = true,
                EnableDebugLogging = false,
                EnableProfiling = false,
                MaxConcurrentCoroutines = 0, // Unlimited for pooling
                UseHighPrecisionTiming = false
            };

            return CreateRunnerInternal(config);
        }

        /// <inheritdoc />
        public ICoroutineRunner CreateDebugRunner(string name, bool enableProfiling = true, bool persistent = false)
        {
            ValidateName(name);

            var config = CoroutineRunnerConfig.CreateDebugConfig(name);
            config.Persistent = persistent;
            config.EnableProfiling = enableProfiling;

            return CreateRunnerInternal(config);
        }

        #endregion

        #region Factory Convenience Methods

        /// <summary>
        /// Creates a runner optimized for UI operations.
        /// </summary>
        /// <param name="name">The name for the UI runner.</param>
        /// <returns>A UI-optimized coroutine runner.</returns>
        public ICoroutineRunner CreateUIRunner(string name = "UI")
        {
            var config = new CoroutineRunnerConfig(name, false, 32)
            {
                EnableStatistics = false,
                EnableDebugLogging = false,
                EnableProfiling = false,
                MaxConcurrentCoroutines = 50, // Reasonable limit for UI
                UseHighPrecisionTiming = false
            };

            return CreateRunnerInternal(config);
        }

        /// <summary>
        /// Creates a runner optimized for audio operations.
        /// </summary>
        /// <param name="name">The name for the audio runner.</param>
        /// <returns>An audio-optimized coroutine runner.</returns>
        public ICoroutineRunner CreateAudioRunner(string name = "Audio")
        {
            var config = new CoroutineRunnerConfig(name, true, 64)
            {
                EnableStatistics = true,
                EnableDebugLogging = false,
                EnableProfiling = false,
                UseHighPrecisionTiming = true // Important for audio timing
            };

            return CreateRunnerInternal(config);
        }

        /// <summary>
        /// Creates a runner optimized for networking operations.
        /// </summary>
        /// <param name="name">The name for the networking runner.</param>
        /// <returns>A networking-optimized coroutine runner.</returns>
        public ICoroutineRunner CreateNetworkingRunner(string name = "Networking")
        {
            var config = new CoroutineRunnerConfig(name, true, 128)
            {
                EnableStatistics = true,
                EnableDebugLogging = false,
                EnableProfiling = false,
                MaxConcurrentCoroutines = 0, // Unlimited for networking
                UseHighPrecisionTiming = false
            };

            return CreateRunnerInternal(config);
        }

        /// <summary>
        /// Creates a runner optimized for gameplay operations.
        /// </summary>
        /// <param name="name">The name for the gameplay runner.</param>
        /// <returns>A gameplay-optimized coroutine runner.</returns>
        public ICoroutineRunner CreateGameplayRunner(string name = "Gameplay")
        {
            var config = new CoroutineRunnerConfig(name, false, 96)
            {
                EnableStatistics = true,
                EnableDebugLogging = false,
                EnableProfiling = false,
                UseHighPrecisionTiming = true // Important for gameplay timing
            };

            return CreateRunnerInternal(config);
        }

        #endregion

        #region Private Methods

        private ICoroutineRunner CreateRunnerInternal(ICoroutineRunnerConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Generate unique name if needed
            string uniqueName = GenerateUniqueName(config.Name);

            // Create GameObject for the runner
            var runnerGO = new GameObject($"[CoroutineRunner_{uniqueName}]");
            
            // Set up parenting and persistence
            if (_rootTransform != null)
            {
                runnerGO.transform.SetParent(_rootTransform);
            }

            if (config.Persistent)
            {
                UnityEngine.Object.DontDestroyOnLoad(runnerGO);
            }

            // Add and configure the runner component
            var runner = runnerGO.AddComponent<UnityCoroutineRunner>();
            
            // Apply configuration
            ApplyConfiguration(runner, config);
            
            // Initialize the runner
            runner.Initialize(uniqueName);

            return runner;
        }

        private void ApplyConfiguration(UnityCoroutineRunner runner, ICoroutineRunnerConfig config)
        {
            // For now, the UnityCoroutineRunner doesn't have all configuration options
            // This is where we would apply the configuration settings
            // This could be extended as the UnityCoroutineRunner gains more configuration options
            
            // Future implementation might include:
            // - Setting initial capacity for collections
            // - Configuring statistics collection
            // - Setting up profiling
            // - Configuring debug logging
            // - etc.
        }

        private string GenerateUniqueName(string baseName)
        {
            return $"{baseName}_{++_runnerCounter:D3}";
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Runner name cannot be null or empty", nameof(name));
        }

        #endregion

        #region Statistics and Management

        /// <summary>
        /// Gets the total number of runners created by this factory.
        /// </summary>
        public int TotalRunnersCreated => _runnerCounter;

        /// <summary>
        /// Gets the root transform used for organizing factory-created runners.
        /// </summary>
        public Transform RootTransform => _rootTransform;

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the factory and cleans up the root container.
        /// </summary>
        public void Dispose()
        {
            if (_rootTransform != null && _rootTransform.gameObject != null)
            {
                UnityEngine.Object.Destroy(_rootTransform.gameObject);
            }
        }

        #endregion
    }
}