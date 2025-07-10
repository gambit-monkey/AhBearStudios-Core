using System.Collections.Generic;
using AhBearStudios.Core.Profiling.Data;
using Unity.Collections;

namespace AhBearStudios.Core.Profiling.Metrics
{
    /// <summary>
    /// Collection for managing profiler metrics of different types
    /// </summary>
    public class ProfilerStatsCollection
    {
        // Core metrics for general profiling
        private readonly Dictionary<ProfilerTag, DefaultMetricsData> _generalMetrics = new Dictionary<ProfilerTag, DefaultMetricsData>();
        
        // Specialized metrics for pools
        private readonly Dictionary<FixedString64Bytes, PoolMetricsData> _poolMetrics = new Dictionary<FixedString64Bytes, PoolMetricsData>();
        
        // Specialized metrics for serializers
        private readonly Dictionary<string, SerializerMetricsData> _serializerMetrics = new Dictionary<string, SerializerMetricsData>();
        
        /// <summary>
        /// Get general metrics for a tag
        /// </summary>
        public DefaultMetricsData GetMetrics(ProfilerTag tag)
        {
            if (_generalMetrics.TryGetValue(tag, out var metrics))
                return metrics;
                
            var newMetrics = new DefaultMetricsData();
            _generalMetrics[tag] = newMetrics;
            return newMetrics;
        }
        
        /// <summary>
        /// Add a sample to general metrics
        /// </summary>
        public void AddSample(ProfilerTag tag, double value)
        {
            var metrics = GetMetrics(tag);
            metrics.AddSample(value);
            _generalMetrics[tag] = metrics; // Reassign struct
        }
        
        /// <summary>
        /// Get pool metrics for a pool ID
        /// </summary>
        public PoolMetricsData GetPoolMetrics(FixedString64Bytes poolId)
        {
            if (_poolMetrics.TryGetValue(poolId, out var metrics))
                return metrics;
                
            return default;
        }
        
        /// <summary>
        /// Update pool metrics
        /// </summary>
        public void UpdatePoolMetrics(PoolMetricsData poolMetrics)
        {
            _poolMetrics[poolMetrics.PoolId] = poolMetrics;
        }
        
        /// <summary>
        /// Get serializer metrics for a serializer ID
        /// </summary>
        public SerializerMetricsData GetSerializerMetrics(string serializerId)
        {
            if (_serializerMetrics.TryGetValue(serializerId, out var metrics))
                return metrics;
                
            return default;
        }
        
        /// <summary>
        /// Update serializer metrics
        /// </summary>
        public void UpdateSerializerMetrics(string serializerId, SerializerMetricsData metrics)
        {
            _serializerMetrics[serializerId] = metrics;
        }
        
        /// <summary>
        /// Get all general metrics
        /// </summary>
        public IReadOnlyDictionary<ProfilerTag, DefaultMetricsData> GetAllGeneralMetrics()
        {
            return _generalMetrics;
        }
        
        /// <summary>
        /// Get all pool metrics
        /// </summary>
        public IReadOnlyDictionary<FixedString64Bytes, PoolMetricsData> GetAllPoolMetrics()
        {
            return _poolMetrics;
        }
        
        /// <summary>
        /// Get all serializer metrics
        /// </summary>
        public IReadOnlyDictionary<string, SerializerMetricsData> GetAllSerializerMetrics()
        {
            return _serializerMetrics;
        }
        
        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void Reset()
        {
            _generalMetrics.Clear();
            _poolMetrics.Clear();
            _serializerMetrics.Clear();
        }
    }
}