using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Unity.Configuration
{
    /// <summary>
    /// Configuration for a system metric
    /// </summary>
    [Serializable]
    public class SystemMetricConfig
    {
        [SerializeField] private string _name;
        [SerializeField] private ProfilerCategory _category = ProfilerCategory.Scripts;
        [SerializeField] private string _statName;
        [SerializeField] private string _unit = "ms";
        [SerializeField] private bool _enabled = true;
        
        /// <summary>
        /// Gets the metric name
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Gets the profiler category
        /// </summary>
        public ProfilerCategory Category => _category;
        
        /// <summary>
        /// Gets the stat name for ProfilerRecorder
        /// </summary>
        public string StatName => _statName;
        
        /// <summary>
        /// Gets the unit of measurement
        /// </summary>
        public string Unit => _unit;
        
        /// <summary>
        /// Gets whether this metric is enabled
        /// </summary>
        public bool Enabled => _enabled;
    }
}