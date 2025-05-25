using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AhBearStudios.Core.Pooling.Diagnostics;
using AhBearStudios.Core.Pooling.Pools.Unity;
using AhBearStudios.Core.Pooling.Services;
using Unity.Collections;

namespace AhBearStudios.Core.Pooling.Unity
{
    /// <summary>
    /// Component for running pooling-related coroutines with efficient memory management.
    /// This implementation optimizes for memory usage while providing a clean API.
    /// </summary>
    public class PoolCoroutineRunner : MonoBehaviour, ICoroutineRunner, IDisposable
    {
        #region Private Fields
        
        // Dependencies
        private IPoolLogger _logger;
        private IPoolingServiceLocator _serviceLocator;
        
        // We need to use a standard Dictionary for Coroutines since they're managed references
        private Dictionary<int, Coroutine> _activeCoroutines;
        
        // This is just to track IDs efficiently with native collections
        private NativeParallelHashMap<int, byte> _activeCoroutineIds;
        private int _nextCoroutineId;
        private bool _isDisposed;
        private bool _isInitialized;
        
        // Configuration
        private string _instanceName;
        
        #endregion
        
        #region Initialization
        
        private void Awake()
        {
            // Call Initialize if not already initialized
            if (!_isInitialized)
            {
                // Default initialization
                Initialize();
            }
        }
        
