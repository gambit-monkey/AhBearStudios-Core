using System;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Core.Profiling.Unity.Configuration
{
    /// <summary>
    /// Configuration for a threshold alert
    /// </summary>
    [Serializable]
    public class ThresholdAlertConfig
    {
        [SerializeField] private string _name;
        [SerializeField] private ProfilerCategory _category = ProfilerCategory.Scripts;
        [SerializeField] private double _threshold;
        [SerializeField] private float _cooldownSeconds = 5.0f;
        [SerializeField] private bool _isSessionAlert = false;
        [SerializeField] private bool _enabled = true;
        
        /// <summary>
        /// Gets the alert name
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Gets the profiler category
        /// </summary>
        public ProfilerCategory Category => _category;
        
        /// <summary>
        /// Gets the threshold value
        /// </summary>
        public double Threshold => _threshold;
        
        /// <summary>
        /// Gets the cooldown period in seconds
        /// </summary>
        public float CooldownSeconds => _cooldownSeconds;
        
        /// <summary>
        /// Gets whether this is a session alert (vs metric alert)
        /// </summary>
        public bool IsSessionAlert => _isSessionAlert;
        
        /// <summary>
        /// Gets whether this alert is enabled
        /// </summary>
        public bool Enabled => _enabled;
    }
}