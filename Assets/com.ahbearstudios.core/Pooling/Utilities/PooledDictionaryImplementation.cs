using System;
using System.Collections;
using System.Collections.Generic;
using AhBearStudios.Pooling.Pools.Managed;

namespace AhBearStudios.Pooling.Utilities
{
    /// <summary>
    /// A Dictionary implementation that returns itself to a pool when disposed.
    /// Fully compatible with Unity Collections v2 and optimized for performance.
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary</typeparam>
    public class PooledDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        /// <summary>
        /// Shared pool for all PooledDictionary instances of the same generic types
        /// </summary>
        private static readonly ManagedPool<PooledDictionary<TKey, TValue>> _pool = new ManagedPool<PooledDictionary<TKey, TValue>>(
            () => new PooledDictionary<TKey, TValue>(false),
            dict => dict.Clear() // Reset action for when items are returned to the pool
        );
        
        /// <summary>
        /// The inner dictionary that stores the actual data
        /// </summary>
        private Dictionary<TKey, TValue> _innerDictionary;
        
        /// <summary>
        /// Whether this dictionary came from the pool
        /// </summary>
        private bool _isFromPool;
        
        /// <summary>
        /// Whether this dictionary has been disposed
        /// </summary>
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the number of elements in the dictionary
        /// </summary>
        public int Count => _innerDictionary.Count;
        
        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only
        /// </summary>
        public bool IsReadOnly => false;
        
        /// <summary>
        /// Gets a collection containing the keys
        /// </summary>
        public ICollection<TKey> Keys => _innerDictionary.Keys;
        
        /// <summary>
        /// Gets a collection containing the values
        /// </summary>
        public ICollection<TValue> Values => _innerDictionary.Values;
        
        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get or set</param>
        /// <returns>The value associated with the key</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the key doesn't exist when getting a value</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public TValue this[TKey key]
        {
            get
            {
                ThrowIfDisposed();
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                return _innerDictionary[key];
            }
            set
            {
                ThrowIfDisposed();
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                _innerDictionary[key] = value;
            }
        }
        