        /// <summary>
        /// Initializes the PoolCoroutineRunner with explicit dependencies.
        /// This can be called manually when adding the component programmatically.
        /// </summary>
        /// <param name="logger">Logger for pool operations</param>
        /// <param name="serviceLocator">Service locator for registration</param>
        /// <param name="instanceName">Optional name for this instance</param>
        /// <returns>The initialized runner instance (this)</returns>
        public PoolCoroutineRunner Initialize(
            IPoolLogger logger = null,
            IPoolingServiceLocator serviceLocator = null,
            string instanceName = null)
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"PoolCoroutineRunner already initialized. Ignoring additional initialization call.");
                return this;
            }
            
            // Initialize name
            _instanceName = string.IsNullOrEmpty(instanceName) 
                ? $"{gameObject.name}_PoolCoroutineRunner" 
                : instanceName;
            
            // Initialize collections
            _activeCoroutines = new Dictionary<int, Coroutine>();
            _activeCoroutineIds = new NativeParallelHashMap<int, byte>(16, Allocator.Persistent);
            _nextCoroutineId = 0;
            
            // Set dependencies or try to resolve from service locator
            _serviceLocator = serviceLocator ?? DefaultPoolingServices.Instance;

            _logger = logger;
            if (_logger == null && _serviceLocator != null)
            {
                _logger = _serviceLocator.GetService<IPoolLogger>();
            }
            
            // Register this instance with the service locator
            _serviceLocator?.RegisterService<ICoroutineRunner>(this);
            _serviceLocator?.RegisterService(this);
            
            _isInitialized = true;
            _isDisposed = false;
            
            _logger?.LogInfoInstance($"{_instanceName} initialized");
            
            return this;
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Creates a new PoolCoroutineRunner GameObject with component attached and initialized.
        /// </summary>
        /// <param name="instanceName">Optional name for the created GameObject</param>
        /// <param name="dontDestroyOnLoad">Whether to mark the object as DontDestroyOnLoad</param>
        /// <param name="logger">Optional logger to inject</param>
        /// <param name="serviceLocator">Optional service locator to inject</param>
        /// <returns>The created and initialized PoolCoroutineRunner component</returns>
        public static PoolCoroutineRunner Create(
            string instanceName = null, 
            bool dontDestroyOnLoad = true, 
            IPoolLogger logger = null, 
            IPoolingServiceLocator serviceLocator = null)
        {
            string name = string.IsNullOrEmpty(instanceName) ? "PoolCoroutineRunner" : instanceName;
            GameObject go = new GameObject(name);
            
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(go);
            }
            
            var runner = go.AddComponent<PoolCoroutineRunner>();
            return runner.Initialize(logger, serviceLocator, instanceName);
        }
        
        #endregion
        
        #region MonoBehaviour Lifecycle Methods
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed || !_isInitialized)
                return;
                
            // Unregister from service locator
            _serviceLocator?.UnregisterService<ICoroutineRunner>();
            _serviceLocator?.UnregisterService<PoolCoroutineRunner>();
            
            // Stop all active coroutines
            if (_activeCoroutines != null)
            {
                foreach (var coroutine in _activeCoroutines.Values)
                {
                    if (coroutine != null)
                    {
                        StopCoroutine(coroutine);
                    }
                }
                _activeCoroutines.Clear();
            }
            
            // Dispose native collection
            if (_activeCoroutineIds.IsCreated)
            {
                _activeCoroutineIds.Dispose();
            }
            
            _isDisposed = true;
            _isInitialized = false;
            
            _logger?.LogInfoInstance($"{_instanceName} disposed");
        }
        
        #endregion
        
        #region ICoroutineRunner Implementation
        
        /// <inheritdoc />
        public int StartReleaseWhenCompleteCoroutine(IParticleSystemPool<ParticleSystem> pool, ParticleSystem particleSystem, bool includeChildren)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (particleSystem == null)
                throw new ArgumentNullException(nameof(particleSystem));
            
            // Check for initialization and disposed state
            if (!_isInitialized || _isDisposed || !_activeCoroutineIds.IsCreated)
            {
                string errorMessage = !_isInitialized 
                    ? $"{_instanceName} has not been initialized" 
                    : $"{_instanceName} has been disposed";
                
                _logger?.LogErrorInstance($"{errorMessage} and cannot start new coroutines");
                return -1;
            }
                
            int id = _nextCoroutineId++;
            Coroutine coroutine = StartCoroutine(ReleaseWhenCompleteCoroutine(id, pool, particleSystem, includeChildren));
            
            // Store the coroutine and track its ID
            _activeCoroutines[id] = coroutine;
            _activeCoroutineIds.TryAdd(id, 1);
            
            return id;
        }
        
        /// <inheritdoc />
        public int StartDelayedReleaseCoroutine<T>(IPool<T> pool, T item, float delay)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            if (delay < 0)
                throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative");
            
            // Check for initialization and disposed state
            if (!_isInitialized || _isDisposed || !_activeCoroutineIds.IsCreated)
            {
                string errorMessage = !_isInitialized 
                    ? $"{_instanceName} has not been initialized" 
                    : $"{_instanceName} has been disposed";
                
                _logger?.LogErrorInstance($"{errorMessage} and cannot start new coroutines");
                return -1;
            }
                
            int id = _nextCoroutineId++;
            Coroutine coroutine = StartCoroutine(DelayedReleaseCoroutine(id, pool, item, delay));
            
            // Store the coroutine and track its ID
            _activeCoroutines[id] = coroutine;
            _activeCoroutineIds.TryAdd(id, 1);
            
            return id;
        }
        
        /// <inheritdoc />
        public int StartConditionalReleaseCoroutine<T>(IPool<T> pool, T item, Func<T, bool> condition)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
                
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            
            // Check for initialization and disposed state
            if (!_isInitialized || _isDisposed || !_activeCoroutineIds.IsCreated)
            {
                string errorMessage = !_isInitialized 
                    ? $"{_instanceName} has not been initialized" 
                    : $"{_instanceName} has been disposed";
                
                _logger?.LogErrorInstance($"{errorMessage} and cannot start new coroutines");
                return -1;
            }
                
            int id = _nextCoroutineId++;
            Coroutine coroutine = StartCoroutine(ConditionalReleaseCoroutine(id, pool, item, condition));
            
            // Store the coroutine and track its ID
            _activeCoroutines[id] = coroutine;
            _activeCoroutineIds.TryAdd(id, 1);
            
            return id;
        }
        
        /// <inheritdoc />
        public bool CancelCoroutine(int id)
        {
            // Check for initialization and disposed state
            if (!_isInitialized || _isDisposed || !_activeCoroutineIds.IsCreated)
            {
                string errorMessage = !_isInitialized 
                    ? $"{_instanceName} has not been initialized" 
                    : $"{_instanceName} has been disposed";
                
                _logger?.LogWarningInstance($"Cannot cancel coroutine: {errorMessage}");
                return false;
            }
            
            if (_activeCoroutines.TryGetValue(id, out Coroutine coroutine))
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                
                _activeCoroutines.Remove(id);
                _activeCoroutineIds.Remove(id);
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region Coroutine Implementation Details
        
        private IEnumerator ReleaseWhenCompleteCoroutine(int id, IParticleSystemPool<ParticleSystem> pool, ParticleSystem particleSystem, bool includeChildren)
        {
            if (!_isInitialized || _isDisposed)
            {
                yield break;
            }
            
            // Wait for one frame to ensure the particle system has started
            yield return null;
            
            // Wait until the particle system has stopped emitting and all particles are gone
            if (includeChildren)
            {
                // Check main and all children
                while (particleSystem != null && particleSystem.IsAlive(true))
                {
                    yield return null;
                }
            }
            else
            {
                // Just check the main system
                while (particleSystem != null && particleSystem.IsAlive(false))
                {
                    yield return null;
                }
            }
            
            // Release the particle system if we haven't been disposed
            if (_isInitialized && !_isDisposed && particleSystem != null)
            {
                try
                {
                    pool.Release(particleSystem);
                    _logger?.LogInfoInstance($"Particle system released after completion");
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error releasing particle system: {ex.Message}");
                }
            }
            
            // Remove from active coroutines
            if (_isInitialized && !_isDisposed)
            {
                _activeCoroutines.Remove(id);
                
                if (_activeCoroutineIds.IsCreated)
                {
                    _activeCoroutineIds.Remove(id);
                }
            }
        }
        
        private IEnumerator DelayedReleaseCoroutine<T>(int id, IPool<T> pool, T item, float delay)
        {
            if (!_isInitialized || _isDisposed)
            {
                yield break;
            }
            
            // Wait for the specified delay
            yield return new WaitForSeconds(delay);
            
            // Release the item back to the pool if we haven't been disposed
            if (_isInitialized && !_isDisposed)
            {
                try
                {
                    pool.Release(item);
                    _logger?.LogInfoInstance($"Item released after {delay}s delay");
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error releasing item after delay: {ex.Message}");
                }
            }
            
            // Remove from active coroutines
            if (_isInitialized && !_isDisposed)
            {
                _activeCoroutines.Remove(id);
                
                if (_activeCoroutineIds.IsCreated)
                {
                    _activeCoroutineIds.Remove(id);
                }
            }
        }
        
        private IEnumerator ConditionalReleaseCoroutine<T>(int id, IPool<T> pool, T item, Func<T, bool> condition)
        {
            if (!_isInitialized || _isDisposed)
            {
                yield break;
            }
            
            // Wait for one frame to ensure everything is initialized
            yield return null;
            
            // Wait until the condition is met or we're disposed
            while (_isInitialized && !_isDisposed && item != null && !condition(item))
            {
                yield return null;
            }
            
            // Release the item back to the pool if we haven't been disposed
            if (_isInitialized && !_isDisposed && item != null)
            {
                try
                {
                    pool.Release(item);
                    _logger?.LogInfoInstance($"Item released after condition met");
                }
                catch (Exception ex)
                {
                    _logger?.LogErrorInstance($"Error releasing item after condition met: {ex.Message}");
                }
            }
            
            // Remove from active coroutines
            if (_isInitialized && !_isDisposed)
            {
                _activeCoroutines.Remove(id);
                
                if (_activeCoroutineIds.IsCreated)
                {
                    _activeCoroutineIds.Remove(id);
                }
            }
        }
        
        #endregion
    }
}