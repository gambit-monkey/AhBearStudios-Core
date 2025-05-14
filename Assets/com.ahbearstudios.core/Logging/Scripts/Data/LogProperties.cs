using System;
using Unity.Collections;

namespace AhBearStudios.Core.Logging.Data
{
    /// <summary>
    /// A Burst-compatible container for structured logging properties.
    /// Stores key-value pairs using native collections.
    /// </summary>
    public struct LogProperties : IEquatable<LogProperties>
    {
        // Key-value pairs for structured data
        private NativeParallelHashMap<FixedString64Bytes, FixedString128Bytes> _properties;
        
        public bool IsCreated => _properties.IsCreated;
        
        public LogProperties(int initialCapacity)
        {
            _properties = new NativeParallelHashMap<FixedString64Bytes, FixedString128Bytes>(
                initialCapacity, Allocator.Persistent);
        }
        
        public void Add(FixedString64Bytes key, FixedString128Bytes value)
        {
            if (!_properties.IsCreated) return;
            
            if (_properties.ContainsKey(key))
                _properties[key] = value;
            else
                _properties.Add(key, value);
        }
        
        public bool TryGetValue(FixedString64Bytes key, out FixedString128Bytes value)
        {
            if (!_properties.IsCreated)
            {
                value = default;
                return false;
            }
            
            return _properties.TryGetValue(key, out value);
        }
        
        public NativeParallelHashMap<FixedString64Bytes, FixedString128Bytes>.Enumerator GetEnumerator()
        {
            return _properties.GetEnumerator();
        }
        
        public void Dispose()
        {
            if (_properties.IsCreated)
                _properties.Dispose();
        }
        
        public bool Equals(LogProperties other)
        {
            if (!_properties.IsCreated && !other._properties.IsCreated)
                return true;
            
            if (_properties.IsCreated != other._properties.IsCreated)
                return false;
            
            if (_properties.Count() != other._properties.Count())
                return false;
            
            foreach (var kvp in _properties)
            {
                if (!other._properties.TryGetValue(kvp.Key, out var value))
                    return false;
                
                if (!value.Equals(kvp.Value))
                    return false;
            }
            
            return true;
        }
        
        public override bool Equals(object obj)
        {
            return obj is LogProperties other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return _properties.IsCreated ? _properties.Count().GetHashCode() : 0;
        }
    }
}