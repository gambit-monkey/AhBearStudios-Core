using System;
using AhBearStudios.Core.Pooling;

namespace AhBearStudios.Core.Pooling.Models
{
    /// <summary>
    /// Pooled network buffer for FishNet serialization operations.
    /// Implements IPooledObject for proper lifecycle management.
    /// </summary>
    public class PooledNetworkBuffer : IPooledObject, IDisposable
    {
        private byte[] _buffer;
        private int _capacity;
        private int _length;
        private bool _disposed;

        /// <summary>
        /// Gets the underlying byte array buffer.
        /// </summary>
        public byte[] Buffer => _buffer;

        /// <summary>
        /// Gets the current length of data in the buffer.
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Gets the total capacity of the buffer.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets or sets the pool name for tracking purposes.
        /// </summary>
        public string PoolName { get; set; } = "NetworkBuffer";

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// Initializes a new PooledNetworkBuffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">Initial buffer capacity</param>
        public PooledNetworkBuffer(int capacity = 4096)
        {
            _capacity = capacity;
            _buffer = new byte[capacity];
            _length = 0;
            LastUsed = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the data in the buffer.
        /// </summary>
        /// <param name="data">Data to set</param>
        /// <param name="offset">Offset in source data</param>
        /// <param name="count">Number of bytes to copy</param>
        public void SetData(byte[] data, int offset = 0, int count = -1)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (count == -1)
                count = data.Length - offset;

            EnsureCapacity(count);
            Array.Copy(data, offset, _buffer, 0, count);
            _length = count;
        }

        /// <summary>
        /// Sets the data in the buffer from a ReadOnlySpan.
        /// </summary>
        /// <param name="data">Data to set</param>
        public void SetData(ReadOnlySpan<byte> data)
        {
            EnsureCapacity(data.Length);
            data.CopyTo(_buffer.AsSpan());
            _length = data.Length;
        }

        /// <summary>
        /// Gets the data as a ReadOnlySpan.
        /// </summary>
        /// <returns>ReadOnlySpan containing the buffer data</returns>
        public ReadOnlySpan<byte> GetData()
        {
            return _buffer.AsSpan(0, _length);
        }

        /// <summary>
        /// Gets the data as a byte array (creates a copy).
        /// </summary>
        /// <returns>Copy of the buffer data</returns>
        public byte[] ToArray()
        {
            var result = new byte[_length];
            Array.Copy(_buffer, 0, result, 0, _length);
            return result;
        }

        /// <summary>
        /// Ensures the buffer has at least the specified capacity.
        /// </summary>
        /// <param name="requiredCapacity">Required capacity</param>
        public void EnsureCapacity(int requiredCapacity)
        {
            if (_capacity >= requiredCapacity)
                return;

            // Grow buffer by doubling until we meet the requirement
            var newCapacity = Math.Max(_capacity * 2, requiredCapacity);
            var newBuffer = new byte[newCapacity];
            
            if (_length > 0)
            {
                Array.Copy(_buffer, 0, newBuffer, 0, _length);
            }

            _buffer = newBuffer;
            _capacity = newCapacity;
        }

        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// </summary>
        public void OnGet()
        {
            LastUsed = DateTime.UtcNow;
            _length = 0; // Reset length but keep buffer allocated
        }

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        public void OnReturn()
        {
            LastUsed = DateTime.UtcNow;
            // Don't clear the buffer to avoid allocations, just reset length
        }

        /// <summary>
        /// Resets the buffer state for reuse.
        /// </summary>
        public void Reset()
        {
            _length = 0;
            // Keep buffer allocated to avoid GC pressure
        }

        /// <summary>
        /// Validates that the buffer is in a valid state.
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            return !_disposed && _buffer != null && _capacity > 0 && _length >= 0 && _length <= _capacity;
        }

        /// <summary>
        /// Disposes the buffer and releases memory.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _buffer = null;
                _capacity = 0;
                _length = 0;
                _disposed = true;
            }
        }
    }
}