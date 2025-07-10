using System;

namespace AhBearStudios.Core.Profiling
{
    /// <summary>
    /// Circular buffer for storing limited history
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _start;
        private int _end;
        private int _size;
        private int _capacity;
        
        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _start = 0;
            _end = 0;
            _size = 0;
            _capacity = capacity;
        }
        
        /// <summary>
        /// Add an item to the buffer
        /// </summary>
        public void Add(T item)
        {
            _buffer[_end] = item;
            
            _end = (_end + 1) % _capacity;
            
            if (_size < _capacity)
            {
                _size++;
            }
            else
            {
                // Buffer is full, move start pointer
                _start = (_start + 1) % _capacity;
            }
        }
        
        /// <summary>
        /// Get item at specified index
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _size)
                {
                    throw new IndexOutOfRangeException();
                }
                
                int actualIndex = (_start + index) % _capacity;
                return _buffer[actualIndex];
            }
        }
        
        /// <summary>
        /// Get current size of buffer
        /// </summary>
        public int Count => _size;
        
        /// <summary>
        /// Get buffer capacity
        /// </summary>
        public int Capacity => _capacity;
        
        /// <summary>
        /// Convert to array
        /// </summary>
        public T[] ToArray()
        {
            T[] result = new T[_size];
            
            for (int i = 0; i < _size; i++)
            {
                result[i] = this[i];
            }
            
            return result;
        }
    }
}