        /// <summary>
        /// Creates a new pooled dictionary
        /// </summary>
        /// <param name="isFromPool">Whether this dictionary is from the pool</param>
        private PooledDictionary(bool isFromPool)
        {
            _innerDictionary = new Dictionary<TKey, TValue>();
            _isFromPool = isFromPool;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new pooled dictionary with initial capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="isFromPool">Whether this dictionary is from the pool</param>
        private PooledDictionary(int capacity, bool isFromPool)
        {
            _innerDictionary = new Dictionary<TKey, TValue>(capacity);
            _isFromPool = isFromPool;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new pooled dictionary with a custom equality comparer
        /// </summary>
        /// <param name="comparer">Equality comparer to use</param>
        /// <param name="isFromPool">Whether this dictionary is from the pool</param>
        private PooledDictionary(IEqualityComparer<TKey> comparer, bool isFromPool)
        {
            _innerDictionary = new Dictionary<TKey, TValue>(comparer);
            _isFromPool = isFromPool;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new pooled dictionary with initial capacity and a custom equality comparer
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="comparer">Equality comparer to use</param>
        /// <param name="isFromPool">Whether this dictionary is from the pool</param>
        private PooledDictionary(int capacity, IEqualityComparer<TKey> comparer, bool isFromPool)
        {
            _innerDictionary = new Dictionary<TKey, TValue>(capacity, comparer);
            _isFromPool = isFromPool;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Gets a pooled dictionary from the pool
        /// </summary>
        /// <returns>A pooled dictionary</returns>
        public static PooledDictionary<TKey, TValue> Get()
        {
            var dict = _pool.Acquire();
            dict._isFromPool = true;
            return dict;
        }
        
        /// <summary>
        /// Gets a pooled dictionary from the pool with a specific capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <returns>A pooled dictionary</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        public static PooledDictionary<TKey, TValue> Get(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative");
                
            var dict = _pool.Acquire();
            dict._isFromPool = true;
            
            // Can't resize a dictionary, so recreate if needed
            if (dict._innerDictionary.Count > 0 || dict._innerDictionary.Comparer != EqualityComparer<TKey>.Default)
            {
                dict._innerDictionary = new Dictionary<TKey, TValue>(capacity);
            }
            
            return dict;
        }
        
        /// <summary>
        /// Gets a pooled dictionary from the pool with a custom equality comparer
        /// </summary>
        /// <param name="comparer">Equality comparer to use</param>
        /// <returns>A pooled dictionary</returns>
        /// <exception cref="ArgumentNullException">Thrown if comparer is null</exception>
        public static PooledDictionary<TKey, TValue> Get(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
                
            var dict = _pool.Acquire();
            dict._isFromPool = true;
            
            // If the dictionary has items or a different comparer, recreate it
            if (dict._innerDictionary.Count > 0 || dict._innerDictionary.Comparer != comparer)
            {
                dict._innerDictionary = new Dictionary<TKey, TValue>(comparer);
            }
            
            return dict;
        }
        
        /// <summary>
        /// Gets a pooled dictionary from the pool with a specific capacity and custom equality comparer
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="comparer">Equality comparer to use</param>
        /// <returns>A pooled dictionary</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity is negative</exception>
        /// <exception cref="ArgumentNullException">Thrown if comparer is null</exception>
        public static PooledDictionary<TKey, TValue> Get(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative");
                
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
                
            var dict = _pool.Acquire();
            dict._isFromPool = true;
            
            // Always recreate with the specified capacity and comparer
            dict._innerDictionary = new Dictionary<TKey, TValue>(capacity, comparer);
            
            return dict;
        }
        
        /// <summary>
        /// Creates a pooled dictionary with the contents of another dictionary
        /// </summary>
        /// <param name="dictionary">Dictionary to copy</param>
        /// <returns>A pooled dictionary with the contents of the other dictionary</returns>
        /// <exception cref="ArgumentNullException">Thrown if dictionary is null</exception>
        public static PooledDictionary<TKey, TValue> From(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
                
            var dict = Get();
            
            foreach (var kvp in dictionary)
            {
                dict.Add(kvp.Key, kvp.Value);
            }
            
            return dict;
        }
        
        /// <summary>
        /// Creates a pooled dictionary with the contents of another dictionary and using the specified comparer
        /// </summary>
        /// <param name="dictionary">Dictionary to copy</param>
        /// <param name="comparer">Equality comparer to use</param>
        /// <returns>A pooled dictionary with the contents of the other dictionary</returns>
        /// <exception cref="ArgumentNullException">Thrown if dictionary or comparer is null</exception>
        public static PooledDictionary<TKey, TValue> From(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
                
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
                
            var dict = Get(comparer);
            
            foreach (var kvp in dictionary)
            {
                dict.Add(kvp.Key, kvp.Value);
            }
            
            return dict;
        }
        
        /// <summary>
        /// Adds the specified key and value to the dictionary
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        /// <exception cref="ArgumentException">Thrown if the key already exists</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public void Add(TKey key, TValue value)
        {
            ThrowIfDisposed();
            _innerDictionary.Add(key, value);
        }
        
        /// <summary>
        /// Adds the specified key-value pair to the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to add</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        /// <exception cref="ArgumentException">Thrown if the key already exists</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ThrowIfDisposed();
            ((IDictionary<TKey, TValue>)_innerDictionary).Add(item);
        }
        
        /// <summary>
        /// Removes all keys and values from the dictionary
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public void Clear()
        {
            ThrowIfDisposed();
            _innerDictionary.Clear();
        }
        
        /// <summary>
        /// Determines whether the dictionary contains a specific key-value pair
        /// </summary>
        /// <param name="item">The key-value pair to locate</param>
        /// <returns>True if the pair is found, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            ThrowIfDisposed();
            return ((IDictionary<TKey, TValue>)_innerDictionary).Contains(item);
        }
        
        /// <summary>
        /// Determines whether the dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate</param>
        /// <returns>True if the key is found, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public bool ContainsKey(TKey key)
        {
            ThrowIfDisposed();
            
            if (key == null)
                throw new ArgumentNullException(nameof(key));
                
            return _innerDictionary.ContainsKey(key);
        }
        
        /// <summary>
        /// Copies the elements to an array, starting at the specified index
        /// </summary>
        /// <param name="array">The destination array</param>
        /// <param name="arrayIndex">The index to start at</param>
        /// <exception cref="ArgumentNullException">Thrown if array is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if arrayIndex is negative</exception>
        /// <exception cref="ArgumentException">Thrown if not enough space in the array</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            
            if (array == null)
                throw new ArgumentNullException(nameof(array));
                
            ((IDictionary<TKey, TValue>)_innerDictionary).CopyTo(array, arrayIndex);
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through the dictionary
        /// </summary>
        /// <returns>An enumerator</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            ThrowIfDisposed();
            return _innerDictionary.GetEnumerator();
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through the dictionary
        /// </summary>
        /// <returns>An enumerator</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();
            return _innerDictionary.GetEnumerator();
        }
        
        /// <summary>
        /// Removes the element with the specified key
        /// </summary>
        /// <param name="key">The key of the element to remove</param>
        /// <returns>True if the element was removed, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public bool Remove(TKey key)
        {
            ThrowIfDisposed();
            
            if (key == null)
                throw new ArgumentNullException(nameof(key));
                
            return _innerDictionary.Remove(key);
        }
        
        /// <summary>
        /// Removes the first occurrence of a specific key-value pair
        /// </summary>
        /// <param name="item">The key-value pair to remove</param>
        /// <returns>True if the pair was removed, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            ThrowIfDisposed();
            return ((IDictionary<TKey, TValue>)_innerDictionary).Remove(item);
        }
        
        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key whose value to get</param>
        /// <param name="value">When this method returns, the value associated with the key, if found</param>
        /// <returns>True if the key was found, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if key is null</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            ThrowIfDisposed();
            
            if (key == null)
                throw new ArgumentNullException(nameof(key));
                
            return _innerDictionary.TryGetValue(key, out value);
        }
        
        /// <summary>
        /// Returns this dictionary to the pool
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _innerDictionary.Clear();
            
            if (_isFromPool)
            {
                _pool.Release(this);
            }
            
            _isDisposed = true;
        }
        
        /// <summary>
        /// Throws if the dictionary is disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}