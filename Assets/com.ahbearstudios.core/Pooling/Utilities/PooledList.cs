using System;
using System.Collections;
using System.Collections.Generic;
using AhBearStudios.Pooling.Pools.Managed;

namespace AhBearStudios.Pooling.Utilities
{
    /// <summary>
    /// A List implementation that returns itself to a pool when disposed.
    /// Fully compatible with Unity Collections v2 and optimized for performance.
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    public class PooledList<T> : IList<T>, IDisposable
    {
        /// <summary>
        /// Shared pool for all PooledList instances of the same type
        /// </summary>
        private static readonly ManagedPool<PooledList<T>> _pool = new ManagedPool<PooledList<T>>(
            () => new PooledList<T>(false),
            list => list.Clear() // Reset action for when items are returned to the pool
        );
        
        /// <summary>
        /// The inner list that stores the actual data
        /// </summary>
        private List<T> _innerList;
        
        /// <summary>
        /// Whether this list came from the pool
        /// </summary>
        private bool _isFromPool;
        
        /// <summary>
        /// Whether this list has been disposed
        /// </summary>
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the number of elements in the list
        /// </summary>
        public int Count => _innerList.Count;
        
        /// <summary>
        /// Gets a value indicating whether the list is read-only
        /// </summary>
        public bool IsReadOnly => false;
        
        /// <summary>
        /// Gets or sets the element at the specified index
        /// </summary>
        /// <param name="index">The index of the element to get or set</param>
        /// <returns>The element at the specified index</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public T this[int index]
        {
            get
            {
                ThrowIfDisposed();
                return _innerList[index];
            }
            set
            {
                ThrowIfDisposed();
                _innerList[index] = value;
            }
        }
        
        /// <summary>
        /// Creates a new pooled list
        /// </summary>
        /// <param name="isFromPool">Whether this list is from the pool</param>
        private PooledList(bool isFromPool)
        {
            _innerList = new List<T>();
            _isFromPool = isFromPool;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Creates a new pooled list with initial capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <param name="isFromPool">Whether this list is from the pool</param>
        private PooledList(int capacity, bool isFromPool)
        {
            _innerList = new List<T>(capacity);
            _isFromPool = isFromPool;
            _isDisposed = false;
        }
        
        /// <summary>
        /// Gets a pooled list from the pool
        /// </summary>
        /// <returns>A pooled list</returns>
        public static PooledList<T> Get()
        {
            var list = _pool.Acquire();
            list._isFromPool = true;
            return list;
        }
        
        /// <summary>
        /// Gets a pooled list from the pool with a specific capacity
        /// </summary>
        /// <param name="capacity">Initial capacity</param>
        /// <returns>A pooled list</returns>
        public static PooledList<T> Get(int capacity)
        {
            var list = _pool.Acquire();
            list._isFromPool = true;
            
            if (list._innerList.Capacity < capacity)
            {
                list._innerList.Capacity = capacity;
            }
            
            return list;
        }
        
        /// <summary>
        /// Creates a pooled list with the contents of a collection
        /// </summary>
        /// <param name="collection">Collection to copy</param>
        /// <returns>A pooled list with the contents of the collection</returns>
        /// <exception cref="ArgumentNullException">Thrown if collection is null</exception>
        public static PooledList<T> From(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
                
            var list = Get();
            list.AddRange(collection);
            return list;
        }
        
        /// <summary>
        /// Adds an item to the list
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public void Add(T item)
        {
            ThrowIfDisposed();
            _innerList.Add(item);
        }
        
        /// <summary>
        /// Adds a range of items to the list
        /// </summary>
        /// <param name="collection">The collection to add</param>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        /// <exception cref="ArgumentNullException">Thrown if collection is null</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            ThrowIfDisposed();
            
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
                
            _innerList.AddRange(collection);
        }
        
        /// <summary>
        /// Removes all items from the list
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public void Clear()
        {
            ThrowIfDisposed();
            _innerList.Clear();
        }
        
        /// <summary>
        /// Determines whether the list contains a specific item
        /// </summary>
        /// <param name="item">The item to locate</param>
        /// <returns>True if the item is found, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public bool Contains(T item)
        {
            ThrowIfDisposed();
            return _innerList.Contains(item);
        }
        
        /// <summary>
        /// Copies the elements to an array, starting at the specified index
        /// </summary>
        /// <param name="array">The destination array</param>
        /// <param name="arrayIndex">The index to start at</param>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        /// <exception cref="ArgumentNullException">Thrown if array is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if arrayIndex is negative</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            _innerList.CopyTo(array, arrayIndex);
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through the list
        /// </summary>
        /// <returns>An enumerator</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public IEnumerator<T> GetEnumerator()
        {
            ThrowIfDisposed();
            return _innerList.GetEnumerator();
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through the list
        /// </summary>
        /// <returns>An enumerator</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();
            return _innerList.GetEnumerator();
        }
        
        /// <summary>
        /// Determines the index of a specific item
        /// </summary>
        /// <param name="item">The item to locate</param>
        /// <returns>The index of the item if found, -1 otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public int IndexOf(T item)
        {
            ThrowIfDisposed();
            return _innerList.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts an item at the specified index
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="item">The item to insert</param>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is negative or greater than count</exception>
        public void Insert(int index, T item)
        {
            ThrowIfDisposed();
            _innerList.Insert(index, item);
        }
        
        /// <summary>
        /// Removes the first occurrence of a specific item
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if the item was removed, false otherwise</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        public bool Remove(T item)
        {
            ThrowIfDisposed();
            return _innerList.Remove(item);
        }
        
        /// <summary>
        /// Removes the item at the specified index
        /// </summary>
        /// <param name="index">The index to remove at</param>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is negative or greater than or equal to count</exception>
        public void RemoveAt(int index)
        {
            ThrowIfDisposed();
            _innerList.RemoveAt(index);
        }
        
        /// <summary>
        /// Returns this list to the pool
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _innerList.Clear();
            
            if (_isFromPool)
            {
                _pool.Release(this);
            }
            
            _isDisposed = true;
        }
        
        /// <summary>
        /// Throws if the list is disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the list has been disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}