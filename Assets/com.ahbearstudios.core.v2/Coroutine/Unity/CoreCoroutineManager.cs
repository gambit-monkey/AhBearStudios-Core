using System;
using System.Collections.Generic;
using AhBearStudios.Core.Coroutine.Interfaces;
using UnityEngine;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.Coroutine.Unity
{
    /// <summary>
    /// Central manager for all coroutine runners in the application.
    /// Provides factory methods and unified management of coroutine execution.
    /// </summary>
    public sealed class CoreCoroutineManager : MonoBehaviour, ICoroutineManager
    {
        #region Static Instance

        private static CoreCoroutineManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of the coroutine manager.
        /// </summary>
        public static CoreCoroutineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("[CoreCoroutineManager]");
                            _instance = go.AddComponent<CoreCoroutineManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Private Fields

        private readonly Dictionary<string, ICoroutineRunner> _runners = new Dictionary<string, ICoroutineRunner>();
        private readonly CoroutineStatistics _globalStatistics = new CoroutineStatistics();
        
        private ICoroutineRunner _defaultRunner;
        private IDependencyProvider _dependencyProvider;
        private bool _isInitialized;
        private bool _isDisposed;

        #endregion

        #region Events

        /// <inheritdoc />
        public event Action<string, ICoroutineRunner> OnRunnerCreated;

        /// <inheritdoc />
        public event Action<string> OnRunnerRemoved;

        #endregion

        #region Properties

        /// <inheritdoc />
        public ICoroutineRunner DefaultRunner => _defaultRunner;

        /// <inheritdoc />
        public bool IsActive => _isInitialized && !_isDisposed;

        /// <inheritdoc />
        public int TotalActiveCoroutines
        {
            get
            {
                int total = 0;
                foreach (var runner in _runners.Values)
                {
                    total += runner.ActiveCoroutineCount;
                }
                return total;
            }
        }

        /// <inheritdoc />
        public ICoroutineStatistics Statistics => _globalStatistics;

        #endregion

        #region Initialization

        private void Awake()
        {
            // Ensure singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Initializes the coroutine manager.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // Create the default runner
            _defaultRunner = CreateRunner("Default", true);

            _isInitialized = true;
    
            Debug.Log("[CoreCoroutineManager] Initialized successfully");
        }

        #endregion

        #region MonoBehaviour Lifecycle

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region ICoroutineManager Implementation

        /// <inheritdoc />
        public ICoroutineRunner CreateRunner(string name, bool persistent = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Runner name cannot be null or empty", nameof(name));

            if (_runners.ContainsKey(name))
                throw new ArgumentException($"A runner with name '{name}' already exists", nameof(name));

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CoreCoroutineManager));

            // Create GameObject for the runner
            var runnerGO = new GameObject($"[CoroutineRunner_{name}]");
            if (persistent)
            {
                DontDestroyOnLoad(runnerGO);
            }

            // Add and initialize the runner component
            var runner = runnerGO.AddComponent<UnityCoroutineRunner>();
            runner.Initialize();

            // Track the runner
            _runners[name] = runner;

            // Fire event
            OnRunnerCreated?.Invoke(name, runner);

            return runner;
        }

        /// <inheritdoc />
        public ICoroutineRunner GetRunner(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return _runners.TryGetValue(name, out var runner) ? runner : null;
        }

        /// <inheritdoc />
        public bool RemoveRunner(string name)
        {
            if (string.IsNullOrEmpty(name) || _isDisposed)
                return false;

            if (!_runners.TryGetValue(name, out var runner))
                return false;

            // Don't allow removal of the default runner
            if (runner == _defaultRunner)
                return false;

            // Remove from tracking
            _runners.Remove(name);

            // Dispose the runner if it's disposable
            if (runner is IDisposable disposableRunner)
            {
                disposableRunner.Dispose();
            }

            // Destroy the GameObject if it's a Unity component
            if (runner is UnityCoroutineRunner unityRunner)
            {
                if (unityRunner != null)
                {
                    Destroy(unityRunner.gameObject);
                }
            }

            // Fire event
            OnRunnerRemoved?.Invoke(name);

            return true;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetRunnerNames()
        {
            return _runners.Keys;
        }

        /// <inheritdoc />
        public int StopAllCoroutines()
        {
            if (_isDisposed)
                return 0;

            int totalStopped = 0;
            foreach (var runner in _runners.Values)
            {
                totalStopped += runner.StopAllCoroutines();
            }

            return totalStopped;
        }

        /// <inheritdoc />
        public void PauseAllCoroutines()
        {
            if (_isDisposed)
                return;

            foreach (var runner in _runners.Values)
            {
                if (runner is UnityCoroutineRunner unityRunner)
                {
                    unityRunner.Pause();
                }
            }
        }

        /// <inheritdoc />
        public void ResumeAllCoroutines()
        {
            if (_isDisposed)
                return;

            foreach (var runner in _runners.Values)
            {
                if (runner is UnityCoroutineRunner unityRunner)
                {
                    unityRunner.Resume();
                }
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a coroutine manager instance and initializes it.
        /// </summary>
        /// <param name="dependencyProvider">Optional dependency provider for service registration.</param>
        /// <returns>The created and initialized coroutine manager.</returns>
        public static CoreCoroutineManager Create(IDependencyProvider dependencyProvider = null)
        {
            var instance = Instance;
            instance.Initialize();
            return instance;
        }

        /// <summary>
        /// Gets or creates a runner with the specified name.
        /// </summary>
        /// <param name="name">The name of the runner to get or create.</param>
        /// <param name="persistent">Whether the runner should persist across scene changes (only used when creating).</param>
        /// <returns>The existing or newly created runner.</returns>
        public ICoroutineRunner GetOrCreateRunner(string name, bool persistent = false)
        {
            var existing = GetRunner(name);
            return existing ?? CreateRunner(name, persistent);
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            // Stop all coroutines
            StopAllCoroutines();

            // Dispose all runners
            var runnersToDispose = new List<string>(_runners.Keys);
            foreach (var runnerName in runnersToDispose)
            {
                if (runnerName != "Default") // Keep default runner for last
                {
                    RemoveRunner(runnerName);
                }
            }

            // Dispose default runner
            if (_defaultRunner is IDisposable disposableDefault)
            {
                disposableDefault.Dispose();
            }

            // Clear collections
            _runners.Clear();

            // Dispose statistics
            _globalStatistics?.Dispose();

            _isDisposed = true;
            _isInitialized = false;

            // Clear singleton reference
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion
    }
}