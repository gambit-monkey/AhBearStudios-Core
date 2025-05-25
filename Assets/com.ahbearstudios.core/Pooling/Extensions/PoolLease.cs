using System;

namespace AhBearStudios.Core.Pooling.Extensions
{
    /// <summary>
    /// Represents a lease for a pooled object that automatically returns it to the pool when disposed.
    /// Helps ensure pool items are properly returned using the 'using' pattern.
    /// </summary>
    /// <typeparam name="T">Type of leased item</typeparam>
    public readonly struct PoolLease<T> : IDisposable
    {
        private readonly IPool<T> _pool;
        private readonly T _item;
        private readonly bool _isValid;
        
        /// <summary>
        /// Gets the leased item
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if trying to access the item after the lease is disposed</exception>
        /// <exception cref="InvalidOperationException">Thrown if the lease is invalid</exception>
        public T Item
        {
            get
            {
                if (!_isValid)
                    throw new InvalidOperationException("This pool lease is invalid or has been disposed");
                    
                return _item;
            }
        }
        
        /// <summary>
        /// Creates a new lease for a pooled item
        /// </summary>
        /// <param name="pool">The pool the item was acquired from</param>
        /// <param name="item">The leased item</param>
        /// <exception cref="ArgumentNullException">Thrown if the pool is null</exception>
        public PoolLease(IPool<T> pool, T item)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _item = item;
            _isValid = true;
        }
        
        /// <summary>
        /// Returns the item to the pool
        /// </summary>
        public void Dispose()
        {
            if (_isValid && _pool != null && _item != null)
            {
                try
                {
                    _pool.Release(_item);
                }
                catch (Exception)
                {
                    // Suppress exceptions during disposal to follow .NET best practices
                    // Exceptions during Dispose should not propagate
                }
            }
        }
    }
}