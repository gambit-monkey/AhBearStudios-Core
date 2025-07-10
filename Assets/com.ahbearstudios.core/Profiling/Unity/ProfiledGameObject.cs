using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;

namespace AhBearStudios.Core.Profiling.Unity
{
    /// <summary>
    /// Component that enables profiling for GameObject lifecycle events and methods.
    /// Automatically profiles Update, FixedUpdate, LateUpdate, and other Unity callbacks.
    /// </summary>
    public class ProfiledGameObject : MonoBehaviour
    {
        [Header("Profiling Configuration")]
        [SerializeField] private bool _enableProfiling = true;
        [SerializeField] private bool _profileUpdate = true;
        [SerializeField] private bool _profileFixedUpdate = true;
        [SerializeField] private bool _profileLateUpdate = true;
        [SerializeField] private bool _profileStart = true;
        [SerializeField] private bool _profileAwake = true;
        [SerializeField] private bool _profileOnEnable = false;
        [SerializeField] private bool _profileOnDisable = false;
        
        [Header("Custom Profiling")]
        [SerializeField] private string _customPrefix = "";
        [SerializeField] private ProfilerCategory _category = ProfilerCategory.Scripts;
        [SerializeField] private bool _includeInstanceId = true;
        [SerializeField] private bool _includeComponentCount = false;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _useDirectMarkers = true;
        [SerializeField] private int _updateFrameSkip = 0; // Skip profiling every N frames
        
        private ProfileManager _profileManager;
        private string _baseProfileName;
        private int _frameCounter;
        
        // Direct ProfilerMarkers for better performance when not using ProfileManager
        private ProfilerMarker _updateMarker;
        private ProfilerMarker _fixedUpdateMarker;
        private ProfilerMarker _lateUpdateMarker;
        private ProfilerMarker _startMarker;
        private ProfilerMarker _awakeMarker;
        private ProfilerMarker _onEnableMarker;
        private ProfilerMarker _onDisableMarker;
        
        // Custom profiling sessions when using ProfileManager
        private readonly Dictionary<string, IProfilerSession> _activeSessions = new Dictionary<string, IProfilerSession>();
        
        /// <summary>
        /// Gets whether profiling is currently enabled
        /// </summary>
        public bool ProfilingEnabled => _enableProfiling;
        
        /// <summary>
        /// Gets the base profile name used for this GameObject
        /// </summary>
        public string BaseProfileName => _baseProfileName;
        
        /// <summary>
        /// Gets the profiler category used
        /// </summary>
        public ProfilerCategory Category => _category;
        
        /// <summary>
        /// Event fired when a profiling session completes
        /// </summary>
        public event Action<string, double> SessionCompleted;
        
        private void Awake()
        {
            InitializeProfiling();
            
            if (_profileAwake && _enableProfiling)
            {
                ProfileMethod("Awake");
            }
        }
        
        private void Start()
        {
            if (_profileStart && _enableProfiling)
            {
                using (BeginProfilingScope("Start"))
                {
                    // Start method content would go here if this were a regular component
                }
            }
        }
        
        private void OnEnable()
        {
            if (_profileOnEnable && _enableProfiling)
            {
                ProfileMethod("OnEnable");
            }
        }
        
        private void Update()
        {
            if (!_profileUpdate || !_enableProfiling)
                return;
                
            // Skip frames if configured
            if (_updateFrameSkip > 0)
            {
                _frameCounter++;
                if (_frameCounter % (_updateFrameSkip + 1) != 0)
                    return;
            }
            
            if (_useDirectMarkers)
            {
                _updateMarker.Begin();
                try
                {
                    // Update logic would go here in a derived class
                    OnProfiledUpdate();
                }
                finally
                {
                    _updateMarker.End();
                }
            }
            else
            {
                using (BeginProfilingScope("Update"))
                {
                    OnProfiledUpdate();
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (!_profileFixedUpdate || !_enableProfiling)
                return;
                
            if (_useDirectMarkers)
            {
                _fixedUpdateMarker.Begin();
                try
                {
                    OnProfiledFixedUpdate();
                }
                finally
                {
                    _fixedUpdateMarker.End();
                }
            }
            else
            {
                using (BeginProfilingScope("FixedUpdate"))
                {
                    OnProfiledFixedUpdate();
                }
            }
        }
        
        private void LateUpdate()
        {
            if (!_profileLateUpdate || !_enableProfiling)
                return;
                
            if (_useDirectMarkers)
            {
                _lateUpdateMarker.Begin();
                try
                {
                    OnProfiledLateUpdate();
                }
                finally
                {
                    _lateUpdateMarker.End();
                }
            }
            else
            {
                using (BeginProfilingScope("LateUpdate"))
                {
                    OnProfiledLateUpdate();
                }
            }
        }
        
        private void OnDisable()
        {
            if (_profileOnDisable && _enableProfiling)
            {
                ProfileMethod("OnDisable");
            }
        }
        
        private void OnDestroy()
        {
            // Clean up any active sessions
            foreach (var session in _activeSessions.Values)
            {
                session?.Dispose();
            }
            _activeSessions.Clear();
        }
        
        /// <summary>
        /// Initializes the profiling system for this GameObject
        /// </summary>
        private void InitializeProfiling()
        {
            // Find or create ProfileManager
            _profileManager = FindObjectOfType<ProfileManager>();
            if (_profileManager == null)
            {
                _profileManager = ProfileManager.Instance;
            }
            
            // Generate base profile name
            GenerateBaseProfileName();
            
            // Initialize direct markers if using them
            if (_useDirectMarkers)
            {
                InitializeDirectMarkers();
            }
        }
        
        /// <summary>
        /// Generates the base profile name for this GameObject
        /// </summary>
        private void GenerateBaseProfileName()
        {
            string baseName = string.IsNullOrEmpty(_customPrefix) ? gameObject.name : _customPrefix;
            
            if (_includeInstanceId)
            {
                baseName += $"_{GetInstanceID()}";
            }
            
            if (_includeComponentCount)
            {
                var componentCount = GetComponents<Component>().Length;
                baseName += $"_C{componentCount}";
            }
            
            _baseProfileName = baseName;
        }
        
        /// <summary>
        /// Initializes direct ProfilerMarkers for better performance
        /// </summary>
        private void InitializeDirectMarkers()
        {
            _updateMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.Update");
            _fixedUpdateMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.FixedUpdate");
            _lateUpdateMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.LateUpdate");
            _startMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.Start");
            _awakeMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.Awake");
            _onEnableMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.OnEnable");
            _onDisableMarker = new ProfilerMarker($"{_category}.{_baseProfileName}.OnDisable");
        }
        
        /// <summary>
        /// Begins a profiling scope for a method
        /// </summary>
        /// <param name="methodName">Name of the method being profiled</param>
        /// <returns>Profiler session that should be disposed when the method completes</returns>
        protected IDisposable BeginProfilingScope(string methodName)
        {
            if (!_enableProfiling || _profileManager == null)
                return null;
                
            var tag = new ProfilerTag(_category, $"{_baseProfileName}.{methodName}");
            return _profileManager.BeginScope(tag);
        }
        
        /// <summary>
        /// Profiles a method call with automatic timing
        /// </summary>
        /// <param name="methodName">Name of the method being profiled</param>
        protected void ProfileMethod(string methodName)
        {
            if (!_enableProfiling)
                return;
                
            if (_useDirectMarkers)
            {
                ProfilerMarker marker = methodName switch
                {
                    "Awake" => _awakeMarker,
                    "OnEnable" => _onEnableMarker,
                    "OnDisable" => _onDisableMarker,
                    _ => new ProfilerMarker($"{_category}.{_baseProfileName}.{methodName}")
                };
                
                marker.Begin();
                marker.End(); // Immediate end since we're just marking the call
            }
            else
            {
                using (BeginProfilingScope(methodName))
                {
                    // Method execution happens in the using scope
                }
            }
        }
        
        /// <summary>
        /// Profiles a custom action
        /// </summary>
        /// <param name="actionName">Name of the action</param>
        /// <param name="action">Action to profile</param>
        protected void ProfileAction(string actionName, Action action)
        {
            if (!_enableProfiling || action == null)
            {
                action?.Invoke();
                return;
            }
            
            if (_useDirectMarkers)
            {
                var marker = new ProfilerMarker($"{_category}.{_baseProfileName}.{actionName}");
                marker.Begin();
                try
                {
                    action.Invoke();
                }
                finally
                {
                    marker.End();
                }
            }
            else
            {
                using (BeginProfilingScope(actionName))
                {
                    action.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Profiles a custom function with return value
        /// </summary>
        /// <param name="functionName">Name of the function</param>
        /// <param name="function">Function to profile</param>
        /// <returns>Result of the function</returns>
        protected T ProfileFunction<T>(string functionName, Func<T> function)
        {
            if (!_enableProfiling || function == null)
                return function != null ? function() : default;
            
            if (_useDirectMarkers)
            {
                var marker = new ProfilerMarker($"{_category}.{_baseProfileName}.{functionName}");
                marker.Begin();
                try
                {
                    return function.Invoke();
                }
                finally
                {
                    marker.End();
                }
            }
            else
            {
                using (BeginProfilingScope(functionName))
                {
                    return function.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Begins a long-running profiling session that must be manually ended
        /// </summary>
        /// <param name="sessionName">Name of the session</param>
        /// <returns>Session key for ending the session</returns>
        protected string BeginSession(string sessionName)
        {
            if (!_enableProfiling || _profileManager == null)
                return null;
                
            string sessionKey = $"{sessionName}_{Time.frameCount}";
            var tag = new ProfilerTag(_category, $"{_baseProfileName}.{sessionName}");
            var session = _profileManager.BeginScope(tag);
            
            if (session != null)
            {
                _activeSessions[sessionKey] = session;
            }
            
            return sessionKey;
        }
        
        /// <summary>
        /// Ends a long-running profiling session
        /// </summary>
        /// <param name="sessionKey">Session key returned from BeginSession</param>
        protected void EndSession(string sessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey) || !_activeSessions.TryGetValue(sessionKey, out var session))
                return;
                
            double duration = 0;
            if (session is ProfilerSession profilerSession)
            {
                duration = profilerSession.ElapsedMilliseconds;
            }
            
            session.Dispose();
            _activeSessions.Remove(sessionKey);
            
            SessionCompleted?.Invoke(sessionKey, duration);
        }
        
        /// <summary>
        /// Sets whether profiling is enabled for this GameObject
        /// </summary>
        /// <param name="enabled">Whether to enable profiling</param>
        public void SetProfilingEnabled(bool enabled)
        {
            _enableProfiling = enabled;
        }
        
        /// <summary>
        /// Sets the profiler category
        /// </summary>
        /// <param name="category">The profiler category to use</param>
        public void SetCategory(ProfilerCategory category)
        {
            _category = category;
            
            // Reinitialize markers with new category
            if (_useDirectMarkers)
            {
                InitializeDirectMarkers();
            }
        }
        
        /// <summary>
        /// Sets whether to use direct markers for better performance
        /// </summary>
        /// <param name="useDirect">Whether to use direct markers</param>
        public void SetUseDirectMarkers(bool useDirect)
        {
            _useDirectMarkers = useDirect;
            
            if (_useDirectMarkers)
            {
                InitializeDirectMarkers();
            }
        }
        
        /// <summary>
        /// Gets current profiling statistics for this GameObject
        /// </summary>
        /// <returns>Dictionary of method names to metrics</returns>
        public Dictionary<string, object> GetProfilingStats()
        {
            var stats = new Dictionary<string, object>();
            
            if (_profileManager != null)
            {
                var allMetrics = _profileManager.GetAllMetrics();
                
                foreach (var kvp in allMetrics)
                {
                    if (kvp.Key.Name.StartsWith(_baseProfileName))
                    {
                        var methodName = kvp.Key.Name.Substring(_baseProfileName.Length + 1);
                        stats[methodName] = new
                        {
                            LastValue = kvp.Value.LastValue,
                            AverageValue = kvp.Value.AverageValue,
                            MinValue = kvp.Value.MinValue,
                            MaxValue = kvp.Value.MaxValue,
                            SampleCount = kvp.Value.SampleCount
                        };
                    }
                }
            }
            
            return stats;
        }
        
        #region Virtual Methods for Override
        
        /// <summary>
        /// Called during Update when profiling is enabled.
        /// Override this in derived classes to add custom Update logic.
        /// </summary>
        protected virtual void OnProfiledUpdate()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called during FixedUpdate when profiling is enabled.
        /// Override this in derived classes to add custom FixedUpdate logic.
        /// </summary>
        protected virtual void OnProfiledFixedUpdate()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called during LateUpdate when profiling is enabled.
        /// Override this in derived classes to add custom LateUpdate logic.
        /// </summary>
        protected virtual void OnProfiledLateUpdate()
        {
            // Override in derived classes
        }
        
        #endregion
        
        #region Static Utility Methods
        
        /// <summary>
        /// Adds ProfiledGameObject component to a GameObject if it doesn't already exist
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>The ProfiledGameObject component</returns>
        public static ProfiledGameObject AddTo(GameObject gameObject, Action<ProfiledGameObject> configure = null)
        {
            if (gameObject == null)
                return null;
                
            var profiled = gameObject.GetComponent<ProfiledGameObject>();
            if (profiled == null)
            {
                profiled = gameObject.AddComponent<ProfiledGameObject>();
            }
            
            configure?.Invoke(profiled);
            return profiled;
        }
        
        /// <summary>
        /// Profiles an action on a specific GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="actionName">Name of the action</param>
        /// <param name="action">Action to profile</param>
        public static void ProfileAction(GameObject gameObject, string actionName, Action action)
        {
            var profiled = gameObject.GetComponent<ProfiledGameObject>();
            if (profiled != null)
            {
                profiled.ProfileAction(actionName, action);
            }
            else
            {
                action?.Invoke();
            }
        }
        
        /// <summary>
        /// Profiles a function on a specific GameObject
        /// </summary>
        /// <param name="gameObject">Target GameObject</param>
        /// <param name="functionName">Name of the function</param>
        /// <param name="function">Function to profile</param>
        /// <returns>Result of the function</returns>
        public static T ProfileFunction<T>(GameObject gameObject, string functionName, Func<T> function)
        {
            var profiled = gameObject.GetComponent<ProfiledGameObject>();
            if (profiled != null)
            {
                return profiled.ProfileFunction(functionName, function);
            }
            else
            {
                return function != null ? function() : default;
            }
        }
        
        #endregion
    }
